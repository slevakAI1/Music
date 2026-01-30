# Music Generator Project Architecture Reference

**Purpose:** Architectural overview of the Music project for AI code generation context.

**Last Updated:** Companion document to active development.

---





## 1) Solution Overview

| Property | Value |
|----------|-------|
| Project | `Music.csproj` |
| Test Project | `Music.Tests.csproj` |
| Framework | `net9.0-windows` |
| App Type | WinForms |
| Nullable | Enabled |

### NuGet Dependencies

| Package | Purpose |
|---------|---------|
| `Melanchall.DryWetMidi` | MIDI read/write/export and playback |
| `MeltySynth` | Software synthesizer |
| `MusicTheory` | Theory utilities |
| `DiffPlex` | Diffing utilities |

---

## 2) Application Entry Points

### Program.cs
Initializes global services and starts main form.

### MainForm.cs
MDI container hosting WriterForm; manages MIDI I/O and playback services.

### WriterForm.cs
Primary design surface with song grid; triggers generation.

---

## 3) Core Data Model

### Timing Constants
`MusicConstants.TicksPerQuarterNote = 480` (canonical resolution)

### SongContext
Central DTO containing all song design tracks and configuration.

Location: `Song/SongContext.cs`

Key properties:
- `BarTrack` — timing ruler
- `GroovePresetDefinition` — groove configuration (anchor layer only; Story 5.2 removed SegmentGrooveProfiles)
- `HarmonyTrack` — chord progression
- `SectionTrack` — song structure
- `LyricTrack` — lyrics with phonetics
- `Song` — runtime output (tempo, time signature, part tracks)
- `VoiceSet` — role to MIDI program mapping
- `MaterialBank` — reusable motifs/fragments

### Song Output
Location: `Song/Song.cs`

Contains:
- `TempoTrack`
- `TimeSignatureTrack`
- `PartTracks` (list)
- `TotalBars`

### Section Model
Location: `Song/Section/`

Components:
- `Section` — section metadata (type, name, start bar, bar count)
- `SectionTrack` — ordered, contiguous sections

### PartTrack
Location: `Song/PartTrack/`

Instrument events container with:
- `PartTrackId` — GUID-based identity
- `PartTrackMeta` — metadata (domain, kind, role, tags)
- `MidiProgramName` / `MidiProgramNumber`
- `PartTrackNoteEvents` — sorted by `AbsoluteTimeTicks`

### PartTrackEvent
Single MIDI event with:
- `AbsoluteTimeTicks`
- `Type`
- `NoteNumber`
- `NoteDurationTicks`
- `NoteOnVelocity`

### BarTrack
Location: `Song/Bar/BarTrack.cs`

Timing ruler derived from `TimingTrack`. Read-only to generator.

### HarmonyTrack
Location: `Song/Harmony/HarmonyTrack.cs`

Timeline of `HarmonyEvent` records containing key, degree, quality, bass.

---

## 4) Generator Pipeline

### Generator Entry Point
Location: `Generator/Core/Generator.cs`

Validates SongContext and delegates to drum track generator.

### GrooveBasedDrumGenerator Pipeline
Location: `Generator/Agents/Drums/GrooveBasedDrumGenerator.cs`

Pipeline orchestrator using `IDrumPolicyProvider` + `IDrumCandidateSource` (typically DrummerAgent).

Pipeline stages:
1. Extract anchors from `GroovePresetDefinition`
2. Build `DrumBarContext` for each bar (section info, phrase position, energy)
3. For each bar + role:
   - Get policy decision from `IDrumPolicyProvider` (density targets, caps, weights)
   - Get candidate groups from `IDrumCandidateSource` (operator-generated candidates)
   - Select candidates using `DrumSelectionEngine` (weighted selection, density enforcement)
4. Combine anchors + selected operator onsets
5. Convert to MIDI events via `PartTrack`

### DrumTrackGenerator (Legacy)
Location: `Generator/Drums/DrumTrackGenerator.cs`

**Status:** Legacy groove-based pipeline; being replaced by GrooveBasedDrumGenerator + DrummerAgent architecture.

---

## 5) Groove System Architecture

### Core Components
Location: `Generator/Groove/`

**GroovePresetDefinition** — simplified groove configuration (Story 5.2, GC-Epic)
- `GroovePresetIdentity` — name and metadata
- `AnchorLayer` (`GrooveInstanceLayer`) — base pattern onsets only

**Note:** Variation infrastructure has been removed (GC-Epic). All variation is now handled by Drummer Agent operators.

**GrooveInstanceLayer** — anchor onset container
- List of `GrooveOnset` records
- Query methods (`GetOnsets`, `GetActiveRoles`, `HasRole`)
- `ToPartTrack()` for preview/audition

**GrooveOnset** — single onset record
- Role, bar, beat
- Strength, velocity, timing offset
- Provenance (anchor/variation tracking)

**GrooveOnsetProvenance** — tracks onset origin
- Anchor provenance (`ForAnchor`)
- Variation provenance (`ForVariation`)
- Candidate ID for tracing

### Groove Infrastructure Files

| Component | Responsibility |
|-----------|---------------|
| `OnsetGrid` / `OnsetGridBuilder` | Valid beat positions from subdivision rules |
| `OnsetStrengthClassifier` | Classify onset strength (downbeat/backbeat/offbeat/etc.) |
| `FeelTimingEngine` | Apply feel timing to offbeats (swing/shuffle/straight) |
| `RoleTimingEngine` | Apply per-role micro-timing offsets |
| `PartTrackBarCoverageAnalyzer` | Analyze per-bar fill state for density tracking |
| `GrooveRoles` | Role name constants (Kick, Snare, ClosedHat, OpenHat, etc.) |
| `AllowedSubdivision` | Subdivision policy enum (Eighth, Sixteenth, Triplet) |
| `GrooveFeel` | Feel/swing policy enum (Straight, Swing, Shuffle, etc.) |
| `TimingFeel` | Micro-timing style enum (OnTheGrid, Relaxed, Pushing, Dragging) |
| `GrooveAnchorFactory` | Creates anchor layers for presets |
| `GroovePresetLibrary` | Registry of preset definitions |
| `GrooveBarPlan` | Per-bar plan record (planned for future use) |
| `GrooveBarDiagnostics` | Decision trace (opt-in, planned for future use) |
| `BarCoverageReport` | Bar fill state analysis result |
| `BarFillState` | Enum for fill state (Empty, Sparse, Moderate, Dense, Full) |
| `SectionRolePresenceDefaults` | Default role presence rules per section type |
| `RoleRhythmVocabulary` | Rhythm vocabulary constraints per role |

### Removed Components (GC-Epic)
- `GrooveVariationCatalog` — deleted
- `GrooveVariationLayer` — deleted
- `GrooveVariationLayerMerger` — deleted
- `OnsetCandidate` (generic) — deleted
- `CandidateGroup` (generic) — deleted
- `SegmentGrooveProfile` — removed in Story 5.2

**Note:** Protection policy infrastructure was removed in Story 5.3. Density and constraint enforcement now handled by DrummerPolicyProvider and physicality systems.

---

## 6) Material System Architecture

### Core Types
Location: `Song/Material/`

**PartTrackMeta** — track metadata
- `TrackId`, `Name`, `Description`
- `IntendedRole`
- `Domain` (SongAbsolute / MaterialLocal)
- `Kind` (RoleTrack / MaterialFragment / MaterialVariant)
- `MaterialKind` (Riff / Hook / MelodyPhrase / etc.)
- `Provenance`
- `Tags`

**Enums:**
- `PartTrackDomain` — time semantics
- `PartTrackKind` — intent/category
- `MaterialKind` — classification

**MotifSpec** — motif definition
- `MotifId`, `Name`, `IntendedRole`, `Kind`
- `RhythmShape` — tick positions
- `Contour` — melodic contour intent
- `Register` — pitch register intent
- `TonePolicy` — harmonic constraints
- `Tags`

**MaterialBank** — container
- Add/remove/query motifs
- Query by kind, material kind, role, name

### Motif Pipeline

| Component | Responsibility |
|-----------|---------------|
| `MotifLibrary` | Hardcoded test motifs |
| `MotifPlacementPlanner` | Determine which motifs where |
| `MotifPlacement` | Placement record |
| `MotifPlacementPlan` | Collection of placements |
| `MotifPresenceMap` | Query motif activity for coordination |
| `MotifRenderer` | Render motif spec + placement to PartTrack |

---

## 7) RNG System

Location: `Generator/Core/Randomization/Rng.cs`

Deterministic RNG with purpose-specific streams. Same seed → identical sequences per purpose.

Usage pattern:
- `Rng.Initialize(seed)` — call once at app start
- `Rng.NextInt(purpose, min, max)`
- `Rng.NextDouble(purpose)`

---

## 8) MIDI Pipeline

### Export Pipeline
Location: `Converters/`

Flow:
1. Convert PartTracks to absolute-time MIDI events
2. Merge by instrument, inject tempo/time signature
3. Materialize to MidiSongDocument

Validation: PartTrack events must be sorted by `AbsoluteTimeTicks`.

### MIDI Services
Location: `Midi/`

| Service | Responsibility |
|---------|---------------|
| `MidiIoService` | Import/export MIDI files |
| `MidiPlaybackService` | Play/pause/stop/resume |
| `MidiSongDocument` | Wrapper around MidiFile |
| `Player.cs` | Software synth integration |

---

## 9) Harmony Infrastructure

Location: `Song/Harmony/`

| Component | Responsibility |
|-----------|---------------|
| `HarmonyTrack` | Timeline of harmony events |
| `HarmonyEvent` | Single chord definition |
| `HarmonyPitchContext` | Chord-scale pitch material |
| `HarmonyPitchContextBuilder` | Build pitch contexts |
| `ChordRealization` | Concrete voicing with MIDI notes |
| `VoiceLeadingSelector` | Cost-based voicing selection |
| `CompVoicingSelector` | Guide-tone biased voicings |
| `StrumTimingEngine` | Micro-offset spread within chords |
| `HarmonyValidator` | Validation gate |
| `HarmonyEventNormalizer` | Normalization pass |
| `PitchClassUtils` | Key parsing utilities |

---

## 10) Voice and Role System

Location: `Song/Voices/`

**VoiceSet** — collection of voices with role mapping

**Voice** — MIDI program name + groove role

**GrooveRoles** — constants for standard roles (Kick, Snare, ClosedHat, OpenHat, DrumKit, Bass, Comp, Pads, Keys, Lead)

---

## 11) Test Infrastructure

### Main Project Tests
Location: in-project test files (compiled with app)

Contains test data for sections, harmony, tempo, timing, and material.

Files include:
- `SectionTests.cs`
- `HarmonyTests.cs`
- `TempoTests.cs`
- `TimingTests.cs`
- `WriterFormTests.cs`
- `TestDesigns.cs` and integration tests in `Generator/TestSetups/`

### Music.Tests Project
Location: `Music.Tests/`

Framework: xUnit

Conventions:
- Constructor-based RNG initialization
- Method naming: `<Component>_<Condition>_<ExpectedResult>`
- Test collections for shared fixtures (`TestCollections.cs`)
- `#region` blocks for organization
- Golden snapshots for regression testing (`DrummerAgentTests`, `GoldenSnapshot.cs`)

Test organization mirrors source structure:
- `Generator/Agents/Common/` — shared agent infrastructure tests
- `Generator/Agents/Drums/` — drummer agent tests
- `Generator/Agents/Drums/Operators/` — operator family tests
- `Generator/Agents/Drums/Physicality/` — physicality system tests
- `Generator/Agents/Drums/Performance/` — performance rendering tests
- `Generator/Agents/Drums/Diagnostics/` — diagnostics and feature extraction tests
- `Generator/Groove/` — groove system tests
- `Generator/Material/` — material system tests
- `Generator/Core/Randomization/` — RNG tests
- `TestFixtures/` — shared test utilities (`GrooveSnapshotHelper.cs`)

---

## 12) Key Architectural Conventions

### Timing
- Bars: 1-based
- Beats: 1-based, fractional
- All tick calculations use `MusicConstants.TicksPerQuarterNote`
- PartTrackEvent uses `AbsoluteTimeTicks`
- Material uses `MaterialLocal` domain

### Determinism
- Same seed → identical output
- Variation tied to (seed, groove name, section type, bar index)
- Randomness is tie-break only

### Validation
- Harmony validated before generation
- MIDI export validates sorted event ordering
- Anchor onsets preserved from groove presets
- Physicality filter ensures playable output

---

## 13) File Organization

| Folder | Contents |
|--------|----------|
| `Generator/Core/` | Entry point (`Generator.cs`), RNG (`Rng.cs`, `RandomPurpose.cs`), overlap helpers (`NoteOverlapHelper.cs`, `NoteOverlapPreventer.cs`, `CreateRepeatingNotes.cs`) |
| `Generator/Drums/` | Legacy drum track generator (`DrumTrackGenerator.cs`) |
| `Generator/Groove/` | Groove system (onset grid, strength classifier, feel/timing engines, anchor factory, preset library, coverage analyzer) |
| `Generator/Agents/` | Agent infrastructure |
| `Generator/Agents/Common/` | Shared agent contracts (`IMusicalOperator`, `AgentContext`, `IAgentMemory`, `AgentMemory`, `OperatorFamily`, `OperatorSelectionEngine`, `StyleConfiguration`, `StyleConfigurationLibrary`, `DecayCurve`, `FillShape`) |
| `Generator/Agents/Drums/` | Drummer agent implementation (`DrummerAgent`, `DrummerPolicyProvider`, `DrummerCandidateSource`, `DrummerContext`, `DrummerContextBuilder`, `DrummerMemory`, `GrooveBasedDrumGenerator`, drum-specific types, interfaces, selection engine, policy provider implementations) |
| `Generator/Agents/Drums/Operators/` | Drum operator families: `MicroAddition/` (7 operators), `SubdivisionTransform/` (5 operators), `PhrasePunctuation/` (7 operators), `PatternSubstitution/` (4 operators), `StyleIdiom/` (5 operators); base class (`DrumOperatorBase`), registry (`DrumOperatorRegistry`, `DrumOperatorRegistryBuilder`), interface (`IDrumOperator`) |
| `Generator/Agents/Drums/Physicality/` | Playability constraints (`LimbModel`, `LimbAssignment`, `LimbConflictDetector`, `StickingRules`, `StickingValidation`, `PhysicalityFilter`, `PhysicalityRules`) |
| `Generator/Agents/Drums/Performance/` | Performance rendering (`DrummerVelocityShaper`, `DrummerTimingShaper`, `DrumArticulationMapper`, `DynamicIntent`, `TimingIntent`, `DrumArticulation`, `DrummerVelocityHintSettings`, `DrummerTimingHintSettings`) |
| `Generator/Agents/Drums/Diagnostics/` | Decision tracing (`DrummerDiagnosticsCollector`, `DrummerDiagnostics`); MIDI feature extraction (event extractors, pattern analysis, coordination analysis, structural markers, velocity/timing analysis, serializers) |
| `Generator/Material/` | Material/motif system (`MotifPlacementPlanner`, `MotifPresenceMap`, `MotifRenderer`, `OnsetSlot`) |
| `Generator/TestSetups/` | Test design data (`TestDesigns.cs`) and integration tests |
| `Song/` | Data model (Bar, Section, Harmony, Tempo, Timing, PartTrack, Voices, Lyrics, Material) |
| `Song/Bar/` | Bar and BarTrack timing ruler |
| `Song/Section/` | Section, SectionTrack, SectionProfile |
| `Song/Harmony/` | Harmony system (HarmonyTrack, HarmonyEvent, HarmonyPitchContext, HarmonyPitchContextBuilder, HarmonyValidator, HarmonyEventNormalizer, HarmonyPolicy, ChordRealization, VoiceLeadingSelector, CompVoicingSelector, StrumTimingEngine, PitchClassUtils, diagnostics) |
| `Song/Tempo/` | TempoTrack, TempoEvent |
| `Song/Timing/` | TimingTrack, TimingEvent |
| `Song/PartTrack/` | PartTrack, PartTrackEvent, PartTrackEventType |
| `Song/Voices/` | VoiceSet, Voice, VoiceCatalog |
| `Song/Lyrics/` | Lyric classes, LyricTrack, LyricPhoneticsHelper, WordParser |
| `Song/Material/` | Material system (MaterialBank, MotifSpec, MotifPlacement, MotifPlacementPlan, PartTrackMeta, PartTrackDomain, PartTrackKind, MaterialKind, MaterialProvenance, ContourIntent, RegisterIntent, TonePolicy, validation classes) |
| `Converters/` | PartTrack ↔ MIDI conversion (3-step export pipeline, import pipeline, update logic) |
| `Midi/` | MIDI I/O and playback services (`MidiIoService`, `MidiPlaybackService`, `MidiSongDocument`, `Player`, `AppState`, `MidiVoices`) |
| `Writer/` | WinForms UI (WriterForm, editor forms, option form, song grid) |
| `Writer/WriterForm/` | WriterForm main class, event handlers, grid operations, data management, test song generation, command handlers |
| `Writer/EditorForms/` | Editor forms (GrooveEditorForm, HarmonyEditorForm, SectionEditorForm, TempoEditorForm, TimingEditorForm, LyricEditorForm, VoiceSelectorForm) |
| `Writer/OptionForm/` | OptionForm for application settings |
| `Writer/SongGrid/` | Song grid components (SongGridManager, GridControlLinesManager, PartTrackViewer, PlaybackProgressTracker, MeasureChangedEventArgs, MusicCalculations) |
| `MainForm/` | MainForm (MDI container) |
| `Core/` | Globals, constants (`MusicConstants`), helpers (`Helpers`, `ObjectViewer`) |
| `Errors/` | Exception handling (`GlobalExceptionHandler`, `MessageBoxHelper`, `Tracer`, `MidiImportException`) |
| `Properties/` | Resources |

---

## 14) Agent Infrastructure

Location: `Generator/Agents/Common/`

Shared contracts for all instrument agents (Drums, Guitar, Keys, Bass, Vocals).

### Core Interfaces
- `IMusicalOperator<TCandidate>` — generic operator interface
- `IAgentMemory` — memory interface for anti-repetition
- `AgentContext` — shared immutable context record
- `OperatorFamily` — operator classification enum

### Common Components
- `AgentMemory` — concrete memory implementation with circular buffer
- `OperatorSelectionEngine<TCandidate>` — weighted selection with scoring
- `StyleConfiguration` — per-style configuration (weights, caps, feel rules, grid rules)
- `StyleConfigurationLibrary` — registry with genre presets

---

## 15) Drummer Agent Architecture

Location: `Generator/Agents/Drums/`

### Core Components

**DrummerContext** — extends AgentContext with drum-specific fields
- Active roles
- Last kick/snare beats
- Current hat mode/subdivision
- Fill window status
- Section boundary status
- Backbeat beats

**DrumCandidate** — drum event candidate
- Operator ID, role, bar, beat
- Onset strength
- Velocity/timing hints (nullable)
- Articulation hint
- Fill role
- Score

**DrummerPolicyProvider** — implements `IDrumPolicyProvider`
- Computes density overrides from section/energy
- Gates fill operators based on context/memory
- Provides timing feel and velocity bias overrides
- Returns `DrumPolicyDecision` with density targets, caps, and weights

**DrummerCandidateSource** — implements `IDrumCandidateSource`
- Queries enabled operators from `DrumOperatorRegistry`
- Generates candidates via operator calls (all 28 operators)
- Maps `DrumCandidate` → `DrumCandidateGroup`
- Groups by operator family
- Applies physicality filter before returning

**DrummerMemory** — extends AgentMemory
- Tracks last fill shape
- Tracks chorus crash patterns
- Tracks hat mode history
- Tracks ghost note frequency

**DrumOperatorRegistry** — operator discovery and filtering
- Registers all 28 operators across 5 families
- Filters by style configuration
- Filters by policy allow list
- Immutable after freeze via `DrumOperatorRegistryBuilder`

**DrummerAgent** — facade
- Delegates to `DrummerPolicyProvider` and `DrummerCandidateSource`
- Implements both `IDrumPolicyProvider` and `IDrumCandidateSource`
- Owns memory and registry instances
- Provides unified interface for `GrooveBasedDrumGenerator`

**GrooveBasedDrumGenerator** — pipeline orchestrator
- Takes `IDrumPolicyProvider` + `IDrumCandidateSource` (typically `DrummerAgent`)
- Uses `DrumSelectionEngine` for weighted selection
- Enforces density targets and caps from policy decisions
- Combines anchors + selected operator candidates
- Converts to MIDI via `PartTrack`

### Drum-Specific Types

**DrumBarContext** — bar context for drum generation
- Section metadata
- Phrase position
- Energy level
- Fill window status

**DrumBarContextBuilder** — builds `DrumBarContext` from `SongContext`

**DrumPolicyDecision** — policy result
- Density targets per role
- Operator caps
- Weights
- Feel/timing overrides

**DrumOnsetCandidate** — drum-specific candidate (simplified)
- No conversion methods to generic types (GC-Epic)
- Direct onset representation

**DrumCandidateGroup** — group of related candidates
- No conversion methods to generic types (GC-Epic)
- Operator family grouping

**DrumGrooveOnsetFactory** — creates `GrooveOnset` from drum candidates
- `FromVariation` creates onset directly (GC-Epic updated in GC-5)
- `FromWeightedCandidate` for weighted selection results

**DrumDensityCalculator** — computes density targets
- Section-aware density calculation
- Energy-based adjustments

**RoleDensityTarget** — target density per role
- Target onset count
- Min/max bounds

**DefaultDrumPolicyProvider** — fallback policy provider
- Returns default policies when no custom provider
- Used for testing and simple cases

**DrumSelectionEngine** — weighted selection engine
- Selects candidates until density target reached
- Respects caps and weights from policy
- Deterministic tie-breaking

**DrumWeightedCandidateSelector** — candidate selection with scoring
- Applies weights from style configuration
- Handles protected candidates

**DrumCandidateMapper** — maps between candidate representations
- Handles conversion logic for pipeline stages

### Operator Families

Location: `Generator/Agents/Drums/Operators/`

| Family | Count | Purpose |
|--------|-------|---------|
| MicroAddition | 7 | Ghost notes, pickups, embellishments |
| SubdivisionTransform | 5 | Timekeeping changes (hat lift/drop, ride swap) |
| PhrasePunctuation | 7 | Fills, crashes, section boundaries |
| PatternSubstitution | 4 | Groove pattern swaps (backbeat variants, half/double time) |
| StyleIdiom | 5 | Genre-specific moves (PopRock) |

### Physicality System

Location: `Generator/Agents/Drums/Physicality/`

**LimbModel** — maps roles to limbs (RightHand, LeftHand, RightFoot, LeftFoot)

**LimbConflictDetector** — detects same limb required for simultaneous events

**StickingRules** — validates hand/foot playability constraints
- `StickingValidation` helper class for rule enforcement

**PhysicalityFilter** — integrates limb conflicts, sticking rules, and overcrowding prevention
- Enforces per-role, per-beat, and per-bar density caps
- Respects protected candidates
- Deterministic pruning with tie-break
- **PhysicalityRules** — consolidated physicality constraints

**LimbAssignment** — assignment of roles to specific limbs

### Performance Rendering

Location: `Generator/Agents/Drums/Performance/`

**DrummerVelocityShaper** — provides style-aware velocity hints
- Classifies dynamic intent (Low, MediumLow, Medium, StrongAccent, PeakAccent, FillRamp)
- Maps intent to numeric targets via style configuration
- Energy-aware adjustments
- Returns velocity hints before final MIDI conversion

**DrummerTimingShaper** — provides style-aware timing hints
- Classifies timing intent (OnTop, SlightlyAhead, SlightlyBehind, Rushed, LaidBack)
- Maps intent to tick offsets via style configuration
- Deterministic jitter
- Returns timing hints before `RoleTimingEngine`

**DrumArticulationMapper** — maps articulations to GM2 MIDI notes
- Handles `DrumArticulation` enum (Standard, OpenRim, ClosedRim, etc.)
- Graceful fallback hierarchy
- Always playable output

**DynamicIntent** — enum for velocity classification
**TimingIntent** — enum for timing feel classification
**DrumArticulation** — enum for articulation types
**DrummerVelocityHintSettings** — style-specific velocity mapping config
**DrummerTimingHintSettings** — style-specific timing mapping config

### Diagnostics

Location: `Generator/Agents/Drums/Diagnostics/`

**DrummerDiagnosticsCollector** — opt-in decision tracing
- Captures operator consideration/selection/rejection
- Captures memory state snapshots
- Captures density comparisons
- Captures physicality violations
- Zero-cost when disabled
- Produces `DrummerDiagnostics` result

**DrummerDiagnostics** — diagnostics result container
- Per-bar decision traces
- Operator statistics
- Memory snapshots

**Drum Feature Extraction** — MIDI analysis for benchmark workflows
- Raw event extraction with role mapping (`DrumTrackEventExtractor`, `DrumMidiEvent`, `DrumRoleMapper`)
- Per-bar pattern fingerprinting (`BarPatternExtractor`, `BarPatternFingerprint`)
- Beat position matrices (`BeatPositionMatrixBuilder`, `BeatPositionMatrix`)
- Pattern repetition/similarity detection (`PatternRepetitionDetector`, `PatternSimilarityAnalyzer`, `PatternRepetitionData`, `PatternSimilarityData`, `SequencePatternDetector`, `SequencePatternData`)
- Cross-role coordination analysis (`CrossRoleCoordinationExtractor`, `CrossRoleCoordinationData`)
- Anchor candidate detection (`AnchorCandidateExtractor`, `AnchorCandidateData`)
- Structural marker detection (`StructuralMarkerDetector`, `StructuralMarkerData`)
- Velocity dynamics and timing feel analysis (`VelocityDynamicsExtractor`, `VelocityDynamicsData`, `TimingFeelExtractor`, `TimingFeelData`)
- Bar onset statistics (`BarOnsetStatsExtractor`, `BarOnsetStats`)
- Feature data builders (`DrumTrackFeatureDataBuilder`, `DrumTrackExtendedFeatureDataBuilder`)
- Feature data containers (`DrumTrackFeatureData`, `DrumTrackExtendedFeatureData`)
- Serializers (`DrumFeatureDataSerializer`, `DrumExtendedFeatureDataSerializer`)

---

## 16) Current System State

### Completed Components
- Groove system simplified (Stories A1-H1, 5.2, 5.3, GC-Epic)
  - Anchor layer infrastructure complete
  - Variation catalog removed (GC-Epic)
  - Protection policies removed (Story 5.3)
  - SegmentGrooveProfiles removed (Story 5.2)
- Drummer agent complete (Stories 1.1-3.6, 4.1-4.4, 6.1-6.3, 7.1, 7.2a-7.2b, 8.1, RF-2)
  - `IDrumPolicyProvider` and `IDrumCandidateSource` interfaces
  - `GrooveBasedDrumGenerator` orchestrator
  - All 28 operators across 5 families
  - Physicality system (limb model, sticking rules, filter)
  - Performance rendering (velocity/timing shapers, articulation mapper)
  - Memory system (anti-repetition, pattern tracking)
  - Operator registry with style filtering
- Material system data model complete
  - MaterialBank, MotifSpec, MotifPlacement
  - MotifRenderer for rendering motifs to PartTracks
  - MotifPresenceMap for coordination
- Drummer diagnostics and feature extraction complete
  - DrummerDiagnosticsCollector for decision tracing
  - Comprehensive MIDI feature extraction pipeline
  - Pattern analysis, coordination detection, structural markers

### Active Development
- Groove Cleanup Epic (GC-1 through GC-7) in progress
  - Removing variation infrastructure from Groove namespace
  - Consolidating all variation in Drummer Agent
- Integration testing and refinement
  - GrooveBasedDrumGenerator + DrummerAgent wiring
  - End-to-end testing with audio verification

### Future Work
- Complete Groove Cleanup Epic (GC-1 through GC-7)
- Additional style configurations (Jazz, Funk, Metal, etc.)
- Guitar, Keys, Bass, Vocal agents
- Motif placement planning integration
- Additional genre style presets

### Known Architectural State
- **Groove System:** Simplified to anchors only; all variation handled by agents
- **Drummer Agent:** Fully implemented with operator-based architecture
- **Pipeline:** Uses `GrooveBasedDrumGenerator` with `IDrumPolicyProvider` + `IDrumCandidateSource`
- **Legacy Code:** `DrumTrackGenerator` exists but is superseded by `GrooveBasedDrumGenerator`

---

