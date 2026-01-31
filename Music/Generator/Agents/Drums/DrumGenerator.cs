// AI: purpose=Builds a full drum track by placing stored drum phrases across song bars.
// AI: invariants=Requires MaterialBank drum phrases; placement covers all bars unless maxBars limits.
// AI: deps=MaterialPhrase.ToPartTrack for placement; BarTrack.GetBarEndTick for clipping.

using Music.MyMidi;
using Music.Song.Material;

namespace Music.Generator.Agents.Drums;

public sealed class DrumGenerator
{
    private readonly MaterialBank _materialBank;

    public DrumGenerator(MaterialBank materialBank)
    {
        ArgumentNullException.ThrowIfNull(materialBank);
        _materialBank = materialBank;
    }

    public PartTrack Generate(SongContext songContext, int maxBars = 0)
        => Generate(songContext, seed: 0, maxBars);

    // AI: purpose=Generates drum track using phrase placement; seed controls section phrase selection.
    public PartTrack Generate(SongContext songContext, int seed, int maxBars)
    {
        ArgumentNullException.ThrowIfNull(songContext);
        if (songContext.SectionTrack == null || songContext.SectionTrack.Sections.Count == 0)
            throw new ArgumentException("SectionTrack must have sections", nameof(songContext));
        if (songContext.BarTrack == null)
            throw new ArgumentException("BarTrack must be provided", nameof(songContext));

        const int drumProgramNumber = 255;
        var phrases = _materialBank.GetDrumPhrasesByMidiProgram(drumProgramNumber);
        if (phrases.Count == 0)
            throw new InvalidOperationException("No drum phrases found for the drum program");

        int effectiveSeed = seed > 0 ? seed : Random.Shared.Next(1, 100_000);
        var planner = new DrumPhrasePlacementPlanner(songContext, effectiveSeed);
        var plan = planner.CreatePlan(songContext.SectionTrack, drumProgramNumber, maxBars);

        return GenerateFromPlan(plan, songContext.BarTrack, drumProgramNumber, effectiveSeed);
    }

    private PartTrack GenerateFromPlan(
        DrumPhrasePlacementPlan plan,
        BarTrack barTrack,
        int midiProgramNumber,
        int seed)
    {
        var allEvents = new List<PartTrackEvent>();
        var evolver = new DrumPhraseEvolver(seed);

        foreach (var placement in plan.Placements)
        {
            var phrase = _materialBank.GetDrumPhraseById(placement.PhraseId);
            if (phrase == null)
                continue;

            if (placement.Evolution != null)
                phrase = evolver.Evolve(phrase, placement.Evolution, barTrack);

            var phraseTrack = phrase.ToPartTrack(barTrack, placement.StartBar, midiProgramNumber);
            long placementEndTick = GetPlacementEndTick(barTrack, placement);

            allEvents.AddRange(phraseTrack.PartTrackNoteEvents
                .Where(e => e.AbsoluteTimeTicks < placementEndTick));
        }

        var ordered = allEvents.OrderBy(e => e.AbsoluteTimeTicks).ToList();

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
