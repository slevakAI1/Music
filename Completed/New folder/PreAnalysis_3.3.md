# Pre-Analysis — Story 3.3: Phrase Punctuation Operators

## 1. Story Intent Summary
- What: Implement operators that mark phrase and section boundaries (fills, crashes, setup hits, stop-time) to provide clear musical punctuation.
- Why: Phrase punctuation gives structure and momentum to generated drum parts; it influences transitions, listener perception of sections, and overall arrangement clarity.
- Who benefits: Generator (produces musically-aware transitions), end-users/listeners (more coherent tracks), developers (exposed hooks for fills/phrasing diagnostics).

## 2. Acceptance Criteria Checklist
1. Implement 7 operators:
   1. `CrashOnOneOperator` — crash cymbal on beat 1 at phrase/section start
   2. `TurnaroundFillShortOperator` — 2-beat fill at end of phrase (beats 3-4)
   3. `TurnaroundFillFullOperator` — 1-bar fill at end of section
   4. `SetupHitOperator` — kick/snare on 4& leading into next section
   5. `StopTimeOperator` — brief dropout then return (hats off for 2 beats)
   6. `BuildFillOperator` — ascending tom fill for tension
   7. `DropFillOperator` — descending tom fill for release
2. Each operator checks `IsFillWindow` and `IsAtSectionBoundary` in context.
3. Generates appropriate fill density based on `Energy` level.
4. Avoids overlapping with previous fill (checks memory).
5. Scores higher at actual phrase boundaries.
6. Fill operators use deterministic patterns with seed-based variation.
7. Unit tests: fills generate only in appropriate windows.

Notes: Grouped related criteria: operator list (1), application gating (2), musical behavior (3,5,6), safety (4), testing (7).

Ambiguous/unclear ACs:
- "Appropriate fill density based on energy" lacks numeric mapping or scale.
- "Avoids overlapping with previous fill" lacks precise temporal window (how many bars?) and strictness.

## 3. Dependencies & Integration Points
- Depends on completed stories / components:
  - Stage 1 common contracts: `IMusicalOperator`, `AgentContext`, `OperatorFamily` (Story 1.1)
  - Drummer context and builder (Story 2.1)
  - Drum candidate type and role enums (Story 2.2)
  - Drummer memory (Story 2.5) for repetition checks
  - Operator registry/discovery (Story 3.6) to register these operators
  - Operator selection & style weighting (Story 1.3 and 1.4) for scoring/weights
  - Physicality filter (Stage 4) for final candidate sanitization
  - RNG streams / deterministic facilities (appendix RNG keys)

- Integration touchpoints in code:
  - `DrummerContext` (read: `IsFillWindow`, `IsAtSectionBoundary`, `EnergyLevel`, `BarNumber`, `Beat`)
  - `DrumCandidate` (produced candidates with `FillRole`, `VelocityHint`, `TimingHint`, `Score`)
  - `DrummerMemory` (read last fill shape, last fill bar)
  - `OperatorSelectionEngine` (operators contribute candidates and base scores)
  - Diagnostics collector (optionally record operators considered/selected)

- Provides for future stories:
  - Fill patterns exposed to `DrummerDiagnostics` and `DrummerMemory`
  - Section-signature inputs for PopRock style tuning (Stage 5)
  - Training/benchmark features (Stage 7) via consistent fill metadata

## 4. Inputs & Outputs
- Inputs (consumed):
  - `DrummerContext` / `AgentContext` (bar/beat, `IsFillWindow`, `IsAtSectionBoundary`, `EnergyLevel`, `PhrasePosition`)
  - `StyleConfiguration` (allowed operators, style gating, weight modifiers)
  - `DrummerMemory` (recent fills, `LastFillBar`, `LastFillShape`)
  - RNG/Seed and stream key for deterministic variation
  - Role availability (`ActiveRoles`) and grid resolution/subdivision rules

- Outputs (produced):
  - Collections of `DrumCandidate` representing fills or punctuation hits, with populated `CandidateId`, `OperatorId`, `Role`, `BarNumber`, `Beat`, `Strength`, `VelocityHint`, `TimingHint`, `FillRole`, `Score`.
  - Diagnostic entries describing why operators were considered, scored, or rejected.
  - Memory-affecting signals (when an operator is selected, it will be recorded in memory).

- Configuration/settings read:
  - Style density rules, max events per bar, operator allowlist
  - Physicality/role caps
  - Memory penalties/decay settings
  - Determinism: RNG stream keys

## 5. Constraints & Invariants
- Operators must only generate fills/punctuation when `IsFillWindow` or `IsAtSectionBoundary` gating allows it (as per AC).
- Determinism invariant: same seed + same context → identical operator outputs and variation choices.
- Memory invariant: do not produce a fill that is considered an exact repeat of `LastFillShape` when `AllowConsecutiveSameFill` is false.
- Physicality invariant: any produced candidate must pass limb/sticking/overcrowding constraints before final selection (filter stage).
- Ordering: operators should consult context → consult memory → generate candidates → score → hand off to selection/physicality pipeline.
- Hard limits to respect (from Epic/Stage 5/4 defaults):
  - MaxHitsPerBar, MaxHitsPerBeat, Role-specific caps (e.g., Crash per bar)
  - Fill density caps per bar (must not exceed role caps)

## 6. Edge Cases to Test
- Empty or missing `DrummerContext` fields (null or default values for `IsFillWindow`, `EnergyLevel`).
- `IsFillWindow = false` but `IsAtSectionBoundary = true` (which gating wins?).
- Recent fill present in memory within the forbidden window (adjacent bars/sections): operator should abstain.
- Multiple fill operators generate overlapping candidate events for same beats — ensure selection/pruning resolves conflicts.
- Active roles missing (e.g., toms disabled) but fill operator requires them — ensure graceful degradation.
- Energy extremes:
  - Energy = 0.0 (should produce minimal/no fills)
  - Energy = 1.0 (allow maximum fill density allowed by style/physicality)
- Short bars or unusual time signatures (fills that assume 4/4 positions 3-4 may be invalid).
- Determinism: same seed but different style config should change outputs; same seed + same inputs must be identical.
- Overcrowding: when adding fills causes MaxHitsPerBar to be exceeded, ensure pruning and deterministic tie-break.
- Concurrency: if memory is accessed/updated concurrently in multi-threaded generation, check invariants (if concurrent generation occurs).

## 7. Clarifying Questions

1. Exact temporal window for "avoid overlapping with previous fill": how many bars back constitutes "previous" (1 bar, N bars, section boundary)?
   **Answer:** Use DrummerMemory.LastFillBar with a configurable lookback window of 8 bars (DrummerPolicySettings.FillLookbackBars). Also check WouldRepeatPreviousSectionFill for cross-section anti-repetition. Operators should abstain if barNumber - LastFillBar < MinBarsBetweenFills (default 4).

2. Numeric mapping between `EnergyLevel` and "fill density" (e.g., Energy 0.0→0.2 density scale, Energy 1.0→1.0)?
   **Answer:** Energy 0.0-0.3 = sparse fills (4-6 hits), Energy 0.3-0.6 = moderate fills (6-10 hits), Energy 0.6-1.0 = dense fills (10-16 hits). For 2-beat fills, scale proportionally. Implemented via hit count computation: baseHits = 4 + (int)(energyLevel * 12).

3. Definition of "appropriate fill density": is this a target number of hits, a relative fullness metric, or a percentage of allowed max-hits?
   **Answer:** Target number of hits per fill duration. TurnaroundFillShort (2 beats): 4-8 hits. TurnaroundFillFull (1 bar): 8-16 hits. BuildFill/DropFill: 6-12 hits across 2 beats. Constrained by style's MaxHitsPerBar caps.

4. Are turnaround fills restricted to specific grid positions (e.g., beats 3-4 in 4/4) only, or should they adapt to time signature/bar length?
   **Answer:** Adapt to time signature. TurnaroundFillShort uses last 2 beats of the bar (BeatsPerBar-1 to BeatsPerBar). TurnaroundFillFull spans full bar. For 3/4, short fill is on beats 2-3; for 6/4, short fill is on beats 5-6.

5. What constitutes "overlapping" with previous fill: any shared beat, same role on adjacent subdivisions, or similar fill-shape fingerprint?
   **Answer:** Use DrummerMemory's FillShape comparison (AreFillShapesSimilar): same roles (sorted), density within tolerance (0.1), same duration, same fill tag. This is a fingerprint match, not beat-level overlap.

6. Are certain fill operators considered higher priority (cannot be pruned) or are they all equal and subject to selection/physicality pruning?
   **Answer:** All fill operators are subject to normal selection and physicality pruning. CrashOnOne has highest base score (0.85) since it's almost always musically appropriate at section starts. Fill operators score 0.6-0.75 range.

7. For `StopTimeOperator`: how is the dropout represented (explicit silent events, role muting, or removal of candidates), and how should downstream systems interpret it?
   **Answer:** StopTimeOperator generates NO candidates for the dropout beats (it's additive, not replacement). The absence of hat candidates from this operator during fill window bars, combined with the IsFillWindow context, signals dropdown. Downstream interprets fewer total candidates as natural thinning.

8. Does "deterministic patterns with seed-based variation" require a specific RNG stream per operator (naming convention), or is a shared operator selection RNG acceptable?
   **Answer:** Use existing DrumGenerator RNG stream with HashCode.Combine(barNumber, beat, seed) for velocity/timing variation. No separate RNG stream per operator needed. Pattern selection within operators uses deterministic hash combining operatorId + context fields.

9. How should operators expose intent to diagnostics (e.g., canonical fill id / variation id) for snapshot/golden tests?
   **Answer:** Via CandidateId format: "{OperatorId}_{Role}_{BarNumber}_{Beat}" with optional FillTag suffix. FillShape.FillTag captures the pattern variant ("TurnaroundShort_V1", "BuildFill_Ascending"). Diagnostics collector records these via GrooveDiagnosticsCollector.

10. Should operators produce full-bar replacement patterns (like pattern-substitution) or additive candidates that mix with existing timekeeping?
    **Answer:** Additive candidates. Fills add to the candidate pool and compete with timekeeping candidates during selection. High fill scores at phrase boundaries naturally win selection. StopTimeOperator is exception—it generates nothing, causing natural thinning.

## 8. Test Scenario Ideas (unit test name suggestions)
- `When_NotInFillWindow_OperatorsProduceNoFillCandidates`
  - Setup: `IsFillWindow=false`, `IsAtSectionBoundary=false`, various energy levels
  - Expect: no fill candidates produced by phrase-punctuation operators

- `When_AtSectionStart_CrashOnOne_IsGenerated`
  - Setup: `IsAtSectionBoundary=true`, `PhrasePosition=Start`, `ActiveRoles` include crash
  - Expect: `CrashOnOneOperator` produces crash candidate on beat 1

- `TurnaroundFillShort_OnlyGeneratesAtPhraseEnd`
  - Setup: `PhrasePosition=End`, `IsFillWindow=true`, last two beats available
  - Expect: 2-beat fill candidates aligned to beats 3-4

- `Fill_Avoids_Repeating_LastFillShape`
  - Setup: `DrummerMemory.LastFillShape` matches candidate fingerprint; `AllowConsecutiveSameFill=false`
  - Expect: operators abstain or score suppressed; selection does not pick identical fill

- `Fill_Density_Scales_With_Energy`
  - Setup: identical context with Energy=0.2 and Energy=0.9
  - Expect: higher energy produces higher total fill hits / denser candidate set

- `Determinism_SameSeed_SameContext_YieldsSameFillPattern`
  - Setup: fixed seed and context
  - Expect: generated candidate set identical across runs

- `ActiveRoles_Missing_TomRequired_FillDegradesGracefully`
  - Setup: TurnaroundFillFull requires toms but `ActiveRoles` exclude toms
  - Expect: fallback to alternate roles (e.g., snare) or no fill, not crash

- `StopTime_Generates_Dropout_And_Return`
  - Setup: `StopTimeOperator` in fill window
  - Expect: candidates reflect two-beat hat dropout then return; downstream pipeline can realize dropout

- `Overcrowding_Prunes_LowestScored_Fills_Deterministically`
  - Setup: candidate set exceeds `MaxHitsPerBar`
  - Expect: pruning removes lowest scored candidates with deterministic tie-breaks


---

// Notes for implementer reviewers:
// AI: This analysis focuses on ambiguity, inputs/outputs, tests and integration only. No implementation guidance.
