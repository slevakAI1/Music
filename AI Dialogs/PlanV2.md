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

#### Story 7.4.5 — Integration testing and validation - NOT STARTED

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

### Story 7.5 — Tension planning hooks (macro vs micro) feeding later stages

**Intent:** incorporate the tension/energy distinction so later stages can create anticipation and release.

**Acceptance criteria:**
- Add optional `TensionTarget` to `SectionEnergyProfile` (or a sibling `SectionTensionProfile`).
- Provide two levels:
  - **Macro-tension:** section transitions (pre-chorus builds, breakdowns, drop/chorus impacts)
  - **Micro-tension:** within phrases (end-of-phrase kick dropout, small fills, impacts)
- For now, Stage 7 should only:
  - compute tension intent and expose hooks/parameters
  - allow Stage 6 drums and existing roles to access it to bias small variations

**Implementation-aligned examples:**
- At phrase ends, increase probability of "pull" events (short fill, kick removal, accent) when `TensionTarget` is high.
- At section transitions into high-energy sections, prefer orchestration "additions" and register lift.

---

### Story 7.6 — Structured repetition engine (A / A' / B transforms)

**Intent:** reuse section "core decisions" while making later repeats evolve in a controlled, musical way.

**Acceptance criteria:**
- Introduce a deterministic `SectionVariationPlan` per section instance:
  - `BaseReferenceSectionIndex` (Verse 2 references Verse 1)
  - per-role transform magnitudes, bounded
- Transforms must be driven by:
  - energy arc position (later repeats often slightly higher energy)
  - phrase position (micro-arcs)
- Examples of bounded transforms:
  - Drums: slightly more hat openness / extra ghost candidates / higher fill probability near transitions.
  - Bass: add occasional octave/approach notes at phrase peaks.
  - Comp: add anticipations or an extra fragment hit on strong beats.
  - Keys/Pads: small density increase (+1 note) or slight register lift.

---

### Story 7.7 — Phrase-level shaping inside sections (energy micro-arcs)

**Intent:** energy should modulate within a section (start → build → peak → cadence), not be flat for 8 bars.

**Acceptance criteria:**
- Compute a deterministic per-section phrase map:
  - phrase length defaults (4 or 8 bars) derived from section length
  - per-bar position type: `Start`, `Middle`, `Peak`, `Cadence`
- Apply subtle per-bar modulation:
  - velocity lift at `Peak`
  - slight density thinning at `Cadence`
  - optional orchestration accents at `Start`/`Peak`

---

### Story 7.8 — Role interaction rules (prevent clutter; reserve melody/lead space)

**Intent:** higher energy often means more parts; without explicit rules, arrangements become muddy.

**Acceptance criteria:**
- Add cross-role constraints:
  - **Lead space reservation:** define a MIDI band reserved for future lead/vocal; non-lead roles avoid dense sustained notes there.
  - **Low-end management:** prevent pads/keys/comp from occupying bass register.
  - **Density budgets:** when multiple roles are busy on the same weak slots, deterministically thin one role.

---

### Story 7.9 — Diagnostics & explainability hooks

**Intent:** Stage 7 becomes the backbone for later creative complexity; we need visibility into decisions.

**Acceptance criteria:**
- Implement opt-in diagnostics:
  - dump chosen `EnergyArc`
  - dump derived `SectionEnergyProfile` per section
  - dump `SectionVariationPlan`
  - summarize realized densities + average velocities per role per section
- Must remain deterministic and must not affect generation.

---

### Story 7.10 — Stage 8/9 integration contracts (future-proofing)

**Intent:** ensure later stages can "ask" Stage 7 for arrangement support to convey emotion without rewriting Stage 7.

**Acceptance criteria:**
- Motifs (Stage 8) can query:
  - energy/tension targets
  - phrase positions (Peak/Cadence)
  - register intent for lead space
- Melody/lyrics (Stage 9) can request:
  - arrangement ducking (reduce comp/pads density under vocal phrases)
  - shift pads/keys register away from vocal band
- Add placeholders in data models (not full behaviors) if needed.

---

## Stage 8 — Motifs / hooks as first-class objects (only after accompaniment behaves)

**Why now:** once accompaniment is credible, motifs actually land.

### Story 8.1 — Motif model + placement policy

**Acceptance criteria:**
- Add `Motif`:
  - `Role` (lead OR riff role like guitar hook)
  - `TargetSections`
  - `RhythmShape` (derived from onset slots)
  - `ContourIntent` (up/down/arch)
  - `Constraints` (range, chord-tone bias)
- Placement: chorus hook; intro riff optional depending on style.

### Story 8.2 — Motif renderer with variation

**Acceptance criteria:**
- Render motif against:
  - `OnsetGrid`
  - harmony at `(bar, beat)`
  - section energy profile
- Repeat recognizably; variation is operator-driven (Stage 7), not note roulette.

---

## Stage 9 — Melody + lyric integration (optional major milestone after accompaniment)

**Why now:** you already have syllable/phonetic infrastructure; melody requires rhythmic windows.

### Story 9.1 — Syllable timing windows → onset slot mapping

**Acceptance criteria:**
- Convert syllable count/stress into a set of candidate onset slots per phrase.
- Support "stretch" (melisma) only if there's space.

### Story 9.2 — Melody generator MVP

**Acceptance criteria:**
- Strong beats: chord tones
- Weak beats: controlled passing tones (policy gated)
- Range driven by voice profile

### Story 9.3 — Arrangement collision avoidance

**Acceptance criteria:**
- When melody exists:
  - comp simplifies / drops density under vocal phrases
  - pads move away in register
  - bass avoids too much rhythmic density under dense lyric phrases

---

## Bottom line

- **Yes:** keep improving `Generator` incrementally.
- **No:** don't build "hooks/riffs system" first; it will be premature and you'll rewrite it.
- Use sections now to drive identity and variation; otherwise you're just generating a loop.
- "Surprise" should come from structured transforms and section contrast, with RNG only as a tie-breaker among valid options.
