// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Utils;
using osuTK;

namespace osu.Game.Tests.Utils
{
    [TestFixture]
    public class GeometryUtilsTest
    {
        private static Vector2[] pts(params int[] v) => Enumerable.Range(0, v.Length / 2).Select(i => new Vector2(v[i * 2], v[i * 2 + 1])).ToArray();

        private static void assertVec(Vector2 actual, float x, float y)
        {
            Assert.That(actual.X, Is.EqualTo(x).Within(1e-3));
            Assert.That(actual.Y, Is.EqualTo(y).Within(1e-3));
        }

        [Test]
        public void TestRotatePointAroundOrigin()
        {
            // positive angle rotates clockwise in screen space (y down), around the given origin.
            var r = GeometryUtils.RotatePointAroundOrigin(new Vector2(2, 1), new Vector2(1, 1), 90);
            assertVec(r, 1, 2);
        }

        [Test]
        public void TestRotateVectorZero()
        {
            assertVec(GeometryUtils.RotateVector(new Vector2(3, 4), 0), 3, 4);
        }

        // CF: GeometryUtils.GetFlippedPosition | direction | case Horizontal
        [Test]
        public void TestFlipHorizontal()
        {
            assertVec(GeometryUtils.GetFlippedPosition(Direction.Horizontal, new Quad(0, 0, 10, 10), new Vector2(2, 3)), 8, 3);
        }

        // CF: GeometryUtils.GetFlippedPosition | direction | case Vertical
        [Test]
        public void TestFlipVertical()
        {
            assertVec(GeometryUtils.GetFlippedPosition(Direction.Vertical, new Quad(0, 0, 10, 10), new Vector2(2, 3)), 2, 7);
        }

        // CF: GeometryUtils.GetFlippedPosition | direction | default
        [Test]
        public void TestFlipUnknownDirectionNoChange()
        {
            assertVec(GeometryUtils.GetFlippedPosition((Direction)99, new Quad(0, 0, 10, 10), new Vector2(2, 3)), 2, 3);
        }

        [Test]
        public void TestFlipAroundAxis()
        {
            // mirror across the vertical line x = 5 (axis is the normal, i.e. horizontal).
            assertVec(GeometryUtils.GetFlippedPosition(new Vector2(1, 0), new Quad(0, 0, 10, 10), new Vector2(2, 3)), 8, 3);
        }

        // CF: GeometryUtils.GetScaledPosition | (reference & Anchor.x0) > 0 | TRUE
        // CF: GeometryUtils.GetScaledPosition | (reference & Anchor.y0) > 0 | TRUE
        // CF: GeometryUtils.GetScaledPosition | scale.X != 0 && selectionQuad.Width > 0 | TRUE
        // CF: GeometryUtils.GetScaledPosition | scale.Y != 0 && selectionQuad.Height > 0 | TRUE
        [Test]
        public void TestScaledPositionFromTopLeft()
        {
            var r = GeometryUtils.GetScaledPosition(Anchor.TopLeft, new Vector2(10, 10), new Quad(0, 0, 10, 10), new Vector2(10, 10));
            assertVec(r, 10, 10);
        }

        // CF: GeometryUtils.GetScaledPosition | (reference & Anchor.x0) > 0 | FALSE
        // CF: GeometryUtils.GetScaledPosition | (reference & Anchor.y0) > 0 | FALSE
        [Test]
        public void TestScaledPositionFromBottomRight()
        {
            var r = GeometryUtils.GetScaledPosition(Anchor.BottomRight, new Vector2(10, 10), new Quad(0, 0, 10, 10), new Vector2(10, 10));
            assertVec(r, 20, 20);
        }

        // CF: GeometryUtils.GetScaledPosition | scale.X != 0 | FALSE
        // CF: GeometryUtils.GetScaledPosition | scale.Y != 0 | FALSE
        [Test]
        public void TestScaledPositionZeroScaleNoOp()
        {
            assertVec(GeometryUtils.GetScaledPosition(Anchor.BottomRight, Vector2.Zero, new Quad(0, 0, 10, 10), new Vector2(3, 4)), 3, 4);
        }

        // CF: GeometryUtils.GetScaledPosition | scale.X != 0 && selectionQuad.Width > 0 | FALSE
        // CF: GeometryUtils.GetScaledPosition | scale.Y != 0 && selectionQuad.Height > 0 | FALSE
        [Test]
        public void TestScaledPositionDegenerateQuadNoOp()
        {
            assertVec(GeometryUtils.GetScaledPosition(Anchor.BottomRight, new Vector2(5, 5), new Quad(0, 0, 0, 0), new Vector2(3, 4)), 3, 4);
        }

        [Test]
        public void TestScaledPositionAroundOrigin()
        {
            assertVec(GeometryUtils.GetScaledPosition(new Vector2(2, 3), new Vector2(1, 1), new Vector2(2, 2)), 3, 4);
            assertVec(GeometryUtils.GetScaledPosition(new Vector2(2, 1), new Vector2(0, 0), new Vector2(1, 0), 90), 1, 0);
        }

        // CF: GeometryUtils.GetSurroundingQuad | !points.Any() | TRUE
        [Test]
        public void TestSurroundingQuadEmpty()
        {
            Assert.That(GeometryUtils.GetSurroundingQuad(Array.Empty<Vector2>()), Is.EqualTo(new Quad()));
        }

        // CF: GeometryUtils.GetSurroundingQuad | !points.Any() | FALSE
        // CF: GeometryUtils.GetSurroundingQuad | foreach points | loop n
        [Test]
        public void TestSurroundingQuad()
        {
            var q = GeometryUtils.GetSurroundingQuad(pts(1, 2, 5, -1, 3, 8));
            Assert.That(q, Is.EqualTo(new Quad(1, -1, 4, 9)));
        }

        // CF: GeometryUtils.GetSurroundingQuad | startAndEndOnly | FALSE
        // CF: GeometryUtils.enumeratePositions | h is IHasPath | FALSE
        // CF: GeometryUtils.enumeratePositions | h is IHasPath | TRUE
        [Test]
        public void TestSurroundingQuadHitObjectsAllControlPoints()
        {
            var circle = new HitCircle { Position = new Vector2(0, 0) };
            var slider = new Slider { Position = new Vector2(10, 10), Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(20, 0), new Vector2(20, 5) }) };

            var q = GeometryUtils.GetSurroundingQuad(new IHasPosition[] { circle, slider });
            Assert.That(q, Is.EqualTo(new Quad(0, 0, 30, 15)));
        }

        // CF: GeometryUtils.GetSurroundingQuad | startAndEndOnly | TRUE
        // CF: GeometryUtils.enumerateStartAndEndPositions | h is IHasPath | TRUE
        // CF: GeometryUtils.enumerateStartAndEndPositions | h is IHasPath | FALSE
        [Test]
        public void TestSurroundingQuadHitObjectsStartAndEnd()
        {
            var circle = new HitCircle { Position = new Vector2(0, 0) };
            var slider = new Slider { Position = new Vector2(10, 10), Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(20, 0) }) };

            var q = GeometryUtils.GetSurroundingQuad(new IHasPosition[] { circle, slider }, true);
            Assert.That(q.Width, Is.EqualTo(30).Within(1e-3));
            Assert.That(q.Height, Is.EqualTo(10).Within(1e-3));
        }

        // CF: GeometryUtils.GetConvexHull | pointsList.Count < 3 | TRUE
        // CF: GeometryUtils.GetConvexHull | pointsList.Count < 3 | FALSE
        // CF: GeometryUtils.GetConvexHull | while convexHullLower.Count > 1 && isClockwise | TRUE
        // CF: GeometryUtils.GetConvexHull | while convexHullLower.Count > 1 && isClockwise | FALSE
        // CF: GeometryUtils.GetConvexHull | while convexHullUpper.Count > 1 && isClockwise | TRUE
        // CF: GeometryUtils.GetConvexHull | while convexHullUpper.Count > 1 && isClockwise | FALSE
        [TestCase(new int[] { }, new int[] { })]
        [TestCase(new[] { 0, 0 }, new[] { 0, 0 })]
        [TestCase(new[] { 0, 0, 1, 1, 1, -1, 2, 0 }, new[] { 0, 0, 1, 1, 2, 0, 1, -1 })]
        [TestCase(new[] { 0, 0, 1, 1, 1, -1, 2, 0, 1, 0 }, new[] { 0, 0, 1, 1, 2, 0, 1, -1 })]
        [TestCase(new[] { 0, 0, 1, 1, 2, -1, 2, 0, 1, 0, 4, 10 }, new[] { 0, 0, 4, 10, 2, -1 })]
        public void TestConvexHull(int[] values, int[] expected)
        {
            Assert.That(GeometryUtils.GetConvexHull(pts(values)), Is.EquivalentTo(pts(expected)));
        }

        [Test]
        public void TestConvexHullHitObjects()
        {
            var objs = new IHasPosition[] { new HitCircle { Position = new Vector2(0, 0) }, new HitCircle { Position = new Vector2(10, 0) }, new HitCircle { Position = new Vector2(0, 10) }, new HitCircle { Position = new Vector2(1, 1) } };
            Assert.That(GeometryUtils.GetConvexHull(objs), Is.EquivalentTo(pts(0, 0, 10, 0, 0, 10)));
        }

        // CF: GeometryUtils.MinimumEnclosingCircle | n == 0 || r.Count == 3 | TRUE
        // CF: GeometryUtils.MinimumEnclosingCircle | n == 0 | FALSE
        // CF: GeometryUtils.MinimumEnclosingCircle | isInside(d, point.Value) | TRUE
        // CF: GeometryUtils.MinimumEnclosingCircle | isInside(d, point.Value) | FALSE
        // CF: GeometryUtils.minCircleTrivial | points.Count | case 0
        // CF: GeometryUtils.minCircleTrivial | points.Count | case 1
        // CF: GeometryUtils.minCircleTrivial | points.Count | case 2
        // CF: GeometryUtils.minCircleTrivial | points.Count == 3 | default
        // CF: GeometryUtils.minCircleTrivial | isValidCircle(c, points) | TRUE
        // CF: GeometryUtils.minCircleTrivial | isValidCircle(c, points) | FALSE
        // CF: GeometryUtils.isValidCircle | !isInside(c, p) | TRUE
        // CF: GeometryUtils.isValidCircle | !isInside(c, p) | FALSE
        [TestCase(new int[] { }, 0, 0, 0)]
        [TestCase(new[] { 0, 0 }, 0, 0, 0)]
        [TestCase(new[] { 0, 0, 1, 1, 1, -1, 2, 0 }, 1, 0, 1)]
        [TestCase(new[] { 0, 0, 1, 1, 1, -1, 2, 0, 1, 0 }, 1, 0, 1)]
        [TestCase(new[] { 0, 0, 1, 1, 2, -1, 2, 0, 1, 0, 4, 10 }, 3, 4.5f, 5.5901699f)]
        [TestCase(new[] { 0, 0, 4, 0, 0, 3 }, 2, 1.5f, 2.5f)]
        public void TestMinimumEnclosingCircle(int[] values, float x, float y, float r)
        {
            (var centre, float radius) = GeometryUtils.MinimumEnclosingCircle(pts(values));
            assertVec(centre, x, y);
            Assert.That(radius, Is.EqualTo(r).Within(1e-3));
        }

        // CF: GeometryUtils.circleFrom | collinear points | TRUE
        [Test]
        public void TestMinimumEnclosingCircleCollinear()
        {
            (var centre, float radius) = GeometryUtils.MinimumEnclosingCircle(pts(0, 0, 1, 0, 2, 0, 4, 0));
            assertVec(centre, 2, 0);
            Assert.That(radius, Is.EqualTo(2).Within(1e-3));
        }

        [Test]
        public void TestMinimumEnclosingCircleHitObjects()
        {
            var objs = new IHasPosition[] { new HitCircle { Position = new Vector2(0, 0) }, new HitCircle { Position = new Vector2(10, 0) } };
            (var centre, float radius) = GeometryUtils.MinimumEnclosingCircle(objs);
            assertVec(centre, 5, 0);
            Assert.That(radius, Is.EqualTo(5).Within(1e-3));
        }
    }
}
