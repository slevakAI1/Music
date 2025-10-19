using System.Windows.Forms;

namespace Music
{
    public partial class GenerateForm
    {
        private void InitializeComponent()
        {
            btnAddSections = new Button();
            btnSave = new Button();
            lblFilepath = new Label();
            btnAddVoices = new Button();
            btnAddChords = new Button();
            textBox1 = new TextBox();
            label1 = new Label();
            label2 = new Label();
            textBox2 = new TextBox();
            btnCreateScore = new Button();
            btnNewDesign = new Button();
            txtDesignSpace = new TextBox();
            SuspendLayout();
            // 
            // btnAddSections
            // 
            btnAddSections.Location = new Point(28, 61);
            btnAddSections.Name = "btnAddSections";
            btnAddSections.Size = new Size(151, 23);
            btnAddSections.TabIndex = 0;
            btnAddSections.Text = "Add Sections";
            btnAddSections.UseVisualStyleBackColor = true;
            btnAddSections.Click += btnCreateSections_Click;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(42, 256);
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
            lblFilepath.Location = new Point(147, 260);
            lblFilepath.Name = "lblFilepath";
            lblFilepath.Size = new Size(47, 15);
            lblFilepath.TabIndex = 3;
            lblFilepath.Text = "filepath";
            // 
            // btnAddVoices
            // 
            btnAddVoices.Location = new Point(28, 90);
            btnAddVoices.Name = "btnAddVoices";
            btnAddVoices.Size = new Size(151, 23);
            btnAddVoices.TabIndex = 4;
            btnAddVoices.Text = "Add Voices";
            btnAddVoices.UseVisualStyleBackColor = true;
            btnAddVoices.Click += btnAddVoices_Click;
            // 
            // btnAddChords
            // 
            btnAddChords.Location = new Point(28, 119);
            btnAddChords.Name = "btnAddChords";
            btnAddChords.Size = new Size(151, 23);
            btnAddChords.TabIndex = 6;
            btnAddChords.Text = "Add Chords";
            btnAddChords.UseVisualStyleBackColor = true;
            btnAddChords.Click += btnAddChords_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(135, 153);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(100, 23);
            textBox1.TabIndex = 8;
            textBox1.Text = "4/4";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(28, 156);
            label1.Name = "label1";
            label1.Size = new Size(87, 15);
            label1.TabIndex = 9;
            label1.Text = "Time Signature";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(28, 189);
            label2.Name = "label2";
            label2.Size = new Size(44, 15);
            label2.TabIndex = 11;
            label2.Text = "Tempo";
            // 
            // textBox2
            // 
            textBox2.Location = new Point(135, 186);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(100, 23);
            textBox2.TabIndex = 10;
            textBox2.Text = "90";
            // 
            // btnCreateScore
            // 
            btnCreateScore.Location = new Point(816, 450);
            btnCreateScore.Name = "btnCreateScore";
            btnCreateScore.Size = new Size(126, 23);
            btnCreateScore.TabIndex = 12;
            btnCreateScore.Text = "Create Score";
            btnCreateScore.UseVisualStyleBackColor = true;
            btnCreateScore.Click += btnCreateMusic_Click;
            // 
            // btnNewDesign
            // 
            btnNewDesign.Location = new Point(28, 32);
            btnNewDesign.Name = "btnNewDesign";
            btnNewDesign.Size = new Size(151, 23);
            btnNewDesign.TabIndex = 13;
            btnNewDesign.Text = "New Design";
            btnNewDesign.UseVisualStyleBackColor = true;
            btnNewDesign.Click += btnNewScore_Click;
            // 
            // txtDesignSpace
            // 
            txtDesignSpace.Location = new Point(280, 12);
            txtDesignSpace.Multiline = true;
            txtDesignSpace.Name = "txtDesignSpace";
            txtDesignSpace.Size = new Size(381, 354);
            txtDesignSpace.TabIndex = 14;
            // 
            // GenerateForm
            // 
            ClientSize = new Size(1085, 539);
            Controls.Add(txtDesignSpace);
            Controls.Add(btnNewDesign);
            Controls.Add(btnCreateScore);
            Controls.Add(label2);
            Controls.Add(textBox2);
            Controls.Add(label1);
            Controls.Add(textBox1);
            Controls.Add(btnAddChords);
            Controls.Add(btnAddVoices);
            Controls.Add(lblFilepath);
            Controls.Add(btnSave);
            Controls.Add(btnAddSections);
            Name = "GenerateForm";
            WindowState = FormWindowState.Maximized;
            Load += MusicForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }
        private Button btnAddSections;
        private Button btnSave;
        private Label lblFilepath;
        private Button btnAddVoices;
        private Button btnAddChords;
        private TextBox textBox1;
        private Label label1;
        private Label label2;
        private TextBox textBox2;
        private Button btnCreateScore;
        private Button btnNewDesign;
        private TextBox txtDesignSpace;
    }
}