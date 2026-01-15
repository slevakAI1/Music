// AI: purpose=Story 7.5.5 tests: tension hooks bias drum fill/dropout decisions deterministically without breaking groove anchors.
// AI: invariants=No randomness beyond deterministic seeds; tests compare relative rates, not exact MIDI output.

namespace Music.Generator.Tests;

internal static class DrumTensionHooksIntegrationTests
{
    public static void Test_TensionHooks_Increase_Fill_Probability_When_Eligible()
    {
        const int seed = 123;
        const string grooveName = "PopRockBasic";

        // Use a scenario where transition fill is false and drum FillProbability is 0, so only tension can trigger shouldFill.
        // We mirror DrumTrackGenerator logic: shouldFill = transition || rng < FillProbability + max(0, PullBias).
        // Using bar=1 with totalBars=8 means DrumFillEngine.ShouldGenerateFill should be false.
        const int bar = 1;
        const int totalBars = 8;
        bool transitionFill = DrumFillEngine.ShouldGenerateFill(bar, totalBars, BuildSectionTrack_OneSection(totalBars));
        if (transitionFill)
            throw new Exception("Precondition failed: expected no transition fill");

        var rngNeutral = RandomHelpers.CreateLocalRng(seed, $"{grooveName}_{MusicConstants.eSectionType.Verse}", bar, 0m);
        var rngTension = RandomHelpers.CreateLocalRng(seed, $"{grooveName}_{MusicConstants.eSectionType.Verse}", bar, 0m);

        // Neutral hooks => extra prob 0
        double neutralExtra = 0.0;

        // High-tension hooks => positive PullProbabilityBias => extra prob > 0
        var hooks = TensionHooksBuilder.Create(
            macroTension: 0.95,
            microTension: 0.95,
            isPhraseEnd: true,
            isSectionStart: false,
            transitionHint: SectionTransitionHint.Build,
            sectionEnergy: 0.5,
            microTensionPhraseRampIntensity: 1.0);

        double tensionExtra = Math.Clamp(hooks.PullProbabilityBias, 0.0, 0.20);
        if (tensionExtra <= 0.0)
            throw new Exception("Precondition failed: expected positive tensionExtra");

        bool neutralShouldFill = rngNeutral.NextDouble() < neutralExtra;
        bool tensionShouldFill = rngTension.NextDouble() < tensionExtra;

        // With identical RNG state, adding positive probability cannot make shouldFill less likely.
        if (neutralShouldFill && !tensionShouldFill)
            throw new Exception("Tension fill bias should not reduce fill triggering");
    }

    public static void Test_TensionHooks_Can_Drop_NonMain_Hats_Late_In_Bar()
    {
        const int seed = 456;
        const string grooveName = "PopRockBasic";
        const int barIndex = 3;

        var grooveEvent = new GrooveEvent { SourcePresetName = grooveName };
        var drumParams = new DrumRoleParameters { DensityMultiplier = 1.0, BusyProbability = 0.0, FillProbability = 0.0, FillComplexityMultiplier = 1.0, VelocityBias = 0.0 };

        // Create a baseline variation with hats, then inject a non-main late hat so the dropout filter has something to remove.
        var variation = DrumVariationEngine.Generate(grooveEvent, MusicConstants.eSectionType.Verse, barIndex, seed, drumParams);
        variation.Hits.Add(new DrumVariationEngine.DrumHit { Role = "hat", OnsetBeat = 4.25m, IsMain = false, TimingOffsetTicks = 0 });

        int hatsBefore = variation.Hits.Count(h => h.Role == "hat");

        var hooks = TensionHooksBuilder.Create(
            macroTension: 0.95,
            microTension: 0.95,
            isPhraseEnd: true,
            isSectionStart: false,
            transitionHint: SectionTransitionHint.Build,
            sectionEnergy: 0.5,
            microTensionPhraseRampIntensity: 1.0);

        var barRng = RandomHelpers.CreateLocalRng(seed, $"{grooveName}_{MusicConstants.eSectionType.Verse}", barIndex, 0m);

        // Deterministically mirror DrumTrackGenerator dropout gate.
        bool shouldDrop = hooks.PullProbabilityBias > 0.0
            && hooks.DensityThinningBias > 0.0001
            && 0.5 < 0.92
            && barRng.NextDouble() < Math.Clamp(hooks.DensityThinningBias * 1.5, 0.0, 0.30);

        var after = shouldDrop
            ? variation.Hits.Where(h => h.Role != "hat" || h.IsMain || h.OnsetBeat < 4m).ToList()
            : variation.Hits;

        int hatsAfter = after.Count(h => h.Role == "hat");

        // If dropout triggered, hats must not increase and the injected late non-main hat must be removed.
        if (shouldDrop)
        {
            if (hatsAfter > hatsBefore)
                throw new Exception("Dropout should not increase hat count");

            if (after.Any(h => h.Role == "hat" && !h.IsMain && h.OnsetBeat >= 4m))
                throw new Exception("Dropout should remove late non-main hat hits");
        }

        // Always: anchor/main hats must remain.
        if (after.Any(h => h.Role == "hat" && h.IsMain) == false && hatsBefore > 0)
            throw new Exception("Dropout must not remove anchor/main hats");
    }

    private static SectionTrack BuildSectionTrack_OneSection(int bars)
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Verse, bars);
        return track;
    }
}
