// AI: purpose=Shared operator IDs for fill-related Bass operators; avoid policy coupling.
// AI: invariants=String values are stable identifiers; keep in sync with operator implementations.

namespace Music.Generator.Bass.Operators;

public static class BassFillOperatorIds
{

    // AI: note=Contains canonical bass fill operator IDs and an immutable set for membership checks; keep values stable.
    public const string BassTurnaroundShort = "BassTurnaroundShort";
    public const string BassTurnaroundFull = "BassTurnaroundFull";
    public const string BassPickup = "BassPickup";
    public const string BassWalkUp = "BassWalkUp";
    public const string BassWalkDown = "BassWalkDown";
    public const string BassOctavePush = "BassOctavePush";
    public const string BassRhythmPush = "BassRhythmPush";
    public const string BassDropToPedal = "BassDropToPedal";
    public const string BassStopTimeHit = "BassStopTimeHit";
    public const string BassDownbeatReinforce = "BassDownbeatReinforce";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        BassTurnaroundShort,
        BassTurnaroundFull,
        BassPickup,
        BassWalkUp,
        BassWalkDown,
        BassOctavePush,
        BassRhythmPush,
        BassDropToPedal,
        BassStopTimeHit,
        BassDownbeatReinforce
    };
}
