// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Checks;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Osu.Tests.Edit.Checks
{
    [TestFixture]
    public class CheckOsuAbnormalDifficultySettingsTest
    {
        private static Issue[] run(float ar, float od, float cs, float hp)
        {
            var beatmap = new Beatmap<HitObject>
            {
                BeatmapInfo = new BeatmapInfo { Difficulty = new BeatmapDifficulty { ApproachRate = ar, OverallDifficulty = od, CircleSize = cs, DrainRate = hp } }
            };

            return new CheckOsuAbnormalDifficultySettings().Run(new BeatmapVerifierContext(beatmap, new TestWorkingBeatmap(beatmap))).ToArray();
        }

        // CF: CheckOsuAbnormalDifficultySettings.Run | HasMoreThanOneDecimalPlace(AR/OD/CS/HP) | FALSE
        // CF: CheckOsuAbnormalDifficultySettings.Run | OutOfRange(AR/OD/CS/HP) | FALSE
        [Test]
        public void TestNormalSettings()
        {
            Assert.That(run(9.3f, 8, 4.2f, 5), Is.Empty);
        }

        // CF: CheckOsuAbnormalDifficultySettings.Run | HasMoreThanOneDecimalPlace(AR/OD/CS/HP) | TRUE
        [Test]
        public void TestEachSettingWithTooManyDecimals()
        {
            var issues = run(9.35f, 8.25f, 4.15f, 5.05f);

            Assert.That(issues, Has.Length.EqualTo(4));
            Assert.That(issues, Has.All.Matches<Issue>(i => i.Template is CheckAbnormalDifficultySettings.IssueTemplateMoreThanOneDecimal));
        }

        // CF: CheckOsuAbnormalDifficultySettings.Run | OutOfRange(AR/OD/CS/HP) | TRUE
        [Test]
        public void TestEachSettingOutOfRange()
        {
            var issues = run(11, -1, 12, -2);

            Assert.That(issues, Has.Length.EqualTo(4));
            Assert.That(issues, Has.All.Matches<Issue>(i => i.Template is CheckAbnormalDifficultySettings.IssueTemplateOutOfRange));
        }

        [Test]
        public void TestBoundaryValuesAreInRange()
        {
            Assert.That(run(0, 10, 0, 10), Is.Empty);
        }
    }
}
