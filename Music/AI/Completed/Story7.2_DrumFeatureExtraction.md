# Story 7.2 — Comprehensive Drum Feature Extraction for Benchmark Analysis

**Epic:** Human Drummer Agent  
**Stage:** 7 — Diagnostics & Tuning  
**Status:** Redesigned (replaces original Story 7.2)

---

## Intent

Enable a feedback loop where MIDI files from known genres/artists are analyzed to inform how groove anchors, style configuration, and operator settings should be tuned so that:

1. The analyzed tracks are within the realm of possibility for this application
2. Settings adjustments are data-driven, not guesswork
3. Alternative groove anchors can be identified when variance suggests a different base pattern

**This story is DATA COLLECTION ONLY.** Analysis and recommendation logic comes in future stages.

---

## Key Insight: Pattern-Level vs Aggregate Statistics

The original story focused on aggregate statistics (density curves, average syncopation). While useful, aggregate stats cannot:

- Identify if a different groove anchor would better fit the source material
- Detect recurring patterns (motifs, riffs, fill shapes)
- Capture inter-instrument coordination (kick-snare relationships)
- Distinguish "high variance from anchor" from "different anchor + low variance"

**This redesign captures pattern-level data that enables both aggregate AND structural analysis.**

---

## Story 7.2a — Raw Event Extraction and Per-Bar Pattern Capture

**As a** developer  
**I want** to extract raw events and per-bar patterns from MIDI drum tracks  
**So that** I have the foundational data for all downstream analysis

### Scope

- Parse MIDI drum tracks into normalized event format
- Capture per-bar pattern fingerprints
- Extract per-onset metadata (velocity, timing offset, role)
- Support variable time signatures
- Store data in serializable format

### Acceptance Criteria

#### 7.2a.1 — Raw Event Extraction (`DrumTrackEventExtractor`)

- [ ] Create `DrumMidiEvent` record:
  ```csharp
  public sealed record DrumMidiEvent
  {
      public required int BarNumber { get; init; }        // 1-based
      public required decimal Beat { get; init; }         // 1-based, fractional
      public required string Role { get; init; }          // Normalized: "Kick", "Snare", etc.
      public required int MidiNote { get; init; }         // Original GM2 note (36-81 range)
      public required int Velocity { get; init; }         // 1-127
      public required int DurationTicks { get; init; }    // Note length
      public required long AbsoluteTimeTicks { get; init; }
      public int? TimingOffsetTicks { get; init; }        // Deviation from grid (computed)
  }
  ```

- [ ] Create `DrumTrackEventExtractor` class:
  - [ ] Input: `PartTrack` (drum track) + `BarTrack` (timing context)
  - [ ] Output: `IReadOnlyList<DrumMidiEvent>` sorted by absolute time
  - [ ] Map MIDI notes to roles using GM2 standard (reuse `DrumArticulationMapper` knowledge)
  - [ ] Compute `TimingOffsetTicks` as deviation from nearest quantized grid position
  - [ ] Handle multiple time signatures across the track

- [ ] Create `DrumRoleMapper` helper:
  - [ ] Map GM2 drum notes (36-81) to normalized role names
  - [ ] Group articulations to base role (e.g., 38 + 40 → "Snare")
  - [ ] Return unknown notes as "Unknown:{midiNote}"

#### 7.2a.2 — Per-Bar Pattern Fingerprint (`BarPatternFingerprint`)

- [ ] Create `BarPatternFingerprint` record:
  ```csharp
  public sealed record BarPatternFingerprint
  {
      public required int BarNumber { get; init; }
      public required int BeatsPerBar { get; init; }
      
      // Role presence bitmap per beat position (quantized to 16th note grid)
      // Key: role name, Value: bitmask where bit N = onset at grid position N
      public required IReadOnlyDictionary<string, long> RoleBitmasks { get; init; }
      
      // Velocity profile per role (average velocity at each hit position)
      public required IReadOnlyDictionary<string, IReadOnlyList<int>> RoleVelocities { get; init; }
      
      // Combined hash for quick pattern comparison
      public required string PatternHash { get; init; }
      
      // Event count per role
      public required IReadOnlyDictionary<string, int> RoleEventCounts { get; init; }
  }
  ```

- [ ] Create `BarPatternExtractor` class:
  - [ ] Input: `IReadOnlyList<DrumMidiEvent>` for a single bar + `beatsPerBar`
  - [ ] Output: `BarPatternFingerprint`
  - [ ] Quantize events to 16th note grid (48 positions for 4/4)
  - [ ] Generate deterministic hash from role bitmasks
  - [ ] Support time signatures: 2/4, 3/4, 4/4, 5/4, 6/4, 6/8, 7/4, 12/8

- [ ] Pattern hash algorithm:
  - [ ] Sorted concatenation of `{role}:{bitmask}` pairs
  - [ ] SHA256 truncated to 16 hex chars for storage efficiency
  - [ ] Same pattern → same hash (deterministic)

#### 7.2a.3 — Per-Role Beat-Position Matrix (`BeatPositionMatrix`)

- [ ] Create `BeatPositionMatrix` record:
  ```csharp
  public sealed record BeatPositionMatrix
  {
      public required string Role { get; init; }
      public required int TotalBars { get; init; }
      public required int GridResolution { get; init; }  // 16 for 16th notes in 4/4
      
      // [barIndex, gridPosition] → (isHit, velocity, timingOffset)
      public required IReadOnlyList<BeatPositionSlot?[]> BarSlots { get; init; }
  }
  
  public sealed record BeatPositionSlot(int Velocity, int TimingOffsetTicks);
  ```

- [ ] Create `BeatPositionMatrixBuilder` class:
  - [ ] Input: `IReadOnlyList<DrumMidiEvent>`, `BarTrack`, `role`
  - [ ] Output: `BeatPositionMatrix` for that role
  - [ ] Enables rapid pattern comparison across bars

#### 7.2a.4 — Onset Statistics Per Bar (`BarOnsetStats`)

- [ ] Create `BarOnsetStats` record:
  ```csharp
  public sealed record BarOnsetStats
  {
      public required int BarNumber { get; init; }
      public required int TotalHits { get; init; }
      public required IReadOnlyDictionary<string, int> HitsPerRole { get; init; }
      
      // Velocity statistics
      public required double AverageVelocity { get; init; }
      public required int MinVelocity { get; init; }
      public required int MaxVelocity { get; init; }
      public required IReadOnlyDictionary<string, double> AverageVelocityPerRole { get; init; }
      
      // Timing statistics
      public required double AverageTimingOffset { get; init; }
      public required int MinTimingOffset { get; init; }
      public required int MaxTimingOffset { get; init; }
      
      // Beat distribution (how many hits per beat position, grouped by beat)
      // Index = beat number (0-based), Value = hit count on that beat
      public required IReadOnlyList<int> HitsPerBeat { get; init; }
      
      // Offbeat ratio: hits not on downbeats / total hits
      public required double OffbeatRatio { get; init; }
  }
  ```

- [ ] Create `BarOnsetStatsExtractor` class:
  - [ ] Input: `IReadOnlyList<DrumMidiEvent>` for a single bar, `beatsPerBar`
  - [ ] Output: `BarOnsetStats`

#### 7.2a.5 — Track-Level Container (`DrumTrackFeatureData`)

- [ ] Create `DrumTrackFeatureData` record (main container):
  ```csharp
  public sealed record DrumTrackFeatureData
  {
      // Metadata
      public required string TrackId { get; init; }
      public required string? GenreHint { get; init; }       // User-provided genre
      public required string? ArtistHint { get; init; }      // User-provided artist (optional)
      public required int TotalBars { get; init; }
      public required int DefaultBeatsPerBar { get; init; }
      public required int TempoEstimateBpm { get; init; }
      
      // Raw events (all drum hits in the track)
      public required IReadOnlyList<DrumMidiEvent> Events { get; init; }
      
      // Per-bar data
      public required IReadOnlyList<BarPatternFingerprint> BarPatterns { get; init; }
      public required IReadOnlyList<BarOnsetStats> BarStats { get; init; }
      
      // Per-role matrices
      public required IReadOnlyDictionary<string, BeatPositionMatrix> RoleMatrices { get; init; }
      
      // Detected roles in this track
      public required IReadOnlySet<string> ActiveRoles { get; init; }
  }
  ```

- [ ] Create `DrumTrackFeatureDataBuilder` class:
  - [ ] Orchestrates extraction pipeline
  - [ ] Input: `PartTrack` + `BarTrack` + optional genre/artist hints
  - [ ] Output: `DrumTrackFeatureData`

#### 7.2a.6 — Serialization Support

- [ ] All records must be JSON serializable
- [ ] Create `DrumFeatureDataSerializer` static class:
  - [ ] `Serialize(DrumTrackFeatureData) → string` (JSON)
  - [ ] `Deserialize(string) → DrumTrackFeatureData`
  - [ ] Compact format option (omit null fields)
- [ ] Include version field in serialized output for schema evolution

#### 7.2a.7 — Unit Tests

- [ ] Test MIDI note → role mapping for all GM2 drum notes
- [ ] Test pattern fingerprint determinism (same events → same hash)
- [ ] Test beat position matrix construction for various time signatures
- [ ] Test timing offset computation accuracy
- [ ] Test serialization round-trip
- [ ] Test with empty track, single-bar track, multi-bar track

### Files to Create (Story 7.2a)

```
Generator/Agents/Drums/Diagnostics/
  ├── DrumMidiEvent.cs                   # Raw event record
  ├── DrumRoleMapper.cs                  # MIDI note → role mapping
  ├── DrumTrackEventExtractor.cs         # Extract events from PartTrack
  ├── BarPatternFingerprint.cs           # Per-bar pattern record
  ├── BarPatternExtractor.cs             # Extract pattern from events
  ├── BeatPositionMatrix.cs              # Role × bar × position matrix
  ├── BeatPositionMatrixBuilder.cs       # Build matrix from events
  ├── BarOnsetStats.cs                   # Per-bar statistics
  ├── BarOnsetStatsExtractor.cs          # Extract stats from events
  ├── DrumTrackFeatureData.cs            # Top-level container
  ├── DrumTrackFeatureDataBuilder.cs     # Orchestrator
  └── DrumFeatureDataSerializer.cs       # JSON serialization

Music.Tests/Generator/Agents/Drums/Diagnostics/
  ├── DrumRoleMapperTests.cs
  ├── BarPatternFingerprintTests.cs
  ├── BeatPositionMatrixTests.cs
  └── DrumTrackFeatureDataTests.cs
```

---

## Story 7.2b — Pattern Detection and Cross-Instrument Analysis Data

**As a** developer  
**I want** to capture pattern repetition, cross-instrument coordination, and structural markers  
**So that** analysis can identify groove anchors, fills, motifs, and multi-instrument relationships

### Scope

- Detect repeating patterns across bars
- Capture cross-role coordination (kick-snare, bass-kick)
- Identify structural elements (potential fills, crashes, section changes)
- Detect evolving patterns (same base, slight variation)
- All data collection only — no recommendations

### Acceptance Criteria

#### 7.2b.1 — Pattern Repetition Detection (`PatternRepetitionData`)

- [ ] Create `PatternRepetitionData` record:
  ```csharp
  public sealed record PatternRepetitionData
  {
      // Pattern hash → list of bar numbers where it appears
      public required IReadOnlyDictionary<string, IReadOnlyList<int>> PatternOccurrences { get; init; }
      
      // Unique pattern count
      public required int UniquePatternCount { get; init; }
      
      // Most common patterns (top 10 by occurrence count)
      public required IReadOnlyList<PatternFrequency> MostCommonPatterns { get; init; }
      
      // Consecutive repetition runs (same pattern for N bars in a row)
      public required IReadOnlyList<PatternRun> ConsecutiveRuns { get; init; }
  }
  
  public sealed record PatternFrequency(
      string PatternHash, 
      int OccurrenceCount, 
      IReadOnlyList<int> BarNumbers);
  
  public sealed record PatternRun(
      string PatternHash, 
      int StartBar, 
      int EndBar, 
      int Length);
  ```

- [ ] Create `PatternRepetitionDetector` class:
  - [ ] Input: `IReadOnlyList<BarPatternFingerprint>`
  - [ ] Output: `PatternRepetitionData`
  - [ ] Identify runs of 2+ consecutive identical patterns
  - [ ] Track all occurrences of each unique pattern

#### 7.2b.2 — Pattern Similarity Analysis (`PatternSimilarityData`)

- [ ] Create `PatternSimilarityData` record:
  ```csharp
  public sealed record PatternSimilarityData
  {
      // Pairs of patterns that are similar (Jaccard similarity > threshold)
      public required IReadOnlyList<SimilarPatternPair> SimilarPairs { get; init; }
      
      // Pattern families: groups of patterns that are variations of each other
      public required IReadOnlyList<PatternFamily> PatternFamilies { get; init; }
  }
  
  public sealed record SimilarPatternPair(
      string PatternHashA, 
      string PatternHashB, 
      double Similarity);  // 0.0-1.0
  
  public sealed record PatternFamily(
      string BasePatternHash,
      IReadOnlyList<string> VariantHashes,
      IReadOnlyList<int> AllBarNumbers);
  ```

- [ ] Create `PatternSimilarityAnalyzer` class:
  - [ ] Input: `IReadOnlyList<BarPatternFingerprint>`
  - [ ] Output: `PatternSimilarityData`
  - [ ] Use Jaccard similarity on role bitmasks
  - [ ] Threshold for "similar": >= 0.7 similarity
  - [ ] Group similar patterns into families

#### 7.2b.3 — Cross-Role Coordination Data (`CrossRoleCoordinationData`)

- [ ] Create `CrossRoleCoordinationData` record:
  ```csharp
  public sealed record CrossRoleCoordinationData
  {
      // Coincidence matrix: how often two roles hit at the same beat position
      // Key: "{roleA}+{roleB}" (alphabetically sorted), Value: count
      public required IReadOnlyDictionary<string, int> CoincidenceCount { get; init; }
      
      // Per-position coincidence for key role pairs
      public required IReadOnlyList<RolePairCoincidence> RolePairDetails { get; init; }
      
      // Lock score: how tightly two roles follow each other
      // Higher = more coordinated (e.g., bass following kick)
      public required IReadOnlyDictionary<string, double> LockScores { get; init; }
  }
  
  public sealed record RolePairCoincidence(
      string RoleA,
      string RoleB,
      int TotalCoincidences,
      double CoincidenceRatio,  // Coincidences / min(roleAHits, roleBHits)
      // Beat positions where both hit (bitmask across all bars)
      long CommonPositionMask);
  ```

- [ ] Create `CrossRoleCoordinationExtractor` class:
  - [ ] Input: `IReadOnlyDictionary<string, BeatPositionMatrix>`
  - [ ] Output: `CrossRoleCoordinationData`
  - [ ] Compute pairwise coordination for: Kick-Snare, Kick-Bass, Snare-Hat, Hat-Crash

#### 7.2b.4 — Anchor Candidate Detection (`AnchorCandidateData`)

- [ ] Create `AnchorCandidateData` record:
  ```csharp
  public sealed record AnchorCandidateData
  {
      // Per-role: which beat positions are consistently hit
      // Key: role, Value: list of (gridPosition, consistencyRatio)
      public required IReadOnlyDictionary<string, IReadOnlyList<PositionConsistency>> RoleAnchors { get; init; }
      
      // Combined anchor pattern: positions consistently hit across the track
      public required IReadOnlyDictionary<string, long> ConsistentPositionMasks { get; init; }
      
      // Variance from common PopRock anchor (kick: 1, 3; snare: 2, 4; hat: all 8ths)
      public required AnchorVarianceFromReference PopRockAnchorVariance { get; init; }
  }
  
  public sealed record PositionConsistency(
      int GridPosition,          // 0-15 for 16th note grid in 4/4
      int HitCount,              // How many bars have a hit here
      int TotalBars,
      double ConsistencyRatio);  // HitCount / TotalBars
  
  public sealed record AnchorVarianceFromReference(
      string ReferenceName,      // "PopRockBasic"
      double OverallVarianceScore,  // 0.0 = perfect match, 1.0 = no match
      IReadOnlyDictionary<string, double> PerRoleVariance,
      IReadOnlyList<string> MissingAnchors,   // Expected but not found
      IReadOnlyList<string> ExtraAnchors);    // Found but not expected
  ```

- [ ] Create `AnchorCandidateExtractor` class:
  - [ ] Input: `IReadOnlyDictionary<string, BeatPositionMatrix>`, `GrooveInstanceLayer` (reference anchor)
  - [ ] Output: `AnchorCandidateData`
  - [ ] Threshold for "consistent": >= 80% of bars have hit at position
  - [ ] Compare against provided reference anchor

#### 7.2b.5 — Structural Marker Detection (`StructuralMarkerData`)

- [ ] Create `StructuralMarkerData` record:
  ```csharp
  public sealed record StructuralMarkerData
  {
      // Bars with significantly higher density (potential fills)
      public required IReadOnlyList<DensityAnomaly> HighDensityBars { get; init; }
      
      // Bars with significantly lower density (potential breakdowns/stops)
      public required IReadOnlyList<DensityAnomaly> LowDensityBars { get; init; }
      
      // Bars with crash cymbal hits (potential section starts)
      public required IReadOnlyList<int> CrashBars { get; init; }
      
      // Pattern change points (bar where pattern differs from previous)
      public required IReadOnlyList<PatternChangePoint> PatternChanges { get; init; }
      
      // Potential fill locations (high density + pattern change + before crash)
      public required IReadOnlyList<PotentialFill> PotentialFills { get; init; }
  }
  
  public sealed record DensityAnomaly(
      int BarNumber,
      int EventCount,
      double DeviationFromMean);  // Standard deviations from mean
  
  public sealed record PatternChangePoint(
      int BarNumber,
      string PreviousPatternHash,
      string NewPatternHash,
      double Similarity);  // How different (0.0 = completely different)
  
  public sealed record PotentialFill(
      int StartBar,
      int EndBar,  // Often same as StartBar for short fills
      double Confidence,  // 0.0-1.0 based on density + pattern + crash proximity
      IReadOnlyList<string> IndicatorReasons);  // Why we think it's a fill
  ```

- [ ] Create `StructuralMarkerDetector` class:
  - [ ] Input: `DrumTrackFeatureData`
  - [ ] Output: `StructuralMarkerData`
  - [ ] Density anomaly: > 2 standard deviations from mean
  - [ ] Pattern change: similarity < 0.5 with previous bar
  - [ ] Fill heuristics: high density + tom presence + before crash

#### 7.2b.6 — Multi-Bar Sequence Detection (`SequencePatternData`)

- [ ] Create `SequencePatternData` record:
  ```csharp
  public sealed record SequencePatternData
  {
      // Recurring 2-bar sequences
      public required IReadOnlyList<MultiBarSequence> TwoBarSequences { get; init; }
      
      // Recurring 4-bar sequences (common phrase length)
      public required IReadOnlyList<MultiBarSequence> FourBarSequences { get; init; }
      
      // Evolving sequences (A → A' patterns where A' is slight variation)
      public required IReadOnlyList<EvolvingSequence> EvolvingSequences { get; init; }
  }
  
  public sealed record MultiBarSequence(
      IReadOnlyList<string> PatternHashes,  // One per bar in sequence
      IReadOnlyList<int> Occurrences,       // Start bars where this sequence appears
      int SequenceLength);
  
  public sealed record EvolvingSequence(
      string BasePatternHash,
      IReadOnlyList<EvolutionStep> Steps,
      int TotalBarsSpanned);
  
  public sealed record EvolutionStep(
      int BarNumber,
      string PatternHash,
      double SimilarityToBase);
  ```

- [ ] Create `SequencePatternDetector` class:
  - [ ] Input: `IReadOnlyList<BarPatternFingerprint>`
  - [ ] Output: `SequencePatternData`
  - [ ] Detect 2-bar and 4-bar repeating sequences
  - [ ] Detect gradual evolution (similarity decreasing over bars)

#### 7.2b.7 — Velocity Dynamics Data (`VelocityDynamicsData`)

- [ ] Create `VelocityDynamicsData` record:
  ```csharp
  public sealed record VelocityDynamicsData
  {
      // Per-role velocity distribution
      public required IReadOnlyDictionary<string, VelocityDistribution> RoleDistributions { get; init; }
      
      // Velocity by beat position (average velocity at each grid position)
      public required IReadOnlyDictionary<string, IReadOnlyList<double>> RoleVelocityByPosition { get; init; }
      
      // Accent patterns: positions with above-average velocity
      public required IReadOnlyDictionary<string, long> AccentMasks { get; init; }
      
      // Ghost note positions: positions with below-average velocity (for snare)
      public required IReadOnlyList<int> GhostPositions { get; init; }
  }
  
  public sealed record VelocityDistribution(
      double Mean,
      double StdDev,
      int Min,
      int Max,
      IReadOnlyList<int> Histogram);  // 8 buckets: 0-15, 16-31, ..., 112-127
  ```

- [ ] Create `VelocityDynamicsExtractor` class:
  - [ ] Input: `DrumTrackFeatureData`
  - [ ] Output: `VelocityDynamicsData`
  - [ ] Compute per-role velocity distributions
  - [ ] Identify accent positions (velocity > mean + 0.5*stdDev)
  - [ ] Identify ghost positions (snare velocity < mean - 0.5*stdDev)

#### 7.2b.8 — Timing Feel Data (`TimingFeelData`)

- [ ] Create `TimingFeelData` record:
  ```csharp
  public sealed record TimingFeelData
  {
      // Per-role average timing offset
      public required IReadOnlyDictionary<string, double> RoleAverageOffset { get; init; }
      
      // Timing offset distribution per role
      public required IReadOnlyDictionary<string, TimingDistribution> RoleTimingDistributions { get; init; }
      
      // Swing detection: ratio of long-short 8th note pairs
      public required double SwingRatio { get; init; }  // 1.0 = straight, 2.0 = triplet swing
      
      // Overall feel classification data
      public required double AheadBehindScore { get; init; }  // Negative = ahead, positive = behind
      public required double TimingConsistency { get; init; }  // 0.0-1.0, higher = more consistent
  }
  
  public sealed record TimingDistribution(
      double Mean,
      double StdDev,
      int MinOffset,
      int MaxOffset,
      IReadOnlyList<int> Histogram);  // Buckets from -20 to +20 ticks
  ```

- [ ] Create `TimingFeelExtractor` class:
  - [ ] Input: `DrumTrackFeatureData`
  - [ ] Output: `TimingFeelData`
  - [ ] Detect swing by measuring 8th note pair ratios
  - [ ] Compute per-role timing feel

#### 7.2b.9 — Extended Feature Container (`DrumTrackExtendedFeatureData`)

- [ ] Create `DrumTrackExtendedFeatureData` record (extends 7.2a container):
  ```csharp
  public sealed record DrumTrackExtendedFeatureData
  {
      // Base data from Story 7.2a
      public required DrumTrackFeatureData BaseData { get; init; }
      
      // Pattern analysis (7.2b)
      public required PatternRepetitionData PatternRepetition { get; init; }
      public required PatternSimilarityData PatternSimilarity { get; init; }
      public required SequencePatternData SequencePatterns { get; init; }
      
      // Cross-role analysis
      public required CrossRoleCoordinationData CrossRoleCoordination { get; init; }
      
      // Anchor analysis
      public required AnchorCandidateData AnchorCandidates { get; init; }
      
      // Structural analysis
      public required StructuralMarkerData StructuralMarkers { get; init; }
      
      // Performance analysis
      public required VelocityDynamicsData VelocityDynamics { get; init; }
      public required TimingFeelData TimingFeel { get; init; }
  }
  ```

- [ ] Create `DrumTrackExtendedFeatureDataBuilder` class:
  - [ ] Orchestrates all extractors from 7.2b
  - [ ] Input: `DrumTrackFeatureData` (from 7.2a) + reference anchor
  - [ ] Output: `DrumTrackExtendedFeatureData`

#### 7.2b.10 — Serialization Support

- [ ] Extend `DrumFeatureDataSerializer` for extended data
- [ ] Support saving base and extended data together or separately
- [ ] Version field for schema evolution

#### 7.2b.11 — Unit Tests

- [ ] Test pattern repetition detection with known repeating patterns
- [ ] Test similarity calculation accuracy
- [ ] Test cross-role coincidence detection
- [ ] Test anchor variance from reference
- [ ] Test structural marker detection (known fills, crashes)
- [ ] Test multi-bar sequence detection
- [ ] Test velocity and timing extraction
- [ ] Test serialization round-trip

### Files to Create (Story 7.2b)

```
Generator/Agents/Drums/Diagnostics/
  ├── PatternRepetitionData.cs            # Pattern repetition records
  ├── PatternRepetitionDetector.cs        # Detect repeating patterns
  ├── PatternSimilarityData.cs            # Similarity analysis records
  ├── PatternSimilarityAnalyzer.cs        # Analyze pattern similarity
  ├── CrossRoleCoordinationData.cs        # Cross-role coordination records
  ├── CrossRoleCoordinationExtractor.cs   # Extract coordination data
  ├── AnchorCandidateData.cs              # Anchor candidate records
  ├── AnchorCandidateExtractor.cs         # Detect potential anchors
  ├── StructuralMarkerData.cs             # Structural marker records
  ├── StructuralMarkerDetector.cs         # Detect fills, crashes, changes
  ├── SequencePatternData.cs              # Multi-bar sequence records
  ├── SequencePatternDetector.cs          # Detect 2-bar, 4-bar sequences
  ├── VelocityDynamicsData.cs             # Velocity dynamics records
  ├── VelocityDynamicsExtractor.cs        # Extract velocity patterns
  ├── TimingFeelData.cs                   # Timing feel records
  ├── TimingFeelExtractor.cs              # Extract timing feel
  ├── DrumTrackExtendedFeatureData.cs     # Extended container
  └── DrumTrackExtendedFeatureDataBuilder.cs  # Orchestrator

Music.Tests/Generator/Agents/Drums/Diagnostics/
  ├── PatternRepetitionTests.cs
  ├── PatternSimilarityTests.cs
  ├── CrossRoleCoordinationTests.cs
  ├── AnchorCandidateTests.cs
  ├── StructuralMarkerTests.cs
  ├── SequencePatternTests.cs
  ├── VelocityDynamicsTests.cs
  ├── TimingFeelTests.cs
  └── ExtendedFeatureDataTests.cs
```

---

## Data Collection Summary

### What This Data Enables (Future Analysis)

| Analysis Goal | Data Used |
|---------------|-----------|
| "Should I use a different anchor?" | `AnchorCandidateData.ConsistentPositionMasks`, `PopRockAnchorVariance` |
| "What patterns repeat?" | `PatternRepetitionData`, `SequencePatternData` |
| "How do kick and snare coordinate?" | `CrossRoleCoordinationData.RolePairDetails` for Kick-Snare |
| "Where are the fills?" | `StructuralMarkerData.PotentialFills` |
| "What variation settings would accommodate this?" | `PatternSimilarityData.PatternFamilies` (shows range of variation) |
| "Is this swing or straight feel?" | `TimingFeelData.SwingRatio` |
| "What accent pattern is used?" | `VelocityDynamicsData.AccentMasks` |
| "Does this match PopRock style?" | `AnchorCandidateData.PopRockAnchorVariance.OverallVarianceScore` |

### Key Design Decisions

1. **Bitmask representation**: 16th note grid as long bitmask (48 bits for 4/4) enables fast pattern comparison

2. **Pattern hash**: Quick equality check; detailed comparison only when hashes differ

3. **Reference anchor comparison**: Compare against known anchor (PopRock) to quantify variance

4. **Structural markers**: Density anomalies + pattern changes + crashes = likely section boundaries

5. **Multi-bar sequences**: 2-bar and 4-bar sequences capture common phrase structures

6. **Separation of extraction and analysis**: All records are data; no recommendations here

---

## Dependencies

- Story 7.2a has no dependencies beyond existing PartTrack/BarTrack infrastructure
- Story 7.2b depends on 7.2a completion (needs `DrumTrackFeatureData`)

---

## Estimated Effort

| Story | Complexity | Points |
|-------|------------|--------|
| 7.2a — Raw Event Extraction and Per-Bar Patterns | Medium | 8 |
| 7.2b — Pattern Detection and Cross-Instrument Analysis | Large | 13 |
| **Total** | | **21** |

---

## Non-Goals (Explicit Exclusions)

The following are **NOT** in scope for Stories 7.2a/7.2b:

1. **Analysis recommendations** — "Use this anchor instead" is a future stage
2. **Style setting suggestions** — "Adjust density target to X" is a future stage
3. **Generator tuning** — Modifying style configurations based on analysis
4. **MIDI import UI** — Loading files is handled elsewhere
5. **Multi-track coordination** — Bass-drum coordination across separate tracks (drums-only here)
6. **Audio analysis** — MIDI only; no audio feature extraction

---

## Future Integration (Stage 21)

Story 7.2a/7.2b provides the data foundation for Stage 21 (Musical Evaluation Loop):

```
Stage 21 Flow:
┌─────────────────────────────────────────────────────────────────┐
│ Import MIDI → Story 7.2a/7.2b extraction                        │
│        ↓                                                        │
│ DrumTrackExtendedFeatureData (saved as JSON)                    │
│        ↓                                                        │
│ Future: Analysis engine compares to generator capabilities      │
│        ↓                                                        │
│ Future: Recommendations (anchor change, setting adjustments)    │
│        ↓                                                        │
│ Future: Apply recommendations, regenerate, compare              │
└─────────────────────────────────────────────────────────────────┘
```

The data structures in 7.2a/7.2b are designed to support all these future analysis needs without modification.
