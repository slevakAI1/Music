// AI: purpose=Collect and map drum operator candidates into grouped DrumCandidateGroups for selection.
// AI: invariants=Deterministic given same context+seed; operators invoked in registry order; groups non-empty implies candidates.
// AI: deps=Uses DrumOperatorRegistry, DrummerContextBuilder, DrumCandidateMapper; affects selection pipeline.

using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Selection.Candidates
{
    // AI: config=Settings for operator execution behavior; ContinueOnOperatorError controls exception policy
    public sealed record DrummerOperatorCandidatesSettings
    {
        public bool ContinueOnOperatorError { get; init; } = true;
        public bool VerboseDiagnostics { get; init; } = false;
        public static DrummerOperatorCandidatesSettings Default => new();
    }

    // AI: diag=Per-operator execution diagnostic: OperatorId, Family, CandidatesGenerated, ErrorMessage, WasSkipped
    public sealed record OperatorExecutionDiagnostic
    {
        public required string OperatorId { get; init; }
        public required OperatorFamily Family { get; init; }
        public required int CandidatesGenerated { get; init; }
        public string? ErrorMessage { get; init; }
        public bool WasSkipped { get; init; }
    }

    // AI: purpose=IDrumOperatorCandidates implementation for drummer; pipeline: Operators->DrumCandidates->Map->Group
    // AI: invariants=Deterministic given same inputs; errors isolated per-operator; groups sorted by family for determinism
    public sealed class DrummerOperatorCandidates : IDrumOperatorCandidates
    {
        private readonly DrumOperatorRegistry _registry;
        private readonly GrooveDiagnosticsCollector? _diagnosticsCollector;
        private readonly DrummerOperatorCandidatesSettings _settings;

        // Cache for last execution diagnostics (for testing/debugging)
        private List<OperatorExecutionDiagnostic>? _lastExecutionDiagnostics;

        // AI: ctor=Requires registry; diagnosticsCollector optional; settings optional with sensible defaults
        public DrummerOperatorCandidates(
            DrumOperatorRegistry registry,
            GrooveDiagnosticsCollector? diagnosticsCollector = null,
            DrummerOperatorCandidatesSettings? settings = null)
        {
            ArgumentNullException.ThrowIfNull(registry);

            _registry = registry;
            _diagnosticsCollector = diagnosticsCollector;
            _settings = settings ?? DrummerOperatorCandidatesSettings.Default;
        }

        // AI: entry=GetCandidateGroups builds context, runs enabled operators, maps and groups results
        public IReadOnlyList<DrumCandidateGroup> GetCandidateGroups(
            Bar bar,
            string role)
        {
            ArgumentNullException.ThrowIfNull(bar);
            ArgumentNullException.ThrowIfNull(role);

            _lastExecutionDiagnostics = new List<OperatorExecutionDiagnostic>();

            // Build DrummerContext from Bar
            var drummerContext = BuildDrummerContext(bar, role);

            // Get enabled operators (style + policy filtering)
            var enabledOperators = GetEnabledOperators(bar, role);

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

            // AI: disconnect=Physicality; skip playability filtering during phrase validation.

            return groups;
        }

        // AI: diagnostics=LastExecutionDiagnostics contains per-operator diagnostics from last invocation
        public IReadOnlyList<OperatorExecutionDiagnostic>? LastExecutionDiagnostics => _lastExecutionDiagnostics;

        // AI: behavior=Builds DrummerContext from Bar and per-bar seed; stateless builder call
        private DrummerContext BuildDrummerContext(Bar bar, string role)
        {
            var input = new DrummerContextBuildInput
            {
                Bar = bar,
                Seed = GetSeed(bar)
            };

            return DrummerContextBuilder.Build(input);
        }

        // AI: policy=Returns enabled operators; TODO: apply DrummerPolicyProvider allow-list in future
        private IReadOnlyList<IDrumOperator> GetEnabledOperators(Bar bar, string role)
        {
            // TODO: Apply policy allow list filtering from DrummerPolicyProvider
            // For now, return all registered operators
            return _registry.GetAllOperators();
        }

        // AI: behavior=Invokes each operator in order and aggregates validated DrumCandidates
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

        // AI: exec=Safely runs CanApply then GenerateCandidates; wraps exceptions into diagnostics per settings
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

        // AI: error=Converts operator exceptions into diagnostics; will rethrow if ContinueOnOperatorError is false
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

        // AI: validate=Per-candidate quick checks; invalid candidates are dropped (diagnostics TODO)
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

        // AI: maps=Uses DrumCandidateMapper to convert to DrumOnsetCandidate preserving hints and tags
        private static IReadOnlyList<DrumOnsetCandidate> MapCandidates(List<DrumCandidate> candidates)
        {
            return DrumCandidateMapper.MapAll(candidates);
        }

        // AI: grouping=Groups mapped candidates by operator family; sorts groups by family enum for determinism
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

        private static int GetSeed(Bar bar)
        {
            // Use bar number as component of seed for per-bar variation
            return 42 + bar.BarNumber;
        }

    }
}
