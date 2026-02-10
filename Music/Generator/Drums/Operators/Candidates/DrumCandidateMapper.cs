// AI: purpose=Map OperatorCandidateAddition -> DrumOnsetCandidate for groove layer; pass hints directly and add trace tags
// AI: invariants=Mapping must be deterministic; tags used for diagnostics only; velocity/timing flow via properties
// AI: deps=OperatorCandidateAddition, DrumOnsetCandidate, FillRole, DrumArticulation; consumed by DrummerOperatorCandidates

using Music.Generator.Core;
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

        // AI: maps=Convert OperatorCandidateAddition to DrumOnsetCandidate; deterministic; preserves hints and adds tags
        public static DrumOnsetCandidate Map(OperatorCandidateAddition candidate)
        {
            ArgumentNullException.ThrowIfNull(candidate);

            var tags = BuildTags(candidate);

            return new DrumOnsetCandidate
            {
                Role = candidate.Role,
                OnsetBeat = candidate.Beat,
                Strength = candidate.GetStrength(),
                ProbabilityBias = candidate.Score,
                MaxAddsPerBar = 1, // Default; can be overridden by operator
                Tags = tags,
                VelocityHint = candidate.VelocityHint,
                TimingHint = candidate.TimingHint
            };
        }

        // AI: maps=Map collection of OperatorCandidateAddition to DrumOnsetCandidate preserving order
        public static IReadOnlyList<DrumOnsetCandidate> MapAll(IEnumerable<OperatorCandidateAddition> candidates)
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
        private static List<string> BuildTags(OperatorCandidateAddition candidate)
        {
            var tags = new List<string>();

            // Traceability
            tags.Add($"{CandidateIdTagPrefix}{candidate.CandidateId}");
            tags.Add($"{OperatorIdTagPrefix}{candidate.OperatorId}");

            // Fill role and protection
            var fillRole = candidate.GetFillRole();
            if (fillRole != FillRole.None)
            {
                tags.Add(fillRole.ToString());
                if (fillRole == FillRole.FillEnd)
                {
                    tags.Add(ProtectedTag);
                }
            }

            // Articulation
            var articulationHint = candidate.GetArticulationHint();
            if (articulationHint.HasValue && articulationHint.Value != DrumArticulation.None)
            {
                tags.Add(articulationHint.Value.ToString());
            }

            // Strength hint tag for downstream logic
            var strength = candidate.GetStrength();
            if (strength == OnsetStrength.Downbeat || strength == OnsetStrength.Backbeat)
            {
                tags.Add($"Strength:{strength}");
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

