using Music.Designer;
using Music.MyMidi;

namespace Music.Writer
{
    /// <summary>
    /// Event handler logic for WriterForm, now as a standalone class.
    /// Each method receives only the dependencies it actually needs.
    /// </summary>
    public class WriterFormEventHandlers
    {
        // ========== PLAYBACK & EXPORT EVENT HANDLERS ==========

        public async Task HandlePlayAsync(
            DataGridView dgSong,
            MidiPlaybackService midiPlaybackService)
        {
            // Check if there are any song tracks in the grid (excluding fixed rows)
            if (dgSong.Rows.Count <= SongGridManager.FIXED_ROWS_COUNT)
            {
                MessageBoxHelper.Show("No pitch events to play.", "Play", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Check if a song track is selected
            var hasTrackSelection = dgSong.SelectedRows
                .Cast<DataGridViewRow>()
                .Any(r => r.Index >= SongGridManager.FIXED_ROWS_COUNT);

            if (!hasTrackSelection)
            {
                MessageBoxHelper.Show("Please select a pitch event to play.", "Play", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Extract tempo timeline from fixed row
            var tempoRow = dgSong.Rows[SongGridManager.FIXED_ROW_TEMPO];
            var tempoTimeline = tempoRow.Cells["colData"].Value as Music.Designer.TempoTimeline;
            if (tempoTimeline == null || tempoTimeline.Events.Count == 0)
            {
                MessageBoxHelper.Show("No tempo events defined. Please add at least one tempo event.", "Missing Tempo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Extract time signature timeline from fixed row
            var timeSignatureRow = dgSong.Rows[SongGridManager.FIXED_ROW_TIME_SIGNATURE];
            var timeSignatureTimeline = timeSignatureRow.Cells["colData"].Value as Music.Designer.TimeSignatureTimeline;
            if (timeSignatureTimeline == null || timeSignatureTimeline.Events.Count == 0)
            {
                MessageBoxHelper.Show("No time signature events defined. Please add at least one time signature event.", "Missing Time Signature", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Build list of SongTrack from all selected songTrack rows (skip fixed rows)
            var songTracks = new List<SongTrack>();
            foreach (DataGridViewRow selectedRow in dgSong.SelectedRows)
            {
                // Skip fixed rows - they contain control line data, not songTracks
                if (selectedRow.Index < SongGridManager.FIXED_ROWS_COUNT)
                    continue;

                // Get the data object from the hidden column
                var dataObj = selectedRow.Cells["colData"].Value;
                
                // Validate that it's actually a SongTrack object (not null or wrong type)
                if (dataObj is not SongTrack songTrack)
                {
                    var eventNumber = selectedRow.Cells["colEventNumber"].Value?.ToString() ?? (selectedRow.Index + 1).ToString();
                    MessageBoxHelper.Show(
                        $"No track data for row #{eventNumber}. Please add or assign a track before playing.",
                        "Missing Track",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return; // Abort playback
                }

                // Validate songTrack has notes
                if (songTrack.SongTrackNoteEvents.Count == 0)
                {
                    var eventNumber = selectedRow.Cells["colEventNumber"].Value?.ToString() ?? (selectedRow.Index + 1).ToString();
                    MessageBoxHelper.Show(
                        $"No track data for row #{eventNumber}. Please add or assign a track before playing.",
                        "Missing Track",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return; // Abort playback
                }

                // Validate instrument cell value (may be DBNull or null)
                var instrObj = selectedRow.Cells["colType"].Value;
                if (instrObj == null || instrObj == DBNull.Value)
                {
                    var eventNumber = selectedRow.Cells["colEventNumber"].Value?.ToString() ?? (selectedRow.Index + 1).ToString();
                    MessageBoxHelper.Show(
                        $"No instrument selected for row #{eventNumber}. Please select an instrument before playing.",
                        "Missing Instrument",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return; // Abort playback
                }

                int programNumber = Convert.ToInt32(instrObj);
                if (programNumber == -1)  // -1 = placeholder "Select..." -> treat as missing selection
                {
                    var eventNumber = selectedRow.Cells["colEventNumber"].Value?.ToString() ?? (selectedRow.Index + 1).ToString();
                    MessageBoxHelper.Show(
                        $"No instrument selected for row #{eventNumber}. Please select an instrument before playing.",
                        "Missing Instrument",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return; // Abort playback
                }

                // Valid program number (0-127 or 255 for drums) – safe to cast now
                songTrack.MidiProgramNumber = (int)programNumber;
                songTracks.Add(songTrack);
            }

            // Verify we actually have songTracks to play
            if (songTracks.Count == 0)
            {
                MessageBoxHelper.Show("No valid track events selected to play.", "Play", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Consolidated conversion: songTracks -> midi document with tempo and time signature timelines
            var midiDoc = ConvertSongTracksToMidiSongDocument.Convert(
                songTracks,
                tempoTimeline,
                timeSignatureTimeline);

            await Player.PlayMidiFromSongTracksAsync(midiPlaybackService, midiDoc);
        }

        public static void HandleExport(
            DataGridView dgSong,
            MidiIoService midiIoService)
        {
            // Check if there are any songTrack rows in the grid (excluding fixed rows)
            if (dgSong.Rows.Count <= SongGridManager.FIXED_ROWS_COUNT)
            {
                MessageBoxHelper.Show("No pitch events to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Check if a songTrack row is selected
            var hasTrackSelection = dgSong.SelectedRows
                .Cast<DataGridViewRow>()
                .Any(r => r.Index >= SongGridManager.FIXED_ROWS_COUNT);

            if (!hasTrackSelection)
            {
                MessageBoxHelper.Show("Please select a pitch event to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Extract tempo timeline from fixed row
            var tempoRow = dgSong.Rows[SongGridManager.FIXED_ROW_TEMPO];
            var tempoTimeline = tempoRow.Cells["colData"].Value as Music.Designer.TempoTimeline;
            if (tempoTimeline == null || tempoTimeline.Events.Count == 0)
            {
                MessageBoxHelper.Show("No tempo events defined. Please add at least one tempo event.", "Missing Tempo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Extract time signature timeline from fixed row
            var timeSignatureRow = dgSong.Rows[SongGridManager.FIXED_ROW_TIME_SIGNATURE];
            var timeSignatureTimeline = timeSignatureRow.Cells["colData"].Value as Music.Designer.TimeSignatureTimeline;
            if (timeSignatureTimeline == null || timeSignatureTimeline.Events.Count == 0)
            {
                MessageBoxHelper.Show("No time signature events defined. Please add at least one time signature event.", "Missing Time Signature", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Build list of SongTrack from all selected songTrack rows (skip fixed rows)
            var tracks = new List<SongTrack>();
            foreach (DataGridViewRow selectedRow in dgSong.SelectedRows)
            {
                // Skip fixed rows - they contain control line data, not songTracks
                if (selectedRow.Index < SongGridManager.FIXED_ROWS_COUNT)
                    continue;

                // Get the data object from the hidden column
                var dataObj = selectedRow.Cells["colData"].Value;
                
                // Validate that it's actually a SongTrack object (not null or wrong type)
                if (dataObj is not SongTrack track)
                {
                    var eventNumber = selectedRow.Cells["colEventNumber"].Value?.ToString() ?? (selectedRow.Index + 1).ToString();
                    MessageBoxHelper.Show(
                        $"No track data for row #{eventNumber}. Please add or assign a track before exporting.",
                        "Missing Track",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return; // Abort export
                }

                // Validate songTrack has notes
                if (track.SongTrackNoteEvents.Count == 0)
                {
                    var eventNumber = selectedRow.Cells["colEventNumber"].Value?.ToString() ?? (selectedRow.Index + 1).ToString();
                    MessageBoxHelper.Show(
                        $"No track data for row #{eventNumber}. Please add or assign a track before exporting.",
                        "Missing Track",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return; // Abort export
                }

                // Validate instrument cell value (may be DBNull or null)
                var instrObj = selectedRow.Cells["colType"].Value;
                if (instrObj == null || instrObj == DBNull.Value)
                {
                    var eventNumber = selectedRow.Cells["colEventNumber"].Value?.ToString() ?? (selectedRow.Index + 1).ToString();
                    MessageBoxHelper.Show(
                        $"No instrument selected for row #{eventNumber}. Please select an instrument before exporting.",
                        "Missing Instrument",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return; // Abort export
                }

                int programNumber = Convert.ToInt32(instrObj);
                if (programNumber == -1)
                {
                    var eventNumber = selectedRow.Cells["colEventNumber"].Value?.ToString() ?? (selectedRow.Index + 1).ToString();
                    MessageBoxHelper.Show(
                        $"No instrument selected for row #{eventNumber}. Please select an instrument before exporting.",
                        "Missing Instrument",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return; // Abort export
                }

                // Preserve drum track indicator (255) or use selected program number
                track.MidiProgramNumber = (byte)programNumber;
                tracks.Add(track);
            }

            // Verify we actually have songTracks to export
            if (tracks.Count == 0)
            {
                MessageBoxHelper.Show("No valid tracks selected to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "MIDI files (*.mid)|*.mid|All files (*.*)|*.*",
                Title = "Export MIDI File",
                DefaultExt = "mid",
                AddExtension = true
            };

            if (sfd.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                // Consolidated conversion: songTracks -> midi document with tempo and time signature timelines
                var midiDoc = ConvertSongTracksToMidiSongDocument.Convert(
                    tracks,
                    tempoTimeline,
                    timeSignatureTimeline);

                // Export to file
                midiIoService.ExportToFile(sfd.FileName, midiDoc);

                MessageBoxHelper.Show(
                    $"Successfully exported to:\n{Path.GetFileName(sfd.FileName)}",
                    "Export Successful",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.Show($"Error exporting MIDI: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void HandleImport(
            DataGridView dgSong,
            MidiIoService midiIoService,
            List<MidiInstrument> midiInstruments,
            ref int trackNumber)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "MIDI files (*.mid;*.midi)|*.mid;*.midi|All files (*.*)|*.*",
                Title = "Import MIDI File",
                CheckFileExists = true
            };

            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                // Import the MIDI file using the existing service
                var midiDoc = midiIoService.ImportFromFile(ofd.FileName);

                // Resolve project root (same approach used elsewhere in the solution)
                var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
                var debugDir = Path.Combine(projectRoot, "Files", "Debug");
                Directory.CreateDirectory(debugDir);

                // Convert MIDI document to lists of MetaMidiEvent objects
                List<List<MetaMidiEvent>> midiEventLists;
                try
                {
                    midiEventLists = ConvertMidiSongDocumentToMidiEventLists.Convert(midiDoc);
                }
                catch (NotSupportedException ex)
                {
                    // Show detailed error about unsupported MIDI event
                    MessageBoxHelper.Show(
                        ex.Message,
                        "Unsupported MIDI Event",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                // Extract ticks per quarter note from the MIDI file
                short ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote; // Default
                if (midiDoc.Raw.TimeDivision is Melanchall.DryWetMidi.Core.TicksPerQuarterNoteTimeDivision tpqn)
                {
                    ticksPerQuarterNote = tpqn.TicksPerQuarterNote;
                }

                // Extract tempo and time signature events from MIDI and attach to grid
                var (tempoTimeline, timeSignatureTimeline) = ExtractTimelinesFromMidiEvents(
                    midiEventLists, 
                    ticksPerQuarterNote);

                if (tempoTimeline != null && tempoTimeline.Events.Count > 0)
                {
                    GridControlLinesManager.AttachTempoTimeline(dgSong, tempoTimeline);
                }

                if (timeSignatureTimeline != null && timeSignatureTimeline.Events.Count > 0)
                {
                    GridControlLinesManager.AttachTimeSignatureTimeline(dgSong, timeSignatureTimeline);
                }

                // Convert MetaMidiEvent lists to SongTrack objects, passing the source ticks per quarter note
                var tracks = ConvertMidiEventListsToSongTracks.Convert(
                    midiEventLists,
                    midiInstruments,
                    ticksPerQuarterNote);

                // Add each songTrack to the grid
                foreach (var track in tracks)
                {
                    SongGridManager.AddSongTrackToGrid(
                        track,
                        midiInstruments,
                        dgSong,
                        ref trackNumber);
                }

                MessageBoxHelper.Show(
                    $"Successfully imported {tracks.Count} track(s) from:\n{Path.GetFileName(ofd.FileName)}",
                    "Import Successful",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.Show(
                    $"Error importing MIDI file:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Import Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Extracts tempo and time signature events from MIDI event lists and converts them to timelines.
        /// </summary>
        private static (TempoTimeline?, TimeSignatureTimeline?) ExtractTimelinesFromMidiEvents(
            List<List<MetaMidiEvent>> midiEventLists,
            short ticksPerQuarterNote)
        {
            var tempoTimeline = new TempoTimeline();
            var timeSignatureTimeline = new TimeSignatureTimeline();

            // Assume 4/4 time signature initially for bar calculation
            int beatsPerBar = 4;
            int ticksPerBeat = ticksPerQuarterNote;

            // First pass: extract time signatures to determine beatsPerBar for bar calculations
            foreach (var trackEvents in midiEventLists)
            {
                foreach (var evt in trackEvents.Where(e => e.Type == MidiEventType.TimeSignature))
                {
                    if (evt.Parameters.TryGetValue("Numerator", out var numObj) &&
                        evt.Parameters.TryGetValue("Denominator", out var denObj))
                    {
                        int numerator = Convert.ToInt32(numObj);
                        int denominator = Convert.ToInt32(denObj);

                        // Calculate bar position (1-based)
                        int ticksPerBar = ticksPerQuarterNote * beatsPerBar;
                        int bar = (int)(evt.AbsoluteTimeTicks / ticksPerBar) + 1;
                        int beatInBar = (int)((evt.AbsoluteTimeTicks % ticksPerBar) / ticksPerBeat) + 1;

                        var timeSignatureEvent = new TimeSignatureEvent
                        {
                            StartBar = bar,
                            StartBeat = beatInBar,
                            Numerator = numerator,
                            Denominator = denominator
                        };

                        timeSignatureTimeline.Add(timeSignatureEvent);

                        // Update beatsPerBar for subsequent calculations
                        beatsPerBar = numerator;
                    }
                }
            }

            // Second pass: extract tempo events using the time signature info
            foreach (var trackEvents in midiEventLists)
            {
                foreach (var evt in trackEvents.Where(e => e.Type == MidiEventType.SetTempo))
                {
                    int bpm = 120; // Default

                    if (evt.Parameters.TryGetValue("BPM", out var bpmObj))
                    {
                        bpm = Convert.ToInt32(bpmObj);
                    }
                    else if (evt.Parameters.TryGetValue("MicrosecondsPerQuarterNote", out var microObj))
                    {
                        int microseconds = Convert.ToInt32(microObj);
                        bpm = (int)Math.Round(60_000_000.0 / microseconds);
                    }

                    // Calculate bar position (1-based)
                    int ticksPerBar = ticksPerQuarterNote * beatsPerBar;
                    int bar = (int)(evt.AbsoluteTimeTicks / ticksPerBar) + 1;
                    int beatInBar = (int)((evt.AbsoluteTimeTicks % ticksPerBar) / ticksPerQuarterNote) + 1;

                    var tempoEvent = new TempoEvent
                    {
                        StartBar = bar,
                        StartBeat = beatInBar,
                        TempoBpm = bpm
                    };

                    tempoTimeline.Add(tempoEvent);
                }
            }

            return (
                tempoTimeline.Events.Count > 0 ? tempoTimeline : null,
                timeSignatureTimeline.Events.Count > 0 ? timeSignatureTimeline : null
            );
        }

        // ========== DESIGNER & FORM SYNC EVENT HANDLERS ==========

        public void HandleUpdateFormFromDesigner(
            WriterFormData writer,
            Designer.Designer designer)
        {
            // Update the form to take into account any changes to Designer
            UpdateWriterFormDataFromDesigner.Update(writer, designer);
        }

        // ========== TEST SCENARIO EVENT HANDLERS ==========

        public void HandleSetDesignTestScenarioD1(
            DataGridView dgSong,
            Designer.Designer designer)
        {
            DesignerTests.SetTestDesignD1(designer);

            GridControlLinesManager.AttachSectionTimeline(dgSong, designer.SectionTimeline);
            GridControlLinesManager.AttachTimeSignatureTimeline(dgSong, designer.TimeSignatureTimeline);
            GridControlLinesManager.AttachTempoTimeline(dgSong, designer.TempoTimeline);
            GridControlLinesManager.AttachHarmonyTimeline(dgSong, designer.HarmonyTimeline);
        }

        public WriterFormData HandleSetWriterTestScenarioG1(Designer.Designer designer)
        {
            return WriterFormTests.SetTestWriterG1(designer);
        }

        public void HandleChordTest(Designer.Designer? designer)
        {
            if (designer?.HarmonyTimeline == null || designer.HarmonyTimeline.Events.Count == 0)
            {
                MessageBoxHelper.Show(
                    "No harmony events available in the current design.",
                    "Chord Test",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var harmonyEvent = designer.HarmonyTimeline.Events[1];

            List<SongTrackNoteEvent> notes;
            try
            {
                notes = ConvertHarmonyEventToListOfPartNoteEvents.Convert(
                    harmonyEvent.Key,
                    harmonyEvent.Degree,
                    harmonyEvent.Quality,
                    harmonyEvent.Bass,
                    baseOctave: 4);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.Show(
                    $"Failed to build chord: {ex.Message}",
                    "Chord Test",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (notes == null || notes.Count == 0)
            {
                MessageBoxHelper.Show(
                    "Chord conversion returned no notes.",
                    "Chord Test",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var lines = new List<string>();
            foreach (var note in notes)
            {
                var accidental = note.Alter switch
                {
                    1 => "#",
                    -1 => "b",
                    _ => ""
                };
                lines.Add($"{note.Step}{accidental} {note.Octave}");
            }

            var title = $"Chord: {harmonyEvent.Key} (Deg {harmonyEvent.Degree}, {harmonyEvent.Quality})";
            MessageBoxHelper.Show(
                string.Join(Environment.NewLine, lines),
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // ========== SCORE EVENT HANDLERS ==========

        public void HandleExportToNotion()
        {
            // Implementation placeholder
        }

        public void HandleNewScore()
        {
            // Implementation placeholder
        }

        // ========== GRID CELL EVENT HANDLERS ==========

        public void HandleTrackDoubleClick(DataGridView dgSong, DataGridViewCellEventArgs e)
        {
            // Skip fixed rows
            if (e.RowIndex < SongGridManager.FIXED_ROWS_COUNT)
                return;

            // Allow double-click on any measure column (starting from MEASURE_START_COLUMN_INDEX)
            if (e.ColumnIndex < SongGridManager.MEASURE_START_COLUMN_INDEX)
                return;

            var row = dgSong.Rows[e.RowIndex];
            var trackData = row.Cells["colData"].Value;

            // Validate that we have a SongTrack object
            if (trackData is not SongTrack songTrack)
            {
                MessageBoxHelper.Show(
                    "No track data available for this row.",
                    "View Track",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Get the songTrack number for the dialog title
            var trackNumber = row.Cells["colEventNumber"].Value?.ToString() ?? (e.RowIndex + 1).ToString();

            // Open the JSON viewer dialog
            using var viewer = new SongTrackViewer(songTrack, trackNumber);
            viewer.ShowDialog();
        }
    }
}