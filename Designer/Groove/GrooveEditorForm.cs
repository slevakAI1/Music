using Music.Generator;
using Music.Writer;
using System.Reflection;

namespace Music.Designer
{
    // Popup editor for arranging Groove Events
    public sealed class GrooveEditorForm : Form
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
        private readonly ComboBox _cbPresetName;

        // Working list mirroring the ListView
        private readonly List<WorkingEvent> _working = new();

        // Drag-and-drop support
        private ListViewItem? _dragItem;

        // Suppress feedback updates while programmatically changing editor controls
        private bool _suppressEditorApply;

        public GrooveTrack ResultTimeline { get; private set; } = new GrooveTrack();

        private sealed class WorkingEvent
        {
            public int StartBar { get; set; } = 1;
            public int StartBeat { get; set; } = 1;
            public string GroovePresetName { get; set; } = string.Empty;
        }

        public GrooveEditorForm(GrooveTrack? initial = null)
        {
            Text = "Edit Groove";
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
            _lv.Columns.Add("Preset Name", 200, HorizontalAlignment.Left);

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
            _btnDefaults.Click += (s, e) => ApplyDefaultTimeline();

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
            var lblPresetName = new Label { Text = "Preset Name:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) };

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

            _cbPresetName = new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cbPresetName.SelectedIndexChanged += (s, e) => ApplyEditorToSelected();

            // Populate preset names using reflection
            PopulatePresetNames();

            // layout rows
            int row = 0;
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // title
            editor.Controls.Add(new Label { Text = "Selected Groove Event", AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) }, 0, row);
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
            editor.Controls.Add(lblPresetName, 0, row);
            editor.Controls.Add(_cbPresetName, 1, row);
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

            // Build ResultTimeline only when OK
            _btnOk.Click += (s, e) =>
            {
                ResultTimeline = BuildResult();
                DialogResult = DialogResult.OK;
                Close();
            };
        }

        private void PopulatePresetNames()
        {
            // Use reflection to get all public static methods from GroovePresets that return GroovePreset
            var methods = typeof(GroovePresets).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.ReturnType == typeof(GroovePreset) && m.Name.StartsWith("Get") && m.Name != "GetByName")
                .ToList();

            _cbPresetName.BeginUpdate();
            _cbPresetName.Items.Clear();

            foreach (var method in methods)
            {
                // Remove "Get" prefix from method name
                var presetName = method.Name.Substring(3);
                _cbPresetName.Items.Add(presetName);
            }

            _cbPresetName.EndUpdate();

            if (_cbPresetName.Items.Count > 0 && string.IsNullOrEmpty(_cbPresetName.Text))
            {
                _cbPresetName.SelectedIndex = 0;
            }
        }

        private void LoadInitial(GrooveTrack? initial)
        {
            _working.Clear();

            if (initial == null || initial.Events.Count == 0)
            {
                return;
            }

            // Sort events by start position
            var sorted = initial.Events
                .OrderBy(e => e.StartBar)
                .ToList();

            foreach (var ev in sorted)
            {
                _working.Add(new WorkingEvent
                {
                    StartBar = ev.StartBar,
                    GroovePresetName = ev.SourcePresetName
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
                var item = new ListViewItem((i + 1).ToString());
                item.SubItems.Add(w.StartBar.ToString());
                item.SubItems.Add(w.StartBeat.ToString());
                item.SubItems.Add(w.GroovePresetName);
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
            it.SubItems[3].Text = w.GroovePresetName;
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

            _numStartBar.Enabled = _numStartBeat.Enabled = _cbPresetName.Enabled = true;

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
                _cbPresetName.Text = w.GroovePresetName ?? string.Empty;
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
            w.GroovePresetName = _cbPresetName.Text?.Trim() ?? string.Empty;

            UpdateRowVisuals(_lv.SelectedIndices[0]);
            UpdateButtonsEnabled();
        }

        private void AddEvent()
        {
            if (!ValidateEditorValues(out var error))
            {
                MessageBoxHelper.Show(error!, "Add Groove Event", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var w = new WorkingEvent
            {
                StartBar = (int)_numStartBar.Value,
                StartBeat = (int)_numStartBeat.Value,
                GroovePresetName = _cbPresetName.Text?.Trim() ?? string.Empty
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
                MessageBoxHelper.Show(error!, "Insert Groove Event", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int insertAt = _lv.SelectedIndices.Count > 0 ? _lv.SelectedIndices[0] : 0;

            var w = new WorkingEvent
            {
                StartBar = (int)_numStartBar.Value,
                StartBeat = (int)_numStartBeat.Value,
                GroovePresetName = _cbPresetName.Text?.Trim() ?? string.Empty
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
                int nextBar = _working.Count > 0
                    ? _working.Max(w => w.StartBar) + 1
                    : 1;

                _numStartBar.Value = Math.Max(_numStartBar.Minimum, Math.Min(_numStartBar.Maximum, nextBar));
                _numStartBeat.Value = 1;
                if (_cbPresetName.Items.Count > 0 && string.IsNullOrEmpty(_cbPresetName.Text))
                {
                    _cbPresetName.SelectedIndex = 0;
                }
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
                StartBar = src.StartBar + 1,
                StartBeat = src.StartBeat,
                GroovePresetName = src.GroovePresetName
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

        private GrooveTrack BuildResult()
        {
            var tl = new GrooveTrack();

            foreach (var w in _working)
            {
                tl.Add(new GrooveInstance
                {
                    StartBar = w.StartBar,
                    SourcePresetName = w.GroovePresetName
                });
            }

            return tl;
        }

        private void ApplyDefaultTimeline()
        {
            _working.Clear();

            // Add a default groove event at bar 1
            _working.Add(new WorkingEvent
            {
                StartBar = 1,
                StartBeat = 1,
                GroovePresetName = _cbPresetName.Items.Count > 0 ? _cbPresetName.Items[0].ToString()! : string.Empty
            });

            RefreshListView(selectIndex: 0);
        }

        private bool ValidateEditorValues(out string? error)
        {
            error = null;
            int startBar = (int)_numStartBar.Value;
            int startBeat = (int)_numStartBeat.Value;
            string presetName = _cbPresetName.Text?.Trim() ?? string.Empty;

            if (startBar < 1)
                error = "Start Bar must be at least 1.";
            if (startBeat < 1)
                error = (error == null) ? "Start Beat must be at least 1." : error + " Start Beat must be at least 1.";
            if (string.IsNullOrWhiteSpace(presetName))
                error = (error == null) ? "Preset Name is required." : error + " Preset Name is required.";

            return error == null;
        }
    }
}