// AI: purpose=Per-style velocity hint configuration for DrummerVelocityShaper; immutable settings record.
// AI: invariants=All velocity values in [1..127]; FillVelocityMin <= FillVelocityMax; defaults conservative.
// AI: deps=FillRampDirection enum; consumed by DrummerVelocityShaper; stored in StyleConfiguration.
// AI: change=Story 6.1; add additional settings as velocity hinting requirements evolve.

namespace Music.Generator.Agents.Drums.Performance
{
    /// <summary>
    /// Per-style velocity hint configuration for the DrummerVelocityShaper.
    /// Defines target velocities for each dynamic intent and fill ramp behavior.
    /// Story 6.1: Implement Drummer Velocity Shaper.
    /// </summary>
    public sealed record DrummerVelocityHintSettings
    {
        /// <summary>
        /// Target velocity for ghost notes (DynamicIntent.Low).
        /// Default: 35 (soft but audible).
        /// </summary>
        public int GhostVelocityTarget { get; init; } = 35;

        /// <summary>
        /// Target velocity for medium-low intensity (DynamicIntent.MediumLow).
        /// Default: 60.
        /// </summary>
        public int MediumLowVelocityTarget { get; init; } = 60;

        /// <summary>
        /// Target velocity for standard groove hits (DynamicIntent.Medium).
        /// Default: 80.
        /// </summary>
        public int MediumVelocityTarget { get; init; } = 80;

        /// <summary>
        /// Target velocity for strong accents/backbeats (DynamicIntent.StrongAccent).
        /// Default: 100.
        /// </summary>
        public int BackbeatVelocityTarget { get; init; } = 100;

        /// <summary>
        /// Target velocity for peak accents/crashes (DynamicIntent.PeakAccent).
        /// Default: 115.
        /// </summary>
        public int CrashVelocityTarget { get; init; } = 115;

        /// <summary>
        /// Minimum velocity for fill ramp patterns.
        /// Default: 60.
        /// </summary>
        public int FillVelocityMin { get; init; } = 60;

        /// <summary>
        /// Maximum velocity for fill ramp patterns.
        /// Default: 110.
        /// </summary>
        public int FillVelocityMax { get; init; } = 110;

        /// <summary>
        /// Direction of velocity ramp for fill patterns.
        /// Default: Ascending (builds to climax).
        /// </summary>
        public FillRampDirection FillRampDirection { get; init; } = FillRampDirection.Ascending;

        /// <summary>
        /// Maximum velocity adjustment when existing hint is present.
        /// Preserves operator intent while nudging toward style target.
        /// Default: 10 velocity units.
        /// </summary>
        public int MaxAdjustmentDelta { get; init; } = 10;

        /// <summary>
        /// Default conservative settings for Pop/Rock style.
        /// </summary>
        public static DrummerVelocityHintSettings PopRockDefaults => new()
        {
            GhostVelocityTarget = 35,
            MediumLowVelocityTarget = 60,
            MediumVelocityTarget = 80,
            BackbeatVelocityTarget = 105,
            CrashVelocityTarget = 115,
            FillVelocityMin = 65,
            FillVelocityMax = 110,
            FillRampDirection = FillRampDirection.Ascending,
            MaxAdjustmentDelta = 10
        };

        /// <summary>
        /// Default settings for Jazz style (more dynamic range, softer overall).
        /// </summary>
        public static DrummerVelocityHintSettings JazzDefaults => new()
        {
            GhostVelocityTarget = 30,
            MediumLowVelocityTarget = 50,
            MediumVelocityTarget = 70,
            BackbeatVelocityTarget = 90,
            CrashVelocityTarget = 105,
            FillVelocityMin = 55,
            FillVelocityMax = 95,
            FillRampDirection = FillRampDirection.Ascending,
            MaxAdjustmentDelta = 8
        };

        /// <summary>
        /// Default settings for Metal style (harder, more consistent dynamics).
        /// </summary>
        public static DrummerVelocityHintSettings MetalDefaults => new()
        {
            GhostVelocityTarget = 45,
            MediumLowVelocityTarget = 70,
            MediumVelocityTarget = 90,
            BackbeatVelocityTarget = 115,
            CrashVelocityTarget = 125,
            FillVelocityMin = 80,
            FillVelocityMax = 120,
            FillRampDirection = FillRampDirection.Ascending,
            MaxAdjustmentDelta = 12
        };

        /// <summary>
        /// Conservative fallback defaults when no style-specific settings exist.
        /// </summary>
        public static DrummerVelocityHintSettings ConservativeDefaults => new();

        /// <summary>
        /// Gets the target velocity for a given dynamic intent.
        /// </summary>
        public int GetTargetVelocity(DynamicIntent intent)
        {
            return intent switch
            {
                DynamicIntent.Low => GhostVelocityTarget,
                DynamicIntent.MediumLow => MediumLowVelocityTarget,
                DynamicIntent.Medium => MediumVelocityTarget,
                DynamicIntent.StrongAccent => BackbeatVelocityTarget,
                DynamicIntent.PeakAccent => CrashVelocityTarget,
                DynamicIntent.FillRampStart => ComputeFillRampVelocity(0.0),
                DynamicIntent.FillRampBody => ComputeFillRampVelocity(0.5),
                DynamicIntent.FillRampEnd => ComputeFillRampVelocity(1.0),
                _ => MediumVelocityTarget
            };
        }

        /// <summary>
        /// Computes velocity for fill ramp based on position (0.0 = start, 1.0 = end).
        /// </summary>
        public int ComputeFillRampVelocity(double position)
        {
            position = Math.Clamp(position, 0.0, 1.0);

            return FillRampDirection switch
            {
                FillRampDirection.Ascending => (int)Math.Round(
                    FillVelocityMin + (FillVelocityMax - FillVelocityMin) * position),
                FillRampDirection.Descending => (int)Math.Round(
                    FillVelocityMax - (FillVelocityMax - FillVelocityMin) * position),
                FillRampDirection.Flat => (FillVelocityMin + FillVelocityMax) / 2,
                _ => MediumVelocityTarget
            };
        }

        /// <summary>
        /// Validates that all velocity values are in valid MIDI range [1..127].
        /// </summary>
        public bool IsValid(out string? errorMessage)
        {
            if (GhostVelocityTarget < 1 || GhostVelocityTarget > 127)
            {
                errorMessage = $"GhostVelocityTarget must be in [1..127], was {GhostVelocityTarget}";
                return false;
            }

            if (MediumLowVelocityTarget < 1 || MediumLowVelocityTarget > 127)
            {
                errorMessage = $"MediumLowVelocityTarget must be in [1..127], was {MediumLowVelocityTarget}";
                return false;
            }

            if (MediumVelocityTarget < 1 || MediumVelocityTarget > 127)
            {
                errorMessage = $"MediumVelocityTarget must be in [1..127], was {MediumVelocityTarget}";
                return false;
            }

            if (BackbeatVelocityTarget < 1 || BackbeatVelocityTarget > 127)
            {
                errorMessage = $"BackbeatVelocityTarget must be in [1..127], was {BackbeatVelocityTarget}";
                return false;
            }

            if (CrashVelocityTarget < 1 || CrashVelocityTarget > 127)
            {
                errorMessage = $"CrashVelocityTarget must be in [1..127], was {CrashVelocityTarget}";
                return false;
            }

            if (FillVelocityMin < 1 || FillVelocityMin > 127)
            {
                errorMessage = $"FillVelocityMin must be in [1..127], was {FillVelocityMin}";
                return false;
            }

            if (FillVelocityMax < 1 || FillVelocityMax > 127)
            {
                errorMessage = $"FillVelocityMax must be in [1..127], was {FillVelocityMax}";
                return false;
            }

            if (FillVelocityMin > FillVelocityMax)
            {
                errorMessage = $"FillVelocityMin ({FillVelocityMin}) cannot exceed FillVelocityMax ({FillVelocityMax})";
                return false;
            }

            if (MaxAdjustmentDelta < 0)
            {
                errorMessage = $"MaxAdjustmentDelta must be >= 0, was {MaxAdjustmentDelta}";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
