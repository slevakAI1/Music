using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Music.Design
{
    // Popup editor for arranging Tempo Events and configuring their spans (in bars)
    // Mirrors interaction patterns used by TimeSignatureEditorForm and SectionEditorForm.
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
        private readonly NumericUpDown _numBpm;
        private readonly NumericUpDown _numBars;
        private readonly Label _lblStart;

        // Working list mirroring the ListView
        private readonly List<WorkingEvent> _working = new();

        // Suppress feedback updates while programmatically changing editor controls
        private bool _suppressEditorApply;

        public TempoTimeline ResultTimeline { get; private set; } = new TempoTimeline();

        private sealed class WorkingEvent
        {
            public int Bpm { get; set; } = 90;
            public int BarCount { get; set; } = 4;
            public int StartBar { get; set; } = 1; // computed
        }

        public TempoEditorForm(TempoTimeline? initial = null)
        {
            Text = "Edit Tempo";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new Size(860, 480);

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
                MultiSelect = false
            };
            _lv.Columns.Add("Start", 120);
            _lv.Columns.Add("BPM", 80);
            _lv.Columns.Add("Bars", 80);
            _lv.Columns.Add("Span (beats)", 110);
            _lv.SelectedIndexChanged += (_, __) => LoadEditorFromSelection();
            left.Controls.Add(_lv, 0, 0);

            var rowBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight
            };
            left.Controls.Add(rowBtns, 0, 1);

            _btnAdd = new Button { Text = "Add" };
            _btnInsert = new Button { Text = "Insert" };
            _btnDelete = new Button { Text = "Delete" };
            _btnDuplicate = new Button { Text = "Duplicate" };
            _btnUp = new Button { Text = "Up" };
            _btnDown = new Button { Text = "Down" };

            _btnAdd.Click += (_, __) => { AddEvent(); };
            _btnInsert.Click += (_, __) => { InsertEventAtSelection(); };
            _btnDelete.Click += (_, __) => { DeleteSelection(); };
            _btnDuplicate.Click += (_, __) => { DuplicateSelection(); };
            _btnUp.Click += (_, __) => { MoveSelection(-1); };
            _btnDown.Click += (_, __) => { MoveSelection(1); };

            rowBtns.Controls.AddRange(new Control[]
            {
                _btnAdd, _btnInsert, _btnDelete, _btnDuplicate, _btnUp, _btnDown
            });

            // Right: editor + defaults + OK/Cancel
            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(8)
            };
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            root.Controls.Add(right, 1, 0);

            var editor = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3
            };
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            right.Controls.Add(editor, 0, 0);

            // Start label (read-only)
            editor.Controls.Add(new Label { Text = "Start (computed):", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            _lblStart = new Label { Text = "Bar 1", AutoSize = true, Anchor = AnchorStyles.Left };
            editor.Controls.Add(_lblStart, 1, 0);

            // BPM
            editor.Controls.Add(new Label { Text = "BPM:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            _numBpm = new NumericUpDown
            {
                Minimum = 20,
                Maximum = 300,
                Increment = 1,
                Value = 90,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = HorizontalAlignment.Right
            };
            _numBpm.ValueChanged += (_, __) => ApplyEditorToSelection();
            editor.Controls.Add(_numBpm, 1, 1);

            // Bars
            editor.Controls.Add(new Label { Text = "Bars:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            _numBars = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 512,
                Increment = 1,
                Value = 4,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = HorizontalAlignment.Right
            };
            _numBars.ValueChanged += (_, __) => ApplyEditorToSelection();
            editor.Controls.Add(_numBars, 1, 2);

            // Defaults button
            _btnDefaults = new Button { Text = "Set Defaults", Dock = DockStyle.Fill };
            _btnDefaults.Click += (_, __) => ApplyDefaults();
            right.Controls.Add(_btnDefaults, 0, 1);

            // OK/Cancel buttons (visible, like SectionEditor)
            var bottomButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            _btnOk = new Button { Text = "OK", AutoSize = true };
            _btnCancel = new Button { Text = "Cancel", AutoSize = true };
            bottomButtons.Controls.AddRange(new Control[] { _btnOk, _btnCancel });
            right.Controls.Add(bottomButtons, 0, 3);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            // Build initial working set
            if (initial != null && initial.Events.Count > 0)
            {
                // Import existing events
                var imported = initial.Events
                    .OrderBy(e => e.StartBar)
                    .ThenBy(e => e.StartBeat)
                    .Select(e => new WorkingEvent
                    {
                        Bpm = e.TempoBpm,
                        // Approximate bars based on 4 beats/bar when reconstructing UI span.
                        BarCount = Math.Max(1, e.DurationBeats / 4)
                    })
                    .ToList();

                _working.AddRange(imported);
            }
            else
            {
                // Defaults
                _working.Add(new WorkingEvent { Bpm = 90, BarCount = 4 });
            }

            RecomputeStarts();
            RebuildListView();
            if (_lv.Items.Count > 0)
                _lv.Items[0].Selected = true;

            // Commit/cancel like SectionEditor
            _btnOk.Click += (_, __) =>
            {
                ResultTimeline = BuildResultTimeline();
                DialogResult = DialogResult.OK;
                Close();
            };
            _btnCancel.Click += (_, __) =>
            {
                DialogResult = DialogResult.Cancel;
                Close(); // just return
            };
        }

        private TempoTimeline BuildResultTimeline()
        {
            var t = new TempoTimeline();
            int beatsPerBar = 4;
            int startBar = 1;

            foreach (var we in _working)
            {
                t.Add(new TempoEvent
                {
                    StartBar = startBar,
                    StartBeat = 1,
                    TempoBpm = we.Bpm,
                    DurationBeats = we.BarCount * beatsPerBar
                });
                startBar += we.BarCount;
            }

            return t;
        }

        // --- Actions ---

        private void ApplyDefaults()
        {
            _working.Clear();
            _working.Add(new WorkingEvent { Bpm = 90, BarCount = 4 });
            RecomputeStarts();
            RebuildListView();
            SelectRow(0);
        }

        private void AddEvent()
        {
            var bpm = _working.Count > 0 ? _working.Last().Bpm : 90;
            _working.Add(new WorkingEvent { Bpm = bpm, BarCount = 4 });
            RecomputeStarts();
            RebuildListView();
            SelectRow(_working.Count - 1);
        }

        private void InsertEventAtSelection()
        {
            int idx = SelectedIndex();
            if (idx < 0) idx = _working.Count;
            var bpm = idx > 0 ? _working[idx - 1].Bpm : 90;
            _working.Insert(idx, new WorkingEvent { Bpm = bpm, BarCount = 4 });
            RecomputeStarts();
            RebuildListView();
            SelectRow(idx);
        }

        private void DeleteSelection()
        {
            int idx = SelectedIndex();
            if (idx < 0) return;
            _working.RemoveAt(idx);
            RecomputeStarts();
            RebuildListView();
            if (_working.Count > 0) SelectRow(Math.Min(idx, _working.Count - 1));
        }

        private void DuplicateSelection()
        {
            int idx = SelectedIndex();
            if (idx < 0) return;
            var src = _working[idx];
            _working.Insert(idx + 1, new WorkingEvent { Bpm = src.Bpm, BarCount = src.BarCount });
            RecomputeStarts();
            RebuildListView();
            SelectRow(idx + 1);
        }

        private void MoveSelection(int delta)
        {
            int idx = SelectedIndex();
            if (idx < 0) return;
            int newIdx = idx + delta;
            if (newIdx < 0 || newIdx >= _working.Count) return;
            (_working[idx], _working[newIdx]) = (_working[newIdx], _working[idx]);
            RecomputeStarts();
            RebuildListView();
            SelectRow(newIdx);
        }

        // --- UI sync ---

        private int SelectedIndex()
        {
            if (_lv.SelectedItems.Count == 0) return -1;
            return _lv.SelectedItems[0].Index;
        }

        private void SelectRow(int idx)
        {
            if (idx < 0 || idx >= _lv.Items.Count) return;
            _lv.SelectedIndices.Clear();
            _lv.Items[idx].Selected = true;
            _lv.Items[idx].Focused = true;
            _lv.EnsureVisible(idx);
        }

        private void LoadEditorFromSelection()
        {
            int idx = SelectedIndex();
            _suppressEditorApply = true;
            try
            {
                if (idx < 0)
                {
                    _lblStart.Text = "Bar -";
                    return;
                }

                var we = _working[idx];
                _lblStart.Text = $"Bar {we.StartBar}";
                _numBpm.Value = Math.Max(_numBpm.Minimum, Math.Min(_numBpm.Maximum, we.Bpm));
                _numBars.Value = Math.Max(_numBars.Minimum, Math.Min(_numBars.Maximum, we.BarCount));
            }
            finally
            {
                _suppressEditorApply = false;
            }
        }

        private void ApplyEditorToSelection()
        {
            if (_suppressEditorApply) return;

            int idx = SelectedIndex();
            if (idx < 0) return;

            var we = _working[idx];
            we.Bpm = (int)_numBpm.Value;
            we.BarCount = (int)_numBars.Value;

            RecomputeStarts();
            RebuildListView();
            SelectRow(idx);
        }

        private void RecomputeStarts()
        {
            int bar = 1;
            foreach (var we in _working)
            {
                we.StartBar = bar;
                bar += Math.Max(1, we.BarCount);
            }
        }

        private void RebuildListView()
        {
            _lv.BeginUpdate();
            _lv.Items.Clear();

            foreach (var we in _working)
            {
                int beatsPerBar = 4;
                int spanBeats = we.BarCount * beatsPerBar;

                var lvi = new ListViewItem($"Bar {we.StartBar}");
                lvi.SubItems.Add(we.Bpm.ToString());
                lvi.SubItems.Add(we.BarCount.ToString());
                lvi.SubItems.Add(spanBeats.ToString());
                _lv.Items.Add(lvi);
            }

            _lv.EndUpdate();
        }
    }
}