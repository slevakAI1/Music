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
            StyleConfiguration? styleConfig)
        {
            ArgumentNullException.ThrowIfNull(candidates);

            if (candidates.Count == 0)
                return candidates;

            var settings = styleConfig?.GetDrummerTimingHints()
                ?? DrummerTimingHintSettings.ConservativeDefaults;

            var result = new List<DrumCandidate>(candidates.Count);

            foreach (var candidate in candidates)
            {
                var hintedCandidate = ApplyHintToCandidate(candidate, settings);
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
            DrummerTimingHintSettings settings)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            settings ??= DrummerTimingHintSettings.ConservativeDefaults;

            // Classify the candidate's timing intent
            var intent = ClassifyTimingIntent(candidate, settings);

            // Get target tick offset for this intent
            int targetOffset = settings.GetTickOffset(intent);

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
        /// Applies timing hints with full diagnostics for debugging.
        /// </summary>
        public static (DrumCandidate candidate, TimingHintDiagnostics diagnostics) ApplyHintWithDiagnostics(
            DrumCandidate candidate,
            DrummerTimingHintSettings settings)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            settings ??= DrummerTimingHintSettings.ConservativeDefaults;

            var intent = ClassifyTimingIntent(candidate, settings);
            int baseTarget = settings.GetTickOffset(intent);

            int? originalHint = candidate.TimingHint;
            int hintBeforeJitter;

            if (candidate.TimingHint.HasValue)
            {
                hintBeforeJitter = AdjustMinimally(
                    candidate.TimingHint.Value,
                    baseTarget,
                    settings.MaxAdjustmentDelta);
            }
            else
            {
                hintBeforeJitter = baseTarget;
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
                EnergyLevel: 0.5,
                EnergyAdjustedTarget: baseTarget,
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
