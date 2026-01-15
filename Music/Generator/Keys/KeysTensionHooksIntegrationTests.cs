// AI: purpose=Story 7.5.6 tests: tension hooks bias keys/pads velocity at phrase peaks/ends; guardrails enforced.
// AI: invariants=Determinism; tension hooks affect velocity but never violate MIDI range [1,127] or lead-space ceiling.

namespace Music.Generator.Tests;

internal static class KeysTensionHooksIntegrationTests
{
    /// <summary>
    /// Verifies tension VelocityAccentBias increases keys velocity at phrase peaks/ends.
    /// Story 7.5.6: Keys uses tension hooks for accent bias.
    /// </summary>
    //public static void Test_TensionHooks_Increase_Velocity_At_Phrase_End()
    //{
    //    const int baseVelocity = 75;
        
    //    // Neutral tension (no accent bias)
    //    var neutralHooks = TensionHooksBuilder.Create(
    //        macroTension: 0.5,
    //        microTension: 0.5,
    //        isPhraseEnd: false,
    //        isSectionStart: false,
    //        transitionHint: SectionTransitionHint.None,
    //        sectionEnergy: 0.5,
    //        microTensionPhraseRampIntensity: 1.0);
        
    //    // High tension at phrase end (positive accent bias)
    //    var tensionHooks = TensionHooksBuilder.Create(
    //        macroTension: 0.95,
    //        microTension: 0.95,
    //        isPhraseEnd: true,
    //        isSectionStart: false,
    //        transitionHint: SectionTransitionHint.Build,
    //        sectionEnergy: 0.5,
    //        microTensionPhraseRampIntensity: 1.0);
        
    //    // Apply velocity calculation (mirrors KeysTrackGenerator logic)
    //    int neutralVelocity = baseVelocity + neutralHooks.VelocityAccentBias;
    //    int tensionVelocity = baseVelocity + tensionHooks.VelocityAccentBias;
        
    //    neutralVelocity = Math.Clamp(neutralVelocity, 1, 127);
    //    tensionVelocity = Math.Clamp(tensionVelocity, 1, 127);
        
    //    // Assert: tension should not reduce velocity
    //    if (tensionVelocity < neutralVelocity - 1) // Allow 1-point tolerance
    //    {
    //        throw new Exception($"Tension hooks should not reduce velocity: {tensionVelocity} < {neutralVelocity}");
    //    }
        
    //    Console.WriteLine($"? KeysTension: Neutral velocity={neutralVelocity}, Tension velocity={tensionVelocity}");
    //}
    
    /// <summary>
    /// Verifies keys velocity stays within MIDI range [1, 127] even with tension bias.
    /// Story 7.5.6: Guardrails must be enforced.
    /// </summary>
    //public static void Test_VelocityGuardrails_Enforced()
    //{
    //    const int baseVelocity = 75;
        
    //    // Extreme high tension
    //    var maxHooks = TensionHooksBuilder.Create(
    //        macroTension: 1.0,
    //        microTension: 1.0,
    //        isPhraseEnd: true,
    //        isSectionStart: true,
    //        transitionHint: SectionTransitionHint.Build,
    //        sectionEnergy: 0.5,
    //        microTensionPhraseRampIntensity: 1.0);
        
    //    // Extreme low tension
    //    var minHooks = TensionHooksBuilder.Create(
    //        macroTension: 0.0,
    //        microTension: 0.0,
    //        isPhraseEnd: false,
    //        isSectionStart: false,
    //        transitionHint: SectionTransitionHint.Drop,
    //        sectionEnergy: 0.5,
    //        microTensionPhraseRampIntensity: 1.0);
        
    //    int maxVelocity = Math.Clamp(baseVelocity + maxHooks.VelocityAccentBias, 1, 127);
    //    int minVelocity = Math.Clamp(baseVelocity + minHooks.VelocityAccentBias, 1, 127);
        
    //    // Assert: velocities must be in valid MIDI range
    //    if (maxVelocity < 1 || maxVelocity > 127)
    //    {
    //        throw new Exception($"Max velocity out of range: {maxVelocity}");
    //    }
        
    //    if (minVelocity < 1 || minVelocity > 127)
    //    {
    //        throw new Exception($"Min velocity out of range: {minVelocity}");
    //    }
        
    //    Console.WriteLine($"? KeysTension: Velocity range [{minVelocity}, {maxVelocity}] within MIDI bounds [1, 127]");
    //}
    
    /// <summary>
    /// Verifies determinism: same inputs produce same tension accent bias.
    /// Story 7.5.6: Integration must preserve determinism.
    /// </summary>
    //public static void Test_TensionIntegration_Determinism()
    //{
    //    const int baseVelocity = 75;
        
    //    // Create hooks twice with identical inputs
    //    var hooks1 = TensionHooksBuilder.Create(
    //        macroTension: 0.75,
    //        microTension: 0.80,
    //        isPhraseEnd: true,
    //        isSectionStart: false,
    //        transitionHint: SectionTransitionHint.Build,
    //        sectionEnergy: 0.5,
    //        microTensionPhraseRampIntensity: 1.0);
        
    //    var hooks2 = TensionHooksBuilder.Create(
    //        macroTension: 0.75,
    //        microTension: 0.80,
    //        isPhraseEnd: true,
    //        isSectionStart: false,
    //        transitionHint: SectionTransitionHint.Build,
    //        sectionEnergy: 0.5,
    //        microTensionPhraseRampIntensity: 1.0);
        
    //    int velocity1 = Math.Clamp(baseVelocity + hooks1.VelocityAccentBias, 1, 127);
    //    int velocity2 = Math.Clamp(baseVelocity + hooks2.VelocityAccentBias, 1, 127);
        
    //    // Assert: identical inputs produce identical velocities
    //    if (velocity1 != velocity2)
    //    {
    //        throw new Exception($"Velocity mismatch: {velocity1} vs {velocity2}");
    //    }
        
    //    Console.WriteLine($"? KeysTension: Determinism verified (velocity={velocity1})");
    //}
    
    //public static void RunAllTests()
    //{
    //    Console.WriteLine("=== Keys Tension Hooks Integration Tests ===");
    //    Test_TensionHooks_Increase_Velocity_At_Phrase_End();
    //    Test_VelocityGuardrails_Enforced();
    //    Test_TensionIntegration_Determinism();
    //    Console.WriteLine("=== All Keys Tension Tests Passed ===\n");
    //}
}
