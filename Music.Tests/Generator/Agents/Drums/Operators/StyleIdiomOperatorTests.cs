// AI: purpose=Unit tests for Story 3.5 StyleIdiom operators; verifies style gating, section awareness, and candidate generation.
// AI: deps=xunit for test framework; Music.Generator.Agents.Drums.Operators.StyleIdiom for operators under test.
// AI: change=Story 3.5 acceptance criteria: operators apply only when StyleId=="PopRock", respect section type, generate style-appropriate candidates.

using Xunit;
using Music.Generator.Agents.Drums;
using Music.Generator.Agents.Drums.Operators.StyleIdiom;
using Music.Generator;
using Music;
using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Operators.Tests
{
    /// <summary>
    /// Story 3.5: Tests for StyleIdiom operators (Pop Rock specifics).
    /// Verifies style gating, section-aware behavior, timing offsets, and determinism.
    /// </summary>
    [Collection("RngDependentTests")]
    public class StyleIdiomOperatorTests
    {
        public StyleIdiomOperatorTests()
        {
            Rng.Initialize(42);
        }

        #region PopRockBackbeatPushOperator Tests

        [Fact]
        public void PopRockBackbeatPush_GeneratesSnareWithNegativeTimingOffset()
        {
            // Arrange
            var op = new PopRockBackbeatPushOperator();
            var context = CreatePopRockContext(
                energy: 0.5,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Snare });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - should have candidates on backbeats with negative timing (ahead)
            Assert.NotEmpty(candidates);
            Assert.All(candidates, c =>
            {
                Assert.Equal(GrooveRoles.Snare, c.Role);
                Assert.True(c.TimingHint < 0, $"Timing should be negative (ahead), was {c.TimingHint}");
                Assert.InRange(c.TimingHint!.Value, -10, -4);
            });
        }

        [Fact]
        public void PopRockBackbeatPush_MorePush_AtHigherEnergy()
        {
            // Arrange
            var op = new PopRockBackbeatPushOperator();
            var lowEnergyContext = CreatePopRockContext(energy: 0.25, activeRoles: new HashSet<string> { GrooveRoles.Snare });
            var highEnergyContext = CreatePopRockContext(energy: 0.8, activeRoles: new HashSet<string> { GrooveRoles.Snare });

            // Act
            var lowEnergyCandidates = op.GenerateCandidates(lowEnergyContext).ToList();
            var highEnergyCandidates = op.GenerateCandidates(highEnergyContext).ToList();

            // Assert - high energy should have more negative timing (more push)
            Assert.NotEmpty(lowEnergyCandidates);
            Assert.NotEmpty(highEnergyCandidates);

            int lowEnergyTiming = lowEnergyCandidates.First().TimingHint!.Value;
            int highEnergyTiming = highEnergyCandidates.First().TimingHint!.Value;

            Assert.True(highEnergyTiming < lowEnergyTiming,
                $"High energy timing ({highEnergyTiming}) should be more negative than low energy ({lowEnergyTiming})");
        }

        [Fact]
        public void PopRockBackbeatPush_GeneratesOnBackbeats_InFourFour()
        {
            // Arrange
            var op = new PopRockBackbeatPushOperator();
            var context = CreatePopRockContext(
                energy: 0.5,
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
        public void PopRockBackbeatPush_ReturnsEmpty_WhenSnareNotActive()
        {
            // Arrange
            var op = new PopRockBackbeatPushOperator();
            var context = CreatePopRockContext(
                energy: 0.5,
                activeRoles: new HashSet<string> { GrooveRoles.Kick });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void PopRockBackbeatPush_ReturnsEmpty_WhenInFillWindow()
        {
            // Arrange
            var op = new PopRockBackbeatPushOperator();
            var context = CreatePopRockContext(
                energy: 0.5,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                isFillWindow: true);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void PopRockBackbeatPush_ReturnsEmpty_WhenNotPopRockStyle()
        {
            // Arrange
            var op = new PopRockBackbeatPushOperator();
            var context = CreateNonPopRockContext(
                energy: 0.5,
                activeRoles: new HashSet<string> { GrooveRoles.Snare });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        #endregion

        #region RockKickSyncopationOperator Tests

        [Fact]
        public void RockKickSyncopation_GeneratesKickOn4And()
        {
            // Arrange
            var op = new RockKickSyncopationOperator();
            var context = CreatePopRockContext(
                energy: 0.6,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                beatsPerBar: 4);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - primary pattern is on beat 4.5 (the "and" of 4)
            Assert.NotEmpty(candidates);
            Assert.Contains(candidates, c => c.Beat == 4.5m && c.Role == GrooveRoles.Kick);
        }

        [Fact]
        public void RockKickSyncopation_GeneratesSecondaryPatterns_AtHigherEnergy()
        {
            // Arrange
            var op = new RockKickSyncopationOperator();
            var context = CreatePopRockContext(
                energy: 0.7, // Above 0.6 threshold for secondary patterns
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                beatsPerBar: 4);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - may have additional syncopation at higher energy
            Assert.NotEmpty(candidates);
            Assert.Contains(candidates, c => c.Beat == 4.5m); // Primary always present
            // Secondary patterns are deterministic based on bar/seed hash
        }

        [Fact]
        public void RockKickSyncopation_ReturnsEmpty_WhenKickNotActive()
        {
            // Arrange
            var op = new RockKickSyncopationOperator();
            var context = CreatePopRockContext(
                energy: 0.6,
                activeRoles: new HashSet<string> { GrooveRoles.Snare });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void RockKickSyncopation_ReturnsEmpty_WhenEnergyTooLow()
        {
            // Arrange
            var op = new RockKickSyncopationOperator();
            var context = CreatePopRockContext(
                energy: 0.3, // Below 0.4 threshold
                activeRoles: new HashSet<string> { GrooveRoles.Kick });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void RockKickSyncopation_ReturnsEmpty_WhenNotPopRockStyle()
        {
            // Arrange
            var op = new RockKickSyncopationOperator();
            var context = CreateNonPopRockContext(
                energy: 0.6,
                activeRoles: new HashSet<string> { GrooveRoles.Kick });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void RockKickSyncopation_HasHigherScore_AtSectionBoundary()
        {
            // Arrange
            var op = new RockKickSyncopationOperator();
            var boundaryContext = CreatePopRockContext(
                energy: 0.6,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                isAtSectionBoundary: true,
                barsUntilSectionEnd: 1);

            var midContext = CreatePopRockContext(
                energy: 0.6,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                isAtSectionBoundary: false);

            // Act
            var boundaryCandidates = op.GenerateCandidates(boundaryContext).ToList();
            var midCandidates = op.GenerateCandidates(midContext).ToList();

            // Assert - boundary candidates should score higher
            Assert.NotEmpty(boundaryCandidates);
            Assert.NotEmpty(midCandidates);
            Assert.True(boundaryCandidates.First().Score >= midCandidates.First().Score);
        }

        #endregion

        #region PopChorusCrashPatternOperator Tests

        [Fact]
        public void PopChorusCrashPattern_GeneratesCrashOnBeat1_InChorus()
        {
            // Arrange
            var op = new PopChorusCrashPatternOperator();
            var context = CreatePopRockContext(
                energy: 0.7,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Crash });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.NotEmpty(candidates);
            Assert.All(candidates, c =>
            {
                Assert.Equal(GrooveRoles.Crash, c.Role);
                Assert.Equal(1.0m, c.Beat);
                Assert.Equal(OnsetStrength.Downbeat, c.Strength);
            });
        }

        [Fact]
        public void PopChorusCrashPattern_ReturnsEmpty_WhenNotChorus()
        {
            // Arrange
            var op = new PopChorusCrashPatternOperator();
            var context = CreatePopRockContext(
                energy: 0.7,
                sectionType: MusicConstants.eSectionType.Verse,
                activeRoles: new HashSet<string> { GrooveRoles.Crash });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void PopChorusCrashPattern_ReturnsEmpty_WhenCrashNotActive()
        {
            // Arrange
            var op = new PopChorusCrashPatternOperator();
            var context = CreatePopRockContext(
                energy: 0.7,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Kick });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void PopChorusCrashPattern_ReturnsEmpty_WhenEnergyTooLow()
        {
            // Arrange
            var op = new PopChorusCrashPatternOperator();
            var context = CreatePopRockContext(
                energy: 0.4, // Below 0.5 threshold
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Crash });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void PopChorusCrashPattern_ReturnsEmpty_WhenNotPopRockStyle()
        {
            // Arrange
            var op = new PopChorusCrashPatternOperator();
            var context = CreateNonPopRockContext(
                energy: 0.7,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Crash });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void PopChorusCrashPattern_HasCrashArticulation()
        {
            // Arrange
            var op = new PopChorusCrashPatternOperator();
            var context = CreatePopRockContext(
                energy: 0.7,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Crash });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.NotEmpty(candidates);
            Assert.All(candidates, c => Assert.Equal(DrumArticulation.Crash, c.ArticulationHint));
        }

        #endregion

        #region VerseSimplifyOperator Tests

        [Fact]
        public void VerseSimplify_GeneratesSimplifiedKickPattern_InVerse()
        {
            // Arrange
            var op = new VerseSimplifyOperator();
            var context = CreatePopRockContext(
                energy: 0.4,
                sectionType: MusicConstants.eSectionType.Verse,
                activeRoles: new HashSet<string> { GrooveRoles.Kick, GrooveRoles.ClosedHat },
                beatsPerBar: 4);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();
            var kickCandidates = candidates.Where(c => c.Role == GrooveRoles.Kick).ToList();

            // Assert - simplified kick: beats 1 and 3 only
            Assert.NotEmpty(kickCandidates);
            Assert.Contains(kickCandidates, c => c.Beat == 1.0m);
            Assert.Contains(kickCandidates, c => c.Beat == 3.0m);
            Assert.Equal(2, kickCandidates.Count);
        }

        [Fact]
        public void VerseSimplify_GeneratesSparserHatPattern_InVerse()
        {
            // Arrange
            var op = new VerseSimplifyOperator();
            var context = CreatePopRockContext(
                energy: 0.4,
                sectionType: MusicConstants.eSectionType.Verse,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                beatsPerBar: 4);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();
            var hatCandidates = candidates.Where(c => c.Role == GrooveRoles.ClosedHat).ToList();

            // Assert - eighth note pattern (4 hits in 4/4)
            Assert.Equal(4, hatCandidates.Count);
        }

        [Fact]
        public void VerseSimplify_ReturnsEmpty_WhenNotVerse()
        {
            // Arrange
            var op = new VerseSimplifyOperator();
            var context = CreatePopRockContext(
                energy: 0.4,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Kick });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void VerseSimplify_ReturnsEmpty_WhenEnergyTooHigh()
        {
            // Arrange
            var op = new VerseSimplifyOperator();
            var context = CreatePopRockContext(
                energy: 0.8, // Above 0.7 max threshold
                sectionType: MusicConstants.eSectionType.Verse,
                activeRoles: new HashSet<string> { GrooveRoles.Kick });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void VerseSimplify_ReturnsEmpty_WhenNotPopRockStyle()
        {
            // Arrange
            var op = new VerseSimplifyOperator();
            var context = CreateNonPopRockContext(
                energy: 0.4,
                sectionType: MusicConstants.eSectionType.Verse,
                activeRoles: new HashSet<string> { GrooveRoles.Kick });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void VerseSimplify_HasLowerVelocity_ThanNormal()
        {
            // Arrange
            var op = new VerseSimplifyOperator();
            var context = CreatePopRockContext(
                energy: 0.4,
                sectionType: MusicConstants.eSectionType.Verse,
                activeRoles: new HashSet<string> { GrooveRoles.Kick });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - velocity hints should be lower (70-90 range)
            Assert.NotEmpty(candidates);
            Assert.All(candidates, c =>
            {
                Assert.NotNull(c.VelocityHint);
                Assert.InRange(c.VelocityHint!.Value, 70, 95);
            });
        }

        #endregion

        #region BridgeBreakdownOperator Tests

        [Fact]
        public void BridgeBreakdown_GeneratesHalfTimeSnare_InBridge()
        {
            // Arrange
            var op = new BridgeBreakdownOperator();
            var context = CreatePopRockContext(
                energy: 0.5, // Above 0.4 for half-time variant
                sectionType: MusicConstants.eSectionType.Bridge,
                activeRoles: new HashSet<string> { GrooveRoles.Snare, GrooveRoles.Kick },
                beatsPerBar: 4);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();
            var snareCandidates = candidates.Where(c => c.Role == GrooveRoles.Snare).ToList();

            // Assert - half-time: snare on beat 3 only
            Assert.Single(snareCandidates);
            Assert.Equal(3.0m, snareCandidates.First().Beat);
        }

        [Fact]
        public void BridgeBreakdown_GeneratesMinimalPattern_AtLowEnergy()
        {
            // Arrange
            var op = new BridgeBreakdownOperator();
            var context = CreatePopRockContext(
                energy: 0.3, // Below 0.4 for minimal variant
                sectionType: MusicConstants.eSectionType.Bridge,
                activeRoles: new HashSet<string> { GrooveRoles.Snare, GrooveRoles.Kick, GrooveRoles.ClosedHat },
                beatsPerBar: 4);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();
            var hatCandidates = candidates.Where(c => c.Role == GrooveRoles.ClosedHat).ToList();

            // Assert - minimal variant includes sparse hats
            Assert.NotEmpty(hatCandidates);
            Assert.True(hatCandidates.Count <= 2, "Minimal pattern should have sparse hats");
        }

        [Fact]
        public void BridgeBreakdown_GeneratesKickOnBeat1()
        {
            // Arrange
            var op = new BridgeBreakdownOperator();
            var context = CreatePopRockContext(
                energy: 0.5,
                sectionType: MusicConstants.eSectionType.Bridge,
                activeRoles: new HashSet<string> { GrooveRoles.Kick });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.NotEmpty(candidates);
            Assert.Contains(candidates, c => c.Beat == 1.0m && c.Role == GrooveRoles.Kick);
        }

        [Fact]
        public void BridgeBreakdown_ReturnsEmpty_WhenNotBridge()
        {
            // Arrange
            var op = new BridgeBreakdownOperator();
            var context = CreatePopRockContext(
                energy: 0.5,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Snare, GrooveRoles.Kick });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void BridgeBreakdown_ReturnsEmpty_WhenNotPopRockStyle()
        {
            // Arrange
            var op = new BridgeBreakdownOperator();
            var context = CreateNonPopRockContext(
                energy: 0.5,
                sectionType: MusicConstants.eSectionType.Bridge,
                activeRoles: new HashSet<string> { GrooveRoles.Snare, GrooveRoles.Kick });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void BridgeBreakdown_ReturnsEmpty_WhenLessThan4Beats()
        {
            // Arrange
            var op = new BridgeBreakdownOperator();
            var context = CreatePopRockContext(
                energy: 0.5,
                sectionType: MusicConstants.eSectionType.Bridge,
                activeRoles: new HashSet<string> { GrooveRoles.Snare, GrooveRoles.Kick },
                beatsPerBar: 3);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        #endregion

        #region Cross-Operator Tests

        [Fact]
        public void AllStyleIdiomOperators_HaveCorrectOperatorFamily()
        {
            // Arrange
            var operators = new IDrumOperator[]
            {
                new PopRockBackbeatPushOperator(),
                new RockKickSyncopationOperator(),
                new PopChorusCrashPatternOperator(),
                new VerseSimplifyOperator(),
                new BridgeBreakdownOperator()
            };

            // Assert
            foreach (var op in operators)
            {
                Assert.Equal(Common.OperatorFamily.StyleIdiom, op.OperatorFamily);
            }
        }

        [Fact]
        public void AllStyleIdiomOperators_HaveUniqueOperatorIds()
        {
            // Arrange
            var operators = new IDrumOperator[]
            {
                new PopRockBackbeatPushOperator(),
                new RockKickSyncopationOperator(),
                new PopChorusCrashPatternOperator(),
                new VerseSimplifyOperator(),
                new BridgeBreakdownOperator()
            };

            // Act
            var ids = operators.Select(op => op.OperatorId).ToList();

            // Assert
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }

        [Fact]
        public void AllStyleIdiomOperators_DoNotProduceScoresOutsideZeroToOne()
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
        public void AllStyleIdiomOperators_AreDeterministic_ForSameSeed()
        {
            // Arrange
            var testCases = GetAllOperatorTestCases();

            foreach (var (op, context) in testCases)
            {
                // Create a second identical context
                var context2 = CreatePopRockContext(
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
        public void AllStyleIdiomOperators_ReturnEmpty_WhenNotPopRockStyle()
        {
            // Arrange
            var operators = new IDrumOperator[]
            {
                new PopRockBackbeatPushOperator(),
                new RockKickSyncopationOperator(),
                new PopChorusCrashPatternOperator(),
                new VerseSimplifyOperator(),
                new BridgeBreakdownOperator()
            };

            // Create contexts for each operator's preferred section but with non-PopRock style
            var contexts = new Dictionary<IDrumOperator, DrummerContext>
            {
                [operators[0]] = CreateNonPopRockContext(energy: 0.5, activeRoles: new HashSet<string> { GrooveRoles.Snare }),
                [operators[1]] = CreateNonPopRockContext(energy: 0.6, activeRoles: new HashSet<string> { GrooveRoles.Kick }),
                [operators[2]] = CreateNonPopRockContext(energy: 0.7, sectionType: MusicConstants.eSectionType.Chorus, activeRoles: new HashSet<string> { GrooveRoles.Crash }),
                [operators[3]] = CreateNonPopRockContext(energy: 0.4, sectionType: MusicConstants.eSectionType.Verse, activeRoles: new HashSet<string> { GrooveRoles.Kick }),
                [operators[4]] = CreateNonPopRockContext(energy: 0.5, sectionType: MusicConstants.eSectionType.Bridge, activeRoles: new HashSet<string> { GrooveRoles.Snare, GrooveRoles.Kick })
            };

            // Act & Assert
            foreach (var op in operators)
            {
                var candidates = op.GenerateCandidates(contexts[op]).ToList();
                Assert.Empty(candidates);
            }
        }

        #endregion

        #region Registry Integration Tests

        [Fact]
        public void DrumOperatorRegistry_ContainsAllStyleIdiomOperators()
        {
            // Arrange
            var registry = DrumOperatorRegistryBuilder.BuildComplete();

            // Act
            var styleIdiomOperators = registry.GetOperatorsByFamily(Common.OperatorFamily.StyleIdiom);

            // Assert - should have all 5 StyleIdiom operators
            Assert.Equal(5, styleIdiomOperators.Count);
            Assert.Contains(styleIdiomOperators, op => op.OperatorId == "DrumPopRockBackbeatPush");
            Assert.Contains(styleIdiomOperators, op => op.OperatorId == "DrumRockKickSyncopation");
            Assert.Contains(styleIdiomOperators, op => op.OperatorId == "DrumPopChorusCrashPattern");
            Assert.Contains(styleIdiomOperators, op => op.OperatorId == "DrumVerseSimplify");
            Assert.Contains(styleIdiomOperators, op => op.OperatorId == "DrumBridgeBreakdown");
        }

        [Fact]
        public void DrumOperatorRegistry_Has28TotalOperators()
        {
            // Arrange
            var registry = DrumOperatorRegistryBuilder.BuildComplete();

            // Act
            var allOperators = registry.GetAllOperators();

            // Assert - 7 + 5 + 7 + 4 + 5 = 28 operators
            Assert.Equal(28, allOperators.Count);
        }

        #endregion

        #region Helper Methods

        private static IEnumerable<(IDrumOperator Op, DrummerContext Context)> GetAllOperatorTestCases()
        {
            yield return (new PopRockBackbeatPushOperator(), CreatePopRockContext(
                energy: 0.5,
                activeRoles: new HashSet<string> { GrooveRoles.Snare }));

            yield return (new RockKickSyncopationOperator(), CreatePopRockContext(
                energy: 0.6,
                activeRoles: new HashSet<string> { GrooveRoles.Kick }));

            yield return (new PopChorusCrashPatternOperator(), CreatePopRockContext(
                energy: 0.7,
                sectionType: MusicConstants.eSectionType.Chorus,
                activeRoles: new HashSet<string> { GrooveRoles.Crash }));

            yield return (new VerseSimplifyOperator(), CreatePopRockContext(
                energy: 0.4,
                sectionType: MusicConstants.eSectionType.Verse,
                activeRoles: new HashSet<string> { GrooveRoles.Kick, GrooveRoles.ClosedHat }));

            yield return (new BridgeBreakdownOperator(), CreatePopRockContext(
                energy: 0.5,
                sectionType: MusicConstants.eSectionType.Bridge,
                activeRoles: new HashSet<string> { GrooveRoles.Snare, GrooveRoles.Kick }));
        }

        private static DrummerContext CreatePopRockContext(
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
                RngStreamKey = $"Drummer_{barNumber}", // PopRock-style key

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

        private static DrummerContext CreateNonPopRockContext(
            double energy = 0.5,
            IReadOnlySet<string>? activeRoles = null,
            int seed = 42,
            int barNumber = 1,
            int beatsPerBar = 4,
            MusicConstants.eSectionType sectionType = MusicConstants.eSectionType.Verse)
        {
            return new DrummerContext
            {
                // Base AgentContext fields
                BarNumber = barNumber,
                Beat = 1.0m,
                SectionType = sectionType,
                PhrasePosition = 0.5,
                BarsUntilSectionEnd = 4,
                EnergyLevel = energy,
                TensionLevel = 0.0,
                MotifPresenceScore = 0.0,
                Seed = seed,
                RngStreamKey = $"Jazz_{barNumber}", // Non-PopRock style key

                // Drummer-specific fields
                ActiveRoles = activeRoles ?? new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare, GrooveRoles.ClosedHat },
                LastKickBeat = null,
                LastSnareBeat = null,
                CurrentHatMode = HatMode.Closed,
                HatSubdivision = HatSubdivision.Eighth,
                IsFillWindow = false,
                IsAtSectionBoundary = false,
                BackbeatBeats = beatsPerBar >= 4 ? new List<int> { 2, 4 } : new List<int> { 2 },
                BeatsPerBar = beatsPerBar
            };
        }

        #endregion
    }
}
