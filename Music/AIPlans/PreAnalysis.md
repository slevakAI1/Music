# Pre-Analysis: Story E1 — Implement Feel Timing (Straight/Swing/Shuffle/Triplet)

## 1. Story Intent Summary

**What:** Apply groove feel timing (straight, swing, shuffle, triplet) to onset positions, shifting offbeats and syncopations to create stylistically appropriate pocket timing.

**Why:** Creates the subtle timing shifts that make music feel human and stylistic. Without feel timing, all onsets land exactly on-grid, which sounds mechanical. This is essential for authentic pop/rock/jazz grooves.

**Who:** 
- **Generator** produces more musical output with correct pocket
- **End-user** hears grooves that feel natural and stylistically appropriate
- **Developer** has deterministic, testable timing behavior

---

## 2. Acceptance Criteria Checklist

### Configuration Resolution (Priority/Fallback)
1. [ ] Read feel from `SegmentGrooveProfile.OverrideFeel` or fallback to `GrooveSubdivisionPolicy.Feel`
2. [ ] Read swing amount from `OverrideSwingAmount01` or fallback to `SwingAmount01`

### Feel Behaviors
3. [ ] `Straight`: no shift applied
4. [ ] `Swing`: shift offbeats later proportional to swing amount
5. [ ] `Shuffle`: map eighth offbeats toward triplet feel
6. [ ] `TripletFeel`: quantize eligible subdivisions to triplet grid (bounded)

### Safety & Validation
7. [ ] Ensure timing adjustments respect `AllowedSubdivisions` (no illegal slot creation)

### Testing
8. [ ] Unit tests for each feel mode with swing at 0, 0.5, 1.0

### Ambiguous or Unclear AC
- **AC 4** (Swing): "proportional to swing amount" — what is the exact formula?
- **AC 5** (Shuffle): How does shuffle differ from swing? Is it a fixed triplet mapping?
- **AC 6** (TripletFeel): What are "eligible subdivisions"? What does "bounded" mean?
- **AC 7**: How is a slot "illegal"? What happens to onsets that would land on illegal slots?

---

## 3. Dependencies & Integration Points

### Depends On (Must Be Complete)
| Story | Provides |
|-------|----------|
| A1 | `GrooveOnset` record with `TimingOffsetTicks` field |
| A2 | RNG stream policy (for any timing jitter, though E1 may not use RNG) |
| D1 | Grid-aware onset strength classification (uses `AllowedSubdivision`) |

### Existing Code/Types to Interact With
- `GrooveFeel` enum (`Straight`, `Swing`, `Shuffle`, `TripletFeel`)
- `GrooveSubdivisionPolicy` (holds `Feel`, `SwingAmount01`, `AllowedSubdivisions`)
- `SegmentGrooveProfile` (holds `OverrideFeel`, `OverrideSwingAmount01`)
- `AllowedSubdivision` flags enum (`Quarter`, `Eighth`, `Sixteenth`, `EighthTriplet`, `SixteenthTriplet`)
- `GrooveOnset` record (has `Beat`, `TimingOffsetTicks`)
- `OnsetGrid` / `OnsetGridBuilder` (validates beat positions against allowed subdivisions)
- `MusicConstants.TicksPerQuarterNote` (480 ticks)

### Provides For (Future Stories)
| Story | Uses |
|-------|------|
| E2 | Role timing (adds per-role bias ON TOP of feel timing) |
| H1/H2 | Test suite and regression locks |

---

## 4. Inputs & Outputs

### Inputs (Consumes)
| Source | Field | Type | Purpose |
|--------|-------|------|---------|
| `SegmentGrooveProfile` | `OverrideFeel` | `GrooveFeel?` | Segment-specific feel override |
| `SegmentGrooveProfile` | `OverrideSwingAmount01` | `double?` | Segment-specific swing intensity |
| `GrooveSubdivisionPolicy` | `Feel` | `GrooveFeel` | Base feel setting |
| `GrooveSubdivisionPolicy` | `SwingAmount01` | `double` | Base swing intensity (0..1) |
| `GrooveSubdivisionPolicy` | `AllowedSubdivisions` | `AllowedSubdivision` | Legal grid positions |
| (Input) | Onset list | `IReadOnlyList<GrooveOnset>` | Onsets to apply feel to |

### Outputs (Produces)
| Type | Description |
|------|-------------|
| `IReadOnlyList<GrooveOnset>` | New onset list with `TimingOffsetTicks` modified |

### Configuration Read
- `MusicConstants.TicksPerQuarterNote` (480) for tick calculations

---

## 5. Constraints & Invariants

### Must ALWAYS Be True
1. **Determinism**: Same inputs (feel, swing amount, onsets) → identical output
2. **Immutability**: Produce new `GrooveOnset` records, never mutate input
3. **Legal slots only**: Resulting timing must not create positions outside `AllowedSubdivisions`
4. **SwingAmount01 range**: Must handle values at 0.0 and 1.0 correctly
5. **Downbeats untouched**: Straight beats (beat 1, 2, 3, 4) should typically not shift

### Hard Limits
- `SwingAmount01` is `[0..1]` — values outside range need defined behavior
- Timing offset is in ticks (integer) — need defined rounding behavior
- Maximum shift should be bounded (can't shift an offbeat past the next beat)

### Operation Order
1. Resolve effective feel (segment override → policy fallback)
2. Resolve effective swing amount (segment override → policy fallback)
3. For each onset, determine if it's eligible for shifting
4. Calculate shift amount based on feel type and swing amount
5. Apply shift as `TimingOffsetTicks` delta
6. Validate against `AllowedSubdivisions` (or skip/clamp if illegal)

---

## 6. Edge Cases to Test

### Boundary Conditions
- `SwingAmount01 = 0.0` — should produce minimal or no shift
- `SwingAmount01 = 1.0` — should produce maximum shift
- `SwingAmount01 < 0.0` or `> 1.0` — clamp or error?
- Empty onset list — should return empty without error
- Onset already at triplet position when applying TripletFeel
- Onset at bar boundary (beat 4.5 or 4.75 shifting near bar end)

### Null/Empty Checks
- `OverrideFeel` is null (must use fallback)
- `OverrideSwingAmount01` is null (must use fallback)
- Both overrides null (pure fallback mode)
- `AllowedSubdivisions` is `None` — what happens?

### Feel-Specific Edge Cases
- **Straight**: Verify absolutely no shift even when swing amount is 1.0
- **Swing**: Quarter notes (downbeats) should not shift
- **Swing**: What about sixteenth offbeats (beat 1.25, 1.75)?
- **Shuffle**: Sixteenth notes — do they shift too or only eighths?
- **TripletFeel**: Eighth notes at 1.5 — where do they quantize?

### Combination Scenarios
- `AllowedSubdivisions` = `Quarter | Eighth` but TripletFeel requested — conflict?
- Segment override feel differs from policy feel
- Segment override swing = 0.0 but policy swing = 0.7

### Error Cases
- Invalid `GrooveFeel` enum value (future-proofing)
- Onset with `TimingOffsetTicks` already set — additive or replace?

---

## 7. Clarifying Questions

This section intentionally resolves the previously-open questions into **explicit rules** that Story E1 must follow.

### E1-1. Effective feel + swing resolution

- **EffectiveFeel** = `SegmentGrooveProfile.OverrideFeel ?? GrooveSubdivisionPolicy.Feel`
- **EffectiveSwingAmount01** = `SegmentGrooveProfile.OverrideSwingAmount01 ?? GrooveSubdivisionPolicy.SwingAmount01`
- **SwingAmount01 clamping**: clamp to `[0.0 .. 1.0]` before use.

### E1-2. What is modified (Beat vs TimingOffsetTicks)

- **Do not change** `GrooveOnset.Beat`.
- Apply feel as a change to **`GrooveOnset.TimingOffsetTicks` only**.
- If `TimingOffsetTicks` is already set, feel timing is **additive**:
  - `FinalTimingOffsetTicks = (existingTimingOffsetTicks ?? 0) + feelOffsetTicks`

Rationale: keeps the rhythmic plan on-grid (beats remain legal), while playback gains pocket.

### E1-3. Eligibility: which onsets can be shifted by feel

- For all feel modes, only onsets that are **eighth offbeats** are eligible for feel shifting.
  - Eligible beat pattern: `beat = integer + 0.5` (within tolerance used elsewhere, see D1 epsilon `0.002`).
- **Quarter-note downbeats** (integer beats) are never shifted by E1.
- **Sixteenth positions** (e.g., `integer + 0.25`, `+0.75`) are not shifted by E1.
- **Triplet positions** are not shifted by E1 (to avoid double-feel).

Note: E2 (role microtiming) can still apply to any onset; E1 is intentionally limited to classic 8th-feel.

### E1-4. Time bounds ("bounded" semantics)

- Any feel shift must remain within the containing beat interval.
- For an eligible offbeat at `n + 0.5`, the shifted time must remain in:
  - `[n + 0.5 .. n + 1.0)` (i.e., never earlier than the straight offbeat, never at/after the next downbeat).

Implementation detail is free, but tests should ensure shifted offsets never cross into the next beat.

### E1-5. Swing behavior (formula)

- Swing acts only on eligible eighth offbeats (`n + 0.5`).
- Define the "swing target" as the **second triplet eighth position** inside the beat:
  - Straight offbeat: `n + 0.5`
  - Triplet swing target: `n + 2/3`
- Swing amount blends from straight to target:
  - At `SwingAmount01 = 0.0` → no shift (stays at `n + 0.5`)
  - At `SwingAmount01 = 1.0` → full shift to `n + 2/3`
  - At intermediate values → linear interpolation between the two positions.

### E1-6. Shuffle behavior (definition and relationship to SwingAmount01)

- Shuffle is a **hard triplet mapping** for eighth offbeats:
  - Eligible `n + 0.5` maps to `n + 2/3`.
- Shuffle **ignores SwingAmount01** (treat as fully-shuffled regardless of 0..1).

This distinguishes Shuffle (fixed template) vs Swing (continuous control).

### E1-7. TripletFeel behavior (definition)

- `TripletFeel` is a **quantizing feel** that moves eligible eighth offbeats toward the triplet grid.
- In Story E1, to keep scope deterministic and aligned with other feel modes:
  - Only `n + 0.5` is quantized, and it quantizes to `n + 2/3` (same target as swing/shuffle).
- `TripletFeel` **ignores SwingAmount01** (it is a feel mode, not an intensity).

This makes `TripletFeel` effectively equivalent to Shuffle for the E1 scope; future stories may increase breadth.

### E1-8. AllowedSubdivisions interaction ("no illegal slot creation")

Because E1 modifies `TimingOffsetTicks` and not `Beat`, E1 **must not create new beat slots**.

Rules to satisfy the Acceptance Criteria without changing beat placement:
- If `AllowedSubdivisions` does **not** include `Eighth`, then there should be **no eligible eighth-offbeat onsets**.
  - Result: E1 applies no feel offsets.
- If onsets contain eighth offbeats but `AllowedSubdivisions` forbids `Eighth`, treat this as inconsistent input.
  - Story E1 behavior: **do not shift** those onsets (leave `TimingOffsetTicks` unchanged).

Notes:
- E1 does **not** require `EighthTriplet` to be allowed, since it is not changing `Beat` to a triplet value.
- If later you choose to interpret "slot creation" as "audible triplet feel requires triplet grid allowed",
  that belongs in a future policy/validation story; E1 stays non-invasive.

### E1-9. Bar-boundary behavior

- Since E1 only shifts offbeats later within a beat and is bounded to `< next downbeat`, it never crosses bar boundaries.
- For any onset on the last eligible offbeat in a bar (e.g., beat `BeatsPerBar - 0.5`), the shift still stays within that beat.

### E1-10. Per-role feel (scope)

- Story E1 applies **uniformly for all roles** using the bar's effective feel settings.
- Per-role push/pull and per-role feel overrides are handled by Story E2 and/or `GroovePolicyDecision` hooks.

---

## 8. Test Scenario Ideas

### Unit Test Names (Per AC)

#### Configuration Resolution Tests
- `ResolveFeel_UsesOverride_WhenOverrideProvided`
- `ResolveFeel_UsesFallback_WhenOverrideNull`
- `ResolveSwingAmount_UsesOverride_WhenOverrideProvided`
- `ResolveSwingAmount_UsesFallback_WhenOverrideNull`

#### Straight Feel Tests
- `Straight_NoShift_WhenSwingZero`
- `Straight_NoShift_WhenSwingHalf`
- `Straight_NoShift_WhenSwingOne`
- `Straight_PreservesExistingTimingOffset`

#### Swing Feel Tests
- `Swing_NoShift_WhenSwingZero`
- `Swing_PartialShift_WhenSwingHalf`
- `Swing_MaxShift_WhenSwingOne`
- `Swing_DownbeatsUnaffected`
- `Swing_OffbeatsShiftLater`
- `Swing_SixteenthOffsetsHandledCorrectly`

#### Shuffle Feel Tests
- `Shuffle_MapsEighthOffbeatToTriplet_WhenSwingZero`
- `Shuffle_MapsEighthOffbeatToTriplet_WhenSwingHalf`
- `Shuffle_MapsEighthOffbeatToTriplet_WhenSwingOne`
- `Shuffle_QuarterNotesUnaffected`

#### TripletFeel Tests
- `TripletFeel_QuantizesEighthsToTripletGrid`
- `TripletFeel_BoundedQuantization_DoesNotExceedLimit`
- `TripletFeel_AlreadyOnTripletGrid_NoChange`

#### AllowedSubdivisions Validation Tests
- `AllowedSubdivisions_PreventsIllegalSlotCreation`
- `AllowedSubdivisions_ClampsToNearestLegalSlot`
- `AllowedSubdivisions_SkipsShiftWhenConflict`

#### Determinism Tests
- `SameInputs_ProduceSameTimingOffsets`
- `DifferentSwingAmounts_ProduceDifferentOffsets`

### Test Data Setups
- **Simple 4/4 bar**: Onsets at 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5 (straight eighths)
- **Sixteenth grid**: Onsets at 1.0, 1.25, 1.5, 1.75 (test which shift)
- **Mixed grid**: Quarter + triplet positions (test no double-shift)
- **Edge positions**: Beat 4.75 (near bar end), beat 1.0 (downbeat)

### Determinism Verification Points
- Run same onset list through feel timing twice → assert identical `TimingOffsetTicks`
- Serialize output and compare to golden snapshot
- Verify tick values are exact integers (no floating-point drift)

---

## Summary

Story E1 introduces groove feel timing, which is essential for musical authenticity. The core challenge is defining precise shift formulas for each feel type while respecting subdivision constraints.

**Highest priority clarifications needed:**
1. Exact swing shift formula (Q1)
2. Swing vs Shuffle distinction (Q2)
3. What modifies — Beat or TimingOffsetTicks (Q4)
4. How to handle AllowedSubdivisions conflicts (Q6)

Once these are answered, implementation can proceed with clear deterministic rules.
