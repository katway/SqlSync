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
      
    }
}
