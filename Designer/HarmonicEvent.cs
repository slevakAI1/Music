namespace Music.Designer
{
    // One harmonic event, potentially spanning multiple bars
    public sealed class HarmonicEvent
    {
        // Placement (1-based bar/beat)
        public int StartBar { get; init; }
        public int StartBeat { get; init; } = 1;

        // Duration in beats (e.g., 4 for one 4/4 bar)
        public int DurationBeats { get; init; } = 4;

        // Musical properties (kept simple/strings for now)
        public string Key { get; init; } = "C major";
        public int Degree { get; init; } // 1..7
        public string Quality { get; init; } = "maj"; // maj, min7, dom7, etc.
        public string Bass { get; init; } = "root";

        // Returns true if this event is active at the specified bar:beat
        public bool Contains(int bar, int beat, int beatsPerBar)
        {
            var startAbs = (StartBar - 1) * beatsPerBar + (StartBeat - 1);
            var targetAbs = (bar - 1) * beatsPerBar + (beat - 1);
            return targetAbs >= startAbs && targetAbs < startAbs + DurationBeats;
        }
    }
}