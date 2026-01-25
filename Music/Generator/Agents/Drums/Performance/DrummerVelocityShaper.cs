// AI: purpose=Drummer-specific velocity hinting that normalizes operator hints and maps to style-aware targets.
// AI: invariants=Only updates VelocityHint, never final MIDI velocity; deterministic; clamps to [1..127]; runs BEFORE groove VelocityShaper.
// AI: deps=DrumCandidate, DrummerVelocityHintSettings, DynamicIntent, FillRole, OnsetStrength; consumed by DrummerCandidateSource.
// AI: change=Story 6.1; extend intent classification as operator patterns evolve.

using Music.Generator.Agents.Common;
using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Performance
{
    /// <summary>
    /// Drummer-specific velocity shaper that provides velocity hints for drum candidates.
    /// Maps normalized dynamic intents to style-aware velocity targets.
    /// Does NOT write final MIDI velocities - that remains the groove VelocityShaper's responsibility.
    /// Story 6.1: Implement Drummer Velocity Shaper.
    /// </summary>
    public static class DrummerVelocityShaper
    {
        /// <summary>
        /// Applies velocity hints to a list of drum candidates.
        /// Updates VelocityHint field based on style configuration and candidate context.
        /// Returns new candidate records with hints applied.
        /// </summary>
        /// <param name="candidates">Source candidates to process</param>
        /// <param name="styleConfig">Style configuration with velocity hint settings</param>
        /// <param name="energyLevel">Current energy level (0.0-1.0) for energy-aware adjustments</param>
        /// <returns>New candidate list with velocity hints applied</returns>
        public static IReadOnlyList<DrumCandidate> ApplyHints(
            IReadOnlyList<DrumCandidate> candidates,
            StyleConfiguration? styleConfig,
            double energyLevel = 0.5)
        {
            ArgumentNullException.ThrowIfNull(candidates);

            if (candidates.Count == 0)
                return candidates;

            var settings = styleConfig?.GetDrummerVelocityHints()
                ?? DrummerVelocityHintSettings.ConservativeDefaults;

            var result = new List<DrumCandidate>(candidates.Count);

            foreach (var candidate in candidates)
            {
                var hintedCandidate = ApplyHintToCandidate(candidate, settings, energyLevel);
                result.Add(hintedCandidate);
            }

            return result;
        }

        /// <summary>
        /// Applies velocity hint to a single candidate.
        /// If candidate already has a VelocityHint, adjusts minimally toward style target.
        /// If candidate has no VelocityHint, provides conservative style-based hint.
        /// </summary>
        public static DrumCandidate ApplyHintToCandidate(
            DrumCandidate candidate,
            DrummerVelocityHintSettings settings,
            double energyLevel = 0.5)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            settings ??= DrummerVelocityHintSettings.ConservativeDefaults;

            // Classify the candidate's dynamic intent
            var intent = ClassifyDynamicIntent(candidate);

            // Get target velocity for this intent
            int targetVelocity = settings.GetTargetVelocity(intent);

            // Apply energy-based adjustment (±10% based on energy level)
            targetVelocity = ApplyEnergyAdjustment(targetVelocity, energyLevel);

            // Compute final hint
            int hintVelocity;

            if (candidate.VelocityHint.HasValue)
            {
                // Existing hint: adjust minimally toward target
                hintVelocity = AdjustMinimally(
                    candidate.VelocityHint.Value,
                    targetVelocity,
                    settings.MaxAdjustmentDelta);
            }
            else
            {
                // No existing hint: use target directly
                hintVelocity = targetVelocity;
            }

            // Clamp to valid MIDI range
            hintVelocity = Math.Clamp(hintVelocity, 1, 127);

            return candidate with { VelocityHint = hintVelocity };
        }

        /// <summary>
        /// Classifies a drum candidate's dynamic intent based on strength and fill role.
        /// </summary>
        public static DynamicIntent ClassifyDynamicIntent(DrumCandidate candidate)
        {
            ArgumentNullException.ThrowIfNull(candidate);

            // Fill candidates have special classification based on FillRole
            if (candidate.FillRole != FillRole.None)
            {
                return candidate.FillRole switch
                {
                    FillRole.Setup => DynamicIntent.StrongAccent,
                    FillRole.FillStart => DynamicIntent.FillRampStart,
                    FillRole.FillBody => DynamicIntent.FillRampBody,
                    FillRole.FillEnd => DynamicIntent.FillRampEnd,
                    _ => DynamicIntent.Medium
                };
            }

            // Crash cymbal always gets peak accent
            if (candidate.Role == GrooveRoles.Crash)
            {
                return DynamicIntent.PeakAccent;
            }

            // Map onset strength to dynamic intent
            return candidate.Strength switch
            {
                OnsetStrength.Downbeat => DynamicIntent.StrongAccent,
                OnsetStrength.Backbeat => DynamicIntent.StrongAccent,
                OnsetStrength.Strong => DynamicIntent.Medium,
                OnsetStrength.Offbeat => DynamicIntent.MediumLow,
                OnsetStrength.Pickup => DynamicIntent.MediumLow,
                OnsetStrength.Ghost => DynamicIntent.Low,
                _ => DynamicIntent.Medium
            };
        }

        /// <summary>
        /// Adjusts existing velocity hint minimally toward target.
        /// Preserves operator intent while nudging toward style.
        /// </summary>
        private static int AdjustMinimally(int currentHint, int target, int maxDelta)
        {
            if (maxDelta <= 0)
                return currentHint;

            int diff = target - currentHint;

            if (Math.Abs(diff) <= maxDelta)
            {
                // Within adjustment range: move to target
                return target;
            }

            // Outside range: move by maxDelta toward target
            return diff > 0
                ? currentHint + maxDelta
                : currentHint - maxDelta;
        }

        /// <summary>
        /// Applies energy-based adjustment to velocity target.
        /// Higher energy = slightly higher velocities.
        /// </summary>
        private static int ApplyEnergyAdjustment(int velocity, double energyLevel)
        {
            // Energy adjustment: ±10% based on energy level (0.5 = neutral)
            // energyLevel 0.0 → -5%, energyLevel 1.0 → +5%
            double energyMultiplier = 1.0 + (energyLevel - 0.5) * 0.1;

            return (int)Math.Round(velocity * energyMultiplier, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Applies velocity hints with full diagnostics for debugging.
        /// </summary>
        public static (DrumCandidate candidate, VelocityHintDiagnostics diagnostics) ApplyHintWithDiagnostics(
            DrumCandidate candidate,
            DrummerVelocityHintSettings settings,
            double energyLevel = 0.5)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            settings ??= DrummerVelocityHintSettings.ConservativeDefaults;

            var intent = ClassifyDynamicIntent(candidate);
            int baseTarget = settings.GetTargetVelocity(intent);
            int energyAdjustedTarget = ApplyEnergyAdjustment(baseTarget, energyLevel);

            int? originalHint = candidate.VelocityHint;
            int finalHint;

            if (candidate.VelocityHint.HasValue)
            {
                finalHint = AdjustMinimally(
                    candidate.VelocityHint.Value,
                    energyAdjustedTarget,
                    settings.MaxAdjustmentDelta);
            }
            else
            {
                finalHint = energyAdjustedTarget;
            }

            finalHint = Math.Clamp(finalHint, 1, 127);

            var hintedCandidate = candidate with { VelocityHint = finalHint };

            var diagnostics = new VelocityHintDiagnostics
            {
                CandidateId = candidate.CandidateId,
                Role = candidate.Role,
                Strength = candidate.Strength,
                FillRole = candidate.FillRole,
                ClassifiedIntent = intent,
                BaseTargetVelocity = baseTarget,
                EnergyLevel = energyLevel,
                EnergyAdjustedTarget = energyAdjustedTarget,
                OriginalHint = originalHint,
                FinalHint = finalHint,
                WasAdjusted = originalHint.HasValue && originalHint.Value != finalHint
            };

            return (hintedCandidate, diagnostics);
        }
    }

    /// <summary>
    /// Diagnostics record for velocity hint application (debugging/testing).
    /// </summary>
    public sealed record VelocityHintDiagnostics
    {
        public required string CandidateId { get; init; }
        public required string Role { get; init; }
        public required OnsetStrength Strength { get; init; }
        public required FillRole FillRole { get; init; }
        public required DynamicIntent ClassifiedIntent { get; init; }
        public required int BaseTargetVelocity { get; init; }
        public required double EnergyLevel { get; init; }
        public required int EnergyAdjustedTarget { get; init; }
        public int? OriginalHint { get; init; }
        public required int FinalHint { get; init; }
        public required bool WasAdjusted { get; init; }
    }
}
