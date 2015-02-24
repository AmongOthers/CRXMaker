using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace DesktopAndroid
{
    public class Register
    {
        static byte[] key = { 19, 62, 52, 151, 89, 238, 198, 204 };
        static byte[] iv = { 43, 134, 22, 227, 186, 10, 193, 127 };
        EnciphermentUtils enciphermentUtil = new EnciphermentUtils(key, iv);

        const string URI = "http://localhost:4639/api/app";

        public bool IsPro { get; set; }
        public RegisterValue RegisterValue { get; set; }

        public void Init()
        {
            var path = Path.Combine(Directory.GetParent(Application.LocalUserAppDataPath).FullName, "keystore");
            if(validate(path))
            {
            }
            else
            {
                var form = new ValidateForm();
                ThreadPool.QueueUserWorkItem((o) =>
                {
                    var req = HttpWebRequest.Create(URI);
                    req.Method = "POST";
                    req.ContentType = "text/json";
                    var registerInfo = new RegisterInfo {
                        MachineId = GetMachineId(),
                        KeyCode = "hello?",
                        Random = Guid.NewGuid().ToString()
                    };
                    var content = Newtonsoft.Json.JsonConvert.SerializeObject(registerInfo);
                    content = this.enciphermentUtil.encStringPlusBase64(content);
                    var body = new Message { Content = content };
                    var bodyContent = Newtonsoft.Json.JsonConvert.SerializeObject(body);
                    var data = Encoding.UTF8.GetBytes(bodyContent);
                    using(var stream = req.GetRequestStream()) {
                        stream.Write(data, 0, data.Length);
                    }
                    var rsp = req.GetResponse() as HttpWebResponse;
                    using(var stream = rsp.GetResponseStream()) {
                        using(var reader = new StreamReader(stream)) {
                            var rspContent = reader.ReadToEnd();
                            var message = Newtonsoft.Json.JsonConvert.DeserializeObject<Message>(rspContent);
                            var messageContent = this.enciphermentUtil.decStringPlusBase64(message.Content);
                            this.RegisterValue = Newtonsoft.Json.JsonConvert.DeserializeObject<RegisterValue>(messageContent);
                        }
                    }
                    //和服务器通信的时候这个值应该一致
                    if (this.RegisterValue.Random != registerInfo.Random)
                    {
                        this.RegisterValue.IsLimited = true;
                    }
                    form.Invoke(new MethodInvoker(() =>
                    {
                        form.Close();
                    }));
                });
                form.ShowDialog();
            }
        }

        private string GetMachineId()
        {
            var machineId = String.Empty;
            var cpuId = String.Empty;
            var boardId = String.Empty;
            //CPU
            try
            {
                System.Management.ManagementClass mc = new ManagementClass("win32_processor");
                ManagementObjectCollection moc = mc.GetInstances();
                if (moc.Count > 0)
                {
                    foreach (var mo in moc)
                    {

                        cpuId = mo["processorid"].ToString();
                        break;
                    }
                }
            }
            catch (Exception)
            {

            }
            machineId += cpuId;
            //主板
            try
            {
                System.Management.ManagementObjectSearcher searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia");
                foreach (var mo in searcher.Get())
                {
                    boardId = mo["SerialNumber"].ToString().Trim();
                }
            }
            catch (Exception)
            {

            }
            machineId += boardId;
            return machineId;
        }

        private bool validate(string path)
        {
            return false;
        }
    }
}
