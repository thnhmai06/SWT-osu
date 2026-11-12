// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Checks;
using static osu.Game.Rulesets.Osu.Tests.Editor.Checks.CheckTestHelpers;

namespace osu.Game.Rulesets.Osu.Tests.Editor.Checks
{
    [TestFixture]
    public class CheckTooShortSpinnersTest
    {
        private readonly CheckTooShortSpinners check = new CheckTooShortSpinners();

        public enum Expected
        {
            None,
            Warning,
            Problem
        }

        private Expected run(double od, double duration)
        {
            var issues = check.Run(Context(new HitObject[] { Spinner(0, duration) }, difficulty: new BeatmapDifficulty { OverallDifficulty = (float)od })).ToList();

            if (issues.Count == 0)
                return Expected.None;

            Assert.That(issues, Has.Count.EqualTo(1));
            return issues[0].Template is CheckTooShortSpinners.IssueTemplateTooShort ? Expected.Problem : Expected.Warning;
        }

        // thresholds at OD 5: problem < 450, warning < 500

        [TestCase(449, Expected.Problem)]
        [TestCase(450, Expected.Warning)]
        [TestCase(499, Expected.Warning)]
        [TestCase(500, Expected.None)]
        public void TestOd5(double duration, Expected expected) => Assert.That(run(5, duration), Is.EqualTo(expected));

        // od < 5 TRUE: problem = 450 - 5*17 = 365, warning = 500 - 5*21.8 = 391

        [TestCase(364, Expected.Problem)]
        [TestCase(365, Expected.Warning)]
        [TestCase(390, Expected.Warning)]
        [TestCase(391, Expected.None)]
        public void TestOd0(double duration, Expected expected) => Assert.That(run(0, duration), Is.EqualTo(expected));

        // od < 5 FALSE with od > 5: problem = 450 + 5*17 = 535, warning = 500 + 5*20 = 600

        [TestCase(534, Expected.Problem)]
        [TestCase(535, Expected.Warning)]
        [TestCase(599, Expected.Warning)]
        [TestCase(600, Expected.None)]
        public void TestOd10(double duration, Expected expected) => Assert.That(run(10, duration), Is.EqualTo(expected));

        [Test]
        public void TestNonSpinnerObjects_AreIgnored()
        {
            // `hitObject is Spinner` FALSE -> continue
            Assert.That(check.Run(Context(new HitObject[] { Circle(0), Slider(100, 100, 100, 10) })), Is.Empty);
        }

        [Test]
        public void TestMultipleSpinners_EachEvaluated()
        {
            var issues = check.Run(Context(new HitObject[] { Spinner(0, 100), Spinner(1000, 2000), Spinner(5000, 480) })).ToList();

            Assert.That(issues.Select(i => i.Template.GetType()), Is.EqualTo(new[] { typeof(CheckTooShortSpinners.IssueTemplateTooShort), typeof(CheckTooShortSpinners.IssueTemplateVeryShort) }));
        }
    }
}
