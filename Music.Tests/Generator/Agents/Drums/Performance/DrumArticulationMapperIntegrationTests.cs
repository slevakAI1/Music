// AI: purpose=Integration verification for DrumArticulationMapper with existing drum candidate pipeline.
// AI: deps=Uses DrumCandidate, DrumArticulation, DrumArticulationMapper; verifies end-to-end flow.
// AI: change=Story 6.3 verification; demonstrates mapper usage pattern.

using FluentAssertions;
using Music.Generator.Agents.Drums;
using Music.Generator.Agents.Drums.Performance;
using Music.Generator.Groove;
using Xunit;

namespace Music.Tests.Generator.Agents.Drums.Performance
{
    /// <summary>
    /// Integration verification tests for DrumArticulationMapper.
    /// Demonstrates how the mapper integrates with existing drum candidate pipeline.
    /// Story 6.3: Implement Articulation Mapping - Integration Verification.
    /// </summary>
    public class DrumArticulationMapperIntegrationTests
    {
        [Fact]
        public void DrumCandidate_WithArticulation_CanBeMappedToMidiNote()
        {
            // Arrange: Create a typical drum candidate with articulation hint
            var candidate = DrumCandidate.CreateMinimal(
                operatorId: "TestOperator",
                role: "Snare",
                barNumber: 1,
                beat: 2.0m,
                strength: OnsetStrength.Backbeat,
                score: 0.8);

            var candidateWithArticulation = candidate with
            {
                ArticulationHint = DrumArticulation.Rimshot
            };

            // Act: Map the articulation to MIDI
            var mapping = DrumArticulationMapper.MapArticulation(
                candidateWithArticulation.ArticulationHint ?? DrumArticulation.None,
                candidateWithArticulation.Role);

            // Assert: Verify mapping is valid and matches expected GM2 note
            mapping.MidiNoteNumber.Should().Be(40, "Rimshot on snare should map to GM2 Electric Snare");
            mapping.Articulation.Should().Be(DrumArticulation.Rimshot);
            mapping.Role.Should().Be("Snare");
            mapping.IsFallback.Should().BeFalse("Rimshot has explicit GM2 mapping");
        }

        [Fact]
        public void DrumCandidate_WithoutArticulation_MapsToStandardNote()
        {
            // Arrange: Candidate with no articulation hint
            var candidate = DrumCandidate.CreateMinimal(
                operatorId: "TestOperator",
                role: "Kick",
                barNumber: 1,
                beat: 1.0m,
                strength: OnsetStrength.Downbeat,
                score: 1.0);

            // Act: Map with None articulation
            var mapping = DrumArticulationMapper.MapArticulation(
                candidate.ArticulationHint ?? DrumArticulation.None,
                candidate.Role);

            // Assert: Should use standard kick note
            mapping.MidiNoteNumber.Should().Be(36, "Standard kick note should be GM2 Acoustic Bass Drum");
            mapping.Articulation.Should().Be(DrumArticulation.None);
            mapping.Role.Should().Be("Kick");
            mapping.IsFallback.Should().BeFalse("Standard role mapping is not a fallback");
        }

        [Fact]
        public void MultipleCandidates_DifferentArticulations_AllMapCorrectly()
        {
            // Arrange: Simulated drum pattern with various articulations
            var pattern = new[]
            {
                (role: "Kick", articulation: DrumArticulation.None, expectedNote: 36),
                (role: "Snare", articulation: DrumArticulation.Rimshot, expectedNote: 40),
                (role: "Snare", articulation: DrumArticulation.SideStick, expectedNote: 37),
                (role: "ClosedHat", articulation: DrumArticulation.None, expectedNote: 42),
                (role: "OpenHat", articulation: DrumArticulation.OpenHat, expectedNote: 46),
                (role: "Crash", articulation: DrumArticulation.Crash, expectedNote: 49)
            };

            // Act & Assert: Map each candidate and verify
            foreach (var (role, articulation, expectedNote) in pattern)
            {
                var mapping = DrumArticulationMapper.MapArticulation(articulation, role);
                
                mapping.MidiNoteNumber.Should().Be(expectedNote,
                    $"{role} with {articulation} should map to MIDI note {expectedNote}");
                mapping.Articulation.Should().Be(articulation);
                mapping.Role.Should().Be(role);
            }
        }

        [Fact]
        public void FillPattern_WithArticulations_AllNotesValid()
        {
            // Arrange: Typical fill pattern (toms + crash ending with articulation)
            var fillPattern = new[]
            {
                ("Tom1", DrumArticulation.None),
                ("Tom2", DrumArticulation.None),
                ("FloorTom", DrumArticulation.None),
                ("Crash", DrumArticulation.Crash)
            };

            // Act: Map all fill candidates
            var mappings = new System.Collections.Generic.List<DrumArticulationMapper.ArticulationMappingResult>();
            foreach (var (role, articulation) in fillPattern)
            {
                mappings.Add(DrumArticulationMapper.MapArticulation(articulation, role));
            }

            // Assert: All mappings valid and playable
            mappings.Should().AllSatisfy(m =>
            {
                m.MidiNoteNumber.Should().BeInRange(0, 127, "All MIDI notes must be valid");
                m.Role.Should().NotBeNullOrEmpty("Role should be preserved");
            });

            // Verify tom notes are distinct
            var tomNotes = mappings.Take(3).Select(m => m.MidiNoteNumber).ToList();
            tomNotes.Should().OnlyHaveUniqueItems("Tom notes should be distinct");

            // Verify crash ending
            mappings.Last().MidiNoteNumber.Should().Be(49, "Crash should be GM2 Crash Cymbal 1");
        }

        [Fact]
        public void DrumCandidate_PreservesVelocityAndTimingHints_AfterArticulationMapping()
        {
            // Arrange: Candidate with velocity and timing hints already set by shapers
            var candidate = DrumCandidate.CreateMinimal(
                operatorId: "TestOperator",
                role: "Snare",
                barNumber: 1,
                beat: 2.0m,
                strength: OnsetStrength.Backbeat,
                score: 0.8);

            var shapedCandidate = candidate with
            {
                VelocityHint = 105,
                TimingHint = -5,
                ArticulationHint = DrumArticulation.Rimshot
            };

            // Act: Map articulation (this should not affect velocity/timing)
            var mapping = DrumArticulationMapper.MapArticulation(
                shapedCandidate.ArticulationHint ?? DrumArticulation.None,
                shapedCandidate.Role);

            // Assert: Verify articulation mapped correctly
            mapping.MidiNoteNumber.Should().Be(40);

            // Verify candidate hints unchanged (mapper is read-only)
            shapedCandidate.VelocityHint.Should().Be(105, "Velocity hint should be preserved");
            shapedCandidate.TimingHint.Should().Be(-5, "Timing hint should be preserved");
            shapedCandidate.ArticulationHint.Should().Be(DrumArticulation.Rimshot, "Articulation hint should be preserved");
        }

        [Fact]
        public void PopRockPattern_AllArticulations_MapToPlayableMidi()
        {
            // Arrange: Typical Pop Rock drum pattern with varied articulations
            var popRockPattern = new[]
            {
                // Verse: simple with side stick
                ("Kick", DrumArticulation.None),
                ("Snare", DrumArticulation.SideStick),
                ("ClosedHat", DrumArticulation.None),
                
                // Chorus: powerful with rimshots and crashes
                ("Kick", DrumArticulation.None),
                ("Snare", DrumArticulation.Rimshot),
                ("Crash", DrumArticulation.Crash),
                ("OpenHat", DrumArticulation.OpenHat),
                
                // Fill: toms
                ("Tom1", DrumArticulation.None),
                ("FloorTom", DrumArticulation.None)
            };

            // Act & Assert: All patterns map to valid MIDI
            foreach (var (role, articulation) in popRockPattern)
            {
                var mapping = DrumArticulationMapper.MapArticulation(articulation, role);
                
                mapping.MidiNoteNumber.Should().BeInRange(0, 127,
                    $"Pop Rock pattern: {role} + {articulation} should produce valid MIDI");
                mapping.IsFallback.Should().BeFalse(
                    $"All Pop Rock pattern elements should have explicit mappings");
            }
        }
    }
}

