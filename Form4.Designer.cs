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
            this.btnStart = new System.Windows.Forms.Button();
            this.stsStatus = new System.Windows.Forms.StatusStrip();
            this.tsslSqlState = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslOracleState = new System.Windows.Forms.ToolStripStatusLabel();
            this.stsTables = new System.Windows.Forms.ToolStripStatusLabel();
            this.stslTable = new System.Windows.Forms.ToolStripStatusLabel();
            this.stslRows = new System.Windows.Forms.ToolStripStatusLabel();
            this.stpProgress = new System.Windows.Forms.ToolStripProgressBar();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnPause = new System.Windows.Forms.Button();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslConfigFile = new System.Windows.Forms.ToolStripStatusLabel();
            this.stsStatus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // btnStart
            // 
            this.btnStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStart.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnStart.Location = new System.Drawing.Point(730, 12);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(104, 30);
            this.btnStart.TabIndex = 0;
            this.btnStart.Text = "开始";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // stsStatus
            // 
            this.stsStatus.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsslSqlState,
            this.tsslOracleState,
            this.stsTables,
            this.stslTable,
            this.stslRows,
            this.stpProgress,
            this.toolStripStatusLabel1,
            this.tsslConfigFile});
            this.stsStatus.Location = new System.Drawing.Point(0, 423);
            this.stsStatus.Name = "stsStatus";
            this.stsStatus.Size = new System.Drawing.Size(846, 26);
            this.stsStatus.TabIndex = 2;
            this.stsStatus.Text = "stsStatus";
            // 
            // tsslSqlState
            // 
            this.tsslSqlState.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.tsslSqlState.BorderStyle = System.Windows.Forms.Border3DStyle.Sunken;
            this.tsslSqlState.Name = "tsslSqlState";
            this.tsslSqlState.Size = new System.Drawing.Size(35, 21);
            this.tsslSqlState.Text = "SQL";
            this.tsslSqlState.ToolTipText = "Sql数据库的连接状态";
            // 
            // tsslOracleState
            // 
            this.tsslOracleState.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.tsslOracleState.BorderStyle = System.Windows.Forms.Border3DStyle.Sunken;
            this.tsslOracleState.Name = "tsslOracleState";
            this.tsslOracleState.Size = new System.Drawing.Size(38, 21);
            this.tsslOracleState.Text = "ORA";
            this.tsslOracleState.ToolTipText = "Oracle数据库的连接状态";
            // 
            // stsTables
            // 
            this.stsTables.Name = "stsTables";
            this.stsTables.Size = new System.Drawing.Size(27, 21);
            this.stsTables.Text = "0/1";
            this.stsTables.ToolTipText = "当前优先级下表数/总表数";
            // 
            // stslTable
            // 
            this.stslTable.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.stslTable.Name = "stslTable";
            this.stslTable.Size = new System.Drawing.Size(32, 21);
            this.stslTable.Text = "表名";
            this.stslTable.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.stslTable.ToolTipText = "当前执行的表名";
            // 
            // stslRows
            // 
            this.stslRows.Name = "stslRows";
            this.stslRows.Size = new System.Drawing.Size(73, 21);
            this.stslRows.Text = "当前行/行数";
            this.stslRows.ToolTipText = "同步表的当前行/总行数";
            // 
            // stpProgress
            // 
            this.stpProgress.Name = "stpProgress";
            this.stpProgress.Size = new System.Drawing.Size(150, 20);
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
            this.txtLog.Size = new System.Drawing.Size(846, 340);
            this.txtLog.TabIndex = 3;
            this.txtLog.DoubleClick += new System.EventHandler(this.txtLog_DoubleClick);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(0, 1);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(711, 83);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 4;
            this.pictureBox1.TabStop = false;
            // 
            // btnStop
            // 
            this.btnStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStop.Enabled = false;
            this.btnStop.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnStop.Location = new System.Drawing.Point(730, 48);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(104, 30);
            this.btnStop.TabIndex = 5;
            this.btnStop.Text = "停止";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // btnPause
            // 
            this.btnPause.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPause.Enabled = false;
            this.btnPause.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPause.Location = new System.Drawing.Point(730, 12);
            this.btnPause.Name = "btnPause";
            this.btnPause.Size = new System.Drawing.Size(104, 30);
            this.btnPause.TabIndex = 6;
            this.btnPause.Tag = "继续";
            this.btnPause.Text = "暂停";
            this.btnPause.UseVisualStyleBackColor = true;
            this.btnPause.Visible = false;
            this.btnPause.Click += new System.EventHandler(this.btnPause_Click);
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(418, 21);
            this.toolStripStatusLabel1.Spring = true;
            // 
            // tsslConfigFile
            // 
            this.tsslConfigFile.Name = "tsslConfigFile";
            this.tsslConfigFile.Size = new System.Drawing.Size(56, 21);
            this.tsslConfigFile.Text = "配置文件";
            this.tsslConfigFile.ToolTipText = "当前正在执行的同步配置信息文件";
            // 
            // Form4
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(846, 449);
            this.Controls.Add(this.btnPause);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.stsStatus);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.pictureBox1);
            this.Name = "Form4";
            this.Text = "SQL > Oracle数据同步工具";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form4_FormClosing);
            this.Load += new System.EventHandler(this.Form4_Load);
            this.stsStatus.ResumeLayout(false);
            this.stsStatus.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.StatusStrip stsStatus;
        private System.Windows.Forms.ToolStripStatusLabel stslTable;
        private System.Windows.Forms.ToolStripProgressBar stpProgress;
        private System.Windows.Forms.ToolStripStatusLabel stsTables;
        private System.Windows.Forms.ToolStripStatusLabel stslRows;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnPause;
        private System.Windows.Forms.ToolStripStatusLabel tsslSqlState;
        private System.Windows.Forms.ToolStripStatusLabel tsslOracleState;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripStatusLabel tsslConfigFile;
    }
}