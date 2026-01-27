// AI: purpose=Test RNG seed variation to ensure different seeds produce different drum tracks.
// AI: invariants=Each test run clears DebugTrace.txt before execution.
// AI: deps=Rng, TestDesigns, Generator, SongContext, Tracer.

using Music;
using Music.Generator;
using Music.MyMidi;
using Xunit;

namespace Music.Tests.Randomization
{
    public class RngSeedVariationTests
    {
        private const string DebugTraceFile = @"C:\Users\sleva\source\repos\Music\Music.Tests\Errors\DebugTrace.txt";

        [Fact]
        public void TwoDifferentSeeds_ShouldProduceDifferentDrumTracks()
        {
            // Clear debug trace
            File.WriteAllText(DebugTraceFile, string.Empty);
            Tracer.DebugTrace("=== TEST START: TwoDifferentSeeds_ShouldProduceDifferentDrumTracks ===");

            // Generate with seed 12345
            Tracer.DebugTrace("--- Generating drum track with seed 12345 ---");
            Rng.ResetRngStats();
            Rng.Initialize(12345);
            var songContext1 = new SongContext();
            TestDesigns.SetTestDesignD1(songContext1);
            var track1 = Music.Generator.Generator.Generate(songContext1);
            Tracer.DebugTrace($"Track1: {track1.PartTrackNoteEvents.Count} notes generated");
            Rng.LogRngStats("After Track1");

            // Generate with seed 99999
            Tracer.DebugTrace("--- Generating drum track with seed 99999 ---");
            Rng.ResetRngStats();
            Rng.Initialize(99999);
            var songContext2 = new SongContext();
            TestDesigns.SetTestDesignD1(songContext2);
            var track2 = Music.Generator.Generator.Generate(songContext2);
            Tracer.DebugTrace($"Track2: {track2.PartTrackNoteEvents.Count} notes generated");
            Rng.LogRngStats("After Track2");

            // Compare tracks
            Tracer.DebugTrace($"--- Comparison ---");
            Tracer.DebugTrace($"Track1 note count: {track1.PartTrackNoteEvents.Count}");
            Tracer.DebugTrace($"Track2 note count: {track2.PartTrackNoteEvents.Count}");

            // Log first 10 notes from each track
            Tracer.DebugTrace("Track1 first 10 notes:");
            for (int i = 0; i < Math.Min(10, track1.PartTrackNoteEvents.Count); i++)
            {
                var note = track1.PartTrackNoteEvents[i];
                Tracer.DebugTrace($"  Note {i}: AbsoluteTimeTicks={note.AbsoluteTimeTicks}, NoteNumber={note.NoteNumber}, NoteOnVelocity={note.NoteOnVelocity}");
            }

            Tracer.DebugTrace("Track2 first 10 notes:");
            for (int i = 0; i < Math.Min(10, track2.PartTrackNoteEvents.Count); i++)
            {
                var note = track2.PartTrackNoteEvents[i];
                Tracer.DebugTrace($"  Note {i}: AbsoluteTimeTicks={note.AbsoluteTimeTicks}, NoteNumber={note.NoteNumber}, NoteOnVelocity={note.NoteOnVelocity}");
            }

            // Assert tracks are different (at minimum, some notes should differ)
            bool anyDifference = false;
            int minCount = Math.Min(track1.PartTrackNoteEvents.Count, track2.PartTrackNoteEvents.Count);
            for (int i = 0; i < minCount; i++)
            {
                if (track1.PartTrackNoteEvents[i].AbsoluteTimeTicks != track2.PartTrackNoteEvents[i].AbsoluteTimeTicks ||
                    track1.PartTrackNoteEvents[i].NoteNumber != track2.PartTrackNoteEvents[i].NoteNumber ||
                    track1.PartTrackNoteEvents[i].NoteOnVelocity != track2.PartTrackNoteEvents[i].NoteOnVelocity)
                {
                    anyDifference = true;
                    Tracer.DebugTrace($"First difference found at note index {i}");
                    break;
                }
            }

            Tracer.DebugTrace($"Tracks are different: {anyDifference}");
            Tracer.DebugTrace("=== TEST END ===");

            Assert.True(anyDifference || track1.PartTrackNoteEvents.Count != track2.PartTrackNoteEvents.Count,
                "Tracks with different seeds should produce different results");
        }
    }
}
