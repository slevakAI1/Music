// AI: purpose=Maps DrumCandidate to GrooveOnsetCandidate for groove system integration.
// AI: invariants=Deterministic: same input → same output; CandidateId preserved in Tags; null hints handled safely.
// AI: deps=DrumCandidate, GrooveOnsetCandidate, FillRole, DrumArticulation; consumed by DrummerCandidateSource.
// AI: change=Story 2.4; extend with additional hint mappings as operators evolve.

namespace Music.Generator.Agents.Drums
{
    /// <summary>
    /// Maps DrumCandidate to GrooveOnsetCandidate for groove system consumption.
    /// Preserves candidate identity and translates drum-specific hints to tags.
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
        /// Maps a DrumCandidate to a GrooveOnsetCandidate.
        /// </summary>
        /// <param name="candidate">Source drum candidate.</param>
        /// <returns>Mapped groove onset candidate with tags from hints.</returns>
        /// <exception cref="ArgumentNullException">If candidate is null.</exception>
        public static GrooveOnsetCandidate Map(DrumCandidate candidate)
        {
            ArgumentNullException.ThrowIfNull(candidate);

            var tags = BuildTags(candidate);

            return new GrooveOnsetCandidate
            {
                Role = candidate.Role,
                OnsetBeat = candidate.Beat,
                Strength = candidate.Strength,
                ProbabilityBias = candidate.Score,
                MaxAddsPerBar = 1, // Default; can be overridden by operator
                Tags = tags
            };
        }

        /// <summary>
        /// Maps multiple DrumCandidates to GrooveOnsetCandidates.
        /// </summary>
        /// <param name="candidates">Source candidates.</param>
        /// <returns>Mapped candidates in same order.</returns>
        public static IReadOnlyList<GrooveOnsetCandidate> MapAll(IEnumerable<DrumCandidate> candidates)
        {
            ArgumentNullException.ThrowIfNull(candidates);

            var result = new List<GrooveOnsetCandidate>();
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

            // Velocity hint tag (for downstream velocity shaping)
            if (candidate.VelocityHint.HasValue)
            {
                tags.Add($"VelocityHint:{candidate.VelocityHint.Value}");
            }

            // Timing hint tag (for downstream timing shaping)
            if (candidate.TimingHint.HasValue)
            {
                tags.Add($"TimingHint:{candidate.TimingHint.Value}");
            }

            return tags;
        }

        /// <summary>
        /// Extracts the original CandidateId from a mapped GrooveOnsetCandidate's tags.
        /// </summary>
        /// <param name="candidate">Mapped candidate with tags.</param>
        /// <returns>Original CandidateId if found, null otherwise.</returns>
        public static string? ExtractCandidateId(GrooveOnsetCandidate candidate)
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
        /// Extracts the original OperatorId from a mapped GrooveOnsetCandidate's tags.
        /// </summary>
        /// <param name="candidate">Mapped candidate with tags.</param>
        /// <returns>Original OperatorId if found, null otherwise.</returns>
        public static string? ExtractOperatorId(GrooveOnsetCandidate candidate)
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
        public static bool IsProtected(GrooveOnsetCandidate candidate)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            return candidate.Tags.Contains(ProtectedTag);
        }
    }
}
