// AI: purpose=Story H1 matrix tests for GrooveOverrideMergePolicy (Story F1 verification).
// AI: invariants=Test each of 4 policy booleans in true/false states; deterministic; no RNG needed.
// AI: deps=OverrideMergePolicyEnforcer, GrooveOverrideMergePolicy, GrooveOnset, GrooveFeel.

using Music.Generator;
using Xunit;

namespace Music.Tests.Generator.Groove;

/// <summary>
/// Story H1: Matrix tests for GrooveOverrideMergePolicy.
/// Tests each of the 4 boolean flags in both true and false states,
/// plus combined all-true and all-false scenarios.
/// </summary>
public class OverrideMergePolicyMatrixTests
{
    #region Test Data Helpers

    private static GrooveOverrideMergePolicy CreatePolicy(
        bool replacesLists = false,
        bool canRemoveProtected = false,
        bool canRelax = false,
        bool canChangeFeel = false)
    {
        return new GrooveOverrideMergePolicy
        {
            OverrideReplacesLists = replacesLists,
            OverrideCanRemoveProtectedOnsets = canRemoveProtected,
            OverrideCanRelaxConstraints = canRelax,
            OverrideCanChangeFeel = canChangeFeel
        };
    }

    private static GrooveOnset CreateOnset(
        bool isMustHit = false,
        bool isNeverRemove = false,
        bool isProtected = false)
    {
        return new GrooveOnset
        {
            Role = "Kick",
            BarNumber = 1,
            Beat = 1.0m,
            Strength = OnsetStrength.Downbeat,
            Velocity = 100,
            TimingOffsetTicks = 0,
            IsMustHit = isMustHit,
            IsNeverRemove = isNeverRemove,
            IsProtected = isProtected
        };
    }

    #endregion

    #region OverrideReplacesLists Tests

    [Fact]
    public void OverrideMergePolicy_ReplacesListsTrue_ReplacesVariationTags()
    {
        // Arrange
        var baseTags = new List<string> { "Fill", "Groove" };
        var segmentTags = new List<string> { "Intro" };
        var policy = CreatePolicy(replacesLists: true);

        // Act
        var result = OverrideMergePolicyEnforcer.ResolveEffectiveVariationTags(baseTags, segmentTags, policy);

        // Assert: segment replaces base entirely
        Assert.Single(result);
        Assert.Equal("Intro", result[0]);
    }

    [Fact]
    public void OverrideMergePolicy_ReplacesListsFalse_UnionsVariationTags()
    {
        // Arrange
        var baseTags = new List<string> { "Fill", "Groove" };
        var segmentTags = new List<string> { "Intro" };
        var policy = CreatePolicy(replacesLists: false);

        // Act
        var result = OverrideMergePolicyEnforcer.ResolveEffectiveVariationTags(baseTags, segmentTags, policy);

        // Assert: union of both lists
        Assert.Equal(3, result.Count);
        Assert.Contains("Fill", result);
        Assert.Contains("Groove", result);
        Assert.Contains("Intro", result);
    }

    #endregion

    #region OverrideCanRemoveProtectedOnsets Tests

    [Fact]
    public void OverrideMergePolicy_CanRemoveProtectedTrue_ProtectedOnsetRemovable()
    {
        // Arrange
        var onset = CreateOnset(isProtected: true);
        var policy = CreatePolicy(canRemoveProtected: true);

        // Act
        bool canRemove = OverrideMergePolicyEnforcer.CanRemoveOnset(onset, policy);

        // Assert: protected can be removed when policy allows
        Assert.True(canRemove);
    }

    [Fact]
    public void OverrideMergePolicy_CanRemoveProtectedFalse_ProtectedOnsetNotRemovable()
    {
        // Arrange
        var onset = CreateOnset(isProtected: true);
        var policy = CreatePolicy(canRemoveProtected: false);

        // Act
        bool canRemove = OverrideMergePolicyEnforcer.CanRemoveOnset(onset, policy);

        // Assert: protected cannot be removed
        Assert.False(canRemove);
    }

    [Fact]
    public void OverrideMergePolicy_MustHitNeverRemovable_RegardlessOfPolicy()
    {
        // Arrange: IsMustHit is the strongest protection
        var onset = CreateOnset(isMustHit: true);
        var policy = CreatePolicy(canRemoveProtected: true); // Even with permission

        // Act
        bool canRemove = OverrideMergePolicyEnforcer.CanRemoveOnset(onset, policy);

        // Assert: IsMustHit is NEVER removable
        Assert.False(canRemove);
    }

    [Fact]
    public void OverrideMergePolicy_IsNeverRemoveNeverRemovable_RegardlessOfPolicy()
    {
        // Arrange: IsNeverRemove is also absolute
        var onset = CreateOnset(isNeverRemove: true);
        var policy = CreatePolicy(canRemoveProtected: true);

        // Act
        bool canRemove = OverrideMergePolicyEnforcer.CanRemoveOnset(onset, policy);

        // Assert: IsNeverRemove is NEVER removable
        Assert.False(canRemove);
    }

    #endregion

    #region OverrideCanRelaxConstraints Tests

    [Fact]
    public void OverrideMergePolicy_CanRelaxConstraintsTrue_SegmentCanIncreaseCap()
    {
        // Arrange
        int baseCap = 8;
        int? segmentCap = 12;
        var policy = CreatePolicy(canRelax: true);

        // Act
        int result = OverrideMergePolicyEnforcer.ResolveEffectiveMaxHitsPerBar(baseCap, segmentCap, policy);

        // Assert: segment can increase beyond base (up to ceiling)
        Assert.Equal(12, result);
    }

    [Fact]
    public void OverrideMergePolicy_CanRelaxConstraintsFalse_SegmentCannotIncreaseCap()
    {
        // Arrange
        int baseCap = 8;
        int? segmentCap = 12;
        var policy = CreatePolicy(canRelax: false);

        // Act
        int result = OverrideMergePolicyEnforcer.ResolveEffectiveMaxHitsPerBar(baseCap, segmentCap, policy);

        // Assert: cannot exceed base when relaxation not allowed
        Assert.Equal(8, result);
    }

    [Fact]
    public void OverrideMergePolicy_CanRelaxConstraintsTrue_StillBoundedByCeiling()
    {
        // Arrange: try to exceed the hard ceiling
        int baseCap = 8;
        int? segmentCap = 100; // Way above MaxHitsPerBarCeiling (32)
        var policy = CreatePolicy(canRelax: true);

        // Act
        int result = OverrideMergePolicyEnforcer.ResolveEffectiveMaxHitsPerBar(baseCap, segmentCap, policy);

        // Assert: clamped to ceiling
        Assert.Equal(OverrideMergePolicyEnforcer.MaxHitsPerBarCeiling, result);
    }

    #endregion

    #region OverrideCanChangeFeel Tests

    [Fact]
    public void OverrideMergePolicy_CanChangeFeelTrue_SegmentFeelApplied()
    {
        // Arrange
        var baseFeel = GrooveFeel.Straight;
        var segmentFeel = GrooveFeel.Swing;
        var policy = CreatePolicy(canChangeFeel: true);

        // Act
        var result = OverrideMergePolicyEnforcer.ResolveEffectiveFeel(baseFeel, segmentFeel, policy);

        // Assert: segment feel takes effect
        Assert.Equal(GrooveFeel.Swing, result);
    }

    [Fact]
    public void OverrideMergePolicy_CanChangeFeelFalse_BaseFeelPreserved()
    {
        // Arrange
        var baseFeel = GrooveFeel.Straight;
        var segmentFeel = GrooveFeel.Swing;
        var policy = CreatePolicy(canChangeFeel: false);

        // Act
        var result = OverrideMergePolicyEnforcer.ResolveEffectiveFeel(baseFeel, segmentFeel, policy);

        // Assert: base feel preserved, segment ignored
        Assert.Equal(GrooveFeel.Straight, result);
    }

    [Fact]
    public void OverrideMergePolicy_CanChangeFeelTrue_SwingAmountApplied()
    {
        // Arrange
        double baseSwing = 0.0;
        double? segmentSwing = 0.66;
        var policy = CreatePolicy(canChangeFeel: true);

        // Act
        double result = OverrideMergePolicyEnforcer.ResolveEffectiveSwingAmount(baseSwing, segmentSwing, policy);

        // Assert: segment swing applied
        Assert.Equal(0.66, result, precision: 3);
    }

    [Fact]
    public void OverrideMergePolicy_CanChangeFeelFalse_BaseSwingPreserved()
    {
        // Arrange
        double baseSwing = 0.5;
        double? segmentSwing = 1.0;
        var policy = CreatePolicy(canChangeFeel: false);

        // Act
        double result = OverrideMergePolicyEnforcer.ResolveEffectiveSwingAmount(baseSwing, segmentSwing, policy);

        // Assert: base swing preserved
        Assert.Equal(0.5, result);
    }

    #endregion

    #region Combined Policy Tests

    [Fact]
    public void OverrideMergePolicy_AllFlagsTrue_CombinedBehavior()
    {
        // Arrange: most permissive configuration
        var policy = CreatePolicy(
            replacesLists: true,
            canRemoveProtected: true,
            canRelax: true,
            canChangeFeel: true);

        // Test list replacement
        var baseTags = new List<string> { "A", "B" };
        var segmentTags = new List<string> { "C" };
        var tags = OverrideMergePolicyEnforcer.ResolveEffectiveVariationTags(baseTags, segmentTags, policy);
        Assert.Single(tags);
        Assert.Equal("C", tags[0]);

        // Test protected removal
        var protectedOnset = CreateOnset(isProtected: true);
        Assert.True(OverrideMergePolicyEnforcer.CanRemoveOnset(protectedOnset, policy));

        // Test cap relaxation
        int cap = OverrideMergePolicyEnforcer.ResolveEffectiveMaxHitsPerBar(8, 16, policy);
        Assert.Equal(16, cap);

        // Test feel change
        var feel = OverrideMergePolicyEnforcer.ResolveEffectiveFeel(GrooveFeel.Straight, GrooveFeel.Shuffle, policy);
        Assert.Equal(GrooveFeel.Shuffle, feel);
    }

    [Fact]
    public void OverrideMergePolicy_AllFlagsFalse_NoOverridesApplied()
    {
        // Arrange: most restrictive configuration (default)
        var policy = CreatePolicy(
            replacesLists: false,
            canRemoveProtected: false,
            canRelax: false,
            canChangeFeel: false);

        // Test list merge (union)
        var baseTags = new List<string> { "A", "B" };
        var segmentTags = new List<string> { "C" };
        var tags = OverrideMergePolicyEnforcer.ResolveEffectiveVariationTags(baseTags, segmentTags, policy);
        Assert.Equal(3, tags.Count);

        // Test protected NOT removable
        var protectedOnset = CreateOnset(isProtected: true);
        Assert.False(OverrideMergePolicyEnforcer.CanRemoveOnset(protectedOnset, policy));

        // Test cap NOT relaxed
        int cap = OverrideMergePolicyEnforcer.ResolveEffectiveMaxHitsPerBar(8, 16, policy);
        Assert.Equal(8, cap);

        // Test feel NOT changed
        var feel = OverrideMergePolicyEnforcer.ResolveEffectiveFeel(GrooveFeel.Straight, GrooveFeel.Shuffle, policy);
        Assert.Equal(GrooveFeel.Straight, feel);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void OverrideMergePolicy_NullSegmentTags_UsesBaseTags()
    {
        // Arrange
        var baseTags = new List<string> { "Fill" };
        var policy = CreatePolicy(replacesLists: true);

        // Act: null segment means use base (even in replace mode)
        var result = OverrideMergePolicyEnforcer.ResolveEffectiveVariationTags(baseTags, null, policy);

        // Assert: empty list when replacing with null
        Assert.Empty(result);
    }

    [Fact]
    public void OverrideMergePolicy_EmptySegmentTags_ReplaceModeClears()
    {
        // Arrange
        var baseTags = new List<string> { "Fill", "Groove" };
        var segmentTags = new List<string>(); // Empty
        var policy = CreatePolicy(replacesLists: true);

        // Act
        var result = OverrideMergePolicyEnforcer.ResolveEffectiveVariationTags(baseTags, segmentTags, policy);

        // Assert: replace with empty = clear all
        Assert.Empty(result);
    }

    [Fact]
    public void OverrideMergePolicy_UnprotectedOnsetAlwaysRemovable()
    {
        // Arrange: onset with no protection flags
        var onset = CreateOnset(isMustHit: false, isNeverRemove: false, isProtected: false);
        var restrictivePolicy = CreatePolicy(canRemoveProtected: false);

        // Act
        bool canRemove = OverrideMergePolicyEnforcer.CanRemoveOnset(onset, restrictivePolicy);

        // Assert: unprotected onsets are always removable
        Assert.True(canRemove);
    }

    [Fact]
    public void OverrideMergePolicy_NullSegmentCap_UsesBaseCap()
    {
        // Arrange
        int baseCap = 10;
        var policy = CreatePolicy(canRelax: true);

        // Act
        int result = OverrideMergePolicyEnforcer.ResolveEffectiveMaxHitsPerBar(baseCap, null, policy);

        // Assert: base cap used when segment is null
        Assert.Equal(10, result);
    }

    [Fact]
    public void OverrideMergePolicy_SegmentCanReduceCap_RegardlessOfRelaxFlag()
    {
        // Arrange: segment cap lower than base
        int baseCap = 16;
        int? segmentCap = 8;
        var policy = CreatePolicy(canRelax: false);

        // Act
        int result = OverrideMergePolicyEnforcer.ResolveEffectiveMaxHitsPerBar(baseCap, segmentCap, policy);

        // Assert: can always reduce (only increasing is restricted)
        Assert.Equal(8, result);
    }

    #endregion
}
