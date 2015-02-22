using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.IO;
using System.Threading;

namespace Tools
{
    public class HttpRequestTool
    {
        public delegate void RequestResultInvoker(string webContent, string error);
        private RequestResultInvoker mRequestResultInvoker;

        private volatile bool mIsWorking;
        private object mIsWorkingLock = new object();

        private HttpWebRequest mRequest = null;
        private string mUrl;
        private Encoding mEncoding;

        private string mAcceptRequest = "text/html,application/xhtml+xml,application/xml";
        public string AcceptRequest
        {
            get
            {
                return mAcceptRequest;
            }
            set
            {
                mAcceptRequest = value;
            }
        }

        private const int DEFAULT_TIMEOUT = 30 * 1000;

        /// <summary>
        /// 单位：毫秒
        /// </summary>
        private int mTimeout;

        /// <summary>
        /// 单位：秒
        /// </summary>
        public int Timeout
        {
            get
            {
                return mTimeout;
            }
            set
            {
                if (value > 0)
                {
                    mTimeout = value;
                }
                else
                {
                    mTimeout = DEFAULT_TIMEOUT;
                }
            }
        }

        public HttpRequestTool(string url, RequestResultInvoker invoker)
            : this(url, Encoding.UTF8, invoker)
        {
        }

        public HttpRequestTool(string url, Encoding encoding, RequestResultInvoker invoker)
        {
            Timeout = DEFAULT_TIMEOUT;
            mIsWorking = false;

            mUrl = url;
            mEncoding = encoding;
            mRequestResultInvoker = invoker;
        }

        public bool Request()
        {
            lock (mIsWorkingLock)
            {
                if (mIsWorking) { return false; }
                mIsWorking = true;
            }

            string url = mUrl;
            Encoding encoding = mEncoding;
            RequestResultInvoker invoker = mRequestResultInvoker;
            ThreadPool.QueueUserWorkItem(new WaitCallback((object obj) =>
            {
                try
                {
                    string webContent = string.Empty;
                    //创建请求
                    HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
                    if (mIsWorking)//创建好请求后，如果主动调用了this.Abort()方法，则要关闭请求。在这个位置做判断，可以防止请求还未被创建就调用了this.Abort()方法，导致程序没能终止请求的问题
                    {
                        mRequest = request;
                        request.Method = "GET";
                        //设置超时
                        request.Timeout = mTimeout;
                        request.ReadWriteTimeout = mTimeout;
                        //
                        request.Accept = AcceptRequest;
                        request.Headers.Set("Pragma", "no-cache");
                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        {
                            using (Stream stream = response.GetResponseStream())
                            {
                                using (StreamReader streamReader = new StreamReader(stream, encoding))
                                {
                                    webContent = streamReader.ReadToEnd();
                                    if (invoker != null)
                                    {
                                        invoker(webContent, null);
                                    }
                                }
                            }

                        }
                    }
                    else
                    {
                        if (request != null)
                        {
                            request.Abort();
                            request = null;
                        }
                        mRequest = null;
                    }
                }
                catch (Exception ex)
                {
                    if (invoker != null)
                    {
                        invoker(null, ex.ToString());
                    }
                    mRequest = null;
                }
                finally
                {
                    lock (mIsWorkingLock)
                    {
                        mIsWorking = false;
                    }
                    mRequest = null;
                }

            }));
            return true;
        }

        public void Abort()
        {
            lock (mIsWorkingLock)
            {
                if (!mIsWorking) { return; }
                mIsWorking = false;
            }
            HttpWebRequest request = mRequest;
            if (request != null)
            {
                request.Abort();
                request = null;
            }
            mRequest = null;
        }
    }
}
