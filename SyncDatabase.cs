using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlSync
{
    /// <summary>
    /// 要进行同步的数据库
    /// </summary>
    internal class SyncDatabase
    {
        /// <summary>
        /// 数据库类型
        /// </summary>
        internal DatabaseType Type { get; set; }
        /// <summary>
        /// 连接字符串
        /// </summary>
        internal string ConnectionString { get; set; }

    }
}
