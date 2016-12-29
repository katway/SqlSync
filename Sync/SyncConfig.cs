using System;
using System.Collections.Generic;
using SqlSync.Sync;

namespace SqlSync.Sync
{
    [Serializable]
    public class SyncConfig
    {

        /// <summary>
        /// 源数据库的连接字符串
        /// </summary>
        public string SqlConnectionString { get; set; }
        /// <summary>
        /// 目标数据库的连接字符串
        /// </summary>
        public string OracleConnectionString { get; set; }

        /// <summary>
        /// 要同步数据的检索语句
        /// </summary>
        public List<SyncTable> SyncTables { get; set; }

        /// <summary>
        /// 自动添加同步所需要的状态字段
        /// </summary>
        public bool AppendSyncFields { get; set; }
        /// <summary>
        /// 同步信息
        /// </summary>
        public SyncInfo SyncInfo { get; set; } = new SyncInfo();

        public SyncConfig()
        {
            this.SyncTables = new List<SyncTable>();
            AppendSyncFields = true;
        }
    }
}
