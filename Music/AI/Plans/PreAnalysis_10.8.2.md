# Pre-Analysis — Story 10.8.2

Story: 10.8.2 — Implement Drummer Unit Tests (Core)

## 1) Story Intent Summary
- What: Provide a comprehensive unit test suite that verifies core drummer agent behaviors (operators, selection, memory, physicality, density, section-aware behavior, determinism).
- Why: Ensures correctness, prevents regressions, and guarantees deterministic behavior required by the generator and tuning workflows.
- Who: Developers (stability and refactor safety), CI (regression gating), and indirectly end-users (reliable generation).

## 2) Acceptance Criteria Checklist
1. All 28 operators generate valid candidates.
2. Operator weights affect selection frequency.
3. Memory penalty affects repetition (reduces repeat frequency).
4. Physicality filter rejects impossible patterns.
5. Density targets are respected by selection pipeline.
6. Section-aware behavior: chorus busier than verse.
7. Fill windows are respected (fills only in allowed windows).
8. Determinism: same seed → identical output.
9. Different seeds → different output when multiple valid options exist.
10. Pop Rock configuration loads and applies correctly.

Notes: Grouped where helpful (operators/candidates, selection/memory/density, physicality, section/fill, determinism, style configuration).

## 3) Dependencies & Integration Points
- Depends on completed stories across Stage 10: 10.1 (contracts), 10.2 (drummer core), 10.3 (operators), 10.4 (physicality), 10.6 (performance), and 10.8.1 (DrummerAgent wiring).
- Interacts with existing types/services: `DrummerAgent`, `DrummerPolicyProvider`, `DrummerCandidateSource`, `DrumOperatorRegistry`, `OperatorSelectionEngine`, `AgentMemory`/`DrummerMemory`, `PhysicalityFilter`, `StyleConfigurationLibrary` (PopRock), RNG (`Rng`), `GrooveOnsetCandidate`/`DrumCandidate`, `PartTrack`/`SongContext` for end-to-end tests.
- Provides: test coverage and golden snapshots used by future regression/acceptance tests and by tuning/golden-file story 10.8.3.

## 4) Inputs & Outputs
- Consumes: style configuration (`PopRock`), deterministic RNG seed, `SongContext` or minimal `GrooveBarContext` fixtures, registered operators from `DrumOperatorRegistry`, physicality rules.
- Produces: test assertions, generated `PartTrack` or `GrooveOnset` snapshots, operator usage logs/diagnostics (for asserts), and one or more golden JSON snapshots used in tests.
- Reads configuration: style weights, role caps, RNG seeds, memory settings (min bars between fills), physicality rules.

## 5) Constraints & Invariants
- Determinism: same seed + same inputs → identical output (must hold for all deterministic tests).
- Protected onsets (must-hit) MUST never be pruned by physicality or selection pruning.
- PartTrack event ordering: generated `PartTrack.PartTrackNoteEvents` must be sorted by `AbsoluteTimeTicks` for MIDI/export validation.
- Physicality invariants: no selected candidate violates limb/sticking rules or hard caps.
- Density invariants: selection engine must not exceed per-role caps and per-bar density targets.
- Memory invariants: repetition penalties must be applied deterministically and reduce selection probability for recently used operators.
- Order: policy decisions → candidate generation → physicality filtering → operator selection → performance shaping.

## 6) Edge Cases to Test
- No enabled operators (empty allow-list) → selection should produce empty candidate set or a defined fallback behavior.
- All candidates filtered by physicality → confirm selection handles zero candidates gracefully and does not crash.
- Conflicting caps (role cap < protected onsets) → test precedence: protected onsets vs caps.
- Minimal bars (1 bar) and very short sections: ensure fill windows/section logic behave.
- High-energy vs low-energy toggles: verify density and operator gating.
- Time signature variations and non-4/4 bars: ensure operators and density calculations adapt (2/4, 3/4, 6/8).
- Repeated seeds across different style configs: same seed but different style → different output.
- Protected onsets present alongside overcrowding: ensure pruning never removes must-hit events.
- Concurrent modifications to `DrummerMemory` or registry (thread-safety) — if tests run in parallel, ensure isolation or test sequence.
- Random tie-break scenarios where scores equal — verify deterministic tie-break rules produce consistent ordering.

## 7) Clarifying Questions

### Question 1:
Test framework and project: confirm we should use `xUnit` and place tests in `Music.Tests` (Epic references both in-project tests and `Music.Tests`).

**Answer:**
Yes, use xUnit framework and place all tests in the `Music.Tests` project. Create test files in `Music.Tests/Generator/Agents/Drums/` directory as specified in Story 10.8.2 acceptance criteria.

---

### Question 2:
What precisely defines a "valid candidate" for an operator? (required fields, acceptable velocity/timing ranges, valid `Role` values)

**Answer:**
A valid `DrumCandidate` must have:
- Non-null `CandidateId`, `OperatorId`, `Role`, `BarNumber`, `Beat`, `Strength`, `FillRole`, `Score`
- `Role` must be from the set: Kick, Snare, ClosedHat, OpenHat, Crash, Ride, Tom1, Tom2, FloorTom
- `VelocityHint` (nullable): if present, must be 0-127
- `TimingHint` (nullable): if present, any integer tick offset is valid
- `ArticulationHint` (nullable): if present, must be a valid `DrumArticulation` enum value
- `Beat` must be within bar bounds (1-based, fractional, typically 1.0 to beatsPerBar+1.0)
- `Score` must be 0.0-1.0

---

### Question 3:
Selection-frequency assertions: should tests assert exact counts with repeated deterministic runs, or statistical/probabilistic expectations (and what thresholds)?

**Answer:**
Tests must assert **exact counts** using fixed deterministic seeds. The RNG system guarantees "same seed → identical output" per the determinism constraint. Run selection multiple times with the same seed and assert identical results each time. No statistical thresholds needed—test for perfect repeatability.

---

### Question 4:
Golden snapshot path and update workflow: should we use `Music.Tests/Generator/Agents/Drums/Snapshots/PopRock_Standard.json` as canonical location and document an update process?

**Answer:**
Yes, use `Music.Tests/Generator/Agents/Drums/Snapshots/PopRock_Standard.json` as the canonical golden snapshot location (per Story 10.8.3 files to create). Update workflow: when behavior changes by design, regenerate the snapshot, manually review the diff, and commit the updated snapshot with a clear explanation of why output changed.

---

### Question 5:
Physicality presets: are there canonical `PhysicalityRules` presets (Strict/Normal/Loose) to use in tests, or should tests construct minimal deterministic rules?

**Answer:**
Use canonical `PhysicalityRules` with `StrictnessLevel` enum values (Strict/Normal/Loose) when testing general physicality behavior. For targeted unit tests that verify specific constraints (e.g., limb conflicts, sticking rules), construct minimal deterministic `PhysicalityRules` instances with only the rules under test enabled to isolate behavior.

---

### Question 6:
Section density canonical values: what are the expected density targets for `PopRock` sections (Intro/Verse/Chorus/Bridge) to assert quantitative differences?

**Answer:**
Per Story 10.2.3 and CurrentEpic Stage 10.5, canonical PopRock density targets are:
- Intro: 0.4
- Verse: 0.5
- Chorus: 0.8
- Bridge: (varies, typically 0.3-0.6 depending on context)

Assert that chorus density (0.8) > verse density (0.5) in section-aware behavior tests.

---

### Question 7:
RNG isolation: should each test call `Rng.Initialize(seed)` and fully avoid shared global RNG state, or is there a per-test RNG helper available?

**Answer:**
Each test must call `Rng.Initialize(seed)` in its constructor or test method setup with a fixed, unique seed per test. xUnit creates new test class instances per test, which provides isolation. There is no per-test RNG helper—use constructor-based initialization pattern as documented in ProjectArchitecture test conventions.

---

### Question 8:
Test isolation/parallelism: should tests be runnable in parallel, or should they be marked to run sequentially due to shared global state (files, RNG, registries)?

**Answer:**
Tests should be designed to run in parallel. Each test must initialize its own fresh state (RNG seed, operator registry, memory, style configuration) to avoid shared global state conflicts. xUnit's default test instance-per-test model supports this. Do not use shared static state across tests. If a specific test absolutely requires sequential execution, mark it with `[Collection("Sequential")]` attribute, but prefer isolated parallel-safe tests.

---

### Question 9:
Scope: prefer unit-level tests (isolated operator + selection engine) or end-to-end pipeline tests that produce `PartTrack` output for golden comparisons? (both implied; prefer clarification.)

**Answer:**
For Story 10.8.2, focus on **unit-level tests** that verify isolated components:
- Individual operators generate valid candidates
- Selection engine respects weights/caps/memory
- Physicality filter removes violations
- Determinism at component level

End-to-end `PartTrack` output and golden file comparisons are covered by Story 10.8.3 (Golden Test). Story 10.8.2 provides the component-level foundation that 10.8.3 builds upon.

## 8) Test Scenario Ideas (suggested test names)
- `Operators_AllOperatorsProduceValidCandidates` — verify each operator emits candidates meeting "valid candidate" criteria.
- `Selection_StyleWeightsAffectSelectionCounts` — deterministic seed + repeated selection rounds → verify weighted selection differences or exact counts per spec.
- `Memory_RepetitionPenaltyReducesSelection` — prime memory then assert reduced selection frequency for recently used operator.
- `Physicality_FilterRemovesImpossiblePatterns` — create conflicting limb assignments and assert removed candidates; ensure protected onsets survive.
- `Density_TargetsRespectedAcrossRoles` — simulate a bar and assert total selected hits <= target and per-role caps honored.
- `SectionBehavior_ChorusHasHigherDensityThanVerse` — two contexts (verse/chorus) with same seed → assert chorus selection density > verse by configured delta.
- `FillWindow_FillsOnlyInAllowedWindow` — assert fill operators only produce selected hits when `IsFillWindow=true`.
- `Determinism_SameSeedProducesSameOutput` — full pipeline run with fixed seed → compare snapshot to baseline.
- `Determinism_DifferentSeedProducesDifferentOutput` — assert at least one bar differs when changing seed.
- `PopRock_ConfigurationLoadsAndApplies` — load PopRock and assert style-specific operator gating and operator weight entries exist.

---

Notes: This  pre-analysis includes also encoding artifacts, adds missed invariants (protected onsets, PartTrack ordering), adds time-signature edge cases, and surfaces additional clarifying questions about test project location, golden snapshot workflow, RNG isolation, and test scope. Tests should emphasize deterministic assertions and clear fixtures to isolate randomness and shared state.
