namespace Music.Writer
{
    partial class WriterForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.ComboBox cbPattern;

        private System.Windows.Forms.GroupBox grpTarget;
        private System.Windows.Forms.CheckedListBox clbParts;
        private System.Windows.Forms.Label lblPart;
        private System.Windows.Forms.Label lblStaff;
        private System.Windows.Forms.CheckedListBox clbStaffs; // Changed from NumericUpDown numStaff
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
        private System.Windows.Forms.Label lblDegree;
        private System.Windows.Forms.NumericUpDown numDegree;
        private System.Windows.Forms.Label lblOctaveKR;

        private System.Windows.Forms.Button btnSetNotesOld;
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
            clbSections = new CheckedListBox();
            lblSections = new Label();
            lblStartBeat = new Label();
            lblPart = new Label();
            clbParts = new CheckedListBox();
            numStartBeat = new NumericUpDown();
            lblStaff = new Label();
            clbStaffs = new CheckedListBox();
            lblEndBeat = new Label();
            chkOverwrite = new CheckBox();
            numericUpDown2 = new NumericUpDown();
            lblEndBarTotal = new Label();
            numEndBar = new NumericUpDown();
            lblEndBar = new Label();
            numStartBar = new NumericUpDown();
            lblStartBar = new Label();
            grpPitch = new GroupBox();
            grpChord = new GroupBox();
            label1 = new Label();
            lblKey = new Label();
            cbChordKey = new ComboBox();
            lblDegree2 = new Label();
            numChordDegree = new NumericUpDown();
            lblQuality = new Label();
            cbChordQuality = new ComboBox();
            label8 = new Label();
            cbChordBase = new ComboBox();
            lblOctaveAbs = new Label();
            numOctaveAbs = new NumericUpDown();
            rbChord = new RadioButton();
            rbIsRest = new RadioButton();
            rbPitchAbsolute = new RadioButton();
            rbPitchKeyRelative = new RadioButton();
            grpAbsolute = new GroupBox();
            lblStep = new Label();
            cbStep = new ComboBox();
            lblAccidental = new Label();
            cbAccidental = new ComboBox();
            grpKeyRelative = new GroupBox();
            lblDegree = new Label();
            numDegree = new NumericUpDown();
            lblOctaveKR = new Label();
            btnSetNotesOld = new Button();
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
            btnSetWriterTestScenarioG1 = new Button();
            btnNewScore = new Button();
            btnSetDesignTestScenarioD1 = new Button();
            btnChordTest = new Button();
            lblNextThingToDo = new Label();
            btnUpdateFormFromDesigner = new Button();
            btnSetNotesNew = new Button();
            grpTarget.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numStartBeat).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numEndBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numStartBar).BeginInit();
            grpPitch.SuspendLayout();
            grpChord.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numChordDegree).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numOctaveAbs).BeginInit();
            grpAbsolute.SuspendLayout();
            grpKeyRelative.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numDegree).BeginInit();
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
            lblTitle.Size = new Size(79, 32);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Writer";
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
            grpTarget.Controls.Add(clbSections);
            grpTarget.Controls.Add(lblSections);
            grpTarget.Controls.Add(lblStartBeat);
            grpTarget.Controls.Add(lblPart);
            grpTarget.Controls.Add(clbParts);
            grpTarget.Controls.Add(numStartBeat);
            grpTarget.Controls.Add(lblStaff);
            grpTarget.Controls.Add(clbStaffs);
            grpTarget.Controls.Add(lblEndBeat);
            grpTarget.Controls.Add(chkOverwrite);
            grpTarget.Controls.Add(numericUpDown2);
            grpTarget.Controls.Add(numEndBar);
            grpTarget.Controls.Add(lblEndBar);
            grpTarget.Controls.Add(numStartBar);
            grpTarget.Controls.Add(lblStartBar);
            grpTarget.Location = new Point(20, 100);
            grpTarget.Name = "grpTarget";
            grpTarget.Size = new Size(442, 552);
            grpTarget.TabIndex = 2;
            grpTarget.TabStop = false;
            grpTarget.Text = "Target Scope";
            // 
            // clbSections
            // 
            clbSections.CheckOnClick = true;
            clbSections.FormattingEnabled = true;
            clbSections.Location = new Point(101, 448);
            clbSections.Name = "clbSections";
            clbSections.Size = new Size(220, 94);
            clbSections.TabIndex = 20;
            // 
            // lblSections
            // 
            lblSections.AutoSize = true;
            lblSections.ForeColor = Color.Red;
            lblSections.Location = new Point(36, 448);
            lblSections.Name = "lblSections";
            lblSections.Size = new Size(54, 15);
            lblSections.TabIndex = 16;
            lblSections.Text = "Sections:";
            // 
            // lblStartBeat
            // 
            lblStartBeat.AutoSize = true;
            lblStartBeat.ForeColor = Color.Red;
            lblStartBeat.Location = new Point(205, 239);
            lblStartBeat.Name = "lblStartBeat";
            lblStartBeat.Size = new Size(60, 15);
            lblStartBeat.TabIndex = 15;
            lblStartBeat.Text = "Start Beat:";
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
            // numStartBeat
            // 
            numStartBeat.Location = new Point(270, 236);
            numStartBeat.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numStartBeat.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numStartBeat.Name = "numStartBeat";
            numStartBeat.Size = new Size(120, 23);
            numStartBeat.TabIndex = 12;
            numStartBeat.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblStaff
            // 
            lblStaff.AutoSize = true;
            lblStaff.ForeColor = Color.FromArgb(0, 192, 0);
            lblStaff.Location = new Point(15, 135);
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
            clbStaffs.Location = new Point(85, 131);
            clbStaffs.Name = "clbStaffs";
            clbStaffs.Size = new Size(120, 40);
            clbStaffs.TabIndex = 6;
            // 
            // lblEndBeat
            // 
            lblEndBeat.AutoSize = true;
            lblEndBeat.ForeColor = Color.Red;
            lblEndBeat.Location = new Point(206, 274);
            lblEndBeat.Name = "lblEndBeat";
            lblEndBeat.Size = new Size(56, 15);
            lblEndBeat.TabIndex = 13;
            lblEndBeat.Text = "End Beat:";
            // 
            // chkOverwrite
            // 
            chkOverwrite.AutoSize = true;
            chkOverwrite.ForeColor = Color.Red;
            chkOverwrite.Location = new Point(18, 359);
            chkOverwrite.Name = "chkOverwrite";
            chkOverwrite.Size = new Size(196, 19);
            chkOverwrite.TabIndex = 11;
            chkOverwrite.Text = "Overwrite existing notes in voice";
            // 
            // numericUpDown2
            // 
            numericUpDown2.Location = new Point(265, 270);
            numericUpDown2.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numericUpDown2.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericUpDown2.Name = "numericUpDown2";
            numericUpDown2.Size = new Size(120, 23);
            numericUpDown2.TabIndex = 14;
            numericUpDown2.Value = new decimal(new int[] { 4, 0, 0, 0 });
            // 
            // lblEndBarTotal
            // 
            lblEndBarTotal.AutoSize = true;
            lblEndBarTotal.Location = new Point(1142, 417);
            lblEndBarTotal.Name = "lblEndBarTotal";
            lblEndBarTotal.Size = new Size(33, 15);
            lblEndBarTotal.TabIndex = 19;
            lblEndBarTotal.Text = "Total";
            // 
            // numEndBar
            // 
            numEndBar.Location = new Point(71, 271);
            numEndBar.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numEndBar.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numEndBar.Name = "numEndBar";
            numEndBar.Size = new Size(120, 23);
            numEndBar.TabIndex = 10;
            numEndBar.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblEndBar
            // 
            lblEndBar.AutoSize = true;
            lblEndBar.ForeColor = Color.FromArgb(0, 192, 0);
            lblEndBar.Location = new Point(12, 275);
            lblEndBar.Name = "lblEndBar";
            lblEndBar.Size = new Size(50, 15);
            lblEndBar.TabIndex = 9;
            lblEndBar.Text = "End Bar:";
            // 
            // numStartBar
            // 
            numStartBar.Location = new Point(74, 236);
            numStartBar.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numStartBar.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numStartBar.Name = "numStartBar";
            numStartBar.Size = new Size(120, 23);
            numStartBar.TabIndex = 8;
            numStartBar.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblStartBar
            // 
            lblStartBar.AutoSize = true;
            lblStartBar.ForeColor = Color.FromArgb(0, 192, 0);
            lblStartBar.Location = new Point(9, 239);
            lblStartBar.Name = "lblStartBar";
            lblStartBar.Size = new Size(54, 15);
            lblStartBar.TabIndex = 7;
            lblStartBar.Text = "Start Bar:";
            // 
            // grpPitch
            // 
            grpPitch.Controls.Add(grpChord);
            grpPitch.Controls.Add(lblOctaveAbs);
            grpPitch.Controls.Add(numOctaveAbs);
            grpPitch.Controls.Add(rbChord);
            grpPitch.Controls.Add(rbIsRest);
            grpPitch.Controls.Add(rbPitchAbsolute);
            grpPitch.Controls.Add(rbPitchKeyRelative);
            grpPitch.Controls.Add(grpAbsolute);
            grpPitch.Controls.Add(grpKeyRelative);
            grpPitch.Location = new Point(504, 312);
            grpPitch.Name = "grpPitch";
            grpPitch.Size = new Size(615, 450);
            grpPitch.TabIndex = 3;
            grpPitch.TabStop = false;
            grpPitch.Text = "Pitch Options";
            // 
            // grpChord
            // 
            grpChord.Controls.Add(label1);
            grpChord.Controls.Add(lblKey);
            grpChord.Controls.Add(cbChordKey);
            grpChord.Controls.Add(lblDegree2);
            grpChord.Controls.Add(numChordDegree);
            grpChord.Controls.Add(lblQuality);
            grpChord.Controls.Add(cbChordQuality);
            grpChord.Controls.Add(label8);
            grpChord.Controls.Add(cbChordBase);
            grpChord.Location = new Point(43, 346);
            grpChord.Name = "grpChord";
            grpChord.Size = new Size(549, 98);
            grpChord.TabIndex = 11;
            grpChord.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(21, 64);
            label1.Name = "label1";
            label1.Size = new Size(43, 15);
            label1.TabIndex = 15;
            label1.Text = "Notes?";
            // 
            // lblKey
            // 
            lblKey.AutoSize = true;
            lblKey.ForeColor = Color.FromArgb(0, 192, 0);
            lblKey.Location = new Point(12, 24);
            lblKey.Name = "lblKey";
            lblKey.Size = new Size(26, 15);
            lblKey.TabIndex = 6;
            lblKey.Text = "Key";
            // 
            // cbChordKey
            // 
            cbChordKey.DropDownStyle = ComboBoxStyle.DropDownList;
            cbChordKey.Items.AddRange(new object[] { "C major", "G major", "D major", "A major", "E major", "B major", "F# major", "C# major", "F major", "Bb majo", "Eb major", "Ab major", "Db major", "Gb major", "Cb major", "A minor", "E minor", "B minor", "F# minor", "C# minor", "G# minor", "D# minor", "A# minor", "D minor", "G minor", "C minor ", "F minor ", "Bb minor", "Eb minor", "Ab minor" });
            cbChordKey.Location = new Point(44, 21);
            cbChordKey.Name = "cbChordKey";
            cbChordKey.Size = new Size(70, 23);
            cbChordKey.TabIndex = 11;
            // 
            // lblDegree2
            // 
            lblDegree2.AutoSize = true;
            lblDegree2.ForeColor = Color.FromArgb(0, 192, 0);
            lblDegree2.Location = new Point(124, 24);
            lblDegree2.Name = "lblDegree2";
            lblDegree2.Size = new Size(44, 15);
            lblDegree2.TabIndex = 7;
            lblDegree2.Text = "Degree";
            // 
            // numChordDegree
            // 
            numChordDegree.Location = new Point(174, 21);
            numChordDegree.Maximum = new decimal(new int[] { 7, 0, 0, 0 });
            numChordDegree.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numChordDegree.Name = "numChordDegree";
            numChordDegree.Size = new Size(45, 23);
            numChordDegree.TabIndex = 12;
            numChordDegree.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblQuality
            // 
            lblQuality.AutoSize = true;
            lblQuality.ForeColor = Color.FromArgb(0, 192, 0);
            lblQuality.Location = new Point(226, 24);
            lblQuality.Name = "lblQuality";
            lblQuality.Size = new Size(45, 15);
            lblQuality.TabIndex = 8;
            lblQuality.Text = "Quality";
            // 
            // cbChordQuality
            // 
            cbChordQuality.DropDownStyle = ComboBoxStyle.DropDownList;
            cbChordQuality.Items.AddRange(new object[] { "Major", "Minor", "Diminished", "Augmented", "Sus2", "Sus4", "Power5", "Major6", "Minor6", "Major6Add9", "Dominant7", "Major7", "Minor7", "Diminished7", "HalfDiminished7", "MinorMajor7", "Dominant9", "Major9", "Minor9", "Dominant11", "Dominant13", "MajorAdd9", "MajorAdd11" });
            cbChordQuality.Location = new Point(277, 21);
            cbChordQuality.Name = "cbChordQuality";
            cbChordQuality.Size = new Size(158, 23);
            cbChordQuality.TabIndex = 13;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.ForeColor = Color.FromArgb(0, 192, 0);
            label8.Location = new Point(442, 24);
            label8.Name = "label8";
            label8.Size = new Size(31, 15);
            label8.TabIndex = 10;
            label8.Text = "Base";
            // 
            // cbChordBase
            // 
            cbChordBase.DropDownStyle = ComboBoxStyle.DropDownList;
            cbChordBase.Items.AddRange(new object[] { "root", "3rd", "5th", "7th" });
            cbChordBase.Location = new Point(479, 21);
            cbChordBase.Name = "cbChordBase";
            cbChordBase.Size = new Size(46, 23);
            cbChordBase.TabIndex = 14;
            // 
            // lblOctaveAbs
            // 
            lblOctaveAbs.AutoSize = true;
            lblOctaveAbs.ForeColor = Color.FromArgb(0, 192, 0);
            lblOctaveAbs.Location = new Point(25, 89);
            lblOctaveAbs.Name = "lblOctaveAbs";
            lblOctaveAbs.Size = new Size(47, 15);
            lblOctaveAbs.TabIndex = 4;
            lblOctaveAbs.Text = "Octave:";
            // 
            // numOctaveAbs
            // 
            numOctaveAbs.Location = new Point(77, 85);
            numOctaveAbs.Maximum = new decimal(new int[] { 8, 0, 0, 0 });
            numOctaveAbs.Name = "numOctaveAbs";
            numOctaveAbs.Size = new Size(47, 23);
            numOctaveAbs.TabIndex = 5;
            numOctaveAbs.Value = new decimal(new int[] { 4, 0, 0, 0 });
            // 
            // rbChord
            // 
            rbChord.AutoSize = true;
            rbChord.ForeColor = Color.FromArgb(0, 192, 0);
            rbChord.Location = new Point(27, 321);
            rbChord.Name = "rbChord";
            rbChord.Size = new Size(58, 19);
            rbChord.TabIndex = 5;
            rbChord.Text = "Chord";
            // 
            // rbIsRest
            // 
            rbIsRest.AutoSize = true;
            rbIsRest.ForeColor = Color.FromArgb(0, 192, 0);
            rbIsRest.Location = new Point(27, 33);
            rbIsRest.Name = "rbIsRest";
            rbIsRest.Size = new Size(47, 19);
            rbIsRest.TabIndex = 4;
            rbIsRest.Text = "Rest";
            // 
            // rbPitchAbsolute
            // 
            rbPitchAbsolute.AutoSize = true;
            rbPitchAbsolute.Checked = true;
            rbPitchAbsolute.ForeColor = Color.FromArgb(0, 192, 0);
            rbPitchAbsolute.Location = new Point(27, 124);
            rbPitchAbsolute.Name = "rbPitchAbsolute";
            rbPitchAbsolute.Size = new Size(72, 19);
            rbPitchAbsolute.TabIndex = 0;
            rbPitchAbsolute.TabStop = true;
            rbPitchAbsolute.Text = "Absolute";
            // 
            // rbPitchKeyRelative
            // 
            rbPitchKeyRelative.AutoSize = true;
            rbPitchKeyRelative.Location = new Point(27, 222);
            rbPitchKeyRelative.Name = "rbPitchKeyRelative";
            rbPitchKeyRelative.Size = new Size(87, 19);
            rbPitchKeyRelative.TabIndex = 1;
            rbPitchKeyRelative.Text = "Key-relative";
            // 
            // grpAbsolute
            // 
            grpAbsolute.Controls.Add(lblStep);
            grpAbsolute.Controls.Add(cbStep);
            grpAbsolute.Controls.Add(lblAccidental);
            grpAbsolute.Controls.Add(cbAccidental);
            grpAbsolute.Location = new Point(33, 139);
            grpAbsolute.Name = "grpAbsolute";
            grpAbsolute.Size = new Size(369, 65);
            grpAbsolute.TabIndex = 2;
            grpAbsolute.TabStop = false;
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
            lblAccidental.ForeColor = Color.FromArgb(0, 192, 0);
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
            // grpKeyRelative
            // 
            grpKeyRelative.Controls.Add(lblDegree);
            grpKeyRelative.Controls.Add(numDegree);
            grpKeyRelative.Controls.Add(lblOctaveKR);
            grpKeyRelative.Location = new Point(42, 236);
            grpKeyRelative.Name = "grpKeyRelative";
            grpKeyRelative.Size = new Size(360, 65);
            grpKeyRelative.TabIndex = 3;
            grpKeyRelative.TabStop = false;
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
            // btnSetNotesOld
            // 
            btnSetNotesOld.Location = new Point(511, 228);
            btnSetNotesOld.Name = "btnSetNotesOld";
            btnSetNotesOld.Size = new Size(143, 22);
            btnSetNotesOld.TabIndex = 4;
            btnSetNotesOld.Text = "Set Notes Old";
            btnSetNotesOld.Click += btnSetNotesOld_Click;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(563, 266);
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
            groupBox1.Location = new Point(757, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(433, 294);
            groupBox1.TabIndex = 6;
            groupBox1.TabStop = false;
            groupBox1.Text = "Rhythm options";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.ForeColor = Color.FromArgb(0, 192, 0);
            label3.Location = new Point(18, 101);
            label3.Name = "label3";
            label3.Size = new Size(99, 15);
            label3.TabIndex = 19;
            label3.Text = "Number of Notes";
            // 
            // numNumberOfNotes
            // 
            numNumberOfNotes.Location = new Point(133, 99);
            numNumberOfNotes.Name = "numNumberOfNotes";
            numNumberOfNotes.Size = new Size(71, 23);
            numNumberOfNotes.TabIndex = 18;
            // 
            // lblNoteValue
            // 
            lblNoteValue.AutoSize = true;
            lblNoteValue.ForeColor = Color.FromArgb(0, 192, 0);
            lblNoteValue.Location = new Point(15, 40);
            lblNoteValue.Name = "lblNoteValue";
            lblNoteValue.Size = new Size(112, 15);
            lblNoteValue.TabIndex = 7;
            lblNoteValue.Text = "Note Value (Length)";
            // 
            // cbNoteValue
            // 
            cbNoteValue.DropDownStyle = ComboBoxStyle.DropDownList;
            cbNoteValue.Items.AddRange(new object[] { "Whole (1)", "Half (1/2)", "Quarter (1/4)", "Eighth (1/8)", "16th (1/16)" });
            cbNoteValue.Location = new Point(133, 38);
            cbNoteValue.Name = "cbNoteValue";
            cbNoteValue.Size = new Size(175, 23);
            cbNoteValue.TabIndex = 8;
            // 
            // lblDots
            // 
            lblDots.AutoSize = true;
            lblDots.Location = new Point(18, 68);
            lblDots.Name = "lblDots";
            lblDots.Size = new Size(34, 15);
            lblDots.TabIndex = 9;
            lblDots.Text = "Dots:";
            // 
            // numDots
            // 
            numDots.Location = new Point(133, 68);
            numDots.Maximum = new decimal(new int[] { 3, 0, 0, 0 });
            numDots.Name = "numDots";
            numDots.Size = new Size(60, 23);
            numDots.TabIndex = 10;
            // 
            // chkTupletEnabled
            // 
            chkTupletEnabled.AutoSize = true;
            chkTupletEnabled.ForeColor = Color.Red;
            chkTupletEnabled.Location = new Point(15, 186);
            chkTupletEnabled.Name = "chkTupletEnabled";
            chkTupletEnabled.Size = new Size(98, 19);
            chkTupletEnabled.TabIndex = 11;
            chkTupletEnabled.Text = "Enable Tuplet";
            // 
            // lblTupletCount
            // 
            lblTupletCount.AutoSize = true;
            lblTupletCount.ForeColor = Color.Red;
            lblTupletCount.Location = new Point(30, 214);
            lblTupletCount.Name = "lblTupletCount";
            lblTupletCount.Size = new Size(65, 15);
            lblTupletCount.TabIndex = 12;
            lblTupletCount.Text = "Count (m):";
            // 
            // numTupletCount
            // 
            numTupletCount.Location = new Point(100, 210);
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
            lblTupletOf.ForeColor = Color.Red;
            lblTupletOf.Location = new Point(166, 214);
            lblTupletOf.Name = "lblTupletOf";
            lblTupletOf.Size = new Size(41, 15);
            lblTupletOf.TabIndex = 14;
            lblTupletOf.Text = "Of (n):";
            // 
            // numTupletOf
            // 
            numTupletOf.Location = new Point(210, 210);
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
            chkTieAcross.ForeColor = Color.Red;
            chkTieAcross.Location = new Point(15, 248);
            chkTieAcross.Name = "chkTieAcross";
            chkTieAcross.Size = new Size(113, 19);
            chkTieAcross.TabIndex = 16;
            chkTieAcross.Text = "Allow ties across";
            // 
            // chkFermata
            // 
            chkFermata.AutoSize = true;
            chkFermata.ForeColor = Color.Red;
            chkFermata.Location = new Point(135, 248);
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
            // btnSetWriterTestScenarioG1
            // 
            btnSetWriterTestScenarioG1.Location = new Point(491, 89);
            btnSetWriterTestScenarioG1.Name = "btnSetWriterTestScenarioG1";
            btnSetWriterTestScenarioG1.Size = new Size(196, 31);
            btnSetWriterTestScenarioG1.TabIndex = 8;
            btnSetWriterTestScenarioG1.Text = "Set Writer - Test Scenario G1";
            btnSetWriterTestScenarioG1.UseVisualStyleBackColor = true;
            btnSetWriterTestScenarioG1.Click += btnSetWriterTestScenarioG1_Click;
            // 
            // btnNewScore
            // 
            btnNewScore.Location = new Point(487, 126);
            btnNewScore.Name = "btnNewScore";
            btnNewScore.Size = new Size(200, 32);
            btnNewScore.TabIndex = 9;
            btnNewScore.Text = "New Score From Design";
            btnNewScore.UseVisualStyleBackColor = true;
            btnNewScore.Click += btnNewScore_Click;
            // 
            // btnSetDesignTestScenarioD1
            // 
            btnSetDesignTestScenarioD1.Location = new Point(491, 49);
            btnSetDesignTestScenarioD1.Name = "btnSetDesignTestScenarioD1";
            btnSetDesignTestScenarioD1.Size = new Size(196, 31);
            btnSetDesignTestScenarioD1.TabIndex = 10;
            btnSetDesignTestScenarioD1.Text = "Set Design - Test Scenario D1";
            btnSetDesignTestScenarioD1.UseVisualStyleBackColor = true;
            btnSetDesignTestScenarioD1.Click += btnSetDesignTestScenarioD1_Click;
            // 
            // btnChordTest
            // 
            btnChordTest.Location = new Point(1142, 376);
            btnChordTest.Name = "btnChordTest";
            btnChordTest.Size = new Size(112, 23);
            btnChordTest.TabIndex = 11;
            btnChordTest.Text = "Chord Test";
            btnChordTest.UseVisualStyleBackColor = true;
            btnChordTest.Click += btnChordTest_Click;
            // 
            // lblNextThingToDo
            // 
            lblNextThingToDo.AutoSize = true;
            lblNextThingToDo.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblNextThingToDo.ForeColor = Color.Fuchsia;
            lblNextThingToDo.Location = new Point(1000, 542);
            lblNextThingToDo.Name = "lblNextThingToDo";
            lblNextThingToDo.Size = new Size(388, 21);
            lblNextThingToDo.TabIndex = 12;
            lblNextThingToDo.Text = "Next - Start and end beats & overwrite/insert mode";
            // 
            // btnUpdateFormFromDesigner
            // 
            btnUpdateFormFromDesigner.Location = new Point(1142, 343);
            btnUpdateFormFromDesigner.Name = "btnUpdateFormFromDesigner";
            btnUpdateFormFromDesigner.Size = new Size(155, 23);
            btnUpdateFormFromDesigner.TabIndex = 13;
            btnUpdateFormFromDesigner.Text = "Update From Designer";
            btnUpdateFormFromDesigner.UseVisualStyleBackColor = true;
            btnUpdateFormFromDesigner.Click += btnUpdateFormFromDesigner_Click;
            // 
            // btnSetNotesNew
            // 
            btnSetNotesNew.Location = new Point(513, 198);
            btnSetNotesNew.Name = "btnSetNotesNew";
            btnSetNotesNew.Size = new Size(141, 25);
            btnSetNotesNew.TabIndex = 14;
            btnSetNotesNew.Text = "Set Notes New";
            btnSetNotesNew.UseVisualStyleBackColor = true;
            btnSetNotesNew.Click += btnSetNotesNew_Click;
            // 
            // WriterForm
            // 
            ClientSize = new Size(1436, 789);
            Controls.Add(btnSetNotesNew);
            Controls.Add(btnUpdateFormFromDesigner);
            Controls.Add(lblNextThingToDo);
            Controls.Add(btnChordTest);
            Controls.Add(btnSetDesignTestScenarioD1);
            Controls.Add(btnNewScore);
            Controls.Add(btnSetWriterTestScenarioG1);
            Controls.Add(label2);
            Controls.Add(groupBox1);
            Controls.Add(lblTitle);
            Controls.Add(cbPattern);
            Controls.Add(lblEndBarTotal);
            Controls.Add(grpTarget);
            Controls.Add(grpPitch);
            Controls.Add(btnSetNotesOld);
            Controls.Add(lblStatus);
            Name = "WriterForm";
            Text = "Writer 2 (MusicXML)";
            grpTarget.ResumeLayout(false);
            grpTarget.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numStartBeat).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown2).EndInit();
            ((System.ComponentModel.ISupportInitialize)numEndBar).EndInit();
            ((System.ComponentModel.ISupportInitialize)numStartBar).EndInit();
            grpPitch.ResumeLayout(false);
            grpPitch.PerformLayout();
            grpChord.ResumeLayout(false);
            grpChord.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numChordDegree).EndInit();
            ((System.ComponentModel.ISupportInitialize)numOctaveAbs).EndInit();
            grpAbsolute.ResumeLayout(false);
            grpAbsolute.PerformLayout();
            grpKeyRelative.ResumeLayout(false);
            grpKeyRelative.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numDegree).EndInit();
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
        private Label label3;
        private NumericUpDown numNumberOfNotes;
        private Label label2;
        private Button btnSetWriterTestScenarioG1;
        private Button btnNewScore;
        private Button btnSetDesignTestScenarioD1;
        private RadioButton rbIsRest;
        private Button btnChordTest;
        private RadioButton rbChord;
        private Label lblQuality;
        private Label lblDegree2;
        private Label lblKey;
        private Label label8;
        private GroupBox grpChord;
        private ComboBox cbChordKey;
        private NumericUpDown numChordDegree;
        private ComboBox cbChordQuality;
        private ComboBox cbChordBase;
        private Label label1;
        private Label lblNextThingToDo;
        private CheckedListBox clbSections;
        private Button btnUpdateFormFromDesigner;
        private Button btnSetNotesNew;
    }
}