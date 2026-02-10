namespace Music.Generator.Drums.Planning
{
    // AI: purpose=Classifies a candidate's role within a fill pattern; used by fill operators and memory
    // AI: invariants=Enum order must remain stable; None is default; one role per candidate
    // AI: deps=Used by OperatorCandidateAddition, fill operators, and DrummerMemory for fill-shape tracking
    public enum FillRole
    {
        None = 0,
        Setup = 1,
        FillStart = 2,
        FillBody = 3,
        FillEnd = 4
    }
}
