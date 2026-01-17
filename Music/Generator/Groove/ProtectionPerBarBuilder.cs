// AI: purpose=Builds per-bar merged protection dictionaries from protection policy and bar contexts.
// AI: deps=ProtectionPolicyMerger, BarContext, SegmentGrooveProfile; generator-agnostic.
// AI: invariants=Returns dictionary keyed by 1-based bar number; merges layers per enabled tags.

namespace Music.Generator
{
    /// <summary>
    /// Builds per-bar merged protection dictionaries from protection policy and bar contexts.
    /// Extracted from DrumTrackGenerator for cross-generator reuse (drums, comp, melody, motifs).
    /// </summary>
    public static class ProtectionPerBarBuilder
    {
        /// <summary>
        /// Builds a dictionary of merged role protections for each bar based on segment-enabled tags.
        /// </summary>
        /// <param name="barContexts">Per-bar context including segment profile with enabled tags.</param>
        /// <param name="protectionPolicy">Global protection policy with hierarchy layers.</param>
        /// <returns>Dictionary mapping bar number to role protection sets.</returns>
        public static Dictionary<int, Dictionary<string, RoleProtectionSet>> Build(
            IReadOnlyList<BarContext> barContexts,
            GrooveProtectionPolicy? protectionPolicy)
        {
            var result = new Dictionary<int, Dictionary<string, RoleProtectionSet>>();

            if (barContexts == null || barContexts.Count == 0)
                return result;

            foreach (var barCtx in barContexts)
            {
                if (protectionPolicy == null)
                {
                    // No policy means empty protections for this bar
                    result[barCtx.BarNumber] = new Dictionary<string, RoleProtectionSet>(StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    var enabledTags = barCtx.SegmentProfile?.EnabledProtectionTags ?? new List<string>();
                    var merged = ProtectionPolicyMerger.MergeProtectionLayers(protectionPolicy, enabledTags);
                    result[barCtx.BarNumber] = merged;
                }
            }

            return result;
        }
    }
}
