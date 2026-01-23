namespace Music.Generator
{
    // AI: purpose=Complete groove preset definition; ties identity + anchor onsets + protection policy + variation catalog.
    // AI: invariants=Identity.Name MUST be unique; AnchorLayer holds base onsets; ProtectionPolicy enforces constraints.
    // AI: compat=Events/GetActiveGroovePreset are migration stubs; single preset per song, no multi-bar events yet.
    // AI: change=Multi-preset support requires bar-range mapping in GroovePresetLibrary.GetPresetForBar.
    public sealed class GroovePresetDefinition
    {
        public GroovePresetIdentity Identity { get; set; } = new();
        public GrooveInstanceLayer AnchorLayer { get; set; } = new();
        public GrooveProtectionPolicy ProtectionPolicy { get; set; } = new();
        public GrooveVariationCatalog VariationCatalog { get; set; } = new();
        public List<object> Events { get; } = new();

        public GroovePresetDefinition GetActiveGroovePreset(int bar) => this;
    }
}
