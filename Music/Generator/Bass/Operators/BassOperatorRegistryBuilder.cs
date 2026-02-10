// AI: purpose=Builder for BassOperatorRegistry; registers operators in deterministic order for startup/tests.
// AI: invariants=Call RegisterAllOperators before Freeze; registration order is stable and reproducible.
// AI: deps=BassOperatorRegistry; no operators registered yet (empty registry expected).

//using Music.Generator.Bass.Operators.MicroAddition;
//using Music.Generator.Bass.Operators.NoteRemoval;
//using Music.Generator.Bass.Operators.SubdivisionTransform;
//using Music.Generator.Bass.Operators.PhrasePunctuation;
//using Music.Generator.Bass.Operators.PatternSubstitution;
//using Music.Generator.Bass.Operators.StyleIdiom;
using Music.Generator.Core;

namespace Music.Generator.Bass.Operators
{
    // AI: purpose=Builds and populates a BassOperatorRegistry; empty until bass operators are implemented.
    // AI: invariants=Register family groups in deterministic order when operators exist.
    public static class BassOperatorRegistryBuilder
    {
        // Create and populate a registry with all operators, validate expected count, then freeze.
        // Throws InvalidOperationException when validation fails.
        public static BassOperatorRegistry BuildComplete()
        {
            var registry = new BassOperatorRegistry();
            RegisterAllOperators(registry);
            registry.Freeze();
            return registry;
        }

        // RegisterAllOperators: populate registry by calling family registration helpers. Exposed for tests.
        public static void RegisterAllOperators(BassOperatorRegistry registry)
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
        private static void RegisterMicroAdditionOperators(BassOperatorRegistry registry)
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
        private static void RegisterSubdivisionTransformOperators(BassOperatorRegistry registry)
        {
            //registry.RegisterOperator(new HatLiftOperator());
            //registry.RegisterOperator(new HatDropOperator());
            //registry.RegisterOperator(new RideSwapOperator());
            //registry.RegisterOperator(new PartialLiftOperator());
            //registry.RegisterOperator(new OpenHatAccentOperator());
        }

        // Register PhrasePunctuation operators (fills, crashes, setup/stop time)
        private static void RegisterPhrasePunctuationOperators(BassOperatorRegistry registry)
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
        private static void RegisterPatternSubstitutionOperators(BassOperatorRegistry registry)
        {
            //registry.RegisterOperator(new BackbeatVariantOperator());
            //registry.RegisterOperator(new KickPatternVariantOperator());
            //registry.RegisterOperator(new HalfTimeFeelOperator());
            //registry.RegisterOperator(new DoubleTimeFeelOperator());
        }

        // Register StyleIdiom operators (PopRock specific behaviors)
        private static void RegisterStyleIdiomOperators(BassOperatorRegistry registry)
        {
            //registry.RegisterOperator(new PopRockBackbeatPushOperator());
            //registry.RegisterOperator(new RockKickSyncopationOperator());
            //registry.RegisterOperator(new PopChorusCrashPatternOperator());
            //registry.RegisterOperator(new VerseSimplifyOperator());
            //registry.RegisterOperator(new BridgeBreakdownOperator());
        }

        // Register NoteRemoval operators (subtractive: thinning, pulling, sparsifying)
        private static void RegisterNoteRemovalOperators(BassOperatorRegistry registry)
        {
            //registry.RegisterOperator(new HatThinningOperator());
            //registry.RegisterOperator(new KickPullOperator());
            //registry.RegisterOperator(new SparseGrooveOperator());
        }
    }
}
