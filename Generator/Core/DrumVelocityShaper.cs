// AI: purpose=Deterministic velocity shaping for drums (Story 6.2); provides hand patterns, accents, ghost note velocities.
// AI: invariants=All velocity values deterministic for (seed, role, bar, onset); clamped to MIDI 1..127 range.
// AI: deps=RandomHelpers for seeding, MusicConstants.eSectionType for energy context.
// AI: perf=Called per drum hit; keep lightweight.

namespace Music.Generator
{
    /// <summary>
    /// Provides deterministic velocity shaping for drum hits to create dynamic expression.
    /// Implements Story 6.2 acceptance criteria for velocity humanization layer.
    /// </summary>
    internal static class DrumVelocityShaper
    {
        /// <summary>
        /// Shapes velocity for a drum hit with accents, hand patterns, and dynamic context.
        /// </summary>
        /// <param name="role">Drum role: "kick", "snare", "hat", "ride"</param>
        /// <param name="baseVelocity">Starting velocity value</param>
        /// <param name="bar">Bar number</param>
        /// <param name="onsetBeat">Beat position in bar</param>
        /// <param name="seed">Randomization seed</param>
        /// <param name="sectionType">Section type for energy context</param>
        /// <param name="isStrongBeat">True if this is a strong beat</param>
        /// <param name="isGhost">True if this is a ghost note</param>
        /// <param name="isInFill">True if this hit is part of a fill</param>
        /// <param name="fillProgress">Fill progress 0.0 to 1.0 for crescendo (0 if not in fill)</param>
        /// <returns>Shaped velocity (1..127)</returns>
        public static int ShapeVelocity(
            string role,
            int baseVelocity,
            int bar,
            decimal onsetBeat,
            int seed,
            MusicConstants.eSectionType sectionType,
            bool isStrongBeat,
            bool isGhost = false,
            bool isInFill = false,
            double fillProgress = 0.0)
        {
            if (isGhost)
            {
                // Ghost notes are consistently quiet
                return ClampVelocity((int)(baseVelocity * 0.3));
            }

            var rng = RandomHelpers.CreateLocalRng(seed, $"velocity_{role}", bar, onsetBeat);

            int velocity = baseVelocity;

            // Apply fill crescendo
            if (isInFill && fillProgress > 0)
            {
                // Crescendo: increase velocity as fill progresses
                int crescendo = (int)(15 * fillProgress); // Up to +15 velocity
                velocity += crescendo;
            }

            // Apply hand pattern accents for hat/ride
            if (role == "hat" || role == "ride")
            {
                velocity = ApplyHandPatternAccent(velocity, onsetBeat, sectionType, isStrongBeat);
            }

            // Apply section energy boost
            velocity = ApplySectionEnergyBoost(velocity, sectionType, role);

            // Add small humanization variation (±5%)
            int variation = (int)(velocity * 0.05);
            velocity += rng.NextInt(-variation, variation + 1);

            return ClampVelocity(velocity);
        }

        /// <summary>
        /// Applies hand pattern accents to hi-hat and ride velocities.
        /// Accents strong beats (2 and 4 in 4/4) for backbeat emphasis.
        /// </summary>
        private static int ApplyHandPatternAccent(
            int velocity,
            decimal onsetBeat,
            MusicConstants.eSectionType sectionType,
            bool isStrongBeat)
        {
            // Determine beat position
            int beatNumber = (int)Math.Floor(onsetBeat);
            bool isOffbeat = onsetBeat % 1m != 0m;

            // Backbeat accent (beats 2 and 4)
            if (beatNumber == 2 || beatNumber == 4)
            {
                velocity += 12;
            }
            // Downbeat accent (beat 1)
            else if (beatNumber == 1 && isStrongBeat)
            {
                velocity += 8;
            }
            // Offbeat accent pattern (for funk/disco feel)
            else if (isOffbeat && (sectionType == MusicConstants.eSectionType.Chorus))
            {
                // Accent offbeats in chorus for more energy
                velocity += 5;
            }

            return velocity;
        }

        /// <summary>
        /// Applies section-specific energy boost to velocity.
        /// Higher energy sections get louder drums.
        /// </summary>
        private static int ApplySectionEnergyBoost(
            int velocity,
            MusicConstants.eSectionType sectionType,
            string role)
        {
            int boost = sectionType switch
            {
                MusicConstants.eSectionType.Chorus => role switch
                {
                    "kick" => 10,
                    "snare" => 12,
                    "hat" => 8,
                    "ride" => 10,
                    _ => 5
                },
                MusicConstants.eSectionType.Bridge => role switch
                {
                    "kick" => 8,
                    "snare" => 10,
                    "hat" => 6,
                    "ride" => 8,
                    _ => 4
                },
                MusicConstants.eSectionType.Solo => role switch
                {
                    "kick" => 7,
                    "snare" => 9,
                    "hat" => 7,
                    "ride" => 9,
                    _ => 5
                },
                MusicConstants.eSectionType.Verse => 0, // No boost
                MusicConstants.eSectionType.Intro => -5, // Slightly quieter
                MusicConstants.eSectionType.Outro => -8, // Fade out feel
                _ => 0
            };

            return velocity + boost;
        }

        /// <summary>
        /// Calculates velocity for a ghost note (consistently quiet).
        /// </summary>
        public static int GhostNoteVelocity(int baseVelocity, int seed, int bar, decimal onset)
        {
            var rng = RandomHelpers.CreateLocalRng(seed, "ghost", bar, onset);
            
            // Ghost notes: 25-35% of base velocity with slight variation
            int velocity = (int)(baseVelocity * 0.30);
            int variation = (int)(velocity * 0.15);
            velocity += rng.NextInt(-variation, variation + 1);
            
            return ClampVelocity(velocity);
        }

        /// <summary>
        /// Calculates velocity for a flam pre-hit (softer than main hit).
        /// </summary>
        public static int FlamPreHitVelocity(int mainHitVelocity, int seed, int bar, decimal onset)
        {
            var rng = RandomHelpers.CreateLocalRng(seed, "flam", bar, onset);
            
            // Flam pre-hit: 55-65% of main hit velocity
            int velocity = (int)(mainHitVelocity * 0.60);
            int variation = (int)(velocity * 0.10);
            velocity += rng.NextInt(-variation, variation + 1);
            
            return ClampVelocity(velocity);
        }

        /// <summary>
        /// Clamps velocity to valid MIDI range (1..127).
        /// </summary>
        private static int ClampVelocity(int velocity)
        {
            return Math.Clamp(velocity, 1, 127);
        }
    }
}
