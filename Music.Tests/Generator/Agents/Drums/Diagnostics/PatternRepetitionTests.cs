// AI: purpose=Unit tests for Story 7.2b pattern detection and analysis components.
// AI: deps=Tests PatternRepetitionDetector, PatternSimilarityAnalyzer; uses xUnit, FluentAssertions.
// AI: change=Story 7.2b; extend tests when adding pattern analysis features.

using FluentAssertions;
using Music.Generator.Agents.Drums.Diagnostics;
using Xunit;

namespace Music.Tests.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Unit tests for pattern repetition and similarity detection (Story 7.2b).
/// </summary>
public class PatternRepetitionTests
{
    #region PatternRepetitionDetector

    [Fact]
    public void Detect_WithRepeatingPatterns_IdentifiesOccurrencesAndRuns()
    {
        // Arrange - Create fingerprints with repeating pattern
        var fingerprints = new List<BarPatternFingerprint>
        {
            CreateFingerprint(1, "HASH_A"),
            CreateFingerprint(2, "HASH_A"),
            CreateFingerprint(3, "HASH_A"),
            CreateFingerprint(4, "HASH_B"),
            CreateFingerprint(5, "HASH_A"),
            CreateFingerprint(6, "HASH_C"),
            CreateFingerprint(7, "HASH_C"),
            CreateFingerprint(8, "HASH_A")
        };

        // Act
        var result = PatternRepetitionDetector.Detect(fingerprints);

        // Assert
        result.TotalBars.Should().Be(8);
        result.UniquePatternCount.Should().Be(3); // A, B, C

        // HASH_A appears 5 times
        result.PatternOccurrences["HASH_A"].Should().HaveCount(5);
        result.MostCommonPatterns[0].PatternHash.Should().Be("HASH_A");
        result.MostCommonPatterns[0].OccurrenceCount.Should().Be(5);

        // Consecutive runs: bars 1-3 (HASH_A), bars 6-7 (HASH_C)
        result.ConsecutiveRuns.Should().HaveCount(2);
        result.ConsecutiveRuns.Should().Contain(r => r.PatternHash == "HASH_A" && r.Length == 3);
        result.ConsecutiveRuns.Should().Contain(r => r.PatternHash == "HASH_C" && r.Length == 2);
    }

    [Fact]
    public void Detect_EmptyList_ReturnsEmptyResult()
    {
        var result = PatternRepetitionDetector.Detect(new List<BarPatternFingerprint>());

        result.TotalBars.Should().Be(0);
        result.UniquePatternCount.Should().Be(0);
        result.MostCommonPatterns.Should().BeEmpty();
        result.ConsecutiveRuns.Should().BeEmpty();
    }

    #endregion

    #region PatternSimilarityAnalyzer

    [Fact]
    public void Analyze_WithSimilarPatterns_IdentifiesPairsAndFamilies()
    {
        // Arrange - Create fingerprints with similar bitmasks
        var fp1 = CreateFingerprintWithBitmask(1, "HASH_A", new Dictionary<string, long>
        {
            ["Kick"] = 0b0001_0001, // positions 0 and 4
            ["Snare"] = 0b0100_0100  // positions 2 and 6
        });

        var fp2 = CreateFingerprintWithBitmask(2, "HASH_B", new Dictionary<string, long>
        {
            ["Kick"] = 0b0001_0011, // positions 0, 1, 4 (similar to HASH_A)
            ["Snare"] = 0b0100_0100  // same as HASH_A
        });

        var fp3 = CreateFingerprintWithBitmask(3, "HASH_C", new Dictionary<string, long>
        {
            ["Kick"] = 0b1000_0000, // completely different
            ["Snare"] = 0b0000_0001
        });

        var fingerprints = new List<BarPatternFingerprint> { fp1, fp2, fp3 };

        // Act
        var result = PatternSimilarityAnalyzer.Analyze(fingerprints);

        // Assert
        // HASH_A and HASH_B should be similar (high Jaccard similarity)
        result.SimilarPairs.Should().NotBeEmpty();

        // Should have pattern families grouping similar patterns
        if (result.PatternFamilies.Count > 0)
        {
            result.PatternFamilies[0].VariantHashes.Should().NotBeEmpty();
        }
    }

    #endregion

    #region Helper Methods

    private static BarPatternFingerprint CreateFingerprint(int barNumber, string hash)
    {
        return new BarPatternFingerprint
        {
            BarNumber = barNumber,
            BeatsPerBar = 4,
            PatternHash = hash,
            RoleBitmasks = new Dictionary<string, long> { ["Kick"] = 1L },
            RoleVelocities = new Dictionary<string, IReadOnlyList<int>> { ["Kick"] = new[] { 100 } },
            RoleEventCounts = new Dictionary<string, int> { ["Kick"] = 1 },
            GridResolution = 16
        };
    }

    private static BarPatternFingerprint CreateFingerprintWithBitmask(
        int barNumber,
        string hash,
        Dictionary<string, long> bitmasks)
    {
        return new BarPatternFingerprint
        {
            BarNumber = barNumber,
            BeatsPerBar = 4,
            PatternHash = hash,
            RoleBitmasks = bitmasks,
            RoleVelocities = bitmasks.ToDictionary(k => k.Key, _ => (IReadOnlyList<int>)new[] { 100 }),
            RoleEventCounts = bitmasks.ToDictionary(k => k.Key, _ => 1),
            GridResolution = 16
        };
    }

    #endregion
}

