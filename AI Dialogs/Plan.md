
## Stage 7 — Section identity via energy/density (structured repetition)

**Why now:** “music” is repetition + contrast. To make later Stage 8 motifs and Stage 9 melody *land*, the accompaniment must already have intentional section-level identity and repeatable A/A’ variation.

Stage 7 formalizes *energy* (and the closely-related concept *tension*) into explicit, deterministic controls so the generator can deliberately shape emotion across time.

### Research-based framing (what Stage 7 should capture)

From songwriting/arranging perspectives, “energy” is not a constant force; it’s typically an **ebb-and-flow** shape (rising and falling) rather than a flat line. Energy is also **relative**: high-energy moments only feel high if there are lower-energy moments to contrast them.

Practical definitions that matter for generation:
- **Energy**: the perceived “spirit/vigor” of a song arising from momentum, tempo feel, intensity, strength, and arrangement choices.
- **Tension**: a listener’s perceived need for release created by expectation. Tension can increase without increasing energy (e.g., filtering down into a breakdown).
- **Energy Arc / Energy Map**: a time-series of intended energy levels across sections (and sometimes within sections), capturing the overall emotional “graph” of the song.

A key insight is that energy is multi-factor and often cycles across different building blocks:
- **Loudness/dynamics + instrumentation** (more/louder layers → higher energy)
- **Rhythmic activity** (more subdivision activity/groove/busy-ness → higher energy)
- **Melody register** (higher/higher-intensity melodic range → higher perceived energy)
- **Harmony** (distance from tonic & harmonic instability influences perceived intensity/tension)
- **Lyrics** (later stage, but Stage 7 must reserve knobs so lyric intensity can drive arrangement later)

Stage 7 must provide a single framework that lets later stages “compose emotion” at the arrangement level.

---

### Stage 7 goal

Introduce a shared, deterministic “section identity + energy shaping” framework that:
- makes Verse vs Chorus (and Pre-chorus/Bridge) feel immediately different even with the same chords
- supports **ebb/flow** (energy up/down) across the whole song
- supports an explicit **Energy Arc** (“energy map”) that can be reused/modified per song
- supports **structured repetition** (A / A’ / B) via bounded transforms
- provides future contracts for Stage 8 motifs and Stage 9 melody/lyrics to request arrangement behavior

**Design principles (must hold):**
- **Determinism:** given `(seed, song structure, section index/type)`, output is repeatable. RNG only as a deterministic tie-break among valid options.
- **Energy is relative:** changes are interpreted against baseline and nearby sections.
- **Separation of concerns:** Stage 7 defines *intent*; roles apply intent.
- **Safety rails:** energy changes must not violate musical constraints (muddy low register, lead space ceiling, drum density caps, etc.).

---

### Story 7.1 — `EnergyArc` + section-level energy targets - Completed

**Intent:** make the “ebb/flow” explicit and controllable: every section has an intended energy target, not just an implicit section type.

**Acceptance criteria:**
- Create `EnergyArc` (or `SongEnergyArc`) representing the song-level energy plan.
- It must support:
  - **per-section** targets (Intro/Verse/PreChorus/Chorus/Bridge/Outro)
  - optional **within-section** subtargets (phrase-level: start/middle/peak/cadence)
- Provide a small library of default arcs keyed by common forms (e.g., Pop, Rock, EDM-ish) that the generator selects deterministically.
- Energy scale guidance:
  - store as `double` in `[0..1]` *or* an `int` scale such as `1..9` (common analysis method). Pick one and standardize.

**Notes/guidance:**
- Keep arc selection deterministic by `(seed, style/groove, song form id)`.
- The arc is a top-level “macro-energy” target; it will later inform tension planning and motif placement.

---

### Story 7.2 — `SectionEnergyProfile` (multi-factor, per-role), derived from the arc - Completed

**Intent:** convert energy targets into actionable per-role controls that map to real musical levers (dynamics, density, register, orchestration, rhythmic activity).

**Acceptance criteria:**
- Create/update `SectionEnergyProfile` as a small strongly-typed model computed per `SongSection`.
- Inputs must include:
  - `EnergyArc` target for this section
  - `SectionType` + `SectionIndex`
  - style/groove identity
- Outputs must include (minimum viable set):
  - A `Global` block (defaults) with:
    - `double Energy` (target)
    - `double TensionTarget` (optional but recommended)
    - `double ContrastBias` (how much we allow the section to differ from previous)
  - A per-role `RoleProfile` for `Bass`, `Comp`, `Keys`, `Pads`, `Drums` (and later `Lead`):
    - `double DensityMultiplier`
    - `int VelocityBias`
    - `int RegisterLiftSemitones`
    - `double BusyProbability`
  - An `Orchestration` block:
    - which roles are present/featured (e.g., pads absent in Verse 1, present in Chorus)
    - cymbal language preference hints (ride vs hat) feeding Stage 6

**Guidance (grounded in energy factors):**
- Don’t make intensity only “velocity.” High energy usually needs *multiple* levers:
  - more layers (orchestration)
  - higher rhythmic activity
  - register lift (melodic/voicing center up)
  - selective density increases (not everywhere)

---

### Story 7.3 — Energy application pass (wire into all role renderers) - Completed

**Intent:** ensure the energy profile affects audible output across all roles.

**Acceptance criteria:**
- Apply `SectionEnergyProfile` to:
  - **Drums:** connect to existing Stage 6 knobs; add “micro-tension” support points (see Story 7.5).
  - **Bass:** pattern selection and embellishment rate respond to density/busy; velocity bias applied; register lift clamped.
  - **Comp:** rhythm pattern choice and slot selection respond to density/busy; voicing center responds to register; velocity applied.
  - **Keys/Pads:** chord realization density and sustain respond to energy; register center responds to lift; velocity applied.
- Implement role-specific **guardrails** (must not be violated):
  - Bass range limits.
  - Lead-space ceiling for comp/keys.
  - Pads avoid the future vocal band (placeholder constraints now).
  - Style-safe drum density caps.

---

### Story 7.4 — "Energy is relative": section-to-section constraints

**Intent:** enforce core songwriting/arranging energy heuristics that listeners expect.

Story 7.4 is decomposed into focused sub-stories to ensure proper implementation, testing, and maintainability:

---

#### Story 7.4.1 — Energy constraint model and rules framework - COMPLETED

**Intent:** Create the constraint model and rule engine infrastructure without yet applying it to generation.

**Acceptance criteria:**
- Create `EnergyConstraintRule` abstraction representing a single heuristic (e.g., "Verse N+1 should be ≥ Verse N")
- Create `EnergyConstraintPolicy` that groups rules by style/genre
- Implement rule evaluation framework:
  - Input: proposed energy value for section at index `i`
  - Context: section type, index, and energies of related sections (previous same-type, previous any-type, next known sections)
  - Output: adjusted energy value (clamped/modified) + optional diagnostic message
- Rules must be **deterministic**: same input → same output
- Rules must be **composable**: multiple rules can apply to same section

**Specific rules to implement (initial set):**
1. `SameTypeSectionsMonotonicRule`: Section N+1 of same type should have energy ≥ Section N (e.g., Verse 2 ≥ Verse 1)
2. `PostChorusDropRule`: First section after a chorus should typically drop energy (unless arc explicitly overrides)
3. `FinalChorusPeakRule`: Last chorus should be at or near the song's peak energy
4. `BridgeContrastRule`: Bridge can either exceed prior chorus OR intentionally drop for contrast (both are valid)

**Design notes:**
- Rules should be able to return "no opinion" (rule doesn't apply to this section)
- Rules should compute suggested adjustments, not hard overrides (allows arc to have final say with explicit overrides)
- Each rule should have a configurable "strength" or priority for conflict resolution

**Implementation notes:**
- Created `EnergyConstraintContext` providing all necessary context for rule evaluation
- Created `EnergyConstraintResult` representing rule evaluation outcomes
- Created abstract `EnergyConstraintRule` base class with deterministic evaluation contract
- Implemented `EnergyConstraintPolicy` with strength-weighted blending for conflict resolution
- Implemented all four specified rules in `Song\Energy\Rules\` namespace
- Created comprehensive unit tests in `EnergyConstraintTests` covering all rules and edge cases
- All tests pass and verify determinism

---

#### Story 7.4.2 — Constraint application in EnergyArc resolution - COMPLETED

**Intent:** Wire constraint rules into the energy resolution pipeline so they actually affect generation.

**Acceptance criteria:**
- Modify `EnergyArc.GetTargetForSection` or `EnergyProfileBuilder.BuildProfile` to apply constraints
- Constraints are applied **after** template lookup but **before** profile construction
- Implementation must maintain determinism:
  - Rule application order is deterministic
  - Conflict resolution (when multiple rules suggest different adjustments) is deterministic
- Add optional `EnergyConstraintPolicy` parameter to energy arc creation
- Default policy should match common pop/rock expectations (safe defaults)

**Processing flow:**
1. Get base energy from template (existing behavior)
2. Apply constraint rules in deterministic order
3. Clamp to valid range [0..1]
4. Proceed with profile building (existing behavior)

**Edge cases to handle:**
- When arc has explicit override for section, constraints should respect it (arc > constraints)
- When multiple rules conflict, use deterministic priority/averaging
- When rules suggest impossible constraints (e.g., V2 must be > V1, but V1 is already 0.95), clamp gracefully

**Implementation notes:**
- Created `EnergyConstraintPolicyLibrary` with predefined policies:
  - Pop/Rock (default): `SameTypeSectionsMonotonicRule` strength=1.0, `PostChorusDropRule` strength=1.2, `FinalChorusPeakRule` strength=1.5, `BridgeContrastRule` strength=0.8
  - Rock: Stronger progression (strength=1.3, minIncrement=0.05), less strict post-chorus drop, strong final peak (strength=1.8, minEnergy=0.85)
  - Jazz: Weak progression (strength=0.3, minIncrement=0.0), no `PostChorusDropRule`, relaxed final peak (strength=0.5, minEnergy=0.70)
  - EDM: `PostChorusDropRule` completely disabled (not included), very strong final peak (strength=2.0, minEnergy=0.90)
  - Minimal: Only `FinalChorusPeakRule` (strength=1.0)
  - Empty: no constraints (for testing)
- Modified `EnergyArc.Create` to accept optional `EnergyConstraintPolicy` parameter
  - Defaults to policy selected by `EnergyConstraintPolicyLibrary.GetPolicyForGroove` based on groove name
- Implemented pre-computation of constrained energies in `EnergyArc` constructor:
  - Processes all sections in order, building context as it goes
  - Each section gets constrained energy by applying policy rules
  - Results cached in dictionary for O(1) lookup
- Modified `EnergyArc.GetTargetForSection` to return cached constrained values
- Added `GetConstraintDiagnostics` method for debugging/explainability
- Updated `EnergySectionTarget` to include required `SectionType` and `SectionIndex` properties
- Updated all existing callers in `EnergyArcTemplate`, `EnergyArcLibrary`, and test files
- Created comprehensive integration tests in `EnergyConstraintApplicationTests`:
  - Basic constraint application and flow-through
  - Determinism verification
  - Various song structures (standard pop, rock anthem, minimal, unusual)
  - Style-specific policy behavior
  - All tests pass
- Energy constraints now affect all generation through `EnergyProfileBuilder.BuildProfile`

---

#### Story 7.4.3 — Style-specific constraint policies - COMPLETED

**Intent:** Make constraints parameterizable per style so different genres have different energy expectations.

**Acceptance criteria:**
- Create `EnergyConstraintPolicyLibrary` with predefined policies for:
  - **Pop**: moderate verse progression, strong chorus contrast, final chorus peak
  - **Rock**: stronger verse progression, sustained high energy allowed
  - **Jazz**: relaxed constraints, allow energy drops/rises more freely
  - **EDM**: strong build emphasis, dramatic drops allowed
- Policy selection is deterministic based on groove name or explicit style parameter
- Each policy enables/disables specific rules and configures rule strengths

**Example policy differences:**
- Pop: `SameTypeSectionsMonotonicRule` strength = High (1.0)
- Jazz: `SameTypeSectionsMonotonicRule` strength = Low (0.3, allow more freedom)
- EDM: `PostChorusDropRule` disabled (EDM often goes straight into build after drop)

**Implementation notes:**
- Story was implemented as part of Story 7.4.2
- `EnergyConstraintPolicyLibrary` exists in `Song\Energy\EnergyConstraintPolicyLibrary.cs` with all required policies:
  - **PopRock** (default): `SameTypeSectionsMonotonicRule` strength=1.0, `PostChorusDropRule` strength=1.2, `FinalChorusPeakRule` strength=1.5, `BridgeContrastRule` strength=0.8
  - **Rock**: Stronger progression (strength=1.3, minIncrement=0.05), less strict post-chorus drop, strong final peak (strength=1.8, minEnergy=0.85)
  - **Jazz**: Weak progression (strength=0.3, minIncrement=0.0), no `PostChorusDropRule`, relaxed final peak (strength=0.5, minEnergy=0.70)
  - **EDM**: `PostChorusDropRule` completely disabled (not included), very strong final peak (strength=2.0, minEnergy=0.90)
  - **Minimal**: Only `FinalChorusPeakRule` (strength=1.0)
  - **Empty/None**: No constraints (for testing)
- Policy selection methods:
  - `GetPolicyForGroove(grooveName)`: Deterministic pattern matching on groove name (jazz/bossa/latin → Jazz, edm/house/techno → EDM, rock/punk/metal → Rock, default → PopRock)
  - `GetDefaultPolicy()`: Returns PopRock
  - `GetAllPolicies()`: Dictionary of all named policies for diagnostics
- Each policy configures rule strengths and enables/disables specific rules as appropriate for style
- Integration tests in `EnergyConstraintApplicationTests.TestStyleSpecificPolicies()` verify different policies produce different energy progressions
- All tests pass and verify determinism

---

#### Story 7.4.4 — Constraint diagnostics and explainability - COMPLETED

**Intent:** Make energy constraint decisions visible for debugging and tuning.

**Acceptance criteria:**
- Add optional diagnostics output showing:
  - Which rules were evaluated for each section
  - Which rules made adjustments (and why)
  - Original template energy vs final constrained energy
  - Any conflicts detected and how they were resolved
- Diagnostics must not affect generation (determinism maintained)
- Add to existing energy diagnostics from Story 7.9 (when implemented)

**Output format (example):**
```
Section: Verse 2 (index=1)
  Template energy: 0.45
  Rules applied:
    - SameTypeSectionsMonotonicRule: adjusted to 0.50 (Verse 1 was 0.48)
    - PostChorusDropRule: no opinion (previous section was Verse)
  Final energy: 0.50
```

**Implementation notes:**
- Created `EnergyConstraintDiagnostics` in `Song\Energy\EnergyConstraintDiagnostics.cs` with multiple report formats:
  - **`GenerateFullReport(arc, includeUnchangedSections)`**: Complete section-by-section analysis showing:
    - Arc template name, groove, policy name and enabled status
    - Active rules with their strengths
    - For each section: template energy, final energy, change amount/percentage, rules applied with diagnostics
  - **`GenerateSummaryReport(arc)`**: High-level overview showing:
    - Energy progression by section type (with monotonic validation)
    - Energy peak location
    - Count of sections with constraint adjustments
  - **`GenerateCompactReport(arc)`**: One-line-per-section format suitable for logging:
    - Shows template → final energy with visual marker (*) for adjusted sections
  - **`CompareArcs(arc1, arc2, labels)`**: Side-by-side comparison of two arcs:
    - Shows energy values and deltas for each section
    - Useful for comparing different policies or templates
  - **`GenerateEnergyChart(arc, height)`**: ASCII chart visualization:
    - Visual bar chart showing energy flow across sections
    - Section type labels on X-axis
- Leverages existing `EnergyArc.GetConstraintDiagnostics(absoluteIndex)` method implemented in Story 7.4.2
- All diagnostic methods are static and do not modify arc state
- Created comprehensive tests in `EnergyConstraintDiagnosticsTests`:
  - `TestFullReportGeneration()`: Verifies full report contains all required information
  - `TestSummaryReportGeneration()`: Verifies summary format and content
  - `TestCompactReportGeneration()`: Verifies compact one-line-per-section format
  - `TestArcComparison()`: Verifies side-by-side comparison works correctly
  - `TestEnergyChartGeneration()`: Verifies ASCII chart rendering
  - `TestDiagnosticsDoNotAffectGeneration()`: **Critical test** verifying diagnostics don't change energy values
  - `TestDiagnosticsDeterminism()`: Verifies report output is deterministic for same inputs
- All tests pass and verify determinism
- Diagnostics are completely non-invasive and maintain determinism

**Usage examples:**
```csharp
// Full detailed report
var arc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);
string report = EnergyConstraintDiagnostics.GenerateFullReport(arc);
Console.WriteLine(report);

// Summary for quick overview
string summary = EnergyConstraintDiagnostics.GenerateSummaryReport(arc);
Console.WriteLine(summary);

// Compare policies
var popArc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);
var jazzArc = EnergyArc.Create(sectionTrack, "JazzGroove", seed: 42);
string comparison = EnergyConstraintDiagnostics.CompareArcs(popArc, jazzArc, "Pop", "Jazz");
Console.WriteLine(comparison);

// Visual chart
string chart = EnergyConstraintDiagnostics.GenerateEnergyChart(arc);
Console.WriteLine(chart);

#### Story 7.4.5 — Integration testing and validation - COMPLETED

**Intent:** Ensure constraints produce musically sensible results across varied song structures.

**Acceptance criteria:**
- Create test suite with various song structures:
  - Standard pop (Intro-V-C-V-C-Bridge-C-Outro)
  - Rock anthem (V-V-C-V-C-Solo-C-C)
  - Minimal structure (V-C-V-C)
  - Unusual structure (Intro-C-V-Bridge-V-C-C)
- Verify for each structure:
  - Constraints produce valid energy values [0..1]
  - Musical heuristics are honored (verse progression, chorus contrast, etc.)
  - Final chorus is at or near peak
  - Results are deterministic (same seed → same output)
- Test with different style policies to ensure style-specific behavior

---

# Stage 7 Plan Revision (Energy/Tension/Section Identity) — Post-Review

Remaining stage 7 stories are updated after reviewed against:
- `AI Dialogs\PlanV3.md` (Stages 8–16 depend on Stage 7/8 intent & constraints)
- `AI Dialogs\musical_energy_compilation.md` (energy research + implementation levers)

Constraints for this revision:

The main goal of the revision is to ensure Stage 7 provides the *right contracts* for later stages:
- bar/phrase level queryability (Stage 8 phrase maps, Stage 10 melody timing, Stage 12 performance rendering)
- energy as multi-factor intent (not velocity-only)
- tension distinct from energy (anticipation vs release)
- deterministic cross-section rules (“energy is relative”)
- explainability/diagnostics that do not affect generation

---

## Key alignment decisions (from Stage 8–16 and energy research)

### DECISION A — Stage 7 remains macro intent; Stage 8 becomes bar-level phrase map + cross-role budgets
Stage 7 should not overreach into detailed per-bar phrase semantics. However, **Stage 7 must expose enough hooks and stable query contracts** so Stage 8 can deterministically build:
- `PhraseMap`
- bar-level `SectionArc` deltas
- cross-role `RoleDensityBudget` logic

### DECISION B — Keep Energy as scalar target, but reserve a “multi-lever” structure
Research indicates energy is multi-factor (density, register, harmonic rhythm, instrumentation, etc.). Stage 7 already has a scalar energy target; that’s fine so long as Stage 7 also derives **multi-factor per-role controls** and is prepared to expand.

### DECISION C — Add a deterministic “release vs build” semantic
Later stages (motif placement, melody shaping, arrangement ducking) benefit from a stable, deterministic concept of *section transition feel*: `Build`, `Release`, `Sustain`, `Drop`.
This can remain a lightweight enum computed as a **derived hint**, not a new large system.

### DECISION D — Style controls micro-tension phrase ramps (new)
Stage 7 micro tension default behavior must be style-tunable so it does not force rising tension in genres where it is undesirable.
Introduce a deterministic knob: `MicroTensionPhraseRampIntensity` in `[0..1]` (0=flat micro map, 1=strong phrase ramps).
Selection is deterministic from groove/style identity (and optionally section type/index if needed), without affecting EnergyArc constraints.


# Story 7.5 : Tension planning hooks (macro vs micro)

Introduce a **tension** concept distinct from energy, computed deterministically at the section/phrase level, and exposed via stable “hooks” that later stages (Stage 8 motifs, Stage 9 melody/lyrics, and future arranging passes) can query.

Guiding constraints (carry-over from Stage 7):
- **Deterministic:** same `(seed, song form/sections, groove/style)` => same targets and decisions.
- **Separation of concerns:** Stage 7 computes intent; renderers consume it.
- **Non-invasive:** tension hooks bias existing systems; they don’t require rewriting role logic.
- **Future-proof:** Stage 8/9 must be able to query tension/phrase positions to drive motifs/melody and arrangement ducking.

---

## Story 7.5.1 — Define the tension model and public contracts - COMPLETED

**Intent:** Introduce a strongly-typed tension representation that later systems can query without knowing how it was computed.

**Acceptance criteria:**
- Add a new model (pick one approach and standardize):
  - **Option A (simpler):** add `double TensionTarget` to `SectionEnergyProfile.Global`.
  - **Option B (more explicit):** create `SectionTensionProfile` and reference it from `SectionEnergyProfile` (or from `SongSectionContext`).
- The model must support:
  - `double MacroTension` in `[0..1]` (section-level intent)
  - `double MicroTension` in `[0..1]` as a **default bias** (phrase/bar-level intent can override)
- Add a small enum describing *why* tension exists to support later explainability and policy tuning:
  - `TensionDriver` (flags or enum): e.g., `PreChorusBuild`, `Breakdown`, `Drop`, `Cadence`, `BridgeContrast`, `None`
- Add a stable query API that renderers can call without needing planner internals:
  - `GetMacroTension(sectionIndex)`
  - `GetMicroTension(sectionIndex, barIndexWithinSection)` (or phrase position)
- Ensure models are immutable (prefer `record` / `record struct`) and live in the existing energy/tension namespace conventions.

**Implementation notes:**
- Chose **Option B**: Created explicit `SectionTensionProfile` for clear separation between energy and tension concepts
- Created 5 core model files in `Song\Energy\`:
  - `TensionDriver.cs` - Flags enum with 9 tension driver types
  - `SectionTensionProfile.cs` - Section-level tension model with factory methods and clamping
  - `MicroTensionMap.cs` - Per-bar tension map with phrase position flags
  - `ITensionQuery.cs` - Query interface and `TensionContext` helper record
  - `NeutralTensionQuery.cs` - Default implementation returning zero tension (placeholder for Story 7.5.2)
- Created comprehensive test file `TensionModelTests.cs` with 64 test methods - all pass ✓
- All models are immutable records
- All tension values automatically clamped to [0..1]
- Query API is deterministic and thread-safe
- See `AI Dialogs\Story_7_5_1_Implementation_Summary.md` for full details

---

### Story 7.5.2 — Compute section-level macro tension targets (deterministic)  (Completed)

**intent:** ensure macro tension reflects *anticipation/release* distinct from energy and yields a stable “transition hint” used 
in Stages 8–16. produce a musically plausible section-level tension plan that is distinct from energy so later stages can create 
anticipation/release.

**Acceptance criteria:**
- Implement deterministic macro-tension per section using only:
  - `SectionType`, `SectionIndex`
  - local song structure neighborhood (previous/next sections)
  - groove/style identity
  - constrained energy target from `EnergyArc`
- Macro tension must not trivially mirror energy; enforce at least one of these behaviors where the form supports it:
  - `PreChorus` macro tension > preceding `Verse`
  - `Chorus` tension often **drops** vs `PreChorus` (release)
  - `Bridge` is either higher tension or contrasting tension (style driven)
  - `Outro` tension trends down
  - if section precedes a higher-energy section, allow elevated tension (anticipation)
- Clamp all outputs to `[0..1]`.
- Output must include explainability:
  - a `TensionDriver` flag set describing why tension took its value.
- Provide a derived, deterministic `SectionTransitionHint` per section boundary:
  - values: `Build`, `Release`, `Sustain`, `Drop`
  - derived from `(energyDelta, tensionDelta, section types)`; must be stable.
- Add unit tests verifying:
  - determinism
  - ranges `[0..1]`
  - at least one non-trivial shape across a common pop form (Intro-V-PreC-C-V-PreC-C-Bridge-C-Outro)
  - driver flag coverage

---
 
### Story 7.5.3 — Derive micro tension map per section (COMPLETED)

**intent:Stage 8/10 require phrase-aware bar-level hooks. Stage 7 should supply a default bar-level map even before Stage 8 phrase mapping exists. 
provide within-section tension shaping so renderers can bias micro-events (fills, dropouts, impacts) at phrase boundaries and peaks.

**Acceptance criteria:**
- Define deterministic `MicroTensionMap` per section keyed by bar index.
- Must support two modes:
  1. **Phrase-aware mode** (when phrase position information is available later)
  2. **Fallback mode** (infer phrase segmentation deterministically: default 4-bar phrases, handle remainders)
- MVP shape constraints:
  - rising micro tension toward the end of each phrase
  - "cadence window" at phrase end (last bar or last N beats) flagged for pull/impact decisions
  - optional "peak window" near the phrase peak (if section length supports it)
- Micro tension is derived from:
  - macro tension
  - phrase position class (`Start`, `Middle`, `Peak`, `Cadence`) when available, otherwise fallback inference
- Output includes:
  - per-bar micro tension value
  - per-bar flags: `IsPhraseEnd`, `IsSectionEnd`, `IsSectionStart`
- Add unit tests verifying:
  - correct map length for sections
  - determinism by seed
  - monotonic-ish rise into cadence for basic phrase lengths (allow small plateaus)

**Implementation notes:**
- Added `MicroTensionMap.Build(barCount, macroTension, microDefault, phraseLength?, seed)` method
- Supports both phrase-aware mode (explicit phraseLength) and fallback mode (infers: 4 bars typical, 2 for 4-bar sections)
- Produces rising tension within phrases using linear ramp (phraseFactor 0..1) with multiplier up to 1.4
- Derives micro baseline from blend of microDefault and macroTension (lerp approach)
- Applies deterministic tiny jitter (±0.01) when seed != 0 to avoid exact repeats
- All tension values clamped to [0..1]
- Flags set correctly: IsPhraseEnd at phrase boundaries + last bar, IsSectionStart/End at section boundaries
- Updated `DeterministicTensionQuery.ComputeProfiles()` to use Build() with per-section seed derivation
- Created comprehensive test file `MicroTensionMapTests.cs` with 24 test methods covering:
  - Basic functionality and edge cases
  - Determinism by seed
  - Fallback mode phrase inference (4-bar default, 2-bar for 4-bar sections, irregular handling)
  - Rising tension shape within phrases
  - Influence of macro tension and micro default on baseline
  - Phrase end and section boundary flags
  - Map length matching bar count
  - Valid tension range [0..1]
  - Jitter application (present when seed != 0, absent when seed = 0)
  - Integration with macro tension
- All tests pass and verify determinism, correct map length, monotonic-ish rise
- NOTE: When implementing style policies, apply `MicroTensionPhraseRampIntensity` so ramp strength is genre-appropriate (0=flat, 1=strong ramps).

---

### Story 7.5.4 — Tension hooks derived from macro+micro (completed)

**intent :align hooks with later stage needs (drums + non-drums + ducking) while keeping them bounded and safe.
translate tension targets into bounded parameters that existing role generators (especially Stage 6 drums) can consume without new complex model dependencies.

**Acceptance criteria:**
- Create `TensionHooks` returned for `(sectionIndex, barIndexWithinSection)` with bounded fields:
  - `double PullProbabilityBias` (phrase-end pull/fill bias)
  - `double ImpactProbabilityBias` (section-start impact bias)
  - `int VelocityAccentBias`
  - `double DensityThinningBias` (supports “breakdown tension” distinct from energy)
  - `double VariationIntensityBias` (small; later used by motif/melody variation operators)
- Hooks must be derived deterministically from:
  - macro tension
  - micro tension
  - transition hint (`Build/Release/Sustain/Drop`)
  - section energy (for safety clamping)
  - `MicroTensionPhraseRampIntensity` (style knob; ramps may be reduced/disabled per genre)
- Hooks must be clamped to safe ranges.
- Hooks must be **style-safe** defaults:
  - never exceed existing drum density caps
  - never force kick removal; only bias optional events
- Guardrails:
  - must not exceed density caps
  - must not force removal of protected groove anchors

---

### Story 7.5.5 — Wire tension hooks into Stage 6 drums (COMPLETED)

**Intent:prove the tension framework affects audible output in a controlled way without destabilizing the groove.

**Acceptance criteria (complete, from original since current text is abbreviated):**
- Use tension hooks to bias at least two existing drum behaviors (deterministically):
  1. **Phrase-end pull events:** increase probability of a small fill, flam, or hat change (style-gated) when micro tension is high.
  2. **Phrase-end dropout option:** allow a kick dropout or hat simplification on the last 1–2 subdivisions of a phrase when tension is high and energy is not maximal.
- Guarantee groove protection:
  - downbeats/backbeats remain protected unless already optional in the preset
  - no event moves outside bar boundaries
- Add tests that validate:
  - determinism of event selection with identical inputs
  - that tension biases increase the rate of eligible fill/dropout events vs. same scenario with tension forced to 0

---

### Story 7.5.6 — Wire tension hooks into non-drum roles (COMPLETED)

**intent :Stage 12 performance rendering and Stage 10 melody ducking benefit from early, strictly-bounded integration.
Make tension meaningful beyond drums while keeping implementation minimal and safe.

**Acceptance criteria:**
- Apply tension hooks in at least one of:
  - comp/keys/pads: small velocity bias (accent bias) at phrase peaks/ends
  - bass: pickup/approach bias only on valid slots and only when energy allows
- Additional valid constraint
  - bass pickup/approach probability increases *only when groove has a valid slot* (policy-gated)
- Ensure lead-space and register guardrails always win.
- No changes that break existing role guardrails (register limits, lead-space ceiling, density caps).
- Determinism preserved.
- Tests:
  - determinism
  - guardrails preserved

**Implementation notes:**
- Updated `GuitarTrackGenerator`, `KeysTrackGenerator`, `BassTrackGenerator` to accept tension query parameters
- Applied `VelocityAccentBias` to comp and keys velocity calculations (additive to energy bias, clamped 1-127)
- Applied `PullProbabilityBias` to bass approach note probability (additive to busy probability, clamped 0-1)
- Updated `Generator.cs` to pass tension query and ramp intensity to all role generators
- Created integration tests verifying:
  - Comp: velocity increase at phrase ends, guardrails enforced, determinism
  - Keys: velocity increase at phrase ends, guardrails enforced, determinism
  - Bass: approach probability increase, policy gate respected, determinism, bounded output
- All tests pass and build successful
- See `AI Dialogs\Story_7_5_6_Implementation_Summary.md` for full details

---

### Story 7.5.7 — Tension diagnostics (COMPLETED)

**Intent :make tension decisions debuggable and tunable, aligned with Story 7.9 direction.

**Acceptance criteria (complete, from original since current text is abbreviated):**
- Add an opt-in diagnostic report similar to energy diagnostics:
  - section macro tension values
  - per-section micro tension summary (min/max/avg)
  - key flags (phrase ends, section-end)
  - which heuristics/drivers contributed (via `TensionDriver`)
- Diagnostics must:
  - not affect generation
  - be deterministic
- Add a unit test verifying diagnostics do not change generated tension values.

---

### Story 7.5.8 — Stage 8/9 integration contract: tension queries (completed)

**intent:Stage 8 requires a unified intent query; Stage 10/14 require versionable query contracts.
Ensure future Stage 8 motif placement and Stage 9 melody/lyrics can request tension-aware arrangement behavior without refactoring Stage 7 again.

**Acceptance criteria:**
- Expand the query API to include:
  - `GetTensionContext(sectionIndex, barIndexWithinSection)` returning:
    - macro tension
    - micro tension
    - tension drivers
    - section transition hint (`Build/Release/Sustain/Drop`)
- Ensure implementations remain immutable and thread-safe.
- Ensure the API can support :
  - motif placement decisions (prefer high-energy + low tension release moments, or use high tension for anticipatory motifs)
  - lyric-driven ducking later (when vocals present, reduce accompaniment density especially when tension is low and release is desired)

---

# Story 7.6 — Structured repetition engine  (COMPLETED)

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

**Implementation notes:**
- Created `BaseReferenceSelectorRules.cs` with deterministic selection logic
- Implemented 3 core rules:
  1. First occurrence of any section type → null (A tag)
  2. Bridge/Solo can be contrasting → deterministic via seed/groove hash (B tag)
  3. Repeated sections → reference earliest prior instance (A' tag)
- Hash-based tie-breaking: `HashCode.Combine(seed, grooveName, sectionIndex, sectionType)`
- Created `BaseReferenceSelectorRulesTests.cs` with 23 test methods
- All tests pass and verify determinism
- See `AI Dialogs/Story_7_6_2_Implementation_Summary.md` and `AI Dialogs/Story_7_6_2_Final_Verification.md` for details

## 7.6.3 — Variation intensity + per-role deltas (bounded planner) - COMPLETED

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

**Implementation notes (COMPLETED):**
- Created `Song\Energy\SectionVariationPlanner.cs` with deterministic `ComputePlans()` method
- Variation intensity formula: base 0.15, practical max 0.6, multi-factor (energy/tension/transition/section type/repeat distance)
- Per-role deltas: magnitudes scaled by intensity, direction bias from transition hints, conservative ranges
- Hash-based deterministic role selection: higher intensity → more roles vary
- Created `Song\Energy\SectionVariationPlannerTests.cs` with 14 comprehensive test methods
- All tests pass and verify determinism, bounds, and A/A'/B variation patterns
- Build successful
- See `AI Dialogs/Story_7_6_3_Implementation_Summary.md`, `AI Dialogs/Story_7_6_3_Acceptance_Criteria_Verification.md`, and `AI Dialogs/Story_7_6_3_Final_Report.md` for complete details

## 7.6.4 — Query surface + generator wiring (completed)

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

## 7.6.5 — Role-parameter application adapters + minimal diagnostics  (completed)

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

# Stage 7.7 — Audible Part Intelligence Pass (Comp + Keys)  (Completed)
**Goal:** Make Verse vs Chorus vs PreChorus differences **clearly audible** before building motif/melody systems.  
**Rationale:** Stage 7 computes energy/tension intent, but current render logic barely uses it audibly. This pass ensures infrastructure produces real musical contrast.

---

## Current Problems (Why It Sounds Same-y)

### Problem 1: Comp (GuitarTrackGenerator) rhythm selection is not audibly varied
**Location:** `Generator\Guitar\GuitarTrackGenerator.cs`, method `ApplyDensityToPattern()` (lines 184-209)  
**Issue:** Always takes "first N" indices from `pattern.IncludedOnsetIndices`, so:
- Different seeds don't change which slots are chosen
- Different sections use the same "most important" hits
- Density changes sound like "same pattern, slightly busier" not "different comp behavior"

### Problem 2: Register lift rounded to octaves, then often undone by guardrails
**Location:** `Generator\Guitar\GuitarTrackGenerator.cs`, method `ApplyRegisterWithGuardrail()` (lines 217-260)  
**Issue:** 
- `RegisterLiftSemitones` is rounded to nearest octave (line 229: `int octaveShift = (int)Math.Round(registerLiftSemitones / 12.0) * 12`)
- Values like +2, +4, +7 become 0
- Lead-space ceiling (line 237-244) can push lifted voicings back down
- Net result: register lift rarely produces audible change

### Problem 3: Keys uses pads onsets directly with no rhythm variation
**Location:** `Generator\Keys\KeysTrackGenerator.cs` (lines 41-46)  
**Issue:** Keys just uses `grooveEvent.AnchorLayer.PadsOnsets` as-is. No pattern library, no density-based filtering, no behavioral modes. Every bar in a section sounds rhythmically identical.

### Problem 4: Duration is always "slot duration" — no sustain/chop variation
**Location:** Both generators use `slot.DurationTicks` directly (GuitarTrackGenerator line 157, KeysTrackGenerator line 171)  
**Issue:** Energy/tension never affects duration. High energy should mean shorter durations (more re-attacks), low energy should mean longer sustains.

### Problem 5: Seed doesn't meaningfully affect rhythm choices
**Current state:** Seed affects velocity jitter and some pitch randomization, but not which slots are played or how duration/behavior changes. Different seeds ? nearly identical output.

---

## Stories

### Story 7.7.1 — Create `CompBehavior` enum and deterministic selector (Completed)

**Intent:** Define audibly-distinct comp behaviors that energy/tension/section can choose from.

**New file:** `Generator\Guitar\CompBehavior.cs`

**Acceptance criteria:**
// AI: purpose=Deterministic selection of comp playing behavior based on energy/tension/section.
// AI: invariants=Selection is deterministic by (sectionType, absoluteSectionIndex, barIndex, energy, busyProbability, seed).
// AI: change=Add new behaviors by extending enum and updating SelectBehavior logic.

**Tests required:**
- Determinism: same inputs ? same behavior
- Different sections ? different behaviors (at least 2 distinct behaviors across typical pop form)
- Seed affects variation within section

---

### Story 7.7.2 — Create `CompBehaviorRealizer` to apply behavior to onset selection + duration  (Completed)

**Intent:** Convert behavior + available onsets into actual onset selection and duration shaping.

**New file:** `Generator\Guitar\CompBehaviorRealizer.cs`

**Acceptance criteria:**
// AI: purpose=Applies CompBehavior to onset selection and duration shaping.
// AI: invariants=Output onsets are valid subset of input; durations bounded by slot constraints; deterministic.
// AI: deps=Consumes CompBehavior, CompRhythmPattern, compOnsets; produces filtered onsets and duration multiplier.

**Tests required:**
- Each behavior produces different onset selection for same input
- Duration multiplier is behavior-specific
- Determinism preserved
- Edge cases: empty onsets, 1 onset, all strong beats, all offbeats

---

### Story 7.7.3 — Update `GuitarTrackGenerator` to use behavior system + duration shaping  (Completed)

**Intent:** Wire behavior selector and realizer into actual generation.

**File:** `Generator\Guitar\GuitarTrackGenerator.cs`

**Changes:**

1. **Add behavior selection** (after getting energy profile, before pattern lookup):
2. **Replace `ApplyDensityToPattern` with `CompBehaviorRealizer`**:
3. **Apply duration multiplier**:
4. **Remove `ApplyDensityToPattern` method** (replaced by `CompBehaviorRealizer`):

**Tests required:**
- Different sections produce different comp behaviors
- Different seeds produce audibly different bar-to-bar variation
- Duration multiplier affects note lengths
- Existing guardrails (lead-space, register) still work

---

### Story 7.7.4 — Create `KeysRoleMode` enum and deterministic selector  (Completed)

**Intent:** Define audibly-distinct keys/pads playing modes.

**New file:** `Generator\Keys\KeysRoleMode.cs`

---

### Story 7.7.5 — Create `KeysModeRealizer` to apply mode to onset selection + duration  (Completed)

**Intent:** Convert mode + available onsets into actual onset filtering and duration shaping.

**New file:** `Generator\Keys\KeysModeRealizer.cs`

**Acceptance criteria:**
// AI: purpose=Applies KeysRoleMode to onset selection and duration shaping for keys/pads.
// AI: invariants=Output onsets are valid subset of input; durations bounded; deterministic.

---

### Story 7.7.6 — Update `KeysTrackGenerator` to use mode system + duration shaping  (Completed)

**Intent:** Wire mode selector and realizer into actual generation.

**File:** `Generator\Keys\KeysTrackGenerator.cs`

**Changes:**

1. **Add mode selection** (after getting energy profile):
2. **Add mode realization** (before building onset grid):
3. **Apply duration multiplier in the note creation loop**:
4. **Handle SplitVoicing mode** (in the slot loop):

**Tests required:**
- Different sections produce different modes
- Duration multiplier affects sustain/chop
- SplitVoicing correctly splits the chord
- Existing guardrails (lead-space, register) still work

---

### Story  7.7.7 — Seed sensitivity audit and test coverage  (Completed)

**Intent:** Verify seed meaningfully affects output.
**New test file:** `Generator\Tests\SeedSensitivityTests.cs` (or add to existing test location)
**Acceptance criteria:**

---

## Key Invariants (Must Not Break)

1. **Determinism**: Same `(seed, song structure, groove)` ? identical output
2. **Lead-space ceiling**: Comp/keys never exceed MIDI 72 (C5)
3. **Bass register floor**: Comp never below MIDI 52 (E3)
4. **Scale membership**: All notes remain diatonic (octave shifts only)
5. **Sorted output**: `PartTrack.PartTrackNoteEvents` sorted by `AbsoluteTimeTicks`
6. **No overlaps**: Notes of same pitch don't overlap (via `NoteOverlapHelper`)

---

## Expected Audible Results

After implementation:
- **Verse**: Sparse anchors or standard comp, sustain/pulse keys ? calm, spacious
- **Chorus**: Syncopated chop or driving full comp, rhythmic keys ? energetic, busy
- **PreChorus/Bridge**: Anticipate comp, possible split voicing keys ? building tension
- **Different seeds**: Noticeably different bar-to-bar patterns within same structure
- **Different sections**: Obviously different rhythmic density and note length

---

## Story 7.8 — Phrase-level shaping inside sections (energy micro-arcs)  (not started)

** Intent: Stage 8 introduces `PhraseMap` formally, but Stage 7 should standardize minimal phrase-position semantics (or at least standard outputs used later).
Energy should modulate within a section (start → build → peak → cadence), not be flat for 8 bars.

**Acceptance criteria:**
- Provide a minimal per-section “micro-arc” representation usable by renderers:
  - either as a bar-indexed list of `EnergyMicroDelta` values
  - or as labeled positions (`Start/Middle/Peak/Cadence`) in a simple internal map
- Must be deterministic from section length + style + energy target.
- Must integrate with:
  - tension micro map (7.5.3)
  - variation plan (7.6)
- Additional unchanged behavior requirements 
  - phrase length defaults (4 or 8 bars) derived from section length
  - apply subtle per-bar modulation:
    - velocity lift at `Peak`
    - slight density thinning at `Cadence`
    - optional orchestration accents at `Start`/`Peak`

---

## Story 7.9 — Integration contracts (future proofing)  (Completed)

** Intent: Plan introduces a unified intent query used everywhere.
Ensure later stages can “ask” Stage 7 for arrangement support to convey emotion without rewriting Stage 7.

**Acceptance criteria:**
- Provide a single Stage 7 query surface (name flexible) that can return an immutable context object for downstream stages:
  - energy target
  - tension target
  - tension drivers
  - transition hint
  - variation plan summary
  - role presence/orchestration hints
  - reserved register bands + density caps
- Must have:
  - `GetSectionIntent(sectionIndex)`
  - `GetBarIntent(sectionIndex, barIndexWithinSection)`
- Keep Stage 7 as the owner of macro intent; Stage 8 may extend with phrase maps / cross-role thinning.
- Additional unchanged integration requirements; informational but still consistent):
  - Motifs can query energy/tension targets, phrase positions (Peak/Cadence), register intent for lead space.
  - Melody/lyrics can request arrangement ducking (reduce comp/pads density under vocal phrases) and shift pads/keys register away from vocal band.

**Implementation notes:**
- Created `ISongIntentQuery` interface with `GetSectionIntent` and `GetBarIntent` methods
- Created context records: `SectionIntentContext` (section-level), `BarIntentContext` (bar-level)
- Created supporting records: `RolePresenceHints`, `RegisterConstraints`, `RoleDensityCaps`
- Implemented `DeterministicSongIntentQuery` that precomputes and caches section contexts
- Aggregates existing `EnergySectionProfile`, `ITensionQuery`, `IVariationQuery` - no new planners
- Standardized existing ad-hoc register constraints (lead ceiling=72, bass floor=52, vocal band=60-76)
- Density caps derived from section energy level (low/mid/high)
- Created comprehensive test suite `SongIntentQueryTests` with 16 test methods - all pass ✓
- Build successful, no dependencies added
- See `AI Dialogs/Story_7_9_Implementation_Summary.md` for usage guide and examples



---

## Stage 8 — Material motifs: data definitions and test fixtures (Story M2)

**Why now:** Stage 7 provides energy/tension/phrase intent; Stage 9 will place and render motifs. Before placement logic, establish motifs as first-class material objects stored in MaterialBank (parallel to Story M1 material fragments).

**Goal:** Create foundational motif data structures and at least one hardcoded test motif, stored in MaterialBank, ready for Stage 9 placement/rendering.

**Design principles:**
- **Backward compatible:** MaterialBank already exists from Story M1; motifs extend it
- **Explicit over implicit:** Motif identity, role, and musical intent are first-class metadata
- **Deterministic-friendly:** All motif properties are immutable records suitable for deterministic selection
- **Serialization-ready:** Types use records/enums for future persistence (serializer not chosen yet)
- **PartTrack-based:** Motifs are PartTracks with `Kind = MaterialFragment` and `MaterialKind = Riff/Hook/MelodyPhrase`

---

### Story 8.1 — `MotifSpec` model (immutable, material-aware, simplified from old 9.1)

**Intent:** Define a compact, immutable motif specification that integrates with existing Material system.

**Acceptance criteria:**
- Create `MotifSpec` record in `Song/Material/` with:
  - `PartTrack.PartTrackId MotifId` (reuses existing identity system)
  - `string Name` (e.g., "Chorus Hook A", "Verse Riff 1")
  - `string IntendedRole` (e.g., "Lead", "GuitarHook", "SynthHook", "BassRiff")
  - `MaterialKind Kind` (e.g., `Hook`, `Riff`, `MelodyPhrase`)
  - `RhythmShape` (onset pattern within bar):
    - Simple list of onset tick offsets within a bar/phrase
    - Or reference to existing onset grid indices
  - `ContourIntent` enum: `Up`, `Down`, `Arch`, `Flat`, `ZigZag`
  - `RegisterIntent`:
    - `int CenterMidiNote` (target register center)
    - `int RangeSemitones` (allowed range around center)
  - `TonePolicy`:
    - `double ChordToneBias` per beat-strength (strong beats prefer chord tones)
    - `bool AllowPassingTones` (diatonic passing tones on weak beats)
  - Optional: `IReadOnlySet<string> Tags` for filtering (e.g., "energetic", "dark", "syncopated")
- Keep model simple and focused on WHAT the motif is, not HOW/WHERE it's placed (Stage 9)
- Must be immutable (use `record`)
- All numeric fields have safe defaults and valid ranges
- Add factory method: `MotifSpec.Create(...)` with clamping/validation

**Notes:**
- This is a simplified version of old Story 9.1 focused purely on data structure
- Removed `RepetitionPolicy` and `TargetSectionSelectors` - those belong in Stage 9 placement logic
- Motif specs will be converted to PartTracks for storage in MaterialBank

---

### Story 8.2 — Motif storage and retrieval in MaterialBank

**Intent:** Extend MaterialBank to properly store and query motifs as a specialized type of material fragment.

**Acceptance criteria:**
- Add motif-specific query methods to `MaterialBank`:
  - `GetMotifsByRole(string intendedRole)`
  - `GetMotifsByKind(MaterialKind kind)` (reuses existing method)
  - `GetMotifByName(string name)` (convenience method)
- Add helper to convert `MotifSpec` to `PartTrack`:
  - Method: `MotifSpec.ToPartTrack()` or `PartTrackFromMotifSpec(spec)`
  - Sets `Meta.Kind = PartTrackKind.MaterialFragment`
  - Sets `Meta.MaterialKind` from `MotifSpec.Kind`
  - Sets `Meta.IntendedRole` from `MotifSpec.IntendedRole`
  - Sets `Meta.Name` from `MotifSpec.Name`
  - Populates `PartTrackNoteEvents` from `MotifSpec.RhythmShape` + pitch placeholders
  - Events use `MaterialLocal` domain (ticks from 0)
- Add reverse helper: `MotifSpecFromPartTrack(track)` for round-trip
- Add validation:
  - Motif PartTracks must have `Domain = MaterialLocal`
  - Motif PartTracks must have `MaterialKind` in {`Riff`, `Hook`, `MelodyPhrase`, `DrumFill`, `BassFill`, `CompPattern`, `KeysPattern`}
- Tests:
  - Round-trip: `MotifSpec` → `PartTrack` → `MotifSpec` preserves data
  - MaterialBank can store and retrieve motifs
  - Query methods return correct subsets

**Notes:**
- Motifs are fragments in MaterialLocal time (local bar/phrase time, not absolute song time)
- Pitch values in the stored PartTrack are relative/placeholder (e.g., scale degrees or MIDI notes relative to a key)
- Stage 9 renderer will realize actual pitches against harmony context

---

### Story 8.3 — Create hardcoded test motifs (popular patterns)

**Intent:** Provide at least one real, usable motif for testing Stage 9 placement and rendering without needing a motif generator.

**Acceptance criteria:**
- Create class `MotifLibrary` (or `TestMotifs`) in `Song/Material/` with static factory methods
- Implement at least one hardcoded motif from each category:
  - **Chorus hook** (Lead role):
    - Rhythmically memorable (e.g., "da-da DUM" syncopated pattern)
    - Contour: Arch or ZigZag
    - 2-4 bar phrase length
    - Example inspiration: opening riff from "Smoke on the Water", "Seven Nation Army", or "Come As You Are"
  - **Verse riff** (Guitar or Bass role):
    - Steady, repeating pattern
    - Contour: Flat or slight Up/Down
    - 1-2 bar loop
    - Example inspiration: "Sunshine of Your Love" bass riff
  - **Optional: Synth hook** (Keys role):
    - Bright, energetic pattern
    - Contour: Up or Arch
    - 1 bar repeating
- Each motif must have:
  - Meaningful name (e.g., "Classic Rock Hook A")
  - Clear rhythm shape (onset ticks)
  - Appropriate register intent
  - Sensible tone policy (chord-tone bias, passing tone rules)
- Add tests:
  - Each test motif can be stored in MaterialBank
  - Each test motif has valid structure (all fields in range)
  - Test motifs are deterministic (same call → same output)

**Notes:**
- Focus on rhythmically distinctive patterns that will be immediately recognizable when rendered
- Keep pitch patterns simple (diatonic, small range) for MVP
- These motifs are MVP test fixtures; Stage 11+ may add procedural motif generation

---

### Story 8.4 — Motif validation helpers

**Intent:** Provide non-throwing validation for motif-specific constraints (parallel to `PartTrackMaterialValidation` from Story M1).

**Acceptance criteria:**
- Create `MotifValidation` static class in `Song/Material/`
- Implement validation method:
  - `IReadOnlyList<string> ValidateMotif(MotifSpec spec)`
  - Returns empty list if valid; otherwise returns issue descriptions
- Validation rules:
  - `RhythmShape` onsets must be >= 0 and within reasonable bar length
  - `RegisterIntent.CenterMidiNote` must be in valid MIDI range (21-108)
  - `RegisterIntent.RangeSemitones` must be positive and reasonable (<= 24)
  - `ChordToneBias` must be in [0..1]
  - `IntendedRole` must be non-empty
- Add tests:
  - Valid motif specs pass validation
  - Invalid specs produce appropriate error messages
  - Validation is deterministic

---

### Story 8.5 — Motif definition tests and MaterialBank integration

**Intent:** Ensure motif data layer is solid before Stage 9 placement work.

**Acceptance criteria:**
- Create test file `Song/Material/Tests/MotifDefinitionsTests.cs` (parallel to `MaterialDefinitionsTests.cs`)
- Test coverage:
  - `MotifSpec` creation and immutability
  - `MotifSpec` ↔ `PartTrack` round-trip
  - MaterialBank storage and retrieval of motifs
  - Query methods return correct subsets
  - Hardcoded test motifs are valid and deterministic
  - Validation catches common errors
- All tests must pass
- All tests must verify determinism where applicable

---

## Stage 9 — Motif placement and rendering (where and how motifs appear)

**Why now:** Stage 8 established motifs as material objects. Stage 9 makes them musically functional by placing them in the song structure and rendering them into actual note sequences.

**Goal:** Deterministically place motifs in appropriate sections, render them against harmony and groove, and integrate them with accompaniment.

---

### Story 9.1 — `MotifPlacementPlanner` (where motifs appear)

**Intent:** Motifs should be intentional: chorus hook, intro answer, pre-chorus lift. Determine WHICH motifs appear WHERE in the song.

**Acceptance criteria:**
- Create `MotifPlacementPlanner` that outputs `MotifPlacementPlan`
- Deterministically select and place motifs using inputs:
  - song form (`SectionTrack`)
  - section types and indices
  - Stage 7 energy/tension/variation plans
  - Stage 7 phrase map (when available, or fallback to inferred phrases)
  - available motifs from `MaterialBank` (filtered by role/kind)
  - seed for deterministic tie-breaking
- MVP placement heuristics:
  - **Chorus**: primary hook motif almost always (highest energy/tension moments)
  - **Intro**: optional motif teaser if energy low and arrangement sparse
  - **Pre-chorus**: motif fragment or rhythmic foreshadowing (build anticipation)
  - **Bridge**: either new motif or transformed existing motif (contrast)
  - **Verse**: optional riff if verse energy is mid-high
- Provide deterministic collision checks:
  - do not place motif when orchestration says role absent
  - do not place motif if register reservation would be violated by required accompaniment
  - avoid simultaneous dense motifs in same register band
- Support motif repetition and variation:
  - reference Stage 7 `SectionVariationPlan` for A/A'/B logic
  - repeated sections reuse same motif with optional bounded variation
- Output: `MotifPlacementPlan` containing:
  - `List<MotifPlacement>` where each placement has:
    - `MotifSpec` reference (or `MotifId`)
    - `int AbsoluteSectionIndex`
    - `int StartBarWithinSection`
    - `int DurationBars`
    - `double VariationIntensity` (for A/A' rendering differences)
    - optional `TransformTags` (e.g., "OctaveUp", "Invert", "Syncopate")
- Tests:
  - Determinism: same inputs → same placement plan
  - Placement respects orchestration constraints
  - Placement respects register reservations
  - Common forms produce sensible placement (Intro-V-C-V-C-Bridge-C-Outro)
  - Different seeds produce different placement choices when valid options exist

**Notes:**
- Placement is INTENT only; actual notes come from Story 9.2 rendering
- Placement must work with test motifs from Stage 8 Story 8.3

---

### Story 9.2 — `MotifRenderer` (notes from motif spec + harmony)

**Intent:** Convert placed motif specs into actual note events against song harmony and groove context.

**Acceptance criteria:**
- Create `MotifRenderer` that takes:
  - `MotifSpec` (defines rhythm, contour, register, tone policy)
  - `MotifPlacement` (where/when it appears, variation intensity, transform tags)
  - `HarmonyContext` (per-slot harmony from harmony track)
  - `OnsetGrid` (from groove preset)
  - Stage 7/8 intent context (energy/tension/phrase position)
  - seed for deterministic pitch selection
- Render motif notes deterministically:
  - **Rhythm**: apply `MotifSpec.RhythmShape` to groove onset grid
    - align motif onsets with valid groove slots
    - preserve rhythmic character of motif
  - **Pitch**: realize pitches from contour and tone policy:
    - strong beats prefer chord tones (bias from `TonePolicy.ChordToneBias`)
    - weak beats allow diatonic passing tones if `TonePolicy.AllowPassingTones`
    - apply contour intent (`Up`/`Down`/`Arch`/`Flat`/`ZigZag`)
    - respect `RegisterIntent` (center + range)
    - clamp to instrument range and avoid vocal band
  - **Variation operators** (deterministic, bounded):
    - driven by `MotifPlacement.VariationIntensity` and `TransformTags`
    - octave displacement (±12 semitones, bounded by register)
    - neighbor-tone ornaments (diatonic only)
    - rhythmic displacement by one slot where safe (doesn't create overlap/collision)
    - small contour-preserving pitch adjustments (±2 semitones)
  - **Velocity**: apply Stage 7 energy/tension biases
- Output: `PartTrack` in `SongAbsolute` domain (absolute song time)
  - `Meta.Kind = RoleTrack`
  - `Meta.IntendedRole` from `MotifSpec.IntendedRole`
  - `Meta.Name` indicates motif source and variation
- Tests:
  - Determinism: same inputs → identical note sequence
  - Rendered notes are in valid MIDI range
  - Rendered notes respect harmony (chord tones vs passing tones)
  - Rendered notes respect register constraints
  - Variation operators stay within bounds
  - No note overlaps
  - Events sorted by `AbsoluteTimeTicks`

**Notes:**
- This is where MaterialLocal (motif template) becomes SongAbsolute (rendered track)
- Renderer must use existing harmony and groove infrastructure (no new systems)

---

### Story 9.3 — Motif integration with accompaniment (call/response + ducking hooks)

**Intent:** When a motif occurs, accompaniment should *make room* without destroying the groove.

**Acceptance criteria:**
- Create `MotifPresenceMap` that can be queried per `(section, bar, slot)`:
  - returns `bool IsMotifActive` and optional `MotifDensity` estimate
- Wire minimal "ducking" into existing role generators:
  - **Comp/Keys**: reduce density under motif windows
    - skip some weak-beat onsets when motif is dense
    - shorten sustain at motif onsets
  - **Pads**: shorten sustain slightly at motif onsets (create breaths)
  - **Drums**: reduce optional hats/ghosts slightly under dense motif windows
    - style-safe: only affect optional events, never groove anchors
- Ducking must be:
  - deterministic
  - bounded (never remove more than 30% of events)
  - groove-protective (never remove downbeats/backbeats)
- Tests:
  - Determinism: same motif presence → same ducking decisions
  - Groove anchors preserved
  - Density reduction stays within bounds
  - Accompaniment still sounds musical (not over-thinned)

**Notes:**
- Ducking is a BIAS, not a hard rule
- Integration should feel natural, not like "everything stops for the motif"

---

### Story 9.4 — Motif diagnostics

**Intent:** Make motif placement and rendering decisions visible for debugging and tuning.

**Acceptance criteria:**
- Create `MotifDiagnostics` (parallel to energy/tension diagnostics)
- Opt-in reports showing:
  - motif placements by section (which motifs where)
  - per-motif variation ops used (what transforms applied)
  - collision/ducking decisions (what was thinned and why)
  - register usage by motif vs accompaniment
- Diagnostics must:
  - not affect generation (read-only)
  - be deterministic
- Add test verifying diagnostics don't change generated output
- Integrate with existing Stage 7 diagnostics where appropriate

---

## Stage 10 — Melody & lyric scaffolding (timing windows + singable melody MVP)

**Why now:** Stage 9 provides instrumental motifs (hooks/riffs). Stage 10 adds vocal melody as a first-class feature, building on motif rendering infrastructure while adding lyric-aware timing constraints.

**Goal:** Build minimal melody engine with future lyric integration in mind. Melodies are like motifs but with syllable timing constraints and vocal register/tessitura considerations.

**Dependencies:**
- Stage 8 material system (melody specs can reuse `MotifSpec` patterns)
- Stage 9 rendering infrastructure (melody rendering parallels motif rendering)
- Stage 7 phrase maps and tension (melody placement uses phrase positions)

---

### Story 10.1 — `LyricProsodyModel` (inputs and constraints; no real lyrics yet)

**Intent:** Provide syllable timing/stress windows even if lyrics are placeholder. Establish the contract for future lyric integration.

**Acceptance criteria:**
- Add a model for phrase-level syllable planning:
  - syllable count per phrase (deterministic default based on phrase length)
  - stress pattern (simple: `Strong`/`Weak` or numeric stress levels 0-2)
  - allowed melisma count (initially low; most syllables get one note)
  - optional rest positions (intentional breaths/pauses)
- Create `LyricProsodyModel` record:
  - `int SyllableCount`
  - `IReadOnlyList<SyllableStress> StressPattern` (Strong/Weak per syllable)
  - `int MaxMelismaNotesPerSyllable` (default: 1-2)
  - `IReadOnlySet<int> RestPositions` (syllable indices that are rests)
- Deterministically generate a default prosody plan when the user provides none:
  - derive syllable count from phrase length (e.g., 4-bar phrase → 8-12 syllables)
  - stress pattern follows simple alternating or downbeat-emphasis pattern
  - seed used for deterministic variation
- Tests:
  - Default prosody generation is deterministic
  - Generated patterns are musically sensible (not too dense, not too sparse)
  - Stress patterns align with typical phrase structures

**Notes:**
- This is a placeholder for real lyrics (Stage 14+)
- Real lyric integration will replace defaults but use same interface

---

### Story 10.2 — Syllable windows → onset slot mapping

**Intent:** Map syllables onto groove onset slots deterministically, respecting prosody constraints.

**Acceptance criteria:**
- Create `VocalTimingPlanner` that takes:
  - `LyricProsodyModel` (syllable count, stress pattern)
  - `OnsetGrid` (from groove)
  - phrase length and position
  - Stage 7 energy/tension context
  - seed
- Map syllables onto onset slots using deterministic rules:
  - **stressed syllables** prefer stronger beats/slots (beat 1, beat 3, strong onsets)
  - **unstressed syllables** can use weaker beats/offbeats
  - avoid impossible densities (cap: max 2-3 syllables per bar typically)
  - allow rests intentionally (respect `RestPositions` from prosody)
  - melisma handling: when multiple notes per syllable, use adjacent slots
- Output: `VocalTimingPlan` that can be queried by `(section, bar, slot)`:
  - returns `SyllableEvent` with:
    - `int SyllableIndex`
    - `SyllableStress Stress`
    - `bool IsMelismaStart` / `bool IsMelismaContinuation`
    - `bool IsRest`
- Tests:
  - Determinism: same inputs → identical timing plan
  - Stressed syllables land on strong beats
  - Density stays within bounds (no overpacked bars)
  - Rest positions respected

---

### Story 10.3 — Melody generator MVP (singable, chord-aware)

**Intent:** Generate pitch for each syllable event, creating a singable melody that respects harmony and phrase structure.

**Acceptance criteria:**
- Create `MelodyRenderer` (parallel to `MotifRenderer` from Stage 9)
- Takes:
  - `VocalTimingPlan` (where syllables occur)
  - `HarmonyContext` (harmony per slot)
  - vocal register range and tessitura (comfortable singing range)
  - Stage 7 phrase position and tension context
  - seed for deterministic pitch selection
- Generate pitch for each syllable event deterministically:
  - **strong beats**: prefer chord tones (root, third, fifth)
  - **weak beats**: allow diatonic passing/neighbor tones (policy-gated)
  - **respect range and tessitura**: stay within comfortable vocal range
    - typical ranges: soprano (C4-A5), alto (G3-E5), tenor (C3-A4), bass (E2-E4)
    - keep most notes in middle tessitura (avoid extremes)
  - **avoid large leaps** unless phrase position suggests emphasis (`Peak`)
    - prefer stepwise motion and small leaps (≤ perfect 5th)
    - larger leaps allowed at phrase starts or climaxes
  - **phrase contour**: apply natural phrase arcs
    - `Start`: establish starting pitch
    - `Middle`: gradual motion
    - `Peak`: highest note(s) of phrase
    - `Cadence`: resolve downward toward stable tone
  - **incorporate tension intent**:
    - higher tension biases scale degrees away from tonic (2nd, 6th, 7th scale degrees)
    - release moments return toward stable tones (1st, 3rd, 5th scale degrees)
- Handle melisma:
  - first note of melisma is primary pitch (aligned with harmony)
  - continuation notes are neighbor tones or passing tones
- Output: `PartTrack` in `SongAbsolute` domain
  - `Meta.Kind = RoleTrack`
  - `Meta.IntendedRole = "Vocal"` or `"Lead"`
- Tests:
  - Determinism: same inputs → identical melody
  - All notes in valid vocal range
  - Strong beats use chord tones
  - Phrase contours are natural (no random jumps)
  - Melisma handled correctly
  - No overlaps

**Notes:**
- Melody rendering reuses harmony and phrase infrastructure from Stages 7-9
- Melody is like a motif but with syllable timing constraints

---

### Story 10.4 — Vocal band protection (make room for melody)

**Intent:** When melody is active, accompaniment must avoid competing in the vocal register.

**Acceptance criteria:**
- Define a `VocalBand` (MIDI note range) per vocal type:
  - Soprano: C4-A5 (MIDI 60-81)
  - Alto: G3-E5 (MIDI 55-76)
  - Tenor: C3-A4 (MIDI 48-69)
  - Bass: E2-E4 (MIDI 40-64)
  - Default: C4-E5 (MIDI 60-76) for unspecified
- When `VocalTimingPlan` indicates active syllables:
  - **Pads/Keys**: avoid sustained notes in vocal band
    - shift voicings down or up by octave
    - reduce density of mid-register notes
  - **Comp**: reduce density and/or shift inversion/register away from vocal band
    - prefer lower inversions or higher inversions (avoid middle)
  - **Drums**: optionally reduce busyness slightly (only optional events like hats/ghosts)
    - style-safe: never remove groove anchors
- Protection must be:
  - deterministic
  - bounded (don't completely remove accompaniment)
  - groove-preserving
- Tests:
  - Determinism: same vocal timing → same protection decisions
  - Accompaniment avoids vocal band when melody active
  - Groove anchors preserved
  - Register shifts stay within instrument ranges

---

### Story 10.5 — Melody variation across repeats (A/A')

**Intent:** Repeated sections (Verse 2, final Chorus) should have melodic variation while preserving identity.

**Acceptance criteria:**
- Support controlled variation for repeated melody sections:
  - reference Stage 7 `SectionVariationPlan` for A/A'/B logic
  - Verse 2 melody = Verse 1 melody + bounded variation ops
  - Final chorus can lift register or add melodic extensions
- Variation operators (deterministic, bounded):
  - octave displacement (±12 semitones, stay in vocal range)
  - neighbor-tone ornaments (add passing tones on weak beats)
  - rhythmic displacement by one slot where safe
  - melodic extensions at phrase ends (add 1-2 notes before rest)
  - register lift for climactic sections (shift up 2-4 semitones)
- Variation intensity driven by:
  - `SectionVariationPlan.VariationIntensity`
  - section type and index
  - energy/tension context
  - seed for deterministic selection
- Tests:
  - Determinism: same inputs → identical varied melody
  - Variations stay within vocal range
  - Variations preserve melodic contour and character
  - A' sections recognizable as variants of A

**Notes:**
- Melody variation parallels motif variation from Stage 9
- Keep variations conservative for singability

---



---

(THIS WAS MOVED HERE PENDING FURTHER ANALYSIS) 
## Stage ? — Phrase map + arrangement interaction (make Stage 7 usable by future melody/motifs)

**Why now:** Stage 7 introduced energy/tension intent. Stage 8 turns that intent into a *time grid of musical meaning* (phrases, peaks, cadences) and adds cross-role interaction rules, so later melody/motif decisions can be made safely.

### Story ?.1 — Cross-role constraints + “density budget” engine

**Intent:** as more musical intelligence is added, arrangements can become crowded.

**Acceptance criteria:**
- Implement a small cross-role constraint pass operating per `(section, bar, slot)`:
  - **Lead space reservation:** define a register band reserved for future lead/vocal.
  - **Low-end management:** prevent pads/comp/keys from colliding with bass.
  - **Simultaneous busy-ness:** when multiple roles are dense on weak slots, deterministically thin one role.
- Introduce a `RoleDensityBudget`:
  - per section (from Stage 7 orchestration)
  - optionally per bar (from `SectionArc`)
- Deterministic thinning policy:
  - choose which role yields using stable precedence rules + deterministic tie-break.
- Tests:
  - determinism
  - budget never exceeded
  - bass/lead space constraints never violated

### Story ?.2 — `PhraseMap` (within-section) as a first-class model

**Intent:** later systems need to ask “where am I in the phrase?” deterministically.

**Acceptance criteria:**
- Create a `PhraseMap` model for each `SongSection`:
  - Bar-level positions: `Start`, `Middle`, `Peak`, `Cadence`.
  - Flags: `IsPhraseStart`, `IsPhraseEnd`, `IsSectionStart`, `IsSectionEnd`.
  - Phrase identity: `PhraseIndex`, `BarIndexInPhrase`, `PhraseLengthBars`.
- Default behavior when not specified by user:
  - Infer phrases deterministically using section length (common cases: 4-bar phrase for 8/16 bar sections, 2-bar for 4 bar sections).
  - Allow style/groove to influence defaults (e.g., EDM often 8-bar builds).
- Must support irregular sections (e.g., 6 bars) by producing best-effort segmentation.
- Add tests:
  - correct map length
  - determinism
  - reasonable distribution of `Peak`/`Cadence` for common sizes

### Story ?.3 — `SectionArc` (micro-energy within section) derived from Stage 7 + `PhraseMap`

**Intent:** energy/tension shouldn’t be flat across the whole section.

**Acceptance criteria:**
- Create a `SectionArc` (bar-level modifiers) that provides small per-bar deltas:
  - `EnergyDelta` and `TensionDelta` (bounded)
  - derived deterministically from `SectionEnergyProfile`, `SectionTensionProfile`, and `PhraseMap`.
- Ensure deltas are subtle (e.g., `±0.05` scale) and clamped.
- Wire into role renderers as a *bias*:
  - drums: fill/pull/impact bias at phrase ends and section starts
  - comp/keys/pads: velocity/density bias at `Peak`, thinning at `Cadence`
  - bass: pickup probability increase at `Cadence` only if a valid slot exists

### Story ?.4 — Expose a unified query contract for later stages

**Intent:** avoid future refactors by giving a stable “ask the planner” API.

**Acceptance criteria:**
- Create a `SongIntentQuery` (or similarly named) API that returns a single immutable `IntentContext`:
  - energy target + tension target
  - micro energy/tension deltas (from `SectionArc`)
  - phrase position (from `PhraseMap`)
  - orchestration hints (roles present, density multipliers)
  - reserved register bands (lead/vocal)
- Provide methods:
  - `GetIntentContext(sectionIndex, barIndexWithinSection)`
  - `GetIntentContext(absoluteBarIndex)` (optional convenience)
- Must not require renderers to know planner internals.

---

(THIS WAS MOVED HERE PENDING FURTHER ANALYSIS)
## Story ?.1 — Role interaction rules (prevent clutter; reserve melody/lead space)

**Intent: Stage 8+ depends on this becoming an actual enforceable contract, not just guidance.
Higher energy often means more parts; without explicit rules, arrangements become muddy.

**Acceptance criteria:**
- Define explicit (queryable) constraints:
  - lead/vocal reserved band (even before melody exists)
  - low-end reserved band for bass
  - per-role density caps
- Provide a deterministic “conflict resolution policy” skeleton:
  - priority order by role (configurable by style)
  - deterministic tie-break function
- Note (explicitly preserved): actual cross-role thinning is intended for Stage 8, but Stage 7 must expose:
  - constraint inputs
  - budgets
  - intended precedence
- Additional unchanged constraint scope
  - **Lead space reservation:** define a MIDI band reserved for future lead/vocal; non-lead roles avoid dense sustained notes there.
  - **Low-end management:** prevent pads/keys/comp from occupying bass register.
  - **Density budgets:** when multiple roles are busy on the same weak slots, deterministically thin one role.

---

## Story ?.10 — Diagnostics & explainability hooks

**Intent: Stage 13/15 require a stable diagnostics bundle and regression-friendly outputs.
Stage 7 becomes the backbone for later creative complexity; we need visibility into decisions.

**Acceptance criteria:**
- Add opt-in diagnostics that can dump:
  - energy arc template + constrained energies
  - derived `SectionEnergyProfile`
  - derived `SectionTensionProfile` + drivers
  - `MicroTensionMap` summary
  - `SectionVariationPlan`
  - transition hints per boundary
- Must be deterministic and must not affect generation.
- Unchanged diagnostic scope:
  - dump chosen `EnergyArc`
  - dump derived `SectionVariationPlan`
  - summarize realized densities + average velocities per role per section

---

## Story ?.12 — Energy lever vector (planning-only metadata)

**Intent: align Stage 7 with the research model that energy is multi-factor, without forcing a rewrite.

**Acceptance criteria:**
- Add a planning-only `EnergyLeverVector` (or similarly named) that tracks intended contributors:
  - instrumentation density intent
  - rhythmic activity intent
  - register lift intent
  - harmonic rhythm intent (future)
  - dynamics intent (velocity bias)
- Derived deterministically from `EnergyArc` + style + section type.
- Used only as metadata/hints at this stage; renderers may ignore fields they do not support yet.

---

## Story ?.13 — Harmonic rhythm intent hooks

**Intent:** research indicates chord-change rate is an energy lever. Stage 11 will do harmonic narrative, but Stage 7 should reserve the hook.

**Acceptance criteria:**
- Add a small intent field at section-level:
  - `HarmonicRhythmMultiplier` (e.g., 1.0 baseline, 1.2 for chorus)
- Deterministic mapping from section type + energy target.
- No behavior change required yet unless harmony engine already supports it.

---

## Story ?.14 — Dominant pedal tension hook reservation 

**Intent:** the research compilation highlights dominant pedal as a repeatable tension technique. Stage 11 will implement; Stage 7 should reserve the hook.

**Acceptance criteria:**
- Add a section-transition intent hint:
  - `bool SuggestDominantPedalLeadIn`
  - optional `int LeadInBars`
- Deterministic mapping:
  - enabled in build contexts (e.g., PreChorus → Chorus) when tension high enough
  - style gated
- No harmony rewriting required yet.

---

## Stage 11 — Harmonic narrative (tension-aware harmony choices beyond static progressions)

**Why now:** the system currently renders harmony *well*, but long-form musicality often needs harmonic storytelling: pre-chorus lift, bridge detour, cadential strength.

### Story 11.1 — Harmonic function tagging per chord (tonic/predominant/dominant)

**Acceptance criteria:**
- Add a lightweight harmonic function tag to harmony events:
  - `Tonic`, `Predominant`, `Dominant`, `ModalMixture`, `ChromaticApproach` (later)
- Provide deterministic inference for common progressions when not specified.

### Story 11.2 — Cadence planner at phrase ends

**Acceptance criteria:**
- At `PhraseMap` cadence bars, bias progressions toward stronger cadences:
  - authentic/plagal/half cadence options depending on style
- Guardrails:
  - do not rewrite user-specified progression unless allowed
  - keep changes minimal and deterministic

### Story 11.3 — Pre-chorus lift & chorus release harmony policy

**Acceptance criteria:**
- Pre-chorus: deterministic tension-raising harmonic options (e.g., secondary dominant policy-gated).
- Chorus: allow harmonic release (return toward tonic stability) even if energy is high.

### Story 11.4 — Borrowed chords + chromaticism (policy-gated)

**Acceptance criteria:**
- Add optional chromatic tools behind strict policy flags:
  - secondary dominants
  - modal mixture (bVI, iv, etc.)
  - chromatic approach chords
- Must be style-aware and strongly bounded.

---

## Stage 12 — Performance rendering (humanization for non-drums + articulations)

**Why now:** drums likely already sound more alive. To feel human, other roles need articulation and micro-performance.

### Story 12.1 — Micro-timing + velocity shaping for comp/keys/pads/bass

**Acceptance criteria:**
- Add deterministic micro-timing per role (distinct feel per style):
  - bass slightly behind or on-grid
  - comp slightly ahead/behind depending on groove
  - pads mostly on-grid with gentle offsets
- Add deterministic velocity shapes per phrase position:
  - `Peak` accents, `Cadence` relax

### Story 12.2 — Articulation model per role

**Acceptance criteria:**
- Add simple articulation tags to note events (renderer-dependent):
  - bass: slide, staccato (later), ghost (optional)
  - comp: palm mute vs open (optional)
  - keys: staccato vs sustain bias
- Deterministic assignment using energy/tension + phrase position.

### Story 12.3 — Sustain control + release tails

**Acceptance criteria:**
- Pads/keys sustain planning per section:
  - denser sustain in chorus, thinner in verse
  - shorten at cadence windows to create breaths
- Ensure no note overlaps create stuck notes; clamp to bar boundaries.

---

## Stage 13 — Sound/render pipeline + export quality (MIDI now, audio later)

**Why now:** more musical intelligence is only useful if export is reliable and debuggable.

### Story 13.1 — Instrument/patch mapping profiles

**Acceptance criteria:**
- Create style-aware “instrumentation profiles”:
  - mapping from roles to MIDI channels/patches
  - register constraints per instrument
- Deterministic selection by style/groove.

### Story 13.2 — MIDI export correctness & validation suite

**Acceptance criteria:**
- Add a validation pass before writing MIDI:
  - no negative times
  - no note overlaps per channel when not allowed
  - velocities in range
  - bar boundaries respected
- Add tests covering tricky edge cases (fills at boundaries, micro-timing clamps).

### Story 13.3 — Render diagnostics bundle

**Acceptance criteria:**
- Ability to dump:
  - intent reports (energy/tension/phrase maps)
  - per-role event summaries
  - motif/melody placement
  - MIDI validation report
- Ensure bundle is deterministic and does not affect generation.

---

## Stage 14 — User input model + constraints (make it a product)

**Why now:** as capability grows, the system needs a clear input schema so outputs can be controlled and reproduced.

### Story 14.1 — `GenerationRequest` schema (versioned)

**Acceptance criteria:**
- Add a versioned request model containing:
  - style/groove
  - tempo, key, meter
  - song form
  - seed(s)
  - optional overrides for energy arc / orchestration / motif enablement
- Backward-compatible parsing for older versions.

### Story 14.2 — Constraint/guardrail configuration

**Acceptance criteria:**
- Add user-configurable guardrails with safe defaults:
  - register limits per role
  - lead/vocal band range
  - max density caps per role
  - chromatic policy flags
- Deterministic behavior when constraints unchanged.

### Story 14.3 — Preset packs (style kits)

**Acceptance criteria:**
- Bundle presets for:
  - energy arcs
  - drum orchestration language
  - comp rhythm libraries
  - bass pattern libraries
  - motif libraries
- Deterministic selection by style.

---

## Stage 15 — Musical evaluation loop (automatic critique + iteration hooks)

**Why now:** increasing “human-like” quality benefits from automated checks and iterative improvement, even without ML.

### Story 15.1 — Rule-based musicality metrics

**Acceptance criteria:**
- Compute metrics such as:
  - note density per role per section
  - register overlap score (muddy risk)
  - repetition score (too repetitive vs too chaotic)
  - cadence strength distribution
- Metrics must be deterministic and cheap.

### Story 15.2 — “Regenerate with constraints” iteration API

**Acceptance criteria:**
- Support a workflow:
  - generate candidate
  - evaluate metrics
  - adjust certain planner knobs deterministically (bounded)
  - regenerate
- Preserve reproducibility:
  - iteration produces a new “derived seed” recorded in output metadata.

---

## Stage 16 — Optional ML/AI augmentation (strictly bounded, still deterministic at the top)

**Why later:** only once the classic pipeline is stable and explainable should AI be introduced.

### Story 16.1 — AI suggestion interface (non-authoritative)

**Acceptance criteria:**
- Add a pluggable suggestion interface that can propose:
  - motif contour variants
  - lyric prosody variants
  - arrangement edits
- Suggestions are treated as candidates and must be validated by guardrails.
- Determinism rule:
  - the *final* selection must remain deterministic given a seed + candidate list.

---

## Notes for remaining Stage 7 work (supported by this plan)

Even assuming Stage 7 is “complete” for the purposes of this plan, the stories above put pressure on these Stage 7 adjacent areas:
- Tension query + hooks must support *bar-level* access (Stage 8/10/12 rely on phrase windows).
- Orchestration rules need to be expressed in a queryable way (role presence + density intent).
- Diagnostics must be opt-in and non-invasive (several later stages depend on explainability).
