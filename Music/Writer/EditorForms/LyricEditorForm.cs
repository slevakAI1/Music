using Music.Generator;
using MusicGen.Lyrics;

// AI: purpose=Modal editor for LyricTrack; text-based phrase editing with auto-phonetic parsing via WordParser.
// AI: invariants=RawText is source of truth; Words/Syllables auto-populated on text change; ResultTrack set when OK clicked.
// AI: deps=Uses LyricPhoneticsHelper to parse text into phonetically-aware syllables; WordParser for pronunciations.
// AI: contract=Caller passes null initial => empty track; editor returns complete LyricTrack with derived phonetics.
// AI: note=User edits RawText per phrase; system auto-generates Words/Syllables/Phones; manual overrides not yet supported.

namespace Music.Writer
{
    public sealed class LyricEditorForm : Form
    {
        private ListView _lvPhrases;
        private Button _btnAddPhrase;
        private Button _btnInsertPhrase;
        private Button _btnDeletePhrase;
        private Button _btnDuplicatePhrase;
        private Button _btnMoveUp;
        private Button _btnMoveDown;
        private Button _btnDefaults;
        private Button _btnOk;
        private Button _btnCancel;

        // Track-level defaults editor
        private TextBox _txtLanguageTag;
        private CheckBox _chkAnchorIsSyllableStart;
        private NumericUpDown _numDefaultLeadInTicks;
        private NumericUpDown _numDefaultTailOutTicks;
        private NumericUpDown _numDefaultMinNotes;
        private NumericUpDown _numDefaultMaxNotes;
        private NumericUpDown _numDefaultPreferMelisma;

        // Phrase editor controls
        private TextBox _txtRawText;
        private TextBox _txtSectionId;
        private NumericUpDown _numStartTicks;
        private CheckBox _chkHasStartTime;
        private NumericUpDown _numMinTicks;
        private NumericUpDown _numTargetTicks;
        private NumericUpDown _numMaxTicks;
        private NumericUpDown _numDurationWeight;
        private CheckBox _chkHasDurationBudget;
        private Button _btnParseText;
        private Label _lblWordCount;
        private Label _lblSyllableCount;

        private readonly List<WorkingPhrase> _working = new();
        private ListViewItem? _dragItem;
        private bool _suppressEditorApply;

        public LyricTrack ResultTrack { get; private set; } = new LyricTrack();

        private sealed class WorkingPhrase
        {
            public string Id { get; set; } = Guid.NewGuid().ToString("N");
            public string RawText { get; set; } = string.Empty;
            public string? SectionId { get; set; }
            public long? StartTicks { get; set; }
            public long? MinTicks { get; set; }
            public long? TargetTicks { get; set; }
            public long? MaxTicks { get; set; }
            public float DurationWeight { get; set; } = 0.7f;
            
            // Derived data (auto-populated from RawText)
            public List<LyricWord> Words { get; } = new();
            public List<LyricSyllable> Syllables { get; } = new();

            public WorkingPhrase Clone() => new WorkingPhrase
            {
                Id = Guid.NewGuid().ToString("N"),
                RawText = RawText,
                SectionId = SectionId,
                StartTicks = StartTicks,
                MinTicks = MinTicks,
                TargetTicks = TargetTicks,
                MaxTicks = MaxTicks,
                DurationWeight = DurationWeight
            };
        }

        public LyricEditorForm(LyricTrack? initial = null)
        {
            Text = "Edit Lyrics";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new Size(1100, 700);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(8)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            Controls.Add(root);

            // Left: phrase list + buttons
            var left = BuildLeftPanel();
            root.Controls.Add(left, 0, 0);

            // Right: track defaults + phrase editor + OK/Cancel
            var right = BuildRightPanel();
            root.Controls.Add(right, 1, 0);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            LoadInitial(initial);
            RefreshListView(selectIndex: _working.Count > 0 ? 0 : -1);

            _btnOk.Click += (s, e) =>
            {
                ResultTrack = BuildResult();
                DialogResult = DialogResult.OK;
                Close();
            };
        }

        private TableLayoutPanel BuildLeftPanel()
        {
            var left = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            _lvPhrases = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = false,
                AllowDrop = true
            };
            _lvPhrases.Columns.Add("#", 40, HorizontalAlignment.Right);
            _lvPhrases.Columns.Add("Text", 300, HorizontalAlignment.Left);
            _lvPhrases.Columns.Add("Words", 60, HorizontalAlignment.Right);
            _lvPhrases.Columns.Add("Syllables", 80, HorizontalAlignment.Right);

            _lvPhrases.SelectedIndexChanged += OnListSelectionChanged;
            _lvPhrases.ItemDrag += OnItemDrag;
            _lvPhrases.DragEnter += OnDragEnter;
            _lvPhrases.DragOver += OnDragOver;
            _lvPhrases.DragDrop += OnDragDrop;
            _lvPhrases.KeyDown += OnListKeyDown;

            left.Controls.Add(_lvPhrases, 0, 0);

            var rowButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            left.Controls.Add(rowButtons, 0, 1);

            _btnAddPhrase = new Button { Text = "Add", AutoSize = true };
            _btnInsertPhrase = new Button { Text = "Insert", AutoSize = true };
            _btnDeletePhrase = new Button { Text = "Delete", AutoSize = true };
            _btnDuplicatePhrase = new Button { Text = "Duplicate", AutoSize = true };
            _btnMoveUp = new Button { Text = "Move Up", AutoSize = true };
            _btnMoveDown = new Button { Text = "Move Down", AutoSize = true };
            _btnDefaults = new Button { Text = "Set Defaults", AutoSize = true };

            _btnAddPhrase.Click += (s, e) => AddPhrase();
            _btnInsertPhrase.Click += (s, e) => InsertPhrase();
            _btnDeletePhrase.Click += (s, e) => DeleteSelected();
            _btnDuplicatePhrase.Click += (s, e) => DuplicateSelected();
            _btnMoveUp.Click += (s, e) => MoveSelected(-1);
            _btnMoveDown.Click += (s, e) => MoveSelected(1);
            _btnDefaults.Click += (s, e) => ApplyDefaults();

            rowButtons.Controls.AddRange(new Control[] {
                _btnAddPhrase, _btnInsertPhrase, _btnDeletePhrase,
                _btnDuplicatePhrase, _btnMoveUp, _btnMoveDown
            });

            return left;
        }

        private TableLayoutPanel BuildRightPanel()
        {
            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 180)); // Track defaults
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Phrase editor
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // Buttons

            // Track defaults section
            var trackDefaults = BuildTrackDefaultsPanel();
            right.Controls.Add(trackDefaults, 0, 0);

            // Phrase editor section
            var phraseEditor = BuildPhraseEditorPanel();
            right.Controls.Add(phraseEditor, 0, 1);

            // Bottom buttons
            var bottomButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };
            right.Controls.Add(bottomButtons, 0, 2);

            _btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
            _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            bottomButtons.Controls.AddRange(new Control[] { _btnOk, _btnCancel, _btnDefaults });

            return right;
        }

        private GroupBox BuildTrackDefaultsPanel()
        {
            var grp = new GroupBox
            {
                Text = "Track Defaults",
                Dock = DockStyle.Fill,
                Padding = new Padding(8)
            };

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(4)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            grp.Controls.Add(panel);

            int row = 0;

            // Language
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.Controls.Add(new Label { Text = "Language Tag:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) }, 0, row);
            _txtLanguageTag = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Text = "en-US" };
            _txtLanguageTag.TextChanged += (s, e) => ApplyTrackDefaultsToWorking();
            panel.Controls.Add(_txtLanguageTag, 1, row);
            row++;

            // Anchor mode
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            _chkAnchorIsSyllableStart = new CheckBox { Text = "Anchor is Syllable Start", AutoSize = true, Anchor = AnchorStyles.Left };
            _chkAnchorIsSyllableStart.CheckedChanged += (s, e) => ApplyTrackDefaultsToWorking();
            panel.Controls.Add(_chkAnchorIsSyllableStart, 1, row);
            panel.SetColumnSpan(_chkAnchorIsSyllableStart, 2);
            row++;

            // Consonant timing
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.Controls.Add(new Label { Text = "Lead-In Ticks:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) }, 0, row);
            _numDefaultLeadInTicks = new NumericUpDown { Minimum = 0, Maximum = 10000, Value = 0, Anchor = AnchorStyles.Left, Width = 80 };
            _numDefaultLeadInTicks.ValueChanged += (s, e) => ApplyTrackDefaultsToWorking();
            panel.Controls.Add(_numDefaultLeadInTicks, 1, row);
            row++;

            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.Controls.Add(new Label { Text = "Tail-Out Ticks:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) }, 0, row);
            _numDefaultTailOutTicks = new NumericUpDown { Minimum = 0, Maximum = 10000, Value = 0, Anchor = AnchorStyles.Left, Width = 80 };
            _numDefaultTailOutTicks.ValueChanged += (s, e) => ApplyTrackDefaultsToWorking();
            panel.Controls.Add(_numDefaultTailOutTicks, 1, row);
            row++;

            // Melisma defaults
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.Controls.Add(new Label { Text = "Min/Max Notes:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) }, 0, row);
            var melismaPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            _numDefaultMinNotes = new NumericUpDown { Minimum = 1, Maximum = 10, Value = 1, Width = 50 };
            _numDefaultMaxNotes = new NumericUpDown { Minimum = 1, Maximum = 10, Value = 2, Width = 50 };
            _numDefaultMinNotes.ValueChanged += (s, e) => ApplyTrackDefaultsToWorking();
            _numDefaultMaxNotes.ValueChanged += (s, e) => ApplyTrackDefaultsToWorking();
            melismaPanel.Controls.AddRange(new Control[] { _numDefaultMinNotes, new Label { Text = " - ", AutoSize = true }, _numDefaultMaxNotes });
            panel.Controls.Add(melismaPanel, 1, row);
            row++;

            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.Controls.Add(new Label { Text = "Prefer Melisma:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) }, 0, row);
            _numDefaultPreferMelisma = new NumericUpDown { Minimum = 0, Maximum = 1, DecimalPlaces = 2, Increment = 0.1M, Value = 0.1M, Anchor = AnchorStyles.Left, Width = 80 };
            _numDefaultPreferMelisma.ValueChanged += (s, e) => ApplyTrackDefaultsToWorking();
            panel.Controls.Add(_numDefaultPreferMelisma, 1, row);
            row++;

            for (; row < panel.RowCount; row++)
                panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            return grp;
        }

        private GroupBox BuildPhraseEditorPanel()
        {
            var grp = new GroupBox
            {
                Text = "Selected Phrase",
                Dock = DockStyle.Fill,
                Padding = new Padding(8)
            };

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 11,
                Padding = new Padding(4)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            grp.Controls.Add(panel);

            int row = 0;

            // Raw text (multi-line)
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            panel.Controls.Add(new Label { Text = "Lyrics Text:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) }, 0, row);
            _txtRawText = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom };
            _txtRawText.TextChanged += (s, e) => OnRawTextChanged();
            panel.Controls.Add(_txtRawText, 1, row);
            row++;

            // Parse button + stats
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            _btnParseText = new Button { Text = "Re-Parse Text", AutoSize = true };
            _btnParseText.Click += (s, e) => ParseSelectedPhrase();
            var statsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            _lblWordCount = new Label { Text = "Words: 0", AutoSize = true, Margin = new Padding(0, 4, 10, 0) };
            _lblSyllableCount = new Label { Text = "Syllables: 0", AutoSize = true, Margin = new Padding(0, 4, 0, 0) };
            statsPanel.Controls.AddRange(new Control[] { _btnParseText, _lblWordCount, _lblSyllableCount });
            panel.Controls.Add(statsPanel, 1, row);
            panel.SetColumnSpan(statsPanel, 2);
            row++;

            // Section ID
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.Controls.Add(new Label { Text = "Section ID:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) }, 0, row);
            _txtSectionId = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            _txtSectionId.TextChanged += (s, e) => ApplyEditorToSelected();
            panel.Controls.Add(_txtSectionId, 1, row);
            row++;

            // Start time
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            _chkHasStartTime = new CheckBox { Text = "Has Start Time", AutoSize = true, Anchor = AnchorStyles.Left };
            _chkHasStartTime.CheckedChanged += (s, e) => { _numStartTicks.Enabled = _chkHasStartTime.Checked; ApplyEditorToSelected(); };
            panel.Controls.Add(_chkHasStartTime, 0, row);
            _numStartTicks = new NumericUpDown { Minimum = 0, Maximum = 1000000, Value = 0, Enabled = false, Anchor = AnchorStyles.Left, Width = 100 };
            _numStartTicks.ValueChanged += (s, e) => ApplyEditorToSelected();
            panel.Controls.Add(_numStartTicks, 1, row);
            row++;

            // Duration budget header
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            _chkHasDurationBudget = new CheckBox { Text = "Has Duration Budget", AutoSize = true, Anchor = AnchorStyles.Left };
            _chkHasDurationBudget.CheckedChanged += (s, e) =>
            {
                _numMinTicks.Enabled = _numTargetTicks.Enabled = _numMaxTicks.Enabled = _numDurationWeight.Enabled = _chkHasDurationBudget.Checked;
                ApplyEditorToSelected();
            };
            panel.Controls.Add(_chkHasDurationBudget, 0, row);
            panel.SetColumnSpan(_chkHasDurationBudget, 2);
            row++;

            // Min ticks
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.Controls.Add(new Label { Text = "  Min Ticks:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) }, 0, row);
            _numMinTicks = new NumericUpDown { Minimum = 0, Maximum = 1000000, Value = 0, Enabled = false, Anchor = AnchorStyles.Left, Width = 100 };
            _numMinTicks.ValueChanged += (s, e) => ApplyEditorToSelected();
            panel.Controls.Add(_numMinTicks, 1, row);
            row++;

            // Target ticks
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.Controls.Add(new Label { Text = "  Target Ticks:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) }, 0, row);
            _numTargetTicks = new NumericUpDown { Minimum = 0, Maximum = 1000000, Value = 0, Enabled = false, Anchor = AnchorStyles.Left, Width = 100 };
            _numTargetTicks.ValueChanged += (s, e) => ApplyEditorToSelected();
            panel.Controls.Add(_numTargetTicks, 1, row);
            row++;

            // Max ticks
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.Controls.Add(new Label { Text = "  Max Ticks:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) }, 0, row);
            _numMaxTicks = new NumericUpDown { Minimum = 0, Maximum = 1000000, Value = 0, Enabled = false, Anchor = AnchorStyles.Left, Width = 100 };
            _numMaxTicks.ValueChanged += (s, e) => ApplyEditorToSelected();
            panel.Controls.Add(_numMaxTicks, 1, row);
            row++;

            // Weight
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.Controls.Add(new Label { Text = "  Weight:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) }, 0, row);
            _numDurationWeight = new NumericUpDown { Minimum = 0, Maximum = 1, DecimalPlaces = 2, Increment = 0.1M, Value = 0.7M, Enabled = false, Anchor = AnchorStyles.Left, Width = 80 };
            _numDurationWeight.ValueChanged += (s, e) => ApplyEditorToSelected();
            panel.Controls.Add(_numDurationWeight, 1, row);
            row++;

            for (; row < panel.RowCount; row++)
                panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            return grp;
        }

        private void LoadInitial(LyricTrack? initial)
        {
            _working.Clear();

            if (initial == null)
            {
                // Set default track defaults
                _txtLanguageTag.Text = "en-US";
                _chkAnchorIsSyllableStart.Checked = false;
                _numDefaultLeadInTicks.Value = 0;
                _numDefaultTailOutTicks.Value = 0;
                _numDefaultMinNotes.Value = 1;
                _numDefaultMaxNotes.Value = 2;
                _numDefaultPreferMelisma.Value = 0.1M;
                return;
            }

            // Load track defaults
            _txtLanguageTag.Text = initial.LanguageTag ?? "en-US";
            _chkAnchorIsSyllableStart.Checked = initial.Defaults.AnchorIsSyllableStart;
            _numDefaultLeadInTicks.Value = initial.Defaults.DefaultConsonantTiming.LeadInTicks;
            _numDefaultTailOutTicks.Value = initial.Defaults.DefaultConsonantTiming.TailOutTicks;
            _numDefaultMinNotes.Value = initial.Defaults.DefaultMelisma.MinNotes;
            _numDefaultMaxNotes.Value = initial.Defaults.DefaultMelisma.MaxNotes;
            _numDefaultPreferMelisma.Value = (decimal)initial.Defaults.DefaultMelisma.PreferMelisma;

            // Load phrases
            foreach (var phrase in initial.Phrases)
            {
                var wp = new WorkingPhrase
                {
                    Id = phrase.Id,
                    RawText = phrase.RawText,
                    SectionId = phrase.SectionId,
                    StartTicks = phrase.StartTime?.Ticks,
                    MinTicks = phrase.DurationBudget?.MinTicks,
                    TargetTicks = phrase.DurationBudget?.TargetTicks,
                    MaxTicks = phrase.DurationBudget?.MaxTicks,
                    DurationWeight = phrase.DurationBudget?.Weight ?? 0.7f
                };

                // Copy words and syllables
                foreach (var word in phrase.Words)
                {
                    wp.Words.Add(word);
                }
                foreach (var syllable in phrase.Syllables)
                {
                    wp.Syllables.Add(syllable);
                }

                _working.Add(wp);
            }
        }

        private LyricTrack BuildResult()
        {
            var track = new LyricTrack
            {
                LanguageTag = _txtLanguageTag.Text?.Trim() ?? "en-US",
                Defaults = new LyricDefaults
                {
                    AnchorIsSyllableStart = _chkAnchorIsSyllableStart.Checked,
                    DefaultConsonantTiming = new ConsonantTimingHints
                    {
                        LeadInTicks = (long)_numDefaultLeadInTicks.Value,
                        TailOutTicks = (long)_numDefaultTailOutTicks.Value
                    },
                    DefaultMelisma = new MelismaConstraint
                    {
                        MinNotes = (int)_numDefaultMinNotes.Value,
                        MaxNotes = (int)_numDefaultMaxNotes.Value,
                        PreferMelisma = (float)_numDefaultPreferMelisma.Value
                    }
                }
            };

            foreach (var wp in _working)
            {
                var phrase = new LyricPhrase
                {
                    Id = wp.Id,
                    RawText = wp.RawText,
                    SectionId = string.IsNullOrWhiteSpace(wp.SectionId) ? null : wp.SectionId,
                    StartTime = wp.StartTicks.HasValue ? new MusicalTime(wp.StartTicks.Value) : null,
                    DurationBudget = (wp.MinTicks.HasValue || wp.TargetTicks.HasValue || wp.MaxTicks.HasValue)
                        ? new TickSpanConstraint
                        {
                            MinTicks = wp.MinTicks,
                            TargetTicks = wp.TargetTicks,
                            MaxTicks = wp.MaxTicks,
                            Weight = wp.DurationWeight
                        }
                        : null
                };

                // Copy derived words and syllables
                foreach (var word in wp.Words)
                {
                    phrase.Words.Add(word);
                }
                foreach (var syllable in wp.Syllables)
                {
                    phrase.Syllables.Add(syllable);
                }

                track.Phrases.Add(phrase);
            }

            return track;
        }

        private void RefreshListView(int selectIndex = -1)
        {
            _lvPhrases.BeginUpdate();
            _lvPhrases.Items.Clear();

            for (int i = 0; i < _working.Count; i++)
            {
                var wp = _working[i];
                var preview = wp.RawText.Length > 40 ? wp.RawText.Substring(0, 40) + "..." : wp.RawText;
                var item = new ListViewItem((i + 1).ToString());
                item.SubItems.Add(preview);
                item.SubItems.Add(wp.Words.Count.ToString());
                item.SubItems.Add(wp.Syllables.Count.ToString());
                item.Tag = wp;
                _lvPhrases.Items.Add(item);
            }

            _lvPhrases.EndUpdate();

            if (selectIndex >= 0 && selectIndex < _lvPhrases.Items.Count)
            {
                _lvPhrases.Items[selectIndex].Selected = true;
                _lvPhrases.EnsureVisible(selectIndex);
            }

            UpdateEditorFromSelected();
            UpdateButtonsEnabled();
        }

        private void UpdateRowVisuals(int index)
        {
            if (index < 0 || index >= _lvPhrases.Items.Count) return;
            var wp = _working[index];
            var it = _lvPhrases.Items[index];
            var preview = wp.RawText.Length > 40 ? wp.RawText.Substring(0, 40) + "..." : wp.RawText;
            it.Text = (index + 1).ToString();
            it.SubItems[1].Text = preview;
            it.SubItems[2].Text = wp.Words.Count.ToString();
            it.SubItems[3].Text = wp.Syllables.Count.ToString();
        }

        private void UpdateButtonsEnabled()
        {
            bool hasSel = _lvPhrases.SelectedIndices.Count > 0;
            int idx = hasSel ? _lvPhrases.SelectedIndices[0] : -1;

            _btnAddPhrase.Enabled = true;
            _btnInsertPhrase.Enabled = true;
            _btnDeletePhrase.Enabled = hasSel;
            _btnDuplicatePhrase.Enabled = hasSel;
            _btnMoveUp.Enabled = hasSel && idx > 0;
            _btnMoveDown.Enabled = hasSel && idx >= 0 && idx < _working.Count - 1;
            _btnDefaults.Enabled = true;
        }

        private void OnListSelectionChanged(object? sender, EventArgs e)
        {
            UpdateEditorFromSelected();
            UpdateButtonsEnabled();
        }

        private void UpdateEditorFromSelected()
        {
            bool hasSel = _lvPhrases.SelectedIndices.Count > 0;

            _txtRawText.Enabled = _txtSectionId.Enabled = _chkHasStartTime.Enabled = _chkHasDurationBudget.Enabled = true;

            if (!hasSel)
            {
                _suppressEditorApply = true;
                try
                {
                    _txtRawText.Clear();
                    _txtSectionId.Clear();
                    _chkHasStartTime.Checked = false;
                    _numStartTicks.Value = 0;
                    _chkHasDurationBudget.Checked = false;
                    _numMinTicks.Value = 0;
                    _numTargetTicks.Value = 0;
                    _numMaxTicks.Value = 0;
                    _numDurationWeight.Value = 0.7M;
                    _lblWordCount.Text = "Words: 0";
                    _lblSyllableCount.Text = "Syllables: 0";
                }
                finally
                {
                    _suppressEditorApply = false;
                }
                UpdateButtonsEnabled();
                return;
            }

            var wp = _lvPhrases.SelectedItems[0].Tag as WorkingPhrase;
            if (wp == null) return;

            _suppressEditorApply = true;
            try
            {
                _txtRawText.Text = wp.RawText;
                _txtSectionId.Text = wp.SectionId ?? string.Empty;
                
                _chkHasStartTime.Checked = wp.StartTicks.HasValue;
                _numStartTicks.Value = wp.StartTicks ?? 0;
                _numStartTicks.Enabled = _chkHasStartTime.Checked;

                bool hasBudget = wp.MinTicks.HasValue || wp.TargetTicks.HasValue || wp.MaxTicks.HasValue;
                _chkHasDurationBudget.Checked = hasBudget;
                _numMinTicks.Value = wp.MinTicks ?? 0;
                _numTargetTicks.Value = wp.TargetTicks ?? 0;
                _numMaxTicks.Value = wp.MaxTicks ?? 0;
                _numDurationWeight.Value = (decimal)wp.DurationWeight;
                _numMinTicks.Enabled = _numTargetTicks.Enabled = _numMaxTicks.Enabled = _numDurationWeight.Enabled = hasBudget;

                _lblWordCount.Text = $"Words: {wp.Words.Count}";
                _lblSyllableCount.Text = $"Syllables: {wp.Syllables.Count}";
            }
            finally
            {
                _suppressEditorApply = false;
            }

            UpdateButtonsEnabled();
        }

        private void ApplyEditorToSelected()
        {
            if (_suppressEditorApply) return;

            if (_lvPhrases.SelectedIndices.Count == 0)
            {
                UpdateButtonsEnabled();
                return;
            }

            var wp = (WorkingPhrase)_lvPhrases.SelectedItems[0].Tag!;

            wp.SectionId = string.IsNullOrWhiteSpace(_txtSectionId.Text) ? null : _txtSectionId.Text.Trim();
            wp.StartTicks = _chkHasStartTime.Checked ? (long)_numStartTicks.Value : null;
            
            if (_chkHasDurationBudget.Checked)
            {
                wp.MinTicks = (long)_numMinTicks.Value;
                wp.TargetTicks = (long)_numTargetTicks.Value;
                wp.MaxTicks = (long)_numMaxTicks.Value;
                wp.DurationWeight = (float)_numDurationWeight.Value;
            }
            else
            {
                wp.MinTicks = null;
                wp.TargetTicks = null;
                wp.MaxTicks = null;
            }

            UpdateRowVisuals(_lvPhrases.SelectedIndices[0]);
            UpdateButtonsEnabled();
        }

        private void OnRawTextChanged()
        {
            if (_suppressEditorApply) return;

            if (_lvPhrases.SelectedIndices.Count == 0)
                return;

            var wp = (WorkingPhrase)_lvPhrases.SelectedItems[0].Tag!;
            wp.RawText = _txtRawText.Text;

            // Auto-parse text to keep derived data in sync
            ParseWorkingPhrase(wp);

            UpdateRowVisuals(_lvPhrases.SelectedIndices[0]);
            _lblWordCount.Text = $"Words: {wp.Words.Count}";
            _lblSyllableCount.Text = $"Syllables: {wp.Syllables.Count}";
        }

        private void ParseSelectedPhrase()
        {
            if (_lvPhrases.SelectedIndices.Count == 0)
                return;

            var wp = (WorkingPhrase)_lvPhrases.SelectedItems[0].Tag!;
            ParseWorkingPhrase(wp);

            UpdateRowVisuals(_lvPhrases.SelectedIndices[0]);
            _lblWordCount.Text = $"Words: {wp.Words.Count}";
            _lblSyllableCount.Text = $"Syllables: {wp.Syllables.Count}";
        }

        private void ParseWorkingPhrase(WorkingPhrase wp)
        {
            wp.Words.Clear();
            wp.Syllables.Clear();

            if (string.IsNullOrWhiteSpace(wp.RawText))
                return;

            var tempPhrase = new LyricPhrase();
            LyricPhoneticsHelper.ParseTextToLyricPhrase(tempPhrase, wp.RawText);
            LyricPhoneticsHelper.MarkBreathPoints(tempPhrase);

            foreach (var word in tempPhrase.Words)
            {
                wp.Words.Add(word);
            }
            foreach (var syllable in tempPhrase.Syllables)
            {
                wp.Syllables.Add(syllable);
            }
        }

        private void ApplyTrackDefaultsToWorking()
        {
            // Track defaults changed - no need to update working phrases
            // They will be applied when building result
        }

        private void AddPhrase()
        {
            var wp = new WorkingPhrase
            {
                RawText = string.Empty
            };

            _working.Add(wp);
            RefreshListView(_working.Count - 1);
        }

        private void InsertPhrase()
        {
            int insertAt = _lvPhrases.SelectedIndices.Count > 0 ? _lvPhrases.SelectedIndices[0] : 0;

            var wp = new WorkingPhrase
            {
                RawText = string.Empty
            };

            _working.Insert(insertAt, wp);
            RefreshListView(insertAt);
        }

        private void DeleteSelected()
        {
            if (_lvPhrases.SelectedIndices.Count == 0) return;
            int idx = _lvPhrases.SelectedIndices[0];
            _working.RemoveAt(idx);
            int nextSel = Math.Min(idx, _working.Count - 1);
            RefreshListView(nextSel);
        }

        private void DuplicateSelected()
        {
            if (_lvPhrases.SelectedIndices.Count == 0) return;
            int idx = _lvPhrases.SelectedIndices[0];
            var clone = _working[idx].Clone();
            
            // Re-parse the cloned text
            ParseWorkingPhrase(clone);
            
            _working.Insert(idx + 1, clone);
            RefreshListView(idx + 1);
        }

        private void MoveSelected(int delta)
        {
            if (_lvPhrases.SelectedIndices.Count == 0) return;
            int idx = _lvPhrases.SelectedIndices[0];
            int newIdx = idx + delta;
            if (newIdx < 0 || newIdx >= _working.Count) return;

            var wp = _working[idx];
            _working.RemoveAt(idx);
            _working.Insert(newIdx, wp);
            RefreshListView(newIdx);
        }

        private void ApplyDefaults()
        {
            _working.Clear();

            // Add sample phrases
            var samples = new[]
            {
                "Amazing grace how sweet the sound",
                "That saved a wretch like me",
                "I once was lost but now am found",
                "Was blind but now I see"
            };

            foreach (var text in samples)
            {
                var wp = new WorkingPhrase { RawText = text };
                ParseWorkingPhrase(wp);
                _working.Add(wp);
            }

            RefreshListView(selectIndex: 0);
        }

        private void OnListKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSelected();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.Up)
            {
                MoveSelected(-1);
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.Down)
            {
                MoveSelected(1);
                e.Handled = true;
            }
        }

        private void OnItemDrag(object? sender, ItemDragEventArgs e)
        {
            _dragItem = (ListViewItem)e.Item;
            DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void OnDragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(typeof(ListViewItem)))
                e.Effect = DragDropEffects.Move;
        }

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            if (e.Data == null || !e.Data.GetDataPresent(typeof(ListViewItem)))
            {
                e.Effect = DragDropEffects.None;
                return;
            }
            e.Effect = DragDropEffects.Move;
        }

        private void OnDragDrop(object? sender, DragEventArgs e)
        {
            if (_dragItem == null) return;

            var clientPoint = _lvPhrases.PointToClient(new Point(e.X, e.Y));
            var target = _lvPhrases.GetItemAt(clientPoint.X, clientPoint.Y);

            int from = _dragItem.Index;
            int to = target != null ? target.Index : _working.Count - 1;

            if (from == to) return;

            var wp = _working[from];
            _working.RemoveAt(from);
            if (to >= _working.Count) _working.Add(wp);
            else _working.Insert(to, wp);

            RefreshListView(to);
            _dragItem = null;
        }
    }
}
