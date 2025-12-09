using Music.MyMidi;

namespace Music.Writer
{
    /// <summary>
    /// Manages adding phrases to the phrase DataGridView.
    /// </summary>
    internal static class PhraseGridManager
    {
        // Constants for the four fixed rows at the top of the grid
        internal const int FIXED_ROW_TEMPO = 0;
        internal const int FIXED_ROW_TIME_SIGNATURE = 1;
        internal const int FIXED_ROW_KEY_SIGNATURE = 2;
        internal const int FIXED_ROW_HARMONY = 3;
        internal const int FIXED_ROWS_COUNT = 4;

        /// <summary>
        /// Configures the dgvPhrase DataGridView with proper columns including MIDI instrument dropdown.
        /// </summary>
        /// <param name="dgvPhrase">The DataGridView to configure</param>
        /// <param name="midiInstruments">List of available MIDI instruments</param>
        /// <param name="cellValueChangedHandler">Event handler for cell value changes</param>
        /// <param name="currentCellDirtyStateChangedHandler">Event handler for dirty state changes</param>
        internal static void ConfigurePhraseDataGridView(
            DataGridView dgvPhrase,
            List<MidiInstrument> midiInstruments,
            DataGridViewCellEventHandler cellValueChangedHandler,
            EventHandler currentCellDirtyStateChangedHandler)
        {
            dgvPhrase.AllowUserToAddRows = false;
            dgvPhrase.AllowUserToResizeColumns = true;
            dgvPhrase.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPhrase.MultiSelect = true;
            dgvPhrase.ReadOnly = false;
            dgvPhrase.Columns.Clear();

            // Column 0: Type column (read-only)
            var colType = new DataGridViewTextBoxColumn
            {
                Name = "colType",
                HeaderText = "Type",
                Width = 100,
                ReadOnly = true
            };
            dgvPhrase.Columns.Add(colType);

            // Column 1: Hidden column containing the data object
            var colData = new DataGridViewTextBoxColumn
            {
                Name = "colData",
                HeaderText = "Data",
                Visible = false,
                ReadOnly = true
            };
            dgvPhrase.Columns.Add(colData);

            // Column 2: MIDI Instrument dropdown (editable)
            var colInstrument = new DataGridViewComboBoxColumn
            {
                Name = "colInstrument",
                HeaderText = "Instrument",
                Width = 200,
                DataSource = new List<MidiInstrument>(midiInstruments),
                DisplayMember = "Name",
                ValueMember = "ProgramNumber",
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FlatStyle = FlatStyle.Flat,
                ReadOnly = false
            };
            dgvPhrase.Columns.Add(colInstrument);

            // Column 3: Stave
            var colStaff = new DataGridViewTextBoxColumn
            {
                Name = "colStave",
                HeaderText = "Stave",
                Width = 40,
                ReadOnly = false
            };
            dgvPhrase.Columns.Add(colStaff);

            // Column 4: Event number (read-only)
            var colEventNumber = new DataGridViewTextBoxColumn
            {
                Name = "colEventNumber",
                HeaderText = "#",
                Width = 50,
                ReadOnly = true
            };
            dgvPhrase.Columns.Add(colEventNumber);

            // Column 5: Description (read-only for now)
            var colDescription = new DataGridViewTextBoxColumn
            {
                Name = "colDescription",
                HeaderText = "Description",
                Width = 300,
                ReadOnly = true
            };
            dgvPhrase.Columns.Add(colDescription);

            // Column 6: Phrase details (fills remaining space)
            var colPhrase = new DataGridViewTextBoxColumn
            {
                Name = "colPhrase",
                HeaderText = "Phrase",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            };
            dgvPhrase.Columns.Add(colPhrase);

            // Wire up event handlers
            dgvPhrase.CellValueChanged += cellValueChangedHandler;
            dgvPhrase.CurrentCellDirtyStateChanged += currentCellDirtyStateChangedHandler;

            // Add the four fixed rows at the top
            InitializeFixedRows(dgvPhrase);
        }

        /// <summary>
        /// Initializes the four fixed rows at the top of the grid.
        /// </summary>
        private static void InitializeFixedRows(DataGridView dgvPhrase)
        {
            // Add four empty rows
            for (int i = 0; i < FIXED_ROWS_COUNT; i++)
            {
                dgvPhrase.Rows.Add();
            }

            // Set Type column values for the fixed rows
            dgvPhrase.Rows[FIXED_ROW_TEMPO].Cells["colType"].Value = "Tempo";
            dgvPhrase.Rows[FIXED_ROW_TIME_SIGNATURE].Cells["colType"].Value = "Time Signature";
            dgvPhrase.Rows[FIXED_ROW_KEY_SIGNATURE].Cells["colType"].Value = "Key Signature";
            dgvPhrase.Rows[FIXED_ROW_HARMONY].Cells["colType"].Value = "Harmony";
        }

        /// <summary>
        /// Handles the CurrentCellDirtyStateChanged event to commit combo box edits immediately.
        /// </summary>
        /// <param name="dgvPhrase">The DataGridView</param>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event args</param>
        internal static void HandleCurrentCellDirtyStateChanged(DataGridView dgvPhrase, object? sender, EventArgs e)
        {
            if (dgvPhrase.IsCurrentCellDirty && dgvPhrase.CurrentCell is DataGridViewComboBoxCell)
            {
                dgvPhrase.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        /// <summary>
        /// Handles the CellValueChanged event to update Phrase objects when instrument selection changes.
        /// </summary>
        /// <param name="dgvPhrase">The DataGridView</param>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event args</param>
        internal static void HandleCellValueChanged(DataGridView dgvPhrase, object? sender, DataGridViewCellEventArgs e)
        {
            // Skip fixed rows
            if (e.RowIndex < FIXED_ROWS_COUNT)
                return;

            // Only handle changes to the instrument column
            if (e.RowIndex < 0 || e.ColumnIndex != dgvPhrase.Columns["colInstrument"]?.Index)
                return;

            var row = dgvPhrase.Rows[e.RowIndex];
            var cellValue = row.Cells["colData"].Value;

            // Check if the hidden data cell contains a Phrase object
            if (cellValue is Phrase phrase)
            {
                // Get the selected instrument name
                var selectedInstrumentName = row.Cells["colInstrument"].FormattedValue?.ToString();

                if (!string.IsNullOrEmpty(selectedInstrumentName))
                {
                    // Update the Phrase object's MidiProgramName property
                    phrase.MidiProgramName = selectedInstrumentName;

                    // Optionally update the description column to reflect the change
                    row.Cells["colDescription"].Value = $"Part: {selectedInstrumentName}";
                }
            }
        }

        /// <summary>
        /// Adds a phrase to the phrase grid with instrument information from the phrase itself.
        /// </summary>
        /// <param name="phrase">The phrase to add</param>
        /// <param name="midiInstruments">List of available MIDI instruments</param>
        /// <param name="dgvPhrase">The DataGridView to add to</param>
        /// <param name="phraseNumber">Reference to the phrase counter (will be incremented)</param>
        internal static void AddPhraseToGrid(
            Phrase phrase,
            List<MidiInstrument> midiInstruments,
            DataGridView dgvPhrase,
            ref int phraseNumber)
        {
            // Set phrase name/number
            phraseNumber++;
            var phraseName = phraseNumber.ToString();

            // Get part name from the phrase
            var partName = phrase.MidiProgramName ?? "Select...";

            // Add new row
            int newRowIndex = dgvPhrase.Rows.Add();
            var row = dgvPhrase.Rows[newRowIndex];

            // Column 0: Type (Phrase for phrase rows)
            row.Cells["colType"].Value = "Phrase";

            // Column 1: Hidden data (Phrase object)
            row.Cells["colData"].Value = phrase;

            // Column 2: MIDI Instrument dropdown
            int programNumberToSet = -1;

            if (phrase.MidiProgramNumber <= 127 || phrase.MidiProgramNumber == 255)
            {
                programNumberToSet = phrase.MidiProgramNumber;
            }

            row.Cells["colInstrument"].Value = programNumberToSet;

            // Column 3: Stave - default to 1 for newly added rows
            row.Cells["colStave"].Value = 1;

            // Column 4: Event number
            row.Cells["colEventNumber"].Value = phraseName;

            // Column 5: Description
            if (!string.IsNullOrEmpty(partName) && partName != "Select...")
            {
                row.Cells["colDescription"].Value = $"Part: {partName}";
            }
            else
            {
                row.Cells["colDescription"].Value = string.Empty;
            }

            // Column 6: Phrase details
            row.Cells["colPhrase"].Value = phrase.PhraseNotes.Count > 0 
                ? $"{phrase.PhraseNotes.Count} note(s)" 
                : "Empty phrase";
        }
    }
}