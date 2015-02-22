using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

namespace Tools
{
    public class DownloadTask
    {
        private enum TaskStatus : int { Paused = 0, Succeeded, Failed }

        public delegate void ProgressChangedEventHandler(long already, long total);
        public event ProgressChangedEventHandler ProgressChanged;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="speed">下载速度（单位：KB/S）</param>
        public delegate void DownloadSpeedEventHandler(double speed);
        public event DownloadSpeedEventHandler DownloadSpeed;

        public event EventHandler<DownloadSucceededEventArgs> DownloadSucceeded;

        public event EventHandler DownloadPaused;

        public delegate void DownloadFailedEventHandler(string error);
        public event DownloadFailedEventHandler DownloadFailed;

        public delegate void ReallyDownloadedChangedEventHandler(long reallyDownloadedLength);
        /// <summary>
        /// 实际下载大小统计事件
        /// </summary>
        public event ReallyDownloadedChangedEventHandler ReallyDownloadedChanged;

        public event EventHandler DownloadTaskStopped;
        public event EventHandler CleanUpBreakpoint;

        public delegate void RedirectUrlHandler(string oldUrl, string newUrl, out bool isContinueDownload);


        //下载统计
        private const int INTERVAL_TIME = 500;//统计更新间隔
        private List<long> mStatisticsLengthList = new List<long>();//用于存放一小段时间内的下载量（计算平均速度）
        private long mCurReallyDownloadedLength;//统计当前的下载大小,不包括调用Start()方法以前下载的
        private ReaderWriterObjectLocker mCurReallyDownloadedLengthLock = new ReaderWriterObjectLocker();
        private long mPreReallyDownloadedLength = 0L;//不包括调用Start()方法以前下载的

        private string mSavePath = null;
        private string mMD5 = null;
        private string mURL = null;
        private string mTempFilePath = null;
        private string mFileInfoPath = null;
        private string mFilePath = null;

        private volatile bool mIsWorking = false;
        private Thread mThread = null;

        private const int MAX_DOWNLOAD_FAILED_COUNT = 3;
        private const int BYTE_BLOCK_SIZE = 50 * 1024;//每次下载的字节大小

        private int mTimeout = 30 * 1000;//单位：毫秒

        /// <summary>
        /// 下载速度控制（时间间隔），单位：毫秒。小于等于0，表示不限制
        /// </summary>
        private long mMinDownloadIntervalTime = 0;

        private bool mIsCheckLocalFile = true;//

        private string mDynamicUrl;
        private string mOldStaticUrl;
        private string mNewStaticUrl;
        private string mOldETag;
        private string mNewETag;
        private string mOldLastModified;
        private string mNewLastModified;

        private bool mIsNotDownload;

        /// <summary>
        /// 超时时间（单位：秒;默认值：30秒）
        /// </summary>
        public int Timeout
        {
            get
            {
                return mTimeout / 1000;
            }
            set
            {
                if (value > 5)
                {
                    mTimeout = value * 1000;
                }
                else
                {
                    mTimeout = 5 * 1000;
                }
            }
        }

        /// <summary>
        /// 代理设置
        /// </summary>
        public WebProxy Proxy { get; set; }

        public int FailedCount { get; set; }

        /// <summary>
        /// 是否使用断点续传(默认为true)
        /// </summary>
        public bool IsUsedBreakpoint { get; set; }

        /// <summary>
        /// 每次重试，是否减去一定断点(默认为true)
        /// </summary>
        public bool IsSafeBreakpoint { get; set; }

        /// <summary>
        /// 时间单位：毫秒
        /// </summary>
        public long ReallyDownloadedTime { get; set; }

        /// <summary>
        /// 单位：KB/s。当值小于等于0时,表示不限速
        /// </summary>
        public long MaxDownloadSpeed
        {

            set
            {
                if (value > 0)
                {
                    //最小间隔时间 = 下载大小 / 最大下载速度
                    long blockSizeByKB = BYTE_BLOCK_SIZE / 1024;
                    mMinDownloadIntervalTime = blockSizeByKB * 1000L / value;//乘于1000是为了把“单位秒”换算成“单位毫秒”
                }
                else
                {
                    mMinDownloadIntervalTime = 0;//不限制
                }

            }
        }

        public RedirectUrlHandler RedirectUrlHandle { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="md5"></param>
        /// <param name="url"></param>
        /// <param name="filePath">文件所在路径</param>
        public DownloadTask(string md5, string url, string filePath)
        {
            mMD5 = md5;
            mURL = url;

            mSavePath = Path.GetDirectoryName(filePath);
            mFilePath = filePath;
            mTempFilePath = mFilePath + ".lxtmp";
            mFileInfoPath = mFilePath + ".info";

            IsUsedBreakpoint = true;
            IsSafeBreakpoint = true;
            //1.有MD5时，要检查。2。无MD5且需要断点续传时，要检查
            mIsCheckLocalFile = !String.IsNullOrEmpty(md5) || IsUsedBreakpoint;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName">文件名（包括扩展名）</param>
        /// <param name="md5"></param>
        /// <param name="url"></param>
        /// <param name="savePath">保存目录</param>
        public DownloadTask(string fileName, string md5, string url, string savePath)
        {
            mMD5 = md5;
            mURL = url;
            mSavePath = savePath;
            mFilePath = Path.Combine(savePath, fileName);
            mTempFilePath = mFilePath + ".lxtmp";
            mFileInfoPath = mFilePath + ".info";

            IsUsedBreakpoint = true;
            IsSafeBreakpoint = true;
            //1.有MD5时，要检查。2。无MD5且需要断点续传时，要检查
            mIsCheckLocalFile = !String.IsNullOrEmpty(md5) || IsUsedBreakpoint;
        }

        /// <summary>
        /// 用于自定义断点文件所在路径
        /// </summary>
        /// <param name="md5"></param>
        /// <param name="url">下载地址</param>
        /// <param name="filePath">文件所在路径</param>
        /// <param name="tmpFilePath">临时文件所在路径</param>
        /// <param name="fileInfoPath">断点信息文件所在路径</param>
        public DownloadTask(string md5, string url, string filePath, string tmpFilePath, string fileInfoPath)
        {
            mMD5 = md5;
            mURL = url;

            mSavePath = Path.GetDirectoryName(filePath);
            mFilePath = filePath;
            mTempFilePath = tmpFilePath;
            mFileInfoPath = fileInfoPath;

            IsUsedBreakpoint = true;
            IsSafeBreakpoint = true;
            //1.有MD5时，要检查。2。无MD5且需要断点续传时，要检查
            mIsCheckLocalFile = !String.IsNullOrEmpty(md5) || IsUsedBreakpoint;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dynamicUrl"></param>
        /// <param name="staticUrl"></param>
        /// <param name="eTag"></param>
        /// <param name="url">下载地址</param>
        /// <param name="filePath">文件所在路径</param>
        /// <param name="md5"></param>
        /// <param name="tmpFilePath"></param>
        /// <param name="tmpFileInfoPath"></param>
        public DownloadTask(string dynamicUrl, string staticUrl, string eTag, string lastModified, string filePath, string md5)
        {
            mMD5 = null;
            mURL = String.IsNullOrEmpty(dynamicUrl) ? staticUrl : dynamicUrl;
            mDynamicUrl = dynamicUrl;
            mOldStaticUrl = staticUrl;
            mOldETag = eTag;
            mOldLastModified = lastModified;

            mSavePath = Path.GetDirectoryName(filePath);
            mFilePath = filePath;
            mTempFilePath = mFilePath + ".lxtmp";
            mFileInfoPath = mFilePath + ".info";

            IsUsedBreakpoint = true;
            IsSafeBreakpoint = true;
            mIsCheckLocalFile = false;//对于要拿静态地址、最后修改时间及ETag的情况时，不能因为本地文件存在就停止网络请求（1.检查本地文件可能会导致停止网络请求；2.停止请求，会拿不到静态地址、最后修改时间及ETag）
        }

        private void OnProgressChangeEvent(long already, long total)
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(already, total);
            }
        }

        public void Start()
        {
            if (mIsWorking) return;

            mIsWorking = true;
            mStatisticsLengthList.Clear();
            mPreReallyDownloadedLength = 0L;
            mCurReallyDownloadedLength = 0L;

            mThread = new Thread(doDownload);
            mThread.Start();
        }

        /// <summary>
        /// 计算下载速度
        /// </summary>
        /// <param name="downloadedLength">下载长度（单位：字节）</param>
        /// <param name="downloadedTime">下载时间（单位：毫秒）</param>
        /// <returns>返回下载速度（单位：KB/S）</returns>
        private double calcDownloadedSpeed(long downloadedLength, int downloadedTime)
        {
            double speed = 0.0;
            double time = (double)downloadedTime / 1000.0;//单位:秒
            double length = (double)(downloadedLength * 100L / 1024L) / 100.0;//单位：KB
            speed = length / time;
            //速度保留两位小数
            speed = (double)((long)(speed * 100.0)) / 100.0;
            return speed;
        }

        public void Stop()
        {
            mIsWorking = false;
        }

        public void Join()
        {
            if (mThread != null)
            {
                mThread.Join();
                mThread = null;
            }
        }

        private void doDownload()
        {
            mIsNotDownload = true;
            long curPos = 0L;//当前下载位置
            DownloadError createDirectoryError = null;
            //如果目录不存在，则创建
            tryCreateDirectory(mSavePath, out createDirectoryError);
            if (createDirectoryError != null)
            {
                OnDownloadFailed(createDirectoryError.SketchyError);
                //触发任务停止事件
                OnDownloadTaskStopped(new EventArgs());
                mIsWorking = false;
                return;
            }

            if (mIsCheckLocalFile)
            {
                //判断本地文件是否存在
                if (File.Exists(mFilePath))
                {
                    //以下情况可以判断为下载成功：
                    //1.MD5为空,则忽略MD5检查，认为下载成功
                    //2.要检查MD5时，只有检查符合才能判定为下载成功
                    if (String.IsNullOrEmpty(mMD5)
                        || checkMd5OfFile(mFilePath, mMD5))
                    {
                        long size = 0L;
                        try
                        {
                            FileInfo fileInfo = new FileInfo(mFilePath);
                            size = fileInfo.Length;
                        }
                        catch (Exception)//读取已下载文件的信息出错，但也算下载成功
                        {
                            size = 1L;
                        }
                        OnProgressChangeEvent(size, size);
                        OnDownloadSucceeded(new DownloadSucceededEventArgs(mDynamicUrl,
                            String.IsNullOrEmpty(mNewStaticUrl) ? mOldStaticUrl : mNewStaticUrl,
                            String.IsNullOrEmpty(mNewETag) ? mOldETag : mNewETag,
                            String.IsNullOrEmpty(mNewLastModified) ? mOldLastModified : mNewLastModified,
                            mIsNotDownload));
                        //触发任务停止事件
                        OnDownloadTaskStopped(new EventArgs());
                        mIsWorking = false;
                        return;
                    }
                    else//文件不符合，要删除
                    {
                        DownloadError deleteFileError = null;
                        tryDeleteFile(mFilePath, out deleteFileError);
                        if (deleteFileError != null)
                        {
                            OnDownloadFailed(deleteFileError.SketchyError);
                            //触发任务停止事件
                            OnDownloadTaskStopped(new EventArgs());
                            mIsWorking = false;
                            return;
                        }
                    }
                }
            }

            //判断是否要断点续传
            if (IsUsedBreakpoint)
            {
                string error = null;
                long curPosLength = 0L;
                tryReadBreakpointInfo(out curPosLength, out error);
                if (error == null)
                {
                    curPos = curPosLength;
                    if (IsSafeBreakpoint)
                    {
                        //减去一定长度的断点，防止下载完成时出现MD5不一致
                        curPos -= 3L * 1024L;
                    }
                    if (curPos < 0L)
                    {
                        curPos = 0L;
                    }
                }
                else
                {
                    OnCleanUpBreakpoint(new EventArgs());//触发清理断点事件
                    DownloadError deleteFileError = null;
                    tryDeleteFile(mFileInfoPath, out deleteFileError);
                    if (deleteFileError == null)//前一个删除成功才往下执行
                    {
                        tryDeleteFile(mTempFilePath, out deleteFileError);
                    }
                    if (deleteFileError != null)
                    {
                        OnDownloadFailed(deleteFileError.SketchyError);
                        //触发任务停止事件
                        OnDownloadTaskStopped(new EventArgs());
                        mIsWorking = false;
                        return;
                    }
                }
            }

            //请求下载
            DownloadError curError = null;
            requestDonwload(curPos, out curError);
            //对外更新下载速度
            OnDownloadSpeed(0.0);

            //判断任务状态(mIsWorking为false时，说明是用户主动调用停止的，即为暂停任务)
            if (mIsWorking)
            {
                //非主动停止下载任务,并且下载过程中没出错时才校验文件
                if (curError == null)
                {
                    //校验所下载的文件
                    checkDownloadFile(mFilePath, mMD5, out curError);
                }
                if (curError == null)
                {
                    OnDownloadSucceeded(new DownloadSucceededEventArgs(mDynamicUrl,
                        String.IsNullOrEmpty(mNewStaticUrl) ? mOldStaticUrl : mNewStaticUrl,
                        String.IsNullOrEmpty(mNewETag) ? mOldETag : mNewETag,
                        String.IsNullOrEmpty(mNewLastModified) ? mOldLastModified : mNewLastModified,
                        mIsNotDownload));
                }
                else
                {
                    OnDownloadFailed(curError != null ? curError.SketchyError : DownloadErrorInfo.Unknown.SketchyError);
                }
            }
            else
            {
                OnDownloadPaused(new EventArgs());
            }
            //触发任务停止事件
            OnDownloadTaskStopped(new EventArgs());
            mIsWorking = false;
            mThread = null;
        }

        private void tryReadBreakpointInfo(out long curPosLength, out string error)
        {
            error = null;
            curPosLength = 0L;
            if (File.Exists(mFileInfoPath) && File.Exists(mTempFilePath))
            {
                long overallLength = 0L;
                string md5 = null;

                string overallLengthInfo = string.Empty;////文件总长度
                string curPosInfo = string.Empty;//已经存在的断点位置

                //获取断点续传信息
                try
                {
                    using (StreamReader streamReader = new StreamReader(File.OpenRead(mFileInfoPath)))
                    {
                        overallLengthInfo = streamReader.ReadLine().Trim();
                        curPosInfo = streamReader.ReadLine().Trim();
                        md5 = streamReader.ReadLine().Trim();
                    }
                    overallLength = Convert.ToInt64(overallLengthInfo);//取得文件总长度
                    curPosLength = Convert.ToInt64(curPosInfo);//取得断点位置
                    if (overallLength <= curPosLength)
                    {
                        error = "断点续传信息文件不正常";
                    }
                    else if (!String.IsNullOrEmpty(mMD5) && !String.IsNullOrEmpty(md5)
                        && !mMD5.Equals(md5, StringComparison.OrdinalIgnoreCase)
                        )//md5不为空时，判断mMD5与md5是否相等。不相等时，该断点不符合
                    {
                        error = "断点续传信息不符合当前下载的文件";
                    }
                }
                catch (Exception ex)
                {
                    error = "读断点续传信息文件出错,Exception为" + ex.ToString();
                }
            }
            else
            {
                error = "断点续传信息文件不存在";
            }
        }

        private void requestDonwload(long curPos, out DownloadError error)
        {
            error = null;
            //开启下载速度计时器
            using (System.Timers.Timer downloadSpeedTimer = new System.Timers.Timer())
            {
                downloadSpeedTimer.Elapsed += downloadSpeedTimer_Elapsed;
                downloadSpeedTimer.Interval = INTERVAL_TIME;
                downloadSpeedTimer.Start();

                startRequestDonwload(mURL, curPos, out error);

                //关闭计时器
                downloadSpeedTimer.Stop();
                downloadSpeedTimer.Elapsed -= downloadSpeedTimer_Elapsed;
            }
        }

        private void startRequestDonwload(string url, long curPos, out DownloadError error)
        {
            error = null;
            int downloadFailedCount = 0;
            long overallLength = 0L;
            while (downloadFailedCount < MAX_DOWNLOAD_FAILED_COUNT && mIsWorking)//如果下载失败次数在规定次数内，则外循环来重新下载
            {
                //解决MD5不一致时，断点BUG及不断重新下载的问题
                if (error != null && error == DownloadErrorInfo.InconsistentMD5)
                {
                    break;//断点不一致，则不再继续下载
                }

                error = null;//重新下载时，重置状态

                //开始创建下载请求
                HttpWebRequest request = null;

                try
                {
                    string dUrl = url;
                    request = (HttpWebRequest)HttpWebRequest.Create(dUrl);
                    request.AddRange((int)curPos);

                    //if (File.Exists(mFilePath))//本地文件存在时，才能判断该缓存是不是最新的
                    //{

                    //ETag
                    if (!String.IsNullOrEmpty(mOldETag))
                    {
                        request.Headers[HttpRequestHeader.IfNoneMatch] = mOldETag;
                    }
                    if (!String.IsNullOrEmpty(mOldLastModified))
                    {
                        request.IfModifiedSince = DateTime.Parse(mOldLastModified);
                    }
                    //}

                    if (Proxy != null)
                    {
                        request.Proxy = Proxy;//设置代理
                    }
                    HttpWebResponse res = request.GetResponse() as HttpWebResponse;

                    RedirectUrlHandler handle = RedirectUrlHandle;
                    if (handle != null)
                    {
                        string oldUrl = mURL;
                        string newUrl = res.ResponseUri.ToString();
                        bool isContinueDownload = true;
                        handle(oldUrl, newUrl, out isContinueDownload);
                        if (!isContinueDownload)//停止下载
                        {
                            error = DownloadErrorInfo.RedirectUrlError;
                            return;
                        }
                    }

                    mNewETag = res.Headers[HttpResponseHeader.ETag];
                    mNewLastModified = res.Headers[HttpResponseHeader.LastModified];
                    mNewStaticUrl = res.ResponseUri.ToString();
                    mIsNotDownload = false;
                    using (Stream stream = res.GetResponseStream())
                    {
                        stream.ReadTimeout = mTimeout;
                        //计算
                        long length = res.ContentLength;
                        overallLength = length + curPos;//计算总长度
                        // Logger.Logger.GetLogger(this).Info("此处计算文件实际总长度。 文件实际总长度 = 当前下载长度(" + curPos + ") + 服务器返回的数据长度(" + length + ") = " + overallLength.ToString());
                        try
                        {
                            using (FileStream fs = File.Open(mTempFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                            {
                                byte[] nByte = new byte[BYTE_BLOCK_SIZE];
                                int exceptionCount = 0;
                                int readErrorCount = 0;
                                int savedCount = 0;//保存进度计数
                                Stopwatch stopwatch = Stopwatch.StartNew();
                                while (mIsWorking)
                                {
                                    try
                                    {
                                        if (stream.CanRead)
                                        {
                                            long preElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                                            int receivedLength = stream.Read(nByte, 0, nByte.Length);
                                            //
                                            fs.Position = curPos;
                                            fs.Write(nByte, 0, receivedLength);
                                            curPos += receivedLength;
                                            readErrorCount = 0;
                                            using (mCurReallyDownloadedLengthLock.WriteLock())
                                            {
                                                mCurReallyDownloadedLength += receivedLength;
                                            }
                                            //触发下载统计事件
                                            OnReallyDownloadedChanged(mCurReallyDownloadedLength);
                                            //检查是否下载完成
                                            if (curPos < overallLength)//判断当前文件长度是否达总长度
                                            {
                                                OnProgressChangeEvent(curPos, overallLength);
                                                savedCount++;
                                                //达到一定时间内或者被停止下载时，就保存进度
                                                if (savedCount > 10 || !mIsWorking)
                                                {
                                                    saveDownLoadInfo(mFileInfoPath, overallLength, curPos, mMD5);
                                                    savedCount = 0;
                                                }
                                            }
                                            else
                                            {
                                                error = null;
                                                //更新进度
                                                OnProgressChangeEvent(overallLength, overallLength);
                                                break;//下载完成，退出
                                            }

                                            //限速控制
                                            long curIntervalTime = stopwatch.ElapsedMilliseconds - preElapsedMilliseconds;
                                            long sleepTime = mMinDownloadIntervalTime - curIntervalTime;//如果当时所用时间比限制的最小时间还小，说明要暂停一下下载
                                            if (sleepTime > 0L)
                                            {
                                                Thread.Sleep((int)sleepTime);
                                            }
                                        }
                                        else
                                        {
                                            Thread.Sleep(100);
                                            readErrorCount++;
                                            if (readErrorCount > 25)
                                            {
                                                Logger.Logger.GetLogger("DownloadTask").Error("没可读取的数据,导致下载失败");
                                                error = DownloadErrorInfo.NetworkError;
                                                break;//退出while (mIsWorking)
                                            }
                                        }//End if (stream.CanRead)
                                        //重置异常计数
                                        exceptionCount = 0;
                                    }
                                    catch (Exception ex)
                                    {
                                        Thread.Sleep(100);
                                        exceptionCount++;
                                        if (exceptionCount > 25)
                                        {
                                            Logger.Logger.GetLogger("DownloadTask").Error("多次出现下载异常", ex);
                                            error = DownloadErrorInfo.Unknown;
                                            break;//退出while (mIsWorking)
                                        }
                                    }
                                }//End while (mIsWorking) 
                                //计算下载时间
                                stopwatch.Stop();
                                ReallyDownloadedTime = stopwatch.ElapsedMilliseconds;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("磁盘空间不足"))
                            {
                                error = DownloadErrorInfo.InsufficientSpace;
                                Logger.Logger.GetLogger("DownloadTask").Error("磁盘空间不足", ex);
                            }
                            else
                            {
                                Logger.Logger.GetLogger("DownloadTask").Error("未知错误", ex);
                                error = DownloadErrorInfo.Unknown;
                            }
                        }
                    }//  using (Stream stream = res.GetResponseStream())
                    res.Close();
                    res = null;
                }
                catch (WebException ex)
                {
                    HttpWebResponse res = ex.Response as HttpWebResponse;
                    if (res != null)
                    {
                        if (res.StatusCode == HttpStatusCode.NotModified)//判断是304，说明不用下载文件（本地文件已是最新的）
                        {
                            mIsNotDownload = true;
                            mNewETag = mOldETag;
                            mNewLastModified = res.Headers[HttpResponseHeader.LastModified];
                            mNewStaticUrl = res.ResponseUri.ToString();
                            error = null;
                            return;//304状态，就直接返回
                        }
                    }
                    Logger.Logger.GetLogger("DownloadTask").Error("下载时,网络通信错误", ex);
                    error = DownloadErrorInfo.NetworkError;
                }
                catch (Exception ex)
                {
                    Logger.Logger.GetLogger("DownloadTask").Error("下载出错", ex);
                    error = DownloadErrorInfo.Unknown;
                }
                finally
                {
                    if (request != null)
                    {
                        request.Abort();
                        request = null;
                    }
                }

                if (error == null && overallLength <= curPos)//说明下载完成了
                {
                    tryMoveFile(mTempFilePath, mFilePath, out error);
                    //删除断点信息文件。删除断点信息文件失败不算下载失败
                    DownloadError deleteFileError = null;
                    tryDeleteFile(mFileInfoPath, out deleteFileError);
                    if (deleteFileError != null)
                    {
                        //保存完成的进度（用于解决删除不了断点文件时带来的问题）
                        saveDownLoadInfo(mFileInfoPath, overallLength, overallLength, mMD5);
                    }
                    if (error == null)
                    {
                        break;//下载成功就直接退出
                    }
                }

                //下载失败，计数
                downloadFailedCount++;
                if (downloadFailedCount == 3)
                {
                    Logger.Logger.GetLogger("DownloadTask").Error("下载URL=" + mURL + "。下载失败的原因：" + error.DetailedError + "。");
                }
            }
        }

        private void checkDownloadFile(string filePath, string md5, out DownloadError error)
        {
            error = null;

            //如果提供MD5值，则比较MD5
            if (!string.IsNullOrEmpty(md5))
            {
                if (File.Exists(filePath))
                {
                    //比较MD5
                    if (!checkMd5OfFile(mFilePath, md5))
                    {
                        error = DownloadErrorInfo.InconsistentMD5;
                        //删除下载失败的文件
                        DownloadError dError = null;
                        tryDeleteFile(mFileInfoPath, out dError);
                    }
                }
                else
                {
                    error = DownloadErrorInfo.FileNotExist;
                }
            }
        }

        private void downloadSpeedTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            double speed = 0.0;
            if (mIsWorking)
            {
                long curAllDownloadedLengthByByte = 0L;//当前已经累计下载大小
                long curDownloadedLengthByByte = 0L;

                //获取当前已经累计下载大小（调用Start方法后实际下载的大小）
                using (mCurReallyDownloadedLengthLock.ReadLock())
                {
                    curAllDownloadedLengthByByte = mCurReallyDownloadedLength;
                }
                //计算当前一段时间内下载大小
                curDownloadedLengthByByte = curAllDownloadedLengthByByte - mPreReallyDownloadedLength;
                if (curDownloadedLengthByByte < 0)
                {
                    curDownloadedLengthByByte = 0L;
                }
                //把当前一段时间内下载量添加到列表里,并计算下载速度
                mStatisticsLengthList.Add(curDownloadedLengthByByte);
                //数量超过5时，移除最早添加的下载量
                if (mStatisticsLengthList.Count > 5)
                {
                    mStatisticsLengthList.RemoveAt(0);
                }
                long tempLength = 0L;//临时存放5秒内，所下载的量
                int tempTime = mStatisticsLengthList.Count * INTERVAL_TIME;//计算列表中的下载量所耗的时间
                foreach (long length in mStatisticsLengthList)
                {
                    tempLength += length;
                }
                speed = calcDownloadedSpeed(tempLength, tempTime);
                //更新上一次下载量，用于下次统计
                mPreReallyDownloadedLength = curAllDownloadedLengthByByte;
            }
            OnDownloadSpeed(speed);
        }

        private bool checkMd5OfFile(string filePath, string md5)
        {
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(md5)) { return false; }
            bool isSuccess = false;

            //比较获取本地文件MD5
            int errorCount = 0;
            string localMd5 = string.Empty;
            while (true)
            {
                localMd5 = Tools.MD5Helper.GetMd5OfFile(filePath);
                if (string.IsNullOrEmpty(localMd5))
                {
                    errorCount++;
                    if (errorCount > 5)
                    {
                        Logger.Logger.GetLogger(this).Error("文件MD5读取为空(可能文件被占用导致读不出MD5)。" + "  filePath为" + filePath + "  目标MD5为" + md5);
                        break;
                    }
                    Thread.Sleep(100);
                }
                else
                {
                    break;
                }
            }
            //比较MD5判断是否成功
            if (!string.IsNullOrEmpty(localMd5) &&
                md5.Equals(localMd5, StringComparison.OrdinalIgnoreCase))
            {
                isSuccess = true;
            }
            else
            {
                isSuccess = false;
                Logger.Logger.GetLogger(this).Error("文件MD5不一致(实际MD5：" + localMd5 + ",所提供的MD5：" + md5 + ")。文件所在路径(" + filePath + ")");
            }
            return isSuccess;
        }

        private void tryCreateDirectory(string directoryPath, out DownloadError error)
        {
            int failedCount = 0;
            error = null;
            Exception exception = null;
            while (!Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                    error = null;
                    exception = null;
                    break;
                }
                catch (PathTooLongException ex)
                {
                    exception = ex;
                    error = DownloadErrorInfo.CreatedDirectoryFailed_PathInvalid;
                }
                catch (DirectoryNotFoundException ex)
                {
                    exception = ex;
                    error = DownloadErrorInfo.CreatedDirectoryFailed_PathInvalid;
                }
                catch (IOException ex)
                {
                    exception = ex;
                    error = DownloadErrorInfo.CreatedDirectoryFailed_Security;
                }
                catch (UnauthorizedAccessException ex)
                {
                    exception = ex;
                    error = DownloadErrorInfo.CreatedDirectoryFailed_Security;
                }
                catch (NotSupportedException ex)
                {
                    exception = ex;
                    error = DownloadErrorInfo.CreatedDirectoryFailed_PathInvalid;
                }
                catch (Exception ex)
                {
                    exception = ex;
                    error = DownloadErrorInfo.CreatedDirectoryFailed_PathInvalid;
                }
                if (exception != null)
                {
                    failedCount++;
                    if (failedCount > 5)
                    {
                        Logger.Logger.GetLogger(this).Error("创建目录失败：" + directoryPath, exception);
                        break;
                    }
                    Thread.Sleep(100);
                }
            }
        }

        private void tryDeleteFile(string filePath, out DownloadError error)
        {
            int failedCount = 0;
            error = null;
            Exception exception = null;
            while (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    error = null;
                    exception = null;
                    break;
                }
                catch (PathTooLongException ex)
                {
                    exception = ex;
                    error = DownloadErrorInfo.DeletedFileFailed_PathInvalid;
                }
                catch (DirectoryNotFoundException ex)
                {
                    exception = ex;
                    error = DownloadErrorInfo.DeletedFileFailed_PathInvalid;
                }
                catch (IOException ex)
                {
                    exception = ex;
                    error = DownloadErrorInfo.DeletedFileFailed_Occupied;
                }
                catch (NotSupportedException ex)
                {
                    exception = ex;
                    error = DownloadErrorInfo.DeletedFileFailed_PathInvalid;
                }
                catch (UnauthorizedAccessException ex)
                {
                    exception = ex;
                    error = DownloadErrorInfo.DeletedFileFailed_Security;
                }
                catch (Exception ex)
                {
                    exception = ex;
                    error = DownloadErrorInfo.DeletedFileFailed_Other;
                }
                if (exception != null)
                {
                    failedCount++;
                    if (failedCount > 5)
                    {
                        Logger.Logger.GetLogger(this).Error("删除文件失败，路径为：" + filePath, exception);
                        break;
                    }
                    Thread.Sleep(100);
                }
            }
        }

        private void tryMoveFile(string oldFilePath, string newFilePath, out DownloadError error)
        {
            int failedCount = 0;
            error = null;
            while (File.Exists(oldFilePath))
            {
                try
                {
                    tryDeleteFile(newFilePath, out error);
                    File.Move(oldFilePath, newFilePath);
                    error = null;
                    break;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    if (failedCount > 4)
                    {
                        error = DownloadErrorInfo.MovedFileError;
                        Logger.Logger.GetLogger(this).Error("移动文件出错。源文件路径为：" + oldFilePath + "。目标路径：" + newFilePath, ex);
                        break;
                    }
                    Thread.Sleep(100);
                }
            }
        }

        private bool saveDownLoadInfo(string fileInfoPath, long overallLength, long curPos, string md5)
        {
            if (string.IsNullOrEmpty(fileInfoPath) ||
                overallLength < 0L || curPos < 0L)
            {
                Logger.Logger.GetLogger(this).Error("保存下载信息时出错：传入参数不正确");
                return false;
            }
            bool isSuccess = false;
            int failedCount = 0;
            while (true)
            {
                try
                {
                    if (File.Exists(fileInfoPath))
                    {
                        File.Delete(fileInfoPath);
                    }
                    using (StreamWriter streamWriter = File.CreateText(fileInfoPath))
                    {
                        streamWriter.WriteLine(overallLength.ToString());
                        streamWriter.WriteLine(curPos.ToString());
                        streamWriter.WriteLine(md5);
                    }
                    isSuccess = true;
                    break;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    if (failedCount > 4)
                    {
                        isSuccess = false;
                        Logger.Logger.GetLogger(this).Error("保存下载进度失败", ex);
                        break;
                    }
                }
            }
            return isSuccess;
        }

        protected void OnDownloadSucceeded(DownloadSucceededEventArgs e)
        {
            if (DownloadSucceeded != null)
            {
                DownloadSucceeded(this, e);
            }
        }

        protected void OnDownloadPaused(EventArgs e)
        {
            if (DownloadPaused != null)
            {
                DownloadPaused(this, e);
            }
        }

        protected void OnDownloadFailed(string error)
        {
            string errorMsg = error;
            if (DownloadFailed != null)
            {
                DownloadFailed(errorMsg);
            }
        }

        protected void OnDownloadSpeed(double speed)
        {
            if (DownloadSpeed != null)
            {
                DownloadSpeed(speed);
            }
        }

        protected void OnReallyDownloadedChanged(long reallyDownloadedLength)
        {
            if (ReallyDownloadedChanged != null)
            {
                ReallyDownloadedChanged(reallyDownloadedLength);
            }
        }

        protected void OnDownloadTaskStopped(EventArgs e)
        {
            if (DownloadTaskStopped != null)
            {
                DownloadTaskStopped(this, e);
            }
        }

        protected void OnCleanUpBreakpoint(EventArgs e)
        {
            if (CleanUpBreakpoint != null)
            {
                CleanUpBreakpoint(this, e);
            }
        }
    }


    public class DownloadSucceededEventArgs : EventArgs
    {
        public string DynamicUrl { get; private set; }
        public string StaticUrl { get; private set; }
        public string ETag { get; private set; }
        public string LastModified { get; private set; }
        public bool IsNotDownload { get; private set; }

        public DownloadSucceededEventArgs(string dynamicUrl, string staticUrl, string eTag, string lastModified, bool isNotDownload)
        {
            DynamicUrl = dynamicUrl;
            StaticUrl = staticUrl;
            ETag = eTag;
            LastModified = lastModified;
            IsNotDownload = isNotDownload;
        }
    }

    public static class DownloadErrorInfo
    {
        public static readonly DownloadError CreatedDirectoryFailed_PathInvalid = new DownloadError("文件出错(0xURL01)", "创建目录失败（服务器路径有问题）");
        public static readonly DownloadError CreatedDirectoryFailed_Security = new DownloadError("文件出错(0xSYSP01)", "创建目录失败（权限问题)");
        public static readonly DownloadError CreatedDirectoryFailed_Other = new DownloadError("文件出错", "创建目录失败");
        public static readonly DownloadError DeletedFileFailed_Occupied = new DownloadError("文件出错(0xOCCP01)", "删除文件失败(文件被占用)");
        public static readonly DownloadError DeletedFileFailed_Security = new DownloadError("文件出错(0xSYSP02)", "删除文件失败(权限问题)");
        public static readonly DownloadError DeletedFileFailed_PathInvalid = new DownloadError("文件出错(0xURL02)", "删除文件失败(路径有问题)");
        public static readonly DownloadError DeletedFileFailed_Other = new DownloadError("文件出错", "删除文件失败");
        public static readonly DownloadError FileNotExist = new DownloadError("文件出错", "文件不存在");
        public static readonly DownloadError InsufficientSpace = new DownloadError("磁盘空间不足", "磁盘空间不足");
        public static readonly DownloadError InconsistentMD5 = new DownloadError("文件出错(0xMD501)", "MD5不一致");
        public static readonly DownloadError MovedFileError = new DownloadError("文件出错(0xOCCP02", "移动文件出错误(文件被占用)");
        public static readonly DownloadError NetworkError = new DownloadError("网络异常", "网络异常");
        public static readonly DownloadError RedirectUrlError = new DownloadError("网络错误", "中断重定向");
        public static readonly DownloadError Unknown = new DownloadError("未知错误", "未知错误");
    }

    public class DownloadError
    {
        /// <summary>
        /// 粗略的错误信息
        /// </summary>
        public string SketchyError { get; private set; }

        /// <summary>
        /// 详细的错误信息
        /// </summary>
        public string DetailedError { get; private set; }

        public DownloadError(string sketchyError, string detailedError)
        {
            SketchyError = sketchyError;
            DetailedError = detailedError;
        }
    }
}
