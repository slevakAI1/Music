namespace Music.Generator
{
    // Definition of a groove pattern that can produce onsets for each role
    public sealed class GroovePreset
    {
        public string Name { get; init; } = string.Empty;

        // Anchor layer: stable patterns that create entrainability
        public GrooveInstanceLayer AnchorLayer { get; init; } = new();

        // Tension layer: syncopation, ghost notes, anticipations, fills
        public GrooveInstanceLayer TensionLayer { get; init; } = new();
    }
}