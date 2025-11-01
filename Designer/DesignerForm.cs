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
            if (!EnsureDesignOrNotify()) return;
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


        // Launch the Section Editor and apply results back to the design
        private void btnEditSections_Click(object sender, EventArgs e)
        {
            if (!EnsureDesignOrNotify()) return;

            using var dlg = new SectionEditorForm(Globals.Design!.SectionSet);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                // Copy back into the existing Sections instance to preserve references
                var target = Globals.Design!.SectionSet;
                target.Reset();
                foreach (var s in dlg.ResultSections.Sections)
                {
                    target.Add(s.SectionType, s.BarCount, s.Name);
                }

                UpdateDesignerReport();
            }
        }

        // Populate voices via popup selector
        private void btnSelectVoices_Click(object sender, EventArgs e)
        {
            if (!EnsureDesignOrNotify()) return;

            using var dlg = new VoiceSelectorForm();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var score = Globals.Design!;
                var existing = new HashSet<string>(score.PartSet.Parts.Select(v => v.PartName),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var name in dlg.SelectedVoices)
                {
                    if (!existing.Contains(name))
                    {
                        score.PartSet.AddVoice(name);
                        existing.Add(name);
                    }
                }

                UpdateDesignerReport();
            }
        }

        private void btnEditHarmony_Click(object sender, EventArgs e)
        {
            if (!EnsureDesignOrNotify()) return;

            var existing = Globals.Design!.HarmonicTimeline;
            using var dlg = new HarmonicEditorForm(existing);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                Globals.Design!.HarmonicTimeline = dlg.ResultTimeline;
                UpdateDesignerReport();
            }
        }

        private void btnEditTimeSignature_Click(object sender, EventArgs e)
        {
            if (!EnsureDesignOrNotify()) return;

            var existing = Globals.Design!.TimeSignatureTimeline;
            using var dlg = new TimeSignatureEditorForm(existing);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                Globals.Design!.TimeSignatureTimeline = dlg.ResultTimeline;
                // Update combined design-space summary to reflect time signatures
                UpdateDesignerReport();
            }
        }

        private void btnEditTempo_Click(object sender, EventArgs e)
        {
            if (!EnsureDesignOrNotify()) return;

            var existing = Globals.Design!.TempoTimeline;
            using var dlg = new TempoEditorForm(existing);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                Globals.Design!.TempoTimeline = dlg.ResultTimeline;
                // Now reflect changes in the Design View
                UpdateDesignerReport();
            }
        }

        // ===============   H E L P E R S   ===============

        private bool EnsureDesignOrNotify()
        {
            if (Globals.Design != null) return true;

            MessageBox.Show(this,
                "Create a new score design first.",
                "No Design",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        private void UpdateDesignerReport()
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