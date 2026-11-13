// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckZeroLengthObjectsTest
    {
        private readonly CheckZeroLengthObjects check = new CheckZeroLengthObjects();

        // CF: CheckZeroLengthObjects.Run | hitObject is IHasDuration | FALSE
        // CF: CheckZeroLengthObjects.Run | foreach hitObject | loop n
        // CF: CheckZeroLengthObjects.Run | foreach hitObject | loop 0
        [Test]
        public void TestObjectsWithoutDurationAreIgnored()
        {
            Assert.That(check.Run(CheckTestUtils.Context(new Beatmap())), Is.Empty);
            Assert.That(check.Run(CheckTestUtils.Context(new Beatmap { HitObjects = { new HitCircle { StartTime = 1000 } } })), Is.Empty);
            Assert.That(check.PossibleTemplates.Count(), Is.EqualTo(1));
        }

        // CF: CheckZeroLengthObjects.Run | hitObject is IHasDuration | TRUE
        // CF: CheckZeroLengthObjects.Run | hasDuration.Duration < leniency | TRUE
        [TestCase(0)]
        [TestCase(0.4)]
        [TestCase(-10)]
        public void TestTooShortDurationIsReported(double duration)
        {
            var beatmap = new Beatmap { HitObjects = { new Spinner { StartTime = 1000, Duration = duration } } };

            var issue = check.Run(CheckTestUtils.Context(beatmap)).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckZeroLengthObjects.IssueTemplateZeroLength>());
        }

        // CF: CheckZeroLengthObjects.Run | hasDuration.Duration < leniency | FALSE
        [TestCase(0.5)]
        [TestCase(1000)]
        public void TestAcceptableDurationIsNotReported(double duration)
        {
            var beatmap = new Beatmap { HitObjects = { new Spinner { StartTime = 1000, Duration = duration } } };

            Assert.That(check.Run(CheckTestUtils.Context(beatmap)), Is.Empty);
        }
    }
}
