using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SqlSync.Sync
{
    public enum SyncDirection
    {
        [Description("=")]
        None = 0x00,
        [Description(">")]
        Push = 0x01,
        [Description("<")]
        Pull = 0x02,
        [Description("<>")]
        Sync = Push | Pull,
        [Description("?")]
        Unkown = 0x04
    }
}
