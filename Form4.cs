using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Data.OracleClient;
using System.Threading;
using log4net;
using System.Linq;
using SqlSync.Extensions;

namespace SqlSync
{
    public partial class Form4 : Form
    {
        List<Thread> SyncThreads = new List<Thread>();
        log4net.ILog log;

        public Form4()
        {
            InitializeComponent();
            log = LogManager.GetLogger(this.GetType());
        }


        private void btnCopy_Click(object sender, EventArgs e)
        {
            Config c = new Config();
            c.LocalConnectionString = @"server=192.168.10.165;uid=sa;pwd=123456;database=zhify_sync";
            c.RemoteConnectionString = @"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.10.114)(PORT=1521)))(CONNECT_DATA=(SID = orcl)));User Id=ZHIFY;Password=ZHIFY;";
            //c.SyncTables.Add(new SyncTable("Employee", "outid"));
            c.SyncTables.Add(new SyncTable("Company", "norder", SyncDirection.Sync));
            c.SyncTables[0].Key.Add("key1");
            c.SyncTables[0].Key.Add("key2");
            c.SyncTables[0].Key.Add("key3");
            Helper.SaveConfig(c);
            c = Helper.ReadConfig();

            //c = c.SyncInfo.Enable ? ConfigLinkSyncLog(c) : c;
            if (c.SyncInfo.AutoCreate)
                Helper.CreateSyncInfoTable();

            //更新状态栏
            this.stsTables.Text = string.Format(@"0/{0}", c.SyncTables.Count);

            ///取得所有优先级的列表
            var priorityList = c.SyncTables.GroupBy(t => t.Priority).ToList();

            foreach (var pr in priorityList)
            {
                ///该优先级下的所有需同步表
                List<SyncTable> ts = (from table in c.SyncTables
                                      where table.Priority == pr.Key
                                      select table).ToList();

                Thread th = new Thread(delegate ()
                {
                    while (true)
                    {
                        log.Info(string.Format("{0}优先级线程开始执行同步", pr.Key));
                        try
                        { TransferData(c, ts); }
                        catch (Exception ex)
                        {
                            log.Error(ex);
                        }
                        Thread.Sleep(pr.Key.DelayTime());
                    }
                });

                th.Start();
                SyncThreads.Add(th);
            }
            //this.btnStart.Enabled = false;
            //this.btnStart.Visible = false;
            ((Control)sender).Enabled = false;
            ((Control)sender).Visible = false;
            this.btnPause.Enabled = true;
            this.btnPause.Visible = true;

            this.btnStop.Enabled = true;
        }


        public Config ConfigLinkSyncLog(Config config)
        {
            if (!config.SyncInfo.Enable) return config;

            DbConnection masterConn = Helper.GetDbConnection(DatabaseType.MsSql);
            DbConnection slaveConn = Helper.GetDbConnection(DatabaseType.Oracle);
            masterConn.ConnectionString = config.LocalConnectionString;
            slaveConn.ConnectionString = config.RemoteConnectionString;

            masterConn.Open();
            slaveConn.Open();
            DataSet ds = null;
            Helper.GetDbDataAdapter(string.Format("select * from {0}", config.SyncInfo.TableName)
                                        , masterConn)
                                   .Fill(ds);
            config.SyncInfo.SyncLogsMaster = ds.Tables[0].ToList<SyncLog>(SyncLog.Mappings);

            Helper.GetDbDataAdapter(string.Format("select * from {0}", config.SyncInfo.TableName)
                                        , slaveConn)
                                  .Fill(ds);
            config.SyncInfo.SyncLogsMaster = ds.Tables[0].ToList<SyncLog>(SyncLog.Mappings);



            if (masterConn.State == ConnectionState.Open) masterConn.Close();
            if (masterConn.State == ConnectionState.Open) slaveConn.Close();
            return null;
        }

        public void TransferData(Config config, List<SyncTable> tables)
        {
            //建立到源数据的连接
            SqlConnection sqlConn = new SqlConnection(config.LocalConnectionString);
            OracleConnection oraConn = new OracleConnection(config.RemoteConnectionString);

            foreach (var tab in tables)
            {

                log.Info(string.Format("开始同步{0}到{1},方向:{2}.", tab.MasterTable, tab.SlaveTable, tab.Direction));
                //更新状态栏
                this.Invoke(new MethodInvoker(
                        delegate ()
                        {
                            this.stsTables.Text = string.Format(@"{0}/{1}", tables.IndexOf(tab) + 1, tables.Count);
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
                            delegate ()
                            {
                                if (this.txtLog.Text.Length > 1024 * 1024 * 10)
                                    this.txtLog.Text = string.Empty;
                                this.txtLog.Text += string.Format("向表{0}/{1}中添加同步字段失败，请手动添加。\r\n"
                                                                    + "alter table {0}/{1} add {2} int default 0 not null;\r\n"
                                                                    + "alter table {0}/{1} add {3} int default 0 not null;\r\n"
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
                    if (GetDirectionBySyncLog(config, tab, sqlConn, oraConn) == SyncDirection.Push)
                    {
                        PushData(tab, sqlConn, oraConn);
                        Helper.UpdateSyncInfo(tab.SyncLogsMaster, oraConn);

                    }
                }

                ///下面进行异向同步
                if ((tab.Direction == SyncDirection.Pull) || tab.Direction == SyncDirection.Sync)
                {
                    if (GetDirectionBySyncLog(config, tab, sqlConn, oraConn) == SyncDirection.Pull)
                    {
                        PullData(tab, sqlConn, oraConn);
                        Helper.UpdateSyncInfo(tab.SyncLogsSlave, sqlConn);
                    }
                }
                sqlConn.Close();
                oraConn.Close();
            }
        }

        private SyncDirection GetDirectionBySyncLog(Config config, SyncTable tab, SqlConnection sqlConn, OracleConnection oraConn)
        {
            SyncDirection direct = SyncDirection.None;

            StringBuilder selectSql = new StringBuilder();
            DataSet ds = new DataSet();
            IList<SyncLog> logs;
            SyncLog masterLog = null;
            SyncLog slaveLog = null;

            Helper.GetDbDataAdapter(string.Format("select * from {0} Where {1}='{2}'", config.SyncInfo.TableName, SyncLog.Mappings["TableName"], tab.MasterTable)
                                        , sqlConn)
                                   .Fill(ds);
            logs = ds.Tables[0].ToList<SyncLog>(SyncLog.Mappings);
            if (logs.Count > 0)
                masterLog = logs[0];

            ds.Clear();
            Helper.GetDbDataAdapter(string.Format("select * from {0} Where {1}='{2}'", config.SyncInfo.TableName, SyncLog.Mappings["TableName"], tab.SlaveTable)
                                        , oraConn)
                                   .Fill(ds);
            logs = ds.Tables[0].ToList<SyncLog>(SyncLog.Mappings);
            if (logs.Count > 0)
                slaveLog = logs[0];


            if (masterLog.ModifyTime > slaveLog.ModifyTime)
                direct = SyncDirection.Push;
            else if (masterLog.ModifyTime < slaveLog.ModifyTime)
                direct = SyncDirection.Pull;
            else
                direct = SyncDirection.None;

            return direct;
        }



        /// <summary>
        /// 将DataTable中的数据写入到目标数据库
        /// 不要嫌这个过程写得太长太复杂，功能是一步步增加的，有时间的重构吧
        /// </summary>
        /// <param name="destConn">目标数据库</param>
        /// <param name="dt">包含数据的源表</param>
        /// <param name="tab">同步表信息</param>
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
                            delegate ()
                            {
                                int index = dt.Rows.IndexOf(row) + 1;
                                this.stslRows.Text = string.Format(@"{0}/{1}", index, dt.Rows.Count);
                                this.stpProgress.Value = index;
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
                            delegate ()
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
            foreach (var th in SyncThreads)
                if (th != null)
                    th.Abort();
            Environment.Exit(Environment.ExitCode);
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            //string[] text = { "暂停", "继续" };
            //((Control)sender).Tag = ((((Control)sender).Tag as int) + 1) / 2;
            //((Control)sender).Text =
            string tmp = ((Control)sender).Text;
            ((Control)sender).Text = ((Control)sender).Tag as string;
            ((Control)sender).Tag = tmp;

            if (tmp == "暂停")
            {
                foreach (var th in SyncThreads)
                    if (th != null)
                        th.Suspend();
                this.btnStop.Enabled = false;
            }
            if (tmp == "继续")
            {
                foreach (var th in SyncThreads)
                    if (th != null)
                        th.Resume();
                this.btnStop.Enabled = true;
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            ((Control)sender).Enabled = false;
            this.btnPause.Visible = false;
            this.btnStart.Enabled = true;
            this.btnStart.Visible = true;

            foreach (var th in SyncThreads)
                if (th != null)
                    th.Abort();
        }




        public void PushData(SyncTable tab, SqlConnection sqlConn, OracleConnection oraConn)
        {
            DataSet ds = new DataSet();

            //读取源数据
            DbDataAdapter sourceAdp = new SqlDataAdapter(tab.GetQueryString(tab.MasterTable), sqlConn);
            sourceAdp.Fill(ds, tab.MasterTable);
            DataTable dt = ds.Tables[tab.MasterTable];

            //更新状态栏
            this.Invoke(new MethodInvoker(delegate ()
            {
                stpProgress.Maximum = dt.Rows.Count;
                stslRows.Text = string.Format(@"{0}/{1}", 0, dt.Rows.Count);
            }));

            //写入目标库
            dt.TableName = tab.SlaveTable;
            var resut = InsertData(oraConn, DatabaseType.Oracle, dt, tab);

            //更新源数据状态
            if (tab.UpdateSyncState)
                foreach (DataRow row in resut.Rows)
                    Helper.UpdateSyncState(SyncDirection.Push, sqlConn, tab, row);
            log.Info(string.Format("方向:{0},需同步纪录数:{1},处理纪录数:{2}.", SyncDirection.Push, dt.Rows.Count, resut.Rows.Count));

        }


        public void PullData(SyncTable tab, SqlConnection sqlConn, OracleConnection oraConn)
        {

            DataSet ds = new DataSet();
            //读取源数据
            DbDataAdapter sourceAdp = new OracleDataAdapter(tab.GetQueryString(tab.SlaveTable), oraConn);
            sourceAdp.Fill(ds, tab.SlaveTable);
            DataTable dt = ds.Tables[tab.SlaveTable];

            //更新状态栏
            this.Invoke(new MethodInvoker(delegate ()
                            {
                                stpProgress.Maximum = dt.Rows.Count;
                                stslRows.Text = string.Format(@"{0}/{1}", 0, dt.Rows.Count);
                            }));

            //写入目标库
            dt.TableName = tab.MasterTable;
            var resut = InsertData(sqlConn, DatabaseType.MsSql, dt, tab);

            //更新源数据状态
            if (tab.UpdateSyncState)
                foreach (DataRow row in resut.Rows)
                    Helper.UpdateSyncState(SyncDirection.Pull, oraConn, tab, row);
            log.Info(string.Format("方向:{0},需同步纪录数:{1},处理纪录数:{2}.", SyncDirection.Pull, dt.Rows.Count, resut.Rows.Count));
        }
    }
}
