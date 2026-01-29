// AI: purpose=Unit tests for PhysicalityFilter Stories 4.3 and 4.4.
// AI: invariants=Tests verify deterministic behavior, protected preservation, strictness modes, overcrowding prevention.

using FluentAssertions;
using Music.Generator.Agents.Drums;
using Music.Generator.Agents.Drums.Physicality;
using Music.Generator.Groove;
using Xunit;

namespace Music.Tests.Generator.Agents.Drums.Physicality;

public class PhysicalityFilterTests
{
    #region Empty and Basic Cases

    [Fact]
    public void PhysicalityFilter_EmptyCandidates_ReturnsEmptyNoDiagnostics()
    {
        var rules = PhysicalityRules.Default;
        var collector = new GrooveDiagnosticsCollector(1, "Snare");
        var filter = new PhysicalityFilter(rules, collector);

        var input = new List<DrumOnsetCandidate>();
        var result = filter.Filter(input, 1);

        result.Should().BeEmpty();
    }

    [Fact]
    public void PhysicalityFilter_SingleCandidate_PassesThrough()
    {
        var rules = PhysicalityRules.Default;
        var filter = new PhysicalityFilter(rules);

        var candidate = CreateCandidate("Snare", 2.0m, 0.5, "Op1", "C1");
        var result = filter.Filter(new List<DrumOnsetCandidate> { candidate }, 1);

        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(candidate);
    }

    #endregion

    #region Protected Onsets

    [Fact]
    public void PhysicalityFilter_PreservesProtectedOnsets_EvenWhenConflicting()
    {
        var rules = PhysicalityRules.Default with { StrictnessLevel = PhysicalityStrictness.Normal };
        var filter = new PhysicalityFilter(rules);

        // Snare (protected) + Tom1 (unprotected) at same beat - both map to LeftHand
        var protectedSnare = CreateCandidate("Snare", 2.0m, 0.6, "Op1", "C1", isProtected: true);
        var unprotectedTom = CreateCandidate("Tom1", 2.0m, 0.4, "Op2", "C2", isProtected: false);

        var result = filter.Filter(new List<DrumOnsetCandidate> { protectedSnare, unprotectedTom }, 1);

        // Protected must remain; unprotected conflicting should be removed
        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(protectedSnare);
    }

    [Fact]
    public void PhysicalityFilter_MultipleProtected_AllPreserved()
    {
        var rules = PhysicalityRules.Default with { StrictnessLevel = PhysicalityStrictness.Strict };
        var filter = new PhysicalityFilter(rules);

        var p1 = CreateCandidate("Snare", 2.0m, 0.6, "Op1", "C1", isProtected: true);
        var p2 = CreateCandidate("Kick", 1.0m, 0.8, "Op2", "C2", isProtected: true);

        var result = filter.Filter(new List<DrumOnsetCandidate> { p1, p2 }, 1);

        result.Should().HaveCount(2);
    }

    #endregion

    #region Limb Conflict Resolution

    [Fact]
    public void PhysicalityFilter_RemovesLimbConflicts_BasedOnLimbModel()
    {
        var rules = PhysicalityRules.Default with { StrictnessLevel = PhysicalityStrictness.Normal };
        var filter = new PhysicalityFilter(rules);

        // Snare + Tom1 at same beat - both LeftHand, neither protected
        var snare = CreateCandidate("Snare", 2.0m, 0.7, "OpSnare", "CS");
        var tom = CreateCandidate("Tom1", 2.0m, 0.3, "OpTom", "CT");

        var result = filter.Filter(new List<DrumOnsetCandidate> { snare, tom }, 1);

        // Higher-scored (snare) should be kept
        result.Should().ContainSingle()
            .Which.Role.Should().Be("Snare");
    }

    [Fact]
    public void PhysicalityFilter_AllowsDifferentLimbs_AtSameBeat()
    {
        var rules = PhysicalityRules.Default;
        var filter = new PhysicalityFilter(rules);

        // Snare (LeftHand) + ClosedHat (RightHand) at same beat - no conflict
        var snare = CreateCandidate("Snare", 2.0m, 0.5, "Op1", "C1");
        var hat = CreateCandidate("ClosedHat", 2.0m, 0.5, "Op2", "C2");

        var result = filter.Filter(new List<DrumOnsetCandidate> { snare, hat }, 1);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void PhysicalityFilter_SkipsUnknownRoleMappings_NoConflictRaised()
    {
        var rules = PhysicalityRules.Default;
        var filter = new PhysicalityFilter(rules);

        // UnknownRole has no limb mapping - should pass through
        var unknown = CreateCandidate("UnknownRole", 2.0m, 0.5, "Op1", "C1");
        var snare = CreateCandidate("Snare", 2.0m, 0.5, "Op2", "C2");

        var result = filter.Filter(new List<DrumOnsetCandidate> { unknown, snare }, 1);

        // Both should pass (unknown doesn't participate in limb conflicts)
        result.Should().HaveCount(2);
    }

    #endregion

    #region Strictness Modes

    [Fact]
    public void PhysicalityFilter_StrictMode_RemovesAllViolators()
    {
        var rules = PhysicalityRules.Default with { StrictnessLevel = PhysicalityStrictness.Strict };
        var filter = new PhysicalityFilter(rules);

        // Two snares at same beat - both LeftHand, neither protected
        var s1 = CreateCandidate("Snare", 2.0m, 0.6, "Op1", "C1");
        var s2 = CreateCandidate("Tom1", 2.0m, 0.4, "Op2", "C2");

        var result = filter.Filter(new List<DrumOnsetCandidate> { s1, s2 }, 1);

        // Strict mode removes ALL non-protected violators
        result.Should().BeEmpty();
    }

    [Fact]
    public void PhysicalityFilter_NormalMode_KeepsHighestScored()
    {
        var rules = PhysicalityRules.Default with { StrictnessLevel = PhysicalityStrictness.Normal };
        var filter = new PhysicalityFilter(rules);

        var high = CreateCandidate("Snare", 2.0m, 0.9, "OpHigh", "CH");
        var low = CreateCandidate("Tom1", 2.0m, 0.2, "OpLow", "CL");

        var result = filter.Filter(new List<DrumOnsetCandidate> { high, low }, 1);

        result.Should().ContainSingle()
            .Which.ProbabilityBias.Should().Be(0.9);
    }

    [Fact]
    public void PhysicalityFilter_LooseMode_LogsButKeepsAll()
    {
        var collector = new GrooveDiagnosticsCollector(1, "Snare");
        var rules = PhysicalityRules.Default with { StrictnessLevel = PhysicalityStrictness.Loose };
        var filter = new PhysicalityFilter(rules, collector);

        var s1 = CreateCandidate("Snare", 2.0m, 0.6, "Op1", "C1");
        var s2 = CreateCandidate("Tom1", 2.0m, 0.4, "Op2", "C2");

        var result = filter.Filter(new List<DrumOnsetCandidate> { s1, s2 }, 1);

        // Both kept in Loose mode
        result.Should().HaveCount(2);

        // Diagnostics should record filter decisions
        var diag = collector.Build();
        diag.FiltersApplied.Should().NotBeEmpty();
    }

    #endregion

    #region Deterministic Tie-Break

    [Fact]
    public void PhysicalityFilter_DeterministicPrune_TieBreakByScoreThenOperatorId()
    {
        var rules = PhysicalityRules.Default with { StrictnessLevel = PhysicalityStrictness.Normal };
        var filter = new PhysicalityFilter(rules);

        // Same score, different operator IDs
        var c1 = CreateCandidate("Snare", 2.0m, 0.5, "OpA", "C1");
        var c2 = CreateCandidate("Tom1", 2.0m, 0.5, "OpB", "C2");

        var result = filter.Filter(new List<DrumOnsetCandidate> { c1, c2 }, 1);

        // OpA < OpB alphabetically, so c1 should be kept
        result.Should().ContainSingle();
        var kept = result[0];
        DrumCandidateMapper.ExtractOperatorId(kept).Should().Be("OpA");
    }

    #endregion

    #region Overcrowding Prevention

    [Fact]
    public void PhysicalityFilter_OvercrowdingPrevention_PrunesLowestScored()
    {
        var rules = PhysicalityRules.Default with { MaxHitsPerBar = 2 };
        var filter = new PhysicalityFilter(rules);

        var c1 = CreateCandidate("Kick", 1.0m, 0.9, "Op1", "C1");
        var c2 = CreateCandidate("Snare", 2.0m, 0.5, "Op2", "C2");
        var c3 = CreateCandidate("ClosedHat", 1.5m, 0.3, "Op3", "C3");

        var result = filter.Filter(new List<DrumOnsetCandidate> { c1, c2, c3 }, 1);

        // MaxHitsPerBar=2, so lowest scored (c3) should be pruned
        result.Should().HaveCount(2);
        result.Select(c => c.ProbabilityBias).Should().Contain(0.9).And.Contain(0.5);
    }

    [Fact]
    public void PhysicalityFilter_OvercrowdingPrevention_ProtectedAlwaysKept()
    {
        var rules = PhysicalityRules.Default with { MaxHitsPerBar = 1 };
        var filter = new PhysicalityFilter(rules);

        var protectedC = CreateCandidate("Kick", 1.0m, 0.3, "Op1", "C1", isProtected: true);
        var unprotectedC = CreateCandidate("Snare", 2.0m, 0.9, "Op2", "C2");

        var result = filter.Filter(new List<DrumOnsetCandidate> { protectedC, unprotectedC }, 1);

        // Protected must remain even with lower score
        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(protectedC);
    }

    #endregion

    #region Story 4.4 - MaxHitsPerBeat

    [Fact]
    public void PhysicalityFilter_MaxHitsPerBeat_EnforcedPerBeat()
    {
        var rules = PhysicalityRules.Default with
        {
            MaxHitsPerBeat = 2,
            MaxHitsPerBar = null // Disable bar cap to isolate beat cap
        };
        var filter = new PhysicalityFilter(rules);

        // 3 candidates on beat 1.0
        var c1 = CreateCandidate("Kick", 1.0m, 0.9, "Op1", "C1");
        var c2 = CreateCandidate("Snare", 1.0m, 0.5, "Op2", "C2");
        var c3 = CreateCandidate("ClosedHat", 1.0m, 0.3, "Op3", "C3");
        // 1 candidate on beat 2.0 (should be unaffected)
        var c4 = CreateCandidate("Snare", 2.0m, 0.8, "Op4", "C4");

        var result = filter.Filter(new List<DrumOnsetCandidate> { c1, c2, c3, c4 }, 1);

        // Beat 1.0: 2 kept (highest scored), beat 2.0: 1 kept = 3 total
        result.Should().HaveCount(3);
        // Lowest scored on beat 1.0 (c3) should be pruned
        result.Should().NotContain(c => c.ProbabilityBias == 0.3);
    }

    [Fact]
    public void PhysicalityFilter_MaxHitsPerBeat_ProtectedPreserved()
    {
        var rules = PhysicalityRules.Default with
        {
            MaxHitsPerBeat = 1,
            MaxHitsPerBar = null
        };
        var filter = new PhysicalityFilter(rules);

        // 2 protected + 1 unprotected on same beat
        var p1 = CreateCandidate("Kick", 1.0m, 0.3, "Op1", "C1", isProtected: true);
        var p2 = CreateCandidate("Snare", 1.0m, 0.4, "Op2", "C2", isProtected: true);
        var u1 = CreateCandidate("ClosedHat", 1.0m, 0.9, "Op3", "C3");

        var result = filter.Filter(new List<DrumOnsetCandidate> { p1, p2, u1 }, 1);

        // Both protected kept; unprotected pruned (cap exceeded by protected alone)
        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => DrumCandidateMapper.IsProtected(c));
    }

    [Fact]
    public void PhysicalityFilter_MaxHitsPerBeat_DifferentBeatsIndependent()
    {
        var rules = PhysicalityRules.Default with
        {
            MaxHitsPerBeat = 1,
            MaxHitsPerBar = null
        };
        var filter = new PhysicalityFilter(rules);

        // 1 candidate per beat across 4 beats
        var c1 = CreateCandidate("Kick", 1.0m, 0.5, "Op1", "C1");
        var c2 = CreateCandidate("ClosedHat", 1.5m, 0.5, "Op2", "C2");
        var c3 = CreateCandidate("Snare", 2.0m, 0.5, "Op3", "C3");
        var c4 = CreateCandidate("ClosedHat", 2.5m, 0.5, "Op4", "C4");

        var result = filter.Filter(new List<DrumOnsetCandidate> { c1, c2, c3, c4 }, 1);

        // All under per-beat cap, all kept
        result.Should().HaveCount(4);
    }

    [Fact]
    public void PhysicalityFilter_MaxHitsPerBeat_DeterministicTieBreak()
    {
        var rules = PhysicalityRules.Default with
        {
            MaxHitsPerBeat = 1,
            MaxHitsPerBar = null
        };
        var filter = new PhysicalityFilter(rules);

        // Same score, different operator IDs on same beat
        var c1 = CreateCandidate("Kick", 1.0m, 0.5, "OpB", "C1");
        var c2 = CreateCandidate("Snare", 1.0m, 0.5, "OpA", "C2"); // OpA < OpB

        var result = filter.Filter(new List<DrumOnsetCandidate> { c1, c2 }, 1);

        result.Should().ContainSingle();
        // OpA wins tie-break
        DrumCandidateMapper.ExtractOperatorId(result[0]).Should().Be("OpA");
    }

    #endregion

    #region Story 4.4 - MaxHitsPerRolePerBar

    [Fact]
    public void PhysicalityFilter_MaxHitsPerRole_EnforcedPerRole()
    {
        var roleCaps = new Dictionary<string, int>
        {
            ["Kick"] = 2,
            ["Snare"] = 1
        };
        var rules = PhysicalityRules.Default with
        {
            MaxHitsPerRolePerBar = roleCaps,
            MaxHitsPerBar = null,
            MaxHitsPerBeat = null
        };
        var filter = new PhysicalityFilter(rules);

        // 3 kicks, 2 snares
        var k1 = CreateCandidate("Kick", 1.0m, 0.9, "Op1", "K1");
        var k2 = CreateCandidate("Kick", 2.0m, 0.7, "Op2", "K2");
        var k3 = CreateCandidate("Kick", 3.0m, 0.3, "Op3", "K3");
        var s1 = CreateCandidate("Snare", 2.0m, 0.8, "Op4", "S1");
        var s2 = CreateCandidate("Snare", 4.0m, 0.5, "Op5", "S2");

        var result = filter.Filter(new List<DrumOnsetCandidate> { k1, k2, k3, s1, s2 }, 1);

        // Kicks: 2 kept (k1, k2 - highest scored), Snares: 1 kept (s1 - highest)
        result.Should().HaveCount(3);
        result.Where(c => c.Role == "Kick").Should().HaveCount(2);
        result.Where(c => c.Role == "Snare").Should().HaveCount(1);
        result.Should().NotContain(c => DrumCandidateMapper.ExtractCandidateId(c) == "K3");
        result.Should().NotContain(c => DrumCandidateMapper.ExtractCandidateId(c) == "S2");
    }

    [Fact]
    public void PhysicalityFilter_MaxHitsPerRole_UncappedRolesUnaffected()
    {
        var roleCaps = new Dictionary<string, int>
        {
            ["Kick"] = 1
        };
        var rules = PhysicalityRules.Default with
        {
            MaxHitsPerRolePerBar = roleCaps,
            MaxHitsPerBar = null,
            MaxHitsPerBeat = null
        };
        var filter = new PhysicalityFilter(rules);

        // 2 kicks (capped), 5 hats (no cap)
        var k1 = CreateCandidate("Kick", 1.0m, 0.9, "Op1", "K1");
        var k2 = CreateCandidate("Kick", 3.0m, 0.3, "Op2", "K2");
        var h1 = CreateCandidate("ClosedHat", 1.0m, 0.5, "Op3", "H1");
        var h2 = CreateCandidate("ClosedHat", 1.5m, 0.5, "Op4", "H2");
        var h3 = CreateCandidate("ClosedHat", 2.0m, 0.5, "Op5", "H3");
        var h4 = CreateCandidate("ClosedHat", 2.5m, 0.5, "Op6", "H4");
        var h5 = CreateCandidate("ClosedHat", 3.0m, 0.5, "Op7", "H5");

        var result = filter.Filter(new List<DrumOnsetCandidate> { k1, k2, h1, h2, h3, h4, h5 }, 1);

        // Kicks: 1 kept, Hats: all 5 kept
        result.Should().HaveCount(6);
        result.Where(c => c.Role == "Kick").Should().ContainSingle();
        result.Where(c => c.Role == "ClosedHat").Should().HaveCount(5);
    }

    [Fact]
    public void PhysicalityFilter_MaxHitsPerRole_ProtectedPreserved()
    {
        var roleCaps = new Dictionary<string, int>
        {
            ["Kick"] = 1
        };
        var rules = PhysicalityRules.Default with
        {
            MaxHitsPerRolePerBar = roleCaps,
            MaxHitsPerBar = null,
            MaxHitsPerBeat = null
        };
        var filter = new PhysicalityFilter(rules);

        // 2 protected kicks + 1 unprotected kick
        var k1 = CreateCandidate("Kick", 1.0m, 0.3, "Op1", "K1", isProtected: true);
        var k2 = CreateCandidate("Kick", 3.0m, 0.4, "Op2", "K2", isProtected: true);
        var k3 = CreateCandidate("Kick", 4.0m, 0.9, "Op3", "K3");

        var result = filter.Filter(new List<DrumOnsetCandidate> { k1, k2, k3 }, 1);

        // Both protected kept, unprotected pruned
        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => DrumCandidateMapper.IsProtected(c));
    }

    [Fact]
    public void PhysicalityFilter_MaxHitsPerRole_EmptyDictionary_NoRoleCaps()
    {
        var rules = PhysicalityRules.Default with
        {
            MaxHitsPerRolePerBar = new Dictionary<string, int>(),
            MaxHitsPerBar = null,
            MaxHitsPerBeat = null
        };
        var filter = new PhysicalityFilter(rules);

        // Many kicks - should all pass with empty role caps
        var candidates = Enumerable.Range(1, 10)
            .Select(i => CreateCandidate("Kick", i * 0.5m, 0.5, $"Op{i}", $"C{i}"))
            .ToList();

        var result = filter.Filter(candidates, 1);

        result.Should().HaveCount(10);
    }

    #endregion

    #region Story 4.4 - Combined Caps

    [Fact]
    public void PhysicalityFilter_CombinedCaps_AllThreeLevelsEnforced()
    {
        var roleCaps = new Dictionary<string, int>
        {
            ["Kick"] = 2,
            ["Snare"] = 2,
            ["ClosedHat"] = 4
        };
        var rules = PhysicalityRules.Default with
        {
            MaxHitsPerBeat = 2,
            MaxHitsPerBar = 6,
            MaxHitsPerRolePerBar = roleCaps
        };
        var filter = new PhysicalityFilter(rules);

        // Create candidates that will hit multiple caps
        var k1 = CreateCandidate("Kick", 1.0m, 0.9, "Op1", "K1");
        var k2 = CreateCandidate("Kick", 2.0m, 0.8, "Op2", "K2");
        var k3 = CreateCandidate("Kick", 3.0m, 0.2, "Op3", "K3"); // Role cap: will be pruned
        var s1 = CreateCandidate("Snare", 2.0m, 0.85, "Op4", "S1");
        var s2 = CreateCandidate("Snare", 4.0m, 0.7, "Op5", "S2");
        var h1 = CreateCandidate("ClosedHat", 1.0m, 0.6, "Op6", "H1");
        var h2 = CreateCandidate("ClosedHat", 1.5m, 0.5, "Op7", "H2");
        var h3 = CreateCandidate("ClosedHat", 2.0m, 0.4, "Op8", "H3");
        var h4 = CreateCandidate("ClosedHat", 2.5m, 0.3, "Op9", "H4");

        var result = filter.Filter(new List<DrumOnsetCandidate>
            { k1, k2, k3, s1, s2, h1, h2, h3, h4 }, 1);

        // Role cap prunes k3 (3rd kick)
        // Bar cap enforces max 6 total (2 kicks + 2 snares + up to 2 hats)
        result.Should().HaveCountLessThanOrEqualTo(6);
        result.Where(c => c.Role == "Kick").Should().HaveCountLessThanOrEqualTo(2);
        result.Where(c => c.Role == "Snare").Should().HaveCountLessThanOrEqualTo(2);
    }

    [Fact]
    public void PhysicalityFilter_CombinedCaps_RoleCapsAppliedFirst()
    {
        // Test that role caps run before bar caps
        var roleCaps = new Dictionary<string, int>
        {
            ["Kick"] = 1
        };
        var rules = PhysicalityRules.Default with
        {
            MaxHitsPerRolePerBar = roleCaps,
            MaxHitsPerBar = 10, // High bar cap
            MaxHitsPerBeat = null
        };
        var filter = new PhysicalityFilter(rules);

        var k1 = CreateCandidate("Kick", 1.0m, 0.9, "Op1", "K1");
        var k2 = CreateCandidate("Kick", 2.0m, 0.3, "Op2", "K2"); // Will be pruned by role cap
        var s1 = CreateCandidate("Snare", 2.0m, 0.8, "Op3", "S1");

        var result = filter.Filter(new List<DrumOnsetCandidate> { k1, k2, s1 }, 1);

        // K2 pruned by role cap, rest kept (under bar cap)
        result.Should().HaveCount(2);
        result.Should().Contain(c => DrumCandidateMapper.ExtractCandidateId(c) == "K1");
        result.Should().Contain(c => DrumCandidateMapper.ExtractCandidateId(c) == "S1");
    }

    [Fact]
    public void PhysicalityFilter_CombinedCaps_BeatCapsAppliedAfterRoleCaps()
    {
        var roleCaps = new Dictionary<string, int>
        {
            ["Kick"] = 3 // Higher than beat cap
        };
        var rules = PhysicalityRules.Default with
        {
            MaxHitsPerRolePerBar = roleCaps,
            MaxHitsPerBeat = 1, // Very restrictive
            MaxHitsPerBar = null
        };
        var filter = new PhysicalityFilter(rules);

        // 3 kicks on same beat - role cap allows 3, but beat cap allows 1
        var k1 = CreateCandidate("Kick", 1.0m, 0.9, "Op1", "K1");
        var k2 = CreateCandidate("Kick", 1.0m, 0.5, "Op2", "K2");
        var k3 = CreateCandidate("Kick", 1.0m, 0.3, "Op3", "K3");

        var result = filter.Filter(new List<DrumOnsetCandidate> { k1, k2, k3 }, 1);

        // Role cap passes all 3, but beat cap prunes to 1
        result.Should().ContainSingle();
        DrumCandidateMapper.ExtractCandidateId(result[0]).Should().Be("K1");
    }

    #endregion

    #region Story 4.4 - Diagnostics

    [Fact]
    public void PhysicalityFilter_Overcrowding_RecordsDiagnostics()
    {
        var collector = new GrooveDiagnosticsCollector(1, "Kick");
        var rules = PhysicalityRules.Default with { MaxHitsPerBar = 1 };
        var filter = new PhysicalityFilter(rules, collector);

        var c1 = CreateCandidate("Kick", 1.0m, 0.9, "Op1", "C1");
        var c2 = CreateCandidate("Kick", 2.0m, 0.3, "Op2", "C2");

        filter.Filter(new List<DrumOnsetCandidate> { c1, c2 }, 1);

        var diag = collector.Build();
        diag.PruneEvents.Should().ContainSingle();
        diag.PruneEvents[0].Reason.Should().Contain("Overcrowding");
        diag.PruneEvents[0].OnsetId.Should().Be("C2");
    }

    [Fact]
    public void PhysicalityFilter_MaxHitsPerBeat_RecordsDiagnosticsWithReason()
    {
        var collector = new GrooveDiagnosticsCollector(1, "Kick");
        var rules = PhysicalityRules.Default with
        {
            MaxHitsPerBeat = 1,
            MaxHitsPerBar = null
        };
        var filter = new PhysicalityFilter(rules, collector);

        var c1 = CreateCandidate("Kick", 1.0m, 0.9, "Op1", "C1");
        var c2 = CreateCandidate("Snare", 1.0m, 0.3, "Op2", "C2");

        filter.Filter(new List<DrumOnsetCandidate> { c1, c2 }, 1);

        var diag = collector.Build();
        diag.PruneEvents.Should().ContainSingle();
        diag.PruneEvents[0].Reason.Should().Contain("MaxHitsPerBeat");
    }

    [Fact]
    public void PhysicalityFilter_MaxHitsPerRole_RecordsDiagnosticsWithRole()
    {
        var collector = new GrooveDiagnosticsCollector(1, "Kick");
        var roleCaps = new Dictionary<string, int> { ["Kick"] = 1 };
        var rules = PhysicalityRules.Default with
        {
            MaxHitsPerRolePerBar = roleCaps,
            MaxHitsPerBar = null,
            MaxHitsPerBeat = null
        };
        var filter = new PhysicalityFilter(rules, collector);

        var k1 = CreateCandidate("Kick", 1.0m, 0.9, "Op1", "K1");
        var k2 = CreateCandidate("Kick", 2.0m, 0.3, "Op2", "K2");

        filter.Filter(new List<DrumOnsetCandidate> { k1, k2 }, 1);

        var diag = collector.Build();
        diag.PruneEvents.Should().ContainSingle();
        diag.PruneEvents[0].Reason.Should().Contain("MaxHitsPerRole:Kick");
    }

    #endregion

    #region Story 4.4 - Determinism

    [Fact]
    public void PhysicalityFilter_Overcrowding_DeterministicAcrossRuns()
    {
        var rules = PhysicalityRules.Default with { MaxHitsPerBar = 3 };

        // Run twice with same input
        var candidates = new List<DrumOnsetCandidate>
        {
            CreateCandidate("Kick", 1.0m, 0.5, "Op1", "C1"),
            CreateCandidate("Snare", 2.0m, 0.5, "Op2", "C2"),
            CreateCandidate("ClosedHat", 1.5m, 0.5, "Op3", "C3"),
            CreateCandidate("ClosedHat", 2.5m, 0.5, "Op4", "C4"),
            CreateCandidate("ClosedHat", 3.5m, 0.5, "Op5", "C5")
        };

        var filter1 = new PhysicalityFilter(rules);
        var filter2 = new PhysicalityFilter(rules);

        var result1 = filter1.Filter(candidates, 1);
        var result2 = filter2.Filter(candidates, 1);

        // Same output
        result1.Should().HaveCount(result2.Count);
        var ids1 = result1.Select(c => DrumCandidateMapper.ExtractCandidateId(c)).ToList();
        var ids2 = result2.Select(c => DrumCandidateMapper.ExtractCandidateId(c)).ToList();
        ids1.Should().BeEquivalentTo(ids2);
    }

    [Fact]
    public void PhysicalityFilter_Overcrowding_DeterministicWithShuffledInput()
    {
        var rules = PhysicalityRules.Default with { MaxHitsPerBar = 2 };

        var c1 = CreateCandidate("Kick", 1.0m, 0.9, "Op1", "C1");
        var c2 = CreateCandidate("Snare", 2.0m, 0.5, "Op2", "C2");
        var c3 = CreateCandidate("ClosedHat", 1.5m, 0.3, "Op3", "C3");

        var filter = new PhysicalityFilter(rules);

        // Different input orders
        var result1 = filter.Filter(new List<DrumOnsetCandidate> { c1, c2, c3 }, 1);
        var result2 = filter.Filter(new List<DrumOnsetCandidate> { c3, c1, c2 }, 1);
        var result3 = filter.Filter(new List<DrumOnsetCandidate> { c2, c3, c1 }, 1);

        // Same candidates kept regardless of input order
        var ids1 = result1.Select(c => DrumCandidateMapper.ExtractCandidateId(c)).OrderBy(x => x).ToList();
        var ids2 = result2.Select(c => DrumCandidateMapper.ExtractCandidateId(c)).OrderBy(x => x).ToList();
        var ids3 = result3.Select(c => DrumCandidateMapper.ExtractCandidateId(c)).OrderBy(x => x).ToList();

        ids1.Should().BeEquivalentTo(ids2);
        ids2.Should().BeEquivalentTo(ids3);
    }

    #endregion

    #region Helpers

    private static DrumOnsetCandidate CreateCandidate(
        string role,
        decimal beat,
        double score,
        string operatorId,
        string candidateId,
        bool isProtected = false)
    {
        var tags = new List<string>
        {
            $"{DrumCandidateMapper.CandidateIdTagPrefix}{candidateId}",
            $"{DrumCandidateMapper.OperatorIdTagPrefix}{operatorId}"
        };

        if (isProtected)
        {
            tags.Add(DrumCandidateMapper.ProtectedTag);
        }

        return new DrumOnsetCandidate
        {
            Role = role,
            OnsetBeat = beat,
            Strength = OnsetStrength.Strong,
            ProbabilityBias = score,
            Tags = tags
        };
    }

    #endregion
}

