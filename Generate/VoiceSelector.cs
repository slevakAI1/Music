using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Music.Generate
{
    // Multi-select voice picker powered by a Notion JSON voice catalog.
    public sealed class VoiceSelector : Form
    {
        private readonly ListBox _lstCategories;
        private readonly CheckedListBox _clbVoices;
        private readonly TextBox _txtFilter;
        private readonly Button _btnSelectAll;
        private readonly Button _btnClear;
        private readonly Button _btnOk;
        private readonly Button _btnCancel;
        private readonly Label _lblSource;

        private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _catalog;
        private readonly HashSet<string> _selected = new(StringComparer.OrdinalIgnoreCase);
        private string? _currentCategory;

        public List<string> SelectedVoices { get; } = new();

        public VoiceSelector()
        {
            Text = "Select Voices";
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(780, 520);

            _catalog = VoiceCatalog.Load(out var sourcePath);

            // Layout root
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(root);

            // Left: Categories
            var leftPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            root.Controls.Add(leftPanel, 0, 0);

            _lstCategories = new ListBox
            {
                Dock = DockStyle.Fill,
                IntegralHeight = false,             // prevent clipping behind a docked label
                Margin = new Padding(0, 4, 0, 0)    // space under the header
            };
            _lstCategories.SelectedIndexChanged += OnCategoryChanged;

            var lblCat = new Label
            {
                Text = "Categories",
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)   // small bottom margin for clearer separation
            };

            // Important: add Fill control first, then Top label so label docks first and doesn't overlap the list.
            leftPanel.Controls.Add(_lstCategories);
            leftPanel.Controls.Add(lblCat);

            // Right: Filter + Voices + Buttons
            var rightPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            rightPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // filter
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // list
            rightPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // actions
            rightPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // ok/cancel
            root.Controls.Add(rightPanel, 1, 0);

            var filterPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(8, 8, 8, 4)
            };
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            rightPanel.Controls.Add(filterPanel, 0, 0);

            var lblFilter = new Label
            {
                Text = "Filter",
                AutoSize = true,                         // let it size to text
                Dock = DockStyle.None,                   // no fill; avoid vertical centering
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Margin = new Padding(0, 2, 8, 0),        // small top offset to align with textbox border
                TextAlign = ContentAlignment.TopLeft
            };
            filterPanel.Controls.Add(lblFilter, 0, 0);

            _txtFilter = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)                  // align with label
            };
            _txtFilter.TextChanged += (_, __) => RefreshVoices();
            filterPanel.Controls.Add(_txtFilter, 1, 0);

            _clbVoices = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true
            };
            _clbVoices.ItemCheck += OnVoiceItemCheck;
            rightPanel.Controls.Add(_clbVoices, 0, 1);

            var actionPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Top,
                Padding = new Padding(8, 4, 8, 4),
                AutoSize = true
            };
            rightPanel.Controls.Add(actionPanel, 0, 2);

            _btnSelectAll = new Button { Text = "Select All in View" };
            _btnSelectAll.Click += (_, __) => SelectAllInView();
            actionPanel.Controls.Add(_btnSelectAll);

            _btnClear = new Button { Text = "Clear in View" };
            _btnClear.Click += (_, __) => ClearInView();
            actionPanel.Controls.Add(_btnClear);

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
                Padding = new Padding(8, 4, 8, 8),
                AutoSize = true
            };
            rightPanel.Controls.Add(okCancelPanel, 0, 3);

            _btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK };
            _btnOk.Click += OnOk;
            _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };

            okCancelPanel.Controls.Add(_btnOk);
            okCancelPanel.Controls.Add(_btnCancel);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            Load += (_, __) => InitializeData();
        }

        private static string BuildSourceLabel(string? sourcePath)
        {
            return sourcePath is { Length: > 0 }
                ? $"Source: {sourcePath}"
                : "Source: catalog load error (see 'error' category)";
        }

        private void InitializeData()
        {
            var categories = _catalog.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
            _lstCategories.Items.Clear();
            _lstCategories.Items.AddRange(categories.Cast<object>().ToArray());
            if (_lstCategories.Items.Count > 0)
                _lstCategories.SelectedIndex = 0;
        }

        private void OnCategoryChanged(object? sender, EventArgs e)
        {
            _currentCategory = _lstCategories.SelectedItem as string;
            RefreshVoices();
        }

        private void RefreshVoices()
        {
            _clbVoices.BeginUpdate();
            try
            {
                _clbVoices.Items.Clear();

                if (_currentCategory == null) return;

                if (!_catalog.TryGetValue(_currentCategory, out var voices)) return;

                var filter = _txtFilter.Text?.Trim();
                IEnumerable<string> view = voices;
                if (!string.IsNullOrEmpty(filter))
                {
                    view = view.Where(v => v.Contains(filter, StringComparison.OrdinalIgnoreCase));
                }

                foreach (var v in view)
                {
                    var idx = _clbVoices.Items.Add(v);
                    if (_selected.Contains(v))
                        _clbVoices.SetItemChecked(idx, true);
                }
            }
            finally
            {
                _clbVoices.EndUpdate();
            }
        }

        private void OnVoiceItemCheck(object? sender, ItemCheckEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _clbVoices.Items.Count) return;
            var name = _clbVoices.Items[e.Index]?.ToString();
            if (string.IsNullOrWhiteSpace(name)) return;

            if (e.NewValue == CheckState.Checked)
                _selected.Add(name);
            else
                _selected.Remove(name);
        }

        private void SelectAllInView()
        {
            for (int i = 0; i < _clbVoices.Items.Count; i++)
            {
                var name = _clbVoices.Items[i]?.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    _selected.Add(name);
                    _clbVoices.SetItemChecked(i, true);
                }
            }
        }

        private void ClearInView()
        {
            for (int i = 0; i < _clbVoices.Items.Count; i++)
            {
                var name = _clbVoices.Items[i]?.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    _selected.Remove(name);
                    _clbVoices.SetItemChecked(i, false);
                }
            }
        }

        private void OnOk(object? sender, EventArgs e)
        {
            SelectedVoices.Clear();
            SelectedVoices.AddRange(_selected.OrderBy(s => s, StringComparer.OrdinalIgnoreCase));
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}