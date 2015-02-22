using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Zip;
using System.Threading;

namespace Tools
{
    public abstract class UploadLog
    {
        protected string mFolderId;
        protected string mLogFilePath;
        protected const string mCurrentLogFileName = "log.xml";
        protected const string mZipSuffix = ".zip";
        protected const string mXmlSuffix = ".xml";
        protected string mLogDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "4SLOG");
        protected string mTempLogDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"4SLOG\tmp");

        public string LogFileName { get; set; }

        protected abstract bool ZipFile();

        /// <summary>
        /// 获取日志目录下所有日志文件
        /// </summary>
        /// <returns></returns>
        protected virtual List<FileInfo> GetLogDirFiles()
        {
            List<FileInfo> list = new List<FileInfo>();
            Regex logFileMatch = new Regex(@"log.xml.(\d+)|log.xml");
            try
            {
                if (Directory.Exists(mLogDirPath))
                {
                    var fileNameList = Directory.GetFiles(mLogDirPath);
                    Array.ForEach(fileNameList, fileName =>
                    {
                        FileInfo logFileInfo = new FileInfo(fileName);
                        if (logFileMatch.IsMatch(logFileInfo.Name))
                        {
                            list.Add(logFileInfo);
                        }
                        else
                        {
                            try
                            {
                                //删除日志文件以外的文件.zip
                                logFileInfo.Delete();
                            }
                            catch 
                            {
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("获取日志文件出错",ex);
            }
            return list;
        }

        /// <summary>
        /// 创建临时目录
        /// </summary>
        /// <returns></returns>
        protected virtual bool CreateTempLogDir()
        {
            try
            {
                if (Directory.Exists(mTempLogDirPath))
                {
                    Directory.Delete(mTempLogDirPath, true);
                }
                Directory.CreateDirectory(mTempLogDirPath);
                for (int i = 0; i < 3; i++)
                {
                    if (!Directory.Exists(mTempLogDirPath))
                    {
                        Directory.CreateDirectory(mTempLogDirPath);
                    }
                    else
                    {
                        break;
                    }
                    System.Threading.Thread.Sleep(500);
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("创建临时目录出错", ex);
            }
            return false;
        }

        protected virtual bool copyCurrentLogFile()
        {
            try
            {
                if (CreateTempLogDir())
                {
                    var currentLogFilePath = Path.Combine(mLogDirPath, mCurrentLogFileName);
                    if (File.Exists(currentLogFilePath))
                    {
                        mLogFilePath = Path.Combine(mTempLogDirPath, LogFileName + mXmlSuffix);
                        File.Copy(currentLogFilePath, mLogFilePath, true);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("复制log.xml到临时目录出错", ex);
            }
            return false;
        }

        /// <summary>
        /// 功能：压缩文件（暂时只压缩文件夹下一级目录中的文件，文件夹及其子级被忽略）
        /// </summary>
        /// <param name="dirPath">被压缩的文件夹夹路径</param>
        /// <param name="zipFilePath">生成压缩文件的路径，为空则默认与被压缩文件夹同一级目录，名称为：文件夹名+.zip</param>
        /// <param name="err">出错信息</param>
        /// <returns>是否压缩成功</returns>
        protected virtual bool ZipFile(string dirPath, string zipFilePath, out string err)
        {
            err = "";
            if (dirPath == string.Empty)
            {
                err = "要压缩的文件夹不能为空！";
                return false;
            }
            if (!Directory.Exists(dirPath))
            {
                err = "要压缩的文件夹不存在！";
                return false;
            }
            //压缩文件名为空时使用文件夹名＋.zip
            if (zipFilePath == string.Empty)
            {
                if (dirPath.EndsWith("\\"))
                {
                    dirPath = dirPath.Substring(0, dirPath.Length - 1);
                }
                zipFilePath = dirPath + ".zip";
            }

            try
            {
                string[] filenames = Directory.GetFiles(dirPath);
                using (ZipOutputStream s = new ZipOutputStream(File.Create(zipFilePath)))
                {
                    s.SetLevel(9);
                    byte[] buffer = new byte[4096];
                    foreach (string file in filenames)
                    {
                        ZipEntry entry = new ZipEntry(Path.GetFileName(file));
                        entry.DateTime = DateTime.Now;
                        s.PutNextEntry(entry);
                        using (FileStream fs = File.OpenRead(file))
                        {
                            int sourceBytes;
                            do
                            {
                                sourceBytes = fs.Read(buffer, 0, buffer.Length);
                                s.Write(buffer, 0, sourceBytes);
                            } while (sourceBytes > 0);
                        }
                    }
                    s.Finish();
                    s.Close();
                }
            }
            catch (Exception ex)
            {
                err = ex.Message;
                return false;
            }
            return true;
        }

        public virtual bool UploadFile(out string serverResponse)
        {
            serverResponse = String.Empty;
            try
            {
                if (CreateTempLogDir())
                {
                    if (!ZipFile())
                    {
                        copyCurrentLogFile();
                    }

                    var fileInfo = new FileInfo(mLogFilePath);
                    if (!fileInfo.Exists)
                    {
                        serverResponse = "文件不存在:" + mLogFilePath;
                        return false;
                    }
 
                    var result = MailUtils.SendEmail(LogFileName, String.Format("上传日志文件{0}进行debug", LogFileName), mLogFilePath);
                    Logger.Logger.GetLogger(this).Info("邮件发送日志文件结果:" + result);
         
                    var url = "http://www.phone580.com:8082/xfolder/openapi/upload";
                    var dict = new Dictionary<string, object>() 
                            {
                                { "folderid",mFolderId },
                                { "uploadfile", new FormFile() { Name = fileInfo.Name, ContentType = "application/octet-stream", FilePath = mLogFilePath } }
                            };

                    for (int i = 0; i < 5; i++)
                    {
                        using (HttpWebResponse response = RequestHelper.PostMultipart(url, dict))
                        {
                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                using (Stream responseStream = response.GetResponseStream())
                                {
                                    using (StreamReader reader = new StreamReader(responseStream))
                                    {
                                        serverResponse = reader.ReadToEnd();
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    Logger.Logger.GetLogger(this).Info("上传日志文件到服务器的错误信息:" + serverResponse);
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("上传日志文件到服务器时出错", ex);
            }
            return false;
        }
    }

    public class UploadBugLog : UploadLog
    {
        public UploadBugLog()
        {
            mFolderId = "/logs/auto/pc";
        }

        protected override bool ZipFile()
        {
            var error = String.Empty;
            try
            {
                var fileInfo = GetLogDirFiles().OrderByDescending(c => c.LastWriteTime).FirstOrDefault();
                if (Directory.Exists(mTempLogDirPath))
                {
                    fileInfo.CopyTo(Path.Combine(mTempLogDirPath, fileInfo.Name), true);
                    mLogFilePath = Path.Combine(mLogDirPath, LogFileName + mZipSuffix);
                    for (int i = 0; i < 3; i++)
                    {
                        if (ZipFile(mTempLogDirPath, mLogFilePath, out error))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("复制日志文件到临时目录出错", ex);
            }
            return false;
        }
    }

    public class UploadDebugLog : UploadLog
    {
        public UploadDebugLog()
        {
            mFolderId = "/logs/debug/pc";
        }

        /// <summary>
        /// 压缩日志文件的起始时间
        /// </summary>
        public string StartTime { get; set; }
        /// <summary>
        /// 压缩日志文件的结束时间
        /// </summary>
        public string EndTime { get; set; }

        protected override bool ZipFile()
        {
            var error = String.Empty;
            try
            {
                var fileInfoList = GetLogDirFiles();
                if (!String.IsNullOrEmpty(StartTime) && !String.IsNullOrEmpty(EndTime))
                {
                    var dtStartTime = DateTime.Parse(StartTime);
                    var dtEndTime = DateTime.Parse(EndTime);
                    fileInfoList = (from c in fileInfoList
                                    where c.LastWriteTime >= dtStartTime &&
                                          c.LastWriteTime <= dtEndTime
                                    orderby c.LastWriteTime descending
                                    select c).ToList();
                }

                fileInfoList.ForEach(fileInfo =>
                {
                    fileInfo.CopyTo(mTempLogDirPath + "\\" + fileInfo.Name, true);
                });
                mLogFilePath = Path.Combine(mLogDirPath, LogFileName + mZipSuffix);
                for (int i = 0; i < 3; i++)
                {
                    if (ZipFile(mTempLogDirPath, mLogFilePath, out error))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("复制Debug日志文件到临时目录出错", ex);
            }
            return false;
        }
    }
}
