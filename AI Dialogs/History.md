# Completed (Current State of the Music Generator)

This document describes what currently exists in the codebase (important concepts, designs, hooks, and major modules), focusing on items that are **not** already fully captured by `AI Dialogs/Plan.md`.

Scope notes:
- Reviewed code under `Converters/`, `Core/`, `Errors/`, `Generator/`, `Midi/`, `Song/`, `Writer/` (and relevant root/UI files).
- Excluded from analysis on purpose: `Archives/`, `Future Ideas/`, `Files/`.

---

## 1) Solution / Application Type

- Single project: `Music.csproj`.
- Target framework: `net9.0-windows`.
- App type: WinForms (`<UseWindowsForms>true</UseWindowsForms>`).
- Nullable reference types enabled (`<Nullable>enable</Nullable>`).

### Runtime entrypoints
- `Program.cs`
  - Calls `GlobalExceptionHandler.Configure()` before any UI.
  - Calls `WordParser.EnsureLoaded()` (pre-loads lyric parser singleton for first-use performance).
  - Starts `MainForm`.
- `MainForm/MainForm.cs`
  - MDI container.
  - Creates `MidiIoService` and `MidiPlaybackService`.
  - Shows `WriterForm` on startup.
  - Has menu actions for MIDI import and playback using the MIDI services.

---

## 2) External Dependencies / NuGet Packages

From `Music.csproj`:
- `Melanchall.DryWetMidi` (MIDI read/write/export + playback).
- `MeltySynth` (software synth; used in MIDI playback path / `Midi/Player.cs` usage).
- `MusicTheory` (theory utilities; used in pitch/chord logic).
- `DiffPlex` (diffing; used for diagnostics/compare workflows).

---

## 3) Core Data Model and Timing Conventions

### Timing resolution and enums
- `Core/MusicConstants.cs`
  - `TicksPerQuarterNote = 480` is the canonical tick resolution used for generator/export math.
  - `MusicConstants.eSectionType`: `Intro`, `Verse`, `Chorus`, `Solo`, `Bridge`, `Outro`, `Custom`.
  - UI-oriented `NoteValueMap` used for note value selection (denominators).

### Song context (design-time + runtime container)
- `Song/SongContext.cs`
  - Central mutable DTO shared across editor UI and generation pipeline.
  - Holds:
    - `BarTrack` (read-only-to-generator timing ruler; rebuilt from timing track by editors)
    - `GrooveTrack`, `HarmonyTrack`, `SectionTrack`, `LyricTrack`
    - `Song` (runtime output: tempo/time signature + rendered `PartTrack`s)
    - `VoiceSet` (role->instrument mapping)
  - Convention: `CurrentBar` is **1-based**.

### Runtime song container
- `Song/Song.cs`
  - Holds `TempoTrack`, `Timingtrack` (time signature track), `List<PartTrack> PartTracks`, `TotalBars`.

### Sections
- `Song/Section/SectionTrack.cs`
  - `Sections` are treated as **ordered and contiguous**.
  - `Add(...)` auto-assigns `StartBar` using internal `_nextBar` (1-based bars).
  - `TotalBars` = `_nextBar - 1`.
  - `GetActiveSection(bar, out section)` finds the last section whose `StartBar <= bar`.

### Part tracks and events
- `Song/PartTrack/PartTrack.cs`
  - Represents one instrument/role output track.
  - Contains `MidiProgramName`, `MidiProgramNumber`, and `List<PartTrackEvent> PartTrackNoteEvents`.
  - These events are the canonical source for MIDI conversion.
  - **Story M1 additions**:
    - `PartTrackId` (nested record struct): stable identity across editing sessions; GUID-based string value
    - `Meta` (property): carries domain/kind/provenance metadata with backward-compatible defaults
    - Defaults to `SongAbsolute` domain and `RoleTrack` kind for existing code compatibility

### Material fragments system (Story M1 - data definitions only)
- **Purpose**: Establish explicit data types for reusable material fragments/variants as first-class citizens alongside song role tracks
- **Status**: Data layer complete; not yet connected to generation or UI

#### Core metadata types (under `Song/Material/`)
- `PartTrackDomain` enum
  - `SongAbsolute`: events in absolute song time (ticks from song start)
  - `MaterialLocal`: events in local material time (ticks from fragment start, always >= 0)
  - **Critical invariant**: `AbsoluteTimeTicks` meaning depends on domain
- `PartTrackKind` enum
  - `RoleTrack`: rendered role track for song playback (Bass/Comp/Keys/Drums)
  - `MaterialFragment`: reusable template-like fragment in local time
  - `MaterialVariant`: transformed output derived from a fragment
- `MaterialKind` enum
  - Optional classification: `Unknown`, `Riff`, `Hook`, `MelodyPhrase`, `DrumFill`, `BassFill`, `CompPattern`, `KeysPattern`
- `PartTrack.PartTrackId` (nested in PartTrack)
  - Record struct with string value (GUID format "N")
  - `NewId()` factory method for unique ID generation
- `MaterialProvenance` record
  - Seed/transform metadata for future locking/regenerate features
  - Fields: `BaseSeed`, `DerivedSeed`, `AttemptIndex`, `SourceFragmentId`, `TransformTags`
  - Not used by generation yet; exists for future deterministic regeneration
- `PartTrackMeta` record
  - Complete metadata for PartTrack: `TrackId`, `Name`, `Description`, `IntendedRole`
  - Domain/kind classification: `Domain`, `Kind`, `MaterialKind`
  - Optional `Provenance` and `Tags` for searching/filtering
  - **Defaults ensure backward compatibility**: `SongAbsolute` + `RoleTrack` + auto-generated TrackId

#### Validation and storage
- `PartTrackMaterialValidation`
  - Non-throwing validation helper for material-specific constraints
  - Validates: material tracks should use `MaterialLocal` domain; no negative ticks allowed
  - Returns list of issue descriptions (empty if valid)
- `MaterialBank`
  - In-memory container for fragments/variants
  - Keyed by `PartTrackId`; supports filtering by `Kind`, `MaterialKind`, and `IntendedRole` (case-insensitive)
  - Methods: `Add`, `Remove`, `Contains`, `TryGet`, `GetByKind`, `GetByMaterialKind`, `GetByRole`, `Clear`
  - Not connected to generation or serialization yet

#### Design principles
- **Backward compatible**: Existing `new PartTrack(notes)` calls work unchanged with safe defaults
- **Explicit over implicit**: Domain/kind enums make track intent clear (no guessing from tick values)
- **Deterministic-friendly**: Provenance fields exist now for future locking/regenerate without refactors
- **Serialization-ready**: All types use records/enums suitable for JSON/serialization (serializer not chosen yet)
- **PartTrack remains usable**: Fragments are just PartTrack objects with different Domain/Kind; existing grids/players/export work as-is

#### Tests
- `Song/Material/Tests/MaterialDefinitionsTests.cs`
  - 13 test cases covering:
    - Backward compatibility: default Meta values for new PartTrack
    - ID uniqueness and non-empty values
    - Validation rules: domain mismatch, negative ticks
    - MaterialBank operations: add/remove/filter/round-trip
    - Provenance defaults
    - Tag support

---

## 4) Generator Pipeline (High-level)

- `Generator/Core/Generator.cs` provides the primary generation API:
  - `Generator.Generate(SongContext songContext) -> GeneratorResult`.
  - Validates presence of `SectionTrack`, `HarmonyTrack`, `GrooveTrack`, and time signature track.
  - Runs `HarmonyValidator.ValidateTrack(...)` as a fast-fail musical correctness gate.
  - Determines `totalBars` from `songContext.SectionTrack.TotalBars`.
  - Uses `RandomizationSettings.Default` and `HarmonyPolicy.Default`.
  - Builds an `EnergyArc` and per-section `EnergySectionProfile`s.
  - Resolves MIDI program numbers from `VoiceSet` by role name.
  - Generates role tracks:
    - `BassTrackGenerator.Generate(...)`
    - `GuitarTrackGenerator.Generate(...)` (comp)
    - `KeysTrackGenerator.Generate(...)` (keys/pads voicing generation)
    - `DrumTrackGenerator.Generate(...)`

Important pipeline invariant:
- `BarTrack` is intentionally treated as a caller-owned timing ruler.
  - The generator explicitly does **not** rebuild or mutate `BarTrack`.
  - Editor flows rebuild via `BarTrack.RebuildFromTimingTrack(...)`.

---

## 5) Stage 3–6 Implementation Notes (migrated from Plan)

### Stage 3 — Harmony sounds connected (keys/pads voice-leading)
- `Song/Harmony/ChordRealization`
  - Unit of harmony rendering for keys/pads (not raw chord-tone lists).
  - Fields used by renderers: `MidiNotes`, `Inversion`, `RegisterCenterMidi`, color-tone metadata, `Density`.
- `Song/Harmony/VoiceLeadingSelector`
  - Selects successive `ChordRealization`s minimizing movement (cost-based), not randomness.
  - Maintains independent previous-voicing state per role (keys vs pads) to avoid cross-role coupling.
- Section arrangement hints (small profile)
  - Per-section lift/density/color controls (chorus lifts; verse lighter) to avoid global-only behavior.

### Stage 4 — Comp becomes comp (multi-note chord fragments)
- `Song/Groove/CompRhythmPatternLibrary` + `Song/Groove/CompRhythmPattern`
  - Groove onsets are candidates; patterns deterministically select subsets per bar/section.
- `Song/Harmony/CompVoicingSelector`
  - Outputs multi-note chord fragments (guide-tone bias on strong beats; root may be omitted).
  - Guardrails: avoid muddy low voicings; keep top comp tone under lead-space ceiling.
- `Song/Harmony/StrumTimingEngine`
  - Deterministic micro-offset spread inside a chord to avoid block-chord feel.
  - Applied to comp (and designed so keys can adopt later if desired).

### Stage 5 — Bassline writing (groove-locked + harmony-aware)
- `Song/Groove/BassPatternLibrary` + `Song/Groove/BassPattern`
  - Small reusable pattern set (root anchors, fifth motion, octaves, approach notes).
  - Pattern selection deterministic by section + bar; seed used only as tie-break.
- `Song/Groove/BassChordChangeDetector`
  - Detects upcoming chord changes within a window; enables diatonic approach/pickups.
  - Approach insertion only when groove has a valid slot (policy/guardrail driven).

### Stage 6 — Drums (template ? performance)
- `Generator/Drums/DrumVariationEngine`
  - Renders per-bar drum performance from groove template + section profile + seed.
  - Variation keyed deterministically by `(seed, grooveName, sectionType, barIndex)`.
- `Generator/Drums/DrumMicroTimingEngine`
  - Deterministic push/pull micro-timing; clamped to avoid crossing bar boundaries.
- `Generator/Drums/DrumVelocityShaper`
  - Deterministic dynamics shaping: accents/ghosts; fill crescendos.
- `Generator/Drums/DrumFillEngine`
  - Fills at section ends or phrase ends; style-aware; density-capped.
  - Structured fill shapes (8th/16th rolls, tom motion) with crash/ride + kick support on next downbeat.
- `Generator/Drums/CymbalOrchestrationEngine`
  - Deterministic crash/ride/hat language decisions tied to section energy and phrase peaks.
- `Generator/Drums/DrumRoleParameters`
  - Stable knob surface for Stage 7+/energy/tension to drive density/velocity/busy/fill behavior.

---

## 6) Harmony Infrastructure (what exists now)

Key harmony modules (all under `Song/Harmony/`):
- `HarmonyTrack` + `HarmonyEvent` as the timeline of harmony.
- `HarmonyPitchContext` and `HarmonyPitchContextBuilder` for chord-scale/pitch material per slot.
- `ChordRealization`
  - A stable unit of harmony rendering used by keys/pads.
  - Holds MIDI notes + inversion/register metadata used across slots.
- `VoiceLeadingSelector`
  - Chooses successive `ChordRealization`s with a movement-minimizing cost function.
  - Enables ?connected? harmony across time.
- `CompVoicingSelector`
  - Returns chord fragments/guide-tone biased voicings for comp.
  - Designed to output multi-note events per slot (not monophonic ?lead-ish? comp).
- `StrumTimingEngine`
  - Deterministic micro-offset spread within a chord (strum/roll feel).
- `HarmonyValidator` + `HarmonyValidationOptions` + `HarmonyValidationResult`
  - Deterministic validation gate used by `Generator`.
- `HarmonyEventNormalizer` (normalization pass for harmony event data).
- Diagnostics:
  - `HarmonyDiagnostics`, `HarmonyEventDiagnostic`.

---

## 7) Groove Infrastructure (what exists now)

Key groove modules (under `Song/Groove/`):
- `GroovePreset`
  - Declarative groove ?template? with at least two layers:
    - `AnchorLayer`
    - `TensionLayer`
- `OnsetGrid` / `OnsetSlot`
  - Slot-based onset model used for rhythm candidate positions.
- `CompRhythmPatternLibrary` / `CompRhythmPattern`
  - Deterministic comp rhythm pattern selection by groove preset + section characteristics.
- Bass support:
  - `BassPatternLibrary` / `BassPattern`
  - `BassChordChangeDetector` (pattern logic aware of upcoming chord changes).
- `GroovePresets` and `GrooveTestData` exist as preset/test fixtures.

---

## 8) Stage 7 Energy System (implemented)

### Energy arc selection and constraint application
- `Song/Energy/EnergyArc.cs`
  - Deterministically selects an `EnergyArcTemplate` from `EnergyArcLibrary` by `(seed, grooveName, songFormId)`.
  - Applies an `EnergyConstraintPolicy` (selected by `EnergyConstraintPolicyLibrary.GetPolicyForGroove(grooveName)` by default).
  - Pre-computes constrained energies in order and caches:
    - `Dictionary<int, double> _constrainedEnergies`
    - `Dictionary<int, List<string>> _constraintDiagnostics`
  - Preferred API for constrained energy:
    - `GetTargetForSection(Section section, int sectionIndex)`.

### Constraint framework
- `EnergyConstraintPolicy`, `EnergyConstraintRule`, `EnergyConstraintContext`, `EnergyConstraintResult`.
- Implemented rule set in `Song/Energy/Rules/`:
  - `SameTypeSectionsMonotonicRule`
  - `PostChorusDropRule`
  - `FinalChorusPeakRule`
  - `BridgeContrastRule`
- Diagnostics:
  - `EnergyConstraintDiagnostics` (full report, compact report, summaries, ASCII chart, arc comparisons).

### Section energy profiles (consumed by generators)
- `EnergyProfileBuilder` produces `EnergySectionProfile` per `Section`.
  - `Global`: includes `Energy`, `TensionTarget` (current placeholder mapping), `ContrastBias`.
  - `Roles`: `Bass`, `Comp`, `Keys`, `Pads`, `Drums` each as `EnergyRoleProfile`:
    - `DensityMultiplier`, `VelocityBias`, `RegisterLiftSemitones`, `BusyProbability`.
  - `Orchestration`: `EnergyOrchestrationProfile` with per-role presence flags and cymbal language hinting.

### Tests (in main project)
Energy system tests exists as compiled test-like classes under `Song/Energy/`:
- `EnergyArcTests`, `EnergySectionProfileTests`.
- Constraint tests:
  - `EnergyConstraintTests`, `EnergyConstraintApplicationTests`, `EnergyConstraintValidationTests`, `EnergyConstraintDiagnosticsTests`.

Note: These tests are not in a separate test project; they compile into the main WinForms project (the `.csproj` explicitly excludes a `Tests/` folder).

---

## 8) Tension Contracts (present, computation mostly pending)

A tension model exists as a contract for later stages.

### Types / contracts
- `Song/Energy/SectionTensionProfile.cs`
  - Immutable record: `MacroTension`, `MicroTensionDefault`, `Driver`, `AbsoluteSectionIndex`.
- `Song/Energy/TensionDriver.cs`
  - Flags enum used for explainability (why tension is high/low).
- `Song/Energy/MicroTensionMap.cs`
  - Bar-level tension map with per-bar flags (phrase/section start/end) + helper creators (e.g., `Flat`).
- `Song/Energy/ITensionQuery.cs`
  - Stable query surface for renderers:
    - `GetMacroTension(section)`
    - `GetMicroTension(section, barInSection)`
    - `GetMicroTensionMap(section)`
    - `GetPhraseFlags(section, barInSection)`
- `Song/Energy/NeutralTensionQuery.cs`
  - Placeholder implementation returning neutral tension and precomputed flat maps.

### Tests
- `Song/Energy/TensionModelTests.cs` (many test cases validating shape/ranges/determinism of the model layer).

---

## 9) Stage 6 Drums System (implemented)

Key modules under `Generator/Drums/`:
- `DrumTrackGenerator`
  - Produces the drum `PartTrack` (with energy-driven parameters passed in).
- `DrumVariationEngine`
  - Converts static groove template into a per-bar performance (deterministic variation).
- `DrumMicroTimingEngine`
  - Deterministic micro-timing for drums (push/pull feel).
- `DrumVelocityShaper`
  - Deterministic dynamics shaping (ghosts, accents, etc.).
- `DrumFillEngine`
  - Deterministic fill generation at transitions/phrase endings.
- `CymbalOrchestrationEngine`
  - Deterministic crash/ride/hat language decisions.
- `DrumRoleParameters`
  - Parameter object used to connect Stage 7 energy profile knobs to Stage 6 mechanics.

Drum tests (compiled into main project):
- `DrumHumanizationTests`, `DrumFillTests`.

---

## 10) Stage 5 Bass System (implemented)

Key bass modules:
- `Generator/Bass/BassTrackGenerator.cs` (role generator)
- `Song/Groove/BassPatternLibrary.cs` and `Song/Groove/BassPattern.cs`
- `Song/Groove/BassChordChangeDetector.cs` (approach/pickup awareness)

---

## 11) Comp / Guitar System (implemented)

Key comp modules:
- `Generator/Guitar/GuitarTrackGenerator.cs`
  - Uses `CompBehaviorSelector` to select playing behavior (Story 8.0.1)
  - Uses `CompBehaviorRealizer` for onset filtering and duration shaping (Story 8.0.2)
  - Applies behavior-specific duration multipliers to note events (Story 8.0.3)
- `Song/Groove/CompRhythmPatternLibrary.cs` and `Song/Groove/CompRhythmPattern.cs`
- `Song/Harmony/CompVoicingSelector.cs`
- `Song/Harmony/StrumTimingEngine.cs` (micro roll offsets applied to comp chords)

### Stage 8.0 Comp Behavior System (implemented)
- `Generator/Guitar/CompBehavior.cs`
  - Enum defining five distinct playing behaviors:
    - `SparseAnchors`: mostly strong beats, fewer hits, longer sustains (Verse low energy, Intro, Outro)
    - `Standard`: balanced strong/weak beats, moderate sustains (Verse mid energy, Bridge)
    - `Anticipate`: adds anticipations, shorter notes (PreChorus, build sections)
    - `SyncopatedChop`: more offbeats, short durations, frequent re-attacks (Chorus high energy)
    - `DrivingFull`: all available onsets, consistent attacks, driving feel (Chorus max energy, Outro)
  - `CompBehaviorSelector`: deterministic selection by `(sectionType, absoluteSectionIndex, barIndex, energy, busyProbability, seed)`
    - Section-type specific thresholds and biases
    - Per-bar variation every 4th bar (30% chance, hash-based)
    - Upgrade/downgrade logic for natural evolution
- `Generator/Guitar/CompBehaviorRealizer.cs`
  - `CompRealizationResult` record: `SelectedOnsets` list + `DurationMultiplier` [0.25..1.5]
  - Behavior-specific onset selection strategies:
    - `SparseAnchors`: prefer strong beats only, limit to max 2 onsets, duration *1.3
    - `Standard`: pattern-based with rotation by bar+seed, duration *1.0
    - `Anticipate`: interleave anticipations and strong beats, duration *0.75
    - `SyncopatedChop`: prefer offbeats (70% target), duration *0.5
    - `DrivingFull`: all/nearly all onsets, duration *0.65
  - All onset selections clamped and deterministic

---

## 12) Keys System (implemented)

Key keys modules:
- `Generator/Keys/KeysTrackGenerator.cs`
  - Uses `KeysRoleModeSelector` to select playing mode (Story 8.0.4)
  - Uses `KeysModeRealizer` for onset filtering and duration shaping (Story 8.0.5)
  - Applies mode-specific duration multipliers and split-voicing logic (Story 8.0.6)
- Harmony dependencies:
  - `ChordRealization` and `VoiceLeadingSelector`
  - `HarmonyPitchContextBuilder`

### Stage 8.0 Keys Role Mode System (implemented)
- `Generator/Keys/KeysRoleMode.cs`
  - Enum defining four distinct playing modes:
    - `Sustain`: hold chord across bar/half-bar, minimal re-attacks (low energy, intros, outros)
    - `Pulse`: re-strike on selected beats, moderate sustain (verses, mid-energy sections)
    - `Rhythmic`: follow pad onsets closely, shorter notes (choruses, high-energy sections)
    - `SplitVoicing`: split voicing across 2 hits (builds, transitions, dramatic moments)
  - `KeysRoleModeSelector`: deterministic selection by `(sectionType, absoluteSectionIndex, barIndex, energy, busyProbability, seed)`
    - Section-type specific mode selection (Verse: Sustain?Pulse?Rhythmic; Chorus: Pulse?Rhythmic)
    - Bridge special case: 40% chance of SplitVoicing on first bar when energy > 0.5
- `Generator/Keys/KeysModeRealizer.cs`
  - `KeysRealizationResult` record: `SelectedOnsets` list + `DurationMultiplier` [0.5..2.0] + `SplitUpperOnsetIndex`
  - Mode-specific onset selection and duration:
    - `Sustain`: only first onset, duration *2.0 (extended beyond slot)
    - `Pulse`: prefer strong beats, deterministic offbeat selection by seed, duration *1.0
    - `Rhythmic`: use most/all onsets (up to 130% density), duration *0.7
    - `SplitVoicing`: two onsets (first + middle), duration *1.2, marks second onset as upper voicing
  - Integration in `KeysTrackGenerator`: splits chord realization when `SplitVoicing` mode active
    - Lower onset: lower half + middle note
    - Upper onset: upper half of voicing

---

## 13) MIDI Model + Playback

### MIDI document wrapper
- `Midi/MidiSongDocument.cs`
  - Wraps a DryWetMidi `MidiFile` (`Raw`) and stores metadata like `FileName`.

### Import / export
- `Midi/MidiIoService.cs`
  - `ImportFromFile(path)` -> `MidiSongDocument` (wraps exceptions as `MidiImportException`).
  - `ExportToFile(path, doc)` writes `doc.Raw.Write(path)`.

### Playback
- `Midi/MidiPlaybackService.cs`
  - Manages `OutputDevice` + `Playback` lifecycle.
  - `Play(doc)`, `Pause()`, `Resume()`, `Stop()`.
  - Exposes `CurrentTick` (MIDI tick position).

### App state
- `Midi/AppState.cs`
  - Stores current `MidiSongDocument` (used by MainForm menu actions).

---

## 14) PartTrack -> MIDI Conversion Pipeline

Implemented as an explicit 3-step pipeline under `Converters/`.

- `ConvertPartTracksToMidiSongDocument_For_Play_And_Export.cs`
  - Orchestrates Steps 1–3.
  - Validates each `PartTrack.PartTrackNoteEvents` list is sorted by `AbsoluteTimeTicks` before converting (shows an error dialog and returns null on violation).

- `ConvertPartTracksToMidiSongDocument_Step_1.cs`
  - Emits:
    - Track name event at time 0
    - Program change at time 0
    - NoteOn/NoteOff events per note event
  - Removes the `Channel` parameter from events (assigned later).

- `ConvertPartTracksToMidiSongDocument_Step_2.cs`
  - Merges part tracks by instrument and injects tempo/time signature events.

- `ConvertPartTracksToMidiSongDocument_Step_3.cs`
  - Materializes merged timed event stream into a `MidiSongDocument`.

There is also an import-only converter:
- `ConvertMidiSongDocumentToPartTracks_For_Import_Only.cs`

---

## 15) Writer / Editor UI (WinForms)

`Writer` is the primary interactive design surface.

### Primary form
- `Writer/WriterForm/WriterForm.cs`
  - Hosts the grid (`dgSong`) and supports editor launch by double-clicking fixed rows.
  - Uses these manager-style helpers:
    - `SongGridManager` (grid layout and behavior)
    - `GridControlLinesManager` (attaching track data to grid)
    - `WriterFormEventHandlers` (general row handlers)
    - `WriterFormGridOperations` (grid operations)
  - Editor dialogs:
    - `VoiceSelectorForm`
    - `SectionEditorForm`
    - `LyricEditorForm`
    - `HarmonyEditorForm`
    - `GrooveEditorForm`
    - `TimingEditorForm` (also triggers `BarTrack.RebuildFromTimingTrack(...)`)
    - `TempoEditorForm`

### Option form
- `Writer/OptionForm/OptionForm.cs`
  - Separate dialog for options that previously existed on `WriterForm`.

---

## 16) Error and Diagnostics Facilities

- `Errors/GlobalExceptionHandler.cs`
  - Centralized error surfacing for:
    - WinForms UI thread (`Application.ThreadException`)
    - AppDomain unhandled exceptions
    - unobserved task exceptions

- `Errors/MessageBoxHelper.cs`
  - Standardizes error dialogs used by converters/validators.

- `Errors/Tracer.cs`
  - Simple append-only file logging under `Errors/` using `Helpers.ProjectPath(...)`.

- `Core/ObjectViewer.cs`
  - Diagnostic object-to-text viewing support (used for debugging/inspection flows).

---

## 16.5) Stage 8.0 Audibility Pass (Comp + Keys Intelligence)

**Priority:** Immediately after Stage 7 (before Stage 8 PhraseMap work)  
**Goal:** Make Verse vs Chorus vs PreChorus differences clearly audible through behavior-based rhythm variation and duration shaping.

### Problems Solved
1. **Comp rhythm selection**: Previously always took "first N" indices; now uses behavior-specific strategies (sparse anchors, anticipations, syncopated chop, etc.)
2. **Register lift ineffectiveness**: Rounded to octaves and often undone by guardrails; now behavior system provides audible variety through rhythm and duration instead
3. **Keys static rhythm**: Previously used pads onsets directly; now filters through mode system (sustain, pulse, rhythmic, split voicing)
4. **No duration variation**: Previously always used slot duration; now applies behavior/mode-specific duration multipliers (0.25x to 2.0x)
5. **Seed insensitivity**: Seed now affects per-bar variation, onset rotation, and mode selection chances

### Implementation Stories (all completed)
- **Story 8.0.1**: `CompBehavior` enum + `CompBehaviorSelector` (deterministic behavior selection)
- **Story 8.0.2**: `CompBehaviorRealizer` (onset filtering + duration shaping per behavior)
- **Story 8.0.3**: Integration into `GuitarTrackGenerator` (behavior selection, realization, duration application)
- **Story 8.0.4**: `KeysRoleMode` enum + `KeysRoleModeSelector` (deterministic mode selection)
- **Story 8.0.5**: `KeysModeRealizer` (onset filtering + duration shaping per mode)
- **Story 8.0.6**: Integration into `KeysTrackGenerator` (mode selection, realization, split voicing)
- **Story 8.0.7**: `SeedSensitivityTests` (12 tests verifying seed sensitivity and determinism)

### Key Modules
- `Generator/Guitar/CompBehavior.cs` (behavior enum + selector)
- `Generator/Guitar/CompBehaviorRealizer.cs` (realization logic)
- `Generator/Keys/KeysRoleMode.cs` (mode enum + selector)
- `Generator/Keys/KeysModeRealizer.cs` (realization logic)
- `Generator/Tests/SeedSensitivityTests.cs` (cross-role seed sensitivity validation)

### Test Coverage
All tests in `Generator/Tests/SeedSensitivityTests.cs` verify:
- Determinism: same seed ? identical output
- Seed sensitivity: different seeds ? different behaviors/modes/onset selections
- Section contrast: Verse vs Chorus produce audibly different behaviors (sparse vs syncopated/driving)
- Bridge special cases: SplitVoicing chance varies by seed
- Every-4th-bar variation in comp behavior selection
- All output meets invariants: sorted events, no overlaps, valid ranges

### Expected Audible Results
- **Verse**: Sparse anchors or standard comp, sustain/pulse keys ? calm, spacious
- **Chorus**: Syncopated chop or driving full comp, rhythmic keys ? energetic, busy
- **PreChorus/Bridge**: Anticipate comp, possible split voicing keys ? building tension
- **Different seeds**: Noticeably different bar-to-bar patterns within same structure
- **Duration contrast**: High energy = shorter notes (choppier); low energy = longer sustains

---

## 17) Notable Architectural Conventions / Contracts

- **Determinism pattern**: variation engines and planners typically treat randomness as a tie-break; inputs include seed + stable keys (e.g., grooveName, sectionType/index, barIndex).
- **1-based bars** across UI and track data structures.
- **`BarTrack` is a timing ruler** built by editor/import pipelines and treated read-only by the generator.
- **Validation-first**: harmony is validated before generation; MIDI export validates sorted PartTrack event time ordering.
- **Energy intent separation**: Stage 7 computes energy intent; role generators consume role profiles + orchestration hints.
- **Tension query is a stable contract** already present even if full macro/micro computation is not yet wired into generation.

---

## 18) File/Folder Map (for quick navigation)

- `Generator/`
  - `Core/` (entrypoint generator, overlap preventers, randomization helpers)
  - `Bass/`, `Guitar/`, `Keys/`, `Drums/` (role generators)
  - `Tests/` (SeedSensitivityTests and other validation tests)
- `Song/`
  - `Energy/` (EnergyArc, constraints, profiles, tension contracts)
  - `Harmony/` (contexts, voicing, validation, diagnostics)
  - `Groove/` (presets, onset grid, pattern libraries)
  - `Material/` (fragment metadata, MaterialBank, validation; Story M1 data layer)
  - `Section/`, `Tempo/`, `Timing/`, `PartTrack/`, `Voices/`, `Lyrics/`
- `Converters/` (PartTrack<->MIDI pipelines)
- `Midi/` (import/export/playback wrappers)
- `Writer/` (WinForms editor surface)
- `Core/`, `Errors/` (utilities + diagnostics)

