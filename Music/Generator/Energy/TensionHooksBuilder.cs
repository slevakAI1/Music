// AI: purpose=Deterministically derive bounded TensionHooks from macro+micro tension and transition hint.
// AI: invariants=Bias-only (never forces events); clamps outputs; optional knobs must not exceed existing role caps.
// AI: deps=Uses ITensionQuery (+ optional DeterministicTensionQuery for transition hint); consumes EnergySectionProfile for safety.

namespace Music.Generator;

public static class TensionHooksBuilder
{
    // AI: purpose=Style knob to weaken/disable phrase-end ramps (0=flat micro map, 1=full effect).
    public static double ApplyPhraseRampIntensity(double microTension, double macroTension, double microTensionPhraseRampIntensity)
    {
        microTensionPhraseRampIntensity = Math.Clamp(microTensionPhraseRampIntensity, 0.0, 1.0);
        microTension = Math.Clamp(microTension, 0.0, 1.0);
        macroTension = Math.Clamp(macroTension, 0.0, 1.0);

        // Blend toward macro (flatter) as intensity approaches 0.
        return Math.Clamp((microTensionPhraseRampIntensity * microTension) + ((1.0 - microTensionPhraseRampIntensity) * macroTension), 0.0, 1.0);
    }

    // AI: purpose=Create bar-level hook biases; inputs are already deterministic; output must be deterministic.
    public static TensionHooks Create(
        double macroTension,
        double microTension,
        bool isPhraseEnd,
        bool isSectionStart,
        SectionTransitionHint transitionHint,
        double sectionEnergy,
        double microTensionPhraseRampIntensity)
    {
        macroTension = Math.Clamp(macroTension, 0.0, 1.0);
        microTension = ApplyPhraseRampIntensity(microTension, macroTension, microTensionPhraseRampIntensity);
        sectionEnergy = Math.Clamp(sectionEnergy, 0.0, 1.0);

        double phraseEndFactor = isPhraseEnd ? 1.0 : 0.0;
        double sectionStartFactor = isSectionStart ? 1.0 : 0.0;

        // Keep biases modest; DT + energy clamp act as safety rails.
        double tensionness = (macroTension * 0.55) + (microTension * 0.45);

        double transitionImpactFactor = transitionHint switch
        {
            SectionTransitionHint.Build => 0.15,
            SectionTransitionHint.Release => 0.05,
            SectionTransitionHint.Drop => -0.05,
            _ => 0.0
        };

        // Phrase-end pull/fill bias: prefer high micro tension, but avoid over-busying at max energy.
        double pullBias = phraseEndFactor * (tensionness - 0.5) * 0.30;
        pullBias *= (1.0 - (sectionEnergy * 0.35));

        // Section-start impacts: mostly transition-driven, only when designed section start.
        double impactBias = sectionStartFactor * ((tensionness - 0.4) * 0.20 + transitionImpactFactor);

        // Accent bias: small MIDI velocity delta (bounded); release/drop reduces accents.
        int velocityBias = (int)Math.Round((tensionness - 0.5) * 14.0);
        if (transitionHint == SectionTransitionHint.Release) velocityBias -= 2;
        if (transitionHint == SectionTransitionHint.Drop) velocityBias -= 4;

        // Thinning supports breakdown tension distinct from energy (higher tension can thin when energy not high).
        double thinningBias = (transitionHint == SectionTransitionHint.Drop ? 0.10 : 0.0) + ((tensionness - sectionEnergy) * 0.15);
        thinningBias = Math.Max(0.0, thinningBias);

        // Variation intensity is a small bias, used later by motif/melody variation ops.
        double variationBias = (tensionness - 0.5) * 0.20;

        // Clamp all outputs to safe ranges.
        pullBias = Math.Clamp(pullBias, -0.20, 0.20);
        impactBias = Math.Clamp(impactBias, -0.15, 0.15);
        velocityBias = Math.Clamp(velocityBias, -12, 12);
        thinningBias = Math.Clamp(thinningBias, 0.0, 0.25);
        variationBias = Math.Clamp(variationBias, -0.20, 0.20);

        return new TensionHooks(
            PullProbabilityBias: pullBias,
            ImpactProbabilityBias: impactBias,
            VelocityAccentBias: velocityBias,
            DensityThinningBias: thinningBias,
            VariationIntensityBias: variationBias);
    }

    // AI: purpose=Convenience overload wiring to existing query types/drivers without changing renderers yet.
    public static TensionHooks Create(
        ITensionQuery tensionQuery,
        int absoluteSectionIndex,
        int barIndexWithinSection,
        EnergySectionProfile? energyProfile,
        double microTensionPhraseRampIntensity)
    {
        ArgumentNullException.ThrowIfNull(tensionQuery);

        var macro = tensionQuery.GetMacroTension(absoluteSectionIndex);
        double micro = tensionQuery.GetMicroTension(absoluteSectionIndex, barIndexWithinSection);
        var (isPhraseEnd, _, isSectionStart) = tensionQuery.GetPhraseFlags(absoluteSectionIndex, barIndexWithinSection);

        var hint = tensionQuery is DeterministicTensionQuery dtq
            ? dtq.GetTransitionHint(absoluteSectionIndex)
            : SectionTransitionHint.None;

        double sectionEnergy = energyProfile?.Global.Energy ?? 0.5;

        return Create(
            macro.MacroTension,
            micro,
            isPhraseEnd,
            isSectionStart,
            hint,
            sectionEnergy,
            microTensionPhraseRampIntensity);
    }
}
