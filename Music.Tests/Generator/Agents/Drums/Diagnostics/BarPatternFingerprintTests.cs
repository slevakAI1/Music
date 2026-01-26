// AI: purpose=Unit tests for BarPatternFingerprint, BarPatternExtractor, BeatPositionMatrix, BeatPositionMatrixBuilder.
// AI: deps=Tests pattern extraction and matrix building; uses xUnit, FluentAssertions.
// AI: change=Story 7.2a; extend tests when adding new pattern analysis features.

using FluentAssertions;
using Music.Generator;
using Music.Generator.Agents.Drums.Diagnostics;
using Xunit;

namespace Music.Tests.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Unit tests for BarPatternFingerprint and BarPatternExtractor.
/// Story 7.2a: Per-Bar Pattern Fingerprint.
/// </summary>
public class BarPatternFingerprintTests
{
    #region Pattern Hash Determinism

    [Fact]
    public void Extract_SameEvents_ProducesSameHash()
    {
        // Arrange
        var events1 = CreateTestEvents();
        var events2 = CreateTestEvents();

        // Act
        var fingerprint1 = BarPatternExtractor.Extract(events1, 1, 4);
        var fingerprint2 = BarPatternExtractor.Extract(events2, 1, 4);

        // Assert
        fingerprint1.PatternHash.Should().Be(fingerprint2.PatternHash);
    }

    [Fact]
    public void Extract_DifferentEvents_ProducesDifferentHash()
    {
        // Arrange
        var events1 = new List<DrumMidiEvent>
        {
            CreateEvent(1, 1.0m, "Kick"),
            CreateEvent(1, 2.0m, "Snare")
        };

        var events2 = new List<DrumMidiEvent>
        {
            CreateEvent(1, 1.0m, "Kick"),
            CreateEvent(1, 3.0m, "Snare") // Different position
        };

        // Act
        var fingerprint1 = BarPatternExtractor.Extract(events1, 1, 4);
        var fingerprint2 = BarPatternExtractor.Extract(events2, 1, 4);

        // Assert
        fingerprint1.PatternHash.Should().NotBe(fingerprint2.PatternHash);
    }

    [Fact]
    public void Extract_EmptyEvents_ProducesStableEmptyHash()
    {
        // Arrange
        var events = new List<DrumMidiEvent>();

        // Act
        var fingerprint1 = BarPatternExtractor.Extract(events, 1, 4);
        var fingerprint2 = BarPatternExtractor.Extract(events, 2, 4);

        // Assert
        fingerprint1.PatternHash.Should().Be("0000000000000000");
        fingerprint2.PatternHash.Should().Be("0000000000000000");
    }

    #endregion

    #region Role Bitmasks

    [Fact]
    public void Extract_SingleKickOnBeatOne_SetsBit0()
    {
        // Arrange
        var events = new List<DrumMidiEvent>
        {
            CreateEvent(1, 1.0m, "Kick")
        };

        // Act
        var fingerprint = BarPatternExtractor.Extract(events, 1, 4, gridResolution: 16);

        // Assert
        fingerprint.RoleBitmasks.Should().ContainKey("Kick");
        fingerprint.RoleBitmasks["Kick"].Should().Be(1L); // Bit 0 set
    }

    [Fact]
    public void Extract_KickOnBeats1And3_SetsCorrectBits()
    {
        // Arrange - In 4/4 with 16th grid: beat 1 = position 0, beat 3 = position 8
        var events = new List<DrumMidiEvent>
        {
            CreateEvent(1, 1.0m, "Kick"),
            CreateEvent(1, 3.0m, "Kick")
        };

        // Act
        var fingerprint = BarPatternExtractor.Extract(events, 1, 4, gridResolution: 16);

        // Assert
        fingerprint.RoleBitmasks["Kick"].Should().Be((1L << 0) | (1L << 8));
    }

    [Fact]
    public void Extract_MultipleRoles_CreatesSeparateBitmasks()
    {
        // Arrange
        var events = new List<DrumMidiEvent>
        {
            CreateEvent(1, 1.0m, "Kick"),
            CreateEvent(1, 2.0m, "Snare"),
            CreateEvent(1, 3.0m, "Kick"),
            CreateEvent(1, 4.0m, "Snare")
        };

        // Act
        var fingerprint = BarPatternExtractor.Extract(events, 1, 4, gridResolution: 16);

        // Assert
        fingerprint.RoleBitmasks.Should().ContainKey("Kick");
        fingerprint.RoleBitmasks.Should().ContainKey("Snare");
        fingerprint.RoleEventCounts["Kick"].Should().Be(2);
        fingerprint.RoleEventCounts["Snare"].Should().Be(2);
    }

    #endregion

    #region Velocity Tracking

    [Fact]
    public void Extract_TracksVelocitiesPerRole()
    {
        // Arrange
        var events = new List<DrumMidiEvent>
        {
            CreateEvent(1, 1.0m, "Kick", velocity: 100),
            CreateEvent(1, 3.0m, "Kick", velocity: 80)
        };

        // Act
        var fingerprint = BarPatternExtractor.Extract(events, 1, 4);

        // Assert
        fingerprint.RoleVelocities["Kick"].Should().BeEquivalentTo(new[] { 100, 80 });
    }

    #endregion

    #region Grid Position Calculation

    [Theory]
    [InlineData(1.0, 4, 16, 0)]    // Beat 1 → position 0
    [InlineData(2.0, 4, 16, 4)]    // Beat 2 → position 4
    [InlineData(3.0, 4, 16, 8)]    // Beat 3 → position 8
    [InlineData(4.0, 4, 16, 12)]   // Beat 4 → position 12
    [InlineData(1.5, 4, 16, 2)]    // Beat 1.5 → position 2
    [InlineData(2.25, 4, 16, 5)]   // Beat 2.25 → position 5
    public void CalculateGridPosition_ReturnsExpectedPosition(
        decimal beat, int beatsPerBar, int gridResolution, int expectedPosition)
    {
        // Act
        var position = BarPatternExtractor.CalculateGridPosition(beat, beatsPerBar, gridResolution);

        // Assert
        position.Should().Be(expectedPosition);
    }

    #endregion

    #region Similarity Calculation

    [Fact]
    public void CalculateSimilarity_IdenticalPatterns_Returns1()
    {
        // Arrange
        var events = CreateTestEvents();
        var fingerprint1 = BarPatternExtractor.Extract(events, 1, 4);
        var fingerprint2 = BarPatternExtractor.Extract(events, 1, 4);

        // Act
        var similarity = BarPatternExtractor.CalculateSimilarity(fingerprint1, fingerprint2);

        // Assert
        similarity.Should().Be(1.0);
    }

    [Fact]
    public void CalculateSimilarity_CompletelyDifferent_ReturnsLowValue()
    {
        // Arrange
        var events1 = new List<DrumMidiEvent> { CreateEvent(1, 1.0m, "Kick") };
        var events2 = new List<DrumMidiEvent> { CreateEvent(1, 4.0m, "Snare") };

        var fingerprint1 = BarPatternExtractor.Extract(events1, 1, 4);
        var fingerprint2 = BarPatternExtractor.Extract(events2, 1, 4);

        // Act
        var similarity = BarPatternExtractor.CalculateSimilarity(fingerprint1, fingerprint2);

        // Assert
        similarity.Should().Be(0.0); // No overlap
    }

    [Fact]
    public void CalculateSimilarity_PartialOverlap_ReturnsMidValue()
    {
        // Arrange
        var events1 = new List<DrumMidiEvent>
        {
            CreateEvent(1, 1.0m, "Kick"),
            CreateEvent(1, 2.0m, "Snare")
        };
        var events2 = new List<DrumMidiEvent>
        {
            CreateEvent(1, 1.0m, "Kick"), // Same
            CreateEvent(1, 3.0m, "Snare") // Different position
        };

        var fingerprint1 = BarPatternExtractor.Extract(events1, 1, 4);
        var fingerprint2 = BarPatternExtractor.Extract(events2, 1, 4);

        // Act
        var similarity = BarPatternExtractor.CalculateSimilarity(fingerprint1, fingerprint2);

        // Assert
        similarity.Should().BeGreaterThan(0.0);
        similarity.Should().BeLessThan(1.0);
    }

    #endregion

    #region Variable Time Signatures

    [Fact]
    public void Extract_ThreeFourTime_HandlesCorrectly()
    {
        // Arrange - 3/4 time signature
        var events = new List<DrumMidiEvent>
        {
            CreateEvent(1, 1.0m, "Kick"),
            CreateEvent(1, 2.0m, "Snare"),
            CreateEvent(1, 3.0m, "Kick")
        };

        // Act
        var fingerprint = BarPatternExtractor.Extract(events, 1, 3, gridResolution: 12);

        // Assert
        fingerprint.BeatsPerBar.Should().Be(3);
        fingerprint.RoleEventCounts["Kick"].Should().Be(2);
        fingerprint.RoleEventCounts["Snare"].Should().Be(1);
    }

    #endregion

    #region Helper Methods

    private static List<DrumMidiEvent> CreateTestEvents()
    {
        return new List<DrumMidiEvent>
        {
            CreateEvent(1, 1.0m, "Kick"),
            CreateEvent(1, 2.0m, "Snare"),
            CreateEvent(1, 3.0m, "Kick"),
            CreateEvent(1, 4.0m, "Snare")
        };
    }

    private static DrumMidiEvent CreateEvent(
        int barNumber,
        decimal beat,
        string role,
        int velocity = 100)
    {
        return new DrumMidiEvent
        {
            BarNumber = barNumber,
            Beat = beat,
            Role = role,
            MidiNote = role == "Kick" ? 36 : 38,
            Velocity = velocity,
            DurationTicks = 120,
            AbsoluteTimeTicks = (barNumber - 1) * 1920 + (long)((beat - 1) * 480),
            TimingOffsetTicks = 0
        };
    }

    #endregion
}

/// <summary>
/// Unit tests for BeatPositionMatrix and BeatPositionMatrixBuilder.
/// Story 7.2a: Beat Position Matrix.
/// </summary>
public class BeatPositionMatrixTests
{
    #region Matrix Construction

    [Fact]
    public void Build_SingleRole_CreatesCorrectMatrix()
    {
        // Arrange
        var events = new List<DrumMidiEvent>
        {
            CreateEvent(1, 1.0m, "Kick", 100),
            CreateEvent(1, 3.0m, "Kick", 80),
            CreateEvent(2, 1.0m, "Kick", 90)
        };
        var barTrack = CreateBarTrack(2);

        // Act
        var matrix = BeatPositionMatrixBuilder.Build(events, barTrack, "Kick");

        // Assert
        matrix.Role.Should().Be("Kick");
        matrix.TotalBars.Should().Be(2);
        matrix.TotalHits.Should().Be(3);
    }

    [Fact]
    public void Build_FiltersByRole()
    {
        // Arrange
        var events = new List<DrumMidiEvent>
        {
            CreateEvent(1, 1.0m, "Kick", 100),
            CreateEvent(1, 2.0m, "Snare", 90),
            CreateEvent(1, 3.0m, "Kick", 80)
        };
        var barTrack = CreateBarTrack(1);

        // Act
        var kickMatrix = BeatPositionMatrixBuilder.Build(events, barTrack, "Kick");
        var snareMatrix = BeatPositionMatrixBuilder.Build(events, barTrack, "Snare");

        // Assert
        kickMatrix.TotalHits.Should().Be(2);
        snareMatrix.TotalHits.Should().Be(1);
    }

    #endregion

    #region Slot Access

    [Fact]
    public void GetSlot_ValidPosition_ReturnsSlot()
    {
        // Arrange
        var events = new List<DrumMidiEvent>
        {
            CreateEvent(1, 1.0m, "Kick", 100)
        };
        var barTrack = CreateBarTrack(1);
        var matrix = BeatPositionMatrixBuilder.Build(events, barTrack, "Kick", gridResolution: 16);

        // Act
        var slot = matrix.GetSlot(0, 0); // Bar 1, grid position 0

        // Assert
        slot.Should().NotBeNull();
        slot!.Velocity.Should().Be(100);
    }

    [Fact]
    public void GetSlot_EmptyPosition_ReturnsNull()
    {
        // Arrange
        var events = new List<DrumMidiEvent>
        {
            CreateEvent(1, 1.0m, "Kick", 100)
        };
        var barTrack = CreateBarTrack(1);
        var matrix = BeatPositionMatrixBuilder.Build(events, barTrack, "Kick", gridResolution: 16);

        // Act
        var slot = matrix.GetSlot(0, 4); // Bar 1, grid position 4 (beat 2) - should be empty

        // Assert
        slot.Should().BeNull();
    }

    [Fact]
    public void GetSlot_OutOfRange_ReturnsNull()
    {
        // Arrange
        var events = new List<DrumMidiEvent>();
        var barTrack = CreateBarTrack(1);
        var matrix = BeatPositionMatrixBuilder.Build(events, barTrack, "Kick");

        // Act
        var slot = matrix.GetSlot(99, 0);

        // Assert
        slot.Should().BeNull();
    }

    #endregion

    #region Hit Positions

    [Fact]
    public void GetHitPositions_ReturnsCorrectPositions()
    {
        // Arrange
        var events = new List<DrumMidiEvent>
        {
            CreateEvent(1, 1.0m, "Kick", 100),  // Position 0
            CreateEvent(1, 3.0m, "Kick", 80)   // Position 8
        };
        var barTrack = CreateBarTrack(1);
        var matrix = BeatPositionMatrixBuilder.Build(events, barTrack, "Kick", gridResolution: 16);

        // Act
        var hitPositions = matrix.GetHitPositions(0).ToList();

        // Assert
        hitPositions.Should().BeEquivalentTo(new[] { 0, 8 });
    }

    #endregion

    #region BuildAll

    [Fact]
    public void BuildAll_CreatesMatricesForAllRoles()
    {
        // Arrange
        var events = new List<DrumMidiEvent>
        {
            CreateEvent(1, 1.0m, "Kick", 100),
            CreateEvent(1, 2.0m, "Snare", 90),
            CreateEvent(1, 1.5m, "ClosedHat", 70)
        };
        var barTrack = CreateBarTrack(1);

        // Act
        var matrices = BeatPositionMatrixBuilder.BuildAll(events, barTrack);

        // Assert
        matrices.Should().ContainKey("Kick");
        matrices.Should().ContainKey("Snare");
        matrices.Should().ContainKey("ClosedHat");
        matrices.Count.Should().Be(3);
    }

    #endregion

    #region Statistics

    [Fact]
    public void GetAverageVelocity_CalculatesCorrectly()
    {
        // Arrange
        var events = new List<DrumMidiEvent>
        {
            CreateEvent(1, 1.0m, "Kick", 100),
            CreateEvent(1, 3.0m, "Kick", 80)
        };
        var barTrack = CreateBarTrack(1);
        var matrix = BeatPositionMatrixBuilder.Build(events, barTrack, "Kick");

        // Act
        var avgVelocity = matrix.GetAverageVelocity();

        // Assert
        avgVelocity.Should().Be(90.0);
    }

    [Fact]
    public void GetAverageVelocity_EmptyMatrix_ReturnsZero()
    {
        // Arrange
        var events = new List<DrumMidiEvent>();
        var barTrack = CreateBarTrack(1);
        var matrix = BeatPositionMatrixBuilder.Build(events, barTrack, "Kick");

        // Act
        var avgVelocity = matrix.GetAverageVelocity();

        // Assert
        avgVelocity.Should().Be(0);
    }

    #endregion

    #region Find Differences

    [Fact]
    public void FindDifferences_IdenticalMatrices_ReturnsEmpty()
    {
        // Arrange
        var events = new List<DrumMidiEvent>
        {
            CreateEvent(1, 1.0m, "Kick", 100)
        };
        var barTrack = CreateBarTrack(1);
        var matrix1 = BeatPositionMatrixBuilder.Build(events, barTrack, "Kick");
        var matrix2 = BeatPositionMatrixBuilder.Build(events, barTrack, "Kick");

        // Act
        var differences = BeatPositionMatrixBuilder.FindDifferences(matrix1, matrix2);

        // Assert
        differences.Should().BeEmpty();
    }

    [Fact]
    public void FindDifferences_DifferentPatterns_ReturnsDifferences()
    {
        // Arrange
        var events1 = new List<DrumMidiEvent> { CreateEvent(1, 1.0m, "Kick", 100) };
        var events2 = new List<DrumMidiEvent> { CreateEvent(1, 2.0m, "Kick", 100) };
        var barTrack = CreateBarTrack(1);

        var matrix1 = BeatPositionMatrixBuilder.Build(events1, barTrack, "Kick", gridResolution: 16);
        var matrix2 = BeatPositionMatrixBuilder.Build(events2, barTrack, "Kick", gridResolution: 16);

        // Act
        var differences = BeatPositionMatrixBuilder.FindDifferences(matrix1, matrix2);

        // Assert
        differences.Should().HaveCount(2); // Position 0 and position 4 differ
    }

    #endregion

    #region Helper Methods

    private static DrumMidiEvent CreateEvent(
        int barNumber,
        decimal beat,
        string role,
        int velocity)
    {
        return new DrumMidiEvent
        {
            BarNumber = barNumber,
            Beat = beat,
            Role = role,
            MidiNote = role == "Kick" ? 36 : 38,
            Velocity = velocity,
            DurationTicks = 120,
            AbsoluteTimeTicks = (barNumber - 1) * 1920 + (long)((beat - 1) * 480),
            TimingOffsetTicks = 0
        };
    }

    private static BarTrack CreateBarTrack(int totalBars)
    {
        var barTrack = new BarTrack();
        var timingTrack = new Timingtrack();
        timingTrack.Events.Add(new TimingEvent { StartBar = 1, Numerator = 4, Denominator = 4 });
        barTrack.RebuildFromTimingTrack(timingTrack, totalBars);
        return barTrack;
    }

    #endregion
}
