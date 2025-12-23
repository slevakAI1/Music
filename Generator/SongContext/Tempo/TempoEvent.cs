namespace Music.Generator
{
    // One tempo event at a specific bar/beat position
    public sealed class TempoEvent
    {
        // Placement (1-based bar/beat)
        public int StartBar { get; init; }
        public int StartBeat { get; init; } = 1;

        // Tempo value in beats per minute
        public int TempoBpm { get; init; }
    }
}
