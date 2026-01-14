// AI: purpose=Test that TestDesigns.SetTestDesignD1 properly populates MaterialBank with test motifs
// AI: invariants=After SetTestDesignD1, MaterialBank should contain all 4 test motifs
// AI: deps=Tests TestDesigns, SongContext, MaterialBank, MotifLibrary integration
using Music.Generator;
using Music.Song.Material;

namespace Music.Tests.Integration;

/// <summary>
/// Tests that TestDesigns properly integrates with the motif system.
/// </summary>
public static class TestDesignsMotifIntegrationTests
{
    public static void RunAll()
    {
        Console.WriteLine("=== TestDesigns + Motif System Integration Tests ===\n");

        TestSetTestDesignD1PopulatesMaterialBank();
        TestMaterialBankContainsAllTestMotifs();
        TestMaterialBankMotifsCanBeQueried();

        Console.WriteLine("\n✓ All TestDesigns + Motif integration tests passed!");
    }

    private static void TestSetTestDesignD1PopulatesMaterialBank()
    {
        var songContext = new SongContext();
        TestDesigns.SetTestDesignD1(songContext);

        Assert(songContext.MaterialBank.Count == 4, 
            $"MaterialBank should contain 4 motifs after SetTestDesignD1, but has {songContext.MaterialBank.Count}");

        Console.WriteLine("  ✓ SetTestDesignD1 populates MaterialBank with 4 motifs");
    }

    private static void TestMaterialBankContainsAllTestMotifs()
    {
        var songContext = new SongContext();
        TestDesigns.SetTestDesignD1(songContext);

        var expectedNames = new[]
        {
            "Classic Rock Hook A",
            "Steady Verse Riff A",
            "Bright Synth Hook A",
            "Bass Transition Fill A"
        };

        foreach (var name in expectedNames)
        {
            var motif = songContext.MaterialBank.GetMotifByName(name);
            Assert(motif != null, $"MaterialBank should contain motif '{name}'");
        }

        Console.WriteLine("  ✓ MaterialBank contains all expected test motifs");
    }

    private static void TestMaterialBankMotifsCanBeQueried()
    {
        var songContext = new SongContext();
        TestDesigns.SetTestDesignD1(songContext);

        // Query by role
        var leadMotifs = songContext.MaterialBank.GetMotifsByRole("Lead");
        Assert(leadMotifs.Count == 1, "Should find 1 Lead motif");

        var guitarMotifs = songContext.MaterialBank.GetMotifsByRole("Guitar");
        Assert(guitarMotifs.Count == 1, "Should find 1 Guitar motif");

        // Query by kind
        var hooks = songContext.MaterialBank.GetMotifsByKind(MaterialKind.Hook);
        Assert(hooks.Count == 2, "Should find 2 Hook motifs");

        var riffs = songContext.MaterialBank.GetMotifsByKind(MaterialKind.Riff);
        Assert(riffs.Count == 1, "Should find 1 Riff motif");

        Console.WriteLine("  ✓ MaterialBank motifs can be queried by role and kind");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"Assertion failed: {message}");
    }
}
