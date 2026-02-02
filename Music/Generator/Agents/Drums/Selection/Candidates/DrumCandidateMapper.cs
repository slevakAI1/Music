// AI: purpose=Maps DrumCandidate to DrumOnsetCandidate for groove system integration.
// AI: invariants=Deterministic: same input → same output; VelocityHint/TimingHint flow directly to DrumOnsetCandidate.
// AI: deps=DrumCandidate, DrumOnsetCandidate, FillRole, DrumArticulation; consumed by DrummerCandidateSource.
// AI: change=VelocityHint/TimingHint now flow directly instead of via tags.


using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums
{
    /// <summary>
    /// Maps DrumCandidate to DrumOnsetCandidate for groove system consumption.
    /// Preserves candidate identity and translates drum-specific hints.
    /// VelocityHint and TimingHint flow directly to DrumOnsetCandidate.
    /// Story 2.4: Implement Drummer Candidate Source.
    /// </summary>
    public static class DrumCandidateMapper
    {
        /// <summary>
        /// Tag prefix for candidate ID traceability.
        /// </summary>
        public const string CandidateIdTagPrefix = "CandidateId:";

        /// <summary>
        /// Tag prefix for operator ID traceability.
        /// </summary>
        public const string OperatorIdTagPrefix = "OperatorId:";

        /// <summary>
        /// Tag indicating a protected candidate (should not be pruned).
        /// </summary>
        public const string ProtectedTag = "Protected";

        /// <summary>
        /// Maps a DrumCandidate to a DrumOnsetCandidate.
        /// </summary>
        /// <param name="candidate">Source drum candidate.</param>
        /// <returns>Mapped groove onset candidate with hints and tags.</returns>
        /// <exception cref="ArgumentNullException">If candidate is null.</exception>
        public static DrumOnsetCandidate Map(DrumCandidate candidate)
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

        /// <summary>
        /// Maps multiple DrumCandidates to DrumOnsetCandidates.
        /// </summary>
        /// <param name="candidates">Source candidates.</param>
        /// <returns>Mapped candidates in same order.</returns>
        public static IReadOnlyList<DrumOnsetCandidate> MapAll(IEnumerable<DrumCandidate> candidates)
        {
            ArgumentNullException.ThrowIfNull(candidates);

            var result = new List<DrumOnsetCandidate>();
            foreach (var candidate in candidates)
            {
                result.Add(Map(candidate));
            }
            return result;
        }

        /// <summary>
        /// Builds tag list from DrumCandidate hints and metadata.
        /// </summary>
        private static List<string> BuildTags(DrumCandidate candidate)
        {
            var tags = new List<string>();

            // Traceability tags
            tags.Add($"{CandidateIdTagPrefix}{candidate.CandidateId}");
            tags.Add($"{OperatorIdTagPrefix}{candidate.OperatorId}");

            // Fill role tags
            if (candidate.FillRole != FillRole.None)
            {
                tags.Add(candidate.FillRole.ToString());

                // FillEnd candidates are protected (crash after fill)
                if (candidate.FillRole == FillRole.FillEnd)
                {
                    tags.Add(ProtectedTag);
                }
            }

            // Articulation tags
            if (candidate.ArticulationHint.HasValue && candidate.ArticulationHint.Value != DrumArticulation.None)
            {
                tags.Add(candidate.ArticulationHint.Value.ToString());
            }

            // Strength-based protection (Downbeat and Backbeat are semi-protected)
            if (candidate.Strength == OnsetStrength.Downbeat || candidate.Strength == OnsetStrength.Backbeat)
            {
                // Don't add Protected tag, but add Strength tag for downstream consideration
                tags.Add($"Strength:{candidate.Strength}");
            }

            // Note: VelocityHint and TimingHint now flow directly via DrumOnsetCandidate properties,
            // not via tags. Tags are kept for traceability/diagnostics only.

            return tags;
        }

        /// <summary>
        /// Extracts the original CandidateId from a mapped DrumOnsetCandidate's tags.
        /// </summary>
        /// <param name="candidate">Mapped candidate with tags.</param>
        /// <returns>Original CandidateId if found, null otherwise.</returns>
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

        /// <summary>
        /// Extracts the original OperatorId from a mapped DrumOnsetCandidate's tags.
        /// </summary>
        /// <param name="candidate">Mapped candidate with tags.</param>
        /// <returns>Original OperatorId if found, null otherwise.</returns>
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

        /// <summary>
        /// Checks if a candidate is marked as protected.
        /// </summary>
        /// <param name="candidate">Candidate to check.</param>
        /// <returns>True if candidate has Protected tag.</returns>
        public static bool IsProtected(DrumOnsetCandidate candidate)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            return candidate.Tags.Contains(ProtectedTag);
        }
    }
}

