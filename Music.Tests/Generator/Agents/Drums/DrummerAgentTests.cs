//// AI: purpose=Unit tests for Story RF-5 DrummerAgent as data source (no Generate method).
//// AI: deps=xunit for test framework; Music.Generator.Agents.Drums for types under test.
//// AI: change=Story RF-5: DrummerAgent is pure data source; tests verify interface delegation only.

//using Xunit;
//using Music.Generator;
//using Music.Generator.Agents.Drums;
//using Music.Generator.Agents.Common;
//using Music.Generator.Agents.Drums.Physicality;
//using Music.Generator.Groove;
//using Music;

//namespace Music.Generator.Agents.Drums.Tests
//{
//    /// <summary>
//    /// Story RF-5: Tests for DrummerAgent as data source.
//    /// Verifies construction and delegation to IGroovePolicyProvider + IGrooveCandidateSource.
//    /// DrummerAgent does NOT generate PartTracks directly - use DrumGenerator pipeline.
//    /// </summary>
//    [Collection("RngDependentTests")]
//    public class DrummerAgentTests
//    {
//        public DrummerAgentTests()
//        {
//            Rng.Initialize(42);
//        }

//        #region Construction Tests

//        [Fact]
//        public void Constructor_WithValidStyleConfig_Succeeds()
//        {
//            // Arrange
//            var style = StyleConfigurationLibrary.PopRock;

//            // Act
//            var agent = new DrummerAgent(style);

//            // Assert
//            Assert.NotNull(agent);
//            Assert.Same(style, agent.StyleConfiguration);
//        }

//        [Fact]
//        public void Constructor_NullStyleConfig_Throws()
//        {
//            // Act & Assert
//            Assert.Throws<ArgumentNullException>(() => new DrummerAgent(null!));
//        }

//        [Fact]
//        public void Constructor_InitializesRegistry()
//        {
//            // Arrange
//            var style = StyleConfigurationLibrary.PopRock;

//            // Act
//            var agent = new DrummerAgent(style);

//            // Assert
//            Assert.NotNull(agent.Registry);
//            Assert.Equal(28, agent.Registry.Count);
//        }

//        [Fact]
//        public void Constructor_InitializesMemory()
//        {
//            // Arrange
//            var style = StyleConfigurationLibrary.PopRock;

//            // Act
//            var agent = new DrummerAgent(style);

//            // Assert
//            Assert.NotNull(agent.Memory);
//        }

//        #endregion

//        #region IGroovePolicyProvider Delegation Tests

//        [Fact]
//        public void GetPolicy_DelegatesToPolicyProvider_Correctly()
//        {
//            // Arrange
//            var agent = CreateAgent();
//            var barContext = CreateBarContext(barNumber: 1);

//            // Act
//            var policy = agent.GetPolicy(barContext, GrooveRoles.Kick);

//            // Assert
//            Assert.NotNull(policy);
//            Assert.NotNull(policy.Density01Override);
//        }

//        [Fact]
//        public void GetPolicy_DifferentContexts_ProduceDifferentPolicies()
//        {
//            // Arrange
//            var agent = CreateAgent();
//            var verseContext = CreateBarContext(barNumber: 1, MusicConstants.eSectionType.Verse);
//            var chorusContext = CreateBarContext(barNumber: 10, MusicConstants.eSectionType.Chorus);

//            // Act
//            var versePolicy = agent.GetPolicy(verseContext, GrooveRoles.Kick);
//            var chorusPolicy = agent.GetPolicy(chorusContext, GrooveRoles.Kick);

//            // Assert - Chorus should typically have higher density than Verse
//            Assert.NotNull(versePolicy);
//            Assert.NotNull(chorusPolicy);
//            Assert.True(chorusPolicy.Density01Override > versePolicy.Density01Override,
//                "Chorus density should be higher than verse density");
//        }

//        [Fact]
//        public void GetPolicy_NullBarContext_Throws()
//        {
//            // Arrange
//            var agent = CreateAgent();

//            // Act & Assert
//            Assert.Throws<ArgumentNullException>(() => agent.GetPolicy(null!, GrooveRoles.Kick));
//        }

//        [Fact]
//        public void GetPolicy_NullRole_Throws()
//        {
//            // Arrange
//            var agent = CreateAgent();
//            var barContext = CreateBarContext(barNumber: 1);

//            // Act & Assert
//            Assert.Throws<ArgumentNullException>(() => agent.GetPolicy(barContext, null!));
//        }

//        [Fact]
//        public void GetPolicy_Deterministic_SameInputsSameOutput()
//        {
//            // Arrange
//            Rng.Initialize(42);
//            var agent1 = CreateAgent();
//            var barContext1 = CreateBarContext(barNumber: 1);

//            Rng.Initialize(42);
//            var agent2 = CreateAgent();
//            var barContext2 = CreateBarContext(barNumber: 1);

//            // Act
//            var policy1 = agent1.GetPolicy(barContext1, GrooveRoles.Snare);
//            var policy2 = agent2.GetPolicy(barContext2, GrooveRoles.Snare);

//            // Assert
//            Assert.Equal(policy1?.Density01Override, policy2?.Density01Override);
//            Assert.Equal(policy1?.MaxEventsPerBarOverride, policy2?.MaxEventsPerBarOverride);
//        }

//        [Fact]
//        public void GetPolicy_UsesSharedMemory_AcrossCalls()
//        {
//            // Arrange
//            var agent = CreateAgent();
//            var barContext1 = CreateBarContext(barNumber: 1);
//            var barContext2 = CreateBarContext(barNumber: 2);

//            // Act - Make multiple calls
//            var policy1 = agent.GetPolicy(barContext1, GrooveRoles.Kick);
//            var policy2 = agent.GetPolicy(barContext2, GrooveRoles.Kick);

//            // Assert - Memory should be shared and updated
//            Assert.NotNull(agent.Memory);
//            // Memory's CurrentBarNumber should reflect the last processed bar
//            Assert.True(agent.Memory.CurrentBarNumber >= 1, "Memory should track bar numbers");
//        }

//        #endregion

//        #region IGrooveCandidateSource Delegation Tests

//        [Fact]
//        public void GetCandidateGroups_DelegatesToCandidateSource_Correctly()
//        {
//            // Arrange
//            var agent = CreateAgent();
//            var barContext = CreateBarContext(barNumber: 1);

//            // Act
//            var groups = agent.GetCandidateGroups(barContext, GrooveRoles.Kick);

//            // Assert
//            Assert.NotNull(groups);
//            // Should have candidate groups from operators
//            Assert.True(groups.Count > 0, "Should have candidate groups");
//            // Verify candidates have valid properties
//            if (groups.Count > 0 && groups[0].Candidates.Count > 0)
//            {
//                var candidate = groups[0].Candidates[0];
//                Assert.NotNull(candidate.Role);
//                Assert.True(candidate.OnsetBeat > 0, "Onset beat should be positive");
//            }
//        }

//        [Fact]
//        public void GetCandidateGroups_DifferentRoles_ProduceDifferentCandidates()
//        {
//            // Arrange
//            var agent = CreateAgent();
//            var barContext = CreateBarContext(barNumber: 1);

//            // Act
//            var kickGroups = agent.GetCandidateGroups(barContext, GrooveRoles.Kick);
//            var snareGroups = agent.GetCandidateGroups(barContext, GrooveRoles.Snare);

//            // Assert
//            Assert.NotNull(kickGroups);
//            Assert.NotNull(snareGroups);
//            // Different roles should produce candidates for their respective roles
//            if (kickGroups.Count > 0 && kickGroups[0].Candidates.Count > 0)
//            {
//                Assert.Equal(GrooveRoles.Kick, kickGroups[0].Candidates[0].Role);
//            }
//            if (snareGroups.Count > 0 && snareGroups[0].Candidates.Count > 0)
//            {
//                Assert.Equal(GrooveRoles.Snare, snareGroups[0].Candidates[0].Role);
//            }
//        }

//        [Fact]
//        public void GetCandidateGroups_NullBarContext_Throws()
//        {
//            // Arrange
//            var agent = CreateAgent();

//            // Act & Assert
//            Assert.Throws<ArgumentNullException>(() => agent.GetCandidateGroups(null!, GrooveRoles.Kick));
//        }

//        [Fact]
//        public void GetCandidateGroups_NullRole_Throws()
//        {
//            // Arrange
//            var agent = CreateAgent();
//            var barContext = CreateBarContext(barNumber: 1);

//            // Act & Assert
//            Assert.Throws<ArgumentNullException>(() => agent.GetCandidateGroups(barContext, null!));
//        }

//        [Fact]
//        public void GetCandidateGroups_RespectsPhysicality_WhenConfigured()
//        {
//            // Arrange - Create agent with physicality rules
//            var physicalityRules = new PhysicalityRules
//            {
//                MaxHitsPerBar = 8,
//                StrictnessLevel = PhysicalityStrictness.Normal
//            };
//            var settings = new DrummerAgentSettings
//            {
//                PhysicalityRules = physicalityRules
//            };
//            var agent = new DrummerAgent(StyleConfigurationLibrary.PopRock, settings);
//            var barContext = CreateBarContext(barNumber: 1);

//            // Act
//            var groups = agent.GetCandidateGroups(barContext, GrooveRoles.Kick);

//            // Assert
//            Assert.NotNull(groups);
//            // Count total candidates across all groups
//            int totalCandidates = groups.Sum(g => g.Candidates.Count);
//            // With physicality filtering, we shouldn't get candidates that violate rules
//            Assert.True(totalCandidates <= 20, "Physicality filter should limit candidate count");
//        }

//        #endregion

//        #region Generator Integration Tests

//        [Fact]
//        public void Generator_WithStyleConfiguration_UsesPipeline()
//        {
//            // Arrange
//            var style = StyleConfigurationLibrary.PopRock;
//            var songContext = CreateTestSongContext();

//            // Act
//            var track = Generator.Generate(songContext, style);

//            // Assert
//            Assert.NotNull(track);
//            Assert.True(track.PartTrackNoteEvents.Count > 0, "Should generate events via pipeline");
//        }

//        [Fact]
//        public void Generator_WithNullStyle_FallsBackToGrooveGenerator()
//        {
//            // Arrange
//            var songContext = CreateTestSongContext();

//            // Act
//            var track = Generator.Generate(songContext, drummerStyle: null);

//            // Assert
//            Assert.NotNull(track);
//            Assert.True(track.PartTrackNoteEvents.Count > 0);
//        }

//        [Fact]
//        public void Generator_OriginalSignature_StillWorks()
//        {
//            // Arrange
//            var songContext = CreateTestSongContext();

//            // Act
//            var track = Generator.Generate(songContext);

//            // Assert
//            Assert.NotNull(track);
//        }

//        #endregion

//        #region Memory Tests

//        [Fact]
//        public void ResetMemory_ClearsMemory()
//        {
//            // Arrange
//            var agent = CreateAgent();
//            var barContext = CreateBarContext(barNumber: 5);
            
//            // Call GetPolicy to populate memory
//            agent.GetPolicy(barContext, GrooveRoles.Kick);

//            // Act
//            agent.ResetMemory();

//            // Assert - memory should be cleared
//            Assert.Equal(0, agent.Memory.CurrentBarNumber);
//        }

//        #endregion

//        #region Helper Methods

//        private static DrummerAgent CreateAgent()
//        {
//            return new DrummerAgent(StyleConfigurationLibrary.PopRock);
//        }

//        private static DrumBarContext CreateBarContext(int barNumber, MusicConstants.eSectionType? sectionType = null)
//        {
//            var section = new Section
//            {
//                SectionId = 1,
//                SectionType = sectionType ?? MusicConstants.eSectionType.Verse,
//                StartBar = 1,
//                BarCount = 8
//            };

//            return new DrumBarContext(
//                barNumber,
//                section,
//                null,
//                barNumber - 1,
//                8 - barNumber);
//        }

//        private static SongContext CreateTestSongContext()
//        {
//            var songContext = new SongContext();

//            // Create section track
//            songContext.SectionTrack = new SectionTrack();
//            songContext.SectionTrack.Add(MusicConstants.eSectionType.Intro, barCount: 4);
//            songContext.SectionTrack.Add(MusicConstants.eSectionType.Verse, barCount: 8);
//            songContext.SectionTrack.Add(MusicConstants.eSectionType.Chorus, barCount: 8);
//            songContext.SectionTrack.Add(MusicConstants.eSectionType.Outro, barCount: 4);

//            // Create timing track
//            songContext.Song.TimeSignatureTrack = new Timingtrack();
//            var timeSignatureEvent = new TimingEvent { StartBar = 1, Numerator = 4, Denominator = 4 };
//            songContext.Song.TimeSignatureTrack.Events.Add(timeSignatureEvent);

//            // Create bar track
//            int totalBars = songContext.SectionTrack.TotalBars;
//            songContext.BarTrack = new BarTrack();
//            songContext.BarTrack.RebuildFromTimingTrack(songContext.Song.TimeSignatureTrack, totalBars);

//            // Create groove preset definition using library
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


