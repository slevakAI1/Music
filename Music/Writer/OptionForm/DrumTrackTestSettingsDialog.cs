// AI: purpose=Simple input dialog for drum track test generation (seed only).
// AI: invariants=Seed defaults to random value.
// AI: deps=None; standalone modal dialog.

namespace Music.Writer.OptionForm
{
    // AI: Modal dialog for drum track test parameter input; returns OK/Cancel with seed value.
    public sealed class DrumTrackTestSettingsDialog : Form
    {
        private NumericUpDown numSeed;
        private Button btnOK;
        private Button btnCancel;
        private Button btnRandomSeed;
        private Label lblSeed;
        private Label lblSeedDisplay;

        public int Seed { get; private set; }

        public DrumTrackTestSettingsDialog()
        {
            InitializeDialog();
            SetDefaultValues();
        }

        private void InitializeDialog()
        {
            Text = "Drum Track Test Settings";
            Size = new Size(400, 180);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            lblSeed = new Label
            {
                Text = "Seed:",
                Location = new Point(20, 20),
                Size = new Size(80, 23),
                TextAlign = ContentAlignment.MiddleRight
            };

            numSeed = new NumericUpDown
            {
                Location = new Point(110, 20),
                Size = new Size(150, 23),
                Minimum = int.MinValue,
                Maximum = int.MaxValue,
                Value = 0
            };
            numSeed.ValueChanged += (s, e) => UpdateSeedDisplay();

            btnRandomSeed = new Button
            {
                Text = "Random",
                Location = new Point(270, 19),
                Size = new Size(90, 25)
            };
            btnRandomSeed.Click += BtnRandomSeed_Click;

            lblSeedDisplay = new Label
            {
                Location = new Point(110, 48),
                Size = new Size(250, 20),
                ForeColor = Color.Gray,
                Text = ""
            };

            btnOK = new Button
            {
                Text = "OK",
                Location = new Point(110, 90),
                Size = new Size(90, 30),
                DialogResult = DialogResult.OK
            };
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(210, 90),
                Size = new Size(90, 30),
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(lblSeed);
            Controls.Add(numSeed);
            Controls.Add(btnRandomSeed);
            Controls.Add(lblSeedDisplay);
            Controls.Add(btnOK);
            Controls.Add(btnCancel);

            AcceptButton = btnOK;
            CancelButton = btnCancel;
        }

        private void SetDefaultValues()
        {
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
        }
    }
}
