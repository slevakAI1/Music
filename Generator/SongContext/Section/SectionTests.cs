namespace Music.Generator
{
    public sealed class SectionTests
    {
        // Populate the provided SectionsClass (persisted on the design), render its summary, and reset dependent sets/displays
        public void SetDefaultSections(
            IWin32Window owner,
            SectionTrack sections,
            VoiceSet voiceSet)
        {
            SetTestSectionsD1(sections);

            // Display only
            var names = new List<string>();
            foreach (var s in sections.Sections)
            {
                names.Add(s.SectionType.ToString());
            }
            //txtSections.Text = string.Join("\r\n", names);
        }

        /// <summary>
        /// Build the standard top-level structure on the provided SectionsClass and return a printable summary.
        /// Structure: Intro → Verse → Chorus → Verse → Chorus → Bridge → Chorus → Outro
        /// Measures per section (total 48 bars): Intro=4, Verse/Chorus=8, Bridge=4, final Chorus=4, Outro=4
        /// </summary>
        public void SetTestSectionsD1(SectionTrack sections)
        {
            sections.Reset();

            // Durations sum to 48 bars to align with default timelines
            sections.Add(MusicConstants.eSectionType.Intro, 4, "Intro");
            sections.Add(MusicConstants.eSectionType.Verse, 8, "Verse 1");
            sections.Add(MusicConstants.eSectionType.Chorus, 8, "Chorus 1");
            sections.Add(MusicConstants.eSectionType.Verse, 8, "Verse 2");
            sections.Add(MusicConstants.eSectionType.Chorus, 8, "Chorus 2");
            sections.Add(MusicConstants.eSectionType.Bridge, 4, "Bridge 1");
            sections.Add(MusicConstants.eSectionType.Chorus, 4, "Chorus 3");
            sections.Add(MusicConstants.eSectionType.Outro, 4, "Outro");
        }
    }
}