// AI: purpose=Unit tests for PhysicalityFilter Story 4.3.
// AI: invariants=Tests verify deterministic behavior, protected preservation, strictness modes.

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

        var input = new List<GrooveOnsetCandidate>();
        var result = filter.Filter(input, 1);

        result.Should().BeEmpty();
    }

    [Fact]
    public void PhysicalityFilter_SingleCandidate_PassesThrough()
    {
        var rules = PhysicalityRules.Default;
        var filter = new PhysicalityFilter(rules);

        var candidate = CreateCandidate("Snare", 2.0m, 0.5, "Op1", "C1");
        var result = filter.Filter(new List<GrooveOnsetCandidate> { candidate }, 1);

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

        var result = filter.Filter(new List<GrooveOnsetCandidate> { protectedSnare, unprotectedTom }, 1);

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

        var result = filter.Filter(new List<GrooveOnsetCandidate> { p1, p2 }, 1);

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

        var result = filter.Filter(new List<GrooveOnsetCandidate> { snare, tom }, 1);

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

        var result = filter.Filter(new List<GrooveOnsetCandidate> { snare, hat }, 1);

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

        var result = filter.Filter(new List<GrooveOnsetCandidate> { unknown, snare }, 1);

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

        var result = filter.Filter(new List<GrooveOnsetCandidate> { s1, s2 }, 1);

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

        var result = filter.Filter(new List<GrooveOnsetCandidate> { high, low }, 1);

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

        var result = filter.Filter(new List<GrooveOnsetCandidate> { s1, s2 }, 1);

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

        var result = filter.Filter(new List<GrooveOnsetCandidate> { c1, c2 }, 1);

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

        var result = filter.Filter(new List<GrooveOnsetCandidate> { c1, c2, c3 }, 1);

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

        var result = filter.Filter(new List<GrooveOnsetCandidate> { protectedC, unprotectedC }, 1);

        // Protected must remain even with lower score
        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(protectedC);
    }

    #endregion

    #region Helpers

    private static GrooveOnsetCandidate CreateCandidate(
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

        return new GrooveOnsetCandidate
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
