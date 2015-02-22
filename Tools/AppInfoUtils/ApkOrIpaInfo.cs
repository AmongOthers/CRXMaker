using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Tools.AppInfoUtils
{
    public class ApkOrIpaInfo
    {
        [JsonProperty("ApkOrIpaPath")]
        public string ApkOrIpaPath { get; set; }
        //图标
        [JsonProperty("AppIcon")]
        public string AppIcon { get; set; }
        //应用名
        [JsonProperty("AppName")]
        public string AppName { get; set; }
        //版本
        [JsonProperty("AppVersion")]
        public string AppVersion { get; set; }
        //版本
        [JsonProperty("PackageName")]
        public string PackageName { get; set; }
        //系统要求
        [JsonProperty("AppSystemRequirements")]
        public string AppSystemRequirements { get; set; }
        //权限
        [JsonProperty("AppPermissions")]
        public List<string> AppPermissions = new List<string>();
    }
}
