// AI: purpose=Registry for bass operators; discovery/filtering by family for BassPhraseGenerator pipeline.
// AI: invariants=RegisterOperator before Freeze; after Freeze registry is immutable and safe for read-only use.
// AI: deps=OperatorBase, OperatorFamily; no bass operators registered yet (empty registry expected).

using Music.Generator.Core;

namespace Music.Generator.Bass.Operators
{
    // AI: purpose=Holds registered OperatorBase instances; supports queries by family, id, and allow lists.
    // AI: invariants=Duplicate OperatorId not allowed; RegisterOperator invalid after Freeze.
    public sealed class BassOperatorRegistry
    {
        private readonly List<OperatorBase> _operators = new();
        private readonly Dictionary<string, OperatorBase> _operatorById = new();
        private readonly Dictionary<OperatorFamily, List<OperatorBase>> _operatorsByFamily = new();
        private bool _frozen;

        // AI: errors=throws InvalidOperationException when frozen or duplicate OperatorId.
        public void RegisterOperator(OperatorBase op)
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
                familyList = new List<OperatorBase>();
                _operatorsByFamily[op.OperatorFamily] = familyList;
            }
            familyList.Add(op);
        }

        public void Freeze()
        {
            _frozen = true;
        }

        public IReadOnlyList<OperatorBase> GetAllOperators() => _operators;

        public IReadOnlyList<OperatorBase> GetOperatorsByFamily(OperatorFamily family)
        {
            return _operatorsByFamily.TryGetValue(family, out var list)
                ? list
                : Array.Empty<OperatorBase>();
        }

        public OperatorBase? GetOperatorById(string operatorId)
        {
            ArgumentNullException.ThrowIfNull(operatorId);
            return _operatorById.TryGetValue(operatorId, out var op) ? op : null;
        }

        public IReadOnlyList<OperatorBase> GetEnabledOperators(IReadOnlyList<string>? allowList)
        {
            if (allowList is null || allowList.Count == 0)
                return _operators;

            var allowSet = new HashSet<string>(allowList);
            var enabled = new List<OperatorBase>();
            foreach (var op in _operators)
            {
                if (allowSet.Contains(op.OperatorId))
                    enabled.Add(op);
            }
            return enabled;
        }

        public int Count => _operators.Count;
    }
}
