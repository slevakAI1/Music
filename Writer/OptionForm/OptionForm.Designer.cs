namespace Music.Writer.OptionForm
{
    partial class OptionForm
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
            gbPartOptions = new GroupBox();
            lblPart = new Label();
            clbParts = new CheckedListBox();
            lblStaff = new Label();
            clbStaffs = new CheckedListBox();
            gbPitchOptions = new GroupBox();
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
            gbRhythmOptions = new GroupBox();
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
            btnOk = new Button();
            btnCancel = new Button();
            gbPartOptions.SuspendLayout();
            gbPitchOptions.SuspendLayout();
            grpChord.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numChordDegree).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numOctaveAbs).BeginInit();
            grpAbsolute.SuspendLayout();
            grpKeyRelative.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numDegree).BeginInit();
            gbRhythmOptions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numNumberOfNotes).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numDots).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numTupletCount).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numTupletOf).BeginInit();
            SuspendLayout();
            // 
            // gbPartOptions
            // 
            gbPartOptions.BackColor = Color.Black;
            gbPartOptions.Controls.Add(lblPart);
            gbPartOptions.Controls.Add(clbParts);
            gbPartOptions.Controls.Add(lblStaff);
            gbPartOptions.Controls.Add(clbStaffs);
            gbPartOptions.ForeColor = Color.FromArgb(255, 128, 0);
            gbPartOptions.Location = new Point(583, 13);
            gbPartOptions.Name = "gbPartOptions";
            gbPartOptions.Size = new Size(322, 174);
            gbPartOptions.TabIndex = 2;
            gbPartOptions.TabStop = false;
            gbPartOptions.Text = "Part";
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
            // gbPitchOptions
            // 
            gbPitchOptions.BackColor = Color.Black;
            gbPitchOptions.Controls.Add(grpChord);
            gbPitchOptions.Controls.Add(lblOctaveAbs);
            gbPitchOptions.Controls.Add(numOctaveAbs);
            gbPitchOptions.Controls.Add(rbChord);
            gbPitchOptions.Controls.Add(rbPitchAbsolute);
            gbPitchOptions.Controls.Add(rbPitchKeyRelative);
            gbPitchOptions.Controls.Add(grpAbsolute);
            gbPitchOptions.Controls.Add(grpKeyRelative);
            gbPitchOptions.ForeColor = Color.White;
            gbPitchOptions.Location = new Point(12, 192);
            gbPitchOptions.Name = "gbPitchOptions";
            gbPitchOptions.Size = new Size(541, 280);
            gbPitchOptions.TabIndex = 3;
            gbPitchOptions.TabStop = false;
            gbPitchOptions.Text = "Pitch Options";
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
            rbChord.ForeColor = Color.FromArgb(255, 128, 0);
            rbChord.Location = new Point(15, 147);
            rbChord.Name = "rbChord";
            rbChord.Size = new Size(58, 19);
            rbChord.TabIndex = 5;
            rbChord.Text = "Chord";
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
            // gbRhythmOptions
            // 
            gbRhythmOptions.BackColor = Color.Black;
            gbRhythmOptions.Controls.Add(txtTupletNumber);
            gbRhythmOptions.Controls.Add(lblTupletNumber);
            gbRhythmOptions.Controls.Add(label3);
            gbRhythmOptions.Controls.Add(numNumberOfNotes);
            gbRhythmOptions.Controls.Add(lblNoteValue);
            gbRhythmOptions.Controls.Add(cbNoteValue);
            gbRhythmOptions.Controls.Add(lblDots);
            gbRhythmOptions.Controls.Add(numDots);
            gbRhythmOptions.Controls.Add(lblTupletCount);
            gbRhythmOptions.Controls.Add(numTupletCount);
            gbRhythmOptions.Controls.Add(lblTupletOf);
            gbRhythmOptions.Controls.Add(numTupletOf);
            gbRhythmOptions.ForeColor = Color.White;
            gbRhythmOptions.Location = new Point(12, 12);
            gbRhythmOptions.Name = "gbRhythmOptions";
            gbRhythmOptions.Size = new Size(325, 174);
            gbRhythmOptions.TabIndex = 6;
            gbRhythmOptions.TabStop = false;
            gbRhythmOptions.Text = "Rhythm options";
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
            // btnOk
            // 
            btnOk.DialogResult = DialogResult.OK;
            btnOk.ForeColor = Color.FromArgb(0, 192, 0);
            btnOk.Location = new Point(652, 419);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(72, 23);
            btnOk.TabIndex = 39;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.ForeColor = Color.FromArgb(0, 192, 0);
            btnCancel.Location = new Point(758, 419);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(72, 23);
            btnCancel.TabIndex = 40;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // OptionForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(930, 485);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Controls.Add(gbRhythmOptions);
            Controls.Add(gbPartOptions);
            Controls.Add(gbPitchOptions);
            Name = "OptionForm";
            Text = "Options";
            gbPartOptions.ResumeLayout(false);
            gbPartOptions.PerformLayout();
            gbPitchOptions.ResumeLayout(false);
            gbPitchOptions.PerformLayout();
            grpChord.ResumeLayout(false);
            grpChord.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numChordDegree).EndInit();
            ((System.ComponentModel.ISupportInitialize)numOctaveAbs).EndInit();
            grpAbsolute.ResumeLayout(false);
            grpAbsolute.PerformLayout();
            grpKeyRelative.ResumeLayout(false);
            grpKeyRelative.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numDegree).EndInit();
            gbRhythmOptions.ResumeLayout(false);
            gbRhythmOptions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numNumberOfNotes).EndInit();
            ((System.ComponentModel.ISupportInitialize)numDots).EndInit();
            ((System.ComponentModel.ISupportInitialize)numTupletCount).EndInit();
            ((System.ComponentModel.ISupportInitialize)numTupletOf).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox gbPartOptions;
        private CheckedListBox clbParts;
        private Label lblPart;
        private Label lblStaff;
        private CheckedListBox clbStaffs;

        private GroupBox gbPitchOptions;
        private RadioButton rbPitchAbsolute;
        private RadioButton rbPitchKeyRelative;

        private GroupBox grpAbsolute;
        private Label lblStep;
        private ComboBox cbStep;
        private Label lblAccidental;
        private ComboBox cbAccidental;
        private Label lblOctaveAbs;
        private NumericUpDown numOctaveAbs;

        private GroupBox grpKeyRelative;
        private Label lblDegree;
        private NumericUpDown numDegree;
        private Label lblOctaveKR;

        private GroupBox gbRhythmOptions;
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
        private Label lblTupletNumber;
        private TextBox txtTupletNumber;
        private Button btnOk;
        private Button btnCancel;
    }
}