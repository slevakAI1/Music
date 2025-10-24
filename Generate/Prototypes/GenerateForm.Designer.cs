namespace Music.Generate
{
    partial class GenerateForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnSetWholeNote = new Button();
            label1 = new Label();
            lblDesign = new Label();
            lblEdit = new Label();
            btnEditTempo = new Button();
            btnEditTimeSignature = new Button();
            btnLoad = new Button();
            btnSetDefault = new Button();
            btnEditHarmony = new Button();
            txtDesignView = new TextBox();
            btnNew = new Button();
            btnSave = new Button();
            btnEditSections = new Button();
            panel1 = new Panel();
            panel2 = new Panel();
            SuspendLayout();
            // 
            // btnSetWholeNote
            // 
            btnSetWholeNote.Location = new Point(30, 285);
            btnSetWholeNote.Name = "btnSetWholeNote";
            btnSetWholeNote.Size = new Size(119, 23);
            btnSetWholeNote.TabIndex = 27;
            btnSetWholeNote.Text = "Set Whole Note";
            btnSetWholeNote.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 26.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.Location = new Point(482, 13);
            label1.Name = "label1";
            label1.Size = new Size(160, 47);
            label1.TabIndex = 37;
            label1.Text = "Generate";
            // 
            // lblDesign
            // 
            lblDesign.AutoSize = true;
            lblDesign.Location = new Point(59, 68);
            lblDesign.Name = "lblDesign";
            lblDesign.Size = new Size(12, 15);
            lblDesign.TabIndex = 36;
            lblDesign.Text = "?";
            // 
            // lblEdit
            // 
            lblEdit.AutoSize = true;
            lblEdit.Location = new Point(64, 264);
            lblEdit.Name = "lblEdit";
            lblEdit.Size = new Size(12, 15);
            lblEdit.TabIndex = 35;
            lblEdit.Text = "?";
            // 
            // btnEditTempo
            // 
            btnEditTempo.Location = new Point(30, 425);
            btnEditTempo.Name = "btnEditTempo";
            btnEditTempo.Size = new Size(96, 23);
            btnEditTempo.TabIndex = 34;
            btnEditTempo.Text = "?";
            btnEditTempo.UseVisualStyleBackColor = true;
            // 
            // btnEditTimeSignature
            // 
            btnEditTimeSignature.Location = new Point(30, 390);
            btnEditTimeSignature.Name = "btnEditTimeSignature";
            btnEditTimeSignature.Size = new Size(96, 23);
            btnEditTimeSignature.TabIndex = 33;
            btnEditTimeSignature.Text = "?";
            btnEditTimeSignature.UseVisualStyleBackColor = true;
            // 
            // btnLoad
            // 
            btnLoad.Location = new Point(30, 186);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(96, 23);
            btnLoad.TabIndex = 32;
            btnLoad.Text = "Load";
            btnLoad.UseVisualStyleBackColor = true;
            // 
            // btnSetDefault
            // 
            btnSetDefault.Location = new Point(30, 87);
            btnSetDefault.Name = "btnSetDefault";
            btnSetDefault.Size = new Size(96, 23);
            btnSetDefault.TabIndex = 31;
            btnSetDefault.Text = "Set Default Timeline";
            btnSetDefault.UseVisualStyleBackColor = true;
            // 
            // btnEditHarmony
            // 
            btnEditHarmony.Location = new Point(30, 355);
            btnEditHarmony.Name = "btnEditHarmony";
            btnEditHarmony.Size = new Size(96, 23);
            btnEditHarmony.TabIndex = 30;
            btnEditHarmony.Text = "?";
            btnEditHarmony.UseVisualStyleBackColor = true;
            // 
            // txtDesignView
            // 
            txtDesignView.Location = new Point(188, 68);
            txtDesignView.Multiline = true;
            txtDesignView.Name = "txtDesignView";
            txtDesignView.ScrollBars = ScrollBars.Vertical;
            txtDesignView.Size = new Size(824, 747);
            txtDesignView.TabIndex = 29;
            // 
            // btnNew
            // 
            btnNew.Location = new Point(30, 120);
            btnNew.Name = "btnNew";
            btnNew.Size = new Size(96, 23);
            btnNew.TabIndex = 28;
            btnNew.Text = "New";
            btnNew.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(30, 153);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(96, 23);
            btnSave.TabIndex = 26;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            // 
            // btnEditSections
            // 
            btnEditSections.Location = new Point(30, 320);
            btnEditSections.Name = "btnEditSections";
            btnEditSections.Size = new Size(96, 23);
            btnEditSections.TabIndex = 25;
            btnEditSections.Text = "?";
            btnEditSections.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Location = new Point(15, 77);
            panel1.Name = "panel1";
            panel1.Size = new Size(138, 145);
            panel1.TabIndex = 38;
            // 
            // panel2
            // 
            panel2.BorderStyle = BorderStyle.FixedSingle;
            panel2.Location = new Point(15, 272);
            panel2.Name = "panel2";
            panel2.Size = new Size(138, 191);
            panel2.TabIndex = 39;
            // 
            // GenerateForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1039, 777);
            Controls.Add(btnSetWholeNote);
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
            Name = "GenerateForm";
            Text = "GenerateForm";
            Load += GenerateForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnSetWholeNote;
        private Label label1;
        private Label lblDesign;
        private Label lblEdit;
        private Button btnEditTempo;
        private Button btnEditTimeSignature;
        private Button btnLoad;
        private Button btnSetDefault;
        private Button btnEditHarmony;
        private TextBox txtDesignView;
        private Button btnNew;
        private Button btnSave;
        private Button btnEditSections;
        private Panel panel1;
        private Panel panel2;
    }
}