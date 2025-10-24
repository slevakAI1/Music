using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Music.Generate
{
    public partial class GenerateForm : Form
    {
        public GenerateForm()
        {
            InitializeComponent();

            // Match DesignForm behavior
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Maximize when shown as an MDI child
            if (this.MdiParent != null && this.WindowState != FormWindowState.Maximized)
                this.WindowState = FormWindowState.Maximized;
        }

        private void GenerateForm_Load(object sender, EventArgs e)
        {

        }
    }
}
