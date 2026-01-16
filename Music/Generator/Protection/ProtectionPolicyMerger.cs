// AI: purpose=Merges GrooveProtectionPolicy.HierarchyLayers into a single RoleProtectionSet per role.
// AI: invariants=Layers are merged in order [0..n]; IsAdditiveOnly=true unions lists; false replaces.
// AI: deps=GrooveProtectionPolicy, RoleProtectionSet from Groove.cs; used by DrumTrackGeneratorNew.

namespace Music.Generator
{
    // AI: ProtectionPolicyMerger: Story 8 implementation; merges hierarchical protection layers respecting IsAdditiveOnly.
    internal static class ProtectionPolicyMerger
    {
        /// <summary>
        /// Merges protection hierarchy layers into a single RoleProtectionSet per role.
        /// Layers are applied in order. If layer.IsAdditiveOnly=true, onset lists are unioned;
        /// otherwise, the layer's lists replace any existing onsets for that role.
        /// </summary>
        /// <param name="protectionPolicy">The protection policy containing hierarchy layers.</param>
        /// <param name="enabledTags">Tags enabled for the current segment (used to filter layers by AppliesWhenTagsAll).</param>
        /// <returns>Dictionary mapping role name to merged RoleProtectionSet.</returns>
        public static Dictionary<string, RoleProtectionSet> MergeProtectionLayers(
            GrooveProtectionPolicy protectionPolicy,
            IReadOnlyList<string> enabledTags)
        {
            ArgumentNullException.ThrowIfNull(protectionPolicy);
            enabledTags ??= Array.Empty<string>();

            var result = new Dictionary<string, RoleProtectionSet>(StringComparer.OrdinalIgnoreCase);

            // Process each layer in hierarchy order: [0]=base, [1]=refine, [2]=grandchild, etc.
            foreach (var layer in protectionPolicy.HierarchyLayers)
            {
                // Check if layer applies based on AppliesWhenTagsAll
                if (!LayerApplies(layer, enabledTags))
                    continue;

                // Merge each role's protections from this layer
                foreach (var (roleName, roleProtection) in layer.RoleProtections)
                {
                    if (layer.IsAdditiveOnly)
                    {
                        // Union: add new onsets to existing
                        MergeAdditive(result, roleName, roleProtection);
                    }
                    else
                    {
                        // Replace: this layer's protections override previous
                        MergeReplace(result, roleName, roleProtection);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if a protection layer applies based on its AppliesWhenTagsAll condition.
        /// A layer applies if AppliesWhenTagsAll is empty OR all its tags are present in enabledTags.
        /// </summary>
        private static bool LayerApplies(GrooveProtectionLayer layer, IReadOnlyList<string> enabledTags)
        {
            // Empty AppliesWhenTagsAll means layer always applies
            if (layer.AppliesWhenTagsAll.Count == 0)
                return true;

            // All tags in AppliesWhenTagsAll must be present in enabledTags
            foreach (var requiredTag in layer.AppliesWhenTagsAll)
            {
                bool found = enabledTags.Any(t => 
                    string.Equals(t, requiredTag, StringComparison.OrdinalIgnoreCase));
                
                if (!found)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Additive merge: union onset lists from layer into result (no duplicates).
        /// </summary>
        private static void MergeAdditive(
            Dictionary<string, RoleProtectionSet> result,
            string roleName,
            RoleProtectionSet layerProtection)
        {
            if (!result.TryGetValue(roleName, out var existing))
            {
                // No existing entry; create new with copied lists
                result[roleName] = CloneProtectionSet(layerProtection);
                return;
            }

            // Union each list (no duplicates)
            UnionOnsets(existing.MustHitOnsets, layerProtection.MustHitOnsets);
            UnionOnsets(existing.ProtectedOnsets, layerProtection.ProtectedOnsets);
            UnionOnsets(existing.NeverRemoveOnsets, layerProtection.NeverRemoveOnsets);
            UnionOnsets(existing.NeverAddOnsets, layerProtection.NeverAddOnsets);
        }

        /// <summary>
        /// Replace merge: layer's protections replace existing role entry entirely.
        /// </summary>
        private static void MergeReplace(
            Dictionary<string, RoleProtectionSet> result,
            string roleName,
            RoleProtectionSet layerProtection)
        {
            result[roleName] = CloneProtectionSet(layerProtection);
        }

        /// <summary>
        /// Unions source onsets into target list (no duplicates).
        /// </summary>
        private static void UnionOnsets(List<decimal> target, List<decimal> source)
        {
            foreach (var onset in source)
            {
                if (!target.Contains(onset))
                {
                    target.Add(onset);
                }
            }
        }

        /// <summary>
        /// Creates a deep copy of a RoleProtectionSet to avoid shared references.
        /// </summary>
        private static RoleProtectionSet CloneProtectionSet(RoleProtectionSet source)
        {
            return new RoleProtectionSet
            {
                MustHitOnsets = new List<decimal>(source.MustHitOnsets),
                ProtectedOnsets = new List<decimal>(source.ProtectedOnsets),
                NeverRemoveOnsets = new List<decimal>(source.NeverRemoveOnsets),
                NeverAddOnsets = new List<decimal>(source.NeverAddOnsets)
            };
        }
    }
}
