// AI: purpose=Manage fixed grid control lines (Section/Harmony/TimeSignature/Tempo rows) and attach their track data to the grid.
// AI: invariants=Fixed rows use hidden "colData" cell to store track objects; columns represent measures starting at MEASURE_START_COLUMN_INDEX.
// AI: deps=Consumers rely on SongGridManager constants and Music.Generator track shapes; changing names breaks many callers.
// AI: change=If adding track types update attach helpers and any grid population logic to keep UI in sync.

using Music.Designer;
using Music.Generator;

namespace Music.Writer
{
    internal static class GridControlLinesManager
    {
        // AI: AttachsectionTrack: safe no-op when sectionTrack null or grid not yet configured.
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

        // AI: PopulateSectionRow: writes SectionTrack into hidden cell and places section names at start-bar columns.
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

            // Populate measure cells with section names only at each section start
            for (int sectionIndex = 0; sectionIndex < sectionTrack.Sections.Count; sectionIndex++)
            {
                var section = sectionTrack.Sections[sectionIndex];

                // Convert 1-based start bar to 0-based measure index
                int measureIndex = (section.StartBar > 0 ? section.StartBar : 1) - 1;

                // Calculate the column index for this measure
                int columnIndex = SongGridManager.MEASURE_START_COLUMN_INDEX + measureIndex;

                // Use section name when present; otherwise fall back to section type or index
                string text = !string.IsNullOrWhiteSpace(section.Name)
                    ? section.Name!
                    : section.SectionType.ToString();

                // Set the section name only at the start bar's cell
                if (columnIndex < dgSong.Columns.Count)
                {
                    dgSong.Rows[SongGridManager.FIXED_ROW_SECTION].Cells[columnIndex].Value = text;
                }
            }
        }

        // AI: AttachharmonyTrack: safe no-op when harmonyTrack null or grid not configured.
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

        // AI: PopulateHarmonyRow: groups events by StartBar and writes chord notation into measure cells, adding columns as needed.
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

        // AI: AttachTimeSignatureTrack: safe no-op guard and then populate fixed row.
        public static void AttachTimeSignatureTrack(DataGridView dgSong, Timingtrack? timeSignatureTrack)
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

        // AI: PopulateTimeSignatureRow: writes "N/D" at the StartBar column for each time signature event.
        private static void PopulateTimeSignatureRow(DataGridView dgSong, Timingtrack timeSignatureTrack)
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
                // StartBar is 1-based, so bar 1 should map to column MEASURE_START_COLUMN_INDEX
                int columnIndex = SongGridManager.MEASURE_START_COLUMN_INDEX + (timeSignatureEvent.StartBar - 1);

                // Set the time signature value in the appropriate measure cell
                if (columnIndex >= 0 && columnIndex < dgSong.Columns.Count)
                {
                    dgSong.Rows[SongGridManager.FIXED_ROW_TIME_SIGNATURE].Cells[columnIndex].Value = 
                        $"{timeSignatureEvent.Numerator}/{timeSignatureEvent.Denominator}";
                }
            }
        }

        // AI: AttachTempoTrack: guard then populate fixed tempo row.
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

        // AI: PopulateTempoRow: writes BPM string at StartBar column for each tempo event; adds columns dynamically.
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

        // AI: GetTimeSignatureTrack: returns Timingtrack stored in fixed row hidden cell or null when absent.
        public static Timingtrack? GetTimeSignatureTrack(DataGridView dgSong)
        {
            if (!dgSong.Columns.Contains("colData"))
                return null;

            if (dgSong.Rows.Count <= SongGridManager.FIXED_ROW_TIME_SIGNATURE)
                return null;

            return dgSong.Rows[SongGridManager.FIXED_ROW_TIME_SIGNATURE].Cells["colData"].Value as Timingtrack;
        }
    }
}