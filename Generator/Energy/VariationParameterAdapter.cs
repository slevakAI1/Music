// AI: purpose=Apply SectionVariationPlan deltas to EnergyRoleProfile parameters with guardrail enforcement (Story 7.6.5).
// AI: invariants=All outputs clamped to safe ranges; DensityMultiplier [0.5,2.0]; VelocityBias [-127,+127]; RegisterLift [-48,+48]; BusyProb [0,1].
// AI: deps=Consumes EnergyRoleProfile and SectionVariationPlan; produces adjusted parameters for role generators.
// AI: change=When adding new role parameters, add corresponding Apply*Delta methods with appropriate clamping.

namespace Music.Generator;

/// <summary>
/// Applies SectionVariationPlan deltas to EnergyRoleProfile parameters with guardrail enforcement.
/// Provides pure mapping functions that keep changes bounded and safe.
/// Story 7.6.5: Role-parameter application adapters + minimal diagnostics.
/// </summary>
/// <remarks>
/// Application semantics:
/// - DensityMultiplier: applied multiplicatively (baseDensity * delta)
/// - VelocityBias: applied additively (baseVelocity + delta)
/// - RegisterLiftSemitones: applied additively (baseRegister + delta)
/// - BusyProbability: applied additively (baseBusy + delta), clamped [0..1]
/// 
/// All methods are pure functions (no side effects) and maintain determinism.
/// Guardrails are enforced to prevent musical violations (muddy low register, velocity overflow, etc.).
/// </remarks>
public static class VariationParameterAdapter
{
    // Guardrail constants
    private const double MinDensityMultiplier = 0.5;
    private const double MaxDensityMultiplier = 2.0;
    private const int MinVelocityBias = -127;
    private const int MaxVelocityBias = 127;
    private const int MinRegisterLiftSemitones = -48;
    private const int MaxRegisterLiftSemitones = 48;
    private const double MinBusyProbability = 0.0;
    private const double MaxBusyProbability = 1.0;

    /// <summary>
    /// Applies a SectionVariationPlan to an EnergyRoleProfile for a specific role.
    /// Returns adjusted parameters with guardrails enforced.
    /// </summary>
    /// <param name="baseProfile">Base energy profile from Stage 7.</param>
    /// <param name="variationDelta">Optional variation delta to apply (null = no change).</param>
    /// <returns>Adjusted EnergyRoleProfile with variation applied and guardrails enforced.</returns>
    public static EnergyRoleProfile ApplyVariation(
        EnergyRoleProfile baseProfile,
        RoleVariationDelta? variationDelta)
    {
        if (variationDelta == null)
        {
            return baseProfile; // No variation = return base unchanged
        }

        return new EnergyRoleProfile
        {
            DensityMultiplier = ApplyDensityDelta(baseProfile.DensityMultiplier, variationDelta.DensityMultiplier),
            VelocityBias = ApplyVelocityDelta(baseProfile.VelocityBias, variationDelta.VelocityBias),
            RegisterLiftSemitones = ApplyRegisterDelta(baseProfile.RegisterLiftSemitones, variationDelta.RegisterLiftSemitones),
            BusyProbability = ApplyBusyProbabilityDelta(baseProfile.BusyProbability, variationDelta.BusyProbability)
        };
    }

    /// <summary>
    /// Applies density multiplier delta (multiplicative application).
    /// </summary>
    private static double ApplyDensityDelta(double baseDensity, double? delta)
    {
        if (delta == null)
        {
            return baseDensity;
        }

        double result = baseDensity * delta.Value;
        return Math.Clamp(result, MinDensityMultiplier, MaxDensityMultiplier);
    }

    /// <summary>
    /// Applies velocity bias delta (additive application).
    /// </summary>
    private static int ApplyVelocityDelta(int baseVelocity, int? delta)
    {
        if (delta == null)
        {
            return baseVelocity;
        }

        int result = baseVelocity + delta.Value;
        return Math.Clamp(result, MinVelocityBias, MaxVelocityBias);
    }

    /// <summary>
    /// Applies register lift delta (additive application).
    /// </summary>
    private static int ApplyRegisterDelta(int baseRegister, int? delta)
    {
        if (delta == null)
        {
            return baseRegister;
        }

        int result = baseRegister + delta.Value;
        return Math.Clamp(result, MinRegisterLiftSemitones, MaxRegisterLiftSemitones);
    }

    /// <summary>
    /// Applies busy probability delta (additive application, clamped [0..1]).
    /// </summary>
    private static double ApplyBusyProbabilityDelta(double baseBusy, double? delta)
    {
        if (delta == null)
        {
            return baseBusy;
        }

        double result = baseBusy + delta.Value;
        return Math.Clamp(result, MinBusyProbability, MaxBusyProbability);
    }

    /// <summary>
    /// Applies variation plan to DrumRoleParameters.
    /// Drums use slightly different parameter names/ranges than other roles.
    /// </summary>
    public static DrumRoleParameters ApplyVariationToDrums(
        DrumRoleParameters baseParams,
        RoleVariationDelta? variationDelta)
    {
        if (variationDelta == null)
        {
            return baseParams; // No variation = return base unchanged
        }

        return new DrumRoleParameters
        {
            DensityMultiplier = ApplyDensityDelta(baseParams.DensityMultiplier, variationDelta.DensityMultiplier),
            VelocityBias = ApplyVelocityDelta((int)baseParams.VelocityBias, variationDelta.VelocityBias),
            BusyProbability = ApplyBusyProbabilityDelta(baseParams.BusyProbability, variationDelta.BusyProbability),
            // FillProbability and FillComplexityMultiplier not affected by variation (kept from base)
            FillProbability = baseParams.FillProbability,
            FillComplexityMultiplier = baseParams.FillComplexityMultiplier
        };
    }

    /// <summary>
    /// Generates a diagnostic string describing the variation applied.
    /// Returns null if no variation applied.
    /// </summary>
    public static string? GetVariationDiagnostic(
        string roleName,
        EnergyRoleProfile baseProfile,
        RoleVariationDelta? variationDelta)
    {
        if (variationDelta == null)
        {
            return null;
        }

        var parts = new List<string>();

        if (variationDelta.DensityMultiplier.HasValue)
        {
            double final = ApplyDensityDelta(baseProfile.DensityMultiplier, variationDelta.DensityMultiplier);
            parts.Add($"Density {baseProfile.DensityMultiplier:F2}?{final:F2}");
        }

        if (variationDelta.VelocityBias.HasValue)
        {
            int final = ApplyVelocityDelta(baseProfile.VelocityBias, variationDelta.VelocityBias);
            parts.Add($"Vel {baseProfile.VelocityBias:+#;-#;0}?{final:+#;-#;0}");
        }

        if (variationDelta.RegisterLiftSemitones.HasValue)
        {
            int final = ApplyRegisterDelta(baseProfile.RegisterLiftSemitones, variationDelta.RegisterLiftSemitones);
            parts.Add($"Reg {baseProfile.RegisterLiftSemitones:+#;-#;0}?{final:+#;-#;0}");
        }

        if (variationDelta.BusyProbability.HasValue)
        {
            double final = ApplyBusyProbabilityDelta(baseProfile.BusyProbability, variationDelta.BusyProbability);
            parts.Add($"Busy {baseProfile.BusyProbability:F2}?{final:F2}");
        }

        return parts.Count > 0 ? $"{roleName}: {string.Join(", ", parts)}" : null;
    }
}
