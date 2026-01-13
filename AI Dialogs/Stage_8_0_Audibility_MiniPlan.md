# Stage 8.0 — Audible Part Intelligence Pass (Comp + Keys)

**Priority:** Immediately after Stage 7 (before Stage 8 PhraseMap work)  
**Goal:** Make Verse vs Chorus vs PreChorus differences **clearly audible** before building motif/melody systems.  
**Rationale:** Stage 7 computes energy/tension intent, but current render logic barely uses it audibly. This pass ensures infrastructure produces real musical contrast.

---

## Current Problems (Why It Sounds Same-y)

### Problem 1: Comp (GuitarTrackGenerator) rhythm selection is not audibly varied
**Location:** `Generator\Guitar\GuitarTrackGenerator.cs`, method `ApplyDensityToPattern()` (lines 184-209)  
**Issue:** Always takes "first N" indices from `pattern.IncludedOnsetIndices`, so:
- Different seeds don't change which slots are chosen
- Different sections use the same "most important" hits
- Density changes sound like "same pattern, slightly busier" not "different comp behavior"

### Problem 2: Register lift rounded to octaves, then often undone by guardrails
**Location:** `Generator\Guitar\GuitarTrackGenerator.cs`, method `ApplyRegisterWithGuardrail()` (lines 217-260)  
**Issue:** 
- `RegisterLiftSemitones` is rounded to nearest octave (line 229: `int octaveShift = (int)Math.Round(registerLiftSemitones / 12.0) * 12`)
- Values like +2, +4, +7 become 0
- Lead-space ceiling (line 237-244) can push lifted voicings back down
- Net result: register lift rarely produces audible change

### Problem 3: Keys uses pads onsets directly with no rhythm variation
**Location:** `Generator\Keys\KeysTrackGenerator.cs` (lines 41-46)  
**Issue:** Keys just uses `grooveEvent.AnchorLayer.PadsOnsets` as-is. No pattern library, no density-based filtering, no behavioral modes. Every bar in a section sounds rhythmically identical.

### Problem 4: Duration is always "slot duration" — no sustain/chop variation
**Location:** Both generators use `slot.DurationTicks` directly (GuitarTrackGenerator line 157, KeysTrackGenerator line 171)  
**Issue:** Energy/tension never affects duration. High energy should mean shorter durations (more re-attacks), low energy should mean longer sustains.

### Problem 5: Seed doesn't meaningfully affect rhythm choices
**Current state:** Seed affects velocity jitter and some pitch randomization, but not which slots are played or how duration/behavior changes. Different seeds ? nearly identical output.

---

## Stories

### Story 8.0.1 — Create `CompBehavior` enum and deterministic selector

**Intent:** Define audibly-distinct comp behaviors that energy/tension/section can choose from.

**New file:** `Generator\Guitar\CompBehavior.cs`

**Acceptance criteria:**
```csharp
// AI: purpose=Deterministic selection of comp playing behavior based on energy/tension/section.
// AI: invariants=Selection is deterministic by (sectionType, absoluteSectionIndex, barIndex, energy, busyProbability, seed).
// AI: change=Add new behaviors by extending enum and updating SelectBehavior logic.

namespace Music.Generator
{
    /// <summary>
    /// Distinct playing behaviors for comp part that produce audibly different results.
    /// </summary>
    public enum CompBehavior
    {
        /// <summary>
        /// Sparse anchors: mostly strong beats, fewer hits, longer sustains.
        /// Typical for: Verse (low energy), Intro, Outro.
        /// </summary>
        SparseAnchors,
        
        /// <summary>
        /// Standard pattern: balanced strong/weak beats, moderate sustains.
        /// Typical for: Verse (mid energy), Bridge.
        /// </summary>
        Standard,
        
        /// <summary>
        /// Push/anticipate: adds anticipations into chord changes, shorter notes.
        /// Typical for: PreChorus, build sections.
        /// </summary>
        Anticipate,
        
        /// <summary>
        /// Syncopated chop: more offbeats, short durations, frequent re-attacks.
        /// Typical for: Chorus (high energy), Dance sections.
        /// </summary>
        SyncopatedChop,
        
        /// <summary>
        /// Driving full: all available onsets, consistent attacks, driving feel.
        /// Typical for: Chorus (max energy), Outro (big ending).
        /// </summary>
        DrivingFull
    }

    /// <summary>
    /// Deterministic selector for comp behavior based on musical context.
    /// </summary>
    public static class CompBehaviorSelector
    {
        /// <summary>
        /// Selects comp behavior deterministically from context.
        /// </summary>
        /// <param name="sectionType">Current section type (Verse, Chorus, etc.)</param>
        /// <param name="absoluteSectionIndex">0-based index of section in song</param>
        /// <param name="barIndexWithinSection">0-based bar index within section</param>
        /// <param name="energy">Section energy [0..1]</param>
        /// <param name="busyProbability">Comp busy probability [0..1]</param>
        /// <param name="seed">Master seed for deterministic variation</param>
        /// <returns>Selected CompBehavior</returns>
        public static CompBehavior SelectBehavior(
            MusicConstants.eSectionType sectionType,
            int absoluteSectionIndex,
            int barIndexWithinSection,
            double energy,
            double busyProbability,
            int seed)
        {
            // Clamp inputs
            energy = Math.Clamp(energy, 0.0, 1.0);
            busyProbability = Math.Clamp(busyProbability, 0.0, 1.0);

            // Combined activity score
            double activityScore = (energy * 0.6) + (busyProbability * 0.4);

            // Section-type specific thresholds and biases
            CompBehavior baseBehavior = sectionType switch
            {
                MusicConstants.eSectionType.Intro => activityScore < 0.4 ? CompBehavior.SparseAnchors : CompBehavior.Standard,
                MusicConstants.eSectionType.Verse => SelectVerseBehavior(activityScore),
                MusicConstants.eSectionType.Chorus => SelectChorusBehavior(activityScore),
                MusicConstants.eSectionType.Bridge => activityScore > 0.6 ? CompBehavior.Anticipate : CompBehavior.Standard,
                MusicConstants.eSectionType.Outro => activityScore > 0.7 ? CompBehavior.DrivingFull : CompBehavior.SparseAnchors,
                MusicConstants.eSectionType.Solo => CompBehavior.SparseAnchors, // Back off for solo
                _ => CompBehavior.Standard
            };

            // Apply deterministic per-bar variation using seed
            // Every 4th bar within section, consider upgrade/downgrade
            if (barIndexWithinSection > 0 && barIndexWithinSection % 4 == 0)
            {
                int variationHash = HashCode.Combine(seed, absoluteSectionIndex, barIndexWithinSection);
                bool shouldVary = (variationHash % 100) < 30; // 30% chance of variation
                
                if (shouldVary)
                {
                    baseBehavior = ApplyVariation(baseBehavior, variationHash);
                }
            }

            return baseBehavior;
        }

        private static CompBehavior SelectVerseBehavior(double activityScore)
        {
            return activityScore switch
            {
                < 0.35 => CompBehavior.SparseAnchors,
                < 0.55 => CompBehavior.Standard,
                < 0.75 => CompBehavior.Anticipate,
                _ => CompBehavior.SyncopatedChop
            };
        }

        private static CompBehavior SelectChorusBehavior(double activityScore)
        {
            return activityScore switch
            {
                < 0.3 => CompBehavior.Standard,
                < 0.5 => CompBehavior.Anticipate,
                < 0.75 => CompBehavior.SyncopatedChop,
                _ => CompBehavior.DrivingFull
            };
        }

        private static CompBehavior ApplyVariation(CompBehavior baseBehavior, int variationHash)
        {
            // Deterministic upgrade/downgrade based on hash
            bool upgrade = (variationHash % 2) == 0;
            
            return baseBehavior switch
            {
                CompBehavior.SparseAnchors => upgrade ? CompBehavior.Standard : CompBehavior.SparseAnchors,
                CompBehavior.Standard => upgrade ? CompBehavior.Anticipate : CompBehavior.SparseAnchors,
                CompBehavior.Anticipate => upgrade ? CompBehavior.SyncopatedChop : CompBehavior.Standard,
                CompBehavior.SyncopatedChop => upgrade ? CompBehavior.DrivingFull : CompBehavior.Anticipate,
                CompBehavior.DrivingFull => upgrade ? CompBehavior.DrivingFull : CompBehavior.SyncopatedChop,
                _ => baseBehavior
            };
        }
    }
}
```

**Tests required:**
- Determinism: same inputs ? same behavior
- Different sections ? different behaviors (at least 2 distinct behaviors across typical pop form)
- Seed affects variation within section

---

### Story 8.0.2 — Create `CompBehaviorRealizer` to apply behavior to onset selection + duration

**Intent:** Convert behavior + available onsets into actual onset selection and duration shaping.

**New file:** `Generator\Guitar\CompBehaviorRealizer.cs`

**Acceptance criteria:**
```csharp
// AI: purpose=Applies CompBehavior to onset selection and duration shaping.
// AI: invariants=Output onsets are valid subset of input; durations bounded by slot constraints; deterministic.
// AI: deps=Consumes CompBehavior, CompRhythmPattern, compOnsets; produces filtered onsets and duration multiplier.

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
                SelectedOnsets = selected.OrderBy(o => o).ToList(),
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
            int rotation = (bar + seed) % pattern.IncludedOnsetIndices.Count;
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
                SelectedOnsets = compOnsets.Take(count).OrderBy(o => o).ToList(),
                DurationMultiplier = 0.65 // Moderate chop for driving feel
            };
        }
    }
}
```

**Tests required:**
- Each behavior produces different onset selection for same input
- Duration multiplier is behavior-specific
- Determinism preserved
- Edge cases: empty onsets, 1 onset, all strong beats, all offbeats

---

### Story 8.0.3 — Update `GuitarTrackGenerator` to use behavior system + duration shaping

**Intent:** Wire behavior selector and realizer into actual generation.

**File:** `Generator\Guitar\GuitarTrackGenerator.cs`

**Changes:**

1. **Add behavior selection** (after getting energy profile, before pattern lookup):
```csharp
// Story 8.0.3: Select comp behavior based on energy/tension/section
var behavior = CompBehaviorSelector.SelectBehavior(
    sectionType,
    absoluteSectionIndex,
    barIndexWithinSection,
    energyProfile?.Global.Energy ?? 0.5,
    compProfile?.BusyProbability ?? 0.5,
    settings.Seed);
```

2. **Replace `ApplyDensityToPattern` with `CompBehaviorRealizer`**:
```csharp
// Story 8.0.3: Use behavior realizer for onset selection and duration
var realization = CompBehaviorRealizer.Realize(
    behavior,
    compOnsets,
    pattern,
    compProfile?.DensityMultiplier ?? 1.0,
    bar,
    settings.Seed);

// Skip if no onsets selected
if (realization.SelectedOnsets.Count == 0)
    continue;

// Build onset grid from realized onsets
var onsetSlots = OnsetGrid.Build(bar, realization.SelectedOnsets, barTrack);
```

3. **Apply duration multiplier**:
```csharp
// Story 8.0.3: Apply behavior duration multiplier
var noteDuration = (int)(slot.DurationTicks * realization.DurationMultiplier);
noteDuration = Math.Max(noteDuration, 60); // Minimum ~30ms at 120bpm
```

4. **Remove `ApplyDensityToPattern` method** (replaced by `CompBehaviorRealizer`):
- Delete lines 184-209 (the old `ApplyDensityToPattern` method)

**Tests required:**
- Different sections produce different comp behaviors
- Different seeds produce audibly different bar-to-bar variation
- Duration multiplier affects note lengths
- Existing guardrails (lead-space, register) still work

---

### Story 8.0.4 — Create `KeysRoleMode` enum and deterministic selector

**Intent:** Define audibly-distinct keys/pads playing modes.

**New file:** `Generator\Keys\KeysRoleMode.cs`

**Acceptance criteria:**
```csharp
// AI: purpose=Deterministic selection of keys/pads playing mode based on energy/section.
// AI: invariants=Selection is deterministic; modes produce audibly different rhythmic behavior.

namespace Music.Generator
{
    /// <summary>
    /// Distinct playing modes for keys/pads that produce audibly different results.
    /// </summary>
    public enum KeysRoleMode
    {
        /// <summary>
        /// Sustain: hold chord across bar/half-bar, minimal re-attacks.
        /// Typical for: low energy sections, intros, outros.
        /// </summary>
        Sustain,

        /// <summary>
        /// Pulse: re-strike on selected beats, moderate sustain.
        /// Typical for: verses, mid-energy sections.
        /// </summary>
        Pulse,

        /// <summary>
        /// Rhythmic: follow pad onsets more closely, shorter notes.
        /// Typical for: choruses, high-energy sections.
        /// </summary>
        Rhythmic,

        /// <summary>
        /// SplitVoicing: split voicing across 2 hits (low notes first, then upper).
        /// Typical for: builds, transitions, dramatic moments.
        /// </summary>
        SplitVoicing
    }

    /// <summary>
    /// Deterministic selector for keys/pads playing mode.
    /// </summary>
    public static class KeysRoleModeSelector
    {
        /// <summary>
        /// Selects keys mode deterministically from context.
        /// </summary>
        public static KeysRoleMode SelectMode(
            MusicConstants.eSectionType sectionType,
            int absoluteSectionIndex,
            int barIndexWithinSection,
            double energy,
            double busyProbability,
            int seed)
        {
            energy = Math.Clamp(energy, 0.0, 1.0);
            busyProbability = Math.Clamp(busyProbability, 0.0, 1.0);
            
            double activityScore = (energy * 0.7) + (busyProbability * 0.3);

            KeysRoleMode baseMode = sectionType switch
            {
                MusicConstants.eSectionType.Intro => activityScore < 0.5 ? KeysRoleMode.Sustain : KeysRoleMode.Pulse,
                MusicConstants.eSectionType.Verse => SelectVerseMode(activityScore),
                MusicConstants.eSectionType.Chorus => SelectChorusMode(activityScore),
                MusicConstants.eSectionType.Bridge => SelectBridgeMode(activityScore, barIndexWithinSection, seed),
                MusicConstants.eSectionType.Outro => KeysRoleMode.Sustain,
                MusicConstants.eSectionType.Solo => KeysRoleMode.Sustain, // Back off for solo
                _ => KeysRoleMode.Pulse
            };

            return baseMode;
        }

        private static KeysRoleMode SelectVerseMode(double activityScore)
        {
            return activityScore switch
            {
                < 0.35 => KeysRoleMode.Sustain,
                < 0.65 => KeysRoleMode.Pulse,
                _ => KeysRoleMode.Rhythmic
            };
        }

        private static KeysRoleMode SelectChorusMode(double activityScore)
        {
            return activityScore switch
            {
                < 0.4 => KeysRoleMode.Pulse,
                _ => KeysRoleMode.Rhythmic
            };
        }

        private static KeysRoleMode SelectBridgeMode(double activityScore, int barIndexWithinSection, int seed)
        {
            // Bridge: consider SplitVoicing for dramatic effect on first bar
            if (barIndexWithinSection == 0 && activityScore > 0.5)
            {
                int hash = HashCode.Combine(seed, barIndexWithinSection);
                if ((hash % 100) < 40) // 40% chance of split voicing at bridge start
                {
                    return KeysRoleMode.SplitVoicing;
                }
            }
            
            return activityScore > 0.5 ? KeysRoleMode.Rhythmic : KeysRoleMode.Pulse;
        }
    }
}
```

---

### Story 8.0.5 — Create `KeysModeRealizer` to apply mode to onset selection + duration

**Intent:** Convert mode + available onsets into actual onset filtering and duration shaping.

**New file:** `Generator\Keys\KeysModeRealizer.cs`

**Acceptance criteria:**
```csharp
// AI: purpose=Applies KeysRoleMode to onset selection and duration shaping for keys/pads.
// AI: invariants=Output onsets are valid subset of input; durations bounded; deterministic.

namespace Music.Generator
{
    /// <summary>
    /// Result of keys mode realization.
    /// </summary>
    public sealed class KeysRealizationResult
    {
        /// <summary>Onset beats to actually play.</summary>
        public required IReadOnlyList<decimal> SelectedOnsets { get; init; }
        
        /// <summary>Duration multiplier [0.5..2.0] applied to slot duration.</summary>
        public double DurationMultiplier { get; init; } = 1.0;
        
        /// <summary>For SplitVoicing mode: which onset is the "upper voicing" onset (others are lower).</summary>
        public int SplitUpperOnsetIndex { get; init; } = -1;
    }

    /// <summary>
    /// Converts KeysRoleMode into onset selection and duration shaping.
    /// </summary>
    public static class KeysModeRealizer
    {
        /// <summary>
        /// Realizes mode into onset selection and duration multiplier.
        /// </summary>
        public static KeysRealizationResult Realize(
            KeysRoleMode mode,
            IReadOnlyList<decimal> padsOnsets,
            double densityMultiplier,
            int bar,
            int seed)
        {
            if (padsOnsets == null || padsOnsets.Count == 0)
            {
                return new KeysRealizationResult
                {
                    SelectedOnsets = Array.Empty<decimal>(),
                    DurationMultiplier = 1.0
                };
            }

            densityMultiplier = Math.Clamp(densityMultiplier, 0.5, 2.0);

            return mode switch
            {
                KeysRoleMode.Sustain => RealizeSustain(padsOnsets),
                KeysRoleMode.Pulse => RealizePulse(padsOnsets, densityMultiplier, bar, seed),
                KeysRoleMode.Rhythmic => RealizeRhythmic(padsOnsets, densityMultiplier),
                KeysRoleMode.SplitVoicing => RealizeSplitVoicing(padsOnsets, bar, seed),
                _ => RealizePulse(padsOnsets, densityMultiplier, bar, seed)
            };
        }

        /// <summary>
        /// Sustain: only first onset of bar, long duration.
        /// </summary>
        private static KeysRealizationResult RealizeSustain(IReadOnlyList<decimal> padsOnsets)
        {
            return new KeysRealizationResult
            {
                SelectedOnsets = new[] { padsOnsets[0] },
                DurationMultiplier = 2.0 // Extend beyond slot
            };
        }

        /// <summary>
        /// Pulse: selected strong beats, normal duration.
        /// </summary>
        private static KeysRealizationResult RealizePulse(
            IReadOnlyList<decimal> padsOnsets,
            double densityMultiplier,
            int bar,
            int seed)
        {
            // Prefer strong beats (integer values)
            var strongBeats = padsOnsets.Where(o => o == Math.Floor(o)).ToList();
            var offbeats = padsOnsets.Where(o => o != Math.Floor(o)).ToList();

            int targetCount = Math.Max(1, (int)Math.Round(padsOnsets.Count * densityMultiplier * 0.5));
            targetCount = Math.Min(targetCount, padsOnsets.Count);

            var selected = new List<decimal>();
            
            // Always include beat 1 if present
            if (strongBeats.Any(b => b == 1m))
            {
                selected.Add(1m);
                strongBeats = strongBeats.Where(b => b != 1m).ToList();
            }

            // Add remaining strong beats
            selected.AddRange(strongBeats.Take(targetCount - selected.Count));

            // Fill with offbeats if needed (deterministic selection based on seed)
            if (selected.Count < targetCount)
            {
                int hash = HashCode.Combine(seed, bar);
                int skipIndex = hash % Math.Max(1, offbeats.Count);
                var selectedOffbeats = offbeats.Where((_, i) => i != skipIndex).Take(targetCount - selected.Count);
                selected.AddRange(selectedOffbeats);
            }

            return new KeysRealizationResult
            {
                SelectedOnsets = selected.OrderBy(o => o).ToList(),
                DurationMultiplier = 1.0
            };
        }

        /// <summary>
        /// Rhythmic: use most/all onsets, shorter duration.
        /// </summary>
        private static KeysRealizationResult RealizeRhythmic(
            IReadOnlyList<decimal> padsOnsets,
            double densityMultiplier)
        {
            int targetCount = (int)Math.Ceiling(padsOnsets.Count * Math.Min(densityMultiplier, 1.3));
            targetCount = Math.Min(targetCount, padsOnsets.Count);

            return new KeysRealizationResult
            {
                SelectedOnsets = padsOnsets.Take(targetCount).ToList(),
                DurationMultiplier = 0.7 // Shorter for rhythmic feel
            };
        }

        /// <summary>
        /// SplitVoicing: two onsets (low voicing first, upper voicing second).
        /// </summary>
        private static KeysRealizationResult RealizeSplitVoicing(
            IReadOnlyList<decimal> padsOnsets,
            int bar,
            int seed)
        {
            if (padsOnsets.Count < 2)
            {
                return new KeysRealizationResult
                {
                    SelectedOnsets = padsOnsets,
                    DurationMultiplier = 1.2,
                    SplitUpperOnsetIndex = -1
                };
            }

            // Select two onsets: first available and one near middle of bar
            var first = padsOnsets[0];
            var second = padsOnsets[padsOnsets.Count / 2];

            return new KeysRealizationResult
            {
                SelectedOnsets = new[] { first, second },
                DurationMultiplier = 1.2,
                SplitUpperOnsetIndex = 1 // Second onset gets upper voicing
            };
        }
    }
}
```

---

### Story 8.0.6 — Update `KeysTrackGenerator` to use mode system + duration shaping

**Intent:** Wire mode selector and realizer into actual generation.

**File:** `Generator\Keys\KeysTrackGenerator.cs`

**Changes:**

1. **Add mode selection** (after getting energy profile):
```csharp
// Story 8.0.6: Select keys mode based on energy/section
var mode = KeysRoleModeSelector.SelectMode(
    section?.SectionType ?? MusicConstants.eSectionType.Verse,
    absoluteSectionIndex,
    barIndexWithinSection,
    energyProfile?.Global.Energy ?? 0.5,
    keysProfile?.BusyProbability ?? 0.5,
    settings.Seed);
```

2. **Add mode realization** (before building onset grid):
```csharp
// Story 8.0.6: Realize mode into onset selection
var realization = KeysModeRealizer.Realize(
    mode,
    padsOnsets,
    keysProfile?.DensityMultiplier ?? 1.0,
    bar,
    settings.Seed);

// Skip if no onsets selected
if (realization.SelectedOnsets.Count == 0)
    continue;

// Build onset grid from realized onsets
var onsetSlots = OnsetGrid.Build(bar, realization.SelectedOnsets, barTrack);
```

3. **Apply duration multiplier in the note creation loop**:
```csharp
// Story 8.0.6: Apply mode duration multiplier
var noteDuration = (int)(slot.DurationTicks * realization.DurationMultiplier);
// Clamp to avoid overlapping into next bar
var maxDuration = (int)barTrack.GetBarEndTick(bar) - noteStart;
noteDuration = Math.Clamp(noteDuration, 60, Math.Max(60, maxDuration));
```

4. **Handle SplitVoicing mode** (in the slot loop):
```csharp
// Story 8.0.6: For SplitVoicing mode, split voicing between onsets
bool isSplitUpperOnset = mode == KeysRoleMode.SplitVoicing && 
    onsetSlots.IndexOf(slot) == realization.SplitUpperOnsetIndex;

if (isSplitUpperOnset && chordRealization.MidiNotes.Count > 2)
{
    // Use only upper half of voicing
    var upperNotes = chordRealization.MidiNotes.OrderBy(n => n).Skip(chordRealization.MidiNotes.Count / 2).ToList();
    // Use upperNotes instead of full chordRealization
}
else if (mode == KeysRoleMode.SplitVoicing && !isSplitUpperOnset && chordRealization.MidiNotes.Count > 2)
{
    // Use only lower half of voicing
    var lowerNotes = chordRealization.MidiNotes.OrderBy(n => n).Take(chordRealization.MidiNotes.Count / 2 + 1).ToList();
    // Use lowerNotes instead of full chordRealization
}
```

**Tests required:**
- Different sections produce different modes
- Duration multiplier affects sustain/chop
- SplitVoicing correctly splits the chord
- Existing guardrails (lead-space, register) still work

---

### Story 8.0.7 — Seed sensitivity audit and test coverage

**Intent:** Verify seed meaningfully affects output.

**New test file:** `Generator\Tests\SeedSensitivityTests.cs` (or add to existing test location)

**Acceptance criteria:**
```csharp
// Tests to verify seed affects audible output

[Fact]
public void DifferentSeeds_ProduceDifferentCompBehaviors_WithinSameSection()
{
    // Generate 8-bar verse with seed 1
    // Generate 8-bar verse with seed 2
    // Assert: at least 1 bar has different behavior or different onset selection
}

[Fact]
public void DifferentSeeds_ProduceDifferentKeysModes_InBridgeSection()
{
    // Bridge SplitVoicing has seed-based chance
    // Verify different seeds produce different mode choices
}

[Fact]
public void SameSeed_ProducesIdenticalOutput()
{
    // Generate full song twice with same seed
    // Assert: identical note events (timing, pitch, velocity, duration)
}

[Fact]
public void VerseVsChorus_ProducesAudiblyDifferentBehaviors()
{
    // Generate verse bars and chorus bars
    // Assert: different behaviors selected (sparse vs syncopated/driving)
    // Assert: different duration multipliers
}
```

---

## Implementation Order

1. **8.0.1** — `CompBehavior` enum + selector (no breaking changes)
2. **8.0.2** — `CompBehaviorRealizer` (no breaking changes)
3. **8.0.3** — Wire into `GuitarTrackGenerator` (breaking: removes old method)
4. **8.0.4** — `KeysRoleMode` enum + selector (no breaking changes)
5. **8.0.5** — `KeysModeRealizer` (no breaking changes)
6. **8.0.6** — Wire into `KeysTrackGenerator` (breaking: changes onset logic)
7. **8.0.7** — Seed sensitivity tests

---

## File Summary

| Story | File | Action |
|-------|------|--------|
| 8.0.1 | `Generator\Guitar\CompBehavior.cs` | **CREATE** |
| 8.0.2 | `Generator\Guitar\CompBehaviorRealizer.cs` | **CREATE** |
| 8.0.3 | `Generator\Guitar\GuitarTrackGenerator.cs` | **MODIFY** |
| 8.0.4 | `Generator\Keys\KeysRoleMode.cs` | **CREATE** |
| 8.0.5 | `Generator\Keys\KeysModeRealizer.cs` | **CREATE** |
| 8.0.6 | `Generator\Keys\KeysTrackGenerator.cs` | **MODIFY** |
| 8.0.7 | `Generator\Tests\SeedSensitivityTests.cs` | **CREATE** |

---

## Key Invariants (Must Not Break)

1. **Determinism**: Same `(seed, song structure, groove)` ? identical output
2. **Lead-space ceiling**: Comp/keys never exceed MIDI 72 (C5)
3. **Bass register floor**: Comp never below MIDI 52 (E3)
4. **Scale membership**: All notes remain diatonic (octave shifts only)
5. **Sorted output**: `PartTrack.PartTrackNoteEvents` sorted by `AbsoluteTimeTicks`
6. **No overlaps**: Notes of same pitch don't overlap (via `NoteOverlapHelper`)

---

## Expected Audible Results

After implementation:
- **Verse**: Sparse anchors or standard comp, sustain/pulse keys ? calm, spacious
- **Chorus**: Syncopated chop or driving full comp, rhythmic keys ? energetic, busy
- **PreChorus/Bridge**: Anticipate comp, possible split voicing keys ? building tension
- **Different seeds**: Noticeably different bar-to-bar patterns within same structure
- **Different sections**: Obviously different rhythmic density and note length
