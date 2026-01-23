namespace Music.Generator
{
    // AI: purpose=Rules for fill/pull windows at phrase/section ends; controls where fills are allowed and how protected.
    // AI: invariants=AllowFills* gates fill candidates; *BarsWindow defines bar count from end; Protect* preserves anchors.
    // AI: change=EnabledFillTags filters candidate groups; typical tags: "Fill", "Pickup"; add tag to enable candidate group.
    public sealed class GroovePhraseHookPolicy
    {
        public bool AllowFillsAtPhraseEnd { get; set; }
        public int PhraseEndBarsWindow { get; set; }
        public bool AllowFillsAtSectionEnd { get; set; }
        public int SectionEndBarsWindow { get; set; }
        public bool ProtectDownbeatOnPhraseEnd { get; set; }
        public bool ProtectBackbeatOnPhraseEnd { get; set; }
        public List<string> EnabledFillTags { get; set; } = new();
    }
}
