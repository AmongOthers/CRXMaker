using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.IO;
using Microsoft.Win32;
using Tools;
using System.Net.Sockets;

namespace HttpDownloader
{
    public class AddTask 
    {
        private string mFileName;
        private string mFilePath;
        private string mMd5;

        private string mTempPath;
        private string mUrl;
        private int mLastRange;

        private bool mIsStart;
        private bool mIsNewLoad = false;
        private Thread mDownloadThread = null;
        private Thread mFileThread = null;
        private Status mStatus;
        public bool IsFailed { get; set; }

        public event EventHandler ProgressChanged;

        public class ProgressEventArgs : EventArgs
        {
            public int Value { get; set; }
            public int MaxValue { get; set; }
        }

        protected void progress_changed(int value, int max)
        {
            ProgressEventArgs e = new ProgressEventArgs();
            e.Value = value;
            e.MaxValue = max;
            if (ProgressChanged != null)
            {
                ProgressChanged(this, e);
            }
        }

        public enum Status
        {
            INIT,
            DOWNLOADING,
            DOWNLOADSUCCEED,
            DOWNLOADFAILED,
            COPYING,
            DONE
        }

        public AddTask(string name,string path, string md5, string url)
        {
            mFileName = name;
            mFilePath = path;
            mMd5 = md5;
            mIsStart = false;
            IsFailed = false;
            mStatus = Status.INIT;
            if (!Directory.Exists(mFilePath))
            {
                Directory.CreateDirectory(mFilePath);
            }
            if (mFilePath.EndsWith("\\"))
            {
                mTempPath = mFilePath + mFileName + ".lxtmp";
                mFilePath = mFilePath + mFileName;
            }
            else
            {
                mTempPath = mFilePath + "\\" + mFileName + ".lxtmp";
                mFilePath = mFilePath + "\\" + mFileName;
            }
            mLastRange = 0;
            //mUrl = "http://192.168.0.109:8080/xfolder/x/app/downloadfile/api?xfolderId=&fileName=DEEPIN_GHOSTXP_SP3_2011_06_CD.iso";
            mUrl = url;
        }

        public AddTask()
        {
            mIsStart = false;
            IsFailed = false;
        }

        private void download_svc()
        {
            mIsStart = true;
            HttpWebRequest request = null;
            try
            {
                request = (HttpWebRequest)HttpWebRequest.Create(mUrl);
            }
            catch (UriFormatException)
            {
                Logger.Logger.GetLogger(this).ErrorFormat("url format exception: {0}, {1}", mFileName, mUrl);
                IsFailed = true;
                mStatus = Status.DOWNLOADFAILED;
                return;
            }
            request.AddRange((int)mLastRange);
            //Stream stream = null;
            NetworkStream mystream = null;
            Stream stream = (Stream)mystream;
            try
            {
                stream = request.GetResponse().GetResponseStream();
            }
            catch (WebException e)
            {
                Logger.Logger.GetLogger(this).ErrorFormat("downloadSvc getRessponseStream exception: {0}, {1}", mFileName, e.Message);
                IsFailed = true;
                mStatus = Status.DOWNLOADFAILED;
                return;
            }
            if (File.Exists(mTempPath) && mLastRange == 0)
            {
                bool IsDeleted = false;
                while (!IsDeleted)
                {
                    try
                    {
                        File.Delete(mTempPath);
                        IsDeleted = true;
                    }
                    catch (System.Exception)
                    {
                        Thread.Sleep(200);
                    }
                }
            }
            FileStream fs = null;
            while (true)
            {
                try
                {
                    fs = File.Open(mTempPath, FileMode.OpenOrCreate);
                }
                catch (System.Exception)
                {
                    fs = null;
                }
                if (fs != null)
                {
                    break;
                }
                Thread.Sleep(100);
            }
            WebResponse rsp = request.GetResponse();
            int length = int.Parse(rsp.Headers["Content-Length"]);
            length += (int)mLastRange;
            using (fs)
            {
                using (stream)
                {
                    int byte_in_second = 0;
                    DateTime last_time = DateTime.Now;
                    int failed_count = 0;
                    byte[] b2 = new byte[1024];
                    while (mIsStart)
                    {
                        byte[] b = new byte[1024];
                        int r = 0;
                        try
                        {
                            r = stream.Read(b, 0, b.Length);
                        }
                        catch (Exception e)
                        {
                            if (failed_count < 50)
                            {
                                Thread.Sleep(100);
                                failed_count++;
                                continue;
                            }
                            else
                            {
                                Logger.Logger.GetLogger(this).ErrorFormat("downloadSvc read exception: {0}, {1}", mFileName, e.Message);
                                IsFailed = true;
                                mStatus = Status.DOWNLOADFAILED;
                                mIsStart = false;
                                return;
                            }
                        }
                        failed_count = 0;
                        if (r == 0)
                        {
                            break;
                        }
                        fs.Position = mLastRange;
                        fs.Write(b, 0, r);
                        byte_in_second += r;
                        mLastRange += r;
                        progress_changed((int)mLastRange, length);
                    }
                }
            }
            string md5 = MD5Helper.GetMd5OfFile(mTempPath).ToUpper();
            if (!mMd5.ToUpper().Equals(md5))
            {
                if (mIsStart)
                {

					Logger.Logger.GetLogger(this).ErrorFormat("mIsStart == true, downloadSvc md5 not match: {0}, serverMd5: {1}, hereMd5: {2}", mFileName, mMd5, md5);
                    IsFailed = true;
                    mStatus = Status.DOWNLOADFAILED;
                    mLastRange = 0;
                    if (File.Exists(mTempPath))
                    {
                        bool IsDeleted = false;
                        while (!IsDeleted)
                        {
                            try
                            {
                                File.Delete(mTempPath);
                                IsDeleted = true;
                            }
                            catch (System.Exception)
                            {
                                Thread.Sleep(200);
                            }
                        }
                    }
                }
                else
                {
					Logger.Logger.GetLogger(this).ErrorFormat("mIsStart == false, downloadSvc md5 not match: {0}", mFileName);
                    IsFailed = true;
                    mStatus = Status.DOWNLOADFAILED;
                }
            }
            else
            {
                IsFailed = false;
                mStatus = Status.DOWNLOADSUCCEED;
            }
            mIsStart = false;
        }

        private void file_svc()
        {
            mIsStart = true;
            while (File.Exists(mFilePath) && mIsStart)
            {
                try
                {
                    File.Delete(mFilePath);
                }
                catch (System.Exception)
                {

                }
                Thread.Sleep(200);
            }
            while (!File.Exists(mFilePath) && File.Exists(mTempPath) && mIsStart)
            {
                try
                {
                    File.Move(mTempPath, mFilePath);
                }
                catch (System.Exception)
                {

                }
                Thread.Sleep(200);
            }
            while (File.Exists(mTempPath) && mIsStart)
            {
                try
                {
                    File.Delete(mTempPath);
                }
                catch (System.Exception)
                {

                }
                Thread.Sleep(200);
            }
            mIsStart = false;
            mStatus = Status.DONE;
        }

        public bool wait()
        {
            switch (mStatus)
            {
                case Status.INIT:
                    {
                        if (mDownloadThread != null)
                        {
                            mIsStart = false;
                            if (mDownloadThread.IsAlive)
                            {
                                mDownloadThread.Join();
                            }
                            mDownloadThread = null;
                        }
                        mDownloadThread = new Thread(new ThreadStart(download_svc));
                        mStatus = Status.DOWNLOADING;
                        mDownloadThread.Start();
                        return false;
                    }
                case Status.DOWNLOADING:
                    {
                        if (mIsNewLoad)
                        {
                            if (mDownloadThread != null)
                            {
                                mIsStart = false;
                                if (mDownloadThread.IsAlive)
                                {
                                    mDownloadThread.Join();
                                }
                                mDownloadThread = null;
                            }
                            mDownloadThread = new Thread(new ThreadStart(download_svc));
                            mDownloadThread.Start();
                            mIsNewLoad = false;
                        }
                        return false;
                    }
                case Status.DOWNLOADSUCCEED:
                    {
                        mStatus = Status.COPYING;
                        if (mDownloadThread != null && mDownloadThread.IsAlive)
                        {
                            mDownloadThread.Join();
                        }
                        mDownloadThread = null;
                        if (mFileThread != null)
                        {
                            mIsStart = false;
                            if (mFileThread.IsAlive)
                            {
                                mFileThread.Join();
                            }
                            mFileThread = null;
                        }
                        mFileThread = new Thread(new ThreadStart(file_svc));
                        mFileThread.Start();
                        return false;
                    }
                case Status.DOWNLOADFAILED:
                    {
                        //mDownloadThread = new Thread(new ThreadStart(download_svc));
                        //mStatus = Status.DOWNLOADING;
                        //mDownloadThread.Start();
                        //return false;
                        mStatus = Status.INIT;
                        return true;
                    }
                case Status.COPYING:
                    {
                        if (mIsNewLoad)
                        {
                            if (mFileThread != null)
                            {
                                mIsStart = false;
                                if (mFileThread.IsAlive)
                                {
                                    mFileThread.Join();
                                }
                                mFileThread = null;
                            }
                            mFileThread = new Thread(new ThreadStart(file_svc));
                            mFileThread.Start();
                            mIsNewLoad = false;
                        }
                        return false;
                    }
                case Status.DONE:
                    {
                        if (mFileThread != null && mFileThread.IsAlive)
                        {
                            mFileThread.Join();
                        }
                        mFileThread = null;
                        return true;
                    }
                default:
                    return false;
            }
        }

        public void abandon()
        {
            if (mDownloadThread != null)
            {
                mIsStart = false;
            }
        }
    }
}
