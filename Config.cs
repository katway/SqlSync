using System;
using System.Collections.Generic;
using System.Text;

namespace SqlSync
{
    [Serializable]
    public class Config
    {

        /// <summary>
        /// 源数据库的连接字符串
        /// </summary>
        public string LocalConnectionString { get; set; }
        /// <summary>
        /// 要同步数据的检索语句
        /// </summary>
        public List<SyncTable> SyncTables { get; set; }
        /// <summary>
        /// 目标数据库的连接字符串
        /// </summary>
        public string RemoteConnectionString { get; set; }
        /// <summary>
        /// 自动添加同步所需要的状态字段
        /// </summary>
        public bool AppendSyncFields { get; set; }

        public Config()
        {
            this.SyncTables = new List<SyncTable>();
            AppendSyncFields = true;
        }
    }
}
