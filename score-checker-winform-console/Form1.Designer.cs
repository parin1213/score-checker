
namespace genshin_relic
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.label1 = new System.Windows.Forms.Label();
            this.txtUrl = new System.Windows.Forms.TextBox();
            this.originalImage = new System.Windows.Forms.PictureBox();
            this.chkCached = new System.Windows.Forms.CheckBox();
            this.button1 = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.イメージを開くToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ログファイルjsonを開くToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.btnAnalyze = new System.Windows.Forms.Button();
            this.cropImage = new System.Windows.Forms.PictureBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.lblSum = new System.Windows.Forms.Label();
            this.btnPrev = new System.Windows.Forms.Button();
            this.btnNext = new System.Windows.Forms.Button();
            this.lblPage = new System.Windows.Forms.Label();
            this.btnWordsShow = new System.Windows.Forms.Button();
            this.chkVerify = new System.Windows.Forms.CheckBox();
            this.chkOnlyDiffirent = new System.Windows.Forms.CheckBox();
            this.btnEdge = new System.Windows.Forms.Button();
            this.chkMultiRelic = new System.Windows.Forms.CheckBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.originalImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cropImage)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 19);
            this.label1.Margin = new System.Windows.Forms.Padding(10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 20);
            this.label1.TabIndex = 4;
            this.label1.Text = "image";
            // 
            // txtUrl
            // 
            this.txtUrl.Location = new System.Drawing.Point(123, 16);
            this.txtUrl.Name = "txtUrl";
            this.txtUrl.Size = new System.Drawing.Size(223, 27);
            this.txtUrl.TabIndex = 5;
            this.txtUrl.Text = "C:\\Users\\schwa\\Videos\\Captures\\relic\\1.png";
            // 
            // originalImage
            // 
            this.originalImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.originalImage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.originalImage.Location = new System.Drawing.Point(3, 314);
            this.originalImage.Name = "originalImage";
            this.originalImage.Size = new System.Drawing.Size(485, 305);
            this.originalImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.originalImage.TabIndex = 7;
            this.originalImage.TabStop = false;
            // 
            // chkCached
            // 
            this.chkCached.Checked = true;
            this.chkCached.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkCached.Location = new System.Drawing.Point(878, 19);
            this.chkCached.Name = "chkCached";
            this.chkCached.Size = new System.Drawing.Size(116, 27);
            this.chkCached.TabIndex = 9;
            this.chkCached.Text = "キャッシュモード";
            this.chkCached.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(778, 17);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(94, 29);
            this.button1.TabIndex = 11;
            this.button1.Text = "一括解析";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.tableLayoutPanel1.SetColumnSpan(this.dataGridView1, 2);
            this.dataGridView1.ContextMenuStrip = this.contextMenuStrip1;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(3, 3);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.RowTemplate.Height = 29;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new System.Drawing.Size(976, 305);
            this.dataGridView1.TabIndex = 13;
            this.dataGridView1.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dataGridView1_RowsAdded);
            this.dataGridView1.SelectionChanged += new System.EventHandler(this.dataGridView1_SelectionChanged);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.イメージを開くToolStripMenuItem,
            this.ログファイルjsonを開くToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(263, 52);
            // 
            // イメージを開くToolStripMenuItem
            // 
            this.イメージを開くToolStripMenuItem.Name = "イメージを開くToolStripMenuItem";
            this.イメージを開くToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
            this.イメージを開くToolStripMenuItem.Size = new System.Drawing.Size(262, 24);
            this.イメージを開くToolStripMenuItem.Text = "イメージを開く";
            this.イメージを開くToolStripMenuItem.Click += new System.EventHandler(this.イメージを開くToolStripMenuItem_Click);
            // 
            // ログファイルjsonを開くToolStripMenuItem
            // 
            this.ログファイルjsonを開くToolStripMenuItem.Name = "ログファイルjsonを開くToolStripMenuItem";
            this.ログファイルjsonを開くToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.J)));
            this.ログファイルjsonを開くToolStripMenuItem.Size = new System.Drawing.Size(262, 24);
            this.ログファイルjsonを開くToolStripMenuItem.Text = "ログファイル(json)を開く";
            this.ログファイルjsonを開くToolStripMenuItem.Click += new System.EventHandler(this.ログファイルjsonを開くToolStripMenuItem_Click);
            // 
            // btnAnalyze
            // 
            this.btnAnalyze.Location = new System.Drawing.Point(577, 17);
            this.btnAnalyze.Name = "btnAnalyze";
            this.btnAnalyze.Size = new System.Drawing.Size(94, 29);
            this.btnAnalyze.TabIndex = 14;
            this.btnAnalyze.Text = "解析";
            this.btnAnalyze.UseVisualStyleBackColor = true;
            this.btnAnalyze.Click += new System.EventHandler(this.btnAnalyze_Click);
            // 
            // cropImage
            // 
            this.cropImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cropImage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cropImage.Location = new System.Drawing.Point(494, 314);
            this.cropImage.Name = "cropImage";
            this.cropImage.Size = new System.Drawing.Size(485, 305);
            this.cropImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.cropImage.TabIndex = 7;
            this.cropImage.TabStop = false;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.originalImage, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.cropImage, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.dataGridView1, 0, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(12, 87);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(982, 622);
            this.tableLayoutPanel1.TabIndex = 15;
            // 
            // lblSum
            // 
            this.lblSum.AutoSize = true;
            this.lblSum.Location = new System.Drawing.Point(352, 19);
            this.lblSum.Name = "lblSum";
            this.lblSum.Size = new System.Drawing.Size(85, 20);
            this.lblSum.TabIndex = 16;
            this.lblSum.Text = "1,200/1,200";
            // 
            // btnPrev
            // 
            this.btnPrev.Enabled = false;
            this.btnPrev.Location = new System.Drawing.Point(12, 52);
            this.btnPrev.Name = "btnPrev";
            this.btnPrev.Size = new System.Drawing.Size(94, 29);
            this.btnPrev.TabIndex = 17;
            this.btnPrev.Text = "前";
            this.btnPrev.UseVisualStyleBackColor = true;
            this.btnPrev.Click += new System.EventHandler(this.btnPrev_Click);
            // 
            // btnNext
            // 
            this.btnNext.Location = new System.Drawing.Point(900, 52);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(94, 29);
            this.btnNext.TabIndex = 18;
            this.btnNext.Text = "次";
            this.btnNext.UseVisualStyleBackColor = true;
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
            // 
            // lblPage
            // 
            this.lblPage.Location = new System.Drawing.Point(442, 52);
            this.lblPage.Name = "lblPage";
            this.lblPage.Size = new System.Drawing.Size(129, 29);
            this.lblPage.TabIndex = 19;
            this.lblPage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnWordsShow
            // 
            this.btnWordsShow.Location = new System.Drawing.Point(677, 17);
            this.btnWordsShow.Name = "btnWordsShow";
            this.btnWordsShow.Size = new System.Drawing.Size(94, 29);
            this.btnWordsShow.TabIndex = 20;
            this.btnWordsShow.Text = "OCR詳細";
            this.btnWordsShow.UseVisualStyleBackColor = true;
            this.btnWordsShow.Click += new System.EventHandler(this.btnWordsShow_Click);
            // 
            // chkVerify
            // 
            this.chkVerify.AutoSize = true;
            this.chkVerify.Location = new System.Drawing.Point(778, 57);
            this.chkVerify.Name = "chkVerify";
            this.chkVerify.Size = new System.Drawing.Size(93, 24);
            this.chkVerify.TabIndex = 9;
            this.chkVerify.Text = "検証モード";
            this.chkVerify.UseVisualStyleBackColor = true;
            // 
            // chkOnlyDiffirent
            // 
            this.chkOnlyDiffirent.AutoSize = true;
            this.chkOnlyDiffirent.Location = new System.Drawing.Point(677, 55);
            this.chkOnlyDiffirent.Name = "chkOnlyDiffirent";
            this.chkOnlyDiffirent.Size = new System.Drawing.Size(91, 24);
            this.chkOnlyDiffirent.TabIndex = 9;
            this.chkOnlyDiffirent.Text = "差分表示";
            this.chkOnlyDiffirent.UseVisualStyleBackColor = true;
            // 
            // btnEdge
            // 
            this.btnEdge.Location = new System.Drawing.Point(477, 17);
            this.btnEdge.Name = "btnEdge";
            this.btnEdge.Size = new System.Drawing.Size(94, 29);
            this.btnEdge.TabIndex = 21;
            this.btnEdge.Text = "エッジ検出";
            this.btnEdge.UseVisualStyleBackColor = true;
            this.btnEdge.Click += new System.EventHandler(this.btnEdge_Click);
            // 
            // chkMultiRelic
            // 
            this.chkMultiRelic.AutoSize = true;
            this.chkMultiRelic.Font = new System.Drawing.Font("Yu Gothic UI", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.chkMultiRelic.Location = new System.Drawing.Point(571, 59);
            this.chkMultiRelic.Name = "chkMultiRelic";
            this.chkMultiRelic.Size = new System.Drawing.Size(100, 19);
            this.chkMultiRelic.TabIndex = 9;
            this.chkMultiRelic.Text = "ビルド画像のみ";
            this.chkMultiRelic.UseVisualStyleBackColor = true;
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1006, 721);
            this.Controls.Add(this.btnEdge);
            this.Controls.Add(this.btnWordsShow);
            this.Controls.Add(this.lblPage);
            this.Controls.Add(this.btnNext);
            this.Controls.Add(this.btnPrev);
            this.Controls.Add(this.lblSum);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.btnAnalyze);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.chkOnlyDiffirent);
            this.Controls.Add(this.chkMultiRelic);
            this.Controls.Add(this.chkVerify);
            this.Controls.Add(this.chkCached);
            this.Controls.Add(this.txtUrl);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.originalImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.cropImage)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtUrl;
        private System.Windows.Forms.PictureBox originalImage;
        private System.Windows.Forms.CheckBox chkCached;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btnAnalyze;
        private System.Windows.Forms.PictureBox cropImage;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.DataGridViewTextBoxColumn accessibleDescriptionDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn accessibleNameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn accessibleRoleDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn allowDropDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn anchorDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn backColorDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewImageColumn backgroundImageDataGridViewImageColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn backgroundImageLayoutDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn causesValidationDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn contextMenuStripDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn cursorDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataBindingsDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn dockDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn enabledDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn fontDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn foreColorDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn locationDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn marginDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn maximumSizeDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn minimumSizeDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn rightToLeftDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn sizeDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn tabIndexDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn tabStopDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn tagDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn textDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn useWaitCursorDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn visibleDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn paddingDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn imeModeDataGridViewTextBoxColumn;
        private System.Windows.Forms.Label lblSum;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem イメージを開くToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ログファイルjsonを開くToolStripMenuItem;
        private System.Windows.Forms.Button btnPrev;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.Label lblPage;
        private System.Windows.Forms.Button btnWordsShow;
        private System.Windows.Forms.CheckBox chkVerify;
        private System.Windows.Forms.CheckBox chkOnlyDiffirent;
        private System.Windows.Forms.Button btnEdge;
        private System.Windows.Forms.CheckBox chkMultiRelic;
        private System.Windows.Forms.Timer timer1;
    }
}

