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
using System.Text;

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

            cbCommand.SelectedIndex = 2; // new groove test command

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
                //if (_songContext == null)
                //    _songContext = new SongContext();

                //var initialGroove = _songContext.GrooveTrack;


                //// TO DO - Groove endbar is always a 1 - doesnt seem to be used

                //using var dlg = new Music.Designer.GrooveEditorForm(_songContext.BarTrack, initialGroove);
                //if (dlg.ShowDialog(this) == DialogResult.OK)
                //{
                //    _songContext.GrooveTrack = dlg.ResultTrack;
                //}

                //return;
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
                    _songContext.BarTrack.RebuildFromTimingTrack(
                        _songContext.Song.TimeSignatureTrack,
                        _songContext.SectionTrack);
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

                case "Groove Test": // Play groove instance as a test song
                    HandleCommandGrooveTest.HandleGrooveTest(_songContext, dgSong);
                    break;

                case "Phrase Test":
                    HandleCommandCreateDrumPhrase.HandleDrumPhraseTest(_songContext, dgSong);
                    break;

                case "Drum Track Test":
                    HandleCommandDrumTrackTest.HandleDrumTrackTest(_songContext, dgSong);
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
            // Pause/resume MIDI playback
            _gridOperations.HandlePause(_midiPlaybackService);

            // Stop tracker during pause (simplest consistent behavior)
            if (_midiPlaybackService.IsPaused)
            {
                _progressTracker?.Stop();
            }
            else if (_midiPlaybackService.IsPlaying)
            {
                // Resume was triggered - restart tracker if we have time signature track
                var timeSignatureTrack = GridControlLinesManager.GetTimeSignatureTrack(dgSong);
                if (timeSignatureTrack != null && timeSignatureTrack.Events.Count > 0)
                {
                    _progressTracker?.Stop();
                    _progressTracker = new PlaybackProgressTracker(_midiPlaybackService, timeSignatureTrack, pollIntervalMs: 50);
                    _progressTracker.MeasureChanged += OnMeasureChanged;
                    _progressTracker.Start();
                }
            }
        }

        // AI: btnSendMidi_Click: lets user pick an external MIDI output device and sends selected tracks to it.
        // AI: reuses HandlePlayAsync validation+conversion logic but targets a user-chosen device (e.g., USB-MIDI to DAW).
        private async void btnSendMidi_Click(object sender, EventArgs e)
        {
            try
            {
                await SendToExternalMidiAsync();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.Show(
                    $"Failed to send MIDI: {ex.Message}",
                    "Send MIDI Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async Task SendToExternalMidiAsync()
        {
            var devices = _midiPlaybackService.EnumerateOutputDevices().ToList();
            if (devices.Count == 0)
            {
                MessageBoxHelper.Show(
                    "No MIDI output devices found on this system.",
                    "Send MIDI",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string? selectedDevice;
            using (var selector = new MyMidi.MidiDeviceSelectorDialog(devices))
            {
                if (selector.ShowDialog(this) != DialogResult.OK)
                    return;

                selectedDevice = selector.SelectedDeviceName;
            }

            if (string.IsNullOrWhiteSpace(selectedDevice))
                return;

            // Validate grid has tracks beyond fixed rows
            if (dgSong.Rows.Count <= SongGridManager.FIXED_ROWS_COUNT)
            {
                MessageBoxHelper.Show("No tracks to send.", "Send MIDI", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var hasTrackSelection = dgSong.SelectedRows
                .Cast<DataGridViewRow>()
                .Any(r => r.Index >= SongGridManager.FIXED_ROWS_COUNT);

            if (!hasTrackSelection)
            {
                MessageBoxHelper.Show("Please select one or more tracks to send.", "Send MIDI", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Extract tempo track
            var tempoRow = dgSong.Rows[SongGridManager.FIXED_ROW_TEMPO];
            var tempoTrack = tempoRow.Cells["colData"].Value as Music.Generator.TempoTrack;
            if (tempoTrack == null || tempoTrack.Events.Count == 0)
            {
                MessageBoxHelper.Show("No tempo events defined.", "Missing Tempo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Extract time signature track
            var timeSignatureRow = dgSong.Rows[SongGridManager.FIXED_ROW_TIME_SIGNATURE];
            var timeSignatureTrack = timeSignatureRow.Cells["colData"].Value as Music.Generator.Timingtrack;
            if (timeSignatureTrack == null || timeSignatureTrack.Events.Count == 0)
            {
                MessageBoxHelper.Show("No time signature events defined.", "Missing Time Signature", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Collect selected PartTracks
            var songTracks = new List<Music.Generator.PartTrack>();
            foreach (DataGridViewRow selectedRow in dgSong.SelectedRows)
            {
                if (selectedRow.Index < SongGridManager.FIXED_ROWS_COUNT)
                    continue;

                if (selectedRow.Cells["colData"].Value is not Music.Generator.PartTrack songTrack
                    || songTrack.PartTrackNoteEvents.Count == 0)
                {
                    continue;
                }

                var instrObj = selectedRow.Cells["colType"].Value;
                if (instrObj == null || instrObj == DBNull.Value)
                    continue;

                int programNumber = Convert.ToInt32(instrObj);
                if (programNumber == -1)
                    continue;

                songTrack.MidiProgramNumber = programNumber;
                songTracks.Add(songTrack);
            }

            if (songTracks.Count == 0)
            {
                MessageBoxHelper.Show("No valid tracks selected to send.", "Send MIDI", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Convert to MIDI document
            var midiDoc = ConvertPartTracksToMidiSongDocument_For_Play_And_Export.Convert(
                songTracks,
                tempoTrack,
                timeSignatureTrack);

            if (midiDoc == null)
            {
                MessageBoxHelper.Show("MIDI conversion failed.", "Send MIDI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Stop any current playback and send to the chosen device
            _midiPlaybackService.PlayToDevice(midiDoc, selectedDevice);

            MessageBoxHelper.Show(
                $"Sending {songTracks.Count} track(s) to '{selectedDevice}'\n" +
                $"Duration: {midiDoc.Duration.TotalSeconds:F1}s | Events: {midiDoc.EventCount} | Tracks: {midiDoc.TrackCount}",
                "Send MIDI - Started",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            // Wait for playback duration then release resources
            var cancellationToken = _midiPlaybackService.GetCancellationToken();
            var totalDelay = midiDoc.Duration.TotalMilliseconds + 250;

            if (totalDelay > 0)
            {
                try
                {
                    await Task.Delay((int)Math.Min(totalDelay, int.MaxValue), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            _midiPlaybackService.Stop();
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
            var phraseText = "I love music";
            LyricPhoneticsHelper.ParseTextToLyricPhrase(phrase, phraseText);
            LyricPhoneticsHelper.MarkBreathPoints(phrase);
            LyricPhoneticsHelper.MarkRhymeGroups(new List<LyricPhrase> { phrase });

            var reportText = BuildLyricPhraseReport(phrase, phraseText);
            ShowLyricPhraseReport(reportText);
        }

        private static string BuildLyricPhraseReport(LyricPhrase phrase, string sourceText)
        {
            ArgumentNullException.ThrowIfNull(phrase);

            var report = new StringBuilder();
            report.AppendLine("Lyric Phonetics Report");
            report.AppendLine("=======================");
            report.AppendLine($"Source Text: {sourceText}");
            report.AppendLine($"Raw Text: {phrase.RawText}");
            report.AppendLine($"Words: {phrase.Words.Count}");
            report.AppendLine($"Syllables: {phrase.Syllables.Count}");
            report.AppendLine($"SectionId: {phrase.SectionId ?? "(none)"}");
            report.AppendLine($"StartTime: {phrase.StartTime?.Ticks.ToString() ?? "(none)"}");
            report.AppendLine($"DurationBudget: {FormatDurationBudget(phrase.DurationBudget)}");
            report.AppendLine();

            for (int wordIndex = 0; wordIndex < phrase.Words.Count; wordIndex++)
            {
                var word = phrase.Words[wordIndex];
                report.AppendLine($"Word {wordIndex + 1}: \"{word.Text}\"");
                report.AppendLine($"  IsPunctuation: {word.IsPunctuation}");

                if (word.Syllables.Count == 0)
                {
                    report.AppendLine("  Syllables: (none)");
                    report.AppendLine();
                    continue;
                }

                for (int syllableIndex = 0; syllableIndex < word.Syllables.Count; syllableIndex++)
                {
                    var syllable = word.Syllables[syllableIndex];
                    report.AppendLine($"  Syllable {syllableIndex + 1}: {syllable.Text}");
                    report.AppendLine($"    Id: {syllable.Id}");
                    report.AppendLine($"    Stress: {syllable.Stress}");
                    report.AppendLine($"    Emphasis: {syllable.Emphasis}");
                    report.AppendLine($"    BreathAfter: {syllable.BreathAfter}");
                    report.AppendLine($"    RhymeGroup: {syllable.RhymeGroup ?? "(none)"}");
                    report.AppendLine($"    AnchorTime: {syllable.AnchorTime?.Ticks.ToString() ?? "(none)"}");
                    report.AppendLine($"    ConsonantTiming: {FormatConsonantTiming(syllable.ConsonantTiming)}");
                    report.AppendLine($"    Melisma: {FormatMelisma(syllable.Melisma)}");
                    report.AppendLine($"    Phones: {FormatPhones(syllable.Phones)}");
                }

                report.AppendLine();
            }

            return report.ToString();
        }

        private static string FormatDurationBudget(TickSpanConstraint? budget)
        {
            if (budget == null)
                return "(none)";

            var min = budget.MinTicks?.ToString() ?? "-";
            var target = budget.TargetTicks?.ToString() ?? "-";
            var max = budget.MaxTicks?.ToString() ?? "-";

            return $"Min={min}, Target={target}, Max={max}, Weight={budget.Weight}";
        }

        private static string FormatConsonantTiming(ConsonantTimingHints timing)
        {
            ArgumentNullException.ThrowIfNull(timing);

            return $"LeadInTicks={timing.LeadInTicks}, TailOutTicks={timing.TailOutTicks}";
        }

        private static string FormatMelisma(MelismaConstraint melisma)
        {
            ArgumentNullException.ThrowIfNull(melisma);

            return $"MinNotes={melisma.MinNotes}, MaxNotes={melisma.MaxNotes}, PreferMelisma={melisma.PreferMelisma}";
        }

        private static string FormatPhones(SyllablePhones phones)
        {
            ArgumentNullException.ThrowIfNull(phones);

            var onset = phones.Onset.Count == 0 ? "-" : string.Join(" ", phones.Onset);
            var nucleus = phones.Nucleus.Count == 0 ? "-" : string.Join(" ", phones.Nucleus);
            var coda = phones.Coda.Count == 0 ? "-" : string.Join(" ", phones.Coda);

            return $"Onset=[{onset}] Nucleus=[{nucleus}] Coda=[{coda}] VariantId={phones.VariantId ?? "(none)"}";
        }

        private void ShowLyricPhraseReport(string reportText)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(reportText);

            using var reportForm = new Form();
            using var reportFont = new Font(FontFamily.GenericMonospace, 9f);
            var reportBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = reportFont,
                Text = reportText
            };

            reportForm.Text = "Lyric Phonetics Report";
            reportForm.StartPosition = FormStartPosition.CenterParent;
            reportForm.Size = new Size(800, 600);
            reportForm.MinimizeBox = false;
            reportForm.MaximizeBox = false;
            reportForm.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            reportForm.Controls.Add(reportBox);
            reportForm.ShowDialog(this);
        }

        private async Task StartPlaybackWithMeasureTrackingAsync()
        {
            try
            {
                // Extract time signature track from fixed row
                var timeSignatureTrack = GridControlLinesManager.GetTimeSignatureTrack(dgSong);

                // If no time signature track, proceed with playback but without measure tracking
                bool enableTracking = timeSignatureTrack != null && timeSignatureTrack.Events.Count > 0;

                if (!enableTracking)
                {
                    await _eventHandlers.HandlePlayAsync(dgSong, _midiPlaybackService);
                    return;
                }

                _progressTracker?.Stop();
                _progressTracker?.Dispose();

                SongGridManager.ClearAllMeasureHighlights(dgSong);

                _progressTracker = new PlaybackProgressTracker(_midiPlaybackService, timeSignatureTrack, pollIntervalMs: 50);
                _progressTracker.MeasureChanged += OnMeasureChanged;
                _progressTracker.Start();

                await _eventHandlers.HandlePlayAsync(dgSong, _midiPlaybackService);
            }
            catch (Exception ex)
            {
                // If measure tracking setup fails, try playback without tracking
                try
                {
                    await _eventHandlers.HandlePlayAsync(dgSong, _midiPlaybackService);
                }
                catch
                {
                    // Let playback errors propagate normally
                    throw;
                }
            }
            finally
            {
                _progressTracker?.Stop();
                SongGridManager.ClearAllMeasureHighlights(dgSong);
            }
        }

        private void OnMeasureChanged(object? sender, MeasureChangedEventArgs e)
        {
            try
            {
                if (e.PreviousMeasure > 0)
                {
                    SongGridManager.ClearMeasureHighlight(dgSong, e.PreviousMeasure);
                }

                if (e.CurrentMeasure > 0)
                {
                    SongGridManager.HighlightCurrentMeasure(dgSong, e.CurrentMeasure);
                }
            }
            catch (Exception)
            {
                // Silently fail to avoid crashing the UI - measure tracking is non-critical
            }
        }

        private void btnSavePhrase_Click(object sender, EventArgs e)
        {
            _gridOperations.HandleSaveSelectedPhrases(dgSong, _songContext);

            if (_songContext != null)
                Globals.SongContext = _songContext;
        }

        private void btnBassPhrase_Click(object sender, EventArgs e)
        {
            HandleCommandCreateBassPhrase.HandleBassPhraseTest(_songContext, dgSong);
        }
    }
}
#pragma warning restore CS8602 // Dereference of a possibly null reference.
