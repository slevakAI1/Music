using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Music.Designer
{
    // Multi-select voice picker powered by a Notion JSON voice catalog.
    public sealed class VoiceSelectorForm : Form
    {
        private readonly ListBox _lstCategories;
        private readonly DataGridView _dgvVoices;
        private readonly DataGridView _dgvSelected;
        private readonly TextBox _txtFilter;
        private readonly Button _btnSelectAll;
        private readonly Button _btnClear;
        private readonly Button _btnDefaults;
        private readonly Button _btnOk;
        private readonly Button _btnCancel;
        private readonly Label _lblSource;

        private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _catalog;
        private readonly Dictionary<string, string> _selectedVoicesWithRoles = new(StringComparer.OrdinalIgnoreCase);
        private string? _currentCategory;

        public Dictionary<string, string> SelectedVoicesWithRoles { get; } = new();

        public VoiceSelectorForm()
        {
            Text = "Select Voices";

            // Fixed, non-resizable dialog that's right-sized for use
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new Size(900, 520);

            _catalog = VoiceCatalog.Load(out var sourcePath);

            // Layout root with padding so inner controls don't touch form edges
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(8)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            Controls.Add(root);

            // Left: Categories
            var leftPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) };
            root.Controls.Add(leftPanel, 0, 0);

            _lstCategories = new ListBox
            {
                Dock = DockStyle.Fill,
                IntegralHeight = false,
                Margin = new Padding(0, 4, 0, 0)
            };
            _lstCategories.SelectedIndexChanged += OnCategoryChanged;

            var lblCat = new Label
            {
                Text = "Categories",
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            // Add fill control first so top label docks above without overlap
            leftPanel.Controls.Add(_lstCategories);
            leftPanel.Controls.Add(lblCat);

            // Middle: Filter + Voices + Buttons
            var middlePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(0)
            };
            middlePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // filter
            middlePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // list
            middlePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // actions
            middlePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // ok/cancel
            root.Controls.Add(middlePanel, 1, 0);

            var filterPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 0, 0, 4)
            };
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            middlePanel.Controls.Add(filterPanel, 0, 0);

            var lblFilter = new Label
            {
                Text = "Filter",
                AutoSize = true,
                Dock = DockStyle.None,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Margin = new Padding(0, 2, 8, 0),
                TextAlign = ContentAlignment.TopLeft
            };
            filterPanel.Controls.Add(lblFilter, 0, 0);

            _txtFilter = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            _txtFilter.TextChanged += (_, __) => RefreshVoices();
            filterPanel.Controls.Add(_txtFilter, 1, 0);

            _dgvVoices = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Add checkbox column for selection
            var checkColumn = new DataGridViewCheckBoxColumn
            {
                Name = "Selected",
                HeaderText = "Select",
                Width = 50,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };
            _dgvVoices.Columns.Add(checkColumn);

            // Add voice name column
            var nameColumn = new DataGridViewTextBoxColumn
            {
                Name = "VoiceName",
                HeaderText = "Voice Name",
                ReadOnly = true,
                FillWeight = 70
            };
            _dgvVoices.Columns.Add(nameColumn);

            // Add groove role dropdown column
            var roleColumn = new DataGridViewComboBoxColumn
            {
                Name = "GrooveRole",
                HeaderText = "Groove Role",
                FillWeight = 30,
                DataSource = VoiceSet.ValidGrooveRoles.ToList()
            };
            _dgvVoices.Columns.Add(roleColumn);

            _dgvVoices.CellValueChanged += OnVoiceCellValueChanged;
            _dgvVoices.CurrentCellDirtyStateChanged += OnVoiceCellDirtyStateChanged;
            middlePanel.Controls.Add(_dgvVoices, 0, 1);

            var actionPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 4, 0, 4),
                AutoSize = true
            };
            middlePanel.Controls.Add(actionPanel, 0, 2);

            _btnSelectAll = new Button { Text = "Select All" };
            _btnSelectAll.Click += (_, __) => SelectAllInView();
            actionPanel.Controls.Add(_btnSelectAll);

            _btnClear = new Button { Text = "Clear" };
            _btnClear.Click += (_, __) => ClearInView();
            actionPanel.Controls.Add(_btnClear);

            _btnDefaults = new Button { Text = "Set Defaults", AutoSize = true };
            _btnDefaults.Click += OnSetDefaults;
            actionPanel.Controls.Add(_btnDefaults);

            _lblSource = new Label
            {
                Text = BuildSourceLabel(sourcePath),
                AutoSize = true,
                Padding = new Padding(12, 6, 0, 0)
            };
            actionPanel.Controls.Add(_lblSource);

            var okCancelPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 4, 0, 0),
                AutoSize = true
            };
            middlePanel.Controls.Add(okCancelPanel, 0, 3);

            _btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK };
            _btnOk.Click += OnOk;
            _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };

            okCancelPanel.Controls.Add(_btnOk);
            okCancelPanel.Controls.Add(_btnCancel);

            // Right: Selected Voices Display
            var rightPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4, 0, 0, 0) };
            root.Controls.Add(rightPanel, 2, 0);

            var lblSelected = new Label
            {
                Text = "Selected Voices",
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            _dgvSelected = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Margin = new Padding(0, 4, 0, 0)
            };

            var selectedNameColumn = new DataGridViewTextBoxColumn
            {
                Name = "VoiceName",
                HeaderText = "Voice Name",
                FillWeight = 60
            };
            _dgvSelected.Columns.Add(selectedNameColumn);

            var selectedRoleColumn = new DataGridViewTextBoxColumn
            {
                Name = "GrooveRole",
                HeaderText = "Groove Role",
                FillWeight = 40
            };
            _dgvSelected.Columns.Add(selectedRoleColumn);

            rightPanel.Controls.Add(_dgvSelected);
            rightPanel.Controls.Add(lblSelected);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            Load += (_, __) => InitializeData();
        }

        private static string BuildSourceLabel(string? sourcePath)
        {
            // show only file name without extension (no full path)
            return sourcePath is { Length: > 0 }
                ? $"Source: {Path.GetFileNameWithoutExtension(sourcePath)}"
                : "Source: catalog load error (see 'error' category)";
        }

        private void InitializeData()
        {
            var categories = _catalog.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
            _lstCategories.Items.Clear();
            _lstCategories.Items.AddRange(categories.Cast<object>().ToArray());

            if (_lstCategories.Items.Count > 0)
            {
                _lstCategories.SelectedIndex = 0;
            }
        }

        // Method to initialize form with existing voices from the designer
        public void SetExistingVoices(Dictionary<string, string> voicesWithRoles)
        {
            if (voicesWithRoles == null) return;

            _selectedVoicesWithRoles.Clear();
            foreach (var kvp in voicesWithRoles)
            {
                _selectedVoicesWithRoles[kvp.Key] = kvp.Value;
            }

            // Refresh views to show selections
            RefreshVoices();
            RefreshSelectedList();
        }

        // Sets the initial default: select the "Rock Band" category and check all voices in it.
        public void SetDefaultVoices()
        {
            const string defaultCategory = "Rock Band";

            var catKey = _catalog.Keys.FirstOrDefault(
                k => string.Equals(k, defaultCategory, StringComparison.OrdinalIgnoreCase));

            _selectedVoicesWithRoles.Clear();

            if (catKey != null && _catalog.TryGetValue(catKey, out var voices) && voices != null)
            {
                foreach (var v in voices)
                {
                    if (!string.IsNullOrWhiteSpace(v))
                        _selectedVoicesWithRoles[v] = "Select...";
                }

                _currentCategory = catKey;

                for (int i = 0; i < _lstCategories.Items.Count; i++)
                {
                    if (string.Equals(_lstCategories.Items[i]?.ToString(), catKey, StringComparison.OrdinalIgnoreCase))
                    {
                        if (_lstCategories.SelectedIndex != i)
                            _lstCategories.SelectedIndex = i;
                        break;
                    }
                }
            }

            RefreshVoices();
            RefreshSelectedList();
        }

        private void OnSetDefaults(object? sender, EventArgs e)
        {
            SetDefaultVoices();
        }

        private void OnCategoryChanged(object? sender, EventArgs e)
        {
            _currentCategory = _lstCategories.SelectedItem as string;
            RefreshVoices();
        }

        private void RefreshVoices()
        {
            _dgvVoices.CellValueChanged -= OnVoiceCellValueChanged;
            _dgvVoices.Rows.Clear();

            if (_currentCategory == null)
            {
                _dgvVoices.CellValueChanged += OnVoiceCellValueChanged;
                return;
            }

            if (!_catalog.TryGetValue(_currentCategory, out var voices))
            {
                _dgvVoices.CellValueChanged += OnVoiceCellValueChanged;
                return;
            }

            var filter = _txtFilter.Text?.Trim();
            IEnumerable<string> view = voices;
            if (!string.IsNullOrEmpty(filter))
            {
                view = view.Where(v => v.Contains(filter, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var v in view)
            {
                var isSelected = _selectedVoicesWithRoles.ContainsKey(v);
                var role = isSelected ? _selectedVoicesWithRoles[v] : "Select...";
                
                _dgvVoices.Rows.Add(isSelected, v, role);
            }

            _dgvVoices.CellValueChanged += OnVoiceCellValueChanged;
        }

        private void RefreshSelectedList()
        {
            _dgvSelected.Rows.Clear();

            var sortedSelected = _selectedVoicesWithRoles
                .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in sortedSelected)
            {
                _dgvSelected.Rows.Add(kvp.Key, kvp.Value);
            }
        }

        private void OnVoiceCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (_dgvVoices.IsCurrentCellDirty)
            {
                _dgvVoices.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void OnVoiceCellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _dgvVoices.Rows.Count) return;

            var row = _dgvVoices.Rows[e.RowIndex];
            var voiceName = row.Cells["VoiceName"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(voiceName)) return;

            var isChecked = row.Cells["Selected"].Value is bool b && b;
            var role = row.Cells["GrooveRole"].Value?.ToString() ?? "Select...";

            if (isChecked)
            {
                _selectedVoicesWithRoles[voiceName] = role;
            }
            else
            {
                _selectedVoicesWithRoles.Remove(voiceName);
            }

            RefreshSelectedList();
        }

        private void SelectAllInView()
        {
            _dgvVoices.CellValueChanged -= OnVoiceCellValueChanged;

            for (int i = 0; i < _dgvVoices.Rows.Count; i++)
            {
                var row = _dgvVoices.Rows[i];
                var voiceName = row.Cells["VoiceName"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(voiceName))
                {
                    row.Cells["Selected"].Value = true;
                    var role = row.Cells["GrooveRole"].Value?.ToString() ?? "Select...";
                    _selectedVoicesWithRoles[voiceName] = role;
                }
            }

            _dgvVoices.CellValueChanged += OnVoiceCellValueChanged;
            RefreshSelectedList();
        }

        private void ClearInView()
        {
            _dgvVoices.CellValueChanged -= OnVoiceCellValueChanged;

            for (int i = 0; i < _dgvVoices.Rows.Count; i++)
            {
                var row = _dgvVoices.Rows[i];
                var voiceName = row.Cells["VoiceName"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(voiceName))
                {
                    row.Cells["Selected"].Value = false;
                    _selectedVoicesWithRoles.Remove(voiceName);
                }
            }

            _dgvVoices.CellValueChanged += OnVoiceCellValueChanged;
            RefreshSelectedList();
        }

        private void OnOk(object? sender, EventArgs e)
        {
            SelectedVoicesWithRoles.Clear();
            foreach (var kvp in _selectedVoicesWithRoles.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                SelectedVoicesWithRoles[kvp.Key] = kvp.Value;
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}