namespace Music.Tests.Generator.Groove
{
    using Music.Generator.Groove;

    // AI: purpose=Unit tests for GrooveAnchorFactory anchor pattern retrieval and genre query methods.
    // AI: invariants=Tests verify determinism, null-safety, error handling, and correct anchor data structure.
    public sealed class GrooveAnchorFactoryTests
    {
        #region GetAnchor Tests

        [Fact]
        public void GetAnchor_PopRock_ReturnsValidAnchorPattern()
        {
            GrooveInstanceLayer anchor = GrooveAnchorFactory.GetAnchor("PopRock");

            Assert.NotNull(anchor);
            Assert.NotNull(anchor.KickOnsets);
            Assert.NotNull(anchor.SnareOnsets);
            Assert.NotNull(anchor.HatOnsets);
            Assert.NotNull(anchor.BassOnsets);
            Assert.NotNull(anchor.CompOnsets);
            Assert.NotNull(anchor.PadsOnsets);
        }

        [Fact]
        public void GetAnchor_PopRock_HasExpectedKickPattern()
        {
            GrooveInstanceLayer anchor = GrooveAnchorFactory.GetAnchor("PopRock");

            Assert.Equal(2, anchor.KickOnsets.Count);
            Assert.Contains(1m, anchor.KickOnsets);
            Assert.Contains(3m, anchor.KickOnsets);
        }

        [Fact]
        public void GetAnchor_PopRock_HasExpectedSnareBackbeat()
        {
            GrooveInstanceLayer anchor = GrooveAnchorFactory.GetAnchor("PopRock");

            Assert.Equal(2, anchor.SnareOnsets.Count);
            Assert.Contains(2m, anchor.SnareOnsets);
            Assert.Contains(4m, anchor.SnareOnsets);
        }

        [Fact]
        public void GetAnchor_PopRock_HasExpectedHatPattern()
        {
            GrooveInstanceLayer anchor = GrooveAnchorFactory.GetAnchor("PopRock");

            Assert.Equal(8, anchor.HatOnsets.Count);
            decimal[] expectedHats = { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m };
            foreach (decimal beat in expectedHats)
            {
                Assert.Contains(beat, anchor.HatOnsets);
            }
        }

        [Fact]
        public void GetAnchor_PopRock_HasExpectedBassPattern()
        {
            GrooveInstanceLayer anchor = GrooveAnchorFactory.GetAnchor("PopRock");

            Assert.Equal(2, anchor.BassOnsets.Count);
            Assert.Contains(1m, anchor.BassOnsets);
            Assert.Contains(3m, anchor.BassOnsets);
        }

        [Fact]
        public void GetAnchor_PopRock_HasExpectedCompPattern()
        {
            GrooveInstanceLayer anchor = GrooveAnchorFactory.GetAnchor("PopRock");

            Assert.Equal(4, anchor.CompOnsets.Count);
            decimal[] expectedComp = { 1.5m, 2.5m, 3.5m, 4.5m };
            foreach (decimal beat in expectedComp)
            {
                Assert.Contains(beat, anchor.CompOnsets);
            }
        }

        [Fact]
        public void GetAnchor_PopRock_HasExpectedPadsPattern()
        {
            GrooveInstanceLayer anchor = GrooveAnchorFactory.GetAnchor("PopRock");

            Assert.Equal(2, anchor.PadsOnsets.Count);
            Assert.Contains(1m, anchor.PadsOnsets);
            Assert.Contains(3m, anchor.PadsOnsets);
        }

        [Fact]
        public void GetAnchor_PopRock_IsDeterministic()
        {
            GrooveInstanceLayer anchor1 = GrooveAnchorFactory.GetAnchor("PopRock");
            GrooveInstanceLayer anchor2 = GrooveAnchorFactory.GetAnchor("PopRock");

            Assert.NotSame(anchor1, anchor2);
            Assert.Equal(anchor1.KickOnsets.Count, anchor2.KickOnsets.Count);
            Assert.Equal(anchor1.SnareOnsets.Count, anchor2.SnareOnsets.Count);
            Assert.Equal(anchor1.HatOnsets.Count, anchor2.HatOnsets.Count);
            Assert.Equal(anchor1.BassOnsets.Count, anchor2.BassOnsets.Count);
            Assert.Equal(anchor1.CompOnsets.Count, anchor2.CompOnsets.Count);
            Assert.Equal(anchor1.PadsOnsets.Count, anchor2.PadsOnsets.Count);
        }

        [Fact]
        public void GetAnchor_UnknownGenre_ThrowsArgumentException()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(() =>
                GrooveAnchorFactory.GetAnchor("UnknownGenre"));

            Assert.Contains("Unknown genre", ex.Message);
            Assert.Contains("UnknownGenre", ex.Message);
        }

        [Fact]
        public void GetAnchor_NullGenre_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                GrooveAnchorFactory.GetAnchor(null!));
        }

        [Fact]
        public void GetAnchor_EmptyString_ThrowsArgumentException()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(() =>
                GrooveAnchorFactory.GetAnchor(""));

            Assert.Contains("Unknown genre", ex.Message);
        }

        [Fact]
        public void GetAnchor_CaseSensitive_ThrowsForWrongCase()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(() =>
                GrooveAnchorFactory.GetAnchor("poprock"));

            Assert.Contains("Unknown genre", ex.Message);
        }

        #endregion

        #region GetAvailableGenres Tests

        [Fact]
        public void GetAvailableGenres_ReturnsNonEmptyList()
        {
            IReadOnlyList<string> genres = GrooveAnchorFactory.GetAvailableGenres();

            Assert.NotNull(genres);
            Assert.NotEmpty(genres);
        }

        [Fact]
        public void GetAvailableGenres_ContainsPopRock()
        {
            IReadOnlyList<string> genres = GrooveAnchorFactory.GetAvailableGenres();

            Assert.Contains("PopRock", genres);
        }

        [Fact]
        public void GetAvailableGenres_AllGenresCanBeRetrieved()
        {
            IReadOnlyList<string> genres = GrooveAnchorFactory.GetAvailableGenres();

            foreach (string genre in genres)
            {
                GrooveInstanceLayer anchor = GrooveAnchorFactory.GetAnchor(genre);
                Assert.NotNull(anchor);
            }
        }

        [Fact]
        public void GetAvailableGenres_IsDeterministic()
        {
            IReadOnlyList<string> genres1 = GrooveAnchorFactory.GetAvailableGenres();
            IReadOnlyList<string> genres2 = GrooveAnchorFactory.GetAvailableGenres();

            Assert.Equal(genres1.Count, genres2.Count);
            Assert.Equal(genres1, genres2);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void Factory_PopRockAnchor_CompatibleWithQueryMethods()
        {
            GrooveInstanceLayer anchor = GrooveAnchorFactory.GetAnchor("PopRock");

            IReadOnlyList<decimal> kickOnsets = anchor.GetOnsets(GrooveRoles.Kick);
            bool hasSnare = anchor.HasRole(GrooveRoles.Snare);
            IReadOnlySet<string> activeRoles = anchor.GetActiveRoles();

            Assert.Equal(2, kickOnsets.Count);
            Assert.True(hasSnare);
            Assert.Contains(GrooveRoles.Kick, activeRoles);
            Assert.Contains(GrooveRoles.Snare, activeRoles);
            Assert.Contains(GrooveRoles.ClosedHat, activeRoles);
            Assert.Contains(GrooveRoles.Bass, activeRoles);
            Assert.Contains(GrooveRoles.Comp, activeRoles);
            Assert.Contains(GrooveRoles.Pads, activeRoles);
        }

        [Fact]
        public void Factory_PopRockAnchor_HasAllRolesActive()
        {
            GrooveInstanceLayer anchor = GrooveAnchorFactory.GetAnchor("PopRock");

            Assert.True(anchor.HasRole(GrooveRoles.Kick));
            Assert.True(anchor.HasRole(GrooveRoles.Snare));
            Assert.True(anchor.HasRole(GrooveRoles.ClosedHat));
            Assert.True(anchor.HasRole(GrooveRoles.Bass));
            Assert.True(anchor.HasRole(GrooveRoles.Comp));
            Assert.True(anchor.HasRole(GrooveRoles.Pads));
        }

        #endregion
    }
}

