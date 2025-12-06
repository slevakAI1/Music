namespace Music.Writer
{
    partial class WriterForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ComboBox cbCommand;

        private System.Windows.Forms.GroupBox grbParts;
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

        private System.Windows.Forms.Button btnAppend;

        // ScoreList management controls
        private System.Windows.Forms.GroupBox grpScoreList;

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
            grbParts = new GroupBox();
            lblPart = new Label();
            clbParts = new CheckedListBox();
            lblStaff = new Label();
            clbStaffs = new CheckedListBox();
            btnAppend = new Button();
            grpPitch = new GroupBox();
            grpChord = new GroupBox();
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
            btnSetDesignTestScenarioD1 = new Button();
            btnChordTest = new Button();
            btnUpdateFormFromDesigner = new Button();
            txtDesignerReport = new TextBox();
            lblScoreReport = new Label();
            txtScoreReport = new TextBox();
            btnExportToNotion = new Button();
            grpScoreList = new GroupBox();
            txtMovementTitle = new TextBox();
            btnNewScore = new Button();
            groupBox3 = new GroupBox();
            btnExecute = new Button();
            grbCurrentDesign = new GroupBox();
            groupBox4 = new GroupBox();
            btnExport = new Button();
            btnImport = new Button();
            btnAddPhrase = new Button();
            btnDeletePhrases = new Button();
            btnClearPhrases = new Button();
            btnPlayPhrases = new Button();
            dgvPhrase = new DataGridView();
            grbParts.SuspendLayout();
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
            groupBox3.SuspendLayout();
            grbCurrentDesign.SuspendLayout();
            groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvPhrase).BeginInit();
            SuspendLayout();
            // 
            // cbCommand
            // 
            cbCommand.DropDownStyle = ComboBoxStyle.DropDownList;
            cbCommand.Items.AddRange(new object[] { "Repeat Note" });
            cbCommand.Location = new Point(13, 22);
            cbCommand.Name = "cbCommand";
            cbCommand.Size = new Size(250, 23);
            cbCommand.TabIndex = 1;
            // 
            // grbParts
            // 
            grbParts.BackColor = Color.Black;
            grbParts.Controls.Add(lblPart);
            grbParts.Controls.Add(clbParts);
            grbParts.Controls.Add(lblStaff);
            grbParts.Controls.Add(clbStaffs);
            grbParts.ForeColor = Color.FromArgb(255, 128, 0);
            grbParts.Location = new Point(1580, 751);
            grbParts.Name = "grbParts";
            grbParts.Size = new Size(322, 174);
            grbParts.TabIndex = 2;
            grbParts.TabStop = false;
            grbParts.Text = "Part";
            // 
            // lblPart
            // 
            lblPart.AutoSize = true;
            lblPart.ForeColor = Color.FromArgb(255, 128, 0);
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
            lblStaff.ForeColor = Color.FromArgb(255, 128, 0);
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
            // btnAppend
            // 
            btnAppend.ForeColor = Color.Red;
            btnAppend.Location = new Point(93, 237);
            btnAppend.Name = "btnAppend";
            btnAppend.Size = new Size(148, 22);
            btnAppend.TabIndex = 4;
            btnAppend.Text = "Old Append to Score?";
            btnAppend.UseVisualStyleBackColor = true;
            btnAppend.Click += btnAppendNotes_Click;
            // 
            // grpPitch
            // 
            grpPitch.BackColor = Color.Black;
            grpPitch.Controls.Add(grpChord);
            grpPitch.Controls.Add(lblOctaveAbs);
            grpPitch.Controls.Add(numOctaveAbs);
            grpPitch.Controls.Add(rbChord);
            grpPitch.Controls.Add(rbIsRest);
            grpPitch.Controls.Add(rbPitchAbsolute);
            grpPitch.Controls.Add(rbPitchKeyRelative);
            grpPitch.Controls.Add(grpAbsolute);
            grpPitch.Controls.Add(grpKeyRelative);
            grpPitch.ForeColor = Color.White;
            grpPitch.Location = new Point(12, 689);
            grpPitch.Name = "grpPitch";
            grpPitch.Size = new Size(541, 280);
            grpPitch.TabIndex = 3;
            grpPitch.TabStop = false;
            grpPitch.Text = "Pitch Options";
            // 
            // grpChord
            // 
            grpChord.Controls.Add(lblKey);
            grpChord.Controls.Add(cbChordKey);
            grpChord.Controls.Add(lblDegree2);
            grpChord.Controls.Add(numChordDegree);
            grpChord.Controls.Add(lblQuality);
            grpChord.Controls.Add(cbChordQuality);
            grpChord.Controls.Add(label8);
            grpChord.Controls.Add(cbChordBase);
            grpChord.Location = new Point(6, 165);
            grpChord.Name = "grpChord";
            grpChord.Size = new Size(299, 97);
            grpChord.TabIndex = 11;
            grpChord.TabStop = false;
            // 
            // lblKey
            // 
            lblKey.AutoSize = true;
            lblKey.ForeColor = Color.FromArgb(0, 192, 0);
            lblKey.Location = new Point(8, 24);
            lblKey.Name = "lblKey";
            lblKey.Size = new Size(26, 15);
            lblKey.TabIndex = 6;
            lblKey.Text = "Key";
            // 
            // cbChordKey
            // 
            cbChordKey.DropDownStyle = ComboBoxStyle.DropDownList;
            cbChordKey.Items.AddRange(new object[] { "C major", "G major", "D major", "A major", "E major", "B major", "F# major", "C# major", "F major", "Bb majo", "Eb major", "Ab major", "Db major", "Gb major", "Cb major", "A minor", "E minor", "B minor", "F# minor", "C# minor", "G# minor", "D# minor", "A# minor", "D minor", "G minor", "C minor ", "F minor ", "Bb minor", "Eb minor", "Ab minor" });
            cbChordKey.Location = new Point(36, 21);
            cbChordKey.Name = "cbChordKey";
            cbChordKey.Size = new Size(70, 23);
            cbChordKey.TabIndex = 11;
            // 
            // lblDegree2
            // 
            lblDegree2.AutoSize = true;
            lblDegree2.ForeColor = Color.FromArgb(0, 192, 0);
            lblDegree2.Location = new Point(109, 24);
            lblDegree2.Name = "lblDegree2";
            lblDegree2.Size = new Size(44, 15);
            lblDegree2.TabIndex = 7;
            lblDegree2.Text = "Degree";
            // 
            // numChordDegree
            // 
            numChordDegree.Location = new Point(155, 21);
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
            lblQuality.Location = new Point(7, 62);
            lblQuality.Name = "lblQuality";
            lblQuality.Size = new Size(45, 15);
            lblQuality.TabIndex = 8;
            lblQuality.Text = "Quality";
            // 
            // cbChordQuality
            // 
            cbChordQuality.DropDownStyle = ComboBoxStyle.DropDownList;
            cbChordQuality.Items.AddRange(new object[] { "Major", "Minor", "Diminished", "Augmented", "Sus2", "Sus4", "Power5", "Major6", "Minor6", "Major6Add9", "Dominant7", "Major7", "Minor7", "Diminished7", "HalfDiminished7", "MinorMajor7", "Dominant9", "Major9", "Minor9", "Dominant11", "Dominant13", "MajorAdd9", "MajorAdd11" });
            cbChordQuality.Location = new Point(52, 59);
            cbChordQuality.Name = "cbChordQuality";
            cbChordQuality.Size = new Size(158, 23);
            cbChordQuality.TabIndex = 13;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.ForeColor = Color.FromArgb(0, 192, 0);
            label8.Location = new Point(214, 62);
            label8.Name = "label8";
            label8.Size = new Size(31, 15);
            label8.TabIndex = 10;
            label8.Text = "Base";
            // 
            // cbChordBase
            // 
            cbChordBase.DropDownStyle = ComboBoxStyle.DropDownList;
            cbChordBase.Items.AddRange(new object[] { "root", "3rd", "5th", "7th" });
            cbChordBase.Location = new Point(247, 59);
            cbChordBase.Name = "cbChordBase";
            cbChordBase.Size = new Size(46, 23);
            cbChordBase.TabIndex = 14;
            // 
            // lblOctaveAbs
            // 
            lblOctaveAbs.AutoSize = true;
            lblOctaveAbs.ForeColor = Color.FromArgb(0, 192, 0);
            lblOctaveAbs.Location = new Point(12, 51);
            lblOctaveAbs.Name = "lblOctaveAbs";
            lblOctaveAbs.Size = new Size(47, 15);
            lblOctaveAbs.TabIndex = 4;
            lblOctaveAbs.Text = "Octave:";
            // 
            // numOctaveAbs
            // 
            numOctaveAbs.Location = new Point(64, 47);
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
            rbChord.Location = new Point(15, 147);
            rbChord.Name = "rbChord";
            rbChord.Size = new Size(58, 19);
            rbChord.TabIndex = 5;
            rbChord.Text = "Chord";
            // 
            // rbIsRest
            // 
            rbIsRest.AutoSize = true;
            rbIsRest.ForeColor = Color.FromArgb(0, 192, 0);
            rbIsRest.Location = new Point(14, 21);
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
            rbPitchAbsolute.Location = new Point(14, 80);
            rbPitchAbsolute.Name = "rbPitchAbsolute";
            rbPitchAbsolute.Size = new Size(72, 19);
            rbPitchAbsolute.TabIndex = 0;
            rbPitchAbsolute.TabStop = true;
            rbPitchAbsolute.Text = "Absolute";
            // 
            // rbPitchKeyRelative
            // 
            rbPitchKeyRelative.AutoSize = true;
            rbPitchKeyRelative.Location = new Point(28, 312);
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
            grpAbsolute.Location = new Point(5, 95);
            grpAbsolute.Name = "grpAbsolute";
            grpAbsolute.Size = new Size(288, 46);
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
            lblAccidental.Location = new Point(118, 20);
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
            grpKeyRelative.Location = new Point(20, 326);
            grpKeyRelative.Name = "grpKeyRelative";
            grpKeyRelative.Size = new Size(198, 47);
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
            // groupBox1
            // 
            groupBox1.BackColor = Color.Black;
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
            groupBox1.ForeColor = Color.White;
            groupBox1.Location = new Point(12, 502);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(325, 174);
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
            btnSetWriterTestScenarioG1.ForeColor = Color.FromArgb(0, 192, 0);
            btnSetWriterTestScenarioG1.Location = new Point(478, 469);
            btnSetWriterTestScenarioG1.Name = "btnSetWriterTestScenarioG1";
            btnSetWriterTestScenarioG1.Size = new Size(196, 23);
            btnSetWriterTestScenarioG1.TabIndex = 8;
            btnSetWriterTestScenarioG1.Text = "Set Writer - Test Scenario G1";
            btnSetWriterTestScenarioG1.UseVisualStyleBackColor = true;
            btnSetWriterTestScenarioG1.Click += btnSetWriterTestScenarioG1_Click;
            // 
            // btnSetDesignTestScenarioD1
            // 
            btnSetDesignTestScenarioD1.ForeColor = Color.FromArgb(0, 192, 0);
            btnSetDesignTestScenarioD1.Location = new Point(478, 440);
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
            btnChordTest.Location = new Point(1706, 719);
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
            btnUpdateFormFromDesigner.Location = new Point(1706, 690);
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
            txtDesignerReport.Size = new Size(325, 109);
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
            txtScoreReport.Location = new Point(16, 26);
            txtScoreReport.Multiline = true;
            txtScoreReport.Name = "txtScoreReport";
            txtScoreReport.ScrollBars = ScrollBars.Vertical;
            txtScoreReport.Size = new Size(324, 200);
            txtScoreReport.TabIndex = 28;
            // 
            // btnExportToNotion
            // 
            btnExportToNotion.ForeColor = Color.FromArgb(0, 192, 0);
            btnExportToNotion.Location = new Point(1706, 661);
            btnExportToNotion.Name = "btnExportToNotion";
            btnExportToNotion.Size = new Size(114, 23);
            btnExportToNotion.TabIndex = 30;
            btnExportToNotion.Text = "Export to Notion";
            btnExportToNotion.UseVisualStyleBackColor = true;
            btnExportToNotion.Click += btnExportToNotion_Click;
            // 
            // grpScoreList
            // 
            grpScoreList.BackColor = Color.Black;
            grpScoreList.Controls.Add(txtMovementTitle);
            grpScoreList.Controls.Add(btnNewScore);
            grpScoreList.Controls.Add(btnAppend);
            grpScoreList.Controls.Add(lblScoreReport);
            grpScoreList.Controls.Add(txtScoreReport);
            grpScoreList.ForeColor = Color.White;
            grpScoreList.Location = new Point(630, 603);
            grpScoreList.Name = "grpScoreList";
            grpScoreList.Size = new Size(610, 328);
            grpScoreList.TabIndex = 31;
            grpScoreList.TabStop = false;
            grpScoreList.Text = "Current Score";
            // 
            // txtMovementTitle
            // 
            txtMovementTitle.ForeColor = Color.FromArgb(0, 192, 0);
            txtMovementTitle.Location = new Point(260, 271);
            txtMovementTitle.Name = "txtMovementTitle";
            txtMovementTitle.PlaceholderText = "Movement Title";
            txtMovementTitle.Size = new Size(250, 23);
            txtMovementTitle.TabIndex = 31;
            // 
            // btnNewScore
            // 
            btnNewScore.ForeColor = Color.Red;
            btnNewScore.Location = new Point(93, 272);
            btnNewScore.Name = "btnNewScore";
            btnNewScore.Size = new Size(148, 22);
            btnNewScore.TabIndex = 30;
            btnNewScore.Text = "New Score";
            btnNewScore.UseVisualStyleBackColor = true;
            btnNewScore.Click += btnNewScore_Click;
            // 
            // groupBox3
            // 
            groupBox3.BackColor = SystemColors.ActiveCaptionText;
            groupBox3.Controls.Add(btnExecute);
            groupBox3.Controls.Add(cbCommand);
            groupBox3.ForeColor = Color.White;
            groupBox3.Location = new Point(12, 431);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(416, 59);
            groupBox3.TabIndex = 33;
            groupBox3.TabStop = false;
            groupBox3.Text = "Command";
            // 
            // btnExecute
            // 
            btnExecute.ForeColor = Color.FromArgb(255, 128, 0);
            btnExecute.Location = new Point(284, 21);
            btnExecute.Name = "btnExecute";
            btnExecute.Size = new Size(114, 23);
            btnExecute.TabIndex = 31;
            btnExecute.Text = "Execute";
            btnExecute.UseVisualStyleBackColor = true;
            btnExecute.Click += btnExecute_Click;
            // 
            // grbCurrentDesign
            // 
            grbCurrentDesign.BackColor = Color.Black;
            grbCurrentDesign.Controls.Add(txtDesignerReport);
            grbCurrentDesign.ForeColor = Color.White;
            grbCurrentDesign.Location = new Point(703, 430);
            grbCurrentDesign.Name = "grbCurrentDesign";
            grbCurrentDesign.Size = new Size(351, 167);
            grbCurrentDesign.TabIndex = 34;
            grbCurrentDesign.TabStop = false;
            grbCurrentDesign.Text = "Current Design";
            // 
            // groupBox4
            // 
            groupBox4.BackColor = Color.Black;
            groupBox4.Controls.Add(btnExport);
            groupBox4.Controls.Add(btnImport);
            groupBox4.Controls.Add(btnAddPhrase);
            groupBox4.Controls.Add(btnDeletePhrases);
            groupBox4.Controls.Add(btnClearPhrases);
            groupBox4.Controls.Add(btnPlayPhrases);
            groupBox4.Controls.Add(dgvPhrase);
            groupBox4.ForeColor = Color.White;
            groupBox4.Location = new Point(12, 12);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new Size(1582, 406);
            groupBox4.TabIndex = 36;
            groupBox4.TabStop = false;
            groupBox4.Text = "Phrases";
            // 
            // btnExport
            // 
            btnExport.ForeColor = Color.FromArgb(0, 192, 0);
            btnExport.Location = new Point(367, 363);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(164, 23);
            btnExport.TabIndex = 42;
            btnExport.Text = "Export - New Untested";
            btnExport.UseVisualStyleBackColor = true;
            btnExport.Click += btnExport_Click;
            // 
            // btnImport
            // 
            btnImport.ForeColor = Color.FromArgb(255, 128, 0);
            btnImport.Location = new Point(725, 365);
            btnImport.Name = "btnImport";
            btnImport.Size = new Size(147, 23);
            btnImport.TabIndex = 41;
            btnImport.Text = "Import - Malfunctioning";
            btnImport.UseVisualStyleBackColor = true;
            btnImport.Click += btnImport_Click;
            // 
            // btnAddPhrase
            // 
            btnAddPhrase.ForeColor = Color.FromArgb(0, 192, 0);
            btnAddPhrase.Location = new Point(15, 365);
            btnAddPhrase.Name = "btnAddPhrase";
            btnAddPhrase.Size = new Size(72, 23);
            btnAddPhrase.TabIndex = 40;
            btnAddPhrase.Text = "Add";
            btnAddPhrase.UseVisualStyleBackColor = true;
            btnAddPhrase.Click += btnAddPhrase_Click;
            // 
            // btnDeletePhrases
            // 
            btnDeletePhrases.ForeColor = Color.FromArgb(0, 192, 0);
            btnDeletePhrases.Location = new Point(281, 363);
            btnDeletePhrases.Name = "btnDeletePhrases";
            btnDeletePhrases.Size = new Size(72, 23);
            btnDeletePhrases.TabIndex = 39;
            btnDeletePhrases.Text = "Delete";
            btnDeletePhrases.UseVisualStyleBackColor = true;
            btnDeletePhrases.Click += btnDeletePhrases_Click;
            // 
            // btnClearPhrases
            // 
            btnClearPhrases.ForeColor = Color.FromArgb(0, 192, 0);
            btnClearPhrases.Location = new Point(193, 363);
            btnClearPhrases.Name = "btnClearPhrases";
            btnClearPhrases.Size = new Size(72, 23);
            btnClearPhrases.TabIndex = 38;
            btnClearPhrases.Text = "Clear";
            btnClearPhrases.UseVisualStyleBackColor = true;
            btnClearPhrases.Click += btnClearPhrases_Click;
            // 
            // btnPlayPhrases
            // 
            btnPlayPhrases.ForeColor = Color.FromArgb(0, 192, 0);
            btnPlayPhrases.Location = new Point(103, 365);
            btnPlayPhrases.Name = "btnPlayPhrases";
            btnPlayPhrases.Size = new Size(72, 23);
            btnPlayPhrases.TabIndex = 32;
            btnPlayPhrases.Text = "Play";
            btnPlayPhrases.UseVisualStyleBackColor = true;
            btnPlayPhrases.Click += btnPlay_Click;
            // 
            // dgvPhrase
            // 
            dgvPhrase.AllowUserToAddRows = false;
            dgvPhrase.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvPhrase.Location = new Point(15, 19);
            dgvPhrase.Name = "dgvPhrase";
            dgvPhrase.ReadOnly = true;
            dgvPhrase.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPhrase.Size = new Size(1532, 334);
            dgvPhrase.TabIndex = 37;
            dgvPhrase.CellDoubleClick += DgvPhrase_CellDoubleClick;
            // 
            // WriterForm
            // 
            BackColor = Color.White;
            ClientSize = new Size(1938, 991);
            Controls.Add(grbCurrentDesign);
            Controls.Add(btnSetDesignTestScenarioD1);
            Controls.Add(btnExportToNotion);
            Controls.Add(grpScoreList);
            Controls.Add(btnSetWriterTestScenarioG1);
            Controls.Add(btnUpdateFormFromDesigner);
            Controls.Add(btnChordTest);
            Controls.Add(groupBox1);
            Controls.Add(grbParts);
            Controls.Add(grpPitch);
            Controls.Add(groupBox3);
            Controls.Add(groupBox4);
            Name = "WriterForm";
            Text = "Music Writer";
            grbParts.ResumeLayout(false);
            grbParts.PerformLayout();
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
            groupBox3.ResumeLayout(false);
            grbCurrentDesign.ResumeLayout(false);
            grbCurrentDesign.PerformLayout();
            groupBox4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvPhrase).EndInit();
            ResumeLayout(false);
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
        private Button btnUpdateFormFromDesigner;
        private TextBox txtDesignerReport;
        private Label lblScoreReport;
        private TextBox txtScoreReport;
        private Button btnExportToNotion;
        private Label lblTupletNumber;
        private TextBox txtTupletNumber;
        private GroupBox groupBox3;
        private GroupBox grbCurrentDesign;
        private GroupBox groupBox4;
        private Button btnExecute;
        private DataGridView dgvPhrase;
        private Button btnPlayPhrases;
        private Button btnDeletePhrases;
        private Button btnClearPhrases;
        private Button btnAddPhrase;
        private Button btnNewScore;
        private TextBox txtMovementTitle;
        private Button btnImport;
        private Button btnExport;
    }
}