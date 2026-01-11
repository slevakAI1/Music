// AI: purpose=Per-role energy controls mapping energy targets to musical levers (dynamics, density, register, rhythmic activity).
// AI: invariants=DensityMultiplier>=0 (typically 0.5-2.0); VelocityBias in MIDI range; RegisterLiftSemitones typically -24 to +24; BusyProbability [0..1].
// AI: deps=Used by EnergySectionProfile; consumed by role generators (Bass, Comp, Keys, Pads, Drums) to apply energy intent.

namespace Music.Generator
{
    /// <summary>
    /// Energy-driven controls for a single role (Bass, Comp, Keys, Pads, or Drums).
    /// Maps high-level energy intent to concrete musical parameters.
    /// </summary>
    public sealed class EnergyRoleProfile
    {
        /// <summary>
        /// Density multiplier affecting note count/activity [typically 0.5-2.0].
        /// 1.0 = baseline density from pattern/groove.
        /// Higher values = more notes/activity; lower values = sparser.
        /// </summary>
        public double DensityMultiplier { get; init; } = 1.0;

        /// <summary>
        /// Velocity bias applied to MIDI velocity values [typically -20 to +20].
        /// Positive = louder; negative = softer.
        /// Applied additively to baseline velocity, clamped to MIDI range [1-127].
        /// </summary>
        public int VelocityBias { get; init; } = 0;

        /// <summary>
        /// Register lift/drop in semitones [typically -24 to +24].
        /// Positive = higher register (octave up = +12).
        /// Negative = lower register.
        /// Role-specific guardrails prevent violations (bass range, lead space ceiling, etc.).
        /// </summary>
        public int RegisterLiftSemitones { get; init; } = 0;

        /// <summary>
        /// Probability [0..1] of "busy" variations (fills, ghost notes, embellishments).
        /// 0.0 = minimal activity (stick to core pattern).
        /// 1.0 = maximum activity (frequent variations).
        /// Interpretation is role-specific:
        /// - Drums: ghost note frequency, fill probability
        /// - Bass: approach note frequency, octave pops
        /// - Comp: anticipation rate, extra fragments
        /// - Keys/Pads: arpeggiation, color tone additions
        /// </summary>
        public double BusyProbability { get; init; } = 0.5;

        /// <summary>
        /// Creates a neutral role profile with default/baseline values.
        /// </summary>
        public static EnergyRoleProfile Neutral()
        {
            return new EnergyRoleProfile
            {
                DensityMultiplier = 1.0,
                VelocityBias = 0,
                RegisterLiftSemitones = 0,
                BusyProbability = 0.5
            };
        }

        /// <summary>
        /// Creates a low-energy role profile (reduced activity).
        /// </summary>
        public static EnergyRoleProfile LowEnergy()
        {
            return new EnergyRoleProfile
            {
                DensityMultiplier = 0.7,
                VelocityBias = -10,
                RegisterLiftSemitones = 0,
                BusyProbability = 0.2
            };
        }

        /// <summary>
        /// Creates a high-energy role profile (increased activity).
        /// </summary>
        public static EnergyRoleProfile HighEnergy()
        {
            return new EnergyRoleProfile
            {
                DensityMultiplier = 1.4,
                VelocityBias = 10,
                RegisterLiftSemitones = 0,
                BusyProbability = 0.8
            };
        }
    }
}
