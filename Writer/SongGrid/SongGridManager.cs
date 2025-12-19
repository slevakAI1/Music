using Music.MyMidi;

namespace Music.Writer
{
    /// <summary>
    /// Manages adding song components to the dgSong DataGridView control
    /// on the WriterForm form. Components include Sections, Harmonies, Time Signatures, Tempos, SongTrackNoteEvents
    /// </summary>
    internal static class SongGridManager
    {
        // Constants for the four fixed rows at the top of the grid
        public const int FIXED_ROW_SECTION = 0;
        public const int FIXED_ROW_HARMONY = 1;
        public const int FIXED_ROW_TIME_SIGNATURE = 2;
        public const int FIXED_ROW_TEMPO = 3;
        public const int FIXED_ROW_SEPARATOR = 4;
        public const int FIXED_ROWS_COUNT = 5;

        // Index where measure columns begin (adjusted because the Stave column was removed)
        public const int MEASURE_START_COLUMN_INDEX = 4;

        // Default number of measure columns to create initially
        public const int DEFAULT_MEASURE_COLUMNS = 32;

        /// <summary>
        /// Configures the dgSong DataGridView with proper columns including MIDI instrument dropdown.
        /// </summary>
        /// <param name="dgSong">The DataGridView to configure</param>
        /// <param name="midiInstruments">List of available MIDI instruments</param>
        /// <param name="cellValueChangedHandler">Event handler for cell value changes</param>
        /// <param name="currentCellDirtyStateChangedHandler">Event handler for dirty state changes</param>
        /// <param name="tempoTimeline">Optional TempoTimeline to place into the fixed Tempo row hidden data cell</param>
        internal static void ConfigureSongGridView(
            DataGridView dgSong,
            List<MidiInstrument> midiInstruments,
            DataGridViewCellEventHandler cellValueChangedHandler,
            EventHandler currentCellDirtyStateChangedHandler,
            Designer.Designer? designer = null)
        {
            dgSong.AllowUserToAddRows = false;
            dgSong.AllowUserToResizeColumns = true;
            dgSong.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgSong.MultiSelect = true;
            dgSong.ReadOnly = false;
            dgSong.Columns.Clear();

            // Column 0: Event number (read-only)
            var colEventNumber = new DataGridViewTextBoxColumn
            {
                Name = "colEventNumber",
                HeaderText = "#",
                Width = 50,
                ReadOnly = true
            };

            dgSong.Columns.Add(colEventNumber);

            // Column 1: Type column - text for fixed rows, combo box for song rows
            var colType = new DataGridViewTextBoxColumn
            {
                Name = "colType",
                HeaderText = "Type",
                Width = 200,
                ReadOnly = false // Will be set per-cell basis in InitializeFixedRows
            };
            dgSong.Columns.Add(colType);


            // Column 2: Hidden column containing the data object
            var colData = new DataGridViewTextBoxColumn
            {
                Name = "colData",
                HeaderText = "Data",
                Visible = false,
                ReadOnly = true
            };
            dgSong.Columns.Add(colData);

            // Column 3: Description (read-only for now)
            var colDescription = new DataGridViewTextBoxColumn
            {
                Name = "colDescription",
                HeaderText = "Description",
                Width = 300,
                ReadOnly = true
            };
            dgSong.Columns.Add(colDescription);

            // Columns 4+: Measure columns (dynamically created)
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
                dgSong.Columns.Add(colMeasure);
            }

            // Ensure existing columns are not sortable and make future added columns not sortable as well
            foreach (DataGridViewColumn col in dgSong.Columns)
            {
                col.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            }

            // This is an event handler!
            dgSong.ColumnAdded += (s, e) =>
            {
                if (e?.Column != null)
                    e.Column.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            };

            // Wire up event handlers
            dgSong.CellValueChanged += cellValueChangedHandler;
            dgSong.CurrentCellDirtyStateChanged += currentCellDirtyStateChangedHandler;

            // Add the fixed rows at the top and optionally attach tempoTimeline to the hidden data cell
            InitializeFixedRows(dgSong, midiInstruments, designer);
        }

        /// <summary>
        /// Initializes the fixed rows at the top of the grid.
        /// Entire fixed rows are set to ReadOnly so users cannot edit them.
        /// If a TempoTimeline is provided it will be stored in the fixed Tempo row's hidden data cell.
        /// The separator row is styled with black background and white foreground.
        /// </summary>
        private static void InitializeFixedRows(
            DataGridView dgSong,
            List<MidiInstrument> midiInstruments,
            Designer.Designer? designer = null)
        {
            // Add fixed rows
            for (int i = 0; i < FIXED_ROWS_COUNT; i++)
            {
                dgSong.Rows.Add();
            }

            // Set Type column values for the fixed rows and mark each row read-only
            dgSong.Rows[FIXED_ROW_SECTION].Cells["colType"].Value = "Section";
            dgSong.Rows[FIXED_ROW_SECTION].Cells["colType"].ReadOnly = true;
            dgSong.Rows[FIXED_ROW_SECTION].ReadOnly = true;

            dgSong.Rows[FIXED_ROW_TIME_SIGNATURE].Cells["colType"].Value = "Time Signature";
            dgSong.Rows[FIXED_ROW_TIME_SIGNATURE].Cells["colType"].ReadOnly = true;
            dgSong.Rows[FIXED_ROW_TIME_SIGNATURE].ReadOnly = true;

            dgSong.Rows[FIXED_ROW_HARMONY].Cells["colType"].Value = "Harmony";
            dgSong.Rows[FIXED_ROW_HARMONY].Cells["colType"].ReadOnly = true;
            dgSong.Rows[FIXED_ROW_HARMONY].ReadOnly = true;

            dgSong.Rows[FIXED_ROW_TEMPO].Cells["colType"].Value = "Tempo";
            dgSong.Rows[FIXED_ROW_TEMPO].Cells["colType"].ReadOnly = true;
            dgSong.Rows[FIXED_ROW_TEMPO].ReadOnly = true;

            // Separator row: style black background and white foreground across entire row
            var sepRow = dgSong.Rows[FIXED_ROW_SEPARATOR];
            // ensure the Type cell exists and is readonly
            sepRow.Cells["colType"].Value = string.Empty;
            sepRow.Cells["colType"].ReadOnly = true;
            sepRow.ReadOnly = true;

            // Apply row styling (including selection colors so selection doesn't hide the appearance)
            sepRow.DefaultCellStyle.BackColor = Color.Black;
            sepRow.DefaultCellStyle.ForeColor = Color.White;
            sepRow.DefaultCellStyle.SelectionBackColor = Color.Black;
            sepRow.DefaultCellStyle.SelectionForeColor = Color.White;

            // Delegate attaching the control lines to the control line manager class
            if (designer != null)
            {
                GridControlLinesManager.AttachSectionTimeline(dgSong, designer.SectionTimeline);
                GridControlLinesManager.AttachHarmonyTimeline(dgSong, designer.HarmonyTimeline);
                GridControlLinesManager.AttachTimeSignatureTimeline(dgSong, designer.TimeSignatureTimeline);
                GridControlLinesManager.AttachTempoTimeline(dgSong, designer.TempoTimeline);
            }
        }

        /// <summary>
        /// Populates the measure columns for a song row based on the Song object's notes.
        /// Assumes 4/4 time signature (4 quarter notes per measure).
        /// </summary>
        /// <param name="dgSong">Target DataGridView</param>
        /// <param name="rowIndex">Index of the row to populate</param>
        public static void PopulatePartMeasureNoteCount(DataGridView dgSong, int rowIndex)
        {
            // Skip fixed rows
            if (rowIndex < FIXED_ROWS_COUNT || rowIndex >= dgSong.Rows.Count)
                return;

            var row = dgSong.Rows[rowIndex];
            var track = row.Cells["colData"].Value as SongTrack;

            // Clear existing measure cells first
            for (int colIndex = MEASURE_START_COLUMN_INDEX; colIndex < dgSong.Columns.Count; colIndex++)
            {
                row.Cells[colIndex].Value = string.Empty;
            }

            if (track == null || track.SongTrackNoteEvents.Count == 0)
                return;

            // Calculate ticks per measure (4/4 time: 4 quarter notes per measure)
            int ticksPerMeasure = MusicConstants.TicksPerQuarterNote * 4;

            // Group notes by measure based on their absolute position
            var notesByMeasure = track.SongTrackNoteEvents
                .GroupBy(note => note.AbsolutePositionTicks / ticksPerMeasure)
                .OrderBy(g => g.Key)
                .ToList();

            // Ensure we have enough columns for all measures
            int maxMeasure = notesByMeasure.Any() ? (int)notesByMeasure.Last().Key : 0;
            int requiredColumns = MEASURE_START_COLUMN_INDEX + maxMeasure + 1;
            
            while (dgSong.Columns.Count < requiredColumns)
            {
                int measureNumber = dgSong.Columns.Count - MEASURE_START_COLUMN_INDEX + 1;
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
                dgSong.Columns.Add(colMeasure);
            }

            // Populate measure cells with note counts
            foreach (var measureGroup in notesByMeasure)
            {
                int measureIndex = (int)measureGroup.Key;
                int columnIndex = MEASURE_START_COLUMN_INDEX + measureIndex;
                
                if (columnIndex < dgSong.Columns.Count)
                {
                    int noteCount = measureGroup.Count();
                    row.Cells[columnIndex].Value = noteCount.ToString();
                }
            }
        }

        /// <summary>
        /// Clears the measure display cells (columns MEASURE_START_COLUMN_INDEX and onward) for the specified row.
        /// Safe to call for both fixed rows and track rows.
        /// </summary>
        /// <param name="dgSong">Target DataGridView</param>
        /// <param name="rowIndex">Row index whose measure cells should be cleared</param>
        public static void ClearMeasureCellsForRow(DataGridView dgSong, int rowIndex)
        {
            if (dgSong == null || rowIndex < 0 || rowIndex >= dgSong.Rows.Count)
                return;

            var row = dgSong.Rows[rowIndex];
            for (int colIndex = MEASURE_START_COLUMN_INDEX; colIndex < dgSong.Columns.Count; colIndex++)
            {
                row.Cells[colIndex].Value = string.Empty;
            }
        }

        /// <summary>
        /// Handles the CurrentCellDirtyStateChanged event to commit combo box edits immediately.
        /// </summary>
        /// <param name="dgSong">The DataGridView</param>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event args</param>
        internal static void HandleCurrentCellDirtyStateChanged(DataGridView dgSong, object? sender, EventArgs e)
        {
            if (dgSong.IsCurrentCellDirty && dgSong.CurrentCell is DataGridViewComboBoxCell)
            {
                dgSong.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        /// <summary>
        /// Handles the CellValueChanged event to update Song objects when instrument selection changes.
        /// </summary>
        /// <param name="dgSong">The DataGridView</param>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event args</param>
        internal static void HandleCellValueChanged(DataGridView dgSong, object? sender, DataGridViewCellEventArgs e)
        {
            // Skip fixed rows
            if (e.RowIndex < FIXED_ROWS_COUNT)
                return;

            // Only handle changes to the Type column (which now contains the instrument dropdown for song rows)
            if (e.RowIndex < 0 || e.ColumnIndex != dgSong.Columns["colType"]?.Index)
                return;

            var row = dgSong.Rows[e.RowIndex];
            var cellValue = row.Cells["colData"].Value;

            // Check if the hidden data cell contains a SongTrack object
            if (cellValue is SongTrack songTrack)
            {
                // Get the selected instrument name
                var selectedInstrumentName = row.Cells["colType"].FormattedValue?.ToString();

                if (!string.IsNullOrEmpty(selectedInstrumentName))
                {
                    // Update the SongTrack object's MidiProgramName property
                    songTrack.MidiProgramName = selectedInstrumentName;

                    // Optionally update the description column to reflect the change
                    row.Cells["colDescription"].Value = $"Part: {selectedInstrumentName}";
                }
            }
        }

        /// <summary>
        /// Adds a track to the song grid with instrument information from the track itself.
        /// </summary>
        /// <param name="track">The track to add</param>
        /// <param name="midiInstruments">List of available MIDI instruments</param>
        /// <param name="dgSong">The DataGridView to add to</param>
        /// <param name="rowNumber">Reference to the row counter (will be incremented)</param>
        internal static void AddSongTrackToGrid(
            SongTrack track,
            List<MidiInstrument> midiInstruments,
            DataGridView dgSong,
            ref int rowNumber)
        {
            // Set row name/number
            rowNumber++;
            var rowName = rowNumber.ToString();

            // Get part name from the track
            var voiceName = track.MidiProgramName ?? "Select...";

            // Add new row
            int newRowIndex = dgSong.Rows.Add();
            var row = dgSong.Rows[newRowIndex];

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

            // Column 1: Hidden data (SongTrack object)
            row.Cells["colData"].Value = track;

            // Set MIDI Instrument dropdown value
            int programNumberToSet = -1;

            if (track.MidiProgramNumber <= 127 || track.MidiProgramNumber == 255)
            {
                programNumberToSet = track.MidiProgramNumber;
            }

            row.Cells["colType"].Value = programNumberToSet;

            // Column 2: Event number
            row.Cells["colEventNumber"].Value = rowName;

            // Column 3: Description
            if (!string.IsNullOrEmpty(voiceName) && voiceName != "Select...")
            {
                row.Cells["colDescription"].Value = $"Part: {voiceName}";
            }
            else
            {
                row.Cells["colDescription"].Value = string.Empty;
            }

            // Populate measure cells (columns 4+) with note counts per measure
            PopulatePartMeasureNoteCount(dgSong, newRowIndex);
        }
    }
}