// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckFewHitsoundsTest
    {
        private class TestCheck : CheckFewHitsounds
        {
        }

        private class SpinnerExcludingCheck : CheckFewHitsounds
        {
            protected override bool IsExcludedFromHitsounding(HitObject hitObject) => hitObject is Spinner;
        }

        private static HitCircle hitsounded(double time) => new HitCircle
        {
            StartTime = time,
            Samples = new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL), new HitSampleInfo(HitSampleInfo.HIT_WHISTLE) }
        };

        private static HitCircle plain(double time) => new HitCircle { StartTime = time, Samples = new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) } };

        private static HitCircle silent(double time) => new HitCircle { StartTime = time };

        private static Issue[] run(ICheck check, IEnumerable<HitObject> objects, params BreakPeriod[] breaks)
        {
            var beatmap = new Beatmap();
            beatmap.HitObjects.AddRange(objects);
            beatmap.Breaks.AddRange(breaks);
            return check.Run(CheckTestUtils.Context(beatmap)).ToArray();
        }

        // hitsound at 0, `count` plain objects in between and a hitsound at `end`.
        private static List<HitObject> sandwich(int count, double end, bool plainObjects = true)
        {
            var objects = new List<HitObject> { hitsounded(0) };
            for (int i = 1; i <= count; i++)
                objects.Add(plainObjects ? plain(end * i / (count + 1)) : silent(end * i / (count + 1)));
            objects.Add(hitsounded(end));
            return objects;
        }

        // CF: CheckFewHitsounds.Run | !HitObjects.Any() | TRUE
        [Test]
        public void TestNoObjects()
        {
            Assert.That(run(new TestCheck(), new List<HitObject>()), Is.Empty);
            Assert.That(new TestCheck().PossibleTemplates.Count(), Is.EqualTo(4));
        }

        // CF: CheckFewHitsounds.Run | !HitObjects.Any() | FALSE
        // CF: CheckFewHitsounds.Run | !mapHasHitsounds | TRUE
        // CF: CheckFewHitsounds.applyHitsoundUpdate | hasHitsound || (isLastObject && mapHasHitsounds) | FALSE
        // CF: CheckFewHitsounds.applyHitsoundUpdate | couldHaveHitsound | TRUE
        [Test]
        public void TestMapWithoutHitsounds()
        {
            var issue = run(new TestCheck(), Enumerable.Range(0, 20).Select(i => plain(i * 1000))).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckFewHitsounds.IssueTemplateNoHitsounds>());
        }

        // CF: CheckFewHitsounds.applyHitsoundUpdate | couldHaveHitsound | FALSE
        // CF: CheckFewHitsounds.Run | foreach hitObject | loop n
        [Test]
        public void TestObjectsWithoutSamplesAreNotCounted()
        {
            // 6 silent objects between two hitsounds do not count towards "objects without hitsounds".
            Assert.That(run(new TestCheck(), sandwich(6, 20_000, plainObjects: false)), Is.Empty);
        }

        // CF: CheckFewHitsounds.Run | mapHasHitsounds | TRUE
        // CF: CheckFewHitsounds.applyHitsoundUpdate | hasHitsound | TRUE
        // CF: CheckFewHitsounds.applyHitsoundUpdate | timeWithoutHitsounds > problem_threshold_time && objects > problem_threshold_objects | TRUE
        [Test]
        public void TestLongPeriodProblem()
        {
            var issue = run(new TestCheck(), sandwich(17, 25_000)).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckFewHitsounds.IssueTemplateLongPeriodProblem>());
            Assert.That(issue.Time, Is.Zero);
        }

        // CF: CheckFewHitsounds.applyHitsoundUpdate | timeWithoutHitsounds > problem_threshold_time && objects > problem_threshold_objects | FALSE
        // CF: CheckFewHitsounds.applyHitsoundUpdate | timeWithoutHitsounds > warning_threshold_time && objects > warning_threshold_objects | TRUE
        [Test]
        public void TestLongPeriodWarning()
        {
            Assert.That(run(new TestCheck(), sandwich(6, 10_000)).Single().Template, Is.InstanceOf<CheckFewHitsounds.IssueTemplateLongPeriodWarning>());
            Assert.That(run(new TestCheck(), sandwich(10, 30_000)).Single().Template, Is.InstanceOf<CheckFewHitsounds.IssueTemplateLongPeriodWarning>(), "long enough for a problem but too few objects");
        }

        // CF: CheckFewHitsounds.applyHitsoundUpdate | timeWithoutHitsounds > warning_threshold_time && objects > warning_threshold_objects | FALSE
        // CF: CheckFewHitsounds.applyHitsoundUpdate | timeWithoutHitsounds > negligible_threshold_time && objects > warning_threshold_objects | TRUE
        [Test]
        public void TestLongPeriodNegligible()
        {
            Assert.That(run(new TestCheck(), sandwich(6, 5_000)).Single().Template, Is.InstanceOf<CheckFewHitsounds.IssueTemplateLongPeriodNegligible>());
        }

        // CF: CheckFewHitsounds.applyHitsoundUpdate | timeWithoutHitsounds > negligible_threshold_time && objects > warning_threshold_objects | FALSE
        [Test]
        public void TestShortOrSparsePeriodsAreFine()
        {
            Assert.That(run(new TestCheck(), sandwich(6, 3_000)), Is.Empty, "too short");
            Assert.That(run(new TestCheck(), sandwich(3, 30_000)), Is.Empty, "too few objects");
        }

        // CF: CheckFewHitsounds.applyHitsoundUpdate | isLastObject && mapHasHitsounds | TRUE
        [Test]
        public void TestPeriodAfterLastHitsoundIsReported()
        {
            var objects = new List<HitObject> { hitsounded(0) };
            for (int i = 1; i <= 6; i++)
                objects.Add(plain(i * 2000));

            Assert.That(run(new TestCheck(), objects).Single().Template, Is.InstanceOf<CheckFewHitsounds.IssueTemplateLongPeriodWarning>());
        }

        // CF: CheckFewHitsounds.getTimeWithoutHitsounds | end <= start | TRUE
        // CF: CheckFewHitsounds.getTimeWithoutHitsounds | overlapEnd > overlapStart | TRUE
        // CF: CheckFewHitsounds.getTimeWithoutHitsounds | overlapEnd > overlapStart | FALSE
        // CF: CheckFewHitsounds.Run | excludedTimeRanges from breaks | loop n
        [Test]
        public void TestBreaksAreExcludedFromThePeriod()
        {
            // 10s between hitsounds minus an 8s break = 2s.
            Assert.That(run(new TestCheck(), sandwich(6, 10_000), new BreakPeriod(1_000, 9_000)), Is.Empty);

            // a break entirely outside the period has no effect.
            Assert.That(run(new TestCheck(), sandwich(6, 10_000), new BreakPeriod(20_000, 21_000)).Single().Template, Is.InstanceOf<CheckFewHitsounds.IssueTemplateLongPeriodWarning>());
        }

        // CF: CheckFewHitsounds.Run | IsExcludedFromHitsounding(hitObject) | TRUE
        // CF: CheckFewHitsounds.Run | IsExcludedFromHitsounding(hitObject) | FALSE
        [Test]
        public void TestExcludedObjectsAreIgnoredAndTheirTimeIsSubtracted()
        {
            var objects = sandwich(6, 10_000);
            objects.Add(new Spinner { StartTime = 10_001, Duration = 100, Samples = new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) } });

            Assert.That(run(new SpinnerExcludingCheck(), objects).Single().Template, Is.InstanceOf<CheckFewHitsounds.IssueTemplateLongPeriodWarning>(), "the spinner is neither counted nor does it start a new period");

            var spanning = sandwich(6, 10_000);
            spanning.Add(new Spinner { StartTime = 1_000, Duration = 8_000 });
            Assert.That(run(new SpinnerExcludingCheck(), spanning), Is.Empty, "excluded spinner time shortens the hitsound-less period");
        }
    }
}
