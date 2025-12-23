using Music.Generator;

namespace Music.Writer
{
    /// <summary>
    /// A modal dialog that displays a PartTrack object as formatted JSON.
    /// </summary>
    public class SongTrackViewer : Form
    {
        private TextBox txtJson;
        private Button btnClose;

        public SongTrackViewer(PartTrack track, string trackNumber)
        {
            InitializeComponents(trackNumber);
            LoadTrackData(track);
        }

        private void InitializeComponents(string trackNumber)
        {
            // Form settings
            this.Text = $"Part Viewer - #{trackNumber}";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimizeBox = false;
            this.MaximizeBox = true;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.BackColor = Color.Black;

            // TextBox for JSON display
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

            // Close button
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

            // Panel for button
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.Black,
                Padding = new Padding(10)
            };
            buttonPanel.Controls.Add(btnClose);
            btnClose.Location = new Point(buttonPanel.Width - btnClose.Width - 10, 10);

            // Add controls to form
            this.Controls.Add(txtJson);
            this.Controls.Add(buttonPanel);
        }

        private void LoadTrackData(PartTrack track)
        {
            try
            {
                // Use the existing Helpers.Json method to convert to JSON
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