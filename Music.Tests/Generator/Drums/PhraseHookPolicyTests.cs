// AI: purpose=Unit tests for Story 12 phrase hook policy; validates anchor protection in phrase/section-end windows.
// AI: deps=DrumTrackGeneratorNew.ApplyPhraseHookPolicyToProtections; uses reflection to test private method.
// AI: coverage=ProtectDownbeatOnPhraseEnd, ProtectBackbeatOnPhraseEnd, window detection, different time signatures.

using FluentAssertions;
using Music.Generator;
using System.Reflection;

namespace Music.Tests.Generator.Drums;

public class Story12PhraseHookPolicyTests
{
    private readonly MethodInfo _applyPhraseHookPolicyMethod;

    public Story12PhraseHookPolicyTests()
    {
        var type = typeof(DrumTrackGeneratorNew);
        
        _applyPhraseHookPolicyMethod = type.GetMethod(
            "ApplyPhraseHookPolicyToProtections",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("ApplyPhraseHookPolicyToProtections method not found");
    }

    [Fact]
    public void ApplyPhraseHookPolicy_NullPolicy_ReturnsUnchanged()
    {
        var mergedProtections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
        {
            [1] = new Dictionary<string, RoleProtectionSet>
            {
                ["Kick"] = new RoleProtectionSet
                {
                    NeverRemoveOnsets = new List<decimal> { 1m }
                }
            }
        };

        var barContexts = new List<DrumBarContext>
        {
            new DrumBarContext(1, null, null, 0, 3)
        };

        var result = InvokeApplyPhraseHookPolicy(mergedProtections, barContexts, null, 4);

        result.Should().BeSameAs(mergedProtections);
        result[1]["Kick"].NeverRemoveOnsets.Should().HaveCount(1);
    }

    [Fact]
    public void ApplyPhraseHookPolicy_EmptyProtections_ReturnsEmpty()
    {
        var mergedProtections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>();
        var barContexts = new List<DrumBarContext>();

        var policy = new GroovePhraseHookPolicy
        {
            ProtectDownbeatOnPhraseEnd = true,
            PhraseEndBarsWindow = 1
        };

        var result = InvokeApplyPhraseHookPolicy(mergedProtections, barContexts, policy, 4);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ProtectDownbeatOnPhraseEnd_True_AddsDownbeatToNeverRemove()
    {
        var mergedProtections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
        {
            [4] = new Dictionary<string, RoleProtectionSet>
            {
                ["Kick"] = new RoleProtectionSet
                {
                    NeverRemoveOnsets = new List<decimal>()
                }
            }
        };

        var barContexts = new List<DrumBarContext>
        {
            new DrumBarContext(4, CreateSection(), null, 3, 0) // Last bar of section
        };

        var policy = new GroovePhraseHookPolicy
        {
            AllowFillsAtPhraseEnd = false,
            PhraseEndBarsWindow = 1,
            ProtectDownbeatOnPhraseEnd = true
        };

        var result = InvokeApplyPhraseHookPolicy(mergedProtections, barContexts, policy, 4);

        result[4]["Kick"].NeverRemoveOnsets.Should().Contain(1m);
    }

    [Fact]
    public void ProtectDownbeatOnPhraseEnd_False_DoesNotAddDownbeat()
    {
        var mergedProtections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
        {
            [4] = new Dictionary<string, RoleProtectionSet>
            {
                ["Kick"] = new RoleProtectionSet
                {
                    NeverRemoveOnsets = new List<decimal>()
                }
            }
        };

        var barContexts = new List<DrumBarContext>
        {
            new DrumBarContext(4, CreateSection(), null, 3, 0)
        };

        var policy = new GroovePhraseHookPolicy
        {
            AllowFillsAtPhraseEnd = false,
            PhraseEndBarsWindow = 1,
            ProtectDownbeatOnPhraseEnd = false
        };

        var result = InvokeApplyPhraseHookPolicy(mergedProtections, barContexts, policy, 4);

        result[4]["Kick"].NeverRemoveOnsets.Should().BeEmpty();
    }

    [Fact]
    public void ProtectBackbeatOnPhraseEnd_True_AddsBackbeatsToNeverRemove()
    {
        var mergedProtections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
        {
            [4] = new Dictionary<string, RoleProtectionSet>
            {
                ["Snare"] = new RoleProtectionSet
                {
                    NeverRemoveOnsets = new List<decimal>()
                }
            }
        };

        var barContexts = new List<DrumBarContext>
        {
            new DrumBarContext(4, CreateSection(), null, 3, 0)
        };

        var policy = new GroovePhraseHookPolicy
        {
            AllowFillsAtPhraseEnd = false,
            PhraseEndBarsWindow = 1,
            ProtectBackbeatOnPhraseEnd = true
        };

        var result = InvokeApplyPhraseHookPolicy(mergedProtections, barContexts, policy, 4);

        result[4]["Snare"].NeverRemoveOnsets.Should().Contain(new[] { 2m, 4m });
    }

    [Fact]
    public void ProtectBackbeatOnPhraseEnd_False_DoesNotAddBackbeats()
    {
        var mergedProtections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
        {
            [4] = new Dictionary<string, RoleProtectionSet>
            {
                ["Snare"] = new RoleProtectionSet
                {
                    NeverRemoveOnsets = new List<decimal>()
                }
            }
        };

        var barContexts = new List<DrumBarContext>
        {
            new DrumBarContext(4, CreateSection(), null, 3, 0)
        };

        var policy = new GroovePhraseHookPolicy
        {
            AllowFillsAtPhraseEnd = false,
            PhraseEndBarsWindow = 1,
            ProtectBackbeatOnPhraseEnd = false
        };

        var result = InvokeApplyPhraseHookPolicy(mergedProtections, barContexts, policy, 4);

        result[4]["Snare"].NeverRemoveOnsets.Should().BeEmpty();
    }

    [Fact]
    public void PhraseEndWindow_DetectsCorrectBars()
    {
        var mergedProtections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
        {
            [1] = new Dictionary<string, RoleProtectionSet> { ["Kick"] = new RoleProtectionSet() },
            [2] = new Dictionary<string, RoleProtectionSet> { ["Kick"] = new RoleProtectionSet() },
            [3] = new Dictionary<string, RoleProtectionSet> { ["Kick"] = new RoleProtectionSet() },
            [4] = new Dictionary<string, RoleProtectionSet> { ["Kick"] = new RoleProtectionSet() }
        };

        var barContexts = new List<DrumBarContext>
        {
            new DrumBarContext(1, CreateSection(), null, 0, 3), // 3 bars until end - not in window
            new DrumBarContext(2, CreateSection(), null, 1, 2), // 2 bars until end - not in window
            new DrumBarContext(3, CreateSection(), null, 2, 1), // 1 bar until end - not in window
            new DrumBarContext(4, CreateSection(), null, 3, 0)  // 0 bars until end - IN WINDOW
        };

        var policy = new GroovePhraseHookPolicy
        {
            AllowFillsAtPhraseEnd = false,
            PhraseEndBarsWindow = 1,
            ProtectDownbeatOnPhraseEnd = true
        };

        var result = InvokeApplyPhraseHookPolicy(mergedProtections, barContexts, policy, 4);

        result[1]["Kick"].NeverRemoveOnsets.Should().BeEmpty("bar 1 not in window");
        result[2]["Kick"].NeverRemoveOnsets.Should().BeEmpty("bar 2 not in window");
        result[3]["Kick"].NeverRemoveOnsets.Should().BeEmpty("bar 3 not in window");
        result[4]["Kick"].NeverRemoveOnsets.Should().Contain(1m, "bar 4 is in window");
    }

    [Fact]
    public void PhraseEndWindow_Size2_DetectsLastTwoBars()
    {
        var mergedProtections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
        {
            [1] = new Dictionary<string, RoleProtectionSet> { ["Kick"] = new RoleProtectionSet() },
            [2] = new Dictionary<string, RoleProtectionSet> { ["Kick"] = new RoleProtectionSet() },
            [3] = new Dictionary<string, RoleProtectionSet> { ["Kick"] = new RoleProtectionSet() },
            [4] = new Dictionary<string, RoleProtectionSet> { ["Kick"] = new RoleProtectionSet() }
        };

        var barContexts = new List<DrumBarContext>
        {
            new DrumBarContext(1, CreateSection(), null, 0, 3),
            new DrumBarContext(2, CreateSection(), null, 1, 2),
            new DrumBarContext(3, CreateSection(), null, 2, 1), // IN WINDOW
            new DrumBarContext(4, CreateSection(), null, 3, 0)  // IN WINDOW
        };

        var policy = new GroovePhraseHookPolicy
        {
            AllowFillsAtPhraseEnd = false,
            PhraseEndBarsWindow = 2,
            ProtectDownbeatOnPhraseEnd = true
        };

        var result = InvokeApplyPhraseHookPolicy(mergedProtections, barContexts, policy, 4);

        result[1]["Kick"].NeverRemoveOnsets.Should().BeEmpty();
        result[2]["Kick"].NeverRemoveOnsets.Should().BeEmpty();
        result[3]["Kick"].NeverRemoveOnsets.Should().Contain(1m);
        result[4]["Kick"].NeverRemoveOnsets.Should().Contain(1m);
    }

    [Fact]
    public void AllowFillsAtPhraseEnd_True_DoesNotProtect()
    {
        var mergedProtections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
        {
            [4] = new Dictionary<string, RoleProtectionSet>
            {
                ["Kick"] = new RoleProtectionSet()
            }
        };

        var barContexts = new List<DrumBarContext>
        {
            new DrumBarContext(4, CreateSection(), null, 3, 0)
        };

        var policy = new GroovePhraseHookPolicy
        {
            AllowFillsAtPhraseEnd = true, // Fills allowed, so no protection
            PhraseEndBarsWindow = 1,
            ProtectDownbeatOnPhraseEnd = true
        };

        var result = InvokeApplyPhraseHookPolicy(mergedProtections, barContexts, policy, 4);

        result[4]["Kick"].NeverRemoveOnsets.Should().BeEmpty();
    }

    [Fact]
    public void ThreeQuarterTime_OnlyAddsAvailableBackbeats()
    {
        var mergedProtections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
        {
            [3] = new Dictionary<string, RoleProtectionSet>
            {
                ["Snare"] = new RoleProtectionSet()
            }
        };

        var barContexts = new List<DrumBarContext>
        {
            new DrumBarContext(3, CreateSection(), null, 2, 0)
        };

        var policy = new GroovePhraseHookPolicy
        {
            AllowFillsAtPhraseEnd = false,
            PhraseEndBarsWindow = 1,
            ProtectBackbeatOnPhraseEnd = true
        };

        var result = InvokeApplyPhraseHookPolicy(mergedProtections, barContexts, policy, 3); // 3/4 time

        result[3]["Snare"].NeverRemoveOnsets.Should().Contain(2m, "beat 2 exists in 3/4");
        result[3]["Snare"].NeverRemoveOnsets.Should().NotContain(4m, "beat 4 doesn't exist in 3/4");
    }

    [Fact]
    public void MultipleRoles_AllReceiveProtections()
    {
        var mergedProtections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
        {
            [4] = new Dictionary<string, RoleProtectionSet>
            {
                ["Kick"] = new RoleProtectionSet(),
                ["Snare"] = new RoleProtectionSet(),
                ["ClosedHat"] = new RoleProtectionSet()
            }
        };

        var barContexts = new List<DrumBarContext>
        {
            new DrumBarContext(4, CreateSection(), null, 3, 0)
        };

        var policy = new GroovePhraseHookPolicy
        {
            AllowFillsAtPhraseEnd = false,
            PhraseEndBarsWindow = 1,
            ProtectDownbeatOnPhraseEnd = true,
            ProtectBackbeatOnPhraseEnd = true
        };

        var result = InvokeApplyPhraseHookPolicy(mergedProtections, barContexts, policy, 4);

        result[4]["Kick"].NeverRemoveOnsets.Should().Contain(new[] { 1m, 2m, 4m });
        result[4]["Snare"].NeverRemoveOnsets.Should().Contain(new[] { 1m, 2m, 4m });
        result[4]["ClosedHat"].NeverRemoveOnsets.Should().Contain(new[] { 1m, 2m, 4m });
    }

    [Fact]
    public void PreservesExistingNeverRemoveOnsets()
    {
        var mergedProtections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
        {
            [4] = new Dictionary<string, RoleProtectionSet>
            {
                ["Kick"] = new RoleProtectionSet
                {
                    NeverRemoveOnsets = new List<decimal> { 3m }
                }
            }
        };

        var barContexts = new List<DrumBarContext>
        {
            new DrumBarContext(4, CreateSection(), null, 3, 0)
        };

        var policy = new GroovePhraseHookPolicy
        {
            AllowFillsAtPhraseEnd = false,
            PhraseEndBarsWindow = 1,
            ProtectDownbeatOnPhraseEnd = true
        };

        var result = InvokeApplyPhraseHookPolicy(mergedProtections, barContexts, policy, 4);

        result[4]["Kick"].NeverRemoveOnsets.Should().Contain(new[] { 1m, 3m });
    }

    [Fact]
    public void DoesNotAddDuplicateOnsets()
    {
        var mergedProtections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
        {
            [4] = new Dictionary<string, RoleProtectionSet>
            {
                ["Kick"] = new RoleProtectionSet
                {
                    NeverRemoveOnsets = new List<decimal> { 1m }
                }
            }
        };

        var barContexts = new List<DrumBarContext>
        {
            new DrumBarContext(4, CreateSection(), null, 3, 0)
        };

        var policy = new GroovePhraseHookPolicy
        {
            AllowFillsAtPhraseEnd = false,
            PhraseEndBarsWindow = 1,
            ProtectDownbeatOnPhraseEnd = true
        };

        var result = InvokeApplyPhraseHookPolicy(mergedProtections, barContexts, policy, 4);

        result[4]["Kick"].NeverRemoveOnsets.Should().HaveCount(1);
        result[4]["Kick"].NeverRemoveOnsets.Should().Contain(1m);
    }

    [Fact]
    public void BarWithNoProtections_CreatesNewProtectionSet()
    {
        var mergedProtections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>();

        var barContexts = new List<DrumBarContext>
        {
            new DrumBarContext(4, CreateSection(), null, 3, 0)
        };

        var policy = new GroovePhraseHookPolicy
        {
            AllowFillsAtPhraseEnd = false,
            PhraseEndBarsWindow = 1,
            ProtectDownbeatOnPhraseEnd = true
        };

        var result = InvokeApplyPhraseHookPolicy(mergedProtections, barContexts, policy, 4);

        result.Should().ContainKey(4);
        result[4].Should().BeEmpty("no existing roles to add protections to");
    }

    [Fact]
    public void SectionEndWindow_WorksSeparately()
    {
        var mergedProtections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
        {
            [4] = new Dictionary<string, RoleProtectionSet>
            {
                ["Kick"] = new RoleProtectionSet()
            }
        };

        var barContexts = new List<DrumBarContext>
        {
            new DrumBarContext(4, CreateSection(), null, 3, 0)
        };

        var policy = new GroovePhraseHookPolicy
        {
            AllowFillsAtPhraseEnd = true, // Phrase fills allowed
            AllowFillsAtSectionEnd = false, // Section fills NOT allowed
            PhraseEndBarsWindow = 1,
            SectionEndBarsWindow = 1,
            ProtectDownbeatOnPhraseEnd = true
        };

        var result = InvokeApplyPhraseHookPolicy(mergedProtections, barContexts, policy, 4);

        // Since AllowFillsAtPhraseEnd=true, phrase window doesn't protect
        // But AllowFillsAtSectionEnd=false would protect if section-end logic was different
        // Current implementation only checks phrase-end window
        result[4]["Kick"].NeverRemoveOnsets.Should().BeEmpty();
    }

    [Fact]
    public void NegativeBarsUntilSectionEnd_IgnoresProtection()
    {
        var mergedProtections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
        {
            [1] = new Dictionary<string, RoleProtectionSet>
            {
                ["Kick"] = new RoleProtectionSet()
            }
        };

        var barContexts = new List<DrumBarContext>
        {
            new DrumBarContext(1, null, null, 0, -1) // No section
        };

        var policy = new GroovePhraseHookPolicy
        {
            AllowFillsAtPhraseEnd = false,
            PhraseEndBarsWindow = 1,
            ProtectDownbeatOnPhraseEnd = true
        };

        var result = InvokeApplyPhraseHookPolicy(mergedProtections, barContexts, policy, 4);

        result[1]["Kick"].NeverRemoveOnsets.Should().BeEmpty();
    }

    [Fact]
    public void ZeroPhraseEndWindow_DoesNotProtect()
    {
        var mergedProtections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
        {
            [4] = new Dictionary<string, RoleProtectionSet>
            {
                ["Kick"] = new RoleProtectionSet()
            }
        };

        var barContexts = new List<DrumBarContext>
        {
            new DrumBarContext(4, CreateSection(), null, 3, 0)
        };

        var policy = new GroovePhraseHookPolicy
        {
            AllowFillsAtPhraseEnd = false,
            PhraseEndBarsWindow = 0, // Zero window size
            ProtectDownbeatOnPhraseEnd = true
        };

        var result = InvokeApplyPhraseHookPolicy(mergedProtections, barContexts, policy, 4);

        result[4]["Kick"].NeverRemoveOnsets.Should().BeEmpty();
    }

    [Fact]
    public void BothProtectionsEnabled_AddsBothDownbeatAndBackbeats()
    {
        var mergedProtections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
        {
            [4] = new Dictionary<string, RoleProtectionSet>
            {
                ["Kick"] = new RoleProtectionSet()
            }
        };

        var barContexts = new List<DrumBarContext>
        {
            new DrumBarContext(4, CreateSection(), null, 3, 0)
        };

        var policy = new GroovePhraseHookPolicy
        {
            AllowFillsAtPhraseEnd = false,
            PhraseEndBarsWindow = 1,
            ProtectDownbeatOnPhraseEnd = true,
            ProtectBackbeatOnPhraseEnd = true
        };

        var result = InvokeApplyPhraseHookPolicy(mergedProtections, barContexts, policy, 4);

        result[4]["Kick"].NeverRemoveOnsets.Should().Contain(new[] { 1m, 2m, 4m });
    }

    // Helper methods
    private Dictionary<int, Dictionary<string, RoleProtectionSet>> InvokeApplyPhraseHookPolicy(
        Dictionary<int, Dictionary<string, RoleProtectionSet>> mergedProtectionsPerBar,
        List<DrumBarContext> barContexts,
        GroovePhraseHookPolicy? phraseHookPolicy,
        int beatsPerBar)
    {
        var result = _applyPhraseHookPolicyMethod.Invoke(
            null,
            new object?[] { mergedProtectionsPerBar, barContexts, phraseHookPolicy, beatsPerBar });
        
        return (Dictionary<int, Dictionary<string, RoleProtectionSet>>)(result 
            ?? new Dictionary<int, Dictionary<string, RoleProtectionSet>>());
    }

    private static Section CreateSection()
    {
        return new Section
        {
            StartBar = 1,
            BarCount = 4,
            SectionType = SectionType.Verse
        };
    }
}
