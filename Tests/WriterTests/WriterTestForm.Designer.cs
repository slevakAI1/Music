namespace Music.Writer
{
    partial class WriterTestForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.ComboBox cbPattern;

        private System.Windows.Forms.GroupBox grpTarget;
        private System.Windows.Forms.CheckedListBox clbParts;
        private System.Windows.Forms.Label lblPart;
        private System.Windows.Forms.Label lblStaff;
        private System.Windows.Forms.CheckedListBox clbStaffs; // Changed from NumericUpDown numStaff

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

        private System.Windows.Forms.Button btnAppendNotes;

        // ScoreList management controls
        private System.Windows.Forms.GroupBox grpScoreList;
        private System.Windows.Forms.ListBox lstScores;
        private System.Windows.Forms.Button btnAddScore;
        private System.Windows.Forms.Button btnDeleteScore;
        private System.Windows.Forms.Button btnLoadScore;
        private System.Windows.Forms.Button btnUpdateScore;

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
            lblTitle = new Label();
            cbPattern = new ComboBox();
            grpTarget = new GroupBox();
            lblPart = new Label();
            clbParts = new CheckedListBox();
            lblStaff = new Label();
            clbStaffs = new CheckedListBox();
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
            btnAppendNotes = new Button();
            groupBox1 = new GroupBox();
            txtTupletNumber = new TextBox();
            lblTupletNumber = new Label();
            label3 = new Label();
            numNumberOfNotes = new NumericUpDown();
            lblNoteValue = new Label();
            cbNoteValue = new ComboBox();
            lblDots = new Label();
            numDots = new NumericUpDown();
            lblTupletCount = new Label();
            numTupletCount = new NumericUpDown();
            lblTupletOf = new Label();
            numTupletOf = new NumericUpDown();
            btnSetWriterTestScenarioG1 = new Button();
            btnNewScore = new Button();
            btnSetDesignTestScenarioD1 = new Button();
            btnChordTest = new Button();
            btnUpdateFormFromDesigner = new Button();
            txtDesignerReport = new TextBox();
            lblScoreReport = new Label();
            txtScoreReport = new TextBox();
            btnExportToNotion = new Button();
            grpScoreList = new GroupBox();
            txtMovementTitle = new TextBox();
            btnUpdateScore = new Button();
            btnLoadScore = new Button();
            btnDeleteScore = new Button();
            btnAddScore = new Button();
            lstScores = new ListBox();
            groupBox2 = new GroupBox();
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
            grbCurrentDesign = new GroupBox();
            grpTarget.SuspendLayout();
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
            grpScoreList.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numStartBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numStartBeat).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown3).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown4).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown2).BeginInit();
            groupBox3.SuspendLayout();
            grbCurrentDesign.SuspendLayout();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 18F);
            lblTitle.Location = new Point(485, 9);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(190, 32);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Writer Test Form";
            // 
            // cbPattern
            // 
            cbPattern.DropDownStyle = ComboBoxStyle.DropDownList;
            cbPattern.Items.AddRange(new object[] { "Append Pitch Events" });
            cbPattern.Location = new Point(13, 22);
            cbPattern.Name = "cbPattern";
            cbPattern.Size = new Size(250, 23);
            cbPattern.TabIndex = 1;
            // 
            // grpTarget
            // 
            grpTarget.Controls.Add(lblPart);
            grpTarget.Controls.Add(clbParts);
            grpTarget.Controls.Add(lblStaff);
            grpTarget.Controls.Add(clbStaffs);
            grpTarget.Location = new Point(20, 117);
            grpTarget.Name = "grpTarget";
            grpTarget.Size = new Size(442, 174);
            grpTarget.TabIndex = 2;
            grpTarget.TabStop = false;
            grpTarget.Text = "Target Scope";
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
            grpPitch.Location = new Point(497, 254);
            grpPitch.Name = "grpPitch";
            grpPitch.Size = new Size(592, 374);
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
            grpChord.Location = new Point(31, 259);
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
            lblOctaveAbs.Location = new Point(13, 57);
            lblOctaveAbs.Name = "lblOctaveAbs";
            lblOctaveAbs.Size = new Size(47, 15);
            lblOctaveAbs.TabIndex = 4;
            lblOctaveAbs.Text = "Octave:";
            // 
            // numOctaveAbs
            // 
            numOctaveAbs.Location = new Point(65, 53);
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
            rbChord.Location = new Point(15, 230);
            rbChord.Name = "rbChord";
            rbChord.Size = new Size(58, 19);
            rbChord.TabIndex = 5;
            rbChord.Text = "Chord";
            // 
            // rbIsRest
            // 
            rbIsRest.AutoSize = true;
            rbIsRest.ForeColor = Color.FromArgb(0, 192, 0);
            rbIsRest.Location = new Point(15, 27);
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
            rbPitchAbsolute.Location = new Point(15, 86);
            rbPitchAbsolute.Name = "rbPitchAbsolute";
            rbPitchAbsolute.Size = new Size(72, 19);
            rbPitchAbsolute.TabIndex = 0;
            rbPitchAbsolute.TabStop = true;
            rbPitchAbsolute.Text = "Absolute";
            // 
            // rbPitchKeyRelative
            // 
            rbPitchKeyRelative.AutoSize = true;
            rbPitchKeyRelative.Location = new Point(15, 159);
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
            grpAbsolute.Location = new Point(21, 101);
            grpAbsolute.Name = "grpAbsolute";
            grpAbsolute.Size = new Size(369, 46);
            grpAbsolute.TabIndex = 2;
            grpAbsolute.TabStop = false;
            // 
            // lblStep
            // 
            lblStep.AutoSize = true;
            lblStep.ForeColor = Color.FromArgb(0, 192, 0);
            lblStep.Location = new Point(10, 20);
            lblStep.Name = "lblStep";
            lblStep.Size = new Size(33, 15);
            lblStep.TabIndex = 0;
            lblStep.Text = "Step:";
            // 
            // cbStep
            // 
            cbStep.DropDownStyle = ComboBoxStyle.DropDownList;
            cbStep.Items.AddRange(new object[] { "C", "D", "E", "F", "G", "A", "B" });
            cbStep.Location = new Point(50, 16);
            cbStep.Name = "cbStep";
            cbStep.Size = new Size(60, 23);
            cbStep.TabIndex = 1;
            // 
            // lblAccidental
            // 
            lblAccidental.AutoSize = true;
            lblAccidental.ForeColor = Color.FromArgb(0, 192, 0);
            lblAccidental.Location = new Point(120, 20);
            lblAccidental.Name = "lblAccidental";
            lblAccidental.Size = new Size(66, 15);
            lblAccidental.TabIndex = 2;
            lblAccidental.Text = "Accidental:";
            // 
            // cbAccidental
            // 
            cbAccidental.DropDownStyle = ComboBoxStyle.DropDownList;
            cbAccidental.Items.AddRange(new object[] { "Natural", "Sharp", "Flat" });
            cbAccidental.Location = new Point(194, 16);
            cbAccidental.Name = "cbAccidental";
            cbAccidental.Size = new Size(80, 23);
            cbAccidental.TabIndex = 3;
            // 
            // grpKeyRelative
            // 
            grpKeyRelative.Controls.Add(lblDegree);
            grpKeyRelative.Controls.Add(numDegree);
            grpKeyRelative.Controls.Add(lblOctaveKR);
            grpKeyRelative.Location = new Point(30, 173);
            grpKeyRelative.Name = "grpKeyRelative";
            grpKeyRelative.Size = new Size(360, 47);
            grpKeyRelative.TabIndex = 3;
            grpKeyRelative.TabStop = false;
            // 
            // lblDegree
            // 
            lblDegree.AutoSize = true;
            lblDegree.Location = new Point(10, 19);
            lblDegree.Name = "lblDegree";
            lblDegree.Size = new Size(47, 15);
            lblDegree.TabIndex = 0;
            lblDegree.Text = "Degree:";
            // 
            // numDegree
            // 
            numDegree.Location = new Point(65, 15);
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
            lblOctaveKR.Location = new Point(140, 19);
            lblOctaveKR.Name = "lblOctaveKR";
            lblOctaveKR.Size = new Size(29, 15);
            lblOctaveKR.TabIndex = 2;
            lblOctaveKR.Text = "Oct:";
            // 
            // btnAppendNotes
            // 
            btnAppendNotes.Location = new Point(269, 22);
            btnAppendNotes.Name = "btnAppendNotes";
            btnAppendNotes.Size = new Size(77, 22);
            btnAppendNotes.TabIndex = 4;
            btnAppendNotes.Text = "Execute";
            btnAppendNotes.Click += btnAppendNotes_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(txtTupletNumber);
            groupBox1.Controls.Add(lblTupletNumber);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(numNumberOfNotes);
            groupBox1.Controls.Add(lblNoteValue);
            groupBox1.Controls.Add(cbNoteValue);
            groupBox1.Controls.Add(lblDots);
            groupBox1.Controls.Add(numDots);
            groupBox1.Controls.Add(lblTupletCount);
            groupBox1.Controls.Add(numTupletCount);
            groupBox1.Controls.Add(lblTupletOf);
            groupBox1.Controls.Add(numTupletOf);
            groupBox1.Location = new Point(497, 62);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(315, 174);
            groupBox1.TabIndex = 6;
            groupBox1.TabStop = false;
            groupBox1.Text = "Rhythm options";
            // 
            // txtTupletNumber
            // 
            txtTupletNumber.Location = new Point(158, 109);
            txtTupletNumber.Name = "txtTupletNumber";
            txtTupletNumber.Size = new Size(47, 23);
            txtTupletNumber.TabIndex = 21;
            // 
            // lblTupletNumber
            // 
            lblTupletNumber.AutoSize = true;
            lblTupletNumber.ForeColor = Color.FromArgb(0, 192, 0);
            lblTupletNumber.Location = new Point(16, 112);
            lblTupletNumber.Name = "lblTupletNumber";
            lblTupletNumber.Size = new Size(131, 15);
            lblTupletNumber.TabIndex = 20;
            lblTupletNumber.Text = "Tuplet Number (Tag Id)";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.ForeColor = Color.FromArgb(0, 192, 0);
            label3.Location = new Point(16, 81);
            label3.Name = "label3";
            label3.Size = new Size(99, 15);
            label3.TabIndex = 19;
            label3.Text = "Number of Notes";
            // 
            // numNumberOfNotes
            // 
            numNumberOfNotes.Location = new Point(133, 78);
            numNumberOfNotes.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numNumberOfNotes.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numNumberOfNotes.Name = "numNumberOfNotes";
            numNumberOfNotes.Size = new Size(71, 23);
            numNumberOfNotes.TabIndex = 18;
            numNumberOfNotes.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblNoteValue
            // 
            lblNoteValue.AutoSize = true;
            lblNoteValue.ForeColor = Color.FromArgb(0, 192, 0);
            lblNoteValue.Location = new Point(16, 21);
            lblNoteValue.Name = "lblNoteValue";
            lblNoteValue.Size = new Size(112, 15);
            lblNoteValue.TabIndex = 7;
            lblNoteValue.Text = "Note Value (Length)";
            // 
            // cbNoteValue
            // 
            cbNoteValue.DropDownStyle = ComboBoxStyle.DropDownList;
            cbNoteValue.Items.AddRange(new object[] { "Whole (1)", "Half (1/2)", "Quarter (1/4)", "Eighth (1/8)", "16th (1/16)", "32nd (1/32)" });
            cbNoteValue.Location = new Point(133, 18);
            cbNoteValue.Name = "cbNoteValue";
            cbNoteValue.Size = new Size(175, 23);
            cbNoteValue.TabIndex = 8;
            // 
            // lblDots
            // 
            lblDots.AutoSize = true;
            lblDots.ForeColor = Color.FromArgb(0, 192, 0);
            lblDots.Location = new Point(17, 50);
            lblDots.Name = "lblDots";
            lblDots.Size = new Size(34, 15);
            lblDots.TabIndex = 9;
            lblDots.Text = "Dots:";
            // 
            // numDots
            // 
            numDots.Location = new Point(133, 48);
            numDots.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            numDots.Name = "numDots";
            numDots.Size = new Size(60, 23);
            numDots.TabIndex = 10;
            // 
            // lblTupletCount
            // 
            lblTupletCount.AutoSize = true;
            lblTupletCount.ForeColor = Color.FromArgb(0, 192, 0);
            lblTupletCount.Location = new Point(17, 142);
            lblTupletCount.Name = "lblTupletCount";
            lblTupletCount.Size = new Size(65, 15);
            lblTupletCount.TabIndex = 12;
            lblTupletCount.Text = "Count (m):";
            // 
            // numTupletCount
            // 
            numTupletCount.Location = new Point(90, 138);
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
            lblTupletOf.ForeColor = Color.FromArgb(0, 192, 0);
            lblTupletOf.Location = new Point(158, 142);
            lblTupletOf.Name = "lblTupletOf";
            lblTupletOf.Size = new Size(41, 15);
            lblTupletOf.TabIndex = 14;
            lblTupletOf.Text = "Of (n):";
            // 
            // numTupletOf
            // 
            numTupletOf.Location = new Point(203, 138);
            numTupletOf.Maximum = new decimal(new int[] { 64, 0, 0, 0 });
            numTupletOf.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            numTupletOf.Name = "numTupletOf";
            numTupletOf.Size = new Size(55, 23);
            numTupletOf.TabIndex = 15;
            numTupletOf.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // btnSetWriterTestScenarioG1
            // 
            btnSetWriterTestScenarioG1.Location = new Point(150, 51);
            btnSetWriterTestScenarioG1.Name = "btnSetWriterTestScenarioG1";
            btnSetWriterTestScenarioG1.Size = new Size(196, 23);
            btnSetWriterTestScenarioG1.TabIndex = 8;
            btnSetWriterTestScenarioG1.Text = "Set Writer - Test Scenario G1";
            btnSetWriterTestScenarioG1.UseVisualStyleBackColor = true;
            btnSetWriterTestScenarioG1.Click += btnSetWriterTestScenarioG1_Click;
            // 
            // btnNewScore
            // 
            btnNewScore.ForeColor = Color.FromArgb(0, 192, 0);
            btnNewScore.Location = new Point(17, 379);
            btnNewScore.Name = "btnNewScore";
            btnNewScore.Size = new Size(91, 20);
            btnNewScore.TabIndex = 9;
            btnNewScore.Text = "New Score";
            btnNewScore.UseVisualStyleBackColor = true;
            btnNewScore.Click += btnNewScore_Click;
            // 
            // btnSetDesignTestScenarioD1
            // 
            btnSetDesignTestScenarioD1.Location = new Point(181, 138);
            btnSetDesignTestScenarioD1.Name = "btnSetDesignTestScenarioD1";
            btnSetDesignTestScenarioD1.Size = new Size(179, 23);
            btnSetDesignTestScenarioD1.TabIndex = 10;
            btnSetDesignTestScenarioD1.Text = "Set Design - Test Scenario D1";
            btnSetDesignTestScenarioD1.UseVisualStyleBackColor = true;
            btnSetDesignTestScenarioD1.Click += btnSetDesignTestScenarioD1_Click;
            // 
            // btnChordTest
            // 
            btnChordTest.Enabled = false;
            btnChordTest.ForeColor = Color.Red;
            btnChordTest.Location = new Point(1710, 843);
            btnChordTest.Name = "btnChordTest";
            btnChordTest.Size = new Size(196, 23);
            btnChordTest.TabIndex = 11;
            btnChordTest.Text = "Chord Test";
            btnChordTest.UseVisualStyleBackColor = true;
            btnChordTest.Click += btnChordTest_Click;
            // 
            // btnUpdateFormFromDesigner
            // 
            btnUpdateFormFromDesigner.Enabled = false;
            btnUpdateFormFromDesigner.ForeColor = Color.Red;
            btnUpdateFormFromDesigner.Location = new Point(1710, 814);
            btnUpdateFormFromDesigner.Name = "btnUpdateFormFromDesigner";
            btnUpdateFormFromDesigner.Size = new Size(196, 23);
            btnUpdateFormFromDesigner.TabIndex = 13;
            btnUpdateFormFromDesigner.Text = "Update From Designer";
            btnUpdateFormFromDesigner.UseVisualStyleBackColor = true;
            btnUpdateFormFromDesigner.Click += btnUpdateFormFromDesigner_Click;
            // 
            // txtDesignerReport
            // 
            txtDesignerReport.Location = new Point(15, 23);
            txtDesignerReport.Multiline = true;
            txtDesignerReport.Name = "txtDesignerReport";
            txtDesignerReport.ScrollBars = ScrollBars.Vertical;
            txtDesignerReport.Size = new Size(557, 109);
            txtDesignerReport.TabIndex = 26;
            // 
            // lblScoreReport
            // 
            lblScoreReport.AutoSize = true;
            lblScoreReport.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblScoreReport.ForeColor = Color.FromArgb(0, 192, 0);
            lblScoreReport.Location = new Point(120, 22);
            lblScoreReport.Name = "lblScoreReport";
            lblScoreReport.Size = new Size(51, 17);
            lblScoreReport.TabIndex = 29;
            lblScoreReport.Text = "Current";
            // 
            // txtScoreReport
            // 
            txtScoreReport.Location = new Point(16, 46);
            txtScoreReport.Multiline = true;
            txtScoreReport.Name = "txtScoreReport";
            txtScoreReport.ScrollBars = ScrollBars.Vertical;
            txtScoreReport.Size = new Size(274, 319);
            txtScoreReport.TabIndex = 28;
            // 
            // btnExportToNotion
            // 
            btnExportToNotion.Location = new Point(13, 51);
            btnExportToNotion.Name = "btnExportToNotion";
            btnExportToNotion.Size = new Size(114, 23);
            btnExportToNotion.TabIndex = 30;
            btnExportToNotion.Text = "Export to Notion";
            btnExportToNotion.UseVisualStyleBackColor = true;
            btnExportToNotion.Click += btnExportToNotion_Click;
            // 
            // grpScoreList
            // 
            grpScoreList.Controls.Add(txtMovementTitle);
            grpScoreList.Controls.Add(lblScoreReport);
            grpScoreList.Controls.Add(btnUpdateScore);
            grpScoreList.Controls.Add(txtScoreReport);
            grpScoreList.Controls.Add(btnLoadScore);
            grpScoreList.Controls.Add(btnDeleteScore);
            grpScoreList.Controls.Add(btnAddScore);
            grpScoreList.Controls.Add(lstScores);
            grpScoreList.Controls.Add(btnNewScore);
            grpScoreList.Location = new Point(1108, 28);
            grpScoreList.Name = "grpScoreList";
            grpScoreList.Size = new Size(595, 414);
            grpScoreList.TabIndex = 31;
            grpScoreList.TabStop = false;
            grpScoreList.Text = "Score List";
            // 
            // txtMovementTitle
            // 
            txtMovementTitle.Location = new Point(121, 375);
            txtMovementTitle.Name = "txtMovementTitle";
            txtMovementTitle.Size = new Size(154, 23);
            txtMovementTitle.TabIndex = 10;
            // 
            // btnUpdateScore
            // 
            btnUpdateScore.ForeColor = Color.FromArgb(0, 192, 0);
            btnUpdateScore.Location = new Point(458, 364);
            btnUpdateScore.Name = "btnUpdateScore";
            btnUpdateScore.Size = new Size(115, 30);
            btnUpdateScore.TabIndex = 4;
            btnUpdateScore.Text = "Update";
            btnUpdateScore.UseVisualStyleBackColor = true;
            btnUpdateScore.Click += btnUpdateScore_Click;
            // 
            // btnLoadScore
            // 
            btnLoadScore.ForeColor = Color.FromArgb(0, 192, 0);
            btnLoadScore.Location = new Point(328, 364);
            btnLoadScore.Name = "btnLoadScore";
            btnLoadScore.Size = new Size(115, 30);
            btnLoadScore.TabIndex = 3;
            btnLoadScore.Text = "Load";
            btnLoadScore.UseVisualStyleBackColor = true;
            btnLoadScore.Click += btnLoadScore_Click;
            // 
            // btnDeleteScore
            // 
            btnDeleteScore.ForeColor = Color.FromArgb(0, 192, 0);
            btnDeleteScore.Location = new Point(458, 330);
            btnDeleteScore.Name = "btnDeleteScore";
            btnDeleteScore.Size = new Size(115, 30);
            btnDeleteScore.TabIndex = 2;
            btnDeleteScore.Text = "Delete";
            btnDeleteScore.UseVisualStyleBackColor = true;
            btnDeleteScore.Click += btnDeleteScore_Click;
            // 
            // btnAddScore
            // 
            btnAddScore.ForeColor = Color.FromArgb(0, 192, 0);
            btnAddScore.Location = new Point(328, 330);
            btnAddScore.Name = "btnAddScore";
            btnAddScore.Size = new Size(115, 30);
            btnAddScore.TabIndex = 1;
            btnAddScore.Text = "Add";
            btnAddScore.UseVisualStyleBackColor = true;
            btnAddScore.Click += btnAddScore_Click;
            // 
            // lstScores
            // 
            lstScores.FormattingEnabled = true;
            lstScores.Location = new Point(328, 47);
            lstScores.Name = "lstScores";
            lstScores.Size = new Size(245, 274);
            lstScores.TabIndex = 0;
            // 
            // groupBox2
            // 
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
            groupBox2.Location = new Point(20, 303);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(442, 385);
            groupBox2.TabIndex = 32;
            groupBox2.TabStop = false;
            groupBox2.Text = "Splice";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.ForeColor = Color.Red;
            label13.Location = new Point(154, 298);
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
            clbSections.Location = new Point(172, 333);
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
            groupBox3.Controls.Add(cbPattern);
            groupBox3.Controls.Add(btnAppendNotes);
            groupBox3.Controls.Add(btnExportToNotion);
            groupBox3.Controls.Add(btnSetWriterTestScenarioG1);
            groupBox3.Location = new Point(29, 12);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(433, 100);
            groupBox3.TabIndex = 33;
            groupBox3.TabStop = false;
            groupBox3.Text = "Command";
            // 
            // grbCurrentDesign
            // 
            grbCurrentDesign.Controls.Add(txtDesignerReport);
            grbCurrentDesign.Controls.Add(btnSetDesignTestScenarioD1);
            grbCurrentDesign.Location = new Point(1109, 459);
            grbCurrentDesign.Name = "grbCurrentDesign";
            grbCurrentDesign.Size = new Size(594, 167);
            grbCurrentDesign.TabIndex = 34;
            grbCurrentDesign.TabStop = false;
            grbCurrentDesign.Text = "Current Design";
            // 
            // WriterTestForm
            // 
            ClientSize = new Size(1918, 878);
            Controls.Add(grbCurrentDesign);
            Controls.Add(groupBox2);
            Controls.Add(grpScoreList);
            Controls.Add(btnUpdateFormFromDesigner);
            Controls.Add(btnChordTest);
            Controls.Add(groupBox1);
            Controls.Add(lblTitle);
            Controls.Add(grpTarget);
            Controls.Add(grpPitch);
            Controls.Add(groupBox3);
            Name = "WriterTestForm";
            Text = "Writer 2 (MusicXML)";
            grpTarget.ResumeLayout(false);
            grpTarget.PerformLayout();
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
            grpScoreList.ResumeLayout(false);
            grpScoreList.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numStartBar).EndInit();
            ((System.ComponentModel.ISupportInitialize)numStartBeat).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown3).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown4).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown2).EndInit();
            groupBox3.ResumeLayout(false);
            grbCurrentDesign.ResumeLayout(false);
            grbCurrentDesign.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
        private GroupBox groupBox1;

        // New time/duration control fields
        private Label lblNoteValue;
        private ComboBox cbNoteValue;
        private Label lblDots;
        private NumericUpDown numDots;
        private Label lblTupletCount;
        private NumericUpDown numTupletCount;
        private Label lblTupletOf;
        private NumericUpDown numTupletOf;
        private Label label3;
        private NumericUpDown numNumberOfNotes;
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
        private Button btnUpdateFormFromDesigner;
        private TextBox txtDesignerReport;
        private Label lblScoreReport;
        private TextBox txtScoreReport;
        private Button btnExportToNotion;
        private Label lblTupletNumber;
        private TextBox txtTupletNumber;
        private TextBox txtMovementTitle;
        private GroupBox groupBox2;
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
        private GroupBox groupBox3;
        private GroupBox grbCurrentDesign;
    }
}