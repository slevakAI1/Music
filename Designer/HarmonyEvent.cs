namespace Music.Designer
{
    // One harmonic event, potentially spanning multiple bars
    public sealed class HarmonyEvent
    {
        // Placement (1-based bar/beat)
        public int StartBar { get; init; }
        public int StartBeat { get; init; } = 1;

        // Musical properties (kept simple/strings for now)
        public string Key { get; init; } = "C major";
        public int Degree { get; init; } // 1..7
        public string Quality { get; init; } = "maj"; // maj, min7, dom7, etc.
        public string Bass { get; init; } = "root";


        // For data entry form support only
        public int DurationBeats { get; init; } = 4;
    }
}