// AI: purpose=DrummerAgent facade class; unifies candidate source and registry for drum generation.
// AI: invariants=Implements IDrumCandidateSource via delegation; deterministic output for same inputs.
// AI: deps=DrummerCandidateSource, DrumOperatorRegistry, StyleConfiguration.
// AI: change=Story 8.1, 4.2; facade pattern enables integration with Generator.cs and future testing.

using Music.Generator.Agents.Common;
using Music.Generator.Agents.Drums.Diagnostics;
using Music.MyMidi;

namespace Music.Generator.Agents.Drums
{
    /// <summary>
    /// Settings for DrummerAgent behavior.
    /// </summary>
    public sealed record DrummerAgentSettings
    {
        /// <summary>Whether to enable diagnostics collection (default: false for zero-cost).</summary>
        public bool EnableDiagnostics { get; init; } = false;

        /// <summary>Candidate source settings for error handling.</summary>
        public DrummerCandidateSourceSettings? CandidateSourceSettings { get; init; }

        /// <summary>Default settings instance.</summary>
        public static DrummerAgentSettings Default => new();
    }

    /// <summary>
    /// Data source for drum generation. Does NOT generate PartTracks directly. Use DrumGenerator pipeline.
    /// Implements IDrumCandidateSource to hook into the groove system.
    /// Story 4.2: Updated to use Drum interfaces.
    /// </summary>
    /// <remarks>
    /// <para>This class owns and manages the lifecycle of:</para>
    /// <list type="bullet">
    ///   <item>DrumOperatorRegistry (built from DrumOperatorRegistryBuilder)</item>
    ///   <item>DrummerCandidateSource (delegates IDrumCandidateSource)</item>
    /// </list>
    /// </remarks>
    public sealed class DrummerAgent : IDrumCandidateSource
    {
        private readonly StyleConfiguration _styleConfig;
        private readonly DrumOperatorRegistry _registry;
        private readonly DrummerCandidateSource _candidateSource;
        private readonly DrummerAgentSettings _settings;

        /// <summary>
        /// Creates a DrummerAgent with the specified style configuration.
        /// </summary>
        /// <param name="styleConfig">Style configuration (PopRock, Jazz, Metal, etc.).</param>
        /// <param name="settings">Optional agent settings (diagnostics, candidate source).</param>
        /// <exception cref="ArgumentNullException">If styleConfig is null.</exception>
        public DrummerAgent(
            StyleConfiguration styleConfig,
            DrummerAgentSettings? settings = null)
        {
            ArgumentNullException.ThrowIfNull(styleConfig);

            _styleConfig = styleConfig;
            _settings = settings ?? DrummerAgentSettings.Default;

            // AI: disconnect=Policy/Physicality; use operator registry + candidate source only.

            // Build operator registry (internally builds and freezes all 28 operators)
            _registry = DrumOperatorRegistryBuilder.BuildComplete();

            // Create candidate source (delegates IGrooveCandidateSource)
            _candidateSource = new DrummerCandidateSource(
                _registry,
                styleConfig,
                diagnosticsCollector: null,
                _settings.CandidateSourceSettings);
        }

        /// <summary>
        /// Gets the style configuration used by this agent.
        /// </summary>
        public StyleConfiguration StyleConfiguration => _styleConfig;

        /// <summary>
        /// Gets the operator registry (for diagnostics/inspection).
        /// </summary>
        public DrumOperatorRegistry Registry => _registry;

        #region IDrumCandidateSource Implementation

        /// <inheritdoc />
        public IReadOnlyList<DrumCandidateGroup> GetCandidateGroups(
            DrumBarContext barContext,
            string role)
        {
            return _candidateSource.GetCandidateGroups(barContext, role);
        }

        #endregion

    }
}
