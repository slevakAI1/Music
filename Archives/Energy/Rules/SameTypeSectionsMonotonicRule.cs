// AI: purpose=Constraint rule ensuring same-type sections have monotonically increasing energy (V2 >= V1, C2 >= C1).
// AI: invariants=Deterministic; only applies when previous same-type section exists; energy always [0..1].
// AI: deps=Extends EnergyConstraintRule; used by EnergyConstraintPolicy; implements common songwriting heuristic.

namespace Music.Generator.EnergyConstraints
{
    /// <summary>
    /// Ensures that repeated sections of the same type have energy greater than or equal to previous instances.
    /// Example: Verse 2 energy >= Verse 1 energy, Chorus 2 energy >= Chorus 1 energy.
    /// This reflects the common songwriting pattern of building energy across verse/chorus repetitions.
    /// </summary>
    public sealed class SameTypeSectionsMonotonicRule : EnergyConstraintRule
    {
        private readonly double _strength;
        private readonly double _minIncrement;

        /// <summary>
        /// Creates a monotonic progression rule.
        /// </summary>
        /// <param name="strength">Rule strength [0..2]. Default 1.0. Higher = more influence in conflict resolution.</param>
        /// <param name="minIncrement">Minimum energy increment for later sections. Default 0.0 (allow equal). Use 0.02-0.05 to force growth.</param>
        public SameTypeSectionsMonotonicRule(double strength = 1.0, double minIncrement = 0.0)
        {
            _strength = Math.Clamp(strength, 0.0, 2.0);
            _minIncrement = Math.Max(0.0, minIncrement);
        }

        public override string RuleName => "SameTypeSectionsMonotonic";

        public override double Strength => _strength;

        public override EnergyConstraintResult Evaluate(EnergyConstraintContext context)
        {
            // Rule only applies when there's a previous section of the same type
            if (!context.PreviousSameTypeEnergy.HasValue)
            {
                return EnergyConstraintResult.NoOpinion();
            }

            double previousEnergy = context.PreviousSameTypeEnergy.Value;
            double proposedEnergy = context.ProposedEnergy;
            double minRequiredEnergy = previousEnergy + _minIncrement;

            // If proposed energy already satisfies constraint, accept it
            if (proposedEnergy >= minRequiredEnergy)
            {
                return EnergyConstraintResult.Accept(
                    $"proposed {proposedEnergy:F3} >= previous same-type {previousEnergy:F3} (no adjustment needed)");
            }

            // Adjust to minimum required energy
            double adjustedEnergy = Clamp(minRequiredEnergy);

            string sectionTypeName = context.SectionType.ToString();
            int instanceNumber = context.SectionIndex + 1; // 0-based to 1-based

            return EnergyConstraintResult.Adjusted(
                adjustedEnergy,
                $"{sectionTypeName} {instanceNumber}: adjusted {proposedEnergy:F3} ? {adjustedEnergy:F3} " +
                $"(previous {sectionTypeName} was {previousEnergy:F3}, min increment {_minIncrement:F3})");
        }
    }
}
