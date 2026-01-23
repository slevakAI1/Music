// AI: purpose=Unit tests for RolePresenceGate; validates orchestration role presence checks and defaulting behavior.
// AI: deps=XUnit; tests deterministic behavior of role gating across section types.
// AI: coverage=Story G8 acceptance criteria: defaulting behavior, null policy handling, DrumKit fallback.

using Music.Generator;
using Music.Generator.Groove;
using Xunit;

namespace Music.Tests.Generator
{
    public class RolePresenceGateTests
    {
        #region Null Policy Tests

        [Fact]
        public void IsRolePresent_WithNullPolicy_ReturnsTrue()
        {
            // Arrange & Act
            bool result = RolePresenceGate.IsRolePresent("Verse", "Kick", null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRolePresent_WithNullPolicy_ReturnsTrueForAnyRole()
        {
            // Arrange & Act & Assert
            Assert.True(RolePresenceGate.IsRolePresent("Verse", "Kick", null));
            Assert.True(RolePresenceGate.IsRolePresent("Chorus", "Snare", null));
            Assert.True(RolePresenceGate.IsRolePresent("Bridge", "ClosedHat", null));
            Assert.True(RolePresenceGate.IsRolePresent("Intro", "Bass", null));
        }

        #endregion

        #region Missing Section Defaults Tests

        [Fact]
        public void IsRolePresent_WithEmptySectionDefaults_ReturnsTrue()
        {
            // Arrange
            var policy = new GrooveOrchestrationPolicy
            {
                DefaultsBySectionType = new List<SectionRolePresenceDefaults>()
            };

            // Act
            bool result = RolePresenceGate.IsRolePresent("Verse", "Kick", policy);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRolePresent_WithMissingSectionType_ReturnsTrue()
        {
            // Arrange
            var policy = new GrooveOrchestrationPolicy
            {
                DefaultsBySectionType = new List<SectionRolePresenceDefaults>
                {
                    new SectionRolePresenceDefaults
                    {
                        SectionType = "Chorus",
                        RolePresent = new Dictionary<string, bool> { ["Kick"] = true }
                    }
                }
            };

            // Act - asking for "Verse" which doesn't exist
            bool result = RolePresenceGate.IsRolePresent("Verse", "Kick", policy);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region Role Present Lookup Tests

        [Fact]
        public void IsRolePresent_RoleExplicitlyTrue_ReturnsTrue()
        {
            // Arrange
            var policy = new GrooveOrchestrationPolicy
            {
                DefaultsBySectionType = new List<SectionRolePresenceDefaults>
                {
                    new SectionRolePresenceDefaults
                    {
                        SectionType = "Verse",
                        RolePresent = new Dictionary<string, bool> { ["Kick"] = true }
                    }
                }
            };

            // Act
            bool result = RolePresenceGate.IsRolePresent("Verse", "Kick", policy);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRolePresent_RoleExplicitlyFalse_ReturnsFalse()
        {
            // Arrange
            var policy = new GrooveOrchestrationPolicy
            {
                DefaultsBySectionType = new List<SectionRolePresenceDefaults>
                {
                    new SectionRolePresenceDefaults
                    {
                        SectionType = "Verse",
                        RolePresent = new Dictionary<string, bool> { ["Kick"] = false }
                    }
                }
            };

            // Act
            bool result = RolePresenceGate.IsRolePresent("Verse", "Kick", policy);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsRolePresent_MultipleRolesConfigured_ReturnsCorrectValue()
        {
            // Arrange
            var policy = new GrooveOrchestrationPolicy
            {
                DefaultsBySectionType = new List<SectionRolePresenceDefaults>
                {
                    new SectionRolePresenceDefaults
                    {
                        SectionType = "Verse",
                        RolePresent = new Dictionary<string, bool>
                        {
                            ["Kick"] = true,
                            ["Snare"] = false,
                            ["ClosedHat"] = true
                        }
                    }
                }
            };

            // Act & Assert
            Assert.True(RolePresenceGate.IsRolePresent("Verse", "Kick", policy));
            Assert.False(RolePresenceGate.IsRolePresent("Verse", "Snare", policy));
            Assert.True(RolePresenceGate.IsRolePresent("Verse", "ClosedHat", policy));
        }

        #endregion

        #region DrumKit Fallback Tests

        [Fact]
        public void IsRolePresent_RoleNotListedWithDrumKitTrue_ReturnsTrue()
        {
            // Arrange
            var policy = new GrooveOrchestrationPolicy
            {
                DefaultsBySectionType = new List<SectionRolePresenceDefaults>
                {
                    new SectionRolePresenceDefaults
                    {
                        SectionType = "Verse",
                        RolePresent = new Dictionary<string, bool>
                        {
                            ["DrumKit"] = true
                        }
                    }
                }
            };

            // Act - Kick not explicitly listed, should fallback to DrumKit
            bool result = RolePresenceGate.IsRolePresent("Verse", "Kick", policy);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRolePresent_RoleNotListedWithDrumKitFalse_ReturnsFalse()
        {
            // Arrange
            var policy = new GrooveOrchestrationPolicy
            {
                DefaultsBySectionType = new List<SectionRolePresenceDefaults>
                {
                    new SectionRolePresenceDefaults
                    {
                        SectionType = "Intro",
                        RolePresent = new Dictionary<string, bool>
                        {
                            ["DrumKit"] = false
                        }
                    }
                }
            };

            // Act - Snare not explicitly listed, should fallback to DrumKit=false
            bool result = RolePresenceGate.IsRolePresent("Intro", "Snare", policy);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsRolePresent_ExplicitRoleOverridesDrumKit()
        {
            // Arrange
            var policy = new GrooveOrchestrationPolicy
            {
                DefaultsBySectionType = new List<SectionRolePresenceDefaults>
                {
                    new SectionRolePresenceDefaults
                    {
                        SectionType = "Verse",
                        RolePresent = new Dictionary<string, bool>
                        {
                            ["DrumKit"] = false,
                            ["Kick"] = true // Explicit override for Kick
                        }
                    }
                }
            };

            // Act
            bool kickResult = RolePresenceGate.IsRolePresent("Verse", "Kick", policy);
            bool snareResult = RolePresenceGate.IsRolePresent("Verse", "Snare", policy); // Falls back to DrumKit

            // Assert
            Assert.True(kickResult); // Explicit true overrides DrumKit=false
            Assert.False(snareResult); // Falls back to DrumKit=false
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void IsRolePresent_WithEmptyRoleName_ReturnsTrue()
        {
            // Arrange
            var policy = new GrooveOrchestrationPolicy
            {
                DefaultsBySectionType = new List<SectionRolePresenceDefaults>
                {
                    new SectionRolePresenceDefaults
                    {
                        SectionType = "Verse",
                        RolePresent = new Dictionary<string, bool> { ["Kick"] = false }
                    }
                }
            };

            // Act
            bool result = RolePresenceGate.IsRolePresent("Verse", "", policy);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRolePresent_WithWhitespaceRoleName_ReturnsTrue()
        {
            // Arrange
            var policy = new GrooveOrchestrationPolicy
            {
                DefaultsBySectionType = new List<SectionRolePresenceDefaults>
                {
                    new SectionRolePresenceDefaults
                    {
                        SectionType = "Verse",
                        RolePresent = new Dictionary<string, bool> { ["Kick"] = false }
                    }
                }
            };

            // Act
            bool result = RolePresenceGate.IsRolePresent("Verse", "   ", policy);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRolePresent_RoleNotListedNoDrumKit_ReturnsTrue()
        {
            // Arrange
            var policy = new GrooveOrchestrationPolicy
            {
                DefaultsBySectionType = new List<SectionRolePresenceDefaults>
                {
                    new SectionRolePresenceDefaults
                    {
                        SectionType = "Verse",
                        RolePresent = new Dictionary<string, bool>
                        {
                            ["Kick"] = true,
                            ["Snare"] = false
                            // No DrumKit fallback defined
                        }
                    }
                }
            };

            // Act - ClosedHat not listed and no DrumKit fallback
            bool result = RolePresenceGate.IsRolePresent("Verse", "ClosedHat", policy);

            // Assert - Defaults to true when neither role nor DrumKit found
            Assert.True(result);
        }

        [Fact]
        public void IsRolePresent_SectionTypeMatchIsCaseInsensitive()
        {
            // Arrange
            var policy = new GrooveOrchestrationPolicy
            {
                DefaultsBySectionType = new List<SectionRolePresenceDefaults>
                {
                    new SectionRolePresenceDefaults
                    {
                        SectionType = "Verse",
                        RolePresent = new Dictionary<string, bool> { ["Kick"] = false }
                    }
                }
            };

            // Act - using "verse" instead of "Verse"
            bool result = RolePresenceGate.IsRolePresent("verse", "Kick", policy);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Multiple Section Types Tests

        [Fact]
        public void IsRolePresent_MultipleSectionTypes_ReturnsCorrectValuePerSection()
        {
            // Arrange
            var policy = new GrooveOrchestrationPolicy
            {
                DefaultsBySectionType = new List<SectionRolePresenceDefaults>
                {
                    new SectionRolePresenceDefaults
                    {
                        SectionType = "Verse",
                        RolePresent = new Dictionary<string, bool> { ["Kick"] = true, ["OpenHat"] = false }
                    },
                    new SectionRolePresenceDefaults
                    {
                        SectionType = "Chorus",
                        RolePresent = new Dictionary<string, bool> { ["Kick"] = true, ["OpenHat"] = true }
                    },
                    new SectionRolePresenceDefaults
                    {
                        SectionType = "Intro",
                        RolePresent = new Dictionary<string, bool> { ["DrumKit"] = false }
                    }
                }
            };

            // Act & Assert
            Assert.True(RolePresenceGate.IsRolePresent("Verse", "Kick", policy));
            Assert.False(RolePresenceGate.IsRolePresent("Verse", "OpenHat", policy));
            Assert.True(RolePresenceGate.IsRolePresent("Chorus", "Kick", policy));
            Assert.True(RolePresenceGate.IsRolePresent("Chorus", "OpenHat", policy));
            Assert.False(RolePresenceGate.IsRolePresent("Intro", "Kick", policy)); // DrumKit fallback
            Assert.False(RolePresenceGate.IsRolePresent("Intro", "Snare", policy)); // DrumKit fallback
        }

        #endregion
    }
}
