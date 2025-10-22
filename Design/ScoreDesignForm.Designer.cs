using System.Windows.Forms;

namespace Music
{
    public partial class ScoreDesignForm
    {
        private void InitializeComponent()
        {
            btnEditSections = new Button();
            btnSave = new Button();
            lblFilepath = new Label();
            btnSelectVoices = new Button();
            //btnAddChords = new Button();
            textBox1 = new TextBox();
            label1 = new Label();
            label2 = new Label();
            textBox2 = new TextBox();
            btnCreateScore = new Button();
            btnNew = new Button();
            txtDesignSpace = new TextBox();
            btnEditHarmonicTimeline = new Button();
            SuspendLayout();
            // 
            // btnEditSections
            // 
            btnEditSections.Location = new Point(28, 94);
            btnEditSections.Name = "btnEditSections";
            btnEditSections.Size = new Size(151, 23);
            btnEditSections.TabIndex = 0;
            btnEditSections.Text = "Edit Sections";
            btnEditSections.UseVisualStyleBackColor = true;
            btnEditSections.Click += btnEditSections_Click;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(42, 317);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(96, 23);
            btnSave.TabIndex = 2;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // lblFilepath
            // 
            lblFilepath.AutoSize = true;
            lblFilepath.Location = new Point(147, 321);
            lblFilepath.Name = "lblFilepath";
            lblFilepath.Size = new Size(47, 15);
            lblFilepath.TabIndex = 3;
            lblFilepath.Text = "filepath";
            // 
            // btnSelectVoices
            // 
            btnSelectVoices.Location = new Point(28, 61);
            btnSelectVoices.Name = "btnSelectVoices";
            btnSelectVoices.Size = new Size(151, 23);
            btnSelectVoices.TabIndex = 4;
            btnSelectVoices.Text = "Select Voices";
            btnSelectVoices.UseVisualStyleBackColor = true;
            btnSelectVoices.Click += btnSelectVoices_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(135, 214);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(100, 23);
            textBox1.TabIndex = 8;
            textBox1.Text = "4/4";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(28, 217);
            label1.Name = "label1";
            label1.Size = new Size(87, 15);
            label1.TabIndex = 9;
            label1.Text = "Time Signature";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(28, 250);
            label2.Name = "label2";
            label2.Size = new Size(44, 15);
            label2.TabIndex = 11;
            label2.Text = "Tempo";
            // 
            // textBox2
            // 
            textBox2.Location = new Point(135, 247);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(100, 23);
            textBox2.TabIndex = 10;
            textBox2.Text = "90";
            // 
            // btnCreateScore
            // 
            btnCreateScore.BackColor = Color.Red;
            btnCreateScore.Location = new Point(56, 384);
            btnCreateScore.Name = "btnCreateScore";
            btnCreateScore.Size = new Size(126, 23);
            btnCreateScore.TabIndex = 12;
            btnCreateScore.Text = "Create Score";
            btnCreateScore.UseVisualStyleBackColor = false;
            btnCreateScore.Click += btnCreateMusic_Click;
            // 
            // btnNew
            // 
            btnNew.Location = new Point(28, 32);
            btnNew.Name = "btnNew";
            btnNew.Size = new Size(151, 23);
            btnNew.TabIndex = 13;
            btnNew.Text = "New";
            btnNew.UseVisualStyleBackColor = true;
            btnNew.Click += btnNew_Click;
            // 
            // txtDesignSpace
            // 
            txtDesignSpace.Location = new Point(280, 12);
            txtDesignSpace.Multiline = true;
            txtDesignSpace.Name = "txtDesignSpace";
            txtDesignSpace.ScrollBars = ScrollBars.Vertical;
            txtDesignSpace.Size = new Size(775, 631);
            txtDesignSpace.TabIndex = 14;
            // 
            // btnEditHarmonicTimeline
            // 
            btnEditHarmonicTimeline.Location = new Point(28, 128);
            btnEditHarmonicTimeline.Name = "btnEditHarmonicTimeline";
            btnEditHarmonicTimeline.Size = new Size(151, 23);
            btnEditHarmonicTimeline.TabIndex = 15;
            btnEditHarmonicTimeline.Text = "Edit Harmonic Timeline";
            btnEditHarmonicTimeline.UseVisualStyleBackColor = true;
            btnEditHarmonicTimeline.Click += btnEditHarmonicTimeline_Click;
            // 
            // DesignForm
            // 
            ClientSize = new Size(1085, 655);
            Controls.Add(btnEditHarmonicTimeline);
            Controls.Add(txtDesignSpace);
            Controls.Add(btnNew);
            Controls.Add(btnCreateScore);
            Controls.Add(label2);
            Controls.Add(textBox2);
            Controls.Add(label1);
            Controls.Add(textBox1);
            Controls.Add(btnAddChords);
            Controls.Add(btnSelectVoices);
            Controls.Add(lblFilepath);
            Controls.Add(btnSave);
            Controls.Add(btnEditSections);
            Name = "DesignForm";
            WindowState = FormWindowState.Maximized;
            Load += MusicForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }
        private Button btnEditSections;
        private Button btnSave;
        private Label lblFilepath;
        private Button btnSelectVoices;
        private Button btnAddChords;
        private TextBox textBox1;
        private Label label1;
        private Label label2;
        private TextBox textBox2;
        private Button btnCreateScore;
        private Button btnNew;
        private TextBox txtDesignSpace;
        private Button btnEditHarmonicTimeline;
    }
}