using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace SqlSync
{
    [Serializable]
    public class SyncTable
    {
        /// <summary>
        /// 数据表名
        /// </summary>
        public string MasterTable { get; set; }

        /// <summary>
        /// 主键
        /// </summary>
        [XmlIgnore]
        public List<string> Key { get; private set; }
        [XmlElement("Key")]
        public string Keys
        {
            get { return string.Join(",", Key.ToArray()); }
            set
            {
                string[] es = value.Split(',');
                foreach (string e in es)
                    Key.Add(e.ToLower());
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
        public List<string> IgnoreFields { get; set; }

        /// <summary>
        /// 目标表
        /// </summary>
        public string SlaveTable { get; set; }

        /// <summary>
        /// 同步方向
        /// </summary>
        public SyncDirection Direction { get; set; }
        /// <summary>
        /// 同步优先级
        /// </summary>
        public SyncPriority Priority { get; set; }

        public SyncTable()
        {
            this.Key = new List<string>();
            this.IgnoreFields = new List<string>();
            //this.Key.Add("sId");
            this.SyncStateField = "SyncState";
            this.SyncErrorsField = "SyncErrors";
            //this.IgnoreFields.Add(this.SyncStateField.ToLower());
            //this.IgnoreFields.Add(this.SyncErrorsField.ToLower());
        }

        public SyncTable(string tableName, string key, string queryString, string destinationTable, SyncDirection direction)
            : this()
        {
            this.MasterTable = tableName;
            this.Key.Add(key);
            this.QueryString = queryString;
            this.SlaveTable = destinationTable;
            this.Direction = direction;
        }
        public SyncTable(string tableName, string queryString, string destinationTable, SyncDirection direction)
            : this()
        {
            this.MasterTable = tableName;
            this.QueryString = queryString;
            this.SlaveTable = destinationTable;
            this.Direction = direction;
        }

        public SyncTable(string tableName, SyncDirection direction)
            : this()
        {
            this.MasterTable = tableName;
            this.QueryStringFormat = "Select * From {0}"
                                    + string.Format(" Where {0} < {1} and {2} < 20",
                                                        this.SyncStateField, (int)SyncState.Sync,
                                                        this.SyncErrorsField);
            this.QueryString = string.Format(QueryStringFormat, tableName);

            this.SlaveTable = tableName;
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
            string[] symbol = { ">", "<", "<>" };
            
            return string.Format("{0}{1}{2}", this.MasterTable, symbol[(int)this.Direction], this.SlaveTable);
        }
       
    }
}
