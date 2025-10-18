namespace Music.Generate
{
    public sealed class SectionManagerClass
    {
        public ScoreDesign CreateAndRenderStructure(
            IWin32Window owner,
            TextBox txtSongStructure,
            TextBox txtVoiceSet,
            TextBox txtChordSet)
        {
            var design = new ScoreDesign();
            var summary = CreateTestStructure(design);

            txtSongStructure.Text = summary;

            // Clear prior outputs tied to an older structure
            txtVoiceSet.Clear();
            txtChordSet.Clear();

            return design;
        }

        /// <summary>
        /// Build the standard top-level structure on the provided design and return a printable summary.
        /// Structure: Intro → Verse → Chorus → Verse → Chorus → Bridge → Chorus → Outro
        /// Measures per section: Intro=4, Verse=8, Chorus=8, Verse=8, Chorus=8, Bridge=8, Chorus=8, Outro=4
        /// </summary>
        public string CreateTestStructure(ScoreDesign design)
        {
            design.ResetSections();

            int measure = 1;
            void Add(ScoreDesign.TopLevelSectionType t, int lengthMeasures)
            {
                var span = new ScoreDesign.MeasureRange(measure, measure + lengthMeasures - 1, true);
                design.AddSection(t, span);
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

            // Build a simple "Intro, bars" summary string per section
            var names = new List<string>(design.Sections.Count);
            foreach (var s in design.Sections)
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