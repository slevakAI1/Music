// AI: purpose=Applies CompBehavior to onset selection and duration shaping.
// AI: invariants=Output onsets are valid subset of input; durations bounded [0.25..1.5]; deterministic.
// AI: deps=Consumes CompBehavior, CompRhythmPattern, compOnsets; produces CompRealizationResult.

namespace Music.Generator
{
    /// <summary>
    /// Result of behavior realization: which onsets to play and how long.
    /// </summary>
    public sealed class CompRealizationResult
    {
        /// <summary>Onset beats to actually play (filtered subset of available).</summary>
        public required IReadOnlyList<decimal> SelectedOnsets { get; init; }
        
        /// <summary>Duration multiplier [0.25..1.5] applied to slot duration. &lt;1 = choppier, &gt;1 = sustain.</summary>
        public double DurationMultiplier { get; init; } = 1.0;
    }

    /// <summary>
    /// Converts CompBehavior + pattern + context into onset selection and duration shaping.
    /// </summary>
    public static class CompBehaviorRealizer
    {
        /// <summary>
        /// Realizes behavior into onset selection and duration multiplier.
        /// </summary>
        /// <param name="behavior">Selected comp behavior</param>
        /// <param name="compOnsets">All available comp onsets from groove</param>
        /// <param name="pattern">Rhythm pattern (provides ordered indices)</param>
        /// <param name="densityMultiplier">Energy-driven density [0.5..2.0]</param>
        /// <param name="bar">Current bar number (1-based)</param>
        /// <param name="seed">Master seed</param>
        public static CompRealizationResult Realize(
            CompBehavior behavior,
            IReadOnlyList<decimal> compOnsets,
            CompRhythmPattern pattern,
            double densityMultiplier,
            int bar,
            int seed)
        {
            if (compOnsets == null || compOnsets.Count == 0)
            {
                return new CompRealizationResult
                {
                    SelectedOnsets = Array.Empty<decimal>(),
                    DurationMultiplier = 1.0
                };
            }

            ArgumentNullException.ThrowIfNull(pattern);

            densityMultiplier = Math.Clamp(densityMultiplier, 0.5, 2.0);

            // Base onset count from pattern * density
            int baseCount = (int)Math.Round(pattern.IncludedOnsetIndices.Count * densityMultiplier);
            baseCount = Math.Clamp(baseCount, 1, compOnsets.Count);

            // Behavior-specific onset selection and duration
            return behavior switch
            {
                CompBehavior.SparseAnchors => RealizeSparseAnchors(compOnsets, baseCount, bar, seed),
                CompBehavior.Standard => RealizeStandard(compOnsets, pattern, baseCount, bar, seed),
                CompBehavior.Anticipate => RealizeAnticipate(compOnsets, pattern, baseCount, bar, seed),
                CompBehavior.SyncopatedChop => RealizeSyncopatedChop(compOnsets, pattern, baseCount, bar, seed),
                CompBehavior.DrivingFull => RealizeDrivingFull(compOnsets, densityMultiplier),
                _ => RealizeStandard(compOnsets, pattern, baseCount, bar, seed)
            };
        }

        /// <summary>
        /// SparseAnchors: prefer strong beats only, long sustains.
        /// </summary>
        private static CompRealizationResult RealizeSparseAnchors(
            IReadOnlyList<decimal> compOnsets,
            int targetCount,
            int bar,
            int seed)
        {
            // Find strong-beat onsets (integer beats)
            var strongBeatOnsets = compOnsets.Where(o => o == Math.Floor(o)).ToList();
            
            // If not enough strong beats, add closest offbeats
            var selected = strongBeatOnsets.Take(Math.Min(targetCount, strongBeatOnsets.Count)).ToList();
            
            if (selected.Count < targetCount)
            {
                var offbeats = compOnsets.Except(strongBeatOnsets).ToList();
                selected.AddRange(offbeats.Take(targetCount - selected.Count));
            }

            // Limit to max 2 onsets for truly sparse feel
            selected = selected.OrderBy(o => o).Take(Math.Min(2, targetCount)).ToList();

            return new CompRealizationResult
            {
                SelectedOnsets = selected,
                DurationMultiplier = 1.3 // Longer sustains
            };
        }

        /// <summary>
        /// Standard: balanced selection using pattern order, normal duration.
        /// </summary>
        private static CompRealizationResult RealizeStandard(
            IReadOnlyList<decimal> compOnsets,
            CompRhythmPattern pattern,
            int targetCount,
            int bar,
            int seed)
        {
            var selected = new List<decimal>();
            
            // Use pattern indices but with rotation based on bar and seed
            int rotation = (bar + seed) % Math.Max(1, pattern.IncludedOnsetIndices.Count);
            var rotatedIndices = pattern.IncludedOnsetIndices
                .Select((idx, i) => (idx, order: (i + rotation) % pattern.IncludedOnsetIndices.Count))
                .OrderBy(x => x.order)
                .Select(x => x.idx)
                .ToList();

            foreach (int index in rotatedIndices.Take(targetCount))
            {
                if (index >= 0 && index < compOnsets.Count)
                {
                    selected.Add(compOnsets[index]);
                }
            }

            return new CompRealizationResult
            {
                SelectedOnsets = selected.OrderBy(o => o).ToList(),
                DurationMultiplier = 1.0
            };
        }

        /// <summary>
        /// Anticipate: add anticipations (offbeats before strong beats), medium-short duration.
        /// </summary>
        private static CompRealizationResult RealizeAnticipate(
            IReadOnlyList<decimal> compOnsets,
            CompRhythmPattern pattern,
            int targetCount,
            int bar,
            int seed)
        {
            // Prefer offbeats that are close to the following strong beat (anticipations)
            var anticipationOnsets = compOnsets.Where(o => 
            {
                decimal fractional = o - Math.Floor(o);
                // Onsets at .5 or later are "anticipating" the next beat
                return fractional >= 0.5m;
            }).ToList();

            var strongBeats = compOnsets.Where(o => o == Math.Floor(o)).ToList();

            // Interleave: anticipation, strong beat, anticipation, strong beat...
            var selected = new List<decimal>();
            int antIdx = 0, strongIdx = 0;
            
            while (selected.Count < targetCount && (antIdx < anticipationOnsets.Count || strongIdx < strongBeats.Count))
            {
                // Add anticipation first if available and not yet at target
                if (antIdx < anticipationOnsets.Count && selected.Count < targetCount)
                {
                    selected.Add(anticipationOnsets[antIdx++]);
                }
                // Then add strong beat
                if (strongIdx < strongBeats.Count && selected.Count < targetCount)
                {
                    selected.Add(strongBeats[strongIdx++]);
                }
            }

            return new CompRealizationResult
            {
                SelectedOnsets = selected.OrderBy(o => o).ToList(),
                DurationMultiplier = 0.75 // Shorter for punchy feel
            };
        }

        /// <summary>
        /// SyncopatedChop: prefer offbeats, very short durations.
        /// </summary>
        private static CompRealizationResult RealizeSyncopatedChop(
            IReadOnlyList<decimal> compOnsets,
            CompRhythmPattern pattern,
            int targetCount,
            int bar,
            int seed)
        {
            // Prefer offbeats (non-integer beats)
            var offbeats = compOnsets.Where(o => o != Math.Floor(o)).ToList();
            var strongBeats = compOnsets.Where(o => o == Math.Floor(o)).ToList();

            var selected = new List<decimal>();
            
            // Take offbeats first (up to 70% of target)
            int offbeatTarget = (int)Math.Ceiling(targetCount * 0.7);
            selected.AddRange(offbeats.Take(offbeatTarget));
            
            // Fill remainder with strong beats for groove anchor
            int remaining = targetCount - selected.Count;
            selected.AddRange(strongBeats.Take(remaining));

            // Apply deterministic shuffle based on seed+bar
            int shuffleHash = HashCode.Combine(seed, bar);
            if (selected.Count > 2 && (shuffleHash % 3) == 0)
            {
                // Occasionally skip first onset for pickup feel
                selected = selected.Skip(1).ToList();
            }

            return new CompRealizationResult
            {
                SelectedOnsets = selected.OrderBy(o => o).ToList(),
                DurationMultiplier = 0.5 // Very short, choppy
            };
        }

        /// <summary>
        /// DrivingFull: all or nearly all onsets, moderate-short duration.
        /// </summary>
        private static CompRealizationResult RealizeDrivingFull(
            IReadOnlyList<decimal> compOnsets,
            double densityMultiplier)
        {
            // Use all onsets (or nearly all based on density)
            int count = (int)Math.Ceiling(compOnsets.Count * Math.Min(densityMultiplier, 1.2));
            count = Math.Min(count, compOnsets.Count);

            return new CompRealizationResult
            {
                SelectedOnsets = compOnsets.Take(count).ToList(),
                DurationMultiplier = 0.65 // Moderate chop for driving feel
            };
        }
    }
}
