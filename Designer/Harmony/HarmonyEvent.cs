namespace Music.Designer
{
    // One harmonic event, potentially spanning multiple bars
    // A harmonic event is assumed to be active until the next harmonic event starts.
    // The event time is determined by StartBar and Start Beat. DurationBeats is a user input
    //      and/or calculated value for use by the Harmonic Event data entry form.

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