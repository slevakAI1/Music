// AI: purpose=Builder for DrumOperatorRegistry; registers all operators in deterministic order for startup/tests.
// AI: invariants=Call RegisterAllOperators before Freeze; registry order is stable and reproducible across runs.
// AI: deps=DrumOperatorRegistry and concrete operator classes; used by DrummerOperatorCandidates and tests.

using Music.Generator.Drums.Operators.MicroAddition;
using Music.Generator.Drums.Operators.NoteRemoval;
using Music.Generator.Drums.Operators.SubdivisionTransform;
using Music.Generator.Drums.Operators.PhrasePunctuation;
using Music.Generator.Drums.Operators.PatternSubstitution;
using Music.Generator.Drums.Operators.StyleIdiom;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Core;

namespace Music.Generator.Drums.Operators
{
    /// <summary>
    /// Builds and populates a DrumOperatorRegistry with all available drum operators.
    /// </summary>
    /// <remarks>
    /// Operators are registered in a deterministic order for reproducibility:
    /// 1. MicroAddition family (ghost notes, pickups, embellishments)
    /// 2. SubdivisionTransform family (timekeeping density changes)
    /// 3. PhrasePunctuation family (fills, crashes, boundaries)
    /// 4. PatternSubstitution family (groove swaps) - Story 3.4
    /// 5. StyleIdiom family (genre-specific moves) - Story 3.5
    /// 6. NoteRemoval family (subtractive operators for variance)
    /// </remarks>
    public static class DrumOperatorRegistryBuilder
    {
        // Create and populate a registry with all operators, validate expected count, then freeze.
        // Throws InvalidOperationException when validation fails.
        public static DrumOperatorRegistry BuildComplete()
        {
            var registry = new DrumOperatorRegistry();

            RegisterAllOperators(registry);

            // Validate total operator count to catch incomplete registrations in tests/dev
            const int ExpectedOperatorCount = 31;
            if (registry.Count != ExpectedOperatorCount)
            {
                var message = BuildCountValidationMessage(registry, ExpectedOperatorCount);
                throw new InvalidOperationException(message);
            }

            registry.Freeze();
            return registry;
        }

        // Build a diagnostic message listing registered operators by family when validation fails.
        private static string BuildCountValidationMessage(DrumOperatorRegistry registry, int expected)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Operator count validation failed. Expected {expected}, found {registry.Count}.");
            sb.AppendLine("Registered operators by family:");

            foreach (OperatorFamily family in Enum.GetValues<OperatorFamily>())
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

        // RegisterAllOperators: populate registry by calling family registration helpers. Exposed for tests.
        public static void RegisterAllOperators(DrumOperatorRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(registry);
            OperatorBase.DefaultInstrumentAdapter = DrumOperatorCandidateInstrumentAdapter.Instance;
            // Register families in deterministic order
            RegisterMicroAdditionOperators(registry);
            RegisterSubdivisionTransformOperators(registry);
            RegisterPhrasePunctuationOperators(registry);
            RegisterPatternSubstitutionOperators(registry);
            RegisterStyleIdiomOperators(registry);
            RegisterNoteRemovalOperators(registry);
        }

        // Register MicroAddition operators (ghosts, pickups, embellishments)
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

        // Register SubdivisionTransform operators (hat/ride swaps, partial lifts)
        private static void RegisterSubdivisionTransformOperators(DrumOperatorRegistry registry)
        {
            registry.RegisterOperator(new HatLiftOperator());
            registry.RegisterOperator(new HatDropOperator());
            registry.RegisterOperator(new RideSwapOperator());
            registry.RegisterOperator(new PartialLiftOperator());
            registry.RegisterOperator(new OpenHatAccentOperator());
        }

        // Register PhrasePunctuation operators (fills, crashes, setup/stop time)
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

        // Register PatternSubstitution operators (groove swaps, half/double time)
        private static void RegisterPatternSubstitutionOperators(DrumOperatorRegistry registry)
        {
            registry.RegisterOperator(new BackbeatVariantOperator());
            registry.RegisterOperator(new KickPatternVariantOperator());
            registry.RegisterOperator(new HalfTimeFeelOperator());
            registry.RegisterOperator(new DoubleTimeFeelOperator());
        }

        // Register StyleIdiom operators (PopRock specific behaviors)
        private static void RegisterStyleIdiomOperators(DrumOperatorRegistry registry)
        {
            registry.RegisterOperator(new PopRockBackbeatPushOperator());
            registry.RegisterOperator(new RockKickSyncopationOperator());
            registry.RegisterOperator(new PopChorusCrashPatternOperator());
            registry.RegisterOperator(new VerseSimplifyOperator());
            registry.RegisterOperator(new BridgeBreakdownOperator());
        }

        // Register NoteRemoval operators (subtractive: thinning, pulling, sparsifying)
        private static void RegisterNoteRemovalOperators(DrumOperatorRegistry registry)
        {
            registry.RegisterOperator(new HatThinningOperator());
            registry.RegisterOperator(new KickPullOperator());
            registry.RegisterOperator(new SparseGrooveOperator());
        }
    }
}
