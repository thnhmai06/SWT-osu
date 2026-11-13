// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckDrainLengthTest
    {
        private readonly CheckDrainLength check = new CheckDrainLength();

        private static Beatmap beatmap(double end, params BreakPeriod[] breaks)
        {
            var b = new Beatmap { HitObjects = { new HitCircle { StartTime = 0 }, new HitCircle { StartTime = end } } };
            b.Breaks.AddRange(breaks);
            return b;
        }

        // CF: CheckDrainLength.Run | drainTime < min_drain_threshold | TRUE
        [Test]
        public void TestTooShort()
        {
            var issue = check.Run(CheckTestUtils.Context(beatmap(29_999))).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckDrainLength.IssueTemplateTooShort>());
            Assert.That(check.PossibleTemplates.Count(), Is.EqualTo(1));
        }

        // CF: CheckDrainLength.Run | drainTime < min_drain_threshold | FALSE
        [Test]
        public void TestLongEnough()
        {
            Assert.That(check.Run(CheckTestUtils.Context(beatmap(30_000))), Is.Empty);
            Assert.That(check.Run(CheckTestUtils.Context(beatmap(60_000))), Is.Empty);
        }

        [Test]
        public void TestBreaksDoNotCountAsDrain()
        {
            var issues = check.Run(CheckTestUtils.Context(beatmap(40_000, new BreakPeriod(5_000, 20_000)))).ToArray();

            Assert.That(issues.Length, Is.EqualTo(1), "40s total minus a 15s break leaves 25s of drain");
        }
    }
}
