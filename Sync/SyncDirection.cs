using System;
using System.Collections.Generic;
using System.Text;

namespace SqlSync.Sync
{
    public enum SyncDirection
    {
        None = 0,
        Push = 1,
        Pull = 2,
        Sync = Push | Pull
    }
}
