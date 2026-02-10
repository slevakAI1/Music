// AI: purpose=Shared operator IDs for fill-related drum operators; avoid policy coupling.
// AI: invariants=String values are stable identifiers; keep in sync with operator implementations.

namespace Music.Generator.Drums.Operators;

public static class DrumFillOperatorIds
{
    // AI: note=Contains canonical fill operator IDs and an immutable set for membership checks; keep values stable.
    public const string TurnaroundFillShort = "TurnaroundFillShort";
    public const string TurnaroundFillFull = "TurnaroundFillFull";
    public const string BuildFill = "BuildFill";
    public const string DropFill = "DropFill";
    public const string SetupHit = "SetupHit";
    public const string StopTime = "StopTime";
    public const string CrashOnOne = "CrashOnOne";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        TurnaroundFillShort,
        TurnaroundFillFull,
        BuildFill,
        DropFill,
        SetupHit,
        StopTime,
        CrashOnOne
    };
}
