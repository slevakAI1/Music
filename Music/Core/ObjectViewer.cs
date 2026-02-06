using System.Collections;
using System.Reflection;
using System.Text.Json;

// AI: purpose=debug-only safe serializer; deps=reflection,System.Text.Json; perf=debug-only; not for production hot paths
// AI: invariants=visited uses reference equality; cycles => "<cyclic reference>"; depth cutoff => "<MaxDepth N reached>"; security=may expose secrets

namespace Music
{
    public static class ObjectViewer
    {
        // AI: Json: convenience wrapper using default maxDepth=6; returns indented JSON
        public static string Json<T>(T obj) => Show(obj, maxDepth: 6);

        private static string Show<T>(T obj, int maxDepth)
        {
            var visited = new HashSet<object>(new ReferenceEqualityComparer());
            var safe = CreateSafeObject(obj, 0, maxDepth, visited);
            return JsonSerializer.Serialize(safe, new JsonSerializerOptions { WriteIndented = true });
        }

        // AI: CreateSafeObject: terminals=primitives,string,decimal,DateTime,enum,Guid,TimeSpan; inspects public props/fields; prevents cycles
        private static object? CreateSafeObject(object? obj, int depth, int maxDepth, HashSet<object> visited)
        {
            if (obj == null)
                return null;

            if (depth > maxDepth)
                return $"<MaxDepth {maxDepth} reached>";

            var type = obj.GetType();

            // Treat primitives, strings, decimals, DateTime, enums as terminal
            if (type.IsPrimitive || obj is string || obj is decimal || obj is DateTime || type.IsEnum || obj is Guid || obj is TimeSpan)
                return obj;

            // Avoid repeated work / infinite recursion
            if (visited.Contains(obj))
                return "<cyclic reference>";

            // Mark visited for reference types
            if (!type.IsValueType)
                visited.Add(obj);

            // IDictionary handling
            if (obj is IDictionary dict)
            {
                var d = new Dictionary<string, object?>();
                foreach (DictionaryEntry de in dict)
                {
                    var key = de.Key?.ToString() ?? "<null>";
                    object? value;
                    try
                    {
                        value = CreateSafeObject(de.Value, depth + 1, maxDepth, visited);
                    }
                    catch (Exception ex)
                    {
                        value = $"<error reading dictionary value: {ex.GetType().Name}: {ex.Message}>";
                    }
                    d[key] = value;
                }
                return d;
            }

            // IEnumerable handling (but not string which is handled above)
            if (obj is IEnumerable enumerable)
            {
                var list = new List<object?>();
                foreach (var item in enumerable)
                {
                    try
                    {
                        list.Add(CreateSafeObject(item, depth + 1, maxDepth, visited));
                    }
                    catch (Exception ex)
                    {
                        list.Add($"<error reading collection item: {ex.GetType().Name}: {ex.Message}>");
                    }
                }
                return list;
            }

            // Complex object: read public properties and fields, catching getter exceptions
            var result = new Dictionary<string, object?>();
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => p.GetIndexParameters().Length == 0);
            foreach (var prop in props)
            {
                object? rawValue = null;
                try
                {
                    rawValue = prop.GetValue(obj);
                }
                catch (Exception ex)
                {
                    rawValue = $"<property getter threw: {ex.GetType().Name}: {ex.Message}>";
                }

                try
                {
                    result[prop.Name] = CreateSafeObject(rawValue, depth + 1, maxDepth, visited);
                }
                catch (Exception ex)
                {
                    result[prop.Name] = $"<error processing property: {ex.GetType().Name}: {ex.Message}>";
                }
            }

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                object? rawValue = null;
                try
                {
                    rawValue = field.GetValue(obj);
                }
                catch (Exception ex)
                {
                    rawValue = $"<field getter threw: {ex.GetType().Name}: {ex.Message}>";
                }

                try
                {
                    result[field.Name] = CreateSafeObject(rawValue, depth + 1, maxDepth, visited);
                }
                catch (Exception ex)
                {
                    result[field.Name] = $"<error processing field: {ex.GetType().Name}: {ex.Message}>";
                }
            }

            return result;
        }

        // AI: ReferenceEqualityComparer: uses ReferenceEquals + RuntimeHelpers.GetHashCode to detect object identity cycles
        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
