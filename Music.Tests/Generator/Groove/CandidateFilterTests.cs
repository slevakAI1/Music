// AI: purpose=Unit tests for Story B2 candidate filtering (GrooveCandidateFilter).
// AI: deps=xunit for test framework; Music.Generator for types under test.
// AI: change=Story B2 acceptance criteria: test tag resolution, group/candidate filtering, empty-tag semantics, determinism.

using Music.Generator.Agents.Drums;
using Music.Generator.Groove;
using Xunit;

namespace Music.Generator.Tests
{
    /// <summary>
    /// Story B2: Tests for candidate filtering by tags.
    /// Verifies tag resolution, group filtering, candidate filtering, and "match all" semantics.
    /// </summary>
    public class CandidateFilterTests
    {
        #region Tag Resolution Tests

        [Fact]
        public void ResolveEnabledTags_PolicyOverride_TakesPrecedence()
        {
            // Arrange
            var segmentProfile = new SegmentGrooveProfile
            {
                EnabledVariationTags = new List<string> { "Drive", "Ghost" }
            };
            var phraseHookPolicy = new GroovePhraseHookPolicy
            {
                EnabledFillTags = new List<string> { "Fill", "Pickup" }
            };
            var policyDecision = new GroovePolicyDecision
            {
                EnabledVariationTagsOverride = new List<string> { "Override1", "Override2" }
            };

            // Act
            var result = GrooveCandidateFilter.ResolveEnabledTags(
                segmentProfile, phraseHookPolicy, policyDecision, isInFillWindow: true);

            // Assert - Policy override takes precedence, ignoring segment and phrase hook tags
            Assert.Equal(2, result.Count);
            Assert.Contains("Override1", result);
            Assert.Contains("Override2", result);
            Assert.DoesNotContain("Drive", result);
            Assert.DoesNotContain("Fill", result);
        }

        [Fact]
        public void ResolveEnabledTags_NoOverride_CombinesSegmentAndPhraseHook()
        {
            // Arrange
            var segmentProfile = new SegmentGrooveProfile
            {
                EnabledVariationTags = new List<string> { "Drive", "Ghost" }
            };
            var phraseHookPolicy = new GroovePhraseHookPolicy
            {
                EnabledFillTags = new List<string> { "Fill", "Pickup" }
            };

            // Act
            var result = GrooveCandidateFilter.ResolveEnabledTags(
                segmentProfile, phraseHookPolicy, policyDecision: null, isInFillWindow: true);

            // Assert - Union of segment + phrase hook (when in fill window)
            Assert.Equal(4, result.Count);
            Assert.Contains("Drive", result);
            Assert.Contains("Ghost", result);
            Assert.Contains("Fill", result);
            Assert.Contains("Pickup", result);
        }

        [Fact]
        public void ResolveEnabledTags_NotInFillWindow_ExcludesFillTags()
        {
            // Arrange
            var segmentProfile = new SegmentGrooveProfile
            {
                EnabledVariationTags = new List<string> { "Drive" }
            };
            var phraseHookPolicy = new GroovePhraseHookPolicy
            {
                EnabledFillTags = new List<string> { "Fill", "Pickup" }
            };

            // Act
            var result = GrooveCandidateFilter.ResolveEnabledTags(
                segmentProfile, phraseHookPolicy, policyDecision: null, isInFillWindow: false);

            // Assert - Only segment tags, fill tags excluded when not in fill window
            Assert.Single(result);
            Assert.Contains("Drive", result);
            Assert.DoesNotContain("Fill", result);
        }

        [Fact]
        public void ResolveEnabledTags_NullInputs_ReturnsEmptySet()
        {
            // Act
            var result = GrooveCandidateFilter.ResolveEnabledTags(
                segmentProfile: null, phraseHookPolicy: null, policyDecision: null);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ResolveEnabledTags_DuplicateTags_Deduplicates()
        {
            // Arrange
            var segmentProfile = new SegmentGrooveProfile
            {
                EnabledVariationTags = new List<string> { "Fill", "Drive" }
            };
            var phraseHookPolicy = new GroovePhraseHookPolicy
            {
                EnabledFillTags = new List<string> { "Fill", "Pickup" } // "Fill" is duplicate
            };

            // Act
            var result = GrooveCandidateFilter.ResolveEnabledTags(
                segmentProfile, phraseHookPolicy, policyDecision: null, isInFillWindow: true);

            // Assert - Duplicates removed
            Assert.Equal(3, result.Count);
            Assert.Contains("Drive", result);
            Assert.Contains("Fill", result);
            Assert.Contains("Pickup", result);
        }

        #endregion

        #region Group Filtering Tests

        [Fact]
        public void FilterGroups_GroupWithMatchingTag_Included()
        {
            // Arrange
            var groups = new List<DrumCandidateGroup>
            {
                CreateGroup("Group1", new[] { "Fill", "Drive" }),
                CreateGroup("Group2", new[] { "Ghost" })
            };
            var enabledTags = new HashSet<string> { "Drive" };

            // Act
            var result = GrooveCandidateFilter.FilterGroups(groups, enabledTags);

            // Assert
            Assert.Single(result);
            Assert.Equal("Group1", result[0].GroupId);
        }

        [Fact]
        public void FilterGroups_GroupWithNoMatchingTag_Excluded()
        {
            // Arrange
            var groups = new List<DrumCandidateGroup>
            {
                CreateGroup("Group1", new[] { "Fill" }),
                CreateGroup("Group2", new[] { "Ghost" })
            };
            var enabledTags = new HashSet<string> { "Drive" };

            // Act
            var result = GrooveCandidateFilter.FilterGroups(groups, enabledTags);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void FilterGroups_EmptyGroupTags_AlwaysMatches()
        {
            // Arrange - Story B2: "Treat empty/null GroupTags as match all"
            var groups = new List<DrumCandidateGroup>
            {
                CreateGroup("Group1", Array.Empty<string>()), // Empty tags
                CreateGroup("Group2", new[] { "Fill" })
            };
            var enabledTags = new HashSet<string> { "Drive" };

            // Act
            var result = GrooveCandidateFilter.FilterGroups(groups, enabledTags);

            // Assert - Group1 matches (empty = match all), Group2 excluded
            Assert.Single(result);
            Assert.Equal("Group1", result[0].GroupId);
        }

        [Fact]
        public void FilterGroups_NullGroupTags_AlwaysMatches()
        {
            // Arrange
            var group = new DrumCandidateGroup { GroupId = "Group1", GroupTags = null! };
            var groups = new List<DrumCandidateGroup> { group };
            var enabledTags = new HashSet<string> { "Drive" };

            // Act
            var result = GrooveCandidateFilter.FilterGroups(groups, enabledTags);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public void FilterGroups_AnyTagIntersection_Matches()
        {
            // Arrange - Group has multiple tags, only one needs to match
            var groups = new List<DrumCandidateGroup>
            {
                CreateGroup("Group1", new[] { "Fill", "Pickup", "Drive" })
            };
            var enabledTags = new HashSet<string> { "Pickup" };

            // Act
            var result = GrooveCandidateFilter.FilterGroups(groups, enabledTags);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public void FilterGroups_PreservesInputOrder()
        {
            // Arrange
            var groups = new List<DrumCandidateGroup>
            {
                CreateGroup("GroupZ", Array.Empty<string>()),
                CreateGroup("GroupA", Array.Empty<string>()),
                CreateGroup("GroupM", Array.Empty<string>())
            };
            var enabledTags = new HashSet<string>();

            // Act
            var result = GrooveCandidateFilter.FilterGroups(groups, enabledTags);

            // Assert - Order preserved (not sorted alphabetically)
            Assert.Equal(3, result.Count);
            Assert.Equal("GroupZ", result[0].GroupId);
            Assert.Equal("GroupA", result[1].GroupId);
            Assert.Equal("GroupM", result[2].GroupId);
        }

        #endregion

        #region Candidate Filtering Tests

        [Fact]
        public void FilterCandidates_CandidateWithMatchingTag_Included()
        {
            // Arrange
            var candidates = new List<DrumOnsetCandidate>
            {
                CreateCandidate(1.0m, new[] { "Fill" }),
                CreateCandidate(2.0m, new[] { "Ghost" })
            };
            var enabledTags = new HashSet<string> { "Fill" };

            // Act
            var result = GrooveCandidateFilter.FilterCandidates(candidates, enabledTags);

            // Assert
            Assert.Single(result);
            Assert.Equal(1.0m, result[0].OnsetBeat);
        }

        [Fact]
        public void FilterCandidates_EmptyCandidateTags_AlwaysMatches()
        {
            // Arrange - Story B2: "Treat empty/null Candidate.Tags as match all"
            var candidates = new List<DrumOnsetCandidate>
            {
                CreateCandidate(1.0m, Array.Empty<string>()), // Empty tags
                CreateCandidate(2.0m, new[] { "Ghost" })
            };
            var enabledTags = new HashSet<string> { "Drive" };

            // Act
            var result = GrooveCandidateFilter.FilterCandidates(candidates, enabledTags);

            // Assert - First candidate matches (empty = match all)
            Assert.Single(result);
            Assert.Equal(1.0m, result[0].OnsetBeat);
        }

        [Fact]
        public void FilterCandidates_NullCandidateTags_AlwaysMatches()
        {
            // Arrange
            var candidate = new DrumOnsetCandidate { OnsetBeat = 1.0m, Tags = null! };
            var candidates = new List<DrumOnsetCandidate> { candidate };
            var enabledTags = new HashSet<string> { "Drive" };

            // Act
            var result = GrooveCandidateFilter.FilterCandidates(candidates, enabledTags);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public void FilterCandidates_PreservesInputOrder()
        {
            // Arrange
            var candidates = new List<DrumOnsetCandidate>
            {
                CreateCandidate(3.0m, Array.Empty<string>()),
                CreateCandidate(1.0m, Array.Empty<string>()),
                CreateCandidate(2.0m, Array.Empty<string>())
            };
            var enabledTags = new HashSet<string>();

            // Act
            var result = GrooveCandidateFilter.FilterCandidates(candidates, enabledTags);

            // Assert - Order preserved
            Assert.Equal(3, result.Count);
            Assert.Equal(3.0m, result[0].OnsetBeat);
            Assert.Equal(1.0m, result[1].OnsetBeat);
            Assert.Equal(2.0m, result[2].OnsetBeat);
        }

        #endregion

        #region Combined Group + Candidate Filtering Tests

        [Fact]
        public void FilterGroupsAndCandidates_FiltersAtBothLevels()
        {
            // Arrange
            var groups = new List<DrumCandidateGroup>
            {
                new DrumCandidateGroup
                {
                    GroupId = "FillGroup",
                    GroupTags = new List<string> { "Fill" },
                    Candidates = new List<DrumOnsetCandidate>
                    {
                        CreateCandidate(1.0m, new[] { "Fill" }),
                        CreateCandidate(2.0m, new[] { "Pickup" }), // Won't match
                        CreateCandidate(3.0m, Array.Empty<string>()) // Empty = match all
                    }
                }
            };
            var enabledTags = new HashSet<string> { "Fill" };

            // Act
            var result = GrooveCandidateFilter.FilterGroupsAndCandidates(groups, enabledTags);

            // Assert
            Assert.Single(result);
            Assert.Equal(2, result[0].Candidates.Count); // Beat 1.0 and 3.0
            Assert.Contains(result[0].Candidates, c => c.OnsetBeat == 1.0m);
            Assert.Contains(result[0].Candidates, c => c.OnsetBeat == 3.0m);
            Assert.DoesNotContain(result[0].Candidates, c => c.OnsetBeat == 2.0m);
        }

        [Fact]
        public void FilterGroupsAndCandidates_ExcludesGroupsWithNoCandidatesAfterFiltering()
        {
            // Arrange
            var groups = new List<DrumCandidateGroup>
            {
                new DrumCandidateGroup
                {
                    GroupId = "Group1",
                    GroupTags = new List<string>(), // Empty = match all
                    Candidates = new List<DrumOnsetCandidate>
                    {
                        CreateCandidate(1.0m, new[] { "Ghost" }) // Won't match
                    }
                }
            };
            var enabledTags = new HashSet<string> { "Fill" };

            // Act
            var result = GrooveCandidateFilter.FilterGroupsAndCandidates(groups, enabledTags);

            // Assert - Group excluded because no candidates remain after filtering
            Assert.Empty(result);
        }

        #endregion

        #region Deterministic Filtering Tests

        [Fact]
        public void FilterGroups_IsDeterministic_MultipleRuns()
        {
            // Arrange
            var groups = new List<DrumCandidateGroup>
            {
                CreateGroup("GroupA", new[] { "Fill" }),
                CreateGroup("GroupB", new[] { "Drive" }),
                CreateGroup("GroupC", new[] { "Fill", "Drive" }),
                CreateGroup("GroupD", Array.Empty<string>())
            };
            var enabledTags = new HashSet<string> { "Fill" };

            // Act - Run multiple times
            var result1 = GrooveCandidateFilter.FilterGroups(groups, enabledTags);
            var result2 = GrooveCandidateFilter.FilterGroups(groups, enabledTags);
            var result3 = GrooveCandidateFilter.FilterGroups(groups, enabledTags);

            // Assert - All runs produce identical results
            Assert.Equal(result1.Select(g => g.GroupId), result2.Select(g => g.GroupId));
            Assert.Equal(result2.Select(g => g.GroupId), result3.Select(g => g.GroupId));
        }

        [Fact]
        public void FilteringAcrossMultipleBars_DeterministicWithDifferentTags()
        {
            // Arrange - Simulate filtering across multiple bars with different segment tags
            var groups = new List<DrumCandidateGroup>
            {
                CreateGroup("BaseGroup", Array.Empty<string>()),
                CreateGroup("FillGroup", new[] { "Fill" }),
                CreateGroup("DriveGroup", new[] { "Drive" }),
                CreateGroup("GhostGroup", new[] { "Ghost" })
            };

            // Simulate different bars with different enabled tags
            var bar1Tags = new HashSet<string> { "Drive" };
            var bar2Tags = new HashSet<string> { "Fill", "Drive" };
            var bar3Tags = new HashSet<string>(); // Empty - only base groups

            // Act
            var bar1Result = GrooveCandidateFilter.FilterGroups(groups, bar1Tags);
            var bar2Result = GrooveCandidateFilter.FilterGroups(groups, bar2Tags);
            var bar3Result = GrooveCandidateFilter.FilterGroups(groups, bar3Tags);

            // Assert
            Assert.Equal(2, bar1Result.Count); // BaseGroup + DriveGroup
            Assert.Contains(bar1Result, g => g.GroupId == "BaseGroup");
            Assert.Contains(bar1Result, g => g.GroupId == "DriveGroup");

            Assert.Equal(3, bar2Result.Count); // BaseGroup + FillGroup + DriveGroup
            Assert.Contains(bar2Result, g => g.GroupId == "BaseGroup");
            Assert.Contains(bar2Result, g => g.GroupId == "FillGroup");
            Assert.Contains(bar2Result, g => g.GroupId == "DriveGroup");

            Assert.Single(bar3Result); // Only BaseGroup (empty tags = match all)
            Assert.Equal("BaseGroup", bar3Result[0].GroupId);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void FilterGroups_EmptyGroupsList_ReturnsEmpty()
        {
            // Arrange
            var groups = new List<DrumCandidateGroup>();
            var enabledTags = new HashSet<string> { "Fill" };

            // Act
            var result = GrooveCandidateFilter.FilterGroups(groups, enabledTags);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void FilterGroups_EmptyEnabledTags_OnlyMatchesEmptyTagGroups()
        {
            // Arrange
            var groups = new List<DrumCandidateGroup>
            {
                CreateGroup("Group1", Array.Empty<string>()), // Empty = match all
                CreateGroup("Group2", new[] { "Fill" }) // Has tags, won't match empty enabledTags
            };
            var enabledTags = new HashSet<string>();

            // Act
            var result = GrooveCandidateFilter.FilterGroups(groups, enabledTags);

            // Assert - Only empty-tag group matches
            Assert.Single(result);
            Assert.Equal("Group1", result[0].GroupId);
        }

        [Fact]
        public void FilterGroups_NullGroups_ThrowsArgumentNullException()
        {
            // Arrange
            var enabledTags = new HashSet<string>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                GrooveCandidateFilter.FilterGroups(null!, enabledTags));
        }

        [Fact]
        public void FilterGroups_NullEnabledTags_ThrowsArgumentNullException()
        {
            // Arrange
            var groups = new List<DrumCandidateGroup>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                GrooveCandidateFilter.FilterGroups(groups, null!));
        }

        #endregion

        #region Test Helpers

        private static DrumCandidateGroup CreateGroup(string groupId, string[] tags)
        {
            return new DrumCandidateGroup
            {
                GroupId = groupId,
                GroupTags = tags.ToList(),
                Candidates = new List<DrumOnsetCandidate>()
            };
        }

        private static DrumOnsetCandidate CreateCandidate(decimal beat, string[] tags)
        {
            return new DrumOnsetCandidate
            {
                OnsetBeat = beat,
                Tags = tags.ToList()
            };
        }

        #endregion
    }
}


