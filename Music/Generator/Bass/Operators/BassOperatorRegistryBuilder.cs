// AI: purpose=Builder for bassOperatorRegistry; registers all operators in deterministic order for startup/tests.
// AI: invariants=Call RegisterAllOperators before Freeze; registry order is stable and reproducible across runs.
// AI: deps=bassOperatorRegistry and concrete operator classes; used by bassmerOperatorCandidates and tests.

//using Music.Generator.Bass.Operators.MicroAddition;
//using Music.Generator.Bass.Operators.NoteRemoval;
//using Music.Generator.Bass.Operators.SubdivisionTransform;
//using Music.Generator.Bass.Operators.PhrasePunctuation;
//using Music.Generator.Bass.Operators.PatternSubstitution;
//using Music.Generator.Bass.Operators.StyleIdiom;
using Music.Generator.Core;
using Music.Generator.Drums.Operators;

namespace Music.Generator.Bass.Operators
{
    /// <summary>
    /// Builds and populates a bassOperatorRegistry with all available Bass operators.
    /// Story 2.4: Stub implementation.
    /// Story 3.1: Registers MicroAddition operators (7 operators).
    /// Story 3.2: Registers SubdivisionTransform operators (5 operators).
    /// Story 3.3: Registers PhrasePunctuation operators (7 operators).
    /// Story 3.4: Registers PatternSubstitution operators (4 operators).
    /// Story 3.6: Completes registration with StyleIdiom operators (full 28 operators).
    /// NoteRemoval: Registers subtractive operators (3 operators, total 31).
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
    public static class BassOperatorRegistryBuilder
    {
        // Create and populate a registry with all operators, validate expected count, then freeze.
        // Throws InvalidOperationException when validation fails.
        public static DrumOperatorRegistry BuildComplete()
        {
            var registry = DrumOperatorRegistry.CreateEmpty();
            RegisterAllOperators(registry);
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
            // Register families in deterministic order
            //RegisterMicroAdditionOperators(registry);
            //RegisterSubdivisionTransformOperators(registry);
            //RegisterPhrasePunctuationOperators(registry);
            //RegisterPatternSubstitutionOperators(registry);
            //RegisterStyleIdiomOperators(registry);
            //RegisterNoteRemovalOperators(registry);
        }

        // Register MicroAddition operators (ghosts, pickups, embellishments)
        private static void RegisterMicroAdditionOperators(DrumOperatorRegistry registry)
        {
            //registry.RegisterOperator(new GhostBeforeBackbeatOperator());
            //registry.RegisterOperator(new GhostAfterBackbeatOperator());
            //registry.RegisterOperator(new KickPickupOperator());
            //registry.RegisterOperator(new KickDoubleOperator());
            //registry.RegisterOperator(new HatEmbellishmentOperator());
            //registry.RegisterOperator(new GhostClusterOperator());
            //registry.RegisterOperator(new FloorTomPickupOperator());
        }

        // Register SubdivisionTransform operators (hat/ride swaps, partial lifts)
        private static void RegisterSubdivisionTransformOperators(DrumOperatorRegistry registry)
        {
            //registry.RegisterOperator(new HatLiftOperator());
            //registry.RegisterOperator(new HatDropOperator());
            //registry.RegisterOperator(new RideSwapOperator());
            //registry.RegisterOperator(new PartialLiftOperator());
            //registry.RegisterOperator(new OpenHatAccentOperator());
        }

        // Register PhrasePunctuation operators (fills, crashes, setup/stop time)
        private static void RegisterPhrasePunctuationOperators(DrumOperatorRegistry registry)
        {
            //registry.RegisterOperator(new CrashOnOneOperator());
            //registry.RegisterOperator(new TurnaroundFillShortOperator());
            //registry.RegisterOperator(new TurnaroundFillFullOperator());
            //registry.RegisterOperator(new SetupHitOperator());
            //registry.RegisterOperator(new StopTimeOperator());
            //registry.RegisterOperator(new BuildFillOperator());
            //registry.RegisterOperator(new DropFillOperator());
        }

        // Register PatternSubstitution operators (groove swaps, half/double time)
        private static void RegisterPatternSubstitutionOperators(DrumOperatorRegistry registry)
        {
            //registry.RegisterOperator(new BackbeatVariantOperator());
            //registry.RegisterOperator(new KickPatternVariantOperator());
            //registry.RegisterOperator(new HalfTimeFeelOperator());
            //registry.RegisterOperator(new DoubleTimeFeelOperator());
        }

        // Register StyleIdiom operators (PopRock specific behaviors)
        private static void RegisterStyleIdiomOperators(DrumOperatorRegistry registry)
        {
            //registry.RegisterOperator(new PopRockBackbeatPushOperator());
            //registry.RegisterOperator(new RockKickSyncopationOperator());
            //registry.RegisterOperator(new PopChorusCrashPatternOperator());
            //registry.RegisterOperator(new VerseSimplifyOperator());
            //registry.RegisterOperator(new BridgeBreakdownOperator());
        }

        // Register NoteRemoval operators (subtractive: thinning, pulling, sparsifying)
        private static void RegisterNoteRemovalOperators(DrumOperatorRegistry registry)
        {
            //registry.RegisterOperator(new HatThinningOperator());
            //registry.RegisterOperator(new KickPullOperator());
            //registry.RegisterOperator(new SparseGrooveOperator());
        }
    }
}
