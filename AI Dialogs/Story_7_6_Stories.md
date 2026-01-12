# Story 7.6 — Structured repetition engine (decomposition)

Goal: implement `SectionVariationPlan` and deterministic A / A’ / B transforms so repeated sections evolve in a controlled, musical way while preserving guardrails.

Overview: This task is decomposed into six stories to keep scope small, testable, and align with existing Stage 7 contracts.

## 7.6.1 — `SectionVariationPlan` model & storage

Intent: define the immutable data model and storage contract for per-section variation plans.

Acceptance criteria:
- Add `SectionVariationPlan` record with:
  - `int? BaseReferenceSectionIndex`
  - `double VariationIntensity` in [0..1]
  - `Dictionary<string, double> RoleMultipliers` (role name -> multiplier)
  - `Dictionary<string, int> RoleBiases` (optional integer biases)
  - `IReadOnlyList<string> Flags` (small intent tags)
- Provide serialization-friendly layout and a factory helper `SectionVariationPlan.Create(...)`.
- Add unit tests verifying immutability, value clamping, and JSON round-trip.

Notes: keep model lightweight; use role keys consistent with `VoiceSet` names.

## 7.6.2 — Deterministic Variation Planner (core algorithm)

Intent: build the planner that deterministically generates a `SectionVariationPlan` for each section.

Acceptance criteria:
- Implement `SectionVariationPlanner` with method `GetPlan(SectionTrack, int absoluteSectionIndex, EnergySectionProfile, EnergyArc, seed)`.
- Planner uses deterministic drivers: section type/index, energy target, tension transition hint, groove/style, and seed as tie-break.
- Planner outputs bounded multipliers/biases obeying `VariationIntensity`.
- Planner exposes `GetAllPlans(sectionTrack, energyArc, seed)` convenience method.
- Add unit tests verifying determinism, idempotence, and safe bounds.

Notes: keep algorithm simple (weighted rules + deterministic hashing) to start; make it pluggable.

## 7.6.3 — Per-role transform executors (application surface)

Intent: implement small, role-specific transform adapters that apply `SectionVariationPlan` to role generators without changing their core logic.

Acceptance criteria:
- For each role (`Drums`, `Bass`, `Comp`, `Keys`): implement a thin adapter that maps plan fields into existing role knobs:
  - `Drums`: adjust `DensityMultiplier`, `FillProbability`, `FillComplexity`.
  - `Bass`: adjust approach/octave chance, busy probability.
  - `Comp`: adjust `DensityMultiplier`, anticipateProbability, strum variations.
  - `Keys`: adjust `DensityMultiplier`, register lift semitones.
- Adapters must be pure mapping functions that return a new parameter object; they must not mutate global state.
- Update role generator constructors or parameter inputs to accept optional variation overrides.
- Add integration tests asserting that applying a plan modifies parameters within allowed guardrails.

Notes: map only—do not change generator core algorithms; this keeps behavior additive and safe.

## 7.6.4 — Query API: `GetVariationPlan(sectionIndex)` and caching

Intent: provide a stable, fast query surface used by generators and diagnostics.

Acceptance criteria:
- Add `IVariationQuery` with `SectionVariationPlan GetVariationPlan(int absoluteSectionIndex)` and `IReadOnlyList<SectionVariationPlan> GetAllPlans()`.
- Implement `DeterministicVariationQuery` that precomputes plans for all sections and caches them (constructed from `SectionTrack`, `EnergyArc`, `seed`).
- Ensure thread-safe immutable reads.
- Wire `Generator` to accept an optional `IVariationQuery` and use plan values when present.
- Add tests verifying cached results are deterministic and thread-safe.

Notes: follow existing pattern used by `EnergyArc` and `ITensionQuery` for consistency.

## 7.6.5 — Guardrails, precedence, and conflict resolution

Intent: ensure variation never violates existing guardrails (lead-space, low-end, density caps) and resolves conflicts deterministically.

Acceptance criteria:
- Implement `VariationGuardrail` helpers that clamp role multipliers/biases against `EnergyProfile` and global caps.
- Define precedence rules (configurable via policy): e.g., `Bass` low-end > `Comp` density > `Keys` density.
- Integrate guardrails into the planner and per-role adapters so final parameters are safe.
- Add tests validating guardrail enforcement and deterministic tie-breaks.

Notes: keep defaults conservative; expose policy hooks for later tuning.

## 7.6.6 — Diagnostics, test coverage, and examples

Intent: add diagnostics and tests so behavior is observable and regression-safe.

Acceptance criteria:
- Add `VariationDiagnostics` to dump per-section `SectionVariationPlan` and explain which rules contributed and final clamped values.
- Add unit and integration tests covering:
  - model round-trip
  - planner determinism under varying seeds/styles
  - adapter mapping correctness
  - guardrail enforcement
  - generator integration smoke test (ensures no runtime errors and parameters applied)
- Provide a small example script/snippet in docs showing how to create a `DeterministicVariationQuery` and consume plans in a generator run.

Notes: diagnostics must be opt-in and must not affect generation results.

---

Total stories: 6 (7.6.1 through 7.6.6). Each is designed to be independently implementable and testable, and together they realize the intent of Story 7.6 while preserving existing guardrails and determinism.
