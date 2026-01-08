namespace Music.Generator
{
    // Represents a concrete voicing/realization of a chord for use by generators (keys, pads, comp, etc.).
    public sealed record ChordRealization
    {
        // Concrete MIDI notes included in the voicing (order may matter to consumers)
        public IReadOnlyList<int> MidiNotes { get; init; } = Array.Empty<int>();

        // Inversion or bass description (e.g. "root", "3rd", "5th")
        public string Inversion { get; init; } = string.Empty;

        // Central MIDI note that describes the target register (e.g. middle of voicing)
        public int RegisterCenterMidi { get; init; }

        // True when a color tone (add9, add11, #11, etc.) was included in the voicing
        public bool HasColorTone { get; init; }

        // Optional tag describing the color tone (e.g. "add9")
        public string? ColorToneTag { get; init; }

        // Approximate number of simultaneous notes (density)
        public int Density { get; init; }
    }
}