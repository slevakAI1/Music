using System.Collections.Generic;
using System.Windows.Forms;

namespace Music.Generate
{
    public sealed class SectionManagerClass
    {
        // Populate the provided SectionsClass (persisted on the design), render its summary, and reset dependent sets/displays
        public void CreateSections(
            IWin32Window owner,
            SectionsClass sections,
            TextBox txtSongStructure,
            TextBox txtVoiceSet,
            TextBox txtChordSet,
            VoiceSetClass voiceSet,
            ChordSetClass chordSet)
        {
            var summary = CreateTestSections(sections);

            // Display only
            txtSongStructure.Text = summary;

            // Reset dependent sets and displays (persisted objects; textboxes only show data)
            voiceSet.Reset();
            chordSet.Reset();
            txtVoiceSet.Clear();
            txtChordSet.Clear();
        }

        /// <summary>
        /// Build the standard top-level structure on the provided SectionsClass and return a printable summary.
        /// Structure: Intro → Verse → Chorus → Verse → Chorus → Bridge → Chorus → Outro
        /// Measures per section: Intro=4, Verse/Chorus/Bridge=8, Outro=4
        /// </summary>
        public string CreateTestSections(SectionsClass sections)
        {
            sections.Reset();

            int measure = 1;
            void Add(ScoreDesignClass.SectionType t, int lengthMeasures)
            {
                var span = new ScoreDesignClass.MeasureRange(measure, measure + lengthMeasures - 1, true);
                sections.AddSection(t, span);
                measure += lengthMeasures;
            }

            Add(ScoreDesignClass.SectionType.Intro, 4);
            Add(ScoreDesignClass.SectionType.Verse, 8);
            Add(ScoreDesignClass.SectionType.Chorus, 8);
            Add(ScoreDesignClass.SectionType.Verse, 8);
            Add(ScoreDesignClass.SectionType.Chorus, 8);
            Add(ScoreDesignClass.SectionType.Bridge, 8);
            Add(ScoreDesignClass.SectionType.Chorus, 8);
            Add(ScoreDesignClass.SectionType.Outro, 4);

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