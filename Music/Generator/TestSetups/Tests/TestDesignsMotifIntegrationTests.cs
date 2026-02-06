// AI: purpose=Integration tests for TestDesigns motif fixtures populating MaterialBank
// AI: invariants=After SetTestDesignD1 MaterialBank contains 4 motifs; queries by role/kind must succeed
// AI: deps=Relies on TestDesigns.SetTestDesignD1, SongContext, MaterialBank API (GetMotifByName/GetMotifsByRole)
using Music.Generator;
using Music.Song.Material;

namespace Music.Tests.Integration;

// AI: tests=Validate TestDesigns D1 populates MaterialBank and motifs are queryable by role/kind
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

    // AI: case=After SetTestDesignD1 MaterialBank.Count must equal 4
    private static void TestSetTestDesignD1PopulatesMaterialBank()
    {
        var songContext = new SongContext();
        TestDesigns.SetTestDesignD1(songContext);

        Assert(songContext.MaterialBank.Count == 4, 
            $"MaterialBank should contain 4 motifs after SetTestDesignD1, but has {songContext.MaterialBank.Count}");

        Console.WriteLine("  ✓ SetTestDesignD1 populates MaterialBank with 4 motifs");
    }

    // AI: case=Ensure each expected motif name exists in MaterialBank after setup
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

    // AI: case=Verify role and kind queries return expected counts for test motifs
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
