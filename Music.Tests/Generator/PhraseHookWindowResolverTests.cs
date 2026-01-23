// AI: purpose=Unit tests for PhraseHookWindowResolver; validates window correctness for phrase/section ends.
// AI: deps=XUnit; tests deterministic behavior of fill window resolution.
// AI: coverage=Story G8 acceptance criteria: null policy, window calculation, enabled fill tags.

using Music.Generator;
using Music.Generator.Groove;
using Xunit;

namespace Music.Tests.Generator
{
    public class PhraseHookWindowResolverTests
    {
        #region Null Policy Tests

        [Fact]
        public void Resolve_WithNullPolicy_ReturnsAllFalseFlags()
        {
            // Arrange
            var ctx = new BarContext(
                BarNumber: 4,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 3,
                BarsUntilSectionEnd: 1);

            // Act
            var result = PhraseHookWindowResolver.Resolve(ctx, null);

            // Assert
            Assert.False(result.InPhraseEndWindow);
            Assert.False(result.InSectionEndWindow);
            Assert.Empty(result.EnabledFillTags);
        }

        #endregion

        #region Phrase End Window Tests

        [Fact]
        public void Resolve_AllowFillsAtPhraseEndTrue_NeverInPhraseEndWindow()
        {
            // Arrange
            var ctx = new BarContext(
                BarNumber: 4,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 3,
                BarsUntilSectionEnd: 0); // Last bar of section

            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = true, // Fills allowed, so window not active
                PhraseEndBarsWindow = 2
            };

            // Act
            var result = PhraseHookWindowResolver.Resolve(ctx, policy);

            // Assert
            Assert.False(result.InPhraseEndWindow);
        }

        [Fact]
        public void Resolve_AllowFillsAtPhraseEndFalse_InWindowWhenWithinBarsFromEnd()
        {
            // Arrange
            var ctx = new BarContext(
                BarNumber: 4,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 3,
                BarsUntilSectionEnd: 1); // 1 bar until section end

            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 2 // Window is 2 bars
            };

            // Act
            var result = PhraseHookWindowResolver.Resolve(ctx, policy);

            // Assert - 1 < 2, so we're in the window
            Assert.True(result.InPhraseEndWindow);
        }

        [Fact]
        public void Resolve_AllowFillsAtPhraseEndFalse_NotInWindowWhenOutsideBarsFromEnd()
        {
            // Arrange
            var ctx = new BarContext(
                BarNumber: 1,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 0,
                BarsUntilSectionEnd: 3); // 3 bars until section end

            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 2 // Window is 2 bars
            };

            // Act
            var result = PhraseHookWindowResolver.Resolve(ctx, policy);

            // Assert - 3 >= 2, so we're NOT in the window
            Assert.False(result.InPhraseEndWindow);
        }

        [Fact]
        public void Resolve_LastBarOfSection_InPhraseEndWindow()
        {
            // Arrange
            var ctx = new BarContext(
                BarNumber: 8,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 7,
                BarsUntilSectionEnd: 0); // Last bar

            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 1
            };

            // Act
            var result = PhraseHookWindowResolver.Resolve(ctx, policy);

            // Assert - 0 < 1, so we're in the window
            Assert.True(result.InPhraseEndWindow);
        }

        [Fact]
        public void Resolve_ZeroWindowSize_NeverInPhraseEndWindow()
        {
            // Arrange
            var ctx = new BarContext(
                BarNumber: 4,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 3,
                BarsUntilSectionEnd: 0);

            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 0 // Zero window size
            };

            // Act
            var result = PhraseHookWindowResolver.Resolve(ctx, policy);

            // Assert
            Assert.False(result.InPhraseEndWindow);
        }

        #endregion

        #region Section End Window Tests

        [Fact]
        public void Resolve_AllowFillsAtSectionEndTrue_NeverInSectionEndWindow()
        {
            // Arrange
            var ctx = new BarContext(
                BarNumber: 8,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 7,
                BarsUntilSectionEnd: 0);

            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtSectionEnd = true,
                SectionEndBarsWindow = 2
            };

            // Act
            var result = PhraseHookWindowResolver.Resolve(ctx, policy);

            // Assert
            Assert.False(result.InSectionEndWindow);
        }

        [Fact]
        public void Resolve_AllowFillsAtSectionEndFalse_InWindowWhenWithinBarsFromEnd()
        {
            // Arrange
            var ctx = new BarContext(
                BarNumber: 7,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 6,
                BarsUntilSectionEnd: 1);

            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtSectionEnd = false,
                SectionEndBarsWindow = 2
            };

            // Act
            var result = PhraseHookWindowResolver.Resolve(ctx, policy);

            // Assert - 1 < 2
            Assert.True(result.InSectionEndWindow);
        }

        [Fact]
        public void Resolve_AllowFillsAtSectionEndFalse_NotInWindowWhenOutsideBarsFromEnd()
        {
            // Arrange
            var ctx = new BarContext(
                BarNumber: 1,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 0,
                BarsUntilSectionEnd: 7);

            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtSectionEnd = false,
                SectionEndBarsWindow = 2
            };

            // Act
            var result = PhraseHookWindowResolver.Resolve(ctx, policy);

            // Assert - 7 >= 2
            Assert.False(result.InSectionEndWindow);
        }

        #endregion

        #region Both Windows Tests

        [Fact]
        public void Resolve_BothWindowsConfigured_BothCanBeActive()
        {
            // Arrange
            var ctx = new BarContext(
                BarNumber: 8,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 7,
                BarsUntilSectionEnd: 0);

            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 2,
                AllowFillsAtSectionEnd = false,
                SectionEndBarsWindow = 1
            };

            // Act
            var result = PhraseHookWindowResolver.Resolve(ctx, policy);

            // Assert - both windows are active
            Assert.True(result.InPhraseEndWindow);
            Assert.True(result.InSectionEndWindow);
        }

        [Fact]
        public void Resolve_BothWindowsConfiguredDifferentSizes_IndependentEvaluation()
        {
            // Arrange - bar at 1 bar from section end
            var ctx = new BarContext(
                BarNumber: 7,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 6,
                BarsUntilSectionEnd: 1);

            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 2, // 1 < 2 = in window
                AllowFillsAtSectionEnd = false,
                SectionEndBarsWindow = 1 // 1 >= 1 = NOT in window
            };

            // Act
            var result = PhraseHookWindowResolver.Resolve(ctx, policy);

            // Assert
            Assert.True(result.InPhraseEndWindow);
            Assert.False(result.InSectionEndWindow);
        }

        #endregion

        #region Enabled Fill Tags Tests

        [Fact]
        public void Resolve_WithEnabledFillTags_ReturnsTagList()
        {
            // Arrange
            var ctx = new BarContext(
                BarNumber: 1,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 0,
                BarsUntilSectionEnd: 7);

            var policy = new GroovePhraseHookPolicy
            {
                EnabledFillTags = new List<string> { "Fill", "Pickup", "Drive" }
            };

            // Act
            var result = PhraseHookWindowResolver.Resolve(ctx, policy);

            // Assert
            Assert.Equal(3, result.EnabledFillTags.Count);
            Assert.Contains("Fill", result.EnabledFillTags);
            Assert.Contains("Pickup", result.EnabledFillTags);
            Assert.Contains("Drive", result.EnabledFillTags);
        }

        [Fact]
        public void Resolve_WithNullEnabledFillTags_ReturnsEmptyList()
        {
            // Arrange
            var ctx = new BarContext(
                BarNumber: 1,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 0,
                BarsUntilSectionEnd: 7);

            var policy = new GroovePhraseHookPolicy
            {
                EnabledFillTags = null!
            };

            // Act
            var result = PhraseHookWindowResolver.Resolve(ctx, policy);

            // Assert
            Assert.Empty(result.EnabledFillTags);
        }

        [Fact]
        public void Resolve_WithEmptyEnabledFillTags_ReturnsEmptyList()
        {
            // Arrange
            var ctx = new BarContext(
                BarNumber: 1,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 0,
                BarsUntilSectionEnd: 7);

            var policy = new GroovePhraseHookPolicy
            {
                EnabledFillTags = new List<string>()
            };

            // Act
            var result = PhraseHookWindowResolver.Resolve(ctx, policy);

            // Assert
            Assert.Empty(result.EnabledFillTags);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Resolve_NegativeBarsUntilSectionEnd_NotInWindow()
        {
            // Arrange - edge case: negative value (shouldn't happen but should handle gracefully)
            var ctx = new BarContext(
                BarNumber: 1,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 0,
                BarsUntilSectionEnd: -1);

            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 2,
                AllowFillsAtSectionEnd = false,
                SectionEndBarsWindow = 2
            };

            // Act
            var result = PhraseHookWindowResolver.Resolve(ctx, policy);

            // Assert - negative value means BarsUntilSectionEnd >= 0 fails
            Assert.False(result.InPhraseEndWindow);
            Assert.False(result.InSectionEndWindow);
        }

        [Fact]
        public void Resolve_LargeWindowSize_CapturesAllBars()
        {
            // Arrange
            var ctx = new BarContext(
                BarNumber: 1,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 0,
                BarsUntilSectionEnd: 7);

            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 100 // Very large window
            };

            // Act
            var result = PhraseHookWindowResolver.Resolve(ctx, policy);

            // Assert - 7 < 100, so in window
            Assert.True(result.InPhraseEndWindow);
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void Resolve_SameInputs_ProducesSameOutput()
        {
            // Arrange
            var ctx = new BarContext(
                BarNumber: 4,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 3,
                BarsUntilSectionEnd: 1);

            var policy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 2,
                AllowFillsAtSectionEnd = false,
                SectionEndBarsWindow = 1,
                EnabledFillTags = new List<string> { "Fill" }
            };

            // Act - call multiple times
            var result1 = PhraseHookWindowResolver.Resolve(ctx, policy);
            var result2 = PhraseHookWindowResolver.Resolve(ctx, policy);
            var result3 = PhraseHookWindowResolver.Resolve(ctx, policy);

            // Assert - all results identical
            Assert.Equal(result1.InPhraseEndWindow, result2.InPhraseEndWindow);
            Assert.Equal(result1.InSectionEndWindow, result2.InSectionEndWindow);
            Assert.Equal(result1.EnabledFillTags.Count, result2.EnabledFillTags.Count);
            Assert.Equal(result2.InPhraseEndWindow, result3.InPhraseEndWindow);
            Assert.Equal(result2.InSectionEndWindow, result3.InSectionEndWindow);
        }

        #endregion
    }
}
