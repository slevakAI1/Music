using System.Collections.Generic;
using System.Windows.Forms;

namespace Music.Generate
{
    public sealed class ChordManager
    {
        public void AddDefaultChordsAndRender(IWin32Window owner, SongStructure? structure, TextBox txtChordSet)
        {
            if (structure == null)
            {
                MessageBox.Show(owner, "Create the song structure first.", "No Structure", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var chords = structure.CreateChordSet();

            var lines = new List<string>(chords.Count);
            foreach (var c in chords)
                lines.Add(c.Name);

            // One chord per line in the chord set textbox
            txtChordSet.Lines = lines.ToArray();
        }
    }
}