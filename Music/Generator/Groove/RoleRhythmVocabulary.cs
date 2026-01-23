namespace Music.Generator
{
    // AI: purpose=Constraint knobs limiting rhythm generation for a role; caps hits, enables syncopation/anticipation.
    // AI: invariants=MaxHitsPerBar/Beat are hard caps; AllowSyncopation/Anticipation gate offbeat/pickup generation.
    // AI: change=SnapStrongBeatsToChordTones is harmonic hint for pitched roles; does not affect drum roles.
    public sealed class RoleRhythmVocabulary
    {
        public int MaxHitsPerBar { get; set; }
        public int MaxHitsPerBeat { get; set; }
        public bool AllowSyncopation { get; set; }
        public bool AllowAnticipation { get; set; }
        public bool SnapStrongBeatsToChordTones { get; set; }
    }
}
