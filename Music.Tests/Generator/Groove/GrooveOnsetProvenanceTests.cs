// AI: purpose=Unit tests for Story G2 provenance tracking (GrooveOnsetProvenance, GrooveOnsetFactory).
// AI: deps=xunit for test framework; Music.Generator for types under test.
// AI: change=Story G2 acceptance criteria: verify provenance fields, stability, and preservation.

using Music.Generator.Agents.Drums;
using Music.Generator.Groove;
using Xunit;

namespace Music.Generator.Tests;

/// <summary>
/// Story G2: Tests for provenance tracking on GrooveOnset.
/// Verifies GrooveOnsetProvenance factory methods and GrooveOnsetFactory.
/// </summary>
public class GrooveOnsetProvenanceTests
{
    #region GrooveOnsetProvenance Factory Tests

    [Fact]
    public void ForAnchor_HasSourceAnchor()
    {
        // Act
        var provenance = GrooveOnsetProvenance.ForAnchor();

        // Assert
        Assert.Equal(GrooveOnsetSource.Anchor, provenance.Source);
    }

    [Fact]
    public void ForAnchor_HasNullGroupId()
    {
        // Act
        var provenance = GrooveOnsetProvenance.ForAnchor();

        // Assert
        Assert.Null(provenance.GroupId);
    }

    [Fact]
    public void ForAnchor_HasNullCandidateId()
    {
        // Act
        var provenance = GrooveOnsetProvenance.ForAnchor();

        // Assert
        Assert.Null(provenance.CandidateId);
    }

    [Fact]
    public void ForAnchor_HasNullTagsSnapshot()
    {
        // Act
        var provenance = GrooveOnsetProvenance.ForAnchor();

        // Assert
        Assert.Null(provenance.TagsSnapshot);
    }

    [Fact]
    public void ForVariation_HasSourceVariation()
    {
        // Act
        var provenance = GrooveOnsetProvenance.ForVariation("TestGroup", "TestGroup:1.00");

        // Assert
        Assert.Equal(GrooveOnsetSource.Variation, provenance.Source);
    }

    [Fact]
    public void ForVariation_HasGroupId()
    {
        // Arrange
        const string expectedGroupId = "GhostSnare";

        // Act
        var provenance = GrooveOnsetProvenance.ForVariation(expectedGroupId, "GhostSnare:1.50");

        // Assert
        Assert.Equal(expectedGroupId, provenance.GroupId);
    }

    [Fact]
    public void ForVariation_HasCandidateId()
    {
        // Arrange
        const string expectedCandidateId = "GhostSnare:3.50";

        // Act
        var provenance = GrooveOnsetProvenance.ForVariation("GhostSnare", expectedCandidateId);

        // Assert
        Assert.Equal(expectedCandidateId, provenance.CandidateId);
    }

    [Fact]
    public void ForVariation_WithTags_HasTagsSnapshot()
    {
        // Arrange
        var expectedTags = new List<string> { "Fill", "Chorus" };

        // Act
        var provenance = GrooveOnsetProvenance.ForVariation("TestGroup", "TestGroup:2.00", expectedTags);

        // Assert
        Assert.NotNull(provenance.TagsSnapshot);
        Assert.Equal(expectedTags, provenance.TagsSnapshot);
    }

    [Fact]
    public void ForVariation_WithNullTags_HasNullTagsSnapshot()
    {
        // Act
        var provenance = GrooveOnsetProvenance.ForVariation("TestGroup", "TestGroup:2.00", null);

        // Assert
        Assert.Null(provenance.TagsSnapshot);
    }

    [Fact]
    public void ForVariation_WithEmptyTags_HasEmptyTagsSnapshot()
    {
        // Arrange
        var emptyTags = new List<string>();

        // Act
        var provenance = GrooveOnsetProvenance.ForVariation("TestGroup", "TestGroup:2.00", emptyTags);

        // Assert
        Assert.NotNull(provenance.TagsSnapshot);
        Assert.Empty(provenance.TagsSnapshot);
    }

    [Fact]
    public void MakeCandidateId_FormatsCorrectly()
    {
        // Arrange & Act
        string candidateId = GrooveOnsetProvenance.MakeCandidateId("TestGroup", 1.50m);

        // Assert
        Assert.Equal("TestGroup:1.50", candidateId);
    }

    [Fact]
    public void MakeCandidateId_HandlesWholeBeats()
    {
        // Act
        string candidateId = GrooveOnsetProvenance.MakeCandidateId("Kick", 2.00m);

        // Assert
        Assert.Equal("Kick:2.00", candidateId);
    }

    #endregion

    #region GrooveOnsetFactory Tests

    [Fact]
    public void FromAnchor_CreatesOnsetWithAnchorProvenance()
    {
        // Act
        var onset = GrooveOnsetFactory.FromAnchor("Kick", barNumber: 1, beat: 1.0m);

        // Assert
        Assert.NotNull(onset.Provenance);
        Assert.Equal(GrooveOnsetSource.Anchor, onset.Provenance.Source);
        Assert.Null(onset.Provenance.GroupId);
        Assert.Null(onset.Provenance.CandidateId);
    }

    [Fact]
    public void FromAnchor_SetsRoleAndPosition()
    {
        // Act
        var onset = GrooveOnsetFactory.FromAnchor("Snare", barNumber: 5, beat: 2.0m);

        // Assert
        Assert.Equal("Snare", onset.Role);
        Assert.Equal(5, onset.BarNumber);
        Assert.Equal(2.0m, onset.Beat);
    }

    [Fact]
    public void FromAnchor_SetsProtectionFlags()
    {
        // Act
        var onset = GrooveOnsetFactory.FromAnchor(
            "Kick", barNumber: 1, beat: 1.0m,
            isMustHit: true, isNeverRemove: true, isProtected: false);

        // Assert
        Assert.True(onset.IsMustHit);
        Assert.True(onset.IsNeverRemove);
        Assert.False(onset.IsProtected);
    }

    [Fact]
    public void FromVariation_CreatesOnsetWithVariationProvenance()
    {
        // Arrange
        var candidate = new DrumOnsetCandidate
        {
            Role = "Snare",
            OnsetBeat = 1.5m,
            Strength = OnsetStrength.Ghost,
            ProbabilityBias = 0.5
        };
        var group = new DrumCandidateGroup
        {
            GroupId = "GhostSnare",
            BaseProbabilityBias = 1.0,
            Candidates = new List<DrumOnsetCandidate> { candidate }
        };

        // Act
        var onset = GrooveOnsetFactory.FromVariation(candidate, group, barNumber: 3);

        // Assert
        Assert.NotNull(onset.Provenance);
        Assert.Equal(GrooveOnsetSource.Variation, onset.Provenance.Source);
        Assert.Equal("GhostSnare", onset.Provenance.GroupId);
        Assert.Equal("GhostSnare:1.50", onset.Provenance.CandidateId);
    }

    [Fact]
    public void FromVariation_CopiesCandidateProperties()
    {
        // Arrange
        var candidate = new DrumOnsetCandidate
        {
            Role = "ClosedHat",
            OnsetBeat = 2.5m,
            Strength = OnsetStrength.Offbeat,
            ProbabilityBias = 0.8
        };
        var group = new DrumCandidateGroup
        {
            GroupId = "HatGhost",
            BaseProbabilityBias = 1.0,
            Candidates = new List<DrumOnsetCandidate> { candidate }
        };

        // Act
        var onset = GrooveOnsetFactory.FromVariation(candidate, group, barNumber: 2);

        // Assert
        Assert.Equal("ClosedHat", onset.Role);
        Assert.Equal(2.5m, onset.Beat);
        Assert.Equal(OnsetStrength.Offbeat, onset.Strength);
        Assert.Equal(2, onset.BarNumber);
    }

    [Fact]
    public void FromVariation_IncludesEnabledTags()
    {
        // Arrange
        var candidate = new DrumOnsetCandidate
        {
            Role = "Snare",
            OnsetBeat = 4.75m,
            Strength = OnsetStrength.Pickup,
            ProbabilityBias = 0.3
        };
        var group = new DrumCandidateGroup
        {
            GroupId = "Pickups",
            BaseProbabilityBias = 0.6,
            Candidates = new List<DrumOnsetCandidate> { candidate }
        };
        var enabledTags = new List<string> { "Fill", "Chorus" };

        // Act
        var onset = GrooveOnsetFactory.FromVariation(candidate, group, barNumber: 8, enabledTags);

        // Assert
        Assert.NotNull(onset.Provenance!.TagsSnapshot);
        Assert.Equal(enabledTags, onset.Provenance.TagsSnapshot);
    }

    [Fact]
    public void FromWeightedCandidate_CreatesOnsetWithProvenance()
    {
        // Arrange
        var candidate = new DrumOnsetCandidate
        {
            Role = "Kick",
            OnsetBeat = 4.75m,
            Strength = OnsetStrength.Pickup,
            ProbabilityBias = 0.4
        };
        var group = new DrumCandidateGroup
        {
            GroupId = "KickPickup",
            BaseProbabilityBias = 0.6,
            Candidates = new List<DrumOnsetCandidate> { candidate }
        };
        var weighted = new WeightedCandidate(candidate, group, ComputedWeight: 0.24, StableId: "KickPickup:4.7500");

        // Act
        var onset = GrooveOnsetFactory.FromWeightedCandidate(weighted, barNumber: 4);

        // Assert
        Assert.NotNull(onset.Provenance);
        Assert.Equal(GrooveOnsetSource.Variation, onset.Provenance.Source);
        Assert.Equal("KickPickup", onset.Provenance.GroupId);
        Assert.Equal("KickPickup:4.75", onset.Provenance.CandidateId);
    }

    [Fact]
    public void WithUpdatedProperties_PreservesProvenance()
    {
        // Arrange
        var originalProvenance = GrooveOnsetProvenance.ForVariation("TestGroup", "TestGroup:1.50");
        var original = new GrooveOnset
        {
            Role = "Snare",
            BarNumber = 1,
            Beat = 1.5m,
            Provenance = originalProvenance
        };

        // Act
        var updated = GrooveOnsetFactory.WithUpdatedProperties(original, velocity: 80, timingOffsetTicks: -5);

        // Assert
        Assert.Same(originalProvenance, updated.Provenance);
        Assert.Equal(80, updated.Velocity);
        Assert.Equal(-5, updated.TimingOffsetTicks);
    }

    [Fact]
    public void WithUpdatedProperties_UpdatesStrength()
    {
        // Arrange
        var original = new GrooveOnset
        {
            Role = "Snare",
            BarNumber = 1,
            Beat = 2.0m,
            Strength = null,
            Provenance = GrooveOnsetProvenance.ForAnchor()
        };

        // Act
        var updated = GrooveOnsetFactory.WithUpdatedProperties(original, strength: OnsetStrength.Backbeat);

        // Assert
        Assert.Equal(OnsetStrength.Backbeat, updated.Strength);
        Assert.NotNull(updated.Provenance);
    }

    #endregion

    #region Provenance Stability Tests

    [Fact]
    public void Provenance_IsStable_ForIdenticalAnchorCreation()
    {
        // Act - create same anchor onset twice
        var onset1 = GrooveOnsetFactory.FromAnchor("Kick", 1, 1.0m);
        var onset2 = GrooveOnsetFactory.FromAnchor("Kick", 1, 1.0m);

        // Assert - provenance should be equivalent
        Assert.Equal(onset1.Provenance!.Source, onset2.Provenance!.Source);
        Assert.Equal(onset1.Provenance.GroupId, onset2.Provenance.GroupId);
        Assert.Equal(onset1.Provenance.CandidateId, onset2.Provenance.CandidateId);
    }

    [Fact]
    public void Provenance_IsStable_ForIdenticalVariationCreation()
    {
        // Arrange
        var candidate = new DrumOnsetCandidate
        {
            Role = "Snare",
            OnsetBeat = 1.5m,
            ProbabilityBias = 0.5
        };
        var group = new DrumCandidateGroup
        {
            GroupId = "TestGroup",
            BaseProbabilityBias = 1.0,
            Candidates = new List<DrumOnsetCandidate> { candidate }
        };

        // Act - create same variation onset twice
        var onset1 = GrooveOnsetFactory.FromVariation(candidate, group, 1);
        var onset2 = GrooveOnsetFactory.FromVariation(candidate, group, 1);

        // Assert - provenance should be equivalent
        Assert.Equal(onset1.Provenance!.Source, onset2.Provenance!.Source);
        Assert.Equal(onset1.Provenance.GroupId, onset2.Provenance.GroupId);
        Assert.Equal(onset1.Provenance.CandidateId, onset2.Provenance.CandidateId);
    }

    [Fact]
    public void Provenance_DoesNotAffect_OnsetEquality()
    {
        // Arrange - two onsets with same position but different provenance
        var anchor = GrooveOnsetFactory.FromAnchor("Kick", 1, 1.0m);

        var candidate = new DrumOnsetCandidate { Role = "Kick", OnsetBeat = 1.0m, ProbabilityBias = 1.0 };
        var group = new DrumCandidateGroup { GroupId = "Test", BaseProbabilityBias = 1.0, Candidates = new() { candidate } };
        var variation = GrooveOnsetFactory.FromVariation(candidate, group, 1);

        // Assert - they are not equal due to different provenance
        Assert.NotEqual(anchor, variation);
        Assert.Equal(anchor.Role, variation.Role);
        Assert.Equal(anchor.Beat, variation.Beat);
        Assert.Equal(anchor.BarNumber, variation.BarNumber);
    }

    #endregion

    #region Provenance Preservation Tests

    [Fact]
    public void Provenance_PreservedAfter_RecordWithExpression()
    {
        // Arrange
        var original = GrooveOnsetFactory.FromAnchor("Kick", 1, 1.0m);

        // Act - use 'with' expression to create modified copy
        var modified = original with { Velocity = 100 };

        // Assert - provenance should be preserved
        Assert.Same(original.Provenance, modified.Provenance);
        Assert.Equal(GrooveOnsetSource.Anchor, modified.Provenance!.Source);
    }

    [Fact]
    public void VariationProvenance_DistinguishesDifferentGroups()
    {
        // Arrange
        var candidate1 = new DrumOnsetCandidate { Role = "Snare", OnsetBeat = 1.5m, ProbabilityBias = 0.5 };
        var group1 = new DrumCandidateGroup { GroupId = "GhostSnare", BaseProbabilityBias = 0.5, Candidates = new() { candidate1 } };

        var candidate2 = new DrumOnsetCandidate { Role = "Snare", OnsetBeat = 1.5m, ProbabilityBias = 0.5 };
        var group2 = new DrumCandidateGroup { GroupId = "PickupSnare", BaseProbabilityBias = 0.6, Candidates = new() { candidate2 } };

        // Act
        var onset1 = GrooveOnsetFactory.FromVariation(candidate1, group1, 1);
        var onset2 = GrooveOnsetFactory.FromVariation(candidate2, group2, 1);

        // Assert - same beat but different groups should have different provenance
        Assert.NotEqual(onset1.Provenance!.GroupId, onset2.Provenance!.GroupId);
        Assert.NotEqual(onset1.Provenance.CandidateId, onset2.Provenance.CandidateId);
    }

    #endregion
}

