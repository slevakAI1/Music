namespace Music.Generate
{
    // Popup editor for arranging Sections: view, add, edit, delete, and drag-reorder.
    public sealed class SectionEditor : Form
    {
        private readonly ListView _lv;
        private readonly Button _btnAdd;
        private readonly Button _btnInsert;
        private readonly Button _btnDelete;
        private readonly Button _btnDuplicate;
        private readonly Button _btnUp;
        private readonly Button _btnDown;
        private readonly Button _btnOk;
        private readonly Button _btnCancel;

        private readonly ComboBox _cbType;
        private readonly NumericUpDown _numBars;
        private readonly TextBox _txtName;
        private readonly Label _lblStart;

        // Working list that mirrors the ListView
        private readonly List<SectionClass> _working = new();

        // Drag-and-drop support
        private ListViewItem? _dragItem;

        public SectionSetClass ResultSections { get; private set; } = new SectionSetClass();

        public SectionEditor(SectionSetClass? initial = null)
        {
            Text = "Edit Sections";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new Size(980, 560);

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
            _lv.Columns.Add("Type", 120, HorizontalAlignment.Left);
            _lv.Columns.Add("Bars", 60, HorizontalAlignment.Right);
            _lv.Columns.Add("Start", 60, HorizontalAlignment.Right);
            _lv.Columns.Add("Name", 220, HorizontalAlignment.Left);

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
                FlowDirection = FlowDirection.LeftToRight
            };
            left.Controls.Add(rowButtons, 0, 1);

            _btnAdd = new Button { Text = "Add", AutoSize = true };
            _btnInsert = new Button { Text = "Insert", AutoSize = true };
            _btnDelete = new Button { Text = "Delete", AutoSize = true };
            _btnDuplicate = new Button { Text = "Duplicate", AutoSize = true };
            _btnUp = new Button { Text = "Move Up", AutoSize = true };
            _btnDown = new Button { Text = "Move Down", AutoSize = true };

            _btnAdd.Click += (s, e) => AddSection(afterIndex: _lv.SelectedIndices.Count > 0 ? _lv.SelectedIndices[0] : (_working.Count - 1));
            _btnInsert.Click += (s, e) => InsertSection(atIndex: _lv.SelectedIndices.Count > 0 ? _lv.SelectedIndices[0] : 0);
            _btnDelete.Click += (s, e) => DeleteSelected();
            _btnDuplicate.Click += (s, e) => DuplicateSelected();
            _btnUp.Click += (s, e) => MoveSelected(-1);
            _btnDown.Click += (s, e) => MoveSelected(1);

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
                RowCount = 8,
                Padding = new Padding(6)
            };
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            right.Controls.Add(editor, 0, 0);

            var lblType = new Label { Text = "Type:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) };
            var lblBars = new Label { Text = "Bars:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) };
            var lblStart = new Label { Text = "Start:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) };
            var lblName = new Label { Text = "Name:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) };

            _cbType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            _cbType.Items.AddRange(Enum.GetNames(typeof(DesignEnums.eSectionType)));
            _cbType.SelectedIndexChanged += (s, e) => ApplyEditorToSelected();

            _numBars = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 512,
                Value = 4,
                Anchor = AnchorStyles.Left,
                Width = 80
            };
            _numBars.ValueChanged += (s, e) => ApplyEditorToSelected();

            _lblStart = new Label { Text = "-", AutoSize = true, Anchor = AnchorStyles.Left };

            _txtName = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            _txtName.TextChanged += (s, e) => ApplyEditorToSelected();

            // layout rows
            int row = 0;
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // spacer/title optional
            editor.Controls.Add(new Label { Text = "Selected Section", AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) }, 0, row);
            editor.SetColumnSpan(editor.GetControlFromPosition(0, row), 2);
            row++;

            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            editor.Controls.Add(lblType, 0, row);
            editor.Controls.Add(_cbType, 1, row);
            row++;

            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            editor.Controls.Add(lblBars, 0, row);
            editor.Controls.Add(_numBars, 1, row);
            row++;

            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            editor.Controls.Add(lblStart, 0, row);
            editor.Controls.Add(_lblStart, 1, row);
            row++;

            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            editor.Controls.Add(lblName, 0, row);
            editor.Controls.Add(_txtName, 1, row);
            row++;

            // fill remainder
            for (; row < editor.RowCount; row++)
                editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var bottomButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            right.Controls.Add(bottomButtons, 0, 1);

            _btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
            _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            bottomButtons.Controls.AddRange(new Control[] { _btnOk, _btnCancel });

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            // Load data
            LoadInitial(initial);
            RefreshListView(selectIndex: _working.Count > 0 ? 0 : -1);

            // Build ResultSections only when OK
            _btnOk.Click += (s, e) =>
            {
                ResultSections = BuildResult();
                DialogResult = DialogResult.OK;
                Close();
            };
        }

        private void LoadInitial(SectionSetClass? initial)
        {
            _working.Clear();

            if (initial == null || initial.Sections.Count == 0)
            {
                // start empty
                return;
            }

            foreach (var s in initial.Sections)
            {
                _working.Add(new SectionClass
                {
                    SectionType = s.SectionType,
                    BarCount = Math.Max(1, s.BarCount),
                    Name = s.Name
                    // StartBar and Id will be handled locally; StartBar recomputed below
                });
            }
            RecalculateStartBars();
        }

        private void RefreshListView(int selectIndex = -1)
        {
            _lv.BeginUpdate();
            _lv.Items.Clear();

            for (int i = 0; i < _working.Count; i++)
            {
                var s = _working[i];
                var item = new ListViewItem((i + 1).ToString()); // "#"
                item.SubItems.Add(s.SectionType.ToString());     // "Type"
                item.SubItems.Add(s.BarCount.ToString());        // "Bars"
                item.SubItems.Add(s.StartBar.ToString());        // "Start"
                item.SubItems.Add(s.Name ?? string.Empty);       // "Name"
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
            it.SubItems[1].Text = s.SectionType.ToString();
            it.SubItems[2].Text = s.BarCount.ToString();
            it.SubItems[3].Text = s.StartBar.ToString();
            it.SubItems[4].Text = s.Name ?? string.Empty;
        }

        private void UpdateButtonsEnabled()
        {
            bool hasSel = _lv.SelectedIndices.Count > 0;
            int idx = hasSel ? _lv.SelectedIndices[0] : -1;

            _btnInsert.Enabled = true;
            _btnAdd.Enabled = true;
            _btnDelete.Enabled = hasSel;
            _btnDuplicate.Enabled = hasSel;
            _btnUp.Enabled = hasSel && idx > 0;
            _btnDown.Enabled = hasSel && idx >= 0 && idx < _working.Count - 1;
        }

        private void OnListSelectionChanged(object? sender, EventArgs e)
        {
            UpdateEditorFromSelected();
            UpdateButtonsEnabled();
        }

        private void UpdateEditorFromSelected()
        {
            if (_lv.SelectedIndices.Count == 0)
            {
                _cbType.Enabled = _numBars.Enabled = _txtName.Enabled = false;
                _cbType.SelectedIndex = -1;
                _numBars.Value = 4;
                _txtName.Text = string.Empty;
                _lblStart.Text = "-";
                return;
            }

            var s = _lv.SelectedItems[0].Tag as SectionClass;
            if (s == null) return;

            _cbType.Enabled = _numBars.Enabled = _txtName.Enabled = true;

            var names = Enum.GetNames(typeof(DesignEnums.eSectionType));
            int idxType = Array.IndexOf(names, s.SectionType.ToString());
            _cbType.SelectedIndex = Math.Max(0, idxType);

            _numBars.Value = Math.Max(1, Math.Min((int)_numBars.Maximum, s.BarCount));
            _txtName.Text = s.Name ?? string.Empty;
            _lblStart.Text = s.StartBar.ToString();
        }

        private void ApplyEditorToSelected()
        {
            if (_lv.SelectedIndices.Count == 0) return;

            var s = (SectionClass)_lv.SelectedItems[0].Tag!;
            if (_cbType.SelectedIndex >= 0)
            {
                var names = Enum.GetNames(typeof(DesignEnums.eSectionType));
                var selectedName = names[_cbType.SelectedIndex];
                if (Enum.TryParse<DesignEnums.eSectionType>(selectedName, out var et))
                {
                    s.SectionType = et;
                }
            }
            s.BarCount = (int)_numBars.Value;
            s.Name = string.IsNullOrWhiteSpace(_txtName.Text) ? null : _txtName.Text.Trim();

            RecalculateStartBars();
            UpdateRowVisuals(_lv.SelectedIndices[0]);
        }

        private void AddSection(int afterIndex)
        {
            // append if none selected
            int insertAt = Math.Max(-1, afterIndex) + 1;
            var s = new SectionClass
            {
                SectionType = DesignEnums.eSectionType.Verse,
                BarCount = 4,
                Name = null
            };
            if (insertAt < 0 || insertAt > _working.Count) insertAt = _working.Count;
            _working.Insert(insertAt, s);
            RecalculateStartBars();
            RefreshListView(insertAt);
        }

        private void InsertSection(int atIndex)
        {
            int insertAt = Math.Max(0, Math.Min(atIndex, _working.Count));
            var s = new SectionClass
            {
                SectionType = DesignEnums.eSectionType.Verse,
                BarCount = 4,
                Name = null
            };
            _working.Insert(insertAt, s);
            RecalculateStartBars();
            RefreshListView(insertAt);
        }

        private void DeleteSelected()
        {
            if (_lv.SelectedIndices.Count == 0) return;
            int idx = _lv.SelectedIndices[0];
            _working.RemoveAt(idx);
            RecalculateStartBars();
            int nextSel = Math.Min(idx, _working.Count - 1);
            RefreshListView(nextSel);
        }

        private void DuplicateSelected()
        {
            if (_lv.SelectedIndices.Count == 0) return;
            int idx = _lv.SelectedIndices[0];
            var src = _working[idx];
            var clone = new SectionClass
            {
                SectionType = src.SectionType,
                BarCount = src.BarCount,
                Name = src.Name
            };
            _working.Insert(idx + 1, clone);
            RecalculateStartBars();
            RefreshListView(idx + 1);
        }

        private void MoveSelected(int delta)
        {
            if (_lv.SelectedIndices.Count == 0) return;
            int idx = _lv.SelectedIndices[0];
            int newIdx = idx + delta;
            if (newIdx < 0 || newIdx >= _working.Count) return;

            var s = _working[idx];
            _working.RemoveAt(idx);
            _working.Insert(newIdx, s);
            RecalculateStartBars();
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

            var s = _working[from];
            _working.RemoveAt(from);
            if (to >= _working.Count) _working.Add(s);
            else _working.Insert(to, s);

            RecalculateStartBars();
            RefreshListView(to);
            _dragItem = null;
        }

        private void RecalculateStartBars()
        {
            int start = 1;
            foreach (var s in _working)
            {
                s.StartBar = start;
                start += Math.Max(1, s.BarCount);
            }

            // Update visible "Start" and "#"
            for (int i = 0; i < _lv.Items.Count && i < _working.Count; i++)
                UpdateRowVisuals(i);

            // Update editor label if selection exists
            if (_lv.SelectedIndices.Count > 0)
            {
                var cur = _working[_lv.SelectedIndices[0]];
                _lblStart.Text = cur.StartBar.ToString();
            }
        }

        private SectionSetClass BuildResult()
        {
            var result = new SectionSetClass();
            foreach (var s in _working)
            {
                result.Add(s.SectionType, s.BarCount, s.Name);
            }
            // StartBar is assigned by Add in order; no extra work needed
            return result;
        }
    }
}