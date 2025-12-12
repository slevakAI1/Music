using Music.MyMidi;
using Music.Designer;

namespace Music.Writer
{
    /// <summary>
    /// Manages adding phrases to the phrase DataGridView.
    /// </summary>
    internal static class PhraseGridManager
    {
        // Constants for the four fixed rows at the top of the grid
        internal const int FIXED_ROW_SECTION = 0;
        internal const int FIXED_ROW_HARMONY = 1;
        internal const int FIXED_ROW_TIME_SIGNATURE = 2;
        internal const int FIXED_ROW_TEMPO = 3;
        internal const int FIXED_ROWS_COUNT = 4;

        // Index where measure columns begin (reduced by 1 due to column consolidation)
        internal const int MEASURE_START_COLUMN_INDEX = 5;

        // Default number of measure columns to create initially
        private const int DEFAULT_MEASURE_COLUMNS = 32;

        /// <summary>
        /// Configures the dgvPhrase DataGridView with proper columns including MIDI instrument dropdown.
        /// </summary>
        /// <param name="dgvPhrase">The DataGridView to configure</param>
        /// <param name="midiInstruments">List of available MIDI instruments</param>
        /// <param name="cellValueChangedHandler">Event handler for cell value changes</param>
        /// <param name="currentCellDirtyStateChangedHandler">Event handler for dirty state changes</param>
        /// <param name="tempoTimeline">Optional TempoTimeline to place into the fixed Tempo row hidden data cell</param>
        internal static void ConfigurePhraseDataGridView(
            DataGridView dgvPhrase,
            List<MidiInstrument> midiInstruments,
            DataGridViewCellEventHandler cellValueChangedHandler,
            EventHandler currentCellDirtyStateChangedHandler,
            Designer.Designer? designer = null)
        {
            dgvPhrase.AllowUserToAddRows = false;
            dgvPhrase.AllowUserToResizeColumns = true;
            dgvPhrase.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPhrase.MultiSelect = true;
            dgvPhrase.ReadOnly = false;
            dgvPhrase.Columns.Clear();

            // Column 0: Type column - text for fixed rows, combo box for phrase rows
            var colType = new DataGridViewTextBoxColumn
            {
                Name = "colType",
                HeaderText = "Type",
                Width = 200,
                ReadOnly = false // Will be set per-cell basis in InitializeFixedRows
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

            // Column 2: Stave
            var colStaff = new DataGridViewTextBoxColumn
            {
                Name = "colStave",
                HeaderText = "Stave",
                Width = 40,
                ReadOnly = false
            };
            dgvPhrase.Columns.Add(colStaff);

            // Column 3: Event number (read-only)
            var colEventNumber = new DataGridViewTextBoxColumn
            {
                Name = "colEventNumber",
                HeaderText = "#",
                Width = 50,
                ReadOnly = true
            };
            dgvPhrase.Columns.Add(colEventNumber);

            // Column 4: Description (read-only for now)
            var colDescription = new DataGridViewTextBoxColumn
            {
                Name = "colDescription",
                HeaderText = "Description",
                Width = 300,
                ReadOnly = true
            };
            dgvPhrase.Columns.Add(colDescription);

            // Columns 5+: Measure columns (dynamically created)
            // Create initial set of measure columns
            for (int i = 0; i < DEFAULT_MEASURE_COLUMNS; i++)
            {
                var colMeasure = new DataGridViewTextBoxColumn
                {
                    Name = $"colMeasure{i + 1}",
                    HeaderText = $"{i + 1}",
                    Width = 40,
                    ReadOnly = true,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleCenter
                    }
                };
                dgvPhrase.Columns.Add(colMeasure);
            }

            // Wire up event handlers
            dgvPhrase.CellValueChanged += cellValueChangedHandler;
            dgvPhrase.CurrentCellDirtyStateChanged += currentCellDirtyStateChangedHandler;

            // Add the four fixed rows at the top and optionally attach tempoTimeline to the hidden data cell
            InitializeFixedRows(dgvPhrase, midiInstruments, designer);
        }

        /// <summary>
        /// Initializes the four fixed rows at the top of the grid.
        /// Entire fixed rows are set to ReadOnly so users cannot edit them.
        /// If a TempoTimeline is provided it will be stored in the fixed Tempo row's hidden data cell.
        /// </summary>
        private static void InitializeFixedRows(
            DataGridView dgvPhrase,
            List<MidiInstrument> midiInstruments,
            Designer.Designer? designer = null)
        {
            // Add four empty rows
            for (int i = 0; i < FIXED_ROWS_COUNT; i++)
            {
                dgvPhrase.Rows.Add();
            }

            // Set Type column values for the fixed rows and mark each cell read-only
            dgvPhrase.Rows[FIXED_ROW_SECTION].Cells["colType"].Value = "Section";
            dgvPhrase.Rows[FIXED_ROW_SECTION].Cells["colType"].ReadOnly = true;

            dgvPhrase.Rows[FIXED_ROW_TIME_SIGNATURE].Cells["colType"].Value = "Time Signature";
            dgvPhrase.Rows[FIXED_ROW_TIME_SIGNATURE].Cells["colType"].ReadOnly = true;

            dgvPhrase.Rows[FIXED_ROW_HARMONY].Cells["colType"].Value = "Harmony";
            dgvPhrase.Rows[FIXED_ROW_HARMONY].Cells["colType"].ReadOnly = true;

            dgvPhrase.Rows[FIXED_ROW_TEMPO].Cells["colType"].Value = "Tempo";
            dgvPhrase.Rows[FIXED_ROW_TEMPO].Cells["colType"].ReadOnly = true;

            // Delegate attaching the TempoTimeline to the new manager class
            if (designer != null)
            {
                GridControlLinesManager.AttachTempoTimeline(dgvPhrase, designer.TempoTimeline);
            }
        }

        /// <summary>
        /// Populates the measure columns for a phrase row based on the Phrase object's notes.
        /// Assumes 4/4 time signature (4 quarter notes per measure).
        /// </summary>
        /// <param name="dgvPhrase">Target DataGridView</param>
        /// <param name="rowIndex">Index of the row to populate</param>
        public static void PopulateMeasureCells(DataGridView dgvPhrase, int rowIndex)
        {
            // Skip fixed rows
            if (rowIndex < FIXED_ROWS_COUNT || rowIndex >= dgvPhrase.Rows.Count)
                return;

            var row = dgvPhrase.Rows[rowIndex];
            var phrase = row.Cells["colData"].Value as Phrase;

            // Clear existing measure cells first
            for (int colIndex = MEASURE_START_COLUMN_INDEX; colIndex < dgvPhrase.Columns.Count; colIndex++)
            {
                row.Cells[colIndex].Value = string.Empty;
            }

            if (phrase == null || phrase.PhraseNotes.Count == 0)
                return;

            // Calculate ticks per measure (4/4 time: 4 quarter notes per measure)
            int ticksPerMeasure = MusicConstants.TicksPerQuarterNote * 4;

            // Group notes by measure based on their absolute position
            var notesByMeasure = phrase.PhraseNotes
                .GroupBy(note => note.AbsolutePositionTicks / ticksPerMeasure)
                .OrderBy(g => g.Key)
                .ToList();

            // Ensure we have enough columns for all measures
            int maxMeasure = notesByMeasure.Any() ? (int)notesByMeasure.Last().Key : 0;
            int requiredColumns = MEASURE_START_COLUMN_INDEX + maxMeasure + 1;
            
            while (dgvPhrase.Columns.Count < requiredColumns)
            {
                int measureNumber = dgvPhrase.Columns.Count - MEASURE_START_COLUMN_INDEX + 1;
                var colMeasure = new DataGridViewTextBoxColumn
                {
                    Name = $"colMeasure{measureNumber}",
                    HeaderText = $"{measureNumber}",
                    Width = 40,
                    ReadOnly = true,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleCenter
                    }
                };
                dgvPhrase.Columns.Add(colMeasure);
            }

            // Populate measure cells with note counts
            foreach (var measureGroup in notesByMeasure)
            {
                int measureIndex = (int)measureGroup.Key;
                int columnIndex = MEASURE_START_COLUMN_INDEX + measureIndex;
                
                if (columnIndex < dgvPhrase.Columns.Count)
                {
                    int noteCount = measureGroup.Count();
                    row.Cells[columnIndex].Value = noteCount.ToString();
                }
            }
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

            // Only handle changes to the Type column (which now contains the instrument dropdown for phrase rows)
            if (e.RowIndex < 0 || e.ColumnIndex != dgvPhrase.Columns["colType"]?.Index)
                return;

            var row = dgvPhrase.Rows[e.RowIndex];
            var cellValue = row.Cells["colData"].Value;

            // Check if the hidden data cell contains a Phrase object
            if (cellValue is Phrase phrase)
            {
                // Get the selected instrument name
                var selectedInstrumentName = row.Cells["colType"].FormattedValue?.ToString();

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

            // Column 0: Type column - convert to combo box for this row
            // Replace the text box cell with a combo box cell
            var comboBoxCell = new DataGridViewComboBoxCell
            {
                DataSource = new List<MidiInstrument>(midiInstruments),
                DisplayMember = "Name",
                ValueMember = "ProgramNumber",
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FlatStyle = FlatStyle.Flat
            };
            row.Cells["colType"] = comboBoxCell;

            // Column 1: Hidden data (Phrase object)
            row.Cells["colData"].Value = phrase;

            // Set MIDI Instrument dropdown value
            int programNumberToSet = -1;

            if (phrase.MidiProgramNumber <= 127 || phrase.MidiProgramNumber == 255)
            {
                programNumberToSet = phrase.MidiProgramNumber;
            }

            row.Cells["colType"].Value = programNumberToSet;

            // Column 2: Stave - default to 1 for newly added rows
            row.Cells["colStave"].Value = 1;

            // Column 3: Event number
            row.Cells["colEventNumber"].Value = phraseName;

            // Column 4: Description
            if (!string.IsNullOrEmpty(partName) && partName != "Select...")
            {
                row.Cells["colDescription"].Value = $"Part: {partName}";
            }
            else
            {
                row.Cells["colDescription"].Value = string.Empty;
            }

            // Populate measure cells (columns 5+) with note counts per measure
            PopulateMeasureCells(dgvPhrase, newRowIndex);
        }
    }
}