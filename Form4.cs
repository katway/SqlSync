using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Data.OracleClient;
using System.Xml.Serialization;
using System.IO;
using System.Threading;
using log4net;

namespace SqlSync
{
    public partial class Form4 : Form
    {
        Thread SyncThread;
        log4net.ILog log;
        public Form4()
        {
            InitializeComponent();
            log = LogManager.GetLogger(this.GetType());
        }


        private void btnCopy_Click(object sender, EventArgs e)
        {
            Config c = new Config();
            //c.LocalConnectionString = @"server=localhost;uid=sa;pwd='123456';database='ZhiFY'";
            //c.RemoteConnectionString = @"Data Source=orcl;Persist Security Info=True;User ID=zhify;Password=zhify;";
            ////c.SyncTables.Add(new SyncTable("Employee", "outid"));
            //c.SyncTables.Add(new SyncTable("Company", "norder", SyncDirection.Sync));
            //c.SyncTables[0].Key.Add("key1");
            //c.SyncTables[0].Key.Add("key2");
            //c.SyncTables[0].Key.Add("key3");
            //Helper.SaveConfig(c);
            c = Helper.ReadConfig();

            //更新状态栏
            this.stsTables.Text = string.Format(@"0/{0}", c.SyncTables.Count);

            SyncThread = new Thread(
                             delegate()
                             {
                                 while (true)
                                 {
                                     log.Info("开始首次同步");
                                     try
                                     { TransferData(c); }
                                     catch (Exception ex)
                                     {
                                         log.Error(ex);
                                     }
                                     Thread.Sleep(1000 * 6);
                                 }
                             });
            SyncThread.Start();
            this.btnCopy.Enabled = false;
        }


        public void TransferData(Config config)
        {
            DataSet ds;

            //建立到源数据的连接
            SqlConnection sqlConn = new SqlConnection(config.LocalConnectionString);
            OracleConnection oraConn = new OracleConnection(config.RemoteConnectionString);

            StringBuilder selectSql = new StringBuilder();
            DbDataAdapter myCommand = null;
            foreach (var tab in config.SyncTables)
            {
                log.Info(string.Format("开始同步{0}到{1},方向:{2}.", tab.MasterTable, tab.SlaveTable, tab.Direction));
                //更新状态栏
                this.Invoke(new MethodInvoker(
                        delegate()
                        {
                            this.stsTables.Text = string.Format(@"{0}/{1}", config.SyncTables.IndexOf(tab) + 1, config.SyncTables.Count);
                            this.stslTable.Text = tab.ToString();
                        }));

                sqlConn.Open();
                oraConn.Open();

                #region 检查并添加同步字段
                if (config.AppendSyncFields)
                {
                    try
                    {
                        Helper.AppendSyncFields(sqlConn, tab);
                        Helper.AppendSyncFields(oraConn, tab);
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.Message);
                        this.Invoke(new MethodInvoker(
                            delegate()
                            {
                                if (this.txtLog.Text.Length > 1024 * 1024 * 10)
                                    this.txtLog.Text = string.Empty;
                                this.txtLog.Text += string.Format("向表{0}中添加同步字段失败，请手动添加。\r\n"
                                                                    + "alter table {0}|{1} add {2} int default 0 not null;\r\n"
                                                                    + "alter table {0}|{1} add {3} int default 0 not null;\r\n"
                                                                    + "{4}\r\n\r\n",
                                                                    tab.MasterTable.ToUpper(),
                                                                    tab.SlaveTable.ToUpper(),
                                                     tab.SyncStateField.ToUpper(),
                                                     tab.SyncErrorsField.ToUpper(),
                                                     ex.Message
                                                     );
                            }));
                    }
                }
                #endregion

                ///下面进行单向同步
                if ((tab.Direction == SyncDirection.Push) || (tab.Direction == SyncDirection.Sync))
                {

                    ds = new DataSet();
                    //读取源数据
                    myCommand = new SqlDataAdapter(tab.GetQueryString(tab.MasterTable), sqlConn);
                    myCommand.Fill(ds, tab.MasterTable);
                    DataTable dt = ds.Tables[tab.MasterTable];

                    //更新状态栏
                    this.Invoke(new MethodInvoker(
                            delegate()
                            {
                                stpProgress.Maximum = dt.Rows.Count;
                                stslRows.Text = string.Format(@"{0}/{1}", 0, dt.Rows.Count);
                            }));

                    //写入目标库
                    dt.TableName = tab.SlaveTable;
                    var resut = InsertData(oraConn, DatabaseType.Oracle, dt, tab);

                    //更新源数据状态
                    foreach (DataRow row in resut.Rows)
                        Helper.UpdateSyncState(SyncDirection.Push, sqlConn, tab, row);
                    log.Info(string.Format("方向:{0},需同步纪录数:{1},处理纪录数:{2}.", SyncDirection.Push, dt.Rows.Count, resut.Rows));
                }

                ///下面进行异向同步
                if ((tab.Direction == SyncDirection.Pull) || tab.Direction == SyncDirection.Sync)
                {
                    ds = new DataSet();
                    //读取源数据
                    myCommand = new OracleDataAdapter(tab.GetQueryString(tab.SlaveTable), oraConn);
                    myCommand.Fill(ds, tab.SlaveTable);
                    DataTable dt = ds.Tables[tab.SlaveTable];

                    //更新状态栏
                    this.Invoke(new MethodInvoker(
                            delegate()
                            {
                                stpProgress.Maximum = dt.Rows.Count;
                                stslRows.Text = string.Format(@"{0}/{1}", 0, dt.Rows.Count);
                            }));

                    //写入目标库
                    dt.TableName = tab.MasterTable;
                    var resut = InsertData(sqlConn, DatabaseType.MsSql, dt, tab);

                    //更新源数据状态
                    foreach (DataRow row in resut.Rows)
                        Helper.UpdateSyncState(SyncDirection.Pull, oraConn, tab, row);
                    log.Info(string.Format("方向:{0},需同步纪录数:{1},处理纪录数:{2}.", SyncDirection.Pull, dt.Rows.Count, resut.Rows));
                }
                sqlConn.Close();
                oraConn.Close();
            }
        }


        /// <summary>
        /// 将指定的数据表中的数据写入目标数据库
        /// 不要嫌这个过程写得太长太复杂，都是因为功能是一步步增加的，有能力的重构吧
        /// </summary>
        /// <param name="destConn"></param>
        /// <param name="dt"></param>
        /// <param name="tab"></param>
        /// <returns></returns>
        private DataTable InsertData(DbConnection destConn, DatabaseType dbType, DataTable dt, SyncTable tab)
        {
            //Dictionary<string, SyncState> result = new Dictionary<string, SyncState>();
            DataTable res = new DataTable("result");
            foreach (var k in tab.Key)
                res.Columns.Add(k);
            res.Columns.Add("SyncState");

            //预备数据转换到SQL的表示规则
            Dictionary<Type, string> dataFormat = Helper.DataSqlFormat(dbType);
            DbCommand dbCommand = Helper.GetDbCommand(destConn);

            if (dbCommand != null)
                foreach (DataRow row in dt.Rows)
                {
                    //更新状态栏
                    this.Invoke(new MethodInvoker(
                            delegate()
                            {
                                int index = dt.Rows.IndexOf(row) + 1;
                                stslRows.Text = string.Format(@"{0}/{1}", index, dt.Rows.Count);
                                stpProgress.Value = index;
                            }));
                    //如果连接故障,跳过其余条目
                    if (destConn.State != ConnectionState.Open)
                        continue;

                    StringBuilder updateSql = new StringBuilder();
                    StringBuilder whereSql = new StringBuilder();

                    StringBuilder insertSql = new StringBuilder();
                    StringBuilder valueSql = new StringBuilder();

                    updateSql.AppendFormat(@"UPDATE {0} SET ", dt.TableName);
                    whereSql.Append(@" WHERE 1=1");

                    insertSql.AppendFormat(@"insert into {0} (", dt.TableName);
                    valueSql.AppendFormat(@" values(");
                    foreach (DataColumn col in dt.Columns)
                    {
                        //如果是要被忽略的字段,则跳过本列
                        if (tab.IgnoreFields.Contains(col.ColumnName.ToLower()))
                            continue;
                        updateSql.AppendFormat(@"{0}=", col.ColumnName);

                        insertSql.AppendFormat(@"{0},", col.ColumnName);
                        if (row[col.ColumnName] != System.DBNull.Value)
                        {
                            if (tab.Key.Contains(col.ColumnName.ToLower()))
                                whereSql.AppendFormat(" AND {0} = {1}", col.ColumnName,
                                                                    string.Format(dataFormat[col.DataType], row[col.ColumnName].ToString()));
                            if (col.DataType != typeof(bool))
                            {
                                updateSql.AppendFormat(dataFormat[col.DataType], row[col.ColumnName].ToString()).Append(",");
                                valueSql.AppendFormat(dataFormat[col.DataType], row[col.ColumnName].ToString()).Append(",");
                            }
                            else
                            {
                                updateSql.AppendFormat((((bool)row[col.ColumnName] == true) ? 1 : 0).ToString()).Append(",");
                                valueSql.AppendFormat((((bool)row[col.ColumnName] == true) ? 1 : 0).ToString()).Append(",");
                            }
                        }
                        else
                        {
                            updateSql.AppendFormat("Null,");
                            valueSql.AppendFormat("Null,");
                        }
                    }
                    //添加同步字段的数据,用于在目标表中标记该纪录无需反向同步
                    updateSql.AppendFormat(@"{0}={1},", tab.SyncStateField, (int)SyncState.Sync);
                    insertSql.AppendFormat(@"{0},", tab.SyncStateField);
                    valueSql.AppendFormat(dataFormat[typeof(int)], (int)SyncState.Sync).Append(",");

                    updateSql.AppendFormat(@"{0}=0,", tab.SyncErrorsField);
                    insertSql.AppendFormat(@"{0},", tab.SyncErrorsField);
                    valueSql.AppendFormat(dataFormat[typeof(int)], 0).Append(",");

                    //拼接Update语句和where条件
                    updateSql.Remove(updateSql.Length - 1, 1).Append(whereSql.ToString());
                    //拼接insert和values语句
                    insertSql.Remove(insertSql.Length - 1, 1).Append(")");
                    valueSql.Remove(valueSql.Length - 1, 1).Append(")");
                    insertSql.Append(valueSql);

                    dbCommand.Connection = destConn;


                    DataRow newRow = res.NewRow();
                    SyncState rowState = SyncState.UnSync;
                    try
                    {
                        dbCommand.CommandText = updateSql.ToString();
                        int r = dbCommand.ExecuteNonQuery();
                        //如果更新条目为0，才执行插入操作
                        if (r <= 0)
                        {
                            dbCommand.CommandText = insertSql.ToString();
                            r = r | dbCommand.ExecuteNonQuery();
                        }

                        //如果执行成功
                        rowState = (r > 0) ? SyncState.Sync : SyncState.Error;
                    }
                    catch (Exception ex)
                    {
                        string err = dbCommand.CommandText + "\r\n" + ex.Message + "\r\n\r\n";
                        log.Error(err);
                        this.Invoke(new MethodInvoker(
                            delegate()
                            { this.txtLog.Text += (err); }));

                        rowState = SyncState.Error;
                    }

                    foreach (var k in tab.Key)
                        newRow[k] = row[k].ToString();
                    newRow["SyncState"] = (int)rowState;

                    res.Rows.Add(newRow);
                }
            return res;
        }

        private void Form4_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (SyncThread != null)
                SyncThread.Abort();
            Environment.Exit(Environment.ExitCode);
        }
    }
}
