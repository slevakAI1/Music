namespace Music.Generator.Groove
{
    // AI: purpose=Classifies onsets into strength buckets for accent/velocity logic; drives VelocityRule selection.
    // AI: invariants=Bucket determines velocity range via GrooveAccentPolicy; style-dependent (e.g., Backbeat=beats 2/4 in 4/4).
    public enum OnsetStrength
    {
        Downbeat,
        Backbeat,
        Strong,
        Offbeat,
        Pickup,
        Ghost
    }
}
