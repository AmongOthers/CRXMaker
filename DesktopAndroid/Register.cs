using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Management;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;


namespace DesktopAndroid
{
    public class Register
    {
        static byte[] key = { 19, 62, 52, 151, 89, 238, 198, 204 };
        static byte[] iv = { 43, 134, 22, 227, 186, 10, 193, 127 };
        EnciphermentUtils enciphermentUtil = new EnciphermentUtils(key, iv);

        //const string URI = "http://localhost:4639/api/app";
        string URI = String.Format("{0}/api/app", ConfigurationManager.AppSettings["server"]);

        public bool IsPro
        {
            get
            {
                return this.RegisterValue != null && !String.IsNullOrEmpty(this.RegisterValue.KeyCode);
            }
        }
        public RegisterValue RegisterValue { get; set; }

        ValidateForm validateForm;
        RegisterForm registerForm;
        bool isValidateFormShown;

        public Register()
        {
            validateForm = new ValidateForm();
            validateForm.RetryClick += validateForm_RetryClick;
            registerForm = new RegisterForm();
            registerForm.RetryClick += registerForm_RetryClick;
        }

        void registerForm_RetryClick()
        {
            doRegister();
        }


        public void Init()
        {
            var path = getKeyStorePath();
            if(!validateLocal(path))
            {
                doInit();
            }
        }

        private static string getKeyStorePath()
        {
            var path = Path.Combine(Directory.GetParent(Application.LocalUserAppDataPath).FullName, "keystore");
            return path;
        }


        public void RegisterNow()
        {
            registerForm.MyShow("");
            registerForm.ShowDialog();
        }

        private bool validateLocal(string path)
        {
            try
            {
                using (var reader = new StreamReader(path))
                {
                    var content = reader.ReadToEnd();
                    content = this.enciphermentUtil.decStringPlusBase64(content);
                    var registerValue = Newtonsoft.Json.JsonConvert.DeserializeObject<RegisterValue>(content);
                    if (registerValue.MachineId == this.GetMachineId() && !String.IsNullOrEmpty(registerValue.KeyCode))
                    {
                        this.RegisterValue = registerValue;
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void doInit()
        {
            validateForm.MyShow("正在和服务器通信...", false);
            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    connectServer();
                    validateForm.Invoke(new MethodInvoker(() =>
                    {
                        validateForm.Close();
                    }));
                }
                catch (Exception e)
                {
                    validateForm.Invoke(new MethodInvoker(() =>
                    {
                        validateForm.MyShow("通信错误", true);
                    }));
                }
            });
            if (!isValidateFormShown)
            {
                isValidateFormShown = true;
                validateForm.ShowDialog();
            }
        }

        private void doRegister()
        {
            var keycode = this.registerForm.KeyCode;
            if (!validateKeycode(keycode))
            {
                this.registerForm.MyShow("请输入有效的注册码");
            }
            this.registerForm.MyShow("正在注册...");
            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    connectServer(keycode);
                    if (!String.IsNullOrEmpty(this.RegisterValue.KeyCode))
                    {
                        var content = Newtonsoft.Json.JsonConvert.SerializeObject(this.RegisterValue);
                        content = this.enciphermentUtil.encStringPlusBase64(content);
                        var path = getKeyStorePath();
                        using (var writer = new StreamWriter(path))
                        {
                            writer.Write(content);
                        }

                        registerForm.Invoke(new MethodInvoker(() => {
                            registerForm.Close();
                        }));
                    }
                    else
                    {
                        registerForm.Invoke(new MethodInvoker(() =>
                        {
                            registerForm.MyShow(this.RegisterValue.Message);
                        }));
                    }
                }
                catch(Exception ex)
                {
                    registerForm.Invoke(new MethodInvoker(() =>
                    {
                        registerForm.MyShow("通信错误，请重试");
                    }));
                }
            });
        }

        private bool validateKeycode(string keycode)
        {
            if (keycode.Length == 8)
            {
                Regex regex = new Regex("[0-9a-z]{8}");
                if(regex.IsMatch(keycode))
                {
                    return true;
                }
            }
            return false;
        }

        void validateForm_RetryClick()
        {
            doInit();
        }

        private void connectServer(string keycode = "")
        {
            var req = HttpWebRequest.Create(URI);
            req.Method = "POST";
            req.ContentType = "text/json";
            var registerInfo = new RegisterInfo
            {
                MachineId = GetMachineId(),
                KeyCode = keycode,
                Random = Guid.NewGuid().ToString()
            };
            req.Timeout = 30 * 1000;
            var content = Newtonsoft.Json.JsonConvert.SerializeObject(registerInfo);
            content = this.enciphermentUtil.encStringPlusBase64(content);
            var body = new Message { Content = content };
            var bodyContent = Newtonsoft.Json.JsonConvert.SerializeObject(body);
            var data = Encoding.UTF8.GetBytes(bodyContent);
            using (var stream = req.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var rsp = req.GetResponse() as HttpWebResponse;
            using (var stream = rsp.GetResponseStream())
            {
                using (var reader = new StreamReader(stream))
                {
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
