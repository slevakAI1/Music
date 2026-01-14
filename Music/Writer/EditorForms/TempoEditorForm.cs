using Music.Generator;

// AI: purpose=Modal UI for editing tempo events; ResultTrack is built only after OK.
// AI: invariants=StartBar/StartBeat are 1-based; BPM constrained to 20-300; _working typically sorted by start.
// AI: ordering=Add/Insert auto-sort by (StartBar,StartBeat); drag/move intentionally preserve list order and may break chronology.
// AI: deps=TempoTrack/TempoEvent/TempoTests; must run on UI thread; ListView index mapping required for drag/drop.
// AI: change=If sorting behavior changes, update Add/Insert/BuildResult and drag/move logic together.

namespace Music.Writer
{
    // Popup editor for arranging Tempo Events
    public sealed class TempoEditorForm : Form
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
        private readonly NumericUpDown _numStartBar;
        private readonly NumericUpDown _numStartBeat;
        private readonly NumericUpDown _numBpm;

        // Working list mirroring the ListView
        private readonly List<WorkingEvent> _working = new();

        // Drag-and-drop support
        private ListViewItem? _dragItem;

        // Suppress feedback updates while programmatically changing editor controls
        private bool _suppressEditorApply;

        public TempoTrack ResultTrack { get; private set; } = new TempoTrack();

        private sealed class WorkingEvent
        {
            public int StartBar { get; set; } = 1;
            public int StartBeat { get; set; } = 1;
            public int Bpm { get; set; } = 118;
        }

        public TempoEditorForm(TempoTrack? initial = null)
        {
            Text = "Edit Tempo";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new Size(900, 520);

            // Root layout
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

            // Left: list + row action buttons
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
            _lv.Columns.Add("Start Bar", 80, HorizontalAlignment.Right);
            _lv.Columns.Add("Start Beat", 80, HorizontalAlignment.Right);
            _lv.Columns.Add("BPM", 80, HorizontalAlignment.Right);

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

            _btnAdd.Click += (s, e) => AddEvent();
            _btnInsert.Click += (s, e) => InsertEvent();
            _btnDelete.Click += (s, e) => DeleteSelected();
            _btnDuplicate.Click += (s, e) => DuplicateSelected();
            _btnUp.Click += (s, e) => MoveSelected(-1);
            _btnDown.Click += (s, e) => MoveSelected(1);
            _btnDefaults.Click += (s, e) => ApplyDefaultTrack();

            rowButtons.Controls.AddRange(new Control[] { _btnAdd, _btnInsert, _btnDelete, _btnDuplicate, _btnUp, _btnDown });

            // Right: editor and OK/Cancel
            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            root.Controls.Add(right, 1, 0);

            var editor = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(6)
            };
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            right.Controls.Add(editor, 0, 0);

            var lblStartBar = new Label { Text = "Start Bar:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) };
            var lblStartBeat = new Label { Text = "Start Beat:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) };
            var lblBpm = new Label { Text = "BPM:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) };

            _numStartBar = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 9999,
                Value = 1,
                Anchor = AnchorStyles.Left,
                Width = 80
            };
            _numStartBar.ValueChanged += (s, e) => ApplyEditorToSelected();

            _numStartBeat = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 32,
                Value = 1,
                Anchor = AnchorStyles.Left,
                Width = 80
            };
            _numStartBeat.ValueChanged += (s, e) => ApplyEditorToSelected();

            _numBpm = new NumericUpDown
            {
                Minimum = 20,
                Maximum = 300,
                Value = 118,
                Anchor = AnchorStyles.Left,
                Width = 80
            };
            _numBpm.ValueChanged += (s, e) => ApplyEditorToSelected();

            // layout rows
            int row = 0;
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // title
            editor.Controls.Add(new Label { Text = "Selected Tempo Event", AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) }, 0, row);
            editor.SetColumnSpan(editor.GetControlFromPosition(0, row), 2);
            row++;

            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            editor.Controls.Add(lblStartBar, 0, row);
            editor.Controls.Add(_numStartBar, 1, row);
            row++;

            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            editor.Controls.Add(lblStartBeat, 0, row);
            editor.Controls.Add(_numStartBeat, 1, row);
            row++;

            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            editor.Controls.Add(lblBpm, 0, row);
            editor.Controls.Add(_numBpm, 1, row);
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

            // Load data
            LoadInitial(initial);
            RefreshListView(selectIndex: _working.Count > 0 ? 0 : -1);

            // Build ResultTrack only when OK
            _btnOk.Click += (s, e) =>
            {
                ResultTrack = BuildResult();
                DialogResult = DialogResult.OK;
                Close();
            };
        }

        // AI: LoadInitial sorts incoming TempoTrack by StartBar,StartBeat to initialize _working chronologically
        private void LoadInitial(TempoTrack? initial)
        {
            _working.Clear();

            if (initial == null || initial.Events.Count == 0)
            {
                // Start empty
                return;
            }

            // Sort events by start position
            var sorted = initial.Events
                .OrderBy(e => e.StartBar)
                .ThenBy(e => e.StartBeat)
                .ToList();

            foreach (var ev in sorted)
            {
                _working.Add(new WorkingEvent
                {
                    StartBar = ev.StartBar,
                    StartBeat = ev.StartBeat,
                    Bpm = ev.TempoBpm
                });
            }
        }

        private void RefreshListView(int selectIndex = -1)
        {
            _lv.BeginUpdate();
            _lv.Items.Clear();

            for (int i = 0; i < _working.Count; i++)
            {
                var w = _working[i];
                var item = new ListViewItem((i + 1).ToString()); // "#"
                item.SubItems.Add(w.StartBar.ToString()); // "Start Bar"
                item.SubItems.Add(w.StartBeat.ToString()); // "Start Beat"
                item.SubItems.Add(w.Bpm.ToString()); // "BPM"
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
            it.SubItems[1].Text = w.StartBar.ToString();
            it.SubItems[2].Text = w.StartBeat.ToString();
            it.SubItems[3].Text = w.Bpm.ToString();
        }

        private void UpdateButtonsEnabled()
        {
            bool hasSel = _lv.SelectedIndices.Count > 0;
            int idx = hasSel ? _lv.SelectedIndices[0] : -1;

            bool isValid = ValidateEditorValues(out _);

            _btnAdd.Enabled = isValid;
            _btnInsert.Enabled = isValid;
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

            // Always allow editing when none is selected so the user can stage values for Add/Insert
            _numStartBar.Enabled = _numStartBeat.Enabled = _numBpm.Enabled = true;

            if (!hasSel)
            {
                UpdateButtonsEnabled();
                return;
            }

            var w = _lv.SelectedItems[0].Tag as WorkingEvent;
            if (w == null) return;

            _suppressEditorApply = true;
            try
            {
                _numStartBar.Value = Math.Max(_numStartBar.Minimum, Math.Min(_numStartBar.Maximum, w.StartBar));
                _numStartBeat.Value = Math.Max(_numStartBeat.Minimum, Math.Min(_numStartBeat.Maximum, w.StartBeat));
                _numBpm.Value = Math.Max(_numBpm.Minimum, Math.Min(_numBpm.Maximum, w.Bpm));
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
                UpdateButtonsEnabled();
                return;
            }

            var w = (WorkingEvent)_lv.SelectedItems[0].Tag!;

            w.StartBar = (int)_numStartBar.Value;
            w.StartBeat = (int)_numStartBeat.Value;
            w.Bpm = (int)_numBpm.Value;

            UpdateRowVisuals(_lv.SelectedIndices[0]);
            UpdateButtonsEnabled();
        }

        private void AddEvent()
        {
            if (!ValidateEditorValues(out var error))
            {
                MessageBoxHelper.Show(error!, "Add Tempo Event", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var w = new WorkingEvent
            {
                StartBar = (int)_numStartBar.Value,
                StartBeat = (int)_numStartBeat.Value,
                Bpm = (int)_numBpm.Value
            };

            _working.Add(w);
            
            // Sort by start position
            _working.Sort((a, b) =>
            {
                int cmp = a.StartBar.CompareTo(b.StartBar);
                return cmp != 0 ? cmp : a.StartBeat.CompareTo(b.StartBeat);
            });
            
            int newIndex = _working.IndexOf(w);
            RefreshListView(newIndex);
            ResetEditorForNextAdd();
        }

        private void InsertEvent()
        {
            if (!ValidateEditorValues(out var error))
            {
                MessageBoxHelper.Show(error!, "Insert Tempo Event", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int insertAt = _lv.SelectedIndices.Count > 0 ? _lv.SelectedIndices[0] : 0;

            var w = new WorkingEvent
            {
                StartBar = (int)_numStartBar.Value,
                StartBeat = (int)_numStartBeat.Value,
                Bpm = (int)_numBpm.Value
            };

            _working.Insert(insertAt, w);
            
            // Sort by start position
            _working.Sort((a, b) =>
            {
                int cmp = a.StartBar.CompareTo(b.StartBar);
                return cmp != 0 ? cmp : a.StartBeat.CompareTo(b.StartBeat);
            });
            
            int newIndex = _working.IndexOf(w);
            RefreshListView(newIndex);
            ResetEditorForNextAdd();
        }

        private void ResetEditorForNextAdd()
        {
            _lv.SelectedIndices.Clear();

            _suppressEditorApply = true;
            try
            {
                // Suggest next bar after the last event
                int nextBar = _working.Count > 0 
                    ? _working.Max(w => w.StartBar) + 1 
                    : 1;
                
                _numStartBar.Value = Math.Max(_numStartBar.Minimum, Math.Min(_numStartBar.Maximum, nextBar));
                _numStartBeat.Value = 1;
                _numBpm.Value = 118;
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
            int nextSel = Math.Min(idx, _working.Count - 1);
            RefreshListView(nextSel);
        }

        private void DuplicateSelected()
        {
            if (_lv.SelectedIndices.Count == 0) return;
            int idx = _lv.SelectedIndices[0];
            var src = _working[idx];
            var clone = new WorkingEvent
            {
                StartBar = src.StartBar + 1, // Suggest next bar
                StartBeat = src.StartBeat,
                Bpm = src.Bpm
            };
            _working.Insert(idx + 1, clone);
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

            RefreshListView(to);
            _dragItem = null;
        }

        // AI: BuildResult preserves current _working order; it does NOT sort the events before creating the track
        private TempoTrack BuildResult()
        {
            var tl = new TempoTrack();

            foreach (var w in _working)
            {
                tl.Add(new TempoEvent
                {
                    StartBar = w.StartBar,
                    StartBeat = w.StartBeat,
                    TempoBpm = w.Bpm
                });
            }

            return tl;
        }

        private void ApplyDefaultTrack()
        {
            var defaults = TempoTests.CreateTestTrackD1();

            _working.Clear();
            
            foreach (var e in defaults.Events)
            {
                _working.Add(new WorkingEvent
                {
                    StartBar = e.StartBar,
                    StartBeat = e.StartBeat,
                    Bpm = e.TempoBpm
                });
            }
            
            RefreshListView(selectIndex: _working.Count > 0 ? 0 : -1);
        }

        // AI: ValidateEditorValues: checks ranges and returns a user-facing error; does NOT mutate editor state
        private bool ValidateEditorValues(out string? error)
        {
            error = null;
            int startBar = (int)_numStartBar.Value;
            int startBeat = (int)_numStartBeat.Value;
            int bpm = (int)_numBpm.Value;

            if (startBar < 1)
                error = "Start Bar must be at least 1.";
            if (startBeat < 1)
                error = (error == null) ? "Start Beat must be at least 1." : error + " Start Beat must be at least 1.";
            if (bpm < 20)
                error = (error == null) ? "BPM must be at least 20." : error + " BPM must be at least 20.";

            return error == null;
        }
    }
}