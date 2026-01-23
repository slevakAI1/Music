// AI: purpose=Unit tests for ProtectionApplier; validates generic protection enforcement logic.
// AI: coverage=Tests must-hit addition, never-add filtering, never-remove/protected flag setting, deduplication, bar mapping.

using Music.Generator;
using Music.Generator.Groove;
using Xunit;

namespace Music.Tests.Generator.Groove
{
    public class ProtectionApplierTests
    {
        // Test event type with all required fields
        private sealed record TestEvent(
            int Bar,
            string Role,
            decimal Beat,
            int Velocity)
        {
            public bool IsMustHit { get; set; }
            public bool IsNeverRemove { get; set; }
            public bool IsProtected { get; set; }
        }

        [Fact]
        public void Apply_NullEvents_ReturnsEmptyList()
        {
            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>();

            var result = ProtectionApplier.Apply(
                events: null,
                mergedProtectionsPerBar: protections,
                getBar: e => e.Bar,
                getRoleName: e => e.Role,
                getBeat: e => e.Beat,
                setFlags: (e, mustHit, neverRemove, prot) =>
                {
                    e.IsMustHit = mustHit;
                    e.IsNeverRemove = neverRemove;
                    e.IsProtected = prot;
                    return e;
                },
                createEvent: (bar, role, beat) => new TestEvent(bar, role, beat, 100));

            Assert.Empty(result);
        }

        [Fact]
        public void Apply_EmptyEvents_ReturnsEmptyList()
        {
            var events = new List<TestEvent>();
            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>();

            var result = ProtectionApplier.Apply(
                events: events,
                mergedProtectionsPerBar: protections,
                getBar: e => e.Bar,
                getRoleName: e => e.Role,
                getBeat: e => e.Beat,
                setFlags: (e, mustHit, neverRemove, prot) =>
                {
                    e.IsMustHit = mustHit;
                    e.IsNeverRemove = neverRemove;
                    e.IsProtected = prot;
                    return e;
                },
                createEvent: (bar, role, beat) => new TestEvent(bar, role, beat, 100));

            Assert.Empty(result);
        }

        [Fact]
        public void Apply_NoProtections_ReturnsEventsUnchanged()
        {
            var events = new List<TestEvent>
            {
                new(Bar: 1, Role: "Kick", Beat: 1m, Velocity: 100),
                new(Bar: 1, Role: "Snare", Beat: 2m, Velocity: 90)
            };
            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>();

            var result = ProtectionApplier.Apply(
                events: events,
                mergedProtectionsPerBar: protections,
                getBar: e => e.Bar,
                getRoleName: e => e.Role,
                getBeat: e => e.Beat,
                setFlags: (e, mustHit, neverRemove, prot) =>
                {
                    e.IsMustHit = mustHit;
                    e.IsNeverRemove = neverRemove;
                    e.IsProtected = prot;
                    return e;
                },
                createEvent: (bar, role, beat) => new TestEvent(bar, role, beat, 100));

            Assert.Equal(2, result.Count);
            Assert.Contains(result, e => e.Role == "Kick" && e.Beat == 1m);
            Assert.Contains(result, e => e.Role == "Snare" && e.Beat == 2m);
        }

        [Fact]
        public void Apply_MustHitOnsets_AddsMiddingEvents()
        {
            var events = new List<TestEvent>
            {
                new(Bar: 1, Role: "Kick", Beat: 1m, Velocity: 100)
            };

            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
            {
                [1] = new()
                {
                    ["Kick"] = new RoleProtectionSet
                    {
                        MustHitOnsets = new List<decimal> { 1m, 3m }
                    }
                }
            };

            var result = ProtectionApplier.Apply(
                events: events,
                mergedProtectionsPerBar: protections,
                getBar: e => e.Bar,
                getRoleName: e => e.Role,
                getBeat: e => e.Beat,
                setFlags: (e, mustHit, neverRemove, prot) =>
                {
                    e.IsMustHit = mustHit || e.IsMustHit;
                    e.IsNeverRemove = neverRemove || e.IsNeverRemove;
                    e.IsProtected = prot || e.IsProtected;
                    return e;
                },
                createEvent: (bar, role, beat) => new TestEvent(bar, role, beat, 100));

            Assert.Equal(2, result.Count);
            Assert.Contains(result, e => e.Role == "Kick" && e.Beat == 1m);
            Assert.Contains(result, e => e.Role == "Kick" && e.Beat == 3m && e.IsMustHit);
        }

        [Fact]
        public void Apply_NeverAddOnsets_RemovesMatchingEvents()
        {
            var events = new List<TestEvent>
            {
                new(Bar: 1, Role: "Kick", Beat: 1m, Velocity: 100),
                new(Bar: 1, Role: "Kick", Beat: 1.5m, Velocity: 80),
                new(Bar: 1, Role: "Kick", Beat: 2m, Velocity: 100)
            };

            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
            {
                [1] = new()
                {
                    ["Kick"] = new RoleProtectionSet
                    {
                        NeverAddOnsets = new List<decimal> { 1.5m }
                    }
                }
            };

            var result = ProtectionApplier.Apply(
                events: events,
                mergedProtectionsPerBar: protections,
                getBar: e => e.Bar,
                getRoleName: e => e.Role,
                getBeat: e => e.Beat,
                setFlags: (e, mustHit, neverRemove, prot) =>
                {
                    e.IsMustHit = mustHit;
                    e.IsNeverRemove = neverRemove;
                    e.IsProtected = prot;
                    return e;
                },
                createEvent: (bar, role, beat) => new TestEvent(bar, role, beat, 100));

            Assert.Equal(2, result.Count);
            Assert.Contains(result, e => e.Beat == 1m);
            Assert.Contains(result, e => e.Beat == 2m);
            Assert.DoesNotContain(result, e => e.Beat == 1.5m);
        }

        [Fact]
        public void Apply_NeverRemoveOnsets_SetsFlag()
        {
            var events = new List<TestEvent>
            {
                new(Bar: 1, Role: "Snare", Beat: 2m, Velocity: 100),
                new(Bar: 1, Role: "Snare", Beat: 4m, Velocity: 100)
            };

            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
            {
                [1] = new()
                {
                    ["Snare"] = new RoleProtectionSet
                    {
                        NeverRemoveOnsets = new List<decimal> { 2m, 4m }
                    }
                }
            };

            var result = ProtectionApplier.Apply(
                events: events,
                mergedProtectionsPerBar: protections,
                getBar: e => e.Bar,
                getRoleName: e => e.Role,
                getBeat: e => e.Beat,
                setFlags: (e, mustHit, neverRemove, prot) =>
                {
                    e.IsMustHit = mustHit;
                    e.IsNeverRemove = neverRemove;
                    e.IsProtected = prot;
                    return e;
                },
                createEvent: (bar, role, beat) => new TestEvent(bar, role, beat, 100));

            Assert.Equal(2, result.Count);
            Assert.All(result, e => Assert.True(e.IsNeverRemove));
        }

        [Fact]
        public void Apply_ProtectedOnsets_SetsFlag()
        {
            var events = new List<TestEvent>
            {
                new(Bar: 1, Role: "Hat", Beat: 1m, Velocity: 80),
                new(Bar: 1, Role: "Hat", Beat: 1.5m, Velocity: 70),
                new(Bar: 1, Role: "Hat", Beat: 2m, Velocity: 80)
            };

            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
            {
                [1] = new()
                {
                    ["Hat"] = new RoleProtectionSet
                    {
                        ProtectedOnsets = new List<decimal> { 1m, 2m }
                    }
                }
            };

            var result = ProtectionApplier.Apply(
                events: events,
                mergedProtectionsPerBar: protections,
                getBar: e => e.Bar,
                getRoleName: e => e.Role,
                getBeat: e => e.Beat,
                setFlags: (e, mustHit, neverRemove, prot) =>
                {
                    e.IsMustHit = mustHit;
                    e.IsNeverRemove = neverRemove;
                    e.IsProtected = prot;
                    return e;
                },
                createEvent: (bar, role, beat) => new TestEvent(bar, role, beat, 100));

            Assert.Equal(3, result.Count);
            Assert.True(result.Single(e => e.Beat == 1m).IsProtected);
            Assert.False(result.Single(e => e.Beat == 1.5m).IsProtected);
            Assert.True(result.Single(e => e.Beat == 2m).IsProtected);
        }

        [Fact]
        public void Apply_MultipleBars_HandlesCorrectly()
        {
            var events = new List<TestEvent>
            {
                new(Bar: 1, Role: "Kick", Beat: 1m, Velocity: 100),
                new(Bar: 2, Role: "Kick", Beat: 1m, Velocity: 100)
            };

            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
            {
                [1] = new()
                {
                    ["Kick"] = new RoleProtectionSet
                    {
                        MustHitOnsets = new List<decimal> { 1m, 3m }
                    }
                },
                [2] = new()
                {
                    ["Kick"] = new RoleProtectionSet
                    {
                        ProtectedOnsets = new List<decimal> { 1m }
                    }
                }
            };

            var result = ProtectionApplier.Apply(
                events: events,
                mergedProtectionsPerBar: protections,
                getBar: e => e.Bar,
                getRoleName: e => e.Role,
                getBeat: e => e.Beat,
                setFlags: (e, mustHit, neverRemove, prot) =>
                {
                    e.IsMustHit = mustHit || e.IsMustHit;
                    e.IsNeverRemove = neverRemove || e.IsNeverRemove;
                    e.IsProtected = prot || e.IsProtected;
                    return e;
                },
                createEvent: (bar, role, beat) => new TestEvent(bar, role, beat, 100));

            Assert.Equal(3, result.Count);
            var bar1Events = result.Where(e => e.Bar == 1).ToList();
            var bar2Events = result.Where(e => e.Bar == 2).ToList();

            Assert.Equal(2, bar1Events.Count);
            Assert.Contains(bar1Events, e => e.Beat == 3m && e.IsMustHit);

            Assert.Single(bar2Events);
            Assert.True(bar2Events[0].IsProtected);
        }

        [Fact]
        public void Apply_MultipleRoles_HandlesIndependently()
        {
            var events = new List<TestEvent>
            {
                new(Bar: 1, Role: "Kick", Beat: 1m, Velocity: 100),
                new(Bar: 1, Role: "Snare", Beat: 2m, Velocity: 90)
            };

            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
            {
                [1] = new()
                {
                    ["Kick"] = new RoleProtectionSet
                    {
                        NeverRemoveOnsets = new List<decimal> { 1m }
                    },
                    ["Snare"] = new RoleProtectionSet
                    {
                        ProtectedOnsets = new List<decimal> { 2m }
                    }
                }
            };

            var result = ProtectionApplier.Apply(
                events: events,
                mergedProtectionsPerBar: protections,
                getBar: e => e.Bar,
                getRoleName: e => e.Role,
                getBeat: e => e.Beat,
                setFlags: (e, mustHit, neverRemove, prot) =>
                {
                    e.IsMustHit = mustHit;
                    e.IsNeverRemove = neverRemove;
                    e.IsProtected = prot;
                    return e;
                },
                createEvent: (bar, role, beat) => new TestEvent(bar, role, beat, 100));

            Assert.Equal(2, result.Count);
            var kick = result.Single(e => e.Role == "Kick");
            var snare = result.Single(e => e.Role == "Snare");

            Assert.True(kick.IsNeverRemove);
            Assert.False(kick.IsProtected);

            Assert.False(snare.IsNeverRemove);
            Assert.True(snare.IsProtected);
        }

        [Fact]
        public void Apply_Deduplicates_ByBarRoleBeat()
        {
            var events = new List<TestEvent>
            {
                new(Bar: 1, Role: "Kick", Beat: 1m, Velocity: 100),
                new(Bar: 1, Role: "Kick", Beat: 1m, Velocity: 110), // duplicate
                new(Bar: 1, Role: "Kick", Beat: 2m, Velocity: 90)
            };

            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>();

            var result = ProtectionApplier.Apply(
                events: events,
                mergedProtectionsPerBar: protections,
                getBar: e => e.Bar,
                getRoleName: e => e.Role,
                getBeat: e => e.Beat,
                setFlags: (e, mustHit, neverRemove, prot) =>
                {
                    e.IsMustHit = mustHit;
                    e.IsNeverRemove = neverRemove;
                    e.IsProtected = prot;
                    return e;
                },
                createEvent: (bar, role, beat) => new TestEvent(bar, role, beat, 100));

            Assert.Equal(2, result.Count);
            Assert.Single(result, e => e.Beat == 1m);
            Assert.Single(result, e => e.Beat == 2m);
        }

        [Fact]
        public void Apply_SortsByBarThenBeat()
        {
            var events = new List<TestEvent>
            {
                new(Bar: 2, Role: "Kick", Beat: 1m, Velocity: 100),
                new(Bar: 1, Role: "Snare", Beat: 2m, Velocity: 90),
                new(Bar: 1, Role: "Kick", Beat: 1m, Velocity: 100)
            };

            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>();

            var result = ProtectionApplier.Apply(
                events: events,
                mergedProtectionsPerBar: protections,
                getBar: e => e.Bar,
                getRoleName: e => e.Role,
                getBeat: e => e.Beat,
                setFlags: (e, mustHit, neverRemove, prot) =>
                {
                    e.IsMustHit = mustHit;
                    e.IsNeverRemove = neverRemove;
                    e.IsProtected = prot;
                    return e;
                },
                createEvent: (bar, role, beat) => new TestEvent(bar, role, beat, 100));

            Assert.Equal(3, result.Count);
            Assert.Equal(1, result[0].Bar);
            Assert.Equal(1m, result[0].Beat);
            Assert.Equal(1, result[1].Bar);
            Assert.Equal(2m, result[1].Beat);
            Assert.Equal(2, result[2].Bar);
        }

        [Fact]
        public void Apply_RoleNameCaseInsensitive()
        {
            var events = new List<TestEvent>
            {
                new(Bar: 1, Role: "kick", Beat: 1m, Velocity: 100)
            };

            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
            {
                [1] = new()
                {
                    ["KICK"] = new RoleProtectionSet
                    {
                        NeverRemoveOnsets = new List<decimal> { 1m }
                    }
                }
            };

            var result = ProtectionApplier.Apply(
                events: events,
                mergedProtectionsPerBar: protections,
                getBar: e => e.Bar,
                getRoleName: e => e.Role,
                getBeat: e => e.Beat,
                setFlags: (e, mustHit, neverRemove, prot) =>
                {
                    e.IsMustHit = mustHit;
                    e.IsNeverRemove = neverRemove;
                    e.IsProtected = prot;
                    return e;
                },
                createEvent: (bar, role, beat) => new TestEvent(bar, role, beat, 100));

            Assert.Single(result);
            Assert.True(result[0].IsNeverRemove);
        }

        [Fact]
        public void Apply_CombinedProtections_AllFlagsSetCorrectly()
        {
            var events = new List<TestEvent>
            {
                new(Bar: 1, Role: "Kick", Beat: 1m, Velocity: 100)
            };

            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
            {
                [1] = new()
                {
                    ["Kick"] = new RoleProtectionSet
                    {
                        MustHitOnsets = new List<decimal> { 1m, 2m },
                        NeverRemoveOnsets = new List<decimal> { 1m, 2m },
                        ProtectedOnsets = new List<decimal> { 1m, 2m }
                    }
                }
            };

            var result = ProtectionApplier.Apply(
                events: events,
                mergedProtectionsPerBar: protections,
                getBar: e => e.Bar,
                getRoleName: e => e.Role,
                getBeat: e => e.Beat,
                setFlags: (e, mustHit, neverRemove, prot) =>
                {
                    e.IsMustHit = mustHit || e.IsMustHit;
                    e.IsNeverRemove = neverRemove || e.IsNeverRemove;
                    e.IsProtected = prot || e.IsProtected;
                    return e;
                },
                createEvent: (bar, role, beat) => new TestEvent(bar, role, beat, 100));

            Assert.Equal(2, result.Count);
            var beat1 = result.Single(e => e.Beat == 1m);
            var beat2 = result.Single(e => e.Beat == 2m);

            // Beat 1 existed, so not marked as MustHit (only added events get that)
            Assert.False(beat1.IsMustHit);
            Assert.True(beat1.IsNeverRemove);
            Assert.True(beat1.IsProtected);

            // Beat 2 was added, so marked as MustHit plus other flags
            Assert.True(beat2.IsMustHit);
            Assert.True(beat2.IsNeverRemove);
            Assert.True(beat2.IsProtected);
        }

        [Fact]
        public void Apply_BarsWithoutProtections_PassThroughUnchanged()
        {
            var events = new List<TestEvent>
            {
                new(Bar: 1, Role: "Kick", Beat: 1m, Velocity: 100),
                new(Bar: 2, Role: "Kick", Beat: 1m, Velocity: 100),
                new(Bar: 3, Role: "Kick", Beat: 1m, Velocity: 100)
            };

            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
            {
                [2] = new()
                {
                    ["Kick"] = new RoleProtectionSet
                    {
                        NeverRemoveOnsets = new List<decimal> { 1m }
                    }
                }
            };

            var result = ProtectionApplier.Apply(
                events: events,
                mergedProtectionsPerBar: protections,
                getBar: e => e.Bar,
                getRoleName: e => e.Role,
                getBeat: e => e.Beat,
                setFlags: (e, mustHit, neverRemove, prot) =>
                {
                    e.IsMustHit = mustHit;
                    e.IsNeverRemove = neverRemove;
                    e.IsProtected = prot;
                    return e;
                },
                createEvent: (bar, role, beat) => new TestEvent(bar, role, beat, 100));

            Assert.Equal(3, result.Count);

            var bar1 = result.Single(e => e.Bar == 1);
            Assert.False(bar1.IsNeverRemove);
            Assert.False(bar1.IsProtected);

            var bar2 = result.Single(e => e.Bar == 2);
            Assert.True(bar2.IsNeverRemove);

            var bar3 = result.Single(e => e.Bar == 3);
            Assert.False(bar3.IsNeverRemove);
            Assert.False(bar3.IsProtected);
        }

        [Fact]
        public void Apply_NullProtectionLists_HandledGracefully()
        {
            var events = new List<TestEvent>
            {
                new(Bar: 1, Role: "Kick", Beat: 1m, Velocity: 100)
            };

            var protections = new Dictionary<int, Dictionary<string, RoleProtectionSet>>
            {
                [1] = new()
                {
                    ["Kick"] = new RoleProtectionSet
                    {
                        MustHitOnsets = null!,
                        NeverRemoveOnsets = null!,
                        ProtectedOnsets = null!,
                        NeverAddOnsets = null!
                    }
                }
            };

            var result = ProtectionApplier.Apply(
                events: events,
                mergedProtectionsPerBar: protections,
                getBar: e => e.Bar,
                getRoleName: e => e.Role,
                getBeat: e => e.Beat,
                setFlags: (e, mustHit, neverRemove, prot) =>
                {
                    e.IsMustHit = mustHit;
                    e.IsNeverRemove = neverRemove;
                    e.IsProtected = prot;
                    return e;
                },
                createEvent: (bar, role, beat) => new TestEvent(bar, role, beat, 100));

            Assert.Single(result);
            Assert.False(result[0].IsMustHit);
            Assert.False(result[0].IsNeverRemove);
            Assert.False(result[0].IsProtected);
        }
    }
}
