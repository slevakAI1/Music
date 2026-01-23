namespace Music.Generator.Groove
{
    // AI: purpose=Rhythmic grid constraints and feel for groove; defines legal subdivision grids + swing/shuffle intensity.
    // AI: invariants=SwingAmount01 in [0..1]; meaningful only when Feel != Straight; AllowedSubdivisions gates onset generation.
    public sealed class GrooveSubdivisionPolicy
    {
        public AllowedSubdivision AllowedSubdivisions { get; set; }
        public GrooveFeel Feel { get; set; }
        public double SwingAmount01 { get; set; }
    }
}
