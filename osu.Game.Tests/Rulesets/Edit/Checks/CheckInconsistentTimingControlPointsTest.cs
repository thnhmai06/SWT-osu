// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckInconsistentTimingControlPointsTest
    {
        private readonly CheckInconsistentTimingControlPoints check = new CheckInconsistentTimingControlPoints();

        private static Beatmap beatmap(string name, params TimingControlPoint[] points)
        {
            var b = new Beatmap();
            b.BeatmapInfo.DifficultyName = name;
            foreach (var p in points)
                b.ControlPointInfo.Add(p.Time, p);
            return b;
        }

        private static TimingControlPoint point(double time, double beatLength = 500, int meter = 4) => new TimingControlPoint { Time = time, BeatLength = beatLength, TimeSignature = new TimeSignature(meter) };

        private Issue[] run(Beatmap current, params Beatmap[] others)
        {
            var context = new BeatmapVerifierContext(
                new BeatmapVerifierContext.VerifiedBeatmap(new TestWorkingBeatmap(current), current),
                others.Select(o => new BeatmapVerifierContext.VerifiedBeatmap(new TestWorkingBeatmap(o), o)).ToList(),
                DifficultyRating.ExpertPlus);

            return check.Run(context).ToArray();
        }

        // CF: CheckInconsistentTimingControlPoints.Run | context.AllDifficulties.Count() <= 1 | TRUE
        [Test]
        public void TestSingleDifficultyHasNoIssues()
        {
            Assert.That(run(beatmap("a", point(0))), Is.Empty);
            Assert.That(check.PossibleTemplates.Count(), Is.EqualTo(5));
        }

        // CF: CheckInconsistentTimingControlPoints.Run | context.AllDifficulties.Count() <= 1 | FALSE
        // CF: CheckInconsistentTimingControlPoints.Run | foreach other difficulty | loop 1
        // CF: CheckInconsistentTimingControlPoints.Run | matchingPoint == null | FALSE
        // CF: CheckInconsistentTimingControlPoints.Run | !referencePoint.TimeSignature.Equals(matchingPoint.TimeSignature) | FALSE
        // CF: CheckInconsistentTimingControlPoints.Run | BPM difference > tolerance | FALSE
        // CF: CheckInconsistentTimingControlPoints.Run | exactMatchingPoint == null | FALSE
        // CF: CheckInconsistentTimingControlPoints.Run | matchingReferencePoint == null | FALSE
        // CF: CheckInconsistentTimingControlPoints.Run | exactMatchingReferencePoint == null | FALSE
        [Test]
        public void TestIdenticalTimingPoints()
        {
            Assert.That(run(beatmap("a", point(0), point(5000)), beatmap("b", point(0), point(5000))), Is.Empty);
        }

        // CF: CheckInconsistentTimingControlPoints.Run | matchingPoint == null | TRUE
        [Test]
        public void TestMissingTimingPoint()
        {
            var issue = run(beatmap("a", point(0), point(5000)), beatmap("b", point(0))).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckInconsistentTimingControlPoints.IssueTemplateMissingTimingPoint>());
        }

        // CF: CheckInconsistentTimingControlPoints.Run | matchingReferencePoint == null | TRUE
        // CF: CheckInconsistentTimingControlPoints.Run | foreach timingPoint of other | loop n
        [Test]
        public void TestExtraTimingPoint()
        {
            var issue = run(beatmap("a", point(0)), beatmap("b", point(0), point(5000))).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckInconsistentTimingControlPoints.IssueTemplateExtraTimingPoint>());
        }

        // CF: CheckInconsistentTimingControlPoints.Run | !referencePoint.TimeSignature.Equals(matchingPoint.TimeSignature) | TRUE
        [Test]
        public void TestInconsistentMeter()
        {
            var issue = run(beatmap("a", point(0, meter: 4)), beatmap("b", point(0, meter: 3))).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckInconsistentTimingControlPoints.IssueTemplateInconsistentMeter>());
        }

        // CF: CheckInconsistentTimingControlPoints.Run | BPM difference > tolerance | TRUE
        [Test]
        public void TestInconsistentBpm()
        {
            var issue = run(beatmap("a", point(0, 500)), beatmap("b", point(0, 400))).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckInconsistentTimingControlPoints.IssueTemplateInconsistentBPM>());
            Assert.That(run(beatmap("a", point(0, 500)), beatmap("b", point(0, 500.005))), Is.Empty, "differences within tolerance are ignored");
        }

        // CF: CheckInconsistentTimingControlPoints.Run | exactMatchingPoint == null | TRUE
        // CF: CheckInconsistentTimingControlPoints.Run | exactMatchingReferencePoint == null | TRUE
        [Test]
        public void TestSubMillisecondOffsetIsReportedFromBothSides()
        {
            var issues = run(beatmap("a", point(1000)), beatmap("b", point(1000.5)));

            Assert.That(issues.Length, Is.EqualTo(2));
            Assert.That(issues.All(i => i.Template is CheckInconsistentTimingControlPoints.IssueTemplateMissingTimingPointMinor), Is.True);
        }

        // CF: CheckInconsistentTimingControlPoints.Run | foreach other difficulty | loop n
        [Test]
        public void TestAllOtherDifficultiesAreChecked()
        {
            var issues = run(beatmap("a", point(0)), beatmap("b", point(0, 400)), beatmap("c", point(0, 300)));

            Assert.That(issues.Length, Is.EqualTo(2));
        }
    }
}
