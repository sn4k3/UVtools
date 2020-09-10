namespace UVtools.GUI.Forms
{
    partial class FrmBenchmark
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmBenchmark));
            this.label1 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.lbDescription = new System.Windows.Forms.Label();
            this.cbTest = new System.Windows.Forms.ComboBox();
            this.lbSingleThreadResults = new System.Windows.Forms.Label();
            this.lbMultiThreadResults = new System.Windows.Forms.Label();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lbMultiThreadDevResults = new System.Windows.Forms.Label();
            this.lbSingleThreadDevResults = new System.Windows.Forms.Label();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 177);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 18);
            this.label1.TabIndex = 0;
            this.label1.Text = "Test:";
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.lbDescription);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Margin = new System.Windows.Forms.Padding(4);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(486, 167);
            this.panel3.TabIndex = 4;
            // 
            // lbDescription
            // 
            this.lbDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbDescription.Location = new System.Drawing.Point(0, 0);
            this.lbDescription.Name = "lbDescription";
            this.lbDescription.Padding = new System.Windows.Forms.Padding(20, 0, 20, 0);
            this.lbDescription.Size = new System.Drawing.Size(484, 165);
            this.lbDescription.TabIndex = 1;
            this.lbDescription.Text = resources.GetString("lbDescription.Text");
            this.lbDescription.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbTest
            // 
            this.cbTest.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbTest.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbTest.FormattingEnabled = true;
            this.cbTest.Location = new System.Drawing.Point(61, 174);
            this.cbTest.Name = "cbTest";
            this.cbTest.Size = new System.Drawing.Size(413, 26);
            this.cbTest.TabIndex = 5;
            this.cbTest.SelectedIndexChanged += new System.EventHandler(this.EventSelectedIndexChanged);
            // 
            // lbSingleThreadResults
            // 
            this.lbSingleThreadResults.AutoSize = true;
            this.lbSingleThreadResults.Location = new System.Drawing.Point(13, 237);
            this.lbSingleThreadResults.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbSingleThreadResults.Name = "lbSingleThreadResults";
            this.lbSingleThreadResults.Size = new System.Drawing.Size(158, 18);
            this.lbSingleThreadResults.TabIndex = 6;
            this.lbSingleThreadResults.Text = "Single Thread: 0 TDPS";
            // 
            // lbMultiThreadResults
            // 
            this.lbMultiThreadResults.AutoSize = true;
            this.lbMultiThreadResults.Location = new System.Drawing.Point(22, 260);
            this.lbMultiThreadResults.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbMultiThreadResults.Name = "lbMultiThreadResults";
            this.lbMultiThreadResults.Size = new System.Drawing.Size(149, 18);
            this.lbMultiThreadResults.TabIndex = 7;
            this.lbMultiThreadResults.Text = "Multi Thread: 0 TDPS";
            // 
            // btnStop
            // 
            this.btnStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStop.BackColor = System.Drawing.SystemColors.Control;
            this.btnStop.Enabled = false;
            this.btnStop.Location = new System.Drawing.Point(387, 378);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(87, 36);
            this.btnStop.TabIndex = 8;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = false;
            this.btnStop.Click += new System.EventHandler(this.EventClicked);
            // 
            // btnStart
            // 
            this.btnStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStart.BackColor = System.Drawing.SystemColors.Control;
            this.btnStart.Location = new System.Drawing.Point(294, 378);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(87, 36);
            this.btnStart.TabIndex = 9;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = false;
            this.btnStart.Click += new System.EventHandler(this.EventClicked);
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(12, 378);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(276, 36);
            this.progressBar.TabIndex = 10;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(13, 213);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(104, 18);
            this.label2.TabIndex = 11;
            this.label2.Text = "Your results:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(13, 290);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(145, 18);
            this.label3.TabIndex = 14;
            this.label3.Text = "Developer results:";
            // 
            // lbMultiThreadDevResults
            // 
            this.lbMultiThreadDevResults.AutoSize = true;
            this.lbMultiThreadDevResults.Enabled = false;
            this.lbMultiThreadDevResults.Location = new System.Drawing.Point(22, 337);
            this.lbMultiThreadDevResults.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbMultiThreadDevResults.Name = "lbMultiThreadDevResults";
            this.lbMultiThreadDevResults.Size = new System.Drawing.Size(149, 18);
            this.lbMultiThreadDevResults.TabIndex = 13;
            this.lbMultiThreadDevResults.Text = "Multi Thread: 0 TDPS";
            // 
            // lbSingleThreadDevResults
            // 
            this.lbSingleThreadDevResults.AutoSize = true;
            this.lbSingleThreadDevResults.Enabled = false;
            this.lbSingleThreadDevResults.Location = new System.Drawing.Point(13, 314);
            this.lbSingleThreadDevResults.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbSingleThreadDevResults.Name = "lbSingleThreadDevResults";
            this.lbSingleThreadDevResults.Size = new System.Drawing.Size(158, 18);
            this.lbSingleThreadDevResults.TabIndex = 12;
            this.lbSingleThreadDevResults.Text = "Single Thread: 0 TDPS";
            // 
            // FrmBenchmark
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(486, 426);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.lbMultiThreadDevResults);
            this.Controls.Add(this.lbSingleThreadDevResults);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.lbMultiThreadResults);
            this.Controls.Add(this.lbSingleThreadResults);
            this.Controls.Add(this.cbTest);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.Name = "FrmBenchmark";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Benchmark System";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmBenchmark_FormClosing);
            this.panel3.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Label lbDescription;
        private System.Windows.Forms.ComboBox cbTest;
        private System.Windows.Forms.Label lbSingleThreadResults;
        private System.Windows.Forms.Label lbMultiThreadResults;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lbMultiThreadDevResults;
        private System.Windows.Forms.Label lbSingleThreadDevResults;
    }
}