// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Utils;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Utils
{
    [TestFixture]
    public class OsuHitObjectGenerationUtilsTest
    {
        private static readonly Vector2 centre = new Vector2(256, 192);

        private static HitCircle circle(float x, float y) => new HitCircle { Position = new Vector2(x, y) };

        private static Slider slider(float x, float y, float dx, float dy) => new Slider
        {
            Position = new Vector2(x, y),
            Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(dx, dy) }),
        };

        private static void assertVector(Vector2 expected, Vector2 actual, float tolerance = 0.01f)
        {
            Assert.That(actual.X, Is.EqualTo(expected.X).Within(tolerance));
            Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(tolerance));
        }

        private static Vector2 polar(float length, float degrees)
            => new Vector2(length * MathF.Cos(MathHelper.DegreesToRadians(degrees)), length * MathF.Sin(MathHelper.DegreesToRadians(degrees)));

        #region RotateAwayFromEdge

        // CF: OsuHitObjectGenerationUtils.RotateAwayFromEdge | prevObjectPos.X < playfield_middle.X | FALSE
        // CF: OsuHitObjectGenerationUtils.RotateAwayFromEdge | prevObjectPos.Y < playfield_middle.Y | FALSE
        [Test]
        public void TestObjectAtCentreIsNotRotated()
        {
            assertVector(new Vector2(0, 50), OsuHitObjectGenerationUtils.RotateAwayFromEdge(centre, new Vector2(0, 50)));
        }

        // CF: OsuHitObjectGenerationUtils.RotateAwayFromEdge | prevObjectPos.X < playfield_middle.X | TRUE
        [Test]
        public void TestRotationAwayFromLeftEdge()
        {
            // at the very edge, default ratio 0.5 rotates halfway to the direction of the centre (0 degrees)
            assertVector(polar(50, 45), OsuHitObjectGenerationUtils.RotateAwayFromEdge(new Vector2(0, 192), new Vector2(0, 50)));
        }

        // CF: OsuHitObjectGenerationUtils.RotateAwayFromEdge | prevObjectPos.Y < playfield_middle.Y | TRUE
        [Test]
        public void TestRotationAwayFromTopEdge()
        {
            assertVector(polar(50, 45), OsuHitObjectGenerationUtils.RotateAwayFromEdge(new Vector2(256, 0), new Vector2(50, 0)));
        }

        [Test]
        public void TestRotationAwayFromBottomEdge()
        {
            assertVector(polar(50, -45), OsuHitObjectGenerationUtils.RotateAwayFromEdge(new Vector2(256, 384), new Vector2(50, 0)));
        }

        [Test]
        public void TestRotationAwayFromRightEdge()
        {
            assertVector(polar(50, 135), OsuHitObjectGenerationUtils.RotateAwayFromEdge(new Vector2(512, 192), new Vector2(0, 50)));
        }

        // CF: OsuHitObjectGenerationUtils.RotateAwayFromEdge | Math.Min(1, relativeRotationDistance * rotationRatio) | non-null
        [Test]
        public void TestRotationRatioIsCappedAtFullRotation()
        {
            // a ratio of 2 at the edge would overshoot, but is capped to rotating fully onto the centre direction (0 degrees)
            assertVector(new Vector2(50, 0), OsuHitObjectGenerationUtils.RotateAwayFromEdge(new Vector2(0, 192), new Vector2(0, 50), 2));
        }

        #endregion

        #region RotateVectorTowardsVector

        // CF: OsuHitObjectGenerationUtils.RotateVectorTowardsVector | diff < -MathF.PI | FALSE
        // CF: OsuHitObjectGenerationUtils.RotateVectorTowardsVector | diff > MathF.PI | FALSE
        [Test]
        public void TestRotateTowardsWithoutWrapping()
        {
            assertVector(polar(10, 45), OsuHitObjectGenerationUtils.RotateVectorTowardsVector(polar(10, 0), polar(3, 90), 0.5f));
        }

        // CF: OsuHitObjectGenerationUtils.RotateVectorTowardsVector | diff < -MathF.PI | TRUE
        [Test]
        public void TestRotateTowardsWrapsForwardAcrossPi()
        {
            // 170 -> -170 degrees is a 20 degree turn the short way round
            assertVector(polar(10, 180), OsuHitObjectGenerationUtils.RotateVectorTowardsVector(polar(10, 170), polar(1, -170), 0.5f));
        }

        // CF: OsuHitObjectGenerationUtils.RotateVectorTowardsVector | diff > MathF.PI | TRUE
        [Test]
        public void TestRotateTowardsWrapsBackwardAcrossPi()
        {
            assertVector(polar(10, 180), OsuHitObjectGenerationUtils.RotateVectorTowardsVector(polar(10, -170), polar(1, 170), 0.5f));
        }

        [Test]
        public void TestRotateTowardsRatioZeroAndOne()
        {
            assertVector(polar(10, 30), OsuHitObjectGenerationUtils.RotateVectorTowardsVector(polar(10, 30), polar(1, 100), 0));
            assertVector(polar(10, 100), OsuHitObjectGenerationUtils.RotateVectorTowardsVector(polar(10, 30), polar(1, 100), 1));
        }

        #endregion

        #region Reflection / flipping / rotation

        // CF: OsuHitObjectGenerationUtils.ReflectHorizontallyAlongPlayfield | osuObject is not Slider slider | TRUE
        [Test]
        public void TestReflectCircleHorizontally()
        {
            var c = circle(100, 50);
            OsuHitObjectGenerationUtils.ReflectHorizontallyAlongPlayfield(c);
            assertVector(new Vector2(412, 50), c.Position);
        }

        // CF: OsuHitObjectGenerationUtils.ReflectHorizontallyAlongPlayfield | osuObject is not Slider slider | FALSE
        [Test]
        public void TestReflectSliderHorizontally()
        {
            var s = slider(100, 50, 40, 30);
            OsuHitObjectGenerationUtils.ReflectHorizontallyAlongPlayfield(s);
            assertVector(new Vector2(412, 50), s.Position);
            assertVector(new Vector2(-40, 30), s.Path.ControlPoints[1].Position);
        }

        // CF: OsuHitObjectGenerationUtils.ReflectVerticallyAlongPlayfield | osuObject is not Slider slider | TRUE
        [Test]
        public void TestReflectCircleVertically()
        {
            var c = circle(100, 50);
            OsuHitObjectGenerationUtils.ReflectVerticallyAlongPlayfield(c);
            assertVector(new Vector2(100, 334), c.Position);
        }

        // CF: OsuHitObjectGenerationUtils.ReflectVerticallyAlongPlayfield | osuObject is not Slider slider | FALSE
        [Test]
        public void TestReflectSliderVertically()
        {
            var s = slider(100, 50, 40, 30);
            OsuHitObjectGenerationUtils.ReflectVerticallyAlongPlayfield(s);
            assertVector(new Vector2(100, 334), s.Position);
            assertVector(new Vector2(40, -30), s.Path.ControlPoints[1].Position);
        }

        [Test]
        public void TestFlipSliderInPlaceKeepsStartPosition()
        {
            var s = slider(100, 50, 40, 30);
            OsuHitObjectGenerationUtils.FlipSliderInPlaceHorizontally(s);
            assertVector(new Vector2(100, 50), s.Position);
            assertVector(new Vector2(-40, 30), s.Path.ControlPoints[1].Position);
        }

        [Test]
        public void TestRotateSlider()
        {
            var s = slider(100, 50, 40, 0);
            OsuHitObjectGenerationUtils.RotateSlider(s, MathF.PI / 2);
            assertVector(new Vector2(0, 40), s.Path.ControlPoints[1].Position);
        }

        #endregion

        #region IsHitObjectOnBeat

        private static OsuBeatmap beatmapWithBeatLength(double beatLength)
        {
            var beatmap = new OsuBeatmap();
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = beatLength });
            return beatmap;
        }

        // CF: OsuHitObjectGenerationUtils.IsHitObjectOnBeat | downbeatsOnly | FALSE
        [Test]
        public void TestOnBeatWithinOneMillisecond()
        {
            var beatmap = beatmapWithBeatLength(500);

            Assert.That(OsuHitObjectGenerationUtils.IsHitObjectOnBeat(beatmap, new HitCircle { StartTime = 500 }), Is.True);
            Assert.That(OsuHitObjectGenerationUtils.IsHitObjectOnBeat(beatmap, new HitCircle { StartTime = 1000 }), Is.True);
            Assert.That(OsuHitObjectGenerationUtils.IsHitObjectOnBeat(beatmap, new HitCircle { StartTime = 250 }), Is.False);
        }

        // CF: OsuHitObjectGenerationUtils.IsHitObjectOnBeat | downbeatsOnly | TRUE
        [Test]
        public void TestOnDownbeat()
        {
            var beatmap = beatmapWithBeatLength(500);

            // default 4/4 -> downbeats every 2000ms
            Assert.That(OsuHitObjectGenerationUtils.IsHitObjectOnBeat(beatmap, new HitCircle { StartTime = 2000 }, true), Is.True);
            Assert.That(OsuHitObjectGenerationUtils.IsHitObjectOnBeat(beatmap, new HitCircle { StartTime = 500 }, true), Is.False);
        }

        #endregion

        [Test]
        public void TestRandomGaussianWithZeroDeviationReturnsMean()
        {
            Assert.That(OsuHitObjectGenerationUtils.RandomGaussian(new Random(1), 3, 0), Is.EqualTo(3));
        }

        [Test]
        public void TestRandomGaussianIsDeterministicForSeedAndScales()
        {
            float a = OsuHitObjectGenerationUtils.RandomGaussian(new Random(7));
            float b = OsuHitObjectGenerationUtils.RandomGaussian(new Random(7), 10, 2);

            Assert.That(b, Is.EqualTo(10 + 2 * a).Within(1e-4));
        }

        #region GeneratePositionInfos

        // CF: OsuHitObjectGenerationUtils.GeneratePositionInfos | foreach hitObjects | loop 0
        [Test]
        public void TestGenerateForNoObjects()
        {
            Assert.That(OsuHitObjectGenerationUtils.GeneratePositionInfos(Array.Empty<OsuHitObject>()), Is.Empty);
        }

        // CF: OsuHitObjectGenerationUtils.GeneratePositionInfos | foreach hitObjects | loop n
        // CF: OsuHitObjectGenerationUtils.GeneratePositionInfos | hitObject is Slider slider | FALSE
        [Test]
        public void TestGenerateForCircles()
        {
            var infos = OsuHitObjectGenerationUtils.GeneratePositionInfos(new OsuHitObject[] { circle(306, 192), circle(306, 242) });

            Assert.That(infos, Has.Count.EqualTo(2));
            Assert.That(infos[0].DistanceFromPrevious, Is.EqualTo(50).Within(1e-3));
            Assert.That(infos[0].RelativeAngle, Is.EqualTo(0).Within(1e-3));
            Assert.That(infos[1].DistanceFromPrevious, Is.EqualTo(50).Within(1e-3));
            Assert.That(infos[1].RelativeAngle, Is.EqualTo(MathF.PI / 2).Within(1e-3));
            Assert.That(infos[1].Rotation, Is.EqualTo(0));
        }

        // CF: OsuHitObjectGenerationUtils.GeneratePositionInfos | hitObject is Slider slider | TRUE
        [Test]
        public void TestGenerateForSliderRecordsRotation()
        {
            // jump angle is 90 degrees (straight down), the slider itself points right (0 degrees)
            var infos = OsuHitObjectGenerationUtils.GeneratePositionInfos(new OsuHitObject[] { slider(256, 242, 100, 0) });

            Assert.That(infos[0].Rotation, Is.EqualTo(-MathF.PI / 2).Within(1e-3));
        }

        #endregion

        #region RepositionHitObjects

        // CF: OsuHitObjectGenerationUtils.RepositionHitObjects | for (i < workingObjects.Count) | loop 0
        [Test]
        public void TestRepositionNothing()
        {
            Assert.That(OsuHitObjectGenerationUtils.RepositionHitObjects(Array.Empty<OsuHitObjectGenerationUtils.ObjectPositionInfo>()), Is.Empty);
        }

        // CF: OsuHitObjectGenerationUtils.RepositionHitObjects | for (i < workingObjects.Count) | loop n
        // CF: OsuHitObjectGenerationUtils.RepositionHitObjects | hitObject is Spinner | FALSE
        // CF: OsuHitObjectGenerationUtils.RepositionHitObjects | i > 1 | TRUE
        // CF: OsuHitObjectGenerationUtils.RepositionHitObjects | shift != Vector2.Zero | FALSE
        // CF: OsuHitObjectGenerationUtils.computeModifiedPosition | previous != null | TRUE
        // CF: OsuHitObjectGenerationUtils.computeModifiedPosition | previous.HitObject is Slider s | TRUE
        // CF: OsuHitObjectGenerationUtils.computeModifiedPosition | previous.HitObject is Slider s | FALSE
        // CF: OsuHitObjectGenerationUtils.computeModifiedPosition | current.HitObject is Slider slider | TRUE
        // CF: OsuHitObjectGenerationUtils.computeModifiedPosition | !Precision.AlmostEquals(relativeRotation, 0) | FALSE
        // CF: OsuHitObjectGenerationUtils.calculateCentreOfMass | slider.Distance <= sample_step | TRUE
        [Test]
        public void TestRepositionOfUnchangedLayoutIsIdentity()
        {
            var objects = new OsuHitObject[]
            {
                circle(270, 200),
                slider(230, 190, 40, 0),
                circle(300, 180),
                circle(240, 210),
            };

            var original = objects.Select(o => o.Position).ToArray();
            var infos = OsuHitObjectGenerationUtils.GeneratePositionInfos(objects);
            var result = OsuHitObjectGenerationUtils.RepositionHitObjects(infos);

            Assert.That(result, Has.Count.EqualTo(4));
            for (int i = 0; i < result.Count; i++)
                assertVector(original[i], result[i].Position);
        }

        // CF: OsuHitObjectGenerationUtils.RepositionHitObjects | hitObject is Spinner | TRUE
        // CF: OsuHitObjectGenerationUtils.computeModifiedPosition | previous != null | FALSE
        // CF: OsuHitObjectGenerationUtils.computeModifiedPosition | current.HitObject is Slider slider | FALSE
        [Test]
        public void TestSpinnerIsNotMoved()
        {
            var spinner = new Spinner { Position = new Vector2(10, 20) };
            var result = OsuHitObjectGenerationUtils.RepositionHitObjects(new[] { new OsuHitObjectGenerationUtils.ObjectPositionInfo(spinner) { DistanceFromPrevious = 500 } });

            assertVector(new Vector2(10, 20), result.Single().Position);
        }

        // CF: OsuHitObjectGenerationUtils.RepositionHitObjects | shift != Vector2.Zero | TRUE
        // CF: OsuHitObjectGenerationUtils.RepositionHitObjects | switch (hitObject) | case HitCircle
        // CF: OsuHitObjectGenerationUtils.RepositionHitObjects | for (j = i - 1; j >= i - preceding_hitobjects_to_shift && j >= 0) | loop n
        // CF: OsuHitObjectGenerationUtils.RepositionHitObjects | j >= i - preceding_hitobjects_to_shift | FALSE
        // CF: OsuHitObjectGenerationUtils.RepositionHitObjects | workingObjects[j].HitObject is HitCircle | TRUE
        // CF: OsuHitObjectGenerationUtils.RepositionHitObjects | toBeShifted.Count > 0 | TRUE
        // CF: OsuHitObjectGenerationUtils.applyDecreasingShift | for (i < hitObjects.Count) | loop n
        // CF: OsuHitObjectGenerationUtils.computeModifiedPosition | beforePrevious?.HitObject.EndPosition ?? playfield_centre | non-null
        [Test]
        public void TestOutOfPlayfieldCircleIsClampedAndOnlyTenPrecedingCirclesShift()
        {
            var circles = Enumerable.Range(0, 12).Select(i => circle(i % 2 == 0 ? 270 : 240, 192)).ToList();
            var outside = circle(0, 0);

            var infos = OsuHitObjectGenerationUtils.GeneratePositionInfos(circles.Cast<OsuHitObject>()).ToList();
            infos.Add(new OsuHitObjectGenerationUtils.ObjectPositionInfo(outside) { RelativeAngle = 0, DistanceFromPrevious = 1000 });

            var originalFirstTwo = circles.Take(2).Select(c => c.Position).ToArray();
            var originalLast = circles[11].Position;

            OsuHitObjectGenerationUtils.RepositionHitObjects(infos);

            // the jump continues the previous leftward movement, far outside -> clamped to the radius-padded left edge
            Assert.That(outside.Position.X, Is.EqualTo((float)outside.Radius).Within(1e-3));

            // the oldest two circles are beyond the 10 object shift window
            assertVector(originalFirstTwo[0], circles[0].Position);
            assertVector(originalFirstTwo[1], circles[1].Position);

            // the most recent circle is pulled right, by the amount the object was moved back into the playfield
            Assert.That(circles[11].Position.X, Is.GreaterThan(originalLast.X));
        }

        // CF: OsuHitObjectGenerationUtils.RepositionHitObjects | switch (hitObject) | case Slider
        // CF: OsuHitObjectGenerationUtils.RepositionHitObjects | workingObjects[j].HitObject is HitCircle | FALSE
        // CF: OsuHitObjectGenerationUtils.RepositionHitObjects | toBeShifted.Count > 0 | FALSE
        // CF: OsuHitObjectGenerationUtils.computeModifiedPosition | !Precision.AlmostEquals(relativeRotation, 0) | TRUE
        // CF: OsuHitObjectGenerationUtils.clampSliderToPlayfield | possibleMovementBounds.Width < 0 || possibleMovementBounds.Height < 0 | FALSE
        // CF: OsuHitObjectGenerationUtils.clampSliderToPlayfield | possibleMovementBounds.Width < 0 | FALSE
        // CF: OsuHitObjectGenerationUtils.clampSliderToPlayfield | possibleMovementBounds.Height < 0 | FALSE
        // CF: OsuHitObjectGenerationUtils.calculateCentreOfMass | slider.Distance <= sample_step | FALSE
        // CF: OsuHitObjectGenerationUtils.calculateCentreOfMass | for (i < pathDistance) | loop n
        [Test]
        public void TestOutOfPlayfieldSliderIsRotatedAwayAndClampedInside()
        {
            var preceding = slider(256, 192, 10, 0);
            var outside = slider(0, 0, 200, 0);

            var infos = new[]
            {
                new OsuHitObjectGenerationUtils.ObjectPositionInfo(preceding),
                new OsuHitObjectGenerationUtils.ObjectPositionInfo(outside) { RelativeAngle = 0, DistanceFromPrevious = 1000 },
            };

            OsuHitObjectGenerationUtils.RepositionHitObjects(infos);

            // too close to the right edge: the slider is turned around to point inwards, then pushed fully inside
            Assert.That(outside.Path.ControlPoints[1].Position.X, Is.LessThan(0));
            var bounds = OsuHitObjectGenerationUtils.CalculatePossibleMovementBounds(outside);
            Assert.That(outside.Position.X, Is.InRange(bounds.Left - 0.01f, bounds.Right + 0.01f));
            Assert.That(outside.Position.Y, Is.InRange(bounds.Top - 0.01f, bounds.Bottom + 0.01f));
        }

        // CF: OsuHitObjectGenerationUtils.clampSliderToPlayfield | possibleMovementBounds.Width < 0 || possibleMovementBounds.Height < 0 | TRUE
        // CF: OsuHitObjectGenerationUtils.clampSliderToPlayfield | possibleMovementBounds.Width < 0 | TRUE
        // CF: OsuHitObjectGenerationUtils.clampSliderToPlayfield | diff1 < diff2 | TRUE
        [Test]
        public void TestSliderWiderThanPlayfieldKeepsOriginalRotationAndHead()
        {
            var wide = slider(0, 0, 700, 0);
            var info = new OsuHitObjectGenerationUtils.ObjectPositionInfo(wide) { RelativeAngle = 0, DistanceFromPrevious = 0 };

            OsuHitObjectGenerationUtils.RepositionHitObjects(new[] { info });

            // head circle is pulled to the left edge padding, still pointing right
            assertVector(new Vector2(64, 192), wide.Position);
            Assert.That(wide.Path.ControlPoints[1].Position.X, Is.EqualTo(700).Within(0.01));
        }

        // CF: OsuHitObjectGenerationUtils.clampSliderToPlayfield | possibleMovementBounds.Height < 0 | TRUE
        // CF: OsuHitObjectGenerationUtils.clampSliderToPlayfield | possibleMovementBounds.Width < 0 | FALSE
        [Test]
        public void TestSliderTallerThanPlayfieldIsPinnedToTop()
        {
            var tall = slider(0, 0, 0, 500);
            var info = new OsuHitObjectGenerationUtils.ObjectPositionInfo(tall) { RelativeAngle = 0, DistanceFromPrevious = 0, Rotation = MathF.PI / 2 };

            OsuHitObjectGenerationUtils.RepositionHitObjects(new[] { info });

            assertVector(new Vector2(256, 64), tall.Position);
        }

        // CF: OsuHitObjectGenerationUtils.clampSliderToPlayfield | diff1 < diff2 | FALSE
        [Test]
        public void TestSliderReversedByLayoutIsKeptReversed()
        {
            var wide = slider(0, 0, 700, 0);
            var info = new OsuHitObjectGenerationUtils.ObjectPositionInfo(wide) { RelativeAngle = 0, DistanceFromPrevious = 0, Rotation = MathF.PI };

            OsuHitObjectGenerationUtils.RepositionHitObjects(new[] { info });

            // reversed slider points left; the head is clamped to the right edge of the playfield
            Assert.That(wide.Path.ControlPoints[1].Position.X, Is.EqualTo(-700).Within(0.01));
            assertVector(new Vector2(512, 192), wide.Position);
        }

        #endregion

        #region CalculatePossibleMovementBounds

        // CF: OsuHitObjectGenerationUtils.CalculatePossibleMovementBounds | foreach pos in CalculatedPath | loop n
        [Test]
        public void TestPossibleMovementBoundsOfStraightSlider()
        {
            var bounds = OsuHitObjectGenerationUtils.CalculatePossibleMovementBounds(slider(0, 0, 100, 0));
            float radius = (float)new HitCircle().Radius;

            Assert.That(bounds.Left, Is.EqualTo(radius).Within(1e-3));
            Assert.That(bounds.Right, Is.EqualTo(512 - 100 - radius).Within(1e-3));
            Assert.That(bounds.Top, Is.EqualTo(radius).Within(1e-3));
            Assert.That(bounds.Bottom, Is.EqualTo(384 - radius).Within(1e-3));
        }

        #endregion
    }
}
