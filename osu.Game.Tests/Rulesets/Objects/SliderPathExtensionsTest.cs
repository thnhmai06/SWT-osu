// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using Moq;
using NUnit.Framework;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Tests.Rulesets.Objects
{
    [TestFixture]
    public class SliderPathExtensionsTest
    {
        private static Slider slider(double startTime = 1000) => new Slider
        {
            StartTime = startTime,
            Path = new SliderPath(PathType.LINEAR, [Vector2.Zero, new Vector2(100, 0)]),
        };

        // CF: SliderPathExtensions.SnapTo | snapProvider?.FindSnappedDistance(...) ?? CalculatedDistance | null
        [Test]
        public void TestSnapToWithoutProviderKeepsCalculatedDistance()
        {
            var s = slider();
            s.Path.ExpectedDistance.Value = 50;

            s.SnapTo(null);

            Assert.That(s.Path.ExpectedDistance.Value, Is.EqualTo(100).Within(1e-3));
        }

        // CF: SliderPathExtensions.SnapTo | snapProvider?.FindSnappedDistance(...) ?? CalculatedDistance | non-null
        [Test]
        public void TestSnapToUsesProviderResult()
        {
            var s = slider(2500);
            var provider = new Mock<IDistanceSnapProvider>();
            provider.Setup(p => p.FindSnappedDistance(It.IsAny<float>(), It.IsAny<double>(), It.IsAny<IHasSliderVelocity?>())).Returns(80);

            s.SnapTo(provider.Object);

            Assert.That(s.Path.ExpectedDistance.Value, Is.EqualTo(80));
            provider.Verify(p => p.FindSnappedDistance(It.Is<float>(d => System.Math.Abs(d - 100) < 1e-3), 2500, s), Times.Once);
        }

        private static (Vector2 offset, SliderPath path) reverse(SliderPath path)
        {
            path.Reverse(out var offset);
            return (offset, path);
        }

        [Test]
        public void TestReverseLinearPath()
        {
            var (offset, path) = reverse(new SliderPath(PathType.LINEAR, [Vector2.Zero, new Vector2(100, 0)]));

            Assert.That(offset, Is.EqualTo(new Vector2(100, 0)), "the new start is the old end");
            Assert.That(path.ControlPoints.Select(p => p.Position), Is.EqualTo(new[] { Vector2.Zero, new Vector2(-100, 0) }));
            Assert.That(path.ControlPoints[0].Type, Is.EqualTo(PathType.LINEAR));
        }

        [Test]
        public void TestReverseBezierPath()
        {
            var (offset, path) = reverse(new SliderPath(PathType.BEZIER, [Vector2.Zero, new Vector2(50, 50), new Vector2(100, 0)]));

            Assert.That(offset, Is.EqualTo(new Vector2(100, 0)));
            Assert.That(path.ControlPoints.Select(p => p.Position), Is.EqualTo(new[] { Vector2.Zero, new Vector2(-50, 50), new Vector2(-100, 0) }));
            Assert.That(path.ControlPoints[0].Type, Is.EqualTo(PathType.BEZIER));
        }

        [Test]
        public void TestReverseMultiSegmentPathKeepsSegmentTypesInReverseOrder()
        {
            var path = new SliderPath(
            [
                new PathControlPoint(Vector2.Zero, PathType.LINEAR),
                new PathControlPoint(new Vector2(100, 0), PathType.BEZIER),
                new PathControlPoint(new Vector2(150, 50)),
                new PathControlPoint(new Vector2(200, 0)),
            ]);

            var (offset, reversed) = reverse(path);

            Assert.That(offset, Is.EqualTo(new Vector2(200, 0)));
            Assert.That(reversed.ControlPoints.Select(p => p.Type), Is.EqualTo(new PathType?[] { PathType.BEZIER, null, PathType.LINEAR, null }));
            Assert.That(reversed.ControlPoints.Select(p => p.Position), Is.EqualTo(new[] { Vector2.Zero, new Vector2(-50, 50), new Vector2(-100, 0), new Vector2(-200, 0) }));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void TestReversedPathEndsAtTheOldStart(int shape)
        {
            var path = shape switch
            {
                0 => new SliderPath(PathType.PERFECT_CURVE, [Vector2.Zero, new Vector2(50, 50), new Vector2(100, 0)]),
                1 => new SliderPath(PathType.BEZIER, [Vector2.Zero, new Vector2(30, 80), new Vector2(120, 40), new Vector2(200, 100)]),
                _ => new SliderPath(PathType.LINEAR, [Vector2.Zero, new Vector2(100, 0), new Vector2(100, 100)]),
            };
            var expectedEnd = -path.PositionAt(1);

            var (offset, reversed) = reverse(path);

            Assert.That(offset, Is.EqualTo(-expectedEnd));
            Assert.That(reversed.ControlPoints[0].Position, Is.EqualTo(Vector2.Zero));
            Assert.That(reversed.PositionAt(1).X, Is.EqualTo(expectedEnd.X).Within(0.5));
            Assert.That(reversed.PositionAt(1).Y, Is.EqualTo(expectedEnd.Y).Within(0.5));
        }

        [Test]
        public void TestReverseTrimsSegmentsBeyondTheExpectedDistance()
        {
            // 100 units of expected length cut off the whole second segment.
            var path = new SliderPath(
            [
                new PathControlPoint(Vector2.Zero, PathType.LINEAR),
                new PathControlPoint(new Vector2(100, 0), PathType.LINEAR),
                new PathControlPoint(new Vector2(100, 100)),
            ], 100);

            var (offset, reversed) = reverse(path);

            Assert.That(offset.X, Is.EqualTo(100).Within(1e-3));
            Assert.That(offset.Y, Is.EqualTo(0).Within(1e-3));
            Assert.That(reversed.PositionAt(1).X, Is.EqualTo(-100).Within(0.5));
            Assert.That(reversed.PositionAt(1).Y, Is.EqualTo(0).Within(0.5));
        }
    }
}
