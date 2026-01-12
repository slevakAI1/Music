# Music Generator Plan (Post–Stage 2)

## Stage 3 — Make harmony sound connected (keys/pads voice-leading first) - COMPLETED

**Why now:** keys/pads are the fastest way to stop the output from sounding like a MIDI test pattern. Voice-leading makes everything feel intentional.

### Story 3.1 — `ChordRealization` becomes the unit of harmony rendering - COMPLETED

**Intent:** stop treating a chord as “list of chord tones.” Treat it as “chosen voicing + register + optional color,” stable for a window.

**Acceptance criteria:**
- Create `ChordRealization` (your existing Stage 3.1 idea) with fields you will actually use:
  - `IReadOnlyList<int> MidiNotes`
  - `string Inversion`
  - `int RegisterCenterMidi`
  - `bool HasColorTone` + `string? ColorToneTag` (e.g., `"add9"`)
  - `int Density` (note count)
- Keys generation consumes `ChordRealization` per onset slot (not raw notes).

### Story 3.2 — `VoiceLeadingSelector` for consecutive harmony contexts - COMPLETED

**Intent:** choose inversions/voicings by minimizing movement, not by randomness.

**Acceptance criteria:**
- Implement `VoiceLeadingSelector`:
  - **Inputs:** previous `ChordRealization` (or previous MIDI set), current `HarmonyPitchContext`, section profile
  - **Output:** next `ChordRealization`
- “Cost function” MVP:
  - minimize total absolute semitone movement (optionally penalize voice crossings)
  - keep top note within a target range for the role
- Keys and pads track keep independent “previous voicing” state.

### Story 3.3 — Section-based voicing / density profile (very small) -  - COMPLETED

**Intent:** chorus should lift (register, density), verse should be lighter.

**Acceptance criteria:**
- Add lightweight per-section “arrangement hints” (doesn’t need a huge model):
  - `RegisterLift` (e.g., `+0` / `+12`)
  - `MaxDensity` (`3–5`)
  - `ColorToneProbability` by section type
- Keys/pads use this instead of a global `KeysAdd9Probability`.

---

## Stage 4 — Make comp into comp (not a monophonic pseudo-lead) - Completed

**Why now:** your guitar “comp” is currently a single-note line. That will always read as lead-ish.

### Story 4.1 — Comp rhythm patterns, not just groove onsets - COMPLETED

**Intent:** groove onsets are a skeleton, not the part.

**Acceptance criteria:**
- Add a small `CompRhythmPattern` library keyed by groove preset + section type:
  - uses the bar’s onset slots as candidates
  - pattern selects a subset (e.g., anticipate beat 1, skip beat 3, etc.)
- Still deterministic: “pattern selection” is deterministic by section + bar index.

### Story 4.2 — `CompVoicingSelector`: chord fragments & guide tones

**Intent:** comp = chord fragments (2–4 notes), guide tones on strong beats, root often omitted.

**Acceptance criteria:**
- Implement `CompVoicingSelector.Select(ctx, slot, previousVoicing, sectionProfile) -> List<int>`
- MVP rules:
  - strong beats: prefer including 3rd/7th when available
  - allow root omission
  - avoid muddy close voicings below a low threshold (e.g., keep lowest comp tone above ~E3-ish)
  - keep top comp tone below the “lead space” ceiling
- Guitar comp emits multi-note events per slot (simultaneous notes).

### Story 4.3 — Strum/roll micro-timing (optional but high payoff) - Completed

**Intent:** block chords sound fake; tiny offsets make it human.

**Acceptance criteria:**
- Add deterministic per-note offsets within a chord (e.g., `0–N` ticks spread).
- Apply only to comp (and maybe keys later).

---

## Stage 5 — Bassline writing (lock to groove + outline harmony)

**Why now:** bass and drums define "song feels like a genre."

### Story 5.1 — Bass pattern library keyed by groove + section type - COMPLETED

**Acceptance criteria:**
- Define a handful of reusable bass patterns:
  - root anchors
  - root–fifth movement
  - octave pop
  - diatonic approach into chord change (policy-gated)
- Pattern selection is deterministic by section + bar; RNG only for tie-break.

### Story 5.2 — Chord-change awareness (approaches + pickups) - COMPLETED

**Acceptance criteria:**
- Detect when next harmony changes within `N` beats.
- Insert approach note on weak beat or pickup onset (only if the groove has a slot there).
- Keep rules strict initially (no chromatic yet unless you later enable it via policy).

---

## Stage 6 — Drums: from static groove to performance (variation + fills)

**Why now:** your drum track is literally the preset. Real songs have micro-variation, transitions, and *part-writing* choices (sound selection, dynamics, phrasing).

**Goal for Stage 6:** keep the existing groove as a *template*, then render a convincing drum performance that sounds like a human drummer with an intentional part: consistent “time feel,” controlled variation, musical transitions, and section-dependent energy.

**Non-goals (push later if needed):**
- Full “genre drummer model” / ML.
- Long-range thematic drum motifs (belongs in Stage 7/8 once section identity/energy transforms are in place).

### Story 6.1 — `GroovePreset` becomes “template”; add variation operators (expanded) -- Completed

**Intent:** turn static onset lists into a living performance while staying deterministic and style-safe.

**Acceptance criteria:**
- Add `DrumVariationEngine` that takes:
  - groove preset (template)
  - section type/profile (energy intent)
  - bar index (for deterministic variation across bars)
  - seed (for deterministic tie-break only)
- Engine outputs per-bar drum events with:
  - **Kick**: small variations that still “make sense” (never sabotage the downbeat/backbeat feel).
  - **Snare**:
    - main hits preserved
    - add **ghost notes** at low velocity on selected weak subdivisions
    - optional **flams** on high-energy accents (policy-gated)
  - **Hi-hat / ride**:
    - controlled open/close articulations (open hat only where it won’t smear the groove)
    - occasional skipped hats / added hats for flow
    - velocity “hand pattern” (alternating or shaped) so hats don’t sound machine-gunned
- Deterministic rules:
  - Variation choices are deterministic for `(seed, grooveName, sectionType, barIndex)`.
  - RNG is only used as a deterministic tie-break among valid options.

### Story 6.2 — Timing & dynamics humanization layer (micro-feel) - Completed

**Intent:** even correct notes sound fake without timing and dynamics nuance.

**Acceptance criteria:**
- Add a deterministic **micro-timing** layer for drums:
  - hats slightly ahead/behind depending on groove style (tiny offsets)
  - kick/snare closer to grid, with controlled push/pull
  - max deviation is clamped (never breaks bar boundaries)
- Add a deterministic **velocity shaping** layer:
  - hat patterns have accents (e.g., beat 2/4 stronger, or offbeat accents per style)
  - ghost notes are consistently quiet
  - fills crescendo into transitions
- Ensure all micro-timing and velocity shaping is repeatable from seed.

### Story 6.3 — Section transitions: fills, turnarounds, and pickups (tight scope) - Completed

**Intent:** a drummer signals form; transitions must feel inevitable.

**Acceptance criteria:**
- At the end of a section **(or last bar of a phrase)** generate an appropriate fill that:
  - respects bar boundaries and `Song.TotalBars`
  - is style-aware (mapped from groove name)
  - is density-capped so it doesn't overwhelm other roles
- Fill selection is deterministic by `(seed, grooveName, sectionType, sectionIndex)`.
- Fills have structured shapes:
  - simple 8th-note and 16th-note rolls
  - tom movement (high→mid→low) where supported
  - crash/ride + kick support at the downbeat of the next section

### Story 6.4 — Cymbal language & orchestration (make it sound “produced”)

**Intent:** pro drum parts use cymbals intentionally (not randomly) to mark phrases/sections.

**Acceptance criteria:**
- Add deterministic crash/ride placement rules:
  - crash on section start (configurable per section type)
  - occasional crash on phrase peaks (e.g., every 4 or 8 bars)
  - ride vs hat selection based on section energy (chorus may go ride)
- Add choke/stop hits for endings/outros (optional, style-gated).

### Story 6.5 — Pattern “A / A’ / B” variation hooks for later Stage 7 integration

**Intent:** Stage 7 will control repetition/contrast; drums must expose clean knobs.

**Acceptance criteria:**
- Expose drum-role parameters that Stage 7 can drive without rewriting drum logic:
  - `DensityMultiplier` (hats/ghosts/fill density)
  - `VelocityBias`
  - `BusyProbability`
  - `FillProbability` and `FillComplexity`
- Keep the engine deterministic when these knobs are held constant.

---

**If something should be later:**
- “Drum identity per section” (A/A’ transforms) becomes truly powerful once Stage 7 `SectionEnergyProfile` exists. Stage 6 should implement the mechanics (variation, fills, feel, orchestration) and keep the controls/inputs ready for Stage 7 to drive.

---

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

**Implementation notes:**
- Created `TensionDiagnostics` static class in `Song\Energy\TensionDiagnostics.cs` with 4 report methods:
  - `GenerateFullReport`: Comprehensive section-by-section analysis showing macro tension, micro tension summary (min/max/avg/range), drivers, and phrase flags
  - `GenerateSummaryReport`: High-level overview showing tension progression by section type, tension peak, high-tension count, and driver summary
  - `GenerateCompactReport`: One-line-per-section format showing macro and micro tension ranges
  - `GenerateTransitionHintSummary`: Section transition hints (Build/Release/Sustain/Drop)
- All diagnostics follow same pattern as `EnergyConstraintDiagnostics` for consistency
- Created comprehensive test file `TensionDiagnosticsTests.cs` with 8 test methods:
  - `TestDiagnosticsDoNotAffectGeneration`: CRITICAL test verifying diagnostics don't mutate tension values
  - `TestDiagnosticsDeterminism`: Verifies same inputs produce identical reports
  - `TestFullReportGeneration`: Validates full report format and content
  - `TestSummaryReportGeneration`: Validates summary report format
  - `TestCompactReportGeneration`: Validates compact report format
  - `TestArcComparison`: Verifies side-by-side comparison works correctly
  - `TestEnergyChartGeneration`: Verifies ASCII chart rendering
  - `TestReportsWithNeutralTension`: Verifies reports work with minimal/neutral data
- All tests pass and build successful
- Diagnostics are deterministic, non-invasive, and produce human-readable output
- See implementation summary for full details

---
 
## Stage 8 — Phrase map + arrangement interaction (make Stage 7 usable by future melody/motifs)

**Why now:** Stage 7 introduced energy/tension intent. Stage 8 turns that intent into a *time grid of musical meaning* (phrases, peaks, cadences) and adds cross-role interaction rules, so later melody/motif decisions can be made safely.

### Story 8.1 — `PhraseMap` (within-section) as a first-class model

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

### Story 8.2 — `SectionArc` (micro-energy within section) derived from Stage 7 + `PhraseMap`

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

### Story 8.3 — Cross-role constraints + “density budget” engine

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

### Story 8.4 — Expose a unified query contract for later stages

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

## Stage 9 — Motifs & hooks as first-class objects (riff writing + repetition with controlled evolution)

**Why now:** once phrase map + constraints exist, motifs can be placed repeatably and safely without clashing.

### Story 9.1 — `MotifSpec` model (role-aware, repeatable)

**Acceptance criteria:**
- Create immutable `MotifSpec` with:
  - `MotifId` (stable string)
  - `Role` (Lead, GuitarHook, SynthHook, etc.)
  - `TargetSectionSelectors` (e.g., Chorus only, Intro optional)
  - `RhythmShape` defined in terms of onset slot indices within bar
  - `ContourIntent` (Up, Down, Arch, Flat, ZigZag)
  - `RegisterIntent` (center + range)
  - `TonePolicy`:
    - chord-tone bias per beat-strength
    - passing tone policy (diatonic only initially)
  - `RepetitionPolicy`:
    - exact repeat vs variation intensity for A/A’
- Add serialization-friendly design (records + enums).

### Story 9.2 — `MotifPlacementPlanner` (where motifs appear)

**Intent:** motifs should be intentional: chorus hook, intro answer, pre-chorus lift.

**Acceptance criteria:**
- Deterministically place motifs using inputs:
  - song form, section types, Stage 7 energy/tension, `PhraseMap`
- MVP placement heuristics:
  - chorus: primary motif almost always
  - intro: optional motif teaser if energy low and arrangement sparse
  - pre-chorus: motif fragment or rhythmic foreshadowing
  - bridge: either new motif or transformed motif
- Provide deterministic collision checks:
  - do not place motif when orchestration says role absent
  - do not place motif if register reservation would be violated by required accompaniment
- Output: `MotifPlacementPlan` referencing `MotifSpec` + time windows.

### Story 9.3 — `MotifRenderer` (notes from motif spec + harmony)

**Acceptance criteria:**
- Render motif notes against:
  - onset grid
  - harmony context at each slot
  - Stage 7/8 intent context (energy/tension + phrase position)
- MVP pitch rules:
  - strong beats prefer chord tones
  - weak beats allow diatonic passing tones (policy-gated)
  - apply register intent + clamp to instrument range
- Add deterministic “variation operators”:
  - octave displacement (bounded)
  - neighbor-tone ornaments (diatonic)
  - rhythmic displacement by one slot where safe
  - small contour-preserving pitch adjustments

### Story 9.4 — Motif integration with accompaniment (call/response + ducking hooks)

**Intent:** when a motif occurs, accompaniment should *make room*.

**Acceptance criteria:**
- Add a `MotifPresenceMap` query per `(section, bar, slot)`.
- Wire minimal “ducking”:
  - comp/keys reduce density under motif windows
  - pads shorten sustain at motif onsets
  - drums reduce optional hats/ghosts slightly under dense motif windows (style-safe)
- Must preserve groove anchors and harmony integrity.

### Story 9.5 — Motif diagnostics

**Acceptance criteria:**
- Opt-in reports:
  - motif placements by section
  - per-motif variation ops used
  - collision/ducking decisions
- Ensure diagnostics do not affect generation.

---

## Stage 10 — Melody & lyric scaffolding (timing windows + singable melody MVP)

**Why now:** motifs can exist without lyrics, but “song-like” output needs either a lead melody or a vocal-friendly proxy. This stage builds the minimal melody engine with future lyric integration in mind.

### Story 10.1 — `LyricProsodyModel` (inputs and constraints; no real lyrics yet)

**Intent:** provide syllable timing/stress windows even if lyrics are placeholder.

**Acceptance criteria:**
- Add a model for phrase-level syllable planning:
  - syllable count per phrase
  - stress pattern (simple: Strong/Weak or 0/1)
  - allowed melisma count (initially low)
- Deterministically generate a default prosody plan when the user provides none.

### Story 10.2 — Syllable windows ? onset slot mapping

**Acceptance criteria:**
- Map syllables onto onset slots within a phrase using deterministic rules:
  - stressed syllables prefer stronger beats/slots
  - avoid impossible densities (cap syllables per bar)
  - allow rests intentionally
- Output: `VocalTimingPlan` that can be queried by `(section, bar, slot)`.

### Story 10.3 — Melody generator MVP (singable, chord-aware)

**Acceptance criteria:**
- Generate pitch for each syllable event:
  - strong beats: chord tones
  - weak beats: allow diatonic passing/neighbor tones (policy-gated)
  - respect range and tessitura
  - avoid large leaps unless phrase position suggests emphasis (`Peak`)
- Incorporate tension intent:
  - higher tension biases scale degrees away from tonic (style-appropriate)
  - release moments return toward stable tones
- Determinism preserved.

### Story 10.4 — Vocal band protection (make room for melody)

**Acceptance criteria:**
- Define a `VocalBand` (register range) per style/voice.
- When `VocalTimingPlan` indicates active syllables:
  - pads/keys avoid sustained notes in vocal band
  - comp reduces density and/or shifts inversion/register away
  - optionally reduce drum busyness (only optional events)

### Story 10.5 — Melody variation across repeats (A/A’)

**Acceptance criteria:**
- Support controlled variation for repeated sections:
  - Verse 2 melody = Verse 1 melody + bounded variation ops
  - Final chorus can lift register or add melodic extensions
- Variation ops remain deterministic and constrained.

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
