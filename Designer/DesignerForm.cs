using Music.Designer;
using Music;

namespace Music
{
    public partial class DesignerForm : Form
    {
        public DesignerForm()
        {
            this.Text = "Music";
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;

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

            PopulateFormFromGlobals(); // keep existing behavior that builds/refreshes other UI pieces
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            ClearDesignAndForm();
        }

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

                RefreshDesignSpaceIfReady();
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

                RefreshDesignSpaceIfReady();
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
                RefreshDesignSpaceIfReady();
            }
        }

        // --------- Helpers ---------

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

        private void RefreshDesignSpaceIfReady()
        {
            if (Globals.Design == null) return;
            txtDesignerReport.Text = DesignerReport.CreateDesignerReport(Globals.Design);
        }

        private void PopulateFormFromGlobals()
        {
            // Combined design-space summary
            RefreshDesignSpaceIfReady();
        }

        private void ClearDesignAndForm()
        {
            // Reset the score design (new instance is fine to ensure clean state)
            Globals.Design = new DesignerData();

            // Repopulate the design area headings with no data
            RefreshDesignSpaceIfReady();
        }

        private void btnSetDefault_Click(object sender, EventArgs e)
        {
            // Ensure we have a design to work with
            var design = Globals.Design ??= new DesignerData();
            Music.Designer.DesignerTests.SetTestDesignD1(design);
            RefreshDesignSpaceIfReady();
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
                RefreshDesignSpaceIfReady();
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
                RefreshDesignSpaceIfReady();
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
                RefreshDesignSpaceIfReady();
            }
        }
    }
}