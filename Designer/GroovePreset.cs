namespace Music.Designer
{
    // Definition of a groove pattern that can produce onsets for each role
    public sealed class GroovePreset
    {
        public string Name { get; init; } = string.Empty;
        
        // Anchor layer: stable patterns that create entrainability
        public GrooveLayer AnchorLayer { get; init; } = new();
        
        // Tension layer: syncopation, ghost notes, anticipations, fills
        public GrooveLayer TensionLayer { get; init; } = new();
    }

    // A layer within a groove (anchor or tension)
    public sealed class GrooveLayer
    {
        // Onsets for kick drum (beat positions within a bar, 1-based)
        public List<decimal> KickOnsets { get; init; } = new();
        
        // Onsets for snare drum (beat positions within a bar, 1-based)
        public List<decimal> SnareOnsets { get; init; } = new();
        
        // Onsets for hi-hat/subdivision carrier (beat positions within a bar, 1-based)
        public List<decimal> HatOnsets { get; init; } = new();
    }
}