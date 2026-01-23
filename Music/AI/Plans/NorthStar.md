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

## Stage 9 — Motif Placement and Rendering (IN PROGRESS)

**Goal:** Deterministically place motifs in appropriate sections, render them against harmony and groove, and integrate them with accompaniment.

**Completed Stories:**
- 9.1: `MotifPlacementPlanner` (where motifs appear) ✅
- 9.2: `MotifRenderer` (notes from motif spec + harmony) ✅ — 22 passing tests

**Pending Stories:**
- 9.3: Motif integration with accompaniment (call/response + ducking hooks)
- 9.4: Motif diagnostics

**Dependencies:** Stage G (groove hooks) ✅, Stage 8 (motif data) ✅

---

## Stage 10 — Melody & Lyric Scaffolding (PENDING)

**Goal:** Build minimal melody engine with future lyric integration. Melodies are like motifs but with syllable timing constraints and vocal register/tessitura considerations.

**Stories:**
- 10.1: `LyricProsodyModel` (inputs and constraints; placeholder lyrics)
- 10.2: Syllable windows → onset slot mapping
- 10.3: Melody generator MVP (singable, chord-aware)
- 10.4: Vocal band protection (make room for melody)
- 10.5: Melody variation across repeats (A/A')

**Dependencies:** Stage 9 (rendering infrastructure), Stage 7 (phrase maps)

---


NOTE - STAGE 11 IS FULLY DETAILED IN: `AI/Plans/CurrentEpic_HumanDrummer.md`

## Stage 11 — Human Drummer Agent (Pop/Rock) (IN PROGRESS)

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

## Stage 12 — Human Guitarist Agent

**Why:** Apply the same agent architecture to guitar, with fretboard-specific constraints and idioms.

### Core Concept: `GuitarAgent` With Guitar Realities

- **Fretboard feasibility:** string sets, max fret span, barre rules
- **Voicing constraints:** chord grips, impossible overlaps
- **Register management:** avoid stepping on melody
- **Idioms:** strumming, arpeggiation, palm mute, open vs closed voicings

### Story 12.1 — Guitar Operator Framework

**Acceptance criteria:**
- Create `IGuitarOperator` interface
- Implement operator families:
  - **Comping patterns:** downstrokes → syncopated → arpeggiated
  - **Subdivision transforms:** add/remove strum subdivisions by energy
  - **Voicing transforms:** open → closed → drop-2, register shift
  - **Phrase punctuation:** passing chord, approach chord (style-gated)
  - **Hook support:** double vocal hook rhythm (simplified pitches)
  - **Fill licks:** phrase-end fills (only if vocal inactive)

### Story 12.2 — Fretboard Feasibility Filter

**Acceptance criteria:**
- Fretboard model with string/fret constraints
- Validate chord grips (max span, barre feasibility)
- Reject impossible voicings
- Suggest alternatives when blocked

### Story 12.3 — Guitar Memory and Style Adaptation

**Acceptance criteria:**
- Track recent voicing choices (avoid repetition)
- Style profiles (acoustic folk vs electric rock vs jazz)
- Energy-driven behavior switching

### Story 12.4 — Guitar Performance Rendering

**Acceptance criteria:**
- Strum timing spread (direction feel)
- Muted strokes
- Velocity shaping by beat position
- Vibrato/bend hints (for future audio)

---

## Stage 13 — Human Keyboardist Agent

**Why:** Keys have unique hand span limits, voice-leading requirements, and pedal behavior.

### Core Concept: `KeysAgent` With Piano/Synth Realities

- **Hand span limits:** realistic chord grips
- **Voice-leading:** smooth motion, avoid awkward leaps
- **Pedal behavior:** sustain control, releases
- **Split voicing:** left hand bass vs right hand chord

### Story 13.1 — Keys Operator Framework

**Acceptance criteria:**
- Implement operator families:
  - **Comping:** shell voicings (3rd/7th) vs full voicings
  - **Left-hand patterns:** bass vs chord, Alberti bass, stride
  - **Extensions:** add upper extensions (9/13) in chorus
  - **Arpeggiation:** broken chord patterns
  - **Texture changes:** pad widen, piano thin

### Story 13.2 — Hand Span and Voice-Leading Filter

**Acceptance criteria:**
- Hand span limits (octave + 2 for most players)
- Voice-leading cost function (penalize large jumps)
- Avoid muddy low clusters

### Story 13.3 — Keys Memory and Role Coordination

**Acceptance criteria:**
- Track voicing register (avoid same register collision with comp)
- Coordinate with left-hand bass when bass role absent
- Section-aware behavior (verse sustain, chorus rhythmic)

### Story 13.4 — Keys Performance Rendering

**Acceptance criteria:**
- Slight asynchrony between hands
- Velocity shaping by phrase position
- Pedal behavior (sustain, half-pedal hints)

---

## Stage 14 — Human Bassist Agent

**Why:** Bass locks with drums and provides harmonic foundation with its own idioms.

### Core Concept: `BassAgent` With Bass Realities

- **Register clarity:** stay in bass register, don't compete with kick
- **Groove lock:** coordinate with kick pattern
- **Approach notes:** walk-ups, walk-downs, anticipations
- **Style idioms:** fingerstyle, pick, slap (future)

### Story 14.1 — Bass Operator Framework

**Acceptance criteria:**
- Implement operator families:
  - **Root anchors:** root on downbeats, octave variations
  - **Motion operators:** add approach tones, walk-up/walk-down
  - **Syncopation:** pickup before chord change, anticipation
  - **Energy scaling:** root-only → add 5ths → add syncopation
  - **Kick lock:** match kick pattern accents

### Story 14.2 — Bass Register and Collision Filter

**Acceptance criteria:**
- Stay below bass ceiling (MIDI ~60)
- Avoid collision with kick on same beats (unless intentional)
- Cap syncopation under dense vocals

### Story 14.3 — Bass Memory and Phrase Awareness

**Acceptance criteria:**
- Track approach note usage (don't overuse)
- Phrase-end pickups (when groove has valid slot)
- Section-aware behavior (verse simple, chorus fuller)

### Story 14.4 — Bass Performance Rendering

**Acceptance criteria:**
- Timing feel (slightly behind or on-grid by style)
- Velocity shaping (accents on root, softer on passing)
- Articulation hints (slide, staccato, ghost)

---

## Stage 15 — Human Vocalist/Lyricist Agent (Advanced)

**Why:** Vocals are the hardest because they involve prosody, tessitura, breath, and lyric intelligibility.

### Core Concept: `VocalAgent` With Vocal Realities

- **Tessitura:** comfortable range, avoid extremes
- **Breath:** natural phrase lengths, rest points
- **Prosody:** syllable stress ↔ rhythmic stress alignment
- **Intelligibility:** consonant/vowel timing, avoid pileups at fast tempos

### Story 15.1 — Vocal Operator Framework

**Acceptance criteria:**
- Implement operator families:
  - **Melodic contour:** stepwise, arch, drop, zigzag
  - **Target tones:** stressed syllables on chord tones
  - **Neighbor embellishments:** unstressed syllables get neighbors
  - **Rhythmic rewrites:** delay/anticipate phrase (preserve intelligibility)
  - **Phrase cadences:** rhyme-end shaping

### Story 15.2 — Prosody and Singability Filter

**Acceptance criteria:**
- Tessitura limits by vocal type
- Breath point requirements (max phrase length)
- Singable intervals (penalize awkward leaps)
- Consonant pileup detection at fast tempos
- Lyric intelligibility under busy accompaniment

### Story 15.3 — Vocal Memory and Identity

**Acceptance criteria:**
- Track melodic motifs (chorus hook identity)
- A/A' variation on repeated verses
- Call-and-response awareness

### Story 15.4 — Vocal Performance Rendering

**Acceptance criteria:**
- Microtiming for phrasing (ahead/behind by emphasis)
- Dynamics to emphasize meaning
- Vibrato hints (for future audio)
- Breath sounds at phrase boundaries

---

## Stage 16 — Cross-Role Coordination ("Band Brain")

**Why:** Individual agents sound good; a band sounds great when they listen to each other.

### Story 16.1 — Spotlight Manager

**Acceptance criteria:**
- Determine who "owns the spotlight" per section/phrase:
  - Vocal lead → accompaniment thins
  - Guitar solo → drums support, others sparse
  - Drum fill → everyone else holds
- Deterministic spotlight assignment by section type + energy + motif presence

### Story 16.2 — Register Collision Avoidance

**Acceptance criteria:**
- Cross-role register map per slot
- Automatic voicing shifts to avoid mud
- Priority order by role (vocal > lead > comp > bass)

### Story 16.3 — Density Budget Enforcement

**Acceptance criteria:**
- Global density budget per bar (energy-driven)
- Per-role allocation
- Deterministic thinning when budget exceeded

### Story 16.4 — Groove Lock (Kick/Bass/Comp Coordination)

**Acceptance criteria:**
- Kick-bass alignment policy
- Comp rhythm coordination with drums
- "Push" or "pull" feel consistency across roles

---

## Stage 17 — Harmonic Narrative

**Why:** Static chord progressions don't tell a story; tension-aware harmony choices create emotional arcs.

### Stories:
- 17.1: Harmonic function tagging (tonic/predominant/dominant)
- 17.2: Cadence planner at phrase ends
- 17.3: Pre-chorus lift & chorus release harmony policy
- 17.4: Borrowed chords + chromaticism (policy-gated)
- 17.5: Dominant pedal tension hook

**Dependencies:** Stage 10 (phrase maps), Stage 7 (tension intent)

---

## Stage 18 — Performance Rendering (Full Humanization)

**Why:** Even with good decisions, quantized MIDI sounds fake.

### Stories:
- 18.1: Micro-timing + velocity shaping for all roles
- 18.2: Articulation model per role
- 18.3: Sustain control + release tails
- 18.4: Pocket tightness/looseness by style

---

## Stage 19 — Sound/Render Pipeline + Export Quality

**Why:** More musical intelligence is only useful if export is reliable and debuggable.

### Stories:
- 19.1: Instrument/patch mapping profiles
- 19.2: MIDI export correctness & validation suite
- 19.3: Render diagnostics bundle
- 19.4: Audio render integration (future)

---

## Stage 20 — User Input Model + Constraints

**Why:** As capability grows, the system needs a clear input schema for control and reproduction.

### Stories:
- 20.1: `GenerationRequest` schema (versioned)
- 20.2: Constraint/guardrail configuration
- 20.3: Preset packs (style kits)
- 20.4: Lock/override/regenerate workflow

---

## Stage 21 — Musical Evaluation Loop

**Why:** Automated checks enable iterative improvement without manual listening.

### Stories:
- 21.1: Rule-based musicality metrics
- 21.2: "Regenerate with constraints" iteration API
- 21.3: A/B comparison tooling
- 21.4: Benchmark suite against human reference tracks

---

## Stage 22 — Optional ML/AI Augmentation

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
    Drums/          # DrummerAgent (Stage 11)
    Guitar/         # GuitarAgent (Stage 12)
    Keys/           # KeysAgent (Stage 13)
    Bass/           # BassAgent (Stage 14)
    Vocal/          # VocalAgent (Stage 15)
    Coordination/   # BandBrain (Stage 16)
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
[11: Drums] 🔄 (Stages 1-3 ✅, Stages 4-8 pending)
                      ↓
           [10: Melody Scaffolding]
                      ↓
    ┌───────────────────────────────────────┐
    ↓           ↓           ↓           ↓   ↓
  [12:      [13:        [14:        [15:
  Guitar]   Keys]       Bass]       Vocal]
    └───────────────────────────────────────┘
                      ↓
           [16: Cross-Role Coordination]
                      ↓
           [17: Harmonic Narrative]
                      ↓
           [18: Performance Rendering]
                      ↓
           [19: Export Quality]
                      ↓
           [20: User Input Model]
                      ↓
           [21: Evaluation Loop]
                      ↓
           [22: ML/AI Augmentation]
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

*Last Updated:* Based on current codebase state (Stage G complete, Stage 11 Stages 1-3 complete)
