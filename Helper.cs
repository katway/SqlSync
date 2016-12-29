using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Data.OracleClient;
using System.Data.Common;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using SqlSync.Sync;

namespace SqlSync
{
    internal class Helper
    {

        /// <summary>
        /// 根据数据库类型取得一个到数据库的连接对象
        /// </summary>
        /// <param name="dbType"><seealso cref="DatabaseType"></param>
        /// <returns></returns>
        internal static DbConnection GetDbConnection(DatabaseType dbType)
        {
            DbConnection connection = null;
            switch (dbType)
            {
                case DatabaseType.MsSql:
                    connection = new SqlConnection();
                    break;
                case DatabaseType.Oracle:
                    connection = new OracleConnection();
                    break;
            }
            return connection;
        }


        /// <summary>
        /// 取得到指定数据库连接的DataAdapter对象
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        internal static DbDataAdapter GetDbDataAdapter(string selectString, DbConnection conn)
        {
            DbDataAdapter adapter = null;
            if (conn.GetType() == typeof(SqlConnection))
                adapter = new SqlDataAdapter(selectString, (SqlConnection)conn);

            if (conn.GetType() == typeof(OracleConnection))
                adapter = new OracleDataAdapter(selectString, (OracleConnection)conn);

            return adapter;
        }

        /// <summary>
        /// 取得到指定数据库连接的DbCommand对象
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        internal static DbCommand GetDbCommand(DbConnection conn)
        {
            DbCommand sqlCommand = null;
            if (conn.GetType() == typeof(SqlConnection))
                sqlCommand = new SqlCommand();

            if (conn.GetType() == typeof(OracleConnection))
                sqlCommand = new OracleCommand();

            sqlCommand.Connection = conn;
            return sqlCommand;
        }

        internal static DatabaseType GetDbType(DbConnection conn)
        {
            DatabaseType t = DatabaseType.Unkown;
            if (conn.GetType() == typeof(SqlConnection))
                t = DatabaseType.MsSql;

            if (conn.GetType() == typeof(OracleConnection))
                t = DatabaseType.Oracle;
            return t;
        }

        /// <summary>
        /// 将配置写入文件
        /// </summary>
        /// <param name="config"></param>
        internal static void SaveConfig(SyncConfig config)
        {
            string file = AppDomain.CurrentDomain.BaseDirectory + "sync.config";
            XmlSerializer x = new XmlSerializer(typeof(SyncConfig));
            FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write);
            x.Serialize(fs, config);
            fs.Close();
        }

        /// <summary>
        /// 从缺省的配置文件中读取配置
        /// </summary>
        /// <returns></returns>
        internal static SyncConfig ReadConfig()
        {
            return ReadConfig(AppDomain.CurrentDomain.BaseDirectory + "sync.config");
        }

        /// <summary>
        /// 从指定文件中读取配置
        /// </summary>
        /// <param name="configFile">指定的xml文件,应指定包含完整路径的文件名</param>
        /// <returns></returns>
        internal static SyncConfig ReadConfig(string configFile)
        {
            XmlSerializer x = new XmlSerializer(typeof(SyncConfig));
            Stream fs = new FileStream(configFile, FileMode.OpenOrCreate, FileAccess.Read);
            SyncConfig c = (SyncConfig)x.Deserialize(fs);
            fs.Close();
            return c;
        }

        /// <summary>
        /// 在数据库中建立同步所需的状态字段，并将初始值设置为0。
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="table"></param>
        internal static void AppendSyncFields(SqlConnection conn, SyncTable table)
        {
            string fieldSql = string.Format(@"if not exists(select * from syscolumns where id=object_id('{0}') and name='{1}') 
                                                        begin alter table {0} add {1} int default 0 not null;end 
                                              if not exists(select * from syscolumns where id=object_id('{0}') and name='{2}') 
                                                        begin alter table {0} add {2} int default 0 not null;end",
                                                     table.SqlTable,
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
        internal static void AppendSyncFields(OracleConnection conn, SyncTable table)
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
                                                     table.OracleTable.ToUpper(),
                                                     table.SyncStateField.ToUpper(),
                                                     table.SyncErrorsField.ToUpper()
                                                     );
            OracleCommand oracmd = new OracleCommand(fieldSql, conn);
            oracmd.ExecuteNonQuery();
        }

        internal static void UpdateSyncInfo(SyncLog log, SqlConnection conn)
        {
            string sql = "delete from SyncInfo where TableName = @TableName ;"
                        + " insert into SyncInfo (TableName,ModifyTime,SyncTime) values(@TableName,@ModifyTime,@SyncTime);";
            DbCommand cmd = Helper.GetDbCommand(conn);


            SqlParameter[] parameters = { new SqlParameter("@TableName", log.TableName)
                                        ,new SqlParameter("@ModifyTime", log.ModifyTime )
                                        ,new SqlParameter("@SyncTime", DateTime.Now)
                                        };
            foreach (SqlParameter p in parameters)
                if (p.Value == null)
                    p.Value = DBNull.Value;

            cmd.CommandText = sql;
            cmd.Parameters.AddRange(parameters);
            cmd.ExecuteNonQuery();
        }


        internal static void UpdateSyncInfo(SyncLog log, OracleConnection conn)
        {
            string sql = "begin "
                        + " delete from SyncInfo where TableName = :TableName ;"
                        + " insert into SyncInfo (TableName,ModifyTime,SyncTime) values(:TableName,:ModifyTime,:SyncTime);"
                        + " end;";
            DbCommand cmd = Helper.GetDbCommand(conn);

            OracleParameter[] parameters = {new OracleParameter(":TableName", log.TableName),
                                            new OracleParameter(":ModifyTime",log.ModifyTime),
                                            new OracleParameter(":SyncTime", DateTime.Now)
                                            };
            foreach (OracleParameter p in parameters)
                if (p.Value == null)
                    p.Value = DBNull.Value;

            cmd.CommandText = sql;
            cmd.Parameters.AddRange(parameters);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 向数据源表中写入更新成功或失败的状态
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="table"></param>
        /// <param name="KeyFieldValue"></param>
        /// <param name="state"></param>
        internal static void UpdateSyncState(SyncDirection direction, DbConnection conn, SyncTable table, DataRow row)
        {
            //如果执行成功
            string stateSql;
            stateSql = string.Format(@"update {0} set {1} ={2} , {3}= {3}+3-{2}",// where {4}='{5}'",
                                                (direction == SyncDirection.Push) ? table.SqlTable : table.OracleTable,
                                                table.SyncStateField, row[table.SyncStateField],
                                                table.SyncErrorsField);
            stateSql += " Where 1=1";
            foreach (string k in table.Key)
                stateSql += string.Format(" AND {0} = {1} ",
                                                k,
                                                string.Format(DataSqlFormat(GetDbType(conn))[row[k].GetType()], row[k].ToString()));

            DbCommand stateCommand = Helper.GetDbCommand(conn);
            stateCommand.CommandText = stateSql;
            stateCommand.ExecuteNonQuery();
        }




        internal static void CreateSyncInfoTable()
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 预备C#数据类型到SQL语句数据表示方式的转换规则
        /// </summary>
        /// <param name="dataFormat">已初始化的字典集</param>
        internal static Dictionary<Type, string> DataSqlFormat(DatabaseType dbType)
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


    }
}
