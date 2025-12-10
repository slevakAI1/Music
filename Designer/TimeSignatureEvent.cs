namespace Music.Designer
{
    // One time signature event, potentially spanning multiple bars
    public sealed class TimeSignatureEvent
    {
        // Placement (1-based bar/beat)
        public int StartBar { get; init; }
        public int StartBeat { get; init; } = 1;

        // Time signature numerator/denominator
        public int Numerator { get; init; } = 4;
        public int Denominator { get; init; } = 4;

        // Number of bars this time signature spans
        public int BarCount { get; init; } = 4;

        // Returns true if this event is active at the specified bar
        public bool Contains(int bar, int beatsPerBar)
        {
            return bar >= StartBar && bar < StartBar + BarCount;
        }
    }
}
