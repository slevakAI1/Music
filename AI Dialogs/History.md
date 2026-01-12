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

## 5) Harmony Infrastructure (what exists now)

Key harmony modules (all under `Song/Harmony/`):
- `HarmonyTrack` + `HarmonyEvent` as the timeline of harmony.
- `HarmonyPitchContext` and `HarmonyPitchContextBuilder` for chord-scale/pitch material per slot.
- `ChordRealization`
  - A stable unit of harmony rendering used by keys/pads.
  - Holds MIDI notes + inversion/register metadata used across slots.
- `VoiceLeadingSelector`
  - Chooses successive `ChordRealization`s with a movement-minimizing cost function.
  - Enables “connected” harmony across time.
- `CompVoicingSelector`
  - Returns chord fragments/guide-tone biased voicings for comp.
  - Designed to output multi-note events per slot (not monophonic “lead-ish” comp).
- `StrumTimingEngine`
  - Deterministic micro-offset spread within a chord (strum/roll feel).
- `HarmonyValidator` + `HarmonyValidationOptions` + `HarmonyValidationResult`
  - Deterministic validation gate used by `Generator`.
- `HarmonyEventNormalizer` (normalization pass for harmony event data).
- Diagnostics:
  - `HarmonyDiagnostics`, `HarmonyEventDiagnostic`.

---

## 6) Groove Infrastructure (what exists now)

Key groove modules (under `Song/Groove/`):
- `GroovePreset`
  - Declarative groove “template” with at least two layers:
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

## 7) Stage 7 Energy System (implemented)

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
- `Song/Groove/CompRhythmPatternLibrary.cs` and `Song/Groove/CompRhythmPattern.cs`
- `Song/Harmony/CompVoicingSelector.cs`
- `Song/Harmony/StrumTimingEngine.cs` (micro roll offsets applied to comp chords)

---

## 12) Keys System (implemented)

Key keys modules:
- `Generator/Keys/KeysTrackGenerator.cs`
- Harmony dependencies:
  - `ChordRealization` and `VoiceLeadingSelector`
  - `HarmonyPitchContextBuilder`

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
- `Song/`
  - `Energy/` (EnergyArc, constraints, profiles, tension contracts)
  - `Harmony/` (contexts, voicing, validation, diagnostics)
  - `Groove/` (presets, onset grid, pattern libraries)
  - `Section/`, `Tempo/`, `Timing/`, `PartTrack/`, `Voices/`, `Lyrics/`
- `Converters/` (PartTrack<->MIDI pipelines)
- `Midi/` (import/export/playback wrappers)
- `Writer/` (WinForms editor surface)
- `Core/`, `Errors/` (utilities + diagnostics)

