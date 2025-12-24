using Music.Designer;
using Music.Generator;

namespace Music.Writer
{
    /// <summary>
    /// Manages the grid control lines (fixed rows) such as Tempo row population.
    /// </summary>
    internal static class GridControlLinesManager
    {

        #region AttachsectionTrack

        /// <summary>
        /// Public helper to attach a SectionTrack instance to the fixed Section row's hidden data cell.
        /// Safe to call anytime after the grid's columns and rows have been created.
        /// </summary>
        /// <param name="dgSong">Target DataGridView</param>
        /// <param name="sectionTrack">SectionTrack to store in the hidden data cell (null to skip)</param>
        public static void AttachsectionTrack(DataGridView dgSong, SectionTrack? sectionTrack)
        {
            if (sectionTrack == null)
                return;

            if (!dgSong.Columns.Contains("colData"))
                return;

            if (dgSong.Rows.Count <= SongGridManager.FIXED_ROW_SECTION)
                return;

            // Populate the section row with the track data
            PopulateSectionRow(dgSong, sectionTrack);
        }

        /// <summary>
        /// Populates the fixed Section row with section numbers at their respective measure positions.
        /// </summary>
        /// <param name="dgSong">Target DataGridView</param>
        /// <param name="sectionTrack">SectionTrack containing section events</param>
        private static void PopulateSectionRow(DataGridView dgSong, SectionTrack sectionTrack)
        {
            // Store the track in the hidden data cell
            dgSong.Rows[SongGridManager.FIXED_ROW_SECTION].Cells["colData"].Value = sectionTrack;

            // Clear all existing measure cells in the section row
            for (int colIndex = SongGridManager.MEASURE_START_COLUMN_INDEX; colIndex < dgSong.Columns.Count; colIndex++)
            {
                dgSong.Rows[SongGridManager.FIXED_ROW_SECTION].Cells[colIndex].Value = string.Empty;
            }

            if (sectionTrack.Sections.Count == 0)
                return;

            // Calculate total measures needed
            int totalMeasures = sectionTrack.TotalBars;

            // Ensure we have enough columns for all measures
            int requiredColumns = SongGridManager.MEASURE_START_COLUMN_INDEX + totalMeasures;
            while (dgSong.Columns.Count < requiredColumns)
            {
                int measureNumber = dgSong.Columns.Count - SongGridManager.MEASURE_START_COLUMN_INDEX + 1;
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

            // Populate measure cells with section numbers
            for (int sectionIndex = 0; sectionIndex < sectionTrack.Sections.Count; sectionIndex++)
            {
                var section = sectionTrack.Sections[sectionIndex];
                int sectionNumber = sectionIndex + 1; // 1-based section number

                // Iterate through each bar in this section
                for (int bar = 0; bar < section.BarCount; bar++)
                {
                    // Calculate the absolute measure number (1-based)
                    int absoluteMeasure = section.StartBar + bar;
                    
                    // Convert to 0-based measure index
                    int measureIndex = absoluteMeasure - 1;
                    
                    // Calculate the column index for this measure
                    int columnIndex = SongGridManager.MEASURE_START_COLUMN_INDEX + measureIndex;

                    // Set the section number in the appropriate measure cell
                    if (columnIndex < dgSong.Columns.Count)
                    {
                        dgSong.Rows[SongGridManager.FIXED_ROW_SECTION].Cells[columnIndex].Value = sectionNumber.ToString();
                    }
                }
            }
        }

        #endregion

        #region AttachharmonyTrack

        /// <summary>
        /// Public helper to attach a HarmonyTrack instance to the fixed Harmony row's hidden data cell.
        /// Safe to call anytime after the grid's columns and rows have been created.
        /// </summary>
        /// <param name="dgSong">Target DataGridView</param>
        /// <param name="harmonyTrack">HarmonyTrack to store in the hidden data cell (null to skip)</param>
        public static void AttachharmonyTrack(DataGridView dgSong, HarmonyTrack? harmonyTrack)
        {
            if (harmonyTrack == null)
                return;

            if (!dgSong.Columns.Contains("colData"))
                return;

            if (dgSong.Rows.Count <= SongGridManager.FIXED_ROW_HARMONY)
                return;

            // Populate the harmony row with the track data
            PopulateHarmonyRow(dgSong, harmonyTrack);
        }

        /// <summary>
        /// Populates the fixed Harmony row with chord notation at their respective measure positions.
        /// Multiple chords per measure are separated by line breaks.
        /// </summary>
        /// <param name="dgSong">Target DataGridView</param>
        /// <param name="harmonyTrack">HarmonyTrack containing harmony events</param>
        private static void PopulateHarmonyRow(DataGridView dgSong, HarmonyTrack harmonyTrack)
        {
            // Store the track in the hidden data cell
            dgSong.Rows[SongGridManager.FIXED_ROW_HARMONY].Cells["colData"].Value = harmonyTrack;

            // Clear all existing measure cells in the harmony row
            for (int colIndex = SongGridManager.MEASURE_START_COLUMN_INDEX; colIndex < dgSong.Columns.Count; colIndex++)
            {
                dgSong.Rows[SongGridManager.FIXED_ROW_HARMONY].Cells[colIndex].Value = string.Empty;
            }

            if (harmonyTrack.Events.Count == 0)
                return;

            // Group harmony events by the bar they start in
            var eventsByBar = harmonyTrack.Events
                .GroupBy(evt => evt.StartBar)
                .OrderBy(g => g.Key)
                .ToList();

            // Populate measure cells with chord notations
            foreach (var barGroup in eventsByBar)
            {
                int bar = barGroup.Key;
                
                // Convert 1-based bar number to 0-based measure index
                int measureIndex = bar - 1;
                
                // Calculate the column index for this measure
                int columnIndex = SongGridManager.MEASURE_START_COLUMN_INDEX + measureIndex;

                // Ensure the column exists (dynamically add if needed)
                while (dgSong.Columns.Count <= columnIndex)
                {
                    int measureNumber = dgSong.Columns.Count - SongGridManager.MEASURE_START_COLUMN_INDEX + 1;
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

                // Convert harmony events to chord notation and join with line breaks
                if (columnIndex < dgSong.Columns.Count)
                {
                    var chordNotations = barGroup
                        .OrderBy(evt => evt.StartBeat) // Order by beat within the bar
                        .Select(evt => MusicCalculations.ConvertToChordNotation(
                            evt.Key,
                            evt.Degree,
                            evt.Quality,
                            evt.Bass))
                        .ToList();

                    // Join multiple chords with CRLF
                    dgSong.Rows[SongGridManager.FIXED_ROW_HARMONY].Cells[columnIndex].Value = 
                        string.Join(Environment.NewLine, chordNotations);
                }
            }
        }

        #endregion

        #region AttachTimeSignatureTrack

        /// <summary>
        /// Public helper to attach a TimeSignatureTrack instance to the fixed Time Signature row's hidden data cell.
        /// Safe to call anytime after the grid's columns and rows have been created.
        /// </summary>
        /// <param name="dgSong">Target DataGridView</param>
        /// <param name="timeSignatureTrack">TimeSignatureTrack to store in the hidden data cell (null to skip)</param>
        public static void AttachTimeSignatureTrack(DataGridView dgSong, TimeSignatureTrack? timeSignatureTrack)
        {
            if (timeSignatureTrack == null)
                return;

            if (!dgSong.Columns.Contains("colData"))
                return;

            if (dgSong.Rows.Count <= SongGridManager.FIXED_ROW_TIME_SIGNATURE)
                return;

            // Populate the time signature row with the track data
            PopulateTimeSignatureRow(dgSong, timeSignatureTrack);
        }

        /// <summary>
        /// Populates the fixed Time Signature row with time signature values at their respective measure positions.
        /// </summary>
        /// <param name="dgSong">Target DataGridView</param>
        /// <param name="timeSignatureTrack">TimeSignatureTrack containing time signature events</param>
        private static void PopulateTimeSignatureRow(DataGridView dgSong, TimeSignatureTrack timeSignatureTrack)
        {
            // Store the track in the hidden data cell
            dgSong.Rows[SongGridManager.FIXED_ROW_TIME_SIGNATURE].Cells["colData"].Value = timeSignatureTrack;

            // Clear all existing measure cells in the time signature row
            for (int colIndex = SongGridManager.MEASURE_START_COLUMN_INDEX; colIndex < dgSong.Columns.Count; colIndex++)
            {
                dgSong.Rows[SongGridManager.FIXED_ROW_TIME_SIGNATURE].Cells[colIndex].Value = string.Empty;
            }

            if (timeSignatureTrack.Events.Count == 0)
                return;

            // Populate measure cells with time signature at the starting bar of each time signature event
            foreach (var timeSignatureEvent in timeSignatureTrack.Events)
            {
                // Convert 1-based bar number to 0-based measure index
                int measureIndex = timeSignatureEvent.StartBar - 1;
                
                // Calculate the column index for this measure
                int columnIndex = SongGridManager.MEASURE_START_COLUMN_INDEX + measureIndex;

                // Ensure the column exists (dynamically add if needed)
                while (dgSong.Columns.Count <= columnIndex)
                {
                    int measureNumber = dgSong.Columns.Count - SongGridManager.MEASURE_START_COLUMN_INDEX + 1;
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

                // Set the time signature value in the appropriate measure cell (format: "numerator/denominator")
                if (columnIndex < dgSong.Columns.Count)
                {
                    dgSong.Rows[SongGridManager.FIXED_ROW_TIME_SIGNATURE].Cells[columnIndex].Value = 
                        $"{timeSignatureEvent.Numerator}/{timeSignatureEvent.Denominator}";
                }
            }
        }
        
        #endregion

        #region AttachTempoTrack

        /// <summary>
        /// Public helper to attach a TempoTrack instance to the fixed Tempo row's hidden data cell.
        /// Safe to call anytime after the grid's columns and rows have been created.
        /// </summary>
        /// <param name="dgSong">Target DataGridView</param>
        /// <param name="tempoTrack">TempoTrack to store in the hidden data cell (null to skip)</param>
        public static void AttachTempoTrack(DataGridView dgSong, TempoTrack? tempoTrack)
        {
            if (tempoTrack == null)
                return;

            if (!dgSong.Columns.Contains("colData"))
                return;

            if (dgSong.Rows.Count <= SongGridManager.FIXED_ROW_TEMPO)
                return;

            // Populate the tempo row with the track data
            PopulateTempoRow(dgSong, tempoTrack);
        }

        /// <summary>
        /// Populates the fixed Tempo row with BPM values at their respective measure positions.
        /// </summary>
        /// <param name="dgSong">Target DataGridView</param>
        /// <param name="tempoTrack">TempoTrack containing tempo events</param>
        private static void PopulateTempoRow(DataGridView dgSong, TempoTrack tempoTrack)
        {
            // Store the track in the hidden data cell
            dgSong.Rows[SongGridManager.FIXED_ROW_TEMPO].Cells["colData"].Value = tempoTrack;

            // Clear all existing measure cells in the tempo row
            for (int colIndex = SongGridManager.MEASURE_START_COLUMN_INDEX; colIndex < dgSong.Columns.Count; colIndex++)
            {
                dgSong.Rows[SongGridManager.FIXED_ROW_TEMPO].Cells[colIndex].Value = string.Empty;
            }

            if (tempoTrack.Events.Count == 0)
                return;

            // Populate measure cells with BPM at the starting bar of each tempo event
            foreach (var tempoEvent in tempoTrack.Events)
            {
                // Convert 1-based bar number to 0-based measure index
                int measureIndex = tempoEvent.StartBar - 1;
                
                // Calculate the column index for this measure
                int columnIndex = SongGridManager.MEASURE_START_COLUMN_INDEX + measureIndex;

                // Ensure the column exists (dynamically add if needed)
                while (dgSong.Columns.Count <= columnIndex)
                {
                    int measureNumber = dgSong.Columns.Count - SongGridManager.MEASURE_START_COLUMN_INDEX + 1;
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

                // Set the BPM value in the appropriate measure cell
                if (columnIndex < dgSong.Columns.Count)
                {
                    dgSong.Rows[SongGridManager.FIXED_ROW_TEMPO].Cells[columnIndex].Value = tempoEvent.TempoBpm.ToString();
                }
            }
        }
        
        #endregion


    }
}