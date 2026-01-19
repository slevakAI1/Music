# Groove System Completion Plan (Revised Agile Stories)
**Scope:** Finish the *groove system* (selection + constraints + velocity + timing + overrides + diagnostics + tests) with **hooks ready** for a future “Pop Rock Human Drummer” epic.  
**Non-goals in this plan:** implementing a specific drummer style, building a large candidate library, or adding heavy data curation.
**All classes will use Music.Generator namespace and will be placed in the Generator/Groove subfolder**
**Use Rng() class for randomness**
**All classes created by these stories should be groove-specific, not part-specific.**

---

## Guiding Principles (kept explicit in story AC)
- Deterministic: same inputs + seeds => same output.
- Modular: groove system remains mostly instrument-agnostic; drum generator consumes its outputs.
- Explainable: every selection/prune decision can be traced (opt-in, zero behavior impact).
- Drummer-ready: the system accepts “policy outputs” (operator enablement, density/texture overrides, performance biases) without refactors.

---

## Definition of “Groove System Done” (for this milestone)
- Produces per-bar per-role **final onset list** with:
  - anchors + selected variations
  - constraint enforcement (caps/grid/vocabulary)
  - onset strength classification
  - per-onset velocity
  - per-onset timing offsets (feel + role microtiming)
- Supports segment overrides with merge policies.
- Exposes a **stable hook interface** for a future DrummerPolicy / OperatorEngine.
- Has tests that lock determinism + verify every enum/boolean handler.
- Has diagnostics/tracing to explain decisions.

---

# EPIC: Groove Core Completion (Drummer-Ready)

## Phase A — Prep: Stable Groove Interfaces + Deterministic RNG Streams   (COMPLETED)

### Story A1 — Define Groove Output Contracts (Drummer-Ready) (COMPLETED)
**As a** developer  
**I want** stable groove output types  
**So that** drums (and later other roles) can consume groove decisions consistently

**Acceptance Criteria:**
- [x] Create `GrooveBarContext` (or reuse/rename existing `DrumBarContext`) as an instrument-agnostic context model.
- [x] Create `GrooveOnset` record with fields at minimum: `Role`, `BarNumber`, `Beat`, `Strength?`, `Velocity?`, `TimingOffsetTicks?`, `Provenance?`, protection flags.
- [x] Create `GrooveBarPlan` containing:
  - [x] `BaseOnsets` (anchors)
  - [x] `SelectedVariationOnsets` (adds)
  - [x] `FinalOnsets` (after constraints)
  - [x] Optional `Diagnostics` reference (null when disabled)
- [x] Ensure existing drum generator can be refit to emit/consume `GrooveOnset` without changing audible output (Phase 1–4 behavior stays identical).
- [x] No new behavior changes beyond type plumbing.

**Implementation Notes:**
- Created `GrooveBarContext` in `Music/Generator/Groove/GrooveBarContext.cs` as an instrument-agnostic context with conversion methods from/to existing `BarContext`
- Created `GrooveOnset` in `Music/Generator/Groove/GrooveOnset.cs` with all required fields: `Role` (string), `BarNumber`, `Beat`, `Strength?`, `Velocity?`, `TimingOffsetTicks?`, `Provenance?`, and protection flags (`IsMustHit`, `IsNeverRemove`, `IsProtected`)
- Created `GrooveBarPlan` in `Music/Generator/Groove/GrooveBarPlan.cs` with `BaseOnsets`, `SelectedVariationOnsets`, `FinalOnsets`, optional `Diagnostics`, and `BarNumber`
- All types are immutable records for deterministic behavior
- Created comprehensive unit tests in `Music.Tests/Generator/Groove/GrooveOutputContractsTests.cs` - all 15 tests passing
- Backward compatibility: existing `DrumOnset` and drum generator remain unchanged; new types are ready for future groove system integration

---

### Story A2 — Formalize Deterministic RNG Stream Policy (COMPLETED)
**As a** developer  
**I want** a deterministic RNG stream contract per “random use case”  
**So that** future drummer policies can add randomness without breaking reproducibility

**Acceptance Criteria:**
- [ ] Define a canonical list of RNG stream keys (examples: `VariationGroupPick`, `CandidatePick`, `TieBreak`, `PrunePick`, `VelocityJitter`, `TimingJitter`).
- [ ] Implement a helper: `RngFor(bar, role, streamKey)` (or equivalent) that derives stable seeds.
- [ ] Replace any ad-hoc RNG usage in groove code paths with `RNG()` instances sourced via this helper.
- [ ] Add a unit test: same song inputs => identical RNG draw sequences per stream key.

---

### Story A3 — Add Drummer Policy Hook (No Behavior Yet) (COMPLETED)
**As a** developer  
**I want** optional external policy overrides  
**So that** a future “human drummer model” can drive groove without refactors

**Acceptance Criteria:**
- [ ] Define `IGroovePolicyProvider` (or similar) with method `GetPolicy(barContext, role) -> GroovePolicyDecision`.
- [ ] `GroovePolicyDecision` supports optional overrides:
  - [ ] `EnabledVariationTagsOverride`
  - [ ] `Density01Override`
  - [ ] `MaxEventsPerBarOverride`
  - [ ] `RoleTimingFeelOverride`, `RoleTimingBiasTicksOverride`
  - [ ] `VelocityBiasOverride` (simple additive or multiplier)
  - [ ] `OperatorAllowList` (reserved for later; unused now)
- [ ] Default provider returns “no overrides” and produces identical output to current system.
- [ ] Unit test: enabling the hook with default provider does not change output.

---

## Phase B — Variation Engine (Works with catalog today, supports operator engine later)

### Story B1 — Implement Variation Layer Merge (Catalog Merge) (COMPLETED)
**As a** generator  
**I want** variation layers merged with tag-gated additive/replace logic  
**So that** candidates can be composed cleanly across style/base/refinement layers

**Acceptance Criteria:**
- [ ] Iterate `GrooveVariationCatalog.HierarchyLayers` in order.
- [ ] Apply `AppliesWhenTagsAll` against bar’s enabled tags.
- [ ] If `IsAdditiveOnly=true`, union candidate groups (dedupe by stable id).
- [ ] If `IsAdditiveOnly=false`, replace the working set entirely.
- [ ] Preserve deterministic ordering in the merged result (stable sort by layer order + group id).
- [ ] Unit tests for:
  - [ ] additive union behavior
  - [ ] replace behavior
  - [ ] tag-gated apply/skip
  - [ ] deterministic ordering

---

### Story B2 — Implement Candidate Filtering (Group + Candidate Tags) (COMPLETED)
**As a** generator  
**I want** candidates filtered by active tags and phrase/fill windows  
**So that** only appropriate variations are considered per segment/bar

**Acceptance Criteria:**
- [ ] Resolve enabled tags from: segment profile, phrase hook policy, and optional `GroovePolicyDecision.EnabledVariationTagsOverride`.
- [ ] Filter `GrooveCandidateGroup` when any `GroupTags` intersects enabled tags.
- [ ] Filter `GrooveOnsetCandidate` when `Tags` specified and intersects enabled tags.
- [ ] Treat empty/null `GroupTags` and empty/null `Candidate.Tags` as “match all”.
- [ ] Add a deterministic unit test that verifies filtering outcomes across multiple bars with different segment tags.

---

### Story B3 — Implement Weighted Candidate Selection (Deterministic) (COMPLETED)
**As a** generator  
**I want** weighted selection for variation candidates  
**So that** variation is configurable but reproducible

**Acceptance Criteria:**
- [ ] Compute weight per candidate: `ProbabilityBias * Group.BaseProbabilityBias`.
- [ ] Use `RngFor(bar, role, VariationPick)` for all selection randomness.
- [ ] Implement deterministic tie-breaking (weight desc, then stable id).
- [ ] Unit tests:
  - [ ] same seed => identical selections
  - [ ] different seed => different selections when multiple valid options exist
  - [ ] zero/negative weights handled safely (treated as 0, or filtered out)

---

### Story B4 — Add “Operator Candidate Source” Hook (COMPLETED)
**As a** developer  
**I want** a second candidate source interface  
**So that** future operator-based drummer logic can supply candidates without changing the engine

**Acceptance Criteria:**
- [ ] Define `IGrooveCandidateSource` returning candidate groups for `(barContext, role)`.
- [ ] Default implementation adapts existing `GrooveVariationCatalog` into this interface.
- [ ] Engine consumes `IGrooveCandidateSource` instead of directly reading the catalog.
- [ ] No behavior change with the default adapter.
- [ ] Add one unit test ensuring adapter output equals prior merged catalog output.

---

## Phase C — Density & Caps (Selection count + hard guardrails) (COMPLETED)

### Story C1 — Implement Density Target Computation (Role + Section + Policy)
**As a** generator  
**I want** a consistent target-count calculation  
**So that** busyness is controlled predictably

**Acceptance Criteria:**
- [ ] Read `RoleDensityTarget.Density01` and `RoleDensityTarget.MaxEventsPerBar`.
- [ ] Apply `SectionRolePresenceDefaults.RoleDensityMultiplier[role]`.
- [ ] Apply `GroovePolicyDecision.Density01Override` and/or `MaxEventsPerBarOverride` when provided.
- [ ] Compute `TargetCount = round(Density01 * MaxEventsPerBar)` with clamping to `[0..MaxEventsPerBar]`.
- [ ] Unit tests for rounding/clamping and multiplier impact.

---

### Story C2 — Select Until Target Reached (With Pool Exhaustion Safety) (COMPLETED)
**As a** generator  
**I want** to select candidates until target count is reached  
**So that** density controls how many additions happen

**Acceptance Criteria:**
- [ ] Select candidates in weighted order with deterministic RNG.
- [ ] Stop when target reached or candidate pool exhausted.
- [ ] Do not exceed `MaxEventsPerBar` from density target (even if pool large).
- [ ] Preserve protected/must-hit anchors; selection only adds.
- [ ] Unit test: small pool + high density => selects all without error.

---

### Story C3 — Enforce Hard Caps (Per Bar / Per Beat / Per Role)
**As a** generator  
**I want** strict cap enforcement  
**So that** output never turns into mush

**Acceptance Criteria:**
- [ ] Enforce `RoleRhythmVocabulary.MaxHitsPerBar`.
- [ ] Enforce `RoleRhythmVocabulary.MaxHitsPerBeat`.
- [ ] Enforce `GrooveRoleConstraintPolicy.RoleMaxDensityPerBar[role]`.
- [ ] Enforce per-candidate `GrooveOnsetCandidate.MaxAddsPerBar`.
- [ ] Enforce per-group `GrooveCandidateGroup.MaxAddsPerBar`.
- [ ] Prune by deterministic policy:
  - [ ] Never prune `IsMustHit` or `IsNeverRemove`.
  - [ ] Prefer pruning non-protected, lowest-scored additions first.
  - [ ] Use `RngFor(bar, role, PrunePick)` only when ties remain after stable sorting.
- [ ] Unit tests cover each cap independently + combined caps.

---

## Phase D — Onset Strength + Velocity (Human realism hooks, still deterministic)

### Story D1 — Implement Onset Strength Classification (All Meters) (COMPLETED)
**As a** generator  
**I want** consistent onset strength classification  
**So that** velocity and timing policies can reference musical meaning

**Acceptance Criteria:**
- [x] Classify `Downbeat` (beat 1 of bar) for all meters.
- [x] Classify `Backbeat` using meter defaults table (2/4, 3/4, 4/4, 5/4, 6/8, 7/4, 12/8).
- [x] Classify `Strong` using meter defaults table for required meters.
- [x] For "other meters", apply deterministic fallback rule (even/odd number rules).
- [x] Classify `Offbeat` relative to **active subdivision grid**:
  - [x] Eighth grid: integer + 0.5
  - [x] Triplet grid: integer + 1/3 (middle triplet)
- [x] Classify `Pickup` as the **last subdivision before stronger beat**, relative to active grid:
  - [x] Sixteenth grid: integer + 0.75
  - [x] Triplet grid: integer + 2/3
  - [x] Bar-end anticipation supported
- [x] Use epsilon tolerance `0.002` beats for all comparisons.
- [x] Support explicit `GrooveOnsetCandidate.Strength` overriding computed strength unconditionally.
- [x] Updated all unit tests to include grid parameter (66 tests, 100% passing)
- [x] Added triplet grid-specific tests (1.333, 1.666 handling)
- [x] Added 12/8 meter tests
- [x] Verified determinism with grid parameter

**Implementation Summary:**
- **BREAKING CHANGE**: Classifier signature now requires `AllowedSubdivision` parameter for grid-aware detection
- Fully compliant with new Story D1 specification:
  - Grid-aware offbeat/pickup detection (triplet and straight grids)
  - 12/8 meter support (backbeat=7, strong=4,10)
  - Refined fallback rules (even/odd meter formulas documented)
  - Classification precedence: Pickup → Downbeat → Backbeat → Strong → Offbeat → fallback
- All extension methods updated to include grid parameter
- Code comments document all meter-specific rules and fallback formulas
- **Test Results**: 66 tests, 100% passing, 4.6s build time
- See `Music/AI Dialogs/StoryD1_Complete.md` for full implementation report

**Meter-Specific Rules Implemented:**
- **2/4:** Backbeat on 2, no strong beats
- **3/4:** Backbeat on 2, Strong on 3
- **4/4:** Backbeats on 2 & 4, Strong on 3
- **5/4:** Backbeats on 2 & 4, Strong on 3
- **6/8:** Backbeat on 4 (updated), Strong on 3 & 6 (updated)
- **7/4:** Backbeats on 3 & 5, Strong on 2, 4, 6
- **12/8:** Backbeat on 7 (NEW), Strong on 4 & 10 (NEW)
- **Other:** Deterministic fallback: even=(N/2+1), odd=Ceil(N/2+0.5)

**Configuration Note**:
- 3/4 backbeat configuration belongs in `GrooveAccentPolicy` per specification
- Current: hardcoded (future: configurable via policy)

**Ready for Story D2 (Velocity Shaping)**

---

### Story D2 — Implement Velocity Shaping (Role x Strength) (COMPLETED)
**As a** generator  
**I want** velocities shaped by role and strength  
**So that** the groove has musical dynamics

**Acceptance Criteria:**
- [ ] Look up `GrooveAccentPolicy.RoleStrengthVelocity[role][strength]`.
- [ ] Use `VelocityRule.Typical` + `VelocityRule.AccentBias`.
- [ ] Clamp within `VelocityRule.Min/Max`.
- [ ] For `Ghost`, use `GrooveAccentPolicy.RoleGhostVelocity[role]` when defined.
- [ ] Apply `GroovePolicyDecision.VelocityBiasOverride` if provided.
- [ ] Unit tests verify:
  - [ ] lookups work
  - [ ] missing lookups fall back to sensible defaults
  - [ ] clamping works
  - [ ] policy override affects output deterministically

** Clarifying questions and answers for D2:
8. VelocityBiasOverride semantics: is it strictly additive (int to add) or can it be multiplicative? Which interpretation should we implement for Story D2?
Implement both, in a fixed, deterministic way: treat VelocityBiasOverride as (Multiplier, Additive) where multiplier is a double (default 1.0) and additive is an int (default 0). If your current GroovePolicyDecision only has one field, interpret it as additive for now unless it’s already modeled as a multiplier somewhere. Practically: Multiplier then Additive (see Q9).
9. Override application order: should the policy override apply before or after AccentBias is added?
Apply override after the strength lookup has been resolved into a base value. Order:
base = Typical + AccentBias (from the resolved rule for role+strength)
biased = round(base * Multiplier) + Additive
clamp (see Q13)
This makes override a true “final policy bias” and keeps the accent model internally consistent.
10. Ghost handling precedence: if RoleGhostVelocity[role] exists AND RoleStrengthVelocity[role] contains an entry for OnsetStrength.Ghost, which should be used?
RoleGhostVelocity[role] wins. Treat it as an explicit “ghost override” because it’s role-specialized and unambiguous.
So precedence for Ghost strength is:
If RoleGhostVelocity[role] exists → use it as the Typical velocity (with AccentBias treated as 0 unless you intentionally want ghost accents, which is usually wrong).
Else if RoleStrengthVelocity[role][Ghost] exists → use that rule.
Else fall back (see Q11).
They are not mutually exclusive; the ghost dictionary is the override.
11. Fallback defaults: when lookup fails (role missing or strength missing), what exact default should be used?
Do not throw. Use deterministic, layered fallback:
Fallback resolution order
If RoleStrengthVelocity contains the role but not the strength:
Try OnsetStrength.Offbeat for that role (it’s the safest “neutral” proxy).
If Offbeat missing, take the first available strength entry for that role using a fixed priority order:
Downbeat > Backbeat > Strong > Pickup > Offbeat (first one that exists).
If role missing entirely: use a global default VelocityRule:
Typical = 80, AccentBias = 0, Min = 1, Max = 127.
For Ghost specifically, if no ghost rule exists, map Ghost → Offbeat under the same fallback rules above.
12. Data types & rounding: are bias and AccentBias integers only? If fractional/multiplicative bias is supported later, how should rounding be handled?
For Story D2, keep the stored rule values as ints (Typical, AccentBias, Min, Max). Allow policy override multiplier as double.
Rounding rule (deterministic):
After multiplication, round to nearest int, midpoint away from zero.
Then apply additive int.
So: biasedInt = (int)Math.Round(base * multiplier, MidpointRounding.AwayFromZero) + add;
13. Min/Max enforcement scope: clamp to rule Min/Max OR also global MIDI 1..127?
Both. Deterministic clamp order:
Normalize the configured rule bounds into the legal MIDI domain (see Q17).
Clamp velocity to [ruleMin..ruleMax].
Clamp again to [1..127] (this second clamp becomes redundant if ruleMin/ruleMax were normalized, but it’s fine and defensive).
14. Diagnostics: what minimal diagnostic fields are required per-onset for velocity shaping (rule used, pre-clamp value, override applied)?
Minimal per-onset fields that are worth locking tests against:
Role
Strength (computed or overridden strength)
RuleSource (e.g., RoleStrength, RoleGhost, FallbackRoleOffbeat, FallbackRoleFirst, FallbackGlobal)
Typical
AccentBias
BaseVelocity (= Typical + AccentBias, or ghost typical)
PolicyMultiplier (default 1.0)
PolicyAdditive (default 0)
PreClampVelocity
RuleMin, RuleMax
FinalVelocity
That’s enough to explain every number without bloating the trace.
15. Interaction with existing onset velocity: if an onset already has a velocity set (from anchors), should velocity shaping override it always, or only for variation-added onsets?
Do not blindly override existing velocities. Use this rule:
If GrooveOnset.Velocity is already set → leave it unchanged.
If it’s null/unspecified → apply velocity shaping.
This preserves anchor intent and avoids “surprising” changes when anchors were authored with explicit dynamics. If you later want global reshaping, make it an explicit opt-in policy flag (not part of Story D2).
16. Concurrency / mutation: is it acceptable to update the Velocity in place, or should a new immutable output record be produced?
Produce a new immutable output record (copy-with). Your groove output contracts are immutable records; keep that consistent:
FinalOnsets = oldOnsets.Select(o => o with { Velocity = computed }).ToList();
No in-place mutation of shared lists—cleaner determinism, fewer bugs, and thread-safe by default.
17. If Min or Max are out of MIDI range, should they be normalized to [1..127] or treated as configuration error?
Normalize (clamp) them to [1..127] at runtime and surface a diagnostic/validation issue (non-throwing). Treat it as a configuration mistake, but don’t break generation.
Deterministic normalization rules:
ruleMin = Clamp(ruleMin, 1, 127)
ruleMax = Clamp(ruleMax, 1, 127)
If after clamp ruleMin > ruleMax, swap them (or set both to the clamped typical—pick one and keep it consistent; I recommend swap for simplest determinism + least surprise).

---

## Phase E — Timing & Feel (Pocket hooks for drummer model)

### Story E1 — Implement Feel Timing (Straight/Swing/Shuffle/Triplet)
**As a** generator  
**I want** feel timing applied deterministically  
**So that** groove pocket is correct per style/segment

**Acceptance Criteria:**
- [ ] Read feel from `SegmentGrooveProfile.OverrideFeel` or fallback to `GrooveSubdivisionPolicy.Feel`.
- [ ] Read swing amount from `OverrideSwingAmount01` or fallback to `SwingAmount01`.
- [ ] Implement behaviors:
  - [ ] `Straight`: no shift
  - [ ] `Swing`: shift offbeats later proportional to swing amount
  - [ ] `Shuffle`: map eighth offbeats toward triplet feel
  - [ ] `TripletFeel`: quantize eligible subdivisions to triplet grid (bounded)
- [ ] Ensure timing adjustments respect `AllowedSubdivisions` (no illegal slot creation).
- [ ] Unit tests for each feel mode with swing at 0, 0.5, 1.0.

---

### Story E2 — Implement Role Timing Feel + Bias + Clamp
**As a** generator  
**I want** per-role microtiming applied with clamping  
**So that** each role can sit ahead/behind and still stay safe

**Acceptance Criteria:**
- [ ] Read `GrooveTimingPolicy.RoleTimingFeel[role]`.
- [ ] Convert feel to base tick offset:
  - [ ] Ahead: negative
  - [ ] OnTop: zero
  - [ ] Behind: positive
  - [ ] LaidBack: larger positive
- [ ] Add `GrooveTimingPolicy.RoleTimingBiasTicks[role]`.
- [ ] Apply `GroovePolicyDecision.RoleTimingFeelOverride` / `RoleTimingBiasTicksOverride` when provided.
- [ ] Clamp by `GrooveTimingPolicy.MaxAbsTimingBiasTicks`.
- [ ] Unit tests verify clamping and override precedence.

---

## Phase F — Override Merge Policy (Segment overrides without surprises)

### Story F1 — Implement Override Merge Policy Enforcement
**As a** developer  
**I want** override merge behavior governed by policy  
**So that** segment overrides behave predictably

**Acceptance Criteria:**
- [ ] Implement `OverrideReplacesLists` for:
  - [ ] variation tags
  - [ ] protection tags
  - [ ] any per-segment list-based overrides used by groove
- [ ] Implement `OverrideCanRemoveProtectedOnsets`:
  - [ ] If false, protected never removed even under override
  - [ ] If true, allow removal logic (only where your pipeline supports removal)
- [ ] Implement `OverrideCanRelaxConstraints`:
  - [ ] If true, segment caps can increase MaxHits/MaxEvents, otherwise base caps apply
- [ ] Implement `OverrideCanChangeFeel`:
  - [ ] If false, ignore segment feel/swing overrides
- [ ] Unit tests cover all four booleans in both states with a small matrix.

---

## Phase G — Diagnostics & Explainability (required to evolve toward human level)

### Story G1 — Add Groove Decision Trace (Opt-in, No Behavior Change)
**As a** developer  
**I want** an explain trace of groove decisions  
**So that** debugging and future drummer tuning is practical

**Acceptance Criteria:**
- [ ] Add an opt-in diagnostics flag (config or parameter).
- [ ] When enabled, capture per bar + role:
  - [ ] enabled tags (after phrase/segment/policy)
  - [ ] candidate groups count, candidate count
  - [ ] filters applied and why (tag mismatch, never-add, grid invalid, etc.)
  - [ ] density target inputs and computed target count
  - [ ] selected candidates with weights/scores and RNG stream used
  - [ ] prune events and reasons (cap violated, tie-break, protected preserved)
  - [ ] final onset list summary
- [ ] When disabled, diagnostics collection is zero-cost-ish and produces identical output.
- [ ] Unit test: diagnostics on/off does not change generated notes.

---

### Story G2 — Add “Provenance” to Onsets
**As a** developer  
**I want** each onset to remember where it came from  
**So that** later systems (drummer model, ducking, analysis) can reason about it

**Acceptance Criteria:**
- [ ] Add `Provenance` fields to `GrooveOnset`:
  - [ ] `Source = Anchor | Variation`
  - [ ] `GroupId`, `CandidateId` (nullable)
  - [ ] `TagsSnapshot` (optional)
- [ ] Ensure provenance does not affect sorting or output determinism.
- [ ] Unit test: provenance fields are stable for identical runs.

---

## Phase H — Test Suite & Regression Locks (finish the groove system confidently)

### Story H1 — Full Groove Phase Unit Tests (Core)
**As a** developer  
**I want** unit tests for each groove phase  
**So that** behavior is verified and regressions are caught

**Acceptance Criteria:**
- [ ] Test: variation merge respects additive/replace/tag gating.
- [ ] Test: candidate filtering by enabled tags and empty-tag semantics.
- [ ] Test: deterministic weighted selection with stable tie-breaks.
- [ ] Test: density computation with multipliers and overrides.
- [ ] Test: caps enforcement (per bar, per beat, per role, per group, per candidate).
- [ ] Test: onset strength classification for 4/4 and 3/4.
- [ ] Test: velocity shaping lookups + clamp + ghost velocity.
- [ ] Test: feel timing for all `GrooveFeel` values + swing amounts.
- [ ] Test: role timing feel + bias + clamp + overrides.
- [ ] Test: merge policy booleans matrix.
- [ ] Test: diagnostics on/off produces identical events.

---

### Story H2 — End-to-End Groove Regression Snapshot (Golden Test)
**As a** developer  
**I want** a golden-file style regression test for a known groove preset  
**So that** improvements don’t accidentally break determinism or musical guardrails

**Acceptance Criteria:**
- [ ] Create a deterministic test song fixture (existing `CreateTestGrooveD1` is fine).
- [ ] Generate groove output and serialize a compact snapshot:
  - [ ] per bar/role: beats, velocities, timing offsets
- [ ] Assert snapshot matches expected output exactly.
- [ ] Provide a controlled way to intentionally update the snapshot when behavior changes by design.

---

# NEXT EPIC (Not part of Groove Completion): Pop Rock Human Drummer Model
**Goal:** Implement a Pop/Rock drummer policy provider and operator candidate source using the hooks added above.

**Entry Criteria (must be true after this file’s plan is done):**
- Groove system accepts `IGroovePolicyProvider` overrides.
- Groove system accepts `IGrooveCandidateSource` (catalog adapter exists).
- Diagnostics/provenance exist to measure and tune drummer behaviors.
- Deterministic RNG stream policy exists and is used everywhere.

---

## Story Path Summary (from now to “Groove System Done”)
1. A1 → A2 → A3  
2. B1 → B2 → B3 → B4  
3. C1 → C2 → C3  
4. D1 → D2  
5. E1 → E2  
6. F1  
7. G1 → G2  
8. H1 → H2  

---
