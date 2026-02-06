// AI: purpose=Registry for drum operators; provides discovery and filtering by family/style for DrummerOperatorCandidates.
// AI: invariants=Operators registered once; GetAllOperators returns deterministic order; thread-safe reads.
// AI: deps=IDrumOperator, OperatorFamily, StyleConfiguration; consumed by DrummerOperatorCandidates.
// AI: change=Story 2.4 stub; full implementation in Story 3.6 when operators exist.


// AI: purpose=Registry for drum operators; provides discovery and filtering by family/style for DrummerOperatorCandidates.
// AI: invariants=Operators registered once; GetAllOperators returns deterministic order; thread-safe reads.
// AI: deps=IDrumOperator, OperatorFamily, StyleConfiguration; consumed by DrummerOperatorCandidates.
// AI: change=Story 2.4 stub; full implementation in Story 3.6 when operators exist.

using Music.Generator.Core;

namespace Music.Generator.Drums.Operators
{
    /// <summary>
    /// Registry for drum operators. Provides discovery and filtering by family, style, and operator ID.
    /// Story 2.4: Stub implementation for DrummerOperatorCandidates integration.
    /// Story 3.6: Full implementation with all 28 operators.
    /// </summary>
    /// <remarks>
    /// Operators are registered at startup and remain immutable during generation.
    /// All query methods return deterministic ordering for reproducibility.
    /// </remarks>
    public sealed class DrumOperatorRegistry
    {
        private readonly List<IDrumOperator> _operators = new();
        private readonly Dictionary<string, IDrumOperator> _operatorById = new();
        private readonly Dictionary<OperatorFamily, List<IDrumOperator>> _operatorsByFamily = new();
        private bool _frozen;

        /// <summary>
        /// Registers an operator. Must be called before any queries.
        /// </summary>
        /// <param name="op">Operator to register.</param>
        /// <exception cref="InvalidOperationException">If registry is frozen or operator ID is duplicate.</exception>
        public void RegisterOperator(IDrumOperator op)
        {
            ArgumentNullException.ThrowIfNull(op);

            if (_frozen)
                throw new InvalidOperationException("Cannot register operators after registry is frozen.");

            if (_operatorById.ContainsKey(op.OperatorId))
                throw new InvalidOperationException($"Duplicate operator ID: {op.OperatorId}");

            _operators.Add(op);
            _operatorById[op.OperatorId] = op;

            if (!_operatorsByFamily.TryGetValue(op.OperatorFamily, out var familyList))
            {
                familyList = new List<IDrumOperator>();
                _operatorsByFamily[op.OperatorFamily] = familyList;
            }
            familyList.Add(op);
        }

        /// <summary>
        /// Freezes the registry, preventing further registrations.
        /// Called after all operators are registered.
        /// </summary>
        public void Freeze()
        {
            _frozen = true;
        }

        /// <summary>
        /// Gets all registered operators in deterministic order (registration order).
        /// </summary>
        public IReadOnlyList<IDrumOperator> GetAllOperators() => _operators;

        /// <summary>
        /// Gets operators belonging to a specific family.
        /// </summary>
        /// <param name="family">Operator family to filter by.</param>
        /// <returns>Operators in the family, or empty list if none.</returns>
        public IReadOnlyList<IDrumOperator> GetOperatorsByFamily(OperatorFamily family)
        {
            return _operatorsByFamily.TryGetValue(family, out var list)
                ? list
                : Array.Empty<IDrumOperator>();
        }

        /// <summary>
        /// Gets a specific operator by ID.
        /// </summary>
        /// <param name="operatorId">Operator ID to look up.</param>
        /// <returns>Operator if found, null otherwise.</returns>
        public IDrumOperator? GetOperatorById(string operatorId)
        {
            ArgumentNullException.ThrowIfNull(operatorId);
            return _operatorById.TryGetValue(operatorId, out var op) ? op : null;
        }

        /// <summary>
        /// Gets operators enabled for a specific style configuration.
        /// Filters by style's AllowedOperatorIds (empty = all allowed).
        /// </summary>
        /// <param name="style">Style configuration with allowed operator list.</param>
        /// <returns>Operators allowed in the style, in deterministic order.</returns>
        public IReadOnlyList<IDrumOperator> GetEnabledOperators(StyleConfiguration style)
        {
            ArgumentNullException.ThrowIfNull(style);

            // Empty AllowedOperatorIds means all operators are allowed
            if (style.AllowedOperatorIds.Count == 0)
                return _operators;

            var enabled = new List<IDrumOperator>();
            foreach (var op in _operators)
            {
                if (style.IsOperatorAllowed(op.OperatorId))
                    enabled.Add(op);
            }
            return enabled;
        }

        /// <summary>
        /// Gets operators enabled by policy allow list.
        /// </summary>
        /// <param name="allowList">List of allowed operator IDs. Null = all allowed.</param>
        /// <returns>Filtered operators in deterministic order.</returns>
        public IReadOnlyList<IDrumOperator> GetEnabledOperators(IReadOnlyList<string>? allowList)
        {
            if (allowList is null || allowList.Count == 0)
                return _operators;

            var allowSet = new HashSet<string>(allowList);
            var enabled = new List<IDrumOperator>();
            foreach (var op in _operators)
            {
                if (allowSet.Contains(op.OperatorId))
                    enabled.Add(op);
            }
            return enabled;
        }

        /// <summary>
        /// Creates an empty registry. Call RegisterOperator to add operators, then Freeze.
        /// </summary>
        public static DrumOperatorRegistry CreateEmpty() => new();

        /// <summary>
        /// Total number of registered operators.
        /// </summary>
        public int Count => _operators.Count;
    }
}
