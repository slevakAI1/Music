// AI: purpose=Constraint rule ensuring final chorus is at or near song's peak energy.
// AI: invariants=Deterministic; only applies to last chorus; considers song-wide energy distribution.
// AI: deps=Extends EnergyConstraintRule; implements climactic chorus pattern common in pop/rock.

namespace Music.Generator.EnergyConstraints
{
    /// <summary>
    /// Ensures that the final chorus is at or near the song's peak energy level.
    /// This reflects the common songwriting pattern where the last chorus serves as the
    /// emotional and energetic climax before the outro/resolution.
    /// </summary>
    public sealed class FinalChorusPeakRule : EnergyConstraintRule
    {
        private readonly double _strength;
        private readonly double _minPeakEnergy;
        private readonly double _peakProximityThreshold;

        /// <summary>
        /// Creates a final chorus peak rule.
        /// </summary>
        /// <param name="strength">Rule strength [0..2]. Default 1.5 (strong influence).</param>
        /// <param name="minPeakEnergy">Minimum energy for final chorus. Default 0.80.</param>
        /// <param name="peakProximityThreshold">How close to song peak the final chorus should be. Default 0.95 (within 5%).</param>
        public FinalChorusPeakRule(
            double strength = 1.5,
            double minPeakEnergy = 0.80,
            double peakProximityThreshold = 0.95)
        {
            _strength = Math.Clamp(strength, 0.0, 2.0);
            _minPeakEnergy = Math.Clamp(minPeakEnergy, 0.0, 1.0);
            _peakProximityThreshold = Math.Clamp(peakProximityThreshold, 0.0, 1.0);
        }

        public override string RuleName => "FinalChorusPeak";

        public override double Strength => _strength;

        public override EnergyConstraintResult Evaluate(EnergyConstraintContext context)
        {
            // Rule only applies to chorus sections
            if (context.SectionType != MusicConstants.eSectionType.Chorus)
            {
                return EnergyConstraintResult.NoOpinion();
            }

            // Rule only applies to the last chorus
            if (!context.IsLastOfType)
            {
                return EnergyConstraintResult.NoOpinion();
            }

            double proposedEnergy = context.ProposedEnergy;

            // Find the current peak energy in the song so far
            double currentPeak = context.FinalizedEnergies.Count > 0
                ? context.FinalizedEnergies.Values.Max()
                : proposedEnergy;

            // Target energy: at least minPeakEnergy, and close to (or exceeding) current peak
            double targetEnergy = Math.Max(_minPeakEnergy, currentPeak * _peakProximityThreshold);
            targetEnergy = Math.Min(targetEnergy, 1.0); // Cap at 1.0

            // If proposed energy already meets target, accept it
            if (proposedEnergy >= targetEnergy)
            {
                return EnergyConstraintResult.Accept(
                    $"final chorus energy {proposedEnergy:F3} >= target {targetEnergy:F3} (peak is {currentPeak:F3})");
            }

            // Adjust to target energy
            double adjustedEnergy = Clamp(targetEnergy);

            return EnergyConstraintResult.Adjusted(
                adjustedEnergy,
                $"final chorus peak: adjusted {proposedEnergy:F3} ? {adjustedEnergy:F3} " +
                $"(song peak {currentPeak:F3}, min {_minPeakEnergy:F3}, proximity {_peakProximityThreshold:F3})");
        }
    }
}
