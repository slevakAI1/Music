// AI: purpose=Builds a full drum track by placing stored drum phrases across song bars.
// AI: invariants=Requires MaterialBank drum phrases; placement covers all bars unless maxBars limits.
// AI: deps=MaterialPhrase.ToPartTrack for placement; BarTrack.GetBarEndTick for clipping.

using Music.Generator.Core;
using Music.Generator.Drums.Planning;
using Music.MyMidi;
using Music.Song.Material;

namespace Music.Generator.Drums.Generation;

public sealed class DrumGenerator
{
    public PartTrack Generate(SongContext songContext, int maxBars = 0)
        => Generate(songContext, seed: 0, maxBars);

    // AI: purpose=Generates drum track using phrase placement; seed controls section phrase selection.
    public PartTrack Generate(SongContext songContext, int seed, int maxBars)
    {
        ArgumentNullException.ThrowIfNull(songContext);
        ArgumentNullException.ThrowIfNull(songContext.MaterialBank);
        if (songContext.SectionTrack == null || songContext.SectionTrack.Sections.Count == 0)
            throw new ArgumentException("SectionTrack must have sections", nameof(songContext));
        if (songContext.BarTrack == null)
            throw new ArgumentException("BarTrack must be provided", nameof(songContext));

        const int drumProgramNumber = 255;
        var phrases = songContext.MaterialBank.GetPhrasesByMidiProgram(drumProgramNumber);
        if (phrases.Count == 0)
            throw new InvalidOperationException("No drum phrases found for the drum program");

        Tracer.DebugTrace($"[DrumGenerator] phrases={phrases.Count}; seed={seed}; maxBars={maxBars}");
        foreach (var phrase in phrases)
        {
            Tracer.DebugTrace($"[DrumGenerator] phraseId={phrase.PhraseId}; bars={phrase.BarCount}; events={phrase.Events.Count}");
        }

        int effectiveSeed = seed > 0 ? seed : Random.Shared.Next(1, 100_000);
        var planner = new DrumPhrasePlacementPlanner(songContext, effectiveSeed);
        var plan = planner.CreatePlan(songContext.SectionTrack, drumProgramNumber, maxBars);

        Tracer.DebugTrace($"[DrumGenerator] placements={plan.Placements.Count}; fillBars={plan.FillBars.Count}");

        return GenerateFromPlan(
            plan,
            songContext.MaterialBank,
            songContext.BarTrack,
            drumProgramNumber,
            effectiveSeed);
    }

    private PartTrack GenerateFromPlan(
        DrumPhrasePlacementPlan plan,
        MaterialBank materialBank,
        BarTrack barTrack,
        int midiProgramNumber,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(materialBank);
        var allEvents = new List<PartTrackEvent>();
        var evolver = new DrumPhraseEvolver(seed);

        foreach (var placement in plan.Placements)
        {
            var phrase = materialBank.GetPhraseById(placement.PhraseId);
            if (phrase == null)
            {
                Tracer.DebugTrace($"[DrumGenerator] missingPhraseId={placement.PhraseId}");
                continue;
            }

            if (placement.Evolution != null)
                phrase = evolver.Evolve(phrase, placement.Evolution, barTrack);

            var phraseTrack = phrase.ToPartTrack(barTrack, placement.StartBar, midiProgramNumber);
            long placementEndTick = GetPlacementEndTick(barTrack, placement);

            int eligibleCount = phraseTrack.PartTrackNoteEvents
                .Count(e => e.AbsoluteTimeTicks < placementEndTick);

            Tracer.DebugTrace(
                $"[DrumGenerator] placement phraseId={phrase.PhraseId}; start={placement.StartBar}; bars={placement.BarCount}; " +
                $"events={phraseTrack.PartTrackNoteEvents.Count}; eligible={eligibleCount}; endTick={placementEndTick}");

            allEvents.AddRange(phraseTrack.PartTrackNoteEvents
                .Where(e => e.AbsoluteTimeTicks < placementEndTick));
        }

        var ordered = allEvents.OrderBy(e => e.AbsoluteTimeTicks).ToList();

        Tracer.DebugTrace($"[DrumGenerator] generatedEvents={ordered.Count}");

        return new PartTrack(ordered)
        {
            MidiProgramName = "Drums (Phrase-Based)",
            MidiProgramNumber = midiProgramNumber
        };
    }

    private static long GetPlacementEndTick(BarTrack barTrack, DrumPhrasePlacement placement)
    {
        int lastBar = placement.EndBar - 1;
        return barTrack.GetBarEndTick(lastBar);
    }
}
