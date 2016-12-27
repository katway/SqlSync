using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlSync.Sync
{
    /// <summary>
    /// 每条纪录的同步结果
    /// </summary>
    public class SyncResult
    {
        /// <summary>
        /// 纪录所在源表名
        /// </summary>
        public string OriginTable { get; set; }
        /// <summary>
        /// 主键字段值
        /// </summary>
        public IList<object> KeyFieldValues { get; set; } = new List<object>();
        /// <summary>
        /// 同步操作的结果
        /// </summary>
        public SyncState State { get; set; }
    }
}
