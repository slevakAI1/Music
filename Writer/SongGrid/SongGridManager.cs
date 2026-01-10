// AI: purpose=Manage WriterForm song grid layout and fixed control rows (voice/section/harmony/time/tempo).
// AI: invariants=Fixed rows 0..8 are reserved; MEASURE_START_COLUMN_INDEX is the canonical start for measure columns.
// AI: deps=Grid consumers rely on hidden "colData" to store track objects; changing cell names breaks multiple callers.
// AI: change=If adding control lines update InitializeFixedRows and GridControlLinesManager attachers to keep UI in sync.

using Music.Generator;
using Music.MyMidi;

namespace Music.Writer
{
    internal static class SongGridManager
    {
        // Constants for the four fixed rows at the top of the grid
        public const int FIXED_ROW_SEPARATOR_1 = 0;
        public const int FIXED_ROW_SECTION = 1;
        public const int FIXED_ROW_LYRICS = 2;
        public const int FIXED_ROW_VOICE = 3;
        public const int FIXED_ROW_HARMONY = 4;
        public const int FIXED_ROW_GROOVE = 5;
        public const int FIXED_ROW_TIME_SIGNATURE = 6;
        public const int FIXED_ROW_SEPARATOR_2 = 7;
        public const int FIXED_ROW_TEMPO = 8;
        public const int FIXED_ROW_SEPARATOR_3 = 9;
        public const int FIXED_ROWS_COUNT = 10;

        // Index where measure columns begin (adjusted because the Stave column was removed)
        public const int MEASURE_START_COLUMN_INDEX = 4;

        // Default number of measure columns to create initially
        public const int DEFAULT_MEASURE_COLUMNS = 32;

        // Track the next available track number for display purposes
        private static int _nextTrackNumber = 0;

        // AI: ResetTrackNumber resets the display counter; used when clearing grid to reuse numbering.
        public static void ResetTrackNumber()
        {
            _nextTrackNumber = 0;
        }

        // AI: ConfigureSongGridView: build columns, wire events, and initialize fixed rows. Keep column and fixed-row contracts stable.
        internal static void ConfigureSongGridView(
            DataGridView dgSong,
            DataGridViewCellEventHandler cellValueChangedHandler,
            EventHandler currentCellDirtyStateChangedHandler,
            SongContext? designer = null)
        {
            dgSong.AllowUserToAddRows = false;
            dgSong.AllowUserToResizeColumns = true;
            dgSong.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgSong.MultiSelect = true;
            dgSong.ReadOnly = false;
            dgSong.Columns.Clear();

            // Column 0: Hidden column containing the data object
            var colData = new DataGridViewTextBoxColumn
            {
                Name = "colData",
                HeaderText = "Data",
                Visible = false,
                ReadOnly = true
            };
            dgSong.Columns.Add(colData);

            // Column 1: Event number (read-only)
            var colEventNumber = new DataGridViewTextBoxColumn
            {
                Name = "colEventNumber",
                HeaderText = "#",
                Width = 50,
                ReadOnly = true
            };

            dgSong.Columns.Add(colEventNumber);

            // Column 2: Type column - text for fixed rows, combo box for song rows
            var colType = new DataGridViewTextBoxColumn
            {
                Name = "colType",
                HeaderText = "",
                Width = 200,
                ReadOnly = false
            };
            dgSong.Columns.Add(colType);

            // Column 3: Description
            var colDescription = new DataGridViewTextBoxColumn
            {
                Name = "colDescription",
                HeaderText = "",
                Width = 150,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            };
            dgSong.Columns.Add(colDescription);

            // Columns 4+: Measure columns (dynamically created)
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

            foreach (DataGridViewColumn col in dgSong.Columns)
            {
                col.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            }

            dgSong.ColumnAdded += (s, e) =>
            {
                if (e?.Column != null)
                    e.Column.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            };

            // Wire up event handlers
            dgSong.CellValueChanged += cellValueChangedHandler;
            dgSong.CurrentCellDirtyStateChanged += currentCellDirtyStateChangedHandler;

            // Add the fixed rows at the top and optionally attach tracks
            InitializeFixedRows(dgSong, designer);
        }

        // AI: InitializeFixedRows: creates fixed rows in defined order and styles separator rows. Do not reorder fixed row indices.
        private static void InitializeFixedRows(
            DataGridView dgSong,
            SongContext? songContext = null)
        {
            for (int i = 0; i < FIXED_ROWS_COUNT; i++)
            {
                dgSong.Rows.Add();
            }

            dgSong.Rows[FIXED_ROW_VOICE].Cells["colType"].Value = "Voice";
            dgSong.Rows[FIXED_ROW_VOICE].Cells["colType"].ReadOnly = true;
            dgSong.Rows[FIXED_ROW_VOICE].ReadOnly = true;

            dgSong.Rows[FIXED_ROW_SECTION].Cells["colType"].Value = "Section";
            dgSong.Rows[FIXED_ROW_SECTION].Cells["colType"].ReadOnly = true;
            dgSong.Rows[FIXED_ROW_SECTION].ReadOnly = true;

            dgSong.Rows[FIXED_ROW_LYRICS].Cells["colType"].Value = "Lyrics";
            dgSong.Rows[FIXED_ROW_LYRICS].Cells["colType"].ReadOnly = true;
            dgSong.Rows[FIXED_ROW_LYRICS].ReadOnly = true;

            dgSong.Rows[FIXED_ROW_TIME_SIGNATURE].Cells["colType"].Value = "Time Signature";
            dgSong.Rows[FIXED_ROW_TIME_SIGNATURE].Cells["colType"].ReadOnly = true;
            dgSong.Rows[FIXED_ROW_TIME_SIGNATURE].ReadOnly = true;

            dgSong.Rows[FIXED_ROW_HARMONY].Cells["colType"].Value = "Harmony";
            dgSong.Rows[FIXED_ROW_HARMONY].Cells["colType"].ReadOnly = true;
            dgSong.Rows[FIXED_ROW_HARMONY].ReadOnly = true;

            dgSong.Rows[FIXED_ROW_GROOVE].Cells["colType"].Value = "Groove";
            dgSong.Rows[FIXED_ROW_HARMONY].Cells["colType"].ReadOnly = true;
            dgSong.Rows[FIXED_ROW_HARMONY].ReadOnly = true;

            dgSong.Rows[FIXED_ROW_TEMPO].Cells["colType"].Value = "Tempo";
            dgSong.Rows[FIXED_ROW_TEMPO].Cells["colType"].ReadOnly = true;
            dgSong.Rows[FIXED_ROW_TEMPO].ReadOnly = true;

            var sepRow1 = dgSong.Rows[FIXED_ROW_SEPARATOR_1];
            sepRow1.Cells["colDescription"].Value = "Design";
            sepRow1.Cells["colType"].ReadOnly = true;
            sepRow1.ReadOnly = true;

            sepRow1.DefaultCellStyle.BackColor = Color.Black;
            sepRow1.DefaultCellStyle.ForeColor = Color.White;
            sepRow1.DefaultCellStyle.SelectionBackColor = Color.Black;
            sepRow1.DefaultCellStyle.SelectionForeColor = Color.White;

            var sepRow2 = dgSong.Rows[FIXED_ROW_SEPARATOR_2];
            sepRow2.Cells["colDescription"].Value = "Midi";
            sepRow2.Cells["colType"].ReadOnly = true;
            sepRow2.ReadOnly = true;

            sepRow2.DefaultCellStyle.BackColor = Color.Black;
            sepRow2.DefaultCellStyle.ForeColor = Color.White;
            sepRow2.DefaultCellStyle.SelectionBackColor = Color.Black;
            sepRow2.DefaultCellStyle.SelectionForeColor = Color.White;

            var sepRow3 = dgSong.Rows[FIXED_ROW_SEPARATOR_3];
            sepRow3.Cells["colType"].Value = string.Empty;
            sepRow3.Cells["colType"].ReadOnly = true;
            sepRow3.ReadOnly = true;

            sepRow3.DefaultCellStyle.BackColor = Color.Black;
            sepRow3.DefaultCellStyle.ForeColor = Color.White;
            sepRow3.DefaultCellStyle.SelectionBackColor = Color.Black;
            sepRow3.DefaultCellStyle.SelectionForeColor = Color.White;

            if (songContext != null)
            {
                GridControlLinesManager.AttachsectionTrack(dgSong, songContext.SectionTrack);
                GridControlLinesManager.AttachharmonyTrack(dgSong, songContext.HarmonyTrack);
                GridControlLinesManager.AttachTimeSignatureTrack(dgSong, songContext.Song.TimeSignatureTrack);
                GridControlLinesManager.AttachTempoTrack(dgSong, songContext.Song.TempoTrack);
            }
        }

        // AI: PopulatePartMeasureNoteCount: counts notes per bar using timeSignatureTrack.GetActiveTimeSignatureEvent and ticks math.
        public static void PopulatePartMeasureNoteCount(DataGridView dgSong, int rowIndex, Timingtrack timeSignatureTrack)
        {
            if (rowIndex < FIXED_ROWS_COUNT || rowIndex >= dgSong.Rows.Count)
                return;

            var row = dgSong.Rows[rowIndex];
            var track = row.Cells["colData"].Value as PartTrack;

            for (int colIndex = MEASURE_START_COLUMN_INDEX; colIndex < dgSong.Columns.Count; colIndex++)
            {
                row.Cells[colIndex].Value = string.Empty;
            }

            if (track == null || track.PartTrackNoteEvents.Count == 0)
                return;

            if (timeSignatureTrack == null || timeSignatureTrack.Events.Count == 0)
                return;

            long maxTicks = track.PartTrackNoteEvents.Max(n => n.AbsoluteTimeTicks);
            int estimatedMaxBar = 100; // Start with a reasonable estimate

            var noteCountByBar = new Dictionary<int, int>();
            int currentTick = 0;

            for (int bar = 1; bar <= estimatedMaxBar; bar++)
            {
                var timeSignature = timeSignatureTrack.GetActiveTimeSignatureEvent(bar);
                if (timeSignature == null)
                    break;

                int ticksPerMeasure = (MusicConstants.TicksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;
                int barStartTick = currentTick;
                int barEndTick = currentTick + ticksPerMeasure;

                int noteCount = track.PartTrackNoteEvents
                    .Count(note => note.AbsoluteTimeTicks >= barStartTick && note.AbsoluteTimeTicks < barEndTick);

                if (noteCount > 0)
                {
                    noteCountByBar[bar] = noteCount;
                }

                currentTick += ticksPerMeasure;

                if (currentTick > maxTicks)
                    break;
            }

            if (noteCountByBar.Any())
            {
                int maxMeasure = noteCountByBar.Keys.Max();
                int requiredColumns = MEASURE_START_COLUMN_INDEX + maxMeasure;

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

                foreach (var kvp in noteCountByBar)
                {
                    int bar = kvp.Key;
                    int noteCount = kvp.Value;
                    int columnIndex = MEASURE_START_COLUMN_INDEX + bar - 1;

                    if (columnIndex < dgSong.Columns.Count)
                    {
                        row.Cells[columnIndex].Value = noteCount.ToString();
                    }
                }
            }
        }

        // AI: ClearMeasureCellsForRow: clears display cells from MEASURE_START_COLUMN_INDEX onward for a given row.
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

        // AI: HandleCurrentCellDirtyStateChanged: commits combo edits immediately so CellValueChanged fires promptly.
        internal static void HandleCurrentCellDirtyStateChanged(DataGridView dgSong, object? sender, EventArgs e)
        {
            if (dgSong.IsCurrentCellDirty && dgSong.CurrentCell is DataGridViewComboBoxCell)
            {
                dgSong.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        // AI: HandleCellValueChanged: only handles instrument dropdown changes for song rows; updates PartTrack.MidiProgramName.
        internal static void HandleCellValueChanged(DataGridView dgSong, object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < FIXED_ROWS_COUNT)
                return;

            if (e.RowIndex < 0 || e.ColumnIndex != dgSong.Columns["colType"]?.Index)
                return;

            var row = dgSong.Rows[e.RowIndex];
            var cellValue = row.Cells["colData"].Value;

            if (cellValue is PartTrack songTrack)
            {
                var selectedInstrumentName = row.Cells["colType"].FormattedValue?.ToString();

                if (!string.IsNullOrEmpty(selectedInstrumentName))
                {
                    songTrack.MidiProgramName = selectedInstrumentName;
                    row.Cells["colDescription"].Value = $"Part: {selectedInstrumentName}";
                }
            }
        }

        // AI: AddNewPartTrack: appends a PartTrack row, configures instrument combo, sets hidden data, and populates measure counts.
        internal static void AddNewPartTrack(
            PartTrack track,
            DataGridView dgSong)
        {
            _nextTrackNumber++;
            var rowName = _nextTrackNumber.ToString();

            var voiceName = track.MidiProgramName ?? "Select...";

            int newRowIndex = dgSong.Rows.Add();
            var row = dgSong.Rows[newRowIndex];

            var comboBoxCell = new DataGridViewComboBoxCell
            {
                DataSource = new List<MidiVoices>(MidiVoices.MidiVoiceList()),
                DisplayMember = "Name",
                ValueMember = "ProgramNumber",
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FlatStyle = FlatStyle.Flat
            };
            row.Cells["colType"] = comboBoxCell;

            row.Cells["colData"].Value = track;

            int programNumberToSet = -1;

            if (track.MidiProgramNumber <= 127 || track.MidiProgramNumber == 255)
            {
                programNumberToSet = track.MidiProgramNumber;
            }

            row.Cells["colType"].Value = programNumberToSet;

            row.Cells["colEventNumber"].Value = rowName;

            if (!string.IsNullOrEmpty(voiceName) && voiceName != "Select...")
            {
                row.Cells["colDescription"].Value = $"Part: {voiceName}";
            }
            else
            {
                row.Cells["colDescription"].Value = string.Empty;
            }

            var timeSignatureTrack = GridControlLinesManager.GetTimeSignatureTrack(dgSong);

            PopulatePartMeasureNoteCount(dgSong, newRowIndex, timeSignatureTrack);
        }

        public static void ClearMeasureHighlight(DataGridView dgSong, int measureNumber)
        {
            System.Diagnostics.Debug.WriteLine($"[SongGridManager] ClearMeasureHighlight: measureNumber={measureNumber}");
            
            if (dgSong == null || measureNumber <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[SongGridManager] ClearMeasureHighlight: SKIPPED (null grid or invalid measure)");
                return;
            }

            int colIndex = MEASURE_START_COLUMN_INDEX + measureNumber - 1;
            System.Diagnostics.Debug.WriteLine($"[SongGridManager] ClearMeasureHighlight: colIndex={colIndex}, TotalColumns={dgSong.Columns.Count}");
            
            if (colIndex < MEASURE_START_COLUMN_INDEX || colIndex >= dgSong.Columns.Count)
            {
                System.Diagnostics.Debug.WriteLine($"[SongGridManager] ClearMeasureHighlight: SKIPPED (column out of range)");
                return;
            }

            // Highlight only FIXED_ROW_SEPARATOR_2 (row 7)
            dgSong.Rows[FIXED_ROW_SEPARATOR_2].Cells[colIndex].Style.BackColor = Color.Black;
            
            System.Diagnostics.Debug.WriteLine($"[SongGridManager] ClearMeasureHighlight: COMPLETED for row {FIXED_ROW_SEPARATOR_2}");
        }

        public static void HighlightCurrentMeasure(DataGridView dgSong, int measureNumber)
        {
            System.Diagnostics.Debug.WriteLine($"[SongGridManager] HighlightCurrentMeasure: measureNumber={measureNumber}");
            
            if (dgSong == null || measureNumber <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[SongGridManager] HighlightCurrentMeasure: SKIPPED (null grid or invalid measure)");
                return;
            }

            int colIndex = MEASURE_START_COLUMN_INDEX + measureNumber - 1;
            System.Diagnostics.Debug.WriteLine($"[SongGridManager] HighlightCurrentMeasure: colIndex={colIndex}, TotalColumns={dgSong.Columns.Count}");
            
            if (colIndex < MEASURE_START_COLUMN_INDEX || colIndex >= dgSong.Columns.Count)
            {
                System.Diagnostics.Debug.WriteLine($"[SongGridManager] HighlightCurrentMeasure: SKIPPED (column out of range)");
                return;
            }

            // Highlight only FIXED_ROW_SEPARATOR_2 (row 7)
            dgSong.Rows[FIXED_ROW_SEPARATOR_2].Cells[colIndex].Style.BackColor = Color.LimeGreen;
            
            System.Diagnostics.Debug.WriteLine($"[SongGridManager] HighlightCurrentMeasure: COMPLETED for row {FIXED_ROW_SEPARATOR_2}");
        }

        public static void ClearAllMeasureHighlights(DataGridView dgSong)
        {
            System.Diagnostics.Debug.WriteLine($"[SongGridManager] ClearAllMeasureHighlights");
            
            if (dgSong == null)
            {
                System.Diagnostics.Debug.WriteLine($"[SongGridManager] ClearAllMeasureHighlights: SKIPPED (null grid)");
                return;
            }

            int clearedCells = 0;
            for (int colIndex = MEASURE_START_COLUMN_INDEX; colIndex < dgSong.Columns.Count; colIndex++)
            {
                // Clear only FIXED_ROW_SEPARATOR_2 (row 7)
                dgSong.Rows[FIXED_ROW_SEPARATOR_2].Cells[colIndex].Style.BackColor = Color.Black;
                clearedCells++;
            }
            
            System.Diagnostics.Debug.WriteLine($"[SongGridManager] ClearAllMeasureHighlights: COMPLETED, cleared {clearedCells} cells in row {FIXED_ROW_SEPARATOR_2}");
        }
    }
}