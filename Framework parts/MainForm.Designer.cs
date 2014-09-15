namespace CSharpRenderer
{
    partial class CSharpRendererMainForm
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
            this.mainSplit = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.debugViewSelect = new System.Windows.Forms.ComboBox();
            this.debugViewImageMode = new System.Windows.Forms.ComboBox();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.debugOnCheckBox = new System.Windows.Forms.CheckBox();
            this.gbuDebugCheckBox = new System.Windows.Forms.CheckBox();
            this.overrideGPUDebugPositionCheckBox = new System.Windows.Forms.CheckBox();
            this.freezeTimeCB = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.overrideGPUDebugPositionXUD = new System.Windows.Forms.NumericUpDown();
            this.overrideGPUDebugPositionYUD = new System.Windows.Forms.NumericUpDown();
            this.overrideGPUDebugPositionZUD = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.gpuDebugTextBox = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.ProfilerName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Time = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pickPixelButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.mainSplit)).BeginInit();
            this.mainSplit.Panel1.SuspendLayout();
            this.mainSplit.Panel2.SuspendLayout();
            this.mainSplit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel3.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.overrideGPUDebugPositionXUD)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.overrideGPUDebugPositionYUD)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.overrideGPUDebugPositionZUD)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // mainSplit
            // 
            this.mainSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainSplit.Location = new System.Drawing.Point(0, 0);
            this.mainSplit.Name = "mainSplit";
            // 
            // mainSplit.Panel1
            // 
            this.mainSplit.Panel1.Controls.Add(this.splitContainer2);
            // 
            // mainSplit.Panel2
            // 
            this.mainSplit.Panel2.Controls.Add(this.splitContainer1);
            this.mainSplit.Size = new System.Drawing.Size(1750, 873);
            this.mainSplit.SplitterDistance = 1354;
            this.mainSplit.TabIndex = 0;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer2.IsSplitterFixed = true;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.flowLayoutPanel1);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.panel1);
            this.splitContainer2.Size = new System.Drawing.Size(1354, 873);
            this.splitContainer2.SplitterDistance = 110;
            this.splitContainer2.TabIndex = 2;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.flowLayoutPanel3);
            this.flowLayoutPanel1.Controls.Add(this.flowLayoutPanel2);
            this.flowLayoutPanel1.Controls.Add(this.tableLayoutPanel2);
            this.flowLayoutPanel1.Controls.Add(this.gpuDebugTextBox);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(1354, 110);
            this.flowLayoutPanel1.TabIndex = 2;
            // 
            // flowLayoutPanel3
            // 
            this.flowLayoutPanel3.AutoSize = true;
            this.flowLayoutPanel3.Controls.Add(this.label1);
            this.flowLayoutPanel3.Controls.Add(this.debugViewSelect);
            this.flowLayoutPanel3.Controls.Add(this.debugViewImageMode);
            this.flowLayoutPanel3.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel3.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.flowLayoutPanel3.Size = new System.Drawing.Size(127, 67);
            this.flowLayoutPanel3.TabIndex = 9;
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(31, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Debug View";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // debugViewSelect
            // 
            this.debugViewSelect.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.debugViewSelect.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.debugViewSelect.FormattingEnabled = true;
            this.debugViewSelect.Location = new System.Drawing.Point(3, 16);
            this.debugViewSelect.Name = "debugViewSelect";
            this.debugViewSelect.Size = new System.Drawing.Size(121, 21);
            this.debugViewSelect.TabIndex = 0;
            // 
            // debugViewImageMode
            // 
            this.debugViewImageMode.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.debugViewImageMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.debugViewImageMode.FormattingEnabled = true;
            this.debugViewImageMode.Items.AddRange(new object[] {
            "RGB",
            "A",
            "FRAC"});
            this.debugViewImageMode.Location = new System.Drawing.Point(3, 43);
            this.debugViewImageMode.Name = "debugViewImageMode";
            this.debugViewImageMode.Size = new System.Drawing.Size(121, 21);
            this.debugViewImageMode.TabIndex = 2;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Controls.Add(this.debugOnCheckBox);
            this.flowLayoutPanel2.Controls.Add(this.gbuDebugCheckBox);
            this.flowLayoutPanel2.Controls.Add(this.overrideGPUDebugPositionCheckBox);
            this.flowLayoutPanel2.Controls.Add(this.freezeTimeCB);
            this.flowLayoutPanel2.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(136, 3);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(200, 100);
            this.flowLayoutPanel2.TabIndex = 8;
            // 
            // debugOnCheckBox
            // 
            this.debugOnCheckBox.AutoSize = true;
            this.debugOnCheckBox.Checked = true;
            this.debugOnCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.debugOnCheckBox.Location = new System.Drawing.Point(3, 3);
            this.debugOnCheckBox.Name = "debugOnCheckBox";
            this.debugOnCheckBox.Size = new System.Drawing.Size(128, 17);
            this.debugOnCheckBox.TabIndex = 3;
            this.debugOnCheckBox.Text = "Surface debug on/off";
            this.debugOnCheckBox.UseVisualStyleBackColor = true;
            // 
            // gbuDebugCheckBox
            // 
            this.gbuDebugCheckBox.AutoSize = true;
            this.gbuDebugCheckBox.Location = new System.Drawing.Point(3, 26);
            this.gbuDebugCheckBox.Name = "gbuDebugCheckBox";
            this.gbuDebugCheckBox.Size = new System.Drawing.Size(126, 17);
            this.gbuDebugCheckBox.TabIndex = 5;
            this.gbuDebugCheckBox.Text = "GPU Debug via UAV";
            this.gbuDebugCheckBox.UseVisualStyleBackColor = true;
            // 
            // overrideGPUDebugPositionCheckBox
            // 
            this.overrideGPUDebugPositionCheckBox.AutoSize = true;
            this.overrideGPUDebugPositionCheckBox.Location = new System.Drawing.Point(3, 49);
            this.overrideGPUDebugPositionCheckBox.Name = "overrideGPUDebugPositionCheckBox";
            this.overrideGPUDebugPositionCheckBox.Size = new System.Drawing.Size(166, 17);
            this.overrideGPUDebugPositionCheckBox.TabIndex = 3;
            this.overrideGPUDebugPositionCheckBox.Text = "Override UAV Debug Position";
            this.overrideGPUDebugPositionCheckBox.UseVisualStyleBackColor = true;
            // 
            // freezeTimeCB
            // 
            this.freezeTimeCB.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.freezeTimeCB.AutoSize = true;
            this.freezeTimeCB.Location = new System.Drawing.Point(3, 72);
            this.freezeTimeCB.Name = "freezeTimeCB";
            this.freezeTimeCB.Size = new System.Drawing.Size(80, 17);
            this.freezeTimeCB.TabIndex = 7;
            this.freezeTimeCB.Text = "Freeze time";
            this.freezeTimeCB.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.AutoSize = true;
            this.tableLayoutPanel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.Controls.Add(this.overrideGPUDebugPositionXUD, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.overrideGPUDebugPositionYUD, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.overrideGPUDebugPositionZUD, 1, 2);
            this.tableLayoutPanel2.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.label3, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.label4, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.pickPixelButton, 1, 3);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(342, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 4;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.Size = new System.Drawing.Size(158, 107);
            this.tableLayoutPanel2.TabIndex = 6;
            // 
            // overrideGPUDebugPositionXUD
            // 
            this.overrideGPUDebugPositionXUD.Location = new System.Drawing.Point(66, 3);
            this.overrideGPUDebugPositionXUD.Maximum = new decimal(new int[] {
            4096,
            0,
            0,
            0});
            this.overrideGPUDebugPositionXUD.Name = "overrideGPUDebugPositionXUD";
            this.overrideGPUDebugPositionXUD.Size = new System.Drawing.Size(89, 20);
            this.overrideGPUDebugPositionXUD.TabIndex = 0;
            // 
            // overrideGPUDebugPositionYUD
            // 
            this.overrideGPUDebugPositionYUD.Location = new System.Drawing.Point(66, 29);
            this.overrideGPUDebugPositionYUD.Maximum = new decimal(new int[] {
            4096,
            0,
            0,
            0});
            this.overrideGPUDebugPositionYUD.Name = "overrideGPUDebugPositionYUD";
            this.overrideGPUDebugPositionYUD.Size = new System.Drawing.Size(89, 20);
            this.overrideGPUDebugPositionYUD.TabIndex = 1;
            // 
            // overrideGPUDebugPositionZUD
            // 
            this.overrideGPUDebugPositionZUD.Location = new System.Drawing.Point(66, 55);
            this.overrideGPUDebugPositionZUD.Maximum = new decimal(new int[] {
            4096,
            0,
            0,
            0});
            this.overrideGPUDebugPositionZUD.Name = "overrideGPUDebugPositionZUD";
            this.overrideGPUDebugPositionZUD.Size = new System.Drawing.Size(89, 20);
            this.overrideGPUDebugPositionZUD.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 6);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(57, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Override X";
            // 
            // label3
            // 
            this.label3.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 32);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(57, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Override Y";
            // 
            // label4
            // 
            this.label4.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 58);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(57, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Override Z";
            // 
            // gpuDebugTextBox
            // 
            this.gpuDebugTextBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.gpuDebugTextBox.Location = new System.Drawing.Point(506, 3);
            this.gpuDebugTextBox.Multiline = true;
            this.gpuDebugTextBox.Name = "gpuDebugTextBox";
            this.gpuDebugTextBox.ReadOnly = true;
            this.gpuDebugTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.gpuDebugTextBox.Size = new System.Drawing.Size(303, 101);
            this.gpuDebugTextBox.TabIndex = 4;
            this.gpuDebugTextBox.WordWrap = false;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Black;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1280, 720);
            this.panel1.TabIndex = 1;
            this.panel1.TabStop = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tableLayoutPanel1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.groupBox1);
            this.splitContainer1.Size = new System.Drawing.Size(392, 873);
            this.splitContainer1.SplitterDistance = 457;
            this.splitContainer1.TabIndex = 0;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 13;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(392, 457);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.dataGridView1);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(392, 412);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Profilers";
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToResizeColumns = false;
            this.dataGridView1.AllowUserToResizeRows = false;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ProfilerName,
            this.Time});
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(3, 16);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.Size = new System.Drawing.Size(386, 393);
            this.dataGridView1.TabIndex = 0;
            // 
            // ProfilerName
            // 
            this.ProfilerName.HeaderText = "Name";
            this.ProfilerName.Name = "ProfilerName";
            this.ProfilerName.ReadOnly = true;
            this.ProfilerName.Width = 200;
            // 
            // Time
            // 
            this.Time.HeaderText = "Time(ms)";
            this.Time.Name = "Time";
            this.Time.ReadOnly = true;
            this.Time.Width = 50;
            // 
            // pickPixelButton
            // 
            this.pickPixelButton.Location = new System.Drawing.Point(66, 81);
            this.pickPixelButton.Name = "pickPixelButton";
            this.pickPixelButton.Size = new System.Drawing.Size(89, 23);
            this.pickPixelButton.TabIndex = 8;
            this.pickPixelButton.Text = "Pick Last Pixel";
            this.pickPixelButton.UseVisualStyleBackColor = true;
            this.pickPixelButton.Click += new System.EventHandler(this.pickPixelButton_Click);
            // 
            // CSharpRendererMainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(1750, 873);
            this.Controls.Add(this.mainSplit);
            this.KeyPreview = true;
            this.Name = "CSharpRendererMainForm";
            this.Text = "CSharpRenderer";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.mainSplit.Panel1.ResumeLayout(false);
            this.mainSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.mainSplit)).EndInit();
            this.mainSplit.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.flowLayoutPanel3.ResumeLayout(false);
            this.flowLayoutPanel3.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.overrideGPUDebugPositionXUD)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.overrideGPUDebugPositionYUD)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.overrideGPUDebugPositionZUD)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer mainSplit;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox debugViewSelect;
        private System.Windows.Forms.ComboBox debugViewImageMode;
        private System.Windows.Forms.CheckBox debugOnCheckBox;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataGridViewTextBoxColumn ProfilerName;
        private System.Windows.Forms.DataGridViewTextBoxColumn Time;
        private System.Windows.Forms.TextBox gpuDebugTextBox;
        private System.Windows.Forms.CheckBox gbuDebugCheckBox;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.NumericUpDown overrideGPUDebugPositionXUD;
        private System.Windows.Forms.NumericUpDown overrideGPUDebugPositionYUD;
        private System.Windows.Forms.NumericUpDown overrideGPUDebugPositionZUD;
        private System.Windows.Forms.CheckBox overrideGPUDebugPositionCheckBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox freezeTimeCB;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Button pickPixelButton;
    }
}