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
            txtChordSet = new TextBox();
            btnAddChords = new Button();
            textBox1 = new TextBox();
            label1 = new Label();
            label2 = new Label();
            textBox2 = new TextBox();
            txtExportTestChords = new Button();
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
            txtVoiceSet.Size = new Size(180, 173);
            txtVoiceSet.TabIndex = 5;
            // 
            // txtChordSet
            // 
            txtChordSet.Location = new Point(691, 367);
            txtChordSet.Multiline = true;
            txtChordSet.Name = "txtChordSet";
            txtChordSet.Size = new Size(180, 173);
            txtChordSet.TabIndex = 7;
            // 
            // btnAddChords
            // 
            btnAddChords.Location = new Point(491, 367);
            btnAddChords.Name = "btnAddChords";
            btnAddChords.Size = new Size(160, 23);
            btnAddChords.TabIndex = 6;
            btnAddChords.Text = "Add Chords";
            btnAddChords.UseVisualStyleBackColor = true;
            btnAddChords.Click += btnAddChords_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(203, 584);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(100, 23);
            textBox1.TabIndex = 8;
            textBox1.Text = "4/4";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(96, 591);
            label1.Name = "label1";
            label1.Size = new Size(87, 15);
            label1.TabIndex = 9;
            label1.Text = "Time Signature";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(96, 642);
            label2.Name = "label2";
            label2.Size = new Size(44, 15);
            label2.TabIndex = 11;
            label2.Text = "Tempo";
            // 
            // textBox2
            // 
            textBox2.Location = new Point(205, 634);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(100, 23);
            textBox2.TabIndex = 10;
            textBox2.Text = "90";
            // 
            // txtExportTestChords
            // 
            txtExportTestChords.Location = new Point(434, 656);
            txtExportTestChords.Name = "txtExportTestChords";
            txtExportTestChords.Size = new Size(203, 23);
            txtExportTestChords.TabIndex = 12;
            txtExportTestChords.Text = "Test Chords";
            txtExportTestChords.UseVisualStyleBackColor = true;
            txtExportTestChords.Click += txtTestChords_Click;
            // 
            // MusicForm
            // 
            ClientSize = new Size(1085, 757);
            Controls.Add(txtExportTestChords);
            Controls.Add(label2);
            Controls.Add(textBox2);
            Controls.Add(label1);
            Controls.Add(textBox1);
            Controls.Add(txtChordSet);
            Controls.Add(btnAddChords);
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
        private TextBox txtChordSet;
        private Button btnAddChords;
        private TextBox textBox1;
        private Label label1;
        private Label label2;
        private TextBox textBox2;
        private Button txtExportTestChords;
    }
}