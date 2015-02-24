using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Security
{
    public class NewEnciphermentUtils
    {
        private static readonly byte[] DEFAULT_DES_KEY = { 29, 61, 50, 151, 89, 238, 198, 204 };
        private static readonly byte[] DEFAULT_DES_IV = { 43, 134, 28, 227, 186, 0, 193, 127 };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputFilePath">源文件</param>
        /// <param name="outputFilePath">加密过后的文件</param>
        /// <returns></returns>
        public static bool DefEncryptFile(string inputFilePath, string outputFilePath)
        {
            return encryptFile(inputFilePath, outputFilePath, DEFAULT_DES_KEY, DEFAULT_DES_IV);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputFilePath">源文件</param>
        /// <param name="outputFilePath">加密过后的文件</param>
        /// <returns></returns>
        public static bool DefDecryptFile(string inputFilePath, string outputFilePath)
        {
            return decryptFile(inputFilePath, outputFilePath, DEFAULT_DES_KEY, DEFAULT_DES_IV);
        }

        /// <summary>
        /// 加密文件
        /// </summary>
        /// <param name="inputFilePath">源文件</param>
        /// <param name="outputFilePath">加密过后的文件</param>
        /// <param name="key">密钥</param>
        /// <returns>是否成功</returns>
        public static bool EncryptFile(string inputFilePath, string outputFilePath, string key)
        {
            if (key == null || key.Length != 8) { throw new ArgumentException("key参数必须为8位字符！"); }
            byte[] b = ASCIIEncoding.ASCII.GetBytes(key);
            return encryptFile(inputFilePath, outputFilePath, b, b);
        }

        /// <summary>
        ///  加密字节数组
        /// </summary>
        /// <param name="inBytes"></param>
        /// <param name="key"></param>
        /// <param name="IV"></param>
        /// <returns>加密成功则，返回加密后的字节数组。否则，则返回null</returns>
        public static byte[] EncryptBytes(byte[] inBytes, byte[] key, byte[] IV)
        {
            if (inBytes == null || key == null || IV == null)
            {
                return null;
            }
            byte[] outBytes = null;
            DESCryptoServiceProvider DES = null;
            try
            {
                DES = new DESCryptoServiceProvider();
                DES.Key = key;
                DES.IV = IV;
                using (ICryptoTransform encrypt = DES.CreateEncryptor())
                {
                    using (MemoryStream outStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(outStream, encrypt, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(inBytes, 0, inBytes.Length);
                            cryptoStream.FlushFinalBlock();
                            outBytes = outStream.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(NewEnciphermentUtils)).Error("加密出错！", ex);
            }
            finally
            {
                if (DES != null)
                {
                    DES.Clear();
                    DES = null;
                }
            }
            return outBytes;
        }

        /// <summary>
        /// 解密字节数组
        /// </summary>
        /// <param name="inBytes"></param>
        /// <param name="key"></param>
        /// <param name="IV"></param>
        /// <returns>解密成功则，返回解密后的字节数组。否则，则返回null</returns>
        public static byte[] DecryptBytes(byte[] inBytes, byte[] key, byte[] IV)
        {
            if (inBytes == null || key == null || IV == null)
            {
                return null;
            }
            byte[] outBytes = null;
            DESCryptoServiceProvider DES = null;
            try
            {
                DES = new DESCryptoServiceProvider();
                DES.Key = key;
                DES.IV = IV;
                using (ICryptoTransform encrypt = DES.CreateDecryptor())
                {
                    using (MemoryStream outStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(outStream, encrypt, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(inBytes, 0, inBytes.Length);
                            cryptoStream.FlushFinalBlock();
                            outBytes = outStream.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(NewEnciphermentUtils)).Error("加密出错！", ex);
            }
            finally
            {
                if (DES != null)
                {
                    DES.Clear();
                    DES = null;
                }
            }
            return outBytes;
        }

        /// <summary>
        /// 解密文件
        /// </summary>
        /// <param name="inputFilePath">源文件</param>
        /// <param name="outputFilePath">解密过后的文件</param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool DecryptFile(string inputFilePath, string outputFilePath, string key)
        {
            if (key == null || key.Length != 8) { throw new ArgumentException("key参数必须为8位字符！"); }
            byte[] b = ASCIIEncoding.ASCII.GetBytes(key);
            return decryptFile(inputFilePath, outputFilePath, b, b);
        }

        public static MemoryStream GetDecryptFileStream(string encryptedFile, string key)
        {
            if (key == null || key.Length != 8) { throw new ArgumentException("key参数必须为8位字符！"); }
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream();
                using (FileStream fsEncryptedFile = new FileStream(encryptedFile, FileMode.Open, FileAccess.Read))
                {
                    DESCryptoServiceProvider DES = new DESCryptoServiceProvider();
                    byte[] b = ASCIIEncoding.ASCII.GetBytes(key);
                    DES.Key = b;
                    DES.IV = b;
                    using (ICryptoTransform decrypt = DES.CreateDecryptor())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(fsEncryptedFile, decrypt, CryptoStreamMode.Read))
                        {
                            byte[] nByte = new byte[1024];
                            int nLength = -1;
                            while (nLength != 0)
                            {
                                nLength = cryptoStream.Read(nByte, 0, nByte.Length);
                                stream.Write(nByte, 0, nLength);
                                stream.Flush();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (stream != null)
                {
                    stream.Dispose();
                    stream = null;
                }
                Logger.Logger.GetLogger(typeof(NewEnciphermentUtils)).Error("解密出错！", ex);
            }
            return stream;
        }

        public static MemoryStream GetDecryptFileStream(Stream fsStream, string key)
        {
            if (key == null || key.Length != 8) { throw new ArgumentException("key参数必须为8位字符！"); }
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream();
                using (fsStream)
                {
                    DESCryptoServiceProvider DES = new DESCryptoServiceProvider();
                    byte[] b = ASCIIEncoding.ASCII.GetBytes(key);
                    DES.Key = b;
                    DES.IV = b;
                    using (ICryptoTransform decrypt = DES.CreateDecryptor())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(fsStream, decrypt, CryptoStreamMode.Read))
                        {
                            byte[] nByte = new byte[1024];
                            int nLength = -1;
                            while (nLength != 0)
                            {
                                nLength = cryptoStream.Read(nByte, 0, nByte.Length);
                                stream.Write(nByte, 0, nLength);
                                stream.Flush();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (stream != null)
                {
                    stream.Dispose();
                    stream = null;
                }
                Logger.Logger.GetLogger(typeof(NewEnciphermentUtils)).Error("解密出错！", ex);
            }
            return stream;
        }

        private static bool encryptFile(string inputFilePath, string outputFilePath, byte[] key, byte[] IV)
        {
            bool isSuccess = false;
            FileStream fsInputFile = null;
            try
            {
                fsInputFile = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
                using (FileStream fsEncryptedFile = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
                {
                    DESCryptoServiceProvider DES = new DESCryptoServiceProvider();
                    DES.Key = key;
                    DES.IV = IV;
                    using (ICryptoTransform encrypt = DES.CreateEncryptor())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(fsEncryptedFile, encrypt, CryptoStreamMode.Write))
                        {
                            byte[] nByte = new byte[1024];
                            long curLength = 0L;
                            long inputFileLength = fsInputFile.Length;
                            while (curLength < inputFileLength)
                            {
                                int nLength = fsInputFile.Read(nByte, 0, nByte.Length);
                                cryptoStream.Write(nByte, 0, nLength);
                                cryptoStream.Flush();
                                curLength += nLength;
                            }
                        }
                    }
                }
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                Logger.Logger.GetLogger(typeof(NewEnciphermentUtils)).Error("加密出错！", ex);
            }
            finally
            {
                if (fsInputFile != null)
                {
                    fsInputFile.Dispose();
                    fsInputFile = null;
                }
            }
            return isSuccess;
        }

        public static bool decryptFile(string inputFilePath, string outputFilePath, byte[] key, byte[] IV)
        {
            bool isSuccess = false;
            FileStream fsInputFile = null;
            try
            {
                fsInputFile = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
                using (FileStream fsOutputFile = new FileStream(outputFilePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    DESCryptoServiceProvider DES = new DESCryptoServiceProvider();
                    DES.Key = key;
                    DES.IV = IV;
                    using (ICryptoTransform decrypt = DES.CreateDecryptor())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(fsOutputFile, decrypt, CryptoStreamMode.Write))
                        {
                            byte[] nByte = new byte[1024];
                            long curLength = 0L;
                            long inputFileLength = fsInputFile.Length;
                            while (curLength < inputFileLength)
                            {
                                int nLength = fsInputFile.Read(nByte, 0, nByte.Length);
                                cryptoStream.Write(nByte, 0, nLength);
                                cryptoStream.Flush();
                                curLength += nLength;
                            }
                        }
                    }
                }
                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                Logger.Logger.GetLogger(typeof(NewEnciphermentUtils)).Error("解密出错！", ex);
            }
            finally
            {
                if (fsInputFile != null)
                {
                    fsInputFile.Dispose();
                    fsInputFile = null;
                }
            }
            return isSuccess;
        }
    }
}
