// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Taiko.Edit.Checks;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Taiko.Tests.Edit.Checks
{
    [TestFixture]
    public class CheckTaikoAbnormalDifficultySettingsTest
    {
        private CheckTaikoAbnormalDifficultySettings check = null!;
        private IBeatmap beatmap = null!;

        [SetUp]
        public void SetUp()
        {
            check = new CheckTaikoAbnormalDifficultySettings();
            beatmap = new Beatmap<HitObject>();
            beatmap.BeatmapInfo.Ruleset = new TaikoRuleset().RulesetInfo;
            beatmap.Difficulty = new BeatmapDifficulty { OverallDifficulty = 5, DrainRate = 5 };
        }

        private System.Collections.Generic.List<Issue> run()
            => check.Run(new BeatmapVerifierContext(beatmap, new TestWorkingBeatmap(beatmap))).ToList();

        // CF: CheckTaikoAbnormalDifficultySettings.Run | HasMoreThanOneDecimalPlace(OD) | FALSE
        // CF: CheckTaikoAbnormalDifficultySettings.Run | OutOfRange(OD) | FALSE
        // CF: CheckTaikoAbnormalDifficultySettings.Run | HasMoreThanOneDecimalPlace(DR) | FALSE
        // CF: CheckTaikoAbnormalDifficultySettings.Run | OutOfRange(DR) | FALSE
        [Test]
        public void TestNormalSettings() => Assert.That(run(), Is.Empty);

        // CF: CheckTaikoAbnormalDifficultySettings.Run | HasMoreThanOneDecimalPlace(OD) | TRUE
        [Test]
        public void TestOverallDifficultyTwoDecimals()
        {
            beatmap.Difficulty.OverallDifficulty = 5.55f;
            var issues = run();
            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues.Single().Template, Is.TypeOf<CheckAbnormalDifficultySettings.IssueTemplateMoreThanOneDecimal>());
        }

        // CF: CheckTaikoAbnormalDifficultySettings.Run | HasMoreThanOneDecimalPlace(DR) | TRUE
        [Test]
        public void TestDrainRateTwoDecimals()
        {
            beatmap.Difficulty.DrainRate = 5.55f;
            var issues = run();
            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues.Single().Template, Is.TypeOf<CheckAbnormalDifficultySettings.IssueTemplateMoreThanOneDecimal>());
        }

        // CF: CheckTaikoAbnormalDifficultySettings.Run | OutOfRange(OD) | TRUE
        [TestCase(-10f)]
        [TestCase(20f)]
        public void TestOverallDifficultyOutOfRange(float value)
        {
            beatmap.Difficulty.OverallDifficulty = value;
            var issues = run();
            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues.Single().Template, Is.TypeOf<CheckAbnormalDifficultySettings.IssueTemplateOutOfRange>());
        }

        // CF: CheckTaikoAbnormalDifficultySettings.Run | OutOfRange(DR) | TRUE
        [TestCase(-10f)]
        [TestCase(20f)]
        public void TestDrainRateOutOfRange(float value)
        {
            beatmap.Difficulty.DrainRate = value;
            var issues = run();
            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues.Single().Template, Is.TypeOf<CheckAbnormalDifficultySettings.IssueTemplateOutOfRange>());
        }
    }
}
