// AI: purpose=DrumCandidate: operator-generated drum event with position, strength, hints, and score.
// AI: invariants=BarNumber>=1; Beat>=1.0; Score in [0,1]; VelocityHint in [0,127] when present.
// AI: deps=Uses OnsetStrength, FillRole, DrumArticulation; consumed by selection and mapping layers.

using Music.Generator.Drums.Performance;
using Music.Generator.Drums.Planning;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Selection.Candidates
{
    // AI: contract=Immutable record used by selection engine; keep property names stable when persisting/mapping
    public sealed record DrumCandidate
    {
        // AI: id=Deterministic format "{OperatorId}_{Role}_{BarNumber}_{Beat}[_{Articulation}]"; used for dedupe
        public required string CandidateId { get; init; }

        // AI: info=Operator identifier; required and non-empty
        public required string OperatorId { get; init; }

        // AI: info=Role name (e.g., Kick, Snare); used for mapping to MIDI program and articulations
        public required string Role { get; init; }

        // AI: invariant=1-based bar number
        public required int BarNumber { get; init; }

        // AI: invariant=1-based beat position within bar; fractional values allowed
        public required decimal Beat { get; init; }

        // AI: info=Onset strength classification guiding velocity shaping
        public required OnsetStrength Strength { get; init; }

        // AI: hint=Optional suggested velocity [0..127]; null means compute from strength/style
        public int? VelocityHint { get; init; }

        // AI: hint=Optional timing offset in ticks (positive=late, negative=early); null means use timing shaper
        public int? TimingHint { get; init; }

        // AI: hint=Optional articulation; null means default for role
        public DrumArticulation? ArticulationHint { get; init; }

        // AI: info=Fill role classification for fill operators and memory tracking
        public required FillRole FillRole { get; init; }

        // AI: invariant=Operator score in [0.0,1.0] before style weighting and penalties
        public required double Score { get; init; }

        // AI: helper=Creates a minimal candidate for tests using the default id format
        public static DrumCandidate CreateMinimal(
            string operatorId = "TestOperator",
            string role = GrooveRoles.Snare,
            int barNumber = 1,
            decimal beat = 2.0m,
            OnsetStrength strength = OnsetStrength.Backbeat,
            double score = 0.5)
        {
            string candidateId = $"{operatorId}_{role}_{barNumber}_{beat}";
            return new DrumCandidate
            {
                CandidateId = candidateId,
                OperatorId = operatorId,
                Role = role,
                BarNumber = barNumber,
                Beat = beat,
                Strength = strength,
                VelocityHint = null,
                TimingHint = null,
                ArticulationHint = null,
                FillRole = FillRole.None,
                Score = score
            };
        }

        // AI: util=Deterministic CandidateId generator; append articulation only if specified and not None
        public static string GenerateCandidateId(
            string operatorId,
            string role,
            int barNumber,
            decimal beat,
            DrumArticulation? articulation = null)
        {
            var baseId = $"{operatorId}_{role}_{barNumber}_{beat}";
            if (articulation.HasValue && articulation.Value != DrumArticulation.None)
            {
                return $"{baseId}_{articulation.Value}";
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
