using System.Collections.Generic;
using System.Windows.Forms;

namespace Music.Generate
{
    public sealed class SectionManagerClass
    {
        // Create and persist a new SongStructure, render its summary, and reset dependent sets/displays
        public SectionsClass CreateSections(
            IWin32Window owner,
            TextBox txtSongStructure,
            TextBox txtVoiceSet,
            TextBox txtChordSet,
            VoiceSet voiceSet,
            ChordSet chordSet)
        {
            var structure = new SectionsClass();
            var summary = CreateTestSections(structure);

            // Display only
            txtSongStructure.Text = summary;

            // Reset dependent sets and displays (persisted objects, textboxes only show data)
            voiceSet.Reset();
            chordSet.Reset();
            txtVoiceSet.Clear();
            txtChordSet.Clear();

            return structure;
        }

        /// <summary>
        /// Build the standard top-level structure on the provided SongStructure and return a printable summary.
        /// Structure: Intro → Verse → Chorus → Verse → Chorus → Bridge → Chorus → Outro
        /// Measures per section: Intro=4, Verse/Chorus/Bridge=8, Outro=4
        /// </summary>
        public string CreateTestSections(SectionsClass sections)
        {
            sections.Reset();

            int measure = 1;
            void Add(ScoreDesign.TopLevelSectionType t, int lengthMeasures)
            {
                var span = new ScoreDesign.MeasureRange(measure, measure + lengthMeasures - 1, true);
                sections.AddSection(t, span);
                measure += lengthMeasures;
            }

            Add(ScoreDesign.TopLevelSectionType.Intro, 4);
            Add(ScoreDesign.TopLevelSectionType.Verse, 8);
            Add(ScoreDesign.TopLevelSectionType.Chorus, 8);
            Add(ScoreDesign.TopLevelSectionType.Verse, 8);
            Add(ScoreDesign.TopLevelSectionType.Chorus, 8);
            Add(ScoreDesign.TopLevelSectionType.Bridge, 8);
            Add(ScoreDesign.TopLevelSectionType.Chorus, 8);
            Add(ScoreDesign.TopLevelSectionType.Outro, 4);

            var names = new List<string>(sections.Sections.Count);
            foreach (var s in sections.Sections)
            {
                int bars = s.Span.EndMeasure is int end
                    ? (s.Span.InclusiveEnd ? end - s.Span.StartMeasure + 1 : end - s.Span.StartMeasure)
                    : 0;
                names.Add($"{s.Type}, {bars}");
            }
            return string.Join("\r\n", names);
        }
    }
}