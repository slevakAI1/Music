//using Music.Generator;
//using Music.Generator.Agents.Drums;
//using Music.MyMidi;
//using Music.Song.Material;
//using Xunit;

//namespace Music.Tests.Generator.Agents.DrumsV2;

//public class DrumGeneratorTests
//{
//    [Fact]
//    public void Generate_WithSinglePhrase_RepeatsAcrossBars()
//    {
//        var songContext = CreateSongContext(totalBars: 4);
//        var materialBank = songContext.MaterialBank;
//        var phrase = CreatePhrase("phrase1", barCount: 2, songContext.BarTrack);
//        materialBank.AddDrumPhrase(phrase);

//        var generator = new DrumGenerator(materialBank);
//        var track = generator.Generate(songContext);

//        Assert.Equal(4, track.PartTrackNoteEvents.Count);
//    }

//    [Fact]
//    public void Generate_WithSinglePhrase_OffsetsEventsByPlacement()
//    {
//        var songContext = CreateSongContext(totalBars: 4);
//        var materialBank = songContext.MaterialBank;
//        var phrase = CreatePhrase("phrase1", barCount: 2, songContext.BarTrack);
//        materialBank.AddDrumPhrase(phrase);

//        var generator = new DrumGenerator(materialBank);
//        var track = generator.Generate(songContext);

//        long bar1Tick = songContext.BarTrack.ToTick(1, 1m);
//        long bar2Tick = songContext.BarTrack.ToTick(2, 1m);
//        long bar3Tick = songContext.BarTrack.ToTick(3, 1m);
//        long bar4Tick = songContext.BarTrack.ToTick(4, 1m);

//        var expected = new[] { bar1Tick, bar2Tick, bar3Tick, bar4Tick };
//        var actual = track.PartTrackNoteEvents.Select(e => e.AbsoluteTimeTicks).ToArray();

//        Assert.Equal(expected, actual);
//    }

//    [Fact]
//    public void Generate_WithPartialPhraseAtSongEnd_ClipsEvents()
//    {
//        var songContext = CreateSongContext(totalBars: 3);
//        var materialBank = songContext.MaterialBank;
//        var phrase = CreatePhrase("phrase1", barCount: 2, songContext.BarTrack);
//        materialBank.AddDrumPhrase(phrase);

//        var generator = new DrumGenerator(materialBank);
//        var track = generator.Generate(songContext);

//        Assert.Equal(3, track.PartTrackNoteEvents.Count);
//    }

//    [Fact]
//    public void Generate_WithPartialPhraseAtSongEnd_DoesNotExceedLastBar()
//    {
//        var songContext = CreateSongContext(totalBars: 3);
//        var materialBank = songContext.MaterialBank;
//        var phrase = CreatePhrase("phrase1", barCount: 2, songContext.BarTrack);
//        materialBank.AddDrumPhrase(phrase);

//        var generator = new DrumGenerator(materialBank);
//        var track = generator.Generate(songContext);

//        long endTick = songContext.BarTrack.GetBarEndTick(3);
//        long maxTick = track.PartTrackNoteEvents.Max(e => e.AbsoluteTimeTicks);

//        Assert.True(maxTick < endTick);
//    }

//    [Fact]
//    public void Generate_WithRepeats_AppliesEvolutionToLaterPlacements()
//    {
//        var songContext = CreateSongContext(totalBars: 2);
//        var materialBank = songContext.MaterialBank;
//        var phrase = CreateSingleNotePhrase("phrase1", barCount: 1, songContext.BarTrack, noteNumber: 42);
//        materialBank.AddDrumPhrase(phrase);

//        var generator = new DrumGenerator(materialBank);
//        var track = generator.Generate(songContext, seed: 123, maxBars: 0);

//        var orderedNotes = track.PartTrackNoteEvents
//            .OrderBy(e => e.AbsoluteTimeTicks)
//            .Select(e => e.NoteNumber)
//            .ToList();

//        Assert.Equal(2, orderedNotes.Count);
//        Assert.Equal(42, orderedNotes[0]);
//        Assert.Equal(46, orderedNotes[1]);
//    }

//    private static SongContext CreateSongContext(int totalBars)
//    {
//        var songContext = new SongContext();
//        var timingTrack = TimingTests.CreateTestTrackD1();
//        songContext.BarTrack.RebuildFromTimingTrack(timingTrack, totalBars);
//        songContext.SectionTrack.Reset();
//        songContext.SectionTrack.Add(MusicConstants.eSectionType.Verse, totalBars);
//        return songContext;
//    }

//    private static MaterialPhrase CreatePhrase(string phraseId, int barCount, BarTrack barTrack)
//    {
//        var events = new List<PartTrackEvent>
//        {
//            new(36, (int)barTrack.ToTick(1, 1m), 120, 100),
//            new(38, (int)barTrack.ToTick(2, 1m), 120, 100)
//        };

//        return new MaterialPhrase
//        {
//            PhraseNumber = 1,
//            PhraseId = phraseId,
//            Name = "Test Phrase",
//            Description = "Test phrase description",
//            BarCount = barCount,
//            MidiProgramNumber = 255,
//            Seed = 123,
//            Events = events,
//            SectionTypes = [MusicConstants.eSectionType.Verse]
//        };
//    }

//    private static MaterialPhrase CreateSingleNotePhrase(
//        string phraseId,
//        int barCount,
//        BarTrack barTrack,
//        int noteNumber)
//    {
//        var events = new List<PartTrackEvent>
//        {
//            new(noteNumber, (int)barTrack.ToTick(1, 1m), 120, 100)
//        };

//        return new MaterialPhrase
//        {
//            PhraseNumber = 1,
//            PhraseId = phraseId,
//            Name = "Test Phrase",
//            Description = "Test phrase description",
//            BarCount = barCount,
//            MidiProgramNumber = 255,
//            Seed = 123,
//            Events = events,
//            SectionTypes = [MusicConstants.eSectionType.Verse]
//        };
//    }
//}
