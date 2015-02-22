using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    public class AndroidDaemonConfig
    {
        //通用守护端命令及包名
        private const string FBS_ACTIVITY_TY = "FBS_ACTIVITY_TY_2";
        private const string FBS_START_SERVER_TY = "FBS_START_SERVER_TY";
        private const string DAEMON_PACKAGE_NAME_TY = "com.phone580.connect";
        //MM守护端命令及包名
        private const string FBS_ACTIVITY = "FBS_ACTIVITY_2";
        private const string FBS_START_SERVER = "FBS_START_SERVER";
        private const string DAEMON_PACKAGE_NAME = "com.phone580.service";
        //当前使用的守护端命令
        private static string sCurActivityCmd;
        private static string sCurStartServerCmd;
        private static string sCurDaemonPackageName;

        public enum DaemonType
        {
            /// <summary>
            /// 通用守护端
            /// </summary>
            GeneralDaemon,
            /// <summary>
            /// MM守护端
            /// </summary>
            MMDaemon,
            /// <summary>
            /// 蜂助手守护端
            /// </summary>
            FZSDaemon
        }

        public static void Init(DaemonType type)
        {
            switch (type)
            {
                case DaemonType.GeneralDaemon:
                    sCurActivityCmd = FBS_ACTIVITY_TY;
                    sCurStartServerCmd = FBS_START_SERVER_TY;
                    sCurDaemonPackageName = DAEMON_PACKAGE_NAME_TY;
                    break;
                case DaemonType.MMDaemon:
                case DaemonType.FZSDaemon:
                    sCurActivityCmd = FBS_ACTIVITY;
                    sCurStartServerCmd = FBS_START_SERVER;
                    sCurDaemonPackageName = DAEMON_PACKAGE_NAME;
                    break;
                default:
                    Logger.Logger.GetLogger("AndroidDaemonConfig").Error("没有对所有守护端类型进行初始化调用");
                    throw new Exception("没有对所有守护端类型进行初始化调用");
            }
        }

        public static string GetActivityCmd()
        {
            if (String.IsNullOrEmpty(sCurActivityCmd))
            {
                Logger.Logger.GetLogger("AndroidDaemonConfig").Error("没有调用Init方法");
                throw new Exception("没有调用Init方法");
            }
            return sCurActivityCmd;
        }

        public static string GetStartServerCmd()
        {
            if (String.IsNullOrEmpty(sCurStartServerCmd))
            {
                Logger.Logger.GetLogger("AndroidDaemonConfig").Error("没有调用Init方法");
                throw new Exception("没有调用Init方法");
            }
            return sCurStartServerCmd;
        }

        public static string GetDaemonPackageName()
        {
            if (String.IsNullOrEmpty(sCurDaemonPackageName))
            {
                Logger.Logger.GetLogger("AndroidDaemonConfig").Error("没有调用Init方法");
                throw new Exception("没有调用Init方法");
            }
            return sCurDaemonPackageName;
        }




        private static string sBroadcastTarget = "ChannelMobileService";
        public static void SetBroadcastTarget(string target)
        {
            sBroadcastTarget = target;
        }
        public static string GetBroadcastTarget()
        {
            return sBroadcastTarget;
        }
    }
}
