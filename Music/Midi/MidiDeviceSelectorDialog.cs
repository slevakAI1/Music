// AI: purpose=Modal dialog letting user pick a MIDI output device from available system devices.
// AI: invariants=SelectedDeviceName is null until OK; populates from MidiPlaybackService.EnumerateOutputDevices.
// AI: deps=MidiPlaybackService for device enumeration; DryWetMidi OutputDevice under the hood.

namespace Music.MyMidi;

// AI: Simple modal selector for external MIDI output devices (e.g., USB-MIDI interfaces).
// AI: Returns SelectedDeviceName on DialogResult.OK; null on cancel or no selection.
internal sealed class MidiDeviceSelectorDialog : Form
{
    private readonly ListBox _lstDevices;
    private readonly Button _btnOk;
    private readonly Button _btnCancel;
    private readonly Label _lblPrompt;

    public string? SelectedDeviceName { get; private set; }

    public MidiDeviceSelectorDialog(IEnumerable<string> deviceNames)
    {
        ArgumentNullException.ThrowIfNull(deviceNames);

        _lblPrompt = new Label();
        _lstDevices = new ListBox();
        _btnOk = new Button();
        _btnCancel = new Button();

        SuspendLayout();

        // _lblPrompt
        _lblPrompt.AutoSize = true;
        _lblPrompt.Location = new Point(12, 12);
        _lblPrompt.Name = "_lblPrompt";
        _lblPrompt.Size = new Size(300, 15);
        _lblPrompt.Text = "Select MIDI output device:";

        // _lstDevices
        _lstDevices.Location = new Point(12, 34);
        _lstDevices.Name = "_lstDevices";
        _lstDevices.Size = new Size(360, 160);
        _lstDevices.TabIndex = 0;
        _lstDevices.DoubleClick += LstDevices_DoubleClick;

        foreach (string name in deviceNames)
        {
            _lstDevices.Items.Add(name);
        }

        if (_lstDevices.Items.Count > 0)
            _lstDevices.SelectedIndex = 0;

        // _btnOk
        _btnOk.DialogResult = DialogResult.OK;
        _btnOk.Location = new Point(216, 204);
        _btnOk.Name = "_btnOk";
        _btnOk.Size = new Size(75, 28);
        _btnOk.TabIndex = 1;
        _btnOk.Text = "OK";
        _btnOk.Click += BtnOk_Click;

        // _btnCancel
        _btnCancel.DialogResult = DialogResult.Cancel;
        _btnCancel.Location = new Point(297, 204);
        _btnCancel.Name = "_btnCancel";
        _btnCancel.Size = new Size(75, 28);
        _btnCancel.TabIndex = 2;
        _btnCancel.Text = "Cancel";

        // MidiDeviceSelectorDialog
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;
        ClientSize = new Size(384, 244);
        Controls.Add(_lblPrompt);
        Controls.Add(_lstDevices);
        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "MidiDeviceSelectorDialog";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Select MIDI Output Device";

        ResumeLayout(false);
        PerformLayout();
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        if (_lstDevices.SelectedItem is string selected)
        {
            SelectedDeviceName = selected;
        }
    }

    private void LstDevices_DoubleClick(object? sender, EventArgs e)
    {
        if (_lstDevices.SelectedItem is string selected)
        {
            SelectedDeviceName = selected;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
