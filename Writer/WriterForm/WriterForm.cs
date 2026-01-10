// AI: purpose=WriterForm UI for designing and generating song tracks, wiring grid, playback, import/export, and command handlers.
// AI: invariants=_songContext and _writer may be null; Globals hold canonical persisted design; UI code assumes 1-based bars and grid fixed rows.
// AI: deps=Depends on SongGridManager, GridControlLinesManager, WriterFormTransform, Midi services, and Generator APIs; renaming these breaks form wiring.
// AI: perf=Many operations run on UI thread (generation, file IO via services); consider backgrounding long ops to avoid UI hangs.
// AI: breaking=Controls gbPartOptions, gbPitchOptions, gbRhythmOptions moved to OptionForm; references to those controls are broken.

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8605 // Unboxing a possibly null value.
#pragma warning disable CS8602 // Dereference of a possibly null reference.

using Music.Designer;
using Music.Generator;
using Music.MyMidi;
using MusicGen.Lyrics;

namespace Music.Writer
{
    public partial class WriterForm : Form
    {
        private SongContext? _songContext;
        private WriterFormData? _writer;

        // playback service (reused for multiple play calls)
        private MidiPlaybackService _midiPlaybackService;

        // MIDI I/O service for importing/exporting MIDI files
        private MidiIoService _midiIoService;

        private PlaybackProgressTracker? _progressTracker;

        // Event handlers instance
        private readonly WriterFormEventHandlers _eventHandlers = new();

        // Grid operations instance
        private readonly WriterFormGridOperations _gridOperations = new();

        //===========================   I N I T I A L I Z A T I O N   ===========================

        // AI: ctor initializes services, default UI selections, and captures initial WriterFormData once.
        public WriterForm()
        {
            InitializeComponent();

            // create playback service
            _midiPlaybackService = new MidiPlaybackService();

            // Initialize MIDI I/O service
            _midiIoService = new MidiIoService();

            // Window behavior similar to other forms
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;

            _songContext = Globals.SongContext;

            dgSong.DefaultCellStyle.ForeColor = Color.Black; // had trouble setting this in the forms designer
            dgSong.DefaultCellStyle.BackColor = Color.White;

            cbCommand.SelectedIndex = 1; // harmony groove sync test

            // Configure dgSong with MIDI instrument dropdown
            SongGridManager.ConfigureSongGridView(
                dgSong,
                dgSong_CellValueChanged,
                dgSong_CurrentCellDirtyStateChanged,
                _songContext);

            // ====================   T H I S   H A S   T O   B E   L A S T  !   =================

            // Capture form control values manually set in the form designer
            // This will only be done once, at form construction time.
            // TODO: Broken - controls moved to OptionForm, need to wire up data capture differently
            // var transform = new WriterFormTransform();
            // _writer ??= transform.CaptureFormData(...);
        }

        /// <summary>
        /// Commits the combo box edit immediately so CellValueChanged fires.
        /// </summary>
        private void dgSong_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            SongGridManager.HandleCurrentCellDirtyStateChanged(dgSong, sender, e);
        }

        /// <summary>
        /// Updates the PartTrack object's MidiProgramName when the user changes the instrument selection.
        /// </summary>
        private void dgSong_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            SongGridManager.HandleCellValueChanged(dgSong, sender, e);
        }

        // AI: dgSong_CellDoubleClick: opens editors for fixed rows (Voice, Section, Lyrics, Harmony, Groove, TimeSignature, Tempo).
        // AI: note=Each editor returns updated track/result which is then attached to grid via GridControlLinesManager.
        private void dgSong_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            // If the fixed Voice row was double-clicked, open the Voice selector and write back to the local _songContext
            if (e.RowIndex == SongGridManager.FIXED_ROW_VOICE)
            {
                if (_songContext == null)
                    _songContext = new SongContext();

                using var voiceForm = new Music.Designer.VoiceSelectorForm();

                // Initialize with existing voices from the designer
                var existingVoices = _songContext.Voices?.Voices?
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
                        _songContext.Voices ??= new Music.Generator.VoiceSet();
                        _songContext.Voices.Reset();

                        foreach (var kvp in selected)
                        {
                            var role = kvp.Value == "Select..." ? "" : kvp.Value;
                            _songContext.Voices.AddVoice(kvp.Key, role);
                        }
                    }
                }

                return;
            }

            // If the fixed Section row was double-clicked, open the Section editor and write back to the local _songContext
            if (e.RowIndex == SongGridManager.FIXED_ROW_SECTION)
            {
                if (_songContext == null)
                    _songContext = new SongContext();

                var initialSections = _songContext.SectionTrack;

                using var dlg = new Music.Designer.SectionEditorForm(initialSections);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _songContext.SectionTrack = dlg.ResultSections;
                    GridControlLinesManager.AttachsectionTrack(dgSong, _songContext.SectionTrack);
                }

                return;
            }

            // If the fixed Lyrics row was double-clicked, open the Lyrics editor and write back to the local _songContext
            if (e.RowIndex == SongGridManager.FIXED_ROW_LYRICS)
			{
                if (_songContext == null)
                    _songContext = new SongContext();

                var initialLyrics = _songContext.LyricTrack;

                using var dlg = new Music.Writer.LyricEditorForm(initialLyrics);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _songContext.LyricTrack = dlg.ResultTrack;
                    // TODO: Add GridControlLinesManager.AttachLyricTrack when ready to visualize
                }

                return;
            }

            // If the fixed Harmony row was double-clicked, open the Harmony editor and write back to the local _songContext
            if (e.RowIndex == SongGridManager.FIXED_ROW_HARMONY)
            {
                if (_songContext == null)
                    _songContext = new SongContext();

                var initialHarmony = _songContext.HarmonyTrack;

                using var dlg = new Music.Designer.HarmonyEditorForm(initialHarmony);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _songContext.HarmonyTrack = dlg.ResultTrack;
                    GridControlLinesManager.AttachharmonyTrack(dgSong, _songContext.HarmonyTrack);
                }

                return;
            }

            // If the fixed Groove row was double-clicked, open the Groove editor and write back to the local _songContext
            if (e.RowIndex == SongGridManager.FIXED_ROW_GROOVE)
            {
                if (_songContext == null)
                    _songContext = new SongContext();

                var initialGroove = _songContext.GrooveTrack;


                // TO DO - Groove endbar is always a 1 - doesnt seem to be used

                using var dlg = new Music.Designer.GrooveEditorForm(_songContext.BarTrack, initialGroove);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _songContext.GrooveTrack = dlg.ResultTrack;
                }

                return;
            }

            // If the fixed Time Signature row was double-clicked, open the Time Signature editor and write back to the local _songContext
            if (e.RowIndex == SongGridManager.FIXED_ROW_TIME_SIGNATURE)
            {
                if (_songContext == null)
                    _songContext = new SongContext();

                var initialTimeSignature = _songContext.Song.TimeSignatureTrack;

                using var dlg = new Music.Designer.TimingEditorForm(initialTimeSignature);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _songContext.Song.TimeSignatureTrack = dlg.ResultTrack;
                    GridControlLinesManager.AttachTimeSignatureTrack(dgSong, _songContext.Song.TimeSignatureTrack);

                    // Rebuild the bar track from the updated timing track
                    _songContext.BarTrack.RebuildFromTimingTrack(_songContext.Song.TimeSignatureTrack);
                }

                return;
            }

            // If the fixed Tempo row was double-clicked, open the Tempo editor and write back to the local _songContext
            if (e.RowIndex == SongGridManager.FIXED_ROW_TEMPO)
            {
                if (_songContext == null)
                    _songContext = new SongContext();

                var initialTempo = _songContext.Song.TempoTrack;

                using var dlg = new Music.Writer.TempoEditorForm(initialTempo);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _songContext.Song.TempoTrack = dlg.ResultTrack;
                    GridControlLinesManager.AttachTempoTrack(dgSong, _songContext.Song.TempoTrack);
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

        // AI: OnActivated: reloads globals into local state and reapplies Writer settings to controls.
        // AI: note=Do not overwrite local _songContext if Globals.SongContext==null to avoid losing in-form edits.
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            // Get from globals on the way in but not if null, would overwrite current state

            if (Globals.SongContext != null)
            {
                _songContext = Globals.SongContext;
            }
            if (Globals.Writer != null)
                _writer = Globals.Writer;

            // TODO: Broken - controls moved to OptionForm, need to wire up data apply differently
            // var transform = new WriterFormTransform();
            // transform.ApplyFormData(_writer, ...);
        }

        // Persist current control state whenever the form loses activation (user switches to another MDI child)
        // AI: OnDeactivate persists both SongContext and Writer snapshot back to Globals for cross-form continuity.
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);

            // Save on the way out
            Globals.SongContext = _songContext;
            // TODO: Broken - controls moved to OptionForm, need to wire up data capture differently
            // var transform = new WriterFormTransform();
            // _writer = Globals.Writer = transform.CaptureFormData(...);
        }

        //===============================   E V E N T S   ==============================

        private async void btnPlay_Click(object sender, EventArgs e)
        {
            await StartPlaybackWithMeasureTrackingAsync();
        }

        private void btnSetDesignTestScenarioD1_Click(object sender, EventArgs e)
        {
            _songContext ??= new SongContext();
            _eventHandlers.HandleSetDesignTestScenarioD1(dgSong, _songContext);
            Globals.SongContext = _songContext;
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            var command = cbCommand?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(command))
                return;

            switch (command)
            {
                case "Repeat Note":
                    // Open OptionForm to capture parameters for the Repeat Note command
                    using (var optionForm = new Music.Writer.OptionForm.OptionForm())
                    {
                        optionForm.ApplyWriterFormData(_writer);

                        if (optionForm.ShowDialog(this) == DialogResult.OK)
                        {
                            var formData = optionForm.CaptureWriterFormData();
                            _writer = formData;
                            Globals.Writer = _writer;
                            HandleRepeatNoteCommand.Execute(formData, dgSong);
                        }
                    }
                    break;

                    // THIS IS WORKING NOW!
                case "Write Test Song":
                    HandleCommandWriteTestSong.HandleWriteTestSong(_songContext, dgSong);
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

        // New Add button handler: add an empty track and select it.
        private void btnAddTrack_Click(object? sender, EventArgs e)
        {
            _gridOperations.HandleAddSongTrack(dgSong);
        }

        private void btnDeleteTracks_Click(object sender, EventArgs e)
        {
            _gridOperations.HandleDeleteSongTracks(dgSong);
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            _eventHandlers.HandleImport(dgSong, _midiIoService);
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            WriterFormEventHandlers.HandleExport(dgSong, _midiIoService);
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _midiPlaybackService.Stop();
            _progressTracker?.Stop();
            SongGridManager.ClearAllMeasureHighlights(dgSong);
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

        private void btnSaveDesign_Click(object sender, EventArgs e)
        {
            if (_songContext == null)
            {
                MessageBoxHelper.Show(
                    "Create a new design first.",
                    "No Design",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            //DesignerFileManager.SaveDesign(this, _songContext);
        }

        private void btnLoadDesign_Click(object sender, EventArgs e)
        {
            // var loaded = DesignerFileManager.LoadDesign(this, out bool success);
            // if (success && loaded != null)
            //{
            //    _songContext = loaded;

            //    // Refresh grid control lines with the newly loaded design
            //    SongGridManager.ConfigureSongGridView(
            //        dgSong,
            //        dgSong_CellValueChanged,
            //        dgSong_CurrentCellDirtyStateChanged,
            //        _songContext);
            //}
        }

        private void btnTestWordparser_Click(object sender, EventArgs e)
        {
            // Simple lookup
            var pronunciation = WordParser.Instance.TryLookup("singing");
            var syllables = pronunciation.GetStressSyllables(); // S[IH]NG<1> [IH]NG<0>

            // Find rhymes
            var rhymes = WordParser.Instance.GetRhymingWords("cat", 20);

            // Parse lyrics with phonetics
            var phrase = new LyricPhrase();
            LyricPhoneticsHelper.ParseTextToLyricPhrase(phrase, "I love music");
            LyricPhoneticsHelper.MarkBreathPoints(phrase);
        }

        private async Task StartPlaybackWithMeasureTrackingAsync()
        {
            System.Diagnostics.Debug.WriteLine("[WriterForm] StartPlaybackWithMeasureTrackingAsync: BEGIN");
            
            try
            {
                // Extract time signature track from fixed row
                var timeSignatureTrack = GridControlLinesManager.GetTimeSignatureTrack(dgSong);
                System.Diagnostics.Debug.WriteLine($"[WriterForm] TimeSignatureTrack: {(timeSignatureTrack == null ? "NULL" : $"{timeSignatureTrack.Events.Count} events")}");
                
                if (timeSignatureTrack == null || timeSignatureTrack.Events.Count == 0)
                {
                    MessageBoxHelper.Show(
                        "No time signature events defined. Please add at least one time signature event.",
                        "Missing Time Signature",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                _progressTracker?.Stop();
                _progressTracker?.Dispose();

                SongGridManager.ClearAllMeasureHighlights(dgSong);

                System.Diagnostics.Debug.WriteLine("[WriterForm] Creating PlaybackProgressTracker");
                _progressTracker = new PlaybackProgressTracker(_midiPlaybackService, timeSignatureTrack, pollIntervalMs: 50);
                _progressTracker.MeasureChanged += OnMeasureChanged;
                _progressTracker.Start();
                System.Diagnostics.Debug.WriteLine("[WriterForm] PlaybackProgressTracker started");

                await _eventHandlers.HandlePlayAsync(dgSong, _midiPlaybackService);
                System.Diagnostics.Debug.WriteLine("[WriterForm] HandlePlayAsync completed");
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine("[WriterForm] StartPlaybackWithMeasureTrackingAsync: FINALLY block");
                _progressTracker?.Stop();
                SongGridManager.ClearAllMeasureHighlights(dgSong);
            }
        }

        private void OnMeasureChanged(object? sender, MeasureChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[WriterForm] OnMeasureChanged: Prev={e.PreviousMeasure}, Current={e.CurrentMeasure}, Tick={e.CurrentTick}");
            
            if (e.PreviousMeasure > 0)
            {
                SongGridManager.ClearMeasureHighlight(dgSong, e.PreviousMeasure);
            }

            if (e.CurrentMeasure > 0)
            {
                SongGridManager.HighlightCurrentMeasure(dgSong, e.CurrentMeasure);
            }
        }
    }
}
#pragma warning restore CS8602 // Dereference of a possibly null reference.
