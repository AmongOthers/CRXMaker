using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Tools
{
    [XmlRoot("AndroidDaemonConfig")]
    public class AndroidDaemonConfig2
    {
        [XmlElement("Package")]
        public string Package { get; set; }
        [XmlElement("Activity")]
        public string Activity { get; set; }
        [XmlElement("Activity2")]
        public string Activity2 { get; set; }
        [XmlElement("Server")]
        public string Server { get; set; }
        [XmlElement("VersionCode")]
        public string VersionCode { get; set; }


        public void Save(string file)
        {
            try
            {
                ObjectXmlSerializer.SerializeToXmlFile(file, this);
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(AndroidDaemonConfig2)).Error("保存守护端配置文件出错", ex);
            }
        }

        public static AndroidDaemonConfig2 Create(string file)
        {
            try
            {
                AndroidDaemonConfig2 config = ObjectXmlSerializer.DeserializeFromXmlFile<AndroidDaemonConfig2>(file);
                return config;
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(AndroidDaemonConfig2)).Error("读取守护端配置文件出错", ex);
                return null;
            }
        }
    }
}
