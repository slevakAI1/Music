using System.Collections.Generic;
using System.Windows.Forms;

namespace Music.Generate
{
    public sealed class ChordManager
    {
        // Persist in ChordSet; text box is display only
        public void AddDefaultChordsAndRender(IWin32Window owner, SectionsClass? structure, ChordSet chordSet, TextBox txtChordSet)
        {
            if (structure == null)
            {
                MessageBox.Show(owner, "Create the song structure first.", "No Structure", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var chords = chordSet.AddDefaultChords();

            var lines = new List<string>(chords.Count);
            foreach (var c in chords)
                lines.Add(c.Name);

            txtChordSet.Lines = lines.ToArray();
        }
    }
}