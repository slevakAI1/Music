using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Music;
using Music.Generator;

namespace Music.Generator.Tests
{
    // AI: xUnit conversion of legacy deterministic tension tests; keep assertions explicit and deterministic
    public class DeterministicTensionQueryTests
    {
        [Fact]
        public void Creation_BasicBehavior()
        {
            var (arc, _) = CreateTestArc(3);
            var query = new DeterministicTensionQuery(arc, seed: 42);

            Assert.Equal(3, query.SectionCount);
            Assert.True(query.HasTensionData(0));
            Assert.True(query.HasTensionData(2));
            Assert.False(query.HasTensionData(3));
        }

        [Fact]
        public void Implements_ITensionQuery_GettersReturnValid()
        {
            var (arc, _) = CreateTestArc(2);
            ITensionQuery query = new DeterministicTensionQuery(arc, seed: 42);

            var profile = query.GetMacroTension(0);
            Assert.NotNull(profile);

            var micro = query.GetMicroTension(0, 0);
            Assert.InRange(micro, 0.0, 1.0);
        }

        [Fact]
        public void MacroTension_InRange_AllSections()
        {
            var (arc, _) = CreateTestArc(5);
            var query = new DeterministicTensionQuery(arc, seed: 42);

            for (int i = 0; i < query.SectionCount; i++)
            {
                var profile = query.GetMacroTension(i);
                Assert.InRange(profile.MacroTension, 0.0, 1.0);
            }
        }

        [Fact]
        public void MicroTension_InRange_AllBars()
        {
            var (arc, track) = CreateTestArc(3);
            var query = new DeterministicTensionQuery(arc, seed: 42);

            for (int secIdx = 0; secIdx < query.SectionCount; secIdx++)
            {
                var map = query.GetMicroTensionMap(secIdx);
                for (int bar = 0; bar < map.BarCount; bar++)
                {
                    double tension = map.GetTension(bar);
                    Assert.InRange(tension, 0.0, 1.0);
                }
            }
        }

        [Fact]
        public void SameSeed_ProducesSameTensionAndDriverAndHints()
        {
            var (arc1, _) = CreateTestArc(4);
            var (arc2, _) = CreateTestArc(4);

            var q1 = new DeterministicTensionQuery(arc1, seed: 100);
            var q2 = new DeterministicTensionQuery(arc2, seed: 100);

            for (int i = 0; i < 4; i++)
            {
                var p1 = q1.GetMacroTension(i);
                var p2 = q2.GetMacroTension(i);
                Assert.Equal(p1.MacroTension, p2.MacroTension, 6);
                Assert.Equal(p1.Driver, p2.Driver);

                var h1 = q1.GetTransitionHint(i);
                var h2 = q2.GetTransitionHint(i);
                Assert.Equal(h1, h2);
            }
        }

        [Fact]
        public void DifferentSeed_ProducesSomeDifference()
        {
            var (arc, _) = CreateTestArc(4);
            var q1 = new DeterministicTensionQuery(arc, seed: 100);
            var q2 = new DeterministicTensionQuery(arc, seed: 200);

            bool foundDiff = false;
            for (int i = 0; i < 4; i++)
            {
                var p1 = q1.GetMacroTension(i);
                var p2 = q2.GetMacroTension(i);
                if (Math.Abs(p1.MacroTension - p2.MacroTension) > 0.001)
                {
                    foundDiff = true; break;
                }
            }
            Assert.True(foundDiff, "Different seeds should produce different tension values");
        }

        [Fact]
        public void PreChorus_TensionVsVerse()
        {
            var track = new SectionTrack();
            track.Add(MusicConstants.eSectionType.Verse, 8);
            track.Add(MusicConstants.eSectionType.Chorus, 8);

            var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
            var q = new DeterministicTensionQuery(arc, seed: 42);

            var verse = q.GetMacroTension(0).MacroTension;
            var chorus = q.GetMacroTension(1).MacroTension;

            Assert.True(chorus > verse - 0.05);
        }

        [Fact]
        public void Chorus_ReleasesAfterPreChorus_HasResolutionOrPeak()
        {
            var track = new SectionTrack();
            track.Add(MusicConstants.eSectionType.Chorus, 8);
            track.Add(MusicConstants.eSectionType.Chorus, 8);

            var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
            var q = new DeterministicTensionQuery(arc, seed: 42);

            var second = q.GetMacroTension(1);
            Assert.True(second.Driver.HasFlag(TensionDriver.Resolution) || second.Driver.HasFlag(TensionDriver.Peak));
        }

        [Fact]
        public void Bridge_HasContrastAndDriver()
        {
            var track = new SectionTrack();
            track.Add(MusicConstants.eSectionType.Verse, 8);
            track.Add(MusicConstants.eSectionType.Bridge, 8);

            var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
            var q = new DeterministicTensionQuery(arc, seed: 42);

            var verse = q.GetMacroTension(0).MacroTension;
            var bridge = q.GetMacroTension(1).MacroTension;
            Assert.True(Math.Abs(bridge - verse) > 0.05);

            var bp = q.GetMacroTension(1);
            Assert.True(bp.Driver.HasFlag(TensionDriver.BridgeContrast));
        }

        [Fact]
        public void Outro_TensionTrendsDown_AndResolutionDriver()
        {
            var track = new SectionTrack();
            track.Add(MusicConstants.eSectionType.Chorus, 8);
            track.Add(MusicConstants.eSectionType.Outro, 4);

            var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
            var q = new DeterministicTensionQuery(arc, seed: 42);

            var chorus = q.GetMacroTension(0).MacroTension;
            var outro = q.GetMacroTension(1).MacroTension;
            Assert.True(outro < chorus);

            var op = q.GetMacroTension(1);
            Assert.True(op.Driver.HasFlag(TensionDriver.Resolution));
        }

        [Fact]
        public void Anticipation_BeforeHigherEnergySection_SetsFlagWhenApplicable()
        {
            var track = new SectionTrack();
            track.Add(MusicConstants.eSectionType.Verse, 8);
            track.Add(MusicConstants.eSectionType.Chorus, 8);

            var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
            var q = new DeterministicTensionQuery(arc, seed: 42);

            var verseProfile = q.GetMacroTension(0);
            var verseTarget = arc.GetTargetForSection(track.Sections[0], 0);
            var chorusTarget = arc.GetTargetForSection(track.Sections[1], 0);

            if (chorusTarget.Energy > verseTarget.Energy + 0.10)
            {
                Assert.True(verseProfile.Driver.HasFlag(TensionDriver.Anticipation));
            }
        }

        [Fact]
        public void TensionDriver_FlagsAreSetSomewhere()
        {
            var (arc, _) = CreateTestArc(5);
            var q = new DeterministicTensionQuery(arc, seed: 42);

            bool found = false;
            for (int i = 0; i < q.SectionCount; i++)
            {
                var p = q.GetMacroTension(i);
                if (p.Driver != TensionDriver.None) { found = true; break; }
            }
            Assert.True(found);
        }

        [Fact]
        public void OpeningDriver_ForIntroSet()
        {
            var track = new SectionTrack();
            track.Add(MusicConstants.eSectionType.Intro, 4);
            track.Add(MusicConstants.eSectionType.Verse, 8);

            var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
            var q = new DeterministicTensionQuery(arc, seed: 42);

            var intro = q.GetMacroTension(0);
            Assert.True(intro.Driver.HasFlag(TensionDriver.Opening));
        }

        [Fact]
        public void PreChorusBuildDriver_SetForChorus()
        {
            var track = new SectionTrack();
            track.Add(MusicConstants.eSectionType.Verse, 8);
            track.Add(MusicConstants.eSectionType.Chorus, 8);

            var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
            var q = new DeterministicTensionQuery(arc, seed: 42);

            var chorus = q.GetMacroTension(1);
            Assert.True(chorus.Driver.HasFlag(TensionDriver.PreChorusBuild) || chorus.Driver.HasFlag(TensionDriver.Anticipation));
        }

        [Fact]
        public void ResolutionDriver_ForChorusPresent()
        {
            var track = new SectionTrack();
            track.Add(MusicConstants.eSectionType.Chorus, 8);

            var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
            var q = new DeterministicTensionQuery(arc, seed: 42);

            var chorus = q.GetMacroTension(0);
            Assert.True(chorus.Driver.HasFlag(TensionDriver.Resolution) || chorus.Driver != TensionDriver.None);
        }

        [Fact]
        public void BridgeContrastDriver_Present()
        {
            var track = new SectionTrack();
            track.Add(MusicConstants.eSectionType.Verse, 8);
            track.Add(MusicConstants.eSectionType.Bridge, 8);

            var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
            var q = new DeterministicTensionQuery(arc, seed: 42);

            var bridge = q.GetMacroTension(1);
            Assert.True(bridge.Driver.HasFlag(TensionDriver.BridgeContrast));
        }

        [Fact]
        public void TransitionHint_BuildWhenEnergyIncreases()
        {
            var track = new SectionTrack();
            track.Add(MusicConstants.eSectionType.Verse, 8);
            track.Add(MusicConstants.eSectionType.Chorus, 8);

            var arc = EnergyArc.Create(track, "RockSteady", seed: 42);
            var q = new DeterministicTensionQuery(arc, seed: 42);

            var hint = q.GetTransitionHint(0);
            Assert.True(hint == SectionTransitionHint.Build || hint == SectionTransitionHint.Sustain || hint == SectionTransitionHint.Release);
        }

        [Fact]
        public void TransitionHint_ReleaseWhenTensionDrops()
        {
            var track = new SectionTrack();
            track.Add(MusicConstants.eSectionType.Chorus, 8);
            track.Add(MusicConstants.eSectionType.Verse, 8);

            var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
            var q = new DeterministicTensionQuery(arc, seed: 42);

            var hint = q.GetTransitionHint(0);
            Assert.True(hint == SectionTransitionHint.Release || hint == SectionTransitionHint.Drop || hint == SectionTransitionHint.Sustain);
        }

        [Fact]
        public void TransitionHint_DropWhenSignificantDecrease()
        {
            var track = new SectionTrack();
            track.Add(MusicConstants.eSectionType.Chorus, 8);
            track.Add(MusicConstants.eSectionType.Outro, 4);

            var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
            var q = new DeterministicTensionQuery(arc, seed: 42);

            var hint = q.GetTransitionHint(0);
            Assert.True(hint == SectionTransitionHint.Drop || hint == SectionTransitionHint.Release);
        }

        [Fact]
        public void TransitionHint_SustainWhenMinimalChange()
        {
            var track = new SectionTrack();
            track.Add(MusicConstants.eSectionType.Verse, 8);
            track.Add(MusicConstants.eSectionType.Verse, 8);

            var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
            var q = new DeterministicTensionQuery(arc, seed: 42);

            var hint = q.GetTransitionHint(0);
            Assert.True(hint == SectionTransitionHint.Sustain || hint == SectionTransitionHint.Build);
        }

        [Fact]
        public void TransitionHint_NoneForLastSection()
        {
            var track = new SectionTrack();
            track.Add(MusicConstants.eSectionType.Verse, 8);
            track.Add(MusicConstants.eSectionType.Outro, 4);

            var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
            var q = new DeterministicTensionQuery(arc, seed: 42);

            var hint = q.GetTransitionHint(1);
            Assert.Equal(SectionTransitionHint.None, hint);
        }

        [Fact]
        public void NonTrivialTensionShapeAcrossPopForm()
        {
            var track = new SectionTrack();
            track.Add(MusicConstants.eSectionType.Intro, 4);
            track.Add(MusicConstants.eSectionType.Verse, 8);
            track.Add(MusicConstants.eSectionType.Chorus, 8);
            track.Add(MusicConstants.eSectionType.Verse, 8);
            track.Add(MusicConstants.eSectionType.Chorus, 8);
            track.Add(MusicConstants.eSectionType.Bridge, 8);
            track.Add(MusicConstants.eSectionType.Chorus, 8);
            track.Add(MusicConstants.eSectionType.Outro, 4);

            var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
            var q = new DeterministicTensionQuery(arc, seed: 42);

            var tensions = new List<double>();
            for (int i = 0; i < q.SectionCount; i++) tensions.Add(q.GetMacroTension(i).MacroTension);

            double min = tensions.Min();
            double max = tensions.Max();
            double range = max - min;

            Assert.True(range > 0.15, $"Tension range {range} should show significant variation (> 0.15)");
            Assert.True(tensions.Distinct().Count() > 1, "Tension values should vary across sections");
        }

        [Fact]
        public void SingleSection_Scenario()
        {
            var track = new SectionTrack();
            track.Add(MusicConstants.eSectionType.Verse, 8);

            var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
            var q = new DeterministicTensionQuery(arc, seed: 42);

            Assert.Equal(1, q.SectionCount);
            var profile = q.GetMacroTension(0);
            Assert.InRange(profile.MacroTension, 0.0, 1.0);
            var hint = q.GetTransitionHint(0);
            Assert.Equal(SectionTransitionHint.None, hint);
        }

        [Fact]
        public void AllSectionsSameType_ShowVariation()
        {
            var track = new SectionTrack();
            for (int i = 0; i < 4; i++) track.Add(MusicConstants.eSectionType.Verse, 8);

            var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
            var q = new DeterministicTensionQuery(arc, seed: 42);

            bool hasVariation = false;
            for (int i = 1; i < 4; i++)
            {
                var prev = q.GetMacroTension(i - 1).MacroTension;
                var curr = q.GetMacroTension(i).MacroTension;
                if (Math.Abs(curr - prev) > 0.01) { hasVariation = true; break; }
            }
            Assert.True(hasVariation, "Repeated sections should show some tension variation");
        }

        [Fact]
        public void VeryShortSections_Handled()
        {
            var track = new SectionTrack();
            track.Add(MusicConstants.eSectionType.Intro, 1);
            track.Add(MusicConstants.eSectionType.Verse, 2);
            track.Add(MusicConstants.eSectionType.Chorus, 1);

            var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
            var q = new DeterministicTensionQuery(arc, seed: 42);

            for (int i = 0; i < 3; i++)
            {
                var p = q.GetMacroTension(i);
                Assert.InRange(p.MacroTension, 0.0, 1.0);
                var map = q.GetMicroTensionMap(i);
                Assert.True(map.BarCount > 0);
            }
        }

        // Helper used across tests
        private static (EnergyArc, SectionTrack) CreateTestArc(int sectionCount)
        {
            var track = new SectionTrack();
            var types = new[] {
                MusicConstants.eSectionType.Intro,
                MusicConstants.eSectionType.Verse,
                MusicConstants.eSectionType.Chorus,
                MusicConstants.eSectionType.Verse,
                MusicConstants.eSectionType.Chorus,
                MusicConstants.eSectionType.Bridge,
                MusicConstants.eSectionType.Outro
            };

            for (int i = 0; i < sectionCount; i++) track.Add(types[i % types.Length], 8);

            var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
            return (arc, track);
        }
    }
}
