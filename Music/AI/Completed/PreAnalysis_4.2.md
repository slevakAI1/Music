# Pre-Analysis: Story 4.2 — Implement Sticking Rules

## 1) Story Intent Summary
- What: Define and validate sticking rules that ensure generated drum patterns respect human hand/foot sticking constraints (limits on consecutive same-hand hits, ghost note density, and minimum gap between rapid hits).
- Why: Prevents unrealistic/unplayable drumming patterns, improving musical realism and providing a deterministic, testable gate before physicality filtering and final selection.
- Who benefits: Developers (clear contract to implement and test), the generator (uses rules to filter/prune candidates), and end-users (more realistic drum output).

## 2) Acceptance Criteria Checklist
1. Create `StickingRules` class exposing:
   1. `MaxConsecutiveSameHand` (default: 4 for 16ths)
   2. `MaxGhostsPerBar` (default: 4)
   3. `MinGapBetweenFastHits` (minimum ticks between same-limb hits)
   4. `ValidatePattern(List<DrumCandidate>) -> StickingValidation`
2. Create `StickingValidation` type containing:
   1. `IsValid` (bool)
   2. `Violations` (list of specific rule breaks)
3. Unit tests: verify sticking violations are detected correctly.

Ambiguities / unclear criteria:
- Units and default value for `MinGapBetweenFastHits` (ticks? relation to `MusicConstants.TicksPerQuarterNote`?)
- Exact meaning of "MaxConsecutiveSameHand" in presence of overlapping subdivisions (e.g., 16ths vs 8ths).
- Definition of "ghost" candidates for `MaxGhostsPerBar` (based on `OnsetStrength.Ghost` only or include low-velocity picks?).

## 3) Dependencies & Integration Points
- Depends on these stories / components:
  - Story 4.1 Limb model (role->limb mapping)
  - Story 2.2 DrumCandidate definition
  - Story 4.3 PhysicalityFilter (will consume these results)
  - Operator generation pipeline (operators produce candidates to validate)
- Interacts with existing types/files:
  - `DrumCandidate` (role, bar, beat, strength, timing/velocity hints)
  - `LimbModel` / `LimbAssignment` / `LimbConflictDetector` (for limb resolution)
  - `MusicConstants.TicksPerQuarterNote` (for tick conversions)
  - `DrummerContext` (for grid/subdivision info when validating consecutive hits)
- Provides for future stories:
  - A deterministic sticking validation API used by `PhysicalityFilter` and operator scoring
  - Structured violations for diagnostics and pruning

## 4) Inputs & Outputs
- Inputs consumed:
  - `IReadOnlyList<DrumCandidate>` (candidate stream for a bar or candidate group)
  - `LimbModel` (role→limb mapping; may be in `PhysicalityRules`)
  - Timing resolution constants (`TicksPerQuarterNote`) and bar/beat → tick conversions (BarTrack/OnsetGrid) when `MinGapBetweenFastHits` requires ticks
  - Optional context flags (e.g., grid resolution, subdivision) from `DrummerContext`
- Outputs produced:
  - `StickingValidation` record with `IsValid` and `Violations` details (which candidates violate which rule)
  - Possibly a normalized report consumable by `PhysicalityFilter` and diagnostics collector
- Configuration read:
  - `StickingRules` properties (defaults may be overridden by `PhysicalityRules` or style settings)

## 5) Constraints & Invariants
- Hard invariants:
  - Never mark hits by different limbs as a sticking violation (only same-limb sequences counted)
  - `MaxConsecutiveSameHand` applies to consecutive events assigned to the same limb at the same granularity (e.g., exact beat position or within `MinGapBetweenFastHits`)
  - `MaxGhostsPerBar` counts only candidates classified as `OnsetStrength.Ghost` unless config overrides
- Hard limits / policy:
  - `MinGapBetweenFastHits` must be interpreted in ticks; it cannot be negative and must be <= `TicksPerQuarterNote` * reasonable factor
  - Validation must be deterministic: same input → same validation result
- Order of operations:
  1. Map `DrumCandidate` → `LimbAssignment` using `LimbModel`
  2. Sort assignments by absolute time (bar, beat, tick)
  3. Evaluate consecutive-same-limb windows and ghost counts
  4. Produce `StickingValidation` with violations

## 6) Edge Cases to Test
- Empty input list -> validation returns IsValid = true, no violations.
- Single event -> valid.
- Simultaneous events at identical tick on different limbs -> valid (no sticking violation).
- Simultaneous events at identical tick on same limb -> treated as immediate violation (if exceeds consecutive limit or below gap).
- Events slightly offset within `MinGapBetweenFastHits` -> violation for same limb.
- Long sequence of valid alternating limbs (R,L,R,L) -> does not count toward `MaxConsecutiveSameHand`.
- Ghost counting: bars with mixture of low-velocity non-ghosts and `OnsetStrength.Ghost`
- Crossing bar boundary: consecutive hits that straddle bar boundary should be considered (configurable: window may cross bar boundary)
- Invalid/missing limb mapping for a role -> treat as unknown and skip from sticking checks or record as informational violation depending on policy.
- Very dense bars exceeding `MaxGhostsPerBar` and other caps simultaneously -> ensure all violations are reported.

## 7) Clarifying Questions
1. Units: Should `MinGapBetweenFastHits` be expressed in absolute ticks? Confirm relation to `MusicConstants.TicksPerQuarterNote`.
2. Ghost definition: Should `MaxGhostsPerBar` count only `OnsetStrength.Ghost` or also low-velocity candidates? If velocity threshold used, what is it?
3. Consecutive definition: Are "consecutive" hits defined strictly by temporal order for a limb, or must they be contiguous in the candidate list (i.e., ignore interleaving other-limb hits)?
4. Bar boundary behavior: Do `MaxConsecutiveSameHand` windows reset at bar boundaries, or can consecutive counts span bars?
5. Unknown limb mapping: How should roles without mapping be handled—skip, treat as separate limb, or fail validation?
6. Interaction with `PhysicalityFilter`: Should sticking violations be a hard rejection or a scored reason for pruning/weighting?
7. Diagnostics: What level of detail is required in `Violations` (candidate ids, timestamps, limb, rule id)?
8. Thread-safety & performance expectations: Will validation be called frequently in hot paths and should it be optimized?

Answer 1
MinGapBetweenFastHits is expressed in absolute ticks (int) and uses `MusicConstants.TicksPerQuarterNote` for context. Defaults chosen relative to a 16th-note: default = TicksPerQuarterNote/4 (120 ticks when TPQN=480).

Answer 2
`MaxGhostsPerBar` counts only candidates with `OnsetStrength.Ghost`. Velocity is not used for ghost classification here; operators must set `Strength=Ghost` for decorative hits.

Answer 3
Consecutive hits are defined per-limb in temporal order and ignore interleaving hits by other limbs. We examine the ordered sequence of assignments for each limb and count consecutive entries separated by less than `MinGapBetweenFastHits` as consecutive rapid hits.

Answer 4
Consecutive same-hand windows may span bar boundaries (do not reset at bar boundary). This avoids edge artifacts when fills or pickups cross bars.

Answer 5
Roles without a limb mapping are skipped from sticking checks (treated as unknown). They do not contribute to consecutive counts or ghost counts but are preserved for diagnostics.

Answer 6
Sticking violations are reported via `StickingValidation.Violations`. The `PhysicalityFilter` will treat violations as rejections under `Strict` mode and as pruning candidates under `Normal/Loose` modes. `StickingRules` itself is pure validation and does not perform pruning.

Answer 7
Violations include: rule id (string), descriptive message, involved `CandidateId`s, `BarNumber`/`Beat` positions, and the `Limb` involved when applicable. This provides actionable diagnostics for upstream filters.

Answer 8
Validation is expected to run in candidate-generation hot paths; implement it with O(n log n) or O(n) approaches where possible. Use simple sorts and grouping; keep allocations modest. The implementation should be thread-safe for concurrent reads (stateless per-call).

## 8) Test Scenario Ideas (suggested test names)
- `StickingRules_ValidatePattern_EmptyCandidates_ReturnsValid`
- `StickingRules_ValidatePattern_SingleCandidate_ReturnsValid`
- `StickingRules_ValidatePattern_AlternatingHands_UnderLimit_ReturnsValid`
- `StickingRules_ValidatePattern_SameLimb_ExceedsMaxConsecutive_ReturnsViolation`
- `StickingRules_ValidatePattern_SameLimb_BelowMinGap_ReturnsViolation`
- `StickingRules_ValidatePattern_GhostCount_ExceedsMax_ReturnsViolation`
- `StickingRules_ValidatePattern_MissingLimbMapping_SkipsOrReportsInformational`
- `StickingRules_ValidatePattern_BarBoundaryConsecutive_RespectsPolicy`
- `StickingRules_ValidatePattern_MultipleViolations_AllReported`

Test data setup notes:
- Use deterministic `DrumCandidate` factory helpers with explicit `Role`, `BarNumber`, `Beat`, `TimingHint`/tick offsets and `OnsetStrength`.
- Provide a `LimbModel.Default` mapping for right-handed scenarios and a `LimbModel.LeftHanded` variant.
- Use `MusicConstants.TicksPerQuarterNote` and `BarTrack.ToTick(bar, beat)` helper for absolute tick calculation when asserting gaps.

---
// AI: purpose=sticking rules pre-analysis; story=4.2; outputs=StickingValidation contract + tests; not implementation
