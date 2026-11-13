// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckUnsnappedObjectsTest
    {
        private readonly CheckUnsnappedObjects check = new CheckUnsnappedObjects();

        private class TestDurationObject : HitObject, IHasDuration
        {
            public double EndTime { get; set; }

            public double Duration
            {
                get => EndTime - StartTime;
                set => EndTime = StartTime + value;
            }
        }

        private class TestRepeatObject : TestDurationObject, IHasRepeats
        {
            public int RepeatCount { get; set; }
            public IList<IList<HitSampleInfo>> NodeSamples { get; set; } = new List<IList<HitSampleInfo>>();
        }

        private Issue[] run(params HitObject[] objects)
        {
            // beat length 100 starting at t=100 (same setup as the original suite): snaps at 100, 200, ... and their divisors.
            var beatmap = new Beatmap();
            beatmap.ControlPointInfo.Add(100, new TimingControlPoint { BeatLength = 100 });
            beatmap.HitObjects.AddRange(objects);
            return check.Run(CheckTestUtils.Context(beatmap)).ToArray();
        }

        // CF: CheckUnsnappedObjects.Run | foreach hitobject | loop 0
        // CF: CheckUnsnappedObjects.Run | foreach hitobject | loop n
        // CF: CheckUnsnappedObjects.getUnsnapIssues | Math.Abs(unsnap) >= UNSNAP_MS_THRESHOLD | FALSE
        // CF: CheckUnsnappedObjects.getUnsnapIssues | Math.Abs(unsnap) >= 1 | FALSE
        // CF: CheckUnsnappedObjects.Run | hitobject is IHasRepeats | FALSE
        // CF: CheckUnsnappedObjects.Run | hitobject is IHasDuration | FALSE
        [Test]
        public void TestSnappedCirclesHaveNoIssues()
        {
            Assert.That(run(), Is.Empty);
            Assert.That(run(new HitCircle { StartTime = 100 }, new HitCircle { StartTime = 150 }, new HitCircle { StartTime = 100.5 }), Is.Empty, "less than 1ms is ignored");
            Assert.That(check.PossibleTemplates.Count(), Is.EqualTo(2));
        }

        // CF: CheckUnsnappedObjects.getUnsnapIssues | Math.Abs(unsnap) >= 1 | TRUE
        [TestCase(101)]
        [TestCase(99)]
        public void TestSmallUnsnap(double time)
        {
            var issue = run(new HitCircle { StartTime = time }).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckUnsnappedObjects.IssueTemplateSmallUnsnap>());
            Assert.That(issue.Time, Is.EqualTo(time));
        }

        // CF: CheckUnsnappedObjects.getUnsnapIssues | Math.Abs(unsnap) >= UNSNAP_MS_THRESHOLD | TRUE
        [TestCase(102)]
        [TestCase(98)]
        public void TestLargeUnsnap(double time)
        {
            var issue = run(new HitCircle { StartTime = time }).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckUnsnappedObjects.IssueTemplateLargeUnsnap>());
            Assert.That(issue.Arguments[0], Is.EqualTo("HitCircle"));
        }

        // CF: CheckUnsnappedObjects.Run | hitobject is IHasDuration | TRUE
        // CF: CheckUnsnappedObjects.Run | startPostfix is "start" | TRUE
        [Test]
        public void TestDurationObjectReportsStartAndEnd()
        {
            var issues = run(new TestDurationObject { StartTime = 98, EndTime = 301 });

            Assert.That(issues.Select(i => (i.Template.GetType(), (string)i.Arguments[0])), Is.EquivalentTo(new[]
            {
                (typeof(CheckUnsnappedObjects.IssueTemplateLargeUnsnap), "TestDurationObject start"),
                (typeof(CheckUnsnappedObjects.IssueTemplateSmallUnsnap), "TestDurationObject end"),
            }));
        }

        // CF: CheckUnsnappedObjects.Run | hitobject is IHasRepeats | TRUE
        // CF: CheckUnsnappedObjects.Run | for repeatIndex < RepeatCount | loop 1
        // CF: CheckUnsnappedObjects.Run | for repeatIndex < RepeatCount | loop 0
        [Test]
        public void TestRepeatsAreCheckedSeparately()
        {
            // start 100, end 502, one repeat => span 201, repeat at 301 (1ms off), end 502 (2ms off).
            var issues = run(new TestRepeatObject { StartTime = 100, EndTime = 502, RepeatCount = 1 });

            Assert.That(issues.Select(i => (i.Template.GetType(), (string)i.Arguments[0])), Is.EquivalentTo(new[]
            {
                (typeof(CheckUnsnappedObjects.IssueTemplateSmallUnsnap), "TestRepeatObject repeat"),
                (typeof(CheckUnsnappedObjects.IssueTemplateLargeUnsnap), "TestRepeatObject end"),
            }));

            Assert.That(run(new TestRepeatObject { StartTime = 100, EndTime = 300, RepeatCount = 0 }), Is.Empty);
        }

        // CF: CheckUnsnappedObjects.Run | for repeatIndex < RepeatCount | loop n
        [Test]
        public void TestMultipleRepeats()
        {
            // start 100, end 400.. three spans of 100 exactly => all snapped.
            Assert.That(run(new TestRepeatObject { StartTime = 100, EndTime = 400, RepeatCount = 2 }), Is.Empty);
        }
    }
}
