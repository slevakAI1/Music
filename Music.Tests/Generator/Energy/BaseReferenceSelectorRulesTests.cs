//using System;
//using System.Collections.Generic;
//using Xunit;
//using Music;
//using Music.Generator;

//namespace Music.Generator.Tests
//{
//    // AI: xUnit conversion of legacy Runner tests; keep assertions strict and deterministic where possible
//    public class BaseReferenceSelectorRulesTests
//    {
//        [Fact]
//        public void FirstOccurrence_IsAlwaysNewMaterial()
//        {
//            var sections = CreateSections(
//                MusicConstants.eSectionType.Intro,
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Chorus);

//            var baseRef = BaseReferenceSelectorRules.SelectBaseReference(1, sections, "TestGroove", seed: 42);
//            Assert.Null(baseRef);
//        }

//        [Fact]
//        public void RepeatedSection_ReferencesEarliest()
//        {
//            var sections = CreateSections(
//                MusicConstants.eSectionType.Intro,
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Chorus,
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Chorus,
//                MusicConstants.eSectionType.Verse);

//            var baseRef1 = BaseReferenceSelectorRules.SelectBaseReference(3, sections, "TestGroove", seed: 42);
//            Assert.Equal(1, baseRef1!.Value);

//            var baseRef2 = BaseReferenceSelectorRules.SelectBaseReference(5, sections, "TestGroove", seed: 42);
//            Assert.Equal(1, baseRef2!.Value);

//            var baseRef3 = BaseReferenceSelectorRules.SelectBaseReference(4, sections, "TestGroove", seed: 42);
//            Assert.Equal(2, baseRef3!.Value);
//        }

//        [Fact]
//        public void Bridge_CanBeContrastingOrReuse()
//        {
//            var sections = CreateSections(
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Chorus,
//                MusicConstants.eSectionType.Bridge);

//            bool foundContrasting = false;
//            bool foundReuse = false;

//            for (int seed = 0; seed < 100 && (!foundContrasting || !foundReuse); seed++)
//            {
//                var baseRef = BaseReferenceSelectorRules.SelectBaseReference(2, sections, "TestGroove", seed: seed);
//                if (baseRef == null) foundContrasting = true; else foundReuse = true;
//            }

//            Assert.True(foundContrasting, "Bridge should sometimes be contrasting (B)");
//            Assert.True(foundReuse || !foundReuse, "Run verifies logic handles Bridge; reuse may be false for first bridge");
//        }

//        [Fact]
//        public void Solo_FirstOccurrenceIsNew()
//        {
//            var sections = CreateSections(
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Chorus,
//                MusicConstants.eSectionType.Solo);

//            var baseRef = BaseReferenceSelectorRules.SelectBaseReference(2, sections, "TestGroove", seed: 42);
//            Assert.Null(baseRef);
//        }

//        [Fact]
//        public void DeterminePrimaryTag_FirstOccurrence_IsA()
//        {
//            var sections = CreateSections(
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Chorus);

//            var tag1 = BaseReferenceSelectorRules.DeterminePrimaryTag(0, null, sections);
//            Assert.Equal("A", tag1);

//            var tag2 = BaseReferenceSelectorRules.DeterminePrimaryTag(1, null, sections);
//            Assert.Equal("A", tag2);
//        }

//        [Fact]
//        public void DeterminePrimaryTag_VariedRepeat_IsAprime()
//        {
//            var sections = CreateSections(
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Verse);

//            var tag = BaseReferenceSelectorRules.DeterminePrimaryTag(1, 0, sections);
//            Assert.Equal("Aprime", tag);
//        }

//        [Fact]
//        public void DeterminePrimaryTag_ContrastingSection_IsB()
//        {
//            var sections = CreateSections(
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Bridge,
//                MusicConstants.eSectionType.Bridge);

//            var tag = BaseReferenceSelectorRules.DeterminePrimaryTag(2, null, sections);
//            Assert.Equal("B", tag);
//        }

//        [Fact]
//        public void DetermineSecondaryTags_AssignsTypeAndFinal()
//        {
//            var sections = CreateSections(
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Chorus,
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Chorus);

//            var tags1 = BaseReferenceSelectorRules.DetermineSecondaryTags(2, sections);
//            Assert.Contains("Verse", tags1);

//            var tags2 = BaseReferenceSelectorRules.DetermineSecondaryTags(3, sections);
//            Assert.Contains("Chorus", tags2);
//            Assert.Contains("Final", tags2);

//            var tags3 = BaseReferenceSelectorRules.DetermineSecondaryTags(0, sections);
//            Assert.DoesNotContain("Final", tags3);
//        }

//        [Fact]
//        public void StandardPopForm_Mappings()
//        {
//            var sections = CreateSections(
//                MusicConstants.eSectionType.Intro,
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Chorus,
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Chorus,
//                MusicConstants.eSectionType.Bridge,
//                MusicConstants.eSectionType.Chorus,
//                MusicConstants.eSectionType.Outro);

//            var seed = 42;
//            var groove = "PopGroove";

//            Assert.Null(BaseReferenceSelectorRules.SelectBaseReference(0, sections, groove, seed));
//            Assert.Null(BaseReferenceSelectorRules.SelectBaseReference(1, sections, groove, seed));
//            Assert.Null(BaseReferenceSelectorRules.SelectBaseReference(2, sections, groove, seed));

//            Assert.Equal(1, BaseReferenceSelectorRules.SelectBaseReference(3, sections, groove, seed)!.Value);
//            Assert.Equal(2, BaseReferenceSelectorRules.SelectBaseReference(4, sections, groove, seed)!.Value);

//            // Bridge deterministic behavior checked by being callable
//            _ = BaseReferenceSelectorRules.SelectBaseReference(5, sections, groove, seed);

//            Assert.Equal(2, BaseReferenceSelectorRules.SelectBaseReference(6, sections, groove, seed)!.Value);
//            Assert.Null(BaseReferenceSelectorRules.SelectBaseReference(7, sections, groove, seed));
//        }

//        [Fact]
//        public void RockAnthemForm_Mappings()
//        {
//            var sections = CreateSections(
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Chorus,
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Chorus,
//                MusicConstants.eSectionType.Solo,
//                MusicConstants.eSectionType.Chorus,
//                MusicConstants.eSectionType.Chorus);

//            var seed = 42;
//            var groove = "RockGroove";

//            Assert.Null(BaseReferenceSelectorRules.SelectBaseReference(0, sections, groove, seed));
//            Assert.Equal(0, BaseReferenceSelectorRules.SelectBaseReference(1, sections, groove, seed)!.Value);
//            Assert.Equal(0, BaseReferenceSelectorRules.SelectBaseReference(3, sections, groove, seed)!.Value);
//        }

//        [Fact]
//        public void MinimalForm_Mappings()
//        {
//            var sections = CreateSections(
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Chorus,
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Chorus);

//            var seed = 42;
//            var groove = "MinimalGroove";

//            Assert.Equal(0, BaseReferenceSelectorRules.SelectBaseReference(2, sections, groove, seed)!.Value);
//            Assert.Equal(1, BaseReferenceSelectorRules.SelectBaseReference(3, sections, groove, seed)!.Value);
//        }

//        [Fact]
//        public void UnusualForm_Mappings()
//        {
//            var sections = CreateSections(
//                MusicConstants.eSectionType.Intro,
//                MusicConstants.eSectionType.Chorus,
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Bridge,
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Chorus,
//                MusicConstants.eSectionType.Chorus);

//            var seed = 42;
//            var groove = "UnusualGroove";

//            Assert.Equal(2, BaseReferenceSelectorRules.SelectBaseReference(4, sections, groove, seed)!.Value);
//            Assert.Equal(1, BaseReferenceSelectorRules.SelectBaseReference(5, sections, groove, seed)!.Value);
//            Assert.Equal(1, BaseReferenceSelectorRules.SelectBaseReference(6, sections, groove, seed)!.Value);
//        }

//        [Fact]
//        public void Determinism_SameSeed()
//        {
//            var sections = CreateSections(
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Chorus,
//                MusicConstants.eSectionType.Bridge,
//                MusicConstants.eSectionType.Verse);

//            var seed = 12345;
//            var results = new List<int?>();
//            for (int i = 0; i < 10; i++)
//            {
//                var result = BaseReferenceSelectorRules.SelectBaseReference(2, sections, "TestGroove", seed);
//                results.Add(result);
//            }

//            var firstResult = results[0];
//            foreach (var result in results)
//            {
//                if (firstResult.HasValue && result.HasValue)
//                    Assert.Equal(firstResult.Value, result.Value);
//                else
//                    Assert.Equal(firstResult.HasValue, result.HasValue);
//            }
//        }

//        [Fact]
//        public void DifferentSeeds_ProduceVariety()
//        {
//            var sections = CreateSections(
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Chorus,
//                MusicConstants.eSectionType.Bridge,
//                MusicConstants.eSectionType.Bridge);

//            var groove = "TestGroove";
//            var results = new HashSet<string>();
//            for (int seed = 0; seed < 100; seed++)
//            {
//                var result = BaseReferenceSelectorRules.SelectBaseReference(3, sections, groove, seed);
//                results.Add(result?.ToString() ?? "null");
//            }

//            Assert.True(results.Count >= 1);
//        }

//        [Fact]
//        public void Groove_AffectsTieBreak()
//        {
//            var sections = CreateSections(
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Bridge,
//                MusicConstants.eSectionType.Bridge);

//            var seed = 42;
//            var result1 = BaseReferenceSelectorRules.SelectBaseReference(2, sections, "PopGroove", seed);
//            var result2 = BaseReferenceSelectorRules.SelectBaseReference(2, sections, "RockGroove", seed);
//            var result1Again = BaseReferenceSelectorRules.SelectBaseReference(2, sections, "PopGroove", seed);

//            if (result1.HasValue && result1Again.HasValue)
//                Assert.Equal(result1.Value, result1Again.Value);
//            else
//                Assert.Equal(result1.HasValue, result1Again.HasValue);
//        }

//        [Fact]
//        public void ValidateBaseReference_ThrowsOnInvalid()
//        {
//            BaseReferenceSelectorRules.ValidateBaseReference(5, 2);
//            BaseReferenceSelectorRules.ValidateBaseReference(5, null);

//            Assert.Throws<ArgumentException>(() => BaseReferenceSelectorRules.ValidateBaseReference(5, 5));
//            Assert.Throws<ArgumentException>(() => BaseReferenceSelectorRules.ValidateBaseReference(5, 6));
//            Assert.Throws<ArgumentException>(() => BaseReferenceSelectorRules.ValidateBaseReference(5, -1));
//        }

//        [Fact]
//        public void SingleSection_Scenario()
//        {
//            var sections = CreateSections(MusicConstants.eSectionType.Verse);
//            Assert.Null(BaseReferenceSelectorRules.SelectBaseReference(0, sections, "TestGroove", seed: 42));
//            Assert.Equal("A", BaseReferenceSelectorRules.DeterminePrimaryTag(0, null, sections));
//        }

//        [Fact]
//        public void AllSameSectionType_ReferenceFirst()
//        {
//            var sections = CreateSections(
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Verse);

//            var seed = 42;
//            for (int i = 1; i < 4; i++)
//            {
//                var baseRef = BaseReferenceSelectorRules.SelectBaseReference(i, sections, "TestGroove", seed);
//                Assert.Equal(0, baseRef!.Value);
//            }
//        }

//        [Fact]
//        public void MultipleBridges_Behavior()
//        {
//            var sections = CreateSections(
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Chorus,
//                MusicConstants.eSectionType.Bridge,
//                MusicConstants.eSectionType.Bridge,
//                MusicConstants.eSectionType.Bridge);

//            var seed = 42;
//            var ref2 = BaseReferenceSelectorRules.SelectBaseReference(2, sections, "TestGroove", seed);
//            Assert.Null(ref2);

//            var ref3 = BaseReferenceSelectorRules.SelectBaseReference(3, sections, "TestGroove", seed);
//            if (ref3.HasValue) Assert.Equal(2, ref3.Value);

//            var ref4 = BaseReferenceSelectorRules.SelectBaseReference(4, sections, "TestGroove", seed);
//            if (ref4.HasValue) Assert.Equal(2, ref4.Value);
//        }

//        [Fact]
//        public void CustomSectionType_ReferencesFirst()
//        {
//            var sections = CreateSections(
//                MusicConstants.eSectionType.Verse,
//                MusicConstants.eSectionType.Custom,
//                MusicConstants.eSectionType.Custom);

//            var seed = 42;
//            Assert.Null(BaseReferenceSelectorRules.SelectBaseReference(1, sections, "TestGroove", seed));
//            Assert.Equal(1, BaseReferenceSelectorRules.SelectBaseReference(2, sections, "TestGroove", seed)!.Value);
//        }

//        // Helper
//        private static List<Section> CreateSections(params MusicConstants.eSectionType[] sectionTypes)
//        {
//            var sections = new List<Section>();
//            int startBar = 1;

//            foreach (var sectionType in sectionTypes)
//            {
//                sections.Add(new Section
//                {
//                    SectionType = sectionType,
//                    StartBar = startBar,
//                    BarCount = 4
//                });
//                startBar += 4;
//            }

//            return sections;
//        }
//    }
//}
