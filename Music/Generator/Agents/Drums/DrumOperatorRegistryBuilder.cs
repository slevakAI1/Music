// AI: purpose=Builder for DrumOperatorRegistry; registers all drum operators for discovery and filtering.
// AI: invariants=RegisterAll() must be called before any operators can be used; operators registered in deterministic order.
// AI: deps=DrumOperatorRegistry, all operator implementations across MicroAddition/SubdivisionTransform/PhrasePunctuation/PatternSubstitution/StyleIdiom families.
// AI: change=Story 2.4 stub; Story 3.1 adds MicroAddition; Story 3.2 adds SubdivisionTransform; Story 3.3 adds PhrasePunctuation; Story 3.4 adds PatternSubstitution; Story 3.5 adds StyleIdiom; Story 3.6 completes with all families.

using Music.Generator.Agents.Drums.Operators.MicroAddition;
using Music.Generator.Agents.Drums.Operators.SubdivisionTransform;
using Music.Generator.Agents.Drums.Operators.PhrasePunctuation;
using Music.Generator.Agents.Drums.Operators.PatternSubstitution;
using Music.Generator.Agents.Drums.Operators.StyleIdiom;

namespace Music.Generator.Agents.Drums
{
    /// <summary>
    /// Builds and populates a DrumOperatorRegistry with all available drum operators.
    /// Story 2.4: Stub implementation.
    /// Story 3.1: Registers MicroAddition operators (7 operators).
    /// Story 3.2: Registers SubdivisionTransform operators (5 operators).
    /// Story 3.3: Registers PhrasePunctuation operators (7 operators).
    /// Story 3.4: Registers PatternSubstitution operators (4 operators).
    /// Story 3.6: Completes registration with StyleIdiom operators (full 28 operators).
    /// </summary>
    /// <remarks>
    /// Operators are registered in a deterministic order for reproducibility:
    /// 1. MicroAddition family (ghost notes, pickups, embellishments)
    /// 2. SubdivisionTransform family (timekeeping density changes)
    /// 3. PhrasePunctuation family (fills, crashes, boundaries)
    /// 4. PatternSubstitution family (groove swaps) - Story 3.4
    /// 5. StyleIdiom family (genre-specific moves) - Story 3.5
    /// </remarks>
    public static class DrumOperatorRegistryBuilder
    {
        /// <summary>
        /// Creates and populates a new registry with all available drum operators.
        /// Operators are registered in deterministic order for reproducibility.
        /// </summary>
        /// <returns>A frozen registry ready for use.</returns>
        /// <exception cref="InvalidOperationException">If operator count != 28.</exception>
        public static DrumOperatorRegistry BuildComplete()
        {
            var registry = DrumOperatorRegistry.CreateEmpty();

            RegisterAllOperators(registry);

            // Story 3.6: Validate total operator count (7+5+7+4+5=28)
            const int ExpectedOperatorCount = 28;
            if (registry.Count != ExpectedOperatorCount)
            {
                var message = BuildCountValidationMessage(registry, ExpectedOperatorCount);
                throw new InvalidOperationException(message);
            }

            registry.Freeze();
            return registry;
        }

        /// <summary>
        /// Builds diagnostic message for operator count validation failure.
        /// </summary>
        private static string BuildCountValidationMessage(DrumOperatorRegistry registry, int expected)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Operator count validation failed. Expected {expected}, found {registry.Count}.");
            sb.AppendLine("Registered operators by family:");

            foreach (Common.OperatorFamily family in Enum.GetValues<Common.OperatorFamily>())
            {
                var ops = registry.GetOperatorsByFamily(family);
                sb.AppendLine($"  {family}: {ops.Count} operators");
                foreach (var op in ops)
                {
                    sb.AppendLine($"    - {op.OperatorId}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Registers all operators into the provided registry.
        /// Called by BuildComplete(); exposed for testing.
        /// </summary>
        /// <param name="registry">Registry to populate.</param>
        public static void RegisterAllOperators(DrumOperatorRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(registry);

            // Story 3.1: MicroAddition operators (7)
            RegisterMicroAdditionOperators(registry);

            // Story 3.2: SubdivisionTransform operators (5)
            RegisterSubdivisionTransformOperators(registry);

            // Story 3.3: PhrasePunctuation operators (7)
            RegisterPhrasePunctuationOperators(registry);

            // Story 3.4: PatternSubstitution operators (4)
            RegisterPatternSubstitutionOperators(registry);

            // Story 3.5: StyleIdiom operators (5)
            RegisterStyleIdiomOperators(registry);
        }

        /// <summary>
        /// Registers MicroAddition family operators (ghost notes, pickups, embellishments).
        /// Story 3.1: 7 operators.
        /// </summary>
        private static void RegisterMicroAdditionOperators(DrumOperatorRegistry registry)
        {
            registry.RegisterOperator(new GhostBeforeBackbeatOperator());
            registry.RegisterOperator(new GhostAfterBackbeatOperator());
            registry.RegisterOperator(new KickPickupOperator());
            registry.RegisterOperator(new KickDoubleOperator());
            registry.RegisterOperator(new HatEmbellishmentOperator());
            registry.RegisterOperator(new GhostClusterOperator());
            registry.RegisterOperator(new FloorTomPickupOperator());
        }

        /// <summary>
        /// Registers SubdivisionTransform family operators (timekeeping density changes).
        /// Story 3.2: 5 operators.
        /// </summary>
        private static void RegisterSubdivisionTransformOperators(DrumOperatorRegistry registry)
        {
            registry.RegisterOperator(new HatLiftOperator());
            registry.RegisterOperator(new HatDropOperator());
            registry.RegisterOperator(new RideSwapOperator());
            registry.RegisterOperator(new PartialLiftOperator());
            registry.RegisterOperator(new OpenHatAccentOperator());
        }

        /// <summary>
        /// Registers PhrasePunctuation family operators (fills, crashes, section boundaries).
        /// Story 3.3: 7 operators.
        /// </summary>
        private static void RegisterPhrasePunctuationOperators(DrumOperatorRegistry registry)
        {
            registry.RegisterOperator(new CrashOnOneOperator());
            registry.RegisterOperator(new TurnaroundFillShortOperator());
            registry.RegisterOperator(new TurnaroundFillFullOperator());
            registry.RegisterOperator(new SetupHitOperator());
            registry.RegisterOperator(new StopTimeOperator());
            registry.RegisterOperator(new BuildFillOperator());
            registry.RegisterOperator(new DropFillOperator());
        }

        /// <summary>
        /// Registers PatternSubstitution family operators (groove swaps, feel changes).
        /// Story 3.4: 4 operators.
        /// </summary>
        private static void RegisterPatternSubstitutionOperators(DrumOperatorRegistry registry)
        {
            registry.RegisterOperator(new BackbeatVariantOperator());
            registry.RegisterOperator(new KickPatternVariantOperator());
            registry.RegisterOperator(new HalfTimeFeelOperator());
            registry.RegisterOperator(new DoubleTimeFeelOperator());
        }

        /// <summary>
        /// Registers StyleIdiom family operators (genre-specific Pop Rock moves).
        /// Story 3.5: 5 operators.
        /// </summary>
        private static void RegisterStyleIdiomOperators(DrumOperatorRegistry registry)
        {
            registry.RegisterOperator(new PopRockBackbeatPushOperator());
            registry.RegisterOperator(new RockKickSyncopationOperator());
            registry.RegisterOperator(new PopChorusCrashPatternOperator());
            registry.RegisterOperator(new VerseSimplifyOperator());
            registry.RegisterOperator(new BridgeBreakdownOperator());
        }
    }
}
