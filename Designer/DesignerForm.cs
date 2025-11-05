using System;
using System.Windows.Forms;
using Music.Designer;
using Music;

namespace Music
{
    public partial class DesignerForm : Form
    {

        //===============   I N I T I A L I Z E   ===============

        public DesignerForm()
        {
            this.Text = "Music";
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;
            Globals.Designer ??= new Designer.Designer();

            InitializeComponent();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Maximize once when shown as an MDI child; preserves design-time size.
            if (this.MdiParent != null && this.WindowState != FormWindowState.Maximized)
                this.WindowState = FormWindowState.Maximized;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            // When entering the Designer form, apply the current design into the controls
            ApplyFormData(Globals.Designer);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            // When leaving the Designer form, persist any form-backed values into Globals.Design
            Globals.Designer = CaptureFormData();
        }

        private void MusicForm_Load(object sender, EventArgs e)
        {
            Globals.Designer ??= new Designer.Designer();
            // Initialize controls from Globals.Design using the binder (parallel to GeneratorForm approach)
            ApplyFormData(Globals.Designer);

            DesignerFormHandler.UpdateDesignerReport(this); // keep existing behavior that builds/refreshes other UI pieces
        }


        //===============   F I L E    E V E N T S   ===============


        private void btnNew_Click(object sender, EventArgs e)
        {
            DesignerFormHandler.NewDesign(this);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            DesignerFormHandler.SaveDesign(this);
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            DesignerFormHandler.LoadDesign(this);
        }


        //==============   E D I T   B U T T O N S   ===============

        private void btnEditSections_Click(object sender, EventArgs e)
        {
            DesignerFormHandler.EditSections(this);
        }

        // Populate voices via popup selector
        private void btnSelectVoices_Click(object sender, EventArgs e)
        {
            DesignerFormHandler.SelectVoices(this);
        }

        private void btnEditHarmony_Click(object sender, EventArgs e)
        {
            DesignerFormHandler.EditHarmony(this);
        }

        private void btnEditTimeSignature_Click(object sender, EventArgs e)
        {
            DesignerFormHandler.EditTimeSignature(this);
        }

        private void btnEditTempo_Click(object sender, EventArgs e)
        {
            DesignerFormHandler.EditTempo(this);
        }

        // ==========================   T E S T    D E S I G N S   ==========================

        private void btnSetTestDesignD1_Click(object sender, EventArgs e)
        {
            // Ensure we have a design to work with
            var design = Globals.Designer ??= new Designer.Designer();
            Music.Designer.DesignerTests.SetTestDesignD1(design);
            DesignerFormHandler.UpdateDesignerReport(this);
        }
    }
}