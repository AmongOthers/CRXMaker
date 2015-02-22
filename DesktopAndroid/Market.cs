using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Windows.Forms;

namespace DesktopAndroid
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class Market : Form
    {
        Launcher launcher;
        public Market(Launcher launcher)
        {
            this.launcher = launcher;
            InitializeComponent();
        }

        public Market()
        {
            InitializeComponent();
        }
    }
}
