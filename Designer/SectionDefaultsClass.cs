﻿using System.Collections.Generic;
using System.Windows.Forms;

namespace Music.Design
{
    public sealed class SectionDefaultsClass
    {
        // Populate the provided SectionsClass (persisted on the design), render its summary, and reset dependent sets/displays
        public void SetDefaultSections(
            IWin32Window owner,
            SectionTimelineClass sections,
            VoiceSetClass voiceSet)
        {
            CreateTestSections(sections);

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
        public void CreateTestSections(SectionTimelineClass sections)
        {
            sections.Reset();

            // Durations sum to 48 bars to align with default timelines
            sections.Add(MusicConstants.eSectionType.Intro, 4);
            sections.Add(MusicConstants.eSectionType.Verse, 8);
            sections.Add(MusicConstants.eSectionType.Chorus, 8);
            sections.Add(MusicConstants.eSectionType.Verse, 8);
            sections.Add(MusicConstants.eSectionType.Chorus, 8);
            sections.Add(MusicConstants.eSectionType.Bridge, 4);
            sections.Add(MusicConstants.eSectionType.Chorus, 4);
            sections.Add(MusicConstants.eSectionType.Outro, 4);
        }
    }
}