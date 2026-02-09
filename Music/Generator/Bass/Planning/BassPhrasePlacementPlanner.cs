// AI: purpose=Plan Bass phrase placements by section; deterministic phrase selection using seed.
// AI: invariants=StartBar>=1; BarCount>=1; placements non-overlapping per section; same seed->same assignment.
// AI: deps=Uses MaterialBank and SectionTrack; changing fields impacts bassTrackGenerator and related tests.

using Music.Song.Material;

namespace Music.Generator.Bass.Planning;

public sealed class BassPhrasePlacementPlanner
{
    // AI: note=_materialBank and _seed are required; seed ensures deterministic RNG for phrase selection
    private readonly MaterialBank _materialBank;
    private readonly int _seed;

    public BassPhrasePlacementPlanner(SongContext songContext, int seed)
    {
        ArgumentNullException.ThrowIfNull(songContext);
        _materialBank = songContext.MaterialBank
            ?? throw new ArgumentException("MaterialBank must be provided", nameof(songContext));
        _seed = seed;
    }

    public bassPhrasePlacementPlan CreatePlan(
        SectionTrack sectionTrack,
        int midiProgramNumber,
        int maxBars = 0)
    {
        // AI: returns=bassPhrasePlacementPlan with Placements and FillBars; respects maxBars and avoids fill-bar overlap
        ArgumentNullException.ThrowIfNull(sectionTrack);
        if (midiProgramNumber < 0 || midiProgramNumber > 255)
            throw new ArgumentOutOfRangeException(nameof(midiProgramNumber), "MIDI program must be 0-255.");

        var phrases = _materialBank.GetPhrasesByMidiProgram(midiProgramNumber);
        if (phrases.Count == 0)
            throw new InvalidOperationException("No bass phrases found for the requested MIDI program");

        int totalBars = sectionTrack.TotalBars;
        if (maxBars > 0 && maxBars < totalBars)
            totalBars = maxBars;

        var plan = new bassPhrasePlacementPlan();
        var sectionPhraseMap = AssignPhrasesToSectionTypes(phrases, sectionTrack);

        for (int i = 0; i < sectionTrack.Sections.Count - 1; i++)
        {
            var section = sectionTrack.Sections[i];
            int fillBar = section.StartBar + section.BarCount - 1;
            if (fillBar <= totalBars)
                plan.FillBars.Add(fillBar);
        }

        foreach (var section in sectionTrack.Sections)
        {
            if (section.StartBar > totalBars)
                break;

            PlacePhrasesInSection(plan, section, sectionPhraseMap, totalBars);
        }

        return plan;
    }

    private Dictionary<MusicConstants.eSectionType, MaterialPhrase> AssignPhrasesToSectionTypes(
        IReadOnlyList<MaterialPhrase> phrases,
        SectionTrack sectionTrack)
    {
        // AI: behavior=Selects one phrase per section type deterministically using RNG seeded with _seed
        var map = new Dictionary<MusicConstants.eSectionType, MaterialPhrase>();
        var rng = new Random(_seed);
        var sectionTypes = sectionTrack.Sections
            .Select(section => section.SectionType)
            .Distinct()
            .ToList();

        foreach (var sectionType in sectionTypes)
        {
            var matchingPhrases = phrases
                .Where(phrase => IsSectionMatch(phrase, sectionType))
                .ToList();

            var pool = matchingPhrases.Count > 0 ? matchingPhrases : phrases;
            map[sectionType] = pool[rng.Next(pool.Count)];
        }

        return map;
    }

    private static void PlacePhrasesInSection(
        bassPhrasePlacementPlan plan,
        Section section,
        Dictionary<MusicConstants.eSectionType, MaterialPhrase> phraseMap,
        int totalBars)
    {
        // AI: note=Places repeats sequentially; skips placements that overlap any FillBar; evolution for repeats only
        if (!phraseMap.TryGetValue(section.SectionType, out var phrase))
            return;

        int sectionStart = section.StartBar;
        int sectionEnd = Math.Min(section.StartBar + section.BarCount - 1, totalBars);
        int currentBar = sectionStart;
        int placementIndex = 0;

        while (currentBar <= sectionEnd)
        {
            int barsRemaining = sectionEnd - currentBar + 1;
            int placementBars = Math.Min(phrase.BarCount, barsRemaining);

            if (OverlapsFillBar(plan, currentBar, placementBars))
            {
                currentBar++;
                continue;
            }

            var evolution = placementIndex == 0
                ? null
                : CreateEvolutionForRepeat(placementIndex, section.SectionType);

            plan.Placements.Add(new BassPhrasePlacementPlan
            {
                PhraseId = phrase.PhraseId,
                StartBar = currentBar,
                BarCount = placementBars,
                EvolutionLevel = placementIndex,
                Evolution = evolution
            });

            currentBar += placementBars;
            placementIndex++;
        }
    }

    private static bool OverlapsFillBar(bassPhrasePlacementPlan plan, int startBar, int barCount)
    {
        // AI: checks any bar in [startBar..endBar] belongs to FillBars
        int endBar = startBar + barCount - 1;
        for (int bar = startBar; bar <= endBar; bar++)
        {
            if (plan.IsFillBar(bar))
                return true;
        }

        return false;
    }

    private static BassPhraseEvolutionParams? CreateEvolutionForRepeat(
        int repeatIndex,
        MusicConstants.eSectionType sectionType)
    {
        // AI: evolution variation scales with repeatIndex, capped to avoid excessive change
        if (repeatIndex <= 0)
            return null;

        double baseVariation = Math.Min(repeatIndex * 0.1, 0.3);
        if (baseVariation <= 0)
            return null;

        return new BassPhraseEvolutionParams
        {
            RandomVariation = baseVariation,
            GhostIntensity = sectionType == MusicConstants.eSectionType.Chorus
                ? baseVariation * 0.5
                : 0,
            HatVariation = baseVariation * 0.3
        };
    }

    private static bool IsSectionMatch(MaterialPhrase phrase, MusicConstants.eSectionType sectionType)
    {
        if (phrase.SectionTypes.Contains(sectionType))
            return true;

        string sectionTag = sectionType.ToString();
        return phrase.Tags.Any(tag => string.Equals(tag, sectionTag, StringComparison.OrdinalIgnoreCase));
    }
}
