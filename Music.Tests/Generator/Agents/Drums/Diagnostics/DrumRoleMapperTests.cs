// AI: purpose=Unit tests for DrumRoleMapper; verifies GM2 note mappings, role grouping, primary role detection.
// AI: deps=Tests DrumRoleMapper; uses xUnit, FluentAssertions.
// AI: change=Story 7.2a; extend tests when adding new role mappings.

using FluentAssertions;
using Music.Generator.Agents.Drums.Diagnostics;
using Xunit;

namespace Music.Tests.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Unit tests for DrumRoleMapper.
/// Story 7.2a: Raw Event Extraction.
/// </summary>
public class DrumRoleMapperTests
{
    #region Primary Role Mappings (GM2 Standard)

    [Theory]
    [InlineData(35, "Kick")]      // Acoustic Bass Drum
    [InlineData(36, "Kick")]      // Bass Drum 1
    [InlineData(37, "SideStick")] // Side Stick
    [InlineData(38, "Snare")]     // Acoustic Snare
    [InlineData(40, "Snare")]     // Electric Snare (grouped with Snare)
    [InlineData(41, "FloorTom")]  // Low Floor Tom
    [InlineData(42, "ClosedHat")] // Closed Hi-Hat
    [InlineData(44, "PedalHat")]  // Pedal Hi-Hat
    [InlineData(46, "OpenHat")]   // Open Hi-Hat
    [InlineData(47, "Tom2")]      // Low-Mid Tom
    [InlineData(48, "Tom1")]      // Hi-Mid Tom
    [InlineData(49, "Crash")]     // Crash Cymbal 1
    [InlineData(51, "Ride")]      // Ride Cymbal 1
    [InlineData(53, "RideBell")]  // Ride Bell
    [InlineData(57, "Crash")]     // Crash Cymbal 2 (grouped with Crash)
    public void MapNoteToRole_KnownNote_ReturnsExpectedRole(int midiNote, string expectedRole)
    {
        // Act
        var result = DrumRoleMapper.MapNoteToRole(midiNote);

        // Assert
        result.Should().Be(expectedRole, $"GM2 note {midiNote} should map to '{expectedRole}'");
    }

    #endregion

    #region Snare Articulation Grouping

    [Fact]
    public void MapNoteToRole_SnareVariants_GroupedAsSnare()
    {
        // GM2 snare-related notes should group to Snare role
        var snareNotes = new[] { 38, 39, 40 }; // Acoustic Snare, Hand Clap, Electric Snare

        foreach (var note in snareNotes)
        {
            var role = DrumRoleMapper.MapNoteToRole(note);
            role.Should().Be("Snare", $"Note {note} should be grouped with Snare");
        }
    }

    #endregion

    #region Crash Cymbal Grouping

    [Fact]
    public void MapNoteToRole_CrashVariants_GroupedAsCrash()
    {
        // GM2 crash cymbal notes should group to Crash role
        var crashNotes = new[] { 49, 57 }; // Crash Cymbal 1, Crash Cymbal 2

        foreach (var note in crashNotes)
        {
            var role = DrumRoleMapper.MapNoteToRole(note);
            role.Should().Be("Crash", $"Note {note} should be grouped with Crash");
        }
    }

    #endregion

    #region Unknown Note Handling

    [Theory]
    [InlineData(0)]
    [InlineData(34)]  // Just below drum range
    [InlineData(82)]  // Just above drum range
    [InlineData(127)]
    public void MapNoteToRole_UnknownNote_ReturnsUnknownWithNoteNumber(int midiNote)
    {
        // Act
        var result = DrumRoleMapper.MapNoteToRole(midiNote);

        // Assert
        result.Should().Be($"Unknown:{midiNote}");
    }

    #endregion

    #region Primary Role Detection

    [Theory]
    [InlineData(36, true)]   // Kick - primary
    [InlineData(38, true)]   // Snare - primary
    [InlineData(42, true)]   // ClosedHat - primary
    [InlineData(49, true)]   // Crash - primary
    [InlineData(51, true)]   // Ride - primary
    [InlineData(54, false)]  // Tambourine - not primary
    [InlineData(56, false)]  // Cowbell - not primary
    [InlineData(60, false)]  // Bongo - not primary
    [InlineData(99, false)]  // Unknown - not primary
    public void IsPrimaryRole_ReturnsExpectedValue(int midiNote, bool expectedIsPrimary)
    {
        // Act
        var result = DrumRoleMapper.IsPrimaryRole(midiNote);

        // Assert
        result.Should().Be(expectedIsPrimary);
    }

    #endregion

    #region GetNotesForRole

    [Fact]
    public void GetNotesForRole_Kick_ReturnsBothKickNotes()
    {
        // Act
        var notes = DrumRoleMapper.GetNotesForRole("Kick");

        // Assert
        notes.Should().BeEquivalentTo(new[] { 35, 36 });
    }

    [Fact]
    public void GetNotesForRole_Snare_ReturnsAllSnareNotes()
    {
        // Act
        var notes = DrumRoleMapper.GetNotesForRole("Snare");

        // Assert
        notes.Should().BeEquivalentTo(new[] { 38, 39, 40 });
    }

    [Fact]
    public void GetNotesForRole_UnknownRole_ReturnsEmptyList()
    {
        // Act
        var notes = DrumRoleMapper.GetNotesForRole("UnknownRole");

        // Assert
        notes.Should().BeEmpty();
    }

    #endregion

    #region GetAllRoles

    [Fact]
    public void GetAllRoles_ContainsExpectedPrimaryRoles()
    {
        // Act
        var roles = DrumRoleMapper.GetAllRoles();

        // Assert
        roles.Should().Contain("Kick");
        roles.Should().Contain("Snare");
        roles.Should().Contain("ClosedHat");
        roles.Should().Contain("OpenHat");
        roles.Should().Contain("Crash");
        roles.Should().Contain("Ride");
        roles.Should().Contain("Tom1");
        roles.Should().Contain("Tom2");
        roles.Should().Contain("FloorTom");
    }

    #endregion

    #region IsInDrumRange

    [Theory]
    [InlineData(35, true)]   // First drum note
    [InlineData(36, true)]   // Kick
    [InlineData(60, true)]   // Mid range
    [InlineData(81, true)]   // Last drum note
    [InlineData(34, false)]  // Below range
    [InlineData(82, false)]  // Above range
    [InlineData(0, false)]   // Way below
    [InlineData(127, false)] // Way above
    public void IsInDrumRange_ReturnsExpectedValue(int midiNote, bool expectedInRange)
    {
        // Act
        var result = DrumRoleMapper.IsInDrumRange(midiNote);

        // Assert
        result.Should().Be(expectedInRange);
    }

    #endregion

    #region Percussion Instruments

    [Theory]
    [InlineData(54, "Tambourine")]
    [InlineData(55, "Splash")]
    [InlineData(56, "Cowbell")]
    [InlineData(60, "Bongo")]
    [InlineData(62, "Conga")]
    [InlineData(75, "Claves")]
    [InlineData(80, "Triangle")]
    public void MapNoteToRole_PercussionInstruments_MapsCorrectly(int midiNote, string expectedRole)
    {
        // Act
        var result = DrumRoleMapper.MapNoteToRole(midiNote);

        // Assert
        result.Should().Be(expectedRole);
    }

    #endregion

    #region Determinism

    [Fact]
    public void MapNoteToRole_SameInput_AlwaysReturnsSameOutput()
    {
        // Test determinism by calling multiple times
        for (int i = 0; i < 100; i++)
        {
            DrumRoleMapper.MapNoteToRole(36).Should().Be("Kick");
            DrumRoleMapper.MapNoteToRole(38).Should().Be("Snare");
            DrumRoleMapper.MapNoteToRole(42).Should().Be("ClosedHat");
        }
    }

    #endregion
}
