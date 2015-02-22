using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace Tools
{
    public class Zip
    {
        //将压缩文件解压成为文件夹形式
        //经测试，winrar3.85制造的压缩包无法使用这个函数解压，3.9.3可以，4.0.1可以
        public static bool unzipAsDir(string zipFilePath, string afterPath)
        {
            //EnciphermentUtils.DecryptData(zipFilePath, afterPath + ".zip");
            const int SIZE = 2048;
            try
            {
                if (!Directory.Exists(afterPath))
                {
                    Directory.CreateDirectory(afterPath);
                }
                ZipInputStream s = new ZipInputStream(File.OpenRead(zipFilePath));

                ZipEntry theEntry;
                while ((theEntry = s.GetNextEntry()) != null)
                {
                    string directoryName = Path.GetDirectoryName(theEntry.Name);
                    string fileName = Path.GetFileName(theEntry.Name);
                    if (directoryName != string.Empty)
                    {
                        Directory.CreateDirectory(Path.Combine(afterPath, directoryName));
                    }
                    if (fileName != string.Empty)
                    {
                        FileStream streamWrtier = File.Create(Path.Combine(afterPath, theEntry.Name));

                        byte[] data = new byte[SIZE];
                        int size;
                        while (true)
                        {
                            size = s.Read(data, 0, data.Length);
                            if (size > 0)
                            {
                                streamWrtier.Write(data, 0, size);
                            }
                            else
                            {
                                break;
                            }
                        }
                        streamWrtier.Close();
                    }
                }
                s.Close();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger("crxmaker").Error(ex);
                return false;
            }
        }

    }
}
