namespace Music.Tests.Generator.Groove
{
    using Music.Generator.Groove;

    // AI: purpose=Unit tests for GrooveInstanceLayer query methods (GetOnsets, GetActiveRoles, HasRole).
    // AI: invariants=Tests verify null-safety, empty list behavior, deterministic results, role mapping correctness.
    public sealed class GrooveInstanceLayerQueryTests
    {
        #region GetOnsets Tests

        [Fact]
        public void GetOnsets_KickRole_ReturnsKickOnsets()
        {
            GrooveInstanceLayer layer = new()
            {
                KickOnsets = new List<decimal> { 1.0m, 3.0m }
            };

            IReadOnlyList<decimal> result = layer.GetOnsets(GrooveRoles.Kick);

            Assert.Equal(2, result.Count);
            Assert.Equal(1.0m, result[0]);
            Assert.Equal(3.0m, result[1]);
        }

        [Fact]
        public void GetOnsets_SnareRole_ReturnsSnareOnsets()
        {
            GrooveInstanceLayer layer = new()
            {
                SnareOnsets = new List<decimal> { 2.0m, 4.0m }
            };

            IReadOnlyList<decimal> result = layer.GetOnsets(GrooveRoles.Snare);

            Assert.Equal(2, result.Count);
            Assert.Equal(2.0m, result[0]);
            Assert.Equal(4.0m, result[1]);
        }

        [Fact]
        public void GetOnsets_ClosedHatRole_ReturnsHatOnsets()
        {
            GrooveInstanceLayer layer = new()
            {
                HatOnsets = new List<decimal> { 1.0m, 1.5m, 2.0m, 2.5m }
            };

            IReadOnlyList<decimal> result = layer.GetOnsets(GrooveRoles.ClosedHat);

            Assert.Equal(4, result.Count);
        }

        [Fact]
        public void GetOnsets_OpenHatRole_ReturnsHatOnsets()
        {
            GrooveInstanceLayer layer = new()
            {
                HatOnsets = new List<decimal> { 1.0m, 1.5m, 2.0m, 2.5m }
            };

            IReadOnlyList<decimal> result = layer.GetOnsets(GrooveRoles.OpenHat);

            Assert.Equal(4, result.Count);
        }

        [Fact]
        public void GetOnsets_BassRole_ReturnsBassOnsets()
        {
            GrooveInstanceLayer layer = new()
            {
                BassOnsets = new List<decimal> { 1.0m, 2.5m }
            };

            IReadOnlyList<decimal> result = layer.GetOnsets(GrooveRoles.Bass);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetOnsets_CompRole_ReturnsCompOnsets()
        {
            GrooveInstanceLayer layer = new()
            {
                CompOnsets = new List<decimal> { 1.0m, 2.0m, 3.0m }
            };

            IReadOnlyList<decimal> result = layer.GetOnsets(GrooveRoles.Comp);

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void GetOnsets_PadsRole_ReturnsPadsOnsets()
        {
            GrooveInstanceLayer layer = new()
            {
                PadsOnsets = new List<decimal> { 1.0m }
            };

            IReadOnlyList<decimal> result = layer.GetOnsets(GrooveRoles.Pads);

            Assert.Single(result);
        }

        [Fact]
        public void GetOnsets_UnknownRole_ReturnsEmptyList()
        {
            GrooveInstanceLayer layer = new();

            IReadOnlyList<decimal> result = layer.GetOnsets("UnknownRole");

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GetOnsets_EmptyOnsets_ReturnsEmptyList()
        {
            GrooveInstanceLayer layer = new();

            IReadOnlyList<decimal> result = layer.GetOnsets(GrooveRoles.Kick);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GetOnsets_NullRole_ThrowsArgumentNullException()
        {
            GrooveInstanceLayer layer = new();

            Assert.Throws<ArgumentNullException>(() => layer.GetOnsets(null!));
        }

        [Fact]
        public void GetOnsets_CalledTwice_ReturnsSameReference()
        {
            GrooveInstanceLayer layer = new()
            {
                KickOnsets = new List<decimal> { 1.0m }
            };

            IReadOnlyList<decimal> result1 = layer.GetOnsets(GrooveRoles.Kick);
            IReadOnlyList<decimal> result2 = layer.GetOnsets(GrooveRoles.Kick);

            Assert.Same(result1, result2);
        }

        #endregion

        #region GetActiveRoles Tests

        [Fact]
        public void GetActiveRoles_NoOnsets_ReturnsEmptySet()
        {
            GrooveInstanceLayer layer = new();

            IReadOnlySet<string> result = layer.GetActiveRoles();

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GetActiveRoles_KickOnly_ReturnsKick()
        {
            GrooveInstanceLayer layer = new()
            {
                KickOnsets = new List<decimal> { 1.0m }
            };

            IReadOnlySet<string> result = layer.GetActiveRoles();

            Assert.Single(result);
            Assert.Contains(GrooveRoles.Kick, result);
        }

        [Fact]
        public void GetActiveRoles_MultipleRoles_ReturnsAllActive()
        {
            GrooveInstanceLayer layer = new()
            {
                KickOnsets = new List<decimal> { 1.0m },
                SnareOnsets = new List<decimal> { 2.0m },
                BassOnsets = new List<decimal> { 1.0m }
            };

            IReadOnlySet<string> result = layer.GetActiveRoles();

            Assert.Equal(3, result.Count);
            Assert.Contains(GrooveRoles.Kick, result);
            Assert.Contains(GrooveRoles.Snare, result);
            Assert.Contains(GrooveRoles.Bass, result);
        }

        [Fact]
        public void GetActiveRoles_HatOnsets_ReturnsBothHatRoles()
        {
            GrooveInstanceLayer layer = new()
            {
                HatOnsets = new List<decimal> { 1.0m, 1.5m }
            };

            IReadOnlySet<string> result = layer.GetActiveRoles();

            Assert.Equal(2, result.Count);
            Assert.Contains(GrooveRoles.ClosedHat, result);
            Assert.Contains(GrooveRoles.OpenHat, result);
        }

        [Fact]
        public void GetActiveRoles_AllRoles_ReturnsAllActive()
        {
            GrooveInstanceLayer layer = new()
            {
                KickOnsets = new List<decimal> { 1.0m },
                SnareOnsets = new List<decimal> { 2.0m },
                HatOnsets = new List<decimal> { 1.0m },
                BassOnsets = new List<decimal> { 1.0m },
                CompOnsets = new List<decimal> { 1.0m },
                PadsOnsets = new List<decimal> { 1.0m }
            };

            IReadOnlySet<string> result = layer.GetActiveRoles();

            Assert.Equal(7, result.Count);
            Assert.Contains(GrooveRoles.Kick, result);
            Assert.Contains(GrooveRoles.Snare, result);
            Assert.Contains(GrooveRoles.ClosedHat, result);
            Assert.Contains(GrooveRoles.OpenHat, result);
            Assert.Contains(GrooveRoles.Bass, result);
            Assert.Contains(GrooveRoles.Comp, result);
            Assert.Contains(GrooveRoles.Pads, result);
        }

        [Fact]
        public void GetActiveRoles_CalledTwice_ReturnsDifferentInstances()
        {
            GrooveInstanceLayer layer = new()
            {
                KickOnsets = new List<decimal> { 1.0m }
            };

            IReadOnlySet<string> result1 = layer.GetActiveRoles();
            IReadOnlySet<string> result2 = layer.GetActiveRoles();

            Assert.NotSame(result1, result2);
            Assert.Equal(result1, result2);
        }

        #endregion

        #region HasRole Tests

        [Fact]
        public void HasRole_KickWithOnsets_ReturnsTrue()
        {
            GrooveInstanceLayer layer = new()
            {
                KickOnsets = new List<decimal> { 1.0m }
            };

            bool result = layer.HasRole(GrooveRoles.Kick);

            Assert.True(result);
        }

        [Fact]
        public void HasRole_KickWithoutOnsets_ReturnsFalse()
        {
            GrooveInstanceLayer layer = new();

            bool result = layer.HasRole(GrooveRoles.Kick);

            Assert.False(result);
        }

        [Fact]
        public void HasRole_SnareWithOnsets_ReturnsTrue()
        {
            GrooveInstanceLayer layer = new()
            {
                SnareOnsets = new List<decimal> { 2.0m }
            };

            bool result = layer.HasRole(GrooveRoles.Snare);

            Assert.True(result);
        }

        [Fact]
        public void HasRole_ClosedHatWithHatOnsets_ReturnsTrue()
        {
            GrooveInstanceLayer layer = new()
            {
                HatOnsets = new List<decimal> { 1.0m }
            };

            bool result = layer.HasRole(GrooveRoles.ClosedHat);

            Assert.True(result);
        }

        [Fact]
        public void HasRole_OpenHatWithHatOnsets_ReturnsTrue()
        {
            GrooveInstanceLayer layer = new()
            {
                HatOnsets = new List<decimal> { 1.0m }
            };

            bool result = layer.HasRole(GrooveRoles.OpenHat);

            Assert.True(result);
        }

        [Fact]
        public void HasRole_BassWithOnsets_ReturnsTrue()
        {
            GrooveInstanceLayer layer = new()
            {
                BassOnsets = new List<decimal> { 1.0m }
            };

            bool result = layer.HasRole(GrooveRoles.Bass);

            Assert.True(result);
        }

        [Fact]
        public void HasRole_CompWithOnsets_ReturnsTrue()
        {
            GrooveInstanceLayer layer = new()
            {
                CompOnsets = new List<decimal> { 1.0m }
            };

            bool result = layer.HasRole(GrooveRoles.Comp);

            Assert.True(result);
        }

        [Fact]
        public void HasRole_PadsWithOnsets_ReturnsTrue()
        {
            GrooveInstanceLayer layer = new()
            {
                PadsOnsets = new List<decimal> { 1.0m }
            };

            bool result = layer.HasRole(GrooveRoles.Pads);

            Assert.True(result);
        }

        [Fact]
        public void HasRole_UnknownRole_ReturnsFalse()
        {
            GrooveInstanceLayer layer = new()
            {
                KickOnsets = new List<decimal> { 1.0m }
            };

            bool result = layer.HasRole("UnknownRole");

            Assert.False(result);
        }

        [Fact]
        public void HasRole_NullRole_ThrowsArgumentNullException()
        {
            GrooveInstanceLayer layer = new();

            Assert.Throws<ArgumentNullException>(() => layer.HasRole(null!));
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void QueryMethods_ConsistentResults_WhenOnsetsPresent()
        {
            GrooveInstanceLayer layer = new()
            {
                KickOnsets = new List<decimal> { 1.0m, 3.0m },
                SnareOnsets = new List<decimal> { 2.0m, 4.0m }
            };

            IReadOnlySet<string> activeRoles = layer.GetActiveRoles();
            IReadOnlyList<decimal> kickOnsets = layer.GetOnsets(GrooveRoles.Kick);
            bool hasKick = layer.HasRole(GrooveRoles.Kick);
            bool hasBass = layer.HasRole(GrooveRoles.Bass);

            Assert.Contains(GrooveRoles.Kick, activeRoles);
            Assert.Contains(GrooveRoles.Snare, activeRoles);
            Assert.Equal(2, kickOnsets.Count);
            Assert.True(hasKick);
            Assert.False(hasBass);
        }

        [Fact]
        public void QueryMethods_ConsistentResults_WhenOnsetsEmpty()
        {
            GrooveInstanceLayer layer = new();

            IReadOnlySet<string> activeRoles = layer.GetActiveRoles();
            IReadOnlyList<decimal> kickOnsets = layer.GetOnsets(GrooveRoles.Kick);
            bool hasKick = layer.HasRole(GrooveRoles.Kick);

            Assert.Empty(activeRoles);
            Assert.Empty(kickOnsets);
            Assert.False(hasKick);
        }

        #endregion
    }
}
