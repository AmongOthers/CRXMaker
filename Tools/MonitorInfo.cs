using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    public class MonitorInfo
    {
        private static MonitorInfo sMonitorInfo;
        private MonitorInfo() { }

        public static MonitorInfo GetInstance()
        {
            if (sMonitorInfo == null)
            {
                sMonitorInfo = new MonitorInfo();
            }
            return sMonitorInfo;
        }

        public string ClientSystemType { get; set; }
        public string ClientVerCode { get; set; }
        public string ClientVerName { get; set; }
        public string ClientTypeId { get; set; }
        public string ClientType { get; set; }

        public string UserId { get; set; }
        public string UserName { get; set; }
        public string CpStaffId { get; set; }
        public string ChannelId { get; set; }
        public string RegionId { get; set; }
        public string Remark { get; set; }
        public int CPUPercent { get; set; }
        public long AvailableMemory { get; set; }
        public long TotalMemory { get; set; }
        public long DiskAvailableSpace { get; set; }
        public long DiskTotalSpace { get; set; }
        public string MacAddress { get; set; }
    }
}
