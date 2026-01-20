# PreAnalysis_G1 — Story G1: Add Groove Decision Trace (Opt-in, No Behavior Change)

## 1) Story Intent Summary
- What: Add an opt-in diagnostics trace that records per-bar and per-role groove decision data (enabled tags, candidate counts, filters and reasons, density targets, selected candidates with weights and RNG streams, prune reasons, final onset summary).
- Why: Improves explainability and debuggability so developers can trace why the generator made specific choices and tune future drummer policies without changing generation behavior.
- Who benefits: Developers and integrators (debugging/tuning), QA (determinism/regression tests), and future Drummer/Operator engineers who need provenance and metrics to train or tune policies. End-users benefit indirectly via better-tuned generators.

## 2) Acceptance Criteria Checklist
1. Add an opt-in diagnostics flag (config or parameter).
2. When enabled, capture per bar + role:
   2.1. Enabled tags (after phrase/segment/policy).
   2.2. Candidate groups count and candidate count.
   2.3. Filters applied and why (tag mismatch, never-add, grid invalid, etc.).
   2.4. Density target inputs and computed target count.
   2.5. Selected candidates with weights/scores and RNG stream used.
   2.6. Prune events and reasons (cap violated, tie-break, protected preserved).
   2.7. Final onset list summary.
3. When disabled, diagnostics collection is effectively zero-cost and produces identical output.
4. Unit test: diagnostics on/off does not change generated notes.

Ambiguous/Unclear ACs:
- "Zero-cost-ish" when disabled is subjective — exact performance budget or measurement target is not defined.
- Level of detail required for "selected candidates with weights/scores" and the exact naming/format for RNG stream identifiers is unspecified.
- Scope of "filters applied and why" could be large; exact canonical set of filter reasons is not listed.

## 3) Dependencies & Integration Points
- Dependent stories (recommended):
  - `A1` (Groove Output Contracts) — diagnostics attach to `GrooveBarPlan`/`GrooveOnset` types.
  - `A2` (Deterministic RNG Stream Policy) — diagnostics must record RNG stream identity and seed deterministically.
  - `A3` (Drummer Policy Hook) — policy decisions should be recorded in diagnostics.
  - `G2` (Provenance) — provenance fields augment per-onset trace; G1 should record provenance if present.
  - Upstream generation stories (B*, C*, D*, E*, F*) — diagnostics will observe outputs and intermediate decisions from those systems.
nIntegration points (existing code/types):
- `GrooveBarPlan` / `GrooveOnset` — final output and where diagnostics reference final onsets.
- Selection engines: `GrooveSelectionEngine`, `GrooveVariationCatalog` (candidate counts, groups).
- Protection/pruning: `ProtectionPerBarBuilder`, `ProtectionApplier` (prune reasons, preserved protected onsets).
- RNG utilities: `Rng` / `RngFor` helper (record RNG stream keys/identifiers).
- Policy provider: `IGroovePolicyProvider` / `GroovePolicyDecision` (record overrides applied).
- Density/caps: `RoleDensityTarget`, `RoleRhythmVocabulary`, `GrooveRoleConstraintPolicy` (inputs & cap reasons).

What this story enables for future stories:
- Provides trace data required by `G2` (provenance validation) and future Drummer/Operator tuning stories.
- Enables golden-file regression snapshots (H2) that include decision metadata for richer diffs.

## 4) Inputs & Outputs
Inputs (consumed):
- `BarContext` / `GrooveBarContext` and `GrooveBarPlan` (base onsets, candidate sets).
- Candidate groups and `GrooveOnsetCandidate` lists from `GrooveVariationCatalog` or `IGrooveCandidateSource`.
- `GroovePolicyDecision` and segment `SegmentGrooveProfile` enabled tags/overrides.
- RNG stream identifiers/seeds from `Rng` helper.
- Constraint/cap policies: `RoleRhythmVocabulary`, `GrooveRoleConstraintPolicy`, per-group/candidate max adds.
nOutputs (produced):
- A diagnostics trace object attached to `GrooveBarPlan` or returned alongside it (per-bar, per-role structured trace).
- Optional serialized diagnostic artifact (for test snapshots or debugging) — format unspecified.
nConfiguration/settings read:
- Diagnostics enabled flag (global or per-run/per-song/per-bar parameter).
- Any diagnostics verbosity level (not defined in AC but may be necessary).

## 5) Constraints & Invariants
- Diagnostics must be opt-in; when disabled, generator behavior and outputs must be identical.
- Diagnostics must not mutate generation inputs in a way that changes randomness order (preserve deterministic RNG draws and ordering).
- Protected onsets (`IsMustHit`, `IsNeverRemove`, `IsProtected`) must never be removed; diagnostics should report attempted removals but not change behavior.
- Ordering invariant: decisions should be recorded in the canonical pipeline order (tag resolution → filtering → density computation → selection → prune → finalization).
- Determinism invariant: same inputs + same seed + same diagnostics flag state should produce identical generation results.
- Diagnostics storage footprint should be bounded or configurable to avoid unbounded memory growth in long songs.

## 6) Edge Cases to Test
- Empty candidate catalog: diagnostics should show zero groups/candidates and resulting behavior.
- All candidates filtered out by tags or grid: diagnostics should explain the filter reasons and show target not met.
- Conflicting policy overrides: diagnostics should record which override applied and precedence result.
- Ties in pruning/selection requiring RNG tie-breaks: diagnostics must record RNG stream used and deterministic tie-break outcome.
- Large number of bars/roles: verify diagnostics memory and serialization does not OOM; verify optional truncation.
- Diagnostics enabled vs disabled: ensure exact output equality and identical RNG draw sequences across both runs.
- Null or malformed `GroovePolicyDecision` fields: diagnostics should handle missing overrides gracefully and record fallback decisions.
- Concurrent generation (if supported): diagnostics capture per-bar traces without race conditions.
- Protected onsets near caps: diagnostics show preserved items and prune candidates chosen instead.

## 7) Clarifying Questions
1. Opt-in scope: should diagnostics be enabled globally (song/generator run) or allow finer granularity (per-segment, per-bar, per-role)?
2. Output format: do we need a canonical schema (POCOs only), or a serialized artifact (JSON/YAML) for snapshot tests and external tools? If serialized, what casing/fields are required?
3. Verbosity levels: do we require multiple verbosity levels (minimal vs full) or a single full trace only?
4. Performance target: what is the acceptable overhead when diagnostics are enabled and when disabled? Any microbenchmark target for "zero-cost-ish"?
5. Lifetime and retention: should diagnostics be attached to `GrooveBarPlan` in-memory only, stored on `Song` for later export, or written out to disk by default?
6. Privacy/logging: are there any restrictions on logging detailed trace data (PII/security) or retention policies?
7. Canonical names: what exact identifiers should be recorded for RNG streams and candidate/group stable ids to ensure stable diffs across runs?
8. Test expectations: for the AC test "diagnostics on/off does not change generated notes", should the test also assert the RNG sequence equality, or only final output equality?
9. Truncate policy: for very long songs or many candidates, should diagnostics include sampling/truncation rules or explicit size limits?
10. Relationship to `G2` (Provenance): should G1 produce provenance fields if not yet implemented, or only start recording provenance when `G2` is applied?

## 8) Test Scenario Ideas
- `Diagnostics_Enabled_DoesNotChangeGeneratedOnsets`: enable diagnostics; assert final onsets identical to disabled run and to golden baseline.
- `Diagnostics_Disabled_IsLightweight`: measure that diagnostics-disabled run does not allocate diagnostic structures (or that diagnostics field is null) and RNG sequence unchanged.
- `Diagnostics_Record_TagFilteringReasons`: use a catalog with mixed-tag candidates; assert diagnostics contains expected filter reasons per candidate and role.
- `Diagnostics_Record_DensityAndTarget`: configure role density and policy overrides; assert diagnostics shows inputs and computed target count.
- `Diagnostics_Record_SelectionWeightsAndRngStream`: build candidates with different weights; assert chosen candidates and recorded RNG stream/tie-break info match expected deterministic outcome for a fixed seed.
- `Diagnostics_Record_Prune_ReasonsAndProtectedPreserved`: construct a case where caps exceed allowed and protected anchors present; assert diagnostics shows protected preserved and prune candidates listed with reasons.
- `Diagnostics_Serializes_CanRoundTrip`: if a serialized format is chosen, assert trace serializes and deserializes without loss of essential fields used by tests.
- `Diagnostics_Scale_LongSongTruncationPolicy`: generate a long song and assert diagnostic size stays within configured limits or that truncation markers exist.

---

# Notes
- This pre-analysis assumes existing `Groove` types (`GrooveBarPlan`, `GrooveOnset`, `Rng`) are present per project architecture and that determinism is enforced by existing RNG policies.
- The acceptance criteria focus on content; implementors will need decisions on schema, verbosity, storage, and precise performance targets before implementation.
