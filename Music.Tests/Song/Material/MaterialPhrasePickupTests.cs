// AI: purpose=Verify MaterialPhrase StartOffsetTicks enables pickup phrase placement before target bar.
// AI: invariants=Tests assert deterministic offset application and correct event positioning.

using Music.Generator;
using Music.MyMidi;
using Music.Song.Material;
using Xunit;

namespace Music.Song.Material.Tests
{
    public class MaterialPhrasePickupTests
    {
        [Fact]
        public void StartOffsetTicks_DefaultsToZero()
        {
            var phrase = CreatePhrase(new List<PartTrackEvent>());

            Assert.Equal(0, phrase.StartOffsetTicks);
        }

        [Fact]
        public void ToPartTrack_NoOffset_PlacesEventsAtTargetBar()
        {
            var barTrack = CreateBarTrack();
            var events = new List<PartTrackEvent>
            {
                CreateEvent(36, absoluteTicks: 0),
                CreateEvent(38, absoluteTicks: 480)
            };
            var phrase = CreatePhrase(events, startOffsetTicks: 0);

            var result = phrase.ToPartTrack(barTrack, startBar: 5, midiProgramNumber: 255);

            long expectedBar5Start = barTrack.ToTick(5, 1.0m);
            Assert.Equal(expectedBar5Start, result.PartTrackNoteEvents[0].AbsoluteTimeTicks);
            Assert.Equal(expectedBar5Start + 480, result.PartTrackNoteEvents[1].AbsoluteTimeTicks);
        }

        [Fact]
        public void ToPartTrack_NegativeOffset_PlacesEventsBeforeTargetBar()
        {
            var barTrack = CreateBarTrack();
            long pickupOffset = -240; // 1/8 note before bar 1
            var events = new List<PartTrackEvent>
            {
                CreateEvent(36, absoluteTicks: 0)
            };
            var phrase = CreatePhrase(events, startOffsetTicks: pickupOffset);

            var result = phrase.ToPartTrack(barTrack, startBar: 5, midiProgramNumber: 255);

            long expectedBar5Start = barTrack.ToTick(5, 1.0m);
            long expectedEventTick = expectedBar5Start + pickupOffset;
            Assert.Equal(expectedEventTick, result.PartTrackNoteEvents[0].AbsoluteTimeTicks);
        }

        [Fact]
        public void ToPartTrack_PositiveOffset_PlacesEventsAfterTargetBarStart()
        {
            var barTrack = CreateBarTrack();
            long delayOffset = 240; // 1/8 note after bar 1
            var events = new List<PartTrackEvent>
            {
                CreateEvent(36, absoluteTicks: 0)
            };
            var phrase = CreatePhrase(events, startOffsetTicks: delayOffset);

            var result = phrase.ToPartTrack(barTrack, startBar: 3, midiProgramNumber: 255);

            long expectedBar3Start = barTrack.ToTick(3, 1.0m);
            long expectedEventTick = expectedBar3Start + delayOffset;
            Assert.Equal(expectedEventTick, result.PartTrackNoteEvents[0].AbsoluteTimeTicks);
        }

        [Fact]
        public void ToPartTrack_PickupPhrase_DeterministicPlacement()
        {
            var barTrack = CreateBarTrack();
            long pickupOffset = -120;
            var events = new List<PartTrackEvent>
            {
                CreateEvent(36, absoluteTicks: 0),
                CreateEvent(38, absoluteTicks: 480)
            };
            var phrase = CreatePhrase(events, startOffsetTicks: pickupOffset);

            var result1 = phrase.ToPartTrack(barTrack, startBar: 10, midiProgramNumber: 255);
            var result2 = phrase.ToPartTrack(barTrack, startBar: 10, midiProgramNumber: 255);

            Assert.Equal(result1.PartTrackNoteEvents[0].AbsoluteTimeTicks, 
                         result2.PartTrackNoteEvents[0].AbsoluteTimeTicks);
            Assert.Equal(result1.PartTrackNoteEvents[1].AbsoluteTimeTicks, 
                         result2.PartTrackNoteEvents[1].AbsoluteTimeTicks);
        }

        [Fact]
        public void FromPartTrack_WithStartOffsetTicks_PreservesOffset()
        {
            var partTrack = new PartTrack(new List<PartTrackEvent>())
            {
                MidiProgramNumber = 255
            };
            long expectedOffset = -360;

            var phrase = MaterialPhrase.FromPartTrack(
                partTrack,
                phraseNumber: 1,
                phraseId: "pickup1",
                name: "Pickup Phrase",
                description: "Test",
                barCount: 1,
                seed: 123,
                startOffsetTicks: expectedOffset);

            Assert.Equal(expectedOffset, phrase.StartOffsetTicks);
        }

        [Fact]
        public void FromPartTrack_WithoutStartOffsetTicks_DefaultsToZero()
        {
            var partTrack = new PartTrack(new List<PartTrackEvent>())
            {
                MidiProgramNumber = 255
            };

            var phrase = MaterialPhrase.FromPartTrack(
                partTrack,
                phraseNumber: 1,
                phraseId: "phrase1",
                name: "Regular Phrase",
                description: "Test",
                barCount: 1,
                seed: 123);

            Assert.Equal(0, phrase.StartOffsetTicks);
        }

        private static MaterialPhrase CreatePhrase(
            IReadOnlyList<PartTrackEvent> events,
            long startOffsetTicks = 0)
        {
            return new MaterialPhrase
            {
                PhraseNumber = 1,
                PhraseId = "test1",
                Name = "Test Phrase",
                Description = "Test",
                BarCount = 1,
                MidiProgramNumber = 255,
                Seed = 123,
                Events = events,
                StartOffsetTicks = startOffsetTicks
            };
        }

        private static BarTrack CreateBarTrack()
        {
            var barTrack = new BarTrack();
            var sectionTrack = new SectionTrack();
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 20);
            barTrack.RebuildFromTimingTrack(TimingTests.CreateTestTrackD1(), sectionTrack, totalBars: 20);
            return barTrack;
        }

        private static PartTrackEvent CreateEvent(int noteNumber, int absoluteTicks)
        {
            return new PartTrackEvent
            {
                AbsoluteTimeTicks = absoluteTicks,
                Type = PartTrackEventType.NoteOn,
                NoteNumber = noteNumber,
                NoteDurationTicks = 60,
                NoteOnVelocity = 100
            };
        }
    }
}
