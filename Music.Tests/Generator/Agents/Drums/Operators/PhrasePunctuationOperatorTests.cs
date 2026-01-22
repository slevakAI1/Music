// AI: purpose=Unit tests for Story 3.3 PhrasePunctuation operators; verifies candidate generation and fill window constraints.
// AI: deps=xunit for test framework; Music.Generator.Agents.Drums.Operators.PhrasePunctuation for operators under test.
// AI: change=Story 3.3 acceptance criteria: fills generate only in appropriate windows, density scales with energy.

using Xunit;
using Music.Generator.Agents.Drums;
using Music.Generator.Agents.Drums.Operators.PhrasePunctuation;
using Music.Generator;
using Music;

namespace Music.Generator.Agents.Drums.Operators.Tests
{
    /// <summary>
    /// Story 3.3: Tests for PhrasePunctuation operators.
    /// Verifies fill/crash generation, context gating, density scaling, and determinism.
    /// </summary>
    [Collection("RngDependentTests")]
    public class PhrasePunctuationOperatorTests
    {
        public PhrasePunctuationOperatorTests()
        {
            Rng.Initialize(42);
        }

        #region CrashOnOneOperator Tests

        [Fact]
        public void CrashOnOne_GeneratesCrash_WhenAtSectionStartAndCrashActive()
        {
            // Arrange
            var op = new CrashOnOneOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.Crash, GrooveRoles.Kick },
                isAtSectionBoundary: true,
                phrasePosition: 0.0); // Start of section

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Single(candidates);
            var crash = candidates[0];
            Assert.Equal(GrooveRoles.Crash, crash.Role);
            Assert.Equal(1.0m, crash.Beat);
            Assert.Equal(OnsetStrength.Downbeat, crash.Strength);
            Assert.Equal(DrumArticulation.Crash, crash.ArticulationHint);
            Assert.Equal(FillRole.FillEnd, crash.FillRole);
            Assert.NotNull(crash.VelocityHint);
            Assert.InRange(crash.VelocityHint!.Value, 100, 127);
        }

        [Fact]
        public void CrashOnOne_ReturnsEmpty_WhenNotAtSectionBoundary()
        {
            // Arrange
            var op = new CrashOnOneOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.Crash },
                isAtSectionBoundary: false);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void CrashOnOne_ReturnsEmpty_WhenCrashNotActive()
        {
            // Arrange
            var op = new CrashOnOneOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare },
                isAtSectionBoundary: true,
                phrasePosition: 0.0);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void CrashOnOne_ReturnsEmpty_WhenAtSectionEndNotStart()
        {
            // Arrange - phrase position near 1.0 means section end, not start
            var op = new CrashOnOneOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.Crash },
                isAtSectionBoundary: true,
                phrasePosition: 0.9); // Near end of section

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        #endregion

        #region TurnaroundFillShortOperator Tests

        [Fact]
        public void TurnaroundFillShort_GeneratesFill_WhenInFillWindowAndSnareActive()
        {
            // Arrange
            var op = new TurnaroundFillShortOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                isFillWindow: true);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.NotEmpty(candidates);
            Assert.All(candidates, c =>
            {
                Assert.Equal(GrooveRoles.Snare, c.Role);
                Assert.True(c.Beat >= 3.0m, "Fill should be in last 2 beats");
                Assert.NotNull(c.VelocityHint);
                Assert.True(c.FillRole != FillRole.None, "Should have fill role");
            });
        }

        [Fact]
        public void TurnaroundFillShort_ReturnsEmpty_WhenNotInFillWindow()
        {
            // Arrange
            var op = new TurnaroundFillShortOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                isFillWindow: false);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void TurnaroundFillShort_DensityScalesWithEnergy()
        {
            // Arrange
            var op = new TurnaroundFillShortOperator();
            var lowEnergyContext = CreateContext(
                energy: 0.3,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                isFillWindow: true);
            var highEnergyContext = CreateContext(
                energy: 0.9,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                isFillWindow: true);

            // Act
            var lowEnergyCount = op.GenerateCandidates(lowEnergyContext).Count();
            var highEnergyCount = op.GenerateCandidates(highEnergyContext).Count();

            // Assert - higher energy should produce more hits
            Assert.True(highEnergyCount > lowEnergyCount,
                $"High energy ({highEnergyCount}) should produce more hits than low energy ({lowEnergyCount})");
        }

        [Fact]
        public void TurnaroundFillShort_AdaptsToTimeSignature_3Over4()
        {
            // Arrange - 3/4 time, fill on beats 2-3
            var op = new TurnaroundFillShortOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                isFillWindow: true,
                beatsPerBar: 3);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - fill should be in last 2 beats (beats 2-3)
            Assert.NotEmpty(candidates);
            Assert.All(candidates, c => Assert.True(c.Beat >= 2.0m && c.Beat <= 3.75m,
                $"Beat {c.Beat} should be in last 2 beats of 3/4 bar"));
        }

        #endregion

        #region TurnaroundFillFullOperator Tests

        [Fact]
        public void TurnaroundFillFull_GeneratesFill_WhenInFillWindowAndLastBar()
        {
            // Arrange
            var op = new TurnaroundFillFullOperator();
            var context = CreateContext(
                energy: 0.6,
                activeRoles: new HashSet<string> { GrooveRoles.Snare, GrooveRoles.Kick },
                isFillWindow: true,
                barsUntilSectionEnd: 1);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.NotEmpty(candidates);
            Assert.True(candidates.Count >= 8, "Full bar fill should have at least 8 hits");
            Assert.Contains(candidates, c => c.Role == GrooveRoles.Kick);
            Assert.Contains(candidates, c => c.Role == GrooveRoles.Snare);
        }

        [Fact]
        public void TurnaroundFillFull_ReturnsEmpty_WhenNotLastBar()
        {
            // Arrange - more than 1 bar until section end
            var op = new TurnaroundFillFullOperator();
            var context = CreateContext(
                energy: 0.6,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                isFillWindow: true,
                barsUntilSectionEnd: 3);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void TurnaroundFillFull_ReturnsEmpty_WhenEnergyLow()
        {
            // Arrange
            var op = new TurnaroundFillFullOperator();
            var context = CreateContext(
                energy: 0.2,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                isFillWindow: true,
                barsUntilSectionEnd: 1);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        #endregion

        #region SetupHitOperator Tests

        [Fact]
        public void SetupHit_GeneratesKick_WhenInFillWindowAndKickActive()
        {
            // Arrange
            var op = new SetupHitOperator();
            var context = CreateContext(
                energy: 0.5,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                isFillWindow: true,
                barsUntilSectionEnd: 1);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.NotEmpty(candidates);
            var kick = candidates.First(c => c.Role == GrooveRoles.Kick);
            Assert.Equal(4.5m, kick.Beat); // 4 "and"
            Assert.Equal(OnsetStrength.Pickup, kick.Strength);
            Assert.Equal(FillRole.Setup, kick.FillRole);
        }

        [Fact]
        public void SetupHit_AddsSnare_WhenEnergyHigh()
        {
            // Arrange
            var op = new SetupHitOperator();
            var context = CreateContext(
                energy: 0.8,
                activeRoles: new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare },
                isFillWindow: true,
                barsUntilSectionEnd: 1);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - should have both kick and snare at high energy
            Assert.Equal(2, candidates.Count);
            Assert.Contains(candidates, c => c.Role == GrooveRoles.Kick);
            Assert.Contains(candidates, c => c.Role == GrooveRoles.Snare);
        }

        [Fact]
        public void SetupHit_ReturnsEmpty_WhenNotInFillWindowOrBoundary()
        {
            // Arrange
            var op = new SetupHitOperator();
            var context = CreateContext(
                energy: 0.5,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                isFillWindow: false,
                isAtSectionBoundary: false);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        #endregion

        #region StopTimeOperator Tests

        [Fact]
        public void StopTime_GeneratesSparseAccents_WhenInFillWindow()
        {
            // Arrange
            var op = new StopTimeOperator();
            var context = CreateContext(
                energy: 0.5,
                activeRoles: new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare },
                isFillWindow: true);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - only kick on 1, snare on 3 (sparse)
            Assert.Equal(2, candidates.Count);
            Assert.Contains(candidates, c => c.Role == GrooveRoles.Kick && c.Beat == 1.0m);
            Assert.Contains(candidates, c => c.Role == GrooveRoles.Snare && c.Beat == 3.0m);
        }

        [Fact]
        public void StopTime_ReturnsEmpty_WhenEnergyTooHigh()
        {
            // Arrange - max energy threshold is 0.7
            var op = new StopTimeOperator();
            var context = CreateContext(
                energy: 0.9,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                isFillWindow: true);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void StopTime_ReturnsEmpty_WhenEnergyTooLow()
        {
            // Arrange - min energy threshold is 0.4
            var op = new StopTimeOperator();
            var context = CreateContext(
                energy: 0.2,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                isFillWindow: true);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        #endregion

        #region BuildFillOperator Tests

        [Fact]
        public void BuildFill_GeneratesAscendingTomPattern_WhenTomsActive()
        {
            // Arrange
            var op = new BuildFillOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.FloorTom, GrooveRoles.Tom2, GrooveRoles.Tom1 },
                isFillWindow: true);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.NotEmpty(candidates);

            // Verify ascending pattern (low to high)
            var sortedCandidates = candidates.OrderBy(c => c.Beat).ToList();
            Assert.Equal(GrooveRoles.FloorTom, sortedCandidates.First().Role); // Starts low
            Assert.Equal(GrooveRoles.Tom1, sortedCandidates.Last().Role); // Ends high
        }

        [Fact]
        public void BuildFill_HasVelocityCrescendo()
        {
            // Arrange
            var op = new BuildFillOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.FloorTom, GrooveRoles.Tom1 },
                isFillWindow: true);

            // Act
            var candidates = op.GenerateCandidates(context).OrderBy(c => c.Beat).ToList();

            // Assert - later hits should have higher velocity on average
            if (candidates.Count >= 2)
            {
                var firstVel = candidates.First().VelocityHint ?? 0;
                var lastVel = candidates.Last().VelocityHint ?? 0;
                Assert.True(lastVel >= firstVel, "Velocity should crescendo (increase)");
            }
        }

        [Fact]
        public void BuildFill_FallsBackToSnare_WhenNoTomsActive()
        {
            // Arrange
            var op = new BuildFillOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                isFillWindow: true);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - uses snare as fallback
            Assert.NotEmpty(candidates);
            Assert.All(candidates, c => Assert.Equal(GrooveRoles.Snare, c.Role));
        }

        [Fact]
        public void BuildFill_ReturnsEmpty_WhenNoTomsOrSnare()
        {
            // Arrange
            var op = new BuildFillOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.Kick, GrooveRoles.ClosedHat },
                isFillWindow: true);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        #endregion

        #region DropFillOperator Tests

        [Fact]
        public void DropFill_GeneratesDescendingTomPattern_WhenTomsActive()
        {
            // Arrange
            var op = new DropFillOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.FloorTom, GrooveRoles.Tom2, GrooveRoles.Tom1 },
                isFillWindow: true);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.NotEmpty(candidates);

            // Verify descending pattern (high to low)
            var sortedCandidates = candidates.OrderBy(c => c.Beat).ToList();
            Assert.Equal(GrooveRoles.Tom1, sortedCandidates.First().Role); // Starts high
            Assert.Equal(GrooveRoles.FloorTom, sortedCandidates.Last().Role); // Ends low
        }

        [Fact]
        public void DropFill_HasVelocityDecrescendo()
        {
            // Arrange
            var op = new DropFillOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.FloorTom, GrooveRoles.Tom1 },
                isFillWindow: true);

            // Act
            var candidates = op.GenerateCandidates(context).OrderBy(c => c.Beat).ToList();

            // Assert - later hits should have lower velocity
            if (candidates.Count >= 2)
            {
                var firstVel = candidates.First().VelocityHint ?? 0;
                var lastVel = candidates.Last().VelocityHint ?? 0;
                Assert.True(lastVel <= firstVel, "Velocity should decrescendo (decrease)");
            }
        }

        #endregion

        #region Cross-Operator Tests

        [Fact]
        public void AllOperators_HaveCorrectOperatorFamily()
        {
            // Arrange
            var operators = new IDrumOperator[]
            {
                new CrashOnOneOperator(),
                new TurnaroundFillShortOperator(),
                new TurnaroundFillFullOperator(),
                new SetupHitOperator(),
                new StopTimeOperator(),
                new BuildFillOperator(),
                new DropFillOperator()
            };

            // Assert
            foreach (var op in operators)
            {
                Assert.Equal(Common.OperatorFamily.PhrasePunctuation, op.OperatorFamily);
            }
        }

        [Fact]
        public void AllOperators_HaveUniqueOperatorIds()
        {
            // Arrange
            var operators = new IDrumOperator[]
            {
                new CrashOnOneOperator(),
                new TurnaroundFillShortOperator(),
                new TurnaroundFillFullOperator(),
                new SetupHitOperator(),
                new StopTimeOperator(),
                new BuildFillOperator(),
                new DropFillOperator()
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
        public void AllFillOperators_RequireFillWindow()
        {
            // Arrange - all fill operators should return empty when IsFillWindow is false
            var fillOperators = new IDrumOperator[]
            {
                new TurnaroundFillShortOperator(),
                new TurnaroundFillFullOperator(),
                new StopTimeOperator(),
                new BuildFillOperator(),
                new DropFillOperator()
            };

            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.Snare, GrooveRoles.Kick, GrooveRoles.FloorTom, GrooveRoles.Tom1 },
                isFillWindow: false,
                barsUntilSectionEnd: 1);

            // Act & Assert
            foreach (var op in fillOperators)
            {
                var candidates = op.GenerateCandidates(context).ToList();
                Assert.Empty(candidates);
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
                    isFillWindow: context.IsFillWindow,
                    isAtSectionBoundary: context.IsAtSectionBoundary,
                    barsUntilSectionEnd: context.BarsUntilSectionEnd,
                    seed: context.Seed,
                    phrasePosition: context.PhrasePosition);

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

        #endregion

        #region Odd Meter Tests

        [Fact]
        public void FillOperators_RespectBarLength_In5Over4()
        {
            // Arrange - 5/4 time
            var op = new TurnaroundFillShortOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                isFillWindow: true,
                beatsPerBar: 5);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - fill in last 2 beats (beats 4-5)
            Assert.NotEmpty(candidates);
            Assert.All(candidates, c => Assert.True(c.Beat >= 4.0m && c.Beat <= 5.75m,
                $"Beat {c.Beat} should be in last 2 beats of 5/4 bar"));
        }

        #endregion

        #region Helper Methods

        private static IEnumerable<(IDrumOperator Op, DrummerContext Context)> GetAllOperatorTestCases()
        {
            // Each operator with a context where it can generate candidates
            yield return (new CrashOnOneOperator(), CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.Crash },
                isAtSectionBoundary: true,
                phrasePosition: 0.0));

            yield return (new TurnaroundFillShortOperator(), CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                isFillWindow: true));

            yield return (new TurnaroundFillFullOperator(), CreateContext(
                energy: 0.6,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                isFillWindow: true,
                barsUntilSectionEnd: 1));

            yield return (new SetupHitOperator(), CreateContext(
                energy: 0.5,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                isFillWindow: true));

            yield return (new StopTimeOperator(), CreateContext(
                energy: 0.5,
                activeRoles: new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare },
                isFillWindow: true));

            yield return (new BuildFillOperator(), CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.FloorTom, GrooveRoles.Tom1 },
                isFillWindow: true));

            yield return (new DropFillOperator(), CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.FloorTom, GrooveRoles.Tom1 },
                isFillWindow: true));
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
