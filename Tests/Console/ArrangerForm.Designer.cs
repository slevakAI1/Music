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
            groupBox2 = new GroupBox();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
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
            groupBox1.Location = new Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(707, 500);
            groupBox1.TabIndex = 15;
            groupBox1.TabStop = false;
            groupBox1.Text = "groupBox1";
            // 
            // groupBox2
            // 
            groupBox2.BackColor = Color.Black;
            groupBox2.Controls.Add(txtDesignerReport);
            groupBox2.Controls.Add(btnSetDesignTestScenarioD1);
            groupBox2.Controls.Add(btnUpdateFormFromDesigner);
            groupBox2.Location = new Point(12, 529);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(714, 265);
            groupBox2.TabIndex = 16;
            groupBox2.TabStop = false;
            groupBox2.Text = "groupBox2";
            // 
            // ArrangerForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            ClientSize = new Size(1733, 796);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(btnExportToNotion);
            ForeColor = Color.White;
            Name = "ArrangerForm";
            Text = "Arranger";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);
        }
        private GroupBox groupBox1;
        private GroupBox groupBox2;
    }
}