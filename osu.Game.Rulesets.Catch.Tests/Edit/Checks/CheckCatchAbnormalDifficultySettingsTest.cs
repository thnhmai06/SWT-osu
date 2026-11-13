// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Edit.Checks;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Rulesets.Objects;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Catch.Tests.Edit.Checks
{
    [TestFixture]
    public class CheckCatchAbnormalDifficultySettingsTest
    {
        private CheckCatchAbnormalDifficultySettings check = null!;
        private Beatmap<HitObject> beatmap = null!;

        [SetUp]
        public void SetUp()
        {
            check = new CheckCatchAbnormalDifficultySettings();
            beatmap = new Beatmap<HitObject>
            {
                Difficulty = new BeatmapDifficulty { ApproachRate = 5, CircleSize = 5, DrainRate = 5 }
            };
            beatmap.BeatmapInfo.Ruleset = new CatchRuleset().RulesetInfo;
        }

        private void setValue(string setting, float value)
        {
            switch (setting)
            {
                case "Approach rate":
                    beatmap.Difficulty.ApproachRate = value;
                    break;

                case "Circle size":
                    beatmap.Difficulty.CircleSize = value;
                    break;

                case "Drain rate":
                    beatmap.Difficulty.DrainRate = value;
                    break;
            }
        }

        private List<Issue> run()
            => check.Run(new BeatmapVerifierContext(beatmap, new TestWorkingBeatmap(beatmap), DifficultyRating.Easy)).ToList();

        // CF: CheckCatchAbnormalDifficultySettings.Run | all six ifs | FALSE; OutOfRange value < 0 FALSE and value > 10 FALSE
        [Test]
        public void TestNormalSettingsYieldNoIssues()
        {
            Assert.That(run(), Is.Empty);
        }

        // CF: CheckAbnormalDifficultySettings.OutOfRange | value < 0 FALSE (0 boundary), value > 10 FALSE (10 boundary)
        [Test]
        public void TestBoundaryValuesZeroAndTenAreAllowed()
        {
            beatmap.Difficulty.ApproachRate = 0;
            beatmap.Difficulty.CircleSize = 10;

            Assert.That(run(), Is.Empty);
        }

        // CF: Run | HasMoreThanOneDecimalPlace TRUE for each of AR/CS/HP
        [TestCase("Approach rate")]
        [TestCase("Circle size")]
        [TestCase("Drain rate")]
        public void TestMoreThanOneDecimalPlaceYieldsDecimalIssue(string setting)
        {
            setValue(setting, 5.55f);

            var issues = run();

            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues.Single().Template, Is.InstanceOf<CheckAbnormalDifficultySettings.IssueTemplateMoreThanOneDecimal>());
            Assert.That(issues.Single().Arguments[0], Is.EqualTo(setting));
        }

        // CF: Run | OutOfRange TRUE via value < 0 for each of AR/CS/HP
        [TestCase("Approach rate")]
        [TestCase("Circle size")]
        [TestCase("Drain rate")]
        public void TestNegativeValueYieldsOutOfRangeIssue(string setting)
        {
            setValue(setting, -1);

            var issues = run();

            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues.Single().Template, Is.InstanceOf<CheckAbnormalDifficultySettings.IssueTemplateOutOfRange>());
            Assert.That(issues.Single().Arguments[0], Is.EqualTo(setting));
        }

        // CF: Run | OutOfRange TRUE via value > 10 (value < 0 FALSE) for each of AR/CS/HP
        [TestCase("Approach rate")]
        [TestCase("Circle size")]
        [TestCase("Drain rate")]
        public void TestValueAboveTenYieldsOutOfRangeIssue(string setting)
        {
            setValue(setting, 11);

            var issues = run();

            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues.Single().Template, Is.InstanceOf<CheckAbnormalDifficultySettings.IssueTemplateOutOfRange>());
            Assert.That(issues.Single().Arguments[0], Is.EqualTo(setting));
        }

        // CF: Run | decimal and out-of-range both TRUE on same setting -> two issues in order
        [Test]
        public void TestDecimalAndOutOfRangeBothReportedInOrder()
        {
            beatmap.Difficulty.ApproachRate = 10.55f;

            var issues = run();

            Assert.That(issues, Has.Count.EqualTo(2));
            Assert.That(issues[0].Template, Is.InstanceOf<CheckAbnormalDifficultySettings.IssueTemplateMoreThanOneDecimal>());
            Assert.That(issues[1].Template, Is.InstanceOf<CheckAbnormalDifficultySettings.IssueTemplateOutOfRange>());
        }
    }
}
