namespace Music.Generator;

public static class TensionHooksTests
{
    public static void Test_Create_Clamps_All_Outputs()
    {
        var hooks = TensionHooksBuilder.Create(
            macroTension: 2.0,
            microTension: -1.0,
            isPhraseEnd: true,
            isSectionStart: true,
            transitionHint: SectionTransitionHint.Build,
            sectionEnergy: 3.0,
            microTensionPhraseRampIntensity: 2.0);

        if (hooks.PullProbabilityBias < -0.2000001 || hooks.PullProbabilityBias > 0.2000001)
            throw new Exception("PullProbabilityBias out of clamp range");

        if (hooks.ImpactProbabilityBias < -0.1500001 || hooks.ImpactProbabilityBias > 0.1500001)
            throw new Exception("ImpactProbabilityBias out of clamp range");

        if (hooks.VelocityAccentBias < -12 || hooks.VelocityAccentBias > 12)
            throw new Exception("VelocityAccentBias out of clamp range");

        if (hooks.DensityThinningBias < -0.0000001 || hooks.DensityThinningBias > 0.2500001)
            throw new Exception("DensityThinningBias out of clamp range");

        if (hooks.VariationIntensityBias < -0.2000001 || hooks.VariationIntensityBias > 0.2000001)
            throw new Exception("VariationIntensityBias out of clamp range");
    }

    public static void Test_Create_Is_Deterministic_For_Same_Inputs()
    {
        var a = TensionHooksBuilder.Create(
            macroTension: 0.7,
            microTension: 0.8,
            isPhraseEnd: true,
            isSectionStart: false,
            transitionHint: SectionTransitionHint.Sustain,
            sectionEnergy: 0.6,
            microTensionPhraseRampIntensity: 1.0);

        var b = TensionHooksBuilder.Create(
            macroTension: 0.7,
            microTension: 0.8,
            isPhraseEnd: true,
            isSectionStart: false,
            transitionHint: SectionTransitionHint.Sustain,
            sectionEnergy: 0.6,
            microTensionPhraseRampIntensity: 1.0);

        if (a != b)
            throw new Exception("TensionHooks not deterministic for same inputs");
    }

    public static void Test_ApplyPhraseRampIntensity_Zero_Uses_Macro()
    {
        double value = TensionHooksBuilder.ApplyPhraseRampIntensity(
            microTension: 0.9,
            macroTension: 0.2,
            microTensionPhraseRampIntensity: 0.0);

        if (Math.Abs(value - 0.2) > 0.000001)
            throw new Exception("Expected ramp intensity 0 to return macro tension");
    }

    public static void Test_ApplyPhraseRampIntensity_One_Uses_Micro()
    {
        double value = TensionHooksBuilder.ApplyPhraseRampIntensity(
            microTension: 0.9,
            macroTension: 0.2,
            microTensionPhraseRampIntensity: 1.0);

        if (Math.Abs(value - 0.9) > 0.000001)
            throw new Exception("Expected ramp intensity 1 to return micro tension");
    }
}
