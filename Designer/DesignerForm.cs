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
            Globals.Design ??= new DesignerData();

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
            new Music.Designer.DesignerForm.DesignerDataBinder().ApplyFormData(this, Globals.Design);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            // When leaving the Designer form, persist any form-backed values into Globals.Design
            Globals.Design = new Music.Designer.DesignerForm.DesignerDataBinder().CaptureFormData(this);
        }

        private void MusicForm_Load(object sender, EventArgs e)
        {
            Globals.Design ??= new DesignerData();
            // Initialize controls from Globals.Design using the binder (parallel to GeneratorForm approach)
            new Music.Designer.DesignerForm.DesignerDataBinder().ApplyFormData(this, Globals.Design);

            UpdateDesignerReport(); // keep existing behavior that builds/refreshes other UI pieces
        }


        //===============   F I L E    E V E N T S   ===============


        private void btnNew_Click(object sender, EventArgs e)
        {
            Globals.Design = new DesignerData();
            UpdateDesignerReport();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (Globals.Design == null)
            {
                MessageBox.Show(this,
                    "Create a new score design first.",
                    "No Design",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            DesignerFileManager.SaveDesign(this);
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            var loaded = DesignerFileManager.LoadDesign(this);
            if (loaded)
            {
                UpdateDesignerReport();
            }
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

        // ===============   H E L P E R S   ===============

        // Note: EnsureDesignOrNotify logic moved to DesignerEdit to centralize checks.

        internal void UpdateDesignerReport()
        {
            txtDesignerReport.Text = DesignerReport.CreateDesignerReport(Globals.Design);
        }


        // ==========================   T E S T    D E S I G N S   ==========================

        private void btnSetTestDesignD1_Click(object sender, EventArgs e)
        {
            // Ensure we have a design to work with
            var design = Globals.Design ??= new DesignerData();
            Music.Designer.DesignerTests.SetTestDesignD1(design);
            UpdateDesignerReport();
        }
    }
}