using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    /// <summary>
    /// 客户端类型 = 客户端ID
    /// </summary>
    public enum ClientTypeMode
    {
        NONE = -1,
        FZSPC = 1,
        XZPC = 3,
        FZSCM = 5,
        MMCM = 6,
        PLPC = 7,
        G3PC = 8,
        SCPC = 9,
        LTPC = 10,
        LLPC = 12,
        WOPC = 23,
        MMPC = 24,
        _360PC = 25, // 客户端类型:360PC
        MDPC = 26,//门店助手
        ZXPC = 29,
        YJPC = 37,
        MDPC2 = 45,
        MMIOS = 44//苹果助手
    }

    public class ClientTypeTool
    {
        private ClientTypeTool()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientType"></param>
        /// <exception cref="InvalidCastClientTypeException"></exception>
        /// <returns></returns>
        public static ClientTypeMode ParseClientType(string clientType)
        {
            ClientTypeMode mode = ClientTypeMode.NONE;
            try
            {
                //枚举不能定义以数字开头的360pc，所以定义成PC360
                if (String.Compare(clientType, "360pc", true) == 0)
                {
                    clientType = "_360PC";
                }
                mode = (ClientTypeMode)Enum.Parse(typeof(ClientTypeMode), clientType, true);
                return mode;
            }
            catch (ArgumentNullException ex)
            {
                string errorMsg = "无效参数，当前参数为：" + clientType;
                //Logger.Logger.GetLogger(typeof(ClientTypeTool).Name).Error(errorMsg, ex);
                throw new InvalidCastClientTypeException(errorMsg, ex);
            }
            catch (ArgumentException ex)
            {
                string errorMsg = "客户端类型clientType是空字符串 (\"\") 或只包含空白或clientType:" + clientType + "未定义，请检查Configuration.xml配置文件";
                //Logger.Logger.GetLogger(typeof(ClientTypeTool).Name).Error(errorMsg, ex);
                throw new InvalidCastClientTypeException(errorMsg, ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientType"></param>
        /// <exception cref="InvalidCastClientTypeException"></exception>
        /// <returns></returns>
        public static string ClientTypeIdToClientType(ClientTypeMode clientTypeId)
        {
            try
            {
                string clientType = clientTypeId.ToString();
                if (String.Compare(clientType, "_360pc", true) == 0)
                {
                    clientType = "360PC";
                }
                return clientType;
            }
            catch (Exception ex)
            {
                string errorMsg = "客户端类型ID转换成客户端类型出错";
                //Logger.Logger.GetLogger(typeof(ClientTypeTool).Name).Error(errorMsg, ex);
                throw new InvalidCastClientTypeException(errorMsg, ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientType"></param>
        /// <exception cref="InvalidCastClientTypeException"></exception>
        /// <returns></returns>
        public static string ClientTypeToClientTypeId(string clientType)
        {
            int clientTypeId = (int)ParseClientType(clientType);
            return clientTypeId.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientType"></param>
        /// <exception cref="NotFoundClientTypeException"></exception>
        /// <returns>返回工单数据库路径</returns>
        public static string GetOrderDatabaseDirPath(string clientType)
        {
            string orderDirName = string.Empty;
            switch (clientType)
            {
                case "FZSCM":
                    orderDirName = "newFBSCM";
                    break;
                case "SCPC":
                    orderDirName = "newSCYD";
                    break;
                case "MMCM":
                    orderDirName = "MM";
                    break;
                case "PLPC":
                    orderDirName = "PLCL";
                    break;
                case "G3PC":
                    orderDirName = "4S";
                    break;
                case "LTPC":
                    orderDirName = "LT3G";
                    break;
                case "XZPC":
                    orderDirName = "XZG3";
                    break;
                case "FZSPC":
                    orderDirName = "FBS";
                    break;
                case "LLPC":
                    orderDirName = "LLKJ";
                    break;
                case "WOPC":
                    orderDirName = "WOKZ";
                    break;
                case "MMPC":
                    orderDirName = "MMPC";
                    break;
                case "360PC":
                    orderDirName = "360PC";
                    break;
                case "MDPC":
                    orderDirName = "MDPC";
                    break;
                case "ZXPC":
                    orderDirName = "ZXPC";
                    break;
                case "YJPC":
                    orderDirName = "YJPC";
                    break;
                case "MDPC2":
                    orderDirName = "MDPC2";
                    break;
                case "MMIOS":
                    orderDirName = "MMIOS";
                    break;
                default:
                    string errorMsg = "客户端类型不在定义范围内，此客户端类型为：" + clientType;
                    Logger.Logger.GetLogger(typeof(ClientTypeTool)).Error(errorMsg);
                    throw new NotFoundClientTypeException(errorMsg);
            }
            orderDirName += "工单勿删";
            //获取我的文档目录
            string path = Tools.CommonHelper.GetMyDocumentPathOrCurrentDrivePath();
            //拼接工单数据库路径
            string orderDatabaseDirPath = System.IO.Path.Combine(path, orderDirName);
            return orderDatabaseDirPath;
        }
    }

    public class InvalidCastClientTypeException : Exception
    {
        public InvalidCastClientTypeException(string errorMsg)
            : base(errorMsg)
        {
        }

        public InvalidCastClientTypeException(string errorMsg, Exception ex)
            : base(errorMsg, ex)
        {
        }
    }

    public class NotFoundClientTypeException : Exception
    {
        public NotFoundClientTypeException(string errorMsg)
            : base(errorMsg)
        {
        }
    }
}
