using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Tools
{
    public class MD5Helper
    {
        public static string GetMd5OfFile(string file_path)
        {
            string md5 = string.Empty;
            try
            {
                if (File.Exists(file_path))
                {
                    using (FileStream fs = new FileStream(file_path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        md5 = GetMd5(fs);
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Logger.GetLogger(typeof(MD5Helper)).ErrorFormat(ex.Message);
                return null;
            }
            return md5;
        }

        public static string GetMd5OfString(string str)
        {
            return GetMd5OfString(str, Encoding.Unicode);
        }

        public static string GetMd5OfString(string str, Encoding encoding)
        {
            using (MemoryStream ms = new MemoryStream(encoding.GetBytes(str)))
            {
                return GetMd5(ms);
            }
        }

        public static string GetMd5(Stream stream)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider md5_provider =
    new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] hash_byte = md5_provider.ComputeHash(stream);
            String md5 = System.BitConverter.ToString(hash_byte);
            md5 = md5.Replace("-", "");
            return md5;
        }
    }
}
