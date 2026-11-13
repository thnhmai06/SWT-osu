// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Tests.Rulesets.Objects
{
    [TestFixture]
    public class BezierConverterTest
    {
        private static PathControlPoint[] points(PathType? firstType, params (float x, float y)[] positions)
            => positions.Select((p, i) => new PathControlPoint(new Vector2(p.x, p.y), i == 0 ? firstType : null)).ToArray();

        private static Vector2 onCircle(double angle) => new Vector2((float)(100 * Math.Cos(angle)), (float)(100 * Math.Sin(angle)));

        // CF: BezierConverter.CountSegments | t.Type != null && i < controlPoints.Count - 1 | TRUE
        // CF: BezierConverter.CountSegments | t.Type != null | FALSE
        // CF: BezierConverter.CountSegments | i < controlPoints.Count - 1 | FALSE
        [Test]
        public void TestCountSegmentsIgnoresUntypedAndLastPoint()
        {
            var cps = new[]
            {
                new PathControlPoint(Vector2.Zero, PathType.LINEAR),
                new PathControlPoint(new Vector2(1, 0)),
                new PathControlPoint(new Vector2(2, 0), PathType.LINEAR),
                new PathControlPoint(new Vector2(3, 0), PathType.LINEAR),
            };

            Assert.That(BezierConverter.CountSegments(cps), Is.EqualTo(2));
            Assert.That(BezierConverter.CountSegments(Array.Empty<PathControlPoint>()), Is.Zero);
        }

        // CF: BezierConverter.ConvertToLegacyBezier | controlPoints[i].Type == null && i < Count - 1 | TRUE
        // CF: BezierConverter.ConvertToLegacyBezier | case Linear | case Linear
        // CF: BezierConverter.ConvertToLegacyBezier | Type ?? LINEAR | null
        [Test]
        public void TestLegacyLinearIsOffsetByPosition()
        {
            var result = BezierConverter.ConvertToLegacyBezier(points(PathType.LINEAR, (0, 0), (10, 0)), new Vector2(5, 5));
            Assert.That(result, Is.EqualTo(new[] { new Vector2(5, 5), new Vector2(15, 5) }));

            // untyped first point defaults to linear.
            var untyped = BezierConverter.ConvertToLegacyBezier(points(null, (0, 0), (10, 0)), Vector2.Zero);
            Assert.That(untyped, Is.EqualTo(new[] { Vector2.Zero, new Vector2(10, 0) }));
        }

        // CF: BezierConverter.ConvertToLegacyBezier | case Catmull | case Catmull
        // CF: BezierConverter.ConvertCatmullToBezierAnchors | i > 0 | TRUE
        // CF: BezierConverter.ConvertCatmullToBezierAnchors | i > 0 | FALSE
        // CF: BezierConverter.ConvertCatmullToBezierAnchors | i < iLen - 2 | TRUE
        // CF: BezierConverter.ConvertCatmullToBezierAnchors | i < iLen - 2 | FALSE
        [Test]
        public void TestCatmullAnchors()
        {
            var anchors = BezierConverter.ConvertCatmullToBezierAnchors(new[] { Vector2.Zero, new Vector2(6, 0), new Vector2(12, 0) });

            Assert.That(anchors.Length, Is.EqualTo(2));
            Assert.That(anchors[0].Select(v => v.X), Is.EqualTo(new[] { 0f, 1f, 4f, 6f }));
            Assert.That(anchors[1].Select(v => v.X), Is.EqualTo(new[] { 6f, 8f, 10f, 12f }));

            var legacy = BezierConverter.ConvertToLegacyBezier(points(PathType.CATMULL, (0, 0), (6, 0), (12, 0)), new Vector2(0, 3));
            Assert.That(legacy.Count, Is.EqualTo(8));
            Assert.That(legacy.All(v => v.Y == 3), Is.True);
        }

        // CF: BezierConverter.ConvertLinearToBezierAnchors | for segments | loop n
        // CF: BezierConverter.ConvertLinearToBezierAnchors | for segments | loop 1
        [Test]
        public void TestLinearAnchors()
        {
            var anchors = BezierConverter.ConvertLinearToBezierAnchors(new[] { Vector2.Zero, new Vector2(1, 0), new Vector2(1, 1) });

            Assert.That(anchors.Length, Is.EqualTo(2));
            Assert.That(anchors[1], Is.EqualTo(new[] { new Vector2(1, 0), new Vector2(1, 1) }));
            Assert.That(BezierConverter.ConvertLinearToBezierAnchors(new[] { Vector2.Zero, Vector2.One }).Length, Is.EqualTo(1));
        }

        // CF: BezierConverter.ConvertToLegacyBezier | case BSpline | case BSpline
        // CF: BezierConverter.ConvertToLegacyBezier | segmentType.Degree != null | FALSE
        [Test]
        public void TestLegacyBSplineWithoutDegreeKeepsVertices()
        {
            var result = BezierConverter.ConvertToLegacyBezier(points(PathType.BEZIER, (0, 0), (5, 5), (10, 0)), new Vector2(1, 1));

            // note: a typed first point also emits itself as a single-vertex segment (extra leading vertex); only the tail is asserted.
            Assert.That(result.TakeLast(3), Is.EqualTo(new[] { new Vector2(1, 1), new Vector2(6, 6), new Vector2(11, 1) }));
        }

        // CF: BezierConverter.ConvertToLegacyBezier | segmentType.Degree != null | throws NotImplementedException
        [Test]
        public void TestLegacyBSplineWithDegreeThrows()
        {
            Assert.Throws<NotImplementedException>(() => BezierConverter.ConvertToLegacyBezier(points(PathType.BSpline(2), (0, 0), (5, 5), (10, 0)), Vector2.Zero));
        }

        // CF: BezierConverter.ConvertToLegacyBezier | case PerfectCurve | case PerfectCurve
        // CF: BezierConverter.ConvertToLegacyBezier | default | throws ArgumentOutOfRangeException
        [Test]
        public void TestUnsupportedSplineTypeThrows()
        {
            var unsupported = new PathType((SplineType)99);

            Assert.Throws<ArgumentOutOfRangeException>(() => BezierConverter.ConvertToLegacyBezier(points(unsupported, (0, 0), (5, 5)), Vector2.Zero));
            Assert.Throws<ArgumentOutOfRangeException>(() => BezierConverter.ConvertToModernBezier(points(unsupported, (0, 0), (5, 5))));
        }

        // CF: BezierConverter.ConvertCircleToBezierAnchors | controlPoints.Length != 3 | TRUE
        // CF: BezierConverter.ConvertCircleToBezierAnchors | controlPoints.Length != 3 | FALSE
        // CF: BezierConverter.ConvertCircleToBezierAnchors | !pr.IsValid | TRUE
        [Test]
        public void TestCircleAnchorsReturnInputWhenNotAnArc()
        {
            var four = new[] { Vector2.Zero, Vector2.One, new Vector2(2, 0), new Vector2(3, 3) };
            Assert.That(BezierConverter.ConvertCircleToBezierAnchors(four), Is.EqualTo(four));

            var colinear = new[] { Vector2.Zero, new Vector2(5, 0), new Vector2(10, 0) };
            Assert.That(BezierConverter.ConvertCircleToBezierAnchors(colinear), Is.EqualTo(colinear));
        }

        // CF: BezierConverter.ConvertCircleToBezierAnchors | !pr.IsValid | FALSE
        // CF: BezierConverter.ConvertCircleToBezierAnchors | cbp.ArcLength < pr.ThetaRange | TRUE
        // CF: BezierConverter.ConvertCircleToBezierAnchors | cbp.ArcLength < pr.ThetaRange | FALSE
        // CF: BezierConverter.ConvertCircleToBezierAnchors | Math.Abs(tf - 1) > 1E-7 | TRUE
        // CF: BezierConverter.ConvertCircleToBezierAnchors | arcLength < 0 | TRUE
        // CF: BezierConverter.ConvertCircleToBezierAnchors | arcLength < 0 | FALSE
        // CF: BezierConverter.ConvertCircleToBezierAnchors | foreach preset | loop n
        [TestCase(0.4, false)]
        [TestCase(1.5, false)]
        [TestCase(3.0, false)]
        [TestCase(5.0, false)]
        [TestCase(6.0, false)]
        [TestCase(0.4, true)]
        [TestCase(3.0, true)]
        [TestCase(6.0, true)]
        public void TestCircleAnchorsStartAndEndOnTheArc(double thetaRange, bool clockwise)
        {
            double sign = clockwise ? -1 : 1;
            var p0 = onCircle(0);
            var p1 = onCircle(sign * thetaRange / 2);
            var p2 = onCircle(sign * thetaRange);

            var anchors = BezierConverter.ConvertCircleToBezierAnchors(new[] { p0, p1, p2 });

            Assert.That(anchors.First().X, Is.EqualTo(p0.X).Within(0.1));
            Assert.That(anchors.First().Y, Is.EqualTo(p0.Y).Within(0.1));
            Assert.That(anchors.Last().X, Is.EqualTo(p2.X).Within(0.1));
            Assert.That(anchors.Last().Y, Is.EqualTo(p2.Y).Within(0.1));
        }

        // CF: BezierConverter.ConvertToModernBezier | case Linear | case Linear
        // CF: BezierConverter.ConvertToModernBezier | j == 0 | TRUE
        // CF: BezierConverter.ConvertToModernBezier | j == 0 | FALSE
        // CF: BezierConverter.ConvertToModernBezier | for segments j | loop n
        [Test]
        public void TestModernLinearBecomesBezierSegments()
        {
            var result = BezierConverter.ConvertToModernBezier(points(PathType.LINEAR, (0, 0), (10, 0), (10, 10)));

            Assert.That(result.Select(c => c.Position), Is.EqualTo(new[] { new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10) }));
            Assert.That(result.Select(c => c.Type), Is.EqualTo(new PathType?[] { PathType.BEZIER, PathType.BEZIER, null }));
        }

        // CF: BezierConverter.ConvertToModernBezier | case Catmull | case Catmull
        [Test]
        public void TestModernCatmullBecomesBezier()
        {
            var result = BezierConverter.ConvertToModernBezier(points(PathType.CATMULL, (0, 0), (6, 0), (12, 0)));

            Assert.That(result.Select(c => c.Position.X), Is.EqualTo(new[] { 0f, 1f, 4f, 6f, 8f, 10f, 12f }));
            Assert.That(result.Count(c => c.Type == PathType.BEZIER), Is.EqualTo(2));
            Assert.That(result.Last().Type, Is.Null);
        }

        // CF: BezierConverter.ConvertToModernBezier | case PerfectCurve | case PerfectCurve
        [Test]
        public void TestModernPerfectCurveBecomesBezier()
        {
            var result = BezierConverter.ConvertToModernBezier(points(PathType.PERFECT_CURVE, (100, 0), (0, 100), (-100, 0)));

            Assert.That(result.First().Type, Is.EqualTo(PathType.BEZIER));
            Assert.That(result.Count, Is.GreaterThan(3));
            Assert.That(result.Last().Position.X, Is.EqualTo(-100).Within(0.1));
        }

        // CF: BezierConverter.ConvertToModernBezier | case BSpline | case BSpline
        // CF: BezierConverter.ConvertToModernBezier | segmentType.Degree == null | TRUE
        // CF: BezierConverter.ConvertToModernBezier | segmentType.Degree == null | FALSE
        [Test]
        public void TestModernBSpline()
        {
            var noDegree = BezierConverter.ConvertToModernBezier(points(PathType.BEZIER, (0, 0), (5, 5), (10, 0)));
            Assert.That(noDegree.Select(c => c.Position), Is.EqualTo(new[] { new Vector2(0, 0), new Vector2(5, 5), new Vector2(10, 0) }));
            Assert.That(noDegree[0].Type, Is.EqualTo(PathType.BEZIER));

            var withDegree = BezierConverter.ConvertToModernBezier(points(PathType.BSpline(2), (0, 0), (5, 5), (10, 0), (15, 5)));
            Assert.That(withDegree.First().Type, Is.EqualTo(PathType.BEZIER));
            Assert.That(withDegree.Last().Position, Is.EqualTo(new Vector2(15, 5)));
        }

        // CF: BezierConverter.ConvertToModernBezier | controlPoints[i].Type == null && i < Count - 1 | TRUE
        // CF: BezierConverter.ConvertToModernBezier | controlPoints[i].Type == null && i < Count - 1 | FALSE
        [Test]
        public void TestModernMultiSegment()
        {
            var cps = new[]
            {
                new PathControlPoint(Vector2.Zero, PathType.LINEAR),
                new PathControlPoint(new Vector2(10, 0), PathType.BEZIER),
                new PathControlPoint(new Vector2(15, 5)),
                new PathControlPoint(new Vector2(20, 0)),
            };

            var result = BezierConverter.ConvertToModernBezier(cps);

            Assert.That(result.Select(c => c.Position), Is.EqualTo(new[] { new Vector2(0, 0), new Vector2(10, 0), new Vector2(15, 5), new Vector2(20, 0) }));
            Assert.That(result.Select(c => c.Type), Is.EqualTo(new PathType?[] { PathType.BEZIER, PathType.BEZIER, null, null }));
        }
    }
}
