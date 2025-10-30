//using Music.Design;

//namespace Music.Generate
//{
//    public sealed class ChordManagerClass
//    {
//        public void AddDefaultChords(IWin32Window owner, SectionTimelineClass? structure, ChordSetClass chordSet, TextBox txtChordSet)
//        {
//            if (structure == null)
//            {
//                MessageBox.Show(owner, "Create the song structure first.", "No Structure", MessageBoxButtons.OK, MessageBoxIcon.Information);
//                return;
//            }

//            var chords = chordSet.AddDefaultChords();

//            var lines = new List<string>(chords.Count);
//            foreach (var c in chords)
//                lines.Add(c.ChordName);

//            txtChordSet.Lines = lines.ToArray();
//        }
//    }
//}