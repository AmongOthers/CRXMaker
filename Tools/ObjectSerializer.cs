using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Text;
using System.Runtime.Serialization.Formatters;

namespace Tools
{
    public class ObjectSerializer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <exception cref="System.Runtime.Serialization.SerializationException"></exception>
        /// <exception cref="System.Security.SecurityException"></exception>
        /// <returns></returns>
        public static string SerializeObject(object obj)
        {
            if (obj != null)
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.AssemblyFormat = FormatterAssemblyStyle.Simple;
                using (MemoryStream ms = new MemoryStream())
                {
                    formatter.Serialize(ms, obj);
                    ms.Position = 0;
                    byte[] nByte = new byte[ms.Length];
                    ms.Read(nByte, 0, nByte.Length);
                    return Convert.ToBase64String(nByte);
                }
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <exception cref="System.FormatException"></exception>
        /// <exception cref="System.Runtime.Serialization.SerializationException"></exception>
        /// <exception cref="System.Security.SecurityException"></exception>
        /// <returns></returns>
        public static T DeserializeObject<T>(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                byte[] nByte = Convert.FromBase64String(value);
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.AssemblyFormat = FormatterAssemblyStyle.Simple;
                using (MemoryStream ms = new MemoryStream(nByte))
                {
                    ms.Position = 0;
                    return (T)formatter.Deserialize(ms);
                }
            }
            return default(T);
        }
    }
}
