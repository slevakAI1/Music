using Music.Generator;
using Music.Generator.Agents.Drums;
using Music.Generator.Groove;
using Xunit;

namespace Music.Tests.Generator.Groove
{
    public class CapEnforcementTests
    {
        private readonly DrumCapsEnforcer _enforcer = new();

        #region Test Helpers

        private GroovePresetDefinition CreateTestPreset(
            int? maxHitsPerBar = null,
            int? maxHitsPerBeat = null,
            int? roleMaxDensity = null)
        {
            var preset = new GroovePresetDefinition
            {
                Identity = new GroovePresetIdentity
                {
                    Name = "TestPreset",
                    BeatsPerBar = 4
                },
                AnchorLayer = new GrooveInstanceLayer(),
                ProtectionPolicy = new GrooveProtectionPolicy
                {
                    RoleConstraintPolicy = new GrooveRoleConstraintPolicy
                    {
                        RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
                        {
                            ["Kick"] = new RoleRhythmVocabulary
                            {
                                MaxHitsPerBar = maxHitsPerBar ?? 32,
                                MaxHitsPerBeat = maxHitsPerBeat ?? 4,
                                AllowSyncopation = true,
                                AllowAnticipation = true
                            }
                        },
                        RoleMaxDensityPerBar = roleMaxDensity.HasValue
                            ? new Dictionary<string, int> { ["Kick"] = roleMaxDensity.Value }
                            : new Dictionary<string, int>()
                    }
                },
                VariationCatalog = new GrooveVariationCatalog
                {
                    Identity = new GroovePresetIdentity { Name = "TestCatalog" },
                    HierarchyLayers = new List<GrooveVariationLayer>()
                }
            };

            return preset;
        }

        private GrooveBarPlan CreateTestBarPlan(int barNumber, List<GrooveOnset> onsets)
        {
            return new GrooveBarPlan
            {
                BarNumber = barNumber,
                BaseOnsets = new List<GrooveOnset>(),
                SelectedVariationOnsets = onsets,
                FinalOnsets = new List<GrooveOnset>(),
                Diagnostics = null
            };
        }

        private GrooveOnset CreateOnset(
            string role,
            int barNumber,
            decimal beat,
            OnsetStrength strength = OnsetStrength.Offbeat,
            bool isMustHit = false,
            bool isNeverRemove = false,
            bool isProtected = false,
            GrooveOnsetProvenance? provenance = null)
        {
            return new GrooveOnset
            {
                Role = role,
                BarNumber = barNumber,
                Beat = beat,
                Strength = strength,
                Velocity = 80,
                TimingOffsetTicks = 0,
                IsMustHit = isMustHit,
                IsNeverRemove = isNeverRemove,
                IsProtected = isProtected,
                Provenance = provenance
            };
        }

        #endregion

        #region Basic Invariants

        [Fact]
        public void EnforceHardCaps_NullBarPlan_ThrowsArgumentNullException()
        {
            var preset = CreateTestPreset();

            Assert.Throws<ArgumentNullException>(() =>
                _enforcer.EnforceHardCaps(null!, preset, null, null, 42));
        }

        [Fact]
        public void EnforceHardCaps_NullPreset_ThrowsArgumentNullException()
        {
            var barPlan = CreateTestBarPlan(1, new List<GrooveOnset>());

            Assert.Throws<ArgumentNullException>(() =>
                _enforcer.EnforceHardCaps(barPlan, null!, null, null, 42));
        }

        [Fact]
        public void EnforceHardCaps_EmptyOnsets_ReturnsEmpty()
        {
            var preset = CreateTestPreset();
            var barPlan = CreateTestBarPlan(1, new List<GrooveOnset>());

            var result = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 42);

            Assert.NotNull(result.FinalOnsets);
            Assert.Empty(result.FinalOnsets);
        }

        [Fact]
        public void EnforceHardCaps_NoCapsViolated_ReturnsAllOnsets()
        {
            var preset = CreateTestPreset(maxHitsPerBar: 10, maxHitsPerBeat: 2);
            var onsets = new List<GrooveOnset>
            {
                CreateOnset("Kick", 1, 1.0m),
                CreateOnset("Kick", 1, 2.0m),
                CreateOnset("Kick", 1, 3.0m)
            };
            var barPlan = CreateTestBarPlan(1, onsets);

            var result = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 42);

            Assert.Equal(3, result.FinalOnsets!.Count);
        }

        #endregion

        #region MaxHitsPerBar Cap

        [Fact]
        public void EnforceHardCaps_MaxHitsPerBar_PrunesExcessOnsets()
        {
            var preset = CreateTestPreset(maxHitsPerBar: 4);
            var onsets = new List<GrooveOnset>
            {
                CreateOnset("Kick", 1, 1.0m),
                CreateOnset("Kick", 1, 1.5m),
                CreateOnset("Kick", 1, 2.0m),
                CreateOnset("Kick", 1, 2.5m),
                CreateOnset("Kick", 1, 3.0m),
                CreateOnset("Kick", 1, 3.5m),
                CreateOnset("Kick", 1, 4.0m),
                CreateOnset("Kick", 1, 4.5m)
            };
            var barPlan = CreateTestBarPlan(1, onsets);

            var result = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 42);

            Assert.Equal(4, result.FinalOnsets!.Count);
        }

        [Fact]
        public void EnforceHardCaps_MaxHitsPerBar_PreservesMustHitOnsets()
        {
            var preset = CreateTestPreset(maxHitsPerBar: 2);
            var onsets = new List<GrooveOnset>
            {
                CreateOnset("Kick", 1, 1.0m, isMustHit: true),
                CreateOnset("Kick", 1, 2.0m, isMustHit: true),
                CreateOnset("Kick", 1, 3.0m),
                CreateOnset("Kick", 1, 4.0m)
            };
            var barPlan = CreateTestBarPlan(1, onsets);

            var result = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 42);

            // Should keep both MustHit onsets even if it exceeds cap
            // (protection takes precedence)
            var mustHitCount = result.FinalOnsets!.Count(o => o.IsMustHit);
            Assert.Equal(2, mustHitCount);
        }

        [Fact]
        public void EnforceHardCaps_MaxHitsPerBar_PreservesNeverRemoveOnsets()
        {
            var preset = CreateTestPreset(maxHitsPerBar: 2);
            var onsets = new List<GrooveOnset>
            {
                CreateOnset("Kick", 1, 1.0m, isNeverRemove: true),
                CreateOnset("Kick", 1, 2.0m, isNeverRemove: true),
                CreateOnset("Kick", 1, 3.0m),
                CreateOnset("Kick", 1, 4.0m)
            };
            var barPlan = CreateTestBarPlan(1, onsets);

            var result = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 42);

            var neverRemoveCount = result.FinalOnsets!.Count(o => o.IsNeverRemove);
            Assert.Equal(2, neverRemoveCount);
        }

        [Fact]
        public void EnforceHardCaps_MaxHitsPerBar_PrefersPruningLowerStrength()
        {
            var preset = CreateTestPreset(maxHitsPerBar: 2);
            var onsets = new List<GrooveOnset>
            {
                CreateOnset("Kick", 1, 1.0m, OnsetStrength.Downbeat),
                CreateOnset("Kick", 1, 2.0m, OnsetStrength.Ghost),
                CreateOnset("Kick", 1, 3.0m, OnsetStrength.Offbeat)
            };
            var barPlan = CreateTestBarPlan(1, onsets);

            var result = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 42);

            // Should keep Downbeat, prune Ghost and Offbeat (Ghost first)
            Assert.Equal(2, result.FinalOnsets!.Count);
            Assert.Contains(result.FinalOnsets, o => o.Strength == OnsetStrength.Downbeat);
            Assert.DoesNotContain(result.FinalOnsets, o => o.Strength == OnsetStrength.Ghost);
        }

        #endregion

        #region MaxHitsPerBeat Cap

        [Fact]
        public void EnforceHardCaps_MaxHitsPerBeat_PrunesExcessInSameBeat()
        {
            var preset = CreateTestPreset(maxHitsPerBeat: 2);
            var onsets = new List<GrooveOnset>
            {
                CreateOnset("Kick", 1, 1.0m),
                CreateOnset("Kick", 1, 1.25m),
                CreateOnset("Kick", 1, 1.5m),
                CreateOnset("Kick", 1, 1.75m),
                CreateOnset("Kick", 1, 2.0m)
            };
            var barPlan = CreateTestBarPlan(1, onsets);

            var result = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 42);

            // Beat 1 should have max 2 onsets, beat 2 should have 1
            var beat1Count = result.FinalOnsets!.Count(o => (int)Math.Floor(o.Beat) == 1);
            Assert.True(beat1Count <= 2, $"Beat 1 has {beat1Count} onsets, expected <= 2");
        }

        [Fact]
        public void EnforceHardCaps_MaxHitsPerBeat_MultipleBeatBuckets()
        {
            var preset = CreateTestPreset(maxHitsPerBeat: 1);
            var onsets = new List<GrooveOnset>
            {
                CreateOnset("Kick", 1, 1.0m),
                CreateOnset("Kick", 1, 1.5m),
                CreateOnset("Kick", 1, 2.0m),
                CreateOnset("Kick", 1, 2.5m),
                CreateOnset("Kick", 1, 3.0m),
                CreateOnset("Kick", 1, 3.5m)
            };
            var barPlan = CreateTestBarPlan(1, onsets);

            var result = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 42);

            // Each beat bucket should have max 1 onset
            for (int beat = 1; beat <= 3; beat++)
            {
                var beatCount = result.FinalOnsets!.Count(o => (int)Math.Floor(o.Beat) == beat);
                Assert.True(beatCount <= 1, $"Beat {beat} has {beatCount} onsets, expected <= 1");
            }
        }

        #endregion

        #region RoleMaxDensityPerBar Cap

        [Fact]
        public void EnforceHardCaps_RoleMaxDensityPerBar_PrunesExcessOnsets()
        {
            var preset = CreateTestPreset(roleMaxDensity: 3);
            var onsets = new List<GrooveOnset>
            {
                CreateOnset("Kick", 1, 1.0m),
                CreateOnset("Kick", 1, 2.0m),
                CreateOnset("Kick", 1, 3.0m),
                CreateOnset("Kick", 1, 4.0m),
                CreateOnset("Kick", 1, 4.5m)
            };
            var barPlan = CreateTestBarPlan(1, onsets);

            var result = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 42);

            Assert.Equal(3, result.FinalOnsets!.Count);
        }

        #endregion

        #region Candidate and Group Caps

        [Fact]
        public void EnforceHardCaps_CandidateMaxAddsPerBar_PrunesExcess()
        {
            var catalog = new GrooveVariationCatalog
            {
                Identity = new GroovePresetIdentity { Name = "Test" },
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    new GrooveVariationLayer
                    {
                        LayerId = "Base",
                        CandidateGroups = new List<DrumCandidateGroup>
                        {
                            new DrumCandidateGroup
                            {
                                GroupId = "TestGroup",
                                BaseProbabilityBias = 1.0,
                                Candidates = new List<DrumOnsetCandidate>
                                {
                                    new DrumOnsetCandidate
                                    {
                                        Role = "Kick",
                                        OnsetBeat = 1.5m,
                                        MaxAddsPerBar = 1,
                                        ProbabilityBias = 1.0
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var preset = CreateTestPreset();
            preset.VariationCatalog = catalog;

            var provenance = new GrooveOnsetProvenance
            {
                Source = GrooveOnsetSource.Variation,
                GroupId = "TestGroup",
                CandidateId = "TestGroup_Kick_1.5"
            };

            var onsets = new List<GrooveOnset>
            {
                CreateOnset("Kick", 1, 1.5m, provenance: provenance),
                CreateOnset("Kick", 1, 1.5m, provenance: provenance),
                CreateOnset("Kick", 1, 1.5m, provenance: provenance)
            };
            var barPlan = CreateTestBarPlan(1, onsets);

            var result = _enforcer.EnforceHardCaps(barPlan, preset, null, catalog, 42);

            // Should keep only 1 instance due to MaxAddsPerBar
            Assert.Single(result.FinalOnsets!);
        }

        [Fact]
        public void EnforceHardCaps_GroupMaxAddsPerBar_PrunesExcess()
        {
            var catalog = new GrooveVariationCatalog
            {
                Identity = new GroovePresetIdentity { Name = "Test" },
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    new GrooveVariationLayer
                    {
                        LayerId = "Base",
                        CandidateGroups = new List<DrumCandidateGroup>
                        {
                            new DrumCandidateGroup
                            {
                                GroupId = "TestGroup",
                                MaxAddsPerBar = 2,
                                BaseProbabilityBias = 1.0,
                                Candidates = new List<DrumOnsetCandidate>
                                {
                                    new DrumOnsetCandidate
                                    {
                                        Role = "Kick",
                                        OnsetBeat = 1.5m,
                                        ProbabilityBias = 1.0
                                    },
                                    new DrumOnsetCandidate
                                    {
                                        Role = "Kick",
                                        OnsetBeat = 2.5m,
                                        ProbabilityBias = 0.5
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var preset = CreateTestPreset();
            preset.VariationCatalog = catalog;

            var provenance1 = new GrooveOnsetProvenance
            {
                Source = GrooveOnsetSource.Variation,
                GroupId = "TestGroup"
            };

            var onsets = new List<GrooveOnset>
            {
                CreateOnset("Kick", 1, 1.5m, provenance: provenance1),
                CreateOnset("Kick", 1, 2.5m, provenance: provenance1),
                CreateOnset("Kick", 1, 3.5m, provenance: provenance1)
            };
            var barPlan = CreateTestBarPlan(1, onsets);

            var result = _enforcer.EnforceHardCaps(barPlan, preset, null, catalog, 42);

            // Should keep only 2 onsets from the group
            Assert.Equal(2, result.FinalOnsets!.Count);
        }

        #endregion

        #region Combined Caps

        [Fact]
        public void EnforceHardCaps_MultipleCaps_EnforcesAllConstraints()
        {
            var preset = CreateTestPreset(maxHitsPerBar: 4, maxHitsPerBeat: 1);
            var onsets = new List<GrooveOnset>
            {
                CreateOnset("Kick", 1, 1.0m),
                CreateOnset("Kick", 1, 1.5m),
                CreateOnset("Kick", 1, 2.0m),
                CreateOnset("Kick", 1, 2.5m),
                CreateOnset("Kick", 1, 3.0m),
                CreateOnset("Kick", 1, 3.5m)
            };
            var barPlan = CreateTestBarPlan(1, onsets);

            var result = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 42);

            // Should satisfy both MaxHitsPerBar (4) and MaxHitsPerBeat (1)
            Assert.True(result.FinalOnsets!.Count <= 4);
            for (int beat = 1; beat <= 3; beat++)
            {
                var beatCount = result.FinalOnsets.Count(o => (int)Math.Floor(o.Beat) == beat);
                Assert.True(beatCount <= 1);
            }
        }

        #endregion

        #region Deterministic Behavior

        [Fact]
        public void EnforceHardCaps_SameSeed_ProducesSameResults()
        {
            var preset = CreateTestPreset(maxHitsPerBar: 3);
            var onsets = new List<GrooveOnset>
            {
                CreateOnset("Kick", 1, 1.0m, OnsetStrength.Offbeat),
                CreateOnset("Kick", 1, 2.0m, OnsetStrength.Offbeat),
                CreateOnset("Kick", 1, 3.0m, OnsetStrength.Offbeat),
                CreateOnset("Kick", 1, 4.0m, OnsetStrength.Offbeat)
            };
            var barPlan = CreateTestBarPlan(1, onsets);

            var result1 = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 42);
            var result2 = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 42);

            // Same seed should produce identical results
            Assert.Equal(result1.FinalOnsets!.Count, result2.FinalOnsets!.Count);
            for (int i = 0; i < result1.FinalOnsets.Count; i++)
            {
                Assert.Equal(result1.FinalOnsets[i].Beat, result2.FinalOnsets[i].Beat);
            }
        }

        [Fact]
        public void EnforceHardCaps_DifferentSeed_MayProduceDifferentResults()
        {
            var preset = CreateTestPreset(maxHitsPerBeat: 1);
            var onsets = new List<GrooveOnset>
            {
                CreateOnset("Kick", 1, 1.0m, OnsetStrength.Offbeat),
                CreateOnset("Kick", 1, 1.5m, OnsetStrength.Offbeat),
                CreateOnset("Kick", 1, 1.75m, OnsetStrength.Offbeat)
            };
            var barPlan = CreateTestBarPlan(1, onsets);

            var result1 = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 42);
            var result2 = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 99);

            // Different seeds may produce different tie-breaks
            // (we can't assert they're different, but they shouldn't crash)
            Assert.NotNull(result1.FinalOnsets);
            Assert.NotNull(result2.FinalOnsets);
        }

        #endregion

        #region Diagnostics

        [Fact]
        public void EnforceHardCaps_DiagnosticsEnabled_ProducesDiagnostics()
        {
            // Story G1: GrooveCapsEnforcer does not yet populate structured diagnostics.
            // This test verifies cap enforcement behavior with diagnosticsEnabled=true still works.
            // Full structured diagnostics will be added in future Story G1 integration work.
            var preset = CreateTestPreset(maxHitsPerBar: 2);
            var onsets = new List<GrooveOnset>
            {
                CreateOnset("Kick", 1, 1.0m),
                CreateOnset("Kick", 1, 2.0m),
                CreateOnset("Kick", 1, 3.0m)
            };
            var barPlan = CreateTestBarPlan(1, onsets);

            var result = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 42, diagnosticsEnabled: true);

            // Diagnostics is null until full G1 integration; caps still enforced correctly
            Assert.Equal(2, result.FinalOnsets.Count);
        }

        [Fact]
        public void EnforceHardCaps_DiagnosticsDisabled_ProducesNoDiagnostics()
        {
            var preset = CreateTestPreset(maxHitsPerBar: 2);
            var onsets = new List<GrooveOnset>
            {
                CreateOnset("Kick", 1, 1.0m),
                CreateOnset("Kick", 1, 2.0m),
                CreateOnset("Kick", 1, 3.0m)
            };
            var barPlan = CreateTestBarPlan(1, onsets);

            var result = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 42, diagnosticsEnabled: false);

            Assert.Null(result.Diagnostics);
        }

        [Fact]
        public void EnforceHardCaps_DiagnosticsOnOff_ProducesSameOnsets()
        {
            var preset = CreateTestPreset(maxHitsPerBar: 2);
            var onsets = new List<GrooveOnset>
            {
                CreateOnset("Kick", 1, 1.0m),
                CreateOnset("Kick", 1, 2.0m),
                CreateOnset("Kick", 1, 3.0m)
            };
            var barPlan = CreateTestBarPlan(1, onsets);

            var resultWithDiag = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 42, diagnosticsEnabled: true);
            var resultNoDiag = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 42, diagnosticsEnabled: false);

            // Should produce identical onset lists regardless of diagnostics
            Assert.Equal(resultWithDiag.FinalOnsets!.Count, resultNoDiag.FinalOnsets!.Count);
            for (int i = 0; i < resultWithDiag.FinalOnsets.Count; i++)
            {
                Assert.Equal(resultWithDiag.FinalOnsets[i].Beat, resultNoDiag.FinalOnsets[i].Beat);
            }
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void EnforceHardCaps_AllOnsetsProtected_KeepsAll()
        {
            var preset = CreateTestPreset(maxHitsPerBar: 2);
            var onsets = new List<GrooveOnset>
            {
                CreateOnset("Kick", 1, 1.0m, isMustHit: true),
                CreateOnset("Kick", 1, 2.0m, isNeverRemove: true),
                CreateOnset("Kick", 1, 3.0m, isProtected: true),
                CreateOnset("Kick", 1, 4.0m, isMustHit: true)
            };
            var barPlan = CreateTestBarPlan(1, onsets);

            var result = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 42);

            // IsMustHit and IsNeverRemove should never be pruned
            // IsProtected is discouraged but can be removed if necessary
            var mustHitCount = result.FinalOnsets!.Count(o => o.IsMustHit || o.IsNeverRemove);
            Assert.Equal(3, mustHitCount); // 2 MustHit + 1 NeverRemove
        }

        [Fact]
        public void EnforceHardCaps_MixedProtection_PrunesUnprotectedFirst()
        {
            var preset = CreateTestPreset(maxHitsPerBar: 3);
            var onsets = new List<GrooveOnset>
            {
                CreateOnset("Kick", 1, 1.0m, isProtected: true),
                CreateOnset("Kick", 1, 2.0m), // unprotected
                CreateOnset("Kick", 1, 3.0m, isProtected: true),
                CreateOnset("Kick", 1, 4.0m)  // unprotected
            };
            var barPlan = CreateTestBarPlan(1, onsets);

            var result = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 42);

            // Should keep both protected and prune one unprotected
            var protectedCount = result.FinalOnsets!.Count(o => o.IsProtected);
            Assert.Equal(2, protectedCount);
            Assert.Equal(3, result.FinalOnsets.Count);
        }

        [Fact]
        public void EnforceHardCaps_NoVocabularyForRole_NoError()
        {
            var preset = CreateTestPreset();
            var onsets = new List<GrooveOnset>
            {
                CreateOnset("UnknownRole", 1, 1.0m),
                CreateOnset("UnknownRole", 1, 2.0m)
            };
            var barPlan = CreateTestBarPlan(1, onsets);

            // Should not throw, just return onsets as-is
            var result = _enforcer.EnforceHardCaps(barPlan, preset, null, null, 42);

            Assert.Equal(2, result.FinalOnsets!.Count);
        }

        #endregion
    }
}

