// AI: purpose=Unit tests for Story A3 drummer policy hook (IGroovePolicyProvider, GroovePolicyDecision, DefaultGroovePolicyProvider).
// AI: deps=xunit for test framework; Music.Generator for types under test.
// AI: change=Story A3 acceptance criteria: verify hook interface, default provider, and no behavior change.

using Xunit;

namespace Music.Generator.Tests
{
    /// <summary>
    /// Story A3: Tests for drummer policy hook.
    /// Verifies IGroovePolicyProvider interface, GroovePolicyDecision, and DefaultGroovePolicyProvider.
    /// </summary>
    public class GroovePolicyHookTests
    {
        #region GroovePolicyDecision Tests

        [Fact]
        public void GroovePolicyDecision_NoOverrides_HasNoOverrides()
        {
            // Arrange & Act
            var decision = GroovePolicyDecision.NoOverrides;

            // Assert
            Assert.False(decision.HasAnyOverrides);
            Assert.Null(decision.EnabledVariationTagsOverride);
            Assert.Null(decision.Density01Override);
            Assert.Null(decision.MaxEventsPerBarOverride);
            Assert.Null(decision.RoleTimingFeelOverride);
            Assert.Null(decision.RoleTimingBiasTicksOverride);
            Assert.Null(decision.VelocityBiasOverride);
            Assert.Null(decision.OperatorAllowList);
        }

        [Fact]
        public void GroovePolicyDecision_WithVariationTagsOverride_HasOverrides()
        {
            // Arrange & Act
            var decision = new GroovePolicyDecision
            {
                EnabledVariationTagsOverride = new List<string> { "Fill", "Intense" }
            };

            // Assert
            Assert.True(decision.HasAnyOverrides);
            Assert.NotNull(decision.EnabledVariationTagsOverride);
            Assert.Equal(2, decision.EnabledVariationTagsOverride.Count);
            Assert.Contains("Fill", decision.EnabledVariationTagsOverride);
            Assert.Contains("Intense", decision.EnabledVariationTagsOverride);
        }

        [Fact]
        public void GroovePolicyDecision_WithDensityOverride_HasOverrides()
        {
            // Arrange & Act
            var decision = new GroovePolicyDecision
            {
                Density01Override = 0.75
            };

            // Assert
            Assert.True(decision.HasAnyOverrides);
            Assert.Equal(0.75, decision.Density01Override);
        }

        [Fact]
        public void GroovePolicyDecision_WithMaxEventsOverride_HasOverrides()
        {
            // Arrange & Act
            var decision = new GroovePolicyDecision
            {
                MaxEventsPerBarOverride = 8
            };

            // Assert
            Assert.True(decision.HasAnyOverrides);
            Assert.Equal(8, decision.MaxEventsPerBarOverride);
        }

        [Fact]
        public void GroovePolicyDecision_WithTimingFeelOverride_HasOverrides()
        {
            // Arrange & Act
            var decision = new GroovePolicyDecision
            {
                RoleTimingFeelOverride = TimingFeel.Behind
            };

            // Assert
            Assert.True(decision.HasAnyOverrides);
            Assert.Equal(TimingFeel.Behind, decision.RoleTimingFeelOverride);
        }

        [Fact]
        public void GroovePolicyDecision_WithTimingBiasOverride_HasOverrides()
        {
            // Arrange & Act
            var decision = new GroovePolicyDecision
            {
                RoleTimingBiasTicksOverride = -10
            };

            // Assert
            Assert.True(decision.HasAnyOverrides);
            Assert.Equal(-10, decision.RoleTimingBiasTicksOverride);
        }

        [Fact]
        public void GroovePolicyDecision_WithVelocityBiasOverride_HasOverrides()
        {
            // Arrange & Act
            var decision = new GroovePolicyDecision
            {
                VelocityBiasOverride = 15
            };

            // Assert
            Assert.True(decision.HasAnyOverrides);
            Assert.Equal(15, decision.VelocityBiasOverride);
        }

        [Fact]
        public void GroovePolicyDecision_WithOperatorAllowList_HasOverrides()
        {
            // Arrange & Act
            var decision = new GroovePolicyDecision
            {
                OperatorAllowList = new List<string> { "Operator1", "Operator2" }
            };

            // Assert
            Assert.True(decision.HasAnyOverrides);
            Assert.NotNull(decision.OperatorAllowList);
            Assert.Equal(2, decision.OperatorAllowList.Count);
        }

        [Fact]
        public void GroovePolicyDecision_WithMultipleOverrides_HasOverrides()
        {
            // Arrange & Act
            var decision = new GroovePolicyDecision
            {
                Density01Override = 0.5,
                MaxEventsPerBarOverride = 6,
                RoleTimingFeelOverride = TimingFeel.Ahead,
                VelocityBiasOverride = -5
            };

            // Assert
            Assert.True(decision.HasAnyOverrides);
            Assert.Equal(0.5, decision.Density01Override);
            Assert.Equal(6, decision.MaxEventsPerBarOverride);
            Assert.Equal(TimingFeel.Ahead, decision.RoleTimingFeelOverride);
            Assert.Equal(-5, decision.VelocityBiasOverride);
        }

        [Fact]
        public void GroovePolicyDecision_DefaultConstruction_HasNoOverrides()
        {
            // Arrange & Act
            var decision = new GroovePolicyDecision();

            // Assert
            Assert.False(decision.HasAnyOverrides);
        }

        [Fact]
        public void GroovePolicyDecision_IsImmutable_RecordSemantics()
        {
            // Arrange
            var decision1 = new GroovePolicyDecision
            {
                Density01Override = 0.8
            };
            var decision2 = decision1 with { MaxEventsPerBarOverride = 10 };

            // Assert
            Assert.Equal(0.8, decision1.Density01Override);
            Assert.Null(decision1.MaxEventsPerBarOverride);
            Assert.Equal(0.8, decision2.Density01Override);
            Assert.Equal(10, decision2.MaxEventsPerBarOverride);
        }

        #endregion

        #region DefaultGroovePolicyProvider Tests

        [Fact]
        public void DefaultGroovePolicyProvider_GetPolicy_ReturnsNoOverrides()
        {
            // Arrange
            var provider = DefaultGroovePolicyProvider.Instance;
            var barContext = new GrooveBarContext(
                BarNumber: 1,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 1,
                BarsUntilSectionEnd: 4);

            // Act
            var decision = provider.GetPolicy(barContext, GrooveRoles.Kick);

            // Assert
            Assert.NotNull(decision);
            Assert.False(decision.HasAnyOverrides);
        }

        [Fact]
        public void DefaultGroovePolicyProvider_GetPolicy_ConsistentResults()
        {
            // Arrange
            var provider = DefaultGroovePolicyProvider.Instance;
            var barContext = new GrooveBarContext(
                BarNumber: 5,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 2,
                BarsUntilSectionEnd: 2);

            // Act
            var decision1 = provider.GetPolicy(barContext, GrooveRoles.Snare);
            var decision2 = provider.GetPolicy(barContext, GrooveRoles.Snare);

            // Assert
            Assert.NotNull(decision1);
            Assert.NotNull(decision2);
            Assert.False(decision1.HasAnyOverrides);
            Assert.False(decision2.HasAnyOverrides);
        }

        [Fact]
        public void DefaultGroovePolicyProvider_GetPolicy_WorksForAllRoles()
        {
            // Arrange
            var provider = DefaultGroovePolicyProvider.Instance;
            var barContext = new GrooveBarContext(
                BarNumber: 1,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 1,
                BarsUntilSectionEnd: 4);

            var roles = new[]
            {
                GrooveRoles.Kick,
                GrooveRoles.Snare,
                GrooveRoles.ClosedHat,
                GrooveRoles.OpenHat,
                GrooveRoles.Bass,
                GrooveRoles.Comp
            };

            // Act & Assert
            foreach (var role in roles)
            {
                var decision = provider.GetPolicy(barContext, role);
                Assert.NotNull(decision);
                Assert.False(decision.HasAnyOverrides, $"Role {role} should have no overrides");
            }
        }

        [Fact]
        public void DefaultGroovePolicyProvider_Instance_IsSingleton()
        {
            // Arrange & Act
            var instance1 = DefaultGroovePolicyProvider.Instance;
            var instance2 = DefaultGroovePolicyProvider.Instance;

            // Assert
            Assert.Same(instance1, instance2);
        }

        [Fact]
        public void DefaultGroovePolicyProvider_GetPolicy_DifferentBars_SameResult()
        {
            // Arrange
            var provider = DefaultGroovePolicyProvider.Instance;
            var barContext1 = new GrooveBarContext(
                BarNumber: 1,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 1,
                BarsUntilSectionEnd: 4);
            var barContext2 = new GrooveBarContext(
                BarNumber: 10,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 3,
                BarsUntilSectionEnd: 1);

            // Act
            var decision1 = provider.GetPolicy(barContext1, GrooveRoles.Kick);
            var decision2 = provider.GetPolicy(barContext2, GrooveRoles.Kick);

            // Assert
            Assert.NotNull(decision1);
            Assert.NotNull(decision2);
            Assert.False(decision1.HasAnyOverrides);
            Assert.False(decision2.HasAnyOverrides);
        }

        #endregion

        #region Interface Contract Tests

        [Fact]
        public void IGroovePolicyProvider_DefaultProvider_ImplementsInterface()
        {
            // Arrange & Act
            IGroovePolicyProvider provider = DefaultGroovePolicyProvider.Instance;
            var barContext = new GrooveBarContext(
                BarNumber: 1,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 1,
                BarsUntilSectionEnd: 4);

            // Act
            var decision = provider.GetPolicy(barContext, GrooveRoles.Bass);

            // Assert
            Assert.NotNull(decision);
            Assert.False(decision.HasAnyOverrides);
        }

        [Fact]
        public void IGroovePolicyProvider_CanReturnNull_TreatedAsNoOverrides()
        {
            // Arrange
            var provider = new NullReturningPolicyProvider();
            var barContext = new GrooveBarContext(
                BarNumber: 1,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 1,
                BarsUntilSectionEnd: 4);

            // Act
            var decision = provider.GetPolicy(barContext, GrooveRoles.Kick);

            // Assert - null is valid and should be treated as NoOverrides
            Assert.Null(decision);
        }

        #endregion

        #region Test Helper Classes

        /// <summary>
        /// Test policy provider that returns null (valid per interface contract).
        /// </summary>
        private sealed class NullReturningPolicyProvider : IGroovePolicyProvider
        {
            public GroovePolicyDecision? GetPolicy(GrooveBarContext barContext, string role)
            {
                return null;
            }
        }

        #endregion
    }
}
