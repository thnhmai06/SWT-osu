// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;
using static osu.Game.Rulesets.Osu.Tests.Difficulty.DifficultyTestHelper;

namespace osu.Game.Rulesets.Osu.Tests.Difficulty.Preprocessing
{
    [TestFixture]
    public class OsuDifficultyHitObjectTest
    {
        private static OsuDifficultyHitObject[] build(OsuHitObject[] objects, double clockRate = 1, BeatmapDifficulty? difficulty = null)
            => Build(objects, clockRate, difficulty).Cast<OsuDifficultyHitObject>().ToArray();

        [Test]
        public void TestDeltaTimeIsCappedBelow()
        {
            var o = build(new[] { Circle(0, 100), Circle(10, 200), Circle(500, 300) });

            Assert.That(o[0].AdjustedDeltaTime, Is.EqualTo(OsuDifficultyHitObject.MIN_DELTA_TIME));
            Assert.That(o[1].AdjustedDeltaTime, Is.EqualTo(490));
        }

        [Test]
        public void TestPreemptIsScaledByClockRate()
        {
            var normal = build(new[] { Circle(0, 100), Circle(500, 200) });
            var fast = build(new[] { Circle(0, 100), Circle(500, 200) }, 1.5);

            Assert.That(fast[0].Preempt, Is.EqualTo(normal[0].Preempt / 1.5).Within(1e-9));
        }

        [Test]
        public void TestSmallCircleBonus()
        {
            var big = build(new[] { Circle(0, 100), Circle(500, 200) }, difficulty: new BeatmapDifficulty { CircleSize = 4 });
            var small = build(new[] { Circle(0, 100), Circle(500, 200) }, difficulty: new BeatmapDifficulty { CircleSize = 9 });

            Assert.That(big[0].SmallCircleBonus, Is.EqualTo(1));
            Assert.That(small[0].SmallCircleBonus, Is.GreaterThan(1));
        }

        [Test]
        public void TestOverallDifficultyRoundTripsFromHitWindow()
        {
            var o = build(new[] { Circle(0, 100), Circle(500, 200) }, difficulty: new BeatmapDifficulty { OverallDifficulty = 8 });

            Assert.That(o[0].OverallDifficulty, Is.EqualTo(8).Within(0.1));
        }

        // CF: OsuDifficultyHitObject.OpacityAt | time > BaseObject.StartTime | TRUE
        [Test]
        public void TestObjectIsInvisibleAfterStartTime()
        {
            var o = build(new[] { Circle(0, 100), Circle(1000, 200) });
            Assert.That(o[0].OpacityAt(1001, false), Is.EqualTo(0));
        }

        // CF: OsuDifficultyHitObject.OpacityAt | time > BaseObject.StartTime | FALSE
        // CF: OsuDifficultyHitObject.OpacityAt | hidden | FALSE
        [Test]
        public void TestOpacityFadesIn()
        {
            var o = build(new[] { Circle(0, 100), Circle(2000, 200) });
            double preempt = ((OsuHitObject)o[0].BaseObject).TimePreempt;
            double start = 2000 - preempt;

            Assert.That(o[0].OpacityAt(start - 100, false), Is.EqualTo(0));
            Assert.That(o[0].OpacityAt(start + 200, false), Is.InRange(0.01, 0.99));
            Assert.That(o[0].OpacityAt(2000, false), Is.EqualTo(1));
        }

        // CF: OsuDifficultyHitObject.OpacityAt | hidden | TRUE
        [Test]
        public void TestHiddenFadesBackOut()
        {
            var o = build(new[] { Circle(0, 100), Circle(2000, 200) });

            Assert.That(o[0].OpacityAt(2000, true), Is.LessThan(o[0].OpacityAt(2000, false)));
            Assert.That(o[0].OpacityAt(2000 - 100000, true), Is.EqualTo(0));
        }

        // CF: OsuDifficultyHitObject.CalculateDoubleTapFeasibility | nextObj == null | TRUE
        [Test]
        public void TestDoubleTapWithoutNextObject()
        {
            var o = build(new[] { Circle(0, 100), Circle(150, 110) });
            Assert.That(o[0].CalculateDoubleTapFeasibility(null), Is.EqualTo(0));
        }

        // CF: OsuDifficultyHitObject.CalculateDoubleTapFeasibility | nextObj == null | FALSE
        [Test]
        public void TestDoubleTapIsMoreFeasibleForOverlappingCircles()
        {
            var close = build(new[] { Circle(0, 100), Circle(30, 105), Circle(200, 110) });
            var far = build(new[] { Circle(0, 100), Circle(30, 300), Circle(200, 110) });

            Assert.That(close[0].CalculateDoubleTapFeasibility(close[1]), Is.GreaterThan(far[0].CalculateDoubleTapFeasibility(far[1])));
        }

        // CF: OsuDifficultyHitObject.setDistances | BaseObject is Spinner || LastObject is Spinner | TRUE
        [Test]
        public void TestSpinnersHaveNoDistances()
        {
            var o = build(new[] { Circle(0, 100), Spinner(200), Circle(1000, 300), Circle(1200, 400) });

            Assert.That(o[0].JumpDistance, Is.EqualTo(0));
            Assert.That(o[0].Angle, Is.Null);
            Assert.That(o[1].JumpDistance, Is.EqualTo(0));
        }

        // CF: OsuDifficultyHitObject.setDistances | BaseObject is Spinner || LastObject is Spinner | FALSE
        // CF: OsuDifficultyHitObject.setDistances | lastLastDifficultyObject != null && lastLastDifficultyObject.BaseObject is not Spinner | FALSE
        [Test]
        public void TestJumpDistanceIsNormalisedByRadius()
        {
            var o = build(new[] { Circle(0, 100), Circle(200, 200) });
            double radius = ((OsuHitObject)o[0].BaseObject).Radius;

            Assert.That(o[0].JumpDistance, Is.EqualTo(100 * OsuDifficultyHitObject.NORMALISED_RADIUS / radius).Within(1e-3));
            Assert.That(o[0].LazyJumpDistance, Is.EqualTo(o[0].JumpDistance).Within(1e-3));
            Assert.That(o[0].Angle, Is.Null);
        }

        // CF: OsuDifficultyHitObject.setDistances | lastLastDifficultyObject != null && lastLastDifficultyObject.BaseObject is not Spinner | TRUE
        [Test]
        public void TestAngles()
        {
            var straight = build(new[] { Circle(0, 100, 100), Circle(200, 200, 100), Circle(400, 300, 100), Circle(600, 400, 100) });
            var right = build(new[] { Circle(0, 100, 100), Circle(200, 200, 100), Circle(400, 200, 200), Circle(600, 100, 200) });

            Assert.That(straight[2].Angle!.Value, Is.EqualTo(Math.PI).Within(1e-3));
            Assert.That(right[2].Angle!.Value, Is.EqualTo(Math.PI / 2).Within(1e-3));
            Assert.That(straight[2].NormalisedVectorAngle, Is.Not.Null);
        }

        // CF: OsuDifficultyHitObject.setDistances | lastLastDifficultyObject != null && lastLastDifficultyObject.BaseObject is not Spinner | FALSE
        [Test]
        public void TestNoAngleAfterSpinner()
        {
            var o = build(new[] { Spinner(0, 100), Circle(1000, 100), Circle(1200, 200), Circle(1400, 300) });

            Assert.That(o[1].Angle, Is.Null);
        }

        // CF: OsuDifficultyHitObject.setDistances | BaseObject is Slider currentSlider | TRUE
        // CF: OsuDifficultyHitObject.computeSliderCursorPosition | BaseObject is not Slider | FALSE
        [Test]
        public void TestSliderTravel()
        {
            var o = build(new[] { Circle(0, 100), Slider(500, 100, 192, 300, 0), Circle(2000, 400, 192) });

            Assert.That(o[0].TravelDistance, Is.GreaterThan(0));
            Assert.That(o[0].LazyEndPosition, Is.Not.Null);
            Assert.That(o[0].TravelTime, Is.GreaterThanOrEqualTo(OsuDifficultyHitObject.MIN_DELTA_TIME));
            Assert.That(o[0].LazyTravelDistance, Is.GreaterThan(0));
        }

        // CF: OsuDifficultyHitObject.setDistances | BaseObject is Slider currentSlider | FALSE
        // CF: OsuDifficultyHitObject.computeSliderCursorPosition | BaseObject is not Slider | TRUE
        [Test]
        public void TestCircleHasNoTravel()
        {
            var o = build(new[] { Circle(0, 100), Circle(500, 200) });

            Assert.That(o[0].TravelDistance, Is.EqualTo(0));
            Assert.That(o[0].LazyEndPosition, Is.Null);
        }

        [Test]
        public void TestRepeatsIncreaseTravelDistance()
        {
            var single = build(new[] { Circle(0, 100), Slider(500, 100, 192, 300, 0, repeats: 0), Circle(5000, 400, 192) });
            var repeated = build(new[] { Circle(0, 100), Slider(500, 100, 192, 300, 0, repeats: 3), Circle(5000, 400, 192) });

            Assert.That(repeated[0].TravelDistance, Is.GreaterThan(single[0].TravelDistance));
        }

        // CF: OsuDifficultyHitObject.setDistances | LastObject is Slider lastSlider && lastDifficultyObject != null | TRUE
        [Test]
        public void TestMinimumJumpAfterSlider()
        {
            var o = build(new[] { Circle(0, 100), Slider(500, 100, 192, 300, 0), Circle(3000, 400, 192) });

            // the circle sits on the slider body, so no real jump is needed
            Assert.That(o[1].MinimumJumpDistance, Is.EqualTo(0).Within(1e-6));
            Assert.That(o[1].MinimumJumpTime, Is.LessThan(o[1].AdjustedDeltaTime));
        }

        // CF: OsuDifficultyHitObject.setDistances | LastObject is Slider lastSlider && lastDifficultyObject != null | FALSE
        [Test]
        public void TestMinimumJumpAfterCircleEqualsLazyJump()
        {
            var o = build(new[] { Circle(0, 100), Circle(200, 300) });

            Assert.That(o[0].MinimumJumpDistance, Is.EqualTo(o[0].LazyJumpDistance));
            Assert.That(o[0].MinimumJumpTime, Is.EqualTo(o[0].AdjustedDeltaTime));
        }

        // CF: OsuDifficultyHitObject.computeSliderCursorPosition | lastRealTick?.StartTime > trackingEndTime | TRUE
        [Test]
        public void TestSliderWithManyTicksAndRepeatsCompletes()
        {
            var o = build(new[] { Circle(0, 100), Slider(500, 100, 192, 400, 100, repeats: 2), Circle(6000, 300, 192) });

            Assert.That(o[0].LazyEndPosition, Is.Not.Null);
            Assert.That(o[0].LazyTravelTime, Is.GreaterThan(0));
        }
    }
}
