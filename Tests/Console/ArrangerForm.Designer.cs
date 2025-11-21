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
        private Button btnSetWriterTestScenarioG1;

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
            btnSetWriterTestScenarioG1 = new Button();
            btnExportToNotion = new Button();
            btnAddScore = new Button();
            btnDeleteScore = new Button();
            btnUpdateScore = new Button();
            btnLoadScore = new Button();
            lstScores = new ListBox();
            lblScoreReport = new Label();
            lblSavedScores = new Label();
            SuspendLayout();
            // 
            // txtScoreReport
            // 
            txtScoreReport.Location = new Point(12, 35);
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
            txtDesignerReport.Location = new Point(12, 490);
            txtDesignerReport.Multiline = true;
            txtDesignerReport.Name = "txtDesignerReport";
            txtDesignerReport.ReadOnly = true;
            txtDesignerReport.ScrollBars = ScrollBars.Vertical;
            txtDesignerReport.Size = new Size(658, 180);
            txtDesignerReport.TabIndex = 10;
            txtDesignerReport.WordWrap = false;
            // 
            // txtMovementTitle
            // 
            txtMovementTitle.ForeColor = Color.FromArgb(0, 192, 0);
            txtMovementTitle.Location = new Point(12, 445);
            txtMovementTitle.Name = "txtMovementTitle";
            txtMovementTitle.PlaceholderText = "Movement Title";
            txtMovementTitle.Size = new Size(250, 23);
            txtMovementTitle.TabIndex = 2;
            // 
            // btnNewScore
            // 
            btnNewScore.ForeColor = Color.FromArgb(0, 192, 0);
            btnNewScore.Location = new Point(270, 445);
            btnNewScore.Name = "btnNewScore";
            btnNewScore.Size = new Size(92, 23);
            btnNewScore.TabIndex = 3;
            btnNewScore.Text = "New Score";
            btnNewScore.Click += btnNewScore_Click;
            // 
            // btnUpdateFormFromDesigner
            // 
            btnUpdateFormFromDesigner.ForeColor = Color.FromArgb(0, 192, 0);
            btnUpdateFormFromDesigner.Location = new Point(690, 35);
            btnUpdateFormFromDesigner.Name = "btnUpdateFormFromDesigner";
            btnUpdateFormFromDesigner.Size = new Size(200, 30);
            btnUpdateFormFromDesigner.TabIndex = 11;
            btnUpdateFormFromDesigner.Text = "Update From Designer";
            btnUpdateFormFromDesigner.Click += btnUpdateFormFromDesigner_Click;
            // 
            // btnSetDesignTestScenarioD1
            // 
            btnSetDesignTestScenarioD1.ForeColor = Color.FromArgb(0, 192, 0);
            btnSetDesignTestScenarioD1.Location = new Point(690, 75);
            btnSetDesignTestScenarioD1.Name = "btnSetDesignTestScenarioD1";
            btnSetDesignTestScenarioD1.Size = new Size(200, 30);
            btnSetDesignTestScenarioD1.TabIndex = 12;
            btnSetDesignTestScenarioD1.Text = "Design Test D1";
            btnSetDesignTestScenarioD1.Click += btnSetDesignTestScenarioD1_Click;
            // 
            // btnSetWriterTestScenarioG1
            // 
            btnSetWriterTestScenarioG1.ForeColor = Color.FromArgb(0, 192, 0);
            btnSetWriterTestScenarioG1.Location = new Point(690, 115);
            btnSetWriterTestScenarioG1.Name = "btnSetWriterTestScenarioG1";
            btnSetWriterTestScenarioG1.Size = new Size(200, 30);
            btnSetWriterTestScenarioG1.TabIndex = 13;
            btnSetWriterTestScenarioG1.Text = "Writer Test G1";
            btnSetWriterTestScenarioG1.Click += btnSetWriterTestScenarioG1_Click;
            // 
            // btnExportToNotion
            // 
            btnExportToNotion.ForeColor = Color.FromArgb(0, 192, 0);
            btnExportToNotion.Location = new Point(690, 155);
            btnExportToNotion.Name = "btnExportToNotion";
            btnExportToNotion.Size = new Size(200, 30);
            btnExportToNotion.TabIndex = 14;
            btnExportToNotion.Text = "Export to Notion";
            btnExportToNotion.Click += btnExportToNotion_Click;
            // 
            // btnAddScore
            // 
            btnAddScore.ForeColor = Color.FromArgb(0, 192, 0);
            btnAddScore.Location = new Point(390, 385);
            btnAddScore.Name = "btnAddScore";
            btnAddScore.Size = new Size(130, 30);
            btnAddScore.TabIndex = 6;
            btnAddScore.Text = "Add";
            btnAddScore.Click += btnAddScore_Click;
            // 
            // btnDeleteScore
            // 
            btnDeleteScore.ForeColor = Color.FromArgb(0, 192, 0);
            btnDeleteScore.Location = new Point(540, 385);
            btnDeleteScore.Name = "btnDeleteScore";
            btnDeleteScore.Size = new Size(130, 30);
            btnDeleteScore.TabIndex = 7;
            btnDeleteScore.Text = "Delete";
            btnDeleteScore.Click += btnDeleteScore_Click;
            // 
            // btnUpdateScore
            // 
            btnUpdateScore.ForeColor = Color.FromArgb(0, 192, 0);
            btnUpdateScore.Location = new Point(540, 425);
            btnUpdateScore.Name = "btnUpdateScore";
            btnUpdateScore.Size = new Size(130, 30);
            btnUpdateScore.TabIndex = 9;
            btnUpdateScore.Text = "Update";
            btnUpdateScore.Click += btnUpdateScore_Click;
            // 
            // btnLoadScore
            // 
            btnLoadScore.ForeColor = Color.FromArgb(0, 192, 0);
            btnLoadScore.Location = new Point(390, 425);
            btnLoadScore.Name = "btnLoadScore";
            btnLoadScore.Size = new Size(130, 30);
            btnLoadScore.TabIndex = 8;
            btnLoadScore.Text = "Load";
            btnLoadScore.Click += btnLoadScore_Click;
            // 
            // lstScores
            // 
            lstScores.FormattingEnabled = true;
            lstScores.Location = new Point(390, 35);
            lstScores.Name = "lstScores";
            lstScores.Size = new Size(280, 334);
            lstScores.TabIndex = 5;
            // 
            // lblScoreReport
            // 
            lblScoreReport.AutoSize = true;
            lblScoreReport.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblScoreReport.ForeColor = Color.FromArgb(0, 192, 0);
            lblScoreReport.Location = new Point(12, 12);
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
            lblSavedScores.Location = new Point(390, 12);
            lblSavedScores.Name = "lblSavedScores";
            lblSavedScores.Size = new Size(43, 17);
            lblSavedScores.TabIndex = 4;
            lblSavedScores.Text = "Saved";
            // 
            // ArrangerForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            ClientSize = new Size(1200, 700);
            Controls.Add(lblScoreReport);
            Controls.Add(txtScoreReport);
            Controls.Add(txtMovementTitle);
            Controls.Add(btnNewScore);
            Controls.Add(lblSavedScores);
            Controls.Add(lstScores);
            Controls.Add(btnAddScore);
            Controls.Add(btnDeleteScore);
            Controls.Add(btnLoadScore);
            Controls.Add(btnUpdateScore);
            Controls.Add(txtDesignerReport);
            Controls.Add(btnUpdateFormFromDesigner);
            Controls.Add(btnSetDesignTestScenarioD1);
            Controls.Add(btnSetWriterTestScenarioG1);
            Controls.Add(btnExportToNotion);
            ForeColor = Color.White;
            Name = "ArrangerForm";
            Text = "Arranger";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}