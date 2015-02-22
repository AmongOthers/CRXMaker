using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Management;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.Threading;
using System.Net;

namespace Tools
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MemoryStatusEx
    {
        public uint dwLenth;
        public uint dwMemoryLoad;
        public ulong ulTotalPhys;
        public ulong ulAvailPhys;
        public ulong ulTotalPageFile;
        public ulong ulAvailPageFile;
        public ulong ulTotalVirtual;
        public ulong ulAvailVirtual;
        public ulong ulAvailExtendedVirtual;
    }

    public enum DeveiceStatus { REGISTER_DEVICE, LISTEN_DEVICE, SUBMIT_DEVICE }

    public class SystemUtils
    {
        private UploadLogManager mUploadLogManager;
        private int mCmdState;
        private string mCmdId;

        private volatile int mInterval = 15 * 60 * 1000;
        private volatile string mDeviceId;
        private readonly string MONITOR_FILEPATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "monitor.fbs");
        private const string ACCEPT_REQUEST_WITH_JSON = "application/json";
        private const long POW_1024_TWO = 1024 * 1024;
        private const long POW_1024_THREE = 1024 * 1024 * 1024;
        public const string FBSPC_WINDOWS = "FBSPC_WINDOWS";//PC版的
        public const string FBSCM_WINDOWS = "FBSCM_WINDOWS";//触摸一体机的
        public const string SERVER_URL_PATH = "http://www.phone580.com:8082/fbsapi/api/jk/"; //"http://192.168.0.47:9001/fbs/api/jk/";

        private static SystemUtils sInstance;
        private static object instanceLock = new object();

        public static SystemUtils GetInstance()
        {
            if (sInstance == null)
            {
                lock (instanceLock)
                {
                    if (sInstance == null)
                    {
                        sInstance = new SystemUtils();
                    }
                }
            }
            return sInstance;
        }

        private SystemUtils()
        {
            mUploadLogManager = UploadLogManager.GetInstance();
            mUploadLogManager.UploadLogResult += OnUploadLogResult;
            CPUPercent.GetCPUPercent();
            MonitorInfo.GetInstance().MacAddress = IPTool.FindMacAddr();
        }

        void OnUploadLogResult(object sender, UploadLogArgs e)
        {
            mCmdId = e.CmdId;
            mCmdState = (int)e.LogUploadMode;
            listenDevice();
        }

        public void SetClientInfo(string clientSystemType, string clientType, string clientTypeId, string cilentVerName, string clientVerCode)
        {
            MonitorInfo.GetInstance().ClientSystemType = clientSystemType;
            MonitorInfo.GetInstance().ClientVerCode = clientVerCode;
            MonitorInfo.GetInstance().ClientType = clientType;
            MonitorInfo.GetInstance().ClientTypeId = clientTypeId;
            MonitorInfo.GetInstance().ClientVerName = cilentVerName;

            if (!String.IsNullOrEmpty(clientVerCode) && !String.IsNullOrEmpty(cilentVerName))
            {
                if (IsDeviceIdEmpty())
                {
                    RegisterDevice();
                }
            }
        }

        public bool IsDeviceIdEmpty()
        {
            bool isEmpty = false;
            if (File.Exists(MONITOR_FILEPATH))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(MONITOR_FILEPATH, Encoding.Default))
                    {
                        string json = sr.ReadToEnd();
                        Dictionary<string, string> dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                        mDeviceId = dict["id"];
                        mInterval = Convert.ToInt32(dict["interval"]);
                        Logger.Logger.GetLogger(this).Debug("读取设备ID成功," + mDeviceId + " 间隔为:" + mInterval);
                        shakeHands();//开启握手计时器
                        timingMonitorDeviceInfo();//开启监控计时器
                    }
                }
                catch (IOException ioEx)
                {
                    Logger.Logger.GetLogger(this).Error("monitor文件读取失败..", ioEx);
                    isEmpty = true;
                }
                catch (Exception ex)
                {
                    Logger.Logger.GetLogger(this).Error("获取设备ID失败..", ex);
                    isEmpty = true;
                }
            }
            else
            {
                isEmpty = true;
            }
            return isEmpty;
        }

        /// <summary>
        /// 注册设备
        /// </summary>
        public void RegisterDevice()
        {
            string path = SERVER_URL_PATH;
            //注册设备,获取所有信息后进行设备注册,只注册一次
            string cpu = CPUPercent.GetCPUPercent().ToString();
            string disk = DiskInfo.AchieveDriveInfo().DiskAvailableSpace.ToString();
            string memory = RAMInfo.AchieveRAMInfo().AvailableMemory.ToString();
            string mac = MonitorInfo.GetInstance().MacAddress;
            string clientSystemType = MonitorInfo.GetInstance().ClientSystemType;
            string clientVerName = MonitorInfo.GetInstance().ClientVerName;
            string clientVerCode = MonitorInfo.GetInstance().ClientVerCode;
            string clientTypeId = MonitorInfo.GetInstance().ClientTypeId;
            //注意事项：
            //URL参数中的“clientType”对应 MonitorInfo.GetInstance().ClientSystemType
            path += string.Format("registerDevice?cpu={0}&disk={1}&memory={2}&mac={3}&clientType={4}&clientVerName={5}&clientVerCode={6}&clientVerId={7}",
                cpu, disk, memory, mac, clientSystemType
                 , clientVerName, clientVerCode, clientTypeId);
            requestServer(path, DeveiceStatus.REGISTER_DEVICE);
        }

        //监听设备
        private void listenDevice()
        {
            string path = SERVER_URL_PATH;
            path += string.Format("deviceShake?id={0}&userId={1}&cmdId={2}&cmdState={3}&clientVerId={4}",
                mDeviceId, MonitorInfo.GetInstance().UserId, mCmdId, mCmdState, MonitorInfo.GetInstance().ClientTypeId);
            requestServer(path, DeveiceStatus.LISTEN_DEVICE);
        }

        //提交设备信息
        private void submitDevice()
        {
            string path = SERVER_URL_PATH;
            //一天只提交一次监控信息到后台
            string userId = MonitorInfo.GetInstance().UserId;
            string cpStaffId = MonitorInfo.GetInstance().CpStaffId;
            string channelId = MonitorInfo.GetInstance().ChannelId;
            string regionId = MonitorInfo.GetInstance().RegionId;
            string cpu = mLastCpuPercent.ToString();
            string disk = mLastDiskSize.ToString();
            string memory = mLastMemorySize.ToString();
            string mac = MonitorInfo.GetInstance().MacAddress;
            string id = mDeviceId;
            string clientVerId = MonitorInfo.GetInstance().ClientTypeId;

            path += string.Format("deviceShake?userId={0}&cpStaffId={1}&channelId={2}&regionId={3}&cpu={4}&disk={5}" +
                   "&memory={6}&mac={7}&id={8}&clientVerId={9}", userId, cpStaffId, channelId, regionId, cpu, disk,
                    memory, mac, mDeviceId, clientVerId);
            requestServer(path, DeveiceStatus.SUBMIT_DEVICE);
        }

        #region 跟后台握手操作
        private System.Timers.Timer mShakeHandsTimer = null;
        private bool mIsShakeHandsWork = true;
        //TODO 隔段时间去握手一次
        private void shakeHands()
        {
            if (mShakeHandsTimer == null)
            {
                mShakeHandsTimer = new System.Timers.Timer();
#if interval
                mShakeHandsTimer.Interval = 3000;//间隔mInterval毫秒触发
#else
                mShakeHandsTimer.Interval = mInterval;//间隔mInterval毫秒触发
#endif
                mShakeHandsTimer.Elapsed += (sender, eventArgs) =>
                {
                    if (mIsShakeHandsWork)
                    {
                        listenDevice();
                    }
                };
                mShakeHandsTimer.Enabled = true;
                mShakeHandsTimer.Start();
                Logger.Logger.GetLogger(this).Info("开启握手计时器成功");
            }
        }

        private void stopShakeHands()
        {
            if (mShakeHandsTimer != null)
            {
                mIsShakeHandsWork = false;
                mShakeHandsTimer.Stop();
                mShakeHandsTimer.Enabled = false;
                mShakeHandsTimer = null;
                Logger.Logger.GetLogger(this).Info("停止握手计时器成功");
            }
        }
        #endregion

        #region 监控CPU,内存,硬盘信息
        private long mLastMemorySize = 0;
        private long mLastCpuPercent = 0;
        private long mLastDiskSize = 0;

        private const int SUBMIT_TIME = 16;//设置提交时间为16点

        private System.Timers.Timer mMonitorDeviceInfoTimer = null;
        private bool mIsMonitoring = true;
        private int mMonitorInterval = 1000 * 60 * 10;

        //TODO 隔段时间获取CPU,内存,硬盘(开一个定时器去获得系统信息)
        private void timingMonitorDeviceInfo()
        {
            if (mMonitorDeviceInfoTimer == null)
            {
                mMonitorDeviceInfoTimer = new System.Timers.Timer();
                //用于记录10分钟
                List<long> memorySizeList = new List<long>();
                List<long> cpuPercentList = new List<long>();

                List<long> totalMemorySizeList = new List<long>();
                List<long> totalCpuPercentList = new List<long>();

                int count = 1;
#if interval
                Dictionary<string, string> dict = new Dictionary<string, string>();
                int submitTime = 0;
                try
                {
                    using (StreamReader sr = new StreamReader("interval.txt"))
                    {
                        string content = sr.ReadLine();
                        dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
                        submitTime = Convert.ToInt32(dict["submit"]);
                    }
                    mMonitorDeviceInfoTimer.Interval = Convert.ToInt32(dict["MonitorInterval"]) * 1000;
                }
                catch (Exception e)
                {
                    Logger.Logger.GetLogger("转换时间失败",ex);
                }
#else
                mMonitorDeviceInfoTimer.Interval = mMonitorInterval;//间隔一段时间
#endif
                mMonitorDeviceInfoTimer.Elapsed += (sender, eventArgs) =>
                {
                    if (mIsMonitoring)
                    {
                        //一小时算一次平均的
                        if (count % 6 == 0)
                        {
                            long memorySize = getSizeByList(memorySizeList);
                            long cpuPercent = getSizeByList(cpuPercentList);
                            //1小时算好的几何平均值放在total列表
                            totalMemorySizeList.Add(memorySize);
                            totalCpuPercentList.Add(cpuPercent);
                            //计算完之后就清空列表
                            memorySizeList.Clear();
                            cpuPercentList.Clear();
                        }
                        MemorySize memory = RAMInfo.AchieveRAMInfo();
                        DiskSize disk = DiskInfo.AchieveDriveInfo();
                        //使用几何平均数求值
                        memorySizeList.Add(memory.AvailableMemory);
                        cpuPercentList.Add(CPUPercent.GetCPUPercent());
                        mLastDiskSize = disk.DiskAvailableSpace;
#if interval
                        if (DateTime.Now.Hour == submitTime)
#else
                        //如果当前时间等于要提交的那个时间,就提交监控信息
                        if (DateTime.Now.Hour == SUBMIT_TIME)
#endif
                        {
                            if (totalCpuPercentList.Count == 0 && totalMemorySizeList.Count == 0)
                            {
                                mLastMemorySize = RAMInfo.AchieveRAMInfo().AvailableMemory;
                                mLastCpuPercent = CPUPercent.GetCPUPercent();
                            }
                            else
                            {
                                mLastMemorySize = getSizeByList(totalMemorySizeList);
                                mLastCpuPercent = getSizeByList(totalCpuPercentList);
                            }
                            Logger.Logger.GetLogger(this).Debug("內存:" + mLastMemorySize + "M|硬盘大小:" + mLastDiskSize + "G|CPU占用:" + mLastCpuPercent + "%");
                            submitDevice();
                            //TODO 提交完监控信息后把list清空
                            totalCpuPercentList.Clear();
                            totalMemorySizeList.Clear();
                            count = 1;

                            mIsMonitoring = false;
                            Logger.Logger.GetLogger(this).Debug("暂停监控信息计时器success");
                            mIsShakeHandsWork = false;
                            mShakeHandsTimer.Enabled = false;
                            Logger.Logger.GetLogger(this).Debug("暂停握手计时器success");
                        }
                        count++;
                    }
                    else
                    {
                        //当用户没有关机的时候,设定10点的时候
                        if (DateTime.Now.Hour == 10)
                        {
                            mIsMonitoring = true;
                            mIsShakeHandsWork = true;
                            mShakeHandsTimer.Enabled = true;
                        }
                    }
                };
                mMonitorDeviceInfoTimer.Enabled = true;
                mMonitorDeviceInfoTimer.Start();
                Logger.Logger.GetLogger(this).Info("开启监控设备计时器成功");
            }
        }

        private void stopMonitorDevice()
        {
            if (mMonitorDeviceInfoTimer != null)
            {
                mIsMonitoring = false;
                mMonitorDeviceInfoTimer.Stop();
                mMonitorDeviceInfoTimer.Enabled = false;
                mMonitorDeviceInfoTimer = null;
                Logger.Logger.GetLogger(this).Info("停止监控计时器成功");
            }
        }
        #endregion

        /// <summary>
        /// 请求服务器返回信息
        /// </summary>
        /// <param name="urlPath">请求地址</param>
        /// <param name="type">请求的类型</param>
        private void requestServer(string urlPath, DeveiceStatus status)
        {
            HttpRequestTool request = new HttpRequestTool(urlPath, (result, error) =>
            {
                //没有错误信息,表明返回信息成功
                if (error == null)
                {
                    switch (status)
                    {
                        case DeveiceStatus.REGISTER_DEVICE://注册设备
                            try
                            {
                                var registerDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(result);
                                if (registerDict != null && !String.IsNullOrEmpty(registerDict["id"]))
                                {
                                    mDeviceId = registerDict["id"];
                                    mInterval = Int32.Parse(registerDict["interval"]);
                                    Logger.Logger.GetLogger(this).Info("注册成功,deviceId: " + mDeviceId);
                                    writeInfo2File();
                                    Logger.Logger.GetLogger(this).Info("设备ID: " + mDeviceId + " 写入本地文件成功...");
                                    timingMonitorDeviceInfo();//开启监控线程
                                    shakeHands();//开启握手计时器   
                                }
                                else
                                {
                                    Logger.Logger.GetLogger(this).Info("服务器返回的deviceId为空");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Logger.GetLogger(this).Error("“注册设备”时，处理服务器返回的值时出错，值为：" + result, ex);
                            }
                            break;
                        case DeveiceStatus.LISTEN_DEVICE://设备跟后台握手
                        case DeveiceStatus.SUBMIT_DEVICE: //提交监控信息
                            try
                            {
                                var deviceDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(result);
                                if (deviceDict != null && deviceDict["success"].ToString() == "1")
                                {
                                    if (status == DeveiceStatus.LISTEN_DEVICE)
                                    {
                                        Logger.Logger.GetLogger(this).Info(mDeviceId + " 握手成功");
                                    }
                                    else if (status == DeveiceStatus.LISTEN_DEVICE)
                                    {
                                        Logger.Logger.GetLogger(this).Info(mDeviceId + " 提交监控信息成功");
                                    }

                                    var cmd = deviceDict["cmd"];
                                    if (cmd == null)
                                    {
                                        mCmdId = String.Empty;
                                        return;
                                    }

                                    mUploadLogManager.ClientType = MonitorInfo.GetInstance().ClientType;
                                    mUploadLogManager.UserId = MonitorInfo.GetInstance().UserId;
                                    mUploadLogManager.UserName = String.Format("{0}-{1}", MonitorInfo.GetInstance().UserId, MonitorInfo.GetInstance().UserName);
                                    mUploadLogManager.AcceptCmd(cmd.ToString());
                                }
                                else//服务器返回success=0时，表示设备ID不存在。这时要重新注册ID
                                {
                                    //先停止，再注册
                                    stopShakeHands();
                                    stopMonitorDevice();
                                    RegisterDevice();
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Logger.GetLogger(this).Error("“设备跟后台握手”或者“提交信息”时，处理服务器返回的值时出错，值为：" + result, ex);
                            }
                            break;
                    }
                }
                else
                {
                    Logger.Logger.GetLogger(this).Error("向服务器请求时出错，错误为：" + error);
                }
            });
            request.AcceptRequest = ACCEPT_REQUEST_WITH_JSON;
            request.Request();
        }

        public void Stop()
        {
            stopShakeHands();
            stopMonitorDevice();
        }

        #region 通用方法
        private long getSizeByList(List<long> list)
        {
            double size = 1d;
            long result = 0;
            list.RemoveAll(
                (item) =>
                {
                    return item == 0;
                });
            foreach (long l in list)
            {
                size *= l * 1.0;
            }
            int count = list.Count;
            result = (long)Math.Round(Math.Pow(size, 1.0 / count));
            return result;
        }

        //获得四舍五入后的内存size
        public static long GetMemorySizeRound(double size, int round)
        {
            long result = 0;
            switch (round)
            {
                case 2:
                    result = (long)Math.Round(size / POW_1024_TWO);
                    break;
                case 3:
                    result = (long)Math.Round(size / POW_1024_THREE);
                    break;
            }
            return result;
        }

        /// <summary>
        /// 写设备ID到本地文本
        /// </summary>
        private void writeInfo2File()
        {
            if (!String.IsNullOrEmpty(mDeviceId) && !String.IsNullOrEmpty(mInterval.ToString()))
            {
                Dictionary<string, string> dataInfo = new Dictionary<string, string>();
                dataInfo.Add("id", mDeviceId);
                dataInfo.Add("interval", mInterval.ToString());
                string json = JsonConvert.SerializeObject(dataInfo);
                try
                {
                    using (StreamWriter sw = new StreamWriter(MONITOR_FILEPATH, false, Encoding.Default))
                    {
                        sw.Write(json);
                        sw.Flush();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Logger.GetLogger(this).Error("写入设备ID到我的文档失败..", ex);
                }
            }
        }
        #endregion
    }

    #region 获取RAM信息
    public class RAMInfo
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern void GlobalMemoryStatusEx(ref MemoryStatusEx memoryEx);

        public static MemorySize AchieveRAMInfo()
        {
            try
            {
                MemoryStatusEx memoryStatusEx = new MemoryStatusEx();
                memoryStatusEx.dwLenth = (uint)Marshal.SizeOf(typeof(MemoryStatusEx));
                GlobalMemoryStatusEx(ref memoryStatusEx);
                MemorySize memory = new MemorySize();
                memory.AvailableMemory = SystemUtils.GetMemorySizeRound(memoryStatusEx.ulAvailPhys, 2);
                memory.TotalMemory = SystemUtils.GetMemorySizeRound(memoryStatusEx.ulTotalPhys, 2);
                return memory;
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(RAMInfo)).Error("获取RAM信息失败", ex);
            }
            return null;
        }
    }

    #endregion

    /// <summary>
    /// 获取硬盘信息
    /// </summary>
    public class DiskInfo
    {
        public static DiskSize AchieveDriveInfo()
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory;
                string drive = path[0].ToString();
                DriveInfo info = new DriveInfo(drive);
                DiskSize disk = new DiskSize();
                disk.DiskAvailableSpace = SystemUtils.GetMemorySizeRound(info.AvailableFreeSpace, 3);
                disk.DiskTotalSpace = SystemUtils.GetMemorySizeRound(info.TotalSize, 3);
                return disk;
            }
            catch (UnauthorizedAccessException unAccessEx)
            {
                Logger.Logger.GetLogger(typeof(DiskInfo)).Error("拒绝访问,获取磁盘信息失败..", unAccessEx);
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(DiskInfo)).Error("获取磁盘信息失败..", ex);
            }
            return null;
        }
    }

    /// <summary>
    /// 获取CPU百分比
    /// </summary>
    public class CPUPercent
    {
        private static PerformanceCounter mPerformanceCounter = null;

        static CPUPercent()
        {
            try
            {
                mPerformanceCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(CPUPercent)).Error("window NT计时器初始化失败", ex);
            }
        }

        public static int GetCPUPercent()
        {
            try
            {
                if (mPerformanceCounter != null)
                {
                    int percent = 0;
                    percent = (int)Math.Round(mPerformanceCounter.NextValue());
                    return percent;
                }
            }
            catch (UnauthorizedAccessException unAccessEx)
            {
                Logger.Logger.GetLogger(typeof(CPUPercent)).Error("拒绝访问,获取CPU百分比失败", unAccessEx);
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(CPUPercent)).Error("获取CPU百分比失败", ex);
            }
            return 0;
        }
    }

    public class MemorySize
    {
        public long AvailableMemory { get; set; }
        public long TotalMemory { get; set; }
    }

    public class DiskSize
    {
        public long DiskAvailableSpace { get; set; }
        public long DiskTotalSpace { get; set; }
    }
}
