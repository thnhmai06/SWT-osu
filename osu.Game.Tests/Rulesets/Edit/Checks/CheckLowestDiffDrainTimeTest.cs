// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckLowestDiffDrainTimeTest
    {
        private class TestCheck : CheckLowestDiffDrainTime
        {
            protected override IEnumerable<(DifficultyRating rating, double thresholdMs, string name)> GetThresholds() =>
            [
                (DifficultyRating.Normal, 90_000, "Normal"),
                (DifficultyRating.Hard, 120_000, "Hard"),
            ];
        }

        private readonly TestCheck check = new TestCheck();

        private static Beatmap beatmap(double length, double stars = 0, params BreakPeriod[] breaks)
        {
            var b = new Beatmap { HitObjects = { new HitCircle { StartTime = 0 }, new HitCircle { StartTime = length } } };
            b.BeatmapInfo.StarRating = stars;
            b.Breaks.AddRange(breaks);
            return b;
        }

        private static BeatmapVerifierContext context(Beatmap current, DifficultyRating interpreted, params Beatmap[] others)
            => new BeatmapVerifierContext(CheckFakes.Verified(current), others.Select(o => CheckFakes.Verified(o)).ToList(), interpreted);

        // CF: CheckLowestDiffDrainTime.Run | applicableThreshold != default && effectiveTime < thresholdMs - thresholdReduction | TRUE
        // CF: CheckLowestDiffDrainTime.Run | lowestDifficulty.Playable == context.CurrentDifficulty.Playable | TRUE
        // CF: CheckLowestDiffDrainTime.Run | !isHighestDifficulty || TotalBreakTime < break_time_leniency | TRUE
        [Test]
        public void TestLowestIsCurrentTooShort()
        {
            var issue = check.Run(context(beatmap(60_000), DifficultyRating.Hard)).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckLowestDiffDrainTime.IssueTemplateTooShort>());
            Assert.That(issue.Arguments[0], Is.EqualTo("Hard"), "highest applicable threshold should win");
            Assert.That(issue.Arguments[1], Is.EqualTo("play"));
        }

        // CF: CheckLowestDiffDrainTime.Run | applicableThreshold != default && effectiveTime < thresholdMs - thresholdReduction | FALSE
        [Test]
        public void TestLongEnough()
        {
            Assert.That(check.Run(context(beatmap(120_000), DifficultyRating.Hard)), Is.Empty);
        }

        // CF: CheckLowestDiffDrainTime.Run | applicableThreshold != default | FALSE
        [Test]
        public void TestBelowAllThresholds()
        {
            Assert.That(check.Run(context(beatmap(1_000), DifficultyRating.Easy)), Is.Empty);
        }

        // CF: CheckLowestDiffDrainTime.Run | lowestDifficulty.Playable == context.CurrentDifficulty.Playable | FALSE
        // CF: CheckLowestDiffDrainTime.Run | !isHighestDifficulty | FALSE
        // CF: CheckLowestDiffDrainTime.Run | TotalBreakTime < break_time_leniency | FALSE
        // CF: CheckLowestDiffDrainTime.Run | !isHighestDifficulty || TotalBreakTime < break_time_leniency | FALSE
        [Test]
        public void TestHighestWithLongBreaksUsesDrainTime()
        {
            // other (lowest) difficulty is 2.1* -> Normal -> 90s threshold, reduced by 30s leniency to 60s.
            var current = beatmap(85_000, 6, new BreakPeriod(5_000, 40_000));
            var other = beatmap(85_000, 2.1);

            var issue = check.Run(context(current, DifficultyRating.ExpertPlus, other)).Single();

            Assert.That(issue.Arguments[0], Is.EqualTo("Normal"));
            Assert.That(issue.Arguments[1], Is.EqualTo("drain"));
        }

        // CF: CheckLowestDiffDrainTime.Run | !isHighestDifficulty | TRUE
        [Test]
        public void TestNotHighestUsesPlayTimeDespiteBreaks()
        {
            var current = beatmap(85_000, 3, new BreakPeriod(5_000, 40_000));
            var lower = beatmap(85_000, 2.1);
            var higher = beatmap(85_000, 6);

            var issue = check.Run(context(current, DifficultyRating.Hard, lower, higher)).Single();

            Assert.That(issue.Arguments[1], Is.EqualTo("play"));
        }

        // CF: CheckLowestDiffDrainTime.Run | TotalBreakTime < break_time_leniency | TRUE
        [Test]
        public void TestHighestWithShortBreaksUsesPlayTime()
        {
            var current = beatmap(85_000, 6, new BreakPeriod(5_000, 20_000));
            var other = beatmap(85_000, 2.1);

            Assert.That(check.Run(context(current, DifficultyRating.ExpertPlus, other)).Single().Arguments[1], Is.EqualTo("play"));
        }

        // CF: CheckLowestDiffDrainTime.Run | difficulties.Count == 0 | infeasible: the current difficulty is always part of AllDifficulties and shares its own ruleset
        [Test]
        public void TestPossibleTemplates()
        {
            Assert.That(check.PossibleTemplates.Count(), Is.EqualTo(1));
        }
    }
}
