// AI: purpose=Unit tests for PhraseHookProtectionAugmenter; validates phrase-end protection augmentation.
// AI: deps=XUnit; tests deterministic behavior of downbeat/backbeat protection in phrase-end windows.
// AI: coverage=Null handling, downbeat protection, backbeat protection, multiple time signatures.

using Music.Generator;
using Music.Generator.Groove;
using Xunit;

namespace Music.Tests.Generator
{
    public class PhraseHookProtectionAugmenterTests
    {
        #region Null/Empty Input Tests

        [Fact]
        public void Augment_WithNullPolicy_DoesNotModifyProtections()
        {
            // Arrange
            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
            {
                [1] = new Dictionary<string, RoleProtectionSet>
                {
                    ["Kick"] = new RoleProtectionSet()
                }
            };
            var barContexts = new List<BarContext>
            {
                new BarContext(BarNumber: 1, Section: null, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 0)
            };

            // Act
            PhraseHookProtectionAugmenter.Augment(protections, barContexts, null, 4);

            // Assert
            Assert.Empty(protections[1]["Kick"].NeverRemoveOnsets);
        }

        [Fact]
        public void Augment_WithNullProtections_DoesNotThrow()
        {
            // Arrange
            var barContexts = new List<BarContext>
            {
                new BarContext(BarNumber: 1, Section: null, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 0)
            };
            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 2,
                ProtectDownbeatOnPhraseEnd = true
            };

            // Act & Assert - should not throw
            var exception = Record.Exception(() => PhraseHookProtectionAugmenter.Augment(null!, barContexts, policy, 4));
            Assert.Null(exception);
        }

        [Fact]
        public void Augment_WithNullBarContexts_DoesNotThrow()
        {
            // Arrange
            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>();
            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 2,
                ProtectDownbeatOnPhraseEnd = true
            };

            // Act & Assert - should not throw
            var exception = Record.Exception(() => PhraseHookProtectionAugmenter.Augment(protections, null!, policy, 4));
            Assert.Null(exception);
        }

        #endregion

        #region Downbeat Protection Tests

        [Fact]
        public void Augment_ProtectDownbeatOnPhraseEnd_AddsNeverRemoveForBeat1()
        {
            // Arrange
            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
            {
                [4] = new Dictionary<string, RoleProtectionSet>
                {
                    ["Kick"] = new RoleProtectionSet(),
                    ["Snare"] = new RoleProtectionSet()
                }
            };
            var barContexts = new List<BarContext>
            {
                new BarContext(BarNumber: 4, Section: null, SegmentProfile: null, BarWithinSection: 3, BarsUntilSectionEnd: 0)
            };
            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 1,
                ProtectDownbeatOnPhraseEnd = true,
                ProtectBackbeatOnPhraseEnd = false
            };

            // Act
            PhraseHookProtectionAugmenter.Augment(protections, barContexts, policy, 4);

            // Assert
            Assert.Contains(1m, protections[4]["Kick"].NeverRemoveOnsets);
            Assert.Contains(1m, protections[4]["Snare"].NeverRemoveOnsets);
        }

        [Fact]
        public void Augment_ProtectDownbeatFalse_DoesNotAddNeverRemoveForBeat1()
        {
            // Arrange
            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
            {
                [4] = new Dictionary<string, RoleProtectionSet>
                {
                    ["Kick"] = new RoleProtectionSet()
                }
            };
            var barContexts = new List<BarContext>
            {
                new BarContext(BarNumber: 4, Section: null, SegmentProfile: null, BarWithinSection: 3, BarsUntilSectionEnd: 0)
            };
            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 1,
                ProtectDownbeatOnPhraseEnd = false,
                ProtectBackbeatOnPhraseEnd = false
            };

            // Act
            PhraseHookProtectionAugmenter.Augment(protections, barContexts, policy, 4);

            // Assert
            Assert.DoesNotContain(1m, protections[4]["Kick"].NeverRemoveOnsets);
        }

        #endregion

        #region Backbeat Protection Tests

        [Fact]
        public void Augment_ProtectBackbeatOnPhraseEnd_AddsNeverRemoveForBeats2And4()
        {
            // Arrange
            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
            {
                [4] = new Dictionary<string, RoleProtectionSet>
                {
                    ["Snare"] = new RoleProtectionSet()
                }
            };
            var barContexts = new List<BarContext>
            {
                new BarContext(BarNumber: 4, Section: null, SegmentProfile: null, BarWithinSection: 3, BarsUntilSectionEnd: 0)
            };
            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 1,
                ProtectDownbeatOnPhraseEnd = false,
                ProtectBackbeatOnPhraseEnd = true
            };

            // Act
            PhraseHookProtectionAugmenter.Augment(protections, barContexts, policy, 4);

            // Assert
            Assert.Contains(2m, protections[4]["Snare"].NeverRemoveOnsets);
            Assert.Contains(4m, protections[4]["Snare"].NeverRemoveOnsets);
        }

        [Fact]
        public void Augment_ProtectBackbeatFalse_DoesNotAddNeverRemoveForBackbeats()
        {
            // Arrange
            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
            {
                [4] = new Dictionary<string, RoleProtectionSet>
                {
                    ["Snare"] = new RoleProtectionSet()
                }
            };
            var barContexts = new List<BarContext>
            {
                new BarContext(BarNumber: 4, Section: null, SegmentProfile: null, BarWithinSection: 3, BarsUntilSectionEnd: 0)
            };
            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 1,
                ProtectDownbeatOnPhraseEnd = false,
                ProtectBackbeatOnPhraseEnd = false
            };

            // Act
            PhraseHookProtectionAugmenter.Augment(protections, barContexts, policy, 4);

            // Assert
            Assert.DoesNotContain(2m, protections[4]["Snare"].NeverRemoveOnsets);
            Assert.DoesNotContain(4m, protections[4]["Snare"].NeverRemoveOnsets);
        }

        #endregion

        #region Time Signature Tests

        [Fact]
        public void Augment_In3_4Time_OnlyAddsBackbeatOnBeat2()
        {
            // Arrange
            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
            {
                [1] = new Dictionary<string, RoleProtectionSet>
                {
                    ["Snare"] = new RoleProtectionSet()
                }
            };
            var barContexts = new List<BarContext>
            {
                new BarContext(BarNumber: 1, Section: null, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 0)
            };
            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 1,
                ProtectBackbeatOnPhraseEnd = true
            };

            // Act
            PhraseHookProtectionAugmenter.Augment(protections, barContexts, policy, beatsPerBar: 3);

            // Assert
            Assert.Contains(2m, protections[1]["Snare"].NeverRemoveOnsets);
            Assert.DoesNotContain(4m, protections[1]["Snare"].NeverRemoveOnsets); // No beat 4 in 3/4
        }

        [Fact]
        public void Augment_In2_4Time_OnlyAddsBackbeatOnBeat2()
        {
            // Arrange
            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
            {
                [1] = new Dictionary<string, RoleProtectionSet>
                {
                    ["Snare"] = new RoleProtectionSet()
                }
            };
            var barContexts = new List<BarContext>
            {
                new BarContext(BarNumber: 1, Section: null, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 0)
            };
            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 1,
                ProtectBackbeatOnPhraseEnd = true
            };

            // Act
            PhraseHookProtectionAugmenter.Augment(protections, barContexts, policy, beatsPerBar: 2);

            // Assert
            Assert.Contains(2m, protections[1]["Snare"].NeverRemoveOnsets);
            Assert.DoesNotContain(4m, protections[1]["Snare"].NeverRemoveOnsets);
        }

        #endregion

        #region Window Logic Tests

        [Fact]
        public void Augment_BarNotInPhraseEndWindow_DoesNotAugment()
        {
            // Arrange
            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
            {
                [1] = new Dictionary<string, RoleProtectionSet>
                {
                    ["Kick"] = new RoleProtectionSet()
                }
            };
            var barContexts = new List<BarContext>
            {
                new BarContext(BarNumber: 1, Section: null, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 5) // Far from section end
            };
            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 2, // Only bars within 2 of section end
                ProtectDownbeatOnPhraseEnd = true,
                ProtectBackbeatOnPhraseEnd = true
            };

            // Act
            PhraseHookProtectionAugmenter.Augment(protections, barContexts, policy, 4);

            // Assert - bar 1 is 5 bars from end, outside window of 2
            Assert.Empty(protections[1]["Kick"].NeverRemoveOnsets);
        }

        [Fact]
        public void Augment_BarInPhraseEndWindow_AugmentsProtections()
        {
            // Arrange
            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
            {
                [4] = new Dictionary<string, RoleProtectionSet>
                {
                    ["Kick"] = new RoleProtectionSet()
                }
            };
            var barContexts = new List<BarContext>
            {
                new BarContext(BarNumber: 4, Section: null, SegmentProfile: null, BarWithinSection: 3, BarsUntilSectionEnd: 1) // 1 bar from end
            };
            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 2, // Bars within 2 of section end
                ProtectDownbeatOnPhraseEnd = true
            };

            // Act
            PhraseHookProtectionAugmenter.Augment(protections, barContexts, policy, 4);

            // Assert - bar 4 is 1 bar from end, inside window of 2
            Assert.Contains(1m, protections[4]["Kick"].NeverRemoveOnsets);
        }

        [Fact]
        public void Augment_CreatesProtectionSetIfMissing()
        {
            // Arrange - bar 4 has no protection entry yet
            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>();
            var barContexts = new List<BarContext>
            {
                new BarContext(BarNumber: 4, Section: null, SegmentProfile: null, BarWithinSection: 3, BarsUntilSectionEnd: 0)
            };
            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 1,
                ProtectDownbeatOnPhraseEnd = true
            };

            // Act
            PhraseHookProtectionAugmenter.Augment(protections, barContexts, policy, 4);

            // Assert - should have created entry for bar 4
            Assert.True(protections.ContainsKey(4));
        }

        #endregion

        #region GetBackbeatPositions Tests

        [Theory]
        [InlineData(1, new double[] { })]
        [InlineData(2, new double[] { 2.0 })]
        [InlineData(3, new double[] { 2.0 })]
        [InlineData(4, new double[] { 2.0, 4.0 })]
        [InlineData(5, new double[] { 2.0, 4.0 })]
        [InlineData(6, new double[] { 2.0, 4.0 })]
        public void GetBackbeatPositions_ReturnsCorrectPositions(int beatsPerBar, double[] expectedDoubles)
        {
            // Arrange
            var expected = expectedDoubles.Select(d => (decimal)d).ToList();

            // Act
            var result = PhraseHookProtectionAugmenter.GetBackbeatPositions(beatsPerBar);

            // Assert
            Assert.Equal(expected.Count, result.Count);
            foreach (var beat in expected)
            {
                Assert.Contains(beat, result);
            }
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void Augment_IsDeterministic()
        {
            // Arrange
            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 2,
                ProtectDownbeatOnPhraseEnd = true,
                ProtectBackbeatOnPhraseEnd = true
            };

            var barContexts = new List<BarContext>
            {
                new BarContext(BarNumber: 3, Section: null, SegmentProfile: null, BarWithinSection: 2, BarsUntilSectionEnd: 1),
                new BarContext(BarNumber: 4, Section: null, SegmentProfile: null, BarWithinSection: 3, BarsUntilSectionEnd: 0)
            };

            // Create fresh protections for each run
            Dictionary<int, Dictionary<string, RoleProtectionSet>> CreateProtections() =>
                new Dictionary<int, Dictionary<string, RoleProtectionSet>>
                {
                    [3] = new Dictionary<string, RoleProtectionSet> { ["Kick"] = new RoleProtectionSet() },
                    [4] = new Dictionary<string, RoleProtectionSet> { ["Kick"] = new RoleProtectionSet() }
                };

            // Act
            var protections1 = CreateProtections();
            var protections2 = CreateProtections();
            var protections3 = CreateProtections();

            PhraseHookProtectionAugmenter.Augment(protections1, barContexts, policy, 4);
            PhraseHookProtectionAugmenter.Augment(protections2, barContexts, policy, 4);
            PhraseHookProtectionAugmenter.Augment(protections3, barContexts, policy, 4);

            // Assert
            Assert.Equal(
                protections1[4]["Kick"].NeverRemoveOnsets.OrderBy(x => x),
                protections2[4]["Kick"].NeverRemoveOnsets.OrderBy(x => x));
            Assert.Equal(
                protections2[4]["Kick"].NeverRemoveOnsets.OrderBy(x => x),
                protections3[4]["Kick"].NeverRemoveOnsets.OrderBy(x => x));
        }

        #endregion

        #region Idempotency Tests

        [Fact]
        public void Augment_DoesNotDuplicateNeverRemoveOnsets()
        {
            // Arrange
            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
            {
                [4] = new Dictionary<string, RoleProtectionSet>
                {
                    ["Kick"] = new RoleProtectionSet { NeverRemoveOnsets = new List<decimal> { 1m } } // Already has beat 1
                }
            };
            var barContexts = new List<BarContext>
            {
                new BarContext(BarNumber: 4, Section: null, SegmentProfile: null, BarWithinSection: 3, BarsUntilSectionEnd: 0)
            };
            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 1,
                ProtectDownbeatOnPhraseEnd = true
            };

            // Act - call twice
            PhraseHookProtectionAugmenter.Augment(protections, barContexts, policy, 4);
            PhraseHookProtectionAugmenter.Augment(protections, barContexts, policy, 4);

            // Assert - beat 1 should only appear once
            Assert.Single(protections[4]["Kick"].NeverRemoveOnsets.Where(b => b == 1m));
        }

        #endregion
    }
}
