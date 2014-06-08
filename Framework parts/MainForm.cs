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
    }
}
