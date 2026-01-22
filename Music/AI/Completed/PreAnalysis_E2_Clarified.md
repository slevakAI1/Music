# Pre-Analysis: Story E2 — Implement Role Timing Feel + Bias + Clamp (CLARIFIED)

## 1. Story Intent Summary

- **What**: Apply per-role micro-timing (feel/bias) on top of global feel timing so each role can sit ahead, on-top, behind, or laid-back relative to the bar grid.
- **Why**: Allows musical roles (kick, snare, hats, comp, bass) to have distinct pocket and microtiming, enabling more expressive and stylistically correct grooves without changing beats.
- **Who benefits**: Generator (more expressive outputs), end-user (more human-feeling grooves), developers (deterministic, testable policy-driven microtiming).

---

## 2. Acceptance Criteria Checklist

1. Read `GrooveTimingPolicy.RoleTimingFeel[role]`.
2. Convert `TimingFeel` enum to a base tick offset direction:
   - Ahead: negative ticks
   - OnTop: zero
   - Behind: positive ticks
   - LaidBack: larger positive ticks
3. Apply `GrooveTimingPolicy.RoleTimingBiasTicks[role]` (nominal tick bias per role).
4. Respect `GroovePolicyDecision.RoleTimingFeelOverride` and `RoleTimingBiasTicksOverride` when provided.
5. Clamp combined per-role timing by `GrooveTimingPolicy.MaxAbsTimingBiasTicks`.
6. Unit tests verify clamping and override precedence.

**Clarified**: See Section 7 for precise numeric mappings, precedence rules, clamping semantics, and operation order.

---

## 3. Dependencies & Integration Points

- **Depends on (must be present)**:
  - Story E1 (FeelTimingEngine) — E2 applies on top of feel timing offsets.
  - Story D1/D2 (Onset strength/velocity) for test data setup consistency.
  - GroovePolicyDecision and IGroovePolicyProvider (A3) for overrides.
- **Interacts with existing code/types**:
  - `GrooveTimingPolicy`, `TimingFeel` enum
  - `GroovePolicyDecision` (override fields: `RoleTimingFeelOverride`, `RoleTimingBiasTicksOverride`)
  - `GrooveOnset.TimingOffsetTicks` (must be immutable copy-with applied)
  - `MusicConstants.TicksPerQuarterNote` (480) for tick calculations
- **Provides for future stories**:
  - Story E2 output feeds Story H1/H2 tests and Story E2-per-role tuning (operator/drummer policies)
  - Enables per-role diagnostic traces (G1)

---

## 4. Inputs & Outputs

**Inputs**:
- `IReadOnlyList<GrooveOnset>` (existing onsets, possibly with TimingOffsetTicks from E1).
- `GrooveTimingPolicy` (RoleTimingFeel, RoleTimingBiasTicks, MaxAbsTimingBiasTicks).
- `GroovePolicyDecision` (optional per-bar/per-role overrides).

**Outputs**:
- `IReadOnlyList<GrooveOnset>` new records with updated `TimingOffsetTicks` (additive to existing offsets).

**Configuration read**:
- `GrooveTimingPolicy.MaxAbsTimingBiasTicks` (clamp magnitude)
- Per-role mapping in `GrooveTimingPolicy.RoleTimingBiasTicks` and `RoleTimingFeel`
- Optional overrides in `GroovePolicyDecision`

---

## 5. Constraints & Invariants

**Must ALWAYS be true**:
- **Determinism**: same inputs + overrides produce same outputs.
- **Immutability**: do not mutate input onsets; return new records.
- **Do not change** `GrooveOnset.Beat` (only adjust `TimingOffsetTicks`).
- **Final offset clamping**: After applying role timing, clamp the **final combined offset** (E1 + E2) to `[-MaxAbsTimingBiasTicks .. +MaxAbsTimingBiasTicks]`.
- **Respect override precedence**: explicit `GroovePolicyDecision` overrides win over base policy values.

**Hard limits**:
- `MaxAbsTimingBiasTicks` is authoritative; timing bias must not exceed it.
- Timing offsets are integer ticks.
- Per-role bias is additive to E1 feel timing.

**Operation order (canonical)**:
1. Resolve effective role feel and bias (policy → per-bar override).
2. Map `TimingFeel` to base tick offset.
3. Add `RoleTimingBiasTicks` to base offset.
4. Add per-role bias to existing `TimingOffsetTicks` (which may include E1 feel).
5. Clamp **final offset** to `[-MaxAbsTimingBiasTicks .. +MaxAbsTimingBiasTicks]`.
6. Produce new `GrooveOnset` records.

---

## 6. Edge Cases to Test

- Role missing from `RoleTimingFeel` and/or `RoleTimingBiasTicks` (fallback to OnTop and 0 bias).
- `RoleTimingBiasTicks` values exceeding `MaxAbsTimingBiasTicks` (clamped at final step).
- `GroovePolicyDecision` provides only feel override or only bias override (partial overrides).
- Existing `TimingOffsetTicks` null vs non-null (treat null as 0, additive semantics).
- Combined large E1 feel offset + E2 role bias causing final offset to exceed max — verify clamping.
- Negative (ahead) vs positive (behind) semantics and clamping boundaries.
- Non-existent role (unknown role string) — default behavior.
- Determinism: repeated runs with same inputs and overrides produce identical outputs.
- Empty onset list, null inputs (guard and throw where appropriate).

**Combination scenarios**:
- High per-role bias combined with high E1 swing causing near-limit positions — verify clamping.
- Overrides that reduce bias to zero but base policy non-zero.
- Role with `Ahead` feel and large negative bias vs. `LaidBack` with large positive bias.

---

## 7. Clarifying Questions — ANSWERED

This section resolves the previously-open questions into **explicit rules** that Story E2 must follow.

### E2-1. Precise mapping: How should qualitative `TimingFeel` map to tick magnitudes?

**Answer:** Use the following **base tick offset** for each `TimingFeel` value:

| TimingFeel | Base Tick Offset | Musical Intent |
|------------|------------------|----------------|
| `Ahead` | **-10 ticks** | Slightly ahead of grid (rushes subtly) |
| `OnTop` | **0 ticks** | Very close to grid (tight, no push/pull) |
| `Behind` | **+10 ticks** | Slightly behind grid (lays back subtly) |
| `LaidBack` | **+20 ticks** | More behind / relaxed (noticeably laid back) |

**Notes**:
- These are **base offsets** before adding `RoleTimingBiasTicks`.
- Final formula: `roleOffset = baseTickOffset(feel) + RoleTimingBiasTicks[role]`
- With TicksPerQuarterNote = 480, ±10 ticks ≈ ±2% of a quarter note (subtle but audible).
- These values provide a **starting point**; future drummer models can tune them via `RoleTimingBiasTicksOverride`.

---

### E2-2. Precedence: If both `RoleTimingFeel` and `RoleTimingBiasTicks` are present along with `GroovePolicyDecision` overrides, which fields win individually when overrides are partial?

**Answer:** Use **field-level override precedence** (each field is independent):

- **EffectiveRoleFeel** = `GroovePolicyDecision.RoleTimingFeelOverride ?? GrooveTimingPolicy.RoleTimingFeel[role] ?? TimingFeel.OnTop`
- **EffectiveRoleBias** = `GroovePolicyDecision.RoleTimingBiasTicksOverride ?? GrooveTimingPolicy.RoleTimingBiasTicks[role] ?? 0`

**Behavior for partial overrides**:
- If policy decision provides only `RoleTimingFeelOverride`, use that feel + base policy bias.
- If policy decision provides only `RoleTimingBiasTicksOverride`, use base policy feel + override bias.
- If both provided, use both overrides.
- If neither provided, use base policy values.

**Rationale**: Drummer models need independent control over feel vs. bias for expressive flexibility.

---

### E2-3. Order relative to E1: Should per-role bias be applied before or after E1 feel timing?

**Answer:** E2 is applied **after E1** in the generation pipeline.

**Pipeline order**:
1. Story E1 (FeelTimingEngine): Apply global feel timing (Swing/Shuffle/TripletFeel) to eligible eighth offbeats.
   - Input: onsets with `TimingOffsetTicks = null or 0`
   - Output: onsets with feel-adjusted `TimingOffsetTicks`
2. Story E2 (RoleTimingEngine): Apply per-role microtiming bias on top of E1 offsets.
   - Input: onsets with E1-applied `TimingOffsetTicks`
   - Output: onsets with combined E1+E2 `TimingOffsetTicks`

**Additive semantics**:
- `FinalTimingOffsetTicks = (E1_feelOffset) + (E2_roleOffset)`
- E2 adds its computed role offset to whatever value E1 left in `TimingOffsetTicks`.

**Note**: This is the same additive pattern used in E1 itself (E1 preserves existing offsets and adds to them).

---

### E2-4. Clamping semantics: Is `MaxAbsTimingBiasTicks` a per-role absolute clamp applied to the delta added by E2 only, or to the final combined offset including E1?

**Answer:** Clamp the **final combined offset** (E1 + E2) to `[-MaxAbsTimingBiasTicks .. +MaxAbsTimingBiasTicks]`.

**Formula**:
```
existingOffset = onset.TimingOffsetTicks ?? 0  // From E1 or prior
roleOffset = baseTickOffset(effectiveFeel) + effectiveRoleBias
combinedOffset = existingOffset + roleOffset
finalOffset = Math.Clamp(combinedOffset, -MaxAbsTimingBiasTicks, +MaxAbsTimingBiasTicks)
```

**Rationale**:
- `MaxAbsTimingBiasTicks` is a **safety guardrail** to prevent timing from drifting too far from the grid.
- Clamping the final sum ensures that extreme combinations of E1 swing + E2 role bias stay within musical bounds.
- Typical value: `MaxAbsTimingBiasTicks = 50` (about 10% of a quarter note at 480 TPQN).

---

### E2-5. Rounding: If `TimingFeel` mapping uses fractional beats to compute ticks, which rounding mode is required?

**Answer:** **No fractional computation required** for E2.

- `TimingFeel` maps to **fixed integer base offsets** (see E2-1).
- `RoleTimingBiasTicks` is already an **integer** in the policy.
- All arithmetic is integer addition: `baseOffset + roleBias`.
- No rounding needed.

**If future extensions require fractional calculations** (e.g., percentage-based feel), use `Math.Round(..., MidpointRounding.AwayFromZero)` for consistency with Story D2.

---

### E2-6. Unknown roles: How should roles not listed in policy behave?

**Answer:** Use **deterministic fallback to neutral defaults**:

- If `RoleTimingFeel[role]` not found → default to `TimingFeel.OnTop` (0 ticks)
- If `RoleTimingBiasTicks[role]` not found → default to `0` ticks
- Result: unknown roles have no microtiming adjustment (stay on-grid).

**Fallback order for role lookup**:
1. If `GroovePolicyDecision.RoleTimingFeelOverride` provided → use it (skip policy lookup).
2. Else if `GrooveTimingPolicy.RoleTimingFeel[role]` exists → use it.
3. Else → default to `TimingFeel.OnTop`.

Same precedence for bias lookup.

**Rationale**: Fail-safe behavior (on-grid) avoids introducing unexpected timing shifts for unconfigured roles.

---

### E2-7. Diagnostics: What minimal per-onset diagnostic fields are required for E2?

**Answer:** Minimal per-onset diagnostic fields for E2 (similar to E1 and D2 patterns):

| Field | Type | Purpose |
|-------|------|---------|
| `Role` | `string` | Which role this onset belongs to |
| `Beat` | `decimal` | Beat position in bar |
| `EffectiveTimingFeel` | `TimingFeel` | Resolved feel (policy or override) |
| `EffectiveTimingBiasTicks` | `int` | Resolved bias (policy or override) |
| `BaseFeelOffsetTicks` | `int` | Base offset from feel mapping |
| `RoleOffsetTicks` | `int` | Base + bias (before combining with E1) |
| `ExistingOffsetTicks` | `int` | E1-applied offset (or 0) |
| `PreClampCombinedOffset` | `int` | E1 + E2 before clamping |
| `FinalOffsetTicks` | `int` | After clamping to MaxAbsTimingBiasTicks |
| `WasClamped` | `bool` | True if clamping was applied |

**Usage**: Return diagnostics via a `ComputeRoleTimingWithDiagnostics` method (optional, for Story G1).

---

## 8. Test Scenario Ideas

**Unit test name suggestions**:
- `ResolveRoleTiming_UsesPolicy_WhenNoOverride`
- `ResolveRoleTiming_UsesOverride_WhenProvided`
- `ResolveRoleTiming_PartialOverride_FeelOnly`
- `ResolveRoleTiming_PartialOverride_BiasOnly`
- `RoleTiming_AheadMapsToNegativeTenTicks`
- `RoleTiming_OnTopMapsToZeroTicks`
- `RoleTiming_BehindMapsToPlustenTicks`
- `RoleTiming_LaidBackMapsToPlusTwentyTicks`
- `RoleTiming_AddsToExistingE1Offset`
- `RoleTiming_ClampsToMaxAbsTimingBiasTicks`
- `RoleTiming_UnknownRole_DefaultsToOnTopAndZeroBias`
- `RoleTiming_NullOnsets_ThrowsArgumentNullException`
- `RoleTiming_EmptyOnsets_ReturnsEmpty`
- `RoleTiming_Deterministic_ForSameInputs`
- `RoleTiming_ClampingIndicator_SetWhenClamped`

**Test data setups**:
- Simple 4/4 bar with anchor onsets at 1.0, 1.5, 2.0 to verify on-top vs ahead/behind behaviors.
- Existing E1-applied offsets (e.g., swing = 40 ticks) then add per-role biases to verify additive and clamped results.
- Per-role bias values: -15, 0, +15, +30, +60 (includes values that will trigger clamping).
- Multiple roles (Kick, Snare, ClosedHat) with different feel/bias settings.

**Determinism verification**:
- Run same input + policy + overrides multiple times → assert identical `TimingOffsetTicks`.
- Compare serialized onset arrays (beats + final offsets) to golden snapshots for regression tests.

**Specific test cases**:

| Test | Policy Feel | Policy Bias | E1 Offset | Expected E2 Offset | Final Offset | Clamped? |
|------|-------------|-------------|-----------|-------------------|--------------|----------|
| Ahead with zero bias | Ahead | 0 | 0 | -10 | -10 | No |
| Behind with positive bias | Behind | +5 | 0 | +15 | +15 | No |
| LaidBack with large bias | LaidBack | +40 | 0 | +60 | +50 (max) | Yes |
| Ahead with E1 swing | Ahead | -5 | +40 | -15 | +25 | No |
| Behind with large E1 + bias | Behind | +30 | +40 | +40 | +50 (max) | Yes |

(Assuming `MaxAbsTimingBiasTicks = 50` for these examples)

---

## Summary

Story E2 introduces per-role micro-timing that layers on top of E1's global feel timing. The key implementation points are:

1. **TimingFeel mappings**: Ahead=-10, OnTop=0, Behind=+10, LaidBack=+20 ticks.
2. **Additive semantics**: E2 adds role offset to existing E1 offset.
3. **Final clamping**: Combined E1+E2 offset clamped to `[-MaxAbsTimingBiasTicks .. +MaxAbsTimingBiasTicks]`.
4. **Override precedence**: Field-level (feel and bias independent).
5. **Unknown roles**: Default to OnTop (0) and zero bias.

With these rules, E2 can be implemented deterministically with full test coverage and diagnostic support.

---

*End of clarified pre-analysis for Story E2.*
