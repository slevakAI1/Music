// AI: purpose=Merges variation layers from GrooveVariationCatalog with tag-gated additive/replace logic (Story B1).
// AI: invariants=Deterministic ordering; same inputs => same output; stable sort by layer order + group id.
// AI: deps=GrooveVariationCatalog, GrooveVariationLayer, GrooveCandidateGroup from Groove.cs.
// AI: change=Story B1 acceptance criteria: iterate layers, apply tags, merge additive/replace, preserve ordering.

namespace Music.Generator
{
    /// <summary>
    /// Merges variation layers from a GrooveVariationCatalog with tag-gated additive/replace logic.
    /// Story B1: Implements catalog merge with deterministic ordering.
    /// </summary>
    public static class GrooveVariationLayerMerger
    {
        /// <summary>
        /// Merges all applicable layers from the catalog for the given enabled tags.
        /// Returns a flattened list of candidate groups in deterministic order.
        /// </summary>
        /// <param name="catalog">The variation catalog containing hierarchical layers.</param>
        /// <param name="enabledTags">Tags currently enabled for this bar/segment.</param>
        /// <returns>Merged candidate groups in deterministic order (by layer order, then group id).</returns>
        public static IReadOnlyList<GrooveCandidateGroup> MergeLayersForBar(
            GrooveVariationCatalog catalog,
            IReadOnlySet<string> enabledTags)
        {
            ArgumentNullException.ThrowIfNull(catalog);
            ArgumentNullException.ThrowIfNull(enabledTags);

            // Working set of merged groups, keyed by GroupId for deduplication
            var workingSet = new Dictionary<string, GrooveCandidateGroup>(StringComparer.Ordinal);

            // Track layer order for stable sorting
            var groupLayerOrder = new Dictionary<string, int>(StringComparer.Ordinal);
            int layerIndex = 0;

            // Iterate layers in order (Story B1: "Iterate GrooveVariationCatalog.HierarchyLayers in order")
            foreach (var layer in catalog.HierarchyLayers)
            {
                // Story B1: "Apply AppliesWhenTagsAll against bar's enabled tags"
                if (!LayerApplies(layer, enabledTags))
                {
                    layerIndex++;
                    continue;
                }

                if (layer.IsAdditiveOnly)
                {
                    // Story B1: "If IsAdditiveOnly=true, union candidate groups (dedupe by stable id)"
                    ApplyAdditiveLayer(layer, workingSet, groupLayerOrder, layerIndex);
                }
                else
                {
                    // Story B1: "If IsAdditiveOnly=false, replace the working set entirely"
                    ApplyReplaceLayer(layer, workingSet, groupLayerOrder, layerIndex);
                }

                layerIndex++;
            }

            // Story B1: "Preserve deterministic ordering in the merged result (stable sort by layer order + group id)"
            return SortGroupsDeterministically(workingSet.Values, groupLayerOrder);
        }

        /// <summary>
        /// Checks if a layer applies based on its AppliesWhenTagsAll requirement.
        /// Empty AppliesWhenTagsAll means "always applies".
        /// </summary>
        private static bool LayerApplies(GrooveVariationLayer layer, IReadOnlySet<string> enabledTags)
        {
            // Empty/null AppliesWhenTagsAll means layer always applies
            if (layer.AppliesWhenTagsAll is null || layer.AppliesWhenTagsAll.Count == 0)
            {
                return true;
            }

            // All required tags must be present in enabledTags
            foreach (var requiredTag in layer.AppliesWhenTagsAll)
            {
                if (!enabledTags.Contains(requiredTag))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Applies an additive layer: unions groups with working set, deduping by GroupId.
        /// If a group with the same ID already exists, the existing one is kept (first-wins).
        /// </summary>
        private static void ApplyAdditiveLayer(
            GrooveVariationLayer layer,
            Dictionary<string, GrooveCandidateGroup> workingSet,
            Dictionary<string, int> groupLayerOrder,
            int layerIndex)
        {
            foreach (var group in layer.CandidateGroups)
            {
                // Dedupe by stable id: only add if not already present
                if (!workingSet.ContainsKey(group.GroupId))
                {
                    workingSet[group.GroupId] = group;
                    groupLayerOrder[group.GroupId] = layerIndex;
                }
            }
        }

        /// <summary>
        /// Applies a replace layer: clears the working set and adds all groups from this layer.
        /// </summary>
        private static void ApplyReplaceLayer(
            GrooveVariationLayer layer,
            Dictionary<string, GrooveCandidateGroup> workingSet,
            Dictionary<string, int> groupLayerOrder,
            int layerIndex)
        {
            // Clear existing working set
            workingSet.Clear();
            groupLayerOrder.Clear();

            // Add all groups from this layer
            foreach (var group in layer.CandidateGroups)
            {
                workingSet[group.GroupId] = group;
                groupLayerOrder[group.GroupId] = layerIndex;
            }
        }

        /// <summary>
        /// Sorts groups deterministically by layer order (ascending), then by GroupId (ordinal).
        /// </summary>
        private static IReadOnlyList<GrooveCandidateGroup> SortGroupsDeterministically(
            IEnumerable<GrooveCandidateGroup> groups,
            Dictionary<string, int> groupLayerOrder)
        {
            return groups
                .OrderBy(g => groupLayerOrder.TryGetValue(g.GroupId, out var order) ? order : int.MaxValue)
                .ThenBy(g => g.GroupId, StringComparer.Ordinal)
                .ToList();
        }
    }
}
