//// AI: purpose=Applies KeysRoleMode to onset selection and duration shaping for keys/pads.
//// AI: invariants=Output onsets are valid subset of input; durations bounded [0.5..2.0]; deterministic.
//// AI: deps=Consumes KeysRoleMode, padsOnsets; produces KeysRealizationResult with onset filtering and duration control.

//namespace Music.Generator
//{
//    /// <summary>
//    /// Result of keys mode realization: which onsets to play and how long.
//    /// </summary>
//    public sealed class KeysRealizationResult
//    {
//        /// <summary>Onset beats to actually play (filtered subset of available).</summary>
//        public required IReadOnlyList<decimal> SelectedOnsets { get; init; }
        
//        /// <summary>Duration multiplier [0.5..2.0] applied to slot duration. &lt;1 = choppier, &gt;1 = sustain.</summary>
//        public double DurationMultiplier { get; init; } = 1.0;
        
//        /// <summary>For SplitVoicing mode: which onset is the "upper voicing" onset (others are lower). -1 if not applicable.</summary>
//        public int SplitUpperOnsetIndex { get; init; } = -1;
//    }

//    /// <summary>
//    /// Converts KeysRoleMode into onset selection and duration shaping.
//    /// </summary>
//    public static class KeysModeRealizer
//    {
//        /// <summary>
//        /// Realizes mode into onset selection and duration multiplier.
//        /// </summary>
//        /// <param name="mode">Selected keys/pads mode</param>
//        /// <param name="padsOnsets">All available pads onsets from groove</param>
//        /// <param name="densityMultiplier">Energy-driven density [0.5..2.0]</param>
//        /// <param name="bar">Current bar number (1-based)</param>
//        /// <param name="seed">Master seed for deterministic variation</param>
//        public static KeysRealizationResult Realize(
//            KeysRoleMode mode,
//            IReadOnlyList<decimal> padsOnsets,
//            double densityMultiplier,
//            int bar,
//            int seed)
//        {
//            if (padsOnsets == null || padsOnsets.Count == 0)
//            {
//                return new KeysRealizationResult
//                {
//                    SelectedOnsets = Array.Empty<decimal>(),
//                    DurationMultiplier = 1.0
//                };
//            }

//            densityMultiplier = Math.Clamp(densityMultiplier, 0.5, 2.0);

//            return mode switch
//            {
//                KeysRoleMode.Sustain => RealizeSustain(padsOnsets),
//                KeysRoleMode.Pulse => RealizePulse(padsOnsets, densityMultiplier, bar, seed),
//                KeysRoleMode.Rhythmic => RealizeRhythmic(padsOnsets, densityMultiplier),
//                KeysRoleMode.SplitVoicing => RealizeSplitVoicing(padsOnsets, bar, seed),
//                _ => RealizePulse(padsOnsets, densityMultiplier, bar, seed)
//            };
//        }

//        /// <summary>
//        /// Sustain: only first onset of bar, long duration (2x slot).
//        /// </summary>
//        private static KeysRealizationResult RealizeSustain(IReadOnlyList<decimal> padsOnsets)
//        {
//            return new KeysRealizationResult
//            {
//                SelectedOnsets = new[] { padsOnsets[0] },
//                DurationMultiplier = 2.0 // Extend beyond slot for sustained feel
//            };
//        }

//        /// <summary>
//        /// Pulse: selected strong beats, normal duration.
//        /// </summary>
//        private static KeysRealizationResult RealizePulse(
//            IReadOnlyList<decimal> padsOnsets,
//            double densityMultiplier,
//            int bar,
//            int seed)
//        {
//            // Prefer strong beats (integer values)
//            var strongBeats = padsOnsets.Where(o => o == Math.Floor(o)).ToList();
//            var offbeats = padsOnsets.Where(o => o != Math.Floor(o)).ToList();

//            // Target count: 50% of onsets scaled by density
//            int targetCount = Math.Max(1, (int)Math.Round(padsOnsets.Count * densityMultiplier * 0.5));
//            targetCount = Math.Min(targetCount, padsOnsets.Count);

//            var selected = new List<decimal>();
            
//            // Always include beat 1 if present
//            if (strongBeats.Any(b => b == 1m))
//            {
//                selected.Add(1m);
//                strongBeats = strongBeats.Where(b => b != 1m).ToList();
//            }

//            // Add remaining strong beats
//            selected.AddRange(strongBeats.Take(targetCount - selected.Count));

//            // Fill with offbeats if needed (deterministic selection based on seed)
//            if (selected.Count < targetCount)
//            {
//                int hash = HashCode.Combine(seed, bar);
//                int skipIndex = hash % Math.Max(1, offbeats.Count);
//                var selectedOffbeats = offbeats.Where((_, i) => i != skipIndex).Take(targetCount - selected.Count);
//                selected.AddRange(selectedOffbeats);
//            }

//            return new KeysRealizationResult
//            {
//                SelectedOnsets = selected.OrderBy(o => o).ToList(),
//                DurationMultiplier = 1.0
//            };
//        }

//        /// <summary>
//        /// Rhythmic: use most/all onsets, shorter duration (0.7x).
//        /// </summary>
//        private static KeysRealizationResult RealizeRhythmic(
//            IReadOnlyList<decimal> padsOnsets,
//            double densityMultiplier)
//        {
//            // Use up to 130% of onsets count based on density (but capped at actual count)
//            int targetCount = (int)Math.Ceiling(padsOnsets.Count * Math.Min(densityMultiplier, 1.3));
//            targetCount = Math.Min(targetCount, padsOnsets.Count);

//            return new KeysRealizationResult
//            {
//                SelectedOnsets = padsOnsets.Take(targetCount).ToList(),
//                DurationMultiplier = 0.7 // Shorter for rhythmic feel
//            };
//        }

//        /// <summary>
//        /// SplitVoicing: two onsets (low voicing first, upper voicing second).
//        /// </summary>
//        private static KeysRealizationResult RealizeSplitVoicing(
//            IReadOnlyList<decimal> padsOnsets,
//            int bar,
//            int seed)
//        {
//            if (padsOnsets.Count < 2)
//            {
//                // Fallback: single onset with extended duration
//                return new KeysRealizationResult
//                {
//                    SelectedOnsets = padsOnsets,
//                    DurationMultiplier = 1.2,
//                    SplitUpperOnsetIndex = -1
//                };
//            }

//            // Select two onsets: first available and one near middle of bar
//            var first = padsOnsets[0];
//            var second = padsOnsets[padsOnsets.Count / 2];

//            return new KeysRealizationResult
//            {
//                SelectedOnsets = new[] { first, second },
//                DurationMultiplier = 1.2,
//                SplitUpperOnsetIndex = 1 // Second onset gets upper voicing
//            };
//        }
//    }
//}
