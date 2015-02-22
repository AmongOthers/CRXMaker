using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;

namespace Tools
{
    public class NodeServer
    {
        public static NodeServer CreateServer(String website, String noCacheId)
        {
            int port = Netport.GetFreePortFrom(10000);
            if (port == -1)
            {
                throw new Exception("我们无法找到这台PC机上的可用的端口？");
            }
            website = website != null ? website : new DirectoryInfo(
                Environment.CurrentDirectory).Parent.FullName;
            var server = new NodeServer(port, website, noCacheId);
            return server;
        }

        private LivingShell mShell = null;
        private int mPort = -1;
        private String mWebsite;
        private String mNoCacheId;
        private bool mIsStopped = false;
        private object mLock = new object();
        private WebClient mWebClient = new WebClient();
        private String mTestPath = null;

        private NodeServer(int port, String website, String noCacheId)
        {
            mPort = port;
            mWebsite = website;
            mNoCacheId = noCacheId;
        }

        public bool Test()
        {
            try
            {
                String path = "http://localhost:" + mPort + "/zbknsygyjczdmzb";
                String test = mWebClient.DownloadString(path);
                return test == "test";
            }
            catch(Exception e) 
            {
                Console.Write(e);
            }
            return false;
        }

        //因为服务器可能中途退出，所以请每次都使用这个API获取端口号，不要使用本地缓冲变量
        public int Port
        {
            get
            {
                return mPort;
            }
        }

        public String Website
        {
            get
            {
                return mWebsite;
            }
        }

        public void Start()
        {
            lock (mLock)
            {
                mTestPath = Path.Combine(mWebsite, "zbknsygyjczdmzb");
                using (StreamWriter writer = new StreamWriter(mTestPath))
                {
                    writer.Write("test");
                }
                startShell();
            }
        }

        public void Stop()
        {
            lock (mLock)
            {
                try
                {
                    File.Delete(mTestPath);
                }
                catch
                {
                }
                mShell.stop();
                mIsStopped = true;
            }
        }

        private void onShellExit()
        {
            lock (mLock)
            {
                if (!mIsStopped)
                {
                    startShell();
                }
            }
        }

        private void startShell()
        {
            //always restart server if it exit
            String args = String.Format("server.js {0} \"{1}\" {2}", 
                mPort, mWebsite, mNoCacheId);
            mShell = new LivingShell("node.exe", args, Encoding.UTF8);
            mShell.Exit += onShellExit;
            mShell.Output += (str) =>
            {
                if (!string.IsNullOrEmpty(str))
                {
                    Logger.Logger.GetLogger(this).Debug("server output: " + str);
                }
            };
            mShell.Error += (str) =>
            {
                if (!string.IsNullOrEmpty(str))
                {
                    if (str.Contains("Error: listen EADDRINUSE"))
                    {
                        //有些端口可能被某些程序占用而无法启用，但是 netstat -an 没有罗列出来
                        mPort++;
                    }
                    Logger.Logger.GetLogger(this).Error("server error: " + str);
                }
            };
            mShell.start();
        }
    }
}
