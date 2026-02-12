// AI: purpose=Simple input dialog for groove preview parameters (seed, genre, bars, repeat, operators).
// AI: invariants=Seed defaults to random; genre defaults to PopRock; bars defaults to 2; repeat defaults to 2; operators defaults to 2.
// AI: deps=Uses GrooveAnchorFactory.GetAvailableGenres for genre dropdown.

using Music.Generator.Groove;
using Music.Properties;

namespace Music.Writer.OptionForm
{
    // AI: Modal dialog for groove preview parameter input; returns OK/Cancel with captured values.
    public sealed class CreateDrumPhraseSettingsDialog : Form
    {
        private NumericUpDown numSeed;
        private ComboBox cbGenre;
        private NumericUpDown numBars;
        private NumericUpDown numRepeat;
        private NumericUpDown numOperators;
        private Button btnOK;
        private Button btnCancel;
        private Button btnRandomSeed;
        private Label lblSeed;
        private Label lblGenre;
        private Label lblBars;
        private Label lblRepeat;
        private Label lblOperators;
        private Label lblSeedDisplay;

        public int Seed { get; private set; }
        public string Genre { get; private set; }
        public int Bars { get; private set; }
        public int Repeat { get; private set; }
        public int NumberOfOperators { get; private set; }

        public CreateDrumPhraseSettingsDialog()
        {
            InitializeDialog();
            SetDefaultValues();
        }

        private void InitializeDialog()
        {
            // Form properties
            Text = "Drum Phrase Settings";
            Size = new Size(400, 330);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            // Seed label
            lblSeed = new Label
            {
                Text = "Seed:",
                Location = new Point(20, 20),
                Size = new Size(80, 23),
                TextAlign = ContentAlignment.MiddleRight
            };

            // Seed numeric input
            numSeed = new NumericUpDown
            {
                Location = new Point(110, 20),
                Size = new Size(150, 23),
                Minimum = int.MinValue,
                Maximum = int.MaxValue,
                Value = 0
            };
            numSeed.ValueChanged += (s, e) => UpdateSeedDisplay();

            // Random seed button
            btnRandomSeed = new Button
            {
                Text = "Random",
                Location = new Point(270, 19),
                Size = new Size(90, 25)
            };
            btnRandomSeed.Click += BtnRandomSeed_Click;

            // Seed display label (shows the actual seed value)
            lblSeedDisplay = new Label
            {
                Location = new Point(110, 48),
                Size = new Size(250, 20),
                ForeColor = Color.Gray,
                Text = ""
            };

            // Genre label
            lblGenre = new Label
            {
                Text = "Genre:",
                Location = new Point(20, 75),
                Size = new Size(80, 23),
                TextAlign = ContentAlignment.MiddleRight
            };

            // Genre dropdown
            cbGenre = new ComboBox
            {
                Location = new Point(110, 75),
                Size = new Size(150, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Populate genres
            var genres = GrooveAnchorFactory.GetAvailableGenres();
            foreach (var genre in genres)
            {
                cbGenre.Items.Add(genre);
            }
            cbGenre.SelectedIndex = 0; // Default to first (PopRock)

            // Bars label
            lblBars = new Label
            {
                Text = "Bars:",
                Location = new Point(20, 110),
                Size = new Size(80, 23),
                TextAlign = ContentAlignment.MiddleRight
            };

            // Bars numeric input
            numBars = new NumericUpDown
            {
                Location = new Point(110, 110),
                Size = new Size(150, 23),
                Minimum = 1,
                Maximum = 100,
                Value = 2
            };

            // Repeat label
            lblRepeat = new Label
            {
                Text = Resources.ResourceManager.GetString("PhraseTest_RepeatLabel") ?? "Repeat:",
                Location = new Point(20, 145),
                Size = new Size(80, 23),
                TextAlign = ContentAlignment.MiddleRight
            };

            // Repeat numeric input
            numRepeat = new NumericUpDown
            {
                Location = new Point(110, 145),
                Size = new Size(150, 23),
                Minimum = 1,
                Maximum = 10,
                Value = 2
            };

            // Operators label
            lblOperators = new Label
            {
                Text = Resources.ResourceManager.GetString("PhraseTest_OperatorsLabel") ?? "Operators:",
                Location = new Point(20, 180),
                Size = new Size(80, 23),
                TextAlign = ContentAlignment.MiddleRight
            };

            // Operators numeric input
            numOperators = new NumericUpDown
            {
                Location = new Point(110, 180),
                Size = new Size(150, 23),
                Minimum = 0,
                Maximum = 100,
                Value = 2
            };

            // OK button
            btnOK = new Button
            {
                Text = "OK",
                Location = new Point(110, 225),
                Size = new Size(90, 30),
                DialogResult = DialogResult.OK
            };
            btnOK.Click += BtnOK_Click;

            // Cancel button
            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(210, 225),
                Size = new Size(90, 30),
                DialogResult = DialogResult.Cancel
            };

            // Add controls
            Controls.Add(lblSeed);
            Controls.Add(numSeed);
            Controls.Add(btnRandomSeed);
            Controls.Add(lblSeedDisplay);
            Controls.Add(lblGenre);
            Controls.Add(cbGenre);
            Controls.Add(lblBars);
            Controls.Add(numBars);
            Controls.Add(lblRepeat);
            Controls.Add(numRepeat);
            Controls.Add(lblOperators);
            Controls.Add(numOperators);
            Controls.Add(btnOK);
            Controls.Add(btnCancel);

            AcceptButton = btnOK;
            CancelButton = btnCancel;
        }

        private void SetDefaultValues()
        {
            // Start with random seed
            GenerateRandomSeed();
        }

        private void GenerateRandomSeed()
        {
            int randomSeed = Random.Shared.Next(int.MinValue, int.MaxValue);
            numSeed.Value = randomSeed;
            UpdateSeedDisplay();
        }

        private void UpdateSeedDisplay()
        {
            lblSeedDisplay.Text = $"Current seed: {(int)numSeed.Value}";
        }

        private void BtnRandomSeed_Click(object? sender, EventArgs e)
        {
            GenerateRandomSeed();
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            Seed = (int)numSeed.Value;
            Genre = cbGenre.SelectedItem?.ToString() ?? "PopRock";
            Bars = (int)numBars.Value;
            Repeat = (int)numRepeat.Value;
            NumberOfOperators = (int)numOperators.Value;
        }
    }
}
