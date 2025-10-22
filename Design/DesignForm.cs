using Music.Design;

namespace Music
{
    public partial class DesignForm : Form
    {
        public DesignForm()
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

        private void MusicForm_Load(object sender, EventArgs e)
        {
            Globals.Design ??= new DesignClass();
            PopulateFormFromGlobals();
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
                var existing = new HashSet<string>(score.VoiceSet.Voices.Select(v => v.VoiceName),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var name in dlg.SelectedVoices)
                {
                    if (!existing.Contains(name))
                    {
                        score.VoiceSet.AddVoice(name);
                        existing.Add(name);
                    }
                }

                RefreshDesignSpaceIfReady();
            }
        }

        private void btnEditHarmonicTimeline_Click(object sender, EventArgs e)
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
            txtDesignView.Text = DesignView.CreateDesignView(Globals.Design);
        }

        private void PopulateFormFromGlobals()
        {
            // Combined design-space summary
            RefreshDesignSpaceIfReady();
        }

        private void ClearDesignAndForm()
        {
            // Reset the score design (new instance is fine to ensure clean state)
            Globals.Design = new DesignClass();

            // Repopulate the design area headings with no data
            RefreshDesignSpaceIfReady();
        }

        private void btnSetDefault_Click(object sender, EventArgs e)
        {
            // Ensure we have a design to work with
            var design = Globals.Design ??= new DesignClass();

            // 1) Sections: apply default/test structure
            var sectionsHelper = new SectionDefaultsClass();
            sectionsHelper.CreateTestSections(design.SectionSet);

            // 2) Voices: apply default voices
            design.VoiceSet.AddDefaultVoices();

            // 3) Harmonic timeline: use the same defaults as the Harmonic Editor's "Set Defaults"
            design.HarmonicTimeline = HarmonicDefault.BuildDefaultTimeline();

            // 4) Time signature timeline: apply default (4/4 starting at bar 1)
            design.TimeSignatureTimeline = TimeSignatureDefault.BuildDefaultTimeline();

            RefreshDesignSpaceIfReady();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!EnsureDesignOrNotify()) return;

            try
            {
                var design = Globals.Design!;
                var json = System.Text.Json.JsonSerializer.Serialize(
                    design,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                // Assume the folder exists under the project root: Design/Designs
                var baseDir = AppContext.BaseDirectory;
                var projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", ".."));
                var targetDir = System.IO.Path.Combine(projectRoot, "Design", "Designs");

                var fileName = $"Design-{design.DesignId}.json";
                var fullPath = System.IO.Path.Combine(targetDir, fileName);

                System.IO.File.WriteAllText(fullPath, json);

                MessageBox.Show(this, $"Design saved to:\n{fullPath}", "Saved",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to save design.\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", ".."));
                var defaultDir = System.IO.Path.Combine(projectRoot, "Design", "Designs");

                using var ofd = new OpenFileDialog
                {
                    Filter = "Design Files (*.json)|*.json|All Files (*.*)|*.*",
                    Title = "Open Design",
                    InitialDirectory = defaultDir,
                    RestoreDirectory = true
                };

                if (ofd.ShowDialog(this) != DialogResult.OK) return;

                var json = System.IO.File.ReadAllText(ofd.FileName);

                // Robust manual deserialization to rebuild computed fields
                var loaded = DesignSerialization.DeserializeDesign(json);

                Globals.Design = loaded;

                RefreshDesignSpaceIfReady();

                MessageBox.Show(this, $"Design loaded from:\n{ofd.FileName}", "Loaded",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to load design.\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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