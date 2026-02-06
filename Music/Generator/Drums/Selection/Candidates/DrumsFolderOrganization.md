# Drums Folder Organization Map

## Agent (Main entry points and orchestration)
    DrummerAgent.cs
    DrummerMemory.cs

## Context (Bar and drummer context building)
    BarContext.cs
    DrumBarContext.cs
    DrumBarContextBuilder.cs
    DrummerContext.cs
    DrummerContextBuilder.cs

## Policy (Policy providers and decisions)
    IDrumPolicyProvider.cs
    DrummerPolicyProvider.cs
    DefaultDrumPolicyProvider.cs
    DrumPolicyDecision.cs
    RoleDensityTarget.cs

## Candidates (Candidate models, mapping, grouping)
    IDrumOperatorCandidates.cs
    DrummerOperatorCandidates.cs
    DrumCandidate.cs
    DrumCandidateGroup.cs
    DrumCandidateMapper.cs
    DrumOnsetCandidate.cs
    DrumGrooveOnsetFactory.cs

## Selection (Selection engines and weighted selection)
    DrumSelectionEngine.cs
    DrumWeightedCandidateSelector.cs
    DrumDensityCalculator.cs

## Generation (Track and phrase generation)
    DrumGenerator.cs
    DrumPhraseGenerator.cs
    DrumTrackGenerator.cs
    DrumPhraseEvolver.cs

## Planning (Phrase placement planning)
    DrumPhrasePlacementPlanner.cs
    DrumPhrasePlacementPlan.cs
    FillRole.cs

## Operators (Interface and registry - keep subfolders)
    IDrumOperator.cs
    DrumOperatorRegistry.cs
    DrumOperatorRegistryBuilder.cs

## Operators/Base
    DrumOperatorBase.cs

## Operators/MicroAddition
    FloorTomPickupOperator.cs
    GhostAfterBackbeatOperator.cs
    GhostBeforeBackbeatOperator.cs
    GhostClusterOperator.cs
    HatEmbellishmentOperator.cs
    KickDoubleOperator.cs
    KickPickupOperator.cs

## Operators/PatternSubstitution
    BackbeatVariantOperator.cs
    DoubleTimeFeelOperator.cs
    HalfTimeFeelOperator.cs
    KickPatternVariantOperator.cs

## Operators/PhrasePunctuation
    BuildFillOperator.cs
    CrashOnOneOperator.cs
    DropFillOperator.cs
    SetupHitOperator.cs
    StopTimeOperator.cs
    TurnaroundFillFullOperator.cs
    TurnaroundFillShortOperator.cs

## Operators/StyleIdiom
    BridgeBreakdownOperator.cs
    PopChorusCrashPatternOperator.cs
    PopRockBackbeatPushOperator.cs
    RockKickSyncopationOperator.cs
    VerseSimplifyOperator.cs

## Operators/SubdivisionTransform
    HatDropOperator.cs
    HatLiftOperator.cs
    OpenHatAccentOperator.cs
    PartialLiftOperator.cs
    RideSwapOperator.cs

## Performance (Timing, velocity, articulation shaping)
    DrumArticulation.cs
    DrumArticulationMapper.cs
    DrummerTimingHintSettings.cs
    DrummerTimingShaper.cs
    DrummerVelocityHintSettings.cs
    DrummerVelocityShaper.cs
    DynamicIntent.cs
    TimingIntent.cs

## Physicality (Physical playability constraints)
    LimbAssignment.cs
    LimbConflictDetector.cs
    LimbModel.cs
    PhysicalityFilter.cs
    PhysicalityRules.cs
    StickingRules.cs
    StickingValidation.cs

## Diagnostics (Analysis and feature extraction)
    DrummerDiagnostics.cs
    DrummerDiagnosticsCollector.cs
    DrumMidiEvent.cs
    DrumRoleMapper.cs
    DrumTrackEventExtractor.cs

## Diagnostics/Features (Feature data models and builders)
    DrumTrackFeatureData.cs
    DrumTrackFeatureDataBuilder.cs
    DrumTrackExtendedFeatureData.cs
    DrumTrackExtendedFeatureDataBuilder.cs
    DrumFeatureDataSerializer.cs
    DrumExtendedFeatureDataSerializer.cs

## Diagnostics/BarAnalysis (Per-bar analysis)
    BarOnsetStats.cs
    BarOnsetStatsExtractor.cs
    BarPatternExtractor.cs
    BarPatternFingerprint.cs
    BeatPositionMatrix.cs
    BeatPositionMatrixBuilder.cs

## Diagnostics/Patterns (Pattern detection and similarity)
    PatternRepetitionData.cs
    PatternRepetitionDetector.cs
    PatternSimilarityAnalyzer.cs
    PatternSimilarityData.cs
    SequencePatternData.cs
    SequencePatternDetector.cs

## Diagnostics/Coordination (Cross-role and structural analysis)
    AnchorCandidateData.cs
    AnchorCandidateExtractor.cs
    CrossRoleCoordinationData.cs
    CrossRoleCoordinationExtractor.cs
    StructuralMarkerData.cs
    StructuralMarkerDetector.cs

## Diagnostics/Dynamics (Timing and velocity analysis)
    TimingFeelData.cs
    TimingFeelExtractor.cs
    VelocityDynamicsData.cs
    VelocityDynamicsExtractor.cs
