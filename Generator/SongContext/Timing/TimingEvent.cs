namespace Music.Generator
{
    // One time signature event, potentially spanning multiple bars
    public sealed class TimingEvent
    {
        // Placement (1-based bar/beat)
        public int StartBar { get; init; }

        // Time signature numerator/denominator
        public int Numerator { get; init; }
        public int Denominator { get; init; }
    }
}
