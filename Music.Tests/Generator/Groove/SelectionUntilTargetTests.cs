// AI: purpose=Unit tests for Story C2 selection until target reached (GrooveSelectionEngine).
// AI: deps=xunit for test framework; Music.Generator for types under test.
// AI: change=Story C2 acceptance criteria: test target enforcement, pool exhaustion, anchors, caps, determinism.

using Xunit;

namespace Music.Generator.Tests
{
    /// <summary>
    /// Story C2: Tests for selection until target reached with pool exhaustion safety.
    /// Verifies target count enforcement, anchor preservation, cap enforcement, and determinism.
    /// </summary>
    public class SelectionUntilTargetTests
    {
        public SelectionUntilTargetTests()
        {
            // Ensure RNG is initialized for all tests
            Rng.Initialize(12345);
        }

        #region Small Pool + High Density Tests

        [Fact]
        public void SelectUntilTargetReached_SmallPoolHighDensity_SelectsAll()
        {
            // Arrange - Story C2: small pool + high density => selects all without error
            var barContext = CreateBarContext();
            var groups = new List<GrooveCandidateGroup>
            {
                CreateGroup("Group1", new[] { 1.0m, 2.0m, 3.0m }) // Only 3 candidates
            };
            var anchors = Array.Empty<GrooveOnset>();

            // Act - Request 10, but only 3 available
            Rng.Initialize(12345);
            var result = GrooveSelectionEngine.SelectUntilTargetReached(
                barContext, "Kick", groups, targetCount: 10, anchors);

            // Assert - Pool exhaustion safety: selects all 3 without error
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void SelectUntilTargetReached_EmptyPool_ReturnsEmpty()
        {
            // Arrange
            var barContext = CreateBarContext();
            var groups = new List<GrooveCandidateGroup>();
            var anchors = Array.Empty<GrooveOnset>();

            // Act
            var result = GrooveSelectionEngine.SelectUntilTargetReached(
                barContext, "Kick", groups, targetCount: 5, anchors);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region Target Count Enforcement Tests

        [Fact]
        public void SelectUntilTargetReached_TargetLessThanPool_SelectsExactly()
        {
            // Arrange
            var barContext = CreateBarContext();
            var groups = new List<GrooveCandidateGroup>
            {
                CreateGroup("Group1", new[] { 1.0m, 2.0m, 3.0m, 4.0m }) // 4 candidates
            };
            var anchors = Array.Empty<GrooveOnset>();

            // Act - Request 2
            Rng.Initialize(12345);
            var result = GrooveSelectionEngine.SelectUntilTargetReached(
                barContext, "Kick", groups, targetCount: 2, anchors);

            // Assert - Story C2: stop when target reached
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void SelectUntilTargetReached_ZeroTarget_ReturnsEmpty()
        {
            // Arrange
            var barContext = CreateBarContext();
            var groups = new List<GrooveCandidateGroup>
            {
                CreateGroup("Group1", new[] { 1.0m, 2.0m })
            };
            var anchors = Array.Empty<GrooveOnset>();

            // Act
            var result = GrooveSelectionEngine.SelectUntilTargetReached(
                barContext, "Kick", groups, targetCount: 0, anchors);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void SelectUntilTargetReached_NegativeTarget_ReturnsEmpty()
        {
            // Arrange
            var barContext = CreateBarContext();
            var groups = new List<GrooveCandidateGroup>
            {
                CreateGroup("Group1", new[] { 1.0m, 2.0m })
            };
            var anchors = Array.Empty<GrooveOnset>();

            // Act
            var result = GrooveSelectionEngine.SelectUntilTargetReached(
                barContext, "Kick", groups, targetCount: -5, anchors);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region Anchor Conflict Tests

        [Fact]
        public void SelectUntilTargetReached_AnchorConflict_ExcludesConflicting()
        {
            // Arrange - Story C2: preserve anchors, selection only adds
            var barContext = CreateBarContext();
            var groups = new List<GrooveCandidateGroup>
            {
                CreateGroup("Group1", new[] { 1.0m, 2.0m, 3.0m })
            };
            var anchors = new List<GrooveOnset>
            {
                CreateAnchor("Kick", 2.0m) // Conflicts with candidate at beat 2
            };

            // Act
            Rng.Initialize(12345);
            var result = GrooveSelectionEngine.SelectUntilTargetReached(
                barContext, "Kick", groups, targetCount: 10, anchors);

            // Assert - Only 2 candidates available (beat 1 and 3), beat 2 excluded
            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(result, c => c.OnsetBeat == 2.0m);
        }

        [Fact]
        public void SelectUntilTargetReached_DifferentRoleAnchor_DoesNotConflict()
        {
            // Arrange - Anchors for different role should not conflict
            var barContext = CreateBarContext();
            var groups = new List<GrooveCandidateGroup>
            {
                CreateGroup("Group1", new[] { 1.0m, 2.0m, 3.0m })
            };
            var anchors = new List<GrooveOnset>
            {
                CreateAnchor("Snare", 2.0m) // Different role
            };

            // Act
            Rng.Initialize(12345);
            var result = GrooveSelectionEngine.SelectUntilTargetReached(
                barContext, "Kick", groups, targetCount: 10, anchors);

            // Assert - All 3 candidates available (anchor is for different role)
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void SelectUntilTargetReached_MultipleAnchors_ExcludesAll()
        {
            // Arrange
            var barContext = CreateBarContext();
            var groups = new List<GrooveCandidateGroup>
            {
                CreateGroup("Group1", new[] { 1.0m, 2.0m, 3.0m, 4.0m })
            };
            var anchors = new List<GrooveOnset>
            {
                CreateAnchor("Kick", 1.0m),
                CreateAnchor("Kick", 3.0m)
            };

            // Act
            Rng.Initialize(12345);
            var result = GrooveSelectionEngine.SelectUntilTargetReached(
                barContext, "Kick", groups, targetCount: 10, anchors);

            // Assert - Only beats 2 and 4 available
            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(result, c => c.OnsetBeat == 1.0m);
            Assert.DoesNotContain(result, c => c.OnsetBeat == 3.0m);
        }

        #endregion

        #region Per-Candidate Cap Tests

        [Fact]
        public void SelectUntilTargetReached_CandidateMaxAddsPerBar_Enforced()
        {
            // Arrange - Candidate with MaxAddsPerBar = 1
            var barContext = CreateBarContext();
            var group = new GrooveCandidateGroup
            {
                GroupId = "Group1",
                BaseProbabilityBias = 1.0,
                MaxAddsPerBar = 0, // Unlimited group
                Candidates = new List<GrooveOnsetCandidate>
                {
                    new GrooveOnsetCandidate
                    {
                        OnsetBeat = 1.0m,
                        ProbabilityBias = 1.0,
                        MaxAddsPerBar = 1 // Can only be added once
                    }
                }
            };
            var groups = new List<GrooveCandidateGroup> { group };
            var anchors = Array.Empty<GrooveOnset>();

            // Act - Request 5, but candidate only allows 1
            Rng.Initialize(12345);
            var result = GrooveSelectionEngine.SelectUntilTargetReached(
                barContext, "Kick", groups, targetCount: 5, anchors);

            // Assert - Only 1 selected (candidate cap enforced)
            Assert.Single(result);
        }

        [Fact]
        public void SelectUntilTargetReached_CandidateMaxAddsZero_IsUnlimited()
        {
            // Arrange - MaxAddsPerBar = 0 means unlimited
            var barContext = CreateBarContext();
            var group = new GrooveCandidateGroup
            {
                GroupId = "Group1",
                BaseProbabilityBias = 1.0,
                Candidates = new List<GrooveOnsetCandidate>
                {
                    new GrooveOnsetCandidate
                    {
                        OnsetBeat = 1.0m,
                        ProbabilityBias = 1.0,
                        MaxAddsPerBar = 0 // Unlimited
                    },
                    new GrooveOnsetCandidate
                    {
                        OnsetBeat = 2.0m,
                        ProbabilityBias = 1.0,
                        MaxAddsPerBar = 0 // Unlimited
                    }
                }
            };
            var groups = new List<GrooveCandidateGroup> { group };
            var anchors = Array.Empty<GrooveOnset>();

            // Act - Request 2, both unlimited
            Rng.Initialize(12345);
            var result = GrooveSelectionEngine.SelectUntilTargetReached(
                barContext, "Kick", groups, targetCount: 2, anchors);

            // Assert - Both selected (unlimited)
            Assert.Equal(2, result.Count);
        }

        #endregion

        #region Per-Group Cap Tests

        [Fact]
        public void SelectUntilTargetReached_GroupMaxAddsPerBar_Enforced()
        {
            // Arrange - Group with MaxAddsPerBar = 2
            var barContext = CreateBarContext();
            var group = new GrooveCandidateGroup
            {
                GroupId = "Group1",
                BaseProbabilityBias = 1.0,
                MaxAddsPerBar = 2, // Can only add 2 from this group
                Candidates = new List<GrooveOnsetCandidate>
                {
                    CreateCandidate(1.0m),
                    CreateCandidate(2.0m),
                    CreateCandidate(3.0m),
                    CreateCandidate(4.0m)
                }
            };
            var groups = new List<GrooveCandidateGroup> { group };
            var anchors = Array.Empty<GrooveOnset>();

            // Act - Request 10, but group only allows 2
            Rng.Initialize(12345);
            var result = GrooveSelectionEngine.SelectUntilTargetReached(
                barContext, "Kick", groups, targetCount: 10, anchors);

            // Assert - Only 2 selected (group cap enforced)
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void SelectUntilTargetReached_MultipleGroups_EachCapEnforced()
        {
            // Arrange
            var barContext = CreateBarContext();
            var groups = new List<GrooveCandidateGroup>
            {
                new GrooveCandidateGroup
                {
                    GroupId = "Group1",
                    BaseProbabilityBias = 1.0,
                    MaxAddsPerBar = 1,
                    Candidates = new List<GrooveOnsetCandidate>
                    {
                        CreateCandidate(1.0m),
                        CreateCandidate(2.0m)
                    }
                },
                new GrooveCandidateGroup
                {
                    GroupId = "Group2",
                    BaseProbabilityBias = 1.0,
                    MaxAddsPerBar = 1,
                    Candidates = new List<GrooveOnsetCandidate>
                    {
                        CreateCandidate(3.0m),
                        CreateCandidate(4.0m)
                    }
                }
            };
            var anchors = Array.Empty<GrooveOnset>();

            // Act - Request 10, but each group only allows 1
            Rng.Initialize(12345);
            var result = GrooveSelectionEngine.SelectUntilTargetReached(
                barContext, "Kick", groups, targetCount: 10, anchors);

            // Assert - Only 2 selected total (1 from each group)
            Assert.Equal(2, result.Count);
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void SelectUntilTargetReached_SameSeed_IdenticalSelections()
        {
            // Arrange
            var barContext = CreateBarContext();
            var groups = CreateTestGroups();
            var anchors = Array.Empty<GrooveOnset>();

            // Act - Run multiple times with same seed
            Rng.Initialize(12345);
            var result1 = GrooveSelectionEngine.SelectUntilTargetReached(
                barContext, "Kick", groups, targetCount: 3, anchors);

            Rng.Initialize(12345);
            var result2 = GrooveSelectionEngine.SelectUntilTargetReached(
                barContext, "Kick", groups, targetCount: 3, anchors);

            Rng.Initialize(12345);
            var result3 = GrooveSelectionEngine.SelectUntilTargetReached(
                barContext, "Kick", groups, targetCount: 3, anchors);

            // Assert - All runs produce identical results
            Assert.Equal(result1.Select(c => c.OnsetBeat), result2.Select(c => c.OnsetBeat));
            Assert.Equal(result2.Select(c => c.OnsetBeat), result3.Select(c => c.OnsetBeat));
        }

        [Fact]
        public void SelectUntilTargetReached_DifferentSeed_MayDifferResults()
        {
            // Arrange
            var barContext = CreateBarContext();
            var groups = CreateTestGroupsWithManyOptions();
            var anchors = Array.Empty<GrooveOnset>();

            // Act
            Rng.Initialize(12345);
            var result1 = GrooveSelectionEngine.SelectUntilTargetReached(
                barContext, "Kick", groups, targetCount: 2, anchors);

            Rng.Initialize(99999);
            var result2 = GrooveSelectionEngine.SelectUntilTargetReached(
                barContext, "Kick", groups, targetCount: 2, anchors);

            // Assert - Both return valid results
            Assert.Equal(2, result1.Count);
            Assert.Equal(2, result2.Count);

            // Verify determinism within each seed
            Rng.Initialize(12345);
            var result1Again = GrooveSelectionEngine.SelectUntilTargetReached(
                barContext, "Kick", groups, targetCount: 2, anchors);
            Assert.Equal(result1.Select(c => c.OnsetBeat), result1Again.Select(c => c.OnsetBeat));
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void SelectUntilTargetReached_NullBarContext_ThrowsArgumentNullException()
        {
            // Arrange
            var groups = CreateTestGroups();
            var anchors = Array.Empty<GrooveOnset>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                GrooveSelectionEngine.SelectUntilTargetReached(null!, "Kick", groups, 5, anchors));
        }

        [Fact]
        public void SelectUntilTargetReached_NullRole_ThrowsArgumentNullException()
        {
            // Arrange
            var barContext = CreateBarContext();
            var groups = CreateTestGroups();
            var anchors = Array.Empty<GrooveOnset>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                GrooveSelectionEngine.SelectUntilTargetReached(barContext, null!, groups, 5, anchors));
        }

        [Fact]
        public void SelectUntilTargetReached_NullGroups_ThrowsArgumentNullException()
        {
            // Arrange
            var barContext = CreateBarContext();
            var anchors = Array.Empty<GrooveOnset>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                GrooveSelectionEngine.SelectUntilTargetReached(barContext, "Kick", null!, 5, anchors));
        }

        [Fact]
        public void SelectUntilTargetReached_NullAnchors_ThrowsArgumentNullException()
        {
            // Arrange
            var barContext = CreateBarContext();
            var groups = CreateTestGroups();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                GrooveSelectionEngine.SelectUntilTargetReached(barContext, "Kick", groups, 5, null!));
        }

        #endregion

        #region Test Helpers

        private static GrooveBarContext CreateBarContext()
        {
            return new GrooveBarContext(
                BarNumber: 1,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 1,
                BarsUntilSectionEnd: 4);
        }

        private static GrooveCandidateGroup CreateGroup(string groupId, decimal[] beats)
        {
            return new GrooveCandidateGroup
            {
                GroupId = groupId,
                BaseProbabilityBias = 1.0,
                MaxAddsPerBar = 0, // Unlimited by default
                Candidates = beats.Select(b => CreateCandidate(b)).ToList()
            };
        }

        private static GrooveOnsetCandidate CreateCandidate(decimal beat)
        {
            return new GrooveOnsetCandidate
            {
                OnsetBeat = beat,
                ProbabilityBias = 1.0,
                MaxAddsPerBar = 0 // Unlimited by default
            };
        }

        private static GrooveOnset CreateAnchor(string role, decimal beat)
        {
            return new GrooveOnset
            {
                Role = role,
                BarNumber = 1,
                Beat = beat,
                Strength = null,
                Velocity = null,
                TimingOffsetTicks = null,
                Provenance = null,
                IsMustHit = true,
                IsNeverRemove = true,
                IsProtected = true
            };
        }

        private static List<GrooveCandidateGroup> CreateTestGroups()
        {
            return new List<GrooveCandidateGroup>
            {
                new GrooveCandidateGroup
                {
                    GroupId = "TestGroup",
                    BaseProbabilityBias = 1.0,
                    MaxAddsPerBar = 0,
                    Candidates = new List<GrooveOnsetCandidate>
                    {
                        CreateCandidate(1.0m),
                        CreateCandidate(2.0m),
                        CreateCandidate(3.0m)
                    }
                }
            };
        }

        private static List<GrooveCandidateGroup> CreateTestGroupsWithManyOptions()
        {
            return new List<GrooveCandidateGroup>
            {
                new GrooveCandidateGroup
                {
                    GroupId = "GroupA",
                    BaseProbabilityBias = 1.0,
                    Candidates = new List<GrooveOnsetCandidate>
                    {
                        CreateCandidate(1.0m),
                        CreateCandidate(1.5m),
                        CreateCandidate(2.0m),
                        CreateCandidate(2.5m)
                    }
                },
                new GrooveCandidateGroup
                {
                    GroupId = "GroupB",
                    BaseProbabilityBias = 1.0,
                    Candidates = new List<GrooveOnsetCandidate>
                    {
                        CreateCandidate(3.0m),
                        CreateCandidate(3.5m),
                        CreateCandidate(4.0m)
                    }
                }
            };
        }

        #endregion
    }
}
