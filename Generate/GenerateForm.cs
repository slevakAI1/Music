using MusicXml.Domain;
using Music.Design;

namespace Music.Generate
{
    public partial class GenerateForm : Form
    {
        private Score? _score;
        private DesignClass? _design;

        private readonly Dictionary<string,int> _noteValueMap = new()
        {
            ["Whole (1)"]    = 1,
            ["Half (1/2)"]    = 2,
            ["Quarter (1/4)"] = 4,
            ["Eighth (1/8)"]  = 8,
            ["16th (1/16)"]   = 16
        };

        public GenerateForm()
        {
            InitializeComponent();

            // Window behavior similar to other forms
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;

            // Keep pitch-mode radio settable repeatedly (not a list)
            rbPitchAbsolute.Checked = true;

            // Load current global score and design into form-local fields for later use
            _score = Globals.Score;
            _design = Globals.Design;

            // Note: list-type controls are NOT populated here.
            // They will be populated on activation by the Populate...() methods.
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (this.MdiParent != null && this.WindowState != FormWindowState.Maximized)
                this.WindowState = FormWindowState.Maximized;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            // Minimal: only call per-control populate helpers (no other logic here).
            PopulatePattern();
            PopulateStep();
            PopulateAccidental();
            PopulatePartsFromDesign();
            PopulateNoteValue();
        }

        private void PopulatePattern()
        {
            if (cbPattern.Items.Count != 0) return;

            cbPattern.Items.Add("Set Notes");
            cbPattern.SelectedIndex = 0;
        }

        private void PopulateStep()
        {
            if (cbStep.Items.Count != 0) return;

            cbStep.Items.AddRange(new object[] { "C", "D", "E", "F", "G", "A", "B" });
            cbStep.SelectedIndex = 0;
        }

        private void PopulateAccidental()
        {
            if (cbAccidental.Items.Count != 0) return;

            cbAccidental.Items.AddRange(new object[] { "Natural", "Sharp", "Flat" });
            cbAccidental.SelectedIndex = 0;
        }

        private void PopulatePartsFromDesign()
        {
            // Populate only when empty
            if (cbPart.Items.Count != 0) return;

            cbPart.Items.Clear();
            cbPart.Items.Add("Choose");

            var design = _design;
            if (design?.VoiceSet?.Voices != null)
            {
                foreach (var v in design.VoiceSet.Voices)
                {
                    var name = v?.VoiceName ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(name))
                        cbPart.Items.Add(name);
                }
            }

            if (cbPart.Items.Count > 0)
                cbPart.SelectedIndex = 0;
        }

        private void PopulateNoteValue()
        {
            if (cbNoteValue.Items.Count != 0) return;

            cbNoteValue.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (var key in _noteValueMap.Keys)
                cbNoteValue.Items.Add(key);

            cbNoteValue.SelectedItem = "Quarter (1/4)";
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            if (_score == null)
            {
                MessageBox.Show(this, "No Score is loaded. Use SetScore(Score) to provide a MusicXML Score.", "No Score", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            // TODO - implement apply logic 
        }

        //================================  SAVE FOR NOW     ========================================


        //private static (char tonicStep, int tonicAlter) GetTonicForKey(int fifths, string? mode)
        //{
        //    bool isMinor = string.Equals(mode, "minor", StringComparison.OrdinalIgnoreCase);

        //    var major = new Dictionary<int, (char, int)>
        //    {
        //        [-7] = ('C', -1),
        //        [-6] = ('G', -1),
        //        [-5] = ('D', -1),
        //        [-4] = ('A', -1),
        //        [-3] = ('E', -1),
        //        [-2] = ('B', -1),
        //        [-1] = ('F',  0),
        //        [ 0] = ('C',  0),
        //        [ 1] = ('G',  0),
        //        [ 2] = ('D',  0),
        //        [ 3] = ('A',  0),
        //        [ 4] = ('E',  0),
        //        [ 5] = ('B',  0),
        //        [ 6] = ('F',  1),
        //        [ 7] = ('C',  1),
        //    };

        //    var minor = new Dictionary<int, (char, int)>
        //    {
        //        [-7] = ('A', -1),
        //        [-6] = ('E', -1),
        //        [-5] = ('B', -1),
        //        [-4] = ('F',  0),
        //        [-3] = ('C',  0),
        //        [-2] = ('G',  0),
        //        [-1] = ('D',  0),
        //        [ 0] = ('A',  0),
        //        [ 1] = ('E',  0),
        //        [ 2] = ('B',  0),
        //        [ 3] = ('F',  1),
        //        [ 4] = ('C',  1),
        //        [ 5] = ('G',  1),
        //        [ 6] = ('D',  1),
        //        [ 7] = ('A',  1),
        //    };

        //    var table = isMinor ? minor : major;
        //    if (!table.TryGetValue(fifths, out var tonic))
        //        tonic = table[0];

        //    return tonic;
        //}
    }
}