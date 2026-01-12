// AI: purpose=Bar-level derived knobs from tension (macro+micro) that renderers can consume without planner internals.
// AI: invariants=All fields clamped to safe ranges; must only bias optional events; must not override groove anchors.
// AI: change=If adding new hook fields, update TensionHooksBuilder + tests; keep derivation deterministic.

namespace Music.Generator;

public readonly record struct TensionHooks(
    double PullProbabilityBias,
    double ImpactProbabilityBias,
    int VelocityAccentBias,
    double DensityThinningBias,
    double VariationIntensityBias)
{
    public static TensionHooks Neutral => new(
        PullProbabilityBias: 0.0,
        ImpactProbabilityBias: 0.0,
        VelocityAccentBias: 0,
        DensityThinningBias: 0.0,
        VariationIntensityBias: 0.0);
}
