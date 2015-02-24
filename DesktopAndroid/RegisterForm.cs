using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DesktopAndroid
{
    public partial class RegisterForm : Form
    {
        public event Action RetryClick;
        public string KeyCode
        {
            get
            {
                return this.codeTextBox.Text;
            }
        }

        public RegisterForm()
        {
            InitializeComponent();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public void MyShow(string message)
        {
            this.messageLabel.Text = message;
        }

        private void errorLabel_Click(object sender, EventArgs e)
        {
            if(this.RetryClick != null) {
                this.RetryClick();
            }
        }

        private void codeTextBox_TextChanged(object sender, EventArgs e)
        {

        }

    }
}
