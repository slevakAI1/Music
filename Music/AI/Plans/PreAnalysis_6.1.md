# Pre-analysis: Story 6.1 — Implement Drummer Velocity Shaper

## 1) Story Intent Summary
- What: Provide analysis of the story that adds a drummer-specific velocity-hinting step that normalizes operator velocity hints and maps normalized dynamic intents to per-style numeric targets while leaving final MIDI velocity generation to the groove `VelocityShaper`.
- Why: Ensures drummer operators produce consistent, style-aware, and bounded dynamic hints so later pipeline stages (groove velocity shaper and MIDI export) can produce realistic final velocities; preserves separation of responsibilities and avoids duplicated velocity logic.
- Who benefits: Developers (clear integration contract), the generator pipeline (consistent velocity hints), and end-users/listeners (more musically realistic dynamics after full pipeline).

## 2) Acceptance Criteria Checklist
1. `DrummerVelocityShaper` exists and operates at the drummer layer (hint-only).
2. Input: `VelocityHint` (nullable) + context (role, onset strength, fill role, energy).
3. Output: updated per-candidate `VelocityHint` only (still nullable).
4. `DrummerVelocityShaper` must NOT write final MIDI velocities; final velocities remain owned by groove `VelocityShaper`.
5. Normalized dynamic intents exist and map to numeric velocity targets via `StyleConfiguration` (per-style). 
6. When `VelocityHint` is present, it is treated as baseline and adjusted minimally.
7. When `VelocityHint` is null, provide a conservative hint (not extreme).
8. Dynamic intent examples: Ghost=Low; Backbeat=StrongAccent; Crash=PeakAccent; Fill=Ramp across fill hits.
9. Ramp behavior for fills (bar-local ramp; direction style-configurable).
10. Numeric ranges/targets must come from `StyleConfiguration`; defaults used when missing.
11. Determinism: same inputs -> same hints.
12. `DrummerVelocityShaper` runs BEFORE groove `VelocityShaper` and integrates with its pipeline.
13. Unit tests: respect style targets, and determinism.

Notes: Criteria 5, 8 and 9 reference normalized intent names and mapping conventions; those mappings and keys are not fully specified and may need clarification.

## 3) Dependencies & Integration Points
- Depends on completed groove velocity pipeline (existing `VelocityShaper`) and groove candidate model (types that carry `VelocityHint`).
- Depends on `StyleConfiguration` / `StyleConfigurationLibrary` for per-style numeric velocity targets and ranges.
- Integrates with drummer candidate objects (the candidates/operators that expose `VelocityHint`), `DrummerContext` (for role/energy/fill info), and the groove selection pipeline where velocity shaping runs.
- Interaction points: operator-generated candidates → `DrummerVelocityShaper` (hints) → groove `VelocityShaper` (final velocities) → MIDI export.
- Provides: normalized drummer hinting contract for downstream groove shaping and tests that lock expected per-style hint behavior.

## 4) Inputs & Outputs
- Inputs consumed:
  - Candidate-level `VelocityHint` (nullable).
  - Candidate metadata: `Role`, `OnsetStrength`, `FillRole` (if any), `BarNumber`/beat, and context `Energy`/section info.
  - `StyleConfiguration` (per-style numeric ranges/targets and possibly ramp direction settings).
- Outputs produced:
  - Updated candidate-level `VelocityHint` (numeric target or adjusted hint; still not final MIDI velocity).
- Configuration/settings read:
  - `StyleConfiguration.OperatorVelocityTargets` (or equivalent keys) and defaults.
  - Fill ramp direction / ramp amount settings (if present in style config).

## 5) Constraints & Invariants
- MUST NOT write final MIDI velocities; only hint fields may be modified.
- MUST be deterministic: identical inputs must yield identical hints.
- MUST treat existing non-null `VelocityHint` as baseline and adjust only minimally.
- MUST clamp hints within safe numeric bounds (1..127) if/when produced (implicit hard MIDI limits).
- MUST use `StyleConfiguration` values when present; fall back to conservative defaults when missing.
- MUST not affect non-drum roles or other parts of the pipeline.
- MUST execute before the groove `VelocityShaper` in the pipeline.

## 6) Edge Cases to Test
- Candidate `VelocityHint` is null and style config is partially or fully missing.
- Candidate `VelocityHint` is present and already outside typical ranges (e.g., extreme values) — should be adjusted minimally.
- Energy values at boundaries: 0.0, 1.0, and mid-range.
- Fill with single hit vs dense fill: ramp logic for 1, 2, many hits.
- Multiple candidates for same onset with different hints (ensure deterministic handling per candidate).
- Unknown role or unknown `OnsetStrength` (how to map intensity) — ensure safe defaults.
- Concurrent invocation if the pipeline is multi-threaded (thread-safety for shared config reads).
- StyleConfiguration contains invalid values (negative ranges, min>max) — fallback behavior.
- Very short bars / odd meters where fill ramp spans fewer slots than expected.

## 7) Clarifying Questions
1. What exact `StyleConfiguration` keys/names should be used for drummer dynamic targets (e.g., GhostRange, BackbeatTarget, FillRampBehavior)?
2. How should "adjust minimally" be quantified? (absolute tick/velocity delta, percentage, or capped offset?)
3. For fills: what is the canonical definition of "bar-local ramp" when fills cross bar boundaries or are shorter than a bar?
4. If `VelocityHint` is already outside 1..127, should the shaper clamp it or treat as authoritative?
5. Are there existing unit-test fixtures (example style configs and candidate records) to reuse for determinism tests?
6. Is thread-safety required for shared reads of `StyleConfiguration` or will the pipeline invoke this single-threaded? 
7. Is it acceptable to add new config fields to `StyleConfiguration` for ramp direction/amount, or must only existing keys be used?
8. Should we record diagnostics or telemetry when conservative defaults are used because style values are missing?
9. Where should the `DrummerVelocityShaper` be registered in the pipeline (exact insertion point) and who is responsible for invoking it in the current codebase?

## 8) Test Scenario Ideas
- `DrummerVelocityShaper_NullHint_UsesStyleDefaultLowForGhosts()`
  - Setup: ghost candidate, null hint, style provides GhostTarget=40
  - Assert: candidate.VelocityHint == 40

- `DrummerVelocityShaper_ExistingHint_AdjustedMinimally()`
  - Setup: candidate with VelocityHint=100, style target=110, minimal adjust cap=+5
  - Assert: resulting hint in [95..105] depending on policy (clarify adjustment rule)

- `DrummerVelocityShaper_Fill_RampsAcrossHits_PerBar()`
  - Setup: fill candidates for bar with increasing strengths, style specifies ramp up
  - Assert: per-hit hints form monotonic ramp and stay within style limits

- `DrummerVelocityShaper_MissingStyle_FallsBackToConservativeDefaults()`
  - Setup: null or incomplete style config
  - Assert: produced hints are conservative (e.g., ghosts <= 50, backbeats >= 90)

- `DrummerVelocityShaper_Determinism_SameInputsProduceSameHints()`
  - Setup: repeated invocations with identical inputs
  - Assert: hints are identical each run

- `DrummerVelocityShaper_DoesNotWriteFinalGrooveVelocity()` (integration)
  - Setup: run full pipeline with `DrummerVelocityShaper` then `VelocityShaper`
  - Assert: before groove `VelocityShaper`, no `GrooveOnset.Velocity` values are persisted as final; after groove pass final numeric velocities exist and reflect hints.

---

// End of pre-analysis for Story 6.1
