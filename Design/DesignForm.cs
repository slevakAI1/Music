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
            // Ensure we have a design object; create one if needed.
            Globals.ScoreDesign ??= new ScoreDesignClass();

            // Populate UI from current globals.
            PopulateFormFromGlobals();
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            // Clear all design elements and UI.
            ClearDesignAndForm();
        }

        // Launch the Section Editor and apply results back to the design
        private void btnEditSections_Click(object sender, EventArgs e)
        {
            if (!EnsureScoreDesignOrNotify()) return;

            using var dlg = new SectionEditorForm(Globals.ScoreDesign!.Sections);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                // Copy back into the existing Sections instance to preserve references
                var target = Globals.ScoreDesign!.Sections;
                target.Reset();
                foreach (var s in dlg.ResultSections.Sections)
                {
                    target.Add(s.SectionType, s.BarCount, s.Name);
                }

                RefreshDesignSpaceIfReady();
            }
        }

        /*   keep for now
                    if (!EnsureScoreDesignOrNotify()) return;
                    Globals.SectionManager.CreateTestSections(Globals.ScoreDesign!.Sections);
                    RefreshDesignSpaceIfReady();
         */

        // Populate voices via popup selector
        private void btnSelectVoices_Click(object sender, EventArgs e)
        {
            if (!EnsureScoreDesignOrNotify()) return;

            using var dlg = new VoiceSelectorForm();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var score = Globals.ScoreDesign!;
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

/* All code for adding default - keep for now
 * if (!EnsureScoreDesignOrNotify()) return;
            Globals.ScoreDesign!.VoiceSet.AddDefaultVoices();
            RefreshDesignSpaceIfReady();
*/

        // Populate chords without touching the UI textboxes
        //private void btnAddChords_Click(object sender, EventArgs e)
        //{
        //    if (!EnsureScoreDesignOrNotify()) return;
        //    Globals.ScoreDesign!.ChordSet.AddDefaultChords();
        //    RefreshDesignSpaceIfReady();
        //}

        private void btnCreateMusic_Click(object sender, EventArgs e)
        {
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
        }

        private void btnEditHarmonicTimeline_Click(object sender, EventArgs e)
        {
            var timeline = HarmonicDefault.BuildDefaultTimeline();
            Globals.HarmonicTimeline = timeline;

            // Reflect meter/tempo in UI when a timeline is created/edited
            UpdateUiFromTimeline(timeline);

            RefreshDesignSpaceIfReady();
        }

        // --------- Helpers ---------

        private bool EnsureScoreDesignOrNotify()
        {
            if (Globals.ScoreDesign != null) return true;

            MessageBox.Show(this,
                "Create a new score design first.",
                "No Design",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        private void RefreshDesignSpaceIfReady()
        {
            if (Globals.ScoreDesign == null) return;
            txtDesignSpace.Text = DesignTextHelper.BuildCombinedText(Globals.ScoreDesign);
        }

        private void PopulateFormFromGlobals()
        {
            // Combined design-space summary
            RefreshDesignSpaceIfReady();

            // Meter/tempo from the harmonic timeline (if any)
            var timeline = Globals.HarmonicTimeline;
            if (timeline != null)
            {
                UpdateUiFromTimeline(timeline);
            }
            else
            {
                // no timeline -> leave designer defaults as-is
            }
        }

        private void UpdateUiFromTimeline(HarmonicTimeline timeline)
        {
            // We only track beats-per-bar; default denominator to 4 in UI
            textBox1.Text = $"{timeline.BeatsPerBar}/4";
            textBox2.Text = timeline.TempoBpm.ToString();
        }

        private void ClearDesignAndForm()
        {
            // Reset the score design (new instance is fine to ensure clean state)
            Globals.ScoreDesign = new ScoreDesignClass();

            // Clear global timeline and related UI
            Globals.HarmonicTimeline = null;

            // Clear UI fields for meter/tempo
            textBox1.Clear(); // time signature
            textBox2.Clear(); // tempo

            // Repopulate the design area headings with no data
            RefreshDesignSpaceIfReady();
        }
    }
}