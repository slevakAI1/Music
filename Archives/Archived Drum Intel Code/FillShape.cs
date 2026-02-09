// AI: purpose=Immutable record describing a fill's characteristics for agent memory and anti-repetition.
// AI: invariants=BarPosition is 1-based when present; DensityLevel in [0.0,1.0]; RolesInvolved non-null.
// AI: deps=Consumed by IAgentMemory.GetLastFillShape(); changing fields affects memory/compatibility.
namespace Music.Generator.Core
{
    // AI: contract=Stores fill metadata: bar, roles, density, duration, optional tag; keep ordering for compat
    public sealed record FillShape(
        int BarPosition,
        IReadOnlyList<string> RolesInvolved,
        double DensityLevel,
        decimal DurationBars,
        string? FillTag = null)
    {
        // AI: sentinel=Empty has BarPosition=0 and empty RolesInvolved; represents "no fill"
        public static FillShape Empty => new(
            BarPosition: 0,
            RolesInvolved: Array.Empty<string>(),
            DensityLevel: 0.0,
            DurationBars: 0);

        // AI: predicate=HasContent true when BarPosition>0 and RolesInvolved non-empty
        public bool HasContent => BarPosition > 0 && RolesInvolved.Count > 0;
    }
}
