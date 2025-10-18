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

        private void txtExportTestChords_Click(object sender, EventArgs e)
        {

        }

        /*


        private void txtTestChords_Click(object sender, EventArgs e)
        {

        }

        private void btnTestParser_Click(object sender, EventArgs e)
        {
        }
        {

        }


        */



    }
}
