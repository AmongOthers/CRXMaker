using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Tools
{
    public class ObjectXmlSerializer
    {
        public static T Deserialize<T>(string xml)
        {
            return Deserialize<T>(xml, Encoding.UTF8);
        }

        public static T Deserialize<T>(string xml, Encoding encoding)
        {
            XmlSerializer xs = new XmlSerializer(typeof(T));
            byte[] nByte = encoding.GetBytes(xml);
            using (MemoryStream ms = new MemoryStream(nByte))
            {
                return (T)xs.Deserialize(ms);
            }
        }

        public static T DeserializeFromXmlFile<T>(string xmlFilePath)
        {
            return DeserializeFromXmlFile<T>(xmlFilePath, Encoding.UTF8);
        }

        public static T DeserializeFromXmlFile<T>(string xmlFilePath, Encoding encoding)
        {
            using (StreamReader reader = new StreamReader(xmlFilePath, encoding))
            {
                string xml = reader.ReadToEnd();
                return Deserialize<T>(xml, encoding);
            }
        }


        public static string Serialize(object obj)
        {
            return Serialize(obj, Encoding.UTF8);
        }

        public static string Serialize(object obj, Encoding encoding)
        {
            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            //去除开头的<?xml version="1.0" encoding="utf-8"?>
            xmlSettings.OmitXmlDeclaration = true;
            //
            xmlSettings.Encoding = encoding;
            //缩进
            xmlSettings.Indent = true;
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");//设置XML的命名空间为空串
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter writer = XmlWriter.Create(ms, xmlSettings))
                {
                    XmlSerializer xs = new XmlSerializer(obj.GetType());
                    xs.Serialize(writer, obj, ns);
                    writer.Flush();
                }
                ms.Position = 0;
                byte[] nByte = new byte[ms.Length];
                ms.Read(nByte, 0, nByte.Length);
                return encoding.GetString(nByte);
            }
        }

        public static bool SerializeToXmlFile(string xmlFilePath, object obj)
        {
            return SerializeToXmlFile(xmlFilePath, obj, Encoding.UTF8);
        }

        public static bool SerializeToXmlFile(string xmlFilePath, object obj, Encoding encoding)
        {
            try
            {
                XmlWriterSettings xmlSettings = new XmlWriterSettings();
                //去除开头的<?xml version="1.0" encoding="utf-8"?>
                xmlSettings.OmitXmlDeclaration = true;
                //
                xmlSettings.Encoding = encoding;
                //缩进
                xmlSettings.Indent = true;
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("", "");//设置XML的命名空间为空串
                using (XmlWriter writer = XmlWriter.Create(xmlFilePath, xmlSettings))
                {
                    XmlSerializer xs = new XmlSerializer(obj.GetType());
                    xs.Serialize(writer, obj, ns);
                    writer.Flush();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
