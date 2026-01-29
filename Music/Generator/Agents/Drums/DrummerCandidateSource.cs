// AI: purpose=Implements IDrumCandidateSource for drummer agent; gathers operator candidates, maps, groups, filters.
// AI: invariants=Deterministic: same context + seed → same groups; operators invoked in registry order; errors isolated.
// AI: deps=IDrumCandidateSource, DrumOperatorRegistry, DrumCandidateMapper, PhysicalityFilter, DrummerContextBuilder.
// AI: change=Story 2.4, 4.2; extend with diagnostics and additional filtering as physicality system matures.

using Music.Generator.Agents.Common;
using Music.Generator.Agents.Drums.Physicality;
using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums
{
    /// <summary>
    /// Configuration for DrummerCandidateSource behavior.
    /// </summary>
    public sealed record DrummerCandidateSourceSettings
    {
        /// <summary>Whether to continue generation when an operator throws.</summary>
        public bool ContinueOnOperatorError { get; init; } = true;

        /// <summary>Whether to collect verbose diagnostics (per-candidate detail).</summary>
        public bool VerboseDiagnostics { get; init; } = false;

        /// <summary>Default settings instance.</summary>
        public static DrummerCandidateSourceSettings Default => new();
    }

    /// <summary>
    /// Diagnostic entry for operator execution during candidate generation.
    /// </summary>
    public sealed record OperatorExecutionDiagnostic
    {
        /// <summary>Operator ID that was executed.</summary>
        public required string OperatorId { get; init; }

        /// <summary>Operator family.</summary>
        public required OperatorFamily Family { get; init; }

        /// <summary>Number of candidates generated.</summary>
        public required int CandidatesGenerated { get; init; }

        /// <summary>Error message if operator threw (null if successful).</summary>
        public string? ErrorMessage { get; init; }

        /// <summary>Whether operator was skipped (CanApply returned false).</summary>
        public bool WasSkipped { get; init; }
    }

    /// <summary>
    /// Drummer agent implementation of IDrumCandidateSource.
    /// Gathers candidates from registered operators, maps to DrumOnsetCandidate,
    /// groups by operator family, and applies physicality filtering.
    /// Story 2.4: Implement Drummer Candidate Source.
    /// Story 4.2: Moved interface ownership from Groove to Drums namespace.
    /// </summary>
    /// <remarks>
    /// Pipeline: Operators → DrumCandidates → Map → Group → PhysicalityFilter → DrumCandidateGroups
    /// All operations are deterministic given same context, seed, and registry state.
    /// </remarks>
    public sealed class DrummerCandidateSource : IDrumCandidateSource
    {
        private readonly DrumOperatorRegistry _registry;
        private readonly StyleConfiguration _styleConfig;
        private readonly IAgentMemory? _memory;
        private readonly PhysicalityFilter? _physicalityFilter;
        private readonly GrooveDiagnosticsCollector? _diagnosticsCollector;
        private readonly DrummerCandidateSourceSettings _settings;

        // Cache for last execution diagnostics (for testing/debugging)
        private List<OperatorExecutionDiagnostic>? _lastExecutionDiagnostics;

        /// <summary>
        /// Creates a DrummerCandidateSource with the specified dependencies.
        /// </summary>
        /// <param name="registry">Registry of drum operators.</param>
        /// <param name="styleConfig">Style configuration for operator filtering and weights.</param>
        /// <param name="memory">Optional agent memory for repetition penalties.</param>
        /// <param name="physicalityFilter">Optional physicality filter for playability validation.</param>
        /// <param name="diagnosticsCollector">Optional collector for structured diagnostics.</param>
        /// <param name="settings">Optional settings for error handling and diagnostics.</param>
        public DrummerCandidateSource(
            DrumOperatorRegistry registry,
            StyleConfiguration styleConfig,
            IAgentMemory? memory = null,
            PhysicalityFilter? physicalityFilter = null,
            GrooveDiagnosticsCollector? diagnosticsCollector = null,
            DrummerCandidateSourceSettings? settings = null)
        {
            ArgumentNullException.ThrowIfNull(registry);
            ArgumentNullException.ThrowIfNull(styleConfig);

            _registry = registry;
            _styleConfig = styleConfig;
            _memory = memory;
            _physicalityFilter = physicalityFilter;
            _diagnosticsCollector = diagnosticsCollector;
            _settings = settings ?? DrummerCandidateSourceSettings.Default;
        }

        /// <inheritdoc />
        public IReadOnlyList<DrumCandidateGroup> GetCandidateGroups(
            GrooveBarContext barContext,
            string role)
        {
            ArgumentNullException.ThrowIfNull(barContext);
            ArgumentNullException.ThrowIfNull(role);

            _lastExecutionDiagnostics = new List<OperatorExecutionDiagnostic>();

            // Build DrummerContext from GrooveBarContext
            var drummerContext = BuildDrummerContext(barContext, role);

            // Get enabled operators (style + policy filtering)
            var enabledOperators = GetEnabledOperators(barContext, role);

            if (enabledOperators.Count == 0)
            {
                return Array.Empty<DrumCandidateGroup>();
            }

            // Generate candidates from all enabled operators
            var allCandidates = GenerateCandidatesFromOperators(enabledOperators, drummerContext);

            if (allCandidates.Count == 0)
            {
                return Array.Empty<DrumCandidateGroup>();
            }

            // Map DrumCandidates to GrooveOnsetCandidates
            var mappedCandidates = MapCandidates(allCandidates);

            // Group by operator family
            var groups = GroupByOperatorFamily(allCandidates, mappedCandidates);

            // Apply physicality filter
            if (_physicalityFilter != null)
            {
                groups = ApplyPhysicalityFilter(groups, barContext.BarNumber);
            }

            return groups;
        }

        /// <summary>
        /// Gets execution diagnostics from the last GetCandidateGroups call.
        /// For testing and debugging purposes.
        /// </summary>
        public IReadOnlyList<OperatorExecutionDiagnostic>? LastExecutionDiagnostics => _lastExecutionDiagnostics;

        /// <summary>
        /// Builds DrummerContext from GrooveBarContext.
        /// </summary>
        private DrummerContext BuildDrummerContext(GrooveBarContext barContext, string role)
        {
            var input = new DrummerContextBuildInput
            {
                BarContext = barContext,
                Seed = GetSeed(barContext),
                EnergyLevel = GetEnergyLevel(barContext),
                BeatsPerBar = 4 // TODO: Extract from time signature when available
            };

            return DrummerContextBuilder.Build(input);
        }

        /// <summary>
        /// Gets enabled operators based on style and policy.
        /// </summary>
        private IReadOnlyList<IDrumOperator> GetEnabledOperators(GrooveBarContext barContext, string role)
        {
            // Start with style-enabled operators
            var styleEnabled = _registry.GetEnabledOperators(_styleConfig);

            // TODO: Apply policy allow list filtering from DrummerPolicyProvider
            // For now, return style-enabled operators
            return styleEnabled;
        }

        /// <summary>
        /// Generates candidates from all enabled operators.
        /// </summary>
        private List<DrumCandidate> GenerateCandidatesFromOperators(
            IReadOnlyList<IDrumOperator> operators,
            DrummerContext context)
        {
            var allCandidates = new List<DrumCandidate>();

            foreach (var op in operators)
            {
                var diagnostic = ExecuteOperator(op, context, allCandidates);
                _lastExecutionDiagnostics?.Add(diagnostic);
            }

            return allCandidates;
        }

        /// <summary>
        /// Executes a single operator and collects its candidates.
        /// </summary>
        private OperatorExecutionDiagnostic ExecuteOperator(
            IDrumOperator op,
            DrummerContext context,
            List<DrumCandidate> allCandidates)
        {
            // Check CanApply first
            bool canApply;
            try
            {
                canApply = op.CanApply(context);
            }
            catch (Exception ex)
            {
                return HandleOperatorError(op, ex, "CanApply");
            }

            if (!canApply)
            {
                return new OperatorExecutionDiagnostic
                {
                    OperatorId = op.OperatorId,
                    Family = op.OperatorFamily,
                    CandidatesGenerated = 0,
                    WasSkipped = true
                };
            }

            // Generate candidates
            try
            {
                var candidates = op.GenerateCandidates(context);
                int count = 0;

                foreach (var candidate in candidates)
                {
                    if (ValidateCandidate(candidate, op.OperatorId))
                    {
                        allCandidates.Add(candidate);
                        count++;
                    }
                }

                return new OperatorExecutionDiagnostic
                {
                    OperatorId = op.OperatorId,
                    Family = op.OperatorFamily,
                    CandidatesGenerated = count,
                    WasSkipped = false
                };
            }
            catch (Exception ex)
            {
                return HandleOperatorError(op, ex, "GenerateCandidates");
            }
        }

        /// <summary>
        /// Handles operator exceptions based on settings.
        /// </summary>
        private OperatorExecutionDiagnostic HandleOperatorError(
            IDrumOperator op,
            Exception ex,
            string method)
        {
            var diagnostic = new OperatorExecutionDiagnostic
            {
                OperatorId = op.OperatorId,
                Family = op.OperatorFamily,
                CandidatesGenerated = 0,
                ErrorMessage = $"{method}: {ex.Message}"
            };

            if (!_settings.ContinueOnOperatorError)
            {
                throw new InvalidOperationException(
                    $"Operator {op.OperatorId} failed during {method}: {ex.Message}", ex);
            }

            return diagnostic;
        }

        /// <summary>
        /// Validates a candidate has required fields and no obvious errors.
        /// </summary>
        private static bool ValidateCandidate(DrumCandidate candidate, string operatorId)
        {
            if (candidate == null)
                return false;

            if (!candidate.TryValidate(out string? error))
            {
                // Log validation error but don't throw
                // TODO: Add to diagnostics
                return false;
            }

            return true;
        }

        /// <summary>
        /// Maps DrumCandidates to GrooveOnsetCandidates.
        /// </summary>
        private static IReadOnlyList<DrumOnsetCandidate> MapCandidates(List<DrumCandidate> candidates)
        {
            return DrumCandidateMapper.MapAll(candidates);
        }

        /// <summary>
        /// Groups candidates by operator family.
        /// </summary>
        private List<DrumCandidateGroup> GroupByOperatorFamily(
            List<DrumCandidate> originalCandidates,
            IReadOnlyList<DrumOnsetCandidate> mappedCandidates)
        {
            // Build lookup from candidate ID to mapped candidate
            var mappedLookup = new Dictionary<string, DrumOnsetCandidate>();
            for (int i = 0; i < originalCandidates.Count; i++)
            {
                mappedLookup[originalCandidates[i].CandidateId] = mappedCandidates[i];
            }

            // Group by operator ID to family mapping
            var familyGroups = new Dictionary<OperatorFamily, List<DrumOnsetCandidate>>();
            var familyScores = new Dictionary<OperatorFamily, List<double>>();

            foreach (var original in originalCandidates)
            {
                var op = _registry.GetOperatorById(original.OperatorId);
                var family = op?.OperatorFamily ?? OperatorFamily.MicroAddition;

                if (!familyGroups.TryGetValue(family, out var list))
                {
                    list = new List<DrumOnsetCandidate>();
                    familyGroups[family] = list;
                    familyScores[family] = new List<double>();
                }

                list.Add(mappedLookup[original.CandidateId]);
                familyScores[family].Add(original.Score);
            }

            // Build groups sorted by family enum value for determinism
            var result = new List<DrumCandidateGroup>();
            foreach (var family in familyGroups.Keys.OrderBy(f => (int)f))
            {
                var candidates = familyGroups[family];
                var scores = familyScores[family];
                double avgScore = scores.Count > 0 ? scores.Average() : 0.5;

                result.Add(new DrumCandidateGroup
                {
                    GroupId = family.ToString(),
                    GroupTags = new List<string> { family.ToString() },
                    MaxAddsPerBar = candidates.Count, // Allow up to all candidates
                    BaseProbabilityBias = avgScore,
                    Candidates = candidates
                });
            }

            return result;
        }

        /// <summary>
        /// Applies physicality filter to remove unplayable candidates.
        /// </summary>
        private List<DrumCandidateGroup> ApplyPhysicalityFilter(
            List<DrumCandidateGroup> groups,
            int barNumber)
        {
            if (_physicalityFilter == null)
                return groups;

            // Collect all candidates for global filtering
            var allCandidates = new List<DrumOnsetCandidate>();
            foreach (var group in groups)
            {
                allCandidates.AddRange(group.Candidates);
            }

            // Filter candidates
            var validCandidates = _physicalityFilter.Filter(allCandidates, barNumber);
            var validSet = new HashSet<DrumOnsetCandidate>(validCandidates);

            // Rebuild groups with only valid candidates
            var result = new List<DrumCandidateGroup>();
            foreach (var group in groups)
            {
                var filtered = group.Candidates.Where(c => validSet.Contains(c)).ToList();
                result.Add(new DrumCandidateGroup
                {
                    GroupId = group.GroupId,
                    GroupTags = group.GroupTags,
                    MaxAddsPerBar = group.MaxAddsPerBar,
                    BaseProbabilityBias = group.BaseProbabilityBias,
                    Candidates = filtered
                });
            }

            return result;
        }

        /// <summary>
        /// Gets seed from bar context or defaults.
        /// </summary>
        private static int GetSeed(GrooveBarContext barContext)
        {
            // Use bar number as component of seed for per-bar variation
            return 42 + barContext.BarNumber;
        }

        /// <summary>
        /// Derives energy level from section type.
        /// </summary>
        private static double GetEnergyLevel(GrooveBarContext barContext)
        {
            var sectionType = barContext.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            return sectionType switch
            {
                MusicConstants.eSectionType.Chorus => 0.8,
                MusicConstants.eSectionType.Solo => 0.7,
                MusicConstants.eSectionType.Bridge => 0.4,
                MusicConstants.eSectionType.Intro => 0.4,
                MusicConstants.eSectionType.Outro => 0.5,
                _ => 0.5
            };
        }
    }
}
