// AI: purpose=Classify musical operators by functional family across instruments for weighting/filtering
// AI: invariants=Enum ordinals stable: add new values only at end to preserve persisted semantics
// AI: deps=Used by IMusicalOperator<T>.OperatorFamily and selection engine; keep names stable
namespace Music.Generator.Core
{
    // AI: contract=Functional categories used to group operators for selection and policy
    public enum OperatorFamily
    {
        // AI: MicroAddition=Small decorative additions; low density impact
        MicroAddition = 0,

        // AI: SubdivisionTransform=Change rhythmic subdivision (doubletime/triplet overlays)
        SubdivisionTransform = 1,

        // AI: PhrasePunctuation=Fill/pickup/turnaround markers at phrase boundaries
        PhrasePunctuation = 2,

        // AI: PatternSubstitution=Full pattern swap; high impact, low frequency
        PatternSubstitution = 3,

        // AI: StyleIdiom=Genre-specific idioms and signature figures
        StyleIdiom = 4,

        // AI: NoteRemoval=Subtractive operators that remove existing onsets for variance and dynamics
        NoteRemoval = 5
    }
}
