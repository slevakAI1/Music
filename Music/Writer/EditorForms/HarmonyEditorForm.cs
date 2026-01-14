using Music.Generator;
using Music.Writer;

// AI: purpose=Modal editor for composing an ordered HarmonyTrack; UI-centric, emits normalized track on OK
// AI: invariants=_working list must remain ordered; RecalculateStartPositions enforces contiguous 1-based bar:beat
// AI: beatsPerBar=4 fixed across methods; change carefully if supporting other meters
// AI: storage=Chord quality stored as short name; UI maps long names <-> short names via ChordQuality helpers
// AI: caller=Form does not validate higher-level musical conflicts; callers should use ResultTrack after OK
// AI: dragdrop=ListView indices drive reorder; always call RecalculateStartPositions then RefreshListView after edits

namespace Music.Designer
{
    // Popup editor for arranging Harmony Events and configuring the track
    public sealed class HarmonyEditorForm : Form
    {
        private readonly ListView _lv;
        private readonly Button _btnAdd;
        private readonly Button _btnInsert;
        private readonly Button _btnDelete;
        private readonly Button _btnDuplicate;
        private readonly Button _btnUp;
        private readonly Button _btnDown;
        private readonly Button _btnDefaults;
        private readonly Button _btnOk;
        private readonly Button _btnCancel;

        // Event editor controls
        private readonly Label _lblStart; // computed start bar:beat
        private readonly NumericUpDown _numDuration;
        private readonly ComboBox _cbKey;
        private readonly NumericUpDown _numDegree;
        private readonly ComboBox _cbQuality;
        private readonly ComboBox _cbBass;

        // Working list mirroring the ListView
        private readonly List<WorkingEvent> _working = new();

        // Drag-and-drop support
        private ListViewItem? _dragItem;

        // Suppress feedback updates while programmatically changing editor controls
        private bool _suppressEditorApply;

        // AI: contract=ResultTrack events are ordered, contiguous, and use short-quality names; set when OK pressed
        public HarmonyTrack ResultTrack { get; private set; } = new HarmonyTrack();

        // Predefined values
        private static readonly string[] AllKeys = new[]
        {
            // Major (15)
            "C major","G major","D major","A major","E major","B major","F# major","C# major",
            "F major","Bb major","Eb major","Ab major","Db major","Gb major","Cb major",
            // Minor (15)
            "A minor","E minor","B minor","F# minor","C# minor","G# minor","D# minor","A# minor",
            "D minor","G minor","C minor","F minor","Bb minor","Eb minor","Ab minor"
        };

        // Use centralized chord quality long names for UI display
        private static readonly string[] AllQualities = ChordQuality.LongNames.ToArray();

        private static readonly string[] AllBassOptions = new[]
        {
            "root","3rd","5th","7th","9th","11th","13th"
        };

        private sealed class WorkingEvent
        {
            public int StartBar { get; set; }
            public int StartBeat { get; set; } = 1;
            public int DurationBeats { get; set; } = 4;
            public string Key { get; set; } = "C major";
            public int Degree { get; set; } = 1; // 1..7
            public string Quality { get; set; } = "maj"; // Always stored as short name
            public string Bass { get; set; } = "root";

            public WorkingEvent Clone() => new WorkingEvent
            {
                StartBar = StartBar,
                StartBeat = StartBeat,
                DurationBeats = DurationBeats,
                Key = Key,
                Degree = Degree,
                Quality = Quality,
                Bass = Bass
            };
        }

        public HarmonyEditorForm(HarmonyTrack? initial = null)
        {
            Text = "Edit Harmony track";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            // Increased width so advisory column can show messages fully and right-side editor is pushed right
            ClientSize = new Size(1200, 600);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(8)
            };
            // Give a bit more width to the list (left) so the advisory column is visible
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
             Controls.Add(root);

            // Left: list and row actions
            var left = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            root.Controls.Add(left, 0, 0);

            _lv = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = false,
                AllowDrop = true
            };
            _lv.Columns.Add("#", 40, HorizontalAlignment.Right);
            _lv.Columns.Add("Start", 80, HorizontalAlignment.Right);   // bar:beat
            _lv.Columns.Add("Dur", 60, HorizontalAlignment.Right);     // beats
            _lv.Columns.Add("Key", 120, HorizontalAlignment.Left);
            _lv.Columns.Add("Deg", 50, HorizontalAlignment.Right);
            _lv.Columns.Add("Qual", 80, HorizontalAlignment.Left);
            _lv.Columns.Add("Bass", 80, HorizontalAlignment.Left);
            // Wider advisory column so short messages like 'Not Diatonic' are fully visible
            _lv.Columns.Add("Adv", 160, HorizontalAlignment.Left); // Advisory column

            _lv.SelectedIndexChanged += OnListSelectionChanged;
            _lv.ItemDrag += OnItemDrag;
            _lv.DragEnter += OnDragEnter;
            _lv.DragOver += OnDragOver;
            _lv.DragDrop += OnDragDrop;
            _lv.KeyDown += OnListKeyDown;

            left.Controls.Add(_lv, 0, 0);

            var rowButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            left.Controls.Add(rowButtons, 0, 1);

            _btnAdd = new Button { Text = "Add", AutoSize = true };
            _btnInsert = new Button { Text = "Insert", AutoSize = true };
            _btnDelete = new Button { Text = "Delete", AutoSize = true };
            _btnDuplicate = new Button { Text = "Duplicate", AutoSize = true };
            _btnUp = new Button { Text = "Move Up", AutoSize = true };
            _btnDown = new Button { Text = "Move Down", AutoSize = true };
            _btnDefaults = new Button { Text = "Set Defaults", AutoSize = true };

            _btnAdd.Click += (s, e) => AddEvent(afterIndex: _lv.SelectedIndices.Count > 0 ? _lv.SelectedIndices[0] : (_working.Count - 1));
            _btnInsert.Click += (s, e) => InsertEvent(atIndex: _lv.SelectedIndices.Count > 0 ? _lv.SelectedIndices[0] : 0);
            _btnDelete.Click += (s, e) => DeleteSelected();
            _btnDuplicate.Click += (s, e) => DuplicateSelected();
            _btnUp.Click += (s, e) => MoveSelected(-1);
            _btnDown.Click += (s, e) => MoveSelected(1);
            _btnDefaults.Click += (s, e) => ApplyDefaultTrack();

            rowButtons.Controls.AddRange(new Control[] { _btnAdd, _btnInsert, _btnDelete, _btnDuplicate, _btnUp, _btnDown });

            // Right: editor + OK/Cancel
            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // event editor
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // buttons
            root.Controls.Add(right, 1, 0);

            // Event editor panel
            var editor = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 10,
                Padding = new Padding(6)
            };
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            right.Controls.Add(editor, 0, 0);

            int row = 0;
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            editor.Controls.Add(new Label { Text = "Selected Event", AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) }, 0, row);
            editor.SetColumnSpan(editor.GetControlFromPosition(0, row), 2);
            row++;

            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            editor.Controls.Add(new Label { Text = "Start:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) }, 0, row);
            _lblStart = new Label { Text = "-", AutoSize = true, Anchor = AnchorStyles.Left };
            editor.Controls.Add(_lblStart, 1, row);
            row++;

            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            editor.Controls.Add(new Label { Text = "Duration (beats):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) }, 0, row);
            _numDuration = new NumericUpDown { Minimum = 1, Maximum = 4096, Value = 4, Anchor = AnchorStyles.Left, Width = 80 };
            _numDuration.ValueChanged += (s, e) => ApplyEditorToSelected();
            editor.Controls.Add(_numDuration, 1, row);
            row++;

            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            editor.Controls.Add(new Label { Text = "Key:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) }, 0, row);
            _cbKey = CreateSelectorCombo();
            _cbKey.SelectedIndexChanged += (s, e) => ApplyEditorToSelected();
            editor.Controls.Add(_cbKey, 1, row);
            row++;

            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            editor.Controls.Add(new Label { Text = "Degree (1-7):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) }, 0, row);
            _numDegree = new NumericUpDown { Minimum = 1, Maximum = 7, Value = 1, Anchor = AnchorStyles.Left, Width = 80 };
            _numDegree.ValueChanged += (s, e) => ApplyEditorToSelected();
            editor.Controls.Add(_numDegree, 1, row);
            row++;

            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            editor.Controls.Add(new Label { Text = "Quality:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) }, 0, row);
            _cbQuality = CreateSelectorCombo();
            _cbQuality.SelectedIndexChanged += (s, e) => ApplyEditorToSelected();
            editor.Controls.Add(_cbQuality, 1, row);
            row++;

            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            editor.Controls.Add(new Label { Text = "Bass:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) }, 0, row);
            _cbBass = CreateSelectorCombo();
            _cbBass.SelectedIndexChanged += (s, e) => ApplyEditorToSelected();
            editor.Controls.Add(_cbBass, 1, row);
            row++;

            for (; row < editor.RowCount; row++)
                editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var bottomButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            right.Controls.Add(bottomButtons, 0, 1);

            _btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
            _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            bottomButtons.Controls.AddRange(new Control[] { _btnOk, _btnCancel, _btnDefaults });

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            // Populate selectors with predefined values
            PopulateSelectors();

            // Load initial data
            LoadInitial(initial);
            RefreshListView(selectIndex: _working.Count > 0 ? 0 : -1);

            _btnOk.Click += (s, e) =>
            {
                ResultTrack = BuildResult();
                DialogResult = DialogResult.OK;
                Close();
            };
        }

        private static ComboBox CreateSelectorCombo()
        {
            return new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
        }

        private void PopulateSelectors()
        {
            FillCombo(_cbKey, AllKeys);
            FillCombo(_cbQuality, AllQualities);
            FillCombo(_cbBass, AllBassOptions);

            if (string.IsNullOrWhiteSpace(_cbKey.Text)) _cbKey.Text = "C major";
            if (string.IsNullOrWhiteSpace(_cbQuality.Text)) _cbQuality.Text = ChordQuality.LongNames[0]; // "Major"
            if (string.IsNullOrWhiteSpace(_cbBass.Text)) _cbBass.Text = "root";
        }

        private static void FillCombo(ComboBox cb, IEnumerable<string> values)
        {
            cb.BeginUpdate();
            cb.Items.Clear();
            foreach (var v in values)
                cb.Items.Add(v);
            cb.EndUpdate();
        }

        private void LoadInitial(HarmonyTrack? initial)
        {
            _working.Clear();

            if (initial == null || initial.Events.Count == 0)
            {
                return;
            }

            foreach (var he in initial.Events)
            {
                var normalized = HarmonyEventNormalizer.Normalize(he);
                
                _working.Add(new WorkingEvent
                {
                    StartBar = normalized.StartBar,
                    StartBeat = normalized.StartBeat,
                    DurationBeats = normalized.DurationBeats,
                    Key = normalized.Key,
                    Degree = normalized.Degree,
                    Quality = normalized.Quality,
                    Bass = normalized.Bass
                });
            }

            // Normalize positions to be contiguous by order, similar to Section editor behavior
            RecalculateStartPositions();
        }

        private HarmonyTrack BuildResult()
        {
            var tl = new HarmonyTrack();
            foreach (var w in _working)
            {
                tl.Add(new HarmonyEvent
                {
                    StartBar = w.StartBar,
                    StartBeat = w.StartBeat,
                    DurationBeats = w.DurationBeats,
                    Key = w.Key,
                    Degree = w.Degree,
                    Quality = w.Quality, // Already stored as short name
                    Bass = w.Bass
                });
            }
            return tl;
        }

        private void RefreshListView(int selectIndex = -1)
        {
            _lv.BeginUpdate();
            _lv.Items.Clear();

            // Validate the current working track to get diagnostics
            var tempTrack = BuildResult();
            var validationResult = HarmonyValidator.ValidateTrack(tempTrack, new HarmonyValidationOptions());
            var diagnostics = validationResult.Diagnostics;

            for (int i = 0; i < _working.Count; i++)
            {
                var w = _working[i];
                var item = new ListViewItem((i + 1).ToString());
                item.SubItems.Add($"{w.StartBar}:{w.StartBeat}");
                item.SubItems.Add(w.DurationBeats.ToString());
                item.SubItems.Add(w.Key);
                item.SubItems.Add(w.Degree.ToString());
                item.SubItems.Add(w.Quality); // Display short name in list
                item.SubItems.Add(w.Bass);
                
                // Advisory: display warnings from diagnostics if available
                string advisory = string.Empty;
                if (diagnostics != null && i < diagnostics.EventDiagnostics.Count)
                {
                    var eventDiag = diagnostics.EventDiagnostics[i];
                    if (eventDiag.Warnings.Count > 0)
                    {
                        // Join warnings with semicolon for compact display
                        advisory = string.Join("; ", eventDiag.Warnings.Select(w => 
                            w.Contains("Non-diatonic") ? "Non-diatonic" : w));
                    }
                }
                item.SubItems.Add(advisory);
                item.Tag = w;
                _lv.Items.Add(item);
            }

            _lv.EndUpdate();

            if (selectIndex >= 0 && selectIndex < _lv.Items.Count)
            {
                _lv.Items[selectIndex].Selected = true;
                _lv.EnsureVisible(selectIndex);
            }

            UpdateEditorFromSelected();
            UpdateButtonsEnabled();
        }

        private void UpdateRowVisuals(int index)
        {
            if (index < 0 || index >= _lv.Items.Count) return;
            
            var w = _working[index];
            var it = _lv.Items[index];
            it.Text = (index + 1).ToString();
            it.SubItems[1].Text = $"{w.StartBar}:{w.StartBeat}";
            it.SubItems[2].Text = w.DurationBeats.ToString();
            it.SubItems[3].Text = w.Key;
            it.SubItems[4].Text = w.Degree.ToString();
            it.SubItems[5].Text = w.Quality;
            it.SubItems[6].Text = w.Bass;
            
            // Advisory: validate this single event to get diagnostics
            while (it.SubItems.Count <= 7) it.SubItems.Add(string.Empty);
            
            var tempTrack = BuildResult();
            var validationResult = HarmonyValidator.ValidateTrack(tempTrack, new HarmonyValidationOptions());
            var diagnostics = validationResult.Diagnostics;
            
            string advisory = string.Empty;
            if (diagnostics != null && index < diagnostics.EventDiagnostics.Count)
            {
                var eventDiag = diagnostics.EventDiagnostics[index];
                if (eventDiag.Warnings.Count > 0)
                {
                    // Join warnings with semicolon for compact display
                    advisory = string.Join("; ", eventDiag.Warnings.Select(w => 
                        w.Contains("Non-diatonic") ? "Non-diatonic" : w));
                }
            }
            it.SubItems[7].Text = advisory;
        }

        private void UpdateButtonsEnabled()
        {
            bool hasSel = _lv.SelectedIndices.Count > 0;
            int idx = hasSel ? _lv.SelectedIndices[0] : -1;

            int addInsertAt = hasSel ? idx + 1 : _working.Count; // Add after selection, else append
            int insertAt = hasSel ? idx : 0;                     // Insert at selection, else at start

            _ = ValidateAndGetEditorValues(addInsertAt, out _, out _, out _, out _, out _, out var _, out string? addErr);
            _ = ValidateAndGetEditorValues(insertAt, out _, out _, out _, out _, out _, out var _, out string? insErr);

            _btnAdd.Enabled = addErr == null;
            _btnInsert.Enabled = insErr == null;

            _btnDelete.Enabled = hasSel;
            _btnDuplicate.Enabled = hasSel;
            _btnUp.Enabled = hasSel && idx > 0;
            _btnDown.Enabled = hasSel && idx >= 0 && idx < _working.Count - 1;
            _btnDefaults.Enabled = true;
        }

        private void OnListSelectionChanged(object? sender, EventArgs e)
        {
            UpdateEditorFromSelected();
            UpdateButtonsEnabled();
        }

        private void UpdateEditorFromSelected()
        {
            bool hasSel = _lv.SelectedIndices.Count > 0;

            // Always allow editing so user can stage next add
            _numDuration.Enabled = _cbKey.Enabled = _numDegree.Enabled = _cbQuality.Enabled = _cbBass.Enabled = true;

            if (!hasSel)
            {
                var (bar, beat) = PreviewStartForIndex(_working.Count);
                _lblStart.Text = $"{bar}:{beat}";
                UpdateButtonsEnabled();
                return;
            }

            var w = _lv.SelectedItems[0].Tag as WorkingEvent;
            if (w == null) return;

            _suppressEditorApply = true;
            try
            {
                _numDuration.Value = Math.Max(_numDuration.Minimum, Math.Min(_numDuration.Maximum, w.DurationBeats));
                
                // Select key by finding matching item in the dropdown
                int keyIndex = Array.IndexOf(AllKeys, w.Key ?? "C major");
                if (keyIndex >= 0)
                    _cbKey.SelectedIndex = keyIndex;
                else
                    _cbKey.SelectedIndex = 0; // Default to C major
                
                _numDegree.Value = Math.Max(_numDegree.Minimum, Math.Min(_numDegree.Maximum, w.Degree));
                // Convert short name to long name for UI display
                _cbQuality.Text = ChordQuality.ToLongName(w.Quality) ?? string.Empty;
                _cbBass.Text = w.Bass ?? string.Empty;
                _lblStart.Text = $"{w.StartBar}:{w.StartBeat}";
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

            if (_lv.SelectedIndices.Count == 0)
            {
                var (bar, beat) = PreviewStartForIndex(_working.Count);
                _lblStart.Text = $"{bar}:{beat}";
                UpdateButtonsEnabled();
                return;
            }

            var w = (WorkingEvent)_lv.SelectedItems[0].Tag!;
            w.DurationBeats = (int)_numDuration.Value;
            
            // Get key from selected index instead of Text
            w.Key = _cbKey.SelectedIndex >= 0 && _cbKey.SelectedIndex < AllKeys.Length
                ? AllKeys[_cbKey.SelectedIndex]
                : "C major";
            
            w.Degree = (int)_numDegree.Value;
            // Convert long name from UI to short name for storage
            w.Quality = ChordQuality.ToShortName(
                string.IsNullOrWhiteSpace(_cbQuality.Text) ? "Major" : _cbQuality.Text.Trim());
            w.Bass = string.IsNullOrWhiteSpace(_cbBass.Text) ? "root" : _cbBass.Text.Trim();

            RecalculateStartPositions();
            UpdateRowVisuals(_lv.SelectedIndices[0]);
            UpdateButtonsEnabled();
        }

        private void AddEvent(int afterIndex)
        {
            int insertAt = Math.Max(-1, afterIndex) + 1;
            if (insertAt < 0 || insertAt > _working.Count) insertAt = _working.Count;

            if (!ValidateAndGetEditorValues(insertAt, out int duration, out string key, out int degree, out string quality, out string bass, out var start, out string? error))
            {
                MessageBoxHelper.Show(error!, "Add Event", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var w = new WorkingEvent
            {
                StartBar = start.bar,
                StartBeat = start.beat,
                DurationBeats = duration,
                Key = key,
                Degree = degree,
                Quality = quality, // Already converted to short name in ValidateAndGetEditorValues
                Bass = bass
            };

            _working.Insert(insertAt, w);
            RecalculateStartPositions();
            RefreshListView(-1);
            ResetEditorForNextAdd();
        }

        private void InsertEvent(int atIndex)
        {
            int insertAt = Math.Max(0, Math.Min(atIndex, _working.Count));

            if (!ValidateAndGetEditorValues(insertAt, out int duration, out string key, out int degree, out string quality, out string bass, out var start, out string? error))
            {
                MessageBoxHelper.Show(error!, "Insert Event", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var w = new WorkingEvent
            {
                StartBar = start.bar,
                StartBeat = start.beat,
                DurationBeats = duration,
                Key = key,
                Degree = degree,
                Quality = quality,
                Bass = bass
            };

            _working.Insert(insertAt, w);
            RecalculateStartPositions();
            RefreshListView(-1);
            ResetEditorForNextAdd();
        }

        private void ResetEditorForNextAdd()
        {
            _lv.SelectedIndices.Clear();

            _suppressEditorApply = true;
            try
            {
                _numDuration.Value = Math.Max(_numDuration.Minimum, Math.Min(_numDuration.Maximum, 4));
                // keep last key/quality/bass/degree for fast entry
                var (bar, beat) = PreviewStartForIndex(_working.Count);
                _lblStart.Text = $"{bar}:{beat}";
            }
            finally
            {
                _suppressEditorApply = false;
            }

            UpdateButtonsEnabled();
        }

        private void DeleteSelected()
        {
            if (_lv.SelectedIndices.Count == 0) return;
            int idx = _lv.SelectedIndices[0];
            _working.RemoveAt(idx);
            RecalculateStartPositions();
            int nextSel = Math.Min(idx, _working.Count - 1);
            RefreshListView(nextSel);
        }

        private void DuplicateSelected()
        {
            if (_lv.SelectedIndices.Count == 0) return;
            int idx = _lv.SelectedIndices[0];
            var clone = _working[idx].Clone();
            _working.Insert(idx + 1, clone);
            RecalculateStartPositions();
            RefreshListView(idx + 1);
        }

        private void MoveSelected(int delta)
        {
            if (_lv.SelectedIndices.Count == 0) return;
            int idx = _lv.SelectedIndices[0];
            int newIdx = idx + delta;
            if (newIdx < 0 || newIdx >= _working.Count) return;

            var w = _working[idx];
            _working.RemoveAt(idx);
            _working.Insert(newIdx, w);
            RecalculateStartPositions();
            RefreshListView(newIdx);
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

            var clientPoint = _lv.PointToClient(new Point(e.X, e.Y));
            var target = _lv.GetItemAt(clientPoint.X, clientPoint.Y);

            int from = _dragItem.Index;
            int to = target != null ? target.Index : _working.Count - 1;

            if (from == to) return;

            var w = _working[from];
            _working.RemoveAt(from);
            if (to >= _working.Count) _working.Add(w);
            else _working.Insert(to, w);

            RecalculateStartPositions();
            RefreshListView(to);
            _dragItem = null;
        }

        private void ApplyDefaultTrack()
        {
            var defaults = HarmonyTests.CreateTestTrackD1();

            _working.Clear();

            foreach (var he in defaults.Events)
            {
                var normalized = HarmonyEventNormalizer.Normalize(he);
                
                _working.Add(new WorkingEvent
                {
                    StartBar = normalized.StartBar,
                    StartBeat = normalized.StartBeat,
                    DurationBeats = normalized.DurationBeats,
                    Key = normalized.Key,
                    Degree = normalized.Degree,
                    Quality = normalized.Quality,
                    Bass = normalized.Bass
                });
            }

            RecalculateStartPositions();
            RefreshListView(selectIndex: _working.Count > 0 ? 0 : -1);
        }

        // AI: invariants=Enforces contiguous 1-based start positions across _working; updates visible rows and editor label
        // AI: note=uses constant beatsPerBar=4; callers must call this after any insertion, deletion, or reorder
        private void RecalculateStartPositions()
        {
            const int beatsPerBar = 4;
            int bar = 1;
            int beat = 1;

            foreach (var w in _working)
            {
                w.StartBar = bar;
                w.StartBeat = beat;

                // advance to next event position
                int remaining = w.DurationBeats + (beat - 1);
                bar += remaining / beatsPerBar;
                beat = (remaining % beatsPerBar) + 1;
            }

            // Update visible rows that exist
            int count = Math.Min(_lv.Items.Count, _working.Count);
            for (int i = 0; i < count; i++)
                UpdateRowVisuals(i);

            // Update editor label and buttons
            if (_lv.SelectedIndices.Count > 0)
            {
                int sel = _lv.SelectedIndices[0];
                if (sel >= 0 && sel < _working.Count)
                    _lblStart.Text = $"{_working[sel].StartBar}:{_working[sel].StartBeat}";
                else
                {
                    var p = PreviewStartForIndex(_working.Count);
                    _lblStart.Text = $"{p.bar}:{p.beat}";
                }
            }
            else
            {
                var p = PreviewStartForIndex(_working.Count);
                _lblStart.Text = $"{p.bar}:{p.beat}";
            }

            UpdateButtonsEnabled();
        }

        // AI: note=Computes start for insertAt using current _working; insertAt==_working.Count means append start
        private (int bar, int beat) PreviewStartForIndex(int insertAt)
        {
            const int beatsPerBar = 4;
            int bar = 1;
            int beat = 1;
            for (int i = 0; i < insertAt && i < _working.Count; i++)
            {
                var w = _working[i];
                int remaining = w.DurationBeats + (beat - 1);
                bar += remaining / beatsPerBar;
                beat = (remaining % beatsPerBar) + 1;
            }
            return (bar, beat);
        }

        // AI: behavior=Validate editor fields; converts long-quality->short; returns startPreview; does not modify _working
        private bool ValidateAndGetEditorValues(
            int insertAt,
            out int duration,
            out string key,
            out int degree,
            out string quality,
            out string bass,
            out (int bar, int beat) startPreview,
            out string? error)
        {
            error = null;
            
            // Get key from selected index
            key = _cbKey.SelectedIndex >= 0 && _cbKey.SelectedIndex < AllKeys.Length
                ? AllKeys[_cbKey.SelectedIndex]
                : "C major";
            
            degree = (int)_numDegree.Value;
            // Convert long name from UI to short name
            quality = ChordQuality.ToShortName(
                string.IsNullOrWhiteSpace(_cbQuality.Text) ? "Major" : _cbQuality.Text.Trim());
            bass = string.IsNullOrWhiteSpace(_cbBass.Text) ? "root" : _cbBass.Text.Trim();
            duration = (int)_numDuration.Value;

            if (duration < 1)
                error = "Duration must be at least 1 beat.";

            if (degree < 1 || degree > 7)
                error = (error == null) ? "Degree must be 1..7." : error + " Degree must be 1..7.";

            startPreview = PreviewStartForIndex(insertAt);

            return error == null;
        }
    }
}
