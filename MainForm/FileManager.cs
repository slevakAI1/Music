using Music.Writer;
using MusicXml;

namespace Music
{
    internal sealed class FileManager
    {
        private readonly Action<string>? _showStatus;
        private string? _lastImportedPath;

        public FileManager(Action<string>? showStatus = null)
        {
            _showStatus = showStatus;
        }

        public void ImportMusicXml(IWin32Window owner)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "MusicXML files (*.musicxml;*.xml)|*.musicxml;*.xml",
                Title = "Import MusicXML File"
            };
            if (ofd.ShowDialog(owner) == DialogResult.OK)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(ofd.FileName) || !File.Exists(ofd.FileName))
                        throw new FileNotFoundException("MusicXML file not found.", ofd.FileName);

                    Globals.Score = MusicXmlParser.GetScore(ofd.FileName);
                    _lastImportedPath = ofd.FileName;

                    var movement = Globals.Score?.MovementTitle ?? "Unknown";
                    _showStatus?.Invoke($"Loaded MusicXML: {Path.GetFileName(ofd.FileName)} (Movement: {movement})");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        owner,
                        $"Error importing MusicXML file:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                        "Import Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        public void ExportMusicXml(IWin32Window owner)
        {
            if (Globals.Score == null)
            {
                MessageBox.Show(owner, "No MusicXML score loaded.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "MusicXML files (*.musicxml;*.xml)|*.musicxml;*.xml",
                Title = "Export MusicXML File",
                FileName = !string.IsNullOrWhiteSpace(_lastImportedPath) ? Path.GetFileName(_lastImportedPath) : "score.musicxml"
            };
            if (sfd.ShowDialog(owner) == DialogResult.OK)
            {
                try
                {
                    var xml = MusicXmlScoreSerializer.Serialize(Globals.Score);
                    File.WriteAllText(sfd.FileName, xml, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                    _showStatus?.Invoke($"Exported to {Path.GetFileName(sfd.FileName)}");
                }
                catch (Exception ex)
                {
                    MessageBoxHelper.Show(
                        $"Error exporting MusicXML file:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                        "Export Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }
    }
}