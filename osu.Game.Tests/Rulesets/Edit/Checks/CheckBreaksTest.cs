// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckBreaksTest
    {
        private readonly CheckBreaks check = new CheckBreaks();

        private static Beatmap beatmap(BreakPeriod? breakPeriod, params double[] objectTimes)
        {
            var b = new Beatmap();
            foreach (double t in objectTimes)
                b.HitObjects.Add(new HitCircle { StartTime = t });
            if (breakPeriod != null)
                b.Breaks.Add(breakPeriod);
            return b;
        }

        private Issue[] run(Beatmap b) => check.Run(CheckTestUtils.Context(b)).ToArray();

        // CF: CheckBreaks.Run | foreach break | loop 0
        // CF: CheckBreaks.PossibleTemplates | new IssueTemplate[] | non-null
        [Test]
        public void TestNoBreaks()
        {
            Assert.That(run(beatmap(null, 0, 1000)), Is.Empty);
            Assert.That(check.PossibleTemplates.Count(), Is.EqualTo(3));
            Assert.That(check.Metadata.Category, Is.EqualTo(CheckCategory.Events));
        }

        // CF: CheckBreaks.Run | breakPeriod.Duration < MIN_BREAK_DURATION | TRUE
        // CF: CheckBreaks.Run | previousObjectEndTimeIndex >= 0 | FALSE
        // CF: CheckBreaks.Run | nextObjectStartTimeIndex < startTimes.Count | FALSE
        // CF: CheckBreaks.Run | foreach break | loop 1
        [Test]
        public void TestBreakTooShort()
        {
            var issues = run(beatmap(new BreakPeriod(0, BreakPeriod.MIN_BREAK_DURATION - 1)));

            Assert.That(issues.Single().Template, Is.InstanceOf<CheckBreaks.IssueTemplateTooShort>());
        }

        // CF: CheckBreaks.Run | breakPeriod.Duration < MIN_BREAK_DURATION | FALSE
        // CF: CheckBreaks.Run | previousObjectEndTimeIndex < 0 | FALSE
        // CF: CheckBreaks.Run | previousObjectEndTimeIndex >= 0 | TRUE
        // CF: CheckBreaks.Run | gapBeforeBreak < GAP_BEFORE_BREAK - leniency | TRUE
        // CF: CheckBreaks.Run | nextObjectStartTimeIndex < startTimes.Count | TRUE
        // CF: CheckBreaks.Run | gapAfterBreak < GAP_AFTER_BREAK - leniency | FALSE
        [Test]
        public void TestBreakStartsEarly()
        {
            var issues = run(beatmap(new BreakPeriod(100, 751), 0, 1200));

            Assert.That(issues.Single().Template, Is.InstanceOf<CheckBreaks.IssueTemplateEarlyStart>());
        }

        // CF: CheckBreaks.Run | gapBeforeBreak < GAP_BEFORE_BREAK - leniency | FALSE
        // CF: CheckBreaks.Run | gapAfterBreak < GAP_AFTER_BREAK - leniency | TRUE
        [Test]
        public void TestBreakEndsLate()
        {
            var issues = run(beatmap(new BreakPeriod(200, 850), 0, 1298));

            Assert.That(issues.Single().Template, Is.InstanceOf<CheckBreaks.IssueTemplateLateEnd>());
        }

        // CF: CheckBreaks.Run | nextObjectStartTimeIndex < startTimes.Count | FALSE
        [Test]
        public void TestBreakAfterLastObject()
        {
            var issues = run(beatmap(new BreakPeriod(1398, 2300), 0, 1200));

            Assert.That(issues.Single().Template, Is.InstanceOf<CheckBreaks.IssueTemplateEarlyStart>());
        }

        // CF: CheckBreaks.Run | previousObjectEndTimeIndex < 0 | TRUE
        [Test]
        public void TestBreakBeforeFirstObject()
        {
            var issues = run(beatmap(new BreakPeriod(0, 652), 1100, 1500));

            Assert.That(issues.Single().Template, Is.InstanceOf<CheckBreaks.IssueTemplateLateEnd>());
        }

        // CF: CheckBreaks.Run | previousObjectEndTimeIndex < 0 (exact match found) | FALSE
        [Test]
        public void TestBreakStartingExactlyAtObjectEnd()
        {
            var issues = run(beatmap(new BreakPeriod(100, 1000), 100, 2000));

            Assert.That(issues.Single().Template, Is.InstanceOf<CheckBreaks.IssueTemplateEarlyStart>());
        }

        [Test]
        public void TestCorrectBreakHasNoIssues()
        {
            Assert.That(run(beatmap(new BreakPeriod(200, 850), 0, 1300)), Is.Empty);
        }

        [Test]
        public void TestMultipleObjectsOnlyNearestMatters()
        {
            var issues = run(beatmap(new BreakPeriod(200, 850), 0, 1297, 1298));

            Assert.That(issues.Single().Template, Is.InstanceOf<CheckBreaks.IssueTemplateLateEnd>());
        }

        // CF: CheckBreaks.Run | foreach break | loop n
        [Test]
        public void TestMultipleBreaks()
        {
            var b = beatmap(new BreakPeriod(200, 850), 0, 1300, 3000);
            b.Breaks.Add(new BreakPeriod(1500, 1600));

            var issues = run(b);

            Assert.That(issues.Select(i => i.Template.GetType()), Does.Contain(typeof(CheckBreaks.IssueTemplateTooShort)));
        }
    }
}
