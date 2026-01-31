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

    public PartTrack Generate(SongContext songContext, string genre, int maxBars = 0)
    {
        ArgumentNullException.ThrowIfNull(songContext);
        if (string.IsNullOrWhiteSpace(genre))
            throw new ArgumentException("Genre must be provided", nameof(genre));
        if (songContext.SectionTrack == null || songContext.SectionTrack.Sections.Count == 0)
            throw new ArgumentException("SectionTrack must have sections", nameof(songContext));
        if (songContext.BarTrack == null)
            throw new ArgumentException("BarTrack must be provided", nameof(songContext));

        var phrases = _materialBank.GetDrumPhrasesByGenre(genre);
        if (phrases.Count == 0)
            throw new InvalidOperationException($"No drum phrases found for genre '{genre}'");

        int totalBars = songContext.SectionTrack.TotalBars;
        if (maxBars > 0 && maxBars < totalBars)
            totalBars = maxBars;

        var plan = CreateSimplePlacementPlan(phrases, totalBars);
        return GenerateFromPlan(plan, songContext.BarTrack);
    }

    private static DrumPhrasePlacementPlan CreateSimplePlacementPlan(
        IReadOnlyList<MaterialPhrase> phrases,
        int totalBars)
    {
        var plan = new DrumPhrasePlacementPlan();
        var phrase = phrases[0];

        int currentBar = 1;
        while (currentBar <= totalBars)
        {
            int barsRemaining = totalBars - currentBar + 1;
            int placementBars = Math.Min(phrase.BarCount, barsRemaining);

            plan.Placements.Add(new DrumPhrasePlacement
            {
                PhraseId = phrase.PhraseId,
                StartBar = currentBar,
                BarCount = placementBars
            });

            currentBar += placementBars;
        }

        return plan;
    }

    private PartTrack GenerateFromPlan(DrumPhrasePlacementPlan plan, BarTrack barTrack)
    {
        var allEvents = new List<PartTrackEvent>();

        foreach (var placement in plan.Placements)
        {
            var phrase = _materialBank.GetDrumPhraseById(placement.PhraseId);
            if (phrase == null)
                continue;

            var phraseTrack = phrase.ToPartTrack(barTrack, placement.StartBar, 255);
            long placementEndTick = GetPlacementEndTick(barTrack, placement);

            allEvents.AddRange(phraseTrack.PartTrackNoteEvents
                .Where(e => e.AbsoluteTimeTicks < placementEndTick));
        }

        var ordered = allEvents.OrderBy(e => e.AbsoluteTimeTicks).ToList();

        return new PartTrack(ordered)
        {
            MidiProgramName = "Drums (Phrase-Based)",
            MidiProgramNumber = 255
        };
    }

    private static long GetPlacementEndTick(BarTrack barTrack, DrumPhrasePlacement placement)
    {
        int lastBar = placement.EndBar - 1;
        return barTrack.GetBarEndTick(lastBar);
    }
}
