using Music.Generate;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Music
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();
        }

        private void btnTestParser_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "MusicXML Files (*.musicxml;*.xml)|*.musicxml;*.xml",
                Title = "Select a MusicXML file"
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            var result = new Music.Tests.MusicXmlParserTests().TestParser(ofd.FileName);
            var passed = string.Equals(result, "Passed", StringComparison.OrdinalIgnoreCase);

            MessageBox.Show(this, passed ? "Passed" : result, "MusicXML Parser Test",
                MessageBoxButtons.OK, passed ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }


        // Import raw MusicXML file, serialize it back out, re-import, and compare the two object graphs
        private void btnTestSerializer_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "MusicXML Files (*.musicxml;*.xml)|*.musicxml;*.xml",
                Title = "Select a MusicXML file"
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            var result = new Music.Tests.MusicXmlSerializerTests().TestSerializer(ofd.FileName);
            var passed = string.Equals(result, "Passed", StringComparison.OrdinalIgnoreCase);

            MessageBox.Show(this, passed ? "Passed" : result, "MusicXML Serializer Test",
                MessageBoxButtons.OK, passed ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void btnCreatTestMusicXmlFile_Click(object sender, EventArgs e)
        {
            try
            {
                var score = Music.Tests.MusicXmlCreateValidFileTests.CreateSingleMeasureCChordKeyboardScore();
                var xml = Music.Generate.MusicXmlScoreSerializer.Serialize(score);

                var targetDir = @"C:\temp";
                Directory.CreateDirectory(targetDir);

                var fileName = $"TestScore_{DateTime.Now:yyyyMMdd_HHmmss}.musicxml";
                var fullPath = Path.Combine(targetDir, fileName);

                File.WriteAllText(fullPath, xml, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                MessageBox.Show(this,
                    $"Saved MusicXML to:\n{fullPath}",
                    "MusicXML Saved",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Failed to create/save test MusicXML file.\n\n{ex.GetType().FullName}: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
