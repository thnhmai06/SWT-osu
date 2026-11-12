// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;
using static osu.Game.Rulesets.Osu.Utils.OsuHitObjectGenerationUtils;

namespace osu.Game.Rulesets.Osu.Tests.Utils
{
    [TestFixture]
    public class OsuHitObjectGenerationUtilsRepositionTest
    {
        private static void assertVector(Vector2 actual, float x, float y, float tolerance = 1e-3f)
        {
            Assert.That(actual.X, Is.EqualTo(x).Within(tolerance), "X");
            Assert.That(actual.Y, Is.EqualTo(y).Within(tolerance), "Y");
        }

        private static HitCircle circle(float x, float y) => new HitCircle { Position = new Vector2(x, y) };

        private static Slider slider(float x, float y, float length, float dy = 0)
            => new Slider { Position = new Vector2(x, y), Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(length, dy) }) };

        private static ObjectPositionInfo info(OsuHitObject hitObject, float distance, float relativeAngle = 0, float rotation = 0)
            => new ObjectPositionInfo(hitObject) { DistanceFromPrevious = distance, RelativeAngle = relativeAngle, Rotation = rotation };

        // ---- GeneratePositionInfos ----

        [Test]
        public void TestGenerate_NoObjects_ReturnsEmpty()
        {
            // foreach body never runs
            Assert.That(GeneratePositionInfos(Array.Empty<OsuHitObject>()), Is.Empty);
        }

        [Test]
        public void TestGenerate_Circles_RecordDistanceAndAngleRelativeToPrevious()
        {
            var first = circle(356, 192);
            var second = circle(356, 292);

            var infos = GeneratePositionInfos(new OsuHitObject[] { first, second });

            // first: 100 to the right of the playfield centre; second: 100 below the first (90 degrees)
            Assert.That(infos[0].DistanceFromPrevious, Is.EqualTo(100).Within(1e-3));
            Assert.That(infos[0].RelativeAngle, Is.EqualTo(0).Within(1e-5));
            Assert.That(infos[1].DistanceFromPrevious, Is.EqualTo(100).Within(1e-3));
            Assert.That(infos[1].RelativeAngle, Is.EqualTo(MathF.PI / 2).Within(1e-5));
            Assert.That(infos[0].Rotation, Is.EqualTo(0));
        }

        [Test]
        public void TestGenerate_Slider_RecordsRotationAndBecomesAngleReference()
        {
            // `hitObject is Slider` TRUE: rotation = slider direction (90deg) - approach angle (0deg)
            var s = slider(356, 192, 0, 100);
            var next = circle(356, 392);

            var infos = GeneratePositionInfos(new OsuHitObject[] { s, next });

            Assert.That(infos[0].Rotation, Is.EqualTo(MathF.PI / 2).Within(1e-5));
            // the slider ends at (356, 292); the next circle is straight down from there, which is the slider's own direction => relative angle 0
            Assert.That(infos[1].DistanceFromPrevious, Is.EqualTo(100).Within(1e-3));
            Assert.That(infos[1].RelativeAngle, Is.EqualTo(0).Within(1e-5));
        }

        // ---- RepositionHitObjects ----

        [Test]
        public void TestReposition_NoObjects_ReturnsEmpty()
        {
            Assert.That(RepositionHitObjects(Array.Empty<ObjectPositionInfo>()), Is.Empty);
        }

        [Test]
        public void TestReposition_Spinner_IsReturnedUntouched()
        {
            // hitObject is Spinner TRUE -> continue
            var spinner = new Spinner { Position = new Vector2(256, 192) };

            var result = RepositionHitObjects(new[] { info(spinner, 500) });

            Assert.That(result, Is.EqualTo(new OsuHitObject[] { spinner }));
            assertVector(spinner.Position, 256, 192);
        }

        [Test]
        public void TestReposition_CirclesInsideCentralArea_RoundTripToSamePositions()
        {
            // no edge rotation inside x 192..320, y 144..240; previous == null, then beforePrevious null / non-null
            var circles = new OsuHitObject[] { circle(256, 192), circle(300, 200), circle(220, 160) };
            var expected = circles.Select(c => c.Position).ToArray();

            var result = RepositionHitObjects(GeneratePositionInfos(circles));

            for (int i = 0; i < expected.Length; i++)
                assertVector(result[i].Position, expected[i].X, expected[i].Y);
        }

        [Test]
        public void TestReposition_ShortSliderBetweenCircles_RoundTripsWithoutRotation()
        {
            // previous is a Slider for the last circle; slider distance <= 50 (short centre-of-mass branch);
            // relative rotation ~0 so RotateSlider is skipped
            var circles = new OsuHitObject[] { circle(256, 192), slider(260, 190, 40), circle(240, 180) };
            var endOfSlider = circles[1].EndPosition;

            var result = RepositionHitObjects(GeneratePositionInfos(circles));

            assertVector(result[1].Position, 260, 190);
            assertVector(result[1].EndPosition, endOfSlider.X, endOfSlider.Y);
            assertVector(result[2].Position, 240, 180);
        }

        [Test]
        public void TestReposition_LongSlider_RoundTripsWithoutRotation()
        {
            // slider distance > 50 (sampling loop of the centre-of-mass calculation)
            var circles = new OsuHitObject[] { circle(256, 192), slider(230, 190, 80) };

            var result = RepositionHitObjects(GeneratePositionInfos(circles));

            assertVector(result[1].Position, 230, 190);
            assertVector(((Slider)result[1]).Path.ControlPoints[1].Position, 80, 0);
        }

        [Test]
        public void TestReposition_CircleOutsidePlayfield_IsClampedByRadius()
        {
            // 400 to the right of the centre => x = 656, clamped to 512 - radius (64)
            var c = circle(256, 192);

            var result = RepositionHitObjects(new[] { info(c, 400) });

            assertVector(result[0].Position, 448, 192);
        }

        [Test]
        public void TestReposition_ClampShift_MovesPrecedingCirclesWithDecreasingStrength()
        {
            // shift = (-208, 0); a single preceding circle is moved by shift * 1/2
            var preceding = circle(256, 192);
            var pushed = circle(256, 192);

            RepositionHitObjects(new[] { info(preceding, 0), info(pushed, 400) });

            assertVector(preceding.Position, 152, 192);
            assertVector(pushed.Position, 448, 192);
        }

        [Test]
        public void TestReposition_ClampShift_OnlyShiftsTenPrecedingObjects()
        {
            // 11 circles at the centre, then one pushed out: the 10 nearest move, the oldest stays
            var circles = Enumerable.Range(0, 11).Select(_ => circle(256, 192)).ToList();
            var pushed = circle(256, 192);
            var infos = circles.Select(c => info(c, 0)).Append(info(pushed, 400)).ToList();

            RepositionHitObjects(infos);

            assertVector(circles[0].Position, 256, 192);
            // nearest (index 10) moves by 10/11 of the shift, farthest shifted one (index 1) by 1/11
            assertVector(circles[10].Position, 256 - 208f * 10 / 11, 192, 1e-2f);
            assertVector(circles[1].Position, 256 - 208f / 11, 192, 1e-2f);
        }

        [Test]
        public void TestReposition_ClampShift_StopsAtNonCircle()
        {
            // preceding spinner is not a HitCircle => break, nothing to shift
            var spinner = new Spinner { Position = new Vector2(256, 192) };
            var pushed = circle(256, 192);

            RepositionHitObjects(new[] { info(spinner, 0), info(pushed, 400) });

            assertVector(spinner.Position, 256, 192);
            assertVector(pushed.Position, 448, 192);
        }

        [Test]
        public void TestReposition_InBoundsCircle_ProducesNoShift()
        {
            // shift == Zero: preceding circles are left alone
            var preceding = circle(256, 192);
            var inside = circle(256, 192);

            RepositionHitObjects(new[] { info(preceding, 0), info(inside, 100) });

            assertVector(preceding.Position, 256, 192);
            assertVector(inside.Position, 356, 192);
        }

        [Test]
        public void TestReposition_SliderBeyondRightEdge_EndsUpInsideItsMovementBounds()
        {
            // 300px right of centre => unclamped x = 556 (outside); after clamping the slider must sit inside the bounds computed for its final shape
            var s = slider(256, 192, 40);

            RepositionHitObjects(new[] { info(s, 300) });

            var bounds = CalculatePossibleMovementBounds(s);
            Assert.That(bounds.Width, Is.GreaterThanOrEqualTo(0));
            Assert.That(s.Position.X, Is.InRange(bounds.Left - 1e-2f, bounds.Right + 1e-2f));
            Assert.That(s.Position.Y, Is.InRange(bounds.Top - 1e-2f, bounds.Bottom + 1e-2f));
            Assert.That(s.Position.X, Is.LessThan(556));
        }

        [Test]
        public void TestReposition_SliderTooLargeForPlayfield_PinnedToLeftBound()
        {
            // movement bounds Width < 0: X pinned to Left (64), Y free to stay
            var s = slider(256, 192, 500);

            RepositionHitObjects(new[] { info(s, 0) });

            assertVector(s.Position, 64, 192);
        }

        [Test]
        public void TestReposition_SliderTooLargeAndRotatedHalfTurn_TakesTheOppositeOrientation()
        {
            // Rotation = pi flips the slider during positioning; the clamp then prefers the opposite orientation (diff1 >= diff2)
            var s = slider(256, 192, 500);

            RepositionHitObjects(new[] { info(s, 0, rotation: MathF.PI) });

            assertVector(s.Position, 512, 192, 1e-2f);
        }

        [Test]
        public void TestReposition_SliderTooTallForPlayfield_PinnedToTopBound()
        {
            // movement bounds Height < 0: Y pinned to Top (64); Rotation = pi / 2 keeps the slider vertical during positioning
            var s = new Slider { Position = new Vector2(256, 192), Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(0, 400) }) };

            RepositionHitObjects(new[] { info(s, 0, rotation: MathF.PI / 2) });

            assertVector(s.Position, 256, 64, 1e-2f);
        }

        // ---- CalculatePossibleMovementBounds ----

        [Test]
        public void TestPossibleMovementBounds_KeepsRadiusInsidePlayfield()
        {
            // radius 64: left/top = 64, right = 512 - (40 + 64), bottom = 384 - 64
            var bounds = CalculatePossibleMovementBounds(slider(0, 0, 40));

            Assert.That(bounds.Left, Is.EqualTo(64).Within(1e-3));
            Assert.That(bounds.Top, Is.EqualTo(64).Within(1e-3));
            Assert.That(bounds.Right, Is.EqualTo(408).Within(1e-3));
            Assert.That(bounds.Bottom, Is.EqualTo(320).Within(1e-3));
        }

        [Test]
        public void TestPossibleMovementBounds_ScaleShrinksRadius()
        {
            var s = slider(0, 0, 40);
            s.Scale = 0.5f;

            var bounds = CalculatePossibleMovementBounds(s);

            Assert.That(bounds.Left, Is.EqualTo(32).Within(1e-3));
            Assert.That(bounds.Right, Is.EqualTo(512 - 40 - 32).Within(1e-3));
        }

        [Test]
        public void TestPossibleMovementBounds_OversizedSlider_HasNegativeWidth()
        {
            Assert.That(CalculatePossibleMovementBounds(slider(0, 0, 500)).Width, Is.LessThan(0));
        }
    }
}
