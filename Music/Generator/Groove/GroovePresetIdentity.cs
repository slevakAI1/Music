namespace Music.Generator
{
    // AI: purpose=Identity + semantic tags for groove preset; defines name, meter, style, compatibility hints.
    // AI: invariants=Name MUST be unique in GroovePresetLibrary; BeatsPerBar MUST match song timing; Tags drive layer/candidate selection.
    // AI: change=Tags and CompatibilityTags control which variation packs/algorithms apply; add tags to enable features.
    public sealed class GroovePresetIdentity
    {
        public string Name { get; set; } = "";
        public int BeatsPerBar { get; set; }
        public string StyleFamily { get; set; } = "";
        public List<string> Tags { get; set; } = new();
        public List<string> CompatibilityTags { get; set; } = new();
    }
}
