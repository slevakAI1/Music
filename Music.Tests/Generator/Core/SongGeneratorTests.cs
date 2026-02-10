namespace Music.Tests.Generator.Core
{
    using Music.Generator;
    using Music.Generator.Groove;
    using Music.MyMidi;

    // AI: purpose=Validate SongGenerator.Generate handles operator count parameter and returns a PartTrack.
    public sealed class SongGeneratorTests
    {
        private static SongContext CreateTestSongContext()
        {
            var songContext = new SongContext();
            songContext.SectionTrack.Add(MusicConstants.eSectionType.Verse, 4);
            songContext.Song.TimeSignatureTrack = TimingTests.CreateTestTrackD1();
            songContext.BarTrack.RebuildFromTimingTrack(
                songContext.Song.TimeSignatureTrack,
                songContext.SectionTrack,
                songContext.SectionTrack.TotalBars);

            songContext.GroovePresetDefinition = new GroovePresetDefinition
            {
                Identity = new GroovePresetIdentity
                {
                    Name = "PopRock",
                    BeatsPerBar = 4,
                    StyleFamily = "PopRock"
                },
                AnchorLayer = GrooveAnchorFactory.GetAnchor("PopRock")
            };

            songContext.Voices.SetTestVoicesD1();

            return songContext;
        }

        [Fact]
        public void Generate_WithOperatorCount_ReturnsPartTrack()
        {
            SongContext songContext = CreateTestSongContext();

            PartTrack result = SongGenerator.Generate(songContext, maxBars: 2, numberOfOperators: 3);

            Assert.NotNull(result);
            Assert.NotEmpty(result.PartTrackNoteEvents);
        }
    }
}
