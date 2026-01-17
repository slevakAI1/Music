// AI: purpose=Unit tests for OnsetGrid and OnsetGridBuilder; validates grid legality for all subdivision flags.
// AI: deps=XUnit; tests deterministic behavior of grid construction and position validation.
// AI: coverage=Story G2 acceptance criteria: each AllowedSubdivision flag, epsilon comparison, empty grid.

using Music.Generator;
using Xunit;

namespace Music.Tests.Generator
{
    public class OnsetGridTests
    {
        [Fact]
        public void Build_WithNone_ReturnsEmptyGrid()
        {
            // Arrange & Act
            var grid = OnsetGridBuilder.Build(4, AllowedSubdivision.None);

            // Assert
            Assert.Equal(4, grid.BeatsPerBar);
            Assert.Equal(AllowedSubdivision.None, grid.AllowedSubdivisions);
            Assert.Empty(grid.ValidPositions);
            Assert.False(grid.IsAllowed(1m));
            Assert.False(grid.IsAllowed(2m));
        }

        [Fact]
        public void Build_WithQuarter_AllowsOnlyWholeBeats()
        {
            // Arrange & Act
            var grid = OnsetGridBuilder.Build(4, AllowedSubdivision.Quarter);

            // Assert
            Assert.Equal(4, grid.BeatsPerBar);
            Assert.True(grid.IsAllowed(1m));
            Assert.True(grid.IsAllowed(2m));
            Assert.True(grid.IsAllowed(3m));
            Assert.True(grid.IsAllowed(4m));
            Assert.False(grid.IsAllowed(1.5m));
            Assert.False(grid.IsAllowed(2.25m));
        }

        [Fact]
        public void Build_WithEighth_AllowsHalfBeats()
        {
            // Arrange & Act
            var grid = OnsetGridBuilder.Build(4, AllowedSubdivision.Eighth);

            // Assert
            Assert.True(grid.IsAllowed(1m));
            Assert.True(grid.IsAllowed(1.5m));
            Assert.True(grid.IsAllowed(2m));
            Assert.True(grid.IsAllowed(2.5m));
            Assert.True(grid.IsAllowed(3m));
            Assert.True(grid.IsAllowed(3.5m));
            Assert.True(grid.IsAllowed(4m));
            Assert.True(grid.IsAllowed(4.5m));
            Assert.False(grid.IsAllowed(1.25m));
            Assert.False(grid.IsAllowed(1.75m));
        }

        [Fact]
        public void Build_WithSixteenth_AllowsQuarterBeats()
        {
            // Arrange & Act
            var grid = OnsetGridBuilder.Build(4, AllowedSubdivision.Sixteenth);

            // Assert
            Assert.True(grid.IsAllowed(1m));
            Assert.True(grid.IsAllowed(1.25m));
            Assert.True(grid.IsAllowed(1.5m));
            Assert.True(grid.IsAllowed(1.75m));
            Assert.True(grid.IsAllowed(2m));
            Assert.True(grid.IsAllowed(2.25m));
            Assert.False(grid.IsAllowed(1.125m));
            Assert.False(grid.IsAllowed(1.33m)); // Triplet position
        }

        [Fact]
        public void Build_WithEighthTriplet_AllowsTripletPositions()
        {
            // Arrange & Act
            var grid = OnsetGridBuilder.Build(4, AllowedSubdivision.EighthTriplet);

            // Assert - epsilon comparison for recurring fractions
            Assert.True(grid.IsAllowed(1m));
            Assert.True(grid.IsAllowed(1.333333m)); // 1 + 1/3
            Assert.True(grid.IsAllowed(1.666667m)); // 1 + 2/3
            Assert.True(grid.IsAllowed(2m));
            Assert.True(grid.IsAllowed(2.333333m)); // 2 + 1/3
            Assert.False(grid.IsAllowed(1.5m)); // Eighth position
            Assert.False(grid.IsAllowed(1.25m)); // Sixteenth position
        }

        [Fact]
        public void Build_WithSixteenthTriplet_AllowsFinerTripletGrid()
        {
            // Arrange & Act
            var grid = OnsetGridBuilder.Build(4, AllowedSubdivision.SixteenthTriplet);

            // Assert - epsilon comparison for 1/6 fractions
            Assert.True(grid.IsAllowed(1m));
            Assert.True(grid.IsAllowed(1.166667m)); // 1 + 1/6
            Assert.True(grid.IsAllowed(1.333333m)); // 1 + 2/6
            Assert.True(grid.IsAllowed(1.5m)); // 1 + 3/6
            Assert.True(grid.IsAllowed(1.666667m)); // 1 + 4/6
            Assert.True(grid.IsAllowed(1.833333m)); // 1 + 5/6
            Assert.True(grid.IsAllowed(2m));
        }

        [Fact]
        public void Build_WithMultipleFlags_CombinesGrids()
        {
            // Arrange & Act
            var grid = OnsetGridBuilder.Build(4, AllowedSubdivision.Quarter | AllowedSubdivision.Eighth);

            // Assert - should allow both quarter and eighth positions
            Assert.True(grid.IsAllowed(1m));
            Assert.True(grid.IsAllowed(1.5m));
            Assert.True(grid.IsAllowed(2m));
            Assert.True(grid.IsAllowed(2.5m));
            Assert.False(grid.IsAllowed(1.25m)); // Sixteenth not included
        }

        [Fact]
        public void Build_WithAllFlags_AllowsAllPositions()
        {
            // Arrange & Act
            var grid = OnsetGridBuilder.Build(4,
                AllowedSubdivision.Quarter |
                AllowedSubdivision.Eighth |
                AllowedSubdivision.Sixteenth |
                AllowedSubdivision.EighthTriplet |
                AllowedSubdivision.SixteenthTriplet);

            // Assert - should allow all common positions
            Assert.True(grid.IsAllowed(1m));
            Assert.True(grid.IsAllowed(1.25m));
            Assert.True(grid.IsAllowed(1.333333m));
            Assert.True(grid.IsAllowed(1.5m));
            Assert.True(grid.IsAllowed(1.666667m));
        }

        [Fact]
        public void Build_With3BeatsPerBar_RespectsTimeSignature()
        {
            // Arrange & Act
            var grid = OnsetGridBuilder.Build(3, AllowedSubdivision.Quarter | AllowedSubdivision.Eighth);

            // Assert - 3/4 time
            Assert.True(grid.IsAllowed(1m));
            Assert.True(grid.IsAllowed(1.5m));
            Assert.True(grid.IsAllowed(2m));
            Assert.True(grid.IsAllowed(2.5m));
            Assert.True(grid.IsAllowed(3m));
            Assert.True(grid.IsAllowed(3.5m));
            Assert.False(grid.IsAllowed(4m)); // No beat 4 in 3/4 time
        }

        [Fact]
        public void IsAllowed_WithEpsilonTolerance_HandlesRecurringFractions()
        {
            // Arrange
            var grid = OnsetGridBuilder.Build(4, AllowedSubdivision.EighthTriplet);

            // Act & Assert - various representations of 1 + 1/3
            Assert.True(grid.IsAllowed(1.333m));
            Assert.True(grid.IsAllowed(1.3333m));
            Assert.True(grid.IsAllowed(1.33333m));
            Assert.True(grid.IsAllowed(1.333333m));
        }

        [Fact]
        public void SnapToGrid_WithValidPosition_ReturnsExactMatch()
        {
            // Arrange
            var grid = OnsetGridBuilder.Build(4, AllowedSubdivision.Quarter);

            // Act
            var snapped = grid.SnapToGrid(2m);

            // Assert
            Assert.Equal(2m, snapped);
        }

        [Fact]
        public void SnapToGrid_WithInvalidPosition_ReturnsNearest()
        {
            // Arrange
            var grid = OnsetGridBuilder.Build(4, AllowedSubdivision.Quarter);

            // Act
            var snapped = grid.SnapToGrid(1.7m);

            // Assert
            Assert.Equal(2m, snapped);
        }

        [Fact]
        public void SnapToGrid_WithEmptyGrid_ReturnsNull()
        {
            // Arrange
            var grid = OnsetGridBuilder.Build(4, AllowedSubdivision.None);

            // Act
            var snapped = grid.SnapToGrid(1.5m);

            // Assert
            Assert.Null(snapped);
        }

        [Fact]
        public void Build_WithZeroBeatsPerBar_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                OnsetGridBuilder.Build(0, AllowedSubdivision.Quarter));
        }

        [Fact]
        public void Build_WithNegativeBeatsPerBar_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                OnsetGridBuilder.Build(-1, AllowedSubdivision.Quarter));
        }

        [Fact]
        public void ValidPositions_IsReadOnly()
        {
            // Arrange
            var grid = OnsetGridBuilder.Build(4, AllowedSubdivision.Quarter);

            // Act
            var positions = grid.ValidPositions;

            // Assert - collection is read-only (IReadOnlySet)
            Assert.NotNull(positions);
            Assert.IsAssignableFrom<IReadOnlySet<double>>(positions);
        }
    }
}
