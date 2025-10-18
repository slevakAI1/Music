using MusicXml;
using MusicXml.Domain;

namespace Music
{
    internal sealed class FileManager
    {
        private readonly IMusicXmlService _musicXmlService;
        private readonly Action<string>? _showStatus;
        private Score? _currentScore;

        public FileManager(IMusicXmlService musicXmlService, Action<string>? showStatus = null)
        {
            _musicXmlService = musicXmlService ?? throw new ArgumentNullException(nameof(musicXmlService));
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
                    _currentScore = _musicXmlService.ImportFromMusicXml(ofd.FileName);

                    var movement = _currentScore?.MovementTitle ?? "Unknown";
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
            if (_currentScore == null)
            {
                MessageBox.Show(owner, "No MusicXML score loaded.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "MusicXML files (*.musicxml;*.xml)|*.musicxml;*.xml",
                Title = "Export MusicXML File",
                FileName = "score.musicxml"
            };
            if (sfd.ShowDialog(owner) == DialogResult.OK)
            {
                try
                {
                    _musicXmlService.ExportLastImportedScore(sfd.FileName);
                    _showStatus?.Invoke($"Exported to {Path.GetFileName(sfd.FileName)}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        owner,
                        $"Error exporting MusicXML file:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                        "Export Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }
    }
}