// AI: purpose=Golden test for DrumTrackGeneratorNew output freeze; validates no behavior changes during refactoring.
// AI: invariants=Exact MIDI event match (absoluteTick, noteNumber, velocity, duration) for PopRockBasic preset + 8 bars.
// AI: deps=GrooveSetupFactory.BuildPopRockBasicGrooveForTestSong; test must pass before Story G1-G8 refactor starts.
// AI: change=Update golden data only when drum generation behavior intentionally changes; snapshots protect refactors.

using FluentAssertions;
using Music.Generator;
using Music.Generator.Groove;
using Music.MyMidi;

namespace Music.Tests.Generator.Drums
{
    public class DrumTrackGeneratorGoldenTests
    {
        private const int TestBars = 8;
        private const int MidiDrumProgramNumber = 0; // General MIDI drum kit

        private static SectionTrack CreateTestSectionTrack()
        {
            var track = new SectionTrack();
            track.Add(MusicConstants.eSectionType.Verse, barCount: 4);
            track.Add(MusicConstants.eSectionType.Chorus, barCount: 4);
            return track;
        }

        private static BarTrack CreateTestBarTrack()
        {
            var timingTrack = new Timingtrack();
            timingTrack.Add(new TimingEvent
            {
                StartBar = 1,
                Numerator = 4,
                Denominator = 4
            });

            var barTrack = new BarTrack();
            barTrack.RebuildFromTimingTrack(timingTrack, TestBars);
            return barTrack;
        }

        [Fact]
        public void PopRockBasic_8Bars_ProducesExpectedOutput()
        {
            // Arrange: Create complete groove definition using test setup factory
            var sectionTrack = CreateTestSectionTrack();
            var groovePreset = GrooveSetupFactory.BuildPopRockBasicGrooveForTestSong(
                sectionTrack,
                out var segmentProfiles,
                beatsPerBar: 4);

            var barTrack = CreateTestBarTrack();

            // Act: Generate drum track
            var result = DrumTrackGenerator.Generate(
                barTrack,
                sectionTrack,
                segmentProfiles,
                groovePreset,
                totalBars: TestBars,
                midiProgramNumber: MidiDrumProgramNumber);

            // Assert: Extract and sort all note events
            var actualEvents = result.PartTrackNoteEvents
                .OrderBy(e => e.AbsoluteTimeTicks)
                .ThenBy(e => e.NoteNumber)
                .Select(e => new DrumEventSnapshot(
                    e.AbsoluteTimeTicks,
                    e.NoteNumber,
                    e.NoteOnVelocity,
                    e.NoteDurationTicks))
                .ToList();

            // Golden snapshot: expected output for PopRockBasic preset, 8 bars
            var expectedEvents = GetGoldenSnapshot();

            // Assert exact match
            actualEvents.Should().HaveCount(expectedEvents.Length,
                "drum event count must match golden snapshot");

            actualEvents.Should().Equal(expectedEvents,
                "all drum events (tick, note, velocity, duration) must match golden snapshot exactly");
        }

        /// <summary>
        /// Golden snapshot data: frozen output from current implementation (96 events over 8 bars).
        /// Pattern: Kick (36) on beats 1 & 3, Snare (38) on beats 2 & 4, ClosedHat (42) on eighths.
        /// DO NOT CHANGE unless drum generation behavior intentionally changes.
        /// </summary>
        private static DrumEventSnapshot[] GetGoldenSnapshot()
        {
            // Total events: 96 (PopRockBasic preset, 8 bars, 4/4 time)
            return new[]
            {
                new DrumEventSnapshot(0L, 36, 100, 60),
                new DrumEventSnapshot(0L, 42, 100, 60),
                new DrumEventSnapshot(240L, 42, 100, 60),
                new DrumEventSnapshot(480L, 38, 100, 60),
                new DrumEventSnapshot(480L, 42, 100, 60),
                new DrumEventSnapshot(720L, 42, 100, 60),
                new DrumEventSnapshot(960L, 36, 100, 60),
                new DrumEventSnapshot(960L, 42, 100, 60),
                new DrumEventSnapshot(1200L, 42, 100, 60),
                new DrumEventSnapshot(1440L, 38, 100, 60),
                new DrumEventSnapshot(1440L, 42, 100, 60),
                new DrumEventSnapshot(1680L, 42, 100, 60),
                new DrumEventSnapshot(1920L, 36, 100, 60),
                new DrumEventSnapshot(1920L, 42, 100, 60),
                new DrumEventSnapshot(2160L, 42, 100, 60),
                new DrumEventSnapshot(2400L, 38, 100, 60),
                new DrumEventSnapshot(2400L, 42, 100, 60),
                new DrumEventSnapshot(2640L, 42, 100, 60),
                new DrumEventSnapshot(2880L, 36, 100, 60),
                new DrumEventSnapshot(2880L, 42, 100, 60),
                new DrumEventSnapshot(3120L, 42, 100, 60),
                new DrumEventSnapshot(3360L, 38, 100, 60),
                new DrumEventSnapshot(3360L, 42, 100, 60),
                new DrumEventSnapshot(3600L, 42, 100, 60),
                new DrumEventSnapshot(3840L, 36, 100, 60),
                new DrumEventSnapshot(3840L, 42, 100, 60),
                new DrumEventSnapshot(4080L, 42, 100, 60),
                new DrumEventSnapshot(4320L, 38, 100, 60),
                new DrumEventSnapshot(4320L, 42, 100, 60),
                new DrumEventSnapshot(4560L, 42, 100, 60),
                new DrumEventSnapshot(4800L, 36, 100, 60),
                new DrumEventSnapshot(4800L, 42, 100, 60),
                new DrumEventSnapshot(5040L, 42, 100, 60),
                new DrumEventSnapshot(5280L, 38, 100, 60),
                new DrumEventSnapshot(5280L, 42, 100, 60),
                new DrumEventSnapshot(5520L, 42, 100, 60),
                new DrumEventSnapshot(5760L, 36, 100, 60),
                new DrumEventSnapshot(5760L, 42, 100, 60),
                new DrumEventSnapshot(6000L, 42, 100, 60),
                new DrumEventSnapshot(6240L, 38, 100, 60),
                new DrumEventSnapshot(6240L, 42, 100, 60),
                new DrumEventSnapshot(6480L, 42, 100, 60),
                new DrumEventSnapshot(6720L, 36, 100, 60),
                new DrumEventSnapshot(6720L, 42, 100, 60),
                new DrumEventSnapshot(6960L, 42, 100, 60),
                new DrumEventSnapshot(7200L, 38, 100, 60),
                new DrumEventSnapshot(7200L, 42, 100, 60),
                new DrumEventSnapshot(7440L, 42, 100, 60),
                new DrumEventSnapshot(7680L, 36, 100, 60),
                new DrumEventSnapshot(7680L, 42, 100, 60),
                new DrumEventSnapshot(7920L, 42, 100, 60),
                new DrumEventSnapshot(8160L, 38, 100, 60),
                new DrumEventSnapshot(8160L, 42, 100, 60),
                new DrumEventSnapshot(8400L, 42, 100, 60),
                new DrumEventSnapshot(8640L, 36, 100, 60),
                new DrumEventSnapshot(8640L, 42, 100, 60),
                new DrumEventSnapshot(8880L, 42, 100, 60),
                new DrumEventSnapshot(9120L, 38, 100, 60),
                new DrumEventSnapshot(9120L, 42, 100, 60),
                new DrumEventSnapshot(9360L, 42, 100, 60),
                new DrumEventSnapshot(9600L, 36, 100, 60),
                new DrumEventSnapshot(9600L, 42, 100, 60),
                new DrumEventSnapshot(9840L, 42, 100, 60),
                new DrumEventSnapshot(10080L, 38, 100, 60),
                new DrumEventSnapshot(10080L, 42, 100, 60),
                new DrumEventSnapshot(10320L, 42, 100, 60),
                new DrumEventSnapshot(10560L, 36, 100, 60),
                new DrumEventSnapshot(10560L, 42, 100, 60),
                new DrumEventSnapshot(10800L, 42, 100, 60),
                new DrumEventSnapshot(11040L, 38, 100, 60),
                new DrumEventSnapshot(11040L, 42, 100, 60),
                new DrumEventSnapshot(11280L, 42, 100, 60),
                new DrumEventSnapshot(11520L, 36, 100, 60),
                new DrumEventSnapshot(11520L, 42, 100, 60),
                new DrumEventSnapshot(11760L, 42, 100, 60),
                new DrumEventSnapshot(12000L, 38, 100, 60),
                new DrumEventSnapshot(12000L, 42, 100, 60),
                new DrumEventSnapshot(12240L, 42, 100, 60),
                new DrumEventSnapshot(12480L, 36, 100, 60),
                new DrumEventSnapshot(12480L, 42, 100, 60),
                new DrumEventSnapshot(12720L, 42, 100, 60),
                new DrumEventSnapshot(12960L, 38, 100, 60),
                new DrumEventSnapshot(12960L, 42, 100, 60),
                new DrumEventSnapshot(13200L, 42, 100, 60),
                new DrumEventSnapshot(13440L, 36, 100, 60),
                new DrumEventSnapshot(13440L, 42, 100, 60),
                new DrumEventSnapshot(13680L, 42, 100, 60),
                new DrumEventSnapshot(13920L, 38, 100, 60),
                new DrumEventSnapshot(13920L, 42, 100, 60),
                new DrumEventSnapshot(14160L, 42, 100, 60),
                new DrumEventSnapshot(14400L, 36, 100, 60),
                new DrumEventSnapshot(14400L, 42, 100, 60),
                new DrumEventSnapshot(14640L, 42, 100, 60),
                new DrumEventSnapshot(14880L, 38, 100, 60),
                new DrumEventSnapshot(14880L, 42, 100, 60),
                new DrumEventSnapshot(15120L, 42, 100, 60),
            };
        }

        /// <summary>
        /// Lightweight snapshot record for drum MIDI events.
        /// </summary>
        private record DrumEventSnapshot(
            long AbsoluteTick,
            int NoteNumber,
            int Velocity,
            int Duration);
    }
}
