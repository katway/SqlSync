using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SqlSync.Sync
{
    [Serializable]
    public class SyncInfo
    {
        /// <summary>
        /// 是否自动创建纪录表
        /// </summary>
        public bool AutoCreate { get; set; } = true;
        /// <summary>
        /// 是否启用同步信息表
        /// </summary>
        public bool Enable { get; set; } = true;
        /// <summary>
        /// 使用的表名
        /// </summary>
        public string TableName { get; set; } = "SyncInfo";

        [XmlIgnore]
        public IList<SyncLog> SyncLogsMaster { get; set; } = new List<SyncLog>();
        [XmlIgnore]
        public IList<SyncLog> SyncLogsSlave { get; set; } = new List<SyncLog>();

    }

    public class SyncLog
    {
        public static SerializableDictionary<string, string> Mappings { get; set; } = new SerializableDictionary<string, string>();

        public string TableName { get; set; }
        public DateTime? ModifyTime { get; set; }
        public DateTime? SyncTime { get; set; }

        static SyncLog()
        {
            Mappings.Add("TableName", "TableName");
            Mappings.Add("ModifyTime", "ModifyTime");
            Mappings.Add("SyncTime", "SyncTime");
        }
    }
}
