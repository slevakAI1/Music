// AI: purpose=Builder for BassOperatorRegistry; registers operators in deterministic order for startup/tests.
// AI: invariants=Call RegisterAllOperators before Freeze; registration order is stable and reproducible.
// AI: deps=BassOperatorRegistry; no operators registered yet (empty registry expected).

using Music.Generator.Bass.Operators.FoundationVariation;
using Music.Generator.Bass.Operators.HarmonicTargeting;
using Music.Generator.Bass.Operators.DensityAndSubdivision;
//using Music.Generator.Bass.Operators.RegisterAndContour;
using Music.Generator.Bass.Operators.RhythmicPlacement;
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
            RegisterHarmonicTargetingOperators(registry);
            RegisterRhythmicPlacementOperators(registry);
            RegisterDensityAndSubdivisionOperators(registry);
            RegisterFoundationVariationOperators(registry);
            //RegisterRegisterAndContourOperators(registry);
        }

        // Register HarmonicTargeting operators (chord-change targeting, approaches)
        private static void RegisterHarmonicTargetingOperators(BassOperatorRegistry registry)
        {
            registry.RegisterOperator(new BassTargetNextChordRootOperator());
            registry.RegisterOperator(new BassApproachNoteOperator());
            registry.RegisterOperator(new BassEnclosureOperator());
            registry.RegisterOperator(new BassGuideToneEmphasisOperator());
            registry.RegisterOperator(new BassStepwiseVoiceLeadingOperator());
        }

        // Register FoundationVariation operators (pedals, root/fifth, chord tone pulse)
        private static void RegisterFoundationVariationOperators(BassOperatorRegistry registry)
        {
            registry.RegisterOperator(new BassPedalRootBarOperator());
            registry.RegisterOperator(new BassRootFifthOstinatoOperator());
            registry.RegisterOperator(new BassChordTonePulseOperator());
            registry.RegisterOperator(new BassPedalWithTurnaroundOperator());
        }

        // Register RhythmicPlacement operators (syncopation, anticipations)
        private static void RegisterRhythmicPlacementOperators(BassOperatorRegistry registry)
        {
            registry.RegisterOperator(new BassAnticipateDownbeatOperator());
            registry.RegisterOperator(new BassSyncopationSwapOperator());
            registry.RegisterOperator(new BassKickLockOperator());
            registry.RegisterOperator(new BassRestStrategicSpaceOperator());
            registry.RegisterOperator(new BassPickupIntoNextBarOperator());
        }

        // Register DensityAndSubdivision operators (splits, passing notes)
        private static void RegisterDensityAndSubdivisionOperators(BassOperatorRegistry registry)
        {
            registry.RegisterOperator(new BassSplitLongNoteOperator());
            registry.RegisterOperator(new BassAddPassingEighthsOperator());
            registry.RegisterOperator(new BassReduceToQuarterNotesOperator());
        }

        // Register RegisterAndContour operators (range, octave contour)
        private static void RegisterRegisterAndContourOperators(BassOperatorRegistry registry)
        {
        }
    }
}
