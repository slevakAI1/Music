// AI: purpose=Generate style-aware drum fills for section transitions (Story 6.3).
// AI: invariants=All fill generation is deterministic for (seed, grooveName, sectionType, sectionIndex); respects bar boundaries and totalBars.
// AI: deps=Uses RandomHelpers for seeding, returns list of DrumHit compatible with DrumVariationEngine.DrumHit.

namespace Music.Generator
{
    /// <summary>
    /// Generates deterministic, style-aware drum fills for section transitions.
    /// Implements Story 6.3: fills signal form changes with structured shapes.
    /// </summary>
    internal static class DrumFillEngine
    {
        private const int MinTransitionFillDensity = 4;

        /// <summary>
        /// Describes the style characteristics of a fill mapped from groove name.
        /// </summary>
        private sealed class FillStyle
        {
            public string StyleName { get; init; } = string.Empty;
            public bool SupportsRoll16th { get; init; } = true;
            public bool SupportsTomMovement { get; init; } = true;
            public int MaxDensity { get; init; } = 8; // Max hits in fill
            public bool PrefersCrashOnDownbeat { get; init; } = true;
        }

        /// <summary>
        /// Maps groove name to fill style characteristics.
        /// Based on music theory and genre-specific drumming conventions.
        /// </summary>
        private static FillStyle GetFillStyle(string grooveName)
        {
            string normalized = grooveName?.Trim() ?? "default";

            return normalized switch
            {
                // === ROCK / POP ROCK ===
                // Rock fills: dense 16th rolls, tom-toms prominent, crash on 1
                // Classic rock drummers use all toms in cascading patterns
                "PopRockBasic" => new FillStyle
                {
                    StyleName = "pop-rock",
                    SupportsRoll16th = true,
                    SupportsTomMovement = true,
                    MaxDensity = 8,
                    PrefersCrashOnDownbeat = true
                },

                // === METAL ===
                // Metal fills: extremely fast, double-bass integration, crash/china accents
                // Tom rolls often faster and more aggressive than rock
                "MetalDoubleKick" => new FillStyle
                {
                    StyleName = "metal",
                    SupportsRoll16th = true,
                    SupportsTomMovement = true,
                    MaxDensity = 10, // Metal can be very dense
                    PrefersCrashOnDownbeat = true
                },

                // === FUNK ===
                // Funk fills: syncopated, ghost notes, less tom-heavy, often snare-focused
                // Emphasis on pocket and groove over flashy fills
                "FunkSyncopated" => new FillStyle
                {
                    StyleName = "funk",
                    SupportsRoll16th = true,
                    SupportsTomMovement = false, // Funk prefers snare-based fills
                    MaxDensity = 6,
                    PrefersCrashOnDownbeat = false // Funk often avoids heavy crashes
                },

                // === ELECTRONIC / EDM / DANCE ===
                // EDM fills: simple, mechanical, often just snare rolls
                // No toms (electronic drum sounds), very predictable
                "DanceEDMFourOnFloor" => new FillStyle
                {
                    StyleName = "edm",
                    SupportsRoll16th = false, // EDM usually 8th note max
                    SupportsTomMovement = false,
                    MaxDensity = 4,
                    PrefersCrashOnDownbeat = true
                },

                // === TRAP / HIP-HOP ===
                // Trap fills: hi-hat rolls (32nd notes), minimal snare, sparse
                // Modern trap often uses hi-hat triplet rolls as fills
                "TrapModern" => new FillStyle
                {
                    StyleName = "trap",
                    SupportsRoll16th = true, // Trap uses fast hi-hat rolls
                    SupportsTomMovement = false,
                    MaxDensity = 5,
                    PrefersCrashOnDownbeat = false // Trap is minimal
                },

                // Hip-hop boom-bap: simple, classic breaks-inspired fills
                "HipHopBoomBap" => new FillStyle
                {
                    StyleName = "hip-hop",
                    SupportsRoll16th = false,
                    SupportsTomMovement = false,
                    MaxDensity = 4,
                    PrefersCrashOnDownbeat = false
                },

                "RapBasic" => new FillStyle
                {
                    StyleName = "rap",
                    SupportsRoll16th = false,
                    SupportsTomMovement = false,
                    MaxDensity = 4,
                    PrefersCrashOnDownbeat = false
                },

                // === LATIN / BOSSA NOVA ===
                // Bossa fills: very sparse, subtle, mostly snare/rim
                // Latin drumming emphasizes ride/cymbal work over fills
                "BossaNovaBasic" => new FillStyle
                {
                    StyleName = "bossa",
                    SupportsRoll16th = false,
                    SupportsTomMovement = false,
                    MaxDensity = 3, // Very minimal
                    PrefersCrashOnDownbeat = false
                },

                // === REGGAE / REGGAETON ===
                // Reggae fills: minimal, rim clicks, occasional tom
                // One-drop style keeps fills very understated
                "ReggaeOneDrop" => new FillStyle
                {
                    StyleName = "reggae",
                    SupportsRoll16th = false,
                    SupportsTomMovement = false,
                    MaxDensity = 3,
                    PrefersCrashOnDownbeat = false // Reggae avoids heavy accents
                },

                // Reggaeton: slightly busier than reggae, dembow pattern emphasis
                "ReggaetonDembow" => new FillStyle
                {
                    StyleName = "reggaeton",
                    SupportsRoll16th = false,
                    SupportsTomMovement = false,
                    MaxDensity = 4,
                    PrefersCrashOnDownbeat = false
                },

                // === COUNTRY ===
                // Country fills: train-beat influenced, simple tom patterns
                // Often uses floor tom and kick together
                "CountryTrain" => new FillStyle
                {
                    StyleName = "country",
                    SupportsRoll16th = true,
                    SupportsTomMovement = true,
                    MaxDensity = 6,
                    PrefersCrashOnDownbeat = true
                },

                // === JAZZ ===
                // Jazz fills: ride cymbal-focused, brushes common, very musical
                // Swing feel, less about tom rolls, more about phrasing
                "JazzSwing" => new FillStyle
                {
                    StyleName = "jazz",
                    SupportsRoll16th = false, // Jazz uses swing subdivisions
                    SupportsTomMovement = false, // Jazz fills are cymbal/snare based
                    MaxDensity = 5,
                    PrefersCrashOnDownbeat = false // Jazz avoids predictable accents
                },

                // === DEFAULT FALLBACK ===
                _ => new FillStyle
                {
                    StyleName = "default",
                    SupportsRoll16th = true,
                    SupportsTomMovement = true,
                    MaxDensity = 6,
                    PrefersCrashOnDownbeat = true
                }
            };
        }

        /// <summary>
        /// Determines if this bar should have a fill based on section boundaries.
        /// Returns true if this is the last bar of a section (transition point).
        /// </summary>
        public static bool ShouldGenerateFill(
            int bar,
            int totalBars,
            SectionTrack sectionTrack)
        {
            // Never fill on the very last bar of the song
            if (bar >= totalBars)
                return false;

            // Check if this is the last bar of a section
            if (sectionTrack.GetActiveSection(bar, out var currentSection) && currentSection != null)
            {
                int sectionEndBar = currentSection.StartBar + currentSection.BarCount - 1;
                return bar == sectionEndBar;
            }

            return false;
        }

        /// <summary>
        /// Generates a drum fill for the specified bar.
        /// Returns list of DrumHit objects compatible with DrumVariationEngine output.
        /// Story 6.5: now accepts DrumRoleParameters to allow Stage 7 to control fill complexity.
        /// </summary>
        public static List<DrumVariationEngine.DrumHit> GenerateFill(
            int bar,
            string grooveName,
            MusicConstants.eSectionType sectionType,
            int sectionIndex,
            int seed,
            int totalBars,
            DrumRoleParameters? drumParams = null)
        {
            var hits = new List<DrumVariationEngine.DrumHit>();
            
            // Use default parameters if not provided (preserves existing behavior)
            drumParams ??= new DrumRoleParameters();
            
            // Get style characteristics
            var style = GetFillStyle(grooveName);

            // Deterministic RNG for this fill
            var fillRng = RandomHelpers.CreateLocalRng(seed, $"fill_{grooveName}", sectionIndex, bar);

            // Determine fill complexity based on section type and allow Stage 7 to scale it.
            int complexity = GetFillComplexity(sectionType, fillRng);
            
            // Scale complexity by supplied parameter (default 1.0)
            complexity = Math.Max(1, (int)Math.Round(complexity * drumParams.FillComplexityMultiplier));

            // Clamp density to style max
            int targetDensity = Math.Min(complexity, style.MaxDensity);

            // Policy: transition fills should not collapse into near-silent measures.
            // The current low-density pickup is useful for subtle "pickup" moments, but for section transitions
            // it creates audible gaps (e.g., only 2 snare hits) when the fill replaces the groove.
            targetDensity = Math.Clamp(targetDensity, MinTransitionFillDensity, style.MaxDensity);

            // Generate fill shape based on style and density
            if (style.SupportsRoll16th && targetDensity >= 6)
            {
                // 16th note roll (high density)
                GenerateRoll16th(hits, bar, style, fillRng, targetDensity);
            }
            else if (targetDensity >= 4)
            {
                // 8th note roll (medium density)
                GenerateRoll8th(hits, bar, style, fillRng, targetDensity);
            }
            else
            {
                // Simple pickup (low density)
                GenerateSimplePickup(hits, bar, style, fillRng);
            }

            // Add crash/ride + kick on downbeat of NEXT section if appropriate
            // Only add if we're not on the last bar
            if (bar < totalBars && style.PrefersCrashOnDownbeat)
            {
                // Note: These hits are technically in the NEXT bar, so they'll be added
                // by the next bar's generation. We'll add them here but mark them specially.
                // Actually, we should NOT generate hits for the next bar here.
                // The crash will be handled by Story 6.4 (cymbal orchestration).
                // For now, keep fills within their own bar boundary.
            }

            // Mark all hits as part of fill and set fill progress
            int hitCount = hits.Count;
            for (int i = 0; i < hitCount; i++)
            {
                var hit = hits[i];
                double progress = hitCount > 1 ? (double)i / (hitCount - 1) : 1.0;
                
                hits[i] = new DrumVariationEngine.DrumHit
                {
                    Role = hit.Role,
                    OnsetBeat = hit.OnsetBeat,
                    TimingOffsetTicks = hit.TimingOffsetTicks,
                    IsOpenHat = hit.IsOpenHat,
                    IsGhost = hit.IsGhost,
                    IsFlam = hit.IsFlam,
                    IsMain = hit.IsMain,
                    IsInFill = true,
                    FillProgress = progress
                };
            }

            return hits;
        }

        /// <summary>
        /// Determines fill complexity (target density) based on section type.
        /// </summary>
        private static int GetFillComplexity(MusicConstants.eSectionType sectionType, IRandomSource rng)
        {
            int baseComplexity = sectionType switch
            {
                MusicConstants.eSectionType.Intro => 3,
                MusicConstants.eSectionType.Verse => 4,
                MusicConstants.eSectionType.Chorus => 6,
                MusicConstants.eSectionType.Bridge => 7,
                MusicConstants.eSectionType.Solo => 6,
                MusicConstants.eSectionType.Outro => 3,
                _ => 4
            };

            // Add small deterministic variation (+/- 1)
            int variation = rng.NextInt(-1, 2);
            return Math.Max(2, baseComplexity + variation);
        }

        /// <summary>
        /// Generates a 16th note roll fill (high density).
        /// Uses tom movement from high to low when supported.
        /// </summary>
        private static void GenerateRoll16th(
            List<DrumVariationEngine.DrumHit> hits,
            int bar,
            FillStyle style,
            IRandomSource rng,
            int targetDensity)
        {
            // Generate 16th note positions across last 2 beats (beats 3-4)
            // 16th subdivisions: 3, 3.25, 3.5, 3.75, 4, 4.25, 4.5, 4.75
            var positions = new List<decimal>
            {
                3m, 3.25m, 3.5m, 3.75m,
                4m, 4.25m, 4.5m, 4.75m
            };

            // Select subset based on target density
            int count = Math.Min(targetDensity, positions.Count);
            var selectedPositions = positions.Take(count).ToList();

            // Assign roles with tom movement if supported
            if (style.SupportsTomMovement && count >= 4)
            {
                // Map positions to tom types (high -> mid -> low)
                for (int i = 0; i < selectedPositions.Count; i++)
                {
                    string role = GetTomRole(i, selectedPositions.Count);
                    
                    hits.Add(new DrumVariationEngine.DrumHit
                    {
                        Role = role,
                        OnsetBeat = selectedPositions[i],
                        IsMain = true,
                        TimingOffsetTicks = 0
                    });
                }
            }
            else
            {
                // Use snare for all hits
                foreach (var pos in selectedPositions)
                {
                    hits.Add(new DrumVariationEngine.DrumHit
                    {
                        Role = "snare",
                        OnsetBeat = pos,
                        IsMain = true,
                        TimingOffsetTicks = 0
                    });
                }
            }
        }

        /// <summary>
        /// Generates an 8th note roll fill (medium density).
        /// </summary>
        private static void GenerateRoll8th(
            List<DrumVariationEngine.DrumHit> hits,
            int bar,
            FillStyle style,
            IRandomSource rng,
            int targetDensity)
        {
            // Generate 8th note positions across last 2 beats
            // 8th subdivisions: 3, 3.5, 4, 4.5
            var positions = new List<decimal> { 3m, 3.5m, 4m, 4.5m };

            // Select subset based on target density
            int count = Math.Min(targetDensity, positions.Count);
            var selectedPositions = positions.Take(count).ToList();

            // Assign roles with tom movement if supported
            if (style.SupportsTomMovement && count >= 3)
            {
                for (int i = 0; i < selectedPositions.Count; i++)
                {
                    string role = GetTomRole(i, selectedPositions.Count);
                    
                    hits.Add(new DrumVariationEngine.DrumHit
                    {
                        Role = role,
                        OnsetBeat = selectedPositions[i],
                        IsMain = true,
                        TimingOffsetTicks = 0
                    });
                }
            }
            else
            {
                // Use snare for all hits
                foreach (var pos in selectedPositions)
                {
                    hits.Add(new DrumVariationEngine.DrumHit
                    {
                        Role = "snare",
                        OnsetBeat = pos,
                        IsMain = true,
                        TimingOffsetTicks = 0
                    });
                }
            }
        }

        /// <summary>
        /// Generates a simple pickup fill (low density).
        /// Just a few hits leading into the next section.
        /// </summary>
        private static void GenerateSimplePickup(
            List<DrumVariationEngine.DrumHit> hits,
            int bar,
            FillStyle style,
            IRandomSource rng)
        {
            // Simple pattern: snare on beat 4 and 4.5
            hits.Add(new DrumVariationEngine.DrumHit
            {
                Role = "snare",
                OnsetBeat = 4m,
                IsMain = true,
                TimingOffsetTicks = 0
            });

            hits.Add(new DrumVariationEngine.DrumHit
            {
                Role = "snare",
                OnsetBeat = 4.5m,
                IsMain = true,
                TimingOffsetTicks = 0
            });
        }

        /// <summary>
        /// Maps position in fill to tom role for high-to-low movement.
        /// </summary>
        private static string GetTomRole(int index, int totalCount)
        {
            if (totalCount <= 1)
                return "tom_mid";

            double progress = (double)index / (totalCount - 1);

            return progress switch
            {
                < 0.33 => "tom_high",
                < 0.67 => "tom_mid",
                _ => "tom_low"
            };
        }
    }
}
