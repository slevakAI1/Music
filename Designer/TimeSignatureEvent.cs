namespace Music.Designer
{
    // One time signature event, potentially spanning multiple bars
    public sealed class TimeSignatureEvent
    {
        // Placement (1-based bar/beat)
        public int StartBar { get; init; }
        public int StartBeat { get; init; } = 1;

        // Duration in beats (until next event if not specified by tooling)
        public int DurationBeats { get; init; } = 4;

        // Time signature numerator/denominator
        public int Numerator { get; init; } = 4;
        public int Denominator { get; init; } = 4;

        // Returns true if this event is active at the specified bar:beat
        public bool Contains(int bar, int beat, int beatsPerBar)
        {
            var startAbs = (StartBar - 1) * beatsPerBar + (StartBeat - 1);
            var targetAbs = (bar - 1) * beatsPerBar + (beat - 1);
            return targetAbs >= startAbs && targetAbs < startAbs + DurationBeats;
        }
    }
}
