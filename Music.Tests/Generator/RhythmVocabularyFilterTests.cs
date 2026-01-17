// AI: purpose=Unit tests for RhythmVocabularyFilter; validates position classification and filtering.
// AI: deps=XUnit; tests deterministic behavior of rhythm vocabulary rules.
// AI: coverage=Story G3 acceptance criteria: IsAllowed, Filter<T>, position detection, role-specific rules.

using Music.Generator;
using Xunit;

namespace Music.Tests.Generator
{
    public class RhythmVocabularyFilterTests
    {
        #region IsAllowed Tests

        [Fact]
        public void IsAllowed_WithNullPolicy_ReturnsTrue()
        {
            // Arrange & Act
            bool result = RhythmVocabularyFilter.IsAllowed("Kick", 1.5m, 4, null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsAllowed_WithNoVocabularyForRole_ReturnsTrue()
        {
            // Arrange
            var policy = new GrooveRoleConstraintPolicy
            {
                RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>()
            };

            // Act
            bool result = RhythmVocabularyFilter.IsAllowed("UnknownRole", 1.5m, 4, policy);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsAllowed_AllowSyncopationTrue_AllowsOffbeat()
        {
            // Arrange
            var policy = new GrooveRoleConstraintPolicy
            {
                RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
                {
                    ["Kick"] = new RoleRhythmVocabulary { AllowSyncopation = true }
                }
            };

            // Act
            bool result = RhythmVocabularyFilter.IsAllowed("Kick", 1.5m, 4, policy);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsAllowed_AllowSyncopationFalse_DisallowsOffbeat()
        {
            // Arrange
            var policy = new GrooveRoleConstraintPolicy
            {
                RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
                {
                    ["Kick"] = new RoleRhythmVocabulary { AllowSyncopation = false }
                }
            };

            // Act
            bool result = RhythmVocabularyFilter.IsAllowed("Kick", 1.5m, 4, policy);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAllowed_AllowAnticipationTrue_AllowsPickup()
        {
            // Arrange
            var policy = new GrooveRoleConstraintPolicy
            {
                RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
                {
                    ["Snare"] = new RoleRhythmVocabulary { AllowAnticipation = true }
                }
            };

            // Act
            bool result = RhythmVocabularyFilter.IsAllowed("Snare", 4.75m, 4, policy);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsAllowed_AllowAnticipationFalse_DisallowsPickup()
        {
            // Arrange
            var policy = new GrooveRoleConstraintPolicy
            {
                RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
                {
                    ["Snare"] = new RoleRhythmVocabulary { AllowAnticipation = false }
                }
            };

            // Act
            bool result = RhythmVocabularyFilter.IsAllowed("Snare", 4.75m, 4, policy);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAllowed_BothRulesFalse_DisallowsBoth()
        {
            // Arrange
            var policy = new GrooveRoleConstraintPolicy
            {
                RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
                {
                    ["Hat"] = new RoleRhythmVocabulary
                    {
                        AllowSyncopation = false,
                        AllowAnticipation = false
                    }
                }
            };

            // Act & Assert
            Assert.False(RhythmVocabularyFilter.IsAllowed("Hat", 1.5m, 4, policy)); // Offbeat
            Assert.False(RhythmVocabularyFilter.IsAllowed("Hat", 4.75m, 4, policy)); // Pickup
        }

        [Fact]
        public void IsAllowed_StrongBeat_AlwaysAllowed()
        {
            // Arrange
            var policy = new GrooveRoleConstraintPolicy
            {
                RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
                {
                    ["Kick"] = new RoleRhythmVocabulary
                    {
                        AllowSyncopation = false,
                        AllowAnticipation = false
                    }
                }
            };

            // Act & Assert - strong beats not affected by syncopation/anticipation rules
            Assert.True(RhythmVocabularyFilter.IsAllowed("Kick", 1m, 4, policy));
            Assert.True(RhythmVocabularyFilter.IsAllowed("Kick", 2m, 4, policy));
            Assert.True(RhythmVocabularyFilter.IsAllowed("Kick", 3m, 4, policy));
            Assert.True(RhythmVocabularyFilter.IsAllowed("Kick", 4m, 4, policy));
        }

        #endregion

        #region Filter<T> Tests

        private record TestEvent(string Role, decimal Beat);

        [Fact]
        public void Filter_WithNullEvents_ReturnsEmpty()
        {
            // Act
            var result = RhythmVocabularyFilter.Filter<TestEvent>(
                null!,
                e => e.Role,
                e => e.Beat,
                4,
                null);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Filter_WithNullPolicy_ReturnsAll()
        {
            // Arrange
            var events = new List<TestEvent>
            {
                new("Kick", 1m),
                new("Kick", 1.5m),
                new("Kick", 2m)
            };

            // Act
            var result = RhythmVocabularyFilter.Filter(
                events,
                e => e.Role,
                e => e.Beat,
                4,
                null);

            // Assert
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void Filter_AllowSyncopationFalse_FiltersOffbeats()
        {
            // Arrange
            var policy = new GrooveRoleConstraintPolicy
            {
                RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
                {
                    ["Kick"] = new RoleRhythmVocabulary { AllowSyncopation = false }
                }
            };

            var events = new List<TestEvent>
            {
                new("Kick", 1m),    // Strong beat - kept
                new("Kick", 1.5m),  // Offbeat - filtered
                new("Kick", 2m),    // Strong beat - kept
                new("Kick", 2.5m)   // Offbeat - filtered
            };

            // Act
            var result = RhythmVocabularyFilter.Filter(
                events,
                e => e.Role,
                e => e.Beat,
                4,
                policy);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, e => e.Beat == 1m);
            Assert.Contains(result, e => e.Beat == 2m);
        }

        [Fact]
        public void Filter_AllowAnticipationFalse_FiltersPickups()
        {
            // Arrange
            var policy = new GrooveRoleConstraintPolicy
            {
                RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
                {
                    ["Snare"] = new RoleRhythmVocabulary { AllowAnticipation = false }
                }
            };

            var events = new List<TestEvent>
            {
                new("Snare", 2m),     // Strong beat - kept
                new("Snare", 2.75m),  // Pickup - filtered
                new("Snare", 4m),     // Strong beat - kept
                new("Snare", 4.75m)   // Pickup - filtered
            };

            // Act
            var result = RhythmVocabularyFilter.Filter(
                events,
                e => e.Role,
                e => e.Beat,
                4,
                policy);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, e => e.Beat == 2m);
            Assert.Contains(result, e => e.Beat == 4m);
        }

        [Fact]
        public void Filter_DifferentRolesHaveDifferentRules()
        {
            // Arrange
            var policy = new GrooveRoleConstraintPolicy
            {
                RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
                {
                    ["Kick"] = new RoleRhythmVocabulary { AllowSyncopation = false },
                    ["Hat"] = new RoleRhythmVocabulary { AllowSyncopation = true }
                }
            };

            var events = new List<TestEvent>
            {
                new("Kick", 1.5m),  // Offbeat - filtered (Kick doesn't allow syncopation)
                new("Hat", 1.5m)    // Offbeat - kept (Hat allows syncopation)
            };

            // Act
            var result = RhythmVocabularyFilter.Filter(
                events,
                e => e.Role,
                e => e.Beat,
                4,
                policy);

            // Assert
            Assert.Single(result);
            Assert.Equal("Hat", result[0].Role);
        }

        #endregion

        #region Position Detection Tests

        [Theory]
        [InlineData(1.5, 4, true)]   // Offbeat in 4/4
        [InlineData(2.5, 4, true)]   // Offbeat in 4/4
        [InlineData(3.5, 4, true)]   // Offbeat in 4/4
        [InlineData(4.5, 4, true)]   // Offbeat in 4/4
        [InlineData(1.0, 4, false)]  // Downbeat
        [InlineData(2.0, 4, false)]  // Strong beat
        [InlineData(1.25, 4, false)] // Sixteenth (not offbeat)
        [InlineData(1.75, 4, false)] // Sixteenth (not offbeat)
        public void IsOffbeatPosition_DetectsOffbeatsCorrectly(double beatDouble, int beatsPerBar, bool expectedOffbeat)
        {
            // Arrange
            decimal beat = (decimal)beatDouble;

            // Act
            bool result = RhythmVocabularyFilter.IsOffbeatPosition(beat, beatsPerBar);

            // Assert
            Assert.Equal(expectedOffbeat, result);
        }

        [Theory]
        [InlineData(4.75, 4, true)]  // Pickup before beat 1
        [InlineData(2.75, 4, true)]  // Pickup before beat 3
        [InlineData(1.75, 4, true)]  // Pickup before beat 2
        [InlineData(1.0, 4, false)]  // Downbeat
        [InlineData(1.5, 4, false)]  // Offbeat (not pickup)
        [InlineData(2.0, 4, false)]  // Strong beat
        public void IsPickupPosition_DetectsPickupsCorrectly(double beatDouble, int beatsPerBar, bool expectedPickup)
        {
            // Arrange
            decimal beat = (decimal)beatDouble;

            // Act
            bool result = RhythmVocabularyFilter.IsPickupPosition(beat, beatsPerBar);

            // Assert
            Assert.Equal(expectedPickup, result);
        }

        [Fact]
        public void IsOffbeatPosition_WorksIn3_4Time()
        {
            // Arrange & Act & Assert
            Assert.True(RhythmVocabularyFilter.IsOffbeatPosition(1.5m, 3));
            Assert.True(RhythmVocabularyFilter.IsOffbeatPosition(2.5m, 3));
            Assert.True(RhythmVocabularyFilter.IsOffbeatPosition(3.5m, 3));
            Assert.False(RhythmVocabularyFilter.IsOffbeatPosition(1m, 3));
            Assert.False(RhythmVocabularyFilter.IsOffbeatPosition(2m, 3));
            Assert.False(RhythmVocabularyFilter.IsOffbeatPosition(3m, 3));
        }

        [Fact]
        public void IsPickupPosition_Detects75Positions()
        {
            // Arrange & Act & Assert - .75 positions are pickups
            Assert.True(RhythmVocabularyFilter.IsPickupPosition(1.75m, 4));
            Assert.True(RhythmVocabularyFilter.IsPickupPosition(2.75m, 4));
            Assert.True(RhythmVocabularyFilter.IsPickupPosition(3.75m, 4));
            Assert.True(RhythmVocabularyFilter.IsPickupPosition(4.75m, 4));
            
            // Other positions are not pickups
            Assert.False(RhythmVocabularyFilter.IsPickupPosition(4.5m, 4)); // Offbeat, not pickup
            Assert.False(RhythmVocabularyFilter.IsPickupPosition(4.8m, 4)); // Not exactly .75
            Assert.False(RhythmVocabularyFilter.IsPickupPosition(4.9m, 4)); // Not exactly .75
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void Filter_IsDeterministic()
        {
            // Arrange
            var policy = new GrooveRoleConstraintPolicy
            {
                RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
                {
                    ["Kick"] = new RoleRhythmVocabulary
                    {
                        AllowSyncopation = false,
                        AllowAnticipation = false
                    }
                }
            };

            var events = new List<TestEvent>
            {
                new("Kick", 1m),
                new("Kick", 1.5m),
                new("Kick", 2m),
                new("Kick", 2.75m),
                new("Kick", 3m)
            };

            // Act - run filter multiple times
            var result1 = RhythmVocabularyFilter.Filter(events, e => e.Role, e => e.Beat, 4, policy);
            var result2 = RhythmVocabularyFilter.Filter(events, e => e.Role, e => e.Beat, 4, policy);
            var result3 = RhythmVocabularyFilter.Filter(events, e => e.Role, e => e.Beat, 4, policy);

            // Assert - all results identical
            Assert.Equal(result1.Count, result2.Count);
            Assert.Equal(result1.Count, result3.Count);
            
            for (int i = 0; i < result1.Count; i++)
            {
                Assert.Equal(result1[i].Beat, result2[i].Beat);
                Assert.Equal(result1[i].Beat, result3[i].Beat);
            }
        }

        #endregion
    }
}
