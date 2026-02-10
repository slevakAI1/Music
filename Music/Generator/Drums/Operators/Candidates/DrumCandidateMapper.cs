// AI: purpose=Map OperatorCandidate -> DrumOnsetCandidate for groove layer; pass hints directly and add trace tags
// AI: invariants=Mapping must be deterministic; tags used for diagnostics only; velocity/timing flow via properties
// AI: deps=OperatorCandidate, DrumOnsetCandidate, FillRole, DrumArticulation; consumed by DrummerOperatorCandidates

using Music.Generator.Drums.Planning;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.Candidates
{
    // AI: contract=Static mapper; do not change tag prefixes (persisted in Tags); keep behavior deterministic
    public static class DrumCandidateMapper
    {
        // AI: tag=Prefix for CandidateId tag in DrumOnsetCandidate.Tags
        public const string CandidateIdTagPrefix = "CandidateId:";

        // AI: tag=Prefix for OperatorId tag in DrumOnsetCandidate.Tags
        public const string OperatorIdTagPrefix = "OperatorId:";

        // AI: tag=Tag value marking candidates that should be protected from pruning
        public const string ProtectedTag = "Protected";

        // AI: maps=Convert OperatorCandidate to DrumOnsetCandidate; deterministic; preserves hints and adds tags
        public static DrumOnsetCandidate Map(OperatorCandidate candidate)
        {
            ArgumentNullException.ThrowIfNull(candidate);

            var tags = BuildTags(candidate);

            return new DrumOnsetCandidate
            {
                Role = candidate.Role,
                OnsetBeat = candidate.Beat,
                Strength = candidate.Strength,
                ProbabilityBias = candidate.Score,
                MaxAddsPerBar = 1, // Default; can be overridden by operator
                Tags = tags,
                VelocityHint = candidate.VelocityHint,
                TimingHint = candidate.TimingHint
            };
        }

        // AI: maps=Map collection of OperatorCandidate to DrumOnsetCandidate preserving order
        public static IReadOnlyList<DrumOnsetCandidate> MapAll(IEnumerable<OperatorCandidate> candidates)
        {
            ArgumentNullException.ThrowIfNull(candidates);

            var result = new List<DrumOnsetCandidate>();
            foreach (var candidate in candidates)
            {
                result.Add(Map(candidate));
            }
            return result;
        }

        // AI: tags=Builds tags for traceability and selection; does NOT carry VelocityHint/TimingHint
        private static List<string> BuildTags(OperatorCandidate candidate)
        {
            var tags = new List<string>();

            // Traceability
            tags.Add($"{CandidateIdTagPrefix}{candidate.CandidateId}");
            tags.Add($"{OperatorIdTagPrefix}{candidate.OperatorId}");

            // Fill role and protection
            if (candidate.FillRole != FillRole.None)
            {
                tags.Add(candidate.FillRole.ToString());
                if (candidate.FillRole == FillRole.FillEnd)
                {
                    tags.Add(ProtectedTag);
                }
            }

            // Articulation
            if (candidate.ArticulationHint.HasValue && candidate.ArticulationHint.Value != DrumArticulation.None)
            {
                tags.Add(candidate.ArticulationHint.Value.ToString());
            }

            // Strength hint tag for downstream logic
            if (candidate.Strength == OnsetStrength.Downbeat || candidate.Strength == OnsetStrength.Backbeat)
            {
                tags.Add($"Strength:{candidate.Strength}");
            }

            return tags;
        }

        // AI: extract=Return original CandidateId from tags or null if missing
        public static string? ExtractCandidateId(DrumOnsetCandidate candidate)
        {
            ArgumentNullException.ThrowIfNull(candidate);

            foreach (var tag in candidate.Tags)
            {
                if (tag.StartsWith(CandidateIdTagPrefix, StringComparison.Ordinal))
                {
                    return tag.Substring(CandidateIdTagPrefix.Length);
                }
            }
            return null;
        }

        // AI: extract=Return original OperatorId from tags or null if missing
        public static string? ExtractOperatorId(DrumOnsetCandidate candidate)
        {
            ArgumentNullException.ThrowIfNull(candidate);

            foreach (var tag in candidate.Tags)
            {
                if (tag.StartsWith(OperatorIdTagPrefix, StringComparison.Ordinal))
                {
                    return tag.Substring(OperatorIdTagPrefix.Length);
                }
            }
            return null;
        }

        // AI: query=Checks Protected tag presence on DrumOnsetCandidate.Tags
        public static bool IsProtected(DrumOnsetCandidate candidate)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            return candidate.Tags.Contains(ProtectedTag);
        }
    }
}

