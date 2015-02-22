using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Tools.AppInfoUtils
{
    public class Common
    {
        /// <summary>
        /// 解压指定文件
        /// </summary>
        /// <param name="zipFilePath"></param>
        /// <param name="specifiedFile">指定的文件</param>
        /// <param name="afterPath"></param>
        /// <returns></returns>
        public static bool UnzipSpecifiedFile(string zipFilePath, string specifiedFile, string afterPath)
        {
            //EnciphermentUtils.DecryptData(zipFilePath, afterPath + ".zip");
            const int SIZE = 2048;
            try
            {
                Directory.CreateDirectory(afterPath);
                ZipInputStream s = new ZipInputStream(File.OpenRead(zipFilePath));

                ZipEntry theEntry;
                while ((theEntry = s.GetNextEntry()) != null)
                {
                    string directoryName = Path.GetDirectoryName(theEntry.Name);
                    string fileName = Path.GetFileName(theEntry.Name);
                    if (fileName != string.Empty)
                    {
                        if (theEntry.Name == specifiedFile)
                        {
                            if (directoryName != string.Empty)
                            {
                                Directory.CreateDirectory(Path.Combine(afterPath, directoryName));
                            }
                            FileStream streamWrtier = File.Create(Path.Combine(afterPath, theEntry.Name));

                            byte[] data = new byte[SIZE];
                            int size;
                            while (true)
                            {
                                size = s.Read(data, 0, data.Length);
                                if (size > 0)
                                {
                                    streamWrtier.Write(data, 0, size);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            streamWrtier.Close();
                        }
                    }
                }
                s.Close();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(Common).Name).Error("解压文件出错", ex);
                return false;
            }
        }
        
        //获取所有安卓版本
        public static string GetAllAndroidVersion(string versionCode)
        {
            string result = string.Empty;

            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                Stream stream = assembly.GetManifestResourceStream("Tools.AppInfoUtils.AndriodVersions.txt");
                StreamReader reader = new StreamReader(stream);

                do
                {
                    string line = reader.ReadLine();
                    string[] datas = line.Split('\t');
                    if (datas[0] == versionCode)
                    {
                        result = datas[1];
                        break;
                    }
                } while (!reader.EndOfStream);
            }
            catch (Exception ex)
            {
                result = string.Empty;
                Logger.Logger.GetLogger(typeof(Common)).Error(ex);
            }

            return result;
        }

        //获取所有的权限说明描述
        public static Dictionary<string, string> GetAllPermissionDescription()
        {
            Dictionary<string, string> result = null;

            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                Stream stream = assembly.GetManifestResourceStream("Tools.AppInfoUtils.AppPermissions.txt");
                StreamReader reader = new StreamReader(stream);
                result = new Dictionary<string, string>();
                do
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrEmpty(line)) //空行后面的不读取
                        break;
                    string[] datas = line.Split('\t');
                    result.Add(datas[0], datas[1]);

                } while (!reader.EndOfStream);
            }
            catch (Exception ex)
            {
                result = null;
                Logger.Logger.GetLogger(typeof(Common)).Error(ex);
            }

            return result;
        }

        public static void MakeThumbnail(string originalImagePath, string thumbnailPath, int width, int height, string mode)
        {
            Image originalImage = null;
            using (FileStream fs = new FileStream(originalImagePath, FileMode.Open))
            {
                originalImage = new Bitmap(fs);

                int towidth = width;
                int toheight = height;

                int x = 0;
                int y = 0;
                int ow = originalImage.Width;
                int oh = originalImage.Height;
                //if (originalImage.Width <= towidth && originalImage.Height <= toheight)
                //{
                //    return;
                //}
                //else
                //{
                switch (mode)
                {
                    case "HW"://指定高宽缩放（可能变形）                
                        break;
                    case "W"://指定宽，高按比例                    
                        toheight = originalImage.Height * width / originalImage.Width;
                        break;
                    case "H"://指定高，宽按比例
                        towidth = originalImage.Width * height / originalImage.Height;
                        break;
                    case "zyHW"://根据比例自动选择按高或宽进行缩写
                        if (originalImage.Height / height >= originalImage.Width / width)//按高缩写
                        {
                            towidth = originalImage.Width * height / originalImage.Height;
                        }
                        else
                        {
                            toheight = originalImage.Height * width / originalImage.Width;//按宽缩写
                        }
                        break;
                    case "Cut"://指定高宽裁减（不变形）

                        if ((double)originalImage.Width / (double)originalImage.Height > (double)towidth / (double)toheight)
                        {
                            oh = originalImage.Height;
                            ow = originalImage.Height * towidth / toheight;
                            y = 0;
                            x = (originalImage.Width - ow) / 2;
                        }
                        else
                        {
                            ow = originalImage.Width;
                            oh = originalImage.Width * height / towidth;
                            x = 0;
                            y = (originalImage.Height - oh) / 2;
                        }
                        break;
                    default:
                        break;
                }
                //}

                //新建一个bmp图片
                System.Drawing.Image bitmap = new System.Drawing.Bitmap(towidth, toheight);

                //新建一个画板
                Graphics g = System.Drawing.Graphics.FromImage(bitmap);

                //设置高质量插值法
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;

                //设置高质量,低速度呈现平滑程度
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                //清空画布并以透明背景色填充
                g.Clear(Color.Transparent);

                //在指定位置并且按指定大小绘制原图片的指定部分
                g.DrawImage(originalImage, new Rectangle(0, 0, towidth, toheight),
                 new Rectangle(x, y, ow, oh),
                 GraphicsUnit.Pixel);

                try
                {
                    //以png格式保存缩略图
                    bitmap.Save(thumbnailPath, System.Drawing.Imaging.ImageFormat.Png);
                }
                catch (System.Exception e)
                {
                    throw e;
                }
                finally
                {
                    originalImage.Dispose();
                    bitmap.Dispose();
                    g.Dispose();
                }
            }
        }
    }
}
