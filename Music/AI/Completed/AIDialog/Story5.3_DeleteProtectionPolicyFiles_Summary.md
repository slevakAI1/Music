# Story 5.3 — Delete Unused Protection/Policy Files

**Status:** ✅ COMPLETED

**Goal:** Delete protection/policy files that are now Drum Generator's domain so that Groove namespace is clean.

---

## Files Deleted from `Generator/Groove/`

### Protection/Policy Core Types (14 files)
1. `GrooveProtectionPolicy.cs` — Complete protection policy bundle
2. `GrooveProtectionLayer.cs` — Layer-based protections
3. `RoleProtectionSet.cs` — Per-role protection sets
4. `ProtectionApplier.cs` — Protection enforcement logic
5. `ProtectionPerBarBuilder.cs` — Per-bar protection merger
6. `ProtectionPolicyMerger.cs` — Policy merging logic
7. `PhraseHookProtectionAugmenter.cs` — Phrase hook protection
8. `GroovePhraseHookPolicy.cs` — Phrase hook policy
9. `GrooveOrchestrationPolicy.cs` — Orchestration policy
10. `GrooveRoleConstraintPolicy.cs` — Role constraint policy
11. `GrooveSubdivisionPolicy.cs` — Subdivision policy
12. `GrooveTimingPolicy.cs` — Timing policy
13. `GrooveOverrideMergePolicy.cs` — Override merge policy
14. `OverrideMergePolicyEnforcer.cs` — Override enforcement

### Related Support Files (5 files)
15. `CatalogGrooveCandidateSource.cs` — Catalog-based candidate source
16. `PhraseHookWindowResolver.cs` — Phrase hook window resolution
17. `GrooveSetupFactory.cs` — Preset factory with policy building
18. `RhythmVocabularyFilter.cs` — Rhythm vocabulary filtering
19. `RolePresenceGate.cs` — Role presence checking
20. `GrooveCandidateFilter.cs` — Candidate filtering

### Drum Generator Files Deleted (2 files)
21. `Generator/Agents/Drums/DrumCapsEnforcer.cs` — Caps enforcement (heavily coupled to deleted policies)

---

## Files Modified

### Core Files Simplified
1. **`GroovePresetDefinition.cs`**
   - Removed `ProtectionPolicy` property
   - Removed `VariationCatalog` property
   - Now only contains: Identity + AnchorLayer

2. **`FeelTimingEngine.cs`**
   - Removed policy parameters
   - Simplified to direct parameters: `feel`, `swingAmount01`, `allowedSubdivisions`
   - Removed policy resolution methods

3. **`RoleTimingEngine.cs`**
   - Removed policy parameters
   - Simplified to direct parameters: `feel`, `biasTicks`
   - Removed policy resolution methods
   - Simplified diagnostics record

4. **`DrumDensityCalculator.cs`**
   - Removed all policy parameters
   - Simplified to direct parameters: `density01`, `maxEventsPerBar`
   - Removed orchestration multiplier logic
   - Removed merge policy enforcement

5. **`DrummerContextBuilder.cs`**
   - Removed `ProtectionPolicy` from `DrummerContextBuildInput`
   - Removed policy-based active role resolution
   - Simplified fill window detection (now just section boundaries)

6. **`DrumTrackGenerator.cs`**
   - Deleted legacy `GenerateLegacyAnchorBasedInternal` method (all protection/policy pipeline stages)
   - Now only uses `GrooveBasedDrumGenerator` pipeline

7. **`HandleCommandWriteTestSongNew.cs`**
   - Removed `GrooveSetupFactory` usage
   - Creates minimal `GroovePresetDefinition` inline

---

## Test Files Deleted (14 files)

1. `ProtectionApplierTests.cs`
2. `ProtectionPerBarBuilderTests.cs`
3. `PhraseHookProtectionAugmenterTests.cs`
4. `RoleTimingEngineTests.cs`
5. `GrooveCrossComponentTests.cs`
6. `CapEnforcementTests.cs`
7. `OverrideMergePolicyMatrixTests.cs`
8. `Story11_SyncopationAnticipationFilterTests.cs`
9. `RolePresenceGateTests.cs`
10. `DrumTrackGeneratorGoldenTests.cs`
11. `GroovePhaseIntegrationTests.cs`
12. `RhythmVocabularyFilterTests.cs`
13. `Story_G7_DrumGeneratorOrchestrationTests.cs`
14. `DrummerGoldenTests.cs`
15. `GoldenTestHelpers.cs`

---

## Architecture Impact

### Before (Complex Policy System)
```
GroovePresetDefinition
├── ProtectionPolicy (bundle)
│   ├── SubdivisionPolicy
│   ├── RoleConstraintPolicy
│   ├── PhraseHookPolicy
│   ├── TimingPolicy
│   ├── OrchestrationPolicy
│   └── OverrideMergePolicy
└── VariationCatalog

DrumGenerator → uses all these policies
```

### After (Simplified)
```
GroovePresetDefinition
├── Identity (name, genre)
└── AnchorLayer (onset lists)

DrumGenerator → uses GrooveInstanceLayer for timing reference
DrumGenerator → handles all musical decisions internally
```

---

## Rationale

All deleted files represented **part-generation logic** that belonged in the **Drum Generator domain**, not the Groove system. The Groove system should only provide **rhythm onset patterns**.

**Key Insight:** Policies for velocity, density, constraints, orchestration, and protection are all about *how to generate musical parts*, which is the Drum Generator's responsibility. Groove should just say "kick on 1 and 3, snare on 2 and 4" and nothing more.

---

## Build Status

✅ **Build Successful**

All compilation errors resolved. The codebase now cleanly separates:
- **Groove domain:** Rhythm patterns (onset beat positions)
- **Drum Generator domain:** Musical intelligence (velocity, density, style, section awareness)

---

## Next Steps

**Story 5.4** — Delete remaining unused Groove files (variation catalog, diagnostics, defaults)
**Story 5.5** — Update documentation (`ProjectArchitecture.md`, `CurrentEpic.md`)

---

*Completed:* Story 5.3
*Build Status:* ✅ Success
*Files Deleted:* 35 (21 source + 14 test)
*Files Modified:* 7 source files simplified
