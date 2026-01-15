namespace Music.Writer
{
    /// <summary>
    /// Tracks the duration used in each measure per part and staff.
    /// Internally uses a composite key format: "PartName|Staff|Measure"
    /// </summary>
    public sealed class MeasureMeta
    {
        private readonly Dictionary<string, long> _data;

        public MeasureMeta()
        {
            _data = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the total duration used in a specific measure for a part and staff.
        /// Returns 0 if no data exists for the given key.
        /// </summary>
        /// <param name="partName">The name of the part</param>
        /// <param name="staff">The staff number</param>
        /// <param name="measureNumber">The measure number</param>
        /// <returns>The total duration used in divisions</returns>
        public long GetDivisionsUsed(string partName, int staff, int measureNumber)
        {
            var key = CreateKey(partName, staff, measureNumber);
            return _data.TryGetValue(key, out var value) ? value : 0;
        }

        /// <summary>
        /// Sets the total duration used in a specific measure for a part and staff.
        /// </summary>
        /// <param name="partName">The name of the part</param>
        /// <param name="staff">The staff number</param>
        /// <param name="measureNumber">The measure number</param>
        /// <param name="duration">The duration to set</param>
        public void SetDivisionsUsed(string partName, int staff, int measureNumber, long duration)
        {
            var key = CreateKey(partName, staff, measureNumber);
            _data[key] = duration;
        }

        /// <summary>
        /// Adds to the duration used in a specific measure for a part and staff.
        /// If no data exists, initializes to 0 before adding.
        /// </summary>
        /// <param name="partName">The name of the part</param>
        /// <param name="staff">The staff number</param>
        /// <param name="measureNumber">The measure number</param>
        /// <param name="duration">The duration to add</param>
        public void AddDivisionsUsed(string partName, int staff, int measureNumber, long duration)
        {
            var key = CreateKey(partName, staff, measureNumber);
            if (!_data.ContainsKey(key))
                _data[key] = 0;
            _data[key] += duration;
        }

        /// <summary>
        /// Gets all entries that match the specified part name and staff number.
        /// Used for processing backup elements.
        /// </summary>
        /// <param name="partName">The name of the part</param>
        /// <param name="staff">The staff number</param>
        /// <returns>List of tuples containing (measureNumber, duration)</returns>
        public List<(int measureNumber, long duration)> GetDivisionsUsedForPartAndStaff(string partName, int staff)
        {
            var prefix = $"{partName}|{staff}|";
            var results = new List<(int measureNumber, long duration)>();

            foreach (var kvp in _data.Where(kvp => kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                var parts = kvp.Key.Split('|');
                if (parts.Length == 3 && int.TryParse(parts[2], out int measureNumber))
                {
                    results.Add((measureNumber, kvp.Value));
                }
            }

            return results;
        }

        /// <summary>
        /// Clears all data from the tracking dictionary.
        /// </summary>
        public void Clear()
        {
            _data.Clear();
        }

        /// <summary>
        /// Creates a composite key from the part name, staff number, and measure number.
        /// Format: "PartName|Staff|Measure"
        /// </summary>
        private static string CreateKey(string partName, int staff, int measureNumber)
        {
            return $"{partName}|{staff}|{measureNumber}";
        }
    }
}
