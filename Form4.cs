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

            //SaveConfig(c);
            c = ReadConfig();

            //更新状态栏
            this.stsTables.Text = string.Format(@"0/{0}", c.SyncTables.Count);

            SyncThread = new Thread(
                             delegate ()
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
                        delegate ()
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
                        AppendSyncFields(sqlConn, tab);
                        AppendSyncFields(oraConn, tab);
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.Message);
                        this.Invoke(new MethodInvoker(
                            delegate ()
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
                            delegate ()
                            {
                                stpProgress.Maximum = dt.Rows.Count;
                                stslRows.Text = string.Format(@"{0}/{1}", 0, dt.Rows.Count);
                            }));

                    //写入目标库
                    dt.TableName = tab.SlaveTable;
                    var resut = InsertData(oraConn, DatabaseType.Oracle, dt, tab);

                    //更新源数据状态
                    foreach (var key in resut.Keys)
                        UpdateSyncState(sqlConn, tab, key, resut[key]);
                    log.Info(string.Format("方向:{0},需同步纪录数:{1},处理纪录数:{2}.", SyncDirection.Push, dt.Rows.Count, resut.Count));
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
                            delegate ()
                            {
                                stpProgress.Maximum = dt.Rows.Count;
                                stslRows.Text = string.Format(@"{0}/{1}", 0, dt.Rows.Count);
                            }));

                    //写入目标库
                    dt.TableName = tab.MasterTable;
                    var resut = InsertData(sqlConn, DatabaseType.MsSql, dt, tab);

                    //更新源数据状态
                    foreach (var key in resut.Keys)
                        UpdateSyncState(oraConn, tab, key, resut[key]);
                    log.Info(string.Format("方向:{0},需同步纪录数:{1},处理纪录数:{2}.", SyncDirection.Pull, dt.Rows.Count, resut.Count));
                }
                sqlConn.Close();
                oraConn.Close();
            }
        }


        /// <summary>
        /// 将指定的数据表中的数据写入目标数据库
        /// </summary>
        /// <param name="destConn"></param>
        /// <param name="dt"></param>
        /// <param name="tab"></param>
        /// <returns></returns>
        private Dictionary<string, SyncState> InsertData(DbConnection destConn, DatabaseType dbType, DataTable dt, SyncTable tab)
        {
            Dictionary<string, SyncState> result = new Dictionary<string, SyncState>();

            //预备数据转换到SQL的表示规则
            Dictionary<Type, string> dataFormat = DataSqlFormat(dbType);
            DbCommand dbCommand = GetDbCommand(destConn);

            if (dbCommand != null)
                foreach (DataRow row in dt.Rows)
                {
                    //更新状态栏
                    this.Invoke(new MethodInvoker(
                            delegate ()
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
                    whereSql.Append(@" WHERE ");

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
                            if (col.ColumnName.ToLower() == tab.Key.ToLower())
                                whereSql.AppendFormat("{0} = {1}", col.ColumnName,
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

                    try
                    {
                        dbCommand.CommandText = updateSql.ToString();
                        int r = dbCommand.ExecuteNonQuery();

                        if (r <= 0)
                        {
                            dbCommand.CommandText = insertSql.ToString();
                            r = r | dbCommand.ExecuteNonQuery();
                        }

                        //如果执行成功
                        if (r > 0)
                            result.Add(row[tab.Key].ToString(), SyncState.Sync);
                        else
                            result.Add(row[tab.Key].ToString(), SyncState.Error);
                    }
                    catch (Exception ex)
                    {
                        string err = dbCommand.CommandText + "\r\n" + ex.Message + "\r\n\r\n";
                        log.Error(err);
                        this.Invoke(new MethodInvoker(
                            delegate ()
                            { this.txtLog.Text += (err); }
                            ));

                        result.Add(row[tab.Key].ToString(), SyncState.Error);
                    }
                }

            return result;
        }


        /// <summary>
        /// 预备C#数据类型到SQL语句数据表示方式的转换规则
        /// </summary>
        /// <param name="dataFormat">已初始化的字典集</param>
        private Dictionary<Type, string> DataSqlFormat(DatabaseType dbType)
        {
            Dictionary<Type, string> dataFormat = new Dictionary<Type, string>();
            dataFormat.Add(typeof(bool), @"{0}");
            dataFormat.Add(typeof(sbyte), @"{0}");
            dataFormat.Add(typeof(byte), @"{0}");
            dataFormat.Add(typeof(short), @"{0}");
            dataFormat.Add(typeof(ushort), @"{0}");
            dataFormat.Add(typeof(int), @"{0}");
            dataFormat.Add(typeof(uint), @"{0}");
            dataFormat.Add(typeof(long), @"{0}");
            dataFormat.Add(typeof(ulong), @"{0}");
            dataFormat.Add(typeof(char), @"'{0}'");
            if (dbType == DatabaseType.Oracle)
                dataFormat.Add(typeof(DateTime), @"to_date('{0}','yyyy-mm-dd hh24:mi:ss')");
            if (dbType == DatabaseType.MsSql)
                dataFormat.Add(typeof(DateTime), @"'{0}'");

            dataFormat.Add(typeof(string), @"'{0}'");

            dataFormat.Add(typeof(float), @"{0}");
            dataFormat.Add(typeof(double), @"{0}");
            dataFormat.Add(typeof(decimal), @"{0}");

            return dataFormat;
        }
        /// <summary>
        /// 取得到指定数据库连接的DbCommand对象
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        private DbCommand GetDbCommand(DbConnection conn)
        {
            DbCommand sqlCommand = null;
            if (conn.GetType() == typeof(SqlConnection))
                sqlCommand = new SqlCommand();

            if (conn.GetType() == typeof(OracleConnection))
                sqlCommand = new OracleCommand();

            sqlCommand.Connection = conn;
            return sqlCommand;

        }

        /// <summary>
        /// 在数据库中建立同步所需的状态字段，并将初始值设置为0。
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="table"></param>
        private void AppendSyncFields(SqlConnection conn, SyncTable table)
        {
            string fieldSql = string.Format(@"if not exists(select * from syscolumns where id=object_id('{0}') and name='{1}') 
                                                        begin alter table {0} add {1} int default 0 not null;end 
                                              if not exists(select * from syscolumns where id=object_id('{0}') and name='{2}') 
                                                        begin alter table {0} add {2} int default 0 not null;end",
                                                     table.MasterTable,
                                                     table.SyncStateField,
                                                     table.SyncErrorsField
                                                     );
            SqlCommand sqlcmd = new SqlCommand(fieldSql, conn);
            sqlcmd.ExecuteNonQuery();
        }
        /// <summary>
        /// 在数据库中建立同步所需的状态字段，并将初始值设置为0。
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="table"></param>
        /// <exception cref=""
        private void AppendSyncFields(OracleConnection conn, SyncTable table)
        {
            string fieldSql = string.Format(@"declare v_count integer;
                                                begin
                                                    select count(1) into v_count from  user_tab_columns where  Table_name='{0}' and Column_name='{1}' ;
                                                    if v_count =0 then
                                                       execute immediate 'alter table {0} add {1} int default 0 not null';                                                       
                                                       commit;                                                       
                                                    end if;
                                                    select count(1) into v_count from  user_tab_columns where  Table_name='{0}' and Column_name='{2}' ;
                                                    if v_count =0 then
                                                        execute immediate 'alter table {0} add {2} int default 0 not null';
                                                        commit;                                                       
                                                    end if;
                                                end;",
                                                     table.SlaveTable.ToUpper(),
                                                     table.SyncStateField.ToUpper(),
                                                     table.SyncErrorsField.ToUpper()
                                                     );
            OracleCommand oracmd = new OracleCommand(fieldSql, conn);
            oracmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 向数据源表中写入更新成功或失败的状态
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="table"></param>
        /// <param name="KeyFieldValue"></param>
        /// <param name="state"></param>
        private void UpdateSyncState(SqlConnection conn, SyncTable table, string KeyFieldValue, SyncState state)
        {
            //如果执行成功
            string stateSql;
            stateSql = string.Format(@"update {0} set {1} ={2} , {3}= {3}+3-{2} where {4}='{5}'",
                                                table.MasterTable,
                                                table.SyncStateField, (int)state,
                                                table.SyncErrorsField, table.Key, KeyFieldValue);

            SqlCommand stateCommand = new SqlCommand();
            stateCommand.CommandText = stateSql;
            stateCommand.Connection = conn;
            stateCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// 向数据源表中写入更新成功或失败的状态
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="table"></param>
        /// <param name="KeyFieldValue"></param>
        /// <param name="state"></param>
        private void UpdateSyncState(OracleConnection conn, SyncTable table, string KeyFieldValue, SyncState state)
        {
            //如果执行成功
            string stateSql;
            stateSql = string.Format(@"update {0} set {1} ={2} , {3}= {3}+3-{2} where {4}='{5}'",
                                                table.SlaveTable,
                                                table.SyncStateField, (int)state,
                                                table.SyncErrorsField, table.Key, KeyFieldValue);

            OracleCommand stateCommand = new OracleCommand();
            stateCommand.CommandText = stateSql;
            stateCommand.Connection = conn;
            try
            { stateCommand.ExecuteNonQuery(); }
            catch
            { }
        }

        /// <summary>
        /// 将配置写入文件
        /// </summary>
        /// <param name="config"></param>
        private void SaveConfig(Config config)
        {
            string file = AppDomain.CurrentDomain.BaseDirectory + "config.xml";
            XmlSerializer x = new XmlSerializer(typeof(Config));
            FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write);
            x.Serialize(fs, config);
            fs.Close();

        }
        /// <summary>
        /// 从缺省的配置文件中读取配置
        /// </summary>
        /// <returns></returns>
        private Config ReadConfig()
        {
            return ReadConfig(AppDomain.CurrentDomain.BaseDirectory + "config.xml");
        }
        /// <summary>
        /// 从指定文件中读取配置
        /// </summary>
        /// <param name="configFile">指定的xml文件,应指定包含完整路径的文件名</param>
        /// <returns></returns>
        private Config ReadConfig(string configFile)
        {

            XmlSerializer x = new XmlSerializer(typeof(Config));
            Stream fs = new FileStream(configFile, FileMode.OpenOrCreate, FileAccess.Read);
            Config c = (Config)x.Deserialize(fs);
            fs.Close();
            return c;
        }

        private void Form4_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (SyncThread != null)
                SyncThread.Abort();
            Environment.Exit(Environment.ExitCode);
        }

    }
}
