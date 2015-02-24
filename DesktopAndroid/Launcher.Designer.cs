namespace DesktopAndroid
{
    partial class Launcher
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Launcher));
            this.listView = new System.Windows.Forms.ListView();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.卸载ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.webBrowser = new System.Windows.Forms.WebBrowser();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.upgradeLabel = new System.Windows.Forms.Label();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // listView
            // 
            this.listView.AllowDrop = true;
            this.listView.BackColor = System.Drawing.SystemColors.InfoText;
            this.listView.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("listView.BackgroundImage")));
            this.listView.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.listView.ForeColor = System.Drawing.SystemColors.Control;
            this.listView.Location = new System.Drawing.Point(0, 45);
            this.listView.Margin = new System.Windows.Forms.Padding(0);
            this.listView.Name = "listView";
            this.listView.Size = new System.Drawing.Size(544, 555);
            this.listView.TabIndex = 0;
            this.listView.UseCompatibleStateImageBehavior = false;
            this.listView.SelectedIndexChanged += new System.EventHandler(this.listView_SelectedIndexChanged);
            this.listView.DragDrop += new System.Windows.Forms.DragEventHandler(this.listView_DragDrop);
            this.listView.DragEnter += new System.Windows.Forms.DragEventHandler(this.listView_DragEnter);
            this.listView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.listView_MouseClick);
            this.listView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listView_MouseDoubleClick);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.卸载ToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(101, 26);
            this.contextMenuStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.contextMenuStrip1_ItemClicked);
            // 
            // 卸载ToolStripMenuItem
            // 
            this.卸载ToolStripMenuItem.Name = "卸载ToolStripMenuItem";
            this.卸载ToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.卸载ToolStripMenuItem.Text = "卸载";
            // 
            // webBrowser
            // 
            this.webBrowser.Location = new System.Drawing.Point(0, 45);
            this.webBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser.Name = "webBrowser";
            this.webBrowser.ScrollBarsEnabled = false;
            this.webBrowser.Size = new System.Drawing.Size(544, 40);
            this.webBrowser.TabIndex = 1;
            this.webBrowser.Visible = false;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox1.BackgroundImage")));
            this.pictureBox1.Location = new System.Drawing.Point(500, 7);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(32, 32);
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
            // 
            // label1
            // 
            this.upgradeLabel.AutoSize = true;
            this.upgradeLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.upgradeLabel.Font = new System.Drawing.Font("宋体", 14.25F, ((System.Drawing.FontStyle)(((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic) 
                | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.upgradeLabel.ForeColor = System.Drawing.SystemColors.Highlight;
            this.upgradeLabel.Location = new System.Drawing.Point(12, 9);
            this.upgradeLabel.Name = "label1";
            this.upgradeLabel.Size = new System.Drawing.Size(129, 19);
            this.upgradeLabel.TabIndex = 4;
            this.upgradeLabel.Text = "升级到正式版";
            this.upgradeLabel.Visible = false;
            this.upgradeLabel.Click += new System.EventHandler(this.label1_Click);
            // 
            // Launcher
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(544, 600);
            this.Controls.Add(this.upgradeLabel);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.webBrowser);
            this.Controls.Add(this.listView);
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Launcher";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "启动器";
            this.Load += new System.EventHandler(this.Launcher_Load);
            this.Shown += new System.EventHandler(this.Launcher_Shown);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Form1_MouseDown);
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView listView;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 卸载ToolStripMenuItem;
        private System.Windows.Forms.WebBrowser webBrowser;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label upgradeLabel;

    }
}

