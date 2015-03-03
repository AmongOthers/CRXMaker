using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Launcher
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Thread.Sleep(2 * 1000);
                Tools.CommandHelper.excute(ConfigurationManager.AppSettings["main"], "", false);
                Environment.Exit(0);
            });
        }
    }
}
