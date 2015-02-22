using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tools
{
    public class DetectIphoneComponent
    {
        [Flags]
        public enum INSTALL_PLUGS
        {
            ALLOK = 0,
            APPLICATION_NOT_SUPPORT = 1,
            ITUNES_MOBILE_DEVICE_NOT_SUPPORT = 2,
            BOTH = 3
        }

        [Flags]
        public enum IPHONE_SUPPORT_VERSION
        {
            ALLOK = 0,
            APPLICATION_IS_OLD_VERSION = 1,
            ITUNES_MOBILE_DEVICE_IS_OLD_VERSION = 2,
            BOTH = 3
        }

        static DetectIphoneComponent _instance = null;
        static object mLock = new object();
        public static DetectIphoneComponent GetInstance()
        {
            if (_instance == null)
            {
                lock (mLock)
                {
                    if (_instance == null)
                    {
                        _instance = new DetectIphoneComponent();
                    }
                }
            }
            return _instance;
        }

        AppleApplicationSupport mAppleApplicationSupport;
        AppleMobileDeviceSupport mAppleMobileDeviceSupport;

        public DetectIphoneComponent()
        {
            mAppleApplicationSupport = new AppleApplicationSupport();
            mAppleMobileDeviceSupport = new AppleMobileDeviceSupport();
        }

        public INSTALL_PLUGS CheckIphoneComponentSupport()
        {
            var installPlugsMode = INSTALL_PLUGS.ALLOK;

            //检测 Apple Application Support
            if (!mAppleApplicationSupport.DetectIphoneSupport())
            {
                installPlugsMode = INSTALL_PLUGS.APPLICATION_NOT_SUPPORT;
            }

            //检测 iTunesMobileDeviceDLL
            if (!mAppleMobileDeviceSupport.DetectIphoneSupport())
            {
                installPlugsMode = installPlugsMode | INSTALL_PLUGS.ITUNES_MOBILE_DEVICE_NOT_SUPPORT;
            }
            return installPlugsMode;
        }

        public IPHONE_SUPPORT_VERSION CheckIphoneComponentVersion()
        {
            var versionMode = IPHONE_SUPPORT_VERSION.ALLOK;
            if (!mAppleApplicationSupport.IsNewestVersion())
            {
                versionMode = IPHONE_SUPPORT_VERSION.APPLICATION_IS_OLD_VERSION;
            }

            if (!mAppleMobileDeviceSupport.IsNewestVersion())
            {
                versionMode = versionMode | IPHONE_SUPPORT_VERSION.ITUNES_MOBILE_DEVICE_IS_OLD_VERSION;
            }
            return versionMode;
        }
    }
}