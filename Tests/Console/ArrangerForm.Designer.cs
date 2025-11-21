using System.Windows.Forms;

namespace Music.Writer
{
    partial class ArrangerForm
    {
        private System.ComponentModel.IContainer components = null;

        private TextBox txtScoreReport;
        private TextBox txtDesignerReport;
        private TextBox txtMovementTitle;

        private Button btnNewScore;
        private Button btnUpdateFormFromDesigner;

        private Button btnSetDesignTestScenarioD1;

        private Button btnExportToNotion;

        private Button btnAddScore;
        private Button btnDeleteScore;
        private Button btnUpdateScore;
        private Button btnLoadScore;

        private ListBox lstScores;

        private Label lblScoreReport;
        private Label lblSavedScores;

        // Parts controls
        private GroupBox grbParts;
        private CheckedListBox clbParts;
        private Label lblPart;
        private Label lblStaff;
        private CheckedListBox clbStaffs;

        // Splice controls
        private GroupBox groupBox2;
        private Button btnSplice;
        private Label label13;
        private ComboBox comboBox1;
        private ComboBox comboBox4;
        private Label lblStartBar;
        private ComboBox comboBox3;
        private NumericUpDown numStartBar;
        private ComboBox comboBox2;
        private NumericUpDown numStartBeat;
        private Label label11;
        private Label lblStartBeat;
        private NumericUpDown numericUpDown3;
        private Label lblSections;
        private NumericUpDown numericUpDown4;
        private Label label12;
        private Label label5;
        private Label label9;
        private Label label6;
        private CheckedListBox clbSections;
        private Label label7;
        private NumericUpDown numericUpDown1;
        private Label label10;
        private NumericUpDown numericUpDown2;

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
            txtScoreReport = new TextBox();
            txtDesignerReport = new TextBox();
            txtMovementTitle = new TextBox();
            btnNewScore = new Button();
            btnUpdateFormFromDesigner = new Button();
            btnSetDesignTestScenarioD1 = new Button();
            btnExportToNotion = new Button();
            btnAddScore = new Button();
            btnDeleteScore = new Button();
            btnUpdateScore = new Button();
            btnLoadScore = new Button();
            lstScores = new ListBox();
            lblScoreReport = new Label();
            lblSavedScores = new Label();
            groupBox1 = new GroupBox();
            grbParts = new GroupBox();
            lblPart = new Label();
            clbParts = new CheckedListBox();
            lblStaff = new Label();
            clbStaffs = new CheckedListBox();
            groupBox2 = new GroupBox();
            btnSplice = new Button();
            label13 = new Label();
            comboBox1 = new ComboBox();
            comboBox4 = new ComboBox();
            lblStartBar = new Label();
            comboBox3 = new ComboBox();
            numStartBar = new NumericUpDown();
            comboBox2 = new ComboBox();
            numStartBeat = new NumericUpDown();
            label11 = new Label();
            lblStartBeat = new Label();
            numericUpDown3 = new NumericUpDown();
            lblSections = new Label();
            numericUpDown4 = new NumericUpDown();
            label12 = new Label();
            label5 = new Label();
            label9 = new Label();
            label6 = new Label();
            clbSections = new CheckedListBox();
            label7 = new Label();
            numericUpDown1 = new NumericUpDown();
            label10 = new Label();
            numericUpDown2 = new NumericUpDown();
            groupBox3 = new GroupBox();
            groupBox1.SuspendLayout();
            grbParts.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numStartBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numStartBeat).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown3).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown4).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown2).BeginInit();
            groupBox3.SuspendLayout();
            SuspendLayout();
            // 
            // txtScoreReport
            // 
            txtScoreReport.Location = new Point(25, 48);
            txtScoreReport.Multiline = true;
            txtScoreReport.Name = "txtScoreReport";
            txtScoreReport.ReadOnly = true;
            txtScoreReport.ScrollBars = ScrollBars.Vertical;
            txtScoreReport.Size = new Size(350, 400);
            txtScoreReport.TabIndex = 1;
            txtScoreReport.WordWrap = false;
            // 
            // txtDesignerReport
            // 
            txtDesignerReport.Location = new Point(17, 26);
            txtDesignerReport.Multiline = true;
            txtDesignerReport.Name = "txtDesignerReport";
            txtDesignerReport.ReadOnly = true;
            txtDesignerReport.ScrollBars = ScrollBars.Vertical;
            txtDesignerReport.Size = new Size(652, 178);
            txtDesignerReport.TabIndex = 10;
            txtDesignerReport.WordWrap = false;
            // 
            // txtMovementTitle
            // 
            txtMovementTitle.ForeColor = Color.FromArgb(0, 192, 0);
            txtMovementTitle.Location = new Point(25, 458);
            txtMovementTitle.Name = "txtMovementTitle";
            txtMovementTitle.PlaceholderText = "Movement Title";
            txtMovementTitle.Size = new Size(250, 23);
            txtMovementTitle.TabIndex = 2;
            // 
            // btnNewScore
            // 
            btnNewScore.ForeColor = Color.FromArgb(0, 192, 0);
            btnNewScore.Location = new Point(283, 458);
            btnNewScore.Name = "btnNewScore";
            btnNewScore.Size = new Size(92, 23);
            btnNewScore.TabIndex = 3;
            btnNewScore.Text = "New Score";
            btnNewScore.Click += btnNewScore_Click;
            // 
            // btnUpdateFormFromDesigner
            // 
            btnUpdateFormFromDesigner.ForeColor = Color.FromArgb(0, 192, 0);
            btnUpdateFormFromDesigner.Location = new Point(390, 210);
            btnUpdateFormFromDesigner.Name = "btnUpdateFormFromDesigner";
            btnUpdateFormFromDesigner.Size = new Size(151, 30);
            btnUpdateFormFromDesigner.TabIndex = 11;
            btnUpdateFormFromDesigner.Text = "Update From Designer";
            btnUpdateFormFromDesigner.Click += btnUpdateFormFromDesigner_Click;
            // 
            // btnSetDesignTestScenarioD1
            // 
            btnSetDesignTestScenarioD1.ForeColor = Color.FromArgb(0, 192, 0);
            btnSetDesignTestScenarioD1.Location = new Point(219, 210);
            btnSetDesignTestScenarioD1.Name = "btnSetDesignTestScenarioD1";
            btnSetDesignTestScenarioD1.Size = new Size(151, 30);
            btnSetDesignTestScenarioD1.TabIndex = 12;
            btnSetDesignTestScenarioD1.Text = "Design Test D1";
            btnSetDesignTestScenarioD1.Click += btnSetDesignTestScenarioD1_Click;
            // 
            // btnExportToNotion
            // 
            btnExportToNotion.ForeColor = Color.FromArgb(0, 192, 0);
            btnExportToNotion.Location = new Point(1489, 31);
            btnExportToNotion.Name = "btnExportToNotion";
            btnExportToNotion.Size = new Size(200, 30);
            btnExportToNotion.TabIndex = 14;
            btnExportToNotion.Text = "Export to Notion";
            btnExportToNotion.Click += btnExportToNotion_Click;
            // 
            // btnAddScore
            // 
            btnAddScore.ForeColor = Color.FromArgb(0, 192, 0);
            btnAddScore.Location = new Point(403, 398);
            btnAddScore.Name = "btnAddScore";
            btnAddScore.Size = new Size(130, 30);
            btnAddScore.TabIndex = 6;
            btnAddScore.Text = "Add";
            btnAddScore.Click += btnAddScore_Click;
            // 
            // btnDeleteScore
            // 
            btnDeleteScore.ForeColor = Color.FromArgb(0, 192, 0);
            btnDeleteScore.Location = new Point(553, 398);
            btnDeleteScore.Name = "btnDeleteScore";
            btnDeleteScore.Size = new Size(130, 30);
            btnDeleteScore.TabIndex = 7;
            btnDeleteScore.Text = "Delete";
            btnDeleteScore.Click += btnDeleteScore_Click;
            // 
            // btnUpdateScore
            // 
            btnUpdateScore.ForeColor = Color.FromArgb(0, 192, 0);
            btnUpdateScore.Location = new Point(553, 438);
            btnUpdateScore.Name = "btnUpdateScore";
            btnUpdateScore.Size = new Size(130, 30);
            btnUpdateScore.TabIndex = 9;
            btnUpdateScore.Text = "Update";
            btnUpdateScore.Click += btnUpdateScore_Click;
            // 
            // btnLoadScore
            // 
            btnLoadScore.ForeColor = Color.FromArgb(0, 192, 0);
            btnLoadScore.Location = new Point(403, 438);
            btnLoadScore.Name = "btnLoadScore";
            btnLoadScore.Size = new Size(130, 30);
            btnLoadScore.TabIndex = 8;
            btnLoadScore.Text = "Load";
            btnLoadScore.Click += btnLoadScore_Click;
            // 
            // lstScores
            // 
            lstScores.FormattingEnabled = true;
            lstScores.Location = new Point(403, 48);
            lstScores.Name = "lstScores";
            lstScores.Size = new Size(280, 334);
            lstScores.TabIndex = 5;
            // 
            // lblScoreReport
            // 
            lblScoreReport.AutoSize = true;
            lblScoreReport.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblScoreReport.ForeColor = Color.FromArgb(0, 192, 0);
            lblScoreReport.Location = new Point(25, 25);
            lblScoreReport.Name = "lblScoreReport";
            lblScoreReport.Size = new Size(51, 17);
            lblScoreReport.TabIndex = 0;
            lblScoreReport.Text = "Current";
            // 
            // lblSavedScores
            // 
            lblSavedScores.AutoSize = true;
            lblSavedScores.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSavedScores.ForeColor = Color.FromArgb(0, 192, 0);
            lblSavedScores.Location = new Point(403, 25);
            lblSavedScores.Name = "lblSavedScores";
            lblSavedScores.Size = new Size(43, 17);
            lblSavedScores.TabIndex = 4;
            lblSavedScores.Text = "Saved";
            // 
            // groupBox1
            // 
            groupBox1.BackColor = Color.FromArgb(64, 64, 64);
            groupBox1.Controls.Add(lstScores);
            groupBox1.Controls.Add(lblScoreReport);
            groupBox1.Controls.Add(btnUpdateScore);
            groupBox1.Controls.Add(txtScoreReport);
            groupBox1.Controls.Add(btnLoadScore);
            groupBox1.Controls.Add(txtMovementTitle);
            groupBox1.Controls.Add(btnDeleteScore);
            groupBox1.Controls.Add(btnNewScore);
            groupBox1.Controls.Add(btnAddScore);
            groupBox1.Controls.Add(lblSavedScores);
            groupBox1.ForeColor = Color.White;
            groupBox1.Location = new Point(117, 9);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(707, 500);
            groupBox1.TabIndex = 15;
            groupBox1.TabStop = false;
            groupBox1.Text = "Score Manager";
            // 
            // grbParts
            // 
            grbParts.BackColor = Color.Black;
            grbParts.Controls.Add(lblPart);
            grbParts.Controls.Add(clbParts);
            grbParts.Controls.Add(lblStaff);
            grbParts.Controls.Add(clbStaffs);
            grbParts.ForeColor = Color.White;
            grbParts.Location = new Point(939, 71);
            grbParts.Name = "grbParts";
            grbParts.Size = new Size(324, 174);
            grbParts.TabIndex = 16;
            grbParts.TabStop = false;
            grbParts.Text = "Part";
            // 
            // lblPart
            // 
            lblPart.AutoSize = true;
            lblPart.ForeColor = Color.FromArgb(0, 192, 0);
            lblPart.Location = new Point(15, 27);
            lblPart.Name = "lblPart";
            lblPart.Size = new Size(70, 15);
            lblPart.TabIndex = 0;
            lblPart.Text = "Part (Voice):";
            // 
            // clbParts
            // 
            clbParts.CheckOnClick = true;
            clbParts.FormattingEnabled = true;
            clbParts.Location = new Point(85, 25);
            clbParts.Name = "clbParts";
            clbParts.Size = new Size(220, 94);
            clbParts.TabIndex = 1;
            // 
            // lblStaff
            // 
            lblStaff.AutoSize = true;
            lblStaff.ForeColor = Color.FromArgb(0, 192, 0);
            lblStaff.Location = new Point(15, 132);
            lblStaff.Name = "lblStaff";
            lblStaff.Size = new Size(34, 15);
            lblStaff.TabIndex = 5;
            lblStaff.Text = "Staff:";
            // 
            // clbStaffs
            // 
            clbStaffs.CheckOnClick = true;
            clbStaffs.FormattingEnabled = true;
            clbStaffs.Items.AddRange(new object[] { "1", "2" });
            clbStaffs.Location = new Point(85, 125);
            clbStaffs.Name = "clbStaffs";
            clbStaffs.Size = new Size(120, 40);
            clbStaffs.TabIndex = 6;
            // 
            // groupBox2
            // 
            groupBox2.BackColor = Color.Black;
            groupBox2.Controls.Add(btnSplice);
            groupBox2.Controls.Add(label13);
            groupBox2.Controls.Add(comboBox1);
            groupBox2.Controls.Add(comboBox4);
            groupBox2.Controls.Add(lblStartBar);
            groupBox2.Controls.Add(comboBox3);
            groupBox2.Controls.Add(numStartBar);
            groupBox2.Controls.Add(comboBox2);
            groupBox2.Controls.Add(numStartBeat);
            groupBox2.Controls.Add(label11);
            groupBox2.Controls.Add(lblStartBeat);
            groupBox2.Controls.Add(numericUpDown3);
            groupBox2.Controls.Add(lblSections);
            groupBox2.Controls.Add(numericUpDown4);
            groupBox2.Controls.Add(label12);
            groupBox2.Controls.Add(label5);
            groupBox2.Controls.Add(label9);
            groupBox2.Controls.Add(label6);
            groupBox2.Controls.Add(clbSections);
            groupBox2.Controls.Add(label7);
            groupBox2.Controls.Add(numericUpDown1);
            groupBox2.Controls.Add(label10);
            groupBox2.Controls.Add(numericUpDown2);
            groupBox2.ForeColor = Color.White;
            groupBox2.Location = new Point(939, 251);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(442, 400);
            groupBox2.TabIndex = 32;
            groupBox2.TabStop = false;
            groupBox2.Text = "Splice";
            // 
            // btnSplice
            // 
            btnSplice.ForeColor = Color.FromArgb(255, 128, 0);
            btnSplice.Location = new Point(195, 290);
            btnSplice.Name = "btnSplice";
            btnSplice.Size = new Size(77, 22);
            btnSplice.TabIndex = 7;
            btnSplice.Text = "Splice";
            btnSplice.UseVisualStyleBackColor = true;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.ForeColor = Color.Red;
            label13.Location = new Point(18, 314);
            label13.Name = "label13";
            label13.Size = new Size(139, 15);
            label13.TabIndex = 37;
            label13.Text = "For future pattern targets";
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(109, 56);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(121, 23);
            comboBox1.TabIndex = 25;
            // 
            // comboBox4
            // 
            comboBox4.FormattingEnabled = true;
            comboBox4.Location = new Point(119, 241);
            comboBox4.Name = "comboBox4";
            comboBox4.Size = new Size(121, 23);
            comboBox4.TabIndex = 36;
            // 
            // lblStartBar
            // 
            lblStartBar.AutoSize = true;
            lblStartBar.ForeColor = Color.Red;
            lblStartBar.Location = new Point(17, 88);
            lblStartBar.Name = "lblStartBar";
            lblStartBar.Size = new Size(54, 15);
            lblStartBar.TabIndex = 7;
            lblStartBar.Text = "Start Bar:";
            // 
            // comboBox3
            // 
            comboBox3.FormattingEnabled = true;
            comboBox3.Location = new Point(108, 22);
            comboBox3.Name = "comboBox3";
            comboBox3.Size = new Size(121, 23);
            comboBox3.TabIndex = 35;
            // 
            // numStartBar
            // 
            numStartBar.Location = new Point(109, 85);
            numStartBar.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numStartBar.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numStartBar.Name = "numStartBar";
            numStartBar.Size = new Size(120, 23);
            numStartBar.TabIndex = 8;
            numStartBar.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // comboBox2
            // 
            comboBox2.FormattingEnabled = true;
            comboBox2.Location = new Point(111, 155);
            comboBox2.Name = "comboBox2";
            comboBox2.Size = new Size(121, 23);
            comboBox2.TabIndex = 34;
            // 
            // numStartBeat
            // 
            numStartBeat.Location = new Point(305, 85);
            numStartBeat.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numStartBeat.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numStartBeat.Name = "numStartBeat";
            numStartBeat.Size = new Size(120, 23);
            numStartBeat.TabIndex = 12;
            numStartBeat.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.ForeColor = Color.Red;
            label11.Location = new Point(235, 118);
            label11.Name = "label11";
            label11.Size = new Size(56, 15);
            label11.TabIndex = 33;
            label11.Text = "End Beat:";
            // 
            // lblStartBeat
            // 
            lblStartBeat.AutoSize = true;
            lblStartBeat.ForeColor = Color.Red;
            lblStartBeat.Location = new Point(235, 87);
            lblStartBeat.Name = "lblStartBeat";
            lblStartBeat.Size = new Size(60, 15);
            lblStartBeat.TabIndex = 15;
            lblStartBeat.Text = "Start Beat:";
            // 
            // numericUpDown3
            // 
            numericUpDown3.Location = new Point(305, 116);
            numericUpDown3.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numericUpDown3.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericUpDown3.Name = "numericUpDown3";
            numericUpDown3.Size = new Size(120, 23);
            numericUpDown3.TabIndex = 32;
            numericUpDown3.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblSections
            // 
            lblSections.AutoSize = true;
            lblSections.ForeColor = Color.Red;
            lblSections.Location = new Point(18, 52);
            lblSections.Name = "lblSections";
            lblSections.Size = new Size(49, 15);
            lblSections.TabIndex = 16;
            lblSections.Text = "Section:";
            // 
            // numericUpDown4
            // 
            numericUpDown4.Location = new Point(109, 116);
            numericUpDown4.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numericUpDown4.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericUpDown4.Name = "numericUpDown4";
            numericUpDown4.Size = new Size(120, 23);
            numericUpDown4.TabIndex = 31;
            numericUpDown4.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.ForeColor = Color.Red;
            label12.Location = new Point(17, 119);
            label12.Name = "label12";
            label12.Size = new Size(50, 15);
            label12.TabIndex = 30;
            label12.Text = "End Bar:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.ForeColor = Color.Red;
            label5.Location = new Point(17, 25);
            label5.Name = "label5";
            label5.Size = new Size(84, 15);
            label5.TabIndex = 22;
            label5.Text = "Score Source 1";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.ForeColor = Color.Red;
            label9.Location = new Point(238, 191);
            label9.Name = "label9";
            label9.Size = new Size(60, 15);
            label9.TabIndex = 29;
            label9.Text = "Start Beat:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.ForeColor = Color.Red;
            label6.Location = new Point(16, 160);
            label6.Name = "label6";
            label6.Size = new Size(84, 15);
            label6.TabIndex = 23;
            label6.Text = "Score Source 2";
            // 
            // clbSections
            // 
            clbSections.CheckOnClick = true;
            clbSections.FormattingEnabled = true;
            clbSections.Location = new Point(36, 349);
            clbSections.Name = "clbSections";
            clbSections.Size = new Size(106, 40);
            clbSections.TabIndex = 20;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.ForeColor = Color.Red;
            label7.Location = new Point(17, 243);
            label7.Name = "label7";
            label7.Size = new Size(99, 15);
            label7.TabIndex = 24;
            label7.Text = "Score Destination";
            // 
            // numericUpDown1
            // 
            numericUpDown1.Location = new Point(308, 189);
            numericUpDown1.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numericUpDown1.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericUpDown1.Name = "numericUpDown1";
            numericUpDown1.Size = new Size(120, 23);
            numericUpDown1.TabIndex = 28;
            numericUpDown1.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.ForeColor = Color.Red;
            label10.Location = new Point(17, 192);
            label10.Name = "label10";
            label10.Size = new Size(54, 15);
            label10.TabIndex = 26;
            label10.Text = "Start Bar:";
            // 
            // numericUpDown2
            // 
            numericUpDown2.Location = new Point(112, 189);
            numericUpDown2.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numericUpDown2.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericUpDown2.Name = "numericUpDown2";
            numericUpDown2.Size = new Size(120, 23);
            numericUpDown2.TabIndex = 27;
            numericUpDown2.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // groupBox3
            // 
            groupBox3.BackColor = Color.Black;
            groupBox3.Controls.Add(txtDesignerReport);
            groupBox3.Controls.Add(btnSetDesignTestScenarioD1);
            groupBox3.Controls.Add(btnUpdateFormFromDesigner);
            groupBox3.ForeColor = Color.White;
            groupBox3.Location = new Point(119, 521);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(714, 250);
            groupBox3.TabIndex = 16;
            groupBox3.TabStop = false;
            groupBox3.Text = "Current Design";
            // 
            // ArrangerForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            ClientSize = new Size(1733, 796);
            Controls.Add(grbParts);
            Controls.Add(groupBox3);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(btnExportToNotion);
            ForeColor = Color.White;
            Name = "ArrangerForm";
            Text = "Arranger";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            grbParts.ResumeLayout(false);
            grbParts.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numStartBar).EndInit();
            ((System.ComponentModel.ISupportInitialize)numStartBeat).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown3).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown4).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown2).EndInit();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ResumeLayout(false);
        }
        private GroupBox groupBox1;
        private GroupBox groupBox3;
    }
}