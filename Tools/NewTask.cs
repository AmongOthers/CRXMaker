using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Tools
{
    [Serializable]
    public class NewTask : ISerializable
    {
        public enum DownloadTaskStatus { Init, DownloadWaiting, Downloading, DownloadFailed, DownloadSucceeded, DownloadPaused, DownloadCancel }
        public enum DownloadPriorityType : int { Low = 0, Medium = 1, High = 2 }
        public enum DownloadTaskType { App, Firmware, Music, Icon, Other }
        public enum InstallTaskStatus { Init, InstallWaiting, Installing, InstallSucceeded, InstallFailed, InstallPaused, InstallCancel };

        public enum MoveAllStatus { Init, WaitingMove, Moving, MoveSucceeded, MoveFailed, PauseMove, CancelMove }

        public string PackageName { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string TemplateId { get; set; }
        public string Size { get; set; }
        public string Cmd { get; set; }

        /// <summary>
        /// 给工单使用的ID
        /// </summary>
        public string AppIdForOrder { get; set; }
        public string ExtraMessage { get; set; }

        public string PhoneType { get; set; }
        public string ResourceType { get; set; }
        public bool IsMatch { get; set; }

        public string ID { get; private set; }
        public string MD5 { get; private set; }
        public string URL { get; private set; }
        public string FilePath { get; private set; }

        public Action<NewTask> CallBackTask { get; set; }
        /// <summary>
        /// 是否使用断点续传(默认为true)
        /// </summary>
        public bool IsUsedBreakpoint { get; set; }

        /// <summary>
        /// 下载优先级别
        /// </summary>
        public DownloadPriorityType PriorityType { get; set; }
        public DownloadTaskType TaskType { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorInfo { get; set; }

        /// <summary>
        /// 已经下载的大小（包括程序启动前下载的大小），单位：字节
        /// </summary>
        public long AlreadyDownloadedSize { get; set; }
        /// <summary>
        /// 进度。例如，20表示20%进度
        /// </summary>
        public int DownloadProgres { get; set; }
        /// <summary>
        /// 下载速度。单位：KB/S
        /// </summary>
        public double DownloadSpeed { get; set; }

        /// <summary>
        /// 下载的时间，单位:（毫秒）
        /// </summary>
        public long DownloadedTime { get; set; }

        [NonSerialized]
        private bool mIsDeleteFile;
        public bool IsDeleteFile
        {
            get
            {
                return mIsDeleteFile;
            }
            set
            {
                mIsDeleteFile = value;
            }
        }

        [NonSerialized]
        private bool mIsDownloadingLock;//不序列化，为了反序列化时，该值为false
        /// <summary>
        /// 用于判断是否被下载线程占用着（和下载状态标记不太一样）
        /// </summary>
        public bool IsDownloadingLock
        {
            get
            {
                return mIsDownloadingLock;
            }
            set
            {
                mIsDownloadingLock = value;
            }
        }

        /// <summary>
        /// 断点文件位置信息
        /// </summary>
        public BreakpointFilePos TempFilePos { get; private set; }

        public BreakpointFilePos IconTempFilePos { get; set; }

        public DateTime CreateTaskTime { get; private set; }

        public DownloadTaskStatus DownloadStatus { get; private set; }
        private ReaderWriterObjectLocker mDownloadStatusLock = new ReaderWriterObjectLocker();

        [NonSerialized]
        private InstallTaskStatus mInstallStatus;
        public InstallTaskStatus InstallStatus
        {
            get
            {
                return mInstallStatus;
            }
            set
            {
                mInstallStatus = value;
            }
        }
        private ReaderWriterObjectLocker mInstallStatusLock = new ReaderWriterObjectLocker();

        [NonSerialized]
        private MoveAllStatus mMoveStatus;
        public MoveAllStatus MoveStatus
        {
            get
            {
                return mMoveStatus;
            }
            set
            {
                mMoveStatus = value;
            }
        }
        private ReaderWriterObjectLocker mMoveStatusLock = new ReaderWriterObjectLocker();

        public string IconTaskId { get; set; }

        /// <summary>
        /// 是否创建图标
        /// </summary>
        public bool IsCreateIcon { get; set; }

        public string IconUrl { get; set; }

        public string IconFilePath { get; set; }

        /// <summary>
        /// 图标存放的相对路径
        /// </summary>
        public string IconRelativePath { get; set; }

        /// <summary>
        /// 图标存放的地址。可能是URL，也可能是本地地址
        /// </summary>
        public string IconAddress
        {
            get
            {
                if (File.Exists(IconFilePath))
                {
                    return IconFilePath;
                }
                return IconUrl;
            }
        }

        public bool IsSilent { get; set; }

        public SilentInfo Silent { get; set; }

        /// <summary>
        /// 安装进度
        /// </summary>
        public int InstallProgress { get; set; }
        /// <summary>
        /// 错误次数
        /// </summary>
        public int ErrorCount { get; set; }

        //缓存资源用到
        public string DynamicUrl { get; set; }
        public string StaticUrl { get; set; }
        public string ETag { get; set; }
        public string LastModified { get; set; }
        public bool IsNoNeedDownload { get; set; }

        public Tools.DownloadTask.RedirectUrlHandler RedirectUrlHandle { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">任务ID</param>
        /// <param name="md5"></param>
        /// <param name="url">下载地址</param>
        /// <param name="filePath">文件保存位置（包括文件名和扩展名）</param>
        /// <param name="isSilent">是否是静默任务</param>
        public NewTask(string id, string md5, string url, string filePath, bool isSilent = false
            , DownloadTaskType taskType = DownloadTaskType.Other)
        {
            ID = id;
            MD5 = md5;
            URL = url;
            FilePath = filePath;
            IsSilent = isSilent;
            TaskType = taskType;
            PriorityType = DownloadPriorityType.Medium;
            CreateTaskTime = DateTime.Now;
            IsDeleteFile = false;
            mIsDownloadingLock = false;

            string tmpFilePath = filePath + ".lxtmp";
            string tmpFileInfoPath = filePath + ".info";
            TempFilePos = new BreakpointFilePos(tmpFilePath, tmpFileInfoPath);
            //
            IsMatch = true;
            TemplateId = string.Empty;
            AppIdForOrder = string.Empty;
            //
            IsCreateIcon = true;
            IsUsedBreakpoint = true;
            MoveStatus = MoveAllStatus.WaitingMove;
        }

        public NewTask(string id, string dynamicUrl, string staticUrl, string eTag, string lastModified, string filePath, string md5)
        {
            ID = id;
            MD5 = md5;
            URL = String.IsNullOrEmpty(dynamicUrl) ? staticUrl : dynamicUrl;
            FilePath = filePath;
            IsSilent = false;
            TaskType = DownloadTaskType.Other;
            PriorityType = DownloadPriorityType.Low;
            CreateTaskTime = DateTime.Now;
            IsDeleteFile = false;
            mIsDownloadingLock = false;

            string tmpFilePath = filePath + ".lxtmp";
            string tmpFileInfoPath = filePath + ".info";
            TempFilePos = new BreakpointFilePos(tmpFilePath, tmpFileInfoPath);

            DynamicUrl = dynamicUrl;
            StaticUrl = staticUrl;
            ETag = eTag;
            LastModified = lastModified;
        }
        /// <summary>
        /// 用于解决混淆后，反射的问题。（增加新成员后，要为新成员写相应的操作）
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected NewTask(SerializationInfo info, StreamingContext context)
        {
            PackageName = getValue<string>(info, "PackageName");
            Name = getValue<string>(info, "Name");
            Version = getValue<string>(info, "Version");
            TemplateId = getValue<string>(info, "TemplateId");
            Size = getValue<string>(info, "Size");
            Cmd = getValue<string>(info, "Cmd");
            AppIdForOrder = getValue<string>(info, "AppIdForOrder");
            ExtraMessage = getValue<string>(info, "ExtraMessage");
            PhoneType = getValue<string>(info, "PhoneType");
            ResourceType = getValue<string>(info, "ResourceType");
            ID = getValue<string>(info, "ID");
            MD5 = getValue<string>(info, "MD5");
            URL = getValue<string>(info, "URL");
            FilePath = getValue<string>(info, "FilePath");
            IsMatch = getValue<bool>(info, "IsMatch");
            PriorityType = getValue<DownloadPriorityType>(info, "PriorityType");
            TaskType = getValue<DownloadTaskType>(info, "TaskType");
            ErrorInfo = getValue<string>(info, "ErrorInfo");
            AlreadyDownloadedSize = getValue<long>(info, "AlreadyDownloadedSize");
            DownloadProgres = getValue<int>(info, "DownloadProgres");
            DownloadSpeed = getValue<double>(info, "DownloadSpeed");
            DownloadedTime = getValue<long>(info, "DownloadedTime");
            TempFilePos = getValue<BreakpointFilePos>(info, "TempFilePos");
            IconTempFilePos = getValue<BreakpointFilePos>(info, "IconTempFilePos");
            CreateTaskTime = getValue<DateTime>(info, "CreateTaskTime");
            DownloadStatus = getValue<DownloadTaskStatus>(info, "DownloadStatus");
            IconTaskId = getValue<string>(info, "IconTaskId");
            IsCreateIcon = getValue<bool>(info, "IsCreateIcon");
            IconUrl = getValue<string>(info, "IconUrl");
            IconFilePath = getValue<string>(info, "IconFilePath");
            IconRelativePath = getValue<string>(info, "IconRelativePath");
            IsSilent = getValue<bool>(info, "IsSilent");
            Silent = getValue<SilentInfo>(info, "Silent");
            InstallProgress = getValue<int>(info, "InstallProgress");
            ErrorCount = getValue<int>(info, "ErrorCount");
            IsUsedBreakpoint = getValue<bool>(info, "IsUsedBreakpoint");
            DynamicUrl = getValue<string>(info, "DynamicUrl");
            StaticUrl = getValue<string>(info, "StaticUrl");
            ETag = getValue<string>(info, "ETag");
            LastModified = getValue<string>(info, "LastModified");
            IsNoNeedDownload = getValue<bool>(info, "IsNoNeedDownload");
        }

        /// <summary>
        /// 用于解决混淆后，反射的问题。（增加新成员后，要为新成员写相应的操作）
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            addValue(info, "PackageName", PackageName, typeof(string));
            addValue(info, "Name", Name, typeof(string));
            addValue(info, "Version", Version, typeof(string));
            addValue(info, "TemplateId", TemplateId, typeof(string));
            addValue(info, "Size", Size, typeof(string));
            addValue(info, "Cmd", Cmd, typeof(string));
            addValue(info, "AppIdForOrder", AppIdForOrder, typeof(string));
            addValue(info, "ExtraMessage", ExtraMessage, typeof(string));
            addValue(info, "PhoneType", PhoneType, typeof(string));
            addValue(info, "ResourceType", ResourceType, typeof(string));
            addValue(info, "ID", ID, typeof(string));
            addValue(info, "MD5", MD5, typeof(string));
            addValue(info, "URL", URL, typeof(string));
            addValue(info, "FilePath", FilePath, typeof(string));
            addValue(info, "IsMatch", IsMatch, typeof(bool));
            addValue(info, "PriorityType", PriorityType, typeof(DownloadPriorityType));
            addValue(info, "TaskType", TaskType, typeof(DownloadTaskType));
            addValue(info, "ErrorInfo", ErrorInfo, typeof(string));
            addValue(info, "AlreadyDownloadedSize", AlreadyDownloadedSize, typeof(long));
            addValue(info, "DownloadProgres", DownloadProgres, typeof(int));
            addValue(info, "DownloadSpeed", DownloadSpeed, typeof(double));
            addValue(info, "DownloadedTime", DownloadedTime, typeof(long));
            addValue(info, "TempFilePos", TempFilePos, typeof(BreakpointFilePos));
            addValue(info, "IconTempFilePos", IconTempFilePos, typeof(BreakpointFilePos));
            addValue(info, "CreateTaskTime", CreateTaskTime, typeof(DateTime));
            addValue(info, "DownloadStatus", DownloadStatus, typeof(DownloadTaskStatus));
            addValue(info, "IconTaskId", IconTaskId, typeof(string));
            addValue(info, "IsCreateIcon", IsCreateIcon, typeof(bool));
            addValue(info, "IconUrl", IconUrl, typeof(string));
            addValue(info, "IconFilePath", IconFilePath, typeof(string));
            addValue(info, "IconRelativePath", IconRelativePath, typeof(string));
            addValue(info, "IsSilent", IsSilent, typeof(bool));
            addValue(info, "Silent", Silent, typeof(SilentInfo));
            addValue(info, "InstallProgress", InstallProgress, typeof(int));
            addValue(info, "ErrorCount", ErrorCount, typeof(int));
            addValue(info, "IsUsedBreakpoint", IsUsedBreakpoint, typeof(bool));
            addValue(info, "DynamicUrl", DynamicUrl, typeof(string));
            addValue(info, "StaticUrl", StaticUrl, typeof(string));
            addValue(info, "ETag", ETag, typeof(string));
            addValue(info, "LastModified", LastModified, typeof(string));
            addValue(info, "IsNoNeedDownload", IsNoNeedDownload, typeof(bool));
        }

        private void addValue(SerializationInfo info, string name, object value, Type type)
        {
            try
            {
                info.AddValue(getDefaltName(name), value, type);
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("添加序列化值出错，name=" + name, ex);
            }
        }

        private T getValue<T>(SerializationInfo info, string name)
        {
            try
            {
                return (T)info.GetValue(getDefaltName(name), typeof(T));
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("获取序列化值出错，name=" + name, ex);
                return default(T);
            }
        }

        /// <summary>
        /// 说明：
        /// 因为当初使用默认的序列化方式（即没有继承ISerializable接口），所以序列化出来的值默认名字格式是“<*****>k__BackingField”，*号就是字段名
        /// 现在继承ISerializable接口，所以解析时名字要按程序默认的来命名（保持向旧的兼容）
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string getDefaltName(string name)
        {
            return "<" + name + ">k__BackingField";
        }

        public bool IsTargetDownloadStatus(DownloadTaskStatus targetStatus)
        {
            using (mDownloadStatusLock.ReadLock())
            {
                return DownloadStatus == targetStatus;
            }
        }

        public bool IsNotTargetDownloadStatus(DownloadTaskStatus targetStatus)
        {
            using (mDownloadStatusLock.ReadLock())
            {
                return DownloadStatus != targetStatus;
            }
        }

        public void SetDownloadStatus(DownloadTaskStatus targetStatus)
        {
            using (mDownloadStatusLock.WriteLock())
            {
                DownloadStatus = targetStatus;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceStatus">源状态</param>
        /// <param name="targetStatus">目标状态</param>
        /// <returns></returns>
        public bool SetDownloadStatus(DownloadTaskStatus sourceStatus, DownloadTaskStatus targetStatus)
        {
            using (mDownloadStatusLock.WriteLock())
            {
                if (DownloadStatus == sourceStatus)
                {
                    DownloadStatus = targetStatus;
                    return true;
                }
                return false;
            }
        }

        public bool IsTargetInstallStatus(InstallTaskStatus targetStatus)
        {
            using (mInstallStatusLock.ReadLock())
            {
                return InstallStatus == targetStatus;
            }
        }

        public bool IsNotTargetInstallStatus(InstallTaskStatus targetStatus)
        {
            using (mInstallStatusLock.ReadLock())
            {
                return InstallStatus != targetStatus;
            }
        }

        public void SetInstallStatus(InstallTaskStatus targetStatus)
        {
            using (mInstallStatusLock.WriteLock())
            {
                InstallStatus = targetStatus;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceStatus">源状态</param>
        /// <param name="targetStatus">目标状态</param>
        /// <returns></returns>
        public bool SetInstallStatus(InstallTaskStatus sourceStatus, InstallTaskStatus targetStatus)
        {
            using (mInstallStatusLock.WriteLock())
            {
                if (InstallStatus == sourceStatus)
                {
                    InstallStatus = targetStatus;
                    return true;
                }
                return false;
            }
        }

        public bool IsTargetMoveStatus(MoveAllStatus targetStatus)
        {
            using (mMoveStatusLock.ReadLock())
            {
                return MoveStatus == targetStatus;
            }
        }

        public bool IsNotTargetMoveStatus(MoveAllStatus targetStatus)
        {
            using (mMoveStatusLock.ReadLock())
            {
                return MoveStatus != targetStatus;
            }
        }

        public void SetMoveStatus(MoveAllStatus targetStatus)
        {
            using (mMoveStatusLock.WriteLock())
            {
                MoveStatus = targetStatus;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceStatus">源状态</param>
        /// <param name="targetStatus">目标状态</param>
        /// <returns></returns>
        public bool SetMoveStatus(MoveAllStatus sourceStatus, MoveAllStatus targetStatus)
        {
            using (mMoveStatusLock.WriteLock())
            {
                if (MoveStatus == sourceStatus)
                {
                    MoveStatus = targetStatus;
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 断点文件所在路径类
        /// </summary>
        [Serializable]
        public class BreakpointFilePos : ISerializable
        {
            /// <summary>
            /// 要下载的文件的临时文件所在路径
            /// </summary>
            public string TmpFilePath { get; private set; }
            /// <summary>
            /// 临时文件的信息文件所在路径
            /// </summary>
            public string TmpFileInfoPath { get; private set; }
            public BreakpointFilePos(string tmpFilePath, string tmpFileInfoPath)
            {
                TmpFilePath = tmpFilePath;
                TmpFileInfoPath = tmpFileInfoPath;
            }

            /// <summary>
            /// 用于解决混淆后，反射的问题。（增加新成员后，要为新成员写相应的操作）
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected BreakpointFilePos(SerializationInfo info, StreamingContext context)
            {
                TmpFilePath = getValue<string>(info, "TmpFilePath");
                TmpFileInfoPath = getValue<string>(info, "TmpFileInfoPath");
            }

            /// <summary>
            /// 用于解决混淆后，反射的问题。（增加新成员后，要为新成员写相应的操作）
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                addValue(info, "TmpFilePath", TmpFilePath, typeof(string));
                addValue(info, "TmpFileInfoPath", TmpFileInfoPath, typeof(string));
            }

            private T getValue<T>(SerializationInfo info, string name)
            {
                try
                {
                    return (T)info.GetValue(getDefaltName(name), typeof(T));
                }
                catch (Exception ex)
                {
                    Logger.Logger.GetLogger(this).Error("获取序列化值出错", ex);
                    return default(T);
                }
            }

            private void addValue(SerializationInfo info, string name, object value, Type type)
            {
                info.AddValue(getDefaltName(name), value, type);
            }

            /// <summary>
            /// 说明：
            /// 因为当初使用默认的序列化方式（即没有继承ISerializable接口），所以序列化出来的值默认名字格式是“<*****>k__BackingField”，*号就是字段名
            /// 现在继承ISerializable接口，所以解析时名字要按程序默认的来命名（保持向旧的兼容）
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            private string getDefaltName(string name)
            {
                return "<" + name + ">k__BackingField";
            }
        }
    }
}
