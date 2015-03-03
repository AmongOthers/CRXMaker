using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Tools;

namespace DesktopAndroid
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class Launcher : Form
    {
        const string INSTALLING_IMAGE_KEY = "INSTALLING";
        const string DOWNLOAD_IMAGE_KEY = "DOWNLOAD";
        ImageList imageList;
        List<AppInfo> appList;
        SQLiteDBHelper sqlHelper;
        ReaderWriterObjectLocker dbLocker;
        string messageHtml;
        Register register;
        Upgrader upgrader;

        public Launcher()
        {
            InitializeComponent();

            this.dbLocker = new ReaderWriterObjectLocker();
            this.sqlHelper = new SQLiteDBHelper("app/app");
            this.appList = new List<AppInfo>();
            using (var reader = new StreamReader("message.html"))
            {
                this.messageHtml = reader.ReadToEnd();
            }

            this.listView.View = View.LargeIcon;
            this.listView.MultiSelect = false;
            this.imageList = new ImageList();
            this.imageList.ImageSize = new Size(44, 44);
            Image installingImage = Image.FromFile("installing.png");
            this.imageList.Images.Add(INSTALLING_IMAGE_KEY, installingImage);
            Image downloadImage = Image.FromFile("download.png");
            this.imageList.Images.Add(DOWNLOAD_IMAGE_KEY, downloadImage);
            this.listView.LargeImageList = this.imageList;
            //设置这个避免ListView图像模糊(blur)
            this.listView.LargeImageList.ColorDepth = ColorDepth.Depth32Bit;

            var downloadItem = this.listView.Items.Add("下载更多");
            downloadItem.Tag = "download";
            downloadItem.ImageKey = DOWNLOAD_IMAGE_KEY;

            loadApps();
        }

        private void loadApps()
        {
            var sql = "select * from app";
            using (this.dbLocker.ReadLock())
            {
                using (SQLiteDataReader dr = sqlHelper.ExecuteReader(sql))
                {
                    while (dr.Read())
                    {
                        AppInfo app = new AppInfo
                        {
                            AppName = dr["appname"].ToString(),
                            PackageName = dr["packagename"].ToString(),
                            RelativePath = dr["relativepath"].ToString(),
                            IconPath = dr["iconpath"].ToString()
                        };
                        this.appList.Add(app);
                    }
                }
            }

            this.listView.BeginUpdate();
            foreach (var app in this.appList)
            {
                try
                {
                    //使用Image.FromFile和using，导致System.Drawing的Exception，不知道为什么
                    using (Stream s = File.Open(app.IconPath, FileMode.Open))
                    {
                        Image icon = Image.FromStream(s);
                        this.imageList.Images.Add(app.AppName, icon);
                    }
                }
                catch (System.IO.IOException)
                {
                    continue;
                }
                

                ListViewItem lvi = this.listView.Items.Add(app.AppName);
                lvi.Tag = app;
                lvi.ImageKey = app.AppName;
            }
            this.listView.EndUpdate();
        }

        private void listView_ItemActivate(object sender, EventArgs e)
        {
            MessageBox.Show(this.listView.SelectedItems[0].ImageKey);
        }

        private void listView_MouseClick(object sender, MouseEventArgs e)
        {
            ListViewItem item = this.listView.SelectedItems[0];
            if (e.Button == MouseButtons.Right)
            {
                this.contextMenuStrip1.Show(this.listView, new Point(e.X, e.Y));
            }
        }
        private void listView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.register.RegisterValue.IsLimited)
            {
                showMessage("试用已结束，请升级到正式版");
                return;
            }
            ListViewItem item = this.listView.SelectedItems[0];
            if (e.Button == MouseButtons.Left)
            {
                if (item.Tag is AppInfo)
                {
                    var appInfo = (AppInfo)item.Tag;
                    var appPath = String.Format("..\\..\\{0}\\{1}", appInfo.RelativePath, appInfo.PackageName);
                    var args = String.Format("--user-data-dir=\"..\\data\" --load-extension=\"..\\..\\archon\" --load-and-launch-app=\"{0}\" --silent-launch", appPath);
                    CommandHelper.excute(@"Chrome\\DAEngine.exe", args, false);
                }
                else
                {
                    Market market = new Market(this);
                    market.Show();
                }
            }
        }

        private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            var item = this.listView.SelectedItems[0];
            this.listView.Items.Remove(item);
            uninstallAsync(item);
        }

        private void listView_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void listView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void listView_DragDrop(object sender, DragEventArgs e)
        {
            object data = e.Data.GetData(DataFormats.FileDrop);
            string path = ((string[])data)[0];
            if (!path.EndsWith(".dacrx"))
            {
                return;
            }
            string appName = Path.GetFileNameWithoutExtension(path);
            foreach (var app in this.appList)
            {
                if (app.AppName.Equals(appName))
                {
                    var message = String.Format("已安装 {0}", appName);
                    showMessage(message);
                    return;
                }
            }
            //TODO 判断是否已经安装
            ListViewItem installingItem = this.listView.Items.Add(String.Format("等待 \"{0}\"", appName));
            installingItem.ImageKey = INSTALLING_IMAGE_KEY;
            installAsync(installingItem, path);
        }

        private void showMessage(string content)
        {
            var message = this.messageHtml.Replace("{0}", content);
            this.webBrowser.DocumentText = message;
            this.webBrowser.Visible = true;
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Thread.Sleep(2000);
                this.Invoke(new MethodInvoker(() =>
                {
                    this.webBrowser.Visible = false;
                }));
            });
        }

        private void uninstallAsync(ListViewItem item)
        {
            var appInfo = (AppInfo)(item.Tag);
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Directory.Delete(appInfo.RelativePath, true);
                using (this.dbLocker.WriteLock())
                {
                    var sql = String.Format("delete from app where appname == \"{0}\"", appInfo.AppName);
                    this.sqlHelper.ExecuteNonQuery(sql);
                }
                this.appList.Remove(appInfo);
            });
        }

        private void installAsync(ListViewItem item, string path)
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                var appName = Path.GetFileNameWithoutExtension(path);
                var fileName = Path.GetFileName(path);
                var dstPath = String.Format("app/{0}", fileName);
                var relativePath = String.Format("app/{0}", appName);
                File.Copy(path, dstPath, true);
                Zip.unzipAsDir(dstPath, relativePath);
                File.Delete(dstPath);
                var packageName = Path.GetFileName(Directory.GetDirectories(relativePath)[0]);
                bool isDuplicat = false;
                foreach (var app in this.appList)
                {
                    if (app.PackageName.Equals(packageName))
                    {
                        isDuplicat = true;
                        break;
                    }
                }
                if (isDuplicat)
                {
                    Directory.Delete(relativePath, true);
                    this.Invoke(new MethodInvoker(() =>
                    {
                        this.listView.Items.Remove(item);
                        showMessage("正式版才支持安装同一应用的不同版本");
                    }));
                    return;
                }
                var iconPath = String.Format("{0}/{1}/icon.png", relativePath, packageName);

                var appInfo = new AppInfo {
                    AppName = appName,
                    PackageName = packageName,
                    RelativePath = relativePath,
                    IconPath = iconPath 
                };
                this.appList.Add(appInfo);
                using (this.dbLocker.WriteLock())
                {
                    var sql = String.Format("insert into app(appname,packagename,relativepath,iconpath) values(\"{0}\",\"{1}\",\"{2}\",\"{3}\")", 
                        appName, packageName, relativePath, iconPath);
                    this.sqlHelper.ExecuteNonQuery(sql);
                }
                this.Invoke(new MethodInvoker(() =>
                {
                    using (Stream s = File.Open(iconPath, FileMode.Open))
                    {
                        Image icon = Image.FromStream(s);
                        this.imageList.Images.Add(appName, icon);
                    }
                    item.ImageKey = appName;
                    item.Text = appName;
                    item.Tag = appInfo;
                }));
            });
        }


        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        private void Form1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void upgrade_Click(object sender, EventArgs e)
        {
            this.register.RegisterNow();
            if (this.register.IsPro)
            {
                showMessage("已升级为正式版");
                this.upgradeLabel.Visible = false;
            }
        }

        private void Launcher_Load(object sender, EventArgs e)
        {
            upgrader = new Upgrader();
            upgrader.Init();
        }

        private void Launcher_Shown(object sender, EventArgs e)
        {
            register = new Register();
            register.Init();
            if (!register.IsPro)
            {
                if (!String.IsNullOrEmpty(register.RegisterValue.Message))
                {
                    showMessage(register.RegisterValue.Message);
                }
                this.upgradeLabel.Visible = true;
            }
        }

    }
}
