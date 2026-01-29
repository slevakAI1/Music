namespace Music.Tests.Generator.Groove
{
    using Music.Generator;
    using Music.Generator.Groove;
    using Music.MyMidi;

    // AI: purpose=Unit tests for GrooveInstanceLayer.ToPartTrack conversion to playable MIDI drum track.
    // AI: invariants=Tests verify MIDI note mapping, timing conversion, event sorting, velocity application.
    public sealed class GrooveInstanceLayerToPartTrackTests
    {
        #region Helper Methods

        private static BarTrack CreateTestBarTrack(int totalBars = 4)
        {
            Timingtrack timingTrack = TimingTests.CreateTestTrackD1();
            BarTrack barTrack = new();
            barTrack.RebuildFromTimingTrack(timingTrack, totalBars);
            return barTrack;
        }

        private static GrooveInstanceLayer CreateBasicGroove()
        {
            return new GrooveInstanceLayer
            {
                KickOnsets = new List<decimal> { 1m, 3m },
                SnareOnsets = new List<decimal> { 2m, 4m },
                HatOnsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m }
            };
        }

        #endregion

        #region ToPartTrack Basic Tests

        [Fact]
        public void ToPartTrack_NullBarTrack_ThrowsArgumentNullException()
        {
            GrooveInstanceLayer groove = CreateBasicGroove();

            Assert.Throws<ArgumentNullException>(() =>
                groove.ToPartTrack(null!, 4));
        }

        [Fact]
        public void ToPartTrack_ZeroBars_ThrowsArgumentOutOfRangeException()
        {
            GrooveInstanceLayer groove = CreateBasicGroove();
            BarTrack barTrack = CreateTestBarTrack();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                groove.ToPartTrack(barTrack, 0));
        }

        [Fact]
        public void ToPartTrack_NegativeBars_ThrowsArgumentOutOfRangeException()
        {
            GrooveInstanceLayer groove = CreateBasicGroove();
            BarTrack barTrack = CreateTestBarTrack();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                groove.ToPartTrack(barTrack, -1));
        }

        [Fact]
        public void ToPartTrack_VelocityTooLow_ThrowsArgumentOutOfRangeException()
        {
            GrooveInstanceLayer groove = CreateBasicGroove();
            BarTrack barTrack = CreateTestBarTrack();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                groove.ToPartTrack(barTrack, 4, velocity: 0));
        }

        [Fact]
        public void ToPartTrack_VelocityTooHigh_ThrowsArgumentOutOfRangeException()
        {
            GrooveInstanceLayer groove = CreateBasicGroove();
            BarTrack barTrack = CreateTestBarTrack();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                groove.ToPartTrack(barTrack, 4, velocity: 128));
        }

        [Fact]
        public void ToPartTrack_ValidInput_ReturnsPartTrack()
        {
            GrooveInstanceLayer groove = CreateBasicGroove();
            BarTrack barTrack = CreateTestBarTrack();

            PartTrack partTrack = groove.ToPartTrack(barTrack, 4);

            Assert.NotNull(partTrack);
            Assert.NotNull(partTrack.PartTrackNoteEvents);
        }

        [Fact]
        public void ToPartTrack_ValidInput_SetsStandardKit()
        {
            GrooveInstanceLayer groove = CreateBasicGroove();
            BarTrack barTrack = CreateTestBarTrack();

            PartTrack partTrack = groove.ToPartTrack(barTrack, 4);

            Assert.Equal("Standard Kit", partTrack.MidiProgramName);
            Assert.Equal(0, partTrack.MidiProgramNumber);
        }

        #endregion

        #region MIDI Note Mapping Tests

        [Fact]
        public void ToPartTrack_KickOnsets_MappedToNote36()
        {
            GrooveInstanceLayer groove = new()
            {
                KickOnsets = new List<decimal> { 1m, 2m }
            };
            BarTrack barTrack = CreateTestBarTrack(1);

            PartTrack partTrack = groove.ToPartTrack(barTrack, 1);

            var kickEvents = partTrack.PartTrackNoteEvents.Where(e => e.NoteNumber == 36).ToList();
            Assert.Equal(2, kickEvents.Count);
        }

        [Fact]
        public void ToPartTrack_SnareOnsets_MappedToNote38()
        {
            GrooveInstanceLayer groove = new()
            {
                SnareOnsets = new List<decimal> { 2m, 4m }
            };
            BarTrack barTrack = CreateTestBarTrack(1);

            PartTrack partTrack = groove.ToPartTrack(barTrack, 1);

            var snareEvents = partTrack.PartTrackNoteEvents.Where(e => e.NoteNumber == 38).ToList();
            Assert.Equal(2, snareEvents.Count);
        }

        [Fact]
        public void ToPartTrack_HatOnsets_MappedToNote42()
        {
            GrooveInstanceLayer groove = new()
            {
                HatOnsets = new List<decimal> { 1m, 1.5m, 2m }
            };
            BarTrack barTrack = CreateTestBarTrack(1);

            PartTrack partTrack = groove.ToPartTrack(barTrack, 1);

            var hatEvents = partTrack.PartTrackNoteEvents.Where(e => e.NoteNumber == 42).ToList();
            Assert.Equal(3, hatEvents.Count);
        }

        #endregion

        #region Timing Conversion Tests

        [Fact]
        public void ToPartTrack_Beat1_StartsAtBarStartTick()
        {
            GrooveInstanceLayer groove = new()
            {
                KickOnsets = new List<decimal> { 1m }
            };
            BarTrack barTrack = CreateTestBarTrack(1);

            PartTrack partTrack = groove.ToPartTrack(barTrack, 1);

            var firstEvent = partTrack.PartTrackNoteEvents.First();
            Assert.Equal(0, firstEvent.AbsoluteTimeTicks);
        }

        [Fact]
        public void ToPartTrack_MultipleBeats_ConvertedCorrectly()
        {
            GrooveInstanceLayer groove = new()
            {
                KickOnsets = new List<decimal> { 1m, 2m, 3m, 4m }
            };
            BarTrack barTrack = CreateTestBarTrack(1);

            PartTrack partTrack = groove.ToPartTrack(barTrack, 1);

            var events = partTrack.PartTrackNoteEvents.OrderBy(e => e.AbsoluteTimeTicks).ToList();
            Assert.Equal(4, events.Count);

            for (int i = 0; i < events.Count; i++)
            {
                Assert.True(events[i].AbsoluteTimeTicks >= 0);
                if (i > 0)
                {
                    Assert.True(events[i].AbsoluteTimeTicks > events[i - 1].AbsoluteTimeTicks);
                }
            }
        }

        #endregion

        #region Velocity Tests

        [Fact]
        public void ToPartTrack_DefaultVelocity_SetsTo100()
        {
            GrooveInstanceLayer groove = new()
            {
                KickOnsets = new List<decimal> { 1m }
            };
            BarTrack barTrack = CreateTestBarTrack(1);

            PartTrack partTrack = groove.ToPartTrack(barTrack, 1);

            var firstEvent = partTrack.PartTrackNoteEvents.First();
            Assert.Equal(100, firstEvent.NoteOnVelocity);
        }

        [Fact]
        public void ToPartTrack_CustomVelocity_AppliedToAllEvents()
        {
            GrooveInstanceLayer groove = CreateBasicGroove();
            BarTrack barTrack = CreateTestBarTrack(1);
            int customVelocity = 80;

            PartTrack partTrack = groove.ToPartTrack(barTrack, 1, velocity: customVelocity);

            Assert.All(partTrack.PartTrackNoteEvents, e =>
                Assert.Equal(customVelocity, e.NoteOnVelocity));
        }

        #endregion

        #region Multi-Bar Tests

        [Fact]
        public void ToPartTrack_MultipleBars_RepeatsPattern()
        {
            GrooveInstanceLayer groove = new()
            {
                KickOnsets = new List<decimal> { 1m, 3m }
            };
            BarTrack barTrack = CreateTestBarTrack(4);

            PartTrack partTrack = groove.ToPartTrack(barTrack, 4);

            var kickEvents = partTrack.PartTrackNoteEvents.Where(e => e.NoteNumber == 36).ToList();
            Assert.Equal(8, kickEvents.Count); // 2 kicks per bar × 4 bars
        }

        [Fact]
        public void ToPartTrack_MultipleBars_EventsIncreaseTiming()
        {
            GrooveInstanceLayer groove = new()
            {
                KickOnsets = new List<decimal> { 1m }
            };
            BarTrack barTrack = CreateTestBarTrack(4);

            PartTrack partTrack = groove.ToPartTrack(barTrack, 4);

            var events = partTrack.PartTrackNoteEvents.OrderBy(e => e.AbsoluteTimeTicks).ToList();
            for (int i = 1; i < events.Count; i++)
            {
                Assert.True(events[i].AbsoluteTimeTicks > events[i - 1].AbsoluteTimeTicks);
            }
        }

        #endregion

        #region Event Sorting Tests

        [Fact]
        public void ToPartTrack_EventsSortedByAbsoluteTicks()
        {
            GrooveInstanceLayer groove = CreateBasicGroove();
            BarTrack barTrack = CreateTestBarTrack(2);

            PartTrack partTrack = groove.ToPartTrack(barTrack, 2);

            var events = partTrack.PartTrackNoteEvents;
            for (int i = 1; i < events.Count; i++)
            {
                Assert.True(events[i].AbsoluteTimeTicks >= events[i - 1].AbsoluteTimeTicks,
                    $"Event {i} (tick {events[i].AbsoluteTimeTicks}) should be >= event {i - 1} (tick {events[i - 1].AbsoluteTimeTicks})");
            }
        }

        #endregion

        #region Empty/Minimal Groove Tests

        [Fact]
        public void ToPartTrack_EmptyGroove_ReturnsEmptyTrack()
        {
            GrooveInstanceLayer groove = new();
            BarTrack barTrack = CreateTestBarTrack(4);

            PartTrack partTrack = groove.ToPartTrack(barTrack, 4);

            Assert.Empty(partTrack.PartTrackNoteEvents);
        }

        [Fact]
        public void ToPartTrack_OnlyKicks_ReturnsOnlyKickEvents()
        {
            GrooveInstanceLayer groove = new()
            {
                KickOnsets = new List<decimal> { 1m, 3m }
            };
            BarTrack barTrack = CreateTestBarTrack(1);

            PartTrack partTrack = groove.ToPartTrack(barTrack, 1);

            Assert.All(partTrack.PartTrackNoteEvents, e =>
                Assert.Equal(36, e.NoteNumber));
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void ToPartTrack_FullGroove_ProducesCorrectEventCount()
        {
            GrooveInstanceLayer groove = CreateBasicGroove();
            BarTrack barTrack = CreateTestBarTrack(1);

            PartTrack partTrack = groove.ToPartTrack(barTrack, 1);

            // 2 kicks + 2 snares + 8 hats = 12 events per bar
            Assert.Equal(12, partTrack.PartTrackNoteEvents.Count);
        }

        [Fact]
        public void ToPartTrack_WithGenerate_ProducesPlayableTrack()
        {
            GrooveInstanceLayer groove = GrooveAnchorFactory.Generate("PopRock", 123);
            BarTrack barTrack = CreateTestBarTrack(8);

            PartTrack partTrack = groove.ToPartTrack(barTrack, 8);

            Assert.NotNull(partTrack);
            Assert.True(partTrack.PartTrackNoteEvents.Count > 0);
            Assert.Equal("Standard Kit", partTrack.MidiProgramName);
        }

        [Fact]
        public void ToPartTrack_DifferentSeeds_ProduceDifferentTracks()
        {
            GrooveInstanceLayer groove1 = GrooveAnchorFactory.Generate("PopRock", 111);
            GrooveInstanceLayer groove2 = GrooveAnchorFactory.Generate("PopRock", 222);
            BarTrack barTrack = CreateTestBarTrack(4);

            PartTrack partTrack1 = groove1.ToPartTrack(barTrack, 4);
            PartTrack partTrack2 = groove2.ToPartTrack(barTrack, 4);

            // Different grooves should produce different event counts (variations differ)
            bool differentEventCounts = partTrack1.PartTrackNoteEvents.Count != partTrack2.PartTrackNoteEvents.Count;
            Assert.True(differentEventCounts || HasDifferentTimings(partTrack1, partTrack2),
                "Different seeds should produce different tracks");
        }

        private static bool HasDifferentTimings(PartTrack track1, PartTrack track2)
        {
            if (track1.PartTrackNoteEvents.Count != track2.PartTrackNoteEvents.Count)
                return true;

            for (int i = 0; i < Math.Min(track1.PartTrackNoteEvents.Count, track2.PartTrackNoteEvents.Count); i++)
            {
                if (track1.PartTrackNoteEvents[i].AbsoluteTimeTicks != track2.PartTrackNoteEvents[i].AbsoluteTimeTicks)
                    return true;
            }
            return false;
        }

        #endregion

        #region Event Properties Tests

        [Fact]
        public void ToPartTrack_EventDuration_SetToFixed120Ticks()
        {
            GrooveInstanceLayer groove = new()
            {
                KickOnsets = new List<decimal> { 1m }
            };
            BarTrack barTrack = CreateTestBarTrack(1);

            PartTrack partTrack = groove.ToPartTrack(barTrack, 1);

            var firstEvent = partTrack.PartTrackNoteEvents.First();
            Assert.Equal(120, firstEvent.NoteDurationTicks);
        }

        [Fact]
        public void ToPartTrack_EventType_SetToNoteOn()
        {
            GrooveInstanceLayer groove = new()
            {
                KickOnsets = new List<decimal> { 1m }
            };
            BarTrack barTrack = CreateTestBarTrack(1);

            PartTrack partTrack = groove.ToPartTrack(barTrack, 1);

            var firstEvent = partTrack.PartTrackNoteEvents.First();
            Assert.Equal(PartTrackEventType.NoteOn, firstEvent.Type);
        }

        #endregion
    }
}

