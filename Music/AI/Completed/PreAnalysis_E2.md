# Pre-Analysis: Story E2 — Implement Role Timing Feel + Bias + Clamp

## 1. Story Intent Summary

- What: Apply per-role micro-timing (feel/bias) on top of global feel timing so each role can sit ahead, on-top, behind, or laid-back relative to the bar grid.
- Why: Allows musical roles (kick, snare, hats, comp, bass) to have distinct pocket and microtiming, enabling more expressive and stylistically correct grooves without changing beats.
- Who benefits: Generator (more expressive outputs), end-user (more human-feeling grooves), developers (deterministic, testable policy-driven microtiming).

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

Ambiguous criteria: how to map `TimingFeel` qualitative values to exact tick magnitudes; precedence semantics when policy override is partially provided (feel only, bias only); and how additive interactions with E1 feel offsets must be ordered.

## 3. Dependencies & Integration Points

- Depends on (must be present):
  - Story E1 (FeelTimingEngine) — E2 applies on top of feel timing offsets.
  - Story D1/D2 (Onset strength/velocity) for ordering of timing vs strength classification if relevant.
  - GroovePolicyDecision and IGroovePolicyProvider (A3) for overrides.
- Interacts with existing code/types:
  - `GrooveTimingPolicy`, `TimingFeel` enum
  - `GroovePolicyDecision` (override fields)
  - `GrooveOnset.TimingOffsetTicks` (must be immutable copy-with applied)
  - `BarTrack` / `ToTick` when computing absolute tick clamps (for bounds)
  - `MusicConstants.TicksPerQuarterNote` if mapping feel→ticks depends on beat subdivisions
- Provides for future stories:
  - Story E2 output feeds Story H1/H2 tests and Story E2-per-role tuning (operator/drummer policies)
  - Enables per-role diagnostic traces (G1)

## 4. Inputs & Outputs

Inputs:
- `IReadOnlyList<GrooveOnset>` (existing onsets, possibly with TimingOffsetTicks from E1).
- `GrooveTimingPolicy` (RoleTimingFeel, RoleTimingBiasTicks, MaxAbsTimingBiasTicks).
- `GroovePolicyDecision` (optional per-bar/per-role overrides).
- `BarContext`/`BarTrack` for beat-to-tick conversions if needed.

Outputs:
- `IReadOnlyList<GrooveOnset>` new records with updated `TimingOffsetTicks` (additive to existing offsets).

Configuration read:
- `GrooveTimingPolicy.MaxAbsTimingBiasTicks` (clamp magnitude)
- Per-role mapping in `GrooveTimingPolicy.RoleTimingBiasTicks` and `RoleTimingFeel`
- Optional overrides in `GroovePolicyDecision`

## 5. Constraints & Invariants

Must ALWAYS be true:
- Determinism: same inputs + overrides produce same outputs.
- Immutability: do not mutate input onsets; return new records.
- Do not change `GrooveOnset.Beat` (only adjust `TimingOffsetTicks`).
- Final applied per-onset offset must be clamped to absolute magnitude ≤ `MaxAbsTimingBiasTicks` (signed clamp centered at zero).
- Respect override precedence: explicit `GroovePolicyDecision` overrides should win over base policy values.

Hard limits:
- `MaxAbsTimingBiasTicks` is authoritative; timing bias must not exceed it.
- Timing offsets are integer ticks; define rounding if fractional mapping used.
- Order of application: global feel (E1) then per-role bias (E2) unless policy specifies otherwise.

Operation order (recommended canonical order to avoid ambiguity):
1. Resolve effective role feel and bias (policy → per-bar override).
2. Compute per-role bias ticks (map `TimingFeel` to a tick magnitude then add `RoleTimingBiasTicks`).
3. Add per-role bias to existing `TimingOffsetTicks` (which may include E1 feel).
4. Clamp to `MaxAbsTimingBiasTicks`.
5. Produce new `GrooveOnset` records.

## 6. Edge Cases to Test

- Role missing from `RoleTimingFeel` and/or `RoleTimingBiasTicks` (fallback to OnTop and 0 bias).
- `RoleTimingBiasTicks` values exceeding `MaxAbsTimingBiasTicks` (clamped).
- `GroovePolicyDecision` provides only feel override or only bias override (partial overrides).
- Existing `TimingOffsetTicks` null vs non-null (treat null as 0, additive semantics).
- Combined large E1 feel offset + E2 role bias that would exceed next beat boundary — clamp to `MaxAbsTimingBiasTicks` and ensure not crossing next downbeat if that invariant matters.
- Negative (ahead) vs positive (behind) semantics and clamping boundaries.
- Non-existent role (unknown role string) — default behavior.
- Determinism: repeated runs with same inputs and overrides produce identical outputs.
- Empty onset list, null inputs (guard and throw where appropriate).

Combination scenarios:
- High per-role bias combined with high E1 swing causing near-next-beat positions — verify bounded semantics.
- Overrides that reduce bias to zero but base policy non-zero.

## 7. Clarifying Questions

1. Precise mapping: How should qualitative `TimingFeel` map to tick magnitudes? (e.g., Ahead = -X ticks, OnTop = 0, Behind = +Y ticks, LaidBack = +Z ticks). Provide numeric defaults or formula.
2. Precedence: If both `RoleTimingFeel` and `RoleTimingBiasTicks` are present along with `GroovePolicyDecision` overrides, which fields win individually (feel vs bias) when overrides are partial?
3. Order relative to E1: Should per-role bias be applied before or after E1 feel timing when a segment override changes `OverrideCanChangeFeel`? (E1 currently applies per-bar; confirm additive stacking direction.)
4. Clamping semantics: Is `MaxAbsTimingBiasTicks` a per-role absolute clamp applied to the delta added by E2 only, or to the final combined offset including E1?
5. Rounding: If `TimingFeel` mapping uses fractional beats to compute ticks, which rounding mode is required (AwayFromZero, ToEven, Floor)?
6. Unknown roles: How should roles not listed in policy behave? (Default to `OnTop` and 0 bias?)
7. Diagnostics: What minimal per-onset diagnostic fields are required for E2 (e.g., role feel source, pre-clamp, post-clamp)?

## 8. Test Scenario Ideas

Unit test name suggestions:
- `ResolveRoleTiming_UsesPolicy_WhenNoOverride`
- `ResolveRoleTiming_UsesOverride_WhenProvided`
- `RoleTiming_AheadMapsToNegativeTicks`
- `RoleTiming_LaidBackMapsToLargerPositiveTicks`
- `RoleTiming_AddsToExistingOffset`
- `RoleTiming_ClampsToMaxAbsTimingBiasTicks`
- `RoleTiming_PartialOverride_Precedence_BehavesPredictably`
- `RoleTiming_UnknownRole_DefaultsToOnTop`
- `RoleTiming_NullOnsets_ReturnsEmpty`
- `RoleTiming_Deterministic_ForSameInputs`

Test data setups:
- Simple 4/4 bar with anchor onsets at 1.0, 1.5, 2.0 to verify on-top vs ahead/behind behaviors.
- Existing E1-applied offsets (e.g., swing applied) then add per-role biases to verify additive and clamped results.
- Per-role bias values negative and positive, and larger than `MaxAbsTimingBiasTicks` to test clamping.

Determinism verification:
- Run same input + policy + overrides multiple times → assert identical `TimingOffsetTicks`.
- Compare serialized onset arrays (beats + final offsets) to golden snapshots for regression tests.

---

*End of pre-analysis for Story E2.*
