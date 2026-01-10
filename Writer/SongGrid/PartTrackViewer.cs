// AI: purpose=Modal viewer showing a PartTrack as formatted JSON for debugging and inspection.
// AI: invariants=Read-only viewer; uses ObjectViewer.Json for serialization; large tracks may produce very large strings.
// AI: deps=Depends on PartTrack structure and ObjectViewer.Json; changing those requires updating this viewer.
// AI: change=If PartTrack shape or JSON helper changes, update error handling to avoid runtime exceptions in viewer.

using Music;
using Music.Generator;
using Music.MyMidi;

namespace Music.Writer
{
    public class PartTrackViewer : Form
    {
        private TextBox txtJson;
        private Button btnClose;

        // AI: ctor: initialize UI and load given PartTrack with measure dividers based on timeSignatureTrack.
        public PartTrackViewer(PartTrack track, string trackNumber, Timingtrack? timeSignatureTrack)
        {
            InitializeComponents(trackNumber);
            LoadTrackData(track, timeSignatureTrack);
        }

        private void InitializeComponents(string trackNumber)
        {
            this.Text = $"Part Viewer - #{trackNumber}";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimizeBox = false;
            this.MaximizeBox = true;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.BackColor = Color.Black;

            txtJson = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                WordWrap = false,
                Font = new Font("Consolas", 10F),
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                BorderStyle = BorderStyle.None,
                TabStop = false
            };

            btnClose = new Button
            {
                Text = "Close",
                Size = new Size(100, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(255, 128, 0);
            btnClose.Click += (s, e) => this.Close();

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.Black,
                Padding = new Padding(10)
            };
            buttonPanel.Controls.Add(btnClose);
            btnClose.Location = new Point(buttonPanel.Width - btnClose.Width - 10, 10);

            this.Controls.Add(txtJson);
            this.Controls.Add(buttonPanel);
        }

        // AI: LoadTrackData: serialize PartTrack with measure dividers aligned to time signatures; on failure display error.
        private void LoadTrackData(PartTrack track, Timingtrack? timeSignatureTrack)
        {
            try
            {
                if (timeSignatureTrack == null || timeSignatureTrack.Events.Count == 0)
                {
                    // No time signature data - fall back to plain JSON
                    string json = ObjectViewer.Json(track);
                    txtJson.Text = json;
                    return;
                }

                // Build formatted output with measure dividers
                var output = new System.Text.StringBuilder();
                
                // Group events by measure
                var eventsByMeasure = GroupEventsByMeasure(track, timeSignatureTrack);
                
                // Header
                output.AppendLine($"PartTrack: {track.MidiProgramName ?? "Unknown"} (Program #{track.MidiProgramNumber})");
                output.AppendLine($"Total Events: {track.PartTrackNoteEvents.Count}");
                output.AppendLine();
                
                // Output events grouped by measure
                foreach (var kvp in eventsByMeasure.OrderBy(x => x.Key))
                {
                    int measureNumber = kvp.Key;
                    var events = kvp.Value;
                    
                    // Measure divider
                    output.AppendLine();
                    output.AppendLine($"---- Measure {measureNumber} ----");
                    output.AppendLine();
                    
                    // Events in this measure
                    foreach (var evt in events)
                    {
                        output.AppendLine(ObjectViewer.Json(evt));
                    }
                }
                
                txtJson.Text = output.ToString();
            }
            catch (Exception ex)
            {
                txtJson.Text = $"Error loading track data:\r\n{ex.Message}";
            }
        }

        private Dictionary<int, List<PartTrackEvent>> GroupEventsByMeasure(PartTrack track, Timingtrack timeSignatureTrack)
        {
            var result = new Dictionary<int, List<PartTrackEvent>>();
            
            foreach (var evt in track.PartTrackNoteEvents)
            {
                int measure = CalculateMeasureForTick(evt.AbsoluteTimeTicks, timeSignatureTrack);
                
                if (!result.ContainsKey(measure))
                    result[measure] = new List<PartTrackEvent>();
                
                result[measure].Add(evt);
            }
            
            return result;
        }

        private int CalculateMeasureForTick(long tick, Timingtrack timeSignatureTrack)
        {
            long currentTick = 0;
            int currentMeasure = 1;
            
            // Sort events by StartBar to ensure proper iteration
            var sortedEvents = timeSignatureTrack.Events.OrderBy(e => e.StartBar).ToList();
            
            for (int i = 0; i < sortedEvents.Count; i++)
            {
                var timeSig = sortedEvents[i];
                int startBar = timeSig.StartBar;
                
                // Calculate ticks from current measure to the start of this time signature event
                while (currentMeasure < startBar)
                {
                    var activeTimeSig = timeSignatureTrack.GetActiveTimeSignatureEvent(currentMeasure);
                    if (activeTimeSig == null)
                        break;
                    
                    int ticksPerMeasure = (MusicConstants.TicksPerQuarterNote * 4 * activeTimeSig.Numerator) / activeTimeSig.Denominator;
                    
                    if (tick >= currentTick && tick < currentTick + ticksPerMeasure)
                        return currentMeasure;
                    
                    currentTick += ticksPerMeasure;
                    currentMeasure++;
                }
                
                // Now process measures with this time signature
                int nextStartBar = (i + 1 < sortedEvents.Count) ? sortedEvents[i + 1].StartBar : int.MaxValue;
                
                while (currentMeasure < nextStartBar)
                {
                    int ticksPerMeasure = (MusicConstants.TicksPerQuarterNote * 4 * timeSig.Numerator) / timeSig.Denominator;
                    
                    if (tick >= currentTick && tick < currentTick + ticksPerMeasure)
                        return currentMeasure;
                    
                    currentTick += ticksPerMeasure;
                    currentMeasure++;
                    
                    // Safety check to avoid infinite loop
                    if (currentTick > tick + (ticksPerMeasure * 1000))
                        break;
                }
            }
            
            // If we haven't found the measure yet, continue with the last time signature
            var lastTimeSig = timeSignatureTrack.Events.OrderBy(e => e.StartBar).LastOrDefault();
            if (lastTimeSig != null)
            {
                int ticksPerMeasure = (MusicConstants.TicksPerQuarterNote * 4 * lastTimeSig.Numerator) / lastTimeSig.Denominator;
                
                while (currentTick <= tick)
                {
                    if (tick >= currentTick && tick < currentTick + ticksPerMeasure)
                        return currentMeasure;
                    
                    currentTick += ticksPerMeasure;
                    currentMeasure++;
                    
                    // Safety check
                    if (currentMeasure > 10000)
                        break;
                }
            }
            
            return currentMeasure;
        }
    }
}