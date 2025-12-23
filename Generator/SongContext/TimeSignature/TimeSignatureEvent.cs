namespace Music.Generator
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
    }
}
