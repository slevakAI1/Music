# Story 8.0.7 Testing Guide

## Running the Tests

The test file `SeedSensitivityTests.cs` follows the internal static test class pattern used in the codebase.

### Test Location
```
Generator\Tests\SeedSensitivityTests.cs
```

### Running Tests

#### Option 1: Run All Tests
```csharp
using Music.Generator.Tests;

// Run all Story 8.0.7 tests
SeedSensitivityTests.RunAllTests();
```

#### Option 2: Run Individual Tests
```csharp
// Determinism tests
SeedSensitivityTests.Test_Comp_SameSeed_ProducesIdenticalBehaviors();
SeedSensitivityTests.Test_Keys_SameSeed_ProducesIdenticalModes();
SeedSensitivityTests.Test_CrossRole_SameSeed_ProducesIdenticalOutput();

// Seed sensitivity tests
SeedSensitivityTests.Test_Comp_DifferentSeeds_ProduceDifferentBehaviors();
SeedSensitivityTests.Test_Keys_DifferentSeeds_ProduceDifferentModes();
SeedSensitivityTests.Test_CrossRole_DifferentSeeds_ProducesDifferentOutput();

// Section differentiation tests
SeedSensitivityTests.Test_VerseVsChorus_CompProducesDifferentBehaviors();
SeedSensitivityTests.Test_VerseVsChorus_KeysProducesDifferentModes();
SeedSensitivityTests.Test_VerseVsChorus_AudiblyDifferentDensityAndDuration();

// Probabilistic feature tests
SeedSensitivityTests.Test_BridgeFirstBar_KeysSplitVoicingVariesBySeed();
SeedSensitivityTests.Test_CompBehavior_SeedAffectsEveryFourthBarVariation();
SeedSensitivityTests.Test_KeysMode_BridgeSeedAffectsSplitVoicingChance();
```

#### Option 3: Add to Existing Test Runner
If you have a test runner that discovers and runs internal static test classes:
```csharp
// In your test runner
CompBehaviorTests.RunAllTests();                    // Story 8.0.1
CompBehaviorRealizerTests.RunAllTests();            // Story 8.0.2
CompBehaviorIntegrationTests.RunAllTests();         // Story 8.0.3
KeysRoleModeTests.RunAllTests();                    // Story 8.0.4
KeysModeRealizerTests.RunAllTests();                // Story 8.0.5
SeedSensitivityTests.RunAllTests();                 // Story 8.0.7 ? NEW
```

---

## Expected Test Output

### Success (All Tests Pass)
```
Running Story 8.0.7 Seed Sensitivity tests...
  ? Comp different seeds: bar 8: seed=100?Standard, seed=200?Anticipate
  ? Comp same seed: Produces identical behaviors across 16 bars
  ? Keys different seeds: Bridge bar 0: seed=3?SplitVoicing, seed=103?Rhythmic
  ? Keys same seed: Produces identical modes across 8 bars
  ? Cross-role different seeds: Comp differs=True, Keys differs=True
  ? Cross-role same seed: Both roles produce identical output
  ? Verse vs Chorus comp: Verse=SparseAnchors, Chorus=SyncopatedChop
  ? Verse vs Chorus keys: Verse=Sustain, Chorus=Rhythmic
  ? Verse vs Chorus audibly different: Comp SparseAnchors?SyncopatedChop, Keys Sustain?Rhythmic
  ? Bridge SplitVoicing varies by seed: 18/50 seeds produced SplitVoicing
  ? Comp every-4th-bar variation: 28.7% rate across 300 checks
  ? Keys Bridge SplitVoicing: 39% rate across 100 seeds
? All Story 8.0.7 Seed Sensitivity tests passed.
```

### Success with Warnings (Still Passing)
Some tests may produce warnings due to statistical variance. This is **normal** for probabilistic features:

```
Running Story 8.0.7 Seed Sensitivity tests...
  ? Comp different seeds: No variation found across 12 bars (low probability but possible)
  ? Comp same seed: Produces identical behaviors across 16 bars
  ? Keys different seeds: No mode variation found (Bridge SplitVoicing is probabilistic)
  ? Keys same seed: Produces identical modes across 8 bars
  ? Cross-role different seeds: Comp differs=False, Keys differs=False
  ? Cross-role same seed: Both roles produce identical output
  ? Verse vs Chorus comp: Verse=SparseAnchors, Chorus=DrivingFull
  ? Verse vs Chorus keys: Verse=Pulse, Chorus=Rhythmic
  ? Verse vs Chorus audibly different: Comp SparseAnchors?DrivingFull, Keys Pulse?Rhythmic
  ? Bridge SplitVoicing: 8/50 seeds (expected ~15-25 with 40% probability)
  ? Comp every-4th-bar variation: 47.3% rate (expected ~30%)
  ? Keys Bridge SplitVoicing: 41% rate across 100 seeds
? All Story 8.0.7 Seed Sensitivity tests passed.
```

**Note:** Warnings (?) indicate statistical edge cases but do not fail the test suite. Re-run the tests to verify consistency. If warnings persist, it may indicate an implementation issue.

### Failure Example
If a test fails, it will throw an exception:

```
Running Story 8.0.7 Seed Sensitivity tests...
  ? Comp different seeds: bar 4: seed=100?Standard, seed=200?Anticipate
  
Unhandled exception: System.Exception: Comp determinism violated at bar 5: Standard != Anticipate
   at Music.Generator.Tests.SeedSensitivityTests.Test_Comp_SameSeed_ProducesIdenticalBehaviors()
   at Music.Generator.Tests.SeedSensitivityTests.RunAllTests()
```

**Action:** Investigate why determinism is violated. Check if non-deterministic logic was introduced.

---

## Test Categories

### 1. Determinism Tests (Strict)
These tests **throw exceptions** on failure:
- `Test_Comp_SameSeed_ProducesIdenticalBehaviors`
- `Test_Keys_SameSeed_ProducesIdenticalModes`
- `Test_CrossRole_SameSeed_ProducesIdenticalOutput`

**Expected:** Always pass with same seed producing identical output.

### 2. Seed Sensitivity Tests (Informational)
These tests **report findings** but don't fail:
- `Test_Comp_DifferentSeeds_ProduceDifferentBehaviors`
- `Test_Keys_DifferentSeeds_ProduceDifferentModes`
- `Test_CrossRole_DifferentSeeds_ProducesDifferentOutput`

**Expected:** Should find seed-dependent variation, but may report warnings due to probability.

### 3. Section Differentiation Tests (Strict)
These tests **throw exceptions** on failure:
- `Test_VerseVsChorus_CompProducesDifferentBehaviors`
- `Test_VerseVsChorus_KeysProducesDifferentModes`
- `Test_VerseVsChorus_AudiblyDifferentDensityAndDuration`

**Expected:** Always pass with Verse ? Chorus and Chorus > Verse.

### 4. Probabilistic Feature Tests (Statistical)
These tests **report warnings** if rates fall outside expected ranges:
- `Test_BridgeFirstBar_KeysSplitVoicingVariesBySeed` (expects 20-60% split rate)
- `Test_CompBehavior_SeedAffectsEveryFourthBarVariation` (expects 15-45% variation rate)
- `Test_KeysMode_BridgeSeedAffectsSplitVoicingChance` (expects 25-55% split rate)

**Expected:** Rates within expected ranges, occasional warnings are acceptable.

---

## Interpreting Test Results

### ? Success Indicators
- All determinism tests pass (no exceptions)
- Section differentiation tests pass (Verse ? Chorus)
- Probabilistic rates within expected ranges
- Total test run completes with "All tests passed"

### ? Warning Indicators (Not Failures)
- "No variation found" messages (probabilistic features)
- Rates slightly outside expected ranges (statistical variance)
- Re-run tests to verify consistency

### ? Failure Indicators (Action Required)
- Exception thrown during test execution
- Determinism violation (same seed ? different output)
- Section differentiation failure (Verse == Chorus)
- Consistent rate violations (probabilistic features broken)

---

## Debugging Failed Tests

### Determinism Violation
```
Exception: Comp determinism violated at bar 5: Standard != Anticipate
```

**Likely causes:**
1. Non-deterministic logic added to selector
2. Seed not being passed correctly
3. Hash collision (very unlikely)

**Action:** Check `CompBehaviorSelector.SelectBehavior` implementation.

### Section Differentiation Failure
```
Exception: Expected different comp behaviors: Verse=SyncopatedChop, Chorus=SyncopatedChop
```

**Likely causes:**
1. Energy thresholds changed
2. Behavior mapping logic modified
3. Test inputs incorrect

**Action:** Check energy values and behavior mapping in selectors.

### Probabilistic Rate Violation
```
? Comp every-4th-bar variation: 2.3% rate (expected ~30%)
```

**Likely causes:**
1. Variation probability changed in implementation
2. Variation condition logic broken
3. Hash calculation issue

**Action:** Check variation probability calculation in `CompBehaviorSelector.ApplyVariation`.

---

## Manual Verification

After running tests, you can manually verify seed sensitivity:

### 1. Generate Two Songs with Different Seeds
```csharp
// In WriterForm or test harness
var settings1 = new RandomizationSettings { Seed = 100 };
var settings2 = new RandomizationSettings { Seed = 200 };

// Generate songs and compare MIDI output
// Expect: Different onset patterns, different durations
```

### 2. Generate Same Song Twice
```csharp
var settings = new RandomizationSettings { Seed = 42 };

// Generate twice with same settings
// Expect: Identical MIDI events (timing, pitch, velocity, duration)
```

### 3. Compare Verse vs Chorus
```csharp
// Generate song with Verse and Chorus sections
// Listen to output
// Expect: Chorus is noticeably denser and more energetic than Verse
```

---

## Test Maintenance

### When to Update Tests

#### Behavior Mapping Changes
If you change behavior/mode mapping thresholds:
```csharp
// Before: Verse energy < 0.35 ? Sustain
// After:  Verse energy < 0.4  ? Sustain

// Update test:
Test_VerseVsChorus_KeysProducesDifferentModes:
  energy: 0.25 ? 0.3  // Ensure still in Sustain range
```

#### Probabilistic Rate Changes
If you change variation/split probabilities:
```csharp
// Before: Bridge SplitVoicing 40% chance
// After:  Bridge SplitVoicing 50% chance

// Update test:
Test_BridgeFirstBar_KeysSplitVoicingVariesBySeed:
  Expected range: 10-30 out of 50 ? 15-35 out of 50

Test_KeysMode_BridgeSeedAffectsSplitVoicingChance:
  Expected range: 25-55% ? 35-65%
```

### Adding New Tests

To add new seed sensitivity tests:

1. **Follow naming convention:**
   ```csharp
   Test_RoleName_Feature_ExpectedBehavior()
   ```

2. **Use consistent patterns:**
   - Determinism tests: throw exceptions on failure
   - Seed sensitivity: report findings, warnings acceptable
   - Probabilistic: check rates against expected ranges

3. **Document expected behavior:**
   ```csharp
   /// <summary>
   /// Verifies [what] produces [expected result].
   /// </summary>
   ```

4. **Add to RunAllTests:**
   ```csharp
   public static void RunAllTests()
   {
       // ... existing tests ...
       Test_NewFeature_ExpectedBehavior();
       // ...
   }
   ```

---

## Integration with CI/CD

### Future Enhancement: Automated Test Execution

```yaml
# Example GitHub Actions workflow
name: Test Suite
on: [push, pull_request]
jobs:
  test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '9.0.x'
      - name: Build
        run: dotnet build
      - name: Run Story 8.0.7 Tests
        run: dotnet run --project TestRunner -- SeedSensitivityTests
```

---

## Summary

- **12 comprehensive tests** covering Comp + Keys seed sensitivity
- **Determinism strictly enforced** via exceptions
- **Probabilistic features** validated statistically
- **Section differentiation** verified (Verse ? Chorus)
- **Easy to run:** Single method call or individual test execution
- **Clear output:** Success messages, warnings, and actionable failures

**Test file ready for use!** ?
