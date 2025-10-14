using System.Windows.Forms;

namespace Music
{
    public partial class MusicForm
    {
        private void InitializeComponent()
        {
            btnCreateStructure = new Button();
            txtSongStructure = new TextBox();
            btnPlayMidi = new Button();
            lblMidiFilepath = new Label();
            SuspendLayout();
            // 
            // btnCreateStructure
            // 
            btnCreateStructure.Location = new Point(52, 120);
            btnCreateStructure.Name = "btnCreateStructure";
            btnCreateStructure.Size = new Size(75, 23);
            btnCreateStructure.TabIndex = 0;
            btnCreateStructure.Text = "Create Structure";
            btnCreateStructure.UseVisualStyleBackColor = true;
            btnCreateStructure.Click += btnCreateStructure_Click;
            // 
            // txtSongStructure
            // 
            txtSongStructure.Location = new Point(252, 123);
            txtSongStructure.Multiline = true;
            txtSongStructure.Name = "txtSongStructure";
            txtSongStructure.Size = new Size(529, 197);
            txtSongStructure.TabIndex = 1;
            // 
            // btnPlayMidi
            // 
            btnPlayMidi.Location = new Point(61, 68);
            btnPlayMidi.Name = "btnPlayMidi";
            btnPlayMidi.Size = new Size(151, 23);
            btnPlayMidi.TabIndex = 2;
            btnPlayMidi.Text = "Play";
            btnPlayMidi.UseVisualStyleBackColor = true;
            btnPlayMidi.Click += btnPlayMidi_Click;
            // 
            // lblMidiFilepath
            // 
            lblMidiFilepath.AutoSize = true;
            lblMidiFilepath.Location = new Point(260, 72);
            lblMidiFilepath.Name = "lblMidiFilepath";
            lblMidiFilepath.Size = new Size(38, 15);
            lblMidiFilepath.TabIndex = 3;
            lblMidiFilepath.Text = "label1";
            // 
            // MusicForm
            // 
            //ClientSize = new Size(1144, 754);
            Controls.Add(lblMidiFilepath);
            Controls.Add(btnPlayMidi);
            Controls.Add(txtSongStructure);
            Controls.Add(btnCreateStructure);
            Name = "MusicForm";
            WindowState = FormWindowState.Maximized;
            Load += MusicForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }
        private Button btnCreateStructure;
        private TextBox txtSongStructure;
        private Button btnPlayMidi;
        private Label lblMidiFilepath;
    }
}