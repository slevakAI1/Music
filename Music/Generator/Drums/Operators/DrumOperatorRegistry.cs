// AI: purpose=Registry for drum operators; discovery/filtering by family/style for DrummerOperatorCandidates.
// AI: invariants=Operators registered once; deterministic registration order; reads are thread-safe after Freeze.
// AI: deps=IDrumOperator, OperatorFamily, StyleConfiguration; used by DrummerOperatorCandidates to source candidates.

using Music.Generator.Core;

namespace Music.Generator.Drums.Operators
{
    // AI: purpose=Holds registered IDrumOperator instances; supports queries by family, id, and style allow lists.
    // AI: invariants=RegisterOperator must be called before Freeze(); after Freeze registry is immutable.
    public sealed class DrumOperatorRegistry
    {
        private readonly List<IDrumOperator> _operators = new();
        private readonly Dictionary<string, IDrumOperator> _operatorById = new();
        private readonly Dictionary<OperatorFamily, List<IDrumOperator>> _operatorsByFamily = new();
        private bool _frozen;

        // Register an operator. Throws InvalidOperationException if registry is frozen or duplicate ID.
        // AI: errors=throws InvalidOperationException when frozen or duplicate operatorId.
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

        // Freeze registry to prevent further registrations; call after startup registration completes.
        public void Freeze()
        {
            _frozen = true;
        }

        // Return all registered operators in deterministic registration order.
        public IReadOnlyList<IDrumOperator> GetAllOperators() => _operators;

        // Get operators for a given family. Returns empty array when none found.
        public IReadOnlyList<IDrumOperator> GetOperatorsByFamily(OperatorFamily family)
        {
            return _operatorsByFamily.TryGetValue(family, out var list)
                ? list
                : Array.Empty<IDrumOperator>();
        }

        // Lookup operator by ID. Returns null when not found. ArgumentNullException on null operatorId.
        public IDrumOperator? GetOperatorById(string operatorId)
        {
            ArgumentNullException.ThrowIfNull(operatorId);
            return _operatorById.TryGetValue(operatorId, out var op) ? op : null;
        }

        // Get operators enabled by explicit allow list. Null or empty list => all operators allowed.
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

        // THIS CAN REMOVE
        // Create an empty registry. Call RegisterOperator then Freeze during startup.
        public static DrumOperatorRegistry CreateEmpty() => new();

        /// <summary>
        /// Total number of registered operators.
        /// </summary>
        public int Count => _operators.Count;
    }
}
