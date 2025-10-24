using MusicXml.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace Music.Generate
{
    public partial class Generate2Form : Form
    {
        private Score? _score;

        public Generate2Form()
        {
            InitializeComponent();

            // Window behavior similar to other forms
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;

            // Initialize static UI
            cbPattern.Items.Add("Set Whole Note");
            cbPattern.SelectedIndex = 0;

            cbStep.Items.AddRange(new object[] { "C", "D", "E", "F", "G", "A", "B" });
            cbStep.SelectedIndex = 0;

            cbAccidental.Items.AddRange(new object[] { "Natural", "Sharp", "Flat" });
            cbAccidental.SelectedIndex = 0;

            rbPitchAbsolute.Checked = true;
            UpdatePitchMode();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (this.MdiParent != null && this.WindowState != FormWindowState.Maximized)
                this.WindowState = FormWindowState.Maximized;
        }

        public void SetScore(Score score)
        {
            _score = score;
            PopulateParts();
            lblStatus.Text = "Score loaded.";
        }

        private void PopulateParts()
        {
            cbPart.Items.Clear();
            if (_score?.Parts == null || _score.Parts.Count == 0)
            {
                cbPart.Items.Add("(no parts)");
                cbPart.SelectedIndex = 0;
                cbPart.Enabled = false;
                chkAllParts.Enabled = false;
                return;
            }

            for (int i = 0; i < _score.Parts.Count; i++)
            {
                var p = _score.Parts[i];
                string label = $"{(string.IsNullOrWhiteSpace(p.Name) ? $"Part {i + 1}" : p.Name)} [{(string.IsNullOrWhiteSpace(p.Id) ? $"P{i + 1}" : p.Id)}]";
                cbPart.Items.Add(label);
            }
            cbPart.SelectedIndex = 0;

            cbPart.Enabled = true;
            chkAllParts.Enabled = true;
        }

        private void rbPitchMode_CheckedChanged(object sender, EventArgs e) => UpdatePitchMode();

        private void UpdatePitchMode()
        {
            grpAbsolute.Enabled = rbPitchAbsolute.Checked;
            grpKeyRelative.Enabled = rbPitchKeyRelative.Checked;
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            if (_score == null)
            {
                MessageBox.Show(this, "No Score is loaded. Use SetScore(Score) to provide a MusicXML Score.", "No Score", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!TryParseVoices(txtVoices.Text, out var voices, out var parseErr))
            {
                MessageBox.Show(this, parseErr, "Voices", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int startBar = (int)numStartBar.Value;
            int endBar = (int)numEndBar.Value;
            if (endBar < startBar)
            {
                MessageBox.Show(this, "End bar must be greater than or equal to Start bar.", "Bar Range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var parts = GetTargetParts();
            if (parts.Count == 0)
            {
                MessageBox.Show(this, "No target parts available.", "Parts", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Resolve pitch for pattern
            bool isKeyRelative = rbPitchKeyRelative.Checked;
            char step;
            int alter;
            int octave;

            if (isKeyRelative)
            {
                int degree = (int)numDegree.Value; // 1..7
                int oct = (int)numOctaveKR.Value;
                // For key-relative we compute per-measure below; placeholders here
                step = 'C';
                alter = 0;
                octave = oct;
            }
            else
            {
                step = ((string)cbStep.SelectedItem!)[0];
                alter = cbAccidental.SelectedIndex switch
                {
                    1 => 1,   // Sharp
                    2 => -1,  // Flat
                    _ => 0
                };
                octave = (int)numOctaveAbs.Value;
            }

            int totalWrites = 0;
            foreach (var part in parts)
            {
                EnsureMeasures(part, endBar);

                for (int bar = startBar; bar <= endBar; bar++)
                {
                    var measure = GetMeasure(part, bar);
                    var attrs = GetEffectiveAttributes(part, bar);

                    EnsureMeasureAttributes(measure, attrs);

                    // Determine duration for a whole note; bound to measure capacity if available.
                    int divisions = measure.Attributes?.Divisions > 0 ? measure.Attributes.Divisions : attrs.Divisions;
                    if (divisions <= 0) divisions = 480;
                    int wholeDuration = divisions * 4;

                    int measureCapacity = GetMeasureCapacityTicks(attrs);
                    int useDuration = measureCapacity > 0 ? Math.Min(wholeDuration, measureCapacity) : wholeDuration;

                    foreach (var v in voices)
                    {
                        if (chkOverwrite.Checked)
                            RemoveVoiceNotes(measure, v);

                        // Resolve pitch
                        Pitch pitch;
                        if (isKeyRelative)
                        {
                            // Compute step/alter from key signature and selected degree
                            var key = attrs.Key ?? new Key { Fifths = 0, Mode = "major" };
                            var (degStep, degAlter) = ComputePitchFromKeyDegree(key, (int)numDegree.Value);
                            pitch = new Pitch
                            {
                                Step = degStep,
                                Alter = degAlter,
                                Octave = (int)numOctaveKR.Value
                            };
                        }
                        else
                        {
                            pitch = new Pitch { Step = step, Alter = alter, Octave = octave };
                        }

                        var note = new Note
                        {
                            IsRest = false,
                            Pitch = pitch,
                            Duration = useDuration,
                            Voice = v,
                            Type = "whole",
                            Staff = (int)numStaff.Value // staff defaults to 1
                        };

                        measure.MeasureElements ??= new List<MeasureElement>();
                        measure.MeasureElements.Add(new MeasureElement
                        {
                            Type = MeasureElementType.Note,
                            Element = note
                        });
                        totalWrites++;
                    }
                }
            }

            lblStatus.Text = $"Applied whole notes: {totalWrites} note(s) written.";
        }

        private List<Part> GetTargetParts()
        {
            var parts = new List<Part>();
            if (_score?.Parts == null) return parts;

            if (chkAllParts.Checked)
            {
                parts.AddRange(_score.Parts);
            }
            else
            {
                int idx = Math.Max(0, cbPart.SelectedIndex);
                if (idx >= 0 && idx < _score.Parts.Count)
                    parts.Add(_score.Parts[idx]);
            }
            return parts;
        }

        private static bool TryParseVoices(string text, out List<int> voices, out string? error)
        {
            voices = new List<int>();
            error = null;

            if (string.IsNullOrWhiteSpace(text))
            {
                error = "Enter one or more voice numbers (e.g., 1 or 1,2).";
                return false;
            }

            var toks = text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in toks)
            {
                if (int.TryParse(t, out var v) && v > 0)
                {
                    if (!voices.Contains(v))
                        voices.Add(v);
                }
                else
                {
                    error = $"Invalid voice: '{t}'. Use positive integers.";
                    return false;
                }
            }
            if (voices.Count == 0)
            {
                error = "No valid voices parsed.";
                return false;
            }
            return true;
        }

        private static Measure GetMeasure(Part part, int barNumber1Based)
        {
            if (part.Measures == null)
                part.Measures = new List<Measure>();

            int idx = barNumber1Based - 1;
            // Ensure index valid (caller should have called EnsureMeasures)
            if (idx < 0)
                idx = 0;
            if (idx >= part.Measures.Count)
                part.Measures.AddRange(Enumerable.Repeat(new Measure(), idx - part.Measures.Count + 1));

            return part.Measures[idx];
        }

        private static void EnsureMeasures(Part part, int upToBar1Based)
        {
            part.Measures ??= new List<Measure>();
            while (part.Measures.Count < upToBar1Based)
                part.Measures.Add(new Measure());
        }

        // NOTE: Use MeasureAttributes (the project's MusicXml.Domain type) instead of a generic 'Attributes' name.
        private static MeasureAttributes GetEffectiveAttributes(Part part, int barNumber1Based)
        {
            // Look current bar first, then walk backward for previous attributes, else defaults.
            EnsureMeasures(part, barNumber1Based);
            var m = GetMeasure(part, barNumber1Based);
            if (m.Attributes != null) return m.Attributes;

            for (int i = barNumber1Based - 1; i >= 1; i--)
            {
                var prev = GetMeasure(part, i);
                if (prev.Attributes != null) return prev.Attributes;
            }

            // Defaults: C major, 4/4, G clef, Divisions=480
            return new MeasureAttributes
            {
                Divisions = 480,
                Key = new Key { Fifths = 0, Mode = "major" },
                Time = new Time { Beats = 4, Mode = "4" },
                Clef = new Clef { Sign = "G", Line = 2 }
            };
        }

        private static void EnsureMeasureAttributes(Measure measure, MeasureAttributes attrs)
        {
            // If measure already has attrs, keep as-is; otherwise apply provided attrs
            if (measure.Attributes == null)
            {
                measure.Attributes = new MeasureAttributes
                {
                    Divisions = attrs.Divisions,
                    Key = attrs.Key != null ? new Key { Fifths = attrs.Key.Fifths, Mode = attrs.Key.Mode } : null,
                    Time = attrs.Time != null ? new Time { Beats = attrs.Time.Beats, Mode = attrs.Time.Mode } : null,
                    Clef = attrs.Clef != null ? new Clef { Sign = attrs.Clef.Sign, Line = attrs.Clef.Line } : null
                };
            }
        }

        private static void RemoveVoiceNotes(Measure measure, int voice)
        {
            if (measure.MeasureElements == null || measure.MeasureElements.Count == 0) return;

            // Remove only notes for the specified voice; leave other elements (backup/forward/attributes) as-is
            measure.MeasureElements.RemoveAll(me =>
                me.Type == MeasureElementType.Note &&
                me.Element is Note n &&
                n.Voice == voice
            );
        }

        private static int GetMeasureCapacityTicks(MeasureAttributes attrs)
        {
            if (attrs.Divisions <= 0 || attrs.Time == null || attrs.Time.Beats <= 0) return 0;

            // In MusicXML, duration is based on quarter = Divisions.
            // Bar capacity: beats * quartersPerBeat * divisions; where quartersPerBeat = 4 / beatType
            int beatType = 0;
            if (!string.IsNullOrWhiteSpace(attrs.Time.Mode))
                int.TryParse(attrs.Time.Mode, out beatType);
            if (beatType <= 0) beatType = 4; // assume quarter note beat
            double quartersPerBeat = 4.0 / beatType;
            double capacity = attrs.Divisions * attrs.Time.Beats * quartersPerBeat;
            return (int)Math.Round(capacity);
        }

        private static (char step, int alter) ComputePitchFromKeyDegree(Key key, int degree1To7)
        {
            // Determine key signature accidentals set from fifths
            var (sharpSet, flatSet) = BuildKeySignatureSets(key.Fifths);

            // Determine tonic letter for major/minor from circle of fifths
            var (tonicStep, tonicAlter) = GetTonicForKey(key.Fifths, key.Mode);

            // Walk diatonic letters from tonic
            char[] letters = new[] { 'C', 'D', 'E', 'F', 'G', 'A', 'B' };
            int tonicIdx = Array.IndexOf(letters, tonicStep);
            int idx = (tonicIdx + (degree1To7 - 1)) % 7;
            char step = letters[idx];

            // Apply signature accidental for that letter
            int alter = 0;
            if (sharpSet.Contains(step)) alter = 1;
            else if (flatSet.Contains(step)) alter = -1;

            if (degree1To7 == 1 && step == tonicStep)
                alter = tonicAlter;

            return (step, alter);
        }

        private static (HashSet<char> sharps, HashSet<char> flats) BuildKeySignatureSets(int fifths)
        {
            var sharpOrder = new[] { 'F', 'C', 'G', 'D', 'A', 'E', 'B' };
            var flatOrder = new[] { 'B', 'E', 'A', 'D', 'G', 'C', 'F' };

            var sharps = new HashSet<char>();
            var flats = new HashSet<char>();

            if (fifths > 0)
            {
                for (int i = 0; i < Math.Min(7, fifths); i++)
                    sharps.Add(sharpOrder[i]);
            }
            else if (fifths < 0)
            {
                for (int i = 0; i < Math.Min(7, -fifths); i++)
                    flats.Add(flatOrder[i]);
            }
            return (sharps, flats);
        }

        private static (char tonicStep, int tonicAlter) GetTonicForKey(int fifths, string? mode)
        {
            bool isMinor = string.Equals(mode, "minor", StringComparison.OrdinalIgnoreCase);

            var major = new Dictionary<int, (char, int)>
            {
                [-7] = ('C', -1),
                [-6] = ('G', -1),
                [-5] = ('D', -1),
                [-4] = ('A', -1),
                [-3] = ('E', -1),
                [-2] = ('B', -1),
                [-1] = ('F',  0),
                [ 0] = ('C',  0),
                [ 1] = ('G',  0),
                [ 2] = ('D',  0),
                [ 3] = ('A',  0),
                [ 4] = ('E',  0),
                [ 5] = ('B',  0),
                [ 6] = ('F',  1),
                [ 7] = ('C',  1),
            };

            var minor = new Dictionary<int, (char, int)>
            {
                [-7] = ('A', -1),
                [-6] = ('E', -1),
                [-5] = ('B', -1),
                [-4] = ('F',  0),
                [-3] = ('C',  0),
                [-2] = ('G',  0),
                [-1] = ('D',  0),
                [ 0] = ('A',  0),
                [ 1] = ('E',  0),
                [ 2] = ('B',  0),
                [ 3] = ('F',  1),
                [ 4] = ('C',  1),
                [ 5] = ('G',  1),
                [ 6] = ('D',  1),
                [ 7] = ('A',  1),
            };

            var table = isMinor ? minor : major;
            if (!table.TryGetValue(fifths, out var tonic))
                tonic = table[0];

            return tonic;
        }
    }
}