// AI: purpose=Modal viewer showing a PartTrack as formatted JSON for debugging and inspection.
// AI: invariants=Read-only viewer; uses ObjectViewer.Json for serialization; large tracks may produce very large strings.
// AI: deps=Depends on PartTrack structure and ObjectViewer.Json; changing those requires updating this viewer.
// AI: change=If PartTrack shape or JSON helper changes, update error handling to avoid runtime exceptions in viewer.

using Music.Generator;

namespace Music.Writer
{
    public class PartTrackViewer : Form
    {
        private TextBox txtJson;
        private Button btnClose;

        // AI: ctor: initialize UI and load given PartTrack; trackNumber used only for title.
        public PartTrackViewer(PartTrack track, string trackNumber)
        {
            InitializeComponents(trackNumber);
            LoadTrackData(track);
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

        // AI: LoadTrackData: serialize PartTrack via ObjectViewer.Json; on failure display error in textbox only.
        private void LoadTrackData(PartTrack track)
        {
            try
            {
                string json = ObjectViewer.Json(track);
                txtJson.Text = json;
            }
            catch (Exception ex)
            {
                txtJson.Text = $"Error loading track data:\r\n{ex.Message}";
            }
        }
    }
}