# Golden Test Determinism Investigation Results

**Date:** 2026-01-27  
**Story:** 10.8.3 - End-to-End Regression Snapshot (Golden Test)

## Problem Summary

The golden test `GoldenTest_StandardPopRock_ProducesIdenticalSnapshot` fails because the generation process produces different outputs between test runs despite using identical RNG initialization (`Rng.Initialize(42)`).

## Root Cause

Through systematic debug tracing, we identified:

### Event Count Mismatch
- **UPDATE mode (snapshot creation):** 1296 events generated
- **VERIFY mode (snapshot comparison):** 1301 events generated
- **Difference:** 5 additional events in VERIFY mode

### RNG Consumption Mismatch
- **UPDATE mode:** 730 RNG calls to `GrooveCandidatePick`
- **VERIFY mode:** 731 RNG calls to `GrooveCandidatePick`  
- **Difference:** Exactly 1 extra RNG call in VERIFY mode

### RNG Sequence Analysis
Both runs start with IDENTICAL RNG sequences:
- First call: `0.944895`
- Second call: `0.431916`
- ... (continues identical for 730 calls)

UPDATE mode last call: `0.768223`
VERIFY mode last calls: `0.768223`, then `0.549468` (extra call)

## Conclusion

**The generation process has NON-DETERMINISTIC behavior** where:
1. One extra RNG call is made at the end of VERIFY run
2. This extra call causes 5 additional events to be generated
3. The extra RNG call shifts the entire sequence by one
4. All subsequent generations produce different velocities/events

## Likely Causes

1. **Conditional operator selection** based on:
   - Timing-dependent logic
   - Hash-based iterations (Dictionary/HashSet iteration order)
   - Floating-point precision differences
   - Memory address-dependent logic

2. **Test isolation issues**:
   - Static state persisting between test runs
   - Different test execution order affecting global state

## Recommended Fix

**Option 1: Fix the generation determinism (PREFERRED)**
- Audit all operator selection logic
- Replace Dictionary/HashSet iterations with deterministic ordered collections
- Ensure all conditional RNG consumption is deterministic

**Option 2: Accept limited determinism**
- Document that cross-process determinism is not guaranteed
- Only test within-process determinism (`GoldenTest_SameSeed_ProducesDeterministicOutput` - already passing)
- Use the golden test as a "change detector" rather than exact regression test

## Test Infrastructure Status

✅ **COMPLETE AND WORKING:**
- GoldenSnapshot data models with JSON serialization
- GoldenTestHelpers with fixture creation, serialization, comparison
- Diff reporting with summary + first N differences
- Environment variable-based snapshot update mechanism
- 11 of 12 golden tests passing

❌ **BLOCKED:**
- Main golden test fails due to systemic generation non-determinism
- Issue is in generation pipeline, not in test infrastructure

## Files Created

- `Music.Tests/Generator/Agents/Drums/Snapshots/GoldenSnapshot.cs`
- `Music.Tests/Generator/Agents/Drums/Snapshots/GoldenTestHelpers.cs`
- `Music.Tests/Generator/Agents/Drums/DrummerGoldenTests.cs`
- `Music.Tests/Generator/Agents/Drums/Snapshots/PopRock_Standard.json` (snapshot file)

## Next Steps

1. Remove debug tracing from Rng class (performance overhead)
2. Create follow-up story to audit generation determinism
3. Document current limitation in golden test comments
4. Consider using within-process determinism test as primary regression test
