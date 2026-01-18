// AI: purpose=Filters groove candidate groups and candidates by enabled tags (Story B2).
// AI: invariants=Deterministic filtering; empty/null tags = "match all"; same inputs => same output.
// AI: deps=GrooveCandidateGroup, GrooveOnsetCandidate, SegmentGrooveProfile, GroovePhraseHookPolicy, GroovePolicyDecision.
// AI: change=Story B2 acceptance criteria: resolve tags, filter groups/candidates by tag intersection.

namespace Music.Generator
{
    /// <summary>
    /// Filters groove candidate groups and candidates by enabled tags.
    /// Story B2: Implements tag-based filtering for variation candidates.
    /// </summary>
    public static class GrooveCandidateFilter
    {
        /// <summary>
        /// Resolves the effective enabled tags for a bar from multiple sources.
        /// Priority: PolicyDecision override > (SegmentProfile + PhraseHookPolicy union).
        /// </summary>
        /// <param name="segmentProfile">Segment profile with enabled variation tags (may be null).</param>
        /// <param name="phraseHookPolicy">Phrase hook policy with enabled fill tags (may be null).</param>
        /// <param name="policyDecision">Optional policy decision with tag override (may be null).</param>
        /// <param name="isInFillWindow">Whether the current bar is within a fill window (activates fill tags).</param>
        /// <returns>Set of enabled tags for filtering.</returns>
        public static IReadOnlySet<string> ResolveEnabledTags(
            SegmentGrooveProfile? segmentProfile,
            GroovePhraseHookPolicy? phraseHookPolicy,
            GroovePolicyDecision? policyDecision,
            bool isInFillWindow = false)
        {
            // Story B2: Policy override takes precedence if provided
            if (policyDecision?.EnabledVariationTagsOverride is not null)
            {
                return policyDecision.EnabledVariationTagsOverride.ToHashSet(StringComparer.Ordinal);
            }

            // Otherwise, union segment profile tags + phrase hook fill tags (if in fill window)
            var enabledTags = new HashSet<string>(StringComparer.Ordinal);

            // Add segment profile tags
            if (segmentProfile?.EnabledVariationTags is not null)
            {
                foreach (var tag in segmentProfile.EnabledVariationTags)
                {
                    enabledTags.Add(tag);
                }
            }

            // Add phrase hook fill tags only when in a fill window
            if (isInFillWindow && phraseHookPolicy?.EnabledFillTags is not null)
            {
                foreach (var tag in phraseHookPolicy.EnabledFillTags)
                {
                    enabledTags.Add(tag);
                }
            }

            return enabledTags;
        }

        /// <summary>
        /// Filters candidate groups by enabled tags.
        /// Groups with empty/null GroupTags always match ("match all" semantics).
        /// Groups with tags match if ANY tag intersects with enabled tags.
        /// </summary>
        /// <param name="groups">Candidate groups to filter.</param>
        /// <param name="enabledTags">Currently enabled tags.</param>
        /// <returns>Filtered groups in deterministic order (preserves input order).</returns>
        public static IReadOnlyList<GrooveCandidateGroup> FilterGroups(
            IEnumerable<GrooveCandidateGroup> groups,
            IReadOnlySet<string> enabledTags)
        {
            ArgumentNullException.ThrowIfNull(groups);
            ArgumentNullException.ThrowIfNull(enabledTags);

            var result = new List<GrooveCandidateGroup>();

            foreach (var group in groups)
            {
                if (GroupMatchesTags(group, enabledTags))
                {
                    result.Add(group);
                }
            }

            return result;
        }

        /// <summary>
        /// Filters candidates within a group by enabled tags.
        /// Candidates with empty/null Tags always match ("match all" semantics).
        /// Candidates with tags match if ANY tag intersects with enabled tags.
        /// </summary>
        /// <param name="candidates">Candidates to filter.</param>
        /// <param name="enabledTags">Currently enabled tags.</param>
        /// <returns>Filtered candidates in deterministic order (preserves input order).</returns>
        public static IReadOnlyList<GrooveOnsetCandidate> FilterCandidates(
            IEnumerable<GrooveOnsetCandidate> candidates,
            IReadOnlySet<string> enabledTags)
        {
            ArgumentNullException.ThrowIfNull(candidates);
            ArgumentNullException.ThrowIfNull(enabledTags);

            var result = new List<GrooveOnsetCandidate>();

            foreach (var candidate in candidates)
            {
                if (CandidateMatchesTags(candidate, enabledTags))
                {
                    result.Add(candidate);
                }
            }

            return result;
        }

        /// <summary>
        /// Filters groups and then filters candidates within each matched group.
        /// Returns a new list of groups with only matching candidates.
        /// </summary>
        /// <param name="groups">Candidate groups to filter.</param>
        /// <param name="enabledTags">Currently enabled tags.</param>
        /// <returns>Filtered groups with filtered candidates, in deterministic order.</returns>
        public static IReadOnlyList<GrooveCandidateGroup> FilterGroupsAndCandidates(
            IEnumerable<GrooveCandidateGroup> groups,
            IReadOnlySet<string> enabledTags)
        {
            ArgumentNullException.ThrowIfNull(groups);
            ArgumentNullException.ThrowIfNull(enabledTags);

            var result = new List<GrooveCandidateGroup>();

            foreach (var group in groups)
            {
                if (!GroupMatchesTags(group, enabledTags))
                {
                    continue;
                }

                // Filter candidates within the matched group
                var filteredCandidates = FilterCandidates(group.Candidates, enabledTags);

                if (filteredCandidates.Count > 0)
                {
                    // Create a new group with filtered candidates (preserve other properties)
                    var filteredGroup = new GrooveCandidateGroup
                    {
                        GroupId = group.GroupId,
                        GroupTags = group.GroupTags,
                        MaxAddsPerBar = group.MaxAddsPerBar,
                        BaseProbabilityBias = group.BaseProbabilityBias,
                        Candidates = filteredCandidates.ToList()
                    };
                    result.Add(filteredGroup);
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if a group matches the enabled tags.
        /// Empty/null GroupTags = "match all" (always matches).
        /// Non-empty GroupTags = match if ANY tag intersects with enabledTags.
        /// </summary>
        private static bool GroupMatchesTags(GrooveCandidateGroup group, IReadOnlySet<string> enabledTags)
        {
            // Story B2: Treat empty/null GroupTags as "match all"
            if (group.GroupTags is null || group.GroupTags.Count == 0)
            {
                return true;
            }

            // Story B2: Filter when ANY GroupTags intersects enabled tags
            foreach (var tag in group.GroupTags)
            {
                if (enabledTags.Contains(tag))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a candidate matches the enabled tags.
        /// Empty/null Tags = "match all" (always matches).
        /// Non-empty Tags = match if ANY tag intersects with enabledTags.
        /// </summary>
        private static bool CandidateMatchesTags(GrooveOnsetCandidate candidate, IReadOnlySet<string> enabledTags)
        {
            // Story B2: Treat empty/null Candidate.Tags as "match all"
            if (candidate.Tags is null || candidate.Tags.Count == 0)
            {
                return true;
            }

            // Story B2: Filter when ANY tag intersects enabled tags
            foreach (var tag in candidate.Tags)
            {
                if (enabledTags.Contains(tag))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
