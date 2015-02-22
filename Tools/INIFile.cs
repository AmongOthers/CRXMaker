using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

namespace Tools
{
    public class INIFile
    {
        public string FilePath { get; private set; }
        public string FileName { get; private set; }

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section,
            string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section,
            string key, string def, StringBuilder retVal,
            int size, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section,
                string key, string def, byte[] retVal, int size, string filePath);

        public INIFile(string iniPath)
        {
            var fullPath = Path.GetDirectoryName(iniPath);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            FilePath = iniPath;
            FileName = Path.GetFileName(FilePath);
        }

        public void WriteString(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, this.FilePath);
        }

        public string ReadString(string section, string key)
        {
            StringBuilder temp = new StringBuilder(65535);
            int i = GetPrivateProfileString(section, key, "", temp,
                                            65535, this.FilePath);
            return temp.ToString();
        }

        public int ReadInteger(string section, string key, int defaultValue)
        {
            int result = defaultValue;
            Int32.TryParse(ReadString(section, key), out result);
            return result;
        }

        public void WriteInteger(string section, string key, int value)
        {
            WriteString(section, key, value.ToString());
        }

        public bool ReadBool(string section, string key, bool defaultValue)
        {
            bool result = defaultValue;
            Boolean.TryParse(ReadString(section, key), out result);
            return result;
        }

        public void WriteBool(string section, string key, bool value)
        {
            WriteString(section, key, Convert.ToString(value));
        }

        public void DeleteKey(string section, string key)
        {
            WritePrivateProfileString(section, key, null, this.FilePath);
        }

        /// <summary>
        /// 将指定的Section名称中的所有Key添加到列表中
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public List<string> ReadSectionKeys(string section)
        {
            Byte[] buffer = new Byte[16384];
            int bufLen = GetPrivateProfileString(section, null, null, buffer, buffer.GetUpperBound(0),
                this.FilePath);
            //对Section进行解析
            return GetStringsFromBuffer(buffer, bufLen);
        }

        /// <summary>
        ///  读取指定的Section的所有Value到列表中
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public NameValueCollection ReadSectionValues(string section)
        {
            var keys = ReadSectionKeys(section);
            var values = new NameValueCollection();
            foreach (string key in keys)
            {
                values.Add(key, ReadString(section, key));
            }
            return values;
        }

        /// <summary>
        /// 读取所有的Sections的名称
        /// </summary>
        /// <returns></returns>
        public List<string> ReadSections()
        {
            //Note:必须得用Bytes来实现，StringBuilder只能取到第一个Section
            byte[] Buffer = new byte[65535];
            int bufLen = 0;
            bufLen = GetPrivateProfileString(null, null, null, Buffer,
            Buffer.GetUpperBound(0), this.FilePath);
            return GetStringsFromBuffer(Buffer, bufLen);
        }

        private List<string> GetStringsFromBuffer(Byte[] Buffer, int bufLen)
        {
            var list = new List<string>();
            if (bufLen != 0)
            {
                int start = 0;
                for (int i = 0; i < bufLen; i++)
                {
                    if ((Buffer[i] == 0) && ((i - start) > 0))
                    {
                        String s = Encoding.GetEncoding(0).GetString(Buffer, start, i - start);
                        list.Add(s);
                        start = i + 1;
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// 清除某个Section
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public bool EraseSection(string section)
        {
            return WritePrivateProfileString(section, null, null, this.FilePath) > -1;
        }

        /// <summary>
        /// 检查某个Section下的某个键值是否存在
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ValueExists(string section, string key)
        {
            var keys = ReadSectionKeys(section);
            return keys.IndexOf(key) > -1;
        }

        //Note:对于Win9X，来说需要实现UpdateFile方法将缓冲中的数据写入文件
        //在Win NT, 2000和XP上，都是直接写文件，没有缓冲，所以，无须实现UpdateFile
        //执行完对Ini文件的修改之后，应该调用本方法更新缓冲区。
        private void updateFile()
        {
            WritePrivateProfileString(null, null, null, this.FilePath);
        }

        //确保资源的释放
        ~INIFile()
        {
            updateFile();
        }
    }
}
