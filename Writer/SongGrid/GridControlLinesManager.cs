using Music.Designer;

namespace Music.Writer
{
    /// <summary>
    /// Manages the grid control lines (fixed rows) such as Tempo row population.
    /// </summary>
    internal static class GridControlLinesManager
    {

        #region AttachSectionTimeline

        /// <summary>
        /// Public helper to attach a SectionTrack instance to the fixed Section row's hidden data cell.
        /// Safe to call anytime after the grid's columns and rows have been created.
        /// </summary>
        /// <param name="dgSong">Target DataGridView</param>
        /// <param name="sectionTimeline">SectionTrack to store in the hidden data cell (null to skip)</param>
        public static void AttachSectionTimeline(DataGridView dgSong, SectionTrack? sectionTimeline)
        {
            if (sectionTimeline == null)
                return;

            if (!dgSong.Columns.Contains("colData"))
                return;

            if (dgSong.Rows.Count <= SongGridManager.FIXED_ROW_SECTION)
                return;

            // Populate the section row with the timeline data
            PopulateSectionRow(dgSong, sectionTimeline);
        }

        /// <summary>
        /// Populates the fixed Section row with section numbers at their respective measure positions.
        /// </summary>
        /// <param name="dgSong">Target DataGridView</param>
        /// <param name="sectionTimeline">SectionTrack containing section events</param>
        private static void PopulateSectionRow(DataGridView dgSong, SectionTrack sectionTimeline)
        {
            // Store the timeline in the hidden data cell
            dgSong.Rows[SongGridManager.FIXED_ROW_SECTION].Cells["colData"].Value = sectionTimeline;

            // Clear all existing measure cells in the section row
            for (int colIndex = SongGridManager.MEASURE_START_COLUMN_INDEX; colIndex < dgSong.Columns.Count; colIndex++)
            {
                dgSong.Rows[SongGridManager.FIXED_ROW_SECTION].Cells[colIndex].Value = string.Empty;
            }

            if (sectionTimeline.Sections.Count == 0)
                return;

            // Calculate total measures needed
            int totalMeasures = sectionTimeline.TotalBars;

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
            for (int sectionIndex = 0; sectionIndex < sectionTimeline.Sections.Count; sectionIndex++)
            {
                var section = sectionTimeline.Sections[sectionIndex];
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

        #region AttachHarmonyTimeline

        /// <summary>
        /// Public helper to attach a HarmonyTrack instance to the fixed Harmony row's hidden data cell.
        /// Safe to call anytime after the grid's columns and rows have been created.
        /// </summary>
        /// <param name="dgSong">Target DataGridView</param>
        /// <param name="harmonyTimeline">HarmonyTrack to store in the hidden data cell (null to skip)</param>
        public static void AttachHarmonyTimeline(DataGridView dgSong, HarmonyTrack? harmonyTimeline)
        {
            if (harmonyTimeline == null)
                return;

            if (!dgSong.Columns.Contains("colData"))
                return;

            if (dgSong.Rows.Count <= SongGridManager.FIXED_ROW_HARMONY)
                return;

            // Populate the harmony row with the timeline data
            PopulateHarmonyRow(dgSong, harmonyTimeline);
        }

        /// <summary>
        /// Populates the fixed Harmony row with chord notation at their respective measure positions.
        /// Multiple chords per measure are separated by line breaks.
        /// </summary>
        /// <param name="dgSong">Target DataGridView</param>
        /// <param name="harmonyTimeline">HarmonyTrack containing harmony events</param>
        private static void PopulateHarmonyRow(DataGridView dgSong, HarmonyTrack harmonyTimeline)
        {
            // Store the timeline in the hidden data cell
            dgSong.Rows[SongGridManager.FIXED_ROW_HARMONY].Cells["colData"].Value = harmonyTimeline;

            // Clear all existing measure cells in the harmony row
            for (int colIndex = SongGridManager.MEASURE_START_COLUMN_INDEX; colIndex < dgSong.Columns.Count; colIndex++)
            {
                dgSong.Rows[SongGridManager.FIXED_ROW_HARMONY].Cells[colIndex].Value = string.Empty;
            }

            if (harmonyTimeline.Events.Count == 0)
                return;

            // Group harmony events by the bar they start in
            var eventsByBar = harmonyTimeline.Events
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

        #region AttachTimeSignatureTimeline

        /// <summary>
        /// Public helper to attach a TimeSignatureTrack instance to the fixed Time Signature row's hidden data cell.
        /// Safe to call anytime after the grid's columns and rows have been created.
        /// </summary>
        /// <param name="dgSong">Target DataGridView</param>
        /// <param name="timeSignatureTimeline">TimeSignatureTrack to store in the hidden data cell (null to skip)</param>
        public static void AttachTimeSignatureTimeline(DataGridView dgSong, TimeSignatureTrack? timeSignatureTimeline)
        {
            if (timeSignatureTimeline == null)
                return;

            if (!dgSong.Columns.Contains("colData"))
                return;

            if (dgSong.Rows.Count <= SongGridManager.FIXED_ROW_TIME_SIGNATURE)
                return;

            // Populate the time signature row with the timeline data
            PopulateTimeSignatureRow(dgSong, timeSignatureTimeline);
        }

        /// <summary>
        /// Populates the fixed Time Signature row with time signature values at their respective measure positions.
        /// </summary>
        /// <param name="dgSong">Target DataGridView</param>
        /// <param name="timeSignatureTimeline">TimeSignatureTrack containing time signature events</param>
        private static void PopulateTimeSignatureRow(DataGridView dgSong, TimeSignatureTrack timeSignatureTimeline)
        {
            // Store the timeline in the hidden data cell
            dgSong.Rows[SongGridManager.FIXED_ROW_TIME_SIGNATURE].Cells["colData"].Value = timeSignatureTimeline;

            // Clear all existing measure cells in the time signature row
            for (int colIndex = SongGridManager.MEASURE_START_COLUMN_INDEX; colIndex < dgSong.Columns.Count; colIndex++)
            {
                dgSong.Rows[SongGridManager.FIXED_ROW_TIME_SIGNATURE].Cells[colIndex].Value = string.Empty;
            }

            if (timeSignatureTimeline.Events.Count == 0)
                return;

            // Populate measure cells with time signature at the starting bar of each time signature event
            foreach (var timeSignatureEvent in timeSignatureTimeline.Events)
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

        #region AttachTempoTimeline

        /// <summary>
        /// Public helper to attach a TempoTrack instance to the fixed Tempo row's hidden data cell.
        /// Safe to call anytime after the grid's columns and rows have been created.
        /// </summary>
        /// <param name="dgSong">Target DataGridView</param>
        /// <param name="tempoTimeline">TempoTrack to store in the hidden data cell (null to skip)</param>
        public static void AttachTempoTimeline(DataGridView dgSong, TempoTrack? tempoTimeline)
        {
            if (tempoTimeline == null)
                return;

            if (!dgSong.Columns.Contains("colData"))
                return;

            if (dgSong.Rows.Count <= SongGridManager.FIXED_ROW_TEMPO)
                return;

            // Populate the tempo row with the timeline data
            PopulateTempoRow(dgSong, tempoTimeline);
        }

        /// <summary>
        /// Populates the fixed Tempo row with BPM values at their respective measure positions.
        /// </summary>
        /// <param name="dgSong">Target DataGridView</param>
        /// <param name="tempoTimeline">TempoTrack containing tempo events</param>
        private static void PopulateTempoRow(DataGridView dgSong, TempoTrack tempoTimeline)
        {
            // Store the timeline in the hidden data cell
            dgSong.Rows[SongGridManager.FIXED_ROW_TEMPO].Cells["colData"].Value = tempoTimeline;

            // Clear all existing measure cells in the tempo row
            for (int colIndex = SongGridManager.MEASURE_START_COLUMN_INDEX; colIndex < dgSong.Columns.Count; colIndex++)
            {
                dgSong.Rows[SongGridManager.FIXED_ROW_TEMPO].Cells[colIndex].Value = string.Empty;
            }

            if (tempoTimeline.Events.Count == 0)
                return;

            // Populate measure cells with BPM at the starting bar of each tempo event
            foreach (var tempoEvent in tempoTimeline.Events)
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