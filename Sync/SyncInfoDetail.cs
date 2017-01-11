﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlSync.Sync
{

    public class SyncInfoDetail
    {
        public static SerializableDictionary<string, string> Mappings { get; set; } = new SerializableDictionary<string, string>();

        public string TableName { get; set; }
        public DateTime? ModifyTime { get; set; }

        public DateTime? SyncTime { get; set; }

        static SyncInfoDetail()
        {
            Mappings.Add("TableName", "TableName");
            Mappings.Add("ModifyTime", "ModifyTime");
            Mappings.Add("SyncTime", "SyncTime");
        }
    }
}
