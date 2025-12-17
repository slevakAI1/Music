namespace Music.Designer
{
    // One groove event at a specific bar/beat position
    public sealed class GrooveEvent
    {
        // Placement (1-based bar/beat)
        public int StartBar { get; init; }
        public int StartBeat { get; init; } = 1;

        // Groove identifier
        public string GroovePresetName { get; init; } = string.Empty;
    }
}