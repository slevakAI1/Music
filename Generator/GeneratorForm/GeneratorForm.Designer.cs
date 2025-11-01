namespace Music.Generator
{
    partial class GeneratorForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.ComboBox cbPattern;

        private System.Windows.Forms.GroupBox grpTarget;
        private System.Windows.Forms.CheckedListBox clbParts;
        private System.Windows.Forms.Label lblPart;
        private System.Windows.Forms.Label lblStaff;
        private System.Windows.Forms.NumericUpDown numStaff;
        private System.Windows.Forms.Label lblStartBar;
        private System.Windows.Forms.NumericUpDown numStartBar;
        private System.Windows.Forms.Label lblEndBar;
        private System.Windows.Forms.NumericUpDown numEndBar;
        private System.Windows.Forms.Label lblEndBarTotal; // new: displays total bars near End Bar
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
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblDegree;
        private System.Windows.Forms.NumericUpDown numDegree;
        private System.Windows.Forms.Label lblOctaveKR;
        private System.Windows.Forms.NumericUpDown numOctaveKR;

        private System.Windows.Forms.Button btnApplySetNotes;
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
            txtSections = new TextBox();
            lblSections = new Label();
            lblStartBeat = new Label();
            numStartBeat = new NumericUpDown();
            lblEndBeat = new Label();
            numericUpDown2 = new NumericUpDown();
            lblPart = new Label();
            clbParts = new CheckedListBox();
            lblStaff = new Label();
            numStaff = new NumericUpDown();
            lblStartBar = new Label();
            numStartBar = new NumericUpDown();
            lblEndBar = new Label();
            numEndBar = new NumericUpDown();
            lblEndBarTotal = new Label();
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
            label1 = new Label();
            lblDegree = new Label();
            numDegree = new NumericUpDown();
            lblOctaveKR = new Label();
            numOctaveKR = new NumericUpDown();
            btnApplySetNotes = new Button();
            lblStatus = new Label();
            tt = new ToolTip(components);
            groupBox1 = new GroupBox();
            label3 = new Label();
            numNumberOfNotes = new NumericUpDown();
            lblNoteValue = new Label();
            cbNoteValue = new ComboBox();
            lblDots = new Label();
            numDots = new NumericUpDown();
            chkTupletEnabled = new CheckBox();
            lblTupletCount = new Label();
            numTupletCount = new NumericUpDown();
            lblTupletOf = new Label();
            numTupletOf = new NumericUpDown();
            chkTieAcross = new CheckBox();
            chkFermata = new CheckBox();
            label2 = new Label();
            btnSetGeneratorTestScenarioG1 = new Button();
            btnNewScore = new Button();
            btnSetDesignTestScenarioD1 = new Button();
            grpTarget.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numStartBeat).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numStaff).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numStartBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numEndBar).BeginInit();
            grpPitch.SuspendLayout();
            grpAbsolute.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numOctaveAbs).BeginInit();
            grpKeyRelative.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numDegree).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numOctaveKR).BeginInit();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numNumberOfNotes).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numDots).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numTupletCount).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numTupletOf).BeginInit();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 18F);
            lblTitle.Location = new Point(363, 9);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(120, 32);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Generator";
            // 
            // cbPattern
            // 
            cbPattern.DropDownStyle = ComboBoxStyle.DropDownList;
            cbPattern.Items.AddRange(new object[] { "Set Note" });
            cbPattern.Location = new Point(100, 65);
            cbPattern.Name = "cbPattern";
            cbPattern.Size = new Size(250, 23);
            cbPattern.TabIndex = 1;
            // 
            // grpTarget
            // 
            grpTarget.Controls.Add(txtSections);
            grpTarget.Controls.Add(lblSections);
            grpTarget.Controls.Add(lblStartBeat);
            grpTarget.Controls.Add(numStartBeat);
            grpTarget.Controls.Add(lblEndBeat);
            grpTarget.Controls.Add(numericUpDown2);
            grpTarget.Controls.Add(lblPart);
            grpTarget.Controls.Add(clbParts);
            grpTarget.Controls.Add(lblStaff);
            grpTarget.Controls.Add(numStaff);
            grpTarget.Controls.Add(lblStartBar);
            grpTarget.Controls.Add(numStartBar);
            grpTarget.Controls.Add(lblEndBar);
            grpTarget.Controls.Add(numEndBar);
            grpTarget.Controls.Add(lblEndBarTotal);
            grpTarget.Controls.Add(chkOverwrite);
            grpTarget.Location = new Point(20, 100);
            grpTarget.Name = "grpTarget";
            grpTarget.Size = new Size(442, 335);
            grpTarget.TabIndex = 2;
            grpTarget.TabStop = false;
            grpTarget.Text = "Target Scope";
            // 
            // txtSections
            // 
            txtSections.BorderStyle = BorderStyle.FixedSingle;
            txtSections.Location = new Point(81, 170);
            txtSections.Name = "txtSections";
            txtSections.Size = new Size(297, 23);
            txtSections.TabIndex = 17;
            // 
            // lblSections
            // 
            lblSections.AutoSize = true;
            lblSections.ForeColor = Color.Red;
            lblSections.Location = new Point(15, 174);
            lblSections.Name = "lblSections";
            lblSections.Size = new Size(54, 15);
            lblSections.TabIndex = 16;
            lblSections.Text = "Sections:";
            // 
            // lblStartBeat
            // 
            lblStartBeat.AutoSize = true;
            lblStartBeat.Location = new Point(15, 244);
            lblStartBeat.Name = "lblStartBeat";
            lblStartBeat.Size = new Size(60, 15);
            lblStartBeat.TabIndex = 15;
            lblStartBeat.Text = "Start Beat:";
            // 
            // numStartBeat
            // 
            numStartBeat.Location = new Point(80, 241);
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
            lblEndBeat.Location = new Point(203, 244);
            lblEndBeat.Name = "lblEndBeat";
            lblEndBeat.Size = new Size(56, 15);
            lblEndBeat.TabIndex = 13;
            lblEndBeat.Text = "End Beat:";
            // 
            // numericUpDown2
            // 
            numericUpDown2.Location = new Point(262, 240);
            numericUpDown2.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numericUpDown2.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericUpDown2.Name = "numericUpDown2";
            numericUpDown2.Size = new Size(120, 23);
            numericUpDown2.TabIndex = 14;
            numericUpDown2.Value = new decimal(new int[] { 4, 0, 0, 0 });
            // 
            // lblPart
            // 
            lblPart.AutoSize = true;
            lblPart.ForeColor = Color.FromArgb(0, 192, 0);
            lblPart.Location = new Point(15, 30);
            lblPart.Name = "lblPart";
            lblPart.Size = new Size(70, 15);
            lblPart.TabIndex = 0;
            lblPart.Text = "Part (Voice):";
            // 
            // clbParts
            // 
            clbParts.CheckOnClick = true;
            clbParts.FormattingEnabled = true;
            clbParts.Location = new Point(85, 28);
            clbParts.Name = "clbParts";
            clbParts.Size = new Size(220, 94);
            clbParts.TabIndex = 1;
            // 
            // lblStaff
            // 
            lblStaff.AutoSize = true;
            lblStaff.ForeColor = Color.Red;
            lblStaff.Location = new Point(15, 138);
            lblStaff.Name = "lblStaff";
            lblStaff.Size = new Size(34, 15);
            lblStaff.TabIndex = 5;
            lblStaff.Text = "Staff:";
            // 
            // numStaff
            // 
            numStaff.Location = new Point(80, 134);
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
            lblStartBar.ForeColor = Color.FromArgb(0, 192, 0);
            lblStartBar.Location = new Point(15, 212);
            lblStartBar.Name = "lblStartBar";
            lblStartBar.Size = new Size(54, 15);
            lblStartBar.TabIndex = 7;
            lblStartBar.Text = "Start Bar:";
            // 
            // numStartBar
            // 
            numStartBar.Location = new Point(80, 209);
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
            lblEndBar.ForeColor = Color.FromArgb(0, 192, 0);
            lblEndBar.Location = new Point(203, 212);
            lblEndBar.Name = "lblEndBar";
            lblEndBar.Size = new Size(50, 15);
            lblEndBar.TabIndex = 9;
            lblEndBar.Text = "End Bar:";
            // 
            // numEndBar
            // 
            numEndBar.Location = new Point(262, 208);
            numEndBar.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numEndBar.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numEndBar.Name = "numEndBar";
            numEndBar.Size = new Size(120, 23);
            numEndBar.TabIndex = 10;
            numEndBar.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblEndBarTotal
            // 
            lblEndBarTotal.AutoSize = true;
            lblEndBarTotal.Location = new Point(392, 212);
            lblEndBarTotal.Name = "lblEndBarTotal";
            lblEndBarTotal.Size = new Size(33, 15);
            lblEndBarTotal.TabIndex = 19;
            lblEndBarTotal.Text = "Total";
            // 
            // chkOverwrite
            // 
            chkOverwrite.AutoSize = true;
            chkOverwrite.Checked = true;
            chkOverwrite.CheckState = CheckState.Checked;
            chkOverwrite.Location = new Point(18, 275);
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
            grpPitch.Location = new Point(20, 454);
            grpPitch.Name = "grpPitch";
            grpPitch.Size = new Size(442, 235);
            grpPitch.TabIndex = 3;
            grpPitch.TabStop = false;
            grpPitch.Text = "Pitch Options";
            // 
            // rbPitchAbsolute
            // 
            rbPitchAbsolute.AutoSize = true;
            rbPitchAbsolute.ForeColor = Color.Orange;
            rbPitchAbsolute.Location = new Point(18, 28);
            rbPitchAbsolute.Name = "rbPitchAbsolute";
            rbPitchAbsolute.Size = new Size(209, 19);
            rbPitchAbsolute.TabIndex = 0;
            rbPitchAbsolute.Text = "Absolute (Step/Accidental/Octave)";
            // 
            // rbPitchKeyRelative
            // 
            rbPitchKeyRelative.AutoSize = true;
            rbPitchKeyRelative.Location = new Point(18, 50);
            rbPitchKeyRelative.Name = "rbPitchKeyRelative";
            rbPitchKeyRelative.Size = new Size(177, 19);
            rbPitchKeyRelative.TabIndex = 1;
            rbPitchKeyRelative.Text = "Key-relative (Degree/Octave)";
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
            grpAbsolute.Size = new Size(407, 65);
            grpAbsolute.TabIndex = 2;
            grpAbsolute.TabStop = false;
            grpAbsolute.Text = "Absolute";
            // 
            // lblStep
            // 
            lblStep.AutoSize = true;
            lblStep.ForeColor = Color.FromArgb(0, 192, 0);
            lblStep.Location = new Point(10, 30);
            lblStep.Name = "lblStep";
            lblStep.Size = new Size(33, 15);
            lblStep.TabIndex = 0;
            lblStep.Text = "Step:";
            // 
            // cbStep
            // 
            cbStep.DropDownStyle = ComboBoxStyle.DropDownList;
            cbStep.Items.AddRange(new object[] { "C", "D", "E", "F", "G", "A", "B" });
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
            lblAccidental.Size = new Size(66, 15);
            lblAccidental.TabIndex = 2;
            lblAccidental.Text = "Accidental:";
            // 
            // cbAccidental
            // 
            cbAccidental.DropDownStyle = ComboBoxStyle.DropDownList;
            cbAccidental.Items.AddRange(new object[] { "Natural", "Sharp", "Flat" });
            cbAccidental.Location = new Point(194, 26);
            cbAccidental.Name = "cbAccidental";
            cbAccidental.Size = new Size(80, 23);
            cbAccidental.TabIndex = 3;
            // 
            // lblOctaveAbs
            // 
            lblOctaveAbs.AutoSize = true;
            lblOctaveAbs.Location = new Point(284, 30);
            lblOctaveAbs.Name = "lblOctaveAbs";
            lblOctaveAbs.Size = new Size(47, 15);
            lblOctaveAbs.TabIndex = 4;
            lblOctaveAbs.Text = "Octave:";
            // 
            // numOctaveAbs
            // 
            numOctaveAbs.Location = new Point(336, 26);
            numOctaveAbs.Maximum = new decimal(new int[] { 8, 0, 0, 0 });
            numOctaveAbs.Name = "numOctaveAbs";
            numOctaveAbs.Size = new Size(47, 23);
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
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(193, 30);
            label1.Name = "label1";
            label1.Size = new Size(44, 15);
            label1.TabIndex = 4;
            label1.Text = "Octave";
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
            numOctaveKR.Maximum = new decimal(new int[] { 8, 0, 0, 0 });
            numOctaveKR.Name = "numOctaveKR";
            numOctaveKR.Size = new Size(104, 23);
            numOctaveKR.TabIndex = 3;
            numOctaveKR.Value = new decimal(new int[] { 4, 0, 0, 0 });
            // 
            // btnApplySetNotes
            // 
            btnApplySetNotes.Location = new Point(537, 216);
            btnApplySetNotes.Name = "btnApplySetNotes";
            btnApplySetNotes.Size = new Size(215, 29);
            btnApplySetNotes.TabIndex = 4;
            btnApplySetNotes.Text = "Appy Set Notes";
            btnApplySetNotes.Click += btnApplySetNotes_Click;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(537, 266);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(42, 15);
            lblStatus.TabIndex = 5;
            lblStatus.Text = "Ready.";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(numNumberOfNotes);
            groupBox1.Controls.Add(lblNoteValue);
            groupBox1.Controls.Add(cbNoteValue);
            groupBox1.Controls.Add(lblDots);
            groupBox1.Controls.Add(numDots);
            groupBox1.Controls.Add(chkTupletEnabled);
            groupBox1.Controls.Add(lblTupletCount);
            groupBox1.Controls.Add(numTupletCount);
            groupBox1.Controls.Add(lblTupletOf);
            groupBox1.Controls.Add(numTupletOf);
            groupBox1.Controls.Add(chkTieAcross);
            groupBox1.Controls.Add(chkFermata);
            groupBox1.Location = new Point(487, 342);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(321, 235);
            groupBox1.TabIndex = 6;
            groupBox1.TabStop = false;
            groupBox1.Text = "Rhythm options";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.ForeColor = Color.FromArgb(0, 192, 0);
            label3.Location = new Point(15, 166);
            label3.Name = "label3";
            label3.Size = new Size(99, 15);
            label3.TabIndex = 19;
            label3.Text = "Number of Notes";
            // 
            // numNumberOfNotes
            // 
            numNumberOfNotes.Location = new Point(124, 164);
            numNumberOfNotes.Name = "numNumberOfNotes";
            numNumberOfNotes.Size = new Size(71, 23);
            numNumberOfNotes.TabIndex = 18;
            // 
            // lblNoteValue
            // 
            lblNoteValue.AutoSize = true;
            lblNoteValue.ForeColor = Color.FromArgb(255, 128, 0);
            lblNoteValue.Location = new Point(15, 40);
            lblNoteValue.Name = "lblNoteValue";
            lblNoteValue.Size = new Size(112, 15);
            lblNoteValue.TabIndex = 7;
            lblNoteValue.Text = "Note Value (Length)";
            // 
            // cbNoteValue
            // 
            cbNoteValue.DropDownStyle = ComboBoxStyle.DropDownList;
            cbNoteValue.Items.AddRange(new object[] { "Rest (0)", "Whole (1)", "Half (1/2)", "Quarter (1/4)", "Eighth (1/8)", "16th (1/16)" });
            cbNoteValue.Location = new Point(133, 38);
            cbNoteValue.Name = "cbNoteValue";
            cbNoteValue.Size = new Size(175, 23);
            cbNoteValue.TabIndex = 8;
            // 
            // lblDots
            // 
            lblDots.AutoSize = true;
            lblDots.Location = new Point(15, 72);
            lblDots.Name = "lblDots";
            lblDots.Size = new Size(34, 15);
            lblDots.TabIndex = 9;
            lblDots.Text = "Dots:";
            // 
            // numDots
            // 
            numDots.Location = new Point(101, 70);
            numDots.Maximum = new decimal(new int[] { 3, 0, 0, 0 });
            numDots.Name = "numDots";
            numDots.Size = new Size(60, 23);
            numDots.TabIndex = 10;
            // 
            // chkTupletEnabled
            // 
            chkTupletEnabled.AutoSize = true;
            chkTupletEnabled.Location = new Point(15, 102);
            chkTupletEnabled.Name = "chkTupletEnabled";
            chkTupletEnabled.Size = new Size(98, 19);
            chkTupletEnabled.TabIndex = 11;
            chkTupletEnabled.Text = "Enable Tuplet";
            // 
            // lblTupletCount
            // 
            lblTupletCount.AutoSize = true;
            lblTupletCount.Location = new Point(30, 130);
            lblTupletCount.Name = "lblTupletCount";
            lblTupletCount.Size = new Size(65, 15);
            lblTupletCount.TabIndex = 12;
            lblTupletCount.Text = "Count (m):";
            // 
            // numTupletCount
            // 
            numTupletCount.Location = new Point(100, 126);
            numTupletCount.Maximum = new decimal(new int[] { 64, 0, 0, 0 });
            numTupletCount.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            numTupletCount.Name = "numTupletCount";
            numTupletCount.Size = new Size(60, 23);
            numTupletCount.TabIndex = 13;
            numTupletCount.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // lblTupletOf
            // 
            lblTupletOf.AutoSize = true;
            lblTupletOf.Location = new Point(166, 130);
            lblTupletOf.Name = "lblTupletOf";
            lblTupletOf.Size = new Size(41, 15);
            lblTupletOf.TabIndex = 14;
            lblTupletOf.Text = "Of (n):";
            // 
            // numTupletOf
            // 
            numTupletOf.Location = new Point(210, 126);
            numTupletOf.Maximum = new decimal(new int[] { 64, 0, 0, 0 });
            numTupletOf.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            numTupletOf.Name = "numTupletOf";
            numTupletOf.Size = new Size(55, 23);
            numTupletOf.TabIndex = 15;
            numTupletOf.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // chkTieAcross
            // 
            chkTieAcross.AutoSize = true;
            chkTieAcross.Location = new Point(15, 205);
            chkTieAcross.Name = "chkTieAcross";
            chkTieAcross.Size = new Size(113, 19);
            chkTieAcross.TabIndex = 16;
            chkTieAcross.Text = "Allow ties across";
            // 
            // chkFermata
            // 
            chkFermata.AutoSize = true;
            chkFermata.Location = new Point(135, 205);
            chkFermata.Name = "chkFermata";
            chkFermata.Size = new Size(69, 19);
            chkFermata.TabIndex = 17;
            chkFermata.Text = "Fermata";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(38, 68);
            label2.Name = "label2";
            label2.Size = new Size(45, 15);
            label2.TabIndex = 7;
            label2.Text = "Pattern";
            // 
            // btnSetGeneratorTestScenarioG1
            // 
            btnSetGeneratorTestScenarioG1.Location = new Point(543, 130);
            btnSetGeneratorTestScenarioG1.Name = "btnSetGeneratorTestScenarioG1";
            btnSetGeneratorTestScenarioG1.Size = new Size(209, 31);
            btnSetGeneratorTestScenarioG1.TabIndex = 8;
            btnSetGeneratorTestScenarioG1.Text = "Set Generator - Test Scenario G1";
            btnSetGeneratorTestScenarioG1.UseVisualStyleBackColor = true;
            btnSetGeneratorTestScenarioG1.Click += btnSetGeneratorTestScenarioG1_Click;
            // 
            // btnNewScore
            // 
            btnNewScore.Location = new Point(539, 171);
            btnNewScore.Name = "btnNewScore";
            btnNewScore.Size = new Size(213, 32);
            btnNewScore.TabIndex = 9;
            btnNewScore.Text = "New Score From Design";
            btnNewScore.UseVisualStyleBackColor = true;
            btnNewScore.Click += btnNewScore_Click;
            // 
            // btnSetDesignTestScenarioD1
            // 
            btnSetDesignTestScenarioD1.Location = new Point(543, 93);
            btnSetDesignTestScenarioD1.Name = "btnSetDesignTestScenarioD1";
            btnSetDesignTestScenarioD1.Size = new Size(209, 31);
            btnSetDesignTestScenarioD1.TabIndex = 10;
            btnSetDesignTestScenarioD1.Text = "Set Design - Test Scenario D1";
            btnSetDesignTestScenarioD1.UseVisualStyleBackColor = true;
            btnSetDesignTestScenarioD1.Click += btnSetDesignTestScenarioD1_Click;
            // 
            // GeneratorForm
            // 
            ClientSize = new Size(832, 701);
            Controls.Add(btnSetDesignTestScenarioD1);
            Controls.Add(btnNewScore);
            Controls.Add(btnSetGeneratorTestScenarioG1);
            Controls.Add(label2);
            Controls.Add(groupBox1);
            Controls.Add(lblTitle);
            Controls.Add(cbPattern);
            Controls.Add(grpTarget);
            Controls.Add(grpPitch);
            Controls.Add(btnApplySetNotes);
            Controls.Add(lblStatus);
            Name = "GeneratorForm";
            Text = "Generate 2 (MusicXML)";
            grpTarget.ResumeLayout(false);
            grpTarget.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numStartBeat).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown2).EndInit();
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
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numNumberOfNotes).EndInit();
            ((System.ComponentModel.ISupportInitialize)numDots).EndInit();
            ((System.ComponentModel.ISupportInitialize)numTupletCount).EndInit();
            ((System.ComponentModel.ISupportInitialize)numTupletOf).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
        private Label lblStartBeat;
        private NumericUpDown numStartBeat;
        private Label lblEndBeat;
        private NumericUpDown numericUpDown2;
        private ToolTip tt;
        private GroupBox groupBox1;

        // New time/duration control fields
        private Label lblNoteValue;
        private ComboBox cbNoteValue;
        private Label lblDots;
        private NumericUpDown numDots;
        private CheckBox chkTupletEnabled;
        private Label lblTupletCount;
        private NumericUpDown numTupletCount;
        private Label lblTupletOf;
        private NumericUpDown numTupletOf;
        private CheckBox chkTieAcross;
        private CheckBox chkFermata;
        private Label lblSections;
        private TextBox txtSections;
        private Label label3;
        private NumericUpDown numNumberOfNotes;
        private Label label2;
        private Button btnSetGeneratorTestScenarioG1;
        private Button btnNewScore;
        private Button btnSetDesignTestScenarioD1;
    }
}