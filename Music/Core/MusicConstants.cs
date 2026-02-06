// AI: purpose=Central readonly constants and enums used by generator and UI; keep stable to avoid breaking contracts
// AI: invariants=TicksPerQuarterNote (480) is authoritative for all tick math; changing it breaks conversions/tests
// AI: deps=NoteValueMap keys are UI display strings; VoicesNotionJsonRelativePath is relative to AppContext.BaseDirectory
// AI: perf=Use constants for tick math; do not scatter magic tick values across codebase
// AI: security=No secrets here; treat paths carefully when using external input
namespace Music
{
    // AI: ENums are authoritative lists for UI and algorithm mapping; preserve order and names to avoid index-based bugs
    public static class MusicConstants
    {
        public enum Step { A, B, C, D, E, F, G }
        public enum eSectionType { Intro, Verse, Chorus, Solo, Bridge, Outro, Custom }

        // AI: fixed MIDI timing resolution used for all conversions and MIDI I/O; typical=480; do not inline literals elsewhere
        public const short TicksPerQuarterNote = 480;

        // AI: NoteValueMap keys are UI display strings ("Name (1/n)") and values are denominators used in tick math.
        // AI: Score divisions requirements: divisions>=8 for 32nd notes, divisions>=16 for 64th notes; update UI/tooltips if changed
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

        // AI: Relative path to voices JSON used to populate instrument lists; keep path in sync with deployment layout
        public const string VoicesNotionJsonRelativePath = "Designer\\Voices\\Voices.Notion.json";
    }
}
