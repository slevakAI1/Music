# PreAnalysis H1

## Story
**ID:** H1
**Title:** Full Groove Phase Unit Tests (Core)

## 1. Story Intent Summary
- What: Provide a complete suite of unit tests that exercise each groove phase and lock regressions for core behaviors listed in acceptance criteria.
- Why: Ensure determinism, correctness of musical guardrails (caps, density, selection, velocity, timing, merge policy), and prevent regressions as the groove system evolves.
- Who: Developers and QA benefit most (ensures generator correctness); downstream consumers (drum generator, drummer policy) gain reliability; product owners gain confidence in release stability.

## 2. Acceptance Criteria Checklist
1. Test: variation merge respects additive/replace/tag gating.
2. Test: candidate filtering by enabled tags and empty-tag semantics.
3. Test: deterministic weighted selection with stable tie-breaks.
4. Test: density computation with multipliers and overrides.
5. Test: caps enforcement (per bar, per beat, per role, per group, per candidate).
6. Test: onset strength classification for 4/4 and 3/4.
7. Test: velocity shaping lookups + clamp + ghost velocity.
8. Test: feel timing for all `GrooveFeel` values + swing amounts.
9. Test: role timing feel + bias + clamp + overrides.
10. Test: merge policy booleans matrix.
11. Test: diagnostics on/off produces identical events.

Notes: Criteria 6–9 reference previous stories (D1, D2, E1, E2). "Merge policy booleans matrix" implies combinatorial tests (matrix of boolean settings). "Diagnostics on/off produces identical events" is an invariance requirement that may need careful isolation.

Ambiguities: "all `GrooveFeel` values"—which discrete set is intended (Straight/Swing/Shuffle/Triplet only?), and acceptable test coverage (one value per feel or multiple swing amounts?).

## 3. Dependencies & Integration Points
- Dependent stories (must be completed or stubbed for tests): A1, A2, A3, B1, B2, B3, B4, C1, C2, C3, D1, D2, E1, E2, F1, G1, G2, SC1.
- Primary code/types touched by tests:
  - `GrooveBarPlan`, `GrooveOnset`, `GrooveBarContext`, `GrooveVariationCatalog`, `GrooveCandidateGroup`, `GrooveOnsetCandidate`
  - Selection & cap engines: `GrooveSelectionEngine`, `GrooveCapsEnforcer`, `GrooveCapsPolicy`/`GrooveRoleConstraintPolicy`, `RoleRhythmVocabulary`
  - RNG helper: `Rng` / `RngFor` streams
  - Velocity/timing/strength: `VelocityShaper`, `OnsetStrengthClassifier`, `FeelTimingEngine`, `RoleTimingEngine`
  - Override/enforcer: `OverrideMergePolicyEnforcer`, `GroovePolicyDecision`, `IGroovePolicyProvider`
  - Diagnostics: `GrooveDiagnosticsCollector`, `GrooveBarDiagnostics`
  - Utilities: `BarTrack`, `PartTrackBarCoverageAnalyzer`
- Provides for future stories: a safety net enabling Story H2 (golden regression snapshot) and confidence for Pop Rock Human Drummer integration.

## 4. Inputs & Outputs
- Inputs consumed by tests:
  - `SongContext` or minimal bar-level `GrooveBarContext` fixtures
  - `GroovePresetDefinition` and `SegmentGrooveProfile` test presets
  - Candidate catalogs (`GrooveVariationCatalog`) or mock `IGrooveCandidateSource`
  - Protection/policy objects (`GrooveProtectionPolicy`, `GrooveAccentPolicy`, `GrooveTimingPolicy`, `GrooveOverrideMergePolicy`)
  - Deterministic RNG seed(s) and explicit `RandomPurpose`/stream keys if applicable
- Outputs asserted by tests:
  - `GrooveBarPlan.FinalOnsets` contents (counts, identities, velocities, timing offsets, provenance)
  - Diagnostics records when enabled
  - Deterministic RNG draws per stream (for selection/tie-break tests)
  - Boolean invariants (diagnostics off == diagnostics disabled and no behavior change)
- Configuration read:
  - Role density targets, vocabulary caps, candidate/group max adds, velocity rules, allowed subdivisions, feel/swing settings, override merge policy booleans.

## 5. Constraints & Invariants
- Determinism: same inputs + same seed => identical outputs.
- Never prune or remove onsets with `IsMustHit` or `IsNeverRemove` set.
- Respect `MaxHitsPerBar`, `MaxHitsPerBeat`, `RoleMaxDensityPerBar[role]`, `Group.MaxAddsPerBar`, `Candidate.MaxAddsPerBar` as hard guards.
- Velocity shaping must not overwrite existing `GrooveOnset.Velocity` values (only fill null velocities).
- Diagnostics toggled off must be zero-cost (no behavioral change) and toggled on must produce expected trace fields.
- Ordering: selection happens before pruning (select-add → cap-enforce/prune → final list).

## 6. Edge Cases to Test
- Empty candidate pool + non-zero density target (pool exhaustion safety).
- All candidates filtered out by tags (ensure selection stops cleanly).
- Candidate weights zero or negative (treated as zero / filtered out).
- Ties in scores across candidates/groups (ensure deterministic tie-break or RNG stream usage).
- Multiple overlapping caps active (per-beat + per-bar + per-role + per-group + per-candidate) and which pruning rule triggers.
- Anchors all protected and candidate adds would exceed caps (ensure protected anchors preserved and additions pruned appropriately).
- Velocity shaping when role rule missing (fallback to Offbeat or global default) and ghost velocity precedence.
- Feel timing with triplet vs straight grids and swing amounts at boundaries (0, 0.5, 1.0).
- Role timing bias exceeding `MaxAbsTimingBiasTicks` (clamping behavior).
- Diagnostics enabled vs disabled parity checks.
- Null/empty policies or missing section/profile information.
- Concurrent or repeated test runs ensuring no shared mutable state leaks (RNG or static caches).

## 7. Clarifying Questions
1. Test scope: should H1 cover only unit tests (isolated classes) or also small integration tests that run the end-to-end pipeline for a bar/role? Which is preferred?
2. Test framework: project uses MSTest and existing tests exist — should new tests follow the same MSTest conventions and live in `Music.Tests`? Any naming/organization constraints?
3. Deterministic seeds: is there an agreed canonical seed and RNG stream naming convention for tests, or should each test set its own fixed seed? Should tests assert RNG stream sequences explicitly?
4. "All `GrooveFeel` values": confirm the authoritative enum members to cover (Straight, Swing, Shuffle, TripletFeel?) and whether multiple swing amounts per feel are required.
5. Merge policy matrix: what exact boolean combinations are required (all 4 booleans cross-product = 16 tests?)—is full combinatorial coverage expected or a representative subset?
6. Diagnostics parity: what minimal fields must be asserted when diagnostics are enabled? Is there an existing helper to compare diagnostics snapshots?
7. Golden test separation: should H1 produce deterministic snapshots for later H2 golden-file tests, and where should those fixtures be stored?
8. Performance/timeout: are there limits on test execution time or constraints for CI parallelization?
9. Mocking vs real catalogs: prefer using lightweight real test catalogs (subset) or mock `IGrooveCandidateSource` behaviors for precise control?
10. Concurrency: Are tests allowed to run in parallel? Any known static mutable state that must be reset between tests (e.g., `Rng.Initialize`)?

## 8. Test Scenario Ideas (suggested names & setups)
- `VariationMerge_AdditiveAndReplace_RespectsTags`
  - Setup: two-layer catalog with additive and replace layers; segment tags vary. Assert merged set equals expected.
- `CandidateFiltering_EmptyTags_MatchAll`
  - Setup: group/candidates with null/empty tags. Assert they pass filter when no enabled tags.
- `WeightedSelection_Deterministic_TieBreakStableId`
  - Setup: candidates with equal weight; fixed seed. Assert deterministic selected IDs and tie-break behavior.
- `DensityTarget_RoundingAndClamp_AppliesMultiplierAndOverride`
  - Setup: density 0.333 × max=5, multiplier and override applied. Assert target count rounded and clamped.
- `CapsEnforcer_PruneLowestScore_ProtectsMustHit`
  - Setup: anchors protected; numerous additions exceed caps. Assert protected kept and lowest additions removed.
- `OnsetStrength_Classifier_4_4_and_3_4`
  - Setup: beats in 4/4 and 3/4 with subdivisions. Assert strengths per beat.
- `VelocityShaper_GhostPrecedenceAndClamp`
  - Setup: ghost rules absent/present. Assert ghost precedence and final velocity clamp.
- `FeelTiming_SwingAndTriplet_Behavior`
  - Setup: test eighth offbeat shifts for swing 0/0.5/1 and triplet mapping. Assert timing offsets.
- `RoleTiming_ClampAndOverride_Applies`
  - Setup: timing feel+bias with override exceeding max. Assert final clamped ticks.
- `OverrideMergePolicy_BooleanMatrix_Behavior`
  - Setup: run a small matrix of policy booleans and assert expected list-replace/union and protected removal behavior.
- `Diagnostics_Toggle_NoBehaviorChange_And_TraceProduced`
  - Setup: run generation twice with diagnostics off/on. Assert final onsets identical and diagnostics populated when enabled.

## Determinism Verification Points
- Repeatable result with same seed for: candidate selection, prune tie-breaks, velocity shaping randomness (if any), and timing jitter streams.
- RNG streams isolation per purpose: selection RNG draws do not affect timing RNG draws in other tests.

<!-- End of pre-analysis -->
