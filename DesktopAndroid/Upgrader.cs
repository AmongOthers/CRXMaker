using HttpDownloader;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using Tools;

namespace DesktopAndroid
{
    public class Upgrader : OnDownloadListener
    {
        private string downloadPath;

        public void Init()
        {
            int currentVersion = Int16.Parse(ConfigurationManager.AppSettings["version"]);
            string upgradeUrl = String.Format("{0}/api/upgrade?version={1}", ConfigurationManager.AppSettings["server"], currentVersion);
            var req = HttpWebRequest.Create(upgradeUrl);
            req.ContentType = "text/json";
            req.Timeout = 30 * 1000;
            var rsp = req.GetResponse() as HttpWebResponse;
            using (var stream = rsp.GetResponseStream())
            {
                using (var reader = new StreamReader(stream))
                {
                    var rspContent = reader.ReadToEnd();
                    var upgradeResult = Newtonsoft.Json.JsonConvert.DeserializeObject<UpgradeResult>(rspContent);
                    if (!String.IsNullOrEmpty(upgradeResult.DownloadPath))
                    {
                        startDownload(upgradeResult.DownloadPath);
                    }
                }
            }
        }

        private void startDownload(string url)
        {
            var downloaer = new WebClientDownloader(this);
            downloadPath = Path.Combine(Application.LocalUserAppDataPath, Path.GetRandomFileName());
            downloaer.download(url, downloadPath, true);
        }

        void OnDownloadListener.onDownloadPrograss(long finished, long total)
        {
        }

        void OnDownloadListener.onDownloadFinish(bool isSucess)
        {
            try
            {
                if (isSucess)
                {
                    Zip.unzipAsDir(downloadPath, Environment.CurrentDirectory);
                }
            }
            catch (Exception)
            {

            }
            
        }
    }
}
