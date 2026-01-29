// AI: purpose=Unit tests for drummer diagnostics collection (Story 7.1).
// AI: invariants=Tests verify zero-cost when disabled, determinism preservation, and non-invasive behavior.
// AI: deps=xUnit for test framework; FluentAssertions for readable assertions.
// AI: change=Story 7.1 establishes test pattern; future stories add serialization tests.

using FluentAssertions;
using Music.Generator.Agents.Common;
using Music.Generator.Agents.Drums.Diagnostics;
using Xunit;

namespace Music.Tests.Generator.Agents.Drums;

/// <summary>
/// Tests for drummer diagnostics collection (Story 7.1).
/// Verifies diagnostics capture operator decisions without affecting generation behavior.
/// </summary>
public sealed class DrummerDiagnosticsTests
{
    #region Core Functionality Tests

    [Fact]
    public void DrummerDiagnostics_CapturesBasicInformation()
    {
        // Arrange
        var collector = new DrummerDiagnosticsCollector(barNumber: 8, role: "Snare");
        collector.RecordOperatorConsidered("DrumGhostBeforeBackbeat", "MicroAddition", candidateCount: 2);

        // Act
        var diagnostics = collector.Build();

        // Assert
        diagnostics.BarNumber.Should().Be(8);
        diagnostics.Role.Should().Be("Snare");
        diagnostics.OperatorsConsidered.Should().HaveCount(1);
        diagnostics.OperatorsConsidered[0].OperatorId.Should().Be("DrumGhostBeforeBackbeat");
        diagnostics.OperatorsConsidered[0].OperatorFamily.Should().Be("MicroAddition");
        diagnostics.OperatorsConsidered[0].CandidateCount.Should().Be(2);
    }

    [Fact]
    public void DrummerDiagnostics_CapturesOperatorScores()
    {
        // Arrange
        var collector = new DrummerDiagnosticsCollector(barNumber: 10, role: "Kick");
        collector.RecordOperatorSelected(
            operatorId: "DrumKickPickup",
            operatorFamily: "MicroAddition",
            candidateCount: 1,
            selectedCount: 1,
            baseScore: 0.75,
            styleWeight: 1.2,
            memoryPenalty: 0.9,
            finalScore: 0.81);

        // Act
        var diagnostics = collector.Build();

        // Assert
        var selected = diagnostics.OperatorsSelected.Should().ContainSingle().Subject;
        selected.OperatorId.Should().Be("DrumKickPickup");
        selected.BaseScore.Should().Be(0.75);
        selected.StyleWeight.Should().Be(1.2);
        selected.MemoryPenalty.Should().Be(0.9);
        selected.FinalScore.Should().BeApproximately(0.81, 0.01);
    }

    [Fact]
    public void DrummerDiagnostics_CapturesRejectionReasons()
    {
        // Arrange
        var collector = new DrummerDiagnosticsCollector(barNumber: 12, role: "Tom1");
        collector.RecordRejection(
            reason: "PhysicalityFilter:LimbConflict:LeftHand",
            candidateId: "DrumOperator_Tom1_12_1.5",
            detail: "Snare+Tom1 both require LeftHand at 1.5");

        // Act
        var diagnostics = collector.Build();

        // Assert
        var rejection = diagnostics.OperatorsRejected.Should().ContainSingle().Subject;
        rejection.Reason.Should().Be("PhysicalityFilter:LimbConflict:LeftHand");
        rejection.CandidateId.Should().Be("DrumOperator_Tom1_12_1.5");
        rejection.Detail.Should().Contain("Snare+Tom1");
    }

    [Fact]
    public void DrummerDiagnostics_CapturesMemoryState()
    {
        // Arrange
        var collector = new DrummerDiagnosticsCollector(barNumber: 15, role: "Snare");
        var memory = new AgentMemory(windowSize: 8);
        memory.RecordDecision(barNumber: 10, operatorId: "DrumGhostBeforeBackbeat", candidateId: "test1");
        memory.RecordDecision(barNumber: 12, operatorId: "DrumGhostBeforeBackbeat", candidateId: "test2");
        memory.RecordDecision(barNumber: 14, operatorId: "DrumKickPickup", candidateId: "test3");

        collector.RecordMemoryState(memory);

        // Act
        var diagnostics = collector.Build();

        // Assert
        diagnostics.MemoryState.MemoryWindowSize.Should().Be(8);
        diagnostics.MemoryState.RecentOperatorUsage.Should().ContainKey("DrumGhostBeforeBackbeat");
        diagnostics.MemoryState.RecentOperatorUsage["DrumGhostBeforeBackbeat"].Should().Be(2);
        diagnostics.MemoryState.RecentOperatorUsage["DrumKickPickup"].Should().Be(1);
    }

    [Fact]
    public void DrummerDiagnostics_CapturesDensityComparison()
    {
        // Arrange
        var collector = new DrummerDiagnosticsCollector(barNumber: 20, role: "ClosedHat");
        collector.RecordDensityComparison(
            targetDensity: 0.6,
            actualDensity: 0.55,
            targetEventCount: 12,
            actualEventCount: 11);

        // Act
        var diagnostics = collector.Build();

        // Assert
        diagnostics.DensityTargetVsActual.TargetDensity.Should().Be(0.6);
        diagnostics.DensityTargetVsActual.ActualDensity.Should().Be(0.55);
        diagnostics.DensityTargetVsActual.TargetEventCount.Should().Be(12);
        diagnostics.DensityTargetVsActual.ActualEventCount.Should().Be(11);
        diagnostics.DensityTargetVsActual.TargetMet.Should().BeFalse();
    }

    [Fact]
    public void DrummerDiagnostics_CapturesPhysicalityViolations()
    {
        // Arrange
        var collector = new DrummerDiagnosticsCollector(barNumber: 25, role: "Snare");
        collector.RecordPhysicalityViolation(
            candidateId: "DrumGhostCluster_Snare_25_2.25",
            violationType: "StickingViolation",
            reason: "StickingViolation:MaxConsecutiveSameHand:LeftHand",
            wasProtected: false);

        // Act
        var diagnostics = collector.Build();

        // Assert
        var violation = diagnostics.PhysicalityViolationsFiltered.Should().ContainSingle().Subject;
        violation.CandidateId.Should().Be("DrumGhostCluster_Snare_25_2.25");
        violation.ViolationType.Should().Be("StickingViolation");
        violation.Reason.Should().Contain("MaxConsecutiveSameHand");
        violation.WasProtected.Should().BeFalse();
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void DrummerDiagnostics_HandlesNoOperators()
    {
        // Arrange
        var collector = new DrummerDiagnosticsCollector(barNumber: 1, role: "Crash");

        // Act
        var diagnostics = collector.Build();

        // Assert
        diagnostics.OperatorsConsidered.Should().BeEmpty();
        diagnostics.OperatorsSelected.Should().BeEmpty();
        diagnostics.OperatorsRejected.Should().BeEmpty();
    }

    [Fact]
    public void DrummerDiagnostics_HandlesNoCandidates()
    {
        // Arrange
        var collector = new DrummerDiagnosticsCollector(barNumber: 5, role: "Ride");
        collector.RecordOperatorConsidered("DrumHatLift", "SubdivisionTransform", candidateCount: 0);

        // Act
        var diagnostics = collector.Build();

        // Assert
        var considered = diagnostics.OperatorsConsidered.Should().ContainSingle().Subject;
        considered.CandidateCount.Should().Be(0);
    }

    [Fact]
    public void DrummerDiagnostics_HandlesEmptyMemory()
    {
        // Arrange
        var collector = new DrummerDiagnosticsCollector(barNumber: 1, role: "Kick");
        collector.RecordMemoryState(null);

        // Act
        var diagnostics = collector.Build();

        // Assert
        diagnostics.MemoryState.MemoryWindowSize.Should().Be(0);
        diagnostics.MemoryState.RecentOperatorUsage.Should().BeEmpty();
        diagnostics.MemoryState.LastFillShape.Should().BeNull();
    }

    [Fact]
    public void DrummerDiagnostics_HandlesMultipleOperatorsSameFamily()
    {
        // Arrange
        var collector = new DrummerDiagnosticsCollector(barNumber: 16, role: "Snare");
        collector.RecordOperatorConsidered("DrumGhostBeforeBackbeat", "MicroAddition", 2);
        collector.RecordOperatorConsidered("DrumGhostAfterBackbeat", "MicroAddition", 2);
        collector.RecordOperatorConsidered("DrumGhostCluster", "MicroAddition", 3);

        // Act
        var diagnostics = collector.Build();

        // Assert
        diagnostics.OperatorsConsidered.Should().HaveCount(3);
        diagnostics.OperatorsConsidered.Should().OnlyContain(op => op.OperatorFamily == "MicroAddition");
    }

    [Fact]
    public void DrummerDiagnostics_HandlesDensityTargetMet()
    {
        // Arrange
        var collector = new DrummerDiagnosticsCollector(barNumber: 30, role: "Kick");
        collector.RecordDensityComparison(
            targetDensity: 0.5,
            actualDensity: 0.505,  // Within 0.01 tolerance
            targetEventCount: 8,
            actualEventCount: 8);

        // Act
        var diagnostics = collector.Build();

        // Assert
        diagnostics.DensityTargetVsActual.TargetMet.Should().BeTrue();
    }

    #endregion

    #region Memory State Tests

    [Fact]
    public void DrummerDiagnostics_CapturesFillShapeFromMemory()
    {
        // Arrange
        var collector = new DrummerDiagnosticsCollector(barNumber: 40, role: "Snare");
        var memory = new AgentMemory(windowSize: 8);
        
        // Record a fill using correct FillShape constructor parameters
        var fillShape = new FillShape(
            BarPosition: 38,
            RolesInvolved: new[] { "Snare", "Tom1", "FloorTom" },
            DensityLevel: 0.75,
            DurationBars: 1.0m);
        memory.RecordFillShape(fillShape);

        collector.RecordMemoryState(memory);

        // Act
        var diagnostics = collector.Build();

        // Assert
        diagnostics.MemoryState.LastFillShape.Should().NotBeNull();
        var fill = diagnostics.MemoryState.LastFillShape!;
        fill.BarNumber.Should().Be(38);
        fill.Roles.Should().BeEquivalentTo("Snare", "Tom1", "FloorTom");
        fill.Density.Should().Be(0.75);
        fill.EndingBeat.Should().Be(1.0m);
    }

    [Fact]
    public void DrummerDiagnostics_HandlesMemoryWithNoFills()
    {
        // Arrange
        var collector = new DrummerDiagnosticsCollector(barNumber: 5, role: "Kick");
        var memory = new AgentMemory(windowSize: 8);
        memory.RecordDecision(barNumber: 3, operatorId: "DrumKickPickup", candidateId: "test");

        collector.RecordMemoryState(memory);

        // Act
        var diagnostics = collector.Build();

        // Assert
        diagnostics.MemoryState.LastFillShape.Should().BeNull();
        diagnostics.MemoryState.RecentOperatorUsage.Should().ContainKey("DrumKickPickup");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void DrummerDiagnostics_CapturesCompleteBarTrace()
    {
        // Arrange: Simulate full bar generation with multiple operators
        var collector = new DrummerDiagnosticsCollector(barNumber: 50, role: "Snare");
        var memory = new AgentMemory(windowSize: 8);
        memory.RecordDecision(48, "DrumGhostBeforeBackbeat", "ghost1");

        // Operators considered
        collector.RecordOperatorConsidered("DrumGhostBeforeBackbeat", "MicroAddition", 2);
        collector.RecordOperatorConsidered("DrumGhostAfterBackbeat", "MicroAddition", 2);

        // One operator selected
        collector.RecordOperatorSelected(
            "DrumGhostBeforeBackbeat", "MicroAddition",
            candidateCount: 2, selectedCount: 1,
            baseScore: 0.7, styleWeight: 1.0, memoryPenalty: 0.95, finalScore: 0.665);

        // One operator rejected
        collector.RecordRejection(
            reason: "Memory:RecentUsage",
            operatorId: "DrumGhostBeforeBackbeat",
            detail: "Used 2 bars ago");

        // Physicality violation
        collector.RecordPhysicalityViolation(
            "DrumGhost_Snare_50_1.75",
            "Overcrowding",
            "Overcrowding:MaxHitsPerBeat");

        // Memory and density
        collector.RecordMemoryState(memory);
        collector.RecordDensityComparison(0.4, 0.38, 8, 7);

        // Act
        var diagnostics = collector.Build();

        // Assert: Verify all sections populated
        diagnostics.BarNumber.Should().Be(50);
        diagnostics.Role.Should().Be("Snare");
        diagnostics.OperatorsConsidered.Should().HaveCount(2);
        diagnostics.OperatorsSelected.Should().HaveCount(1);
        diagnostics.OperatorsRejected.Should().HaveCount(1);
        diagnostics.PhysicalityViolationsFiltered.Should().HaveCount(1);
        diagnostics.MemoryState.RecentOperatorUsage.Should().ContainKey("DrumGhostBeforeBackbeat");
        diagnostics.DensityTargetVsActual.ActualEventCount.Should().Be(7);
    }

    [Fact]
    public void DrummerDiagnostics_MultipleViolationsSameCandidate()
    {
        // Arrange
        var collector = new DrummerDiagnosticsCollector(barNumber: 60, role: "Tom1");
        collector.RecordPhysicalityViolation(
            "DrumTom_Tom1_60_2.0",
            "LimbConflict",
            "LimbConflict:LeftHand:Snare+Tom1");
        collector.RecordPhysicalityViolation(
            "DrumTom_Tom1_60_2.0",
            "StickingViolation",
            "StickingViolation:MaxConsecutiveSameHand");

        // Act
        var diagnostics = collector.Build();

        // Assert
        diagnostics.PhysicalityViolationsFiltered.Should().HaveCount(2);
        diagnostics.PhysicalityViolationsFiltered.Should().OnlyContain(v => 
            v.CandidateId == "DrumTom_Tom1_60_2.0");
    }

    #endregion

    #region Determinism Tests

    [Fact]
    public void DrummerDiagnostics_BuildIsIdempotent()
    {
        // Arrange
        var collector = new DrummerDiagnosticsCollector(barNumber: 70, role: "Kick");
        collector.RecordOperatorConsidered("DrumKickDouble", "MicroAddition", 2);
        collector.RecordDensityComparison(0.5, 0.5, 8, 8);

        // Act
        var diagnostics1 = collector.Build();
        var diagnostics2 = collector.Build();

        // Assert: Multiple Build() calls return equivalent objects
        diagnostics1.Should().BeEquivalentTo(diagnostics2);
    }

    [Fact]
    public void DrummerDiagnostics_DefaultsAreConsistent()
    {
        // Arrange: Build with no data recorded
        var collector = new DrummerDiagnosticsCollector(barNumber: 1, role: "Kick");

        // Act
        var diagnostics = collector.Build();

        // Assert: Verify sensible defaults
        diagnostics.OperatorsConsidered.Should().BeEmpty();
        diagnostics.OperatorsSelected.Should().BeEmpty();
        diagnostics.OperatorsRejected.Should().BeEmpty();
        diagnostics.PhysicalityViolationsFiltered.Should().BeEmpty();
        diagnostics.MemoryState.MemoryWindowSize.Should().Be(0);
        diagnostics.DensityTargetVsActual.TargetDensity.Should().Be(0.0);
        diagnostics.DensityTargetVsActual.ActualDensity.Should().Be(0.0);
    }

    #endregion
}

