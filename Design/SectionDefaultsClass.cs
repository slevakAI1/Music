using System.Collections.Generic;
using System.Windows.Forms;

namespace Music.Design
{
    public sealed class SectionDefaultsClass
    {
        // Populate the provided SectionsClass (persisted on the design), render its summary, and reset dependent sets/displays
        public void SetDefaultSections(
            IWin32Window owner,
            SectionSetClass sections,
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
        /// Measures per section: Intro=4, Verse/Chorus/Bridge=8, Outro=4
        /// </summary>
        public void CreateTestSections(SectionSetClass sections)
        {
            sections.Reset();

            sections.Add(DesignEnums.eSectionType.Intro);
            sections.Add(DesignEnums.eSectionType.Verse);
            sections.Add(DesignEnums.eSectionType.Chorus);
            sections.Add(DesignEnums.eSectionType.Verse);
            sections.Add(DesignEnums.eSectionType.Chorus);
            sections.Add(DesignEnums.eSectionType.Bridge);
            sections.Add(DesignEnums.eSectionType.Chorus);
            sections.Add(DesignEnums.eSectionType.Outro);
        }
    }
}