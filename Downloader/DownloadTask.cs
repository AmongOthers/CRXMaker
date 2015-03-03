using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;

namespace HttpDownloader
{
    class DownloadTask
    {
        private Thread mThread;
        private String mUrl;
        private String mPath;
        private String mFileName;
        public String FullPath;
        public bool IsCache {get; set;}
        Downloader.onFileDownloadComplete mCallBack = null;

        public DownloadTask(String url, String path, String filename, Downloader.onFileDownloadComplete callback)
        {
            mUrl = url;
            mPath = path;
            mFileName = filename;
            mCallBack = callback;
            FullPath = mPath + "\\" + mFileName;
        }

        public void start()
        {
            mThread = new Thread(new ThreadStart(this.threadFunc));
            mThread.Start();
        }

        public void shutdown()
        {
            if (mThread != null && mThread.IsAlive)
            {
                mThread.Abort();
            }
        }

        public void join()
        {
            mThread.Join();
        }

        public bool download()
        {
            bool result = true;
            WebClient web_client = new WebClient();
            try
            {
                web_client.DownloadFile(mUrl, FullPath);
            }
            catch (Exception)
            {
                result = false;
            }
            if (result)
            {
                Downloader.getInstance().downloadSuccessful(mUrl, mPath, mFileName, mCallBack, IsCache);
            }
            else
            {
                Downloader.getInstance().downloadFailed(mUrl, mPath, mFileName, mCallBack, IsCache);
            }
            return result;
        }

        private void threadFunc()
        {
            download();
        }
    }
}
