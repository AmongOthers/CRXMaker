using System;
using System.Collections.Generic;
using System.IO;
using System.Resources;
using System.Text;

namespace Tools
{
    public class ResourceFileHelper
    {
        private static ResourceFileHelper sInstance;
        private static object sInstanceLock = new object();
        public static readonly string UIRES_PATH = Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "ui"), "UI.resources");

        private ResourceManager mResourceManager = null;
        public static bool IsClientTypeExist { get; set; }
        public static string ResClientType { get; set; }

        private ResourceFileHelper()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "ui");
            if (isUIFileExist())
            {
                mResourceManager = ResourceManager.CreateFileBasedResourceManager("UI", path, null);
            }
        }

        public static ResourceFileHelper GetInstance()
        {
            if (sInstance == null)
            {
                lock (sInstanceLock)
                {
                    if (sInstance == null)
                    {
                        sInstance = new ResourceFileHelper();
                    }
                }
            }
            return sInstance;
        }

        public Stream GetStreamWithFilePath(string filePath)
        {
            Stream stream = null;
            try
            {
                if (mResourceManager != null)
                {
                    stream = (Stream)mResourceManager.GetObject(filePath.ToLower());
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger("ResourceFileHelper").Error("获取文件出错,文件地址:" + filePath, ex);
            }
            return stream;
        }

        private bool isUIFileExist()
        {
            if (File.Exists(UIRES_PATH))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 把目标路径下的所有文件合并到一个资源文件里
        /// </summary>
        /// <param name="destPath">目录路径</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="isOverride">是否覆盖存在的文件</param>
        /// <returns>写入成功返回:true;否则:false</returns>
        public static bool SetResourceWriter(string destPath, string savePath, bool isOverride = true)
        {
            try
            {
                using (ResourceWriter resourceWriter = new ResourceWriter(savePath))
                {
                    //递归目录下的所有文件夹和文件
                    recursionDir(destPath, destPath, savePath, resourceWriter);
                    resourceWriter.Generate();
                    Logger.Logger.GetLogger(typeof(ResourceFileHelper).Name).Info("资源保存成功");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(ResourceFileHelper).Name).ErrorFormat("资源保存时出错:{0}", ex.ToString());
                return false;
            }
        }

        public static byte[] GetContentFromResource(string resourceFilePath, string resourceName)
        {
            try
            {
                if (!File.Exists(resourceFilePath))
                {
                    throw new FileNotFoundException("resourceFilePath");
                }

                using (var resReader = new System.Resources.ResourceReader(resourceFilePath))
                {
                    string outResourceType;
                    byte[] outResourceDataBytes;
                    resReader.GetResourceData(resourceName, out outResourceType, out outResourceDataBytes);
                    return outResourceDataBytes;
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(ResourceFileHelper).Name).ErrorFormat("读取资源时出错:{0}", ex.ToString());
                return null;
            }
        }

        public static bool CreateFileFromResource(string resourceFilePath)
        {
            if (string.IsNullOrEmpty(resourceFilePath))
            {
                throw new ArgumentNullException("资源文件路径参数不能为空!");
            }

            if (!File.Exists(resourceFilePath))
            {
                throw new FileNotFoundException("资源文件不存在!");
            }

            try
            {
                FileInfo fileInfo = new FileInfo(resourceFilePath);
                using (var resReader = new System.Resources.ResourceReader(resourceFilePath))
                {
                    foreach (System.Collections.DictionaryEntry item in resReader)
                    {
                        Logger.Logger.GetLogger(typeof(ResourceFileHelper).Name).InfoFormat("开始创建文件:{0}", item.Key);
                        var createFilePath = Path.Combine(fileInfo.Directory.FullName, item.Key.ToString());
                        try
                        {
                            if (File.Exists(createFilePath))
                            {
                                File.Delete(createFilePath);
                            }

                            var dir = new FileInfo(createFilePath).Directory.FullName;
                            if (!Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Logger.GetLogger(typeof(ResourceFileHelper).Name).ErrorFormat("删除已存在的{0}文件时出错:{1}", createFilePath, ex.ToString());
                        }

                        using (FileStream fs = File.Create(createFilePath))
                        {
                            Byte[] info = Encoding.UTF8.GetBytes(item.Value.ToString());
                            fs.Write(info, 0, info.Length);
                        }
                        Logger.Logger.GetLogger(typeof(ResourceFileHelper).Name).InfoFormat("结束创建文件:{0}", item.Key);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(ResourceFileHelper).Name).ErrorFormat("释放资源时出错:{0}", ex.ToString());
                return false;
            }
        }

        //递归目录
        private static void recursionDir(string rootPath, string destPath, string savePath, ResourceWriter resourceWriter)
        {
            DirectoryInfo directoryRoot = new DirectoryInfo(destPath);
            FileSystemInfo[] fileSystemRoot = directoryRoot.GetFileSystemInfos();//获取特定目录下的所有文件夹和文件
            foreach (FileSystemInfo fileSystem in fileSystemRoot)
            {
                string tmpPath = fileSystem.FullName;
                //判断是文件还是文件夹
                FileAttributes fileAttr = File.GetAttributes(tmpPath);
                if ((fileAttr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    //是文件夹
                    recursionDir(rootPath, tmpPath, savePath, resourceWriter);
                }
                else
                {
                    //是文件
                    if (tmpPath.ToLower().Equals(savePath.ToLower()))//过滤 资源文件
                    {
                        continue;
                    }
                    var relativePath = tmpPath.Substring(rootPath.Length + 1);
                    relativePath = relativePath.Replace(@"\", @"/").ToLower(); //  /css/base/PC/G3/app.css
                    try
                    {
                        using (StreamReader sr = File.OpenText(tmpPath))
                        {
                            resourceWriter.AddResource(relativePath, sr.ReadToEnd());
                        }

                    }
                    catch (Exception ex)
                    {
                        Logger.Logger.GetLogger(typeof(ResourceFileHelper).Name).Error("读取文件或添加资源失败...", ex);
                        throw ex;
                    }
                }
            }
        }
    }
}
