using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Music.Designer
{
    // Popup editor for arranging Harmony Events and configuring the timeline
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

        // Global timeline controls
        private readonly TextBox _txtMeter;   // e.g., 4/4 (only numerator used)

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

        // Current global settings
        private int _beatsPerBar = 4;

        public HarmonyTimeline ResultTimeline { get; private set; } = new HarmonyTimeline();

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

        private static readonly string[] AllQualities = new[]
        {
            // Triads
            "maj","min","dim","aug","sus2","sus4","5",
            // 6ths
            "maj6","min6","6/9",
            // 7ths
            "dom7","maj7","min7","dim7","hdim7","minMaj7",
            // Extensions
            "9","maj9","min9","11","13",
            // Adds
            "add9","add11","add13"
        };

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
            public string Quality { get; set; } = "maj";
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

        public HarmonyEditorForm(HarmonyTimeline? initial = null)
        {
            Text = "Edit Harmony Timeline";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new Size(1000, 600);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(8)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
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
            _btnDefaults.Click += (s, e) => ApplyDefaultTimeline();

            rowButtons.Controls.AddRange(new Control[] { _btnAdd, _btnInsert, _btnDelete, _btnDuplicate, _btnUp, _btnDown });

            // Right: global + editor + OK/Cancel
            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 90)); // global settings
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // event editor
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // buttons
            root.Controls.Add(right, 1, 0);

            // Global settings panel (Meter only)
            var globalsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(6)
            };
            globalsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            globalsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            right.Controls.Add(globalsPanel, 0, 0);

            globalsPanel.Controls.Add(new Label { Text = "Meter:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) }, 0, 0);
            _txtMeter = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Text = "4/4" };
            _txtMeter.TextChanged += (s, e) => OnGlobalsChanged();
            globalsPanel.Controls.Add(_txtMeter, 1, 0);

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
            right.Controls.Add(editor, 0, 1);

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
            _cbKey.TextChanged += (s, e) => ApplyEditorToSelected();
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
            _cbQuality.TextChanged += (s, e) => ApplyEditorToSelected();
            editor.Controls.Add(_cbQuality, 1, row);
            row++;

            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            editor.Controls.Add(new Label { Text = "Bass:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) }, 0, row);
            _cbBass = CreateSelectorCombo();
            _cbBass.TextChanged += (s, e) => ApplyEditorToSelected();
            editor.Controls.Add(_cbBass, 1, row);
            row++;

            for (; row < editor.RowCount; row++)
                editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var bottomButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            right.Controls.Add(bottomButtons, 0, 2);

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
                ResultTimeline = BuildResult();
                DialogResult = DialogResult.OK;
                Close();
            };
        }

        private static ComboBox CreateSelectorCombo()
        {
            return new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
        }

        private void PopulateSelectors()
        {
            FillCombo(_cbKey, AllKeys);
            FillCombo(_cbQuality, AllQualities);
            FillCombo(_cbBass, AllBassOptions);

            if (string.IsNullOrWhiteSpace(_cbKey.Text)) _cbKey.Text = "C major";
            if (string.IsNullOrWhiteSpace(_cbQuality.Text)) _cbQuality.Text = "maj";
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

        private void LoadInitial(HarmonyTimeline? initial)
        {
            _working.Clear();

            if (initial == null || initial.Events.Count == 0)
            {
                if (initial != null)
                {
                    _beatsPerBar = Math.Max(1, initial.BeatsPerBar);
                }
                ApplyGlobalsToUi();
                return;
            }

            _beatsPerBar = Math.Max(1, initial.BeatsPerBar);
            ApplyGlobalsToUi();

            foreach (var he in initial.Events)
            {
                _working.Add(new WorkingEvent
                {
                    StartBar = he.StartBar,
                    StartBeat = he.StartBeat,
                    DurationBeats = he.DurationBeats,
                    Key = he.Key,
                    Degree = he.Degree,
                    Quality = he.Quality,
                    Bass = he.Bass
                });
            }

            // Normalize positions to be contiguous by order, similar to Section editor behavior
            RecalculateStartPositions();
        }

        private void ApplyGlobalsToUi()
        {
            _suppressEditorApply = true;
            try
            {
                _txtMeter.Text = $"{_beatsPerBar}/4";
            }
            finally
            {
                _suppressEditorApply = false;
            }
        }

        private void OnGlobalsChanged()
        {
            if (_suppressEditorApply) return;

            // Parse meter like x/y but only numerator matters
            int beats = _beatsPerBar;
            var txt = _txtMeter.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(txt))
            {
                var parts = txt.Split('/');
                if (parts.Length == 2 && int.TryParse(parts[0], out var b) && b >= 1)
                    beats = b;
            }

            if (beats != _beatsPerBar)
            {
                _beatsPerBar = beats;
                RecalculateStartPositions();
                RefreshListView(_lv.SelectedIndices.Count > 0 ? _lv.SelectedIndices[0] : -1);
            }

            UpdateButtonsEnabled();
        }

        private HarmonyTimeline BuildResult()
        {
            var tl = new HarmonyTimeline();
            tl.ConfigureGlobal($"{_beatsPerBar}/4");
            foreach (var w in _working)
            {
                tl.Add(new HarmonyEvent
                {
                    StartBar = w.StartBar,
                    StartBeat = w.StartBeat,
                    DurationBeats = w.DurationBeats,
                    Key = w.Key,
                    Degree = w.Degree,
                    Quality = w.Quality,
                    Bass = w.Bass
                });
            }
            return tl;
        }

        private void RefreshListView(int selectIndex = -1)
        {
            _lv.BeginUpdate();
            _lv.Items.Clear();

            for (int i = 0; i < _working.Count; i++)
            {
                var w = _working[i];
                var item = new ListViewItem((i + 1).ToString());
                item.SubItems.Add($"{w.StartBar}:{w.StartBeat}");
                item.SubItems.Add(w.DurationBeats.ToString());
                item.SubItems.Add(w.Key);
                item.SubItems.Add(w.Degree.ToString());
                item.SubItems.Add(w.Quality);
                item.SubItems.Add(w.Bass);
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
                _cbKey.Text = w.Key ?? string.Empty;
                _numDegree.Value = Math.Max(_numDegree.Minimum, Math.Min(_numDegree.Maximum, w.Degree));
                _cbQuality.Text = w.Quality ?? string.Empty;
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
            w.Key = string.IsNullOrWhiteSpace(_cbKey.Text) ? "C major" : _cbKey.Text.Trim();
            w.Degree = (int)_numDegree.Value;
            w.Quality = string.IsNullOrWhiteSpace(_cbQuality.Text) ? "maj" : _cbQuality.Text.Trim();
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
                MessageBox.Show(this, error!, "Add Event", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void InsertEvent(int atIndex)
        {
            int insertAt = Math.Max(0, Math.Min(atIndex, _working.Count));

            if (!ValidateAndGetEditorValues(insertAt, out int duration, out string key, out int degree, out string quality, out string bass, out var start, out string? error))
            {
                MessageBox.Show(this, error!, "Insert Event", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                _numDuration.Value = Math.Max(_numDuration.Minimum, Math.Min(_numDuration.Maximum, _beatsPerBar));
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

        private void ApplyDefaultTimeline()
        {
            var defaults = HarmonyTests.CreateTestTimelineD1();

            _working.Clear();
            _beatsPerBar = Math.Max(1, defaults.BeatsPerBar);
            ApplyGlobalsToUi();

            foreach (var he in defaults.Events)
            {
                _working.Add(new WorkingEvent
                {
                    StartBar = he.StartBar,
                    StartBeat = he.StartBeat,
                    DurationBeats = he.DurationBeats,
                    Key = he.Key,
                    Degree = he.Degree,
                    Quality = he.Quality,
                    Bass = he.Bass
                });
            }

            RecalculateStartPositions();
            RefreshListView(selectIndex: _working.Count > 0 ? 0 : -1);
        }

        // Recompute contiguous start positions from order and durations
        private void RecalculateStartPositions()
        {
            int bar = 1;
            int beat = 1;

            foreach (var w in _working)
            {
                w.StartBar = bar;
                w.StartBeat = beat;

                // advance by duration in beats
                int remaining = w.DurationBeats;
                int localBeatIndex = beat - 1; // 0-based index within bar
                remaining += localBeatIndex;

                // compute new bar/beat after advancing
                bar += remaining / _beatsPerBar;
                int newBeatIndex = remaining % _beatsPerBar;
                beat = newBeatIndex + 1;
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

        private (int bar, int beat) PreviewStartForIndex(int insertAt)
        {
            int bar = 1;
            int beat = 1;
            for (int i = 0; i < insertAt && i < _working.Count; i++)
            {
                var w = _working[i];
                int remaining = w.DurationBeats + (beat - 1);
                bar += remaining / _beatsPerBar;
                beat = (remaining % _beatsPerBar) + 1;
            }
            return (bar, beat);
        }

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
            key = string.IsNullOrWhiteSpace(_cbKey.Text) ? "C major" : _cbKey.Text.Trim();
            degree = (int)_numDegree.Value;
            quality = string.IsNullOrWhiteSpace(_cbQuality.Text) ? "maj" : _cbQuality.Text.Trim();
            bass = string.IsNullOrWhiteSpace(_cbBass.Text) ? "root" : _cbBass.Text.Trim();
            duration = (int)_numDuration.Value;

            if (duration < 1)
                error = "Duration must be at least 1 beat.";

            if (degree < 1 || degree > 7)
                error = (error == null) ? "Degree must be 1..7." : error + " Degree must be 1..7.";

            if (_beatsPerBar < 1)
                error = (error == null) ? "Invalid meter." : error + " Invalid meter.";

            startPreview = PreviewStartForIndex(insertAt);

            return error == null;
        }
    }
}
