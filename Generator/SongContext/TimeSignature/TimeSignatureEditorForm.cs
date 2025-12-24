using Music.Generator;
using Music.Writer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Music.Designer
{
    // Popup editor for arranging Time Signature Events
    public sealed class TimeSignatureEditorForm : Form
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
        private readonly NumericUpDown _numNumerator;
        private readonly NumericUpDown _numDenominator;

        // Working list mirroring the ListView
        private readonly List<WorkingEvent> _working = new();

        // Drag-and-drop support
        private ListViewItem? _dragItem;

        // Suppress feedback updates while programmatically changing editor controls
        private bool _suppressEditorApply;

        public TimeSignatureTrack ResultTrack { get; private set; } = new TimeSignatureTrack();

        private sealed class WorkingEvent
        {
            public int StartBar { get; set; } = 1;
            public int StartBeat { get; set; } = 1;
            public int Numerator { get; set; } = 4;
            public int Denominator { get; set; } = 4;
        }

        public TimeSignatureEditorForm(TimeSignatureTrack? initial = null)
        {
            Text = "Edit Time Signatures";
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
            _lv.Columns.Add("Meter", 120, HorizontalAlignment.Left);

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
                RowCount = 6,
                Padding = new Padding(6)
            };
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            right.Controls.Add(editor, 0, 0);

            var lblStartBar = new Label { Text = "Start Bar:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) };
            var lblMeter = new Label { Text = "Meter:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) };

            _numStartBar = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 9999,
                Value = 1,
                Anchor = AnchorStyles.Left,
                Width = 80
            };
            _numStartBar.ValueChanged += (s, e) => ApplyEditorToSelected();

            _numNumerator = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 32,
                Value = 4,
                Anchor = AnchorStyles.Left,
                Width = 80
            };
            _numNumerator.ValueChanged += (s, e) => ApplyEditorToSelected();

            _numDenominator = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 64,
                Value = 4,
                Anchor = AnchorStyles.Left,
                Width = 80
            };
            _numDenominator.ValueChanged += (s, e) => ApplyEditorToSelected();

            // layout rows
            int row = 0;
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // title
            editor.Controls.Add(new Label { Text = "Selected Time Signature", AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) }, 0, row);
            editor.SetColumnSpan(editor.GetControlFromPosition(0, row), 2);
            row++;

            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            editor.Controls.Add(lblStartBar, 0, row);
            editor.Controls.Add(_numStartBar, 1, row);
            row++;

            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            var meterPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true };
            // Compact meter layout: [numerator] [/] [denominator]
            var slashLabel = new Label { Text = "/", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(6, 6, 6, 0) };
            meterPanel.Controls.AddRange(new Control[] { _numNumerator, slashLabel, _numDenominator });
            editor.Controls.Add(lblMeter, 0, row);
            editor.Controls.Add(meterPanel, 1, row);
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

        private void LoadInitial(TimeSignatureTrack? initial)
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
                    Numerator = Math.Max(1, ev.Numerator),
                    Denominator = Math.Max(1, ev.Denominator)
                });
            }
        }

        private void RefreshListView(int selectIndex = -1)
        {
            _lv.BeginUpdate();
            _lv.Items.Clear();

            for (int i = 0; i < _working.Count; i++)
            {
                var s = _working[i];
                var item = new ListViewItem((i + 1).ToString()); // "#"
                item.SubItems.Add(s.StartBar.ToString()); // "Start Bar"
                item.SubItems.Add($"{s.Numerator}/{s.Denominator}"); // "Meter"
                item.Tag = s;
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
            var s = _working[index];
            var it = _lv.Items[index];
            it.Text = (index + 1).ToString();
            it.SubItems[1].Text = s.StartBar.ToString();
            it.SubItems[2].Text = $"{s.Numerator}/{s.Denominator}";
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
            _numStartBar.Enabled = _numNumerator.Enabled = _numDenominator.Enabled = true;

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
                _numNumerator.Value = Math.Max(_numNumerator.Minimum, Math.Min(_numNumerator.Maximum, w.Numerator));
                _numDenominator.Value = Math.Max(_numDenominator.Minimum, Math.Min(_numDenominator.Maximum, w.Denominator));
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
            w.Numerator = (int)_numNumerator.Value;
            w.Denominator = (int)_numDenominator.Value;

            UpdateRowVisuals(_lv.SelectedIndices[0]);
            UpdateButtonsEnabled();
        }

        private void AddEvent()
        {
            if (!ValidateEditorValues(out var error))
            {
                MessageBoxHelper.Show(error!, "Add Time Signature", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var w = new WorkingEvent
            {
                StartBar = (int)_numStartBar.Value,
                StartBeat = 1,
                Numerator = (int)_numNumerator.Value,
                Denominator = (int)_numDenominator.Value
            };

            _working.Add(w);
            
            // Sort by start position
            _working.Sort((a, b) => a.StartBar.CompareTo(b.StartBar));
            
            int newIndex = _working.IndexOf(w);
            RefreshListView(newIndex);
            ResetEditorForNextAdd();
        }

        private void InsertEvent()
        {
            if (!ValidateEditorValues(out var error))
            {
                MessageBoxHelper.Show(error!, "Insert Time Signature", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int insertAt = _lv.SelectedIndices.Count > 0 ? _lv.SelectedIndices[0] : 0;

            var w = new WorkingEvent
            {
                StartBar = (int)_numStartBar.Value,
                StartBeat = 1,
                Numerator = (int)_numNumerator.Value,
                Denominator = (int)_numDenominator.Value
            };

            _working.Insert(insertAt, w);
            
            // Sort by start position
            _working.Sort((a, b) => a.StartBar.CompareTo(b.StartBar));
            
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
                _numNumerator.Value = 4;
                _numDenominator.Value = 4;
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
                Numerator = src.Numerator,
                Denominator = src.Denominator
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

        private TimeSignatureTrack BuildResult()
        {
            var tl = new TimeSignatureTrack();
            tl.ConfigureGlobal("4/4");

            foreach (var w in _working)
            {
                tl.Add(new TimeSignatureEvent
                {
                    StartBar = w.StartBar,
                    StartBeat = w.StartBeat,
                    Numerator = w.Numerator,
                    Denominator = w.Denominator
                });
            }

            return tl;
        }

        private void ApplyDefaultTrack()
        {
            var defaults = TimeSignatureTests.CreateTestTrackD1();

            _working.Clear();
            
            foreach (var e in defaults.Events)
            {
                _working.Add(new WorkingEvent
                {
                    StartBar = e.StartBar,
                    StartBeat = e.StartBeat,
                    Numerator = Math.Max(1, e.Numerator),
                    Denominator = Math.Max(1, e.Denominator)
                });
            }
            
            RefreshListView(selectIndex: _working.Count > 0 ? 0 : -1);
        }

        private bool ValidateEditorValues(out string? error)
        {
            error = null;
            int startBar = (int)_numStartBar.Value;
            int numerator = (int)_numNumerator.Value;
            int denominator = (int)_numDenominator.Value;

            if (startBar < 1)
                error = "Start Bar must be at least 1.";
            if (numerator < 1)
                error = (error == null) ? "Numerator must be at least 1." : error + " Numerator must be at least 1.";
            if (denominator < 1)
                error = (error == null) ? "Denominator must be at least 1." : error + " Denominator must be at least 1.";

            return error == null;
        }
    }
}
