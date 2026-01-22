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

## Stage G — Groove System Completion (IN PROGRESS)

**Reference:** `GroovePlanToDo.md` for full story breakdown

**Goal:** Finish the groove system (selection + constraints + velocity + timing + overrides + diagnostics + tests) with hooks ready for human musician agent models.

**Current Status:**
- Phase A (Prep): ✅ Complete (A1, A2, A3)
- Phase B (Variation Engine): ✅ Complete (B1, B2, B3, B4)
- Phase C (Density & Caps): ✅ Complete (C1, C2, C3)
- Phase D (Onset Strength + Velocity): ✅ Complete (D1, D2)
- Phase E (Timing & Feel): 🔄 In Progress
- Phase F (Override Merge Policy): ⏳ Pending
- Phase G (Diagnostics): ⏳ Pending
- Phase H (Test Suite): ⏳ Pending

**Definition of Done:**
- Produces per-bar per-role final onset list with anchors + variations + constraints + velocity + timing
- Supports segment overrides with merge policies
- Exposes stable hook interface for `IGroovePolicyProvider` / `IGrooveCandidateSource`
- Has determinism-locked tests and diagnostics

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

## Stage 9 — Motif Placement and Rendering (PENDING)

**Goal:** Deterministically place motifs in appropriate sections, render them against harmony and groove, and integrate them with accompaniment.

**Stories:**
- 9.1: `MotifPlacementPlanner` (where motifs appear)
- 9.2: `MotifRenderer` (notes from motif spec + harmony)
- 9.3: Motif integration with accompaniment (call/response + ducking hooks)
- 9.4: Motif diagnostics

**Dependencies:** Stage G (groove hooks), Stage 8 (motif data)

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


NOTE - STAGE 11 IS FURTHER REFINED IN A SEPARATE DOCUMENT - CurrentEpic_HumanDrummer.md

## Stage 11 — Human Drummer Agent (Pop/Rock)

**Why:** The groove system provides the framework; this stage implements a realistic drummer that makes musical decisions like a human.

**Reference:** `AIResearch/groove_human_drummer_session_notes.md`

### Core Concept: `DrummerAgent` With Priorities

A skilled drummer optimizes for:
1. **Timekeeping is sacred** (anchors rarely change)
2. **Backbeat identity** stays consistent
3. **Energy arc** changes density + orchestration (hat lift, crashes, ghosts)
4. **Phrase boundaries** get punctuation (turnarounds/fills)
5. **Variation** avoids repetition, but stays in style
6. **Hands/feet constraints** avoid physically absurd patterns

### Story 11.1 — Drummer Operator Framework

**Intent:** Define the operator interface and core operator families for drums.

**Acceptance criteria:**
- Create `IDrumOperator` interface with:
  - `OperatorId` (stable identifier)
  - `OperatorType` (enum: MicroAddition, SubdivisionTransform, PhrasePunctuation, PatternSubstitution)
  - `GenerateCandidates(DrummerContext) → IEnumerable<DrumCandidate>`
  - `Score(candidate, context) → double`
- Implement core operator families:
  - **Micro-additions:** ghost-before-backbeat, ghost-after-backbeat, kick pickup, kick double, hat embellishment
  - **Subdivision transforms:** hat lift (8ths→16ths), hat drop, ride swap, partial lift
  - **Phrase punctuation:** crash on 1, turnaround fill, setup hit, stop-time/dropout
  - **Pattern substitution:** backbeat variant, kick pattern variant, half-time/double-time toggle
- Operators parameterized by style (Pop, Rock, Jazz, Metal, EDM)
- All operators deterministic given context + seed

### Story 11.2 — Physicality Constraints (Limb Feasibility)

**Intent:** Drums are physical; impossible patterns sound fake.

**Acceptance criteria:**
- Create `DrumPhysicalityFilter` with rules:
  - Limb feasibility (no impossible simultaneous hits)
  - Sticking bias (limits on ghost density, fast alternations)
  - No-overcrowd rules (caps per role, per beat)
- Rules are style-aware (jazz allows more freedom; metal has double-kick constraints)
- All filtering deterministic
- Diagnostics: log rejected candidates with reason

### Story 11.3 — Drummer Memory and Anti-Repetition

**Intent:** Human drummers don't repeat the exact same bar 8 times.

**Acceptance criteria:**
- Create `DrummerMemory` tracking:
  - Last N bars operator usage
  - Last phrase fill "shape"
  - Section signature choices (chorus hat-open on beat 1, etc.)
- Anti-repetition policy:
  - Bias against same operators in consecutive bars
  - Vary fill placement/shape across repeated sections
  - "Sometimes do nothing" policy for restraint
- All deterministic with seed

### Story 11.4 — Drummer Policy Provider

**Intent:** Connect the drummer agent to the groove system via the policy hook.

**Acceptance criteria:**
- Implement `DrummerPolicyProvider : IGroovePolicyProvider`
- Outputs `GroovePolicyDecision` with:
  - `EnabledVariationTagsOverride` (style-filtered)
  - `Density01Override` (energy-driven)
  - `MaxEventsPerBarOverride` (cap enforcement)
  - `OperatorAllowList` (which operators can fire this bar)
- Integrate with Stage 7 intent query for energy/tension/phrase context
- Unit tests verify determinism and musical sensibility

### Story 11.5 — Drummer Candidate Source

**Intent:** Replace static catalog with operator-generated candidates.

**Acceptance criteria:**
- Implement `DrummerCandidateSource : IGrooveCandidateSource`
- Generates candidates via enabled operators
- Scores and ranks candidates
- Respects physicality filters
- Integrates with groove selection engine (density targets, caps)
- Unit tests: same context → same candidates

### Story 11.6 — Drummer Performance Rendering

**Intent:** Final velocity/timing/articulation for realistic drum output.

**Acceptance criteria:**
- Drummer-specific velocity shaping:
  - Ghost velocity targets
  - Accent patterns on phrase boundaries
  - Fill crescendos
- Drummer-specific timing:
  - Push/pull by role (snare slightly behind, hats on top)
  - Fill timing (rushed at climax, laid back in groove)
- Articulation hints (for future audio rendering):
  - Rimshot vs snare
  - Open vs closed hat
  - Crash vs ride
- All deterministic

### Story 11.7 — Drummer Diagnostics and Tuning

**Intent:** Make drummer decisions visible for debugging and improvement.

**Acceptance criteria:**
- Per-bar trace showing:
  - Operators considered/selected/rejected
  - Physicality filter decisions
  - Memory state
  - Density targets vs actuals
- Non-invasive (read-only)
- Supports benchmark loop:
  - Extract features from human tracks
  - Run generator with matched context
  - Compare with objective deltas
  - Map gaps to operator/policy/constraint/performance buckets

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
Completed: [1-2] → [3] → [4] → [5] → [6] → [7] → [8.0] → [M1]
                                                            ↓
Current:   [Stage G: Groove Completion] ←──────────────────┘
                      ↓
           [8: Motif Data] (complete)
                      ↓
           [9: Motif Placement/Rendering]
                      ↓
           [10: Melody Scaffolding]
                      ↓
    ┌───────────────────────────────────────┐
    ↓           ↓           ↓           ↓   ↓
  [11:      [12:        [13:        [14:  [15:
  Drums]    Guitar]     Keys]       Bass] Vocal]
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

1. **Complete Stage G** (groove system) per `GroovePlanToDo.md`
2. **Continue Stage 9** (motif placement) once Stage G hooks are stable
3. **Begin Stage 11** (human drummer) as first full agent implementation
4. **Apply learnings** from Stage 11 to Stages 12-15

---

*This plan supersedes the original `NorthStarPlan.md` and incorporates insights from `groove_human_drummer_session_notes.md` and current codebase state.*
