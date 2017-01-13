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
using System.IO;
using System.Drawing;
using SqlSync.Sync;
using System.Configuration;

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

        private void Form4_Load(object sender, EventArgs e)
        {
            if (bool.Parse(ConfigurationManager.AppSettings["AutoStart"]))
            {
                btnStart_Click(this.btnStart, null);
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            SyncConfig c = new SyncConfig();
            //c.SqlConnectionString = @"server=192.168.1.135;uid=sa;pwd=sa;database=zhify";
            //c.OracleConnectionString = @"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.1.135)(PORT=1521)))(CONNECT_DATA=(SID = orcl)));User Id=zhify;Password=zhify;";
            ////c.SyncTables.Add(new SyncTable("Employee", "outid"));
            //c.SyncTables.Add(new SyncTable("Company", "norder", SyncDirection.Sync));
            //c.SyncTables[0].Key.Clear();
            //c.SyncTables[0].Key.Add("sid");
            //c.SyncTables[0].FieldMappings.Add("sid", "norder");

            //c.SyncTables.Add(new SyncTable("Employee", SyncDirection.Pull));
            //c.SyncTables[1].Key.Clear();
            //c.SyncTables[1].Key.Add("sid");
            //c.SyncTables[1].IgnoreFields.Add("SALLOWIPS");
            //c.SyncTables[1].IgnoreFields.Add("NKH");
            //c.SyncTables[1].IgnoreFields.Add("CO");
            //c.SyncTables[1].IgnoreFields.Add("COO");
            //c.SyncTables.Add(new SyncTable("oplog", SyncDirection.Push));
            //c.SyncTables[2].FieldMappings.Add("id", "id2");
            //c.SyncInfo.Enable = false;

            //Helper.SaveConfig(c);

            this.tsslConfigFile.Text = ConfigurationManager.AppSettings["SyncConfigFile"];
            c = Helper.ReadConfig(ConfigurationManager.AppSettings["SyncConfigFile"]);

            if (c.SyncInfo.Enable && c.SyncInfo.AutoCreate)
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

        #region 功能代码
        /// <summary>
        /// 执行数据传送的主体方法
        /// </summary>
        /// <param name="config">传送数据的配置信息</param>
        /// <param name="tables">在config中出现的，要执行传送的数据表，其余不会执行。</param>
        public void TransferData(SyncConfig config, List<SyncTable> tables)
        {
            //建立到源数据的连接
            SqlConnection sqlConn = new SqlConnection(config.SqlConnectionString);
            OracleConnection oraConn = new OracleConnection(config.OracleConnectionString);

            sqlConn.StateChange += SqlConn_StateChange;
            oraConn.StateChange += OraConn_StateChange;

            foreach (var tab in tables)
            {

                log.Info(string.Format("开始同步{0}到{1},方向:{2}.", tab.SqlTable, tab.OracleTable, tab.Direction));

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
                                                                    tab.SqlTable.ToUpper(),
                                                                    tab.OracleTable.ToUpper(),
                                                     tab.SyncStateField.ToUpper(),
                                                     tab.SyncErrorsField.ToUpper(),
                                                     ex.Message
                                                     );
                            }));
                    }
                }
                #endregion

                #region 检查SyncInfo并计算同步方向
                SyncDirection direct = tab.Direction;
                if (config.SyncInfo.Enable)
                {
                    direct = GetDirectionBySyncLog(config, tab, sqlConn, oraConn);
                    if (direct == SyncDirection.Unkown)
                        direct = tab.Direction;
                    else
                        direct = direct & tab.Direction;
                }
                #endregion

                //更新状态栏
                this.Invoke(new MethodInvoker(
                        delegate ()
                        {
                            this.stsTables.Text = string.Format(@"{0}/{1}", tables.IndexOf(tab) + 1, tables.Count);
                            this.stslTable.Text = tab.ToString(direct);
                        }));

                ///下面进行单向同步
                if ((direct == SyncDirection.Push) || (direct == SyncDirection.Sync))
                {
                    //对方的同步时间应该是从本地取数据的时刻
                    tab.SyncLogsSlave.SyncTime = DateTime.Now;
                    PushData(tab, sqlConn, oraConn);
                    if (config.SyncInfo.Enable)
                    {
                        tab.SyncLogsSlave.ModifyTime = tab.SyncLogsMaster.ModifyTime;
                        Helper.UpdateSyncInfo(tab.SyncLogsSlave, oraConn);
                    }

                }

                ///下面进行异向同步
                if ((direct == SyncDirection.Pull) || direct == SyncDirection.Sync)
                {
                    //本地的同步时间应该是从对方取数据的时刻
                    tab.SyncLogsMaster.SyncTime = DateTime.Now;
                    PullData(tab, sqlConn, oraConn);
                    if (config.SyncInfo.Enable)
                    {
                        tab.SyncLogsMaster.ModifyTime = tab.SyncLogsSlave.ModifyTime;
                        Helper.UpdateSyncInfo(tab.SyncLogsMaster, sqlConn);
                    }
                }
                sqlConn.Close();
                oraConn.Close();
            }
        }

        /// <summary>
        /// 根据SyncInfo表中的同步纪录提前加载表的数据传送方向
        /// </summary>
        /// <param name="config"></param>
        /// <param name="tab"></param>
        /// <param name="sqlConn"></param>
        /// <param name="oraConn"></param>
        /// <returns></returns>
        private SyncDirection GetDirectionBySyncLog(SyncConfig config, SyncTable tab, SqlConnection sqlConn, OracleConnection oraConn)
        {
            tab.SyncLogsMaster.TableName = tab.SqlTable;
            tab.SyncLogsSlave.TableName = tab.OracleTable;
            SyncDirection direct = SyncDirection.Unkown;

            StringBuilder selectSql = new StringBuilder();
            DataSet ds = new DataSet();
            IList<SyncInfoDetail> logs;

            try
            {
                Helper.GetDbDataAdapter(string.Format("select * from {0} Where {1}='{2}'", config.SyncInfo.TableName, SyncInfoDetail.Mappings["TableName"], tab.SqlTable)
                                        , sqlConn)
                                   .Fill(ds);
                logs = ds.Tables[0].ToList<SyncInfoDetail>(SyncInfoDetail.Mappings);
                if (logs.Count > 0)
                    tab.SyncLogsMaster = logs[0];
                else
                    return direct;

                ds.Clear();
                Helper.GetDbDataAdapter(string.Format("select * from {0} Where {1}='{2}'", config.SyncInfo.TableName, SyncInfoDetail.Mappings["TableName"], tab.OracleTable)
                                            , oraConn)
                                       .Fill(ds);
                logs = ds.Tables[0].ToList<SyncInfoDetail>(SyncInfoDetail.Mappings);
                if (logs.Count > 0)
                    tab.SyncLogsSlave = logs[0];
                else
                    return direct;

                direct = SyncDirection.None;
                if (tab.SyncLogsMaster.ModifyTime.GetValueOrDefault() > tab.SyncLogsSlave.SyncTime.GetValueOrDefault())
                    direct = direct | SyncDirection.Push;
                if (tab.SyncLogsMaster.SyncTime.GetValueOrDefault() < tab.SyncLogsSlave.ModifyTime.GetValueOrDefault())
                    direct = direct | SyncDirection.Pull;
                //}
            }
            catch (DbException ex)
            {
                log.Error(string.Format("读取{0}表遇到错误，可能是表不存在，详细信息见下条。", config.SyncInfo.TableName));
                log.Error(ex);
            }
            return direct;
        }

        /// <summary>
        /// 由Sql读取数据表数据，写入到Oracle
        /// </summary>
        /// <param name="tab">读取和写入的表</param>
        /// <param name="sqlConn">Sql连接</param>
        /// <param name="oraConn">Oracle连接</param>
        public void PushData(SyncTable tab, SqlConnection sqlConn, OracleConnection oraConn)
        {
            DataSet ds = new DataSet();

            //读取源数据
            DbDataAdapter sourceAdp = new SqlDataAdapter(tab.GetQueryString(tab.SqlTable), sqlConn);
            sourceAdp.Fill(ds, tab.SqlTable);
            DataTable dt = ds.Tables[tab.SqlTable];

            //更新状态栏
            this.Invoke(new MethodInvoker(delegate ()
            {
                stpProgress.Maximum = dt.Rows.Count;
                stslRows.Text = string.Format(@"{0}/{1}", 0, dt.Rows.Count);
            }));

            //写入目标库
            dt.TableName = tab.OracleTable;
            var resut = InsertData(oraConn, DatabaseType.Oracle, dt, tab);

            //更新源数据状态
            if (tab.UpdateSyncState)
                foreach (DataRow row in resut.Rows)
                    Helper.UpdateSyncState(SyncDirection.Push, sqlConn, tab, row);
            log.Info(string.Format("方向:{0},需同步纪录数:{1},处理纪录数:{2}.", SyncDirection.Push, dt.Rows.Count, resut.Rows.Count));

        }

        /// <summary>
        /// 由Oracle读取数据表数据，写入到Sql
        /// </summary>
        /// <param name="tab">读取和写入的表</param>
        /// <param name="sqlConn">Sql连接</param>
        /// <param name="oraConn">Oracle连接</param>
        public void PullData(SyncTable tab, SqlConnection sqlConn, OracleConnection oraConn)
        {

            DataSet ds = new DataSet();
            //读取源数据
            DbDataAdapter sourceAdp = new OracleDataAdapter(tab.GetQueryString(tab.OracleTable), oraConn);
            sourceAdp.Fill(ds, tab.OracleTable);
            DataTable dt = ds.Tables[tab.OracleTable];

            //更新状态栏
            this.Invoke(new MethodInvoker(delegate ()
            {
                stpProgress.Maximum = dt.Rows.Count;
                stslRows.Text = string.Format(@"{0}/{1}", 0, dt.Rows.Count);
            }));

            //写入目标库
            dt.TableName = tab.SqlTable;
            var resut = InsertData(sqlConn, DatabaseType.MsSql, dt, tab);

            //更新源数据状态
            if (tab.UpdateSyncState)
                foreach (DataRow row in resut.Rows)
                    Helper.UpdateSyncState(SyncDirection.Pull, oraConn, tab, row);
            log.Info(string.Format("方向:{0},需同步纪录数:{1},处理纪录数:{2}.", SyncDirection.Pull, dt.Rows.Count, resut.Rows.Count));
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
                                this.stpProgress.Maximum = dt.Rows.Count;
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

                        string colMapping = tab.FieldMappings.ContainsKey(col.ColumnName) ? tab.FieldMappings[col.ColumnName] : col.ColumnName;

                        updateSql.AppendFormat(@"{0}=", colMapping);

                        insertSql.AppendFormat(@"{0},", colMapping);
                        if (row[col.ColumnName] != System.DBNull.Value)
                        {
                            if (tab.Key.Contains(col.ColumnName.ToLower()))
                                whereSql.AppendFormat(" AND {0} = {1}", colMapping,
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
                        int upRows = 0;
                        if (tab.Action.Contains(Sync.SyncAction.Update))
                        {
                            string upSql = updateSql.ToString();
                            if (upSql.Contains("AND"))
                            {
                                dbCommand.CommandText = upSql;
                                upRows = dbCommand.ExecuteNonQuery();
                            }
                            else
                            {
                                string err = string.Format("同步表{0}时，数据集中缺少主键字段，Update操作被取消。\r\n", tab);
                                log.Error(err);
                                this.Invoke(new MethodInvoker(
                                    delegate ()
                                    { this.txtLog.Text += (err); }));
                            }

                        }

                        //如果更新条目为0，才执行插入操作
                        if (upRows <= 0 && tab.Action.Contains(Sync.SyncAction.Insert))
                        {
                            dbCommand.CommandText = insertSql.ToString();
                            upRows = upRows | dbCommand.ExecuteNonQuery();
                        }

                        //如果执行成功
                        rowState = (upRows > 0) ? SyncState.Sync : SyncState.Error;
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
        #endregion

        #region 交互事件
        private void btnPause_Click(object sender, EventArgs e)
        {
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


        private void txtLog_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                StreamReader m_streamReader = new StreamReader(fs, Encoding.UTF8);
                //使用StreamReader类来读取文件
                m_streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
                // 从数据流中读取每一行，直到文件的最后一行，并在richTextBox1中显示出内容
                this.txtLog.Text = "";
                string strLine = m_streamReader.ReadLine();
                while (strLine != null)
                {
                    this.txtLog.Text += strLine + "\r\n";
                    strLine = m_streamReader.ReadLine();
                }
                //关闭此StreamReader对象
                m_streamReader.Close();
                fs.Close();
            }
        }

        private void Form4_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (var th in SyncThreads)
                if (th != null)
                    th.Abort();
            Environment.Exit(Environment.ExitCode);
        }
        #endregion

        #region 自触发事件
        private void OraConn_StateChange(object sender, StateChangeEventArgs e)
        {
            Color c = Color.Black;
            switch (e.CurrentState)
            {
                case ConnectionState.Open:
                    c = Color.Green;
                    break;
                case ConnectionState.Fetching:
                    c = Color.LawnGreen;
                    break;
                case ConnectionState.Executing:
                    c = Color.Green;
                    break;
                case ConnectionState.Connecting:
                    c = Color.GreenYellow;
                    break;
                case ConnectionState.Closed:
                    c = SystemColors.Control;
                    break;
                case ConnectionState.Broken:
                    c = Color.Red;
                    break;
            }
            this.Invoke(new MethodInvoker(delegate () { this.tsslOracleState.BackColor = c; }));
        }

        private void SqlConn_StateChange(object sender, StateChangeEventArgs e)
        {

            Color c = Color.Black;
            switch (e.CurrentState)
            {
                case ConnectionState.Open:
                    c = Color.Green;
                    break;
                case ConnectionState.Fetching:
                    c = Color.LawnGreen;
                    break;
                case ConnectionState.Executing:
                    c = Color.Green;
                    break;
                case ConnectionState.Connecting:
                    c = Color.GreenYellow;
                    break;
                case ConnectionState.Closed:
                    c = SystemColors.Control;
                    break;
                case ConnectionState.Broken:
                    c = Color.Red;
                    break;
            }
            this.Invoke(new MethodInvoker(delegate () { this.tsslSqlState.BackColor = c; }));
        }
        #endregion

    }
}
