// AI: purpose=Drummer policy provider implementing IGroovePolicyProvider; computes per-bar policy decisions from style + context + memory.
// AI: invariants=Deterministic: same inputs → same GroovePolicyDecision; read-only access to memory; never mutates inputs.
// AI: deps=IGroovePolicyProvider, GroovePolicyDecision, StyleConfiguration, IAgentMemory, DrummerContext, MotifPresenceMap (Story 9.3).
// AI: change=Story 2.3, 9.3; extend with additional override logic as operators are implemented.

using Music.Generator.Agents.Common;
using Music.Generator.Material;

namespace Music.Generator.Agents.Drums
{
    /// <summary>
    /// Known operator IDs for fill-related operators (Story 3.3).
    /// Used to gate fill operators based on IsFillWindow and memory state.
    /// </summary>
    public static class FillOperatorIds
    {
        public const string TurnaroundFillShort = "TurnaroundFillShort";
        public const string TurnaroundFillFull = "TurnaroundFillFull";
        public const string BuildFill = "BuildFill";
        public const string DropFill = "DropFill";
        public const string SetupHit = "SetupHit";
        public const string StopTime = "StopTime";
        public const string CrashOnOne = "CrashOnOne";

        /// <summary>All fill operator IDs for lookup.</summary>
        public static readonly IReadOnlySet<string> All = new HashSet<string>
        {
            TurnaroundFillShort,
            TurnaroundFillFull,
            BuildFill,
            DropFill,
            SetupHit,
            StopTime,
            CrashOnOne
        };
    }

    /// <summary>
    /// Drummer agent policy provider that computes per-bar policy decisions.
    /// Implements IGroovePolicyProvider to drive groove system behavior from drummer context.
    /// Story 2.3: Implement Drummer Policy Provider.
    /// Story 9.3: Motif-aware density reduction and variation tags.
    /// </summary>
    /// <remarks>
    /// Precedence order for overrides: immediate context > memory > style config.
    /// All computations are deterministic and read-only.
    /// </remarks>
    public sealed class DrummerPolicyProvider : IGroovePolicyProvider
    {
        private readonly StyleConfiguration _styleConfig;
        private readonly IAgentMemory? _memory;
        private readonly DrummerPolicySettings _settings;
        private readonly MotifPresenceMap? _motifPresenceMap;

        /// <summary>
        /// Creates a drummer policy provider with the specified configuration.
        /// </summary>
        /// <param name="styleConfig">Style configuration (PopRock, Jazz, etc.).</param>
        /// <param name="memory">Agent memory for anti-repetition (optional, null = no memory influence).</param>
        /// <param name="settings">Policy settings for density modifiers and lookback (optional, uses defaults).</param>
        /// <param name="motifPresenceMap">Motif presence map for ducking (optional, null = no motif awareness). Story 9.3.</param>
        public DrummerPolicyProvider(
            StyleConfiguration styleConfig,
            IAgentMemory? memory = null,
            DrummerPolicySettings? settings = null,
            MotifPresenceMap? motifPresenceMap = null)
        {
            ArgumentNullException.ThrowIfNull(styleConfig);
            _styleConfig = styleConfig;
            _memory = memory;
            _settings = settings ?? DrummerPolicySettings.Default;
            _motifPresenceMap = motifPresenceMap;
        }

        /// <inheritdoc />
        public GroovePolicyDecision? GetPolicy(GrooveBarContext barContext, string role)
        {
            ArgumentNullException.ThrowIfNull(barContext);
            ArgumentNullException.ThrowIfNull(role);

            // Unknown role: return no overrides (safe fallback)
            if (!IsDrumRole(role))
                return GroovePolicyDecision.NoOverrides;

            // Compute density override from energy + section type
            double density01 = ComputeDensityOverride(barContext);

            // Compute max events per bar from style caps
            int? maxEventsPerBar = ComputeMaxEventsOverride(role);

            // Compute operator allow list based on context + memory
            var operatorAllowList = ComputeOperatorAllowList(barContext, role);

            // Compute timing feel override (style-driven)
            TimingFeel? timingFeel = ComputeTimingFeelOverride(role);

            // Compute velocity bias override (energy-driven)
            int? velocityBias = ComputeVelocityBiasOverride(barContext);

            // Compute enabled variation tags based on context
            var variationTags = ComputeVariationTagsOverride(barContext);

            return new GroovePolicyDecision
            {
                Density01Override = density01,
                MaxEventsPerBarOverride = maxEventsPerBar,
                OperatorAllowList = operatorAllowList,
                RoleTimingFeelOverride = timingFeel,
                VelocityBiasOverride = velocityBias,
                EnabledVariationTagsOverride = variationTags
            };
        }

        /// <summary>
        /// Computes density override from energy level and section type.
        /// Uses style default density as base, then applies energy modifier.
        /// Story 9.3: Applies motif-based density reduction when motif is active.
        /// </summary>
        private double ComputeDensityOverride(GrooveBarContext barContext)
        {
            // Get section type from bar context
            var sectionType = barContext.Section?.SectionType ?? MusicConstants.eSectionType.Verse;

            // Get base density from style config (role-agnostic section density)
            double baseDensity = GetSectionBaseDensity(sectionType);

            // Apply energy modifier: higher energy increases density
            // EnergyLevel is computed from context upstream; for now we use section-based defaults
            // until DrummerContext is passed (integration with Story 2.4)
            double energyModifier = GetEnergyModifier(sectionType);
            double adjustedDensity = baseDensity + (energyModifier * _settings.EnergyDensityScale);

            // Story 9.3: Apply motif-based density reduction (bounded)
            if (_motifPresenceMap != null && _motifPresenceMap.IsMotifActive(barContext.BarNumber))
            {
                // Compute reduction based on motif density (0.5 for one motif, 1.0 for two or more)
                double motifDensity = _motifPresenceMap.GetMotifDensity(barContext.BarNumber);
                
                // Scale the reduction by motif density: more motifs = more reduction, up to the configured max
                double reductionFactor = _settings.MotifDensityReductionPercent * motifDensity / 0.5;
                reductionFactor = Math.Min(reductionFactor, _settings.MaxMotifDensityReduction);
                
                adjustedDensity *= (1.0 - reductionFactor);
            }

            // Clamp to valid range [0.0, 1.0]
            return Math.Clamp(adjustedDensity, 0.0, 1.0);
        }

        /// <summary>
        /// Gets base density for a section type (from style or defaults).
        /// </summary>
        private double GetSectionBaseDensity(MusicConstants.eSectionType sectionType)
        {
            return sectionType switch
            {
                MusicConstants.eSectionType.Intro => 0.4,
                MusicConstants.eSectionType.Verse => 0.5,
                MusicConstants.eSectionType.Chorus => 0.8,
                MusicConstants.eSectionType.Solo => 0.7,
                MusicConstants.eSectionType.Bridge => 0.4,
                MusicConstants.eSectionType.Outro => 0.5,
                _ => StyleConfiguration.DefaultRoleDensity
            };
        }

        /// <summary>
        /// Gets energy modifier for a section type (chorus = higher, bridge = lower).
        /// </summary>
        private static double GetEnergyModifier(MusicConstants.eSectionType sectionType)
        {
            return sectionType switch
            {
                MusicConstants.eSectionType.Chorus => 0.2,
                MusicConstants.eSectionType.Solo => 0.1,
                MusicConstants.eSectionType.Bridge => -0.1,
                MusicConstants.eSectionType.Intro => -0.1,
                MusicConstants.eSectionType.Outro => -0.1,
                _ => 0.0
            };
        }

        /// <summary>
        /// Computes max events per bar from style role caps.
        /// </summary>
        private int? ComputeMaxEventsOverride(string role)
        {
            int cap = _styleConfig.GetRoleCap(role);
            return cap < int.MaxValue ? cap : null;
        }

        /// <summary>
        /// Computes operator allow list based on context and memory.
        /// Fill operators are gated by IsFillWindow and memory state.
        /// </summary>
        private List<string>? ComputeOperatorAllowList(GrooveBarContext barContext, string role)
        {
            // Get all allowed operators from style
            var allowedByStyle = _styleConfig.AllowedOperatorIds;

            // If style allows all (empty list), return null to allow all
            if (allowedByStyle.Count == 0)
            {
                // Still need to gate fills based on context
                return ComputeFillGatedOperatorList(barContext);
            }

            // Start with style-allowed operators
            var result = new List<string>(allowedByStyle);

            // Apply fill gating
            if (!ShouldAllowFills(barContext))
            {
                result.RemoveAll(id => FillOperatorIds.All.Contains(id));
            }

            return result.Count > 0 ? result : null;
        }

        /// <summary>
        /// Computes fill-gated operator list when style allows all.
        /// Returns null if fills are allowed; returns list excluding fills if not.
        /// </summary>
        private List<string>? ComputeFillGatedOperatorList(GrooveBarContext barContext)
        {
            if (ShouldAllowFills(barContext))
                return null; // All operators allowed

            // Return explicit exclusion by not including fill operators
            // Since we can't enumerate all operators, return null and let downstream handle
            // Actually: the contract says empty list = all allowed, so we need to track excluded fills differently
            // For now, return null since we don't have complete operator list yet
            // Fill gating will be enforced when operators exist (Story 3.x)
            return null;
        }

        /// <summary>
        /// Determines whether fill operators should be allowed for this bar.
        /// Fills are allowed when: IsFillWindow AND memory doesn't disallow.
        /// </summary>
        private bool ShouldAllowFills(GrooveBarContext barContext)
        {
            // Check if in fill window (determined by PhraseHookWindowResolver upstream)
            // For now, use BarsUntilSectionEnd as proxy: fills allowed in last 1-2 bars
            bool inFillWindow = barContext.BarsUntilSectionEnd <= _settings.FillWindowBars;

            if (!inFillWindow)
                return false;

            // Check memory for recent fills
            if (_memory != null && _settings.AllowConsecutiveFills == false)
            {
                var lastFill = _memory.GetLastFillShape();
                if (lastFill != null && lastFill.HasContent)
                {
                    int barsSinceLastFill = barContext.BarNumber - lastFill.BarPosition;
                    if (barsSinceLastFill < _settings.MinBarsBetweenFills)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Computes timing feel override based on style and role.
        /// </summary>
        private TimingFeel? ComputeTimingFeelOverride(string role)
        {
            // Pop Rock specific: snare slightly behind for laid-back feel
            if (_styleConfig.StyleId == "PopRock" && role == GrooveRoles.Snare)
                return TimingFeel.Behind;

            return null; // Use default timing
        }

        /// <summary>
        /// Computes velocity bias override based on energy.
        /// Higher energy = positive bias (louder), lower energy = negative bias.
        /// </summary>
        private int? ComputeVelocityBiasOverride(GrooveBarContext barContext)
        {
            var sectionType = barContext.Section?.SectionType ?? MusicConstants.eSectionType.Verse;

            // Section-based energy bias
            int bias = sectionType switch
            {
                MusicConstants.eSectionType.Chorus => 10,
                MusicConstants.eSectionType.Solo => 5,
                MusicConstants.eSectionType.Bridge => -10,
                MusicConstants.eSectionType.Intro => -5,
                MusicConstants.eSectionType.Outro => -5,
                _ => 0
            };

            return bias != 0 ? bias : null;
        }

        /// <summary>
        /// Computes enabled variation tags based on context.
        /// Section boundaries enable punctuation tags; fills enable fill tags.
        /// Story 9.3: Adds "MotifPresent" tag when a motif is active in the bar.
        /// </summary>
        private List<string>? ComputeVariationTagsOverride(GrooveBarContext barContext)
        {
            var tags = new List<string>();

            // Story 9.3: Add MotifPresent tag when motif is active
            if (_motifPresenceMap != null && _motifPresenceMap.IsMotifActive(barContext.BarNumber))
            {
                tags.Add("MotifPresent");
            }

            // Enable fill tags when in fill window
            if (ShouldAllowFills(barContext))
            {
                tags.Add("Fill");
                tags.Add("TurnAround");
            }

            // Enable punctuation at section start
            if (barContext.BarWithinSection == 0)
            {
                tags.Add("SectionStart");
                tags.Add("Crash");
            }

            // Enable buildup tags near section end
            if (barContext.BarsUntilSectionEnd <= 2)
            {
                tags.Add("Buildup");
                tags.Add("SectionEnd");
            }

            return tags.Count > 0 ? tags : null;
        }

        /// <summary>
        /// Checks if a role is a known drum role.
        /// </summary>
        private static bool IsDrumRole(string role)
        {
            return role == GrooveRoles.Kick ||
                   role == GrooveRoles.Snare ||
                   role == GrooveRoles.ClosedHat ||
                   role == GrooveRoles.OpenHat ||
                   role == "Crash" ||
                   role == "Ride" ||
                   role == "Tom1" ||
                   role == "Tom2" ||
                   role == "FloorTom";
        }
    }

    /// <summary>
    /// Configurable settings for DrummerPolicyProvider behavior.
    /// Story 9.3: Added motif density reduction settings.
    /// </summary>
    public sealed record DrummerPolicySettings
    {
        /// <summary>How much energy affects density (default 0.2 = ±20%).</summary>
        public double EnergyDensityScale { get; init; } = 0.2;

        /// <summary>Number of bars from section end that count as fill window (default 2).</summary>
        public int FillWindowBars { get; init; } = 2;

        /// <summary>Whether consecutive fills are allowed (default false).</summary>
        public bool AllowConsecutiveFills { get; init; } = false;

        /// <summary>Minimum bars between fills (default 4).</summary>
        public int MinBarsBetweenFills { get; init; } = 4;

        /// <summary>
        /// Density reduction percentage when a motif is active (default 0.15 = 15%).
        /// Story 9.3: Reduces density target to make room for motifs.
        /// </summary>
        public double MotifDensityReductionPercent { get; init; } = 0.15;

        /// <summary>
        /// Maximum density reduction from motif presence (default 0.20 = 20%).
        /// Story 9.3: Caps reduction to avoid over-thinning.
        /// </summary>
        public double MaxMotifDensityReduction { get; init; } = 0.20;

        /// <summary>Default settings instance.</summary>
        public static DrummerPolicySettings Default => new();
    }
}
