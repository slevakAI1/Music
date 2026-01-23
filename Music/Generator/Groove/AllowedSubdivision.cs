namespace Music.Generator.Groove
{
    // AI: purpose=Flags enum for legal rhythmic subdivision grids (quarter/eighth/sixteenth/triplets).
    // AI: invariants=Bitwise combinable; None=0 means no subdivisions allowed; used to constrain onset generation.
    [Flags]
    public enum AllowedSubdivision
    {
        None = 0,
        Quarter = 1 << 0,
        Eighth = 1 << 1,
        Sixteenth = 1 << 2,
        EighthTriplet = 1 << 3,
        SixteenthTriplet = 1 << 4
    }
}
