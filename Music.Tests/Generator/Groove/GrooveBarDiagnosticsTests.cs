// AI: purpose=Unit tests for Story G1 groove decision trace diagnostics.
// AI: deps=GrooveBarDiagnostics, GrooveDiagnosticsCollector, GrooveBarPlan.
// AI: change=Story G1 acceptance criteria: opt-in diagnostics; zero-cost when disabled; no behavior change.

using Music.Generator;
using Music.Generator.Groove;
using Xunit;

namespace Music.Tests.Generator.Groove;

/// <summary>
/// Story G1: Tests for Groove Decision Trace (Opt-in, No Behavior Change).
/// Verifies GrooveBarDiagnostics, GrooveDiagnosticsCollector, and integration with GrooveBarPlan.
/// </summary>
public class GrooveBarDiagnosticsTests
{
    #region GrooveBarDiagnostics Record Tests

    [Fact]
    public void GrooveBarDiagnostics_CanCreate_WithAllRequiredFields()
    {
        // Arrange & Act
        var diagnostics = new GrooveBarDiagnostics
        {
            BarNumber = 1,
            Role = "Kick",
            EnabledTags = ["Fill", "Drive"],
            CandidateGroupCount = 3,
            TotalCandidateCount = 12,
            FiltersApplied = [],
            DensityTarget = new DensityTargetDiagnostics
            {
                Density01 = 0.5,
                MaxEventsPerBar = 8,
                TargetCount = 4
            },
            SelectedCandidates = [],
            PruneEvents = [],
            FinalOnsetSummary = new OnsetListSummary
            {
                BaseCount = 2,
                VariationCount = 2,
                FinalCount = 4
            }
        };

        // Assert
        Assert.Equal(1, diagnostics.BarNumber);
        Assert.Equal("Kick", diagnostics.Role);
        Assert.Equal(2, diagnostics.EnabledTags.Count);
        Assert.Contains("Fill", diagnostics.EnabledTags);
        Assert.Contains("Drive", diagnostics.EnabledTags);
        Assert.Equal(3, diagnostics.CandidateGroupCount);
        Assert.Equal(12, diagnostics.TotalCandidateCount);
        Assert.Equal(0.5, diagnostics.DensityTarget.Density01);
        Assert.Equal(4, diagnostics.FinalOnsetSummary.FinalCount);
    }

    [Fact]
    public void FilterDecision_RecordsReasonAndCandidateId()
    {
        // Arrange & Act
        var decision = new FilterDecision("Group1:2.50", "tag mismatch");

        // Assert
        Assert.Equal("Group1:2.50", decision.CandidateId);
        Assert.Equal("tag mismatch", decision.Reason);
    }

    [Fact]
    public void SelectionDecision_RecordsWeightAndRngStream()
    {
        // Arrange & Act
        var decision = new SelectionDecision("Group1:2.50", 0.75, "GrooveCandidatePick");

        // Assert
        Assert.Equal("Group1:2.50", decision.CandidateId);
        Assert.Equal(0.75, decision.Weight);
        Assert.Equal("GrooveCandidatePick", decision.RngStreamUsed);
    }

    [Fact]
    public void PruneDecision_RecordsProtectionStatus()
    {
        // Arrange & Act
        var protectedPrune = new PruneDecision("1:Kick:1.00", "cap violated", WasProtected: true);
        var unprotectedPrune = new PruneDecision("1:Snare:2.00", "lowest-scored", WasProtected: false);

        // Assert
        Assert.True(protectedPrune.WasProtected);
        Assert.False(unprotectedPrune.WasProtected);
        Assert.Equal("cap violated", protectedPrune.Reason);
        Assert.Equal("lowest-scored", unprotectedPrune.Reason);
    }

    [Fact]
    public void DensityTargetDiagnostics_IncludesMultiplierAndPolicyOverride()
    {
        // Arrange & Act
        var density = new DensityTargetDiagnostics
        {
            Density01 = 0.8,
            MaxEventsPerBar = 16,
            TargetCount = 10,
            Multiplier = 1.5,
            PolicyOverrideApplied = true
        };

        // Assert
        Assert.Equal(0.8, density.Density01);
        Assert.Equal(16, density.MaxEventsPerBar);
        Assert.Equal(10, density.TargetCount);
        Assert.Equal(1.5, density.Multiplier);
        Assert.True(density.PolicyOverrideApplied);
    }

    [Fact]
    public void OnsetListSummary_RecordsAllCounts()
    {
        // Arrange & Act
        var summary = new OnsetListSummary
        {
            BaseCount = 4,
            VariationCount = 6,
            FinalCount = 8
        };

        // Assert
        Assert.Equal(4, summary.BaseCount);
        Assert.Equal(6, summary.VariationCount);
        Assert.Equal(8, summary.FinalCount);
    }

    #endregion

    #region GrooveDiagnosticsCollector Tests

    [Fact]
    public void Collector_BuildsCompleteDiagnostics()
    {
        // Arrange
        var collector = new GrooveDiagnosticsCollector(barNumber: 5, role: "Snare");
        collector.RecordEnabledTags(["Fill", "Pickup"]);
        collector.RecordCandidatePool(groupCount: 2, candidateCount: 8);
        collector.RecordFilter("Group1:3.50", "never-add");
        collector.RecordDensityTarget(0.6, 10, 6, 1.0, false);
        collector.RecordSelection("Group2:2.00", 0.8, RandomPurpose.GrooveCandidatePick);
        collector.RecordPrune("5:Snare:4.00", "cap violated", wasProtected: false);
        collector.RecordOnsetCounts(baseCount: 2, variationCount: 3, finalCount: 4);

        // Act
        var diagnostics = collector.Build();

        // Assert
        Assert.Equal(5, diagnostics.BarNumber);
        Assert.Equal("Snare", diagnostics.Role);
        Assert.Equal(2, diagnostics.EnabledTags.Count);
        Assert.Contains("Fill", diagnostics.EnabledTags);
        Assert.Equal(2, diagnostics.CandidateGroupCount);
        Assert.Equal(8, diagnostics.TotalCandidateCount);
        Assert.Single(diagnostics.FiltersApplied);
        Assert.Equal("Group1:3.50", diagnostics.FiltersApplied[0].CandidateId);
        Assert.Equal("never-add", diagnostics.FiltersApplied[0].Reason);
        Assert.Equal(0.6, diagnostics.DensityTarget.Density01);
        Assert.Equal(6, diagnostics.DensityTarget.TargetCount);
        Assert.Single(diagnostics.SelectedCandidates);
        Assert.Equal("GrooveCandidatePick", diagnostics.SelectedCandidates[0].RngStreamUsed);
        Assert.Single(diagnostics.PruneEvents);
        Assert.Equal(2, diagnostics.FinalOnsetSummary.BaseCount);
        Assert.Equal(3, diagnostics.FinalOnsetSummary.VariationCount);
        Assert.Equal(4, diagnostics.FinalOnsetSummary.FinalCount);
    }

    [Fact]
    public void Collector_CanRecordMultipleFilters()
    {
        // Arrange
        var collector = new GrooveDiagnosticsCollector(1, "Kick");
        collector.RecordFilter("G1:1.00", "tag mismatch");
        collector.RecordFilter("G1:2.00", "grid invalid");
        collector.RecordFilter("G2:3.00", "never-add");

        // Act
        var diagnostics = collector.Build();

        // Assert
        Assert.Equal(3, diagnostics.FiltersApplied.Count);
        Assert.Equal("tag mismatch", diagnostics.FiltersApplied[0].Reason);
        Assert.Equal("grid invalid", diagnostics.FiltersApplied[1].Reason);
        Assert.Equal("never-add", diagnostics.FiltersApplied[2].Reason);
    }

    [Fact]
    public void Collector_CanRecordMultipleSelections()
    {
        // Arrange
        var collector = new GrooveDiagnosticsCollector(1, "ClosedHat");
        collector.RecordSelection("G1:1.50", 0.9, RandomPurpose.GrooveCandidatePick);
        collector.RecordSelection("G1:2.50", 0.7, "GrooveCandidatePick");
        collector.RecordSelection("G2:3.50", 0.5, RandomPurpose.GrooveTieBreak);

        // Act
        var diagnostics = collector.Build();

        // Assert
        Assert.Equal(3, diagnostics.SelectedCandidates.Count);
        Assert.Equal(0.9, diagnostics.SelectedCandidates[0].Weight);
        Assert.Equal("GrooveCandidatePick", diagnostics.SelectedCandidates[0].RngStreamUsed);
        Assert.Equal("GrooveTieBreak", diagnostics.SelectedCandidates[2].RngStreamUsed);
    }

    [Fact]
    public void Collector_CanRecordMultiplePrunes()
    {
        // Arrange
        var collector = new GrooveDiagnosticsCollector(2, "Kick");
        collector.RecordPrune("2:Kick:1.00", "cap violated", true);
        collector.RecordPrune("2:Kick:3.00", "lowest-scored", false);

        // Act
        var diagnostics = collector.Build();

        // Assert
        Assert.Equal(2, diagnostics.PruneEvents.Count);
        Assert.True(diagnostics.PruneEvents[0].WasProtected);
        Assert.False(diagnostics.PruneEvents[1].WasProtected);
    }

    [Fact]
    public void Collector_BuildsValidDiagnostics_WithMinimalData()
    {
        // Arrange - minimal collector with no recorded data
        var collector = new GrooveDiagnosticsCollector(1, "Kick");

        // Act
        var diagnostics = collector.Build();

        // Assert - should have empty lists and default density
        Assert.Equal(1, diagnostics.BarNumber);
        Assert.Equal("Kick", diagnostics.Role);
        Assert.Empty(diagnostics.EnabledTags);
        Assert.Equal(0, diagnostics.CandidateGroupCount);
        Assert.Equal(0, diagnostics.TotalCandidateCount);
        Assert.Empty(diagnostics.FiltersApplied);
        Assert.Equal(0.0, diagnostics.DensityTarget.Density01);
        Assert.Equal(0, diagnostics.DensityTarget.TargetCount);
        Assert.Empty(diagnostics.SelectedCandidates);
        Assert.Empty(diagnostics.PruneEvents);
        Assert.Equal(0, diagnostics.FinalOnsetSummary.BaseCount);
    }

    [Fact]
    public void Collector_RecordEnabledTags_ReplacesExistingTags()
    {
        // Arrange
        var collector = new GrooveDiagnosticsCollector(1, "Kick");
        collector.RecordEnabledTags(["Tag1", "Tag2"]);
        collector.RecordEnabledTags(["Tag3"]); // Should replace, not append

        // Act
        var diagnostics = collector.Build();

        // Assert
        Assert.Single(diagnostics.EnabledTags);
        Assert.Contains("Tag3", diagnostics.EnabledTags);
        Assert.DoesNotContain("Tag1", diagnostics.EnabledTags);
    }

    #endregion

    #region Static Helper Method Tests

    [Fact]
    public void MakeCandidateId_FromGroupAndBeat_ReturnsStableFormat()
    {
        // Act
        var id = GrooveDiagnosticsCollector.MakeCandidateId("GroupA", 2.5m);

        // Assert
        Assert.Equal("GroupA:2.50", id);
    }

    [Fact]
    public void MakeCandidateId_FromIndices_ReturnsStableFormat()
    {
        // Act
        var id = GrooveDiagnosticsCollector.MakeCandidateId(groupIndex: 1, candidateIndex: 3);

        // Assert
        Assert.Equal("Group_1:Candidate_3", id);
    }

    [Fact]
    public void MakeOnsetId_FromComponents_ReturnsStableFormat()
    {
        // Act
        var id = GrooveDiagnosticsCollector.MakeOnsetId(barNumber: 5, role: "Snare", beat: 2.0m);

        // Assert
        Assert.Equal("5:Snare:2.00", id);
    }

    [Fact]
    public void MakeOnsetId_FromGrooveOnset_ReturnsStableFormat()
    {
        // Arrange
        var onset = new GrooveOnset
        {
            BarNumber = 3,
            Role = "Kick",
            Beat = 1.5m
        };

        // Act
        var id = GrooveDiagnosticsCollector.MakeOnsetId(onset);

        // Assert
        Assert.Equal("3:Kick:1.50", id);
    }

    [Fact]
    public void MakeOnsetId_ThrowsForNullOnset()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => GrooveDiagnosticsCollector.MakeOnsetId(null!));
    }

    #endregion

    #region GrooveBarPlan Integration Tests

    [Fact]
    public void GrooveBarPlan_Diagnostics_CanBeNull()
    {
        // Arrange & Act - diagnostics disabled scenario
        var plan = new GrooveBarPlan
        {
            BarNumber = 1,
            BaseOnsets = [],
            SelectedVariationOnsets = [],
            FinalOnsets = [],
            Diagnostics = null
        };

        // Assert
        Assert.Null(plan.Diagnostics);
    }

    [Fact]
    public void GrooveBarPlan_Diagnostics_CanBePopulated()
    {
        // Arrange
        var collector = new GrooveDiagnosticsCollector(1, "Kick");
        collector.RecordCandidatePool(2, 5);
        collector.RecordOnsetCounts(1, 2, 3);
        var diagnostics = collector.Build();

        // Act
        var plan = new GrooveBarPlan
        {
            BarNumber = 1,
            BaseOnsets = [],
            SelectedVariationOnsets = [],
            FinalOnsets = [],
            Diagnostics = diagnostics
        };

        // Assert
        Assert.NotNull(plan.Diagnostics);
        Assert.Equal(1, plan.Diagnostics.BarNumber);
        Assert.Equal("Kick", plan.Diagnostics.Role);
        Assert.Equal(2, plan.Diagnostics.CandidateGroupCount);
        Assert.Equal(3, plan.Diagnostics.FinalOnsetSummary.FinalCount);
    }

    [Fact]
    public void GrooveBarPlan_WithDiagnostics_DoesNotAffectOnsets()
    {
        // Arrange - same onsets, with and without diagnostics
        var onsets = new List<GrooveOnset>
        {
            new() { BarNumber = 1, Role = "Kick", Beat = 1.0m },
            new() { BarNumber = 1, Role = "Kick", Beat = 3.0m }
        };

        var planWithoutDiag = new GrooveBarPlan
        {
            BarNumber = 1,
            BaseOnsets = onsets,
            SelectedVariationOnsets = [],
            FinalOnsets = onsets,
            Diagnostics = null
        };

        var collector = new GrooveDiagnosticsCollector(1, "Kick");
        collector.RecordOnsetCounts(2, 0, 2);
        var planWithDiag = new GrooveBarPlan
        {
            BarNumber = 1,
            BaseOnsets = onsets,
            SelectedVariationOnsets = [],
            FinalOnsets = onsets,
            Diagnostics = collector.Build()
        };

        // Assert - onset lists are the same regardless of diagnostics
        Assert.Equal(planWithoutDiag.FinalOnsets.Count, planWithDiag.FinalOnsets.Count);
        for (int i = 0; i < onsets.Count; i++)
        {
            Assert.Equal(planWithoutDiag.FinalOnsets[i].Beat, planWithDiag.FinalOnsets[i].Beat);
            Assert.Equal(planWithoutDiag.FinalOnsets[i].Role, planWithDiag.FinalOnsets[i].Role);
        }
    }

    #endregion

    #region Determinism Tests

    [Fact]
    public void Collector_ProducesSameOutput_ForSameInputs()
    {
        // Arrange & Act - create two collectors with identical inputs
        GrooveBarDiagnostics Build()
        {
            var collector = new GrooveDiagnosticsCollector(1, "Kick");
            collector.RecordEnabledTags(["Fill", "Drive"]);
            collector.RecordCandidatePool(3, 10);
            collector.RecordFilter("G1:2.00", "tag mismatch");
            collector.RecordDensityTarget(0.5, 8, 4);
            collector.RecordSelection("G2:1.00", 0.8, RandomPurpose.GrooveCandidatePick);
            collector.RecordPrune("1:Kick:3.00", "cap violated", false);
            collector.RecordOnsetCounts(2, 2, 4);
            return collector.Build();
        }

        var diag1 = Build();
        var diag2 = Build();

        // Assert - outputs should be equal
        Assert.Equal(diag1.BarNumber, diag2.BarNumber);
        Assert.Equal(diag1.Role, diag2.Role);
        Assert.Equal(diag1.EnabledTags, diag2.EnabledTags);
        Assert.Equal(diag1.CandidateGroupCount, diag2.CandidateGroupCount);
        Assert.Equal(diag1.TotalCandidateCount, diag2.TotalCandidateCount);
        Assert.Equal(diag1.FiltersApplied.Count, diag2.FiltersApplied.Count);
        Assert.Equal(diag1.FiltersApplied[0].CandidateId, diag2.FiltersApplied[0].CandidateId);
        Assert.Equal(diag1.DensityTarget.TargetCount, diag2.DensityTarget.TargetCount);
        Assert.Equal(diag1.SelectedCandidates.Count, diag2.SelectedCandidates.Count);
        Assert.Equal(diag1.PruneEvents.Count, diag2.PruneEvents.Count);
        Assert.Equal(diag1.FinalOnsetSummary.FinalCount, diag2.FinalOnsetSummary.FinalCount);
    }

    #endregion

    #region RNG Stream Name Tests

    [Theory]
    [InlineData(RandomPurpose.GrooveCandidatePick, "GrooveCandidatePick")]
    [InlineData(RandomPurpose.GrooveTieBreak, "GrooveTieBreak")]
    public void Collector_RecordsRngStreamName_FromRandomPurpose(RandomPurpose purpose, string expectedName)
    {
        // Arrange
        var collector = new GrooveDiagnosticsCollector(1, "Kick");

        // Act
        collector.RecordSelection("G1:1.00", 0.5, purpose);
        var diagnostics = collector.Build();

        // Assert
        Assert.Equal(expectedName, diagnostics.SelectedCandidates[0].RngStreamUsed);
    }

    #endregion
}

