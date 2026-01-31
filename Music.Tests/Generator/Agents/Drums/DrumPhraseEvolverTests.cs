// AI: purpose=Verify DrumPhraseEvolver applies bounded, deterministic evolution to MaterialPhrase event data.
// AI: invariants=Tests assert determinism, essential hit preservation, and evolution operator effects.

using Music.Generator;
using Music.MyMidi;
using Music.Song.Material;
using Xunit;

namespace Music.Generator.Agents.Drums.Tests
{
    public class DrumPhraseEvolverTests
    {
        [Fact]
        public void Evolve_NoEvolution_ReturnsOriginalInstance()
        {
            var phrase = CreatePhrase(new List<PartTrackEvent>());
            var evolver = new DrumPhraseEvolver(seed: 42);

            var result = evolver.Evolve(phrase, new DrumPhraseEvolutionParams(), CreateBarTrack());

            Assert.Same(phrase, result);
        }

        [Fact]
        public void ApplySimplification_RemovesNonEssentialHits()
        {
            var phrase = CreatePhrase(new List<PartTrackEvent>
            {
                CreateEvent(36, 0, 90),
                CreateEvent(38, 120, 100),
                CreateEvent(42, 240, 70),
                CreateEvent(42, 360, 70)
            });
            var evolver = new DrumPhraseEvolver(seed: 7);

            var result = evolver.Evolve(phrase, new DrumPhraseEvolutionParams { Simplification = 1.0 }, CreateBarTrack());

            Assert.True(result.Events.Count < phrase.Events.Count);
        }

        [Fact]
        public void ApplySimplification_PreservesKickHit()
        {
            var phrase = CreatePhrase(new List<PartTrackEvent>
            {
                CreateEvent(36, 0, 100),
                CreateEvent(42, 120, 70)
            });
            var evolver = new DrumPhraseEvolver(seed: 9);

            var result = evolver.Evolve(phrase, new DrumPhraseEvolutionParams { Simplification = 1.0 }, CreateBarTrack());

            Assert.Contains(result.Events, e => e.NoteNumber == 36);
        }

        [Fact]
        public void ApplySimplification_PreservesMainSnare()
        {
            var phrase = CreatePhrase(new List<PartTrackEvent>
            {
                CreateEvent(38, 120, 110),
                CreateEvent(42, 240, 70)
            });
            var evolver = new DrumPhraseEvolver(seed: 11);

            var result = evolver.Evolve(phrase, new DrumPhraseEvolutionParams { Simplification = 1.0 }, CreateBarTrack());

            Assert.Contains(result.Events, e => e.NoteNumber == 38 && e.NoteOnVelocity > 80);
        }

        [Fact]
        public void AddGhostNotes_AddsSnareGhost()
        {
            var phrase = CreatePhrase(new List<PartTrackEvent>
            {
                CreateEvent(38, 240, 110)
            });
            var evolver = new DrumPhraseEvolver(seed: 13);

            var result = evolver.Evolve(phrase, new DrumPhraseEvolutionParams { GhostIntensity = 1.0 }, CreateBarTrack());

            Assert.Contains(result.Events, e => e.NoteNumber == 38 && e.NoteOnVelocity < 80 && e.AbsoluteTimeTicks < 240);
        }

        [Fact]
        public void ApplyHatVariation_OpensClosedHat()
        {
            var phrase = CreatePhrase(new List<PartTrackEvent>
            {
                CreateEvent(42, 120, 80)
            });
            var evolver = new DrumPhraseEvolver(seed: 17);

            var result = evolver.Evolve(phrase, new DrumPhraseEvolutionParams { HatVariation = 1.0 }, CreateBarTrack());

            Assert.Contains(result.Events, e => e.NoteNumber == 46);
        }

        [Fact]
        public void ApplyRandomVariation_ChangesTimingOrVelocity()
        {
            var phrase = CreatePhrase(new List<PartTrackEvent>
            {
                CreateEvent(45, 120, 90)
            });
            var evolver = new DrumPhraseEvolver(seed: 23);

            var result = evolver.Evolve(phrase, new DrumPhraseEvolutionParams { RandomVariation = 1.0 }, CreateBarTrack());
            var original = phrase.Events[0];
            var evolved = result.Events[0];

            Assert.True(evolved.NoteOnVelocity != original.NoteOnVelocity || evolved.AbsoluteTimeTicks != original.AbsoluteTimeTicks);
        }

        [Fact]
        public void Evolve_SameSeed_IsDeterministic()
        {
            var phrase = CreatePhrase(new List<PartTrackEvent>
            {
                CreateEvent(36, 0, 100),
                CreateEvent(42, 240, 70)
            }, phraseId: "phraseA");
            var evolution = new DrumPhraseEvolutionParams { RandomVariation = 0.7, HatVariation = 0.5 };
            var barTrack = CreateBarTrack();
            var evolver = new DrumPhraseEvolver(seed: 31);

            var first = evolver.Evolve(phrase, evolution, barTrack).Events
                .Select(e => (e.AbsoluteTimeTicks, e.NoteNumber, e.NoteOnVelocity))
                .ToArray();
            var second = evolver.Evolve(phrase, evolution, barTrack).Events
                .Select(e => (e.AbsoluteTimeTicks, e.NoteNumber, e.NoteOnVelocity))
                .ToArray();

            Assert.Equal(first, second);
        }

        [Fact]
        public void Evolve_DifferentSeed_ProducesDifferentPhraseId()
        {
            var phrase = CreatePhrase(new List<PartTrackEvent>
            {
                CreateEvent(36, 0, 100)
            }, phraseId: "phraseB");
            var evolution = new DrumPhraseEvolutionParams { RandomVariation = 1.0 };
            var barTrack = CreateBarTrack();

            var first = new DrumPhraseEvolver(seed: 41).Evolve(phrase, evolution, barTrack);
            var second = new DrumPhraseEvolver(seed: 99).Evolve(phrase, evolution, barTrack);

            Assert.NotEqual(first.PhraseId, second.PhraseId);
        }

        private static MaterialPhrase CreatePhrase(IReadOnlyList<PartTrackEvent> events, string phraseId = "phrase1")
        {
            return new MaterialPhrase
            {
                PhraseNumber = 1,
                PhraseId = phraseId,
                Name = "Test Phrase",
                Description = "Test",
                BarCount = 1,
                MidiProgramNumber = 255,
                Seed = 123,
                Events = events
            };
        }

        private static BarTrack CreateBarTrack()
        {
            var barTrack = new BarTrack();
            barTrack.RebuildFromTimingTrack(TimingTests.CreateTestTrackD1(), totalBars: 1);
            return barTrack;
        }

        private static PartTrackEvent CreateEvent(int noteNumber, int absoluteTicks, int velocity)
        {
            return new PartTrackEvent
            {
                AbsoluteTimeTicks = absoluteTicks,
                Type = PartTrackEventType.NoteOn,
                NoteNumber = noteNumber,
                NoteDurationTicks = 60,
                NoteOnVelocity = velocity
            };
        }
    }
}
