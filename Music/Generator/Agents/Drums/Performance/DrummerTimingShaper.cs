// AI: purpose=Drummer-specific timing hinting that normalizes operator hints and maps to style-aware tick offsets.
// AI: invariants=Only updates TimingHint, never final timing offset; deterministic; clamps to MaxAbsoluteOffset; runs BEFORE groove RoleTimingEngine.
// AI: deps=DrumCandidate, DrummerTimingHintSettings, TimingIntent, FillRole; consumed by DrummerCandidateSource.
// AI: change=Story 6.2; extend intent classification as operator patterns evolve.

using Music.Generator.Agents.Common;

namespace Music.Generator.Agents.Drums.Performance
{
    /// <summary>
    /// Drummer-specific timing shaper that provides timing hints for drum candidates.
    /// Maps normalized timing intents to style-aware tick offsets.
    /// Does NOT write final timing offsets - that remains the groove RoleTimingEngine's responsibility.
    /// Story 6.2: Implement Drummer Timing Nuance.
    /// </summary>
    public static class DrummerTimingShaper
    {
        /// <summary>
        /// Applies timing hints to a list of drum candidates.
        /// Updates TimingHint field based on style configuration and candidate context.
        /// Returns new candidate records with hints applied.
        /// </summary>
        /// <param name="candidates">Source candidates to process</param>
        /// <param name="styleConfig">Style configuration with timing hint settings</param>
        /// <param name="energyLevel">Current energy level (0.0-1.0) for energy-aware adjustments</param>
        /// <returns>New candidate list with timing hints applied</returns>
        public static IReadOnlyList<DrumCandidate> ApplyHints(
            IReadOnlyList<DrumCandidate> candidates,
            StyleConfiguration? styleConfig,
            double energyLevel = 0.5)
        {
            ArgumentNullException.ThrowIfNull(candidates);

            if (candidates.Count == 0)
                return candidates;

            var settings = styleConfig?.GetDrummerTimingHints()
                ?? DrummerTimingHintSettings.ConservativeDefaults;

            var result = new List<DrumCandidate>(candidates.Count);

            foreach (var candidate in candidates)
            {
                var hintedCandidate = ApplyHintToCandidate(candidate, settings, energyLevel);
                result.Add(hintedCandidate);
            }

            return result;
        }

        /// <summary>
        /// Applies timing hint to a single candidate.
        /// If candidate already has a TimingHint, adjusts minimally toward style target.
        /// If candidate has no TimingHint, provides conservative style-based hint.
        /// </summary>
        public static DrumCandidate ApplyHintToCandidate(
            DrumCandidate candidate,
            DrummerTimingHintSettings settings,
            double energyLevel = 0.5)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            settings ??= DrummerTimingHintSettings.ConservativeDefaults;

            // Classify the candidate's timing intent
            var intent = ClassifyTimingIntent(candidate, settings);

            // Get target tick offset for this intent
            int targetOffset = settings.GetTickOffset(intent);

            // Apply energy-based adjustment (±2 ticks based on energy level)
            targetOffset = ApplyEnergyAdjustment(targetOffset, energyLevel);

            // Compute base hint before jitter
            int hintOffset;

            if (candidate.TimingHint.HasValue)
            {
                // Existing hint: adjust minimally toward target
                hintOffset = AdjustMinimally(
                    candidate.TimingHint.Value,
                    targetOffset,
                    settings.MaxAdjustmentDelta);
            }
            else
            {
                // No existing hint: use target directly
                hintOffset = targetOffset;
            }

            // Apply deterministic jitter for humanization
            int jitter = settings.ComputeJitter(
                candidate.BarNumber,
                candidate.Beat,
                candidate.Role,
                candidate.CandidateId);

            hintOffset += jitter;

            // Clamp to max absolute offset
            hintOffset = Math.Clamp(hintOffset, -settings.MaxAbsoluteOffset, settings.MaxAbsoluteOffset);

            return candidate with { TimingHint = hintOffset };
        }

        /// <summary>
        /// Classifies a drum candidate's timing intent based on FillRole and role.
        /// Priority: FillRole > Role (if FillRole != None, use fill-based timing).
        /// </summary>
        public static TimingIntent ClassifyTimingIntent(DrumCandidate candidate, DrummerTimingHintSettings? settings = null)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            settings ??= DrummerTimingHintSettings.ConservativeDefaults;

            // Fill candidates have special classification based on FillRole
            // Priority: FillRole > Role
            if (candidate.FillRole != FillRole.None)
            {
                return candidate.FillRole switch
                {
                    FillRole.Setup => TimingIntent.OnTop,          // Setup hits are precise
                    FillRole.FillStart => TimingIntent.SlightlyAhead,  // Start of fill pushes slightly
                    FillRole.FillBody => TimingIntent.Rushed,      // Fill body rushes toward climax
                    FillRole.FillEnd => TimingIntent.OnTop,        // Clean resolution on-grid
                    _ => TimingIntent.OnTop
                };
            }

            // Use role-based default from settings
            return settings.GetRoleDefaultIntent(candidate.Role);
        }

        /// <summary>
        /// Adjusts existing timing hint minimally toward target.
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
        /// Applies energy-based adjustment to timing offset.
        /// High energy (>0.5) nudges toward earlier (rush).
        /// Low energy (<0.5) nudges toward later (laid-back).
        /// Range: ±2 ticks.
        /// </summary>
        private static int ApplyEnergyAdjustment(int offset, double energyLevel)
        {
            // Energy adjustment: ±2 ticks based on energy level (0.5 = neutral)
            // energyLevel 0.0 → +2 ticks (laid-back)
            // energyLevel 1.0 → -2 ticks (rush)
            double energyAdjustment = (0.5 - energyLevel) * 4.0;

            return offset + (int)Math.Round(energyAdjustment, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Applies timing hints with full diagnostics for debugging.
        /// </summary>
        public static (DrumCandidate candidate, TimingHintDiagnostics diagnostics) ApplyHintWithDiagnostics(
            DrumCandidate candidate,
            DrummerTimingHintSettings settings,
            double energyLevel = 0.5)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            settings ??= DrummerTimingHintSettings.ConservativeDefaults;

            var intent = ClassifyTimingIntent(candidate, settings);
            int baseTarget = settings.GetTickOffset(intent);
            int energyAdjustedTarget = ApplyEnergyAdjustment(baseTarget, energyLevel);

            int? originalHint = candidate.TimingHint;
            int hintBeforeJitter;

            if (candidate.TimingHint.HasValue)
            {
                hintBeforeJitter = AdjustMinimally(
                    candidate.TimingHint.Value,
                    energyAdjustedTarget,
                    settings.MaxAdjustmentDelta);
            }
            else
            {
                hintBeforeJitter = energyAdjustedTarget;
            }

            int jitter = settings.ComputeJitter(
                candidate.BarNumber,
                candidate.Beat,
                candidate.Role,
                candidate.CandidateId);

            int finalHint = Math.Clamp(
                hintBeforeJitter + jitter,
                -settings.MaxAbsoluteOffset,
                settings.MaxAbsoluteOffset);

            var diagnostics = new TimingHintDiagnostics(
                CandidateId: candidate.CandidateId,
                Role: candidate.Role,
                FillRole: candidate.FillRole,
                ClassifiedIntent: intent,
                BaseTargetOffset: baseTarget,
                EnergyLevel: energyLevel,
                EnergyAdjustedTarget: energyAdjustedTarget,
                OriginalHint: originalHint,
                HintBeforeJitter: hintBeforeJitter,
                Jitter: jitter,
                FinalHint: finalHint);

            return (candidate with { TimingHint = finalHint }, diagnostics);
        }
    }

    /// <summary>
    /// Diagnostics record for timing hint application.
    /// Used for debugging and verification of timing decisions.
    /// </summary>
    public sealed record TimingHintDiagnostics(
        string CandidateId,
        string Role,
        FillRole FillRole,
        TimingIntent ClassifiedIntent,
        int BaseTargetOffset,
        double EnergyLevel,
        int EnergyAdjustedTarget,
        int? OriginalHint,
        int HintBeforeJitter,
        int Jitter,
        int FinalHint);
}
