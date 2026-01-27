# Pre-Analysis: Story 10.8.3 — End-to-End Regression Snapshot (Golden Test)

**Story ID:** 10.8.3  
**Status:** Pending  
**Epic:** Human Drummer Agent (Stage 10.8 — Integration & Testing)

---

## 1. Story Intent Summary

**What:** Create a golden-file regression test that generates a complete drum track with a known seed and section structure, serializes it to a snapshot file, and verifies that future runs produce identical output.

**Why:** This provides a safety net against accidental breaking changes during refactoring or feature additions. It locks in the complete behavior of the drummer agent system end-to-end, capturing not just the final MIDI output but also the decision-making process (which operators were used where).

**Who benefits:**
- **Developers:** Can confidently refactor knowing they'll catch unintended behavior changes
- **Generator:** Ensures determinism and behavioral consistency across code changes
- **QA/Testing:** Provides clear evidence when behavior intentionally changes (snapshot update) vs. when it breaks (test failure)

---

## 2. Acceptance Criteria Checklist

### Group A: Test Fixture Setup (AC1)
1. Create deterministic test fixture
2. Use known seed value (for reproducibility)
3. Define known section structure: Intro-Verse-Chorus-Verse-Chorus-Bridge-Chorus-Outro
4. Use Pop Rock style configuration

### Group B: Snapshot Generation (AC2)
5. Generate complete drum track using the fixture
6. Serialize per-bar data:
   - Onset positions (bar + beat)
   - Roles (Kick, Snare, Hat, etc.)
   - Velocities (MIDI 0-127)
   - Timing offsets (tick deviations)
7. Include operators used per bar (for transparency into decision-making)

### Group C: Snapshot Verification (AC3)
8. Assert snapshot matches expected output exactly
9. Every field must match (positions, roles, velocities, timing offsets, operator IDs)
10. No tolerance for variation (determinism requirement)

### Group D: Snapshot Update Mechanism (AC4)
11. Provide controlled way to update snapshot when behavior changes by design
12. Clear distinction between "test failure" (regression) vs. "intentional update" (feature change)

**Ambiguous/Unclear AC:**
- **AC4 mechanism:** What is the "controlled way"? Environment variable? Command-line flag? Manual file replacement? Needs clarification.
- **Snapshot format:** JSON is specified, but what is the exact schema? Should it be human-readable or optimized?
- **Diff reporting:** When test fails, how should differences be reported? Full diff? First N differences?

---

## 3. Dependencies & Integration Points

### Dependencies on Prior Stories
- **10.8.1 (Generator Integration):** Requires working `Generator.Generate(songContext, StyleConfiguration)` integration
- **10.2.3 (DrummerPolicyProvider):** Needs policy decisions for operator selection
- **10.2.4 (DrummerCandidateSource):** Needs operator-generated candidates
- **10.3.6 (Operator Registry):** All 28 operators must be registered
- **Story 1.4 (StyleConfiguration):** Requires `StyleConfigurationLibrary.PopRock` configuration
- **Story 7.1 (Diagnostics):** May need diagnostics to capture operator usage per bar

### Existing Code Interaction
**Inputs:**
- `Rng.Initialize(seed)` — deterministic RNG initialization
- `Generator.Generate(songContext, styleConfig)` — main generation entry point
- `SongContext` — container for all song data (bars, sections, groove, harmony)
- `StyleConfigurationLibrary.PopRock` — style configuration
- `BarTrack` — timing ruler
- `SectionTrack` — section structure
- `GroovePresetDefinition` — groove anchor pattern

**Outputs:**
- `PartTrack` — generated drum track with events
- `PartTrackNoteEvents` — list of MIDI events (sorted by time)
- Snapshot JSON file — serialized representation of output

### Provides for Future Stories
- **Regression protection:** All future development protected against breaking changes
- **Behavioral documentation:** Snapshot serves as executable specification
- **Change detection:** Immediate feedback when operator logic changes
- **Baseline for comparison:** Can be used to compare different seeds or styles

---

## 4. Inputs & Outputs

### Test Inputs
| Input | Source | Purpose |
|-------|--------|---------|
| **Seed** | Test constant (e.g., 42) | Deterministic RNG initialization |
| **Section structure** | Test fixture | Defines song form (8 sections total) |
| **Bar count** | Derived from sections | Total bars across all sections |
| **Style configuration** | `StyleConfigurationLibrary.PopRock` | Operator weights, caps, feel rules |
| **Groove preset** | Test helper method | Anchor pattern definition |
| **Time signature** | Test constant (4/4) | Timing context |
| **Tempo** | Test constant (120 BPM) | Timing context |

### Test Outputs
| Output | Format | Contents |
|--------|--------|----------|
| **Generated PartTrack** | C# object | Full drum track with events |
| **Snapshot file** | JSON | Serialized track + metadata |
| **Per-bar onset data** | JSON array | Bar number, beat, role, velocity, timing offset |
| **Per-bar operator usage** | JSON array | Bar number, list of operator IDs used |

### Configuration Read
- `StyleConfiguration.OperatorWeights` (affects operator selection frequency)
- `StyleConfiguration.RoleCaps` (affects max events per role)
- `StyleConfiguration.AllowedOperatorIds` (affects which operators can run)
- `DrummerPolicySettings` (affects fill windows, density modifiers)
- `PhysicalityRules` (affects playability filtering)

---

## 5. Constraints & Invariants

### MUST ALWAYS be true:
1. **Perfect determinism:** Same seed + same context → **identical** snapshot (not just similar)
2. **Snapshot immutability during test:** Snapshot file must not be modified during test execution
3. **Bar ordering:** Events in snapshot must be sorted by (BarNumber asc, Beat asc)
4. **Timing ordering:** Events within a bar must be sorted by AbsoluteTimeTicks
5. **Valid MIDI data:** All velocities 1-127, all note numbers in GM2 drum range
6. **Section contiguity:** Sections must be ordered and non-overlapping
7. **Operator transparency:** Every onset must be traceable to an operator (or anchor)

### Hard Limits:
- 8 sections (Intro, Verse, Chorus, Verse, Chorus, Bridge, Chorus, Outro)
- 4/4 time signature (48 ticks per 16th note, 480 ticks per quarter)
- PopRock style limits (e.g., max 24 hits per bar per role)
- Snapshot file size (should be reasonable, < 1 MB for typical track)

### Operation Order:
1. Initialize RNG with known seed
2. Build SongContext with fixed section structure
3. Call Generator.Generate() **once**
4. Serialize output to snapshot format
5. Compare with expected snapshot
6. Report differences (if any)
7. Fail test if differences found (unless update mode active)

---

## 6. Edge Cases to Test

### Boundary Conditions
- **Empty sections:** What if Intro has 0 bars? (Should error or handle gracefully)
- **Single-bar sections:** Minimum section length (affects fill window calculations)
- **Very long sections:** 16-bar chorus (tests memory window overflow)
- **Section transitions:** Boundary between sections (fill placement, crash placement)
- **First/last bars:** Edge cases at song start/end

### Determinism Edge Cases
- **Tie-breaking:** Multiple operators with identical scores (must break deterministically)
- **Floating-point precision:** Density calculations must be reproducible across platforms
- **RNG stream ordering:** Operator selection order must be stable
- **Memory state:** Memory must be identical at each bar for same inputs

### Serialization Edge Cases
- **Special characters in operator IDs:** Ensure JSON-safe strings
- **Null/missing values:** How to represent optional fields (articulation hints, timing offsets)
- **Large numbers:** AbsoluteTimeTicks can be large (ensure no overflow in JSON)
- **Unicode safety:** If operator names have special characters

### Snapshot Update Edge Cases
- **Partial updates:** What if only some bars changed? (Should update entire snapshot)
- **Snapshot file missing:** First run should create it (or fail with clear message?)
- **Snapshot file corrupted:** Invalid JSON should fail with clear error
- **Multiple snapshots:** If we have multiple test cases, each needs its own snapshot

---

## 7. Clarifying Questions

### 🔴 CRITICAL: Snapshot Update Mechanism (AC4)

**Question:** What is the "controlled way to update snapshot when behavior changes by design"?

**Options to clarify:**
1. **Environment variable:** `UPDATE_SNAPSHOTS=true dotnet test`
2. **Test attribute:** `[UpdateSnapshot]` attribute on test method
3. **Manual process:** Developer manually copies generated output to snapshot file
4. **Command-line flag:** `dotnet test --update-snapshots`
5. **Approval workflow:** Generate new snapshot, require explicit approval before committing

**Answer:**
Use **environment variable** approach (`UPDATE_SNAPSHOTS=true`). This is the most common pattern for golden tests because:
- Simple to use: `UPDATE_SNAPSHOTS=true dotnet test --filter "FullyQualifiedName~DrummerGoldenTests"`
- No code changes required (no attributes to add/remove)
- CI-friendly: environment variable is not set in CI, preventing accidental updates
- Clear intent: explicit opt-in prevents accidental snapshot overwrites
- Standard practice: matches patterns used by Jest, Cargo, and other test frameworks

Implementation:
```csharp
bool updateMode = Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS") == "true";
if (updateMode)
{
    File.WriteAllText(snapshotPath, actualJson);
    return; // Skip assertion in update mode
}
// Otherwise, assert actual == expected
```

---

### Snapshot Format Details

**Question:** What is the exact JSON schema for the snapshot file?

**Needs clarification:**
- Should it be flat (array of events) or nested (per-bar structure)?
- Should it include metadata (seed, timestamp, version)?
- Should it be human-readable (indented, verbose) or compact?
- Should it include provenance (which operator generated each onset)?

**Answer:**
Use **per-bar structure (Option B)** with the following schema:

```json
{
  "schemaVersion": 1,
  "seed": 42,
  "styleId": "PopRock",
  "totalBars": 52,
  "bars": [
    {
      "barNumber": 1,
      "sectionType": "Intro",
      "events": [
        {
          "beat": 1.0,
          "role": "Kick",
          "velocity": 100,
          "timingOffset": 0,
          "provenance": "Anchor"
        },
        {
          "beat": 2.0,
          "role": "Snare",
          "velocity": 110,
          "timingOffset": -2,
          "provenance": "Anchor"
        }
      ],
      "operatorsUsed": ["Anchor", "GhostBeforeBackbeat"]
    }
  ]
}
```

**Rationale:**
- **Per-bar structure:** Easier to debug (can quickly find bar 23), matches acceptance criteria "per-bar data"
- **Include metadata:** Seed and style are essential for reproducibility
- **Include schema version:** Enables future migration if schema evolves
- **Human-readable JSON:** Use indented formatting (`JsonSerializerOptions.WriteIndented = true`)
- **Provenance field:** Track which operator (or "Anchor") generated each onset for transparency (AC2 requirement)
- **operatorsUsed list:** Unique operators used in the bar (for quick scanning)

---

### Diff Reporting

**Question:** When the test fails (snapshot mismatch), how should differences be reported?

**Options:**
1. **First difference only:** Report first mismatch, stop (fast but incomplete)
2. **All differences:** Report every mismatch (complete but verbose)
3. **Summary + details:** Count of differences + first N examples
4. **Visual diff:** Side-by-side comparison of expected vs. actual
5. **Threshold:** Fail only if > N differences (too lenient?)

**Answer:**
Use **summary + details (Option 3)** with the following format:

```
Snapshot mismatch detected:
- Total differences: 15
- First 5 differences:
  1. Bar 3, Beat 1.75: Expected velocity=45, Actual velocity=50
  2. Bar 5, Beat 2.0: Expected role=Snare, Actual role=Kick
  3. Bar 7, Beat 3.5: Event missing in actual
  4. Bar 9, Beat 4.0: Expected timingOffset=-2, Actual timingOffset=0
  5. Bar 12, Beat 1.0: Extra event in actual (not in expected)
```

Implementation approach:
- Deserialize both expected and actual JSON
- Compare bar-by-bar, event-by-event
- Collect all differences into a list
- Report count + first 10 differences (configurable limit)
- Write full diff to a temp file if > 10 differences, provide path in message

**Rationale:**
- Summary gives quick assessment of magnitude (1 difference vs. 100 differences)
- First N examples provide enough context to diagnose root cause
- Full diff file prevents overwhelming console output
- Balances clarity (not just "failed") with brevity (not 1000 lines of diff)

---

### Snapshot Versioning

**Question:** Should the snapshot include a version field?

**Rationale:** If the snapshot schema evolves (e.g., we add new fields like articulation hints), old snapshots might become incompatible.

**Options:**
1. **No versioning:** Assume schema is stable (risky)
2. **Version field:** Include `"schemaVersion": 1` (enables migration)
3. **Separate snapshots per version:** Use different files for different schema versions

**Answer:**
**Yes, include a version field** (`"schemaVersion": 1`) at the root level.

**Rationale:**
- Enables future-proofing: if we add articulation hints or other fields, we can increment to version 2
- Allows graceful migration: test can detect version mismatch and provide clear error message
- Minimal overhead: single integer field
- Standard practice: many snapshot testing frameworks include version metadata
- Prevents silent failures: if schema changes, version mismatch fails loudly rather than silently comparing incompatible data

Implementation:
```csharp
if (snapshot.SchemaVersion != ExpectedSchemaVersion)
{
    throw new InvalidOperationException(
        $"Snapshot schema version mismatch: expected {ExpectedSchemaVersion}, got {snapshot.SchemaVersion}. " +
        "Regenerate snapshot with UPDATE_SNAPSHOTS=true.");
}
```

---

### Test Data Setup

**Question:** Should the test fixture be reusable for other tests, or specific to this golden test?

**Options:**
1. **Shared fixture:** Create `TestHelpers.CreateStandardSongContext()` (reusable)
2. **Test-specific:** Inline all fixture setup in golden test (self-contained)
3. **Hybrid:** Shared helpers + test-specific customization

**Answer:**
Use **hybrid approach (Option 3)** with shared helpers for common patterns and test-specific customization for unique requirements.

**Implementation:**
- Create `GoldenTestHelpers.CreateStandardFixture()` for the 8-section song structure (reusable)
- Inline customization in each test (e.g., different seeds, section counts)
- Keep fixture creation close to the test (same file or nested class)

**Rationale:**
- **Reusable baseline:** The 8-section Intro-Verse-Chorus structure is the canonical test case
- **Test isolation:** Each test can customize seed, sections, or style without affecting others
- **Discoverability:** Fixture creation is in the test file, not scattered across helper classes
- **Balance:** Avoids both code duplication (shared helpers) and hidden dependencies (fully shared fixture)

Example:
```csharp
[Fact]
public void GoldenTest_StandardPopRock_ProducesSnapshot()
{
    // Shared helper provides baseline fixture
    var songContext = GoldenTestHelpers.CreateStandardFixture(seed: 42);
    
    // Test-specific: use PopRock style
    var track = Generator.Generate(songContext, StyleConfigurationLibrary.PopRock);
    
    // Assertion...
}
```

---

### Operator Usage Transparency

**Question:** How detailed should the "operators used per bar" information be?

**Options:**
1. **Operator IDs only:** `["GhostBeforeBackbeat", "HatLift"]`
2. **Operator + count:** `{"GhostBeforeBackbeat": 2, "HatLift": 8}`
3. **Operator + onsets:** `[{"operator": "GhostBeforeBackbeat", "onsets": [{"beat": 1.75, "role": "Snare"}]}]`

**Answer:**
Use **Option 1 (Operator IDs only)** for the `operatorsUsed` list, combined with full provenance in the event-level `provenance` field.

**Implementation:**
```json
{
  "barNumber": 5,
  "operatorsUsed": ["Anchor", "GhostBeforeBackbeat", "HatLift"],
  "events": [
    {"beat": 1.0, "role": "Kick", "velocity": 100, "provenance": "Anchor"},
    {"beat": 1.75, "role": "Snare", "velocity": 45, "provenance": "GhostBeforeBackbeat"},
    {"beat": 2.0, "role": "Snare", "velocity": 110, "provenance": "Anchor"},
    {"beat": 2.5, "role": "ClosedHat", "velocity": 80, "provenance": "HatLift"}
  ]
}
```

**Rationale:**
- **operatorsUsed list:** Quick overview of which operators were active (for scanning)
- **Event-level provenance:** Full traceability (satisfies AC2 "operators used per bar" + "transparency")
- **No counts needed:** Can compute counts from event-level provenance if needed
- **Compact:** Avoids duplication (operator name appears once in list + once per event)
- **Debuggable:** Can quickly see "Bar 5 used HatLift" and find the specific events it generated

**Trade-off:**
- Slightly larger file size (provenance field on every event)
- Much better debugging (can trace every onset to its source)

**Example schema options:**

**Option A: Flat event list**
```json
{
  "seed": 42,
  "totalBars": 32,
  "events": [
    {"bar": 1, "beat": 1.0, "role": "Kick", "velocity": 100, "timingOffset": 0, "operator": "Anchor"},
    {"bar": 1, "beat": 2.0, "role": "Snare", "velocity": 110, "timingOffset": -2, "operator": "Anchor"},
    ...
  ]
}
```

**Option B: Per-bar structure**
```json
{
  "seed": 42,
  "totalBars": 32,
  "bars": [
    {
      "barNumber": 1,
      "sectionType": "Intro",
      "events": [...],
      "operatorsUsed": ["Anchor", "GhostBeforeBackbeat", "HatLift"]
    },
    ...
  ]
}
```

---

### Diff Reporting

**Question:** When the test fails (snapshot mismatch), how should differences be reported?

**Options:**
1. **First difference only:** Report first mismatch, stop (fast but incomplete)
2. **All differences:** Report every mismatch (complete but verbose)
3. **Summary + details:** Count of differences + first N examples
4. **Visual diff:** Side-by-side comparison of expected vs. actual
5. **Threshold:** Fail only if > N differences (too lenient?)

**Recommendation needed:** Which approach balances clarity with information density?

---

### Snapshot Versioning

**Question:** Should the snapshot include a version field?

**Rationale:** If the snapshot schema evolves (e.g., we add new fields like articulation hints), old snapshots might become incompatible.

**Options:**
1. **No versioning:** Assume schema is stable (risky)
2. **Version field:** Include `"schemaVersion": 1` (enables migration)
3. **Separate snapshots per version:** Use different files for different schema versions

---

### Test Data Setup

**Question:** Should the test fixture be reusable for other tests, or specific to this golden test?

**Options:**
1. **Shared fixture:** Create `TestHelpers.CreateStandardSongContext()` (reusable)
2. **Test-specific:** Inline all fixture setup in golden test (self-contained)
3. **Hybrid:** Shared helpers + test-specific customization

**Trade-offs:**
- Shared = less duplication, but changes affect multiple tests
- Test-specific = isolated, but duplicates setup code

---

### Operator Usage Transparency

**Question:** How detailed should the "operators used per bar" information be?

**Options:**
1. **Operator IDs only:** `["GhostBeforeBackbeat", "HatLift"]`
2. **Operator + count:** `{"GhostBeforeBackbeat": 2, "HatLift": 8}`
3. **Operator + onsets:** `[{"operator": "GhostBeforeBackbeat", "onsets": [{"beat": 1.75, "role": "Snare"}]}]`

**Trade-off:**
- Slightly larger file size (provenance field on every event)
- Much better debugging (can trace every onset to its source)

---

## 8. Test Scenario Ideas

### Unit Test Names (Based on AC)

**Basic Functionality:**
- `GoldenTest_KnownSeed_ProducesIdenticalSnapshot()` — Main golden test (AC1-4)
- `GoldenTest_SameSeed_RepeatedRuns_ProducesIdenticalOutput()` — Determinism verification
- `GoldenTest_SnapshotFileExists_LoadsAndCompares()` — Snapshot loading

**Snapshot Verification:**
- `GoldenTest_SnapshotMismatch_ReportsFirstDifference()` — Diff reporting
- `GoldenTest_SnapshotMismatch_ListsAllDifferences()` — Full diff
- `GoldenTest_SnapshotMissing_FailsWithClearMessage()` — Missing snapshot error

**Snapshot Update:**
- `GoldenTest_UpdateMode_OverwritesExistingSnapshot()` — Update mechanism (AC4)
- `GoldenTest_UpdateMode_CreatesNewSnapshot_WhenMissing()` — Initial snapshot creation

**Section Structure:**
- `GoldenTest_IntroVerseChorus_CorrectSectionTransitions()` — Section boundary handling
- `GoldenTest_BridgeSection_AppliesBreakdownOperator()` — Section-specific behavior
- `GoldenTest_OutroSection_EndsCleanly()` — Song ending

**Operator Usage:**
- `GoldenTest_OperatorUsage_RecordedPerBar()` — AC2 transparency requirement
- `GoldenTest_FillOperators_UsedAtPhraseBoundaries()` — Fill placement
- `GoldenTest_StyleIdiomOperators_UsedInAppropriateSections()` — Style-gated operators

**Determinism:**
- `GoldenTest_DifferentSeed_ProducesDifferentSnapshot()` — Seed variation (negative test)
- `GoldenTest_SameContextDifferentRun_IdenticalOperatorSelection()` — Operator determinism
- `GoldenTest_MemoryState_IdenticalAtEachBar()` — Memory determinism

### Test Data Setups

**Standard fixture (8 sections):**
```csharp
public static SongContext CreateStandardGoldenFixture()
{
    var sections = new[]
    {
        new Section(startBar: 1,  barCount: 4, type: Intro,   name: "Intro"),
        new Section(startBar: 5,  barCount: 8, type: Verse,   name: "Verse 1"),
        new Section(startBar: 13, barCount: 8, type: Chorus,  name: "Chorus 1"),
        new Section(startBar: 21, barCount: 8, type: Verse,   name: "Verse 2"),
        new Section(startBar: 29, barCount: 8, type: Chorus,  name: "Chorus 2"),
        new Section(startBar: 37, barCount: 4, type: Bridge,  name: "Bridge"),
        new Section(startBar: 41, barCount: 8, type: Chorus,  name: "Chorus 3"),
        new Section(startBar: 49, barCount: 4, type: Outro,   name: "Outro")
    };
    // Total: 52 bars
    
    Rng.Initialize(seed: 42); // Known seed
    
    return new SongContext
    {
        BarTrack = BuildBarTrack(totalBars: 52, beatsPerBar: 4),
        SectionTrack = new SectionTrack(sections),
        GroovePresetDefinition = PopRockBasicGroove,
        SegmentGrooveProfiles = EmptyProfiles,
        Voices = DefaultVoiceSet,
        Song = BuildSong(tempo: 120, timeSignature: 4/4)
    };
}
```

**Minimal fixture (for faster tests):**
```csharp
public static SongContext CreateMinimalGoldenFixture()
{
    var sections = new[]
    {
        new Section(startBar: 1, barCount: 4, type: Verse,  name: "Verse"),
        new Section(startBar: 5, barCount: 4, type: Chorus, name: "Chorus")
    };
    // Total: 8 bars
    
    Rng.Initialize(seed: 42);
    
    return CreateSongContextWithSections(sections);
}
```

### Determinism Verification Points

**RNG determinism:**
1. Same seed → same RNG sequence
2. Same operator selection order → same candidates generated
3. Same tie-breaking → same final selection

**Memory determinism:**
1. Same operator usage history → same repetition penalties
2. Same fill history → same fill placement
3. Same section signature → same section-specific choices

**Snapshot determinism:**
1. Same PartTrack → same JSON serialization
2. Same JSON → same deserialization
3. Same comparison → same test result

---

## 9. Snapshot Comparison Strategy

### Exact Matching (Recommended for Determinism)
- **Approach:** Serialize both expected and actual to JSON, compare strings
- **Pros:** Simple, catches all differences, enforces perfect determinism
- **Cons:** No tolerance for floating-point precision, brittle to formatting changes

### Field-by-Field Comparison
- **Approach:** Compare each field individually with appropriate tolerances
- **Pros:** More robust, can handle minor variations
- **Cons:** Defeats determinism guarantee, requires careful tolerance tuning

### Hybrid Approach
- **Approach:** Exact match for structural fields (bar, role, operator), tolerance for numerics (velocity, timing)
- **Pros:** Balances determinism with robustness
- **Cons:** More complex, requires defining tolerance thresholds

**Recommendation:** Start with exact matching (enforce perfect determinism). If platform differences cause issues, add minimal tolerances only where necessary.

---

## 10. Snapshot File Management

### File Location
- **Proposed:** `Music.Tests/Generator/Agents/Drums/Snapshots/PopRock_Standard.json`
- **Alternative:** Store in test output directory (excluded from source control)

**Trade-offs:**
- **Source control:** Snapshots should be checked in (provides history of behavior changes)
- **Large files:** If snapshot is very large, consider compression or summary
- **Multiple snapshots:** May need separate snapshots for different scenarios

### Snapshot Organization
**Option A: Single snapshot per test**
```
Snapshots/
  PopRock_Standard.json           # Main golden test
  PopRock_MinimalFill.json        # Test with minimal fills
  PopRock_HighEnergy.json         # Test with high energy
```

**Option B: Nested by category**
```
Snapshots/
  PopRock/
    Standard.json
    MinimalFill.json
    HighEnergy.json
  Jazz/
    Standard.json
```

---

## 11. Test Execution Workflow

### Normal Run (Verification)
1. Load expected snapshot from file
2. Initialize RNG with known seed
3. Generate PartTrack via Generator.Generate()
4. Serialize actual output to JSON (in-memory)
5. Compare actual vs. expected (field-by-field or string comparison)
6. If match: PASS
7. If mismatch: FAIL with diff report

### Update Mode (Intentional Change)
1. Initialize RNG with known seed
2. Generate PartTrack via Generator.Generate()
3. Serialize output to JSON
4. **Write JSON to snapshot file** (overwrite existing)
5. Test PASSES (update mode suppresses assertion)
6. Developer reviews diff in source control
7. Developer commits updated snapshot if change is intentional

---

## 12. Files Referenced

### Created by This Story:
- `Music.Tests/Generator/Agents/Drums/DrummerGoldenTests.cs` — Main test class
- `Music.Tests/Generator/Agents/Drums/Snapshots/PopRock_Standard.json` — Snapshot file

### Dependencies:
- `Music/Generator/Core/Generator.cs` — Generation entry point
- `Music/Generator/Agents/Common/StyleConfigurationLibrary.cs` — PopRock config
- `Music/Generator/Agents/Drums/DrummerAgent.cs` — Agent facade
- `Music/Generator/Groove/GroovePresetDefinition.cs` — Groove definition
- `Music/Song/Section/SectionTrack.cs` — Section structure
- `Music/Song/Bar/BarTrack.cs` — Timing ruler
- `Music/MyMidi/PartTrack.cs` — Output format
- `Music/Generator/Core/Randomization/Rng.cs` — RNG initialization

---

## Summary

**Story 10.8.3 is a critical regression testing story** that provides a safety net for all future drummer agent development. It requires:

1. **Perfect determinism:** Same seed → identical output (no tolerance)
2. **Complete coverage:** End-to-end test from SongContext to PartTrack
3. **Transparency:** Capture not just outputs but also decision-making (operator usage)
4. **Controlled updates:** Clear mechanism to update snapshot when behavior intentionally changes

**Key open questions:**
- Snapshot update mechanism (environment variable, attribute, manual?)
- Snapshot format (flat vs. nested, metadata inclusion)
- Diff reporting (first difference vs. all differences)
- Operator usage detail level (IDs only vs. full provenance)

**Critical dependencies:**
- Story 10.8.1 (Generator integration) must be complete
- All 28 operators must be registered and working
- Determinism must be guaranteed at RNG, memory, and selection levels

**Test execution considerations:**
- Snapshot should be checked into source control
- Update mode should require explicit opt-in (prevent accidental overwrites)
- Diff reporting should be clear enough to diagnose issues quickly
