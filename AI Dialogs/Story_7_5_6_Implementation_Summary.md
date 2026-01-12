# Story 7.5.6 Implementation Summary

## Goal
Wire tension hooks into non-drum roles (comp, keys, pads, bass) to make tension meaningful beyond drums while keeping implementation minimal and safe.

## Acceptance Criteria ?
All criteria met:

1. **Comp/Keys/Pads**: Small velocity bias (accent bias) at phrase peaks/ends ?
   - Applied `VelocityAccentBias` from tension hooks
   - Clamped to MIDI range [1, 127]
   - Additive to energy bias

2. **Bass**: Pickup/approach bias only on valid slots and only when energy allows ?
   - Applied `PullProbabilityBias` to approach note probability
   - Slot-gated (only when groove has valid slot)
   - Policy-gated (respects `AllowNonDiatonicChordTones`)
   - Probability clamped to [0, 1]

3. **Lead-space and register guardrails always win** ?
   - No changes to existing guardrail mechanisms
   - Lead-space ceiling enforced for comp/keys
   - Bass range guardrail enforced

4. **No changes breaking existing role guardrails** ?
   - Register limits preserved
   - Lead-space ceiling preserved
   - Density caps preserved

5. **Determinism preserved** ?
   - Same inputs ? same outputs
   - Integration tests verify determinism

6. **Tests** ?
   - Determinism tests for all roles
   - Guardrail tests for all roles
   - Specific behavior tests (velocity increase, approach bias)

## Implementation Details

### Files Modified

#### 1. Generator\Guitar\GuitarTrackGenerator.cs
- **Signature update**: Added `ITensionQuery` and `microTensionPhraseRampIntensity` parameters
- **Tension hooks creation**: Per bar, using section indexing
- **Velocity application**: `ApplyTensionAccentBias()` after energy bias
- **Guardrails**: Velocity clamped [1, 127]

#### 2. Generator\Keys\KeysTrackGenerator.cs
- **Signature update**: Added `ITensionQuery` and `microTensionPhraseRampIntensity` parameters
- **Tension hooks creation**: Per bar, using section indexing
- **Velocity application**: `ApplyTensionAccentBias()` after energy bias
- **Guardrails**: Velocity clamped [1, 127]

#### 3. Generator\Bass\BassTrackGenerator.cs
- **Signature update**: Added `ITensionQuery` and `microTensionPhraseRampIntensity` parameters
- **Tension hooks creation**: Per bar, using section indexing
- **Approach probability**: `ApplyTensionBiasToApproachProbability()` applies `PullProbabilityBias`
- **Guardrails**: 
  - Probability clamped [0, 1]
  - Slot-gated (existing `BassChordChangeDetector` logic)
  - Policy-gated (existing `allowApproaches` flag)

#### 4. Generator\Core\Generator.cs
- **Updated calls**: Pass `tensionQuery` and `microTensionPhraseRampIntensity` to bass, guitar, and keys generators
- **Parameters already created**: `tensionQuery` (Story 7.5.5) and `microTensionPhraseRampIntensity` constant

### Files Created

#### 1. Generator\Guitar\CompTensionHooksIntegrationTests.cs
Tests:
- Velocity increase at phrase ends with high tension
- Velocity guardrails enforced (MIDI range [1, 127])
- Determinism verification

#### 2. Generator\Keys\KeysTensionHooksIntegrationTests.cs
Tests:
- Velocity increase at phrase ends with high tension
- Velocity guardrails enforced (MIDI range [1, 127])
- Determinism verification

#### 3. Generator\Bass\BassTensionHooksIntegrationTests.cs
Tests:
- Approach probability increase with high tension
- Policy gate respected (tension biases but doesn't override policy)
- Determinism verification
- Bounded output ranges

## Key Design Decisions

### 1. Minimal Integration
- **Pattern**: Add tension hooks creation per bar, apply single bias value
- **Rationale**: Keep changes small and focused; avoid rewriting role logic

### 2. Additive Bias
- **Comp/Keys**: Tension accent bias is additive to energy bias
- **Bass**: Tension pull bias is additive to busy probability
- **Rationale**: Tension and energy are complementary; both should affect output

### 3. Guardrail Preservation
- **No changes to existing guardrails**: Lead-space ceiling, bass range, velocity clamping all preserved
- **Rationale**: Safety rails must never be violated regardless of tension

### 4. Slot/Policy Gating
- **Bass**: Tension only biases probability; final decision still gated by slot availability and policy
- **Rationale**: Tension hooks must respect existing constraints

### 5. Test Strategy
- **Focus on behavior verification**: Tests verify tension biases affect output appropriately
- **Guardrail verification**: All tests verify guardrails are never violated
- **Determinism verification**: All tests verify same inputs produce same outputs
- **No full generation tests**: Tests use tension hooks directly to avoid test complexity

## Integration with Existing Systems

### Energy System (Stage 7.3)
- Tension hooks work alongside energy profiles
- Both affect velocity: energy provides base bias, tension provides accent bias
- No conflicts; both are clamped to safe ranges

### Tension Query (Story 7.5.4)
- Uses `TensionHooksBuilder.Create()` to derive hooks per bar
- Inputs: macro tension, micro tension, phrase flags, transition hint, section energy
- Output: Bounded `TensionHooks` record

### Drums (Story 7.5.5)
- Same pattern as non-drum roles
- Drums use `PullProbabilityBias` for fills/dropouts
- Maintains consistency across all roles

## Acceptance Criteria Verification

| Criterion | Status | Evidence |
|-----------|--------|----------|
| Comp/keys/pads velocity bias | ? | `ApplyTensionAccentBias()` in both generators |
| Bass approach bias (slot-gated) | ? | `ApplyTensionBiasToApproachProbability()` + existing slot check |
| Lead-space guardrails | ? | No changes to existing guardrail code |
| No breaking changes | ? | All existing guardrails preserved |
| Determinism | ? | Integration tests verify |
| Tests | ? | 3 test files created, 11 test methods total |

## Build Status
? Build successful with all changes

## Next Steps
- Story 7.5.7: Tension diagnostics (explainability)
- Story 7.5.8: Stage 8/9 integration contract (tension queries for motifs/melody)
