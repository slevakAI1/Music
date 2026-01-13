# Story 7.6 — Structured repetition engine (decomposition)

Goal: implement `SectionVariationPlan` and deterministic A / A’ / B transforms so repeated sections reuse a stable "base" while evolving in bounded, style-safe ways.

Constraints:
- Deterministic: same `(seed, groove/style, sectionTrack)` => same plans.
- Safe: plans must not override existing role guardrails (range, density caps, lead-space ceilings).
- Planner-only: this is intent metadata + bounded knobs; core role algorithms remain unchanged.
- Minimal surface: expose a stable query contract so later stages can consume without refactors.

Overview: This work is decomposed into five stories to keep changes small, testable, and aligned with existing Stage 7 query patterns.

## 7.6.1 — `SectionVariationPlan` model (immutable + bounded)

Intent: define a compact, serialization-friendly model expressing section-to-section reuse and bounded per-role intent deltas.

Acceptance criteria:
- Add `SectionVariationPlan` record with:
  - `int AbsoluteSectionIndex`
  - `int? BaseReferenceSectionIndex` (null => no reuse)
  - `double VariationIntensity` in [0..1]
  - per-role bounded controls (minimum viable set):
    - `double? DensityMultiplier`
    - `int? VelocityBias`
    - `int? RegisterLiftSemitones`
    - `double? BusyProbability`
  - `IReadOnlySet<string> Tags` (small, stable intent tags like `A`, `Aprime`, `B`, `Lift`, `Thin`, `Breakdown`)
- Provide `Create(...)` helper(s) that clamp all numeric fields.
- Add unit tests verifying:
  - clamping/range enforcement
  - immutability
  - deterministic JSON round-trip (if the repo already has a JSON approach; otherwise skip JSON and just test invariants)

Notes:
- Prefer strongly-typed role fields over dictionaries to reduce ambiguity for later stages.
- Keep role set aligned with Stage 7: `Bass`, `Comp`, `Keys`, `Pads`, `Drums`.

## 7.6.2 — Base-reference selection (A / A’ / B mapping)

Intent: deterministically choose which section instances reuse which earlier "base" section so later renderers can repeat core decisions.

Acceptance criteria:
- Implement deterministic `BaseReferenceSectionIndex` selection rules:
  - same `SectionType` repeats tend to reference the earliest prior instance (A) unless contrast is required (B).
  - allow a deterministic B-case for `Bridge`/`Solo`/explicit contrasts.
  - ties resolved deterministically via stable keys (sectionType/index + groove/style + seed).
- Produce stable `Tags` at least including `A`, `Aprime`, and `B` where applicable.
- Add unit tests verifying:
  - determinism
  - expected mapping on common forms (e.g., Intro-V-C-V-C-Bridge-C-Outro)

Notes:
- This story does not apply the plan to generation; it only computes the reuse graph.

## 7.6.3 — Variation intensity + per-role deltas (bounded planner)

Intent: compute per-section bounded per-role deltas driven by existing Stage 7 intent (energy/tension/transition hint).

Acceptance criteria:
- Implement `SectionVariationPlanner` that outputs a `SectionVariationPlan` for each section.
- Deterministic drivers (no new systems):
  - section type/index and its base/reference selection (7.6.2)
  - section energy target (from `EnergyArc` / `EnergySectionProfile`)
  - tension transition hint (from existing tension query contract) when available
  - groove/style identity
  - seed used only for deterministic tie-breaks
- Rules must be conservative and clamped:
  - `VariationIntensity` stays small by default; only rises near transitions or higher-energy sections.
  - per-role deltas are bounded and optional (null means "no change").
- Unit tests:
  - determinism
  - values always in valid ranges
  - variation differs across repeats in at least one controlled way (A vs A’) while staying bounded

Notes:
- This is planning; do not encode bar/slot-level behavior here.

## 7.6.4 — Query surface + generator wiring

Intent: expose a stable query method `GetVariationPlan(sectionIndex)` and integrate it into the pipeline without changing existing behavior when absent.

Acceptance criteria:
- Add `IVariationQuery` with:
  - `SectionVariationPlan GetVariationPlan(int absoluteSectionIndex)`
- Implement `DeterministicVariationQuery` that precomputes and caches plans for the whole `SectionTrack`.
- Update generator entrypoint(s) to optionally accept/use `IVariationQuery`.
  - If not provided, generation remains unchanged.
- Add tests verifying:
  - determinism of cached plans
  - no plan => no behavior change (where test harness allows; otherwise validate by comparing parameters passed into role generators)

Notes:
- Mirror the architecture style used by `EnergyArc` caching and `ITensionQuery`.

## 7.6.5 — Role-parameter application adapters + minimal diagnostics

Intent: apply `SectionVariationPlan` to existing role parameter objects in a safe, non-invasive way and make it debuggable.

Acceptance criteria:
- Add thin, pure mapping helpers that take:
  - existing role profile/parameters (from Stage 7 energy/tension)
  - optional `SectionVariationPlan`
  - output adjusted parameters with clamps/guardrails preserved
- Apply in at least: `Drums`, `Bass`, `Comp`, `Keys/Pads` (as feasible with current parameter surfaces).
- Add minimal opt-in diagnostics:
  - one-line-per-section dump: baseRef + intensity + non-null per-role deltas
  - diagnostics must not affect generation results
- Add tests verifying:
  - applying a plan adjusts parameters only within caps
  - determinism

Notes:
- Keep mapping intentionally shallow: bias existing knobs (density/velocity/busy/register), do not add new musical logic.

---

Total stories: 5 (7.6.1 through 7.6.5). Together they implement Story 7.6 intent (structured repetition with safe evolution) while keeping scope planner-level and preserving determinism.
