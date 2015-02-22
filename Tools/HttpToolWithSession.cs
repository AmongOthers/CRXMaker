using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Tools
{
    public class HttpToolWithSession
    {
        public delegate void OnReceiveDataDelegate(string data);
        public interface GetDataListener
        {
            void recive_data(string data);
        }

        private WebBrowser _web_browser = new WebBrowser();

        private OnReceiveDataDelegate _onReceiveData;

        public HttpToolWithSession()
        {
        }

        protected void OnReceiveData(string data)
        {
            if (_onReceiveData != null)
            {
                _onReceiveData(data);
            }
        }

        public void get_data(string url, OnReceiveDataDelegate onReceiveData)
        {
            _web_browser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(_web_browser_DocumentCompleted);
            _onReceiveData = onReceiveData;
            _web_browser.Navigate(url);
        }

        public void get_data(string url, GetDataListener listener)
        {
            get_data(url, listener.recive_data);
        }

        void _web_browser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            _web_browser.DocumentCompleted -= new WebBrowserDocumentCompletedEventHandler(_web_browser_DocumentCompleted);
            string data = _web_browser.Document.Body.InnerText;
            if (data.StartsWith("无法显示网页"))
            {
                data = null;
            }
            _onReceiveData(data);
        }
    }
}
