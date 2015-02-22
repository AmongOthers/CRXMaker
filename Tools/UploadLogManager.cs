using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Tools
{
    public class UploadLogManager
    {
        private const string CMD_CODE = "debug_log";
        private readonly UploadDebugLog mUploadDebugLog;
        //状态：-1失败；0执行中；1完成
        private LogUploadMode mLogUploadState = LogUploadMode.FAIL;

        private static UploadLogManager instance = null;
        private static object mLock = new object();
        public static UploadLogManager GetInstance()
        {
            if (instance == null)
            {
                lock (mLock)
                {
                    if (instance == null)
                    {
                        instance = new UploadLogManager();
                    }
                }
            }
            return instance;
        }

        private UploadLogManager()
        {
            mUploadDebugLog = new UploadDebugLog();
        }

        /// <summary>
        /// 接收一个命令
        /// </summary>
        public void AcceptCmd(string cmd)
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(cmd);
            if (dict != null)
            {
                Logger.Logger.GetLogger(this).InfoFormat("AcceptCmd cmdCode:{0},userId:{1}", dict["cmdCode"], dict["userId"]);
                if (String.Compare(dict["cmdCode"], CMD_CODE, true) == 0 && String.Compare(dict["userId"], UserId, true) == 0)
                {
                    if (mLogUploadState != LogUploadMode.UPLOADING)
                    {
                        mLogUploadState = LogUploadMode.UPLOADING;

                        if (uploadDebugLog(dict["bgTimeStr"], dict["edTimeStr"]))
                        {
                            mLogUploadState = LogUploadMode.SUCCESS;
                            Logger.Logger.GetLogger(this).Info("上传Debug日志文件成功");
                        }
                        else
                        {
                            mLogUploadState = LogUploadMode.FAIL;
                            Logger.Logger.GetLogger(this).Info("上传Debug日志文件失败");
                        }
                        OnUploadLogResult(new UploadLogArgs(dict["id"], mLogUploadState));
                    }
                }
            }
        }

        private bool uploadDebugLog(string logFileStartTime, string logFileEndTime)
        {
            mUploadDebugLog.LogFileName = String.Format("{0}-{1}-{2}",
                ClientType,
                UserName,
                DateTime.Now.ToString("yyyyMMdd-HHmmss"));

            mUploadDebugLog.StartTime = logFileStartTime;
            mUploadDebugLog.EndTime = logFileEndTime;

            var serverResponse = String.Empty;
            if (mUploadDebugLog.UploadFile(out serverResponse))
            {
                return checkUploadLogResult(serverResponse);
            }
            return false;
        }

        private bool checkUploadLogResult(string serverResponse)
        {
            if (!string.IsNullOrEmpty(serverResponse))
            {
                try
                {
                    var serverResponseDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(serverResponse);
                    if (String.Compare(serverResponseDict["success"].ToString(), "true", true) == 0)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Logger.GetLogger(this).Error("解析上传日志文件后服务器返回来的Json出错",ex);
                }
            }
            return false;
        }

        protected void OnUploadLogResult(UploadLogArgs args)
        {
            if(UploadLogResult != null)
            {
                UploadLogResult(this,args);
            }
        }

        public string ClientType { get; set; }

        public string UserId { get; set; }
        public string UserName { get; set; }
        public event EventHandler<UploadLogArgs> UploadLogResult;
    }

    public enum LogUploadMode { FAIL = -1, UPLOADING, SUCCESS }

    public class UploadLogArgs : EventArgs
    {
        public string CmdId {get;private set;}
        public LogUploadMode LogUploadMode {get;private set;}
        public UploadLogArgs(string cmdId,LogUploadMode logUploadMode)
        {
            CmdId = cmdId;
            LogUploadMode = logUploadMode;
        }
    }
}
