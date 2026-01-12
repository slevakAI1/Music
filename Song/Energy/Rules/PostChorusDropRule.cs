// AI: purpose=Constraint rule ensuring section after chorus typically drops energy for contrast.
// AI: invariants=Deterministic; only applies to sections immediately following chorus; respects explicit arc overrides.
// AI: deps=Extends EnergyConstraintRule; implements common pop/rock arrangement pattern.

namespace Music.Generator.EnergyConstraints
{
    /// <summary>
    /// Ensures that the first section after a chorus typically drops energy for contrast.
    /// This reflects the common songwriting pattern where choruses are high-energy peaks
    /// followed by a return to lower energy (verse, bridge, or outro).
    /// Rule can be overridden by explicit arc targets or disabled for certain styles (e.g., EDM).
    /// </summary>
    public sealed class PostChorusDropRule : EnergyConstraintRule
    {
        private readonly double _strength;
        private readonly double _maxEnergyAfterChorus;
        private readonly double _typicalDropAmount;

        /// <summary>
        /// Creates a post-chorus energy drop rule.
        /// </summary>
        /// <param name="strength">Rule strength [0..2]. Default 1.0.</param>
        /// <param name="maxEnergyAfterChorus">Maximum allowed energy for section after chorus. Default 0.55.</param>
        /// <param name="typicalDropAmount">Typical energy drop from chorus. Default 0.20.</param>
        public PostChorusDropRule(
            double strength = 1.0, 
            double maxEnergyAfterChorus = 0.55,
            double typicalDropAmount = 0.20)
        {
            _strength = Math.Clamp(strength, 0.0, 2.0);
            _maxEnergyAfterChorus = Math.Clamp(maxEnergyAfterChorus, 0.0, 1.0);
            _typicalDropAmount = Math.Max(0.0, typicalDropAmount);
        }

        public override string RuleName => "PostChorusDrop";

        public override double Strength => _strength;

        public override EnergyConstraintResult Evaluate(EnergyConstraintContext context)
        {
            // Rule only applies if previous section was a chorus
            if (context.PreviousSectionType != MusicConstants.eSectionType.Chorus)
            {
                return EnergyConstraintResult.NoOpinion();
            }

            // Rule doesn't apply if current section is also a chorus (back-to-back choruses OK)
            if (context.SectionType == MusicConstants.eSectionType.Chorus)
            {
                return EnergyConstraintResult.Accept("post-chorus rule skipped (current is also chorus)");
            }

            // Get previous chorus energy
            if (!context.PreviousAnySectionEnergy.HasValue)
            {
                return EnergyConstraintResult.NoOpinion();
            }

            double chorusEnergy = context.PreviousAnySectionEnergy.Value;
            double proposedEnergy = context.ProposedEnergy;

            // Compute suggested drop
            double suggestedEnergy = chorusEnergy - _typicalDropAmount;
            suggestedEnergy = Math.Clamp(suggestedEnergy, 0.0, _maxEnergyAfterChorus);

            // If proposed energy is already low enough, accept it
            if (proposedEnergy <= _maxEnergyAfterChorus)
            {
                return EnergyConstraintResult.Accept(
                    $"proposed {proposedEnergy:F3} already below max post-chorus {_maxEnergyAfterChorus:F3}");
            }

            // Suggest the drop
            double adjustedEnergy = Clamp(suggestedEnergy);

            return EnergyConstraintResult.Adjusted(
                adjustedEnergy,
                $"post-chorus drop: adjusted {proposedEnergy:F3} ? {adjustedEnergy:F3} " +
                $"(chorus was {chorusEnergy:F3}, typical drop {_typicalDropAmount:F3}, max {_maxEnergyAfterChorus:F3})");
        }
    }
}
