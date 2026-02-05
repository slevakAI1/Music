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
            StyleConfiguration? styleConfig)
        {
            ArgumentNullException.ThrowIfNull(candidates);

            if (candidates.Count == 0)
                return candidates;

            var settings = styleConfig?.GetDrummerVelocityHints()
                ?? DrummerVelocityHintSettings.ConservativeDefaults;

            var result = new List<DrumCandidate>(candidates.Count);

            foreach (var candidate in candidates)
            {
                var hintedCandidate = ApplyHintToCandidate(candidate, settings);
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
            DrummerVelocityHintSettings settings)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            settings ??= DrummerVelocityHintSettings.ConservativeDefaults;

            // Classify the candidate's dynamic intent
            var intent = ClassifyDynamicIntent(candidate);

            // Get target velocity for this intent
            int targetVelocity = settings.GetTargetVelocity(intent);

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
        /// Applies velocity hints with full diagnostics for debugging.
        /// </summary>
        public static (DrumCandidate candidate, VelocityHintDiagnostics diagnostics) ApplyHintWithDiagnostics(
            DrumCandidate candidate,
            DrummerVelocityHintSettings settings)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            settings ??= DrummerVelocityHintSettings.ConservativeDefaults;

            var intent = ClassifyDynamicIntent(candidate);
            int baseTarget = settings.GetTargetVelocity(intent);

            int? originalHint = candidate.VelocityHint;
            int finalHint;

            if (candidate.VelocityHint.HasValue)
            {
                finalHint = AdjustMinimally(
                    candidate.VelocityHint.Value,
                    baseTarget,
                    settings.MaxAdjustmentDelta);
            }
            else
            {
                finalHint = baseTarget;
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
                EnergyLevel = 0.5,
                EnergyAdjustedTarget = baseTarget,
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
