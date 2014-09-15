using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CSharpRenderer
{
    public partial class CSharpRendererMainForm : Form
    {
        public CSharpRendererMainForm()
        {
            InitializeComponent();
        }

        public Panel GetRenderingPanel()
        {
            return panel1;
        }

        public DataGridView GetDataGridView()
        {
            return dataGridView1;
        }

        public TableLayoutPanel GetTableLayoutPanel()
        {
            return tableLayoutPanel1;
        }

        public ComboBox GetDebugSurface()
        {
            return debugViewSelect;
        }

        public ComboBox GetDebugMode()
        {
            return debugViewImageMode;
        }

        public bool GetDebugModeOn()
        {
            return debugOnCheckBox.Checked;
        }

        public bool GetGPUDebugModeOn()
        {
            return gbuDebugCheckBox.Checked;
        }

        public bool GetFreezeTime()
        {
            return freezeTimeCB.Checked;
        }

        public bool GetGPUDebuggingOverridePos()
        {
            return overrideGPUDebugPositionCheckBox.Checked;
        }

        public uint GetGPUDebuggingOverridePosX()
        {
            return (uint)overrideGPUDebugPositionXUD.Value;
        }

        public uint GetGPUDebuggingOverridePosY()
        {
            return (uint)overrideGPUDebugPositionYUD.Value;
        }

        public uint GetGPUDebuggingOverridePosZ()
        {
            return (uint)overrideGPUDebugPositionZUD.Value;
        }

        public TextBox GetGPUDebuggingTB()
        {
            return gpuDebugTextBox;
        }

        private void pickPixelButton_Click(object sender, EventArgs e)
        {
            overrideGPUDebugPositionXUD.Value = Program.m_ClickedX;
            overrideGPUDebugPositionYUD.Value = Program.m_ClickedY;
        }
    }
}
