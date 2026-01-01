namespace Music.Writer
{
    partial class WriterForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ComboBox cbCommand;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            cbCommand = new ComboBox();
            btnSetWriterTestScenarioG1 = new Button();
            btnSetDesignTestScenarioD1 = new Button();
            groupBox3 = new GroupBox();
            btnExecute = new Button();
            gbSong = new GroupBox();
            btnLoadDesign = new Button();
            btnSaveDesign = new Button();
            btnPause = new Button();
            btnClearSelectedTracks = new Button();
            btnStop = new Button();
            btnExport = new Button();
            btnImport = new Button();
            btnAddTrack = new Button();
            btnDeleteTrack = new Button();
            btnClearAll = new Button();
            btnPlayTracks = new Button();
            dgSong = new DataGridView();
            btnSettings = new Button();
            groupBox3.SuspendLayout();
            gbSong.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgSong).BeginInit();
            SuspendLayout();
            // 
            // cbCommand
            // 
            cbCommand.DropDownStyle = ComboBoxStyle.DropDownList;
            cbCommand.Items.AddRange(new object[] { "Repeat Note", "Harmony Groove Sync Test" });
            cbCommand.Location = new Point(13, 22);
            cbCommand.Name = "cbCommand";
            cbCommand.Size = new Size(250, 23);
            cbCommand.TabIndex = 1;
            // 
            // btnSetWriterTestScenarioG1
            // 
            btnSetWriterTestScenarioG1.BackColor = Color.Black;
            btnSetWriterTestScenarioG1.ForeColor = Color.FromArgb(0, 192, 0);
            btnSetWriterTestScenarioG1.Location = new Point(212, 609);
            btnSetWriterTestScenarioG1.Name = "btnSetWriterTestScenarioG1";
            btnSetWriterTestScenarioG1.Size = new Size(196, 33);
            btnSetWriterTestScenarioG1.TabIndex = 8;
            btnSetWriterTestScenarioG1.Text = "Set Writer - Test Scenario G1";
            btnSetWriterTestScenarioG1.UseVisualStyleBackColor = false;
            btnSetWriterTestScenarioG1.Click += btnSetWriterTestScenarioG1_Click;
            // 
            // btnSetDesignTestScenarioD1
            // 
            btnSetDesignTestScenarioD1.BackColor = Color.Black;
            btnSetDesignTestScenarioD1.ForeColor = Color.FromArgb(0, 192, 0);
            btnSetDesignTestScenarioD1.Location = new Point(18, 609);
            btnSetDesignTestScenarioD1.Name = "btnSetDesignTestScenarioD1";
            btnSetDesignTestScenarioD1.Size = new Size(179, 33);
            btnSetDesignTestScenarioD1.TabIndex = 10;
            btnSetDesignTestScenarioD1.Text = "Set Design - Test Scenario D1";
            btnSetDesignTestScenarioD1.UseVisualStyleBackColor = false;
            btnSetDesignTestScenarioD1.Click += btnSetDesignTestScenarioD1_Click;
            // 
            // groupBox3
            // 
            groupBox3.BackColor = SystemColors.ActiveCaptionText;
            groupBox3.Controls.Add(btnSettings);
            groupBox3.Controls.Add(btnExecute);
            groupBox3.Controls.Add(cbCommand);
            groupBox3.ForeColor = Color.White;
            groupBox3.Location = new Point(436, 609);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(538, 59);
            groupBox3.TabIndex = 33;
            groupBox3.TabStop = false;
            groupBox3.Text = "Command";
            // 
            // btnExecute
            // 
            btnExecute.ForeColor = Color.FromArgb(0, 192, 0);
            btnExecute.Location = new Point(411, 22);
            btnExecute.Name = "btnExecute";
            btnExecute.Size = new Size(114, 23);
            btnExecute.TabIndex = 31;
            btnExecute.Text = "Execute";
            btnExecute.UseVisualStyleBackColor = true;
            btnExecute.Click += btnExecute_Click;
            // 
            // gbSong
            // 
            gbSong.BackColor = Color.Black;
            gbSong.Controls.Add(btnLoadDesign);
            gbSong.Controls.Add(btnSaveDesign);
            gbSong.Controls.Add(btnPause);
            gbSong.Controls.Add(btnClearSelectedTracks);
            gbSong.Controls.Add(btnStop);
            gbSong.Controls.Add(btnExport);
            gbSong.Controls.Add(btnImport);
            gbSong.Controls.Add(btnAddTrack);
            gbSong.Controls.Add(btnDeleteTrack);
            gbSong.Controls.Add(btnClearAll);
            gbSong.Controls.Add(btnPlayTracks);
            gbSong.Controls.Add(dgSong);
            gbSong.ForeColor = Color.White;
            gbSong.Location = new Point(12, 12);
            gbSong.Name = "gbSong";
            gbSong.Size = new Size(1900, 581);
            gbSong.TabIndex = 36;
            gbSong.TabStop = false;
            gbSong.Text = "Song";
            // 
            // btnLoadDesign
            // 
            btnLoadDesign.ForeColor = Color.FromArgb(0, 192, 0);
            btnLoadDesign.Location = new Point(1089, 542);
            btnLoadDesign.Name = "btnLoadDesign";
            btnLoadDesign.Size = new Size(89, 23);
            btnLoadDesign.TabIndex = 47;
            btnLoadDesign.Text = "Load Design";
            btnLoadDesign.UseVisualStyleBackColor = true;
            btnLoadDesign.Click += btnLoadDesign_Click;
            // 
            // btnSaveDesign
            // 
            btnSaveDesign.ForeColor = Color.FromArgb(0, 192, 0);
            btnSaveDesign.Location = new Point(994, 542);
            btnSaveDesign.Name = "btnSaveDesign";
            btnSaveDesign.Size = new Size(85, 23);
            btnSaveDesign.TabIndex = 46;
            btnSaveDesign.Text = "Save Design";
            btnSaveDesign.UseVisualStyleBackColor = true;
            btnSaveDesign.Click += btnSaveDesign_Click;
            // 
            // btnPause
            // 
            btnPause.ForeColor = Color.FromArgb(0, 192, 0);
            btnPause.Location = new Point(870, 544);
            btnPause.Name = "btnPause";
            btnPause.Size = new Size(72, 23);
            btnPause.TabIndex = 45;
            btnPause.Text = "Pause";
            btnPause.UseVisualStyleBackColor = true;
            btnPause.Click += btnPause_Click;
            // 
            // btnClearSelectedTracks
            // 
            btnClearSelectedTracks.ForeColor = Color.FromArgb(0, 192, 0);
            btnClearSelectedTracks.Location = new Point(174, 544);
            btnClearSelectedTracks.Name = "btnClearSelectedTracks";
            btnClearSelectedTracks.Size = new Size(116, 23);
            btnClearSelectedTracks.TabIndex = 44;
            btnClearSelectedTracks.Text = "Clear Selected";
            btnClearSelectedTracks.UseVisualStyleBackColor = true;
            btnClearSelectedTracks.Click += btnClearSelected_Click;
            // 
            // btnStop
            // 
            btnStop.ForeColor = Color.FromArgb(0, 192, 0);
            btnStop.Location = new Point(783, 544);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(72, 23);
            btnStop.TabIndex = 43;
            btnStop.Text = "Stop";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // btnExport
            // 
            btnExport.ForeColor = Color.FromArgb(0, 192, 0);
            btnExport.Location = new Point(535, 542);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(75, 23);
            btnExport.TabIndex = 42;
            btnExport.Text = "Export";
            btnExport.UseVisualStyleBackColor = true;
            btnExport.Click += btnExport_Click;
            // 
            // btnImport
            // 
            btnImport.ForeColor = Color.FromArgb(0, 192, 0);
            btnImport.Location = new Point(457, 542);
            btnImport.Name = "btnImport";
            btnImport.Size = new Size(72, 23);
            btnImport.TabIndex = 41;
            btnImport.Text = "Import";
            btnImport.UseVisualStyleBackColor = true;
            btnImport.Click += btnImport_Click;
            // 
            // btnAddTrack
            // 
            btnAddTrack.ForeColor = Color.FromArgb(0, 192, 0);
            btnAddTrack.Location = new Point(15, 544);
            btnAddTrack.Name = "btnAddTrack";
            btnAddTrack.Size = new Size(72, 23);
            btnAddTrack.TabIndex = 40;
            btnAddTrack.Text = "Add";
            btnAddTrack.UseVisualStyleBackColor = true;
            btnAddTrack.Click += btnAddTrack_Click;
            // 
            // btnDeleteTrack
            // 
            btnDeleteTrack.ForeColor = Color.FromArgb(0, 192, 0);
            btnDeleteTrack.Location = new Point(95, 544);
            btnDeleteTrack.Name = "btnDeleteTrack";
            btnDeleteTrack.Size = new Size(72, 23);
            btnDeleteTrack.TabIndex = 39;
            btnDeleteTrack.Text = "Delete";
            btnDeleteTrack.UseVisualStyleBackColor = true;
            btnDeleteTrack.Click += btnDeleteTracks_Click;
            // 
            // btnClearAll
            // 
            btnClearAll.ForeColor = Color.FromArgb(0, 192, 0);
            btnClearAll.Location = new Point(313, 544);
            btnClearAll.Name = "btnClearAll";
            btnClearAll.Size = new Size(72, 23);
            btnClearAll.TabIndex = 38;
            btnClearAll.Text = "Clear All";
            btnClearAll.UseVisualStyleBackColor = true;
            btnClearAll.Click += btnClearAll_Click;
            // 
            // btnPlayTracks
            // 
            btnPlayTracks.ForeColor = Color.FromArgb(0, 192, 0);
            btnPlayTracks.Location = new Point(703, 544);
            btnPlayTracks.Name = "btnPlayTracks";
            btnPlayTracks.Size = new Size(72, 23);
            btnPlayTracks.TabIndex = 32;
            btnPlayTracks.Text = "Play";
            btnPlayTracks.UseVisualStyleBackColor = true;
            btnPlayTracks.Click += btnPlay_Click;
            // 
            // dgSong
            // 
            dgSong.AllowUserToAddRows = false;
            dgSong.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgSong.Location = new Point(15, 19);
            dgSong.Name = "dgSong";
            dgSong.ReadOnly = true;
            dgSong.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgSong.Size = new Size(1864, 503);
            dgSong.TabIndex = 37;
            dgSong.CellDoubleClick += dgSong_CellDoubleClick;
            // 
            // btnSettings
            // 
            btnSettings.ForeColor = Color.FromArgb(0, 192, 0);
            btnSettings.Location = new Point(282, 22);
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new Size(114, 23);
            btnSettings.TabIndex = 32;
            btnSettings.Text = "Settings";
            btnSettings.UseVisualStyleBackColor = true;
            btnSettings.Click += btnSettings_Click;
            // 
            // WriterForm
            // 
            BackColor = Color.White;
            ClientSize = new Size(1924, 750);
            Controls.Add(btnSetDesignTestScenarioD1);
            Controls.Add(btnSetWriterTestScenarioG1);
            Controls.Add(groupBox3);
            Controls.Add(gbSong);
            Name = "WriterForm";
            Text = "Music Writer";
            groupBox3.ResumeLayout(false);
            gbSong.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgSong).EndInit();
            ResumeLayout(false);
        }

        private Button btnSetWriterTestScenarioG1;
        private Button btnSetDesignTestScenarioD1;
        private GroupBox groupBox3;
        private GroupBox gbSong;
        private Button btnExecute;
        private DataGridView dgSong;
        private Button btnPlayTracks;
        private Button btnDeleteTrack;
        private Button btnClearAll;
        private Button btnAddTrack;
        private Button btnImport;
        private Button btnExport;
        private Button btnStop;
        private Button btnClearSelectedTracks;
        private Button btnPause;
        private Button btnLoadDesign;
        private Button btnSaveDesign;
        private Button btnSettings;
    }
}