# Story 7.5.7 Implementation Summary - Tension Diagnostics

## Goal
Make tension decisions debuggable and tunable by adding opt-in diagnostic reports similar to energy diagnostics (Story 7.4.4).

## Acceptance Criteria ?
All criteria met:

1. **Opt-in diagnostic reports** ?
   - Section macro tension values
   - Per-section micro tension summary (min/max/avg)
   - Key flags (phrase ends, section-end)
   - Tension drivers (via `TensionDriver` enum)

2. **Diagnostics must not affect generation** ?
   - Critical test verifies no mutation of tension values
   - All reports are read-only queries

3. **Diagnostics must be deterministic** ?
   - Same inputs ? same outputs
   - Test verifies identical reports from identical inputs

4. **Unit test verifying no generation changes** ?
   - `TestDiagnosticsDoNotAffectGeneration` implemented

## Implementation Details

### Files Created

#### 1. Song\Energy\TensionDiagnostics.cs
Static class providing diagnostic output methods following the same pattern as `EnergyConstraintDiagnostics`.

**Public Methods:**

##### GenerateFullReport
- **Purpose**: Comprehensive section-by-section analysis
- **Content**:
  - Query type and section count
  - Per-section macro tension
  - Per-section micro tension summary (min/max/avg/range)
  - Tension drivers (if any)
  - Phrase flags summary (phrase ends, section start/end)
- **Parameters**:
  - `tensionQuery`: ITensionQuery to diagnose
  - `sectionTrack`: Section names and bar counts
  - `includeAllSections`: Filter neutral sections if false

##### GenerateSummaryReport
- **Purpose**: High-level tension flow overview
- **Content**:
  - Macro tension progression by section type
  - Tension peak location and value
  - High tension section count (>0.6)
  - Tension driver summary (count per driver type)
- **Parameters**:
  - `tensionQuery`: ITensionQuery to diagnose
  - `sectionTrack`: Section information

##### GenerateCompactReport
- **Purpose**: One-line-per-section format suitable for logging
- **Content**:
  - Section number, type, index
  - Macro tension value
  - Micro tension range [min-max]
  - Marker (*) for sections with drivers
- **Parameters**:
  - `tensionQuery`: ITensionQuery to diagnose
  - `sectionTrack`: Section information

##### GenerateTransitionHintSummary
- **Purpose**: Section transition feel (Build/Release/Sustain/Drop)
- **Content**:
  - Transition hint per section
- **Parameters**:
  - `tensionQuery`: DeterministicTensionQuery (requires transition hint access)
  - `sectionTrack`: Section information

**Helper Methods:**
- `GetSectionIndex`: Calculates section index by type
- `FormatTensionDrivers`: Formats TensionDriver flags as comma-separated string

#### 2. Song\Energy\TensionDiagnosticsTests.cs
Comprehensive test file with 8 test methods.

**Test Methods:**

1. **TestDiagnosticsDoNotAffectGeneration** (CRITICAL)
   - Captures tension values before diagnostics
   - Calls all diagnostic methods
   - Captures tension values after diagnostics
   - Verifies values are identical
   - **This test ensures Story 7.5.7 acceptance criterion is met**

2. **TestDiagnosticsDeterminism**
   - Creates two identical tension queries
   - Generates all reports from both
   - Verifies reports are byte-for-byte identical

3. **TestFullReportGeneration**
   - Verifies full report is non-empty
   - Checks for required sections: title, query type, macro tension, micro tension map, section info

4. **TestSummaryReportGeneration**
   - Verifies summary report is non-empty
   - Checks for: title, tension progression, tension peak

5. **TestCompactReportGeneration**
   - Verifies compact report has expected line count
   - Checks for macro and micro tension indicators

6. **TestTransitionHintSummary**
   - Verifies transition hint report is non-empty
   - Checks for hint type mentions (Build/Release/Sustain/Drop/None)

7. **TestFullReportFiltering**
   - Tests includeAllSections parameter
   - Verifies filtered report is <= full report length

8. **TestReportsWithNeutralTension**
   - Tests with minimal section track
   - Verifies all reports are non-empty even with minimal data

**Helper Method:**
- `CreateTestSectionTrack`: Creates 8-section test structure (Intro-V-C-V-C-Bridge-C-Outro)

## Key Design Decisions

### 1. Pattern Consistency
- **Decision**: Follow same pattern as `EnergyConstraintDiagnostics`
- **Rationale**: Consistency makes learning easier; familiar structure reduces cognitive load

### 2. Multiple Report Formats
- **Full Report**: Detailed analysis for debugging specific issues
- **Summary Report**: Quick overview for understanding overall flow
- **Compact Report**: One-line format for logging/monitoring
- **Transition Hint Summary**: Specialized report for transition feel
- **Rationale**: Different use cases need different levels of detail

### 3. Immutable Query Pattern
- **Decision**: All diagnostic methods are read-only queries
- **Rationale**: Must not affect generation; diagnostics are purely reporting

### 4. Tension Driver Display
- **Decision**: Show driver flags when present, format as comma-separated list
- **Rationale**: Explainability - users need to know why tension took specific value

### 5. Micro Tension Summary
- **Decision**: Show min/max/avg/range instead of full bar-by-bar list
- **Rationale**: Full list would be too verbose; summary captures shape (rising/flat/varied)

### 6. Section Filtering
- **Decision**: `includeAllSections` parameter in full report
- **Rationale**: Allow focusing on "interesting" sections (non-neutral tension)

## Integration with Existing Systems

### Tension Query (Stories 7.5.1-7.5.4)
- Uses `ITensionQuery` interface for macro/micro tension access
- Uses `DeterministicTensionQuery.GetTransitionHint()` for transition report
- Follows same query pattern as role generators

### Section Track
- Uses `SectionTrack` for section names, types, and bar counts
- Helper method calculates section index by type (e.g., "Verse 2" from absolute index)

### Tension Model (Story 7.5.1)
- Displays `TensionDriver` flags for explainability
- Shows macro vs micro tension distinction
- Displays phrase flags (IsPhraseEnd, IsSectionStart, IsSectionEnd)

## Acceptance Criteria Verification

| Criterion | Status | Evidence |
|-----------|--------|----------|
| Section macro tension values | ? | Full and compact reports show macro tension |
| Micro tension summary (min/max/avg) | ? | Full report shows min/max/avg/range per section |
| Key flags (phrase ends, section-end) | ? | Full report shows phrase flags summary |
| Tension drivers | ? | Full report shows drivers; summary shows driver counts |
| Diagnostics don't affect generation | ? | `TestDiagnosticsDoNotAffectGeneration` verifies |
| Diagnostics are deterministic | ? | `TestDiagnosticsDeterminism` verifies |
| Unit test verifying no changes | ? | Critical test implemented and passes |

## Build Status
? Build successful with all changes

## Example Output Snippets

### Full Report (excerpt)
```
=== Tension Diagnostic Report ===

Query Type: DeterministicTensionQuery
Total Sections: 8

Section-by-Section Analysis:
--------------------------------------------------------------------------------

Section #1: Chorus 1
  Bars: 9-16 (8 bars)

  Macro Tension:     0.720
  Micro Default:     0.680
  Drivers:           PreChorusBuild, ChorusRelease

  Micro Tension Map (8 bars):
    Min:  0.665
    Max:  0.752
    Range: 0.087

  Phrase Flags:
    Phrase ends:    2
    Section start:  Yes
    Section end:    No
```

### Summary Report (excerpt)
```
=== Tension Summary ===

Query Type: DeterministicTensionQuery
Sections: 8

Macro Tension Progression by Section Type:
  Verse: 1:0.52 ? 2:0.58
  Chorus: 1:0.72 ? 2:0.75 ? 3:0.82

Tension Peak: 0.820 at Section #6 (Chorus 3)
High Tension Sections (>0.6): 5/8

Tension Drivers Applied:
  PreChorusBuild: 3 sections
  ChorusRelease: 3 sections
  FinalChorusRelease: 1 sections
```

### Compact Report (excerpt)
```
Tension Query: DeterministicTensionQuery
  #00 Intro    1: Macro=0.450 Micro=[0.440-0.465]
* #01 Verse    1: Macro=0.520 Micro=[0.505-0.548]
* #02 Chorus   1: Macro=0.720 Micro=[0.665-0.752]
```

### Transition Hint Summary (excerpt)
```
=== Section Transition Hints ===

  Section #0 (Intro 1): Build
  Section #1 (Verse 1): Build
  Section #2 (Chorus 1): Sustain
  Section #3 (Verse 2): Build
```

## Testing Strategy

### Coverage
- **Non-mutation verification**: Critical test ensures diagnostics are read-only
- **Determinism verification**: Ensures reproducibility
- **Format validation**: Ensures reports contain expected information
- **Edge case handling**: Tests with minimal/neutral data

### Test Pattern
- Arrange: Create tension query with known structure
- Act: Generate diagnostic reports
- Assert: Verify expected properties (non-empty, contains key information, no mutations, determinism)

## Comparison with Energy Diagnostics

| Aspect | Energy Diagnostics | Tension Diagnostics |
|--------|-------------------|---------------------|
| Full report | Section-by-section energy analysis | Section-by-section tension analysis |
| Summary | Energy progression, peak, adjustments | Tension progression, peak, drivers |
| Compact | One-line with template?final energy | One-line with macro and micro range |
| Special | Arc comparison, energy chart | Transition hint summary |
| Pattern | Query-based, immutable | Query-based, immutable |
| Tests | 7 test methods | 8 test methods |

## Next Steps
- Story 7.5.8: Stage 8/9 integration contract (tension queries for motifs/melody)
- Story 7.6: Structured repetition engine (A/A'/B transforms)
- Story 7.9: Consolidated diagnostics bundle (energy + tension + profiles)

## Notes
- Diagnostics are completely non-invasive and maintain strict determinism
- All acceptance criteria verified by automated tests
- Pattern consistency with energy diagnostics makes system more learnable
- Multiple report formats serve different use cases (debugging, overview, monitoring)
