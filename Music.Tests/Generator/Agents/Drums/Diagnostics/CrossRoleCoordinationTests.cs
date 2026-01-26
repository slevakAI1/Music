// AI: purpose=Unit tests for Story 7.2b cross-role coordination extraction.
// AI: deps=Tests CrossRoleCoordinationExtractor; uses xUnit, FluentAssertions.
// AI: change=Story 7.2b; extend tests when adding coordination metrics.

using FluentAssertions;
using Music.Generator.Agents.Drums.Diagnostics;
using Xunit;

namespace Music.Tests.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Unit tests for cross-role coordination extraction (Story 7.2b).
/// </summary>
public class CrossRoleCoordinationTests
{
    [Fact]
    public void Extract_WithCoincidentHits_ComputesCoincidenceAndLockScores()
    {
        // Arrange - Create matrices with overlapping hits
        var kickMatrix = CreateMatrix("Kick", 4, new[]
        {
            new[] { 0, 4, 8, 12 },  // Bar 1: beats 1, 2, 3, 4
            new[] { 0, 8 },         // Bar 2: beats 1, 3
            new[] { 0, 4, 8, 12 },  // Bar 3
            new[] { 0, 8 }          // Bar 4
        });

        var snareMatrix = CreateMatrix("Snare", 4, new[]
        {
            new[] { 4, 12 },        // Bar 1: beats 2, 4 (backbeats)
            new[] { 4, 12 },        // Bar 2
            new[] { 4, 12 },        // Bar 3
            new[] { 4, 12 }         // Bar 4
        });

        var roleMatrices = new Dictionary<string, BeatPositionMatrix>
        {
            ["Kick"] = kickMatrix,
            ["Snare"] = snareMatrix
        };

        // Act
        var result = CrossRoleCoordinationExtractor.Extract(roleMatrices);

        // Assert
        result.CoincidenceCount.Should().ContainKey("Kick+Snare");
        result.CoincidenceCount["Kick+Snare"].Should().BeGreaterThan(0);

        result.LockScores.Should().ContainKey("Kick+Snare");
        result.LockScores["Kick+Snare"].Should().BeGreaterThanOrEqualTo(0);

        result.RolePairDetails.Should().NotBeEmpty();
        result.RolePairDetails[0].RoleA.Should().Be("Kick");
        result.RolePairDetails[0].RoleB.Should().Be("Snare");
    }

    [Fact]
    public void Extract_SingleRole_ReturnsEmptyResult()
    {
        var matrices = new Dictionary<string, BeatPositionMatrix>
        {
            ["Kick"] = CreateMatrix("Kick", 2, new[] { new[] { 0 }, new[] { 0 } })
        };

        var result = CrossRoleCoordinationExtractor.Extract(matrices);

        result.CoincidenceCount.Should().BeEmpty();
        result.LockScores.Should().BeEmpty();
    }

    [Fact]
    public void CreateKey_SortsRolesAlphabetically()
    {
        CrossRoleCoordinationData.CreateKey("Snare", "Kick").Should().Be("Kick+Snare");
        CrossRoleCoordinationData.CreateKey("Kick", "Snare").Should().Be("Kick+Snare");
    }

    #region Helper Methods

    private static BeatPositionMatrix CreateMatrix(string role, int totalBars, int[][] hitPositions)
    {
        var barSlots = new List<BeatPositionSlot?[]>();

        for (int bar = 0; bar < totalBars; bar++)
        {
            var slots = new BeatPositionSlot?[16];
            if (bar < hitPositions.Length)
            {
                foreach (var pos in hitPositions[bar])
                {
                    if (pos < 16)
                        slots[pos] = new BeatPositionSlot(100, 0);
                }
            }
            barSlots.Add(slots);
        }

        return new BeatPositionMatrix
        {
            Role = role,
            TotalBars = totalBars,
            GridResolution = 16,
            BarSlots = barSlots
        };
    }

    #endregion
}
