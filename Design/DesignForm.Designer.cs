using System.Windows.Forms;

namespace Music
{
    public partial class DesignForm
    {
        private void InitializeComponent()
        {
            btnEditSections = new Button();
            btnSave = new Button();
            btnEditVoices = new Button();
            btnNew = new Button();
            txtDesignView = new TextBox();
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
            SuspendLayout();
            // 
            // btnEditSections
            // 
            btnEditSections.Location = new Point(43, 316);
            btnEditSections.Name = "btnEditSections";
            btnEditSections.Size = new Size(96, 23);
            btnEditSections.TabIndex = 0;
            btnEditSections.Text = "Sections";
            btnEditSections.UseVisualStyleBackColor = true;
            btnEditSections.Click += btnEditSections_Click;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(43, 149);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(96, 23);
            btnSave.TabIndex = 2;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnEditVoices
            // 
            btnEditVoices.Location = new Point(43, 281);
            btnEditVoices.Name = "btnEditVoices";
            btnEditVoices.Size = new Size(96, 23);
            btnEditVoices.TabIndex = 4;
            btnEditVoices.Text = "Voices";
            btnEditVoices.UseVisualStyleBackColor = true;
            btnEditVoices.Click += btnSelectVoices_Click;
            // 
            // btnNew
            // 
            btnNew.Location = new Point(43, 116);
            btnNew.Name = "btnNew";
            btnNew.Size = new Size(96, 23);
            btnNew.TabIndex = 13;
            btnNew.Text = "New";
            btnNew.UseVisualStyleBackColor = true;
            btnNew.Click += btnNew_Click;
            // 
            // txtDesignView
            // 
            txtDesignView.Location = new Point(201, 64);
            txtDesignView.Multiline = true;
            txtDesignView.Name = "txtDesignView";
            txtDesignView.ScrollBars = ScrollBars.Vertical;
            txtDesignView.Size = new Size(824, 747);
            txtDesignView.TabIndex = 14;
            // 
            // btnEditHarmony
            // 
            btnEditHarmony.Location = new Point(43, 351);
            btnEditHarmony.Name = "btnEditHarmony";
            btnEditHarmony.Size = new Size(96, 23);
            btnEditHarmony.TabIndex = 15;
            btnEditHarmony.Text = "Harmony";
            btnEditHarmony.UseVisualStyleBackColor = true;
            btnEditHarmony.Click += btnEditHarmony_Click;
            // 
            // btnSetDefault
            // 
            btnSetDefault.Location = new Point(43, 83);
            btnSetDefault.Name = "btnSetDefault";
            btnSetDefault.Size = new Size(96, 23);
            btnSetDefault.TabIndex = 16;
            btnSetDefault.Text = "Set Default Timeline";
            btnSetDefault.UseVisualStyleBackColor = true;
            btnSetDefault.Click += btnSetDefault_Click;
            // 
            // btnLoad
            // 
            btnLoad.Location = new Point(43, 182);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(96, 23);
            btnLoad.TabIndex = 17;
            btnLoad.Text = "Load";
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += btnLoad_Click;
            // 
            // btnEditTimeSignature
            // 
            btnEditTimeSignature.Location = new Point(43, 386);
            btnEditTimeSignature.Name = "btnEditTimeSignature";
            btnEditTimeSignature.Size = new Size(96, 23);
            btnEditTimeSignature.TabIndex = 18;
            btnEditTimeSignature.Text = "Time Signature";
            btnEditTimeSignature.UseVisualStyleBackColor = true;
            btnEditTimeSignature.Click += btnEditTimeSignature_Click;
            // 
            // btnEditTempo
            // 
            btnEditTempo.Location = new Point(43, 421);
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
            lblEdit.Location = new Point(77, 260);
            lblEdit.Name = "lblEdit";
            lblEdit.Size = new Size(27, 15);
            lblEdit.TabIndex = 20;
            lblEdit.Text = "Edit";
            // 
            // lblDesign
            // 
            lblDesign.AutoSize = true;
            lblDesign.Location = new Point(72, 64);
            lblDesign.Name = "lblDesign";
            lblDesign.Size = new Size(43, 15);
            lblDesign.TabIndex = 21;
            lblDesign.Text = "Design";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 26.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.Location = new Point(495, 9);
            label1.Name = "label1";
            label1.Size = new Size(127, 47);
            label1.TabIndex = 22;
            label1.Text = "Design";
            // 
            // panel1
            // 
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Location = new Point(28, 73);
            panel1.Name = "panel1";
            panel1.Size = new Size(138, 145);
            panel1.TabIndex = 23;
            // 
            // panel2
            // 
            panel2.BorderStyle = BorderStyle.FixedSingle;
            panel2.Location = new Point(28, 268);
            panel2.Name = "panel2";
            panel2.Size = new Size(138, 191);
            panel2.TabIndex = 24;
            // 
            // DesignForm
            // 
            ClientSize = new Size(1085, 804);
            Controls.Add(btnEditVoices);
            Controls.Add(label1);
            Controls.Add(lblDesign);
            Controls.Add(lblEdit);
            Controls.Add(btnEditTempo);
            Controls.Add(btnEditTimeSignature);
            Controls.Add(btnLoad);
            Controls.Add(btnSetDefault);
            Controls.Add(btnEditHarmony);
            Controls.Add(txtDesignView);
            Controls.Add(btnNew);
            Controls.Add(btnSave);
            Controls.Add(btnEditSections);
            Controls.Add(panel1);
            Controls.Add(panel2);
            Name = "DesignForm";
            WindowState = FormWindowState.Maximized;
            Load += MusicForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }
        private Button btnEditSections;
        private Button btnSave;
        private Button btnEditVoices;
        private Button btnAddChords;
        private Button btnNew;
        private TextBox txtDesignView;
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
    }
}