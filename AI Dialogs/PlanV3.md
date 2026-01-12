# Music Generator Plan (Stage 8+)

This plan assumes **Stage 7 is complete** and focuses on what comes next to reach the goal: **human-like, creative, unique music generation** driven by inputs + one or more seeds, while preserving strict determinism where required.

Design principles retained:
- **Determinism-first:** given `(seed(s), inputs, song structure)` outputs are repeatable. Use randomness only as deterministic tie-break among valid options.
- **Safety rails:** avoid muddiness, preserve lead space, protect groove anchors, clamp ranges.
- **Separation of concerns:** planning produces intent + constraints; renderers realize them.
- **Explainability:** every major planner can emit diagnostics without affecting generation.

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
