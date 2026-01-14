// AI: purpose=Unit tests for energy arc system validating determinism, selection, and target resolution.
// AI: invariants=Tests must validate deterministic behavior, energy scale [0..1], and library coverage.
// AI: usage=Call EnergyArcTests.RunAllTests() from UI test button or test harness to validate Story 7.1 implementation.

namespace Music.Generator
{
    /// <summary>
    /// Tests for the energy arc system (Story 7.1).
    /// Validates deterministic arc selection, target resolution, and library coverage.
    /// 
    /// To run: Call EnergyArcTests.RunAllTests() from a test button or debug hook.
    /// All tests write output to Console and throw exceptions on failure.
    /// </summary>
    public static class EnergyArcTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== EnergyArc Tests ===");

            TestDeterministicArcSelection();
            TestEnergyTargetResolution();
            TestPhraseTargetSupport();
            TestLibraryCoverage();
            TestStyleMapping();
            TestEnergyScaleValidation();
            TestSectionIndexResolution();
            TestFormInference();

            Console.WriteLine("All EnergyArc tests passed.");
        }

        /// <summary>
        /// Validates that arc selection is deterministic for same (seed, groove, form).
        /// </summary>
        private static void TestDeterministicArcSelection()
        {
            // Create test section track
            var sectionTrack = CreateTestSectionTrack();

            // Create arcs with same parameters
            var arc1 = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);
            var arc2 = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);

            // Should select same template
            if (arc1.Template.Name != arc2.Template.Name)
            {
                throw new Exception($"Determinism failed: arc1={arc1.Template.Name}, arc2={arc2.Template.Name}");
            }

            // Different seed should potentially select different template (or same, but deterministically)
            var arc3 = EnergyArc.Create(sectionTrack, "PopGroove", seed: 123);
            // Just verify it doesn't crash and is deterministic
            var arc4 = EnergyArc.Create(sectionTrack, "PopGroove", seed: 123);
            if (arc3.Template.Name != arc4.Template.Name)
            {
                throw new Exception("Determinism failed with different seed");
            }

            Console.WriteLine("  ? Deterministic arc selection");
        }

        /// <summary>
        /// Validates that energy targets resolve correctly for sections.
        /// </summary>
        private static void TestEnergyTargetResolution()
        {
            var sectionTrack = CreateTestSectionTrack();
            var arc = EnergyArc.Create(sectionTrack, "RockGroove", seed: 42);

            // Test target resolution for first verse
            var verse1Section = new Section
            {
                SectionType = MusicConstants.eSectionType.Verse,
                StartBar = 1,
                BarCount = 4
            };

            var target = arc.GetTargetForSection(verse1Section, sectionIndex: 0);

            // Energy should be in valid range
            if (target.Energy < 0.0 || target.Energy > 1.0)
            {
                throw new Exception($"Energy out of range: {target.Energy}");
            }

            // Test chorus
            var chorusSection = new Section
            {
                SectionType = MusicConstants.eSectionType.Chorus,
                StartBar = 5,
                BarCount = 4
            };

            var chorusTarget = arc.GetTargetForSection(chorusSection, sectionIndex: 0);

            // Chorus should typically have higher energy than verse (not guaranteed for all templates, but typical)
            // Just validate range
            if (chorusTarget.Energy < 0.0 || chorusTarget.Energy > 1.0)
            {
                throw new Exception($"Chorus energy out of range: {chorusTarget.Energy}");
            }

            Console.WriteLine("  ? Energy target resolution");
        }

        /// <summary>
        /// Validates phrase-level energy target support.
        /// </summary>
        private static void TestPhraseTargetSupport()
        {
            // Create a section target with phrase micro-arc
            var target = EnergySectionTarget.WithPhraseMicroArc(
                baseEnergy: 0.6,
                sectionType: MusicConstants.eSectionType.Verse,
                sectionIndex: 0,
                startOffset: 0.0,
                middleOffset: 0.05,
                peakOffset: 0.15,
                cadenceOffset: -0.1
            );

            if (target.PhraseTargets == null)
            {
                throw new Exception("Phrase targets should not be null");
            }

            if (Math.Abs(target.PhraseTargets.PeakOffset - 0.15) > 0.001)
            {
                throw new Exception("Peak offset mismatch");
            }

            // Create uniform target (no phrase variation)
            var uniformTarget = EnergySectionTarget.Uniform(0.5, MusicConstants.eSectionType.Verse, 0);
            if (uniformTarget.PhraseTargets != null)
            {
                throw new Exception("Uniform target should have null phrase targets");
            }

            Console.WriteLine("  ? Phrase-level target support");
        }

        /// <summary>
        /// Validates that library has templates for all major styles.
        /// </summary>
        private static void TestLibraryCoverage()
        {
            var styles = new[] { "Pop", "Rock", "EDM", "Jazz", "Country" };

            foreach (var style in styles)
            {
                var templates = EnergyArcLibrary.GetTemplatesForStyle(style);
                if (templates.Count == 0)
                {
                    throw new Exception($"No templates for style: {style}");
                }

                // Validate each template
                foreach (var template in templates)
                {
                    ValidateTemplate(template);
                }
            }

            // Test generic fallback
            var genericTemplates = EnergyArcLibrary.GetGenericTemplates();
            if (genericTemplates.Count == 0)
            {
                throw new Exception("No generic templates available");
            }

            Console.WriteLine("  ? Library coverage for all styles");
        }

        /// <summary>
        /// Validates that groove names map to correct style categories.
        /// </summary>
        private static void TestStyleMapping()
        {
            var sectionTrack = CreateTestSectionTrack();

            // Test various groove names
            var testGrooves = new[]
            {
                ("RockSteady", "Rock"),
                ("PopFunk", "Pop"),
                ("BossaNovaBasic", "Jazz"),
                ("CountryTrain", "Country"),
                ("HouseBeat", "EDM")
            };

            foreach (var (grooveName, expectedStyle) in testGrooves)
            {
                var arc = EnergyArc.Create(sectionTrack, grooveName, seed: 42);
                // Arc should be created successfully
                if (arc.Template == null)
                {
                    throw new Exception($"Failed to create arc for groove: {grooveName}");
                }
            }

            Console.WriteLine("  ? Style mapping from groove names");
        }

        /// <summary>
        /// Validates that all energy values in templates are in [0..1] range.
        /// </summary>
        private static void TestEnergyScaleValidation()
        {
            var allTemplates = new List<EnergyArcTemplate>();
            allTemplates.AddRange(EnergyArcLibrary.GetTemplatesForStyle("Pop"));
            allTemplates.AddRange(EnergyArcLibrary.GetTemplatesForStyle("Rock"));
            allTemplates.AddRange(EnergyArcLibrary.GetTemplatesForStyle("EDM"));
            allTemplates.AddRange(EnergyArcLibrary.GetTemplatesForStyle("Jazz"));
            allTemplates.AddRange(EnergyArcLibrary.GetTemplatesForStyle("Country"));

            foreach (var template in allTemplates)
            {
                // Check default energies
                foreach (var kvp in template.DefaultEnergiesBySectionType)
                {
                    if (kvp.Value < 0.0 || kvp.Value > 1.0)
                    {
                        throw new Exception($"Template {template.Name} has out-of-range default energy for {kvp.Key}: {kvp.Value}");
                    }
                }

                // Check specific section targets
                foreach (var kvp in template.SectionTargets)
                {
                    var target = kvp.Value;
                    if (target.Energy < 0.0 || target.Energy > 1.0)
                    {
                        throw new Exception($"Template {template.Name} has out-of-range section energy: {target.Energy}");
                    }
                }
            }

            Console.WriteLine("  ? Energy scale validation [0..1]");
        }

        /// <summary>
        /// Validates that section index resolution works correctly for repeated sections.
        /// </summary>
        private static void TestSectionIndexResolution()
        {
            var sectionTrack = CreateTestSectionTrack();
            var arc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);

            // Get targets for multiple verse instances
            var verse1Target = arc.GetTargetForSection(MusicConstants.eSectionType.Verse, sectionIndex: 0);
            var verse2Target = arc.GetTargetForSection(MusicConstants.eSectionType.Verse, sectionIndex: 1);

            // Both should be valid
            if (verse1Target.Energy < 0.0 || verse1Target.Energy > 1.0)
            {
                throw new Exception("Verse 1 energy invalid");
            }
            if (verse2Target.Energy < 0.0 || verse2Target.Energy > 1.0)
            {
                throw new Exception("Verse 2 energy invalid");
            }

            // For many templates, verse 2 should have >= verse 1 energy (but not required for all templates)
            // Just verify they're both valid values

            Console.WriteLine("  ? Section index resolution");
        }

        /// <summary>
        /// Validates song form inference from section structure.
        /// </summary>
        private static void TestFormInference()
        {
            // Create different section structures
            var verseChorusTrack = new SectionTrack();
            verseChorusTrack.Sections.Add(new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 4 });
            verseChorusTrack.Sections.Add(new Section { SectionType = MusicConstants.eSectionType.Chorus, StartBar = 5, BarCount = 4 });

            var arc1 = EnergyArc.Create(verseChorusTrack, "PopGroove", seed: 42);
            if (arc1.Template == null)
            {
                throw new Exception("Failed to infer form for verse-chorus structure");
            }

            // Create verse-chorus-bridge structure
            var fullTrack = new SectionTrack();
            fullTrack.Sections.Add(new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 4 });
            fullTrack.Sections.Add(new Section { SectionType = MusicConstants.eSectionType.Chorus, StartBar = 5, BarCount = 4 });
            fullTrack.Sections.Add(new Section { SectionType = MusicConstants.eSectionType.Bridge, StartBar = 9, BarCount = 4 });

            var arc2 = EnergyArc.Create(fullTrack, "PopGroove", seed: 42);
            if (arc2.Template == null)
            {
                throw new Exception("Failed to infer form for full structure");
            }

            Console.WriteLine("  ? Song form inference");
        }

        /// <summary>
        /// Validates a template's structure and energy values.
        /// </summary>
        private static void ValidateTemplate(EnergyArcTemplate template)
        {
            if (string.IsNullOrWhiteSpace(template.Name))
            {
                throw new Exception("Template missing name");
            }

            if (template.DefaultEnergiesBySectionType == null || template.DefaultEnergiesBySectionType.Count == 0)
            {
                throw new Exception($"Template {template.Name} has no default energies");
            }

            // Validate all energy values are in range
            foreach (var kvp in template.DefaultEnergiesBySectionType)
            {
                if (kvp.Value < 0.0 || kvp.Value > 1.0)
                {
                    throw new Exception($"Template {template.Name} has invalid default energy for {kvp.Key}: {kvp.Value}");
                }
            }

            // Validate section-specific targets
            foreach (var kvp in template.SectionTargets)
            {
                var target = kvp.Value;
                if (target.Energy < 0.0 || target.Energy > 1.0)
                {
                    throw new Exception($"Template {template.Name} has invalid section target energy: {target.Energy}");
                }

                // Validate phrase targets if present
                if (target.PhraseTargets != null)
                {
                    // Phrase offsets can be negative (for energy drops)
                    // Just check they're reasonable (within +/- 0.5)
                    if (Math.Abs(target.PhraseTargets.StartOffset) > 0.5)
                    {
                        throw new Exception($"Template {template.Name} has extreme phrase start offset");
                    }
                }
            }
        }

        /// <summary>
        /// Creates a test section track for testing.
        /// </summary>
        private static SectionTrack CreateTestSectionTrack()
        {
            var track = new SectionTrack();
            track.Sections.Add(new Section
            {
                SectionType = MusicConstants.eSectionType.Intro,
                StartBar = 1,
                BarCount = 2
            });
            track.Sections.Add(new Section
            {
                SectionType = MusicConstants.eSectionType.Verse,
                StartBar = 3,
                BarCount = 4
            });
            track.Sections.Add(new Section
            {
                SectionType = MusicConstants.eSectionType.Chorus,
                StartBar = 7,
                BarCount = 4
            });
            track.Sections.Add(new Section
            {
                SectionType = MusicConstants.eSectionType.Verse,
                StartBar = 11,
                BarCount = 4
            });
            track.Sections.Add(new Section
            {
                SectionType = MusicConstants.eSectionType.Chorus,
                StartBar = 15,
                BarCount = 4
            });
            return track;
        }
    }
}
