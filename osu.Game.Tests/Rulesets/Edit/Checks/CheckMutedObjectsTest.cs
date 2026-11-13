// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckMutedObjectsTest
    {
        private readonly CheckMutedObjects check = new CheckMutedObjects();

        private class RepeatingParent : HitObject, IHasRepeats
        {
            public RepeatingParent(double start, double end, int repeats, params HitObject[] nested)
            {
                StartTime = start;
                EndTime = end;
                RepeatCount = repeats;

                foreach (var n in nested)
                    AddNested(n);
            }

            public double EndTime { get; }

            public double Duration
            {
                get => EndTime - StartTime;
                set { }
            }

            public int RepeatCount { get; set; }
            public IList<IList<HitSampleInfo>> NodeSamples { get; set; } = new List<IList<HitSampleInfo>>();
        }

        private static List<HitSampleInfo> samples(int volume) => new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, volume: volume) };

        private Issue[] run(params HitObject[] objects)
        {
            var beatmap = new Beatmap();
            beatmap.HitObjects.AddRange(objects);
            return check.Run(CheckTestUtils.Context(beatmap)).ToArray();
        }

        // CF: CheckMutedObjects.Run | foreach hitObject | loop 0
        // CF: CheckMutedObjects.getVolumeIssues | !sampledHitObject.Samples.Any() | TRUE
        [Test]
        public void TestNothingToCheck()
        {
            Assert.That(run(), Is.Empty);
            Assert.That(run(new HitCircle { StartTime = 100 }), Is.Empty);
            Assert.That(check.PossibleTemplates.Count(), Is.EqualTo(3));
        }

        // CF: CheckMutedObjects.getVolumeIssues | maxVolume <= muted_threshold | TRUE
        // CF: CheckMutedObjects.getVolumeIssues | edgeType == Head | TRUE
        // CF: CheckMutedObjects.getEdgeAtTime | AlmostEquals(time, StartTime) | TRUE
        [TestCase(0)]
        [TestCase(5)]
        public void TestMutedCircleIsActiveIssue(int volume)
        {
            var issue = run(new HitCircle { StartTime = 100, Samples = samples(volume) }).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckMutedObjects.IssueTemplateMutedActive>());
            Assert.That(issue.Arguments[0], Is.EqualTo("HitCircle"));
            Assert.That(issue.Time, Is.EqualTo(100));
        }

        // CF: CheckMutedObjects.getVolumeIssues | maxVolume <= muted_threshold | FALSE
        // CF: CheckMutedObjects.getVolumeIssues | maxVolume <= low_volume_threshold && edgeType == Head | TRUE
        [TestCase(6)]
        [TestCase(20)]
        public void TestLowVolumeCircle(int volume)
        {
            Assert.That(run(new HitCircle { StartTime = 100, Samples = samples(volume) }).Single().Template, Is.InstanceOf<CheckMutedObjects.IssueTemplateLowVolumeActive>());
        }

        // CF: CheckMutedObjects.getVolumeIssues | maxVolume <= low_volume_threshold | FALSE
        [Test]
        public void TestNormalVolumeCircle()
        {
            Assert.That(run(new HitCircle { StartTime = 100, Samples = samples(21) }), Is.Empty);
        }

        // CF: CheckMutedObjects.getVolumeIssues | edgeType == Head | FALSE
        // CF: CheckMutedObjects.getEdgeAtTime | AlmostEquals(time, GetEndTime()) | TRUE
        // CF: CheckMutedObjects.getVolumeIssues | hitObject is IHasDuration | TRUE
        [Test]
        public void TestMutedSpinnerEndIsPassiveIssueWithPostfix()
        {
            var issue = run(new Spinner { StartTime = 1000, Duration = 500, Samples = samples(0) }).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckMutedObjects.IssueTemplateMutedPassive>());
            Assert.That(issue.Arguments[0], Is.EqualTo("Spinner tail"));
            Assert.That(issue.Time, Is.EqualTo(1500));
        }

        // CF: CheckMutedObjects.getVolumeIssues | maxVolume <= low_volume_threshold && edgeType == Head | FALSE
        [Test]
        public void TestLowVolumeTailIsNotReported()
        {
            Assert.That(run(new Spinner { StartTime = 1000, Duration = 500, Samples = samples(10) }), Is.Empty);
        }

        // CF: CheckMutedObjects.getEdgeAtTime | hitObject is IHasRepeats | TRUE
        // CF: CheckMutedObjects.getEdgeAtTime | spans close to an integer | TRUE
        // CF: CheckMutedObjects.getVolumeIssues | foreach nestedHitObject | loop n
        [Test]
        public void TestNestedRepeatSampleIsPassive()
        {
            var parent = new RepeatingParent(1000, 3000, 1, new HitObject { StartTime = 2000, Samples = samples(3) });

            var issue = run(parent).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckMutedObjects.IssueTemplateMutedPassive>());
            Assert.That(issue.Arguments[0], Is.EqualTo("RepeatingParent repeat"));
        }

        // CF: CheckMutedObjects.getEdgeAtTime | spans close to an integer | FALSE
        // CF: CheckMutedObjects.getEdgeAtTime | edgeType == None | TRUE
        [Test]
        public void TestNestedSampleBetweenEdgesIsIgnored()
        {
            Assert.That(run(new RepeatingParent(1000, 3000, 1, new HitObject { StartTime = 1500, Samples = samples(3) })), Is.Empty, "e.g. slider ticks");
        }

        // CF: CheckMutedObjects.getEdgeAtTime | hitObject is IHasRepeats | FALSE
        [Test]
        public void TestNonRepeatingParentWithNestedSampleBetweenEdgesIsIgnored()
        {
            var parent = new Spinner { StartTime = 1000, Duration = 1000 };
            Assert.That(run(parent), Is.Empty);
        }

        // CF: CheckMutedObjects.getEdgeAtTime | spanDuration <= 0 | TRUE
        [Test]
        public void TestNegativeLengthRepeatingObjectDoesNotThrow()
        {
            Assert.That(run(new RepeatingParent(1000, 900, 1, new HitObject { StartTime = 950, Samples = samples(0) })), Is.Empty);
        }
    }
}
