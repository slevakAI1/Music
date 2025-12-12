using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Music.Designer;

namespace Music.Writer
{
    /// <summary>
    /// Manages the grid control lines (fixed rows) such as Tempo row population.
    /// </summary>
    internal static class GridControlLinesManager
    {
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
    }
}