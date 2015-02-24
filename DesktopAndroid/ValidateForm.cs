using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DesktopAndroid
{
    public partial class ValidateForm : Form
    {
        public event Action RetryClick;

        public ValidateForm()
        {
            InitializeComponent();
        }

        private void label2_Click(object sender, EventArgs e)
        {
            if (RetryClick != null)
            {
                RetryClick();
            }
        }

        public void MyShow(string message, bool retryVisible)
        {
            this.label1.Text = message;
            this.label2.Visible = retryVisible;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

    }
}
