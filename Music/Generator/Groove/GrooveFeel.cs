namespace Music.Generator
{
    // AI: purpose=Rhythmic feel templates for subdivision interpretation (swing/shuffle); affects timing of subdivisions.
    // AI: invariants=Used by GrooveSubdivisionPolicy; Straight=no swing, others bias timing per SwingAmount01.
    public enum GrooveFeel
    {
        Straight,
        Swing,
        Shuffle,
        TripletFeel
    }
}
