//// AI: purpose=Unit tests for Story 9 (Apply Must-Hit and Protection Rules); validates EnforceProtections behavior.
//// AI: coverage=MustHitOnsets addition, NeverRemoveOnsets marking, NeverAddOnsets filtering, ProtectedOnsets marking.
//// AI: deps=DrumTrackGeneratorNew.Generate; uses FluentAssertions for readable assertions.

//using FluentAssertions;
//using Music.Generator;
//using Music.Generator.Groove;
//using Music.MyMidi;

//namespace Music.Tests.Generator.Drums
//{
//    public class Story09_Tests
//    {
//        #region Test Data Builders

//        private static GroovePresetDefinition CreateMinimalGroovePreset(
//            List<decimal>? kickAnchors = null,
//            List<decimal>? snareAnchors = null,
//            List<decimal>? hatAnchors = null)
//        {
//            return new GroovePresetDefinition
//            {
//                Identity = new GroovePresetIdentity
//                {
//                    Name = "TestPreset",
//                    BeatsPerBar = 4
//                },
//                AnchorLayer = new GrooveInstanceLayer
//                {
//                    KickOnsets = kickAnchors ?? new List<decimal> { 1m, 3m },
//                    SnareOnsets = snareAnchors ?? new List<decimal> { 2m, 4m },
//                    HatOnsets = hatAnchors ?? new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m }
//                },
//                ProtectionPolicy = new GrooveProtectionPolicy
//                {
//                    Identity = new GroovePresetIdentity { BeatsPerBar = 4 },
//                    SubdivisionPolicy = new GrooveSubdivisionPolicy
//                    {
//                        AllowedSubdivisions = AllowedSubdivision.Quarter | AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth
//                    },
//                    RoleConstraintPolicy = new GrooveRoleConstraintPolicy(),
//                    PhraseHookPolicy = new GroovePhraseHookPolicy(),
//                    TimingPolicy = new GrooveTimingPolicy(),
//                    OrchestrationPolicy = new GrooveOrchestrationPolicy
//                    {
//                        DefaultsBySectionType = new List<SectionRolePresenceDefaults>
//                        {
//                            new SectionRolePresenceDefaults
//                            {
//                                SectionType = "Verse",
//                                RolePresent = new Dictionary<string, bool>
//                                {
//                                    ["DrumKit"] = true
//                                }
//                            }
//                        }
//                    },
//                    HierarchyLayers = new List<GrooveProtectionLayer>(),
//                    MergePolicy = new GrooveOverrideMergePolicy()
//                }
//            };
//        }

//        private static SectionTrack CreateSectionTrack(int totalBars)
//        {
//            var track = new SectionTrack();
//            track.Add(MusicConstants.eSectionType.Verse, totalBars);
//            return track;
//        }

//        private static BarTrack CreateBarTrack(int totalBars)
//        {
//            var timingTrack = new Timingtrack();
//            timingTrack.Add(new TimingEvent 
//            { 
//                StartBar = 1, 
//                Numerator = 4, 
//                Denominator = 4 
//            });
            
//            var barTrack = new BarTrack();
//            barTrack.RebuildFromTimingTrack(timingTrack, totalBars);
//            return barTrack;
//        }

//        #endregion

//        #region Story 9 Acceptance Criteria Tests

//        [Fact]
//        public void MustHitOnsets_AreAlwaysPresent_EvenIfMissingFromAnchorLayer()
//        {
//            // Arrange: Create preset with NO kick at beat 3, but MustHit requires it
//            var groovePreset = CreateMinimalGroovePreset(
//                kickAnchors: new List<decimal> { 1m }, // Missing beat 3
//                snareAnchors: new List<decimal> { 2m, 4m },
//                hatAnchors: new List<decimal> { 1m, 2m, 3m, 4m });

//            // Add protection layer with MustHitOnsets for kick beat 3
//            groovePreset.ProtectionPolicy.HierarchyLayers.Add(new GrooveProtectionLayer
//            {
//                LayerId = "Base",
//                IsAdditiveOnly = true,
//                RoleProtections = new Dictionary<string, RoleProtectionSet>
//                {
//                    ["Kick"] = new RoleProtectionSet
//                    {
//                        MustHitOnsets = new List<decimal> { 1m, 3m } // Require kick on 1 and 3
//                    }
//                }
//            });

//            var sectionTrack = CreateSectionTrack(totalBars: 2);
//            var barTrack = CreateBarTrack(totalBars: 2);
//            var segmentProfiles = new List<object>
//            {
//                new object
//                {
//                    StartBar = 1,
//                    EndBar = 2,
//                    EnabledProtectionTags = new List<string>()
//                }
//            };

//            // Act
//            var result = DrumTrackGenerator.Generate(
//                barTrack,
//                sectionTrack,
//                segmentProfiles,
//                groovePreset,
//                totalBars: 2,
//                midiProgramNumber: 255);

//            // Assert: Verify kick at beat 3 was added (even though absent from anchors)
//            var kickEvents = result.PartTrackNoteEvents
//                .Where(e => e.NoteNumber == DrumTrackGenerator.GetMidiNoteNumber(DrumRole.Kick))
//                .ToList();

//            // Should have kick on beat 1 (bar 1), beat 3 (bar 1), beat 1 (bar 2), beat 3 (bar 2)
//            kickEvents.Should().HaveCount(4, "MustHitOnsets requires kick on beats 1 and 3 in both bars");

//            // Verify beat 3 is present in bar 1
//            var bar1Beat3Tick = barTrack.ToTick(barNumber: 1, onsetBeat: 3m);
//            kickEvents.Should().Contain(e => e.AbsoluteTimeTicks == bar1Beat3Tick,
//                "kick at beat 3 in bar 1 must be present due to MustHitOnsets");
//        }

//        [Fact]
//        public void NeverRemoveOnsets_AreMarkedAsProtected()
//        {
//            // Arrange: Create preset with protection layer marking kick beat 1 as NeverRemove
//            var groovePreset = CreateMinimalGroovePreset();

//            groovePreset.ProtectionPolicy.HierarchyLayers.Add(new GrooveProtectionLayer
//            {
//                LayerId = "Base",
//                IsAdditiveOnly = true,
//                RoleProtections = new Dictionary<string, RoleProtectionSet>
//                {
//                    ["Kick"] = new RoleProtectionSet
//                    {
//                        NeverRemoveOnsets = new List<decimal> { 1m } // Beat 1 cannot be removed
//                    }
//                }
//            });

//            var sectionTrack = CreateSectionTrack(totalBars: 1);
//            var barTrack = CreateBarTrack(totalBars: 1);
//            var segmentProfiles = new List<object>
//            {
//                new object
//                {
//                    StartBar = 1,
//                    EndBar = 1,
//                    EnabledProtectionTags = new List<string>()
//                }
//            };

//            // Act
//            var result = DrumTrackGenerator.Generate(
//                barTrack,
//                sectionTrack,
//                segmentProfiles,
//                groovePreset,
//                totalBars: 1,
//                midiProgramNumber: 255);

//            // Assert: Verify kick at beat 1 exists (NeverRemove protection is enforced during generation)
//            var kickEvents = result.PartTrackNoteEvents
//                .Where(e => e.NoteNumber == DrumTrackGenerator.GetMidiNoteNumber(DrumRole.Kick))
//                .ToList();

//            var bar1Beat1Tick = barTrack.ToTick(barNumber: 1, onsetBeat: 1m);
//            kickEvents.Should().Contain(e => e.AbsoluteTimeTicks == bar1Beat1Tick,
//                "kick at beat 1 must be present and cannot be removed");
//        }

//        [Fact]
//        public void NeverAddOnsets_AreFilteredOut_EvenIfPresentInAnchors()
//        {
//            // Arrange: Create preset with kick at beat 2, but NeverAdd forbids it
//            var groovePreset = CreateMinimalGroovePreset(
//                kickAnchors: new List<decimal> { 1m, 2m, 3m }, // Includes beat 2
//                snareAnchors: new List<decimal> { 4m },
//                hatAnchors: new List<decimal> { 1m, 2m, 3m, 4m });

//            groovePreset.ProtectionPolicy.HierarchyLayers.Add(new GrooveProtectionLayer
//            {
//                LayerId = "Base",
//                IsAdditiveOnly = true,
//                RoleProtections = new Dictionary<string, RoleProtectionSet>
//                {
//                    ["Kick"] = new RoleProtectionSet
//                    {
//                        NeverAddOnsets = new List<decimal> { 2m } // Beat 2 is forbidden
//                    }
//                }
//            });

//            var sectionTrack = CreateSectionTrack(totalBars: 1);
//            var barTrack = CreateBarTrack(totalBars: 1);
//            var segmentProfiles = new List<object>
//            {
//                new object
//                {
//                    StartBar = 1,
//                    EndBar = 1,
//                    EnabledProtectionTags = new List<string>()
//                }
//            };

//            // Act
//            var result = DrumTrackGenerator.Generate(
//                barTrack,
//                sectionTrack,
//                segmentProfiles,
//                groovePreset,
//                totalBars: 1,
//                midiProgramNumber: 255);

//            // Assert: Verify kick at beat 2 was removed
//            var kickEvents = result.PartTrackNoteEvents
//                .Where(e => e.NoteNumber == DrumTrackGenerator.GetMidiNoteNumber(DrumRole.Kick))
//                .ToList();

//            var bar1Beat2Tick = barTrack.ToTick(barNumber: 1, onsetBeat: 2m);
//            kickEvents.Should().NotContain(e => e.AbsoluteTimeTicks == bar1Beat2Tick,
//                "kick at beat 2 should be filtered out by NeverAddOnsets");

//            // Verify beats 1 and 3 are still present
//            var bar1Beat1Tick = barTrack.ToTick(barNumber: 1, onsetBeat: 1m);
//            var bar1Beat3Tick = barTrack.ToTick(barNumber: 1, onsetBeat: 3m);
//            kickEvents.Should().Contain(e => e.AbsoluteTimeTicks == bar1Beat1Tick);
//            kickEvents.Should().Contain(e => e.AbsoluteTimeTicks == bar1Beat3Tick);
//        }

//        [Fact]
//        public void ProtectedOnsets_AreMarked_ButNotForbiddenToRemove()
//        {
//            // Arrange: Create preset with ProtectedOnsets (discouraged but not hard-protected)
//            var groovePreset = CreateMinimalGroovePreset();

//            groovePreset.ProtectionPolicy.HierarchyLayers.Add(new GrooveProtectionLayer
//            {
//                LayerId = "Base",
//                IsAdditiveOnly = true,
//                RoleProtections = new Dictionary<string, RoleProtectionSet>
//                {
//                    ["ClosedHat"] = new RoleProtectionSet
//                    {
//                        ProtectedOnsets = new List<decimal> { 1m, 2m, 3m, 4m } // Strong beats protected
//                    }
//                }
//            });

//            var sectionTrack = CreateSectionTrack(totalBars: 1);
//            var barTrack = CreateBarTrack(totalBars: 1);
//            var segmentProfiles = new List<object>
//            {
//                new object
//                {
//                    StartBar = 1,
//                    EndBar = 1,
//                    EnabledProtectionTags = new List<string>()
//                }
//            };

//            // Act
//            var result = DrumTrackGenerator.Generate(
//                barTrack,
//                sectionTrack,
//                segmentProfiles,
//                groovePreset,
//                totalBars: 1,
//                midiProgramNumber: 255);

//            // Assert: Verify protected hats are present (this is the baseline; future variations may prune them)
//            var hatEvents = result.PartTrackNoteEvents
//                .Where(e => e.NoteNumber == DrumTrackGenerator.GetMidiNoteNumber(DrumRole.ClosedHat))
//                .ToList();

//            hatEvents.Should().HaveCountGreaterThanOrEqualTo(4,
//                "protected onsets should be present in anchor layer");
//        }

//        [Fact]
//        public void MultipleProtectionRules_CanCoexist()
//        {
//            // Arrange: Test MustHit + NeverRemove + NeverAdd together
//            var groovePreset = CreateMinimalGroovePreset(
//                kickAnchors: new List<decimal> { 1m, 2m, 3m },
//                snareAnchors: new List<decimal> { 2m, 4m },
//                hatAnchors: new List<decimal> { 1m, 2m, 3m, 4m });

//            groovePreset.ProtectionPolicy.HierarchyLayers.Add(new GrooveProtectionLayer
//            {
//                LayerId = "Base",
//                IsAdditiveOnly = true,
//                RoleProtections = new Dictionary<string, RoleProtectionSet>
//                {
//                    ["Kick"] = new RoleProtectionSet
//                    {
//                        MustHitOnsets = new List<decimal> { 1m, 3m },
//                        NeverRemoveOnsets = new List<decimal> { 1m },
//                        NeverAddOnsets = new List<decimal> { 2m }
//                    },
//                    ["Snare"] = new RoleProtectionSet
//                    {
//                        MustHitOnsets = new List<decimal> { 2m, 4m },
//                        NeverRemoveOnsets = new List<decimal> { 2m, 4m }
//                    }
//                }
//            });

//            var sectionTrack = CreateSectionTrack(totalBars: 1);
//            var barTrack = CreateBarTrack(totalBars: 1);
//            var segmentProfiles = new List<object>
//            {
//                new object
//                {
//                    StartBar = 1,
//                    EndBar = 1,
//                    EnabledProtectionTags = new List<string>()
//                }
//            };

//            // Act
//            var result = DrumTrackGenerator.Generate(
//                barTrack,
//                sectionTrack,
//                segmentProfiles,
//                groovePreset,
//                totalBars: 1,
//                midiProgramNumber: 255);

//            // Assert: Verify kick beat 2 was removed (NeverAdd), kick beats 1,3 present (MustHit)
//            var kickEvents = result.PartTrackNoteEvents
//                .Where(e => e.NoteNumber == DrumTrackGenerator.GetMidiNoteNumber(DrumRole.Kick))
//                .ToList();

//            var bar1Beat1Tick = barTrack.ToTick(barNumber: 1, onsetBeat: 1m);
//            var bar1Beat2Tick = barTrack.ToTick(barNumber: 1, onsetBeat: 2m);
//            var bar1Beat3Tick = barTrack.ToTick(barNumber: 1, onsetBeat: 3m);

//            kickEvents.Should().Contain(e => e.AbsoluteTimeTicks == bar1Beat1Tick, "kick beat 1 is MustHit");
//            kickEvents.Should().NotContain(e => e.AbsoluteTimeTicks == bar1Beat2Tick, "kick beat 2 is NeverAdd");
//            kickEvents.Should().Contain(e => e.AbsoluteTimeTicks == bar1Beat3Tick, "kick beat 3 is MustHit");

//            // Verify snare beats 2,4 present (MustHit + NeverRemove)
//            var snareEvents = result.PartTrackNoteEvents
//                .Where(e => e.NoteNumber == DrumTrackGenerator.GetMidiNoteNumber(DrumRole.Snare))
//                .ToList();

//            var bar1Beat2TickSnare = barTrack.ToTick(barNumber: 1, onsetBeat: 2m);
//            var bar1Beat4Tick = barTrack.ToTick(barNumber: 1, onsetBeat: 4m);

//            snareEvents.Should().Contain(e => e.AbsoluteTimeTicks == bar1Beat2TickSnare, "snare beat 2 is MustHit");
//            snareEvents.Should().Contain(e => e.AbsoluteTimeTicks == bar1Beat4Tick, "snare beat 4 is MustHit");
//        }

//        [Fact]
//        public void ProtectionLayer_WithEnabledTags_IsApplied()
//        {
//            // Arrange: Protection layer requires "Drive" tag to be enabled
//            var groovePreset = CreateMinimalGroovePreset();

//            groovePreset.ProtectionPolicy.HierarchyLayers.Add(new GrooveProtectionLayer
//            {
//                LayerId = "DriveProtection",
//                AppliesWhenTagsAll = new List<string> { "Drive" },
//                IsAdditiveOnly = true,
//                RoleProtections = new Dictionary<string, RoleProtectionSet>
//                {
//                    ["Kick"] = new RoleProtectionSet
//                    {
//                        MustHitOnsets = new List<decimal> { 1m, 2m, 3m, 4m } // Extra kicks in drive mode
//                    }
//                }
//            });

//            var sectionTrack = CreateSectionTrack(totalBars: 1);
//            var barTrack = CreateBarTrack(totalBars: 1);
//            var segmentProfiles = new List<object>
//            {
//                new object
//                {
//                    StartBar = 1,
//                    EndBar = 1,
//                    EnabledProtectionTags = new List<string> { "Drive" } // Enable Drive tag
//                }
//            };

//            // Act
//            var result = DrumTrackGenerator.Generate(
//                barTrack,
//                sectionTrack,
//                segmentProfiles,
//                groovePreset,
//                totalBars: 1,
//                midiProgramNumber: 255);

//            // Assert: Verify all 4 kicks are present (MustHit applied because "Drive" tag enabled)
//            var kickEvents = result.PartTrackNoteEvents
//                .Where(e => e.NoteNumber == DrumTrackGenerator.GetMidiNoteNumber(DrumRole.Kick))
//                .ToList();

//            kickEvents.Should().HaveCount(4, "Drive protection layer adds MustHit for all 4 beats");
//        }

//        [Fact]
//        public void ProtectionLayer_WithoutEnabledTags_IsNotApplied()
//        {
//            // Arrange: Protection layer requires "Drive" tag, but segment doesn't enable it
//            var groovePreset = CreateMinimalGroovePreset(
//                kickAnchors: new List<decimal> { 1m, 3m } // Only beats 1,3 in anchors
//            );

//            groovePreset.ProtectionPolicy.HierarchyLayers.Add(new GrooveProtectionLayer
//            {
//                LayerId = "DriveProtection",
//                AppliesWhenTagsAll = new List<string> { "Drive" },
//                IsAdditiveOnly = true,
//                RoleProtections = new Dictionary<string, RoleProtectionSet>
//                {
//                    ["Kick"] = new RoleProtectionSet
//                    {
//                        MustHitOnsets = new List<decimal> { 1m, 2m, 3m, 4m }
//                    }
//                }
//            });

//            var sectionTrack = CreateSectionTrack(totalBars: 1);
//            var barTrack = CreateBarTrack(totalBars: 1);
//            var segmentProfiles = new List<object>
//            {
//                new object
//                {
//                    StartBar = 1,
//                    EndBar = 1,
//                    EnabledProtectionTags = new List<string>() // NO tags enabled
//                }
//            };

//            // Act
//            var result = DrumTrackGenerator.Generate(
//                barTrack,
//                sectionTrack,
//                segmentProfiles,
//                groovePreset,
//                totalBars: 1,
//                midiProgramNumber: 255);

//            // Assert: Verify only anchor kicks (beats 1,3) are present; Drive layer NOT applied
//            var kickEvents = result.PartTrackNoteEvents
//                .Where(e => e.NoteNumber == DrumTrackGenerator.GetMidiNoteNumber(DrumRole.Kick))
//                .ToList();

//            kickEvents.Should().HaveCount(2, "Drive protection layer not applied; only anchor onsets present");
//        }

//        [Fact]
//        public void EmptyProtectionPolicy_ProducesAnchorOnsetsOnly()
//        {
//            // Arrange: No protection layers
//            var groovePreset = CreateMinimalGroovePreset();
//            groovePreset.ProtectionPolicy.HierarchyLayers.Clear();

//            var sectionTrack = CreateSectionTrack(totalBars: 1);
//            var barTrack = CreateBarTrack(totalBars: 1);
//            var segmentProfiles = new List<object>
//            {
//                new object
//                {
//                    StartBar = 1,
//                    EndBar = 1,
//                    EnabledProtectionTags = new List<string>()
//                }
//            };

//            // Act
//            var result = DrumTrackGenerator.Generate(
//                barTrack,
//                sectionTrack,
//                segmentProfiles,
//                groovePreset,
//                totalBars: 1,
//                midiProgramNumber: 255);

//            // Assert: Verify anchor onsets are emitted (kicks: 1,3; snares: 2,4; hats: 8)
//            var totalEvents = result.PartTrackNoteEvents.Count;
//            totalEvents.Should().BeGreaterThan(0, "anchor layer should produce onsets");

//            var kickEvents = result.PartTrackNoteEvents
//                .Where(e => e.NoteNumber == DrumTrackGenerator.GetMidiNoteNumber(DrumRole.Kick))
//                .ToList();
//            kickEvents.Should().HaveCount(2, "anchor layer has kick on beats 1,3");
//        }

//        #endregion
//    }
//}
