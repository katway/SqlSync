using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using SqlSync.Sync;

namespace SqlSync.Sync
{
    [Serializable]
    public class SyncTable
    {
        /// <summary>
        /// 数据表名
        /// </summary>
        public string SqlTable { get; set; }

        /// <summary>
        /// 主键
        /// </summary>
        [XmlIgnore]
        public List<string> Key { get; private set; } = new List<string>() { "id" };
        [XmlElement("Key")]
        public string Keys
        {
            get { return string.Join(",", Key.ToArray()); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    Key.Clear();
                    string[] es = value.Split(',');
                    foreach (string e in es)
                        Key.Add(e.ToLower());
                }
            }
        }

        /// <summary>
        ///同步状态字段名 
        /// </summary>
        /// 
        public string SyncStateField
        {
            get { return syncStateField; }
            set
            {
                syncStateField = value;
                this.IgnoreFields.Add(syncStateField.ToLower());
            }
        }
        private string syncStateField;

        /// <summary>
        /// 同步错误计数字段名
        /// </summary>
        public string SyncErrorsField
        {
            get { return syncErrorField; }
            set
            {
                syncErrorField = value;
                this.IgnoreFields.Add(syncErrorField.ToLower());
            }
        }
        private string syncErrorField;

        /// <summary>
        /// 数据表的查询语句
        /// </summary>
        private string QueryString { get; set; }

        /// <summary>
        /// 查询语句的格式化形式
        /// </summary>
        public string QueryStringFormat { get; set; }

        /// <summary>
        /// 同步时被忽略的字段
        /// </summary>
        [XmlIgnore]
        public List<string> IgnoreFields { get; set; }

        [XmlElement("IgnoreFields")]
        public string ignoreFields
        {
            get { return string.Join(",", IgnoreFields.ToArray()); }
            set
            {
                string[] es = value.Split(',');
                foreach (string e in es)
                    IgnoreFields.Add(e.ToLower());
            }
        }

        /// <summary>
        /// 目标表
        /// </summary>
        public string OracleTable { get; set; }

        /// <summary>
        /// 同步方向
        /// </summary>
        public SyncDirection Direction { get; set; }
        /// <summary>
        /// 同步优先级
        /// </summary>
        public SyncPriority Priority { get; set; } = SyncPriority.Normal;


        /// <summary>
        /// 对目标表进行的操作
        /// 如果配置为Insert和Update同时存在，则Update结果大于0时，Insert不再执行
        /// </summary>
        [XmlIgnore]
        public List<SyncAction> Action { get; set; } = new List<SyncAction>() { SyncAction.Insert, SyncAction.Update };

        [XmlElement("Action")]
        public string action
        {
            get { return string.Join(",", Action.ToArray()); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    Action.Clear();
                    string[] es = value.Split(',');
                    foreach (string e in es)
                        Action.Add((SyncAction)Enum.Parse(typeof(SyncAction), e));
                }
            }
        }

        /// <summary>
        /// 字段映射
        /// </summary>
        [XmlIgnore]
        public SerializableDictionary<string, string> FieldMappings { get; set; } = new SerializableDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        [XmlElement("FieldMappings")]
        public SerializableDictionary<string, string> FieldMappingsXML
        {
            get { return FieldMappings; }
            set { FieldMappings = value; }
        }



        /// <summary>
        /// 是否向源表中写入同步状态
        /// </summary>
        public bool UpdateSyncState { get; set; } = true;


        [XmlIgnore]
        public SyncInfoDetail SyncLogsMaster { get; set; } = new SyncInfoDetail();
        [XmlIgnore]
        public SyncInfoDetail SyncLogsSlave { get; set; } = new SyncInfoDetail();




        public SyncTable()
        {
            //this.Key = new List<string>();
            this.IgnoreFields = new List<string>();
            //this.Key.Add("sId");
            this.SyncStateField = "SyncState";
            this.SyncErrorsField = "SyncErrors";
            //this.IgnoreFields.Add(this.SyncStateField.ToLower());
            //this.IgnoreFields.Add(this.SyncErrorsField.ToLower());
            //this.Action.AddRange(new List<SyncAction> { SyncAction.Insert, SyncAction.Update });
        }

        public SyncTable(string tableName, string key, string queryString, string destinationTable, SyncDirection direction)
            : this()
        {
            this.SqlTable = tableName;
            this.Key.Add(key);
            this.QueryString = queryString;
            this.OracleTable = destinationTable;
            this.Direction = direction;
        }
        public SyncTable(string tableName, string queryString, string destinationTable, SyncDirection direction)
            : this()
        {
            this.SqlTable = tableName;
            this.QueryString = queryString;
            this.OracleTable = destinationTable;
            this.Direction = direction;
        }

        public SyncTable(string tableName, SyncDirection direction)
            : this()
        {
            this.SqlTable = tableName;
            this.QueryStringFormat = "Select * From {0}"
                                    + string.Format(" Where {0} < {1} and {2} < 20",
                                                        this.SyncStateField, (int)SyncState.Sync,
                                                        this.SyncErrorsField);
            this.QueryString = string.Format(QueryStringFormat, tableName);

            this.OracleTable = tableName;
            this.Direction = direction;
        }

        public SyncTable(string tableName)
            : this(tableName, SyncDirection.Push)
        { }

        public SyncTable(string tableName, string ignoreFields)
            : this(tableName)
        {
            string[] igs = ignoreFields.Split(',');
            foreach (string ig in igs)
            {
                this.IgnoreFields.Add(ig.ToLower());
            }
        }
        public SyncTable(string tableName, string ignoreFields, SyncDirection direction)
            : this(tableName, ignoreFields)
        {
            this.Direction = direction;
        }
        /// <summary>
        /// 获取同步表的数据查询语句
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public string GetQueryString(string tableName)
        {
            return string.Format(QueryStringFormat, tableName);
        }

        public override string ToString()
        {
            string[] symbol = { "=", ">", "<", "<>", "?" };
            return string.Format("{0}{1}{2}", this.SqlTable, symbol[(int)this.Direction], this.OracleTable);
        }

        public string ToString(SyncDirection direct)
        {
            string[] symbol = { "=", ">", "<", "<>", "?" };
            string[] symbolFormat = { "{0}{1}{3}{2}", "{0}{1}{3}{2}", "{0}{3}{1}{2}", "{0}{1}{2}" };
            return string.Format(symbolFormat[(int)direct], this.SqlTable, symbol[(int)this.Direction], this.OracleTable, symbol[(int)direct]);
        }

    }
}
