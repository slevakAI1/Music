// AI: purpose=Constraint rule allowing bridge to either exceed or drop below previous chorus for contrast.
// AI: invariants=Deterministic; only applies to bridge sections; allows dual valid outcomes (high or low).
// AI: deps=Extends EnergyConstraintRule; implements bridge contrast pattern (can be climactic OR breakdown).

namespace Music.Generator.EnergyConstraints
{
    /// <summary>
    /// Allows bridge sections to create contrast by either exceeding the previous chorus energy
    /// OR intentionally dropping below it (breakdown pattern).
    /// Bridges serve as contrasting sections, so both high-energy and low-energy bridges are valid.
    /// Rule primarily validates that the bridge creates meaningful contrast rather than being too similar.
    /// </summary>
    public sealed class BridgeContrastRule : EnergyConstraintRule
    {
        private readonly double _strength;
        private readonly double _minContrastAmount;
        private readonly double _neutralZone;

        /// <summary>
        /// Creates a bridge contrast rule.
        /// </summary>
        /// <param name="strength">Rule strength [0..2]. Default 0.8 (moderate - bridge has more freedom).</param>
        /// <param name="minContrastAmount">Minimum energy difference from previous chorus. Default 0.15.</param>
        /// <param name="neutralZone">Energy range around previous chorus considered "too similar". Default 0.10.</param>
        public BridgeContrastRule(
            double strength = 0.8,
            double minContrastAmount = 0.15,
            double neutralZone = 0.10)
        {
            _strength = Math.Clamp(strength, 0.0, 2.0);
            _minContrastAmount = Math.Max(0.0, minContrastAmount);
            _neutralZone = Math.Max(0.0, neutralZone);
        }

        public override string RuleName => "BridgeContrast";

        public override double Strength => _strength;

        public override EnergyConstraintResult Evaluate(EnergyConstraintContext context)
        {
            // Rule only applies to bridge sections
            if (context.SectionType != MusicConstants.eSectionType.Bridge)
            {
                return EnergyConstraintResult.NoOpinion();
            }

            // Find the most recent chorus energy (if any)
            double? recentChorusEnergy = FindRecentChorusEnergy(context);
            if (!recentChorusEnergy.HasValue)
            {
                // No previous chorus - bridge has freedom
                return EnergyConstraintResult.Accept("no previous chorus (bridge has freedom)");
            }

            double chorusEnergy = recentChorusEnergy.Value;
            double proposedEnergy = context.ProposedEnergy;
            double energyDiff = Math.Abs(proposedEnergy - chorusEnergy);

            // Check if proposed energy creates sufficient contrast
            if (energyDiff >= _minContrastAmount)
            {
                string direction = proposedEnergy > chorusEnergy ? "climactic" : "breakdown";
                return EnergyConstraintResult.Accept(
                    $"bridge creates {direction} contrast: {proposedEnergy:F3} vs chorus {chorusEnergy:F3} (diff {energyDiff:F3})");
            }

            // Energy is too similar - suggest contrast
            // Decide whether to go higher or lower based on which is closer
            double distanceToHighContrast = Math.Abs((chorusEnergy + _minContrastAmount) - proposedEnergy);
            double distanceToLowContrast = Math.Abs((chorusEnergy - _minContrastAmount) - proposedEnergy);

            double suggestedEnergy;
            string contrastType;

            if (distanceToHighContrast <= distanceToLowContrast)
            {
                // Closer to high contrast - make it climactic
                suggestedEnergy = chorusEnergy + _minContrastAmount;
                contrastType = "climactic";
            }
            else
            {
                // Closer to low contrast - make it a breakdown
                suggestedEnergy = chorusEnergy - _minContrastAmount;
                contrastType = "breakdown";
            }

            double adjustedEnergy = Clamp(suggestedEnergy);

            return EnergyConstraintResult.Adjusted(
                adjustedEnergy,
                $"bridge contrast ({contrastType}): adjusted {proposedEnergy:F3} ? {adjustedEnergy:F3} " +
                $"(chorus was {chorusEnergy:F3}, min contrast {_minContrastAmount:F3})");
        }

        /// <summary>
        /// Finds the energy of the most recent chorus section.
        /// </summary>
        private static double? FindRecentChorusEnergy(EnergyConstraintContext context)
        {
            // Search backwards through finalized energies for the most recent chorus
            // We need to match section indices to section types, so we look at previous sections
            
            // Simple approach: check if previous section was chorus
            if (context.PreviousSectionType == MusicConstants.eSectionType.Chorus &&
                context.PreviousAnySectionEnergy.HasValue)
            {
                return context.PreviousAnySectionEnergy.Value;
            }

            // More complex: scan through finalized energies
            // (This would require additional context about section types at each index,
            // which we'll add if needed in Story 7.4.2 integration)
            
            return null;
        }
    }
}
