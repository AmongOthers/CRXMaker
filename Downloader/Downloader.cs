using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.IO;

namespace HttpDownloader
{
    public class Downloader
    {
        private static Downloader mInstance = null;
        private Hashtable mPathMap = new Hashtable();
        private static String CacheFilePath = "Downloader\\Cache.che";

        public delegate void onFileDownloadComplete(bool result, String url,String path,String file_name);

        private Downloader()
        {
            if (!Directory.Exists("Downloader"))
            {
                Directory.CreateDirectory("Downloader");
            }
            loadPathMap();
        }

        private void loadPathMap()
        {
            FileStream fs = new FileStream(CacheFilePath, FileMode.OpenOrCreate);
            StreamReader reader = new StreamReader(fs);
            String buffer_line = "";
            do 
            {
                buffer_line = reader.ReadLine();
                if (buffer_line == null || buffer_line.Length == 0)
                {
                    continue;
                }
                buffer_line = buffer_line.Trim();
                if (buffer_line.Length == 0)
                {
                    continue;
                }
                int index = buffer_line.LastIndexOf("=");
                string url = buffer_line.Substring(0, index);
                url = url.Trim();
                string path = buffer_line.Substring(index + 1);
                path = path.Trim();
                mPathMap[url] = path;
            } while (buffer_line != null && !buffer_line.Equals(""));
            fs.Flush();
            fs.Close();
        }

        private void addPath(String url, String path, String name)
        {
            String full_path = path + "\\" + name;
            mPathMap[url] = full_path;
            FileStream fs = new FileStream(CacheFilePath, FileMode.OpenOrCreate);
            String write_string = url + "=" + full_path + "\r\n";
            byte[] data = new UTF8Encoding().GetBytes(write_string);
            fs.Seek(0,SeekOrigin.End);
            fs.Write(data, 0, write_string.Length);
            fs.Flush();
            fs.Close();
        }

        public static Downloader getInstance()
        {
            if (mInstance == null)
            {
                mInstance = new Downloader();
            }
            return mInstance;
        }

        public String getLocalPathIfPossible(String url)
        {
            if (mPathMap.Contains(url))
            {
                return (String)mPathMap[url];
            }
            else
            {
                return null;
            }
        }

        public String download(String url, String path, String name)
        {
            if (mPathMap.Contains(url))
            {
                return (String)mPathMap[url];
            }
            else
            {
                DownloadTask task = new DownloadTask(url, path, name, null);
                task.IsCache = true;
                if (task.download())
                {
                    return task.FullPath;
                }
                else
                {
                    return null;
                }
            }
        }

        public String download(String url, String path, String name, onFileDownloadComplete callback)
        {
            if (mPathMap.Contains(url))
            {
                return (String)mPathMap[url];
            }
            else
            {
                DownloadTask task = new DownloadTask(url, path, name, callback);
                task.IsCache = true;
                task.start();
                return null;
            }
        }

        public void downloadNoCache(String url, String path, String name, onFileDownloadComplete callback)
        {
            DownloadTask task = new DownloadTask(url, path, name, callback);
            task.IsCache = false;
            task.start();
        }

        internal void downloadSuccessful(String url, String path, String name, onFileDownloadComplete callback , bool isCache)
        {
            if (isCache)
            {
                addPath(url, path, name);
            }
            if (callback != null)
            {
                callback(true, url, path, name);
            }
        }

        internal void downloadFailed(String url, String path, String name, onFileDownloadComplete callback, bool isCache)
        {
            if (callback != null)
            {
                callback(false, url, path, name);
            }
        }
    }
}
