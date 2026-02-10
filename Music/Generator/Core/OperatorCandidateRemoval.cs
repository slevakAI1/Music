// AI: purpose=Instrument-agnostic removal target; matched against existing onset (BarNumber,Beat,Role) tuples.
// AI: invariants=BarNumber>=1; Beat>=1.0; Role must match existing onset key.
// AI: deps=Consumed by instrument applicators to filter onsets; respects instrument-specific protection flags.

namespace Music.Generator.Core
{
    // AI: contract=Immutable removal target; instrument layer matches against its onset model
    public sealed record OperatorCandidateRemoval
    {
        // AI: invariant=1-based bar number of the onset to remove
        public required int BarNumber { get; init; }

        // AI: invariant=1-based beat position of the onset to remove; fractional values allowed
        public required decimal Beat { get; init; }

        // AI: info=Role of the onset to remove (instrument-specific, e.g., Kick, ClosedHat, BassRoot)
        public required string Role { get; init; }

        // AI: info=Operator that proposed this removal; for diagnostics/provenance
        public required string OperatorId { get; init; }

        // AI: info=Human-readable reason for removal; aids diagnostics
        public string? Reason { get; init; }
    }
}
