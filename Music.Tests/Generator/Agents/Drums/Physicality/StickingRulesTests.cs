using FluentAssertions;
using Music.Generator.Agents.Drums;
using Music.Generator.Agents.Drums.Physicality;
using Music.Generator.Groove;

namespace Music.Tests.Generator.Agents.Drums.Physicality
{
    public class StickingRulesTests
    {
        [Fact]
        public void StickingRules_ValidatePattern_EmptyCandidates_ReturnsValid()
        {
            var rules = new StickingRules();
            var validation = rules.ValidatePattern(Array.Empty<DrumCandidate>());
            validation.IsValid.Should().BeTrue();
            validation.Violations.Should().BeEmpty();
        }

        [Fact]
        public void StickingRules_ValidatePattern_SingleCandidate_ReturnsValid()
        {
            var rules = new StickingRules();
            var candidate = DrumCandidate.CreateMinimal();
            var validation = rules.ValidatePattern(new[] { candidate });
            validation.IsValid.Should().BeTrue();
        }

        [Fact]
        public void StickingRules_ValidatePattern_GhostCount_ExceedsMax_ReturnsViolation()
        {
            var rules = new StickingRules();
            // create 5 ghost candidates in bar 1 (default max 4)
            var list = new List<DrumCandidate>();
            for (int i = 0; i < 5; i++)
            {
                var c = DrumCandidate.CreateMinimal(operatorId: "G", role: GrooveRoles.Snare, barNumber: 1, beat: 1 + i * 0.25m, strength: OnsetStrength.Ghost);
                list.Add(c);
            }

            var validation = rules.ValidatePattern(list);
            validation.IsValid.Should().BeFalse();
            validation.Violations.Should().ContainSingle(v => v.RuleId == "MaxGhostsPerBar");
        }

        [Fact]
        public void StickingRules_ValidatePattern_SameLimb_ExceedsMaxConsecutive_ReturnsViolation()
        {
            var rules = new StickingRules();
            // create 6 quick hits for same limb (right foot -> Kick)
            var list = new List<DrumCandidate>();
            for (int i = 0; i < 6; i++)
            {
                var c = DrumCandidate.CreateMinimal(operatorId: "K", role: GrooveRoles.Kick, barNumber: 1, beat: 1 + i * 0.25m, strength: OnsetStrength.Strong);
                var cid = DrumCandidate.GenerateCandidateId("K", GrooveRoles.Kick, c.BarNumber, c.Beat);
                c = c with { CandidateId = cid };
                list.Add(c);
            }

            var validation = rules.ValidatePattern(list);
            validation.IsValid.Should().BeFalse();
            validation.Violations.Should().Contain(v => v.RuleId == "MaxConsecutiveSameHand");
        }
    }
}

