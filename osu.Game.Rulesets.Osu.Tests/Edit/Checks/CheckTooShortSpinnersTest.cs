// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Checks;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Osu.Tests.Edit.Checks
{
    [TestFixture]
    public class CheckTooShortSpinnersTest
    {
        private readonly CheckTooShortSpinners check = new CheckTooShortSpinners();

        private List<Issue> run(float od, params HitObject[] objects)
        {
            var beatmap = new Beatmap<HitObject>
            {
                HitObjects = objects.ToList(),
                BeatmapInfo = new BeatmapInfo { Difficulty = new BeatmapDifficulty { OverallDifficulty = od } }
            };

            return check.Run(new BeatmapVerifierContext(beatmap, new TestWorkingBeatmap(beatmap))).ToList();
        }

        private static Spinner spinner(double duration) => new Spinner { StartTime = 0, Duration = duration };

        // CF: CheckTooShortSpinners.Run | foreach hitObject | loop 0
        [Test]
        public void TestNoObjects()
        {
            Assert.That(run(5), Is.Empty);
        }

        // CF: CheckTooShortSpinners.Run | foreach hitObject | loop 1
        // CF: CheckTooShortSpinners.Run | !(hitObject is Spinner) | TRUE
        [Test]
        public void TestNonSpinnerIsIgnored()
        {
            Assert.That(run(5, new HitCircle()), Is.Empty);
        }

        // CF: CheckTooShortSpinners.Run | !(hitObject is Spinner) | FALSE
        // CF: CheckTooShortSpinners.Run | spinner.Duration < problemThreshold | FALSE
        // CF: CheckTooShortSpinners.Run | spinner.Duration < warningThreshold | FALSE
        // CF: CheckTooShortSpinners.Run | od < 5 | FALSE
        [Test]
        public void TestLongSpinnerIsOk()
        {
            Assert.That(run(5, spinner(4000)), Is.Empty);
        }

        // CF: CheckTooShortSpinners.Run | spinner.Duration < problemThreshold | TRUE
        [Test]
        public void TestTooShortSpinner()
        {
            var issues = run(5, spinner(400));
            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckTooShortSpinners.IssueTemplateTooShort>());
        }

        // CF: CheckTooShortSpinners.Run | spinner.Duration < warningThreshold | TRUE
        [Test]
        public void TestVeryShortSpinner()
        {
            var issues = run(5, spinner(475));
            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckTooShortSpinners.IssueTemplateVeryShort>());
        }

        // CF: CheckTooShortSpinners.Run | foreach hitObject | loop n
        // CF: CheckTooShortSpinners.Run | od < 5 | TRUE
        [Test]
        public void TestThresholdsDependOnOverallDifficulty()
        {
            // 450ms is fine at OD1 but too short at OD10
            Assert.That(run(1, spinner(450), spinner(450)), Is.Empty);
            Assert.That(run(10, spinner(450)), Has.Count.EqualTo(1));
            Assert.That(run(10, spinner(450))[0].Template, Is.TypeOf<CheckTooShortSpinners.IssueTemplateTooShort>());
        }
    }
}
