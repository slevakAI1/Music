
## Product North Star Goal

Build a deterministic, user-controllable music generator that produces **original, fully-produced songs** that listeners **enjoy, remember, and replay**—to the point that, in blind listening, the output is **not reliably distinguishable** from songs written and performed by **top-tier human songwriters and musicians**.

### Success means

- **Originality:** outputs are non-derivative at the song level (melody, motifs, arrangement, groove, phrasing), not just shuffled templates.
- **Stickiness:** songs create memorable hooks and satisfying tension/release so a high percentage of listeners can recall the main idea after listening.
- **Human realism:** timing, dynamics, articulation, and instrument behavior sound like expert players, not quantized MIDI.
- **Songwriter-level structure:** clear section identity, repetition with purposeful variation, effective builds/drops, and strong cadences.
- **Control + automation range:** the system supports both:
  - **High-level generation** from a few controls (genre/style, mood, tempo, seed(s), energy arc), and
  - **Fine-grained authoring** where the user can lock/override chords, form, motifs, melodic phrases, or even exact events—while retaining coherent musical results.
- **Iterative creativity workflow:** users can generate alternatives, audition, accept/lock, and refine parts (motifs, melody phrases, riffs, drum fills, voicings) without losing reproducibility.

### Engineering constraints that must always hold

- **Determinism & reproducibility:** same inputs + seed(s) must reproduce the same song, and localized regeneration must not unintentionally change locked/accepted material.
- **Guardrails & musical safety:** the system must enforce register management, role separation, groove anchors, and anti-mud rules so added complexity improves musicality rather than breaking it.
- **Measurable improvement path:** every stage must move at least one of these needles: **memorability**, **human realism**, **section identity**, **variation**, **user control**, or **objective musicality metrics** (with later stages adding brain-pattern/surprise modeling).

### Definition of “done” (long-term)

In blinded tests across target genres, the generator’s songs achieve **listener preference and recall rates** that meet or exceed strong human baselines, while maintaining **originality**, **realism**, and **user-directed control**.


See History.md for the full history of how this plan evolved. This is remaining work to be done (work in progress)


============================================================================================================================
REMAINING STORIES (in rough order, pending further analysis and refinement):
============================================================================================================================

## Stage 8 — Material motifs: data definitions and test fixtures (Story M2)

**Why now:** Stage 7 provided energy/tension/phrase intent; Stage 8 will place and render motifs. Before placement logic, establish motifs as first-class material objects stored in MaterialBank

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
