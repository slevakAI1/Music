using System;
using System.Linq;
using Music;
using Music.Generator;
using Music.Generator.Agents.Common;
using Music.Generator.Groove;
using Music.Song.Material;
using Xunit;

namespace Music.Tests.Generator.Agents.DrumsV2;

public sealed class DrumPhraseTrackIntegrationTests
{
    // AI: purpose=Reproduce phrase generation + save + phrase-based drum generation workflow with trace output.
    [Fact]
    public void GenerateFromPhrases_WithSavedPhrase_EmitsTrace()
    {
        const string genre = "PopRock";
        const int seed = 12345;
        const int phraseBars = 2;

        var songContext = new SongContext();
        TestDesigns.SetTestDesignD1(songContext);

        var groovePreset = new GroovePresetDefinition
        {
            Identity = new GroovePresetIdentity
            {
                Name = genre,
                BeatsPerBar = 4,
                StyleFamily = genre
            },
            AnchorLayer = GrooveAnchorFactory.GetAnchor(genre)
        };
        songContext.GroovePresetDefinition = groovePreset;

        var drummerStyle = StyleConfigurationLibrary.GetStyle(genre)
            ?? throw new InvalidOperationException($"Style configuration not found for genre: {genre}");

        Rng.Initialize(seed);

        var phraseTrack = Music.Generator.Generator.Generate(songContext, drummerStyle, phraseBars);
        phraseTrack.MidiProgramNumber = 255;

        int phraseNumber = songContext.MaterialBank.GetPhrasesByMidiProgram(phraseTrack.MidiProgramNumber).Count + 1;
        int barCount = GetPhraseBarCount(songContext.BarTrack, phraseTrack);

        var firstTick = phraseTrack.PartTrackNoteEvents.Min(e => e.AbsoluteTimeTicks);
        var lastTick = phraseTrack.PartTrackNoteEvents.Max(e => e.AbsoluteTimeTicks + e.NoteDurationTicks);
        Tracer.DebugTrace($"[TEST] PhraseTrack firstTick={firstTick}; lastTick={lastTick}; calculatedBarCount={barCount}");

        var phrase = MaterialPhrase.FromPartTrack(
            phraseTrack,
            phraseNumber,
            Guid.NewGuid().ToString("N"),
            $"{genre} Phrase {phraseNumber}",
            "Test phrase",
            barCount,
            seed);

        Tracer.DebugTrace($"[TEST] PhraseTrack events={phraseTrack.PartTrackNoteEvents.Count}; barCount={barCount}; midi={phraseTrack.MidiProgramNumber}");
        Tracer.DebugTrace($"[TEST] Phrase events={phrase.Events.Count}; phraseId={phrase.PhraseId}");

        songContext.MaterialBank.AddPhrase(phrase);

        var drumTrack = Music.Generator.Generator.GenerateFromPhrases(songContext, seed, maxBars: 0);

        Tracer.DebugTrace($"[TEST] Generated drum track events={drumTrack.PartTrackNoteEvents.Count}");
    }

    private static int GetPhraseBarCount(BarTrack barTrack, PartTrack track)
    {
        ArgumentNullException.ThrowIfNull(barTrack);
        ArgumentNullException.ThrowIfNull(track);

        if (track.PartTrackNoteEvents.Count == 0 || barTrack.Bars.Count == 0)
            return 1;

        long firstTick = track.PartTrackNoteEvents.Min(e => e.AbsoluteTimeTicks);
        long lastTick = track.PartTrackNoteEvents.Max(e => e.AbsoluteTimeTicks + e.NoteDurationTicks);
        long phraseDuration = lastTick - firstTick;

        if (!barTrack.TryGetBar(1, out var bar1))
            return 1;

        long ticksPerBar = bar1.TicksPerMeasure;
        int barCount = (int)Math.Ceiling((double)phraseDuration / ticksPerBar);

        return Math.Max(1, barCount);
    }
}
