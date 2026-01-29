// AI: purpose=Unit tests for DrumTrackFeatureData, DrumTrackFeatureDataBuilder, DrumFeatureDataSerializer.
// AI: deps=Tests feature data building and serialization; uses xUnit, FluentAssertions.
// AI: change=Story 7.2a; extend tests when adding new feature data fields.

using FluentAssertions;
using Music.Generator;
using Music.Generator.Agents.Drums.Diagnostics;
using Music.MyMidi;
using Xunit;

namespace Music.Tests.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Unit tests for DrumTrackFeatureData and related builders/serializers.
/// Story 7.2a: Track-Level Container and Serialization.
/// </summary>
public class DrumTrackFeatureDataTests
{
    #region Feature Data Builder

    [Fact]
    public void Build_EmptyTrack_ReturnsEmptyData()
    {
        // Arrange
        var partTrack = new PartTrack(new List<PartTrackEvent>());
        var barTrack = CreateBarTrack(4);

        // Act
        var data = DrumTrackFeatureDataBuilder.Build(partTrack, barTrack);

        // Assert
        data.Events.Should().BeEmpty();
        data.TotalBars.Should().Be(4);
        data.ActiveRoles.Should().BeEmpty();
        data.TotalHits.Should().Be(0);
    }

    [Fact]
    public void Build_WithEvents_ExtractsAllData()
    {
        // Arrange
        var partTrack = CreateTestPartTrack();
        var barTrack = CreateBarTrack(2);

        // Act
        var data = DrumTrackFeatureDataBuilder.Build(
            partTrack,
            barTrack,
            genreHint: "PopRock",
            artistHint: "TestArtist",
            tempoEstimateBpm: 120);

        // Assert
        data.Events.Should().NotBeEmpty();
        data.TotalBars.Should().Be(2);
        data.GenreHint.Should().Be("PopRock");
        data.ArtistHint.Should().Be("TestArtist");
        data.TempoEstimateBpm.Should().Be(120);
        data.ActiveRoles.Should().Contain("Kick");
        data.ActiveRoles.Should().Contain("Snare");
        data.BarPatterns.Should().HaveCount(2);
        data.BarStats.Should().HaveCount(2);
        data.RoleMatrices.Should().ContainKey("Kick");
        data.RoleMatrices.Should().ContainKey("Snare");
    }

    [Fact]
    public void Build_GeneratesUniqueTrackId()
    {
        // Arrange
        var partTrack = CreateTestPartTrack();
        var barTrack = CreateBarTrack(2);

        // Act
        var data = DrumTrackFeatureDataBuilder.Build(partTrack, barTrack);

        // Assert
        data.TrackId.Should().StartWith("drum_");
        data.TrackId.Should().HaveLength(13); // "drum_" + 8 hex chars
    }

    [Fact]
    public void Build_SetsSchemaVersion()
    {
        // Arrange
        var partTrack = CreateTestPartTrack();
        var barTrack = CreateBarTrack(2);

        // Act
        var data = DrumTrackFeatureDataBuilder.Build(partTrack, barTrack);

        // Assert
        data.SchemaVersion.Should().Be("1.0");
    }

    #endregion

    #region BuildWithValidation

    [Fact]
    public void BuildWithValidation_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var partTrack = CreateTestPartTrack();
        var barTrack = CreateBarTrack(2);
        var options = new DrumFeatureBuildOptions
        {
            GenreHint = "PopRock",
            TempoEstimateBpm = 120
        };

        // Act
        var result = DrumTrackFeatureDataBuilder.BuildWithValidation(partTrack, barTrack, options);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Messages.Should().BeEmpty();
    }

    [Fact]
    public void BuildWithValidation_EmptyTrack_ReturnsWarning()
    {
        // Arrange
        var partTrack = new PartTrack(new List<PartTrackEvent>());
        var barTrack = CreateBarTrack(2);
        var options = new DrumFeatureBuildOptions();

        // Act
        var result = DrumTrackFeatureDataBuilder.BuildWithValidation(partTrack, barTrack, options);

        // Assert
        result.IsSuccess.Should().BeFalse(); // Has warnings
        result.Data.Should().NotBeNull();
        result.Messages.Should().Contain(m => m.Contains("No drum events"));
    }

    [Fact]
    public void BuildWithValidation_NullPartTrack_ReturnsError()
    {
        // Arrange
        PartTrack partTrack = null!;
        var barTrack = CreateBarTrack(2);
        var options = new DrumFeatureBuildOptions();

        // Act
        var result = DrumTrackFeatureDataBuilder.BuildWithValidation(partTrack, barTrack, options);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Messages.Should().Contain("PartTrack is null");
    }

    #endregion

    #region Serialization Round-Trip

    [Fact]
    public void Serialize_Deserialize_PreservesAllData()
    {
        // Arrange
        var partTrack = CreateTestPartTrack();
        var barTrack = CreateBarTrack(2);
        var originalData = DrumTrackFeatureDataBuilder.Build(
            partTrack,
            barTrack,
            genreHint: "PopRock",
            artistHint: "TestArtist",
            tempoEstimateBpm: 120);

        // Act
        var json = DrumFeatureDataSerializer.Serialize(originalData);
        var deserializedData = DrumFeatureDataSerializer.Deserialize(json);

        // Assert
        deserializedData.TrackId.Should().Be(originalData.TrackId);
        deserializedData.GenreHint.Should().Be(originalData.GenreHint);
        deserializedData.ArtistHint.Should().Be(originalData.ArtistHint);
        deserializedData.TotalBars.Should().Be(originalData.TotalBars);
        deserializedData.TempoEstimateBpm.Should().Be(originalData.TempoEstimateBpm);
        deserializedData.Events.Should().HaveCount(originalData.Events.Count);
        deserializedData.BarPatterns.Should().HaveCount(originalData.BarPatterns.Count);
        deserializedData.BarStats.Should().HaveCount(originalData.BarStats.Count);
        deserializedData.ActiveRoles.Should().BeEquivalentTo(originalData.ActiveRoles);
    }

    [Fact]
    public void Serialize_Compact_ProducesSmallerOutput()
    {
        // Arrange
        var partTrack = CreateTestPartTrack();
        var barTrack = CreateBarTrack(2);
        var data = DrumTrackFeatureDataBuilder.Build(partTrack, barTrack);

        // Act
        var normalJson = DrumFeatureDataSerializer.Serialize(data, compact: false);
        var compactJson = DrumFeatureDataSerializer.Serialize(data, compact: true);

        // Assert
        compactJson.Length.Should().BeLessThan(normalJson.Length);
    }

    [Fact]
    public void Serialize_IncludesSchemaVersion()
    {
        // Arrange
        var partTrack = CreateTestPartTrack();
        var barTrack = CreateBarTrack(2);
        var data = DrumTrackFeatureDataBuilder.Build(partTrack, barTrack);

        // Act
        var json = DrumFeatureDataSerializer.Serialize(data);

        // Assert
        json.Should().Contain("schemaVersion");
        json.Should().Contain("1.0");
    }

    [Fact]
    public void Deserialize_InvalidJson_ThrowsException()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act & Assert
        Action act = () => DrumFeatureDataSerializer.Deserialize(invalidJson);
        act.Should().Throw<System.Text.Json.JsonException>();
    }

    [Fact]
    public void Deserialize_EmptyString_ThrowsArgumentException()
    {
        // Arrange
        var emptyJson = "";

        // Act & Assert
        Action act = () => DrumFeatureDataSerializer.Deserialize(emptyJson);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryDeserialize_ValidJson_ReturnsTrue()
    {
        // Arrange
        var partTrack = CreateTestPartTrack();
        var barTrack = CreateBarTrack(2);
        var data = DrumTrackFeatureDataBuilder.Build(partTrack, barTrack);
        var json = DrumFeatureDataSerializer.Serialize(data);

        // Act
        var success = DrumFeatureDataSerializer.TryDeserialize(json, out var result, out var error);

        // Assert
        success.Should().BeTrue();
        result.Should().NotBeNull();
        error.Should().BeNull();
    }

    [Fact]
    public void TryDeserialize_InvalidJson_ReturnsFalse()
    {
        // Arrange
        var invalidJson = "not valid json";

        // Act
        var success = DrumFeatureDataSerializer.TryDeserialize(invalidJson, out var result, out var error);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
        error.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Derived Properties

    [Fact]
    public void AverageHitsPerBar_CalculatesCorrectly()
    {
        // Arrange
        var partTrack = CreateTestPartTrack();
        var barTrack = CreateBarTrack(2);

        // Act
        var data = DrumTrackFeatureDataBuilder.Build(partTrack, barTrack);

        // Assert
        data.AverageHitsPerBar.Should().Be((double)data.TotalHits / data.TotalBars);
    }

    [Fact]
    public void UniquePatternCount_CountsDistinctPatterns()
    {
        // Arrange - Create track with 2 bars, same pattern in each
        var events = new List<PartTrackEvent>
        {
            CreateNoteEvent(0, 36, 100),      // Bar 1, beat 1, kick
            CreateNoteEvent(480, 38, 90),     // Bar 1, beat 2, snare
            CreateNoteEvent(1920, 36, 100),   // Bar 2, beat 1, kick
            CreateNoteEvent(2400, 38, 90)     // Bar 2, beat 2, snare
        };
        var partTrack = new PartTrack(events);
        var barTrack = CreateBarTrack(2);

        // Act
        var data = DrumTrackFeatureDataBuilder.Build(partTrack, barTrack);

        // Assert
        // Both bars have identical patterns, so unique count should be 1
        data.UniquePatternCount.Should().Be(1);
    }

    #endregion

    #region Event Extraction

    [Fact]
    public void Build_ExtractsEventsInOrder()
    {
        // Arrange
        var partTrack = CreateTestPartTrack();
        var barTrack = CreateBarTrack(2);

        // Act
        var data = DrumTrackFeatureDataBuilder.Build(partTrack, barTrack);

        // Assert
        data.Events.Should().BeInAscendingOrder(e => e.AbsoluteTimeTicks);
    }

    [Fact]
    public void Build_MapsRolesCorrectly()
    {
        // Arrange
        var partTrack = CreateTestPartTrack();
        var barTrack = CreateBarTrack(2);

        // Act
        var data = DrumTrackFeatureDataBuilder.Build(partTrack, barTrack);

        // Assert
        var kickEvents = data.Events.Where(e => e.Role == "Kick");
        var snareEvents = data.Events.Where(e => e.Role == "Snare");

        kickEvents.All(e => e.MidiNote == 36).Should().BeTrue();
        snareEvents.All(e => e.MidiNote == 38).Should().BeTrue();
    }

    #endregion

    #region Single-Bar and Multi-Bar Tracks

    [Fact]
    public void Build_SingleBarTrack_ExtractsCorrectly()
    {
        // Arrange
        var events = new List<PartTrackEvent>
        {
            CreateNoteEvent(0, 36, 100),
            CreateNoteEvent(480, 38, 90)
        };
        var partTrack = new PartTrack(events);
        var barTrack = CreateBarTrack(1);

        // Act
        var data = DrumTrackFeatureDataBuilder.Build(partTrack, barTrack);

        // Assert
        data.TotalBars.Should().Be(1);
        data.Events.Should().HaveCount(2);
        data.BarPatterns.Should().HaveCount(1);
        data.BarStats.Should().HaveCount(1);
    }

    [Fact]
    public void Build_MultiBarTrack_ExtractsAllBars()
    {
        // Arrange
        var events = new List<PartTrackEvent>();
        for (int bar = 0; bar < 8; bar++)
        {
            events.Add(CreateNoteEvent(bar * 1920, 36, 100));
            events.Add(CreateNoteEvent(bar * 1920 + 480, 38, 90));
        }
        var partTrack = new PartTrack(events);
        var barTrack = CreateBarTrack(8);

        // Act
        var data = DrumTrackFeatureDataBuilder.Build(partTrack, barTrack);

        // Assert
        data.TotalBars.Should().Be(8);
        data.Events.Should().HaveCount(16);
        data.BarPatterns.Should().HaveCount(8);
        data.BarStats.Should().HaveCount(8);
    }

    #endregion

    #region Helper Methods

    private static PartTrack CreateTestPartTrack()
    {
        var events = new List<PartTrackEvent>
        {
            // Bar 1
            CreateNoteEvent(0, 36, 100),      // Kick on beat 1
            CreateNoteEvent(480, 38, 90),     // Snare on beat 2
            CreateNoteEvent(960, 36, 95),     // Kick on beat 3
            CreateNoteEvent(1440, 38, 92),    // Snare on beat 4
            // Bar 2
            CreateNoteEvent(1920, 36, 100),   // Kick on beat 1
            CreateNoteEvent(2400, 38, 90)     // Snare on beat 2
        };

        return new PartTrack(events);
    }

    private static PartTrackEvent CreateNoteEvent(int absoluteTicks, int noteNumber, int velocity)
    {
        return new PartTrackEvent(noteNumber, absoluteTicks, 120, velocity);
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

