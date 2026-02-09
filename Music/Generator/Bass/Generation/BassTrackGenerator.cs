// AI: purpose=Place stored Bass phrases across song bars to produce a full Bass PartTrack.
// AI: invariants=Requires MaterialBank with Bass phrases; covers bars up to maxBars when provided.
// AI: deps=MaterialPhrase.ToPartTrack for placement; BarTrack.GetBarEndTick used to clip endings.

using Music.Generator.Bass.Planning;
using Music.MyMidi;
using Music.Song.Material;

namespace Music.Generator.Bass.Generation;

public sealed class BassTrackGenerator
{
    // AI: purpose=Convenience entry: generate with implicit seed. Delegates to seeded overload.
    public PartTrack Generate(SongContext songContext, int maxBars = 0)
        => Generate(songContext, seed: 0, maxBars);

    // AI: purpose=Generate Bass PartTrack by planning phrase placements and converting to events.
    // AI: invariants=Throws on null/missing songContext.MaterialBank or required tracks; stable seed affects phrase selection.
    public PartTrack Generate(SongContext songContext, int seed, int maxBars)
    {
        ArgumentNullException.ThrowIfNull(songContext);
        ArgumentNullException.ThrowIfNull(songContext.MaterialBank);
        if (songContext.SectionTrack == null || songContext.SectionTrack.Sections.Count == 0)
            throw new ArgumentException("SectionTrack must have sections", nameof(songContext));
        if (songContext.BarTrack == null)
            throw new ArgumentException("BarTrack must be provided", nameof(songContext));

        const int bassProgramNumber = 255;
        var phrases = songContext.MaterialBank.GetPhrasesByMidiProgram(bassProgramNumber);
        if (phrases.Count == 0)
            throw new InvalidOperationException("No bass phrases found for the bass program");

        Tracer.DebugTrace($"[bassGenerator] phrases={phrases.Count}; seed={seed}; maxBars={maxBars}");
        foreach (var phrase in phrases)
        {
            Tracer.DebugTrace($"[bassGenerator] phraseId={phrase.PhraseId}; bars={phrase.BarCount}; events={phrase.Events.Count}");
        }

        int effectiveSeed = seed > 0 ? seed : Random.Shared.Next(1, 100_000);
        var planner = new BassPhrasePlacementPlanner(songContext, effectiveSeed);
        var plan = planner.CreatePlan(songContext.SectionTrack, bassProgramNumber, maxBars);

        Tracer.DebugTrace($"[bassGenerator] placements={plan.Placements.Count}; fillBars={plan.FillBars.Count}");

        return GenerateFromPlan(
            plan,
            songContext.MaterialBank,
            songContext.BarTrack,
            bassProgramNumber,
            effectiveSeed);
    }

    private PartTrack GenerateFromPlan(
        bassPhrasePlacementPlan plan,
        MaterialBank materialBank,
        BarTrack barTrack,
        int midiProgramNumber,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(materialBank);
        var allEvents = new List<PartTrackEvent>();
        var evolver = new BassPhraseEvolver(seed);

        foreach (var placement in plan.Placements)
        {
            var phrase = materialBank.GetPhraseById(placement.PhraseId);
            if (phrase == null)
            {
                Tracer.DebugTrace($"[bassGenerator] missingPhraseId={placement.PhraseId}");
                continue;
            }

            if (placement.Evolution != null)
                phrase = evolver.Evolve(phrase, placement.Evolution, barTrack);

            var phraseTrack = phrase.ToPartTrack(barTrack, placement.StartBar, midiProgramNumber);
            long placementEndTick = GetPlacementEndTick(barTrack, placement);

            int eligibleCount = phraseTrack.PartTrackNoteEvents
                .Count(e => e.AbsoluteTimeTicks < placementEndTick);

            Tracer.DebugTrace(
                $"[bassGenerator] placement phraseId={phrase.PhraseId}; start={placement.StartBar}; bars={placement.BarCount}; " +
                $"events={phraseTrack.PartTrackNoteEvents.Count}; eligible={eligibleCount}; endTick={placementEndTick}");

            allEvents.AddRange(phraseTrack.PartTrackNoteEvents
                .Where(e => e.AbsoluteTimeTicks < placementEndTick));
        }

        var ordered = allEvents.OrderBy(e => e.AbsoluteTimeTicks).ToList();

        Tracer.DebugTrace($"[bassGenerator] generatedEvents={ordered.Count}");

        return new PartTrack(ordered)
        {
            MidiProgramName = "Bass (Phrase-Based)",
            MidiProgramNumber = midiProgramNumber
        };
    }

    private static long GetPlacementEndTick(BarTrack barTrack, BassPhrasePlacementPlan placement)
    {
        int lastBar = placement.EndBar - 1;
        return barTrack.GetBarEndTick(lastBar);
    }
}
