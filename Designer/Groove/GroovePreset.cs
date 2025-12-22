namespace Music.Generator
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
        // Drum roles
        public List<decimal> KickOnsets { get; init; } = new();
        public List<decimal> SnareOnsets { get; init; } = new();
        public List<decimal> HatOnsets { get; init; } = new();
        
        // Bass role
        public List<decimal> BassOnsets { get; init; } = new();
        
        // Guitar/comping role (rhythm guitar, typically on offbeats/8ths)
        public List<decimal> CompOnsets { get; init; } = new();
        
        // Keys/pads role (sustained chords, typically on longer values)
        public List<decimal> PadsOnsets { get; init; } = new();
    }
}