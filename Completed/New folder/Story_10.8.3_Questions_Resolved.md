# Story 10.8.3 — Clarifying Questions: RESOLVED

**Date:** 2025-01-27  
**Story:** End-to-End Regression Snapshot (Golden Test)  
**Status:** All clarifying questions answered ✅

---

## Summary of Decisions

All clarifying questions in `PreAnalysis_10.8.3.md` have been resolved with concrete, actionable answers. These decisions provide authoritative guidance for implementation.

---

## Question 1: Snapshot Update Mechanism ✅

**Decision:** Environment variable (`UPDATE_SNAPSHOTS=true`)

**Usage:**
```bash
UPDATE_SNAPSHOTS=true dotnet test --filter "FullyQualifiedName~DrummerGoldenTests"
```

**Rationale:**
- Simple to use, no code changes required
- CI-friendly (prevents accidental updates)
- Standard practice (matches Jest, Cargo)
- Explicit opt-in prevents overwrites

---

## Question 2: Snapshot Format ✅

**Decision:** Per-bar structure with metadata and provenance

**Schema:**
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
        }
      ],
      "operatorsUsed": ["Anchor", "GhostBeforeBackbeat"]
    }
  ]
}
```

**Rationale:**
- Per-bar structure: easier to debug (find bar 23 quickly)
- Metadata: seed and style essential for reproducibility
- Schema version: future-proofs against schema evolution
- Human-readable: indented JSON for readability
- Provenance: full traceability (satisfies AC2 transparency requirement)

---

## Question 3: Diff Reporting ✅

**Decision:** Summary + first N differences (N=10)

**Format:**
```
Snapshot mismatch detected:
- Total differences: 15
- First 5 differences:
  1. Bar 3, Beat 1.75: Expected velocity=45, Actual velocity=50
  2. Bar 5, Beat 2.0: Expected role=Snare, Actual role=Kick
  3. Bar 7, Beat 3.5: Event missing in actual
  4. Bar 9, Beat 4.0: Expected timingOffset=-2, Actual timingOffset=0
  5. Bar 12, Beat 1.0: Extra event in actual (not in expected)

Full diff saved to: C:\Temp\snapshot_diff_20250127_143052.txt
```

**Rationale:**
- Summary: quick assessment of magnitude
- First N examples: enough context to diagnose root cause
- Full diff file: prevents overwhelming console output
- Balances clarity with brevity

---

## Question 4: Snapshot Versioning ✅

**Decision:** Include `schemaVersion` field

**Implementation:**
```csharp
if (snapshot.SchemaVersion != ExpectedSchemaVersion)
{
    throw new InvalidOperationException(
        $"Snapshot schema version mismatch: expected {ExpectedSchemaVersion}, got {snapshot.SchemaVersion}. " +
        "Regenerate snapshot with UPDATE_SNAPSHOTS=true.");
}
```

**Rationale:**
- Enables future-proofing (add articulation hints later)
- Allows graceful migration
- Minimal overhead (single integer field)
- Prevents silent failures (version mismatch fails loudly)

---

## Question 5: Test Data Setup ✅

**Decision:** Hybrid approach (shared helpers + test-specific customization)

**Implementation:**
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

**Rationale:**
- Reusable baseline: 8-section structure is canonical
- Test isolation: each test can customize without affecting others
- Discoverability: fixture creation in test file
- Avoids both code duplication and hidden dependencies

---

## Question 6: Operator Usage Transparency ✅

**Decision:** Operator IDs in list + provenance on each event

**Implementation:**
```json
{
  "barNumber": 5,
  "operatorsUsed": ["Anchor", "GhostBeforeBackbeat", "HatLift"],
  "events": [
    {"beat": 1.0, "role": "Kick", "velocity": 100, "provenance": "Anchor"},
    {"beat": 1.75, "role": "Snare", "velocity": 45, "provenance": "GhostBeforeBackbeat"},
    {"beat": 2.5, "role": "ClosedHat", "velocity": 80, "provenance": "HatLift"}
  ]
}
```

**Rationale:**
- operatorsUsed: quick overview for scanning
- Event-level provenance: full traceability (AC2 requirement)
- Compact: avoids duplication
- Debuggable: can trace every onset to its source

**Trade-off:** Slightly larger file, much better debugging

---

## Implementation Checklist

Based on these decisions, the implementation should:

- [ ] Use environment variable `UPDATE_SNAPSHOTS=true` for update mode
- [ ] Implement per-bar JSON schema with metadata and provenance
- [ ] Generate summary + first 10 differences on mismatch
- [ ] Write full diff to temp file when > 10 differences
- [ ] Include `schemaVersion: 1` in snapshot
- [ ] Validate schema version on load
- [ ] Create `GoldenTestHelpers.CreateStandardFixture()` helper
- [ ] Track provenance (operator ID) for each generated onset
- [ ] Serialize with `JsonSerializerOptions.WriteIndented = true`
- [ ] Store snapshot at `Music.Tests/Generator/Agents/Drums/Snapshots/PopRock_Standard.json`

---

## Key Architectural Decisions

1. **Perfect determinism enforced:** No tolerance for variation (same seed → identical output)
2. **Human-readable format:** Developers can manually inspect snapshots
3. **Full traceability:** Every onset tracks which operator generated it
4. **Future-proof:** Schema versioning enables evolution
5. **CI-safe:** Environment variable prevents accidental updates in CI
6. **Clear diagnostics:** Diff reporting provides actionable information

---

## Next Steps

1. **Implementation:** Use these answers as authoritative guidance for Story 10.8.3
2. **Review:** Confirm decisions align with team preferences (can override if needed)
3. **Documentation:** Update epic with any cross-cutting decisions (e.g., environment variable pattern)

---

## Files Modified

- ✅ `Music/AI/Plans/PreAnalysis_10.8.3.md` — All clarifying questions answered

---

## Notes for Implementation

- The environment variable approach is standard practice across many test frameworks
- The per-bar structure matches the acceptance criteria requirement for "per-bar data"
- Schema versioning is defensive programming (minimal cost, high value)
- Provenance tracking is essential for debugging operator behavior
- Hybrid test fixture approach balances reusability with test isolation
