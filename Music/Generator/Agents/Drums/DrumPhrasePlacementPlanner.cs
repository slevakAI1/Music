// AI: purpose=Plan drum phrase placements by section type with deterministic phrase selection per seed.
// AI: invariants=Same seed+material order yields same section phrase map; placements non-overlapping per section.
// AI: deps=MaterialBank phrases filtered by MIDI program; SectionTrack sections; MaterialPhrase.SectionTypes/Tags.

using Music.Song.Material;

namespace Music.Generator.Agents.Drums;

public sealed class DrumPhrasePlacementPlanner
{
    private readonly MaterialBank _materialBank;
    private readonly int _seed;

    public DrumPhrasePlacementPlanner(SongContext songContext, int seed)
    {
        ArgumentNullException.ThrowIfNull(songContext);
        _materialBank = songContext.MaterialBank
            ?? throw new ArgumentException("MaterialBank must be provided", nameof(songContext));
        _seed = seed;
    }

    public DrumPhrasePlacementPlan CreatePlan(
        SectionTrack sectionTrack,
        int midiProgramNumber,
        int maxBars = 0)
    {
        ArgumentNullException.ThrowIfNull(sectionTrack);
        if (midiProgramNumber < 0 || midiProgramNumber > 255)
            throw new ArgumentOutOfRangeException(nameof(midiProgramNumber), "MIDI program must be 0-255.");

        var phrases = _materialBank.GetDrumPhrasesByMidiProgram(midiProgramNumber);
        if (phrases.Count == 0)
            throw new InvalidOperationException("No drum phrases found for the requested MIDI program");

        int totalBars = sectionTrack.TotalBars;
        if (maxBars > 0 && maxBars < totalBars)
            totalBars = maxBars;

        var plan = new DrumPhrasePlacementPlan();
        var sectionPhraseMap = AssignPhrasesToSectionTypes(phrases, sectionTrack);

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
        DrumPhrasePlacementPlan plan,
        Section section,
        Dictionary<MusicConstants.eSectionType, MaterialPhrase> phraseMap,
        int totalBars)
    {
        if (!phraseMap.TryGetValue(section.SectionType, out var phrase))
            return;

        int sectionStart = section.StartBar;
        int sectionEnd = Math.Min(section.StartBar + section.BarCount - 1, totalBars);
        int currentBar = sectionStart;

        while (currentBar <= sectionEnd)
        {
            int barsRemaining = sectionEnd - currentBar + 1;
            int placementBars = Math.Min(phrase.BarCount, barsRemaining);

            plan.Placements.Add(new DrumPhrasePlacement
            {
                PhraseId = phrase.PhraseId,
                StartBar = currentBar,
                BarCount = placementBars
            });

            currentBar += placementBars;
        }
    }

    private static bool IsSectionMatch(MaterialPhrase phrase, MusicConstants.eSectionType sectionType)
    {
        if (phrase.SectionTypes.Contains(sectionType))
            return true;

        string sectionTag = sectionType.ToString();
        return phrase.Tags.Any(tag => string.Equals(tag, sectionTag, StringComparison.OrdinalIgnoreCase));
    }
}
