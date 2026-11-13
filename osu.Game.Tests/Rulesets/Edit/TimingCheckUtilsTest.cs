// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Edit.Checks.Components;

namespace osu.Game.Tests.Rulesets.Edit
{
    [TestFixture]
    public class TimingCheckUtilsTest
    {
        private static readonly TimingControlPoint[] points = [new TimingControlPoint { Time = 1000.4 }, new TimingControlPoint { Time = 2000 }];

        // CF: TimingCheckUtils.FindMatchingTimingPoint | (int)tp.Time == (int)time | TRUE
        [Test]
        public void TestMatchingPointComparedAtMillisecondPrecision()
        {
            Assert.That(TimingCheckUtils.FindMatchingTimingPoint(points, 1000.9), Is.SameAs(points[0]));
            Assert.That(TimingCheckUtils.FindMatchingTimingPoint(points, 2000), Is.SameAs(points[1]));
        }

        // CF: TimingCheckUtils.FindMatchingTimingPoint | (int)tp.Time == (int)time | FALSE
        [Test]
        public void TestNoMatchingPoint()
        {
            Assert.That(TimingCheckUtils.FindMatchingTimingPoint(points, 1500), Is.Null);
            Assert.That(TimingCheckUtils.FindMatchingTimingPoint([], 1000), Is.Null);
        }

        // CF: TimingCheckUtils.FindExactMatchingTimingPoint | Precision.AlmostEquals(tp.Time, time, tolerance) | TRUE
        [Test]
        public void TestExactMatchWithinTolerance()
        {
            Assert.That(TimingCheckUtils.FindExactMatchingTimingPoint(points, 1000.4 + TimingCheckUtils.TIME_OFFSET_TOLERANCE_MS / 2), Is.SameAs(points[0]));
        }

        // CF: TimingCheckUtils.FindExactMatchingTimingPoint | Precision.AlmostEquals(tp.Time, time, tolerance) | FALSE
        [Test]
        public void TestExactMatchOutsideTolerance()
        {
            Assert.That(TimingCheckUtils.FindExactMatchingTimingPoint(points, 1000.9), Is.Null, "within a millisecond is not exact");
            Assert.That(TimingCheckUtils.FindExactMatchingTimingPoint([], 1000), Is.Null);
        }
    }
}
