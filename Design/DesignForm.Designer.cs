using System.Windows.Forms;

namespace Music
{
    public partial class DesignForm
    {
        private void InitializeComponent()
        {
            btnEditSections = new Button();
            btnSave = new Button();
            btnSelectVoices = new Button();
            btnNew = new Button();
            txtDesignView = new TextBox();
            btnEditHarmonicTimeline = new Button();
            btnSetDefault = new Button();
            btnLoad = new Button();
            btnEditTimeSignature = new Button();
            btnEditTempo = new Button();
            SuspendLayout();
            // 
            // btnEditSections
            // 
            btnEditSections.Location = new Point(28, 95);
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
            // btnSelectVoices
            // 
            btnSelectVoices.Location = new Point(28, 63);
            btnSelectVoices.Name = "btnSelectVoices";
            btnSelectVoices.Size = new Size(151, 23);
            btnSelectVoices.TabIndex = 4;
            btnSelectVoices.Text = "Select Voices";
            btnSelectVoices.UseVisualStyleBackColor = true;
            btnSelectVoices.Click += btnSelectVoices_Click;
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
            // txtDesignView
            // 
            txtDesignView.Location = new Point(280, 12);
            txtDesignView.Multiline = true;
            txtDesignView.Name = "txtDesignView";
            txtDesignView.ScrollBars = ScrollBars.Vertical;
            txtDesignView.Size = new Size(775, 631);
            txtDesignView.TabIndex = 14;
            // 
            // btnEditHarmonicTimeline
            // 
            btnEditHarmonicTimeline.Location = new Point(28, 127);
            btnEditHarmonicTimeline.Name = "btnEditHarmonicTimeline";
            btnEditHarmonicTimeline.Size = new Size(151, 23);
            btnEditHarmonicTimeline.TabIndex = 15;
            btnEditHarmonicTimeline.Text = "Edit Harmonic Timeline";
            btnEditHarmonicTimeline.UseVisualStyleBackColor = true;
            btnEditHarmonicTimeline.Click += btnEditHarmonicTimeline_Click;
            // 
            // btnSetDefault
            // 
            btnSetDefault.Location = new Point(28, 245);
            btnSetDefault.Name = "btnSetDefault";
            btnSetDefault.Size = new Size(151, 23);
            btnSetDefault.TabIndex = 16;
            btnSetDefault.Text = "Set Default Timeline";
            btnSetDefault.UseVisualStyleBackColor = true;
            btnSetDefault.Click += btnSetDefault_Click;
            // 
            // btnLoad
            // 
            btnLoad.Location = new Point(42, 351);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(96, 23);
            btnLoad.TabIndex = 17;
            btnLoad.Text = "Load";
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += btnLoad_Click;
            // 
            // btnEditTimeSignature
            // 
            btnEditTimeSignature.Location = new Point(28, 162);
            btnEditTimeSignature.Name = "btnEditTimeSignature";
            btnEditTimeSignature.Size = new Size(151, 23);
            btnEditTimeSignature.TabIndex = 18;
            btnEditTimeSignature.Text = "Edit Time Signature";
            btnEditTimeSignature.UseVisualStyleBackColor = true;
            btnEditTimeSignature.Click += btnEditTimeSignature_Click;
            // 
            // btnEditTempo
            // 
            btnEditTempo.Location = new Point(28, 202);
            btnEditTempo.Name = "btnEditTempo";
            btnEditTempo.Size = new Size(151, 23);
            btnEditTempo.TabIndex = 19;
            btnEditTempo.Text = "Edit Tempo";
            btnEditTempo.UseVisualStyleBackColor = true;
            btnEditTempo.Click += btnEditTempo_Click;
            // 
            // DesignForm
            // 
            ClientSize = new Size(1085, 655);
            Controls.Add(btnEditTempo);
            Controls.Add(btnEditTimeSignature);
            Controls.Add(btnLoad);
            Controls.Add(btnSetDefault);
            Controls.Add(btnEditHarmonicTimeline);
            Controls.Add(txtDesignView);
            Controls.Add(btnNew);
            Controls.Add(btnSelectVoices);
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
        private Button btnSelectVoices;
        private Button btnAddChords;
        private Button btnNew;
        private TextBox txtDesignView;
        private Button btnEditHarmonicTimeline;
        private Button btnSetDefault;
        private Button btnLoad;
        private Button btnEditTimeSignature;
        private Button btnEditTempo;
    }
}