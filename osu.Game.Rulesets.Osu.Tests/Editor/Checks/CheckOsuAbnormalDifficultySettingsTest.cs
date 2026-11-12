// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Checks;
using static osu.Game.Rulesets.Osu.Tests.Editor.Checks.CheckTestHelpers;

namespace osu.Game.Rulesets.Osu.Tests.Editor.Checks
{
    [TestFixture]
    public class CheckOsuAbnormalDifficultySettingsTest
    {
        private readonly CheckOsuAbnormalDifficultySettings check = new CheckOsuAbnormalDifficultySettings();

        private static BeatmapDifficulty difficulty(float ar = 5, float od = 5, float cs = 5, float hp = 5)
            => new BeatmapDifficulty { ApproachRate = ar, OverallDifficulty = od, CircleSize = cs, DrainRate = hp };

        private System.Collections.Generic.List<osu.Game.Rulesets.Edit.Checks.Components.Issue> run(BeatmapDifficulty diff)
            => check.Run(Context(new HitObject[0], difficulty: diff)).ToList();

        [Test]
        public void TestNormalSettings_NoIssue() => Assert.That(run(difficulty()), Is.Empty);

        [Test]
        public void TestBoundaryValues_NoIssue()
        {
            // 0 and 10 are inside the inclusive [0, 10] range
            Assert.That(run(difficulty(0, 10, 0, 10)), Is.Empty);
        }

        // Each setting is evaluated by its own pair of ifs; `index` selects which argument is abnormal.
        private static BeatmapDifficulty withValue(int index, float value) => index switch
        {
            0 => difficulty(ar: value),
            1 => difficulty(od: value),
            2 => difficulty(cs: value),
            _ => difficulty(hp: value),
        };

        private static readonly string[] names = { "Approach rate", "Overall difficulty", "Circle size", "Drain rate" };

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void TestTooManyDecimals_ReportedForThatSettingOnly(int index)
        {
            // HasMoreThanOneDecimalPlace TRUE, OutOfRange FALSE
            var issues = run(withValue(index, 5.25f));

            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckAbnormalDifficultySettings.IssueTemplateMoreThanOneDecimal>());
            Assert.That(issues[0].Arguments[0], Is.EqualTo(names[index]));
        }

        [TestCase(0, 11f)]
        [TestCase(1, 11f)]
        [TestCase(2, -1f)]
        [TestCase(3, -1f)]
        public void TestOutOfRange_ReportedForThatSettingOnly(int index, float value)
        {
            // HasMoreThanOneDecimalPlace FALSE, OutOfRange TRUE (above 10 and below 0)
            var issues = run(withValue(index, value));

            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckAbnormalDifficultySettings.IssueTemplateOutOfRange>());
            Assert.That(issues[0].Arguments[0], Is.EqualTo(names[index]));
        }

        [Test]
        public void TestBothProblems_ReportedTogether()
        {
            // 10.25: more than one decimal AND above range
            var issues = run(difficulty(ar: 10.25f));

            Assert.That(issues.Select(i => i.Template.GetType()), Is.EquivalentTo(new[]
            {
                typeof(CheckAbnormalDifficultySettings.IssueTemplateMoreThanOneDecimal),
                typeof(CheckAbnormalDifficultySettings.IssueTemplateOutOfRange)
            }));
        }

        [Test]
        public void TestIssuesAreYieldedInSettingOrder()
        {
            var issues = run(difficulty(ar: 11, od: 11, cs: 11, hp: 11));

            Assert.That(issues.Select(i => (string)i.Arguments[0]), Is.EqualTo(names));
        }
    }

    [TestFixture]
    public class CheckOsuRulesetSpecificOverridesTest
    {
        private class ExposedFewHitsounds : CheckOsuFewHitsounds
        {
            public bool Excluded(HitObject hitObject) => IsExcludedFromHitsounding(hitObject);
        }

        private class ExposedDrainTime : CheckOsuLowestDiffDrainTime
        {
            public System.Collections.Generic.List<(DifficultyRating rating, double thresholdMs, string name)> Thresholds => GetThresholds().ToList();
        }

        [Test]
        public void TestSpinnersAreExcludedFromHitsounding()
        {
            Assert.That(new ExposedFewHitsounds().Excluded(Spinner(0, 1000)), Is.True);
        }

        [Test]
        public void TestOtherObjectsAreNotExcludedFromHitsounding()
        {
            var check = new ExposedFewHitsounds();

            Assert.That(check.Excluded(Circle(0)), Is.False);
            Assert.That(check.Excluded(Slider(0, 100, 100, 100)), Is.False);
        }

        [Test]
        public void TestDrainTimeThresholds_AreOrderedByDifficultyAndDuration()
        {
            var thresholds = new ExposedDrainTime().Thresholds;

            Assert.That(thresholds.Select(t => t.rating), Is.EqualTo(new[] { DifficultyRating.Hard, DifficultyRating.Insane, DifficultyRating.Expert }));
            Assert.That(thresholds.Select(t => t.thresholdMs), Is.EqualTo(new[] { 210_000d, 255_000d, 300_000d }));
            Assert.That(thresholds.Select(t => t.name), Is.EqualTo(new[] { "Hard", "Insane", "Expert" }));
        }
    }
}
