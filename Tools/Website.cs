using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Web;

namespace Tools
{
    public class Website
    {
        public enum WebsiteType
        {
            FILE,
            HTTP,
            UNDEFINED
        }
        public delegate void WebSiteReadyDelegate(WebsiteType type, String homePage);
        public event WebSiteReadyDelegate Ready;

        private NodeServer mServer;
        private String mHomePage;
        private WebsiteType mType;
        private String mNoCacheId;

        private static Website sInstance;
        public static Website GetInstance() 
        {
            if (sInstance == null)
            {
                sInstance = new Website();
            }
            return sInstance;
        }
        private Website()
        {
        }

        public void Prepare(String indexHtmlRelativePath, String website, WebsiteType type = WebsiteType.UNDEFINED)
        {
            mType = type == WebsiteType.UNDEFINED ? 
                (Tools.ToolLongChinesePathTest.IsTooLongChinesePath(website) ? WebsiteType.HTTP : WebsiteType.FILE) :
                type;
            prepareWebsite(indexHtmlRelativePath, website);
        }
        public void Shutdown()
        {
            if (mServer != null)
            {
                mServer.Stop();
            }
        }
        public String HomePage
        {
            get
            {
                return mHomePage;
            }
        }
        public WebsiteType Type
        {
            get
            {
                return mType;
            }
        }
        public String RoutePath(String path)
        {
            String result;
            if (mType == WebsiteType.HTTP)
            {
                result = "http://localhost:" + mServer.Port + "/" + mNoCacheId + path;
            }
            else
            {
                result = "file:///" + path;
            }
            return result;
        }

        protected void ready()
        {
            if (Ready != null)
            {
                Ready(mType, mHomePage);
            }
        }

        private void prepareWebsite(String indexHtmlRelativePath, String website)
        {
            if (mType == WebsiteType.HTTP)
            {
                runWebsiteWithHttpProtocol(website, indexHtmlRelativePath);
            }
            else
            {
                runWebsiteWithFileProtocol(website, indexHtmlRelativePath);
            }
        }
        private void runWebsiteWithFileProtocol(String website, String indexHtmlRelativePath)
        {
            String path = "file:///" + HttpUtility.UrlPathEncode(website + indexHtmlRelativePath);
            mHomePage = path;
            mType = WebsiteType.FILE;
            ready();
        }
        private void runWebsiteWithHttpProtocol(String website, String indexHtmlRelativePath)
        {
            mNoCacheId = Tools.CommonHelper.GetUtcNow().ToString();
            mServer = NodeServer.CreateServer(website, mNoCacheId);
            mServer.Start();
            //wait for server ready
            new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(100);
                    if (mServer.Test())
                    {
                        break;
                    }
                }
                String path = "http://localhost:" + mServer.Port + "/" + mNoCacheId + indexHtmlRelativePath;
                mHomePage = path;
                mType = WebsiteType.HTTP;
                ready();
            }).Start();
        }
    }
}
