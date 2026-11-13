// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Rulesets.Catch.Tests.Objects
{
    [TestFixture]
    public class JuiceStreamPathTest
    {
        private static JuiceStreamPath createPath(params (double time, float x)[] points)
        {
            var path = new JuiceStreamPath();
            // the first point must be at time 0 and replaces the default origin vertex.
            path.SetVertexPosition(0, points[0].x);

            foreach (var (time, x) in points.Skip(1))
                path.Add(time, x);
            return path;
        }

        // CF: JuiceStreamPath.positionAtTime | index <= 0 | TRUE
        [Test]
        public void TestPositionBeforeStartIsFirstVertex()
        {
            var path = createPath((0, 10), (100, 50));
            Assert.That(path.PositionAtTime(-50), Is.EqualTo(10));
        }

        // CF: JuiceStreamPath.positionAtTime | index <= 0 | FALSE
        // CF: JuiceStreamPath.positionAtTime | index >= vertices.Count | TRUE
        [Test]
        public void TestPositionAfterEndIsLastVertex()
        {
            var path = createPath((0, 10), (100, 50));
            Assert.That(path.PositionAtTime(500), Is.EqualTo(50));
        }

        // CF: JuiceStreamPath.positionAtTime | index >= vertices.Count | FALSE
        // CF: JuiceStreamPath.positionAtTime | Precision.AlmostEquals(duration, 0) | FALSE
        [Test]
        public void TestPositionInterpolatesLinearly()
        {
            var path = createPath((0, 0), (100, 100));
            Assert.That(path.PositionAtTime(25), Is.EqualTo(25).Within(1e-3));
        }

        // CF: JuiceStreamPath.positionAtTime | Precision.AlmostEquals(duration, 0) | TRUE
        [Test]
        public void TestPositionAtZeroDurationSegmentReturnsLaterVertex()
        {
            var path = new JuiceStreamPath();
            path.Add(100, 20);
            path.Add(100, 80);
            Assert.That(path.PositionAtTime(100), Is.EqualTo(80));
        }

        // CF: JuiceStreamPath.InsertVertex | !double.IsFinite(time) | TRUE
        // CF: JuiceStreamPath.InsertVertex | !double.IsFinite(time) | throws ArgumentOutOfRangeException
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void TestInsertNonFiniteTimeThrows(double time)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new JuiceStreamPath().InsertVertex(time));
        }

        // CF: JuiceStreamPath.InsertVertex | !double.IsFinite(time) | FALSE
        // CF: JuiceStreamPath.InsertVertex | time is finite | no throw
        [Test]
        public void TestInsertKeepsPathShapeAndInvalidates()
        {
            var path = createPath((0, 0), (100, 100));
            int before = path.InvalidationID;

            int index = path.InsertVertex(50);

            Assert.That(index, Is.EqualTo(1));
            Assert.That(path.Vertices[1].X, Is.EqualTo(50).Within(1e-3));
            Assert.That(path.InvalidationID, Is.GreaterThan(before));
        }

        // CF: JuiceStreamPath.SetVertexPosition | index < 0 | TRUE
        // CF: JuiceStreamPath.SetVertexPosition | index < 0 | throws ArgumentOutOfRangeException
        [Test]
        public void TestSetNegativeIndexThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new JuiceStreamPath().SetVertexPosition(-1, 0));
        }

        // CF: JuiceStreamPath.SetVertexPosition | index < 0 | FALSE
        // CF: JuiceStreamPath.SetVertexPosition | index >= vertices.Count | TRUE
        [Test]
        public void TestSetIndexPastEndThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new JuiceStreamPath().SetVertexPosition(1, 0));
        }

        // CF: JuiceStreamPath.SetVertexPosition | index >= vertices.Count | FALSE
        // CF: JuiceStreamPath.SetVertexPosition | !float.IsFinite(newX) | TRUE
        // CF: JuiceStreamPath.SetVertexPosition | !float.IsFinite(newX) | throws ArgumentOutOfRangeException
        [Test]
        public void TestSetNonFinitePositionThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new JuiceStreamPath().SetVertexPosition(0, float.NaN));
        }

        // CF: JuiceStreamPath.SetVertexPosition | !float.IsFinite(newX) | FALSE
        // CF: JuiceStreamPath.SetVertexPosition | valid arguments | no throw
        [Test]
        public void TestSetPositionUpdatesVertex()
        {
            var path = new JuiceStreamPath();
            path.SetVertexPosition(0, 42);
            Assert.That(path.Vertices[0].X, Is.EqualTo(42));
        }

        [Test]
        public void TestClearResetsToSingleOriginVertex()
        {
            var path = createPath((0, 10), (100, 20));
            int before = path.InvalidationID;

            path.Clear();

            Assert.That(path.Vertices, Has.Count.EqualTo(1));
            Assert.That(path.Vertices[0].Time, Is.Zero);
            Assert.That(path.Vertices[0].X, Is.Zero);
            Assert.That(path.InvalidationID, Is.GreaterThan(before));
        }

        // CF: JuiceStreamPath.RemoveVertices | vertices.Count == 0 | TRUE
        // CF: JuiceStreamPath.RemoveVertices | removeCount != 0 | TRUE
        [Test]
        public void TestRemoveAllReaddsOriginVertex()
        {
            var path = createPath((0, 10), (100, 20));
            int before = path.InvalidationID;

            int removed = path.RemoveVertices((_, _) => true);

            Assert.That(removed, Is.EqualTo(2));
            Assert.That(path.Vertices, Has.Count.EqualTo(1));
            Assert.That(path.Vertices[0].X, Is.Zero);
            Assert.That(path.InvalidationID, Is.GreaterThan(before));
        }

        // CF: JuiceStreamPath.RemoveVertices | vertices.Count == 0 | FALSE
        // CF: JuiceStreamPath.RemoveVertices | removeCount != 0 | FALSE
        [Test]
        public void TestRemoveNothingDoesNotInvalidate()
        {
            var path = createPath((0, 10), (100, 20));
            int before = path.InvalidationID;

            int removed = path.RemoveVertices((_, _) => false);

            Assert.That(removed, Is.Zero);
            Assert.That(path.InvalidationID, Is.EqualTo(before));
        }

        [Test]
        public void TestRemovePredicateReceivesIndex()
        {
            var path = createPath((0, 10), (100, 20), (200, 30));

            int removed = path.RemoveVertices((_, i) => i == 1);

            Assert.That(removed, Is.EqualTo(1));
            Assert.That(path.Vertices.Select(v => v.X), Is.EqualTo(new float[] { 10, 30 }));
        }

        // CF: JuiceStreamPath.ResampleVertices | foreach sampleTimes | loop 0
        // CF: JuiceStreamPath.ResampleVertices | vertices.Count == 1 | TRUE
        [Test]
        public void TestResampleNoTimesOnSingleVertexPath()
        {
            var path = new JuiceStreamPath();
            path.ResampleVertices(Array.Empty<double>());
            Assert.That(path.Vertices, Has.Count.EqualTo(1));
        }

        // CF: JuiceStreamPath.ResampleVertices | foreach sampleTimes | loop 1
        // CF: JuiceStreamPath.ResampleVertices | !double.IsFinite(time) | FALSE
        // CF: JuiceStreamPath.ResampleVertices | vertices.Count == 1 | FALSE
        [Test]
        public void TestResampleSingleTimeKeepsEndpoints()
        {
            var path = createPath((0, 0), (100, 100), (200, 0));

            path.ResampleVertices(new double[] { 50 });

            Assert.That(path.Vertices.Select(v => v.Time), Is.EqualTo(new double[] { 0, 50, 200 }));
            Assert.That(path.Vertices[1].X, Is.EqualTo(50).Within(1e-3));
        }

        // CF: JuiceStreamPath.ResampleVertices | foreach sampleTimes | loop n
        [Test]
        public void TestResampleClampsAndSortsTimes()
        {
            var path = createPath((0, 0), (100, 100));

            path.ResampleVertices(new double[] { 80, -10, 500, 20 });

            Assert.That(path.Vertices.Select(v => v.Time), Is.EqualTo(new double[] { 0, 0, 20, 80, 100, 100 }));
        }

        // CF: JuiceStreamPath.ResampleVertices | !double.IsFinite(time) | TRUE
        // CF: JuiceStreamPath.ResampleVertices | !double.IsFinite(time) | throws ArgumentOutOfRangeException
        [Test]
        public void TestResampleNonFiniteTimeThrows()
        {
            var path = createPath((0, 0), (100, 100));
            Assert.Throws<ArgumentOutOfRangeException>(() => path.ResampleVertices(new[] { double.NaN }));
        }

        // CF: JuiceStreamPath.ConvertFromSliderPath | for i in CalculatedPath | loop 0
        [Test]
        public void TestConvertFromSinglePointSliderPath()
        {
            var path = new JuiceStreamPath();
            var sliderPath = new SliderPath(PathType.LINEAR, new[] { new Vector2(30, 0) });

            path.ConvertFromSliderPath(sliderPath, 1);

            Assert.That(path.Vertices, Has.Count.EqualTo(1));
            Assert.That(path.Vertices[0].X, Is.EqualTo(30));
        }

        // CF: JuiceStreamPath.ConvertFromSliderPath | for i in CalculatedPath | loop 1
        // CF: JuiceStreamPath.ConvertFromSliderPath | Precision.AlmostEquals(vertices[^1].Time, time) | FALSE
        [Test]
        public void TestConvertFromSliderPathUsesDistanceOverVelocity()
        {
            var path = new JuiceStreamPath();
            var sliderPath = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(0, 100) });

            path.ConvertFromSliderPath(sliderPath, 2);

            Assert.That(path.Vertices, Has.Count.EqualTo(2));
            Assert.That(path.Vertices[1].Time, Is.EqualTo(50).Within(1e-3));
        }

        // CF: JuiceStreamPath.ConvertFromSliderPath | for i in CalculatedPath | loop n
        [Test]
        public void TestConvertFromMultiSegmentSliderPath()
        {
            var path = new JuiceStreamPath();
            var sliderPath = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(10, 10), new Vector2(40, 10) });

            path.ConvertFromSliderPath(sliderPath, 1);

            Assert.That(path.Vertices.Select(v => v.X), Is.EqualTo(new float[] { 0, 10, 40 }));
        }

        // CF: JuiceStreamPath.ConvertFromSliderPath | Precision.AlmostEquals(vertices[^1].Time, time) | TRUE
        [Test]
        public void TestConvertFromSliderPathDropsDuplicateTimes()
        {
            var path = new JuiceStreamPath();
            var sliderPath = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(10, 0) });

            // velocity is so high that the resulting time is indistinguishable from the previous vertex time.
            path.ConvertFromSliderPath(sliderPath, 1e9);

            Assert.That(path.Vertices, Has.Count.EqualTo(1));
        }

        // CF: JuiceStreamPath.ComputeRequiredVelocity | for i in vertices | loop 0
        [Test]
        public void TestRequiredVelocityOfSingleVertexIsZero()
        {
            Assert.That(new JuiceStreamPath().ComputeRequiredVelocity(), Is.Zero);
        }

        // CF: JuiceStreamPath.ComputeRequiredVelocity | for i in vertices | loop 1
        // CF: JuiceStreamPath.ComputeRequiredVelocity | Precision.AlmostEquals(timeDifference, 0) | FALSE
        [Test]
        public void TestRequiredVelocityIsSlope()
        {
            Assert.That(createPath((0, 0), (10, 50)).ComputeRequiredVelocity(), Is.EqualTo(5).Within(1e-6));
        }

        // CF: JuiceStreamPath.ComputeRequiredVelocity | for i in vertices | loop n
        // CF: JuiceStreamPath.ComputeRequiredVelocity | Precision.AlmostEquals(timeDifference, 0) | TRUE
        [Test]
        public void TestRequiredVelocityIsMaximumSlopeAndIgnoresZeroDurationSegments()
        {
            var path = new JuiceStreamPath();
            path.Add(0, 0);
            path.Add(10, 10); // slope 1
            path.Add(10, 500); // zero duration: ignored
            path.Add(20, 530); // slope 3

            Assert.That(path.ComputeRequiredVelocity(), Is.EqualTo(3).Within(1e-6));
        }

        // CF: JuiceStreamPath.ConvertToSliderPath | for i in vertices | loop 0
        [Test]
        public void TestConvertToSliderPathSingleVertex()
        {
            var sliderPath = new SliderPath();
            new JuiceStreamPath().ConvertToSliderPath(sliderPath, 0, 1);
            Assert.That(sliderPath.ControlPoints, Has.Count.EqualTo(1));
        }

        // CF: JuiceStreamPath.ConvertToSliderPath | for i in vertices | loop 1
        // CF: JuiceStreamPath.ConvertToSliderPath | deltaY <= OSU_PLAYFIELD_HEIGHT / 2 - margin | TRUE
        // CF: JuiceStreamPath.ConvertToSliderPath | lastSliderY < OSU_PLAYFIELD_HEIGHT / 2 | TRUE
        [Test]
        public void TestConvertToSliderPathShortSegmentGoesDownFromTop()
        {
            var sliderPath = new SliderPath();
            createPath((0, 0), (100, 50)).ConvertToSliderPath(sliderPath, 0, 1);

            Assert.That(sliderPath.ControlPoints, Has.Count.EqualTo(2));
            Assert.That(sliderPath.ControlPoints[0].Type, Is.EqualTo(PathType.LINEAR));
            Assert.That(sliderPath.ControlPoints[1].Position.X, Is.EqualTo(50));
            Assert.That(sliderPath.ControlPoints[1].Position.Y, Is.EqualTo(Math.Sqrt(100 * 100 - 50 * 50)).Within(1e-3));
        }

        // CF: JuiceStreamPath.ConvertToSliderPath | lastSliderY < OSU_PLAYFIELD_HEIGHT / 2 | FALSE
        [Test]
        public void TestConvertToSliderPathShortSegmentGoesUpFromBottom()
        {
            var sliderPath = new SliderPath();
            createPath((0, 0), (100, 50)).ConvertToSliderPath(sliderPath, 300, 1);

            Assert.That(sliderPath.ControlPoints[1].Position.Y, Is.EqualTo(-Math.Sqrt(100 * 100 - 50 * 50)).Within(1e-3));
        }

        // CF: JuiceStreamPath.ConvertToSliderPath | deltaY <= OSU_PLAYFIELD_HEIGHT / 2 - margin | FALSE
        // CF: JuiceStreamPath.ConvertToSliderPath | currentProgress < deltaY | loop n
        // CF: JuiceStreamPath.ConvertToSliderPath | for i in vertices | loop n
        [Test]
        public void TestConvertToSliderPathLongSegmentIsFoldedIntoPlayfield()
        {
            const float start_y = 100;
            var sliderPath = new SliderPath();
            createPath((0, 0), (1000, 0), (1500, 100)).ConvertToSliderPath(sliderPath, start_y, 1);

            Assert.That(sliderPath.ControlPoints.Count, Is.GreaterThan(3));

            foreach (var point in sliderPath.ControlPoints)
                Assert.That(start_y + point.Position.Y, Is.InRange(0f, JuiceStreamPath.OSU_PLAYFIELD_HEIGHT));

            Assert.That(sliderPath.ControlPoints.Last().Position.X, Is.EqualTo(100).Within(1e-3));
        }
    }
}
