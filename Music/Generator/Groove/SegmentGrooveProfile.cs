namespace Music.Generator.Groove
{
    // AI: purpose=Per-segment groove configuration; lightweight tag enabling/disabling + density targets for sections/phrases.
    // AI: invariants=GroovePresetName links to GroovePresetLibrary for mid-song switching; null=use default preset.
    // AI: change=EnabledVariationTags/EnabledProtectionTags control layer activation; add tags to enable features per segment.
    public sealed class SegmentGrooveProfile
    {
        public string SegmentId { get; set; } = "";
        public int? SectionIndex { get; set; }
        public int? PhraseIndex { get; set; }
        public int? StartBar { get; set; }
        public int? EndBar { get; set; }
        public string? GroovePresetName { get; set; }
        public List<string> EnabledVariationTags { get; set; } = new();
        public List<string> EnabledProtectionTags { get; set; } = new();
        public List<RoleDensityTarget> DensityTargets { get; set; } = new();
        public GrooveFeel? OverrideFeel { get; set; }
        public double? OverrideSwingAmount01 { get; set; }
    }
}
