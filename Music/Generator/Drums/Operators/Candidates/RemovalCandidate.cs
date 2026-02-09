// AI: purpose=Identifies an existing onset to remove; used by IDrumRemovalOperator implementations.
// AI: invariants=BarNumber>=1; Beat>=1.0; Role must match existing onset key (BarNumber,Beat,Role).
// AI: deps=Consumed by DrumOperatorApplicator to filter onsets; respects GrooveOnset protection flags.

namespace Music.Generator.Drums.Operators.Candidates
{
    // AI: contract=Immutable removal target; matched against GrooveOnset (BarNumber,Beat,Role) tuples.
    public sealed record RemovalCandidate
    {
        // AI: invariant=1-based bar number of the onset to remove
        public required int BarNumber { get; init; }

        // AI: invariant=1-based beat position of the onset to remove; fractional values allowed
        public required decimal Beat { get; init; }

        // AI: info=Role of the onset to remove (e.g., Kick, ClosedHat)
        public required string Role { get; init; }

        // AI: info=Operator that proposed this removal; for diagnostics/provenance
        public required string OperatorId { get; init; }

        // AI: info=Human-readable reason for removal; aids diagnostics
        public string? Reason { get; init; }
    }
}
