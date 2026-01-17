// AI: purpose=Generic protection applier for generators; applies RoleProtectionSet semantics to any event type.
// AI: invariants=Must preserve exact protection semantics (must-hit, never-add, never-remove, protected) and dedupe behavior.
// AI: deps=Uses delegates to adapt event shape: getBar,getRole,getBeat,setFlags,createEvent; deterministic ordering preserved.
// AI: change=Safe refactor: moves drum-specific protection logic into reusable service; behavior must remain identical.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Music.Generator
{
    public static class ProtectionApplier
    {
        /// <summary>
        /// Applies merged protections to a heterogeneous event list.
        /// </summary>
        /// <typeparam name="TEvent">Consumer-defined event type (immutable or mutable). All mutations must be via provided delegates.</typeparam>
        /// <param name="events">Initial event pool (may be null/empty).</param>
        /// <param name="mergedProtectionsPerBar">Dictionary bar -> roleName -> RoleProtectionSet.</param>
        /// <param name="getBar">Extract bar number from event.</param>
        /// <param name="getRoleName">Extract role identifier (string) from event.</param>
        /// <param name="getBeat">Extract beat position (decimal) from event.</param>
        /// <param name="setFlags">Return event with flags applied (isMustHit,isNeverRemove,isProtected).</param>
        /// <param name="createEvent">Create a new event for missing MustHit onsets (bar, roleName, beat).</param>
        /// <returns>Deduplicated list ordered by bar then beat (stable deterministic).</returns>
        public static List<TEvent> Apply<TEvent>(
            List<TEvent>? events,
            Dictionary<int, Dictionary<string, RoleProtectionSet>> mergedProtectionsPerBar,
            Func<TEvent, int> getBar,
            Func<TEvent, string> getRoleName,
            Func<TEvent, decimal> getBeat,
            Func<TEvent, bool, bool, bool, TEvent> setFlags,
            Func<int, string, decimal, TEvent> createEvent)
        {
            var result = new List<TEvent>();

            // Group existing events by bar for efficient lookup.
            var eventsByBar = (events ?? new List<TEvent>())
                .GroupBy(getBar)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Process each bar that has protections.
            foreach (var kv in mergedProtectionsPerBar)
            {
                int bar = kv.Key;
                var protectionsByRole = kv.Value;

                var barEvents = eventsByBar.TryGetValue(bar, out var existing) ? new List<TEvent>(existing) : new List<TEvent>();

                // For each role with protections in this bar, apply rules.
                foreach (var roleKvp in protectionsByRole)
                {
                    string roleName = roleKvp.Key;
                    var protectionSet = roleKvp.Value;

                    // 1) Remove events that match NeverAddOnsets.
                    if (protectionSet.NeverAddOnsets != null && protectionSet.NeverAddOnsets.Count > 0)
                    {
                        barEvents.RemoveAll(e =>
                        {
                            var r = getRoleName(e);
                            var b = getBeat(e);
                            return string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase)
                                && protectionSet.NeverAddOnsets.Contains(b);
                        });
                    }

                    // 2) Mark existing events as NeverRemove / Protected where applicable.
                    for (int i = 0; i < barEvents.Count; i++)
                    {
                        var evt = barEvents[i];
                        var r = getRoleName(evt);
                        if (!string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase))
                            continue;

                        bool isNeverRemove = protectionSet.NeverRemoveOnsets != null && protectionSet.NeverRemoveOnsets.Contains(getBeat(evt));
                        bool isProtected = protectionSet.ProtectedOnsets != null && protectionSet.ProtectedOnsets.Contains(getBeat(evt));

                        // setFlags returns an updated event (or the same mutated instance).
                        barEvents[i] = setFlags(evt, false, isNeverRemove, isProtected);
                    }

                    // 3) Ensure MustHitOnsets exist.
                    if (protectionSet.MustHitOnsets != null)
                    {
                        foreach (var mustBeat in protectionSet.MustHitOnsets)
                        {
                            bool exists = barEvents.Any(o =>
                                string.Equals(getRoleName(o), roleName, StringComparison.OrdinalIgnoreCase)
                                && getBeat(o) == mustBeat);

                            if (!exists)
                            {
                                var newEvt = createEvent(bar, roleName, mustBeat);
                                newEvt = setFlags(newEvt,
                                    true,
                                    protectionSet.NeverRemoveOnsets?.Contains(mustBeat) ?? false,
                                    protectionSet.ProtectedOnsets?.Contains(mustBeat) ?? false);
                                barEvents.Add(newEvt);
                            }
                        }
                    }
                }

                result.AddRange(barEvents);
            }

            // Add events from bars without protections unchanged.
            var barsWithProtections = new HashSet<int>(mergedProtectionsPerBar.Keys);
            foreach (var kv in eventsByBar)
            {
                if (!barsWithProtections.Contains(kv.Key))
                    result.AddRange(kv.Value);
            }

            // Deduplicate by bar|role|beat and order deterministically by bar then beat then role string.
            var deduped = new List<TEvent>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var evt in result.OrderBy(e => getBar(e)).ThenBy(e => getBeat(e)).ThenBy(e => getRoleName(e)))
            {
                string key = $"{getBar(evt)}|{getRoleName(evt)}|{getBeat(evt)}";
                if (seen.Add(key))
                    deduped.Add(evt);
            }

            return deduped;
        }
    }
}
