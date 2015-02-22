using CE.iPhone.PList;
using ICSharpCode.SharpZipLib.Zip;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Tools.AppInfoUtils
{
    public class AppInfoTool
    {
        public Apk Apk { get; private set; }

        public AppInfoTool()
        {
            this.Apk = new Apk();
        }
    }

    public class Apk
    {
        private static Regex ICON_REGEX = new Regex("icon='(.*?)'");
        private static Regex NAME_REGEX = new Regex("application: label='(.*?)'");
        private static Regex VERSION_REGEX = new Regex("versionName='(.*?)'");
        private static Regex SYSTEMREQUIREMENTS_REGEX = new Regex("sdkVersion:'(.*?)'");
        private static Regex PERMISSION_REGEX = new Regex("uses-permission:'(.*?)'");
        private static Regex PACKAGE_NAME_REGEX = new Regex("name='(.*?)'");

        public void GetApkInfo(string apkPath, Action<ApkOrIpaInfo> callBack)
        {
            ApkOrIpaInfo result = GetApkInfo(apkPath);
            callBack(result);
        }

        public ApkOrIpaInfo GetApkInfo(string apkPath)
        {
            ApkOrIpaInfo result = null;
            string currentApk = string.Empty;
            try
            {
                string tempPath = Path.GetTempPath();
                string appExtension = Path.GetExtension(apkPath);
                string apkDirPath = Path.Combine(tempPath, "ApkTemp");
                Directory.CreateDirectory(apkDirPath);
                currentApk = Path.Combine(apkDirPath, Guid.NewGuid().ToString() + appExtension);
                File.Copy(apkPath, currentApk, true);
                if (File.Exists(currentApk))
                {
                    result = new ApkOrIpaInfo();
                    result.ApkOrIpaPath = apkPath;
                    string args = string.Format("dump badging \"{0}\"", currentApk);

                    ExecutorControl executorControl;
                    CommandErrno erron = CommandHelper.excute("aapt.exe", args, 10 * 1000, out executorControl, Encoding.UTF8);
                    string output = erron.Output;
                    if (!string.IsNullOrEmpty(output))
                    {
                        Match match;
                        //图标
                        match = ICON_REGEX.Match(output);
                        if (match.Success)
                        {
                            result.AppIcon = match.Groups[1].Value;
                        }
                        //名称
                        match = NAME_REGEX.Match(output);
                        if (match.Success)
                        {
                            result.AppName = match.Groups[1].Value;
                        }
                        else
                        {
                            FileInfo fi = new FileInfo(apkPath);
                            result.AppName = fi.Name.Replace(fi.Extension, "");
                        }
                        //版本
                        match = VERSION_REGEX.Match(output);
                        if (match.Success)
                        {
                            result.AppVersion = match.Groups[1].Value;
                        }
                        //包名
                        match = PACKAGE_NAME_REGEX.Match(output);
                        if (match.Success)
                        {
                            result.PackageName = match.Groups[1].Value;
                        }
                        //系统要求
                        match = SYSTEMREQUIREMENTS_REGEX.Match(output);
                        if (match.Success)
                        {
                            result.AppSystemRequirements = Common.GetAllAndroidVersion(match.Groups[1].Value);
                        }
                        //权限
                        match = PERMISSION_REGEX.Match(output);
                        Dictionary<string, string> allPermissionDescription = Common.GetAllPermissionDescription();
                        while (match.Success)
                        {
                            string currPermission = match.Groups[1].Value;
                            if (allPermissionDescription.ContainsKey(currPermission))
                            {
                                result.AppPermissions.Add(allPermissionDescription[currPermission]);
                            }
                            else
                            {
                                //result.AppPermissions.Add(match.Groups[1].Value);
                            }
                            match = match.NextMatch();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                result = null;
                Logger.Logger.GetLogger(this).Error("GetApkInfo", e);
            }
            finally
            {
                try
                {
                    if (File.Exists(currentApk))
                    {
                        File.Delete(currentApk);
                    }
                }
                catch (Exception)
                {
                }
            }
            return result;
        }

        public void GetApkIcon(string apkPath, string icoPathInApk, Action<string> callBack)
        {
            string fileDirPath = Path.Combine(Path.GetTempPath(), "AppInstallerTemp");
            string iconName = Guid.NewGuid().ToString();

            string iconPath = ExtractApkIcon(apkPath, icoPathInApk, fileDirPath, iconName);
            callBack(iconPath);
        }

        /// <summary>
        /// 提取图标到指定位置
        /// </summary>
        /// <param name="apkPath"></param>
        /// <param name="icoPathInApk"></param>
        /// <param name="iconDstDirPath"></param>
        /// <param name="iconDstName"></param>
        /// <returns>返回图标所在位置</returns>
        public string ExtractApkIcon(string apkPath, string icoPathInApk, string iconDstDirPath, string iconDstName)
        {
            try
            {
                string tempPath = Path.GetTempPath();
                string iconExtension = Path.GetExtension(icoPathInApk);
                string unzipPath = Path.Combine(tempPath, "ApkIconTemp_" + Guid.NewGuid().ToString());//图标解压的临时目录
                string iconSrcPath = (icoPathInApk.StartsWith("\\") || icoPathInApk.StartsWith("/")) ?
                    Path.Combine(unzipPath, icoPathInApk.Substring(1)) : Path.Combine(unzipPath, icoPathInApk);//解压出来的APK图标（临时目录下的）

                string iconDstPath = Path.Combine(iconDstDirPath, iconDstName + iconExtension);
                Directory.CreateDirectory(iconDstDirPath);
                //解压以获得icon文件
                if (Common.UnzipSpecifiedFile(apkPath, icoPathInApk, unzipPath))
                {
                    Common.MakeThumbnail(iconSrcPath, iconDstPath, 48, 48, "HW");
                    try
                    {
                        Directory.Delete(unzipPath);
                    }
                    catch (Exception)
                    {
                    }
                    return iconDstPath;
                }
                else
                {
                    Logger.Logger.GetLogger(this).Error("解压应用图标失败");
                }
            }
            catch (Exception e)
            {
                Logger.Logger.GetLogger(this).Error("获取应用图标失败", e);
            }
            return null;
        }
    }
    
}
