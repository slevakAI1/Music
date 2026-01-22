// AI: purpose=Unit tests for Story 3.4 PatternSubstitution operators; verifies pattern generation and section/energy gating.
// AI: deps=xunit for test framework; Music.Generator.Agents.Drums.Operators.PatternSubstitution for operators under test.
// AI: change=Story 3.4 acceptance criteria: operators generate correct patterns, respect section type and energy thresholds.

using Xunit;
using Music.Generator.Agents.Drums;
using Music.Generator.Agents.Drums.Operators.PatternSubstitution;
using Music.Generator;
using Music;

namespace Music.Generator.Agents.Drums.Operators.Tests
{
    /// <summary>
    /// Story 3.4: Tests for PatternSubstitution operators.
    /// Verifies pattern generation, section/energy gating, articulation variants, and determinism.
    /// </summary>
    [Collection("RngDependentTests")]
    public class PatternSubstitutionOperatorTests
    {
        public PatternSubstitutionOperatorTests()
        {
            Rng.Initialize(42);
        }

        #region BackbeatVariantOperator Tests

        [Fact]
        public void BackbeatVariant_GeneratesSideStick_WhenInVerseWithLowEnergy()
        {
            // Arrange
            var op = new BackbeatVariantOperator();
            var context = CreateContext(
                energy: 0.4,
                sectionType: MusicConstants.eSectionType.Verse,
                activeRoles: new HashSet<string> { GrooveRoles.Snare });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - sidestick for verse with lower energy
            Assert.NotEmpty(candidates);
            Assert.All(candidates, c =>
            {
                Assert.Equal(GrooveRoles.Snare, c.Role);
                Assert.Equal(DrumArticulation.SideStick, c.ArticulationHint);
            });
        }

        [Fact]
        public void BackbeatVariant_GeneratesRimshot_WhenInChorusWithHighEnergy()
        {
            // Arrange
            var op = new BackbeatVariantOperator();
            var context = CreateContext(
                energy: 0.8,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Snare });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - rimshot for chorus with high energy
            Assert.NotEmpty(candidates);
            Assert.All(candidates, c =>
            {
                Assert.Equal(GrooveRoles.Snare, c.Role);
                Assert.Equal(DrumArticulation.Rimshot, c.ArticulationHint);
            });
        }

        [Fact]
        public void BackbeatVariant_GeneratesFlam_WhenInBridgeWithHighEnergy()
        {
            // Arrange
            var op = new BackbeatVariantOperator();
            var context = CreateContext(
                energy: 0.6,
                sectionType: MusicConstants.eSectionType.Bridge,
                activeRoles: new HashSet<string> { GrooveRoles.Snare });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - flam for bridge with higher energy
            Assert.NotEmpty(candidates);
            Assert.All(candidates, c =>
            {
                Assert.Equal(GrooveRoles.Snare, c.Role);
                Assert.Equal(DrumArticulation.Flam, c.ArticulationHint);
            });
        }

        [Fact]
        public void BackbeatVariant_GeneratesOnBackbeats_InFourFour()
        {
            // Arrange
            var op = new BackbeatVariantOperator();
            var context = CreateContext(
                energy: 0.8,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                beatsPerBar: 4);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - should have candidates on beats 2 and 4
            Assert.Equal(2, candidates.Count);
            Assert.Contains(candidates, c => c.Beat == 2m);
            Assert.Contains(candidates, c => c.Beat == 4m);
        }

        [Fact]
        public void BackbeatVariant_ReturnsEmpty_WhenSnareNotActive()
        {
            // Arrange
            var op = new BackbeatVariantOperator();
            var context = CreateContext(
                energy: 0.7,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Kick });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void BackbeatVariant_ReturnsEmpty_WhenInFillWindow()
        {
            // Arrange
            var op = new BackbeatVariantOperator();
            var context = CreateContext(
                energy: 0.7,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                isFillWindow: true);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void BackbeatVariant_ReturnsEmpty_WhenEnergyBelowThreshold()
        {
            // Arrange
            var op = new BackbeatVariantOperator();
            var context = CreateContext(
                energy: 0.2, // Below 0.3 threshold
                sectionType: MusicConstants.eSectionType.Verse,
                activeRoles: new HashSet<string> { GrooveRoles.Snare });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void BackbeatVariant_HasHigherScore_AtSectionBoundary()
        {
            // Arrange
            var op = new BackbeatVariantOperator();
            var contextBoundary = CreateContext(
                energy: 0.8,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                isAtSectionBoundary: true);

            var contextMid = CreateContext(
                energy: 0.8,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                isAtSectionBoundary: false);

            // Act
            var candidatesBoundary = op.GenerateCandidates(contextBoundary).ToList();
            var candidatesMid = op.GenerateCandidates(contextMid).ToList();

            // Assert - boundary candidates should score higher
            Assert.NotEmpty(candidatesBoundary);
            Assert.NotEmpty(candidatesMid);
            Assert.True(candidatesBoundary.First().Score > candidatesMid.First().Score);
        }

        #endregion

        #region KickPatternVariantOperator Tests

        [Fact]
        public void KickPatternVariant_GeneratesFourOnFloor_WhenInChorusWithHighEnergy()
        {
            // Arrange
            var op = new KickPatternVariantOperator();
            var context = CreateContext(
                energy: 0.8,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                beatsPerBar: 4);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - four-on-floor should have kick on every beat
            Assert.Equal(4, candidates.Count);
            Assert.Contains(candidates, c => c.Beat == 1m);
            Assert.Contains(candidates, c => c.Beat == 2m);
            Assert.Contains(candidates, c => c.Beat == 3m);
            Assert.Contains(candidates, c => c.Beat == 4m);
            Assert.All(candidates, c => Assert.Equal(GrooveRoles.Kick, c.Role));
        }

        [Fact]
        public void KickPatternVariant_GeneratesSyncopated_WhenInVerseWithModerateEnergy()
        {
            // Arrange
            var op = new KickPatternVariantOperator();
            var context = CreateContext(
                energy: 0.5, // Moderate energy (0.4-0.7 range)
                sectionType: MusicConstants.eSectionType.Verse,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                beatsPerBar: 4);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - syncopated pattern has offbeat (2.5)
            Assert.NotEmpty(candidates);
            Assert.Contains(candidates, c => c.Beat == 1m); // Beat 1
            Assert.Contains(candidates, c => c.Beat == 2.5m); // Syncopation
        }

        [Fact]
        public void KickPatternVariant_GeneratesHalfTime_WhenInBridge()
        {
            // Arrange
            var op = new KickPatternVariantOperator();
            var context = CreateContext(
                energy: 0.5,
                sectionType: MusicConstants.eSectionType.Bridge,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                beatsPerBar: 4);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - half-time is sparse (beats 1 and 3)
            Assert.NotEmpty(candidates);
            Assert.Contains(candidates, c => c.Beat == 1m);
            Assert.True(candidates.Count <= 2); // Sparse pattern
        }

        [Fact]
        public void KickPatternVariant_ReturnsEmpty_WhenKickNotActive()
        {
            // Arrange
            var op = new KickPatternVariantOperator();
            var context = CreateContext(
                energy: 0.8,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Snare });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void KickPatternVariant_ReturnsEmpty_WhenInFillWindow()
        {
            // Arrange
            var op = new KickPatternVariantOperator();
            var context = CreateContext(
                energy: 0.8,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                isFillWindow: true);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void KickPatternVariant_AdaptsTo_ThreeFour()
        {
            // Arrange - 3/4 time, bridge section for half-time
            var op = new KickPatternVariantOperator();
            var context = CreateContext(
                energy: 0.5,
                sectionType: MusicConstants.eSectionType.Bridge,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                beatsPerBar: 3);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - pattern should stay within 3 beats
            Assert.NotEmpty(candidates);
            Assert.All(candidates, c => Assert.True(c.Beat <= 3.5m, $"Beat {c.Beat} should be within 3/4 bar"));
        }

        #endregion

        #region HalfTimeFeelOperator Tests

        [Fact]
        public void HalfTimeFeel_GeneratesSnareOnThree_WhenInBridge()
        {
            // Arrange
            var op = new HalfTimeFeelOperator();
            var context = CreateContext(
                energy: 0.4, // Below 0.6 max threshold
                sectionType: MusicConstants.eSectionType.Bridge,
                activeRoles: new HashSet<string> { GrooveRoles.Snare, GrooveRoles.Kick },
                beatsPerBar: 4);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();
            var snareCandidates = candidates.Where(c => c.Role == GrooveRoles.Snare).ToList();

            // Assert - snare on beat 3 only
            Assert.Single(snareCandidates);
            Assert.Equal(3m, snareCandidates[0].Beat);
            Assert.Equal(OnsetStrength.Backbeat, snareCandidates[0].Strength);
        }

        [Fact]
        public void HalfTimeFeel_GeneratesSparseKicks()
        {
            // Arrange
            var op = new HalfTimeFeelOperator();
            var context = CreateContext(
                energy: 0.5, // Moderate for kick on 3 as well
                sectionType: MusicConstants.eSectionType.Bridge,
                activeRoles: new HashSet<string> { GrooveRoles.Snare, GrooveRoles.Kick },
                beatsPerBar: 4);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();
            var kickCandidates = candidates.Where(c => c.Role == GrooveRoles.Kick).ToList();

            // Assert - sparse kicks (beat 1, possibly beat 3)
            Assert.NotEmpty(kickCandidates);
            Assert.Contains(kickCandidates, c => c.Beat == 1m); // Beat 1 always
            Assert.True(kickCandidates.Count <= 2); // At most 2 kicks
        }

        [Fact]
        public void HalfTimeFeel_ReturnsEmpty_WhenEnergyTooHigh()
        {
            // Arrange - energy above 0.6 max threshold
            var op = new HalfTimeFeelOperator();
            var context = CreateContext(
                energy: 0.8, // Above 0.6 max
                sectionType: MusicConstants.eSectionType.Bridge,
                activeRoles: new HashSet<string> { GrooveRoles.Snare, GrooveRoles.Kick });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void HalfTimeFeel_ReturnsEmpty_WhenInChorus()
        {
            // Arrange - Chorus not suitable for half-time
            var op = new HalfTimeFeelOperator();
            var context = CreateContext(
                energy: 0.4,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Snare, GrooveRoles.Kick });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void HalfTimeFeel_ReturnsEmpty_WhenSnareNotActive()
        {
            // Arrange
            var op = new HalfTimeFeelOperator();
            var context = CreateContext(
                energy: 0.4,
                sectionType: MusicConstants.eSectionType.Bridge,
                activeRoles: new HashSet<string> { GrooveRoles.Kick });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void HalfTimeFeel_HasHigherScore_InBridge()
        {
            // Arrange
            var op = new HalfTimeFeelOperator();
            var contextBridge = CreateContext(
                energy: 0.4,
                sectionType: MusicConstants.eSectionType.Bridge,
                activeRoles: new HashSet<string> { GrooveRoles.Snare, GrooveRoles.Kick });

            var contextVerse = CreateContext(
                energy: 0.4,
                sectionType: MusicConstants.eSectionType.Verse,
                activeRoles: new HashSet<string> { GrooveRoles.Snare, GrooveRoles.Kick });

            // Act
            var candidatesBridge = op.GenerateCandidates(contextBridge).ToList();
            var candidatesVerse = op.GenerateCandidates(contextVerse).ToList();

            // Assert - bridge candidates should score higher
            Assert.NotEmpty(candidatesBridge);
            Assert.NotEmpty(candidatesVerse);
            Assert.True(candidatesBridge.First().Score > candidatesVerse.First().Score);
        }

        #endregion

        #region DoubleTimeFeelOperator Tests

        [Fact]
        public void DoubleTimeFeel_GeneratesDenseKicks_WhenInChorusWithHighEnergy()
        {
            // Arrange
            var op = new DoubleTimeFeelOperator();
            var context = CreateContext(
                energy: 0.8, // Above 0.6 min threshold
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare },
                beatsPerBar: 4);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();
            var kickCandidates = candidates.Where(c => c.Role == GrooveRoles.Kick).ToList();

            // Assert - 8th note density (4 downbeats + 4 offbeats)
            Assert.Equal(8, kickCandidates.Count);
            // Check for offbeats
            Assert.Contains(kickCandidates, c => c.Beat == 1.5m);
            Assert.Contains(kickCandidates, c => c.Beat == 2.5m);
        }

        [Fact]
        public void DoubleTimeFeel_GeneratesBackbeats_WithSnare()
        {
            // Arrange
            var op = new DoubleTimeFeelOperator();
            var context = CreateContext(
                energy: 0.8,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare },
                beatsPerBar: 4);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();
            var snareCandidates = candidates.Where(c => c.Role == GrooveRoles.Snare).ToList();

            // Assert - standard backbeats on 2 and 4
            Assert.Equal(2, snareCandidates.Count);
            Assert.Contains(snareCandidates, c => c.Beat == 2m);
            Assert.Contains(snareCandidates, c => c.Beat == 4m);
        }

        [Fact]
        public void DoubleTimeFeel_ReturnsEmpty_WhenEnergyTooLow()
        {
            // Arrange - energy below 0.6 min threshold
            var op = new DoubleTimeFeelOperator();
            var context = CreateContext(
                energy: 0.5, // Below 0.6 min
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void DoubleTimeFeel_ReturnsEmpty_WhenInBridge()
        {
            // Arrange - Bridge not suitable for double-time
            var op = new DoubleTimeFeelOperator();
            var context = CreateContext(
                energy: 0.8,
                sectionType: MusicConstants.eSectionType.Bridge,
                activeRoles: new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void DoubleTimeFeel_ReturnsEmpty_WhenKickNotActive()
        {
            // Arrange
            var op = new DoubleTimeFeelOperator();
            var context = CreateContext(
                energy: 0.8,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Snare });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        #endregion

        #region HalfTime vs DoubleTime Mutual Exclusion Tests

        [Fact]
        public void HalfTimeAndDoubleTime_AreMutuallyExclusive_ViaEnergyThresholds()
        {
            // Arrange
            var halfTimeOp = new HalfTimeFeelOperator();
            var doubleTimeOp = new DoubleTimeFeelOperator();

            // Context where both would want to apply if energy allowed
            var contextLowEnergy = CreateContext(
                energy: 0.4, // Below double-time min (0.6), within half-time range
                sectionType: MusicConstants.eSectionType.Bridge,
                activeRoles: new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare });

            var contextHighEnergy = CreateContext(
                energy: 0.8, // Above half-time max (0.6), within double-time range
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare });

            // Act
            var halfTimeLow = halfTimeOp.GenerateCandidates(contextLowEnergy).ToList();
            var doubleTimeLow = doubleTimeOp.GenerateCandidates(contextLowEnergy).ToList();
            var halfTimeHigh = halfTimeOp.GenerateCandidates(contextHighEnergy).ToList();
            var doubleTimeHigh = doubleTimeOp.GenerateCandidates(contextHighEnergy).ToList();

            // Assert - at low energy, only half-time applies
            Assert.NotEmpty(halfTimeLow);
            Assert.Empty(doubleTimeLow);

            // At high energy, only double-time applies (also needs correct section)
            Assert.Empty(halfTimeHigh);
            Assert.NotEmpty(doubleTimeHigh);
        }

        #endregion

        #region Cross-Operator Tests

        [Fact]
        public void AllOperators_HaveCorrectOperatorFamily()
        {
            // Arrange
            var operators = new IDrumOperator[]
            {
                new BackbeatVariantOperator(),
                new KickPatternVariantOperator(),
                new HalfTimeFeelOperator(),
                new DoubleTimeFeelOperator()
            };

            // Assert
            foreach (var op in operators)
            {
                Assert.Equal(Common.OperatorFamily.PatternSubstitution, op.OperatorFamily);
            }
        }

        [Fact]
        public void AllOperators_HaveUniqueOperatorIds()
        {
            // Arrange
            var operators = new IDrumOperator[]
            {
                new BackbeatVariantOperator(),
                new KickPatternVariantOperator(),
                new HalfTimeFeelOperator(),
                new DoubleTimeFeelOperator()
            };

            // Act
            var ids = operators.Select(op => op.OperatorId).ToList();

            // Assert
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }

        [Fact]
        public void AllOperators_DoNotProduceScoresOutsideZeroToOne()
        {
            // Arrange
            var testCases = GetAllOperatorTestCases();

            foreach (var (op, context) in testCases)
            {
                // Act
                var candidates = op.GenerateCandidates(context).ToList();

                // Assert
                Assert.All(candidates, c => Assert.InRange(c.Score, 0.0, 1.0));
            }
        }

        [Fact]
        public void AllOperators_AreDeterministic_ForSameSeed()
        {
            // Arrange
            var testCases = GetAllOperatorTestCases();

            foreach (var (op, context) in testCases)
            {
                // Create a second identical context
                var context2 = CreateContext(
                    energy: context.EnergyLevel,
                    activeRoles: new HashSet<string>(context.ActiveRoles),
                    sectionType: context.SectionType,
                    isFillWindow: context.IsFillWindow,
                    isAtSectionBoundary: context.IsAtSectionBoundary,
                    barsUntilSectionEnd: context.BarsUntilSectionEnd,
                    seed: context.Seed);

                // Act
                var candidates1 = op.GenerateCandidates(context).ToList();
                var candidates2 = op.GenerateCandidates(context2).ToList();

                // Assert
                Assert.Equal(candidates1.Count, candidates2.Count);
                for (int i = 0; i < candidates1.Count; i++)
                {
                    Assert.Equal(candidates1[i].CandidateId, candidates2[i].CandidateId);
                    Assert.Equal(candidates1[i].Beat, candidates2[i].Beat);
                    Assert.Equal(candidates1[i].VelocityHint, candidates2[i].VelocityHint);
                }
            }
        }

        [Fact]
        public void AllOperators_SuppressDuringFillWindow()
        {
            // Arrange - all pattern substitution operators should return empty when IsFillWindow is true
            var operators = new IDrumOperator[]
            {
                new BackbeatVariantOperator(),
                new KickPatternVariantOperator(),
                new HalfTimeFeelOperator(),
                new DoubleTimeFeelOperator()
            };

            var context = CreateContext(
                energy: 0.5,
                activeRoles: new HashSet<string> { GrooveRoles.Snare, GrooveRoles.Kick },
                sectionType: MusicConstants.eSectionType.Verse, // Use Verse for HalfTime
                isFillWindow: true);

            // Act & Assert
            foreach (var op in operators)
            {
                var candidates = op.GenerateCandidates(context).ToList();
                Assert.Empty(candidates);
            }
        }

        [Fact]
        public void PatternSubstitution_GeneratesFullBarPatterns_NotSingleHits()
        {
            // Arrange - use contexts where operators will generate patterns
            var kickOp = new KickPatternVariantOperator();
            var doubleTimeOp = new DoubleTimeFeelOperator();

            var kickContext = CreateContext(
                energy: 0.8,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                beatsPerBar: 4);

            var doubleContext = CreateContext(
                energy: 0.8,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare },
                beatsPerBar: 4);

            // Act
            var kickCandidates = kickOp.GenerateCandidates(kickContext).ToList();
            var doubleCandidates = doubleTimeOp.GenerateCandidates(doubleContext).ToList();

            // Assert - should have multiple candidates (full pattern)
            Assert.True(kickCandidates.Count >= 2, "Kick pattern should have multiple hits");
            Assert.True(doubleCandidates.Count >= 4, "Double-time should have many hits");
        }

        #endregion

        #region Odd Meter Tests

        [Fact]
        public void PatternSubstitution_AdaptsTo_FiveFour()
        {
            // Arrange - 5/4 time
            var op = new KickPatternVariantOperator();
            var context = CreateContext(
                energy: 0.8,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                beatsPerBar: 5);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - four-on-floor adapted to 5 beats
            Assert.Equal(5, candidates.Count); // Beat on each of 5 beats
            Assert.All(candidates, c => Assert.True(c.Beat <= 5.5m, $"Beat {c.Beat} should be within 5/4 bar"));
        }

        [Fact]
        public void BackbeatVariant_AdaptsTo_ThreeFour()
        {
            // Arrange - 3/4 time with single backbeat on 2
            var op = new BackbeatVariantOperator();
            var context = CreateContext(
                energy: 0.8,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                beatsPerBar: 3);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - should only have backbeat on 2 (not 4, since no beat 4)
            Assert.Single(candidates);
            Assert.Equal(2m, candidates[0].Beat);
        }

        #endregion

        #region Helper Methods

        private static IEnumerable<(IDrumOperator Op, DrummerContext Context)> GetAllOperatorTestCases()
        {
            yield return (new BackbeatVariantOperator(), CreateContext(
                energy: 0.8,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                sectionType: MusicConstants.eSectionType.Chorus));

            yield return (new KickPatternVariantOperator(), CreateContext(
                energy: 0.8,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                sectionType: MusicConstants.eSectionType.Chorus));

            yield return (new HalfTimeFeelOperator(), CreateContext(
                energy: 0.4,
                activeRoles: new HashSet<string> { GrooveRoles.Snare, GrooveRoles.Kick },
                sectionType: MusicConstants.eSectionType.Bridge));

            yield return (new DoubleTimeFeelOperator(), CreateContext(
                energy: 0.8,
                activeRoles: new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare },
                sectionType: MusicConstants.eSectionType.Chorus));
        }

        private static DrummerContext CreateContext(
            double energy = 0.5,
            IReadOnlySet<string>? activeRoles = null,
            HatSubdivision hatSubdivision = HatSubdivision.Eighth,
            HatMode hatMode = HatMode.Closed,
            bool isFillWindow = false,
            bool isAtSectionBoundary = false,
            int seed = 42,
            int barNumber = 1,
            int beatsPerBar = 4,
            int barsUntilSectionEnd = 4,
            double phrasePosition = 0.5,
            MusicConstants.eSectionType sectionType = MusicConstants.eSectionType.Verse)
        {
            return new DrummerContext
            {
                // Base AgentContext fields
                BarNumber = barNumber,
                Beat = 1.0m,
                SectionType = sectionType,
                PhrasePosition = phrasePosition,
                BarsUntilSectionEnd = barsUntilSectionEnd,
                EnergyLevel = energy,
                TensionLevel = 0.0,
                MotifPresenceScore = 0.0,
                Seed = seed,
                RngStreamKey = $"Test_{barNumber}",

                // Drummer-specific fields
                ActiveRoles = activeRoles ?? new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare, GrooveRoles.ClosedHat },
                LastKickBeat = null,
                LastSnareBeat = null,
                CurrentHatMode = hatMode,
                HatSubdivision = hatSubdivision,
                IsFillWindow = isFillWindow,
                IsAtSectionBoundary = isAtSectionBoundary,
                BackbeatBeats = beatsPerBar >= 4 ? new List<int> { 2, 4 } : new List<int> { 2 },
                BeatsPerBar = beatsPerBar
            };
        }

        #endregion
    }
}
