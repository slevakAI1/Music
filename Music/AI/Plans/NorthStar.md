# Music Generator — North Star Plan (Revised)

**Last Updated:** Based on current codebase state and human musician modeling research

---

## Product North Star Goal

Build a deterministic, user-controllable music generator that produces **original, fully-produced songs** that listeners **enjoy, remember, and replay**—to the point that, in blind listening, the output is **not reliably distinguishable** from songs written and performed by **top-tier human songwriters and musicians**.

### Success Means

- **Originality:** outputs are non-derivative at the song level (melody, motifs, arrangement, groove, phrasing), not just shuffled templates.
- **Stickiness:** songs create memorable hooks/motif and satisfying tension/release so a high percentage of listeners can recall the main idea after listening.
- **Human realism:** timing, dynamics, articulation, and instrument behavior sound like expert players, not quantized MIDI.
- **Songwriter-level structure:** clear section identity, repetition with purposeful variation, effective builds/drops, and strong cadences.
- **Control + automation range:** the system supports both:
  - **High-level generation** from a few controls (genre/style, mood, tempo, seed(s), energy arc), and
  - **Fine-grained authoring** where the user can lock/override chords, form, motifs, melodic phrases, or even exact events—while retaining coherent musical results.
- **Iterative creativity workflow:** users can generate alternatives, audition, accept/lock, and refine parts (motifs, melody phrases, riffs, drum fills, voicings) without losing reproducibility.

### Engineering Constraints That Must Always Hold

- **Determinism & reproducibility:** same inputs + seed(s) must reproduce the same song, and localized regeneration must not unintentionally change locked/accepted material.
- **Guardrails & musical safety:** the system must enforce register management, role separation, groove anchors, and anti-mud rules so added complexity improves musicality rather than breaking it.
- **Measurable improvement path:** every stage must move at least one of these needles: **memorability**, **human realism**, **section identity**, **variation**, **user control**, or **objective musicality metrics**.

### Definition of "Done" (Long-Term)

In blinded tests across target genres, the generator's songs achieve **listener preference and recall rates** that meet or exceed strong human baselines, while maintaining **originality**, **realism**, and **user-directed control**.

---

## Core Architectural Paradigm: Expert Musician Agents

**Key Insight:** A skilled human musician can be modeled without massive datasets by using:

1. **Policy + Operators + Constraints + State + Selection**
   - **Operators:** Procedural candidate generators representing musical "moves" (not frozen patterns)
   - **Policy:** Decision rules for when/how to apply operators (context-aware, style-gated)
   - **Constraints:** Playability, idiom, and mix-clarity guardrails
   - **State:** Memory of recent decisions (anti-repetition, phrase awareness, section identity)
   - **Selection:** Score-based picking with deterministic tie-breaks and caps

2. **Layered Architecture**
   - **Intent Layer:** Energy, tension, section arc, motif presence, "what the music needs now"
   - **Groove/Rhythm Layer:** Abstract event planning (when/where density increases, punctuation windows)
   - **Instrument Agent Layer:** Concrete realization (which hits, pitches, articulations + how they're played)

3. **Instrument-Specific Realities**
   - **Drums:** Limbs, kit pieces, density/texture, fills, crash language
   - **Guitar:** Fretboard feasibility, voicing, strumming, comping idioms
   - **Keys:** Hand span, voice-leading, pedal behavior, split voicing
   - **Bass:** Register, lock with kick, approach notes, groove lock
   - **Vocals:** Tessitura, breath, prosody, syllable timing, phrasing

This paradigm applies to **every instrument** with the same structural approach but different operator families and constraints.

---

## Completed Work (Reference: History.md)

The following stages are implemented and production-ready:

| Stage | Description | Status |
|-------|-------------|--------|
| 1-2 | Foundation (MIDI, timing, sections, harmony data) | ✅ Complete |
| 3 | Harmony sounds connected (keys/pads voice-leading) | ✅ Complete |
| 4 | Comp becomes comp (multi-note chord fragments) | ✅ Complete |
| 5 | Bassline writing (groove-locked + harmony-aware) | ✅ Complete |
| 6 | Drums (template → performance) | ✅ Complete |
| 7 | Energy, Tension, Section Identity System | ✅ Complete |
| 8.0 | Audibility Pass (Comp + Keys behavior system) | ✅ Complete |
| M1 | Material fragments data definitions | ✅ Complete |

See `History.md` for detailed implementation notes on each completed stage.

---

## Stage G — Groove System Completion (COMPLETE)

**Reference:** `Completed/Epic_Groove.md` for full implementation details

**Goal:** Finish the groove system (selection + constraints + velocity + timing + overrides + diagnostics + tests) with hooks ready for human musician agent models.

**All Phases Complete:**
- Phase A (Prep): ✅ Complete (A1, A2, A3) — Output contracts, RNG streams, policy hooks
- Phase B (Variation Engine): ✅ Complete (B1, B2, B3, B4) — Layer merge, filtering, weighted selection
- Phase C (Density & Caps): ✅ Complete (C1, C2, C3) — Density targets, selection, hard caps
- Phase D (Onset Strength + Velocity): ✅ Complete (D1, D2) — Strength classification, velocity shaping
- Phase E (Timing & Feel): ✅ Complete (E1, E2) — Feel timing, role timing bias
- Phase F (Override Merge Policy): ✅ Complete (F1) — Policy enforcement for segment overrides
- Phase G (Diagnostics): ✅ Complete (G1, G2) — Decision trace, provenance tracking
- Phase H (Test Suite): ✅ Complete (H1, H2) — Unit tests, golden regression test

**Key Deliverables:**
- `IGroovePolicyProvider` and `IGrooveCandidateSource` hooks ready for agent use
- `FeelTimingEngine` with straight/swing/shuffle/triplet support
- `OverrideMergePolicyEnforcer` for segment override control
- `GrooveBarDiagnostics` for decision tracing
- All tests passing (200+ groove-related tests)

---

## Stage 8 — Material Motifs: Data Definitions (COMPLETE)

**Reference:** Story M2 in `NorthStarPlan.md` (original)

**Goal:** Establish motifs as first-class material objects stored in `MaterialBank`, ready for Stage 9 placement/rendering.

**Completed Stories:**
- 8.1: `MotifSpec` model (immutable, material-aware) ✅
- 8.2: Motif storage and retrieval in `MaterialBank` ✅
- 8.3: Hardcoded test motifs (popular patterns) ✅
- 8.4: Motif validation helpers ✅
- 8.5: Motif definition tests and MaterialBank integration ✅

---

## Stage 9 — Motif placement and rendering (where and how motifs appear)  (in progress)

**Why now:** Stage 8 established motifs as material objects. Stage 9 makes them musically functional by placing them in the song structure and rendering them into actual note sequences.

**Goal:** Deterministically place motifs in appropriate sections, render them against harmony and groove, and integrate them with accompaniment.

---

### Story 9.1 — `MotifPlacementPlanner` (where motifs appear)   (completed)

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

### Story 9.2 — `MotifRenderer` (notes from motif spec + harmony)  (COMPLETED)

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

### Story 9.3 — Motif integration with accompaniment (call/response + ducking infrastructure) (COMPLETED)

**Intent:** When a motif occurs, accompaniment should *make room* without destroying the groove. Build infrastructure for motif-aware coordination and integrate with DrummerAgent (Stage 10).

**Acceptance criteria:**

**Part A: MotifPresenceMap (shared query service)**
- Create `MotifPresenceMap` class in `Generator/Material/`:
  - Constructor takes `MotifPlacementPlan` and `BarTrack`
  - Query methods:
    - `IsMotifActive(int barNumber, string? role = null) → bool` — any motif active in bar (optionally filtered by role)
    - `GetMotifDensity(int barNumber, string? role = null) → double` — estimated density [0.0-1.0] from motif note density
    - `GetActiveMotifs(int barNumber) → IReadOnlyList<MotifPlacement>` — which motifs are active
  - Deterministic: same placement plan → same query results
  - Register-aware: can filter by role when querying (e.g., "Lead", "Guitar")

**Part B: DrummerAgent integration (NOW - Stage 10 active)**
- Update `DrummerPolicyProvider` to accept optional `MotifPresenceMap`:
  - When motif is active in current bar: reduce density target by 10-15%
  - Add "MotifPresent" to EnabledVariationTagsOverride when motif active
- Update relevant drum operators to query motif presence in `CanApply()` or `Score()`:
  - **GhostClusterOperator**: reduce score by 50% when motif active (avoid clutter)
  - **HatEmbellishmentOperator**: reduce score by 30% when motif active
  - **GhostBeforeBackbeatOperator** / **GhostAfterBackbeatOperator**: reduce score by 20% when motif active
  - Operators receive `MotifPresenceMap` via `DrummerContext` (extend context with optional field)
- Ducking rules:
  - **Never affect**: kick/snare anchors, backbeats, downbeats (groove-protective)
  - **Affect only**: optional embellishments (ghosts, clusters, hat fills)
  - **Bounded**: max 20% density reduction in any bar
- Tests:
  - Determinism: same motif placement → same ducking decisions
  - Groove anchors preserved (kick/snare/backbeats unaffected)
  - Density reduction stays within 20% bound
  - Operators correctly query motif presence

**Part C: Infrastructure hooks for future agents (Stages 11-13)**
- Add `MotifPresenceMap?` field to `AgentContext` (base class for all agents):
  - DrummerContext inherits this field
  - GuitarContext / KeysContext / BassContext will inherit this field (future stages)
- Document pattern in `ProjectArchitecture.md`:
  - All instrument agents can query motif presence via context
  - Policy providers should reduce density when motifs active
  - Operators should adjust scores/eligibility based on motif presence
  - Suggested reductions:
    - Drums: 10-20% density reduction (only optional events)
    - Comp/Keys: 20-30% density reduction (future Stage 12)
    - Bass: minimal reduction, mainly register shifts (future Stage 13)

**Part D: Testing**
- Unit tests for `MotifPresenceMap`:
  - Query correctness (IsMotifActive, GetMotifDensity)
  - Role filtering (motif in "Lead" doesn't affect "Bass" queries)
  - Edge cases (empty plan, no motifs in bar, multiple motifs)
- Integration tests for DrummerAgent:
  - Determinism: same motif placement + seed → identical drum track
  - Groove protection: anchors never removed by ducking
  - Density bounds: verify 20% max reduction
  - Operator scoring: ghosts/embellishments score lower when motif present
- Snapshot test:
  - Generate drums with and without motif presence
  - Verify reduced optional events (ghosts/clusters) when motif active
  - Verify identical anchors in both cases

**Notes:**
- **Ducking is a BIAS, not a hard rule** — operators still generate candidates; selection engine applies bias
- **Scope limited to DrummerAgent NOW** — Comp/Keys/Bass integration happens in their respective stages
- **Infrastructure complete for future agents** — pattern is established and documented
- **Integration feels natural** — accompaniment thins slightly, doesn't stop
- **No new generator complexity** — uses existing operator/policy architecture

**Dependencies:**
- Stage 9 Stories 9.1-9.2 (motif placement and rendering) ✅
- Stage 10 Stories 2.1-2.3 (DrummerContext, DrummerPolicyProvider) ✅
- Stage 10 Story 3.1 (MicroAddition operators) ✅

**Files to create:**
- `Generator/Material/MotifPresenceMap.cs`
- `Music.Tests/Generator/Material/MotifPresenceMapTests.cs`
- `Music.Tests/Generator/Agents/Drums/DrummerMotifIntegrationTests.cs`

**Files to modify:**
- `Generator/Agents/Common/AgentContext.cs` (add optional `MotifPresenceMap?` field)
- `Generator/Agents/Drums/DrummerContext.cs` (inherit MotifPresenceMap field)
- `Generator/Agents/Drums/DrummerPolicyProvider.cs` (density reduction when motif active)
- `Generator/Agents/Drums/Operators/MicroAddition/*.cs` (score reductions based on motif presence)
- `Music/AI/Plans/ProjectArchitecture.md` (document motif coordination pattern)

---

### Story 9.4 — Motif diagnostics (PENDING COMPLETION OF STAGE 10)

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

**Completed Stories:**
- 9.1: `MotifPlacementPlanner` (where motifs appear) ✅
- 9.2: `MotifRenderer` (notes from motif spec + harmony) ✅ — 22 passing tests

**Pending Stories:**
- 9.3: Motif integration with accompaniment (call/response + ducking hooks)
- 9.4: Motif diagnostics

**Dependencies:** Stage G (groove hooks) ✅, Stage 8 (motif data) ✅

---

NOTE - STAGE 10 IS FULLY DETAILED IN: `AI/Plans/CurrentEpic_HumanDrummer.md`

## Stage 10 — Human Drummer Agent (Pop/Rock) (IN PROGRESS)

**Why:** The groove system provides the framework; this stage implements a realistic drummer that makes musical decisions like a human.

**Reference:** `AI/Plans/CurrentEpic_HumanDrummer.md` for full story breakdown with acceptance criteria.

### Core Concept: `DrummerAgent` With Priorities

A skilled drummer optimizes for:
1. **Timekeeping is sacred** (anchors rarely change)
2. **Backbeat identity** stays consistent
3. **Energy arc** changes density + orchestration (hat lift, crashes, ghosts)
4. **Phrase boundaries** get punctuation (turnarounds/fills)
5. **Variation** avoids repetition, but stays in style
6. **Hands/feet constraints** avoid physically absurd patterns

### Progress Summary

**Stage 1 — Shared Agent Infrastructure: ✅ COMPLETE**
- 1.1: Common agent contracts (`IMusicalOperator`, `AgentContext`, `IAgentMemory`, `OperatorFamily`) ✅
- 1.2: Agent memory with anti-repetition (`AgentMemory`, `FillShape`) ✅
- 1.3: Operator selection engine (`OperatorSelectionEngine`) ✅
- 1.4: Style configuration model (`StyleConfiguration`, `StyleConfigurationLibrary`) ✅

**Stage 2 — Drummer Agent Core: ✅ COMPLETE**
- 2.1: Drummer-specific context (`DrummerContext`, `DrummerContextBuilder`) ✅
- 2.2: Drum candidate type (`DrumCandidate`, `DrumArticulation`, `FillRole`) ✅
- 2.3: Drummer policy provider (`DrummerPolicyProvider` : `IGroovePolicyProvider`) ✅
- 2.4: Drummer candidate source (`DrummerCandidateSource` : `IGrooveCandidateSource`) ✅
- 2.5: Drummer memory (`DrummerMemory`) ✅

**Stage 3 — Drum Operators (28 Total): ✅ COMPLETE**
- 3.1: MicroAddition operators (7): ghost notes, kick pickups, embellishments ✅
- 3.2: SubdivisionTransform operators (5): hat lift/drop, ride swap, partial lift ✅
- 3.3: PhrasePunctuation operators (7): crash on 1, fills, setup hits, stop-time ✅
- 3.4: PatternSubstitution operators (4): backbeat variants, half/double-time ✅
- 3.5: StyleIdiom operators (5): Pop Rock specific patterns ✅
- 3.6: Operator registry and discovery (`DrumOperatorRegistry`) ✅

**Stage 4 — Physicality Constraints: ⏳ PENDING**
- 4.1: Limb model (which limb plays which role)
- 4.2: Sticking rules (max consecutive same-hand, ghost density limits)
- 4.3: Physicality filter (reject impossible patterns)
- 4.4: Overcrowding prevention (density caps at physicality level)

**Stage 5 — Pop Rock Style Configuration: ⏳ PENDING**
- 5.1: Operator weights (high/medium/low by musical relevance)
- 5.2: Density curves (section-aware targets)
- 5.3: Physicality rules (Pop Rock specific constraints)
- 5.4: Memory settings (anti-repetition tuning)

**Stage 6 — Performance Rendering: ⏳ PENDING**
- 6.1: Velocity shaper (role × strength dynamics)
- 6.2: Timing nuance (push/pull by role)
- 6.3: Articulation mapping (MIDI note variations)

**Stage 7 — Diagnostics & Tuning: ⏳ PENDING**
- 7.1: Drummer diagnostics collector (per-bar trace)
- 7.2: Benchmark feature extraction (density, syncopation, punctuation)

**Stage 8 — Integration & Testing: ⏳ PENDING**
- 8.1: Wire drummer agent into generator (`DrummerAgent` facade)
- 8.2: Unit tests (determinism, musical sensibility)
- 8.3: Golden regression snapshot

---
## Stage 11 — Melody & lyric scaffolding (timing windows + singable melody MVP)

**Why now:** Stage 9 provides instrumental motifs (hooks/riffs). Stage 10 adds vocal melody as a first-class feature, building on motif rendering infrastructure while adding lyric-aware timing constraints.

**Goal:** Build minimal melody engine with future lyric integration in mind. Melodies are like motifs but with syllable timing constraints and vocal register/tessitura considerations.

**Dependencies:**
- Stage 8 material system (melody specs can reuse `MotifSpec` patterns)
- Stage 9 rendering infrastructure (melody rendering parallels motif rendering)
- Stage 7 phrase maps and tension (melody placement uses phrase positions)

---

### Story 11.1 — `LyricProsodyModel` (inputs and constraints; no real lyrics yet)

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

### Story 11.2 — Syllable windows → onset slot mapping

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

### Story 11.3 — Melody generator MVP (singable, chord-aware)

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

### Story 11.4 — Vocal band protection (make room for melody)

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

### Story 11.5 — Melody variation across repeats (A/A')

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

## Stage 11 — Human Guitarist Agent

**Why:** Apply the same agent architecture to guitar, with fretboard-specific constraints and idioms.

### Core Concept: `GuitarAgent` With Guitar Realities

- **Fretboard feasibility:** string sets, max fret span, barre rules
- **Voicing constraints:** chord grips, impossible overlaps
- **Register management:** avoid stepping on melody
- **Idioms:** strumming, arpeggiation, palm mute, open vs closed voicings

### Story 11.1 — Guitar Operator Framework

**Acceptance criteria:**
- Create `IGuitarOperator` interface
- Implement operator families:
  - **Comping patterns:** downstrokes → syncopated → arpeggiated
  - **Subdivision transforms:** add/remove strum subdivisions by energy
  - **Voicing transforms:** open → closed → drop-2, register shift
  - **Phrase punctuation:** passing chord, approach chord (style-gated)
  - **Hook support:** double vocal hook rhythm (simplified pitches)
  - **Fill licks:** phrase-end fills (only if vocal inactive)

### Story 11.2 — Fretboard Feasibility Filter

**Acceptance criteria:**
- Fretboard model with string/fret constraints
- Validate chord grips (max span, barre feasibility)
- Reject impossible voicings
- Suggest alternatives when blocked

### Story 11.3 — Guitar Memory and Style Adaptation

**Acceptance criteria:**
- Track recent voicing choices (avoid repetition)
- Style profiles (acoustic folk vs electric rock vs jazz)
- Energy-driven behavior switching

### Story 11.4 — Guitar Performance Rendering

**Acceptance criteria:**
- Strum timing spread (direction feel)
- Muted strokes
- Velocity shaping by beat position
- Vibrato/bend hints (for future audio)

---

## Stage 12 — Human Keyboardist Agent

**Why:** Keys have unique hand span limits, voice-leading requirements, and pedal behavior.

### Core Concept: `KeysAgent` With Piano/Synth Realities

- **Hand span limits:** realistic chord grips
- **Voice-leading:** smooth motion, avoid awkward leaps
- **Pedal behavior:** sustain control, releases
- **Split voicing:** left hand bass vs right hand chord

### Story 12.1 — Keys Operator Framework

**Acceptance criteria:**
- Implement operator families:
  - **Comping:** shell voicings (3rd/7th) vs full voicings
  - **Left-hand patterns:** bass vs chord, Alberti bass, stride
  - **Extensions:** add upper extensions (9/13) in chorus
  - **Arpeggiation:** broken chord patterns
  - **Texture changes:** pad widen, piano thin

### Story 12.2 — Hand Span and Voice-Leading Filter

**Acceptance criteria:**
- Hand span limits (octave + 2 for most players)
- Voice-leading cost function (penalize large jumps)
- Avoid muddy low clusters

### Story 12.3 — Keys Memory and Role Coordination

**Acceptance criteria:**
- Track voicing register (avoid same register collision with comp)
- Coordinate with left-hand bass when bass role absent
- Section-aware behavior (verse sustain, chorus rhythmic)

### Story 12.4 — Keys Performance Rendering

**Acceptance criteria:**
- Slight asynchrony between hands
- Velocity shaping by phrase position
- Pedal behavior (sustain, half-pedal hints)

---

## Stage 13 — Human Bassist Agent

**Why:** Bass locks with drums and provides harmonic foundation with its own idioms.

### Core Concept: `BassAgent` With Bass Realities

- **Register clarity:** stay in bass register, don't compete with kick
- **Groove lock:** coordinate with kick pattern
- **Approach notes:** walk-ups, walk-downs, anticipations
- **Style idioms:** fingerstyle, pick, slap (future)

### Story 13.1 — Bass Operator Framework

**Acceptance criteria:**
- Implement operator families:
  - **Root anchors:** root on downbeats, octave variations
  - **Motion operators:** add approach tones, walk-up/walk-down
  - **Syncopation:** pickup before chord change, anticipation
  - **Energy scaling:** root-only → add 5ths → add syncopation
  - **Kick lock:** match kick pattern accents

### Story 13.2 — Bass Register and Collision Filter

**Acceptance criteria:**
- Stay below bass ceiling (MIDI ~60)
- Avoid collision with kick on same beats (unless intentional)
- Cap syncopation under dense vocals

### Story 13.3 — Bass Memory and Phrase Awareness

**Acceptance criteria:**
- Track approach note usage (don't overuse)
- Phrase-end pickups (when groove has valid slot)
- Section-aware behavior (verse simple, chorus fuller)

### Story 13.4 — Bass Performance Rendering

**Acceptance criteria:**
- Timing feel (slightly behind or on-grid by style)
- Velocity shaping (accents on root, softer on passing)
- Articulation hints (slide, staccato, ghost)

---

## Stage 14 — Human Vocalist/Lyricist Agent (Advanced)

**Why:** Vocals are the hardest because they involve prosody, tessitura, breath, and lyric intelligibility.

### Core Concept: `VocalAgent` With Vocal Realities

- **Tessitura:** comfortable range, avoid extremes
- **Breath:** natural phrase lengths, rest points
- **Prosody:** syllable stress ↔ rhythmic stress alignment
- **Intelligibility:** consonant/vowel timing, avoid pileups at fast tempos

### Story 14.1 — Vocal Operator Framework

**Acceptance criteria:**
- Implement operator families:
  - **Melodic contour:** stepwise, arch, drop, zigzag
  - **Target tones:** stressed syllables on chord tones
  - **Neighbor embellishments:** unstressed syllables get neighbors
  - **Rhythmic rewrites:** delay/anticipate phrase (preserve intelligibility)
  - **Phrase cadences:** rhyme-end shaping

### Story 14.2 — Prosody and Singability Filter

**Acceptance criteria:**
- Tessitura limits by vocal type
- Breath point requirements (max phrase length)
- Singable intervals (penalize awkward leaps)
- Consonant pileup detection at fast tempos
- Lyric intelligibility under busy accompaniment

### Story 14.3 — Vocal Memory and Identity

**Acceptance criteria:**
- Track melodic motifs (chorus hook identity)
- A/A' variation on repeated verses
- Call-and-response awareness

### Story 14.4 — Vocal Performance Rendering

**Acceptance criteria:**
- Microtiming for phrasing (ahead/behind by emphasis)
- Dynamics to emphasize meaning
- Vibrato hints (for future audio)
- Breath sounds at phrase boundaries

---

## Stage 15 — Cross-Role Coordination ("Band Brain")

**Why:** Individual agents sound good; a band sounds great when they listen to each other.

### Story 15.1 — Spotlight Manager

**Acceptance criteria:**
- Determine who "owns the spotlight" per section/phrase:
  - Vocal lead → accompaniment thins
  - Guitar solo → drums support, others sparse
  - Drum fill → everyone else holds
- Deterministic spotlight assignment by section type + energy + motif presence

### Story 15.2 — Register Collision Avoidance

**Acceptance criteria:**
- Cross-role register map per slot
- Automatic voicing shifts to avoid mud
- Priority order by role (vocal > lead > comp > bass)

### Story 15.3 — Density Budget Enforcement

**Acceptance criteria:**
- Global density budget per bar (energy-driven)
- Per-role allocation
- Deterministic thinning when budget exceeded

### Story 15.4 — Groove Lock (Kick/Bass/Comp Coordination)

**Acceptance criteria:**
- Kick-bass alignment policy
- Comp rhythm coordination with drums
- "Push" or "pull" feel consistency across roles

---

## Stage 16 — Harmonic Narrative

**Why:** Static chord progressions don't tell a story; tension-aware harmony choices create emotional arcs.

### Stories:
- 17.1: Harmonic function tagging (tonic/predominant/dominant)
- 17.2: Cadence planner at phrase ends
- 17.3: Pre-chorus lift & chorus release harmony policy
- 17.4: Borrowed chords + chromaticism (policy-gated)
- 17.5: Dominant pedal tension hook

**Dependencies:** Stage 11 (phrase maps), Stage 7 (tension intent)

---

## Stage 17 — Performance Rendering (Full Humanization)

**Why:** Even with good decisions, quantized MIDI sounds fake.

### Stories:
- 18.1: Micro-timing + velocity shaping for all roles
- 18.2: Articulation model per role
- 18.3: Sustain control + release tails
- 18.4: Pocket tightness/looseness by style

---

## Stage 18 — Sound/Render Pipeline + Export Quality

**Why:** More musical intelligence is only useful if export is reliable and debuggable.

### Stories:
- 19.1: Instrument/patch mapping profiles
- 19.2: MIDI export correctness & validation suite
- 19.3: Render diagnostics bundle
- 19.4: Audio render integration (future)

---

## Stage 19 — User Input Model + Constraints

**Why:** As capability grows, the system needs a clear input schema for control and reproduction.

### Stories:
- 20.1: `GenerationRequest` schema (versioned)
- 20.2: Constraint/guardrail configuration
- 20.3: Preset packs (style kits)
- 20.4: Lock/override/regenerate workflow

---

## Stage 20 — Musical Evaluation Loop

**Why:** Automated checks enable iterative improvement without manual listening.

### Stories:
- 21.1: Rule-based musicality metrics
- 21.2: "Regenerate with constraints" iteration API
- 21.3: A/B comparison tooling
- 21.4: Benchmark suite against human reference tracks

---

## Stage 21 — Optional ML/AI Augmentation

**Why later:** Only once the classic pipeline is stable and explainable should AI be introduced.

### Stories:
- 22.1: AI suggestion interface (non-authoritative)
- 22.2: Operator weight learning from examples
- 22.3: Style transfer hooks
- 22.4: Lyric generation integration

---

## Appendix A: Skill Levels (Conceptual)

Each instrument agent can be characterized by skill level:

| Level | Description |
|-------|-------------|
| 1 | **Clean timekeeping:** anchors + basic variations + simple fills + no illegal clutter |
| 2 | **Tasteful variation + restraint:** memory, anti-repetition, "sometimes do nothing" |
| 3 | **Stylistic vocabulary + phrasing:** section-aware choices; phrase builds/setups/releases |
| 4 | **Micro-performance realism:** velocity/timing nuance, accents, pocket |
| 5 | **Signature behavior:** recognizable tendencies; motifs; narrative across sections |

---

## Appendix B: Benchmark → Compare → Improve Loop

For each instrument agent, establish a repeatable benchmark loop:

### A) Extract Features from Human Tracks (prefer MIDI)
- Density curves
- Syncopation profile
- Punctuation rate (fills/crashes/setups)
- Velocity distributions by beat strength
- Timing offsets (microtiming feel)
- Repetition vs variation rates
- Motifs (recurring patterns + variations)

### B) Run Generator with Matched Context
- Same tempo, meter, section layout, energy arc

### C) Compare with Objective Deltas
- Chorus density change
- Ghost placement clustering
- Fill placement correctness
- Microtiming push/pull

### D) Map Gaps to Buckets
- **Operator gap:** missing a musical move
- **Policy gap:** wrong timing/frequency of moves
- **Constraint gap:** wrong allow/forbid rules
- **Performance gap:** timing/velocity/articulation issues

---

## Appendix C: Genre Overlap Analysis

From the drummer research, operator overlap varies by genre:

| Overlap Level | Examples |
|---------------|----------|
| **High (most genres)** | Conservative ghosts, pickups into boundaries, hat lift/drop, crash punctuation, simple turnaround fills |
| **Medium** | Shuffle/swing ghost placement, kick syncopation vocabulary, ride vs hat usage, half-time/double-time toggles |
| **Low (specialized)** | Trap hats (rolls/ratchets), drum & bass (break language), metal (double kick, blast), jazz (ride patterns + comping), reggaeton/dembow (signature placements) |

This analysis applies to other instruments too—most operators are shared across genres; specialization is in weights and constraints.

---

## Appendix D: Key Design Principles (Unchanging)

1. **Determinism first:** All systems deterministic by `(seed, song structure, groove, style)`
2. **Operator = musical move:** Not frozen patterns; parameterized by context
3. **Constraints are the craft:** Playability + idiom + mix clarity define realism
4. **Policy = timing/frequency:** When and how often to apply operators
5. **Memory = anti-repetition + identity:** Track recent decisions; avoid robotic loops
6. **Style = configuration:** Same operator interface; different weights/caps/idioms
7. **Non-invasive diagnostics:** All diagnostic systems read-only, deterministic
8. **Query pattern:** Stable APIs shield later stages from planner internals
9. **Backward compatible:** New features don't break existing generation
10. **Measurable improvement:** Every stage moves at least one needle

---

## Appendix E: File/Folder Map for Agent Code

Proposed organization:

```
Generator/
  Groove/           # Agnostic rhythm engine (Stage G)
  Agents/
    Drums/          # DrummerAgent (Stage 10)
    Guitar/         # GuitarAgent (Stage 11)
    Keys/           # KeysAgent (Stage 12)
    Bass/           # BassAgent (Stage 13)
    Vocal/          # VocalAgent (Stage 14)
    Coordination/   # BandBrain (Stage 15)
  Core/             # Shared generator infrastructure
Song/
  Material/         # Motifs, fragments, MaterialBank
  Energy/           # Stage 7 intent system
  Harmony/          # Chord realization, voice-leading
  Groove/           # Groove presets, onset grid
```

---

## Summary: Stage Dependency Graph

```
COMPLETED STAGES:
[1-2] → [3] → [4] → [5] → [6] → [7] → [8.0] → [M1]
                                                  ↓
[Stage G: Groove Completion] ✅ ←─────────────────┘
                      ↓
[8: Motif Data] ✅
                      ↓
[9: Motif Placement/Rendering] 🔄 (9.1-9.2 ✅, 9.3-9.4 pending)
                      ↓
[10: Drums] 🔄 (Stages 1-3 ✅, Stages 4-8 pending)
                      ↓
           [11: Melody Scaffolding]
                      ↓
    ┌───────────────────────────────────────┐
    ↓           ↓           ↓           ↓   ↓
  [11:      [12:        [13:        [14:
  Guitar]   Keys]       Bass]       Vocal]
    └───────────────────────────────────────┘
                      ↓
           [15: Cross-Role Coordination]
                      ↓
           [16: Harmonic Narrative]
                      ↓
           [17: Performance Rendering]
                      ↓
           [18: Export Quality]
                      ↓
           [19: User Input Model]
                      ↓
           [20: Evaluation Loop]
                      ↓
           [21: ML/AI Augmentation]
```

---

## Next Actions

**Recommended Path Forward:**

1. **Story 9.3** — Motif integration with accompaniment (ducking hooks)
   - Enables drums and other instruments to query motif presence
   - Small story, unlocks coordination between motifs and agents

2. **Continue CurrentEpic Stage 4** — Physicality Constraints (Stories 4.1-4.4)
   - Makes drum patterns physically realistic
   - Required before style configuration makes sense

3. **CurrentEpic Stage 5** — Pop Rock Style Configuration (Stories 5.1-5.4)
   - Tunes operator weights and density curves for Pop Rock
   - Completes the "musical intelligence" layer

4. **CurrentEpic Stage 6** — Performance Rendering (Stories 6.1-6.3)
   - Adds human-like velocity and timing nuance
   - Makes output sound realistic

5. **CurrentEpic Stages 7-8** — Diagnostics + Integration
   - Completes drummer agent implementation
   - Enables tuning and golden tests

6. **Story 9.4** — Motif diagnostics (after drummer integration)

**Rationale:** Story 9.3 first because it's small and enables motif-aware coordination in the drummer agent (crash on hook entries, ducking). Then complete the drummer epic sequentially.

---

*This plan supersedes the original `NorthStarPlan.md` and incorporates insights from `groove_human_drummer_session_notes.md` and current codebase state.*

*Last Updated:* Based on current codebase state (Stage G complete, Stage 10 Stages 1-3 complete)
