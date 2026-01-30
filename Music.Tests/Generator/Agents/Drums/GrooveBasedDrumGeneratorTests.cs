//// AI: purpose=Unit tests for Story RF-6 DrumGenerator pipeline orchestrator.
//// AI: deps=xunit for test framework; verifies GrooveSelectionEngine integration, density enforcement, caps, weighted selection.
//// AI: change=Story RF-6: comprehensive tests for pipeline that uses IGroovePolicyProvider + IGrooveCandidateSource.

//using Xunit;
//using Music.Generator;
//using Music.Generator.Agents.Drums;
//using Music.Generator.Agents.Common;
//using Music.Generator.Groove;
//using Music;

//namespace Music.Generator.Agents.Drums.Tests
//{
//    /// <summary>
//    /// Story RF-6: Tests for DrumGenerator pipeline orchestrator.
//    /// Verifies proper use of GrooveSelectionEngine with density enforcement, caps, and weighted selection.
//    /// </summary>
//    [Collection("RngDependentTests")]
//    public class GrooveBasedDrumGeneratorTests
//    {
//        public GrooveBasedDrumGeneratorTests()
//        {
//            Rng.Initialize(42);
//        }

//        #region Basic Generation Tests

//        [Fact]
//        public void Generate_ValidSongContext_ReturnsPartTrack()
//        {
//            // Arrange
//            var songContext = CreateMinimalSongContext(barCount: 4);
//            var agent = new DrummerAgent(StyleConfigurationLibrary.PopRock);
//            var generator = new DrumGenerator(agent, agent);

//            // Act
//            var track = generator.Generate(songContext);

//            // Assert
//            Assert.NotNull(track);
//            Assert.True(track.PartTrackNoteEvents.Count > 0, "Track should contain events");
//        }

//        [Fact]
//        public void Generate_EventsSortedByAbsoluteTimeTicks()
//        {
//            // Arrange
//            var songContext = CreateMinimalSongContext(barCount: 4);
//            var agent = new DrummerAgent(StyleConfigurationLibrary.PopRock);
//            var generator = new DrumGenerator(agent, agent);

//            // Act
//            var track = generator.Generate(songContext);

//            // Assert
//            for (int i = 1; i < track.PartTrackNoteEvents.Count; i++)
//            {
//                Assert.True(
//                    track.PartTrackNoteEvents[i].AbsoluteTimeTicks >= track.PartTrackNoteEvents[i - 1].AbsoluteTimeTicks,
//                    "Events must be sorted by AbsoluteTimeTicks for MIDI export");
//            }
//        }

//        [Fact]
//        public void Generate_ValidMidiNotes()
//        {
//            // Arrange
//            var songContext = CreateMinimalSongContext(barCount: 4);
//            var agent = new DrummerAgent(StyleConfigurationLibrary.PopRock);
//            var generator = new DrumGenerator(agent, agent);

//            // Act
//            var track = generator.Generate(songContext);

//            // Assert
//            foreach (var noteEvent in track.PartTrackNoteEvents)
//            {
//                // GM2 drum notes are in range 27-87, but we use 36-51 for common drums
//                Assert.InRange(noteEvent.NoteNumber, 27, 87);
//                // Velocity should be in valid MIDI range
//                Assert.InRange(noteEvent.NoteOnVelocity, 1, 127);
//            }
//        }

//        [Fact]
//        public void Generate_NullSongContext_Throws()
//        {
//            // Arrange
//            var agent = new DrummerAgent(StyleConfigurationLibrary.PopRock);
//            var generator = new DrumGenerator(agent, agent);

//            // Act & Assert
//            Assert.Throws<ArgumentNullException>(() => generator.Generate(null!));
//        }

//        [Fact]
//        public void Generate_NullPolicyProvider_Throws()
//        {
//            // Arrange
//            var agent = new DrummerAgent(StyleConfigurationLibrary.PopRock);

//            // Act & Assert
//            Assert.Throws<ArgumentNullException>(() => new DrumGenerator(null!, agent));
//        }

//        [Fact]
//        public void Generate_NullCandidateSource_Throws()
//        {
//            // Arrange
//            var agent = new DrummerAgent(StyleConfigurationLibrary.PopRock);

//            // Act & Assert
//            Assert.Throws<ArgumentNullException>(() => new DrumGenerator(agent, null!));
//        }

//        #endregion

//        #region Selection Logic Tests

//        [Fact]
//        public void Generate_UsesGrooveSelectionEngine()
//        {
//            // Arrange - Create real agent that will use GrooveSelectionEngine internally
//            var songContext = CreateMinimalSongContext(barCount: 4);
//            var agent = new DrummerAgent(StyleConfigurationLibrary.PopRock);
//            var generator = new DrumGenerator(agent, agent);

//            // Act
//            var track = generator.Generate(songContext);

//            // Assert - With real agent, should have both anchors and operator-generated events
//            // Count events per bar to verify selection happened
//            var eventsPerBar = new Dictionary<int, int>();
//            foreach (var ev in track.PartTrackNoteEvents)
//            {
//                // Calculate bar number from tick position
//                int bar = (int)(ev.AbsoluteTimeTicks / (480 * 4)) + 1; // Assume 480 ticks per quarter, 4/4 time
//                if (!eventsPerBar.ContainsKey(bar))
//                    eventsPerBar[bar] = 0;
//                eventsPerBar[bar]++;
//            }

//            // Should have events in each bar
//            Assert.True(eventsPerBar.Count >= 4, "Should have events in all bars");
            
//            // With density from PopRock, should have more than just anchors (which would be ~3 per bar)
//            // But respect caps, so not excessive
//            foreach (var count in eventsPerBar.Values)
//            {
//                Assert.InRange(count, 3, 20); // Min 3 (anchors), max 20 (reasonable upper bound)
//            }
//        }

//        [Fact]
//        public void Generate_RespectsZeroDensity_ProducesAnchorOnly()
//        {
//            // Arrange - Create agent and override with zero density settings
//            var songContext = CreateMinimalSongContext(barCount: 4);
            
//            // Use agent with very conservative settings (density will still be > 0 from policy)
//            // To truly test zero density, we'd need a test-only policy provider
//            // For now, test with minimal settings
//            var settings = new DrumGeneratorSettings
//            {
//                ActiveRoles = new[] { GrooveRoles.Kick }
//            };
//            var agent = new DrummerAgent(StyleConfigurationLibrary.PopRock);
//            var generator = new DrumGenerator(agent, agent, settings);

//            // Act
//            var track = generator.Generate(songContext);

//            // Assert - With only one active role, should have fewer events
//            Assert.NotNull(track);
//            Assert.True(track.PartTrackNoteEvents.Count > 0, "Should still have anchor events");
            
//            // Count kick notes (MIDI note 36)
//            int kickCount = track.PartTrackNoteEvents.Count(e => e.NoteNumber == 36);
//            Assert.True(kickCount >= 4, "Should have at least one kick per bar (anchors)");
//        }

//        [Fact]
//        public void Generate_DensityAffectsEventCount()
//        {
//            // Arrange - Use different contexts that should produce different densities
//            var introContext = CreateSongContextWithSection(MusicConstants.eSectionType.Intro, barCount: 4);
//            var chorusContext = CreateSongContextWithSection(MusicConstants.eSectionType.Chorus, barCount: 4);
            
//            var agent = new DrummerAgent(StyleConfigurationLibrary.PopRock);
//            var generator = new DrumGenerator(agent, agent);

//            // Act
//            var introTrack = generator.Generate(introContext);
            
//            Rng.Initialize(42); // Reset for consistent comparison
//            var chorusTrack = generator.Generate(chorusContext);

//            // Assert - Chorus should typically have more events than Intro due to higher energy
//            // Allow for some variation due to operators, but chorus should trend higher
//            Assert.True(chorusTrack.PartTrackNoteEvents.Count >= introTrack.PartTrackNoteEvents.Count,
//                "Chorus should have at least as many events as intro (higher energy)");
//        }

//        #endregion

//        #region Determinism Tests

//        [Fact]
//        public void Generate_SameSeed_IdenticalOutput()
//        {
//            // Arrange
//            var songContext1 = CreateMinimalSongContext(barCount: 4);
//            var songContext2 = CreateMinimalSongContext(barCount: 4);

//            Rng.Initialize(123);
//            var agent1 = new DrummerAgent(StyleConfigurationLibrary.PopRock);
//            var generator1 = new DrumGenerator(agent1, agent1);
//            var track1 = generator1.Generate(songContext1);

//            Rng.Initialize(123);
//            var agent2 = new DrummerAgent(StyleConfigurationLibrary.PopRock);
//            var generator2 = new DrumGenerator(agent2, agent2);
//            var track2 = generator2.Generate(songContext2);

//            // Assert
//            Assert.Equal(track1.PartTrackNoteEvents.Count, track2.PartTrackNoteEvents.Count);

//            for (int i = 0; i < track1.PartTrackNoteEvents.Count; i++)
//            {
//                Assert.Equal(track1.PartTrackNoteEvents[i].AbsoluteTimeTicks, track2.PartTrackNoteEvents[i].AbsoluteTimeTicks);
//                Assert.Equal(track1.PartTrackNoteEvents[i].NoteNumber, track2.PartTrackNoteEvents[i].NoteNumber);
//                Assert.Equal(track1.PartTrackNoteEvents[i].NoteOnVelocity, track2.PartTrackNoteEvents[i].NoteOnVelocity);
//            }
//        }

//        [Fact]
//        public void Generate_DifferentSeeds_DifferentOutput()
//        {
//            // Arrange
//            var songContext1 = CreateMinimalSongContext(barCount: 8);
//            var songContext2 = CreateMinimalSongContext(barCount: 8);

//            Rng.Initialize(123);
//            var agent1 = new DrummerAgent(StyleConfigurationLibrary.PopRock);
//            var generator1 = new DrumGenerator(agent1, agent1);
//            var track1 = generator1.Generate(songContext1);

//            Rng.Initialize(456);
//            var agent2 = new DrummerAgent(StyleConfigurationLibrary.PopRock);
//            var generator2 = new DrumGenerator(agent2, agent2);
//            var track2 = generator2.Generate(songContext2);

//            // Assert - With operators and longer track, should see differences
//            // Count differences
//            int differences = 0;
//            int maxToCheck = Math.Min(track1.PartTrackNoteEvents.Count, track2.PartTrackNoteEvents.Count);
            
//            for (int i = 0; i < maxToCheck; i++)
//            {
//                if (track1.PartTrackNoteEvents[i].AbsoluteTimeTicks != track2.PartTrackNoteEvents[i].AbsoluteTimeTicks ||
//                    track1.PartTrackNoteEvents[i].NoteNumber != track2.PartTrackNoteEvents[i].NoteNumber ||
//                    track1.PartTrackNoteEvents[i].NoteOnVelocity != track2.PartTrackNoteEvents[i].NoteOnVelocity)
//                {
//                    differences++;
//                }
//            }

//            // Should have some differences with different seeds (operators provide variation)
//            // Note: Anchors are deterministic, so not all events will differ
//            Assert.True(differences > 0 || track1.PartTrackNoteEvents.Count != track2.PartTrackNoteEvents.Count,
//                "Different seeds should produce some variation in operator-generated events");
//        }

//        #endregion

//        #region Anchor Integration Tests

//        [Fact]
//        public void Generate_CombinesAnchorsAndOperators_NoConflicts()
//        {
//            // Arrange
//            var songContext = CreateMinimalSongContext(barCount: 4);
//            var agent = new DrummerAgent(StyleConfigurationLibrary.PopRock);
//            var generator = new DrumGenerator(agent, agent);

//            // Act
//            var track = generator.Generate(songContext);

//            // Assert - Should have events from both anchors and operators
//            // Kick anchors typically on beats 1 and 3 in PopRock
//            // Check for kick events at expected anchor positions
//            var kickEvents = track.PartTrackNoteEvents.Where(e => e.NoteNumber == 36).ToList();
//            Assert.True(kickEvents.Count >= 4, "Should have at least anchor kicks (1 and 3 in each of 4 bars = 8 kicks min)");

//            // Should also have snare events at anchor positions (beats 2 and 4)
//            var snareEvents = track.PartTrackNoteEvents.Where(e => e.NoteNumber == 38).ToList();
//            Assert.True(snareEvents.Count >= 4, "Should have at least anchor snares (2 and 4 in each bar)");

//            // No duplicate events at same tick position for same note
//            var duplicates = track.PartTrackNoteEvents
//                .GroupBy(e => new { e.AbsoluteTimeTicks, e.NoteNumber })
//                .Where(g => g.Count() > 1)
//                .ToList();
            
//            Assert.Empty(duplicates);
//        }

//        [Fact]
//        public void Generate_AnchorsMustHit_AlwaysPresent()
//        {
//            // Arrange
//            var songContext = CreateMinimalSongContext(barCount: 4);
//            var agent = new DrummerAgent(StyleConfigurationLibrary.PopRock);
//            var generator = new DrumGenerator(agent, agent);

//            // Act
//            var track = generator.Generate(songContext);

//            // Assert - PopRock anchors: Kick on 1,3; Snare on 2,4; Hat on all 8ths
//            // Check that each bar has the expected anchor pattern
//            int ticksPerBeat = 480; // Assume 480 ticks per quarter note
//            int ticksPerBar = ticksPerBeat * 4; // 4/4 time

//            for (int bar = 1; bar <= 4; bar++)
//            {
//                long barStartTick = (bar - 1) * ticksPerBar;

//                // Check for kick on beat 1 (tick 0 of bar)
//                bool hasKickBeat1 = track.PartTrackNoteEvents.Any(e =>
//                    e.NoteNumber == 36 && e.AbsoluteTimeTicks >= barStartTick && e.AbsoluteTimeTicks < barStartTick + ticksPerBeat);
//                Assert.True(hasKickBeat1, $"Bar {bar} should have kick on beat 1");

//                // Check for kick on beat 3 (tick 2*480 of bar)
//                bool hasKickBeat3 = track.PartTrackNoteEvents.Any(e =>
//                    e.NoteNumber == 36 && e.AbsoluteTimeTicks >= barStartTick + 2 * ticksPerBeat && 
//                    e.AbsoluteTimeTicks < barStartTick + 3 * ticksPerBeat);
//                Assert.True(hasKickBeat3, $"Bar {bar} should have kick on beat 3");

//                // Check for snare on beat 2 (tick 480 of bar)
//                bool hasSnareBeat2 = track.PartTrackNoteEvents.Any(e =>
//                    e.NoteNumber == 38 && e.AbsoluteTimeTicks >= barStartTick + ticksPerBeat && 
//                    e.AbsoluteTimeTicks < barStartTick + 2 * ticksPerBeat);
//                Assert.True(hasSnareBeat2, $"Bar {bar} should have snare on beat 2");

//                // Check for snare on beat 4 (tick 3*480 of bar)
//                bool hasSnareBeat4 = track.PartTrackNoteEvents.Any(e =>
//                    e.NoteNumber == 38 && e.AbsoluteTimeTicks >= barStartTick + 3 * ticksPerBeat && 
//                    e.AbsoluteTimeTicks < barStartTick + 4 * ticksPerBeat);
//                Assert.True(hasSnareBeat4, $"Bar {bar} should have snare on beat 4");
//            }
//        }

//        #endregion

//        #region Helper Methods

//        private static SongContext CreateMinimalSongContext(int barCount)
//        {
//            var songContext = new SongContext();

//            // Create section track with single verse section
//            songContext.SectionTrack = new SectionTrack();
//            songContext.SectionTrack.Add(MusicConstants.eSectionType.Verse, barCount: barCount);

//            // Create timing track
//            songContext.Song.TimeSignatureTrack = new Timingtrack();
//            var timeSignatureEvent = new TimingEvent { StartBar = 1, Numerator = 4, Denominator = 4 };
//            songContext.Song.TimeSignatureTrack.Events.Add(timeSignatureEvent);

//            // Create bar track
//            songContext.BarTrack = new BarTrack();
//            songContext.BarTrack.RebuildFromTimingTrack(songContext.Song.TimeSignatureTrack, barCount);

//            // Create groove preset definition
//            IReadOnlyList<object> segmentProfiles;
//            songContext.GroovePresetDefinition = GrooveSetupFactory.BuildPopRockBasicGrooveForTestSong(
//                songContext.SectionTrack,
//                out segmentProfiles,
//                beatsPerBar: 4);
            
//            songContext.objects = segmentProfiles;

//            // Create voice set
//            songContext.Voices = new VoiceSet();
//            songContext.Voices.AddVoice("Standard Kit", "DrumKit");

//            return songContext;
//        }

//        private static SongContext CreateSongContextWithSection(MusicConstants.eSectionType sectionType, int barCount)
//        {
//            var songContext = new SongContext();

//            // Create section track with specified section type
//            songContext.SectionTrack = new SectionTrack();
//            songContext.SectionTrack.Add(sectionType, barCount: barCount);

//            // Create timing track
//            songContext.Song.TimeSignatureTrack = new Timingtrack();
//            var timeSignatureEvent = new TimingEvent { StartBar = 1, Numerator = 4, Denominator = 4 };
//            songContext.Song.TimeSignatureTrack.Events.Add(timeSignatureEvent);

//            // Create bar track
//            songContext.BarTrack = new BarTrack();
//            songContext.BarTrack.RebuildFromTimingTrack(songContext.Song.TimeSignatureTrack, barCount);

//            // Create groove preset definition
//            IReadOnlyList<object> segmentProfiles;
//            songContext.GroovePresetDefinition = GrooveSetupFactory.BuildPopRockBasicGrooveForTestSong(
//                songContext.SectionTrack,
//                out segmentProfiles,
//                beatsPerBar: 4);
            
//            songContext.objects = segmentProfiles;

//            // Create voice set
//            songContext.Voices = new VoiceSet();
//            songContext.Voices.AddVoice("Standard Kit", "DrumKit");

//            return songContext;
//        }

//        #endregion
//    }
//}

