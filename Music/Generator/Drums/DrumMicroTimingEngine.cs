// AI: purpose=Deterministic micro-timing offsets for drums (Story 6.2); provides groove-style-aware timing feel.
// AI: invariants=All timing offsets deterministic for (seed, role, bar, onset); max deviation clamped to prevent bar boundary breaks.
// AI: deps=RandomHelpers for seeding, MusicConstants for timing units.
// AI: perf=Called per drum hit; keep lightweight.

namespace Music.Generator
{
    /// <summary>
    /// Provides deterministic micro-timing offsets for drum hits to create human feel.
    /// Implements Story 6.2 acceptance criteria for timing humanization layer.
    /// </summary>
    internal static class DrumMicroTimingEngine
    {
        // Max tick deviations per role (prevent bar boundary breaks)
        private const int MaxKickOffsetTicks = 8;
        private const int MaxSnareOffsetTicks = 6;
        private const int MaxHatOffsetTicks = 5;
        private const int MaxRideOffsetTicks = 4;

        /// <summary>
        /// Calculates deterministic micro-timing offset for a drum hit.
        /// Returns offset in ticks (can be negative for laid-back feel).
        /// </summary>
        /// <param name="role">Drum role: "kick", "snare", "hat", "ride"</param>
        /// <param name="grooveStyle">Groove style name for style-specific feel</param>
        /// <param name="bar">Bar number</param>
        /// <param name="onsetBeat">Beat position in bar</param>
        /// <param name="seed">Randomization seed</param>
        /// <param name="isStrongBeat">True if this is a strong beat (on the grid)</param>
        /// <returns>Timing offset in ticks (negative = earlier, positive = later)</returns>
        public static int GetTimingOffset(
            string role, 
            string grooveStyle, 
            int bar, 
            decimal onsetBeat, 
            int seed,
            bool isStrongBeat)
        {
            var rng = RandomHelpersOld.CreateLocalRng(seed, $"timing_{role}", bar, onsetBeat);

            int maxOffset = role switch
            {
                "kick" => MaxKickOffsetTicks,
                "snare" => MaxSnareOffsetTicks,
                "hat" => MaxHatOffsetTicks,
                "ride" => MaxRideOffsetTicks,
                _ => MaxHatOffsetTicks
            };

            // Determine style-specific timing feel
            var styleFeel = GetStyleFeel(grooveStyle);

            // Strong beats stay closer to grid
            if (isStrongBeat)
            {
                maxOffset = (int)(maxOffset * 0.6);
            }

            // Apply style-specific bias
            int offset;
            switch (styleFeel)
            {
                case TimingFeel.Ahead:
                    // Hats slightly ahead, kick/snare closer to grid
                    if (role == "hat" || role == "ride")
                    {
                        // Bias toward negative (earlier)
                        offset = rng.NextInt(-maxOffset, maxOffset / 2);
                    }
                    else
                    {
                        offset = rng.NextInt(-maxOffset / 2, maxOffset / 2);
                    }
                    break;

                case TimingFeel.Behind:
                    // Laid-back feel: bias toward positive (later)
                    offset = rng.NextInt(-maxOffset / 3, maxOffset);
                    break;

                case TimingFeel.PushPull:
                    // Alternating push/pull based on beat
                    bool pushBeat = ((int)onsetBeat) % 2 == 0;
                    if (pushBeat)
                    {
                        offset = rng.NextInt(-maxOffset, -maxOffset / 3); // Push
                    }
                    else
                    {
                        offset = rng.NextInt(maxOffset / 3, maxOffset); // Pull
                    }
                    break;

                case TimingFeel.Tight:
                default:
                    // Minimal deviation, very close to grid
                    offset = rng.NextInt(-maxOffset / 2, maxOffset / 2);
                    break;
            }

            return offset;
        }

        /// <summary>
        /// Timing feel characteristics for different groove styles.
        /// </summary>
        private enum TimingFeel
        {
            Tight,      // Minimal deviation, precise
            Ahead,      // Hats push slightly ahead
            Behind,     // Laid-back, slightly behind the beat
            PushPull    // Controlled push-pull alternation
        }

        /// <summary>
        /// Maps groove style names to timing feel characteristics.
        /// </summary>
        private static TimingFeel GetStyleFeel(string grooveStyle)
        {
            // Normalize style name
            string style = (grooveStyle ?? "").Trim().ToLowerInvariant();

            // Map common groove patterns to timing feels
            if (style.Contains("rock") || style.Contains("metal"))
                return TimingFeel.Tight;
            
            if (style.Contains("funk") || style.Contains("disco"))
                return TimingFeel.Ahead;
            
            if (style.Contains("blues") || style.Contains("jazz") || style.Contains("shuffle"))
                return TimingFeel.Behind;
            
            if (style.Contains("reggae") || style.Contains("dub"))
                return TimingFeel.Behind;

            // Default to tight timing
            return TimingFeel.Tight;
        }
    }
}
