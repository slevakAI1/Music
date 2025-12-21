#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8605 // Unboxing a possibly null value.
#pragma warning disable CS8602 // Dereference of a possibly null reference.

using Music.Generator;
using Music.MyMidi;

namespace Music.Writer
{
    public partial class WriterForm : Form
    {
        private Designer.Designer? _designer;
        private WriterFormData? _writer;

        // playback service (reused for multiple play calls)
        private MidiPlaybackService _midiPlaybackService;

        // MIDI I/O service for importing/exporting MIDI files
        private MidiIoService _midiIoService;

        private int trackNumber = 0;

        // MIDI instrument list for dropdown
        private List<MidiVoices> _midiInstruments;

        // Event handlers instance
        private readonly WriterFormEventHandlers _eventHandlers = new();

        // Grid operations instance
        private readonly WriterFormGridOperations _gridOperations = new();

        //===========================   I N I T I A L I Z A T I O N   ===========================
        public WriterForm()
        {
            InitializeComponent();

            // create playback service
            _midiPlaybackService = new MidiPlaybackService();

            // Initialize MIDI I/O service
            _midiIoService = new MidiIoService();

            // Initialize MIDI instruments list
            _midiInstruments = MidiVoices.GetMidiVoices();

            // Window behavior similar to other forms
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;

            _designer = Globals.Designer;

            dgSong.DefaultCellStyle.ForeColor = Color.Black; // had trouble setting this in the forms designer
            dgSong.DefaultCellStyle.BackColor = Color.White;

            // Initialize comboboxes - doesn't seem to be a way to set a default in the designer or form.
            // The changes keep getting discarded. wtf?
            cbChordBase.SelectedIndex = 0; // C
            cbChordQuality.SelectedIndex = 0; // Major
            cbChordKey.SelectedIndex = 0; // C

            // Initialize staff selection - default to staff 1 checked
            if (clbStaffs != null && clbStaffs.Items.Count > 0)
                clbStaffs.SetItemChecked(0, true); // Check staff "1"

            cbCommand.SelectedIndex = 2; // harmony groove sync test

            // Configure dgSong with MIDI instrument dropdown
            SongGridManager.ConfigureSongGridView(
                dgSong,
                _midiInstruments,
                dgSong_CellValueChanged,
                dgSong_CurrentCellDirtyStateChanged,
                _designer);

            // ====================   T H I S   H A S   T O   B E   L A S T  !   =================

            // Capture form control values manually set in the form designer
            // This will only be done once, at form construction time.
            var transform = new WriterFormTransform();
            _writer ??= transform.CaptureFormData(
                cbCommand, clbParts, clbStaffs, rbIsRest, rbChord, cbStep,
                rbPitchAbsolute, rbPitchKeyRelative, cbAccidental, numOctaveAbs,
                numDegree, cbChordKey, numChordDegree, cbChordQuality, cbChordBase,
                cbNoteValue, numDots, txtTupletNumber, numTupletCount, numTupletOf, numNumberOfNotes);
        }

        /// <summary>
        /// Commits the combo box edit immediately so CellValueChanged fires.
        /// </summary>
        private void dgSong_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            SongGridManager.HandleCurrentCellDirtyStateChanged(dgSong, sender, e);
        }

        /// <summary>
        /// Updates the SongTrack object's MidiProgramName when the user changes the instrument selection.
        /// </summary>
        private void dgSong_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            SongGridManager.HandleCellValueChanged(dgSong, sender, e);
        }

        /// <summary>
        /// Opens a JSON viewer when the user double-clicks on the SongTrack column.
        /// </summary>
        private void dgSong_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            // If the fixed Voice row was double-clicked, open the Voice selector and write back to the local _designer
            if (e.RowIndex == SongGridManager.FIXED_ROW_VOICE)
            {
                if (_designer == null)
                    _designer = new Music.Designer.Designer();

                using var voiceForm = new Music.Designer.VoiceSelectorForm();

                // Initialize with existing voices from the designer
                var existingVoices = _designer.Voices?.Voices?
                    .Where(v => !string.IsNullOrWhiteSpace(v.VoiceName))
                    .ToDictionary(
                        v => v.VoiceName,
                        v => string.IsNullOrWhiteSpace(v.GrooveRole) ? "Select..." : v.GrooveRole,
                        StringComparer.OrdinalIgnoreCase);

                if (existingVoices?.Count > 0)
                {
                    voiceForm.SetExistingVoices(existingVoices);
                }

                if (voiceForm.ShowDialog(this) == DialogResult.OK)
                {
                    var selected = voiceForm.SelectedVoicesWithRoles;
                    if (selected?.Count > 0)
                    {
                        _designer.Voices ??= new Music.Designer.VoiceSet();
                        _designer.Voices.Reset();

                        foreach (var kvp in selected)
                        {
                            var role = kvp.Value == "Select..." ? "" : kvp.Value;
                            _designer.Voices.AddVoice(kvp.Key, role);
                        }
                    }
                }

                return;
            }

            // If the fixed Section row was double-clicked, open the Section editor and write back to the local _designer
            if (e.RowIndex == SongGridManager.FIXED_ROW_SECTION)
            {
                if (_designer == null)
                    _designer = new Music.Designer.Designer();

                var initialSections = _designer.SectionTrack;

                using var dlg = new Music.Designer.SectionEditorForm(initialSections);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _designer.SectionTrack = dlg.ResultSections;
                    GridControlLinesManager.AttachSectionTimeline(dgSong, _designer.SectionTrack);
                }

                return;
            }

            // If the fixed Harmony row was double-clicked, open the Harmony editor and write back to the local _designer
            if (e.RowIndex == SongGridManager.FIXED_ROW_HARMONY)
            {
                if (_designer == null)
                    _designer = new Music.Designer.Designer();

                var initialHarmony = _designer.HarmonyTrack;

                using var dlg = new Music.Designer.HarmonyEditorForm(initialHarmony);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _designer.HarmonyTrack = dlg.ResultTimeline;
                    GridControlLinesManager.AttachHarmonyTimeline(dgSong, _designer.HarmonyTrack);
                }

                return;
            }

            // If the fixed Time Signature row was double-clicked, open the Time Signature editor and write back to the local _designer
            if (e.RowIndex == SongGridManager.FIXED_ROW_TIME_SIGNATURE)
            {
                if (_designer == null)
                    _designer = new Music.Designer.Designer();

                var initialTimeSignature = _designer.TimeSignatureTrack;

                using var dlg = new Music.Designer.TimeSignatureEditorForm(initialTimeSignature);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _designer.TimeSignatureTrack = dlg.ResultTimeline;
                    GridControlLinesManager.AttachTimeSignatureTimeline(dgSong, _designer.TimeSignatureTrack);
                }

                return;
            }

            // If the fixed Tempo row was double-clicked, open the Tempo editor and write back to the local _designer
            if (e.RowIndex == SongGridManager.FIXED_ROW_TEMPO)
            {
                if (_designer == null)
                    _designer = new Music.Designer.Designer();

                var initialTempo = _designer.TempoTrack;

                using var dlg = new Music.Designer.TempoEditorForm(initialTempo);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _designer.TempoTrack = dlg.ResultTimeline;
                    GridControlLinesManager.AttachTempoTimeline(dgSong, _designer.TempoTrack);
                }

                return;
            }

            // Default behavior: delegate to existing track double-click handler
            _eventHandlers.HandleTrackDoubleClick(dgSong, e);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (this.MdiParent != null && this.WindowState != FormWindowState.Maximized)
                this.WindowState = FormWindowState.Maximized;
        }

        // The Writer form is activated each time it gains focus.
        // The initialization of controls is controlled entirely by the current Design and persisted Writer.
        // It does not depend on the prior state of the controls.
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            // Get from globals on the way in but not if null, would overwrite current state

            if (Globals.Designer != null)
            {
                _designer = Globals.Designer;
            }
            if (Globals.Writer != null)
                _writer = Globals.Writer;

            var transform = new WriterFormTransform();
            transform.ApplyFormData(_writer,
                cbCommand, clbParts, clbStaffs, rbIsRest, rbChord, cbStep,
                rbPitchAbsolute, rbPitchKeyRelative, cbAccidental, numOctaveAbs,
                numDegree, cbChordKey, numChordDegree, cbChordQuality, cbChordBase,
                cbNoteValue, numDots, txtTupletNumber, numTupletCount, numTupletOf, numNumberOfNotes);
        }

        // Persist current control state whenever the form loses activation (user switches to another MDI child)
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);

            // Save on the way out
            Globals.Designer = _designer;
            var transform = new WriterFormTransform();
            _writer = Globals.Writer = transform.CaptureFormData(
                cbCommand, clbParts, clbStaffs, rbIsRest, rbChord, cbStep,
                rbPitchAbsolute, rbPitchKeyRelative, cbAccidental, numOctaveAbs,
                numDegree, cbChordKey, numChordDegree, cbChordQuality, cbChordBase,
                cbNoteValue, numDots, txtTupletNumber, numTupletCount, numTupletOf, numNumberOfNotes);
            Globals.Writer = _writer;
        }

        //===============================   E V E N T S   ==============================

        private async void btnPlay_Click(object sender, EventArgs e)
        {
            await _eventHandlers.HandlePlayAsync(dgSong, _midiPlaybackService);
        }

        private void btnUpdateFormFromDesigner_Click(object sender, EventArgs e)
        {
            if (_writer != null && _designer != null)
            {
                _eventHandlers.HandleUpdateFormFromDesigner(_writer, _designer);
            }
        }

        private void btnSetDesignTestScenarioD1_Click(object sender, EventArgs e)
        {
            _designer ??= new Designer.Designer();
            _eventHandlers.HandleSetDesignTestScenarioD1(dgSong, _designer);

            Globals.Designer = _designer;  // TO DO ... currently test groove track is pulling from globals, should get from local
            // copy

        }

        private void btnSetWriterTestScenarioG1_Click(object sender, EventArgs e)
        {
            _writer = _eventHandlers.HandleSetWriterTestScenarioG1(_designer);
            var transform = new WriterFormTransform();
            transform.ApplyFormData(_writer,
                cbCommand, clbParts, clbStaffs, rbIsRest, rbChord, cbStep,
                rbPitchAbsolute, rbPitchKeyRelative, cbAccidental, numOctaveAbs,
                numDegree, cbChordKey, numChordDegree, cbChordQuality, cbChordBase,
                cbNoteValue, numDots, txtTupletNumber, numTupletCount, numTupletOf, numNumberOfNotes);
        }

        private void btnChordTest_Click(object sender, EventArgs e)
        {
            _eventHandlers.HandleChordTest(_designer);
        }

        private void btnExportToNotion_Click(object sender, EventArgs e)
        {
            _eventHandlers.HandleExportToNotion();
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            var command = cbCommand?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(command))
                return;

            // Capture form data once at the higher level and pass to command handlers
            var transform = new WriterFormTransform();
            var formData = transform.CaptureFormData(
                cbCommand, clbParts, clbStaffs, rbIsRest, rbChord, cbStep,
                rbPitchAbsolute, rbPitchKeyRelative, cbAccidental, numOctaveAbs,
                numDegree, cbChordKey, numChordDegree, cbChordQuality, cbChordBase,
                cbNoteValue, numDots, txtTupletNumber, numTupletCount, numTupletOf, numNumberOfNotes);

            switch (command)
            {

                // TO DO this is not running the note count per measure routine. call must have got lost!!!!

                case "Repeat Note":
                    HandleRepeatNoteCommand.Execute(formData, dgSong);
                    break;

                case "Harmony Sync Test":
                    CommandHarmonySyncTest.HandleHarmonySyncTest(dgSong, _midiInstruments, ref trackNumber);
                    break;

                case "Harmony Groove Sync Test":
                    CommandGrooveSyncTest.HandleGrooveSyncTest(dgSong, _midiInstruments, ref trackNumber);
                    break;

                // Other cases will be added here later.

                default:
                    // Do nothing - do not change this branch code ever
                    break;
            }
        }

        private void btnClearAll_Click(object sender, EventArgs e)
        {
            _gridOperations.HandleClearAll(dgSong);
        }

        private void btnNewScore_Click(object sender, EventArgs e)
        {
            _eventHandlers.HandleNewScore();
        }

        // New Add button handler: add an empty track and select it.
        private void btnAddTrack_Click(object? sender, EventArgs e)
        {
            _gridOperations.HandleAddSongTrack(dgSong, _midiInstruments, ref trackNumber);
        }

        private void btnDeleteTracks_Click(object sender, EventArgs e)
        {
            _gridOperations.HandleDeleteSongTracks(dgSong);
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            _eventHandlers.HandleImport(dgSong, _midiIoService, _midiInstruments, ref trackNumber);
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            WriterFormEventHandlers.HandleExport(dgSong, _midiIoService);
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _midiPlaybackService.Stop();
        }

        private void btnClearSelected_Click(object sender, EventArgs e)
        {
            _gridOperations.HandleClearSelected(dgSong);
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            // Delegate pause/resume behavior to the grid/operations file for consistency.
            _gridOperations.HandlePause(_midiPlaybackService);
        }
    }
}