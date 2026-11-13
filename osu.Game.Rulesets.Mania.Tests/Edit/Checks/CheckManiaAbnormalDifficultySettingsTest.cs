// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Rulesets.Mania.Edit.Checks;
using osu.Game.Rulesets.Objects;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Mania.Tests.Edit.Checks
{
    [TestFixture]
    public class CheckManiaAbnormalDifficultySettingsTest
    {
        private readonly CheckManiaAbnormalDifficultySettings check = new CheckManiaAbnormalDifficultySettings();

        private System.Collections.Generic.List<Issue> run(float od, float hp)
        {
            var beatmap = new Beatmap<HitObject> { Difficulty = new BeatmapDifficulty { OverallDifficulty = od, DrainRate = hp } };
            return check.Run(new BeatmapVerifierContext(beatmap, new TestWorkingBeatmap(beatmap))).ToList();
        }

        // CF: CheckManiaAbnormalDifficultySettings.Run | HasMoreThanOneDecimalPlace(OD) | FALSE
        // CF: CheckManiaAbnormalDifficultySettings.Run | OutOfRange(OD) | FALSE
        // CF: CheckManiaAbnormalDifficultySettings.Run | HasMoreThanOneDecimalPlace(HP) | FALSE
        // CF: CheckManiaAbnormalDifficultySettings.Run | OutOfRange(HP) | FALSE
        [Test]
        public void TestNormalSettings() => Assert.That(run(5, 5), Is.Empty);

        // CF: CheckManiaAbnormalDifficultySettings.Run | HasMoreThanOneDecimalPlace(OD) | TRUE
        [Test]
        public void TestOverallDifficultyTwoDecimals() => assertSingle(run(5.55f, 5), typeof(CheckAbnormalDifficultySettings.IssueTemplateMoreThanOneDecimal));

        // CF: CheckManiaAbnormalDifficultySettings.Run | HasMoreThanOneDecimalPlace(HP) | TRUE
        [Test]
        public void TestDrainRateTwoDecimals() => assertSingle(run(5, 5.55f), typeof(CheckAbnormalDifficultySettings.IssueTemplateMoreThanOneDecimal));

        // CF: CheckManiaAbnormalDifficultySettings.Run | OutOfRange(OD) | TRUE
        [TestCase(-10f)]
        [TestCase(20f)]
        public void TestOverallDifficultyOutOfRange(float od) => assertSingle(run(od, 5), typeof(CheckAbnormalDifficultySettings.IssueTemplateOutOfRange));

        // CF: CheckManiaAbnormalDifficultySettings.Run | OutOfRange(HP) | TRUE
        [TestCase(-10f)]
        [TestCase(20f)]
        public void TestDrainRateOutOfRange(float hp) => assertSingle(run(5, hp), typeof(CheckAbnormalDifficultySettings.IssueTemplateOutOfRange));

        private static void assertSingle(System.Collections.Generic.List<Issue> issues, System.Type template)
        {
            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf(template));
        }
    }
}
