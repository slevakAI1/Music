using System.Windows.Forms;

namespace Music
{
    public partial class GenerateForm
    {
        private void InitializeComponent()
        {
            btnCreateStructure = new Button();
            txtSongStructure = new TextBox();
            btnSave = new Button();
            lblFilepath = new Label();
            btnAddVoices = new Button();
            txtVoiceSet = new TextBox();
            txtChordSet = new TextBox();
            btnAddChords = new Button();
            textBox1 = new TextBox();
            label1 = new Label();
            label2 = new Label();
            textBox2 = new TextBox();
            btnCreateScore = new Button();
            SuspendLayout();
            // 
            // btnCreateStructure
            // 
            btnCreateStructure.Location = new Point(28, 49);
            btnCreateStructure.Name = "btnCreateStructure";
            btnCreateStructure.Size = new Size(151, 23);
            btnCreateStructure.TabIndex = 0;
            btnCreateStructure.Text = "Create Song Structure";
            btnCreateStructure.UseVisualStyleBackColor = true;
            btnCreateStructure.Click += btnCreateScoreStructure_Click;
            // 
            // txtSongStructure
            // 
            txtSongStructure.Location = new Point(212, 49);
            txtSongStructure.Multiline = true;
            txtSongStructure.Name = "txtSongStructure";
            txtSongStructure.Size = new Size(529, 89);
            txtSongStructure.TabIndex = 1;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(423, 411);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(151, 23);
            btnSave.TabIndex = 2;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // lblFilepath
            // 
            lblFilepath.AutoSize = true;
            lblFilepath.Location = new Point(599, 415);
            lblFilepath.Name = "lblFilepath";
            lblFilepath.Size = new Size(47, 15);
            lblFilepath.TabIndex = 3;
            lblFilepath.Text = "filepath";
            // 
            // btnAddVoices
            // 
            btnAddVoices.Location = new Point(28, 172);
            btnAddVoices.Name = "btnAddVoices";
            btnAddVoices.Size = new Size(160, 23);
            btnAddVoices.TabIndex = 4;
            btnAddVoices.Text = "Add Voices";
            btnAddVoices.UseVisualStyleBackColor = true;
            btnAddVoices.Click += btnAddVoices_Click;
            // 
            // txtVoiceSet
            // 
            txtVoiceSet.Location = new Point(215, 172);
            txtVoiceSet.Multiline = true;
            txtVoiceSet.Name = "txtVoiceSet";
            txtVoiceSet.Size = new Size(180, 76);
            txtVoiceSet.TabIndex = 5;
            // 
            // txtChordSet
            // 
            txtChordSet.Location = new Point(667, 172);
            txtChordSet.Multiline = true;
            txtChordSet.Name = "txtChordSet";
            txtChordSet.Size = new Size(180, 76);
            txtChordSet.TabIndex = 7;
            // 
            // btnAddChords
            // 
            btnAddChords.Location = new Point(467, 172);
            btnAddChords.Name = "btnAddChords";
            btnAddChords.Size = new Size(160, 23);
            btnAddChords.TabIndex = 6;
            btnAddChords.Text = "Add Chords";
            btnAddChords.UseVisualStyleBackColor = true;
            btnAddChords.Click += btnAddChords_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(142, 271);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(100, 23);
            textBox1.TabIndex = 8;
            textBox1.Text = "4/4";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(35, 278);
            label1.Name = "label1";
            label1.Size = new Size(87, 15);
            label1.TabIndex = 9;
            label1.Text = "Time Signature";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(35, 329);
            label2.Name = "label2";
            label2.Size = new Size(44, 15);
            label2.TabIndex = 11;
            label2.Text = "Tempo";
            // 
            // textBox2
            // 
            textBox2.Location = new Point(144, 321);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(100, 23);
            textBox2.TabIndex = 10;
            textBox2.Text = "90";
            // 
            // btnCreateScore
            // 
            btnCreateScore.Location = new Point(423, 382);
            btnCreateScore.Name = "btnCreateScore";
            btnCreateScore.Size = new Size(238, 23);
            btnCreateScore.TabIndex = 12;
            btnCreateScore.Text = "Create Score";
            btnCreateScore.UseVisualStyleBackColor = true;
            btnCreateScore.Click += btnCreateMusic_Click;
            // 
            // GenerateForm
            // 
            ClientSize = new Size(1085, 539);
            Controls.Add(btnCreateScore);
            Controls.Add(label2);
            Controls.Add(textBox2);
            Controls.Add(label1);
            Controls.Add(textBox1);
            Controls.Add(txtChordSet);
            Controls.Add(btnAddChords);
            Controls.Add(txtVoiceSet);
            Controls.Add(btnAddVoices);
            Controls.Add(lblFilepath);
            Controls.Add(btnSave);
            Controls.Add(txtSongStructure);
            Controls.Add(btnCreateStructure);
            Name = "GenerateForm";
            WindowState = FormWindowState.Maximized;
            Load += MusicForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }
        private Button btnCreateStructure;
        private TextBox txtSongStructure;
        private Button btnSave;
        private Label lblFilepath;
        private Button btnAddVoices;
        private TextBox txtVoiceSet;
        private TextBox txtChordSet;
        private Button btnAddChords;
        private TextBox textBox1;
        private Label label1;
        private Label label2;
        private TextBox textBox2;
        private Button btnCreateScore;
    }
}