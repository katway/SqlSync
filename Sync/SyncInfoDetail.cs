using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlSync.Sync
{

    public class SyncInfoDetail
    {
        public static SerializableDictionary<string, string> Mappings { get; set; } = new SerializableDictionary<string, string>();

        public string TableName { get; set; }
        public DateTime? ModifyTime
        {
            get { return modifyTime; }
            set
            {
                if (value.HasValue)
                    modifyTime = value.Value;
                else
                    modifyTime = DateTime.MinValue;
            }
        }
        public DateTime modifyTime;
        public DateTime? SyncTime
        {
            get { return syncTime; }
            set
            {
                if (value.HasValue)
                    syncTime = value.Value;
                else
                    syncTime = DateTime.MinValue;
            }
        }
        public DateTime syncTime;
        static SyncInfoDetail()
        {
            Mappings.Add("TableName", "TableName");
            Mappings.Add("ModifyTime", "ModifyTime");
            Mappings.Add("SyncTime", "SyncTime");
        }
    }
}
