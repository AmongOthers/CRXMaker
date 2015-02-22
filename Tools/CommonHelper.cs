using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Diagnostics;
using System.Linq;

namespace Tools
{
    public class CommonHelper
    {
        public static Int64 GetUtcNow()
        {
            return Decimal.ToInt64(Decimal.Divide(DateTime.UtcNow.Ticks - 621355968000000000, 10000));
        }

        public static WebClient sWebClient;

        public static string formatSize(string sizeStr)
        {
            double temp = 0;
            double dSize = double.Parse(sizeStr) / 1024.0;
            String suffix = "K";
            temp = dSize / 1024.0;
            if (temp > 1)
            {
                dSize = temp;
                suffix = "M";
                temp = dSize / 1024.0;
                if (temp > 1)
                {
                    dSize = temp;
                    suffix = "G";
                }
            }
            StringBuilder resultBuffer = new StringBuilder(Math.Round(dSize, 2).ToString());
            resultBuffer.Append(suffix);
            return resultBuffer.ToString();
        }
        //如果本地文件的存放目录不存在，将自动创建，注意路径暂时只能兼容“/”的情况
        public static bool download(string url, string path)
        {
            if (sWebClient == null)
            {
                sWebClient = new WebClient();
            }
            const String DOWNLOAD_TEMP_PATH = "download_temp";
            if (!Directory.Exists(DOWNLOAD_TEMP_PATH))
            {
                Directory.CreateDirectory(DOWNLOAD_TEMP_PATH);
            }
            String fileName = Path.GetFileName(path);
            String tempFullpath = Path.Combine(DOWNLOAD_TEMP_PATH, fileName);
            try
            {
                int lastSepIndex = path.LastIndexOf("/");
                if (lastSepIndex > 0)
                {
                    string containingPath = path.Substring(0, lastSepIndex);
                    if (!Directory.Exists(containingPath))
                    {
                        Directory.CreateDirectory(containingPath);
                    }
                }
                if (File.Exists(tempFullpath))
                {
                    File.Delete(tempFullpath);
                }
                sWebClient.DownloadFile(url, tempFullpath);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                File.Move(tempFullpath, path);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string getPathWithoutZipPostfix(string zipFilePath)
        {
            if (zipFilePath.EndsWith(".zip"))
            {
                int len = zipFilePath.Length;
                return zipFilePath.Substring(0, len - 4);
            }
            else
            {
                return null;
            }
        }

        public static void DeleteWhaterverOnExist(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    DeleteFolder(path);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }

        public static void DeleteFolder(string dir)
        {
            try
            {
                Directory.Delete(dir, true);
                File.Delete(dir + ".zip");
            }
            catch
            {
            }
        }

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
            catch(Exception ex)
            {
                Logger.Logger.GetLogger(typeof(CommonHelper).Name).Error("解压文件出错",ex);
                return false;
            }
        }

        public static bool openDocuments(String path)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            if (!string.IsNullOrEmpty(dir.FullName))
            {
                System.Diagnostics.Process explorer = System.Diagnostics.Process.Start("explorer.exe", dir.FullName);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string getPostfixWithDot(string path)
        {
            int lastDotIndex = path.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                return path.Substring(lastDotIndex);
            }
            else
            {
                return string.Empty;
            }
        }


        public static string getNowString()
        {
            return DateTime.Now.ToString("yyMMddHHmmss");
        }

        public static string getCurrentDiskPath()
        {
            string volume = Path.GetPathRoot(Directory.GetCurrentDirectory());
            return volume;
        }

        static string sDocPath = null;
        public static string GetMyDocumentPathOrCurrentDrivePath()
        {
            if (sDocPath == null)
            {
				var folderType = Environment.SpecialFolder.MyDocuments;
				String docPath = Environment.GetFolderPath(folderType);
				sDocPath = getSpecialFolderOrCurrentDrivePath(docPath);
            }
			return sDocPath;
        }

        static string sTempPath = null;
        public static string GetTempPathOrCurrentDrivePath()
        {
            if (sTempPath == null)
            {
				var tempPath = Environment.GetEnvironmentVariable("TEMP");
				return getSpecialFolderOrCurrentDrivePath(tempPath);
            }
            return sTempPath;
        }

        private static string getSpecialFolderOrCurrentDrivePath(string targetPath)
        {
            if (!String.IsNullOrEmpty(targetPath))
            {
                if (Directory.Exists(targetPath))
                {
                    return targetPath;
                }
                else
                {
                    try
                    {
                        Directory.CreateDirectory(targetPath);
                        return targetPath;
                    }
                    catch (Exception e)
                    {
                        Logger.Logger.GetLogger(typeof(CommonHelper)).Error("CommonHelper.GetMyDocumentPathOrCurrentDrivePath", e);
                    }
                }
            }
            return Path.GetPathRoot(System.AppDomain.CurrentDomain.BaseDirectory);
        }

        public static String suitPath(long size)
        {
            string result = null;
            String docPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            if (!string.IsNullOrEmpty(docPath))
            {
                String disk = docPath.Substring(0, 1);
                DriveInfo driveInfo = new DriveInfo(disk);
                if(isDriveFreeSpaceMoreThan(driveInfo, size))
                {
                    result = docPath;
                }
            }
            if (result == null)
            {
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                foreach (DriveInfo d in allDrives)
                {
                    if (d.DriveType == DriveType.Fixed)
                    {
                        if(isDriveFreeSpaceMoreThan(d, size))
                        {
                            result = d.Name;
                        }
                    }
                }
           }
            return result;
        }

        private static bool isDriveFreeSpaceMoreThan(DriveInfo drive,
            long size)
        {
            long freeSpace = drive.AvailableFreeSpace;
            return freeSpace > size;
        }

        public static void openUrlInIe(string url)
        {
            try
            {
                Process p = new Process();//实例化进程对象 
                //StartInfo用于设置启动进程所需参数 
                p.StartInfo.FileName = "iexplore.exe";//设置要启动的应用程序或文档 
                p.StartInfo.Arguments = url;//设置启动所需命令行参数 
                p.Start();//将进程与Process组件关联并启动 
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static void copyDirectory(DirectoryInfo diSrcDir, DirectoryInfo diDstDir)
        {
            if (!diDstDir.Exists)
            {
                diDstDir.Create();
            }
            FileInfo[] fiSrcFiles = diSrcDir.GetFiles();
            foreach (FileInfo fiSrcFile in fiSrcFiles)
            {
                fiSrcFile.CopyTo(Path.Combine(diDstDir.FullName, fiSrcFile.Name));
            }
            DirectoryInfo[] diSrcDirectories = diSrcDir.GetDirectories();
            foreach (DirectoryInfo diSrcDirectory in diSrcDirectories)
            {
                copyDirectory(diSrcDirectory, new DirectoryInfo(Path.Combine(diDstDir.FullName, diSrcDirectory.Name)));
            }
        }

        public static string ContactStrs(List<string> strs, string sep)
        {
            if (strs == null)
            {
                return null;
            }
            else if (strs.Count == 0)
            {
                return string.Empty;
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < strs.Count - 1; i++)
                {
                    sb.Append(strs[i] + sep);
                }
                sb.Append(strs[strs.Count - 1]);
                return sb.ToString();
            }
        }

        public static string ContactAsStr<T>(List<T> values, System.Converter<T, string> converter, string sep)
        {
            if (values == null)
            {
                return null;
            }
            else if (values.Count == 0)
            {
                return string.Empty;
            }
            List<string> strs = values.ConvertAll<string>(converter);
            return ContactStrs(strs, sep);
        }
    }
}
