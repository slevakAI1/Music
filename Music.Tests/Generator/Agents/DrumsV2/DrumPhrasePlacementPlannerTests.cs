using Music.Generator;
using Music.Generator.Agents.Drums;
using Music.MyMidi;
using Music.Song.Material;
using Xunit;

namespace Music.Tests.Generator.Agents.DrumsV2;

public class DrumPhrasePlacementPlannerTests
{
    [Fact]
    public void CreatePlan_WithSectionTypes_AssignsPhrasePerSection()
    {
        var songContext = new SongContext();
        var versePhrase = CreatePhrase("verse-phrase", barCount: 1,
            sectionTypes: [MusicConstants.eSectionType.Verse]);
        var chorusPhrase = CreatePhrase("chorus-phrase", barCount: 1,
            sectionTypes: [MusicConstants.eSectionType.Chorus]);
        songContext.MaterialBank.AddDrumPhrase(versePhrase);
        songContext.MaterialBank.AddDrumPhrase(chorusPhrase);

        var sectionTrack = new SectionTrack();
        sectionTrack.Add(MusicConstants.eSectionType.Verse, 2);
        sectionTrack.Add(MusicConstants.eSectionType.Chorus, 2);

        var planner = new DrumPhrasePlacementPlanner(songContext, seed: 1234);
        var plan = planner.CreatePlan(sectionTrack, midiProgramNumber: 255);

        var placements = plan.Placements.OrderBy(p => p.StartBar).ToList();

        Assert.Equal("verse-phrase", placements[0].PhraseId);
        Assert.Equal("verse-phrase", placements[1].PhraseId);
        Assert.Equal("chorus-phrase", placements[2].PhraseId);
        Assert.Equal("chorus-phrase", placements[3].PhraseId);
    }

    [Fact]
    public void CreatePlan_WithTagMatch_PrefersTaggedPhrase()
    {
        var songContext = new SongContext();
        var taggedPhrase = CreatePhrase("tagged-phrase", barCount: 1, tags: ["Verse"]);
        var genericPhrase = CreatePhrase("generic-phrase", barCount: 1);
        songContext.MaterialBank.AddDrumPhrase(taggedPhrase);
        songContext.MaterialBank.AddDrumPhrase(genericPhrase);

        var sectionTrack = new SectionTrack();
        sectionTrack.Add(MusicConstants.eSectionType.Verse, 2);

        var planner = new DrumPhrasePlacementPlanner(songContext, seed: 2024);
        var plan = planner.CreatePlan(sectionTrack, midiProgramNumber: 255);

        Assert.All(plan.Placements, placement => Assert.Equal("tagged-phrase", placement.PhraseId));
    }

    [Fact]
    public void CreatePlan_WithSameSeed_IsDeterministic()
    {
        var songContext = new SongContext();
        songContext.MaterialBank.AddDrumPhrase(CreatePhrase("phrase-a", barCount: 1,
            sectionTypes: [MusicConstants.eSectionType.Verse]));
        songContext.MaterialBank.AddDrumPhrase(CreatePhrase("phrase-b", barCount: 1,
            sectionTypes: [MusicConstants.eSectionType.Verse]));

        var sectionTrack = new SectionTrack();
        sectionTrack.Add(MusicConstants.eSectionType.Verse, 2);

        var plannerA = new DrumPhrasePlacementPlanner(songContext, seed: 77);
        var plannerB = new DrumPhrasePlacementPlanner(songContext, seed: 77);

        var planA = plannerA.CreatePlan(sectionTrack, midiProgramNumber: 255);
        var planB = plannerB.CreatePlan(sectionTrack, midiProgramNumber: 255);

        Assert.Equal(planA.Placements[0].PhraseId, planB.Placements[0].PhraseId);
    }

    [Fact]
    public void CreatePlan_WithRepeats_AssignsProgressiveEvolution()
    {
        var songContext = new SongContext();
        songContext.MaterialBank.AddDrumPhrase(CreatePhrase("phrase-a", barCount: 1,
            sectionTypes: [MusicConstants.eSectionType.Verse]));

        var sectionTrack = new SectionTrack();
        sectionTrack.Add(MusicConstants.eSectionType.Verse, 3);

        var planner = new DrumPhrasePlacementPlanner(songContext, seed: 9001);
        var plan = planner.CreatePlan(sectionTrack, midiProgramNumber: 255);

        var placements = plan.Placements.OrderBy(p => p.StartBar).ToList();

        Assert.Equal(0, placements[0].EvolutionLevel);
        Assert.Null(placements[0].Evolution);

        Assert.Equal(1, placements[1].EvolutionLevel);
        Assert.NotNull(placements[1].Evolution);
        Assert.Equal(0.1, placements[1].Evolution!.RandomVariation, 3);

        Assert.Equal(2, placements[2].EvolutionLevel);
        Assert.NotNull(placements[2].Evolution);
        Assert.Equal(0.2, placements[2].Evolution!.RandomVariation, 3);
    }

    [Fact]
    public void CreatePlan_WithChorusRepeats_AssignsGhostIntensity()
    {
        var songContext = new SongContext();
        songContext.MaterialBank.AddDrumPhrase(CreatePhrase("phrase-a", barCount: 1,
            sectionTypes: [MusicConstants.eSectionType.Chorus]));

        var sectionTrack = new SectionTrack();
        sectionTrack.Add(MusicConstants.eSectionType.Chorus, 2);

        var planner = new DrumPhrasePlacementPlanner(songContext, seed: 4242);
        var plan = planner.CreatePlan(sectionTrack, midiProgramNumber: 255);

        var placements = plan.Placements.OrderBy(p => p.StartBar).ToList();

        Assert.Null(placements[0].Evolution);
        Assert.NotNull(placements[1].Evolution);
        Assert.Equal(0.05, placements[1].Evolution!.GhostIntensity, 3);
    }

    private static MaterialPhrase CreatePhrase(
        string phraseId,
        int barCount,
        IReadOnlyList<MusicConstants.eSectionType>? sectionTypes = null,
        IReadOnlyList<string>? tags = null)
    {
        return new MaterialPhrase
        {
            PhraseNumber = 1,
            PhraseId = phraseId,
            Name = phraseId,
            Description = phraseId,
            BarCount = barCount,
            MidiProgramNumber = 255,
            Seed = 1,
            Events = new List<PartTrackEvent>(),
            SectionTypes = sectionTypes ?? [],
            Tags = tags ?? []
        };
    }
}
