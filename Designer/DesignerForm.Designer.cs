using System.Windows.Forms;

namespace Music
{
    public partial class DesignerForm
    {
        private void InitializeComponent()
        {
            btnEditSections = new Button();
            btnSave = new Button();
            btnEditVoices = new Button();
            btnNew = new Button();
            txtDesignerReport = new TextBox();
            btnEditHarmony = new Button();
            btnSetDefault = new Button();
            btnLoad = new Button();
            btnEditTimeSignature = new Button();
            btnEditTempo = new Button();
            lblEdit = new Label();
            lblDesign = new Label();
            label1 = new Label();
            panel1 = new Panel();
            panel2 = new Panel();
            btnEditGroove = new Button();
            label2 = new Label();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // btnEditSections
            // 
            btnEditSections.Enabled = false;
            btnEditSections.ForeColor = Color.Red;
            btnEditSections.Location = new Point(41, 381);
            btnEditSections.Name = "btnEditSections";
            btnEditSections.Size = new Size(96, 23);
            btnEditSections.TabIndex = 0;
            btnEditSections.Text = "Sections";
            btnEditSections.UseVisualStyleBackColor = true;
            btnEditSections.Click += btnEditSections_Click;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(41, 182);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(96, 23);
            btnSave.TabIndex = 2;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnEditVoices
            // 
            btnEditVoices.Location = new Point(41, 314);
            btnEditVoices.Name = "btnEditVoices";
            btnEditVoices.Size = new Size(96, 23);
            btnEditVoices.TabIndex = 4;
            btnEditVoices.Text = "Voices";
            btnEditVoices.UseVisualStyleBackColor = true;
            btnEditVoices.Click += btnSelectVoices_Click;
            // 
            // btnNew
            // 
            btnNew.Location = new Point(41, 149);
            btnNew.Name = "btnNew";
            btnNew.Size = new Size(96, 23);
            btnNew.TabIndex = 13;
            btnNew.Text = "New";
            btnNew.UseVisualStyleBackColor = true;
            btnNew.Click += btnNew_Click;
            // 
            // txtDesignerReport
            // 
            txtDesignerReport.Location = new Point(201, 132);
            txtDesignerReport.Multiline = true;
            txtDesignerReport.Name = "txtDesignerReport";
            txtDesignerReport.ScrollBars = ScrollBars.Vertical;
            txtDesignerReport.Size = new Size(824, 679);
            txtDesignerReport.TabIndex = 14;
            // 
            // btnEditHarmony
            // 
            btnEditHarmony.Enabled = false;
            btnEditHarmony.ForeColor = Color.Red;
            btnEditHarmony.Location = new Point(14, 148);
            btnEditHarmony.Name = "btnEditHarmony";
            btnEditHarmony.Size = new Size(96, 23);
            btnEditHarmony.TabIndex = 15;
            btnEditHarmony.Text = "Harmony";
            btnEditHarmony.UseVisualStyleBackColor = true;
            btnEditHarmony.Click += btnEditHarmony_Click;
            // 
            // btnSetDefault
            // 
            btnSetDefault.Location = new Point(36, 83);
            btnSetDefault.Name = "btnSetDefault";
            btnSetDefault.Size = new Size(119, 23);
            btnSetDefault.TabIndex = 16;
            btnSetDefault.Text = "Set Test Design D1";
            btnSetDefault.UseVisualStyleBackColor = true;
            btnSetDefault.Click += btnSetTestDesignD1_Click;
            // 
            // btnLoad
            // 
            btnLoad.Location = new Point(41, 215);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(96, 23);
            btnLoad.TabIndex = 17;
            btnLoad.Text = "Load";
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += btnLoad_Click;
            // 
            // btnEditTimeSignature
            // 
            btnEditTimeSignature.Enabled = false;
            btnEditTimeSignature.ForeColor = Color.Red;
            btnEditTimeSignature.Location = new Point(14, 45);
            btnEditTimeSignature.Name = "btnEditTimeSignature";
            btnEditTimeSignature.Size = new Size(96, 23);
            btnEditTimeSignature.TabIndex = 18;
            btnEditTimeSignature.Text = "Time Signature";
            btnEditTimeSignature.UseVisualStyleBackColor = true;
            btnEditTimeSignature.Click += btnEditTimeSignature_Click;
            // 
            // btnEditTempo
            // 
            btnEditTempo.Enabled = false;
            btnEditTempo.ForeColor = Color.Red;
            btnEditTempo.Location = new Point(41, 415);
            btnEditTempo.Name = "btnEditTempo";
            btnEditTempo.Size = new Size(96, 23);
            btnEditTempo.TabIndex = 19;
            btnEditTempo.Text = "Tempo";
            btnEditTempo.UseVisualStyleBackColor = true;
            btnEditTempo.Click += btnEditTempo_Click;
            // 
            // lblEdit
            // 
            lblEdit.AutoSize = true;
            lblEdit.Location = new Point(75, 293);
            lblEdit.Name = "lblEdit";
            lblEdit.Size = new Size(27, 15);
            lblEdit.TabIndex = 20;
            lblEdit.Text = "Edit";
            // 
            // lblDesign
            // 
            lblDesign.AutoSize = true;
            lblDesign.Location = new Point(72, 127);
            lblDesign.Name = "lblDesign";
            lblDesign.Size = new Size(43, 15);
            lblDesign.TabIndex = 21;
            lblDesign.Text = "Design";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 26.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.Location = new Point(492, 9);
            label1.Name = "label1";
            label1.Size = new Size(157, 47);
            label1.TabIndex = 22;
            label1.Text = "Designer";
            // 
            // panel1
            // 
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Location = new Point(26, 136);
            panel1.Name = "panel1";
            panel1.Size = new Size(138, 115);
            panel1.TabIndex = 23;
            // 
            // panel2
            // 
            panel2.BorderStyle = BorderStyle.FixedSingle;
            panel2.Controls.Add(btnEditGroove);
            panel2.Controls.Add(btnEditHarmony);
            panel2.Controls.Add(btnEditTimeSignature);
            panel2.Location = new Point(26, 301);
            panel2.Name = "panel2";
            panel2.Size = new Size(138, 220);
            panel2.TabIndex = 24;
            // 
            // btnEditGroove
            // 
            btnEditGroove.Location = new Point(14, 183);
            btnEditGroove.Name = "btnEditGroove";
            btnEditGroove.Size = new Size(96, 23);
            btnEditGroove.TabIndex = 19;
            btnEditGroove.Text = "Groove";
            btnEditGroove.UseVisualStyleBackColor = true;
            btnEditGroove.Click += btnEditGroove_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 15F);
            label2.Location = new Point(493, 96);
            label2.Name = "label2";
            label2.Size = new Size(153, 28);
            label2.TabIndex = 25;
            label2.Text = "Designer Report";
            // 
            // DesignerForm
            // 
            ClientSize = new Size(1085, 804);
            Controls.Add(label2);
            Controls.Add(btnEditVoices);
            Controls.Add(label1);
            Controls.Add(lblDesign);
            Controls.Add(lblEdit);
            Controls.Add(btnEditTempo);
            Controls.Add(btnLoad);
            Controls.Add(btnSetDefault);
            Controls.Add(txtDesignerReport);
            Controls.Add(btnNew);
            Controls.Add(btnSave);
            Controls.Add(btnEditSections);
            Controls.Add(panel1);
            Controls.Add(panel2);
            Name = "DesignerForm";
            WindowState = FormWindowState.Maximized;
            Load += MusicForm_Load;
            panel2.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }
        private Button btnEditSections;
        private Button btnSave;
        private Button btnEditVoices;
        private Button btnAddChords;
        private Button btnNew;
        private TextBox txtDesignerReport;
        private Button btnEditHarmony;
        private Button btnSetDefault;
        private Button btnLoad;
        private Button btnEditTimeSignature;
        private Button btnEditTempo;
        private Label lblEdit;
        private Label lblDesign;
        private Label label1;
        private Panel panel1;
        private Panel panel2;
        private Label label2;
        private Button btnEditGroove;
    }
}