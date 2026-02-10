// AI: purpose=Registry for drum operators; discovery/filtering by family/style for DrummerOperatorCandidates.
// AI: invariants=Operators registered once; deterministic registration order; reads are thread-safe after Freeze.
// AI: deps=DrumOperatorBase, OperatorFamily, StyleConfiguration; used by DrummerOperatorCandidates to source candidates.

using Music.Generator.Core;
using Music.Generator.Drums.Operators.Base;

namespace Music.Generator.Drums.Operators
{
    // AI: purpose=Holds registered DrumOperatorBase instances; supports queries by family, id, and style allow lists.
    // AI: invariants=RegisterOperator must be called before Freeze(); after Freeze registry is immutable.
    public sealed class DrumOperatorRegistry
    {
        private readonly List<DrumOperatorBase> _operators = new();
        private readonly Dictionary<string, DrumOperatorBase> _operatorById = new();
        private readonly Dictionary<OperatorFamily, List<DrumOperatorBase>> _operatorsByFamily = new();
        private bool _frozen;

        // Register an operator. Throws InvalidOperationException if registry is frozen or duplicate ID.
        // AI: errors=throws InvalidOperationException when frozen or duplicate operatorId.
        public void RegisterOperator(DrumOperatorBase op)
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
                familyList = new List<DrumOperatorBase>();
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
        public IReadOnlyList<DrumOperatorBase> GetAllOperators() => _operators;

        // Get operators for a given family. Returns empty array when none found.
        public IReadOnlyList<DrumOperatorBase> GetOperatorsByFamily(OperatorFamily family)
        {
            return _operatorsByFamily.TryGetValue(family, out var list)
                ? list
                : Array.Empty<DrumOperatorBase>();
        }

        // Lookup operator by ID. Returns null when not found. ArgumentNullException on null operatorId.
        public DrumOperatorBase? GetOperatorById(string operatorId)
        {
            ArgumentNullException.ThrowIfNull(operatorId);
            return _operatorById.TryGetValue(operatorId, out var op) ? op : null;
        }

        // Get operators enabled by explicit allow list. Null or empty list => all operators allowed.
        public IReadOnlyList<DrumOperatorBase> GetEnabledOperators(IReadOnlyList<string>? allowList)
        {
            if (allowList is null || allowList.Count == 0)
                return _operators;

            var allowSet = new HashSet<string>(allowList);
            var enabled = new List<DrumOperatorBase>();
            foreach (var op in _operators)
            {
                if (allowSet.Contains(op.OperatorId))
                    enabled.Add(op);
            }
            return enabled;
        }

        // THIS CAN REMOVE
        // Create an empty registry
        public static DrumOperatorRegistry CreateEmpty() => new();

        /// <summary>
        /// Total number of registered operators.
        /// </summary>
        public int Count => _operators.Count;
    }
}
