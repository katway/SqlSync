namespace SqlSync
{
    partial class Form4
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form4));
            this.btnCopy = new System.Windows.Forms.Button();
            this.stsStatus = new System.Windows.Forms.StatusStrip();
            this.stsTables = new System.Windows.Forms.ToolStripStatusLabel();
            this.stslTable = new System.Windows.Forms.ToolStripStatusLabel();
            this.stslRows = new System.Windows.Forms.ToolStripStatusLabel();
            this.stpProgress = new System.Windows.Forms.ToolStripProgressBar();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.stsStatus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // btnCopy
            // 
            this.btnCopy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCopy.Location = new System.Drawing.Point(548, 48);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.Size = new System.Drawing.Size(104, 30);
            this.btnCopy.TabIndex = 0;
            this.btnCopy.Text = "开始";
            this.btnCopy.UseVisualStyleBackColor = true;
            this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);
            // 
            // stsStatus
            // 
            this.stsStatus.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.stsTables,
            this.stslTable,
            this.stslRows,
            this.stpProgress});
            this.stsStatus.Location = new System.Drawing.Point(0, 394);
            this.stsStatus.Name = "stsStatus";
            this.stsStatus.Size = new System.Drawing.Size(664, 22);
            this.stsStatus.TabIndex = 2;
            this.stsStatus.Text = "stsStatus";
            // 
            // stsTables
            // 
            this.stsTables.Name = "stsTables";
            this.stsTables.Size = new System.Drawing.Size(27, 17);
            this.stsTables.Text = "0/1";
            // 
            // stslTable
            // 
            this.stslTable.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.stslTable.Name = "stslTable";
            this.stslTable.Size = new System.Drawing.Size(32, 17);
            this.stslTable.Text = "表名";
            this.stslTable.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // stslRows
            // 
            this.stslRows.Name = "stslRows";
            this.stslRows.Size = new System.Drawing.Size(73, 17);
            this.stslRows.Text = "当前行/行数";
            // 
            // stpProgress
            // 
            this.stpProgress.Name = "stpProgress";
            this.stpProgress.Size = new System.Drawing.Size(150, 16);
            this.stpProgress.Step = 1;
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.Location = new System.Drawing.Point(0, 84);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtLog.Size = new System.Drawing.Size(664, 307);
            this.txtLog.TabIndex = 3;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(0, 1);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(529, 83);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 4;
            this.pictureBox1.TabStop = false;
            // 
            // Form4
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(664, 416);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.stsStatus);
            this.Controls.Add(this.btnCopy);
            this.Controls.Add(this.pictureBox1);
            this.Name = "Form4";
            this.Text = "SQL > Oracle数据同步工具";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form4_FormClosing);
            this.stsStatus.ResumeLayout(false);
            this.stsStatus.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCopy;
        private System.Windows.Forms.StatusStrip stsStatus;
        private System.Windows.Forms.ToolStripStatusLabel stslTable;
        private System.Windows.Forms.ToolStripProgressBar stpProgress;
        private System.Windows.Forms.ToolStripStatusLabel stsTables;
        private System.Windows.Forms.ToolStripStatusLabel stslRows;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}