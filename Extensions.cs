using System;
using System.Collections.Generic;
using System.Text;

namespace SqlSync
{
    public static class Extensions
    {
        /// <summary>
        /// 返回各优先级的间隔时间
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static int DelayTime(this SyncPriority p)
        {
            return (int)p * 10 * 1000;
        }
    }

}
