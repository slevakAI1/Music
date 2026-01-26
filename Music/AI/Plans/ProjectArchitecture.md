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
- `GroovePresetDefinition` — groove configuration
- `SegmentGrooveProfiles` — per-section overrides
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

### DrumTrackGenerator Pipeline
Location: `Generator/Drums/DrumTrackGenerator.cs`

Pipeline stages:
1. Build bar contexts
2. Merge protection hierarchy
3. Augment phrase hooks
4. Extract anchor onsets
5. Filter by subdivision
6. Filter by syncopation/anticipation
7. Filter by role presence
8. Apply protections
9. Apply feel timing
10. Apply role micro-timing
11. Generate operator candidates
12. Apply physicality filter
13. Select operators
14. Convert to MIDI events

---

## 5) Groove System Architecture

### Core Components
Location: `Generator/Groove/`

**GroovePresetDefinition** — container for groove configuration
- `GroovePresetIdentity`
- `AnchorLayer` — base pattern
- `ProtectionPolicy` — bundled policies
- `VariationCatalog` — optional candidates

**GrooveProtectionPolicy** — bundled policies
- `SubdivisionPolicy` — grid + feel
- `RoleConstraintPolicy` — vocabulary constraints
- `PhraseHookPolicy` — fill windows/cadence
- `TimingPolicy` — micro-timing rules
- `OrchestrationPolicy` — role presence
- `HierarchyLayers` — layered protections

**SegmentGrooveProfile** — per-section overrides
- Section-specific groove preset switching
- Enabled variation tags
- Density targets
- Feel/swing overrides

**GrooveOnset** — output type
- Role, bar, beat
- Strength, velocity, timing offset
- Provenance
- Protection flags

**GrooveBarPlan** — per-bar plan
- Base onsets (anchors)
- Selected variation onsets
- Final onsets (after constraints)
- Optional diagnostics

**GrooveBarDiagnostics** — decision trace (opt-in)
- Enabled tags
- Candidate statistics
- Filter decisions
- Selection decisions
- Prune events
- Final onset summary

### Groove Infrastructure Files

| Component | Responsibility |
|-----------|---------------|
| `OnsetGrid` / `OnsetGridBuilder` | Valid beat positions from subdivision policy |
| `OnsetStrengthClassifier` | Classify onset strength |
| `FeelTimingEngine` | Apply feel timing to offbeats |
| `RoleTimingEngine` | Apply per-role micro-timing |
| `ProtectionPerBarBuilder` | Merge protection layers per bar |
| `ProtectionApplier` | Apply must-hit/never-add/never-remove rules |
| `RolePresenceGate` | Check role presence in section |
| `RhythmVocabularyFilter` | Filter by syncopation/anticipation |
| `PhraseHookProtectionAugmenter` | Protect anchors near phrase ends |
| `BarContextBuilder` | Build per-bar context |
| `GrooveSelectionEngine` | Select candidates until target reached |
| `GrooveCapsEnforcer` | Enforce hard caps |
| `OverrideMergePolicyEnforcer` | Enforce override merge policy |
| `PartTrackBarCoverageAnalyzer` | Analyze per-bar fill state |

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

Contains test data for sections, harmony, tempo, timing, material, and UI.

### Music.Tests Project
Location: `Music.Tests/`

Framework: xUnit

Conventions:
- Constructor-based RNG initialization
- Method naming: `<Component>_<Condition>_<ExpectedResult>`
- `#region` blocks for organization

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
- Protection system ensures must-hit onsets

---

## 13) File Organization

| Folder | Contents |
|--------|----------|
| `Generator/Core/` | Entry point, RNG, overlap helpers |
| `Generator/Drums/` | Drum track generator |
| `Generator/Groove/` | Groove system (policies, selection, protection) |
| `Generator/Agents/` | Agent infrastructure |
| `Generator/Agents/Common/` | Shared agent contracts (IMusicalOperator, AgentContext, IAgentMemory, StyleConfiguration) |
| `Generator/Agents/Drums/` | Drummer agent implementation |
| `Generator/Agents/Drums/Operators/` | Drum operator families (MicroAddition, SubdivisionTransform, PhrasePunctuation, PatternSubstitution, StyleIdiom) |
| `Generator/Agents/Drums/Physicality/` | Playability constraints (LimbModel, StickingRules, PhysicalityFilter) |
| `Generator/Agents/Drums/Performance/` | Performance rendering (velocity/timing shapers, articulation mapper) |
| `Generator/Agents/Drums/Diagnostics/` | Decision tracing and MIDI feature extraction |
| `Song/` | Data model (Section, Harmony, Tempo, Timing, PartTrack, Bar, Voices, Lyrics) |
| `Song/Material/` | Material/motif system |
| `Converters/` | PartTrack ↔ MIDI conversion |
| `Midi/` | MIDI I/O and playback services |
| `Writer/` | WinForms UI |
| `Core/` | Globals, constants, helpers |
| `Errors/` | Exception handling |

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

**DrummerPolicyProvider** — implements `IGroovePolicyProvider`
- Computes density overrides from section/energy
- Gates fill operators based on context/memory
- Provides timing feel and velocity bias overrides

**DrummerCandidateSource** — implements `IGrooveCandidateSource`
- Queries enabled operators
- Generates candidates via operator calls
- Maps DrumCandidate → GrooveOnsetCandidate
- Groups by operator family
- Applies physicality filter

**DrummerMemory** — extends AgentMemory
- Tracks last fill shape
- Tracks chorus crash patterns
- Tracks hat mode history
- Tracks ghost note frequency

**DrumOperatorRegistry** — operator discovery and filtering
- Registers all 28 operators across 5 families
- Filters by style configuration
- Filters by policy allow list
- Immutable after freeze

**DrummerAgent** — facade
- Delegates to policy provider and candidate source
- Implements both `IGroovePolicyProvider` and `IGrooveCandidateSource`
- Owns memory and registry instances

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

**PhysicalityFilter** — integrates limb conflicts, sticking rules, and overcrowding prevention
- Enforces per-role, per-beat, and per-bar density caps
- Respects protected candidates
- Deterministic pruning with tie-break

### Performance Rendering

Location: `Generator/Agents/Drums/Performance/`

**DrummerVelocityShaper** — provides style-aware velocity hints
- Classifies dynamic intent (Low, MediumLow, Medium, StrongAccent, PeakAccent, FillRamp)
- Maps intent to numeric targets via style configuration
- Energy-aware adjustments
- Runs before groove VelocityShaper

**DrummerTimingShaper** — provides style-aware timing hints
- Classifies timing intent (OnTop, SlightlyAhead, SlightlyBehind, Rushed, LaidBack)
- Maps intent to tick offsets via style configuration
- Deterministic jitter
- Runs before groove RoleTimingEngine

**DrumArticulationMapper** — maps articulations to GM2 MIDI notes
- Graceful fallback hierarchy
- Always playable output

### Diagnostics

Location: `Generator/Agents/Drums/Diagnostics/`

**DrummerDiagnosticsCollector** — opt-in decision tracing
- Captures operator consideration/selection/rejection
- Captures memory state snapshots
- Captures density comparisons
- Captures physicality violations
- Zero-cost when disabled

**Drum Feature Extraction** — MIDI analysis for benchmark workflows
- Raw event extraction with role mapping
- Per-bar pattern fingerprinting
- Beat position matrices
- Pattern repetition/similarity detection
- Cross-role coordination analysis
- Anchor candidate detection
- Structural marker detection (fills, crashes, pattern changes)
- Velocity dynamics and timing feel analysis

---

## 16) Current System State

### Completed Components
- Groove system (Stories A1-H1)
- Drummer agent core (Stories 1.1-3.6, 4.1-4.4, 6.1-6.3, 7.1, 7.2a-7.2b, 8.1)
- Material system data model
- Drummer diagnostics and feature extraction

### Active Development
- Drummer agent integration testing
- Pop Rock style configuration tuning

### Future Work
- Guitar, Keys, Bass, Vocal agents
- Motif placement and rendering
- Additional genre styles

---

