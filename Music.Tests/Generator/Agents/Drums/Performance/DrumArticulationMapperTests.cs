// AI: purpose=Unit tests for DrumArticulationMapper; verifies GM2 mappings, fallback behavior, determinism, null-safety.
// AI: deps=Tests DrumArticulationMapper; uses xUnit, FluentAssertions.
// AI: change=Story 6.3; extend tests when adding new articulations or custom kit mappings.

using FluentAssertions;
using Music.Generator.Agents.Drums;
using Music.Generator.Agents.Drums.Performance;
using Xunit;

namespace Music.Tests.Generator.Agents.Drums.Performance
{
    /// <summary>
    /// Unit tests for DrumArticulationMapper.
    /// Story 6.3: Implement Articulation Mapping.
    /// </summary>
    public class DrumArticulationMapperTests
    {
        #region Known Articulation Mappings (GM2 Standard)

        [Theory]
        [InlineData(DrumArticulation.Rimshot, "Snare", 40)] // GM2: Electric Snare (rimshot approximation)
        [InlineData(DrumArticulation.SideStick, "Snare", 37)] // GM2: Side Stick
        [InlineData(DrumArticulation.OpenHat, "ClosedHat", 46)] // GM2: Open Hi-Hat
        [InlineData(DrumArticulation.Crash, "Crash", 49)] // GM2: Crash Cymbal 1
        [InlineData(DrumArticulation.Ride, "Ride", 51)] // GM2: Ride Cymbal 1
        [InlineData(DrumArticulation.RideBell, "Ride", 53)] // GM2: Ride Bell
        public void MapArticulation_KnownArticulation_ReturnsExpectedMidiNote(
            DrumArticulation articulation, string role, int expectedMidiNote)
        {
            // Act
            var result = DrumArticulationMapper.MapArticulation(articulation, role);

            // Assert
            result.MidiNoteNumber.Should().Be(expectedMidiNote, $"GM2 mapping for {articulation} should be {expectedMidiNote}");
            result.Articulation.Should().Be(articulation);
            result.Role.Should().Be(role);
            result.IsFallback.Should().BeFalse($"{articulation} has explicit GM2 mapping");
            result.ArticulationMetadata.Should().Be(articulation.ToString());
        }

        #endregion

        #region Standard Role Mappings (No Articulation)

        [Theory]
        [InlineData("Kick", 36)]        // GM2: Acoustic Bass Drum
        [InlineData("Snare", 38)]       // GM2: Acoustic Snare
        [InlineData("ClosedHat", 42)]   // GM2: Closed Hi-Hat
        [InlineData("OpenHat", 46)]     // GM2: Open Hi-Hat
        [InlineData("Crash", 49)]       // GM2: Crash Cymbal 1
        [InlineData("Crash2", 57)]      // GM2: Crash Cymbal 2
        [InlineData("Ride", 51)]        // GM2: Ride Cymbal 1
        [InlineData("Tom1", 48)]        // GM2: Hi Mid Tom
        [InlineData("Tom2", 47)]        // GM2: Low Mid Tom
        [InlineData("FloorTom", 41)]    // GM2: Low Floor Tom
        [InlineData("RideBell", 53)]    // GM2: Ride Bell
        public void MapArticulation_NoneArticulation_ReturnsStandardRoleNote(string role, int expectedMidiNote)
        {
            // Act
            var result = DrumArticulationMapper.MapArticulation(DrumArticulation.None, role);

            // Assert
            result.MidiNoteNumber.Should().Be(expectedMidiNote, $"Standard note for {role} should be {expectedMidiNote}");
            result.Articulation.Should().Be(DrumArticulation.None);
            result.Role.Should().Be(role);
            result.IsFallback.Should().BeFalse("None articulation uses standard role mapping");
            result.ArticulationMetadata.Should().BeNull("None articulation has no metadata");
        }

        #endregion

        #region Fallback Behavior

        [Fact]
        public void MapArticulation_UnknownArticulation_FallbackToStandardNote()
        {
            // Arrange: Future articulation value not yet mapped
            var futureArticulation = (DrumArticulation)99;

            // Act
            var result = DrumArticulationMapper.MapArticulation(futureArticulation, "Snare");

            // Assert
            result.MidiNoteNumber.Should().Be(38, "Should fallback to standard snare note");
            result.Articulation.Should().Be(futureArticulation);
            result.Role.Should().Be("Snare");
            result.IsFallback.Should().BeTrue("Unknown articulation requires fallback");
            result.ArticulationMetadata.Should().Contain("Fallback");
        }

        [Fact]
        public void MapArticulation_UnknownRole_FallbackToSafeMidiNote()
        {
            // Arrange
            var unknownRole = "FutureDrumRole";

            // Act
            var result = DrumArticulationMapper.MapArticulation(DrumArticulation.None, unknownRole);

            // Assert
            result.MidiNoteNumber.Should().Be(38, "Should fallback to snare as safe default");
            result.Articulation.Should().Be(DrumArticulation.None);
            result.Role.Should().Be(unknownRole);
            result.IsFallback.Should().BeTrue("Unknown role requires fallback");
            result.ArticulationMetadata.Should().Contain("UnknownRole");
        }

        [Theory]
        [InlineData(DrumArticulation.Flam, "Snare", 38)] // Flam has no specific GM2 note, fallback to standard snare
        [InlineData(DrumArticulation.CrashChoke, "Crash", 49)] // CrashChoke uses standard crash (choke is duration-based)
        public void MapArticulation_ArticulationWithoutSpecificNote_FallbackToStandardRole(
            DrumArticulation articulation, string role, int expectedMidiNote)
        {
            // Act
            var result = DrumArticulationMapper.MapArticulation(articulation, role);

            // Assert
            result.MidiNoteNumber.Should().Be(expectedMidiNote, $"{articulation} should fallback to standard {role} note");
            result.Articulation.Should().Be(articulation);
            result.Role.Should().Be(role);
            result.ArticulationMetadata.Should().NotBeNullOrEmpty($"{articulation} should preserve metadata");
        }

        #endregion

        #region Null Safety and Edge Cases

        [Fact]
        public void MapArticulation_NullRole_GracefulFallback()
        {
            // Act
            var result = DrumArticulationMapper.MapArticulation(DrumArticulation.Rimshot, null);

            // Assert
            result.MidiNoteNumber.Should().BeInRange(0, 127, "Should return valid MIDI note");
            result.IsFallback.Should().BeTrue("Null role requires fallback");
            result.Role.Should().Be("Unknown");
            result.ArticulationMetadata.Should().Contain("Fallback");
        }

        [Fact]
        public void MapArticulation_EmptyRole_GracefulFallback()
        {
            // Act
            var result = DrumArticulationMapper.MapArticulation(DrumArticulation.None, "");

            // Assert
            result.MidiNoteNumber.Should().BeInRange(0, 127, "Should return valid MIDI note");
            result.IsFallback.Should().BeTrue("Empty role requires fallback");
        }

        [Fact]
        public void MapArticulation_WhitespaceRole_GracefulFallback()
        {
            // Act
            var result = DrumArticulationMapper.MapArticulation(DrumArticulation.Crash, "   ");

            // Assert
            result.MidiNoteNumber.Should().BeInRange(0, 127, "Should return valid MIDI note");
            result.IsFallback.Should().BeTrue("Whitespace role requires fallback");
        }

        #endregion

        #region Determinism

        [Fact]
        public void MapArticulation_SameInputs_SameOutput()
        {
            // Arrange
            var articulation = DrumArticulation.Rimshot;
            var role = "Snare";

            // Act
            var result1 = DrumArticulationMapper.MapArticulation(articulation, role);
            var result2 = DrumArticulationMapper.MapArticulation(articulation, role);

            // Assert
            result1.MidiNoteNumber.Should().Be(result2.MidiNoteNumber, "Determinism: same inputs → same MIDI note");
            result1.Articulation.Should().Be(result2.Articulation);
            result1.Role.Should().Be(result2.Role);
            result1.IsFallback.Should().Be(result2.IsFallback);
            result1.ArticulationMetadata.Should().Be(result2.ArticulationMetadata);
        }

        [Theory]
        [InlineData("Snare", DrumArticulation.None, 38)]
        [InlineData("Snare", DrumArticulation.Rimshot, 40)]
        [InlineData("Snare", DrumArticulation.SideStick, 37)]
        public void MapArticulation_DifferentArticulations_DifferentOutputs(
            string role, DrumArticulation articulation, int expectedMidiNote)
        {
            // Act
            var result = DrumArticulationMapper.MapArticulation(articulation, role);

            // Assert
            result.MidiNoteNumber.Should().Be(expectedMidiNote, $"{articulation} for {role} should map to {expectedMidiNote}");
        }

        #endregion

        #region MIDI Note Range Validation

        [Fact]
        public void MapArticulation_AllKnownMappings_ProduceValidMidiNotes()
        {
            // Arrange: Test all articulations with standard roles
            var articulations = new[]
            {
                DrumArticulation.None, DrumArticulation.Rimshot, DrumArticulation.SideStick,
                DrumArticulation.OpenHat, DrumArticulation.Crash, DrumArticulation.Ride,
                DrumArticulation.RideBell, DrumArticulation.CrashChoke, DrumArticulation.Flam
            };

            var roles = new[] { "Kick", "Snare", "ClosedHat", "OpenHat", "Crash", "Ride", "Tom1", "FloorTom" };

            // Act & Assert
            foreach (var articulation in articulations)
            {
                foreach (var role in roles)
                {
                    var result = DrumArticulationMapper.MapArticulation(articulation, role);
                    result.MidiNoteNumber.Should().BeInRange(0, 127,
                        $"{articulation} + {role} should produce valid MIDI note");
                }
            }
        }

        #endregion

        #region Helper Method Tests

        [Theory]
        [InlineData("Kick", 36)]
        [InlineData("Snare", 38)]
        [InlineData("ClosedHat", 42)]
        [InlineData("Ride", 51)]
        public void GetStandardNoteForRole_KnownRole_ReturnsCorrectNote(string role, int expectedNote)
        {
            // Act
            var note = DrumArticulationMapper.GetStandardNoteForRole(role);

            // Assert
            note.Should().Be(expectedNote, $"Standard note for {role} should be {expectedNote}");
        }

        [Fact]
        public void GetStandardNoteForRole_UnknownRole_ReturnsNull()
        {
            // Act
            var note = DrumArticulationMapper.GetStandardNoteForRole("UnknownRole");

            // Assert
            note.Should().BeNull("Unknown roles should return null");
        }

        [Fact]
        public void GetStandardNoteForRole_NullRole_ReturnsNull()
        {
            // Act
            var note = DrumArticulationMapper.GetStandardNoteForRole(null);

            // Assert
            note.Should().BeNull("Null role should return null");
        }

        [Fact]
        public void GetStandardNoteForRole_EmptyRole_ReturnsNull()
        {
            // Act
            var note = DrumArticulationMapper.GetStandardNoteForRole("");

            // Assert
            note.Should().BeNull("Empty role should return null");
        }

        #endregion

        #region Integration Scenario Tests

        [Fact]
        public void MapArticulation_TypicalDrumPattern_AllRolesMapCorrectly()
        {
            // Arrange: Typical drum pattern roles
            var pattern = new (string role, DrumArticulation articulation)[]
            {
                ("Kick", DrumArticulation.None),
                ("Snare", DrumArticulation.None),
                ("ClosedHat", DrumArticulation.None),
                ("Snare", DrumArticulation.Rimshot),
                ("Crash", DrumArticulation.Crash)
            };

            // Act & Assert
            foreach (var (role, articulation) in pattern)
            {
                var result = DrumArticulationMapper.MapArticulation(articulation, role);
                result.MidiNoteNumber.Should().BeInRange(0, 127, $"{role} + {articulation} should be valid");
                result.Role.Should().Be(role);
                result.Articulation.Should().Be(articulation);
            }
        }

        [Fact]
        public void MapArticulation_FillPattern_TomsAndCymbalsMapCorrectly()
        {
            // Arrange: Typical fill pattern (toms + crash ending)
            var fillPattern = new (string role, DrumArticulation articulation)[]
            {
                ("Tom1", DrumArticulation.None),
                ("Tom2", DrumArticulation.None),
                ("FloorTom", DrumArticulation.None),
                ("Crash", DrumArticulation.Crash)
            };

            // Act & Assert
            foreach (var (role, articulation) in fillPattern)
            {
                var result = DrumArticulationMapper.MapArticulation(articulation, role);
                result.MidiNoteNumber.Should().BeInRange(0, 127, $"Fill: {role} + {articulation} should be valid");
                result.IsFallback.Should().BeFalse($"Standard fill roles should not require fallback");
            }
        }

        #endregion
    }
}

