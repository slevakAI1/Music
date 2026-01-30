namespace Music.Generator.Groove
{
    // AI: purpose=Simplified groove preset definition; ties identity + anchor onsets.
    // AI: invariants=Identity.Name MUST be unique; AnchorLayer holds base onsets.
    public sealed class GroovePresetDefinition
    {
        public GroovePresetIdentity Identity { get; set; } = new();
        public GrooveInstanceLayer AnchorLayer { get; set; } = new();
        public List<object> Events { get; } = new();

        public GroovePresetDefinition GetActiveGroovePreset(int bar) => this;
    }
}
