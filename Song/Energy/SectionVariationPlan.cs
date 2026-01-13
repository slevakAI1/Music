// AI: purpose=Immutable variation plan expressing section-to-section reuse and bounded per-role deltas for A/A'/B transforms.
// AI: invariants=VariationIntensity [0..1]; all numeric role fields bounded and optional (null=no change); immutable record; Tags read-only.
// AI: deps=Consumed by role generators via IVariationQuery (Story 7.6.4); produced by SectionVariationPlanner (Story 7.6.3).
// AI: constraints=BaseReferenceSectionIndex must be < AbsoluteSectionIndex if non-null; role deltas must not violate existing guardrails (applied by adapters in Story 7.6.5).

namespace Music.Generator;

/// <summary>
/// Immutable variation plan for a section expressing reuse and bounded per-role transforms.
/// Supports A / A' / B repetition patterns: sections can reference an earlier "base" section
/// and apply controlled deltas to create variation while maintaining musical coherence.
/// </summary>
/// <remarks>
/// Design principles:
/// - BaseReferenceSectionIndex = null means no reuse (new material, "A" or "B" tag).
/// - BaseReferenceSectionIndex != null means reuse with variation ("A'" tag).
/// - Per-role deltas are optional (null = no change from base or energy profile).
/// - All numeric values are clamped to safe ranges via factory methods.
/// - Tags are small stable strings for diagnostics and later stage decisions.
/// </remarks>
public sealed record SectionVariationPlan
{
    /// <summary>
    /// Absolute section index (0-based) in the SectionTrack.
    /// </summary>
    public required int AbsoluteSectionIndex { get; init; }

    /// <summary>
    /// Reference to an earlier section index to reuse as a "base" (null = no reuse).
    /// When non-null, role generators should attempt to replicate core decisions from the base section
    /// while applying bounded per-role deltas specified by this plan.
    /// Must be &lt; AbsoluteSectionIndex if non-null.
    /// </summary>
    public int? BaseReferenceSectionIndex { get; init; }

    /// <summary>
    /// Variation intensity [0..1] controlling how much this section should differ from its base.
    /// 0.0 = minimal variation (near-exact repeat).
    /// 1.0 = maximum variation (substantial bounded transforms).
    /// Only meaningful when BaseReferenceSectionIndex is non-null.
    /// </summary>
    public double VariationIntensity { get; init; }

    /// <summary>
    /// Per-role bounded variation controls.
    /// These deltas are applied *on top of* the base section's decisions and energy profile controls.
    /// </summary>
    public required VariationRoleDeltas Roles { get; init; }

    /// <summary>
    /// Small stable intent tags for diagnostics and later stage decisions.
    /// Common tags: "A" (first occurrence), "Aprime" (varied repeat), "B" (contrasting section),
    /// "Lift" (register lift), "Thin" (density reduction), "Breakdown" (tension without energy).
    /// </summary>
    public required IReadOnlySet<string> Tags { get; init; }

    /// <summary>
    /// Creates a neutral variation plan with no reuse and no deltas (new material, "A" or "B" tag).
    /// </summary>
    public static SectionVariationPlan NoReuse(int absoluteSectionIndex, string tag = "A")
    {
        return new SectionVariationPlan
        {
            AbsoluteSectionIndex = absoluteSectionIndex,
            BaseReferenceSectionIndex = null,
            VariationIntensity = 0.0,
            Roles = VariationRoleDeltas.Neutral(),
            Tags = new HashSet<string> { tag }
        };
    }

    /// <summary>
    /// Creates a variation plan that references a base section with specified intensity and role deltas.
    /// All numeric values are clamped to safe ranges.
    /// </summary>
    public static SectionVariationPlan WithReuse(
        int absoluteSectionIndex,
        int baseReferenceSectionIndex,
        double variationIntensity,
        VariationRoleDeltas? roleDeltas = null,
        IEnumerable<string>? tags = null)
    {
        // Validation: base reference must be earlier
        if (baseReferenceSectionIndex >= absoluteSectionIndex)
        {
            throw new ArgumentException(
                $"BaseReferenceSectionIndex ({baseReferenceSectionIndex}) must be < AbsoluteSectionIndex ({absoluteSectionIndex})",
                nameof(baseReferenceSectionIndex));
        }

        return new SectionVariationPlan
        {
            AbsoluteSectionIndex = absoluteSectionIndex,
            BaseReferenceSectionIndex = baseReferenceSectionIndex,
            VariationIntensity = Math.Clamp(variationIntensity, 0.0, 1.0),
            Roles = roleDeltas ?? VariationRoleDeltas.Neutral(),
            Tags = tags != null ? new HashSet<string>(tags) : new HashSet<string> { "Aprime" }
        };
    }

    /// <summary>
    /// Creates a copy of this plan with modified variation intensity.
    /// </summary>
    public SectionVariationPlan WithVariationIntensity(double newIntensity)
    {
        return this with { VariationIntensity = Math.Clamp(newIntensity, 0.0, 1.0) };
    }

    /// <summary>
    /// Creates a copy of this plan with modified role deltas.
    /// </summary>
    public SectionVariationPlan WithRoleDeltas(VariationRoleDeltas newDeltas)
    {
        return this with { Roles = newDeltas };
    }

    /// <summary>
    /// Creates a copy of this plan with an additional tag.
    /// </summary>
    public SectionVariationPlan WithTag(string tag)
    {
        var newTags = new HashSet<string>(Tags) { tag };
        return this with { Tags = newTags };
    }
}

/// <summary>
/// Container for per-role variation deltas.
/// All fields are optional (null = no change from base or energy profile).
/// When non-null, values are bounded and will be applied additively or multiplicatively
/// by role-specific parameter adapters (Story 7.6.5).
/// </summary>
public sealed record VariationRoleDeltas
{
    /// <summary>
    /// Bass role variation controls.
    /// </summary>
    public RoleVariationDelta? Bass { get; init; }

    /// <summary>
    /// Comp (guitar/rhythm) role variation controls.
    /// </summary>
    public RoleVariationDelta? Comp { get; init; }

    /// <summary>
    /// Keys role variation controls.
    /// </summary>
    public RoleVariationDelta? Keys { get; init; }

    /// <summary>
    /// Pads role variation controls.
    /// </summary>
    public RoleVariationDelta? Pads { get; init; }

    /// <summary>
    /// Drums role variation controls.
    /// </summary>
    public RoleVariationDelta? Drums { get; init; }

    /// <summary>
    /// Creates neutral role deltas with all roles set to null (no change).
    /// </summary>
    public static VariationRoleDeltas Neutral()
    {
        return new VariationRoleDeltas();
    }

    /// <summary>
    /// Creates role deltas with a specified delta applied to all roles.
    /// </summary>
    public static VariationRoleDeltas AllRoles(RoleVariationDelta delta)
    {
        return new VariationRoleDeltas
        {
            Bass = delta,
            Comp = delta,
            Keys = delta,
            Pads = delta,
            Drums = delta
        };
    }
}

/// <summary>
/// Bounded per-role variation controls.
/// Values are applied by parameter adapters (Story 7.6.5) with guardrail enforcement.
/// Null fields mean "no change" from base or energy profile.
/// </summary>
/// <remarks>
/// Application semantics (defined by parameter adapters in Story 7.6.5):
/// - DensityMultiplier: applied multiplicatively to existing density multiplier.
/// - VelocityBias: applied additively to existing velocity bias.
/// - RegisterLiftSemitones: applied additively to existing register lift.
/// - BusyProbability: applied additively to existing busy probability (clamped [0..1]).
/// </remarks>
public sealed record RoleVariationDelta
{
    /// <summary>
    /// Density multiplier delta [typically 0.7-1.3].
    /// Applied multiplicatively: finalDensity = baseDensity * thisDelta.
    /// Values outside [0.5, 2.0] will be clamped by parameter adapters.
    /// </summary>
    public double? DensityMultiplier { get; init; }

    /// <summary>
    /// Velocity bias delta in MIDI units [typically -10 to +10].
    /// Applied additively: finalVelocityBias = baseVelocityBias + thisDelta.
    /// Final velocity values are clamped to MIDI range [1-127] by role generators.
    /// </summary>
    public int? VelocityBias { get; init; }

    /// <summary>
    /// Register lift/drop delta in semitones [typically -12 to +12].
    /// Applied additively: finalRegisterLift = baseRegisterLift + thisDelta.
    /// Role-specific guardrails (bass range, lead space ceiling) enforced by adapters.
    /// </summary>
    public int? RegisterLiftSemitones { get; init; }

    /// <summary>
    /// Busy probability delta [typically -0.2 to +0.2].
    /// Applied additively: finalBusyProb = baseBusyProb + thisDelta.
    /// Final value clamped to [0..1] by parameter adapters.
    /// </summary>
    public double? BusyProbability { get; init; }

    /// <summary>
    /// Creates a neutral delta with all fields null (no change).
    /// </summary>
    public static RoleVariationDelta Neutral()
    {
        return new RoleVariationDelta();
    }

    /// <summary>
    /// Creates a delta with specified values, clamping to safe ranges.
    /// </summary>
    public static RoleVariationDelta Create(
        double? densityMultiplier = null,
        int? velocityBias = null,
        int? registerLiftSemitones = null,
        double? busyProbability = null)
    {
        return new RoleVariationDelta
        {
            DensityMultiplier = densityMultiplier.HasValue
                ? Math.Clamp(densityMultiplier.Value, 0.5, 2.0)
                : null,
            VelocityBias = velocityBias.HasValue
                ? Math.Clamp(velocityBias.Value, -30, 30)
                : null,
            RegisterLiftSemitones = registerLiftSemitones.HasValue
                ? Math.Clamp(registerLiftSemitones.Value, -24, 24)
                : null,
            BusyProbability = busyProbability.HasValue
                ? Math.Clamp(busyProbability.Value, -1.0, 1.0)
                : null
        };
    }

    /// <summary>
    /// Creates a "lift" delta that increases energy (higher register, louder, busier).
    /// </summary>
    public static RoleVariationDelta Lift()
    {
        return Create(
            densityMultiplier: 1.1,
            velocityBias: 5,
            registerLiftSemitones: 12,
            busyProbability: 0.1);
    }

    /// <summary>
    /// Creates a "thin" delta that reduces density (sparser, quieter).
    /// </summary>
    public static RoleVariationDelta Thin()
    {
        return Create(
            densityMultiplier: 0.8,
            velocityBias: -5,
            registerLiftSemitones: 0,
            busyProbability: -0.1);
    }
}
