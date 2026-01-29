// AI: purpose=Enforce override merge policy for segment overrides (Story F1).
// AI: invariants=Deterministic; same inputs => same output; respects all 4 policy booleans.
// AI: deps=GrooveOverrideMergePolicy, SegmentGrooveProfile, RoleDensityTarget (moved to Drums in Story 4.3).
// AI: change=Story 4.3: Import RoleDensityTarget from Drums namespace.

using Music.Generator.Agents.Drums;

namespace Music.Generator.Groove;

/// <summary>
/// Enforces override merge policy semantics for segment-level overrides.
/// Story F1: Controls how segment overrides combine with base preset values.
/// </summary>
/// <remarks>
/// Policy booleans:
/// - OverrideReplacesLists: true=replace base list, false=union/merge
/// - OverrideCanRemoveProtectedOnsets: true=IsProtected can be pruned, false=only unprotected
/// - OverrideCanRelaxConstraints: true=segment can increase caps, false=min(segment, base) wins
/// - OverrideCanChangeFeel: true=allow segment feel/swing, false=use base only
/// 
/// Default policy (all false) is the safest: merge lists, protect all, no cap increase, no feel change.
/// </remarks>
public static class OverrideMergePolicyEnforcer
{
    // Hard ceilings for cap relaxation (Story F1 Q5)
    public const int MaxHitsPerBarCeiling = 32;
    public const int MaxHitsPerBeatCeiling = 8;

    /// <summary>
    /// Resolves effective variation tags based on merge policy.
    /// </summary>
    /// <param name="baseTags">Base variation tags from preset/catalog.</param>
    /// <param name="segmentTags">Segment override tags (may be null/empty).</param>
    /// <param name="mergePolicy">Policy controlling merge behavior.</param>
    /// <returns>Effective list of enabled variation tags.</returns>
    public static List<string> ResolveEffectiveVariationTags(
        IReadOnlyList<string>? baseTags,
        IReadOnlyList<string>? segmentTags,
        GrooveOverrideMergePolicy mergePolicy)
    {
        return ResolveEffectiveStringList(baseTags, segmentTags, mergePolicy.OverrideReplacesLists);
    }

    /// <summary>
    /// Resolves effective protection tags based on merge policy.
    /// </summary>
    /// <param name="baseTags">Base protection tags from preset.</param>
    /// <param name="segmentTags">Segment override tags (may be null/empty).</param>
    /// <param name="mergePolicy">Policy controlling merge behavior.</param>
    /// <returns>Effective list of enabled protection tags.</returns>
    public static List<string> ResolveEffectiveProtectionTags(
        IReadOnlyList<string>? baseTags,
        IReadOnlyList<string>? segmentTags,
        GrooveOverrideMergePolicy mergePolicy)
    {
        return ResolveEffectiveStringList(baseTags, segmentTags, mergePolicy.OverrideReplacesLists);
    }

    /// <summary>
    /// Resolves effective density targets based on merge policy.
    /// </summary>
    /// <param name="baseTargets">Base density targets from preset.</param>
    /// <param name="segmentTargets">Segment override targets (may be null/empty).</param>
    /// <param name="mergePolicy">Policy controlling merge behavior.</param>
    /// <returns>Effective list of density targets.</returns>
    public static List<RoleDensityTarget> ResolveEffectiveDensityTargets(
        IReadOnlyList<RoleDensityTarget>? baseTargets,
        IReadOnlyList<RoleDensityTarget>? segmentTargets,
        GrooveOverrideMergePolicy mergePolicy)
    {
        var baseList = baseTargets?.ToList() ?? new List<RoleDensityTarget>();
        var segmentList = segmentTargets?.ToList() ?? new List<RoleDensityTarget>();

        if (mergePolicy.OverrideReplacesLists)
        {
            // Replace: segment list replaces base (empty means clear all)
            return segmentList;
        }

        // Merge: union by role, segment values take precedence for same role
        if (segmentList.Count == 0)
        {
            return baseList;
        }

        var result = new List<RoleDensityTarget>(baseList);
        var existingRoles = new HashSet<string>(baseList.Select(t => t.Role), StringComparer.Ordinal);

        foreach (var target in segmentList)
        {
            if (existingRoles.Contains(target.Role))
            {
                // Replace existing entry for this role
                int index = result.FindIndex(t => string.Equals(t.Role, target.Role, StringComparison.Ordinal));
                if (index >= 0)
                {
                    result[index] = target;
                }
            }
            else
            {
                result.Add(target);
                existingRoles.Add(target.Role);
            }
        }

        return result;
    }

    /// <summary>
    /// Determines if an onset can be removed during pruning based on its protection status and policy.
    /// </summary>
    /// <param name="onset">The onset to check.</param>
    /// <param name="mergePolicy">Policy controlling removal behavior.</param>
    /// <returns>True if the onset can be removed; false if protected.</returns>
    /// <remarks>
    /// Protection hierarchy (Story F1 Q4):
    /// - IsMustHit: NEVER removable (absolute structural anchor)
    /// - IsNeverRemove: NEVER removable (style-defining)
    /// - IsProtected: Removable ONLY when OverrideCanRemoveProtectedOnsets=true
    /// - Unprotected: Always removable
    /// </remarks>
    public static bool CanRemoveOnset(GrooveOnset onset, GrooveOverrideMergePolicy mergePolicy)
    {
        ArgumentNullException.ThrowIfNull(onset);

        // IsMustHit and IsNeverRemove are NEVER removable regardless of policy
        if (onset.IsMustHit || onset.IsNeverRemove)
        {
            return false;
        }

        // IsProtected is only removable when policy allows
        if (onset.IsProtected)
        {
            return mergePolicy.OverrideCanRemoveProtectedOnsets;
        }

        // Unprotected onsets can always be removed
        return true;
    }

    /// <summary>
    /// Computes effective cap value based on segment override, base cap, and relaxation policy.
    /// </summary>
    /// <param name="baseCap">Base cap from preset/policy.</param>
    /// <param name="segmentCap">Segment override cap (may be null for no override).</param>
    /// <param name="mergePolicy">Policy controlling relaxation behavior.</param>
    /// <param name="ceiling">Absolute ceiling value to enforce regardless of policy.</param>
    /// <returns>Effective cap value after policy enforcement.</returns>
    /// <remarks>
    /// Story F1 Q5:
    /// - When CanRelax=false: effective = min(segment ?? base, base)
    /// - When CanRelax=true: effective = min(segment ?? base, ceiling)
    /// - Always clamp to [0..ceiling]
    /// </remarks>
    public static int ResolveEffectiveCap(
        int baseCap,
        int? segmentCap,
        GrooveOverrideMergePolicy mergePolicy,
        int ceiling)
    {
        int segmentValue = segmentCap ?? baseCap;

        int effectiveCap;
        if (mergePolicy.OverrideCanRelaxConstraints)
        {
            // Can increase, but still bounded by ceiling
            effectiveCap = Math.Min(segmentValue, ceiling);
        }
        else
        {
            // Cannot increase beyond base
            effectiveCap = Math.Min(segmentValue, baseCap);
        }

        // Always clamp to valid range
        return Math.Clamp(effectiveCap, 0, ceiling);
    }

    /// <summary>
    /// Resolves effective MaxHitsPerBar with policy enforcement.
    /// </summary>
    public static int ResolveEffectiveMaxHitsPerBar(
        int baseCap,
        int? segmentCap,
        GrooveOverrideMergePolicy mergePolicy)
    {
        return ResolveEffectiveCap(baseCap, segmentCap, mergePolicy, MaxHitsPerBarCeiling);
    }

    /// <summary>
    /// Resolves effective MaxHitsPerBeat with policy enforcement.
    /// </summary>
    public static int ResolveEffectiveMaxHitsPerBeat(
        int baseCap,
        int? segmentCap,
        GrooveOverrideMergePolicy mergePolicy)
    {
        return ResolveEffectiveCap(baseCap, segmentCap, mergePolicy, MaxHitsPerBeatCeiling);
    }

    /// <summary>
    /// Resolves effective feel based on policy.
    /// </summary>
    /// <param name="baseFeel">Base feel from subdivision policy.</param>
    /// <param name="segmentFeel">Segment override feel (may be null).</param>
    /// <param name="mergePolicy">Policy controlling feel changes.</param>
    /// <returns>Effective feel to use.</returns>
    public static GrooveFeel ResolveEffectiveFeel(
        GrooveFeel baseFeel,
        GrooveFeel? segmentFeel,
        GrooveOverrideMergePolicy mergePolicy)
    {
        if (!mergePolicy.OverrideCanChangeFeel)
        {
            return baseFeel;
        }

        return segmentFeel ?? baseFeel;
    }

    /// <summary>
    /// Resolves effective swing amount based on policy.
    /// </summary>
    /// <param name="baseSwing">Base swing amount from subdivision policy.</param>
    /// <param name="segmentSwing">Segment override swing (may be null).</param>
    /// <param name="mergePolicy">Policy controlling feel changes.</param>
    /// <returns>Effective swing amount clamped to [0.0..1.0].</returns>
    public static double ResolveEffectiveSwingAmount(
        double baseSwing,
        double? segmentSwing,
        GrooveOverrideMergePolicy mergePolicy)
    {
        if (!mergePolicy.OverrideCanChangeFeel)
        {
            return Math.Clamp(baseSwing, 0.0, 1.0);
        }

        double value = segmentSwing ?? baseSwing;
        return Math.Clamp(value, 0.0, 1.0);
    }

    /// <summary>
    /// Generic string list resolution based on replace vs merge policy.
    /// </summary>
    private static List<string> ResolveEffectiveStringList(
        IReadOnlyList<string>? baseList,
        IReadOnlyList<string>? segmentList,
        bool replaceMode)
    {
        var baseItems = baseList?.ToList() ?? new List<string>();
        var segmentItems = segmentList?.ToList() ?? new List<string>();

        if (replaceMode)
        {
            // Replace: segment list replaces base (empty means clear all)
            return segmentItems;
        }

        // Merge: union of both lists, null/empty segment means use base only
        if (segmentItems.Count == 0)
        {
            return baseItems;
        }

        // Union with dedupe (case-sensitive for tags)
        var result = new HashSet<string>(baseItems, StringComparer.Ordinal);
        foreach (var item in segmentItems)
        {
            result.Add(item);
        }

        return result.ToList();
    }
}
