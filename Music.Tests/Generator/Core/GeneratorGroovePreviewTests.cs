namespace Music.Tests.Generator.Core
{
    using Music.Generator;
    using Music.Generator.Groove;

    // AI: purpose=Unit tests for Generator.GenerateGroovePreview facade method (seed+genre to PartTrack).
    // AI: invariants=Tests verify determinism, parameter validation, integration with GrooveAnchorFactory and ToPartTrack.
    public sealed class GeneratorGroovePreviewTests
    {
        #region Helper Methods

        private static BarTrack CreateTestBarTrack(int totalBars = 8)
        {
            Timingtrack timingTrack = TimingTests.CreateTestTrackD1();
            BarTrack barTrack = new();
            barTrack.RebuildFromTimingTrack(timingTrack, totalBars);
            return barTrack;
        }

        #endregion

        #region Basic Tests

        [Fact]
        public void GenerateGroovePreview_ValidInput_ReturnsPartTrack()
        {
            int seed = 123;
            string genre = "PopRock";
            BarTrack barTrack = CreateTestBarTrack(4);

            PartTrack partTrack = Generator.GenerateGroovePreview(seed, genre, barTrack, 4);

            Assert.NotNull(partTrack);
            Assert.NotNull(partTrack.PartTrackNoteEvents);
        }

        [Fact]
        public void GenerateGroovePreview_ValidInput_SetsStandardKit()
        {
            BarTrack barTrack = CreateTestBarTrack(4);

            PartTrack partTrack = Generator.GenerateGroovePreview(123, "PopRock", barTrack, 4);

            Assert.Equal("Standard Kit", partTrack.MidiProgramName);
            Assert.Equal(0, partTrack.MidiProgramNumber);
        }

        [Fact]
        public void GenerateGroovePreview_ValidInput_ProducesEvents()
        {
            BarTrack barTrack = CreateTestBarTrack(4);

            PartTrack partTrack = Generator.GenerateGroovePreview(123, "PopRock", barTrack, 4);

            Assert.True(partTrack.PartTrackNoteEvents.Count > 0);
        }

        #endregion

        #region Parameter Validation Tests

        [Fact]
        public void GenerateGroovePreview_NullGenre_ThrowsArgumentNullException()
        {
            BarTrack barTrack = CreateTestBarTrack(4);

            Assert.Throws<ArgumentNullException>(() =>
                Generator.GenerateGroovePreview(123, null!, barTrack, 4));
        }

        [Fact]
        public void GenerateGroovePreview_NullBarTrack_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                Generator.GenerateGroovePreview(123, "PopRock", null!, 4));
        }

        [Fact]
        public void GenerateGroovePreview_UnknownGenre_ThrowsArgumentException()
        {
            BarTrack barTrack = CreateTestBarTrack(4);

            Assert.Throws<ArgumentException>(() =>
                Generator.GenerateGroovePreview(123, "UnknownGenre", barTrack, 4));
        }

        [Fact]
        public void GenerateGroovePreview_InvalidTotalBars_ThrowsArgumentOutOfRangeException()
        {
            BarTrack barTrack = CreateTestBarTrack(4);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Generator.GenerateGroovePreview(123, "PopRock", barTrack, 0));
        }

        [Fact]
        public void GenerateGroovePreview_InvalidVelocity_ThrowsArgumentOutOfRangeException()
        {
            BarTrack barTrack = CreateTestBarTrack(4);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Generator.GenerateGroovePreview(123, "PopRock", barTrack, 4, velocity: 128));
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void GenerateGroovePreview_SameSeedAndGenre_ProducesIdenticalTrack()
        {
            int seed = 12345;
            string genre = "PopRock";
            BarTrack barTrack = CreateTestBarTrack(4);

            PartTrack track1 = Generator.GenerateGroovePreview(seed, genre, barTrack, 4);
            PartTrack track2 = Generator.GenerateGroovePreview(seed, genre, barTrack, 4);

            Assert.Equal(track1.PartTrackNoteEvents.Count, track2.PartTrackNoteEvents.Count);
            for (int i = 0; i < track1.PartTrackNoteEvents.Count; i++)
            {
                Assert.Equal(track1.PartTrackNoteEvents[i].AbsoluteTimeTicks,
                           track2.PartTrackNoteEvents[i].AbsoluteTimeTicks);
                Assert.Equal(track1.PartTrackNoteEvents[i].NoteNumber,
                           track2.PartTrackNoteEvents[i].NoteNumber);
                Assert.Equal(track1.PartTrackNoteEvents[i].NoteOnVelocity,
                           track2.PartTrackNoteEvents[i].NoteOnVelocity);
            }
        }

        [Fact]
        public void GenerateGroovePreview_DifferentSeeds_ProducesDifferentTracks()
        {
            string genre = "PopRock";
            BarTrack barTrack = CreateTestBarTrack(4);

            PartTrack track1 = Generator.GenerateGroovePreview(111, genre, barTrack, 4);
            PartTrack track2 = Generator.GenerateGroovePreview(222, genre, barTrack, 4);

            bool different = track1.PartTrackNoteEvents.Count != track2.PartTrackNoteEvents.Count;
            if (!different && track1.PartTrackNoteEvents.Count > 0)
            {
                for (int i = 0; i < Math.Min(10, track1.PartTrackNoteEvents.Count); i++)
                {
                    if (track1.PartTrackNoteEvents[i].AbsoluteTimeTicks != 
                        track2.PartTrackNoteEvents[i].AbsoluteTimeTicks)
                    {
                        different = true;
                        break;
                    }
                }
            }

            Assert.True(different, "Different seeds should produce different tracks");
        }

        [Fact]
        public void GenerateGroovePreview_MultipleCalls_AllIdenticalWithSameSeed()
        {
            int seed = 99999;
            string genre = "PopRock";
            BarTrack barTrack = CreateTestBarTrack(4);

            PartTrack[] tracks = new PartTrack[5];
            for (int i = 0; i < 5; i++)
            {
                tracks[i] = Generator.GenerateGroovePreview(seed, genre, barTrack, 4);
            }

            for (int i = 1; i < 5; i++)
            {
                Assert.Equal(tracks[0].PartTrackNoteEvents.Count, tracks[i].PartTrackNoteEvents.Count);
            }
        }

        #endregion

        #region Velocity Tests

        [Fact]
        public void GenerateGroovePreview_DefaultVelocity_UsesVelocity100()
        {
            BarTrack barTrack = CreateTestBarTrack(4);

            PartTrack partTrack = Generator.GenerateGroovePreview(123, "PopRock", barTrack, 4);

            Assert.All(partTrack.PartTrackNoteEvents, e =>
                Assert.Equal(100, e.NoteOnVelocity));
        }

        [Fact]
        public void GenerateGroovePreview_CustomVelocity_AppliedToAllEvents()
        {
            BarTrack barTrack = CreateTestBarTrack(4);
            int customVelocity = 75;

            PartTrack partTrack = Generator.GenerateGroovePreview(123, "PopRock", barTrack, 4, velocity: customVelocity);

            Assert.All(partTrack.PartTrackNoteEvents, e =>
                Assert.Equal(customVelocity, e.NoteOnVelocity));
        }

        #endregion

        #region Bar Count Tests

        [Fact]
        public void GenerateGroovePreview_SingleBar_ProducesCorrectEvents()
        {
            BarTrack barTrack = CreateTestBarTrack(1);

            PartTrack partTrack = Generator.GenerateGroovePreview(123, "PopRock", barTrack, 1);

            Assert.True(partTrack.PartTrackNoteEvents.Count > 0);
            Assert.True(partTrack.PartTrackNoteEvents.Count < 20); // Single bar shouldn't have too many events
        }

        [Fact]
        public void GenerateGroovePreview_MultipleBars_ScalesEventCount()
        {
            BarTrack barTrack1 = CreateTestBarTrack(2);
            BarTrack barTrack4 = CreateTestBarTrack(8);

            PartTrack track2Bars = Generator.GenerateGroovePreview(123, "PopRock", barTrack1, 2);
            PartTrack track8Bars = Generator.GenerateGroovePreview(123, "PopRock", barTrack4, 8);

            // 8 bars should have more events than 2 bars (approximately 4x)
            Assert.True(track8Bars.PartTrackNoteEvents.Count > track2Bars.PartTrackNoteEvents.Count);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void GenerateGroovePreview_IntegrationWithGrooveFactory_Works()
        {
            // Verify the method properly integrates with GrooveAnchorFactory.Generate
            BarTrack barTrack = CreateTestBarTrack(4);

            PartTrack partTrack = Generator.GenerateGroovePreview(456, "PopRock", barTrack, 4);

            // Should produce a valid playable track
            Assert.NotNull(partTrack);
            Assert.True(partTrack.PartTrackNoteEvents.Count > 0);
            Assert.Equal("Standard Kit", partTrack.MidiProgramName);
        }

        [Fact]
        public void GenerateGroovePreview_IntegrationWithToPartTrack_Works()
        {
            // Verify the method properly calls ToPartTrack
            BarTrack barTrack = CreateTestBarTrack(4);

            PartTrack partTrack = Generator.GenerateGroovePreview(789, "PopRock", barTrack, 4, velocity: 80);

            // All events should have the custom velocity
            Assert.All(partTrack.PartTrackNoteEvents, e =>
                Assert.Equal(80, e.NoteOnVelocity));
        }

        [Fact]
        public void GenerateGroovePreview_EventsSortedByTime()
        {
            BarTrack barTrack = CreateTestBarTrack(8);

            PartTrack partTrack = Generator.GenerateGroovePreview(555, "PopRock", barTrack, 8);

            for (int i = 1; i < partTrack.PartTrackNoteEvents.Count; i++)
            {
                Assert.True(partTrack.PartTrackNoteEvents[i].AbsoluteTimeTicks >= 
                          partTrack.PartTrackNoteEvents[i - 1].AbsoluteTimeTicks);
            }
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void GenerateGroovePreview_SeedZero_ProducesValidTrack()
        {
            BarTrack barTrack = CreateTestBarTrack(4);

            PartTrack partTrack = Generator.GenerateGroovePreview(0, "PopRock", barTrack, 4);

            Assert.NotNull(partTrack);
            Assert.True(partTrack.PartTrackNoteEvents.Count > 0);
        }

        [Fact]
        public void GenerateGroovePreview_NegativeSeed_ProducesValidTrack()
        {
            BarTrack barTrack = CreateTestBarTrack(4);

            PartTrack partTrack = Generator.GenerateGroovePreview(-12345, "PopRock", barTrack, 4);

            Assert.NotNull(partTrack);
            Assert.True(partTrack.PartTrackNoteEvents.Count > 0);
        }

        [Fact]
        public void GenerateGroovePreview_MaxIntSeed_ProducesValidTrack()
        {
            BarTrack barTrack = CreateTestBarTrack(4);

            PartTrack partTrack = Generator.GenerateGroovePreview(int.MaxValue, "PopRock", barTrack, 4);

            Assert.NotNull(partTrack);
            Assert.True(partTrack.PartTrackNoteEvents.Count > 0);
        }

        [Fact]
        public void GenerateGroovePreview_MinIntSeed_ProducesValidTrack()
        {
            BarTrack barTrack = CreateTestBarTrack(4);

            PartTrack partTrack = Generator.GenerateGroovePreview(int.MinValue, "PopRock", barTrack, 4);

            Assert.NotNull(partTrack);
            Assert.True(partTrack.PartTrackNoteEvents.Count > 0);
        }

        [Fact]
        public void GenerateGroovePreview_VelocityMin_AppliesCorrectly()
        {
            BarTrack barTrack = CreateTestBarTrack(2);

            PartTrack partTrack = Generator.GenerateGroovePreview(123, "PopRock", barTrack, 2, velocity: 1);

            Assert.All(partTrack.PartTrackNoteEvents, e =>
                Assert.Equal(1, e.NoteOnVelocity));
        }

        [Fact]
        public void GenerateGroovePreview_VelocityMax_AppliesCorrectly()
        {
            BarTrack barTrack = CreateTestBarTrack(2);

            PartTrack partTrack = Generator.GenerateGroovePreview(123, "PopRock", barTrack, 2, velocity: 127);

            Assert.All(partTrack.PartTrackNoteEvents, e =>
                Assert.Equal(127, e.NoteOnVelocity));
        }

        #endregion

        #region Practical Usage Tests

        [Fact]
        public void GenerateGroovePreview_TypicalAuditionWorkflow_ProducesPlayableTrack()
        {
            // Simulate typical audition workflow: user enters seed and generates 8-bar preview
            int userSeed = new Random().Next();
            BarTrack barTrack = CreateTestBarTrack(8);

            PartTrack partTrack = Generator.GenerateGroovePreview(userSeed, "PopRock", barTrack, 8);

            // Verify it's ready for playback
            Assert.NotNull(partTrack);
            Assert.True(partTrack.PartTrackNoteEvents.Count > 0);
            Assert.Equal("Standard Kit", partTrack.MidiProgramName);
            Assert.Equal(0, partTrack.MidiProgramNumber);
            
            // Events should be time-ordered
            for (int i = 1; i < partTrack.PartTrackNoteEvents.Count; i++)
            {
                Assert.True(partTrack.PartTrackNoteEvents[i].AbsoluteTimeTicks >= 
                          partTrack.PartTrackNoteEvents[i - 1].AbsoluteTimeTicks);
            }
        }

        #endregion
    }
}
