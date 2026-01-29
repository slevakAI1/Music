using Music.Generator.Agents.Drums;

namespace Music.Generator.Groove
{
    public static class GrooveSetupFactory
    {
        /// <summary>
        /// Builds a complete PopRockBasic groove definition + segment profiles.
        /// This is DEFINITIONS ONLY: no selection algorithms yet.
        /// </summary>
        /// <param name="sectionTrack">The SectionTrack containing the song sections.</param>
        /// <param name="segmentProfiles">Output: segment profiles for each section.</param>
        /// <param name="beatsPerBar">Beats per bar for the groove (default 4/4 time).</param>
        public static GroovePresetDefinition BuildPopRockBasicGrooveForTestSong(
            SectionTrack sectionTrack,
            out IReadOnlyList<SegmentGrooveProfile> segmentProfiles,
            int beatsPerBar = 4)
        {
            // =========================
            // INPUTS (everything here)
            // =========================

            // Song form (from actual sections) ------------------------------------
            var sections = ConvertSectionsToSpecs(sectionTrack.Sections, beatsPerBar);

            // Groove identity ----------------------------------------------------
            string presetName = "PopRockBasic";                              // Standard
            string styleFamily = "PopRock";                                  // Standard
            var identityTags = new List<string>                              // Standard
            {
                "Backbeat",
                "Straight8",
                "PopRock",
                "4_4"
            };

            // Subdivision / feel -------------------------------------------------
            var allowedSubdivisions = AllowedSubdivision.Quarter             // Standard
                                    | AllowedSubdivision.Eighth              // Standard
                                    | AllowedSubdivision.Sixteenth;          // Intelligent choice (allow 16ths for fills/drive)
            GrooveFeel feel = GrooveFeel.Straight;                           // Standard (for this preset)
            double swingAmount01 = 0.0;                                      // Standard (Straight)

            // Phrase hooks (fill windows) ---------------------------------------
            bool allowPhraseEndFills = true;                                 // Intelligent choice
            int phraseEndBarsWindow = 1;                                     // Intelligent choice
            bool allowSectionEndFills = true;                                // Intelligent choice
            int sectionEndBarsWindow = 1;                                    // Intelligent choice
            bool protectDownbeatOnPhraseEnd = true;                          // Standard
            bool protectBackbeatOnPhraseEnd = true;                          // Standard
            var enabledFillTags = new List<string> { "Fill", "Pickup" };     // Intelligent choice

            // Role constraints / vocab ------------------------------------------
            // (These are caps + constraints, not “targets”.)
            bool allowSyncopation = true;                                    // Intelligent choice
            bool allowAnticipation = true;                                   // Intelligent choice

            // Protection hierarchy depth ----------------------------------------
            // [0]=parent base, [1]=child refine, [2]=grandchild refine
            int hierarchyDepth = 3;                                          // Standard

            // Segment defaults (targets; later tuned by algorithms/UI) ----------
            // These are “what we want”, not hard caps.
            double verseCompDensity01 = 0.50;                                // Intelligent choice
            double chorusCompDensity01 = 0.70;                               // Intelligent choice
            double verseDrumDensity01 = 0.70;                                // Intelligent choice
            double chorusDrumDensity01 = 0.85;                               // Intelligent choice
            double verseBassDensity01 = 0.45;                                // Intelligent choice
            double chorusBassDensity01 = 0.55;                               // Intelligent choice

            // =========================
            // BUILD (helpers)
            // =========================

            var identity = BuildPopRockBasicIdentity(
                presetName, styleFamily, beatsPerBar, identityTags);

            var anchor = BuildPopRockBasicAnchorLayer(); // your hardcoded onsets

            var protectionPolicy = BuildPopRockBasicProtectionPolicy(
                identity,
                hierarchyDepth,
                allowedSubdivisions,
                feel,
                swingAmount01,
                allowPhraseEndFills,
                phraseEndBarsWindow,
                allowSectionEndFills,
                sectionEndBarsWindow,
                protectDownbeatOnPhraseEnd,
                protectBackbeatOnPhraseEnd,
                enabledFillTags,
                allowSyncopation,
                allowAnticipation);

            var variationCatalog = BuildPopRockBasicVariationCatalog(identity, hierarchyDepth);

            segmentProfiles = BuildSegmentProfilesForTestSong(
                sections,
                verseCompDensity01,
                chorusCompDensity01,
                verseDrumDensity01,
                chorusDrumDensity01,
                verseBassDensity01,
                chorusBassDensity01);

            return new GroovePresetDefinition
            {
                Identity = identity,
                AnchorLayer = anchor,
                ProtectionPolicy = protectionPolicy,
                VariationCatalog = variationCatalog
            };
        }

        // ---------------------------------------------------------------------
        // HELPERS
        // ---------------------------------------------------------------------

        private static GroovePresetIdentity BuildPopRockBasicIdentity(
            string presetName,
            string styleFamily,
            int beatsPerBar,
            List<string> tags)
        {
            return new GroovePresetIdentity
            {
                Name = presetName,
                StyleFamily = styleFamily,
                BeatsPerBar = beatsPerBar,
                Tags = tags,
                CompatibilityTags = new List<string>
                {
                    "NoTripletsByDefault",
                    "BackbeatProtected",
                    "SafeForSimpleMotifs"
                }
            };
        }

        private static GrooveInstanceLayer BuildPopRockBasicAnchorLayer()
        {
            // Your current PopRockBasic anchors.
            return new GrooveInstanceLayer
            {
                KickOnsets = new List<decimal> { 1m, 3m },
                SnareOnsets = new List<decimal> { 2m, 4m },
                HatOnsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m },
                BassOnsets = new List<decimal> { 1m, 3m },
                CompOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },
                PadsOnsets = new List<decimal> { 1m, 3m }
            };
        }

        private static GrooveProtectionPolicy BuildPopRockBasicProtectionPolicy(
            GroovePresetIdentity identity,
            int hierarchyDepth,
            AllowedSubdivision allowedSubdivisions,
            GrooveFeel feel,
            double swingAmount01,
            bool allowPhraseEndFills,
            int phraseEndBarsWindow,
            bool allowSectionEndFills,
            int sectionEndBarsWindow,
            bool protectDownbeatOnPhraseEnd,
            bool protectBackbeatOnPhraseEnd,
            List<string> enabledFillTags,
            bool allowSyncopation,
            bool allowAnticipation)
        {
            var policy = new GrooveProtectionPolicy
            {
                Identity = identity,

                // (#1) subdivision / feel
                SubdivisionPolicy = new GrooveSubdivisionPolicy
                {
                    AllowedSubdivisions = allowedSubdivisions,
                    Feel = feel,
                    SwingAmount01 = swingAmount01
                },

                // (#4) role constraints / vocab
                RoleConstraintPolicy = BuildDefaultRoleConstraints(allowSyncopation, allowAnticipation),

                // (#7) phrase hooks
                PhraseHookPolicy = new GroovePhraseHookPolicy
                {
                    AllowFillsAtPhraseEnd = allowPhraseEndFills,
                    PhraseEndBarsWindow = phraseEndBarsWindow,
                    AllowFillsAtSectionEnd = allowSectionEndFills,
                    SectionEndBarsWindow = sectionEndBarsWindow,
                    ProtectDownbeatOnPhraseEnd = protectDownbeatOnPhraseEnd,
                    ProtectBackbeatOnPhraseEnd = protectBackbeatOnPhraseEnd,
                    EnabledFillTags = enabledFillTags
                },

                // (#6) timing feel (keep neutral for now)
                TimingPolicy = new GrooveTimingPolicy
                {
                    MaxAbsTimingBiasTicks = 0, // set >0 later when you add microtiming
                    RoleTimingFeel = new Dictionary<string, TimingFeel>
                    {
                        [GrooveRoles.DrumKit] = TimingFeel.OnTop,
                        [GrooveRoles.Bass] = TimingFeel.OnTop,
                        [GrooveRoles.Comp] = TimingFeel.OnTop,
                        [GrooveRoles.Pads] = TimingFeel.OnTop
                    },
                    RoleTimingBiasTicks = new Dictionary<string, int>
                    {
                        [GrooveRoles.DrumKit] = 0,
                        [GrooveRoles.Bass] = 0,
                        [GrooveRoles.Comp] = 0,
                        [GrooveRoles.Pads] = 0
                    }
                },

                // (#8) orchestration defaults by section type
                OrchestrationPolicy = BuildDefaultOrchestrationPolicy(),

                // override merge policy (conservative defaults)
                MergePolicy = new GrooveOverrideMergePolicy
                {
                    OverrideReplacesLists = false,
                    OverrideCanRemoveProtectedOnsets = false,
                    OverrideCanRelaxConstraints = false,
                    OverrideCanChangeFeel = false
                }
            };

            // Protection hierarchy layers: base -> refined -> more refined.
            policy.HierarchyLayers = BuildProtectionHierarchyLayers(hierarchyDepth);

            return policy;
        }

        private static GrooveRoleConstraintPolicy BuildDefaultRoleConstraints(
            bool allowSyncopation,
            bool allowAnticipation)
        {
            return new GrooveRoleConstraintPolicy
            {
                RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
                {
                    // Drums (conceptually): allow syncopation/anticipation as optional events later
                    [GrooveRoles.DrumKit] = new RoleRhythmVocabulary
                    {
                        MaxHitsPerBar = 32,
                        MaxHitsPerBeat = 4,
                        AllowSyncopation = allowSyncopation,
                        AllowAnticipation = allowAnticipation,
                        SnapStrongBeatsToChordTones = false
                    },

                    // Bass: conservative
                    [GrooveRoles.Bass] = new RoleRhythmVocabulary
                    {
                        MaxHitsPerBar = 8,
                        MaxHitsPerBeat = 2,
                        AllowSyncopation = allowSyncopation,
                        AllowAnticipation = allowAnticipation,
                        SnapStrongBeatsToChordTones = true
                    },

                    // Comp: moderate
                    [GrooveRoles.Comp] = new RoleRhythmVocabulary
                    {
                        MaxHitsPerBar = 12,
                        MaxHitsPerBeat = 3,
                        AllowSyncopation = allowSyncopation,
                        AllowAnticipation = allowAnticipation,
                        SnapStrongBeatsToChordTones = true
                    },

                    // Pads: sparse
                    [GrooveRoles.Pads] = new RoleRhythmVocabulary
                    {
                        MaxHitsPerBar = 4,
                        MaxHitsPerBeat = 1,
                        AllowSyncopation = false,
                        AllowAnticipation = false,
                        SnapStrongBeatsToChordTones = true
                    }
                },

                // Hard caps (can be enforced later)
                RoleMaxDensityPerBar = new Dictionary<string, int>
                {
                    [GrooveRoles.DrumKit] = 32,
                    [GrooveRoles.Bass] = 8,
                    [GrooveRoles.Comp] = 12,
                    [GrooveRoles.Pads] = 4
                },

                // Sustain caps (for pads/keys)
                RoleMaxSustainSlots = new Dictionary<string, int>
                {
                    [GrooveRoles.Pads] = 2,
                    [GrooveRoles.Comp] = 2
                }
            };
        }

        private static GrooveOrchestrationPolicy BuildDefaultOrchestrationPolicy()
        {
            // Lightweight “role present” defaults by section type.
            return new GrooveOrchestrationPolicy
            {
                DefaultsBySectionType = new List<SectionRolePresenceDefaults>
                {
                    new SectionRolePresenceDefaults
                    {
                        SectionType = "Intro",
                        RolePresent = new Dictionary<string, bool>
                        {
                            [GrooveRoles.DrumKit] = true,
                            [GrooveRoles.Bass] = true,
                            [GrooveRoles.Comp] = true,
                            [GrooveRoles.Pads] = false
                        }
                    },
                    new SectionRolePresenceDefaults
                    {
                        SectionType = "Verse",
                        RolePresent = new Dictionary<string, bool>
                        {
                            [GrooveRoles.DrumKit] = true,
                            [GrooveRoles.Bass] = true,
                            [GrooveRoles.Comp] = true,
                            [GrooveRoles.Pads] = true,
                            [GrooveRoles.OpenHat] = true
                        }
                    },
                    new SectionRolePresenceDefaults
                    {
                        SectionType = "Chorus",
                        RolePresent = new Dictionary<string, bool>
                        {
                            [GrooveRoles.DrumKit] = true,
                            [GrooveRoles.Bass] = true,
                            [GrooveRoles.Comp] = true,
                            [GrooveRoles.Pads] = true
                        },
                        RoleRegisterLiftSemitones = new Dictionary<string, int>
                        {
                            [GrooveRoles.Comp] = 0,
                            [GrooveRoles.Pads] = 0
                        },
                        RoleDensityMultiplier = new Dictionary<string, double>
                        {
                            [GrooveRoles.Comp] = 1.1,
                            [GrooveRoles.Pads] = 1.1
                        }
                    },
                    new SectionRolePresenceDefaults
                    {
                        SectionType = "Bridge",
                        RolePresent = new Dictionary<string, bool>
                        {
                            [GrooveRoles.DrumKit] = true,
                            [GrooveRoles.Bass] = true,
                            [GrooveRoles.Comp] = true,
                            [GrooveRoles.Pads] = true
                        }
                    },
                    new SectionRolePresenceDefaults
                    {
                        SectionType = "Outro",
                        RolePresent = new Dictionary<string, bool>
                        {
                            [GrooveRoles.DrumKit] = true,
                            [GrooveRoles.Bass] = true,
                            [GrooveRoles.Comp] = true,
                            [GrooveRoles.Pads] = false
                        }
                    }
                }
            };
        }

        private static List<GrooveProtectionLayer> BuildProtectionHierarchyLayers(int hierarchyDepth)
        {
            // Base protections (PopRock backbeat + anchors).
            // Child/grandchild layers are placeholders now; you’ll refine later.
            var layers = new List<GrooveProtectionLayer>();

            for (int i = 0; i < hierarchyDepth; i++)
            {
                var layer = new GrooveProtectionLayer
                {
                    LayerId = i switch
                    {
                        0 => "Base",
                        1 => "PopRockRefine",
                        _ => "PopRockBasicRefine"
                    },
                    IsAdditiveOnly = true,
                    RoleProtections = new Dictionary<string, RoleProtectionSet>()
                };

                // Only base layer defines must-hit anchors.
                if (i == 0)
                {
                    layer.RoleProtections[GrooveRoles.Kick] = new RoleProtectionSet
                    {
                        MustHitOnsets = new List<decimal> { 1m, 3m },
                        NeverRemoveOnsets = new List<decimal> { 1m },
                    };
                    layer.RoleProtections[GrooveRoles.Snare] = new RoleProtectionSet
                    {
                        MustHitOnsets = new List<decimal> { 2m, 4m },
                        NeverRemoveOnsets = new List<decimal> { 2m, 4m }
                    };
                    layer.RoleProtections[GrooveRoles.ClosedHat] = new RoleProtectionSet
                    {
                        ProtectedOnsets = new List<decimal> { 1m, 2m, 3m, 4m }
                    };
                    layer.RoleProtections[GrooveRoles.OpenHat] = new RoleProtectionSet
                    {
                        ProtectedOnsets = new List<decimal>()
                    };
                }

                layers.Add(layer);
            }

            return layers;
        }

        private static GrooveVariationCatalog BuildPopRockBasicVariationCatalog(
            GroovePresetIdentity identity,
            int hierarchyDepth)
        {
            var catalog = new GrooveVariationCatalog
            {
                Identity = identity,
                KnownTags = new List<string>
                {
                    "Core",
                    "Drive",
                    "Fill",
                    "Pickup",
                    "Drop",
                    "GhostSnare",
                    "OpenHat"
                }
            };

            // Candidate groups MUST include actual candidates; otherwise enabling tags does nothing.
            var baseGroups = new List<DrumCandidateGroup>
            {
                new DrumCandidateGroup
                {
                    GroupId = "Drive_Hats16",
                    GroupTags = new List<string> { "Drive" },
                    MaxAddsPerBar = 4,
                    BaseProbabilityBias = 1.0,
                    Candidates = new List<DrumOnsetCandidate>
                    {
                        // Add 16th-like pushes by inserting between existing 8ths (you already allow decimal beats).
                        new DrumOnsetCandidate { Role = GrooveRoles.ClosedHat, OnsetBeat = 1.25m, Strength = OnsetStrength.Offbeat, MaxAddsPerBar = 1, ProbabilityBias = 0.6, Tags = new List<string>{ "Drive" } },
                        new DrumOnsetCandidate { Role = GrooveRoles.ClosedHat, OnsetBeat = 2.25m, Strength = OnsetStrength.Offbeat, MaxAddsPerBar = 1, ProbabilityBias = 0.6, Tags = new List<string>{ "Drive" } },
                        new DrumOnsetCandidate { Role = GrooveRoles.ClosedHat, OnsetBeat = 3.25m, Strength = OnsetStrength.Offbeat, MaxAddsPerBar = 1, ProbabilityBias = 0.6, Tags = new List<string>{ "Drive" } },
                        new DrumOnsetCandidate { Role = GrooveRoles.ClosedHat, OnsetBeat = 4.25m, Strength = OnsetStrength.Offbeat, MaxAddsPerBar = 1, ProbabilityBias = 0.6, Tags = new List<string>{ "Drive" } },
                    }
                },
                new DrumCandidateGroup
                {
                    GroupId = "Pickups",
                    GroupTags = new List<string> { "Pickup" },
                    MaxAddsPerBar = 2,
                    BaseProbabilityBias = 0.6,
                    Candidates = new List<DrumOnsetCandidate>
                    {
                        new DrumOnsetCandidate { Role = GrooveRoles.Snare, OnsetBeat = 4.75m, Strength = OnsetStrength.Pickup, MaxAddsPerBar = 1, ProbabilityBias = 0.35, Tags = new List<string>{ "Pickup", "Fill" } },
                        new DrumOnsetCandidate { Role = GrooveRoles.Kick,  OnsetBeat = 4.75m, Strength = OnsetStrength.Pickup, MaxAddsPerBar = 1, ProbabilityBias = 0.35, Tags = new List<string>{ "Pickup", "Fill" } },
                    }
                },
                new DrumCandidateGroup
                {
                    GroupId = "GhostSnare",
                    GroupTags = new List<string> { "GhostSnare" },
                    MaxAddsPerBar = 2,
                    BaseProbabilityBias = 0.5,
                    Candidates = new List<DrumOnsetCandidate>
                    {
                        new DrumOnsetCandidate { Role = GrooveRoles.Snare, OnsetBeat = 1.5m, Strength = OnsetStrength.Ghost, MaxAddsPerBar = 1, ProbabilityBias = 0.25, Tags = new List<string>{ "GhostSnare" } },
                        new DrumOnsetCandidate { Role = GrooveRoles.Snare, OnsetBeat = 3.5m, Strength = OnsetStrength.Ghost, MaxAddsPerBar = 1, ProbabilityBias = 0.25, Tags = new List<string>{ "GhostSnare" } },
                    }
                },
                new DrumCandidateGroup
                {
                    GroupId = "OpenHat",
                    GroupTags = new List<string> { "OpenHat", "Fill" },
                    MaxAddsPerBar = 1,
                    BaseProbabilityBias = 0.35,
                    Candidates = new List<DrumOnsetCandidate>
                    {
                        new DrumOnsetCandidate { Role = GrooveRoles.OpenHat, OnsetBeat = 4.5m, Strength = OnsetStrength.Offbeat, MaxAddsPerBar = 1, ProbabilityBias = 0.3, Tags = new List<string>{ "OpenHat", "Fill" } },
                    }
                }
            };

            var layers = new List<GrooveVariationLayer>();
            for (int i = 0; i < hierarchyDepth; i++)
            {
                layers.Add(new GrooveVariationLayer
                {
                    LayerId = i switch
                    {
                        0 => "BaseCandidates",
                        1 => "PopRockRefineCandidates",
                        _ => "PopRockBasicRefineCandidates"
                    },
                    IsAdditiveOnly = true,
                    CandidateGroups = baseGroups
                });
            }

            catalog.HierarchyLayers = layers;
            return catalog;
        }

        private static IReadOnlyList<SegmentGrooveProfile> BuildSegmentProfilesForTestSong(
            IReadOnlyList<SectionSpec> sections,
            double verseCompDensity01,
            double chorusCompDensity01,
            double verseDrumDensity01,
            double chorusDrumDensity01,
            double verseBassDensity01,
            double chorusBassDensity01)
        {
            var result = new List<SegmentGrooveProfile>();

            int barCursor = 1;
            for (int i = 0; i < sections.Count; i++)
            {
                var s = sections[i];

                // Default tag enables by section type (later: rules/user/seed-driven).
                var enabledTags = GetDefaultEnabledVariationTagsForSection(s.SectionType);

                // Density targets (these are targets, not hard caps)
                var (drums, comp, bass) = s.SectionType.Equals("Chorus", StringComparison.OrdinalIgnoreCase)
                    ? (chorusDrumDensity01, chorusCompDensity01, chorusBassDensity01)
                    : (verseDrumDensity01, verseCompDensity01, verseBassDensity01);

                // Basic “max events per bar” defaults: set non-zero so your later systems don’t read “disabled”.
                int maxDrumEvents = 32;
                int maxCompEvents = 12;
                int maxBassEvents = 8;
                int maxHatEvents = 16;

                result.Add(new SegmentGrooveProfile
                {
                    SegmentId = $"{s.SectionType}_{i}",
                    SectionIndex = i,
                    PhraseIndex = null,
                    StartBar = barCursor,
                    EndBar = barCursor + s.Bars - 1,

                    EnabledVariationTags = enabledTags,


                    // THIS WILL BE A TEST FOR AFTER Story 9 implemented
                    //EnabledProtectionTags = new List<string>(),
                    EnabledProtectionTags = new List<string> { "Drive" }, // enables PopRockRefine layer for that segment



                    DensityTargets = new List<RoleDensityTarget>
                    {
                        new RoleDensityTarget { Role = GrooveRoles.DrumKit, Density01 = drums, MaxEventsPerBar = maxDrumEvents },
                        new RoleDensityTarget { Role = GrooveRoles.Comp,   Density01 = comp,  MaxEventsPerBar = maxCompEvents },
                        new RoleDensityTarget { Role = GrooveRoles.Bass,   Density01 = bass,  MaxEventsPerBar = maxBassEvents },
                        new RoleDensityTarget { Role = GrooveRoles.ClosedHat, Density01 = drums, MaxEventsPerBar = maxHatEvents },
                    },

                    OverrideFeel = null,
                    OverrideSwingAmount01 = null
                });

                barCursor += s.Bars;
            }

            return result;
        }

        private static List<string> GetDefaultEnabledVariationTagsForSection(string sectionType)
        {
            // Keep simple now. This is just “what tags are allowed”.
            return sectionType.ToLowerInvariant() switch
            {
                "intro"  => new List<string> { "Core" },
                "verse"  => new List<string> { "Core", "GhostSnare" },
                "chorus" => new List<string> { "Core", "Drive", "OpenHat" },
                "bridge" => new List<string> { "Core", "Fill", "Pickup" },
                "outro"  => new List<string> { "Core", "Drop" },
                _        => new List<string> { "Core" }
            };
        }

        // Local helper type so inputs stay in one place in the main method
        private sealed record SectionSpec(string SectionType, int Bars, int BeatsPerBar);

        /// <summary>
        /// Converts Section objects from SectionTrack to internal SectionSpec format.
        /// </summary>
        private static List<SectionSpec> ConvertSectionsToSpecs(List<Section> sections, int beatsPerBar)
        {
            var result = new List<SectionSpec>();
            foreach (var section in sections)
            {
                // Use Name if available, otherwise convert enum to string
                string sectionType = !string.IsNullOrWhiteSpace(section.Name)
                    ? section.Name
                    : section.SectionType.ToString();

                result.Add(new SectionSpec(sectionType, section.BarCount, beatsPerBar));
            }
            return result;
        }
    }
}

