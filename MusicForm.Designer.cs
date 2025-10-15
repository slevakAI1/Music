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
            btnAddVoices = new Button();
            txtVoiceSet = new TextBox();
            SuspendLayout();
            // 
            // btnCreateStructure
            // 
            btnCreateStructure.Location = new Point(61, 122);
            btnCreateStructure.Name = "btnCreateStructure";
            btnCreateStructure.Size = new Size(151, 23);
            btnCreateStructure.TabIndex = 0;
            btnCreateStructure.Text = "Create Song Structure";
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
            lblMidiFilepath.Location = new Point(252, 72);
            lblMidiFilepath.Name = "lblMidiFilepath";
            lblMidiFilepath.Size = new Size(47, 15);
            lblMidiFilepath.TabIndex = 3;
            lblMidiFilepath.Text = "filepath";
            // 
            // btnAddVoices
            // 
            btnAddVoices.Location = new Point(52, 367);
            btnAddVoices.Name = "btnAddVoices";
            btnAddVoices.Size = new Size(160, 23);
            btnAddVoices.TabIndex = 4;
            btnAddVoices.Text = "Add Voices";
            btnAddVoices.UseVisualStyleBackColor = true;
            btnAddVoices.Click += btnAddVoices_Click;
            // 
            // txtVoiceSet
            // 
            txtVoiceSet.Location = new Point(252, 367);
            txtVoiceSet.Multiline = true;
            txtVoiceSet.Name = "txtVoiceSet";
            txtVoiceSet.Size = new Size(529, 173);
            txtVoiceSet.TabIndex = 5;
            // 
            // MusicForm
            // 
            ClientSize = new Size(1085, 635);
            Controls.Add(txtVoiceSet);
            Controls.Add(btnAddVoices);
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
        private Button btnAddVoices;
        private TextBox txtVoiceSet;
    }
}