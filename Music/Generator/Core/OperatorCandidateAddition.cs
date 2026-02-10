// AI: purpose=Instrument-agnostic candidate for adding onsets; timing, role, score, hints.
// AI: invariants=BarNumber>=1; Beat>=1.0; Score in [0,1]; VelocityHint in [0,127] when present.
// AI: deps=Consumed by instrument layers via mapping; Metadata carries instrument-specific data.

namespace Music.Generator.Core
{
    // AI: contract=Immutable record for cross-instrument candidate additions; Metadata extends for instrument needs
    public sealed record OperatorCandidateAddition
    {
        // AI: id=Deterministic format "{OperatorId}_{Role}_{BarNumber}_{Beat}[_{Discriminator}]"; used for dedupe
        public required string CandidateId { get; init; }

        // AI: info=Operator identifier; required and non-empty
        public required string OperatorId { get; init; }

        // AI: info=Role name (instrument-specific, e.g., Kick, Snare, BassRoot); used by instrument mapper
        public required string Role { get; init; }

        // AI: invariant=1-based bar number
        public required int BarNumber { get; init; }

        // AI: invariant=1-based beat position within bar; fractional values allowed
        public required decimal Beat { get; init; }

        // AI: hint=Optional suggested velocity [0..127]; null means compute from instrument layer
        public int? VelocityHint { get; init; }

        // AI: hint=Optional timing offset in ticks (positive=late, negative=early); null means use timing shaper
        public int? TimingHint { get; init; }

        // AI: invariant=Operator score in [0.0,1.0] before style weighting and penalties
        public required double Score { get; init; }

        // AI: extension=Instrument-specific data; mapping layer owns key conventions; keep deterministic usage
        public Dictionary<string, object>? Metadata { get; init; }

        // AI: util=Deterministic CandidateId generator; append discriminator if provided for uniqueness
        public static string GenerateCandidateId(
            string operatorId,
            string role,
            int barNumber,
            decimal beat,
            string? discriminator = null)
        {
            var baseId = $"{operatorId}_{role}_{barNumber}_{beat}";
            if (!string.IsNullOrEmpty(discriminator))
            {
                return $"{baseId}_{discriminator}";
            }
            return baseId;
        }

        // AI: validate=Checks invariants; returns false with errorMessage when invalid
        public bool TryValidate(out string? errorMessage)
        {
            if (BarNumber < 1)
            {
                errorMessage = $"BarNumber must be >= 1, was {BarNumber}";
                return false;
            }

            if (Beat < 1.0m)
            {
                errorMessage = $"Beat must be >= 1.0, was {Beat}";
                return false;
            }

            if (Score < 0.0 || Score > 1.0)
            {
                errorMessage = $"Score must be in [0.0, 1.0], was {Score}";
                return false;
            }

            if (VelocityHint.HasValue && (VelocityHint.Value < 0 || VelocityHint.Value > 127))
            {
                errorMessage = $"VelocityHint must be in [0, 127], was {VelocityHint.Value}";
                return false;
            }

            if (string.IsNullOrWhiteSpace(OperatorId))
            {
                errorMessage = "OperatorId cannot be null or whitespace";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Role))
            {
                errorMessage = "Role cannot be null or whitespace";
                return false;
            }

            if (string.IsNullOrWhiteSpace(CandidateId))
            {
                errorMessage = "CandidateId cannot be null or whitespace";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
