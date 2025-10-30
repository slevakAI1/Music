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
        // Map is here for backward compatibility with some code that references it
        // TODO: Resolve this in future refactoring
        public static readonly Dictionary<string, int> NoteValueMap = new()
        {
            ["Whole (1)"] = 1,
            ["Half (1/2)"] = 2,
            ["Quarter (1/4)"] = 4,
            ["Eighth (1/8)"] = 8,
            ["16th (1/16)"] = 16
        };
    }
}