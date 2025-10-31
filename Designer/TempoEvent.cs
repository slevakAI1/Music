namespace Music.Designer
{
    // One tempo event, potentially spanning multiple bars
    public sealed class TempoEvent
    {
        // Placement (1-based bar/beat)
        public int StartBar { get; init; }
        public int StartBeat { get; init; } = 1;

        // Duration in beats (until next event if not specified by tooling)
        public int DurationBeats { get; init; } = 4;

        // Tempo value in beats per minute
        public int TempoBpm { get; init; } = 96;

        // Returns true if this event is active at the specified bar:beat
        public bool Contains(int bar, int beat, int beatsPerBar)
        {
            var startAbs = (StartBar - 1) * beatsPerBar + (StartBeat - 1);
            var targetAbs = (bar - 1) * beatsPerBar + (beat - 1);
            return targetAbs >= startAbs && targetAbs < startAbs + DurationBeats;
        }
    }
}
