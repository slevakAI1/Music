# Pre-Analysis — Story 3.1: Micro-Addition Operators (Ghost Notes & Embellishments)

## 1. Story Intent Summary
- What: Add a family of micro-addition drum operators that generate subtle single-hit embellishments (ghosts, pickups, sparse hi-hat 16ths, small clusters, tom anticipations).
- Why: Improve musical realism by introducing human-like micro-variations that increase interest without changing core timekeeping; supports later selection, physicality, and style-weighting stages.
- Who: Drummer agent (generator) benefits directly; developers gain modular operator units; end users (producers/listeners) get more natural-sounding drum parts.

## 2. Acceptance Criteria Checklist
1. `IDrumOperator` specialization exists for micro-addition operators.
2. Implement 7 operators:
   2.1 `GhostBeforeBackbeatOperator` (ghost snare at ~1.75→2, 3.75→4)
   2.2 `GhostAfterBackbeatOperator` (ghost snare at ~2.25, 4.25)
   2.3 `KickPickupOperator` (kick at ~4.75 leading into next bar)
   2.4 `KickDoubleOperator` (extra kick on 1.5, 3.5 or 16th-grid variants 1.25/1.75)
   2.5 `HatEmbellishmentOperator` (sparse 16th hi-hat notes)
   2.6 `GhostClusterOperator` (2–3 ghost notes as mini-fill)
   2.7 `FloorTomPickupOperator` (floor tom anticipation on ~4.75)
3. Each operator must:
   3.1 Implement `CanApply` based on context (energy, section type, grid availability)
   3.2 Generate `DrumCandidate`(s) with appropriate `Strength` (Ghost, Pickup)
   3.3 Provide `VelocityHint` ranges (ghosts: 30–50, pickups: 60–80)
   3.4 Assign operator `Score` reflecting musical relevance
4. Unit tests: each operator produces expected candidates under representative contexts.

Notes: Criteria 3.1 and 3.4 are somewhat high-level and could use numeric thresholds or clearer pass/fail rules for tests.

## 3. Dependencies & Integration Points
- Depends on stories: 1.1 (common operator contracts), 2.1 (drummer context), 2.2 (drum candidate type). Also relies on 1.2 (memory) and 1.3 (selection engine) for scoring/penalties at runtime.
- Interacts with types/components: `IDrumOperator` (interface), `DrummerContext`/`AgentContext`, `DrumCandidate`, `IAgentMemory` (for repetition checks), `StyleConfiguration` (to gate by style and weights), RNG streams (for deterministic variation), `PhysicalityFilter` (downstream validation), operator registry (for discovery).
- Provides: operator implementations that will be registered by the operator registry and used by `DrummerCandidateSource` and selection engine; semantic operator IDs, candidate templates, and operator-level diagnostics.

## 4. Inputs & Outputs
- Inputs consumed:
  - `DrummerContext` (bar, beat, energy, section, grid, active roles, isFillWindow, etc.)
  - Style settings (allowed operators, operator weights, grid rules)
  - Agent memory snapshot (recent operator usage)
  - Deterministic RNG seed / stream key for variation
- Outputs produced:
  - One or more `DrumCandidate` instances per operator invocation, including `CandidateId`, `OperatorId`, `Role`, `BarNumber`, `Beat`, `Strength`, `VelocityHint`, `TimingHint`, `Score`.
  - Operator-level metadata useful for diagnostics (why applied/why not)
- Configuration read:
  - Operator enablement (style-level allow list)
  - Energy thresholds and section gating rules from style config or policy provider
  - Grid/subdivision rules (whether 16th subdivision is available)

## 5. Constraints & Invariants
- Determinism: same seed + same context → same candidates and attributes.
- VelocityHint bounds: ghosts must fall within 30–50; pickups within 60–80.
- Operators must not produce candidates for roles disabled in `DrummerContext.ActiveRoles`.
- Operators must respect grid rules (do not emit notes on subdivisions disallowed by `GridRules`).
- Operators should not violate hard caps enforced later (e.g., `MaxHitsPerBar`, `MaxHitsPerRolePerBar`) — they may produce candidates but downstream filters will prune.
- `CanApply` should be a conservative prefilter: if false, operator must produce no candidates.
- Candidate identifiers must be stable for identical operator parameters (stable operatorId + parameter hashing).

## 6. Edge Cases to Test
- Contexts with very low energy (operators should suppress or reduce outputs).
- Fill windows and section boundaries: ensure micro-additions do not collide with planned fills or are suppressed when `IsFillWindow` disallows embellishments.
- Empty `ActiveRoles` (e.g., hi-hat disabled) — operators targeting that role must yield no candidates.
- Grid mismatch: requested 16th placements when only 8th grid allowed.
- Overlap with protected events (e.g., operator-generated pickup landing on a protected onset) — verify candidate creation vs downstream rejection.
- Memory-based suppression: operator suppressed when recently used (test repetition penalty influence on `CanApply` or `Score`).
- Multiple operators proposing the same onset (tie situations) — ensure determinism in candidate attributes even if downstream resolves selection.
- Off-by-one timing positions (e.g., 4.75 placement at bar boundary) — ensure bar/beat assignment and candidate IDs reflect intended bar.
- Null or malformed input `DrummerContext` — validate defensive behavior.

## 7. Clarifying Questions and Answers

1. What is the canonical grid resolution and allowed subdivisions (e.g., is 16th always available or style-dependent)?
   **Answer:** Grid resolution is style-dependent via `StyleConfiguration.GridRules.AllowedSubdivisions`. PopRock uses `SixteenthGrid` (Quarter|Eighth|Sixteenth). Operators check `AllowedSubdivision.Sixteenth` flag before emitting 16th-note positions.

2. For `CandidateId` stability, is there an existing hashing/fingerprint convention to follow?
   **Answer:** Yes. Use `DrumCandidate.GenerateCandidateId(operatorId, role, barNumber, beat, articulation?)` which produces format `"{OperatorId}_{Role}_{BarNumber}_{Beat}"` with optional articulation suffix.

3. Should `CanApply` factor memory-based repetition suppression, or should memory only affect scoring (penalty) via the selection engine?
   **Answer:** `CanApply` is a fast pre-filter for context checks only (energy, section, grid, active roles). Memory-based repetition affects scoring via the selection engine, not `CanApply`.

4. Are the velocity hint ranges (ghosts 30–50, pickups 60–80) absolute or style-adjustable multipliers?
   **Answer:** Absolute ranges as specified in the AC. VelocityShaper (Story 6.1) may further adjust, but operators provide hints within these fixed ranges.

5. How strictly should operators avoid generating candidates that will later be pruned by physicality/overcrowding filters vs. generating and relying on downstream filters?
   **Answer:** Operators generate valid candidates liberally; downstream filters (`PhysicalityFilter`) handle pruning. Operators only check `ActiveRoles` and grid rules as hard constraints.

6. For `KickDoubleOperator`, clarify whether 16th-grid variants (1.25/1.75) are required or optional and under what conditions to use them.
   **Answer:** Use 8th positions (1.5, 3.5) when only Eighth grid allowed. Use 16th positions (1.25/1.75, 3.25/3.75) when Sixteenth grid allowed and energy > 0.6, selected deterministically via RNG.

7. Are diagnostics required per operator invocation (opt-in), and what minimum diagnostic fields are expected for unit tests?
   **Answer:** Diagnostics are opt-in (Story 7.1). For unit tests, verify `CandidateId`, `OperatorId`, `Role`, `Beat`, `Strength`, `VelocityHint`, `Score` fields only.

## 8. Test Scenario Ideas (Unit Test Names & Setups)
- `GhostBeforeBackbeat_GeneratesGhostAtExpectedPositions_WhenEnergyHighAndHatAvailable`
  - Setup: 4/4 bar, energy=0.7, hat active, 16th grid allowed, seed fixed. Assert ghost candidate at 1.75 and 3.75 with VelocityHint ∈ [30,50].
- `GhostAfterBackbeat_Suppressed_WhenEnergyLowOrInFillWindow`
  - Setup low energy and IsFillWindow=true variations; expect no candidates.
- `KickPickup_ProducesKickAtBarBoundary_WithStableCandidateId_ForSameSeed`
  - Setup: last beat anticipation, check `BarNumber`/`Beat` and deterministic `CandidateId` across runs with same seed.
- `KickDouble_Uses16thVariants_WhenGridAllows_OtherwiseUses8th`
  - Setup grid toggles; assert beat positions differ accordingly.
- `HatEmbellishment_RespectsActiveRolesAndMaxPerBar`
  - Setup: hi-hat disabled -> no candidates; hi-hat enabled -> candidates count within expected small range.
- `GhostCluster_GeneratesMiniFill_NotOverlapWithExistingFill`
  - Setup memory indicating recent fill; assert cluster suppressed or shifted.
- `FloorTomPickup_PositionedBeforeBarAndVelocityHint_InPickupRange`
  - Setup and assert beat ~4.75 on previous bar and VelocityHint ∈ [60,80].
- `Operator_CanApply_ReturnsFalse_ForNullOrMalformedContext`
  - Defensive test for parameter validation.
- `Operators_AreDeterministic_AcrossSeeds_VaryAcrossDifferentSeeds`
  - Determinism tests: same seed -> same outputs; different seed -> differences when multiple valid options.

---

// End of pre-analysis for Story 3.1
