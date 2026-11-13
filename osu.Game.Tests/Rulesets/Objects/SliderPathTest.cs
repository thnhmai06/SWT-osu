// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Tests.Rulesets.Objects
{
    [TestFixture]
    public class SliderPathTest
    {
        private static SliderPath line(double? expected = null) => new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }, expected);

        // CF: SliderPath.calculatePath | ControlPoints.Count == 0 | TRUE
        // CF: SliderPath.Distance | cumulativeLength.Count == 0 | FALSE
        // CF: SliderPath.interpolateVertices | calculatedPath.Count == 0 | TRUE
        // CF: SliderPath.GetSegmentEnds | segmentEndDistances | loop 0
        [Test]
        public void TestEmptyPath()
        {
            var path = new SliderPath();

            Assert.That(path.CalculatedDistance, Is.Zero);
            Assert.That(path.PositionAt(0.5), Is.EqualTo(Vector2.Zero));
            Assert.That(path.GetSegmentEnds(), Is.Empty);
        }

        // CF: SliderPath.calculatePath | ControlPoints.Count == 0 | FALSE
        // CF: SliderPath.calculatePath | ControlPoints[i].Type == null | FALSE
        // CF: SliderPath.calculateSubPath | case Linear | case Linear
        // CF: SliderPath.calculateLength | ExpectedDistance.Value is double | FALSE
        // CF: SliderPath.PositionAt | progress clamped to [0,1] | no throw
        [Test]
        public void TestLinearPath()
        {
            var path = line();

            Assert.That(path.Distance, Is.EqualTo(100).Within(1e-3));
            Assert.That(path.PositionAt(0.5).X, Is.EqualTo(50).Within(1e-3));
            Assert.That(path.PositionAt(-1), Is.EqualTo(Vector2.Zero));
            Assert.That(path.PositionAt(2).X, Is.EqualTo(100).Within(1e-3));
        }

        // CF: SliderPath.calculatePath | segmentVertices.Length == 1 | TRUE
        [Test]
        public void TestSingleControlPoint()
        {
            var path = new SliderPath(new[] { new PathControlPoint(new Vector2(5, 5)) });

            Assert.That(path.CalculatedPath, Is.EqualTo(new[] { new Vector2(5, 5) }));
            Assert.That(path.Distance, Is.Zero);
        }

        // CF: SliderPath.calculateLength | ExpectedDistance.Value is double | TRUE
        // CF: SliderPath.calculateLength | calculatedLength != expectedDistance | TRUE
        // CF: SliderPath.calculateLength | calculatedLength > expectedDistance | TRUE
        // CF: SliderPath.calculateLength | pathEndIndex <= 0 | FALSE
        [Test]
        public void TestExpectedDistanceShortens()
        {
            var path = line(50);

            Assert.That(path.Distance, Is.EqualTo(50).Within(1e-3));
            Assert.That(path.CalculatedDistance, Is.EqualTo(100).Within(1e-3));
            Assert.That(path.CalculatedPath.Last().X, Is.EqualTo(50).Within(1e-3));
        }

        // CF: SliderPath.calculateLength | calculatedLength > expectedDistance | FALSE
        // CF: SliderPath.calculateLength | calculatedPath.Count >= 2 && last two equal | FALSE
        [Test]
        public void TestExpectedDistanceLengthens()
        {
            var path = line(150);

            Assert.That(path.Distance, Is.EqualTo(150).Within(1e-3));
            Assert.That(path.CalculatedDistance, Is.EqualTo(100).Within(1e-3));
        }

        // CF: SliderPath.calculateLength | calculatedLength != expectedDistance | FALSE
        [Test]
        public void TestExpectedDistanceEqualToCalculated()
        {
            Assert.That(line(100).Distance, Is.EqualTo(100).Within(1e-3));
        }

        // CF: SliderPath.calculateLength | calculatedPath.Count >= 2 | TRUE
        // CF: SliderPath.calculateLength | calculatedPath[^1] == calculatedPath[^2] | TRUE
        // CF: SliderPath.calculateLength | expectedDistance > calculatedLength | TRUE
        [Test]
        public void TestNoExtensionWhenLastTwoPointsEqual()
        {
            var path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0), new Vector2(100, 0) }, 200);

            Assert.That(path.Distance, Is.EqualTo(100).Within(1e-3));
        }

        // CF: SliderPath.calculateLength | expectedDistance > calculatedLength | FALSE
        [Test]
        public void TestEqualLastPointsStillShortened()
        {
            var path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0), new Vector2(100, 0) }, 40);

            Assert.That(path.Distance, Is.EqualTo(40).Within(1e-3));
        }

        // CF: SliderPath.calculateLength | pathEndIndex <= 0 | TRUE
        // CF: SliderPath.calculateLength | cumulativeLength.Count > 0 && cumulativeLength[^1] >= expectedDistance | TRUE
        [Test]
        public void TestZeroExpectedDistance()
        {
            Assert.That(line(0).Distance, Is.Zero);
        }

        // CF: SliderPath.calculateLength | cumulativeLength.Count > 0 | FALSE
        // CF: SliderPath.calculateLength | while loop | loop n
        [Test]
        public void TestNegativeExpectedDistance()
        {
            Assert.That(line(-10).Distance, Is.Zero);
        }

        // CF: SliderPath.calculatePath | i < ControlPoints.Count - 1 && Type == null | TRUE
        // CF: SliderPath.calculatePath | skipFirst | TRUE
        // CF: SliderPath.calculatePath | i > 0 | TRUE
        // CF: SliderPath.GetSegmentEnds | segmentEndDistances | loop n
        [Test]
        public void TestMultipleSegmentsShareJoinVertex()
        {
            var path = new SliderPath(new[]
            {
                new PathControlPoint(Vector2.Zero, PathType.LINEAR),
                new PathControlPoint(new Vector2(100, 0), PathType.LINEAR),
                new PathControlPoint(new Vector2(100, 100)),
            });

            Assert.That(path.CalculatedPath.Count, Is.EqualTo(3));
            Assert.That(path.Distance, Is.EqualTo(200).Within(1e-3));
            Assert.That(path.GetSegmentEnds().ToArray(), Is.EqualTo(new[] { 0.5, 1 }).Within(1e-6));
        }

        // CF: SliderPath.calculatePath | skipFirst | FALSE
        [Test]
        public void TestDisjointSegmentsKeepBothVertices()
        {
            var path = new SliderPath(new[]
            {
                new PathControlPoint(Vector2.Zero, PathType.LINEAR),
                new PathControlPoint(new Vector2(100, 0), PathType.LINEAR),
                new PathControlPoint(new Vector2(100, 0), PathType.LINEAR),
                new PathControlPoint(new Vector2(100, 50)),
            });

            Assert.That(path.CalculatedPath.Count, Is.GreaterThanOrEqualTo(3));
        }

        // CF: SliderPath.calculateSubPath | case PerfectCurve | case PerfectCurve
        // CF: SliderPath.calculateSubPath | subControlPoints.Length != 3 | FALSE
        // CF: SliderPath.calculateSubPath | !circularArcProperties.IsValid | FALSE
        // CF: SliderPath.calculateSubPath | subPoints >= 1000 | FALSE
        // CF: SliderPath.calculateSubPath | subPath.Count == 0 | FALSE
        [Test]
        public void TestPerfectCurveFollowsCircle()
        {
            // quarter circle of radius 100 around the origin.
            var path = new SliderPath(PathType.PERFECT_CURVE, new[] { new Vector2(100, 0), new Vector2(70.7107f, 70.7107f), new Vector2(0, 100) });

            Assert.That(path.CalculatedDistance, Is.EqualTo(System.Math.PI * 100 / 2).Within(1));
            Assert.That(path.PositionAt(0.5).Length, Is.EqualTo(100).Within(0.5));
        }

        // CF: SliderPath.calculateSubPath | subControlPoints.Length != 3 | TRUE
        [Test]
        public void TestPerfectCurveWithFourPointsFallsBackToBezier()
        {
            var points = new[] { Vector2.Zero, new Vector2(50, 50), new Vector2(100, 50), new Vector2(150, 0) };
            var perfect = new SliderPath(PathType.PERFECT_CURVE, points);
            var bezier = new SliderPath(PathType.BEZIER, points);

            Assert.That(perfect.CalculatedDistance, Is.EqualTo(bezier.CalculatedDistance).Within(1e-3));
        }

        // CF: SliderPath.calculateSubPath | !circularArcProperties.IsValid | TRUE
        [Test]
        public void TestPerfectCurveWithCollinearPointsFallsBackToBezier()
        {
            var path = new SliderPath(PathType.PERFECT_CURVE, new[] { Vector2.Zero, new Vector2(50, 0), new Vector2(100, 0) });

            Assert.That(path.CalculatedDistance, Is.EqualTo(100).Within(1e-3));
        }

        // CF: SliderPath.calculateSubPath | subPoints >= 1000 | TRUE
        [Test]
        public void TestPerfectCurveWithHugeArcFallsBackToBezier()
        {
            // 3/4 of a circle with radius 50000 would need more than 1000 sub-points, so a (much shorter) bezier is used instead.
            var path = new SliderPath(PathType.PERFECT_CURVE, new[] { new Vector2(50000, 0), new Vector2(-35355.34f, 35355.34f), new Vector2(0, -50000) });

            Assert.That(path.CalculatedDistance, Is.LessThan(System.Math.PI * 1.5 * 50000 * 0.9));
        }

        // CF: SliderPath.calculateSubPath | default: BSpline fallthrough | default
        [Test]
        public void TestBezierPathEndsAtLastControlPoint()
        {
            var path = new SliderPath(PathType.BEZIER, new[] { Vector2.Zero, new Vector2(50, 100), new Vector2(100, 0) });

            Assert.That(path.PositionAt(1).X, Is.EqualTo(100).Within(1e-2));
            Assert.That(path.PositionAt(1).Y, Is.EqualTo(0).Within(1e-2));
        }

        // CF: SliderPath.calculateSubPath | case Catmull | case Catmull
        // CF: SliderPath.calculateSubPath | !OptimiseCatmull | TRUE
        [Test]
        public void TestCatmullWithoutOptimisation()
        {
            var path = new SliderPath(PathType.CATMULL, new[] { Vector2.Zero, new Vector2(50, 0), new Vector2(50, 0), new Vector2(100, 0) });

            Assert.That(path.CalculatedPath.Count, Is.GreaterThan(10));
        }

        // CF: SliderPath.calculateSubPath | !OptimiseCatmull | FALSE
        // CF: SliderPath.calculateSubPath | lastStart == null | TRUE
        // CF: SliderPath.calculateSubPath | lastStart == null | FALSE
        // CF: SliderPath.calculateSubPath | distFromStart > 6 | TRUE
        // CF: SliderPath.calculateSubPath | distFromStart > 6 | FALSE
        // CF: SliderPath.calculateSubPath | i == subPath.Count - 1 | TRUE
        [Test]
        public void TestCatmullWithOptimisationRemovesVertices()
        {
            var points = new[] { Vector2.Zero, new Vector2(50, 0), new Vector2(50, 0), new Vector2(100, 0) };

            var plain = new SliderPath(PathType.CATMULL, points);
            var optimised = new SliderPath(PathType.CATMULL, points) { OptimiseCatmull = true };

            Assert.That(optimised.CalculatedPath.Count, Is.LessThan(plain.CalculatedPath.Count));
            Assert.That(optimised.CalculatedPath.Last(), Is.EqualTo(plain.CalculatedPath.Last()));
        }

        // CF: SliderPath.PointsInSegment | point.Type != null | TRUE
        // CF: SliderPath.PointsInSegment | !found | TRUE
        // CF: SliderPath.PointsInSegment | !found | FALSE
        // CF: SliderPath.PointsInSegment | point == controlPoint | TRUE
        // CF: SliderPath.PointsInSegment | point == controlPoint | FALSE
        [Test]
        public void TestPointsInSegment()
        {
            var p0 = new PathControlPoint(Vector2.Zero, PathType.LINEAR);
            var p1 = new PathControlPoint(new Vector2(10, 0));
            var p2 = new PathControlPoint(new Vector2(20, 0), PathType.LINEAR);
            var p3 = new PathControlPoint(new Vector2(30, 0));
            var path = new SliderPath(new[] { p0, p1, p2, p3 });

            Assert.That(path.PointsInSegment(p1), Is.EqualTo(new List<PathControlPoint> { p0, p1, p2 }));
            Assert.That(path.PointsInSegment(p3), Is.EqualTo(new List<PathControlPoint> { p2, p3 }));
        }

        // CF: SliderPath.GetPathToProgress | for (i < calculatedPath.Count && cumulativeLength[i] < d0) | loop n
        // CF: SliderPath.GetPathToProgress | for (i < calculatedPath.Count && cumulativeLength[i] <= d1) | loop n
        [Test]
        public void TestGetPathToProgressIncludesInteriorVertices()
        {
            var path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(50, 0), new Vector2(100, 0) });
            var result = new List<Vector2> { new Vector2(999, 999) };

            path.GetPathToProgress(result, 0.25, 0.75);

            Assert.That(result.Select(v => v.X).ToArray(), Is.EqualTo(new[] { 25f, 50f, 75f }).Within(1e-3));
        }

        // CF: SliderPath.GetPathToProgress | for (i < calculatedPath.Count && cumulativeLength[i] < d0) | loop 0
        // CF: SliderPath.interpolateVertices | i <= 0 | TRUE
        // CF: SliderPath.interpolateVertices | i >= calculatedPath.Count | TRUE
        [Test]
        public void TestGetPathToProgressFullRange()
        {
            var result = new List<Vector2>();

            line().GetPathToProgress(result, 0, 1);

            Assert.That(result.First(), Is.EqualTo(Vector2.Zero));
            Assert.That(result.Last().X, Is.EqualTo(100).Within(1e-3));
        }

        // CF: SliderPath.interpolateVertices | Precision.AlmostEquals(d0, d1) | TRUE
        [Test]
        public void TestCoincidentVerticesDoNotDivideByZero()
        {
            var path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, Vector2.Zero, new Vector2(100, 0) });

            Assert.That(path.PositionAt(0), Is.EqualTo(Vector2.Zero));
            Assert.That(float.IsNaN(path.PositionAt(0.5).X), Is.False);
        }

        // CF: SliderPath.HasValidLengthForPlacement | Precision.DefinitelyBigger(Distance, 0, 1) | TRUE
        // CF: SliderPath.HasValidLengthForPlacement | Precision.DefinitelyBigger(Distance, 0, 1) | FALSE
        [Test]
        public void TestHasValidLengthForPlacement()
        {
            Assert.That(line().HasValidLengthForPlacement, Is.True);
            Assert.That(line(0.5).HasValidLengthForPlacement, Is.False);
        }

        // CF: SliderPath.ctor | case Add | case Add
        // CF: SliderPath.ctor | case Remove | case Remove
        // CF: SliderPath.ctor | case Reset | case Reset
        // CF: SliderPath.ctor | foreach newItem / oldItem | loop n
        [Test]
        public void TestVersionTracksControlPointChanges()
        {
            var path = new SliderPath();
            var point = new PathControlPoint(Vector2.Zero, PathType.LINEAR);

            int version = path.Version.Value;
            path.ControlPoints.Add(point);
            Assert.That(path.Version.Value, Is.GreaterThan(version));

            version = path.Version.Value;
            point.Position = new Vector2(10, 0);
            Assert.That(path.Version.Value, Is.GreaterThan(version));

            path.ControlPoints.Remove(point);
            version = path.Version.Value;
            point.Position = new Vector2(20, 0);
            Assert.That(path.Version.Value, Is.EqualTo(version));

            path.ControlPoints.Add(point);
            path.ControlPoints.Clear();
            version = path.Version.Value;
            point.Position = new Vector2(30, 0);
            Assert.That(path.Version.Value, Is.EqualTo(version));
        }

        // CF: SliderPath.OptimiseCatmull | set | no throw
        // CF: SliderPath.ctor | ExpectedDistance.ValueChanged | no throw
        [Test]
        public void TestExpectedDistanceAndOptimiseCatmullInvalidate()
        {
            var path = line();

            int version = path.Version.Value;
            path.ExpectedDistance.Value = 10;
            Assert.That(path.Version.Value, Is.GreaterThan(version));
            Assert.That(path.Distance, Is.EqualTo(10).Within(1e-3));

            version = path.Version.Value;
            path.OptimiseCatmull = true;
            Assert.That(path.Version.Value, Is.GreaterThan(version));
        }
    }
}
