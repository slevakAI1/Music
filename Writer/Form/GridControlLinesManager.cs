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
        /// Public helper to attach a SectionTimeline instance to the fixed Section row's hidden data cell.
        /// Safe to call anytime after the grid's columns and rows have been created.
        /// </summary>
        /// <param name="dgvPhrase">Target DataGridView</param>
        /// <param name="sectionTimeline">SectionTimeline to store in the hidden data cell (null to skip)</param>
        public static void AttachSectionTimeline(DataGridView dgvPhrase, SectionTimeline? sectionTimeline)
        {
            if (sectionTimeline == null)
                return;

            if (!dgvPhrase.Columns.Contains("colData"))
                return;

            if (dgvPhrase.Rows.Count <= PhraseGridManager.FIXED_ROW_SECTION)
                return;

            // Populate the section row with the timeline data
            PopulateSectionRow(dgvPhrase, sectionTimeline);
        }

        /// <summary>
        /// Populates the fixed Section row with section numbers at their respective measure positions.
        /// </summary>
        /// <param name="dgvPhrase">Target DataGridView</param>
        /// <param name="sectionTimeline">SectionTimeline containing section events</param>
        private static void PopulateSectionRow(DataGridView dgvPhrase, SectionTimeline sectionTimeline)
        {
            // Store the timeline in the hidden data cell
            dgvPhrase.Rows[PhraseGridManager.FIXED_ROW_SECTION].Cells["colData"].Value = sectionTimeline;

            // Clear all existing measure cells in the section row
            for (int colIndex = PhraseGridManager.MEASURE_START_COLUMN_INDEX; colIndex < dgvPhrase.Columns.Count; colIndex++)
            {
                dgvPhrase.Rows[PhraseGridManager.FIXED_ROW_SECTION].Cells[colIndex].Value = string.Empty;
            }

            if (sectionTimeline.Sections.Count == 0)
                return;

            // Calculate total measures needed
            int totalMeasures = sectionTimeline.TotalBars;

            // Ensure we have enough columns for all measures
            int requiredColumns = PhraseGridManager.MEASURE_START_COLUMN_INDEX + totalMeasures;
            while (dgvPhrase.Columns.Count < requiredColumns)
            {
                int measureNumber = dgvPhrase.Columns.Count - PhraseGridManager.MEASURE_START_COLUMN_INDEX + 1;
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
                    int columnIndex = PhraseGridManager.MEASURE_START_COLUMN_INDEX + measureIndex;

                    // Set the section number in the appropriate measure cell
                    if (columnIndex < dgvPhrase.Columns.Count)
                    {
                        dgvPhrase.Rows[PhraseGridManager.FIXED_ROW_SECTION].Cells[columnIndex].Value = sectionNumber.ToString();
                    }
                }
            }
        }

        #endregion

        #region AttachTempoTimeline

        /// <summary>
        /// Public helper to attach a TempoTimeline instance to the fixed Tempo row's hidden data cell.
        /// Safe to call anytime after the grid's columns and rows have been created.
        /// </summary>
        /// <param name="dgvPhrase">Target DataGridView</param>
        /// <param name="tempoTimeline">TempoTimeline to store in the hidden data cell (null to skip)</param>
        public static void AttachTempoTimeline(DataGridView dgvPhrase, TempoTimeline? tempoTimeline)
        {
            if (tempoTimeline == null)
                return;

            if (!dgvPhrase.Columns.Contains("colData"))
                return;

            if (dgvPhrase.Rows.Count <= PhraseGridManager.FIXED_ROW_TEMPO)
                return;

            // Populate the tempo row with the timeline data
            PopulateTempoRow(dgvPhrase, tempoTimeline);
        }

        /// <summary>
        /// Populates the fixed Tempo row with BPM values at their respective measure positions.
        /// </summary>
        /// <param name="dgvPhrase">Target DataGridView</param>
        /// <param name="tempoTimeline">TempoTimeline containing tempo events</param>
        private static void PopulateTempoRow(DataGridView dgvPhrase, TempoTimeline tempoTimeline)
        {
            // Store the timeline in the hidden data cell
            dgvPhrase.Rows[PhraseGridManager.FIXED_ROW_TEMPO].Cells["colData"].Value = tempoTimeline;

            // Clear all existing measure cells in the tempo row
            for (int colIndex = PhraseGridManager.MEASURE_START_COLUMN_INDEX; colIndex < dgvPhrase.Columns.Count; colIndex++)
            {
                dgvPhrase.Rows[PhraseGridManager.FIXED_ROW_TEMPO].Cells[colIndex].Value = string.Empty;
            }

            if (tempoTimeline.Events.Count == 0)
                return;

            // Populate measure cells with BPM at the starting bar of each tempo event
            foreach (var tempoEvent in tempoTimeline.Events)
            {
                // Convert 1-based bar number to 0-based measure index
                int measureIndex = tempoEvent.StartBar - 1;
                
                // Calculate the column index for this measure
                int columnIndex = PhraseGridManager.MEASURE_START_COLUMN_INDEX + measureIndex;

                // Ensure the column exists (dynamically add if needed)
                while (dgvPhrase.Columns.Count <= columnIndex)
                {
                    int measureNumber = dgvPhrase.Columns.Count - PhraseGridManager.MEASURE_START_COLUMN_INDEX + 1;
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

                // Set the BPM value in the appropriate measure cell
                if (columnIndex < dgvPhrase.Columns.Count)
                {
                    dgvPhrase.Rows[PhraseGridManager.FIXED_ROW_TEMPO].Cells[columnIndex].Value = tempoEvent.TempoBpm.ToString();
                }
            }
        }
        
        #endregion


    }
}