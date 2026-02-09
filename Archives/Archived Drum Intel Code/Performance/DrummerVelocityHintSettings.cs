namespace Music.Generator.Drums.Performance
{
    // AI: purpose=Per-style velocity hint settings for DrummerVelocityShaper; immutable record.
    // AI: invariants=Velocities in [1..127]; FillVelocityMin <= FillVelocityMax; MaxAdjustmentDelta >= 0.
    // AI: deps=FillRampDirection enum; persisted in StyleConfiguration; consumed by Shaper.
    public sealed record DrummerVelocityHintSettings
    {
        public int GhostVelocityTarget { get; init; } = 35;
        public int MediumLowVelocityTarget { get; init; } = 60;
        public int MediumVelocityTarget { get; init; } = 80;
        public int BackbeatVelocityTarget { get; init; } = 100;
        public int CrashVelocityTarget { get; init; } = 115;
        public int FillVelocityMin { get; init; } = 60;
        public int FillVelocityMax { get; init; } = 110;
        public FillRampDirection FillRampDirection { get; init; } = FillRampDirection.Ascending;
        public int MaxAdjustmentDelta { get; init; } = 10;

        // Preset: Pop/Rock conservative defaults
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

        // Preset: Jazz defaults (softer, wider pocket)
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

        // Preset: Metal defaults (harder, consistent dynamics)
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

        public static DrummerVelocityHintSettings ConservativeDefaults => new();

        // Get target velocity for a dynamic intent; FillRamp mapped to ramp positions.
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

        // Compute fill ramp velocity for position [0.0..1.0] according to FillRampDirection.
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

        // Validate velocity ranges and invariants: MIDI range and min<=max and non-negative adjustments.
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
