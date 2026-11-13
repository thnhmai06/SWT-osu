// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckConcurrentObjectsTest
    {
        private readonly CheckConcurrentObjects check = new CheckConcurrentObjects();

        private static Beatmap beatmap(params HitObject[] objects)
        {
            var b = new Beatmap();
            b.HitObjects.AddRange(objects);
            return b;
        }

        private static HitCircle circle(double time) => new HitCircle { StartTime = time };

        private Issue[] run(params HitObject[] objects) => check.Run(CheckTestUtils.Context(beatmap(objects))).ToArray();

        // CF: CheckConcurrentObjects.Run | for i < Count - 1 | loop 0
        // CF: CheckConcurrentObjects.Run | for j | loop 0
        [Test]
        public void TestNoPairs()
        {
            Assert.That(run(), Is.Empty);
            Assert.That(run(circle(0)), Is.Empty);
            Assert.That(check.PossibleTemplates.Count(), Is.EqualTo(2));
        }

        // CF: CheckConcurrentObjects.Run | !AreConcurrent && !AreAlmostConcurrent | TRUE
        // CF: CheckConcurrentObjects.Run | AreConcurrent | FALSE
        // CF: CheckConcurrentObjects.AreAlmostConcurrent | Math.Abs(diff) < 10 | FALSE
        [Test]
        public void TestWellSeparatedObjects()
        {
            Assert.That(run(circle(0), circle(500), circle(1000)), Is.Empty);
        }

        // CF: CheckConcurrentObjects.Run | AreConcurrent | TRUE
        // CF: CheckConcurrentObjects.AreConcurrent | next.StartTime <= end + ms_leniency | TRUE
        // CF: CheckConcurrentObjects.Run | !AreConcurrent && !AreAlmostConcurrent | FALSE
        [Test]
        public void TestConcurrentObjectsIncludingLeniencyBoundary()
        {
            Assert.That(run(circle(1000), circle(1000)).Single().Template, Is.InstanceOf<CheckConcurrentObjects.IssueTemplateConcurrent>());
            Assert.That(run(circle(1000), circle(1000 + CheckUnsnappedObjects.UNSNAP_MS_THRESHOLD)).Single().Template, Is.InstanceOf<CheckConcurrentObjects.IssueTemplateConcurrent>());
        }

        // CF: CheckConcurrentObjects.AreConcurrent | next.StartTime <= end + ms_leniency | FALSE
        // CF: CheckConcurrentObjects.AreAlmostConcurrent | Math.Abs(diff) < 10 | TRUE
        // CF: CheckConcurrentObjects.Run | AreAlmostConcurrent | TRUE
        [Test]
        public void TestAlmostConcurrentObjects()
        {
            Assert.That(run(circle(1000), circle(1005)).Single().Template, Is.InstanceOf<CheckConcurrentObjects.IssueTemplateAlmostConcurrent>());
            Assert.That(run(circle(1000), circle(1020)), Is.Empty);
        }

        // CF: CheckConcurrentObjects.Run | for j | loop n
        [Test]
        public void TestThreeConcurrentObjectsProducePairwiseIssues()
        {
            Assert.That(run(circle(1000), circle(1000), circle(1000)).Length, Is.EqualTo(3));
        }

        [Test]
        public void TestIterationStopsAtFirstNonConcurrentObject()
        {
            Assert.That(run(circle(1000), circle(1000), circle(5000), circle(5000)).Length, Is.EqualTo(2), "one pair at 1000 and one pair at 5000, nothing across");
        }

        // CF: CheckConcurrentObjects.IssueTemplateConcurrent.Create | same type ? ... : ... | TRUE
        // CF: CheckConcurrentObjects.IssueTemplateConcurrent.Create | same type ? ... : ... | FALSE
        // CF: CheckConcurrentObjects.IssueTemplateAlmostConcurrent.Create | same type ? ... : ... | TRUE
        // CF: CheckConcurrentObjects.IssueTemplateAlmostConcurrent.Create | same type ? ... : ... | FALSE
        [Test]
        public void TestIssueMessagesAndEndTimes()
        {
            var same = run(circle(1000), circle(1000)).Single();
            Assert.That(same.Arguments[0], Is.EqualTo("HitCircles are concurrent here."));
            Assert.That(same.Time, Is.EqualTo(1000));

            // spinner ends at 1500, so a circle at 1400 is concurrent with it.
            var mixed = run(new Spinner { StartTime = 1000, Duration = 500 }, circle(1400)).Single();
            Assert.That(mixed.Arguments[0], Is.EqualTo("Spinner and HitCircle are concurrent here."));

            var almostSame = run(circle(1000), circle(1005)).Single();
            Assert.That(almostSame.Arguments[0], Is.EqualTo("HitCircles are less than 10ms apart."));

            var almostMixed = run(circle(1000), new Spinner { StartTime = 1005, Duration = 100 }).Single();
            Assert.That(almostMixed.Arguments[0], Is.EqualTo("HitCircle and Spinner are less than 10ms apart."));
        }
    }
}
