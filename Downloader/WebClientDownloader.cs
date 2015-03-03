using System;
using System.Net;
using System.Threading;
using System.ComponentModel;

namespace HttpDownloader
{
    public interface OnDownloadListener
    {
        void onDownloadPrograss(long finished, long total);
        void onDownloadFinish(bool isSucess);
    }

    public class WebClientDownloader
    {
        private WebClient mWebClient;
        private OnDownloadListener mListener;
        private byte[] mLock = new byte[0];
        private bool mIsSucceed;
        private bool mIsAsyc;

        public WebClientDownloader(OnDownloadListener listener)
        {
            mListener = listener;
            mWebClient = new WebClient();
            mWebClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(onDownloadProgress);
            mWebClient.DownloadFileCompleted += new AsyncCompletedEventHandler(onDownloadCompleted);
            mIsAsyc = false;
        }

        public void run()
        {

        }

        public bool download(string url, string filePath, bool isAsyc)
        {
            mIsAsyc = isAsyc;
            Uri uri = new Uri(url);
            try
            {
                mWebClient.DownloadFileAsync(uri, filePath);
            }
            catch (Exception e)
            {
                Logger.Logger.GetLogger(this).Error("download: exception", e);
                return false;
            }

            if (!mIsAsyc)
            {
                lock (mLock)
                {
                    Monitor.Wait(mLock);
                    return mIsSucceed;
                }
            }
            else
            {
                return true;
            }
        }

        public void cancel()
        {
            mWebClient.CancelAsync();
        }

        private void onDownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            int p = e.ProgressPercentage;
            mListener.onDownloadPrograss(e.BytesReceived, e.TotalBytesToReceive);
        }

        private void onDownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                mIsSucceed = true;
                mListener.onDownloadFinish(true);
            }
            else
            {
                Logger.Logger.GetLogger(this).ErrorFormat("onDownloadCompleted: exception: {0}", e.Error.ToString());
                mIsSucceed = false;
                mListener.onDownloadFinish(false);
            }
            if (!mIsAsyc)
            {
                lock (mLock)
                {
                    Monitor.Pulse(mLock);
                }
            }
        }
    }
}
