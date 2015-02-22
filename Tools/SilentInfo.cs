using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Tools
{
    [Serializable]
    public class SilentInfo : ISerializable
    {
        /// <summary>
        /// 是否只下载
        /// </summary>
        public bool IsOnlyDownload { get; set; }

        /// <summary>
        /// 是否提交工单
        /// </summary>
        public bool IsSubmitOrder { get; set; }

        /// <summary>
        /// 是否要安装该应用
        /// </summary>
        public bool IsInstall { get; set; }

        public SilentInfo()
        {
            IsOnlyDownload = false;
            IsSubmitOrder = true;
            IsInstall = true;
        }

        /// <summary>
        /// 用于解决混淆后，反射的问题。（增加新成员后，要为新成员写相应的操作）
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected SilentInfo(SerializationInfo info, StreamingContext context)
        {
            IsOnlyDownload = getValue<bool>(info, "IsOnlyDownload");
            IsSubmitOrder = getValue<bool>(info, "IsSubmitOrder");
            IsInstall = getValue<bool>(info, "IsInstall");
        }

        /// <summary>
        /// 用于解决混淆后，反射的问题。（增加新成员后，要为新成员写相应的操作）
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            addValue(info, "IsOnlyDownload", IsOnlyDownload, typeof(bool));
            addValue(info, "IsSubmitOrder", IsSubmitOrder, typeof(bool));
            addValue(info, "IsInstall", IsInstall, typeof(bool));
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
