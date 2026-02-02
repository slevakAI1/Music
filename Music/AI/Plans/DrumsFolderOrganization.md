# Drums Folder Organization Map

## Root (Main Agent & Orchestration)
    DrummerAgent.cs
    DrummerAgentSettings.cs
    DrumGenerator.cs
    DrumPhraseGenerator.cs

## Planning
    DrumPhrasePlacementPlanner.cs
    DrumPhrasePlacementPlan.cs
    DrumPhrasePlacement.cs
    DrumPhraseEvolver.cs
    DrumPhraseEvolutionParams.cs

## Context
    DrummerContext.cs
    DrummerContextBuilder.cs
    DrummerContextBuildInput.cs
    DrumBarContext.cs
    DrumBarContextBuilder.cs

## Policy
    DrummerPolicyProvider.cs
    DrummerPolicyProviderSettings.cs
    IDrumPolicyProvider.cs
    DrumPolicyDecision.cs

## CandidateSource
    DrummerCandidateSource.cs
    DrummerCandidateSourceSettings.cs
    IDrumCandidateSource.cs

## Candidates
    DrumCandidate.cs
    DrumCandidateGroup.cs
    DrumOnsetCandidate.cs
    DrumCandidateMapper.cs

## Operators (Parent Interface & Registry)
    IDrumOperator.cs
    DrumOperatorRegistry.cs
    DrumOperatorRegistryBuilder.cs
    OperatorExecutionDiagnostic.cs

## Operators/Base
    DrumOperatorBase.cs

## Operators/MicroAddition
    GhostBeforeBackbeatOperator.cs
    GhostAfterBackbeatOperator.cs
    KickPickupOperator.cs
    KickDoubleOperator.cs
    HatEmbellishmentOperator.cs
    GhostClusterOperator.cs
    FloorTomPickupOperator.cs

## Operators/SubdivisionTransform
    HatLiftOperator.cs
    HatDropOperator.cs
    RideSwapOperator.cs
    PartialLiftOperator.cs
    OpenHatAccentOperator.cs

## Operators/PhrasePunctuation
    CrashOnOneOperator.cs
    TurnaroundFillShortOperator.cs
    TurnaroundFillFullOperator.cs
    SetupHitOperator.cs
    StopTimeOperator.cs
    BuildFillOperator.cs
    DropFillOperator.cs

## Operators/PatternSubstitution
    BackbeatVariantOperator.cs
    KickPatternVariantOperator.cs
    HalfTimeFeelOperator.cs
    DoubleTimeFeelOperator.cs

## Operators/StyleIdiom
    PopRockBackbeatPushOperator.cs
    RockKickSyncopationOperator.cs
    PopChorusCrashPatternOperator.cs
    VerseSimplifyOperator.cs
    BridgeBreakdownOperator.cs

## Selection
    DrumSelectionEngine.cs
    DrumSelectionSettings.cs

## Memory
    DrummerMemory.cs
    IAgentMemory.cs

## Physicality
    PhysicalityFilter.cs
    PhysicalityRules.cs
    PhysicalityDiagnostic.cs

## Factories
    DrumGrooveOnsetFactory.cs

## Settings
    DrumGeneratorSettings.cs
