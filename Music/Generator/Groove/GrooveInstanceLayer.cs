namespace Music.Generator.Groove
{
    // AI: purpose=Instance layer holding onset lists per role; used for anchor onsets in GroovePresetDefinition.
    // AI: invariants=Onset values in domain units (beats or fractional bar offsets); callers normalize/sort when needed.
    // AI: change=Add role onset list when introducing new role; decoupled from old groove code (migration needed).
    public sealed class GrooveInstanceLayer
    {
        public List<decimal> KickOnsets { get; set; } = new();
        public List<decimal> SnareOnsets { get; set; } = new();
        public List<decimal> HatOnsets { get; set; } = new();
        public List<decimal> BassOnsets { get; set; } = new();
        public List<decimal> CompOnsets { get; set; } = new();
        public List<decimal> PadsOnsets { get; set; } = new();
    }
}
