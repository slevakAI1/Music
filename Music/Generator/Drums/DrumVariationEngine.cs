// AI: purpose=Deterministic drum variation engine for Story 6.1; transforms static groove templates into living drum performances.
// AI: invariants=All variation choices deterministic for (seed, grooveName, sectionType, barIndex); RNG only for tie-breaking valid options.
// AI: deps=Uses RandomHelpers for seeding, GrooveEvent for template, MusicConstants.eSectionType for section context.
// AI: perf=Called once per bar during generation; keep candidate generation lightweight.

namespace Music.Generator
{
    /// <summary>
    /// Generates deterministic, style-aware drum variations from groove templates.
    /// Implements Story 6.1: turn static onset lists into living performance with controlled variation.
    /// </summary>
    internal static class DrumVariationEngine
    {
        /// <summary>
        /// Describes a single drum hit with articulation and timing information.
        /// </summary>
        public sealed class DrumHit
        {
            public string Role { get; init; } = string.Empty;  // "kick" | "snare" | "hat" | "ride"
            public decimal OnsetBeat { get; init; }            // beat position in bar (domain units)
            public int TimingOffsetTicks { get; init; } = 0;   // micro-timing offset (can be negative)
            public bool IsOpenHat { get; init; } = false;      // open vs closed articulation
            public bool IsGhost { get; init; } = false;        // ghost note (low velocity)
            public bool IsFlam { get; init; } = false;         // flam accent (pre-hit)
            public bool IsMain { get; init; } = false;         // from template anchor layer
            public bool IsInFill { get; init; } = false;       // part of a fill
            public double FillProgress { get; init; } = 0.0;   // fill progress 0.0 to 1.0
            public bool IsChoke { get; init; } = false;        // choke/stop hit (very short duration)
        }

        /// <summary>
        /// Per-bar variation plan containing all drum hits with articulations.
        /// </summary>
        public sealed class DrumBarVariation
        {
            public List<DrumHit> Hits { get; } = new List<DrumHit>();
        }

        /// <summary>
        /// Generates deterministic per-bar drum variation from groove template.
        /// All choices are deterministic for (seed, grooveName, sectionType, barIndex).
        /// Story 6.5: now accepts DrumRoleParameters to expose knobs for Stage 7.
        /// </summary>
        public static DrumBarVariation Generate(
            GrooveEvent grooveEvent,
            MusicConstants.eSectionType sectionType,
            int barIndex,
            int seed,
            DrumRoleParameters? drumParams = null)
        {
            if (grooveEvent == null) throw new ArgumentNullException(nameof(grooveEvent));
            
            // Use default parameters if not provided (preserves existing behavior)
            drumParams ??= new DrumRoleParameters();
            
            var variation = new DrumBarVariation();

            // Use event layers, but if they are empty try resolving preset by name
            var anchor = grooveEvent.AnchorLayer ?? new GrooveInstanceLayer();
            var tension = grooveEvent.TensionLayer ?? new GrooveInstanceLayer();
            bool usedPresetFallback = false;

            // If anchor appears empty and we have a preset name, load the preset template
            if (IsLayerEmpty(anchor) && !string.IsNullOrWhiteSpace(grooveEvent.SourcePresetName))
            {
                var preset = GroovePresets.GetByName(grooveEvent.SourcePresetName.Trim());
                if (preset != null)
                {
                    anchor = preset.AnchorLayer ?? anchor;
                    tension = preset.TensionLayer ?? tension;
                    usedPresetFallback = true;
                }
            }

            // Bar-level deterministic RNG for consistent bar-wide choices
            // FIX: include sectionType in RNG seed for full determinism over (seed, grooveName, sectionType, barIndex)
            string sectionKey = sectionType.ToString();
            var barRng = RandomHelpers.CreateLocalRng(seed, $"{grooveEvent.SourcePresetName ?? "groove"}_{sectionKey}", barIndex, 0m);

            // Determine if this bar uses ride instead of hat (based on section energy)
            bool useRide = ShouldUseRide(sectionType, barRng);

            // --- KICK: preserve main hits, add small sensible variations ---
            if (anchor.KickOnsets != null)
            {
                foreach (var onset in anchor.KickOnsets)
                {
                    bool isStrongBeat = RandomHelpers.IsStrongBeat(onset);
                    int timingOffset = DrumMicroTimingEngine.GetTimingOffset(
                        "kick", 
                        grooveEvent.SourcePresetName ?? "default",
                        barIndex,
                        onset,
                        seed,
                        isStrongBeat);

                    variation.Hits.Add(new DrumHit
                    {
                        Role = "kick",
                        OnsetBeat = onset,
                        IsMain = true,
                        TimingOffsetTicks = timingOffset
                    });
                }
            }

            // Optionally include tension layer kicks (deterministic base probability scaled by density)
            double tensionKickBaseProb = 0.5 * Math.Clamp(drumParams.DensityMultiplier, 0.25, 2.5);
            if (tension.KickOnsets != null && tension.KickOnsets.Count > 0 && barRng.NextDouble() < tensionKickBaseProb)
            {
                foreach (var onset in tension.KickOnsets)
                {
                    bool isStrongBeat = RandomHelpers.IsStrongBeat(onset);
                    int timingOffset = DrumMicroTimingEngine.GetTimingOffset(
                        "kick",
                        grooveEvent.SourcePresetName ?? "default",
                        barIndex,
                        onset,
                        seed,
                        isStrongBeat);

                    variation.Hits.Add(new DrumHit
                    {
                        Role = "kick",
                        OnsetBeat = onset,
                        IsMain = false,
                        TimingOffsetTicks = timingOffset
                    });
                }
            }

            // Small chance to add extra kick for flow (never on main kick positions).
            // Scale chance by density multiplier and busy probability.
            if (anchor.KickOnsets != null && anchor.KickOnsets.Count > 0)
            {
                var candidates = BuildWeakSubdivisionCandidates(anchor.HatOnsets, anchor.KickOnsets);
                double extraKickChance = (0.12 * drumParams.DensityMultiplier) + drumParams.BusyProbability;
                if (candidates.Count > 0 && barRng.NextDouble() < Math.Clamp(extraKickChance, 0.0, 0.95))
                {
                    var pick = RandomHelpers.ChooseRandom(barRng, candidates);
                    bool isStrongBeat = RandomHelpers.IsStrongBeat(pick);
                    int timingOffset = DrumMicroTimingEngine.GetTimingOffset(
                        "kick",
                        grooveEvent.SourcePresetName ?? "default",
                        barIndex,
                        pick,
                        seed,
                        isStrongBeat);

                    variation.Hits.Add(new DrumHit
                    {
                        Role = "kick",
                        OnsetBeat = pick,
                        IsMain = false,
                        TimingOffsetTicks = timingOffset
                    });
                }
            }

            // --- SNARE: preserve main hits, add ghost notes, optional flams ---
            if (anchor.SnareOnsets != null)
            {
                foreach (var onset in anchor.SnareOnsets)
                {
                    var snareRng = RandomHelpers.CreateLocalRng(seed, $"{grooveEvent.SourcePresetName ?? "snare"}_{sectionKey}", barIndex, onset);
                    
                    // Flams (currently disabled)
                    bool allowFlam = IsHighEnergySection(sectionType);
                    bool flam = allowFlam && snareRng.NextDouble() < 0.22;
                    bool isStrongBeat = RandomHelpers.IsStrongBeat(onset);

                    if (flam)
                    {
                        // Add flam pre-hit (small negative offset)
                        int flamTimingOffset = DrumMicroTimingEngine.GetTimingOffset(
                            "snare",
                            grooveEvent.SourcePresetName ?? "default",
                            barIndex,
                            onset,
                            seed,
                            isStrongBeat);
                        // Flam pre-hit: additional negative offset
                        flamTimingOffset -= Math.Max(8, (int)(snareRng.NextDouble() * 18));

                        variation.Hits.Add(new DrumHit
                        {
                            Role = "snare",
                            OnsetBeat = onset,
                            IsMain = false,
                            IsFlam = true,
                            TimingOffsetTicks = flamTimingOffset
                        });
                    }

                    // Main snare hit
                    int mainTimingOffset = DrumMicroTimingEngine.GetTimingOffset(
                        "snare",
                        grooveEvent.SourcePresetName ?? "default",
                        barIndex,
                        onset,
                        seed,
                        isStrongBeat);

                    variation.Hits.Add(new DrumHit
                    {
                        Role = "snare",
                        OnsetBeat = onset,
                        IsMain = true,
                        TimingOffsetTicks = mainTimingOffset
                    });
                }

                // Add ghost notes on weak subdivisions
                var ghostCandidates = anchor.HatOnsets ?? new List<decimal>();
                if (ghostCandidates.Count > 0)
                {
                    var ghostRng = RandomHelpers.CreateLocalRng(seed, $"{grooveEvent.SourcePresetName ?? "ghost"}_{sectionKey}", barIndex, 0m);
                    // Base 0-2 ghosts per bar, scaled by density multiplier
                    int maxGhostBase = 2;
                    int maxGhost = Math.Max(0, (int)Math.Round(maxGhostBase * drumParams.DensityMultiplier));
                    int ghostCount = ghostRng.NextInt(0, Math.Max(1, maxGhost + 1));
                    
                    for (int i = 0; i < ghostCount; i++)
                    {
                        var pick = RandomHelpers.ChooseRandom(ghostRng, ghostCandidates);
                        
                        // Avoid ghost on strong beats (where main snare typically hits)
                        if (RandomHelpers.IsStrongBeat(pick)) continue;
                        
                        bool ghostIsStrongBeat = RandomHelpers.IsStrongBeat(pick);
                        int ghostTimingOffset = DrumMicroTimingEngine.GetTimingOffset(
                            "snare",
                            grooveEvent.SourcePresetName ?? "default",
                            barIndex,
                            pick,
                            seed,
                            ghostIsStrongBeat);

                        variation.Hits.Add(new DrumHit
                        {
                            Role = "snare",
                            OnsetBeat = pick,
                            IsGhost = true,
                            IsMain = false,
                            TimingOffsetTicks = ghostTimingOffset
                        });
                    }
                }
            }

            // --- HI-HAT / RIDE: open/close articulations, occasional skips/adds ---
            if (anchor.HatOnsets != null)
            {
                string role = useRide ? "ride" : "hat";

                foreach (var onset in anchor.HatOnsets)
                {
                    var hatRng = RandomHelpers.CreateLocalRng(seed, $"{grooveEvent.SourcePresetName ?? role}_{sectionKey}", barIndex, onset);

                    // Open hat probability (ride doesn't use open articulation)
                    bool open = false;
                    if (!useRide)
                    {
                        double openProb = OpenHatProbabilityForSection(sectionType);
                        open = hatRng.NextDouble() < openProb;
                    }

                    // Occasionally skip hats/ride for flow (never skip strong beats)
                    bool skip = !RandomHelpers.IsStrongBeat(onset) && hatRng.NextDouble() < 0.06;

                    if (!skip)
                    {
                        bool isStrongBeat = RandomHelpers.IsStrongBeat(onset);
                        int timingOffset = DrumMicroTimingEngine.GetTimingOffset(
                            role,
                            grooveEvent.SourcePresetName ?? "default",
                            barIndex,
                            onset,
                            seed,
                            isStrongBeat);

                        variation.Hits.Add(new DrumHit
                        {
                            Role = role,
                            OnsetBeat = onset,
                            IsMain = true,
                            IsOpenHat = open,
                            TimingOffsetTicks = timingOffset
                        });
                    }
                }

                // Occasionally add extra hat/ride for flow; scale by density multiplier and busy probability.
                double extraHatChance = (0.08 * drumParams.DensityMultiplier) + drumParams.BusyProbability;
                if (anchor.HatOnsets.Count > 0 && barRng.NextDouble() < Math.Clamp(extraHatChance, 0.0, 0.95))
                {
                    var pickBase = RandomHelpers.ChooseRandom(barRng, anchor.HatOnsets);
                    decimal extraOnset = pickBase + 0.25m; // quarter subdivision ahead
                    bool isStrongBeat = RandomHelpers.IsStrongBeat(extraOnset);
                    int timingOffset = DrumMicroTimingEngine.GetTimingOffset(
                        role,
                        grooveEvent.SourcePresetName ?? "default",
                        barIndex,
                        extraOnset,
                        seed,
                        isStrongBeat);
                    
                    variation.Hits.Add(new DrumHit
                    {
                        Role = role,
                        OnsetBeat = extraOnset,
                        IsMain = false,
                        IsOpenHat = false,
                        TimingOffsetTicks = timingOffset
                    });
                }
            }

            // Sort for deterministic output order (role, onset, offset)
            variation.Hits.Sort((a, b) =>
            {
                int roleComp = string.CompareOrdinal(a.Role, b.Role);
                if (roleComp != 0) return roleComp;
                
                int onsetComp = a.OnsetBeat.CompareTo(b.OnsetBeat);
                if (onsetComp != 0) return onsetComp;
                
                return a.TimingOffsetTicks.CompareTo(b.TimingOffsetTicks);
            });

            return variation;
        }

        /// <summary>
        /// Determines if ride should be used instead of hi-hat.
        /// Returns fixed low probability regardless of section type.
        /// </summary>
        private static bool ShouldUseRide(MusicConstants.eSectionType sectionType, IRandomSource rng)
        {
            double rideProb = 0.15;
            return rng.NextDouble() < rideProb;
        }

        /// <summary>
        /// Determines if section type allows flams.
        /// Returns false (flams disabled).
        /// </summary>
        private static bool IsHighEnergySection(MusicConstants.eSectionType sectionType)
        {
            return false;
        }

        /// <summary>
        /// Returns open hat probability.
        /// Returns fixed value regardless of section type.
        /// </summary>
        private static double OpenHatProbabilityForSection(MusicConstants.eSectionType sectionType)
        {
            return 0.1;
        }

        /// <summary>
        /// Builds candidate subdivisions for extra hits from hat onsets,
        /// excluding existing kick positions to avoid conflicts.
        /// </summary>
        private static List<decimal> BuildWeakSubdivisionCandidates(List<decimal>? hatOnsets, List<decimal>? kickOnsets)
        {
            var candidates = new List<decimal>();
            
            if (hatOnsets != null && hatOnsets.Count > 0)
                candidates.AddRange(hatOnsets);

            // Fallback to common offbeat subdivisions if no hats
            if (candidates.Count == 0)
                candidates.AddRange(new[] { 1.5m, 2.5m, 3.5m, 4.5m });

            // Remove positions that already have kicks (prefer weak positions)
            if (kickOnsets != null)
                candidates = candidates.Where(c => !kickOnsets.Contains(c)).ToList();

            return candidates.Distinct().ToList();
        }

        /// <summary>
        /// True when the provided layer has no meaningful onsets to drive hits.
        /// </summary>
        private static bool IsLayerEmpty(GrooveInstanceLayer? layer)
        {
            if (layer == null) return true;
            bool emptyKick = layer.KickOnsets == null || layer.KickOnsets.Count == 0;
            bool emptySnare = layer.SnareOnsets == null || layer.SnareOnsets.Count == 0;
            bool emptyHat = layer.HatOnsets == null || layer.HatOnsets.Count == 0;
            return emptyKick && emptySnare && emptyHat;
        }
    }
}
