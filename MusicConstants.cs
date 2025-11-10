using System.Collections.Generic;

namespace Music
{
    // Central holder for public enums used across the design domain
    public static class MusicConstants
    {
        public enum Step { A, B, C, D, E, F, G }
        public enum eSectionType { Intro, Verse, Chorus, Solo, Bridge, Outro, Custom }

        // Map of note value display strings to their corresponding denominator values
        // These values are loaded into the Note Value dropdown
        // Note: Score must have divisions≥8 to support 32nd notes, divisions≥16 for 64th notes
        public static readonly Dictionary<string, int> NoteValueMap = new()
        {
            ["Whole (1)"] = 1,
            ["Half (1/2)"] = 2,
            ["Quarter (1/4)"] = 4,
            ["Eighth (1/8)"] = 8,
            ["16th (1/16)"] = 16,
            ["32nd (1/32)"] = 32,
            ["64th (1/64)"] = 64   // Requires divisions≥16
        };

        // Relative path from AppContext.BaseDirectory to the Voices.Notion.json file
        public const string VoicesNotionJsonRelativePath = "Designer\\Voices.Notion.json";
    }
}