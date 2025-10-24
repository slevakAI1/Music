namespace Music.Generate
{
    partial class Generate2Form
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.ComboBox cbPattern;

        private System.Windows.Forms.GroupBox grpTarget;
        private System.Windows.Forms.CheckBox chkAllParts;
        private System.Windows.Forms.ComboBox cbPart;
        private System.Windows.Forms.Label lblPart;
        private System.Windows.Forms.Label lblVoices;
        private System.Windows.Forms.TextBox txtVoices;
        private System.Windows.Forms.Label lblStaff;
        private System.Windows.Forms.NumericUpDown numStaff;
        private System.Windows.Forms.Label lblStartBar;
        private System.Windows.Forms.NumericUpDown numStartBar;
        private System.Windows.Forms.Label lblEndBar;
        private System.Windows.Forms.NumericUpDown numEndBar;
        private System.Windows.Forms.CheckBox chkOverwrite;

        private System.Windows.Forms.GroupBox grpPitch;
        private System.Windows.Forms.RadioButton rbPitchAbsolute;
        private System.Windows.Forms.RadioButton rbPitchKeyRelative;

        private System.Windows.Forms.GroupBox grpAbsolute;
        private System.Windows.Forms.Label lblStep;
        private System.Windows.Forms.ComboBox cbStep;
        private System.Windows.Forms.Label lblAccidental;
        private System.Windows.Forms.ComboBox cbAccidental;
        private System.Windows.Forms.Label lblOctaveAbs;
        private System.Windows.Forms.NumericUpDown numOctaveAbs;

        private System.Windows.Forms.GroupBox grpKeyRelative;
        private System.Windows.Forms.Label lblDegree;
        private System.Windows.Forms.NumericUpDown numDegree;
        private System.Windows.Forms.Label lblOctaveKR;
        private System.Windows.Forms.NumericUpDown numOctaveKR;

        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Label lblStatus;

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
            components = new System.ComponentModel.Container();
            lblTitle = new Label();
            cbPattern = new ComboBox();
            grpTarget = new GroupBox();
            lblPart = new Label();
            cbPart = new ComboBox();
            chkAllParts = new CheckBox();
            lblVoices = new Label();
            txtVoices = new TextBox();
            lblStaff = new Label();
            numStaff = new NumericUpDown();
            lblStartBar = new Label();
            numStartBar = new NumericUpDown();
            lblEndBar = new Label();
            numEndBar = new NumericUpDown();
            chkOverwrite = new CheckBox();
            grpPitch = new GroupBox();
            rbPitchAbsolute = new RadioButton();
            rbPitchKeyRelative = new RadioButton();
            grpAbsolute = new GroupBox();
            lblStep = new Label();
            cbStep = new ComboBox();
            lblAccidental = new Label();
            cbAccidental = new ComboBox();
            lblOctaveAbs = new Label();
            numOctaveAbs = new NumericUpDown();
            grpKeyRelative = new GroupBox();
            lblDegree = new Label();
            numDegree = new NumericUpDown();
            lblOctaveKR = new Label();
            numOctaveKR = new NumericUpDown();
            btnApply = new Button();
            lblStatus = new Label();
            tt = new ToolTip(components);
            numStartBeat = new NumericUpDown();
            lblEndBeat = new Label();
            numericUpDown2 = new NumericUpDown();
            lblStartBeat = new Label();
            label1 = new Label();
            groupBox1 = new GroupBox();
            label2 = new Label();
            grpTarget.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numStaff).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numStartBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numEndBar).BeginInit();
            grpPitch.SuspendLayout();
            grpAbsolute.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numOctaveAbs).BeginInit();
            grpKeyRelative.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numDegree).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numOctaveKR).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numStartBeat).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown2).BeginInit();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 18F);
            lblTitle.Location = new Point(20, 15);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(262, 32);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Generate 2 (MusicXML)";
            // 
            // cbPattern
            // 
            cbPattern.DropDownStyle = ComboBoxStyle.DropDownList;
            cbPattern.Location = new Point(25, 60);
            cbPattern.Name = "cbPattern";
            cbPattern.Size = new Size(250, 23);
            cbPattern.TabIndex = 1;
            // 
            // grpTarget
            // 
            grpTarget.Controls.Add(lblStartBeat);
            grpTarget.Controls.Add(numStartBeat);
            grpTarget.Controls.Add(lblEndBeat);
            grpTarget.Controls.Add(numericUpDown2);
            grpTarget.Controls.Add(lblPart);
            grpTarget.Controls.Add(cbPart);
            grpTarget.Controls.Add(chkAllParts);
            grpTarget.Controls.Add(lblVoices);
            grpTarget.Controls.Add(txtVoices);
            grpTarget.Controls.Add(lblStaff);
            grpTarget.Controls.Add(numStaff);
            grpTarget.Controls.Add(lblStartBar);
            grpTarget.Controls.Add(numStartBar);
            grpTarget.Controls.Add(lblEndBar);
            grpTarget.Controls.Add(numEndBar);
            grpTarget.Controls.Add(chkOverwrite);
            grpTarget.Location = new Point(20, 100);
            grpTarget.Name = "grpTarget";
            grpTarget.Size = new Size(400, 220);
            grpTarget.TabIndex = 2;
            grpTarget.TabStop = false;
            grpTarget.Text = "Target Scope";
            // 
            // lblPart
            // 
            lblPart.AutoSize = true;
            lblPart.Location = new Point(15, 30);
            lblPart.Name = "lblPart";
            lblPart.Size = new Size(31, 15);
            lblPart.TabIndex = 0;
            lblPart.Text = "Part:";
            // 
            // cbPart
            // 
            cbPart.Location = new Point(80, 26);
            cbPart.Name = "cbPart";
            cbPart.Size = new Size(220, 23);
            cbPart.TabIndex = 1;
            // 
            // chkAllParts
            // 
            chkAllParts.AutoSize = true;
            chkAllParts.Location = new Point(310, 28);
            chkAllParts.Name = "chkAllParts";
            chkAllParts.Size = new Size(40, 19);
            chkAllParts.TabIndex = 2;
            chkAllParts.Text = "All";
            // 
            // lblVoices
            // 
            lblVoices.AutoSize = true;
            lblVoices.Location = new Point(15, 65);
            lblVoices.Name = "lblVoices";
            lblVoices.Size = new Size(51, 15);
            lblVoices.TabIndex = 3;
            lblVoices.Text = "Voice(s):";
            // 
            // txtVoices
            // 
            txtVoices.Location = new Point(80, 60);
            txtVoices.Name = "txtVoices";
            txtVoices.Size = new Size(220, 23);
            txtVoices.TabIndex = 4;
            txtVoices.Text = "1";
            tt.SetToolTip(txtVoices, "Comma-separated voices (e.g., 1 or 1,2)");
            // 
            // lblStaff
            // 
            lblStaff.AutoSize = true;
            lblStaff.Location = new Point(15, 95);
            lblStaff.Name = "lblStaff";
            lblStaff.Size = new Size(34, 15);
            lblStaff.TabIndex = 5;
            lblStaff.Text = "Staff:";
            // 
            // numStaff
            // 
            numStaff.Location = new Point(80, 91);
            numStaff.Maximum = new decimal(new int[] { 4, 0, 0, 0 });
            numStaff.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numStaff.Name = "numStaff";
            numStaff.Size = new Size(120, 23);
            numStaff.TabIndex = 6;
            numStaff.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblStartBar
            // 
            lblStartBar.AutoSize = true;
            lblStartBar.Location = new Point(15, 125);
            lblStartBar.Name = "lblStartBar";
            lblStartBar.Size = new Size(54, 15);
            lblStartBar.TabIndex = 7;
            lblStartBar.Text = "Start Bar:";
            // 
            // numStartBar
            // 
            numStartBar.Location = new Point(80, 122);
            numStartBar.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numStartBar.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numStartBar.Name = "numStartBar";
            numStartBar.Size = new Size(120, 23);
            numStartBar.TabIndex = 8;
            numStartBar.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblEndBar
            // 
            lblEndBar.AutoSize = true;
            lblEndBar.Location = new Point(203, 125);
            lblEndBar.Name = "lblEndBar";
            lblEndBar.Size = new Size(50, 15);
            lblEndBar.TabIndex = 9;
            lblEndBar.Text = "End Bar:";
            // 
            // numEndBar
            // 
            numEndBar.Location = new Point(256, 121);
            numEndBar.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numEndBar.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numEndBar.Name = "numEndBar";
            numEndBar.Size = new Size(120, 23);
            numEndBar.TabIndex = 10;
            numEndBar.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // chkOverwrite
            // 
            chkOverwrite.AutoSize = true;
            chkOverwrite.Checked = true;
            chkOverwrite.CheckState = CheckState.Checked;
            chkOverwrite.Location = new Point(18, 188);
            chkOverwrite.Name = "chkOverwrite";
            chkOverwrite.Size = new Size(196, 19);
            chkOverwrite.TabIndex = 11;
            chkOverwrite.Text = "Overwrite existing notes in voice";
            // 
            // grpPitch
            // 
            grpPitch.Controls.Add(rbPitchAbsolute);
            grpPitch.Controls.Add(rbPitchKeyRelative);
            grpPitch.Controls.Add(grpAbsolute);
            grpPitch.Controls.Add(grpKeyRelative);
            grpPitch.Location = new Point(20, 330);
            grpPitch.Name = "grpPitch";
            grpPitch.Size = new Size(400, 220);
            grpPitch.TabIndex = 3;
            grpPitch.TabStop = false;
            grpPitch.Text = "Pitch Options";
            // 
            // rbPitchAbsolute
            // 
            rbPitchAbsolute.AutoSize = true;
            rbPitchAbsolute.Location = new Point(18, 28);
            rbPitchAbsolute.Name = "rbPitchAbsolute";
            rbPitchAbsolute.Size = new Size(209, 19);
            rbPitchAbsolute.TabIndex = 0;
            rbPitchAbsolute.Text = "Absolute (Step/Accidental/Octave)";
            rbPitchAbsolute.CheckedChanged += rbPitchMode_CheckedChanged;
            // 
            // rbPitchKeyRelative
            // 
            rbPitchKeyRelative.AutoSize = true;
            rbPitchKeyRelative.Location = new Point(18, 50);
            rbPitchKeyRelative.Name = "rbPitchKeyRelative";
            rbPitchKeyRelative.Size = new Size(177, 19);
            rbPitchKeyRelative.TabIndex = 1;
            rbPitchKeyRelative.Text = "Key-relative (Degree/Octave)";
            rbPitchKeyRelative.CheckedChanged += rbPitchMode_CheckedChanged;
            // 
            // grpAbsolute
            // 
            grpAbsolute.Controls.Add(lblStep);
            grpAbsolute.Controls.Add(cbStep);
            grpAbsolute.Controls.Add(lblAccidental);
            grpAbsolute.Controls.Add(cbAccidental);
            grpAbsolute.Controls.Add(lblOctaveAbs);
            grpAbsolute.Controls.Add(numOctaveAbs);
            grpAbsolute.Location = new Point(18, 75);
            grpAbsolute.Name = "grpAbsolute";
            grpAbsolute.Size = new Size(360, 65);
            grpAbsolute.TabIndex = 2;
            grpAbsolute.TabStop = false;
            grpAbsolute.Text = "Absolute";
            // 
            // lblStep
            // 
            lblStep.AutoSize = true;
            lblStep.Location = new Point(10, 30);
            lblStep.Name = "lblStep";
            lblStep.Size = new Size(33, 15);
            lblStep.TabIndex = 0;
            lblStep.Text = "Step:";
            // 
            // cbStep
            // 
            cbStep.DropDownStyle = ComboBoxStyle.DropDownList;
            cbStep.Location = new Point(50, 26);
            cbStep.Name = "cbStep";
            cbStep.Size = new Size(60, 23);
            cbStep.TabIndex = 1;
            // 
            // lblAccidental
            // 
            lblAccidental.AutoSize = true;
            lblAccidental.Location = new Point(120, 30);
            lblAccidental.Name = "lblAccidental";
            lblAccidental.Size = new Size(40, 15);
            lblAccidental.TabIndex = 2;
            lblAccidental.Text = "Accid:";
            // 
            // cbAccidental
            // 
            cbAccidental.DropDownStyle = ComboBoxStyle.DropDownList;
            cbAccidental.Location = new Point(165, 26);
            cbAccidental.Name = "cbAccidental";
            cbAccidental.Size = new Size(80, 23);
            cbAccidental.TabIndex = 3;
            // 
            // lblOctaveAbs
            // 
            lblOctaveAbs.AutoSize = true;
            lblOctaveAbs.Location = new Point(255, 30);
            lblOctaveAbs.Name = "lblOctaveAbs";
            lblOctaveAbs.Size = new Size(29, 15);
            lblOctaveAbs.TabIndex = 4;
            lblOctaveAbs.Text = "Oct:";
            // 
            // numOctaveAbs
            // 
            numOctaveAbs.Location = new Point(285, 26);
            numOctaveAbs.Maximum = new decimal(new int[] { 9, 0, 0, 0 });
            numOctaveAbs.Name = "numOctaveAbs";
            numOctaveAbs.Size = new Size(69, 23);
            numOctaveAbs.TabIndex = 5;
            numOctaveAbs.Value = new decimal(new int[] { 4, 0, 0, 0 });
            // 
            // grpKeyRelative
            // 
            grpKeyRelative.Controls.Add(label1);
            grpKeyRelative.Controls.Add(lblDegree);
            grpKeyRelative.Controls.Add(numDegree);
            grpKeyRelative.Controls.Add(lblOctaveKR);
            grpKeyRelative.Controls.Add(numOctaveKR);
            grpKeyRelative.Location = new Point(18, 145);
            grpKeyRelative.Name = "grpKeyRelative";
            grpKeyRelative.Size = new Size(360, 65);
            grpKeyRelative.TabIndex = 3;
            grpKeyRelative.TabStop = false;
            grpKeyRelative.Text = "Key-relative";
            // 
            // lblDegree
            // 
            lblDegree.AutoSize = true;
            lblDegree.Location = new Point(10, 30);
            lblDegree.Name = "lblDegree";
            lblDegree.Size = new Size(47, 15);
            lblDegree.TabIndex = 0;
            lblDegree.Text = "Degree:";
            // 
            // numDegree
            // 
            numDegree.Location = new Point(65, 26);
            numDegree.Maximum = new decimal(new int[] { 7, 0, 0, 0 });
            numDegree.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numDegree.Name = "numDegree";
            numDegree.Size = new Size(120, 23);
            numDegree.TabIndex = 1;
            numDegree.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblOctaveKR
            // 
            lblOctaveKR.AutoSize = true;
            lblOctaveKR.Location = new Point(140, 30);
            lblOctaveKR.Name = "lblOctaveKR";
            lblOctaveKR.Size = new Size(29, 15);
            lblOctaveKR.TabIndex = 2;
            lblOctaveKR.Text = "Oct:";
            // 
            // numOctaveKR
            // 
            numOctaveKR.Location = new Point(250, 26);
            numOctaveKR.Maximum = new decimal(new int[] { 9, 0, 0, 0 });
            numOctaveKR.Name = "numOctaveKR";
            numOctaveKR.Size = new Size(104, 23);
            numOctaveKR.TabIndex = 3;
            numOctaveKR.Value = new decimal(new int[] { 4, 0, 0, 0 });
            // 
            // btnApply
            // 
            btnApply.Location = new Point(666, 109);
            btnApply.Name = "btnApply";
            btnApply.Size = new Size(120, 36);
            btnApply.TabIndex = 4;
            btnApply.Text = "Apply";
            btnApply.Click += btnApply_Click;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(666, 159);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(42, 15);
            lblStatus.TabIndex = 5;
            lblStatus.Text = "Ready.";
            // 
            // numStartBeat
            // 
            numStartBeat.Location = new Point(80, 154);
            numStartBeat.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numStartBeat.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numStartBeat.Name = "numStartBeat";
            numStartBeat.Size = new Size(120, 23);
            numStartBeat.TabIndex = 12;
            numStartBeat.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblEndBeat
            // 
            lblEndBeat.AutoSize = true;
            lblEndBeat.Location = new Point(203, 157);
            lblEndBeat.Name = "lblEndBeat";
            lblEndBeat.Size = new Size(53, 15);
            lblEndBeat.TabIndex = 13;
            lblEndBeat.Text = "End Beat";
            // 
            // numericUpDown2
            // 
            numericUpDown2.Location = new Point(256, 153);
            numericUpDown2.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numericUpDown2.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericUpDown2.Name = "numericUpDown2";
            numericUpDown2.Size = new Size(120, 23);
            numericUpDown2.TabIndex = 14;
            numericUpDown2.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblStartBeat
            // 
            lblStartBeat.AutoSize = true;
            lblStartBeat.Location = new Point(15, 157);
            lblStartBeat.Name = "lblStartBeat";
            lblStartBeat.Size = new Size(57, 15);
            lblStartBeat.TabIndex = 15;
            lblStartBeat.Text = "Start Beat";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(193, 30);
            label1.Name = "label1";
            label1.Size = new Size(44, 15);
            label1.TabIndex = 4;
            label1.Text = "Octave";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(label2);
            groupBox1.Location = new Point(458, 330);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(285, 220);
            groupBox1.TabIndex = 6;
            groupBox1.TabStop = false;
            groupBox1.Text = "Time options";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(15, 41);
            label2.Name = "label2";
            label2.Size = new Size(82, 15);
            label2.TabIndex = 6;
            label2.Text = "Note Duration";
            // 
            // Generate2Form
            // 
            ClientSize = new Size(900, 600);
            Controls.Add(groupBox1);
            Controls.Add(lblTitle);
            Controls.Add(cbPattern);
            Controls.Add(grpTarget);
            Controls.Add(grpPitch);
            Controls.Add(btnApply);
            Controls.Add(lblStatus);
            Name = "Generate2Form";
            Text = "Generate 2 (MusicXML)";
            grpTarget.ResumeLayout(false);
            grpTarget.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numStaff).EndInit();
            ((System.ComponentModel.ISupportInitialize)numStartBar).EndInit();
            ((System.ComponentModel.ISupportInitialize)numEndBar).EndInit();
            grpPitch.ResumeLayout(false);
            grpPitch.PerformLayout();
            grpAbsolute.ResumeLayout(false);
            grpAbsolute.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numOctaveAbs).EndInit();
            grpKeyRelative.ResumeLayout(false);
            grpKeyRelative.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numDegree).EndInit();
            ((System.ComponentModel.ISupportInitialize)numOctaveKR).EndInit();
            ((System.ComponentModel.ISupportInitialize)numStartBeat).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown2).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
        private Label lblStartBeat;
        private NumericUpDown numStartBeat;
        private Label lblEndBeat;
        private NumericUpDown numericUpDown2;
        private ToolTip tt;
        private Label label1;
        private GroupBox groupBox1;
        private Label label2;
    }
}