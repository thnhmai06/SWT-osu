// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Lists;
using osu.Game.Rulesets.Timing;
using osu.Game.Rulesets.UI.Scrolling.Algorithms;

namespace osu.Game.Tests.Rulesets.UI.Scrolling
{
    [TestFixture]
    public class ScrollAlgorithmsTest
    {
        private const double time_range = 1000;
        private const float scroll_length = 100;

        private static MultiplierControlPoint point(double time, double velocity) => new MultiplierControlPoint(time) { Velocity = velocity };

        private static List<MultiplierControlPoint> twoPoints() => new List<MultiplierControlPoint> { point(0, 1), point(1000, 2) };

        private static SortedList<MultiplierControlPoint> sorted(params MultiplierControlPoint[] points)
        {
            var list = new SortedList<MultiplierControlPoint>(Comparer<MultiplierControlPoint>.Default);
            foreach (var p in points) list.Add(p);
            return list;
        }

        #region Sequential

        // CF: SequentialScrollAlgorithm.relativePositionAt | controlPoints.Count == 0 | TRUE
        [Test]
        public void TestSequentialWithoutControlPoints()
        {
            var algorithm = new SequentialScrollAlgorithm(new List<MultiplierControlPoint>());

            Assert.That(algorithm.PositionAt(500, 0, time_range, scroll_length), Is.EqualTo(50).Within(1e-3));
            Assert.That(algorithm.GetLength(0, 500, time_range, scroll_length), Is.EqualTo(50).Within(1e-3));
        }

        // CF: SequentialScrollAlgorithm.TimeAt | controlPoints.Count == 0 | TRUE
        [Test]
        public void TestSequentialTimeAtWithoutControlPointsIsScaledByTimeRange()
        {
            // Only the branch is exercised; the exact formula for this degenerate case ignores currentTime and scrollLength.
            Assert.That(() => new SequentialScrollAlgorithm(new List<MultiplierControlPoint>()).TimeAt(0, 0, time_range, scroll_length), Throws.Nothing);
        }

        // CF: SequentialScrollAlgorithm.relativePositionAt | controlPoints.Count == 0 | FALSE
        // CF: SequentialScrollAlgorithm.generatePositionMappings | controlPoints.Count > 0 | TRUE
        // CF: SequentialScrollAlgorithm.generatePositionMappings | for i < controlPoints.Count - 1 | loop n
        // CF: SequentialScrollAlgorithm.findControlPointMapping | mappingIndex < 0 | TRUE
        [Test]
        public void TestSequentialPositionAcrossSpeedChange()
        {
            var algorithm = new SequentialScrollAlgorithm(twoPoints());

            // 500ms -> 0.5 range units; 1500ms -> 1.0 (first segment) + 0.5 * 2x = 2.0 range units.
            Assert.That(algorithm.PositionAt(1500, 500, time_range, scroll_length), Is.EqualTo(150).Within(1e-3));
            Assert.That(algorithm.GetLength(500, 1500, time_range, scroll_length), Is.EqualTo(150).Within(1e-3));
        }

        // CF: SequentialScrollAlgorithm.findControlPointMapping | mappingIndex < 0 | FALSE
        [Test]
        public void TestSequentialPositionBeforeFirstControlPointUsesFirst()
        {
            var algorithm = new SequentialScrollAlgorithm(twoPoints());

            Assert.That(algorithm.PositionAt(0, -500, time_range, scroll_length), Is.EqualTo(50).Within(1e-3));
        }

        [Test]
        public void TestSequentialTimeAt()
        {
            var algorithm = new SequentialScrollAlgorithm(twoPoints());

            Assert.That(algorithm.TimeAt(150, 500, time_range, scroll_length), Is.EqualTo(1500).Within(1e-6), "inverse of PositionAt across the speed change");
            Assert.That(algorithm.TimeAt(-50, 500, time_range, scroll_length), Is.EqualTo(0).Within(1e-6), "exact mapping hit");
            Assert.That(algorithm.TimeAt(-100, 500, time_range, scroll_length), Is.EqualTo(-500).Within(1e-6), "before first mapping");
        }

        [Test]
        public void TestSequentialDisplayStartTime()
        {
            var algorithm = new SequentialScrollAlgorithm(twoPoints());

            Assert.That(algorithm.GetDisplayStartTime(1500, 0, time_range, scroll_length), Is.EqualTo(1000).Within(1e-6));
        }

        // CF: SequentialScrollAlgorithm.generatePositionMappings | positionMappings.Count > 0 | TRUE
        [Test]
        public void TestSequentialMappingsCachedUntilReset()
        {
            var points = twoPoints();
            var algorithm = new SequentialScrollAlgorithm(points);

            Assert.That(algorithm.PositionAt(1500, 500, time_range, scroll_length), Is.EqualTo(150).Within(1e-3));

            points[0].Velocity = 2;
            algorithm.Reset();

            Assert.That(algorithm.PositionAt(1500, 500, time_range, scroll_length), Is.EqualTo(200).Within(1e-3));
        }

        // CF: SequentialScrollAlgorithm.generatePositionMappings | controlPoints.Count > 0 | infeasible: callers guard on Count == 0 beforehand
        // CF: SequentialScrollAlgorithm.generatePositionMappings | for i < controlPoints.Count - 1 | loop 0
        [Test]
        public void TestSequentialSingleControlPoint()
        {
            var algorithm = new SequentialScrollAlgorithm(new List<MultiplierControlPoint> { point(0, 2) });

            Assert.That(algorithm.PositionAt(500, 0, time_range, scroll_length), Is.EqualTo(100).Within(1e-3));
        }

        #endregion

        #region Overlapping

        [Test]
        public void TestOverlappingPositionUsesMultiplierAtOriginTime()
        {
            var algorithm = new OverlappingScrollAlgorithm(sorted(point(0, 1), point(1000, 2)));

            // multiplier is looked up at the object's own time by default...
            Assert.That(algorithm.PositionAt(1500, 500, time_range, scroll_length), Is.EqualTo(200).Within(1e-3));
            // ...or at the explicit origin time.
            Assert.That(algorithm.PositionAt(1500, 500, time_range, scroll_length, 500), Is.EqualTo(100).Within(1e-3));
        }

        [Test]
        public void TestOverlappingLength()
        {
            var algorithm = new OverlappingScrollAlgorithm(sorted(point(0, 1), point(1000, 2)));

            Assert.That(algorithm.GetLength(500, 1500, time_range, scroll_length), Is.EqualTo(100).Within(1e-3));
        }

        [Test]
        public void TestOverlappingDisplayStartTime()
        {
            var algorithm = new OverlappingScrollAlgorithm(sorted(point(0, 1), point(1000, 2)));

            Assert.That(algorithm.GetDisplayStartTime(1500, 0, time_range, scroll_length), Is.EqualTo(1000).Within(1e-6), "double speed halves the visible duration");
        }

        // CF: OverlappingScrollAlgorithm.TimeAt | controlPoints.LastOrDefault(cp => PositionAt(cp.Time, ...) <= position) | non-null
        // CF: OverlappingScrollAlgorithm.TimeAt | controlPoints.LastOrDefault(cp => PositionAt(cp.Time, ...) <= position) | null
        [Test]
        public void TestOverlappingTimeAt()
        {
            var algorithm = new OverlappingScrollAlgorithm(sorted(point(0, 1), point(1000, 2)));

            Assert.That(algorithm.TimeAt(100, 500, time_range, scroll_length), Is.EqualTo(1000).Within(1e-6), "last control point at or before the position wins");
            Assert.That(algorithm.TimeAt(0, 500, time_range, scroll_length), Is.EqualTo(500).Within(1e-6), "position 0 is the current time");
            Assert.That(algorithm.TimeAt(-1000, 500, time_range, scroll_length), Is.EqualTo(-9500).Within(1e-6), "falls back to the first control point");
        }

        // CF: OverlappingScrollAlgorithm.controlPointAt | BinarySearch(...) ?? controlPoints.FirstOrDefault() ?? new MultiplierControlPoint(-inf) | default
        [Test]
        public void TestOverlappingWithoutControlPointsUsesUnitMultiplier()
        {
            var algorithm = new OverlappingScrollAlgorithm(sorted());

            Assert.That(algorithm.PositionAt(500, 0, time_range, scroll_length), Is.EqualTo(50).Within(1e-3));
            Assert.That(algorithm.GetDisplayStartTime(1000, 0, time_range, scroll_length), Is.EqualTo(0).Within(1e-6));
        }

        // CF: OverlappingScrollAlgorithm.controlPointAt | BinarySearch(...) ?? controlPoints.FirstOrDefault() | non-null (first)
        [Test]
        public void TestOverlappingTimeBeforeFirstControlPointUsesFirst()
        {
            var algorithm = new OverlappingScrollAlgorithm(sorted(point(1000, 2)));

            Assert.That(algorithm.PositionAt(500, 0, time_range, scroll_length), Is.EqualTo(100).Within(1e-3));
        }

        [Test]
        public void TestOverlappingResetIsNoOp()
        {
            Assert.That(() => new OverlappingScrollAlgorithm(sorted()).Reset(), Throws.Nothing);
        }

        #endregion
    }
}
