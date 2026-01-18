// AI: purpose=Adapter that wraps GrooveVariationCatalog as IGrooveCandidateSource (Story B4).
// AI: invariants=Deterministic; uses GrooveVariationLayerMerger and GrooveCandidateFilter.
// AI: deps=IGrooveCandidateSource, GrooveVariationCatalog, GrooveVariationLayerMerger, GrooveCandidateFilter.
// AI: change=Story B4 acceptance criteria: default implementation adapting existing catalog.

namespace Music.Generator
{
    /// <summary>
    /// Default candidate source that adapts GrooveVariationCatalog using layer merger and filter.
    /// Story B4: Operator Candidate Source Hook - baseline implementation for catalog-based candidates.
    /// </summary>
    public sealed class CatalogGrooveCandidateSource : IGrooveCandidateSource
    {
        private readonly GrooveVariationCatalog _catalog;
        private readonly GroovePhraseHookPolicy? _phraseHookPolicy;
        private readonly IGroovePolicyProvider? _policyProvider;

        /// <summary>
        /// Creates a catalog-based candidate source.
        /// </summary>
        /// <param name="catalog">Variation catalog with hierarchical layers.</param>
        /// <param name="phraseHookPolicy">Optional phrase hook policy for fill tag resolution.</param>
        /// <param name="policyProvider">Optional policy provider for overrides.</param>
        public CatalogGrooveCandidateSource(
            GrooveVariationCatalog catalog,
            GroovePhraseHookPolicy? phraseHookPolicy = null,
            IGroovePolicyProvider? policyProvider = null)
        {
            ArgumentNullException.ThrowIfNull(catalog);
            _catalog = catalog;
            _phraseHookPolicy = phraseHookPolicy;
            _policyProvider = policyProvider;
        }

        /// <summary>
        /// Gets candidate groups by merging catalog layers and filtering by enabled tags.
        /// Story B4: Uses GrooveVariationLayerMerger and GrooveCandidateFilter for processing.
        /// </summary>
        /// <param name="barContext">Bar context with segment profile and phrase position.</param>
        /// <param name="role">Role name for policy decision lookup.</param>
        /// <returns>Merged and filtered candidate groups for this bar and role.</returns>
        public IReadOnlyList<GrooveCandidateGroup> GetCandidateGroups(
            GrooveBarContext barContext,
            string role)
        {
            ArgumentNullException.ThrowIfNull(barContext);
            ArgumentNullException.ThrowIfNull(role);

            // Get policy decision if provider available
            var policyDecision = _policyProvider?.GetPolicy(barContext, role);

            // Resolve enabled tags from segment profile, phrase hooks, and policy override
            var isInFillWindow = DetermineIfInFillWindow(barContext);
            var enabledTags = GrooveCandidateFilter.ResolveEnabledTags(
                barContext.SegmentProfile,
                _phraseHookPolicy,
                policyDecision,
                isInFillWindow);

            // Story B4: Merge layers from catalog
            var mergedGroups = GrooveVariationLayerMerger.MergeLayersForBar(_catalog, enabledTags);

            // Story B4: Filter groups and candidates by enabled tags
            var filteredGroups = GrooveCandidateFilter.FilterGroupsAndCandidates(mergedGroups, enabledTags);

            return filteredGroups;
        }

        /// <summary>
        /// Determines if the current bar is within a fill window.
        /// Simple heuristic: last bar within section or phrase.
        /// </summary>
        private static bool DetermineIfInFillWindow(GrooveBarContext barContext)
        {
            // Simple heuristic: if we're within 1 bar of section end, consider it a fill window
            // This matches the PhraseHookPolicy.SectionEndBarsWindow = 1 pattern
            return barContext.BarsUntilSectionEnd <= 1;
        }
    }
}
