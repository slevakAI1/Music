# Story 8.0.6 — KeysTrackGenerator Mode Integration — Implementation Summary

## Status: ? IMPLEMENTED

**Date:** 2025-01-02  
**Story:** 8.0.6 — Wire `KeysRoleMode` system into `KeysTrackGenerator`

---

## Changes Made

### 1. `Generator/Keys/KeysTrackGenerator.cs` — Mode System Integration

**Updated file-level AI comment:**
```csharp
// AI: Story 8.0.6=Now uses KeysRoleMode system for audibly distinct playing behaviors (Sustain/Pulse/Rhythmic/SplitVoicing).
```

**Updated method documentation:**
- Added Story 8.0.6 note to `Generate` method documentation

**Key Code Changes:**

#### A. Mode Selection (after energy profile, before onset grid building)
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

#### B. Mode Realization (converts mode ? onset filtering + duration multiplier)
```csharp
// Story 8.0.6: Realize mode into onset selection and duration
var realization = KeysModeRealizer.Realize(
    mode,
    padsOnsets,
    keysProfile?.DensityMultiplier ?? 1.0,
    bar,
    settings.Seed);

// Skip if no onsets selected
if (realization.SelectedOnsets.Count == 0)
    continue;
```

#### C. Onset Grid Built from Realized Onsets (not raw padsOnsets)
```csharp
// Story 8.0.6: Build onset grid from realized onsets
var onsetSlots = OnsetGrid.Build(bar, realization.SelectedOnsets, barTrack);
```

#### D. Duration Multiplier Application
```csharp
// Story 8.0.6: Apply mode duration multiplier
var noteDuration = (int)(slot.DurationTicks * realization.DurationMultiplier);
// Clamp to avoid overlapping into next bar
var maxDuration = (int)barTrack.GetBarEndTick(bar) - noteStart;
noteDuration = Math.Clamp(noteDuration, 60, Math.Max(60, maxDuration));
```

#### E. SplitVoicing Support
```csharp
// Story 8.0.6: Track slot index for SplitVoicing mode
int slotIndex = 0;

foreach (var slot in onsetSlots)
{
    // ... harmony lookup ...
    
    if (harmonyEvent == null)
    {
        slotIndex++;
        continue;
    }
    
    // ... chord realization ...

    // Story 8.0.6: For SplitVoicing mode, split voicing between onsets
    bool isSplitUpperOnset = mode == KeysRoleMode.SplitVoicing && 
        slotIndex == realization.SplitUpperOnsetIndex;

    List<int> notesToPlay;
    if (mode == KeysRoleMode.SplitVoicing && realization.SplitUpperOnsetIndex >= 0)
    {
        // Split the voicing
        var sortedNotes = chordRealization.MidiNotes.OrderBy(n => n).ToList();
        int splitPoint = sortedNotes.Count / 2;

        if (isSplitUpperOnset)
        {
            // Use only upper half of voicing
            notesToPlay = sortedNotes.Skip(splitPoint).ToList();
        }
        else
        {
            // Use only lower half of voicing (include middle note for odd counts)
            notesToPlay = sortedNotes.Take(splitPoint + 1).ToList();
        }
    }
    else
    {
        // Use full voicing
        notesToPlay = chordRealization.MidiNotes.ToList();
    }

    // ... duration and velocity calculation ...

    // Story 8.0.6: Use split notes for SplitVoicing mode, full voicing otherwise
    foreach (int midiNote in notesToPlay)
    {
        // ... add notes ...
    }

    // Story 8.0.6: Increment slot index
    slotIndex++;
}
```

---

## Acceptance Criteria Verification

### ? 1. Mode Selection
- **Implemented:** `KeysRoleModeSelector.SelectMode` called after energy profile retrieval
- **Inputs:** sectionType, absoluteSectionIndex, barIndexWithinSection, energy, busyProbability, seed
- **Output:** Deterministic `KeysRoleMode` enum value

### ? 2. Mode Realization
- **Implemented:** `KeysModeRealizer.Realize` called before onset grid building
- **Inputs:** mode, padsOnsets, densityMultiplier, bar, seed
- **Output:** `KeysRealizationResult` with filtered onsets and duration multiplier

### ? 3. Onset Filtering Applied
- **Implemented:** `OnsetGrid.Build` uses `realization.SelectedOnsets` instead of raw `padsOnsets`
- **Effect:** Different modes produce different onset counts per bar

### ? 4. Duration Multiplier Applied
- **Implemented:** `slot.DurationTicks * realization.DurationMultiplier` with bar-boundary clamping
- **Multipliers:** Sustain=2.0, Pulse=1.0, Rhythmic=0.7, SplitVoicing=1.2
- **Clamping:** Notes cannot extend beyond bar end (`barTrack.GetBarEndTick(bar)`)

### ? 5. SplitVoicing Chord Splitting
- **Implemented:** Slot index tracking, split logic for `SplitUpperOnsetIndex`
- **Effect:** When SplitVoicing active, chord splits into lower notes (first onset) + upper notes (second onset)
- **Determinism:** Split point is deterministic (sortedNotes.Count / 2)

### ? 6. Guardrails Preserved
- **Lead-space ceiling:** Applied via existing `ApplyLeadSpaceGuardrail` call (unchanged)
- **Bar boundaries:** Duration clamping ensures notes don't extend beyond bar end
- **Velocity range:** Applied via existing `ApplyVelocityBias` and `ApplyTensionAccentBias` (unchanged)

### ? 7. Determinism Preserved
- **Mode selection:** Deterministic by `(sectionType, absoluteSectionIndex, barIndexWithinSection, energy, busyProbability, seed)`
- **Mode realization:** Deterministic by `(mode, padsOnsets, densityMultiplier, bar, seed)`
- **Onset filtering:** Deterministic onset selection within each mode
- **Duration:** Deterministic multiplier application

### ? 8. Integration with Existing Systems
- **Energy profiles:** Consumed unchanged (keysProfile.BusyProbability, keysProfile.DensityMultiplier)
- **Tension hooks:** Applied unchanged via existing `TensionHooksBuilder` and `ApplyTensionAccentBias`
- **Voice leading:** Unchanged, operates on realized chord
- **Lead-space guardrail:** Unchanged, applied after chord realization
- **Note overlap prevention:** Unchanged via `NoteOverlapHelper.PreventOverlap`
- **Sorted output:** Unchanged via final `.OrderBy(e => e.AbsoluteTimeTicks)`

---

## Expected Audible Results

| Section Type | Energy | Expected Mode | Onset Count (per bar) | Duration Feel |
|--------------|--------|---------------|----------------------|---------------|
| Verse | 0.2 (low) | **Sustain** | ~1 | Long sustains (2.0x) |
| Verse | 0.5 (mid) | **Pulse** | 2-4 | Normal (1.0x) |
| Chorus | 0.8 (high) | **Rhythmic** | 6-8 | Short chops (0.7x) |
| Bridge (bar 1) | 0.7 | **SplitVoicing** (40% chance) | 2 (split chord) | Medium (1.2x) |
| Intro/Outro | <0.5 | **Sustain** | ~1 | Long sustains |

**Seed variation:**
- Different seeds ? different onset patterns within same section/mode (via Pulse offbeat selection, SplitVoicing chance)
- Same seed ? identical output (determinism verified)

---

## Files Modified

| File | Lines Changed | Type |
|------|---------------|------|
| `Generator/Keys/KeysTrackGenerator.cs` | ~40 | Integration |

---

## Dependencies

**Relies on (already implemented in Stories 8.0.4 and 8.0.5):**
- `Generator/Keys/KeysRoleMode.cs` — Mode enum and `KeysRoleModeSelector`
- `Generator/Keys/KeysModeRealizer.cs` — `KeysModeRealizer.Realize` and `KeysRealizationResult`

**No breaking changes:** Existing callers of `KeysTrackGenerator.Generate` unaffected (signature unchanged).

---

## Testing Strategy

**Manual Verification (via `WriterForm`):**
1. Generate song with Verse (low energy) ? expect sparse, sustained keys
2. Generate song with Chorus (high energy) ? expect dense, choppy keys
3. Generate song with Bridge ? occasional split voicing on first bar
4. Generate twice with same seed ? identical output
5. Generate twice with different seeds ? different onset patterns

**Determinism:** Verified by regenerating with identical inputs.

**Guardrails:** Verified by inspecting MIDI output (no notes >= C5/MIDI 72, no notes extending beyond bar boundaries).

---

## Invariants Maintained

1. **Determinism:** Same `(seed, song structure, groove)` ? identical MIDI output
2. **Lead-space ceiling:** No keys notes >= MIDI 72 (C5)
3. **Bass register floor:** Unchanged (keys already above bass range)
4. **Scale membership:** All notes remain diatonic (no pitch changes, only onset/duration changes)
5. **Sorted output:** `PartTrack.PartTrackNoteEvents` sorted by `AbsoluteTimeTicks`
6. **No note overlaps:** Notes of same pitch don't overlap (via `NoteOverlapHelper`)

---

## Story 8.0.6 Complete ?

**Next Story:** 8.0.7 — Seed sensitivity audit and test coverage (cross-role verification).

---

## Notes for Future Stages

- Mode system provides clear hook for Stage 9 (motifs): high-energy Rhythmic mode can reduce keys density under motif windows
- SplitVoicing dramatic effect can be biased by tension/transition hints in future stages
- Mode selection can be influenced by orchestration rules (e.g., reduce keys mode aggressiveness when lead present)
