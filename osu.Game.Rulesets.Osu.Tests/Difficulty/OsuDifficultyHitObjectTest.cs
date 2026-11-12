// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using static osu.Game.Rulesets.Osu.Tests.Difficulty.DifficultyTestHelpers;

namespace osu.Game.Rulesets.Osu.Tests.Difficulty
{
    [TestFixture]
    public class OsuDifficultyHitObjectTest
    {
        private static OsuHitObject[] objs(params OsuHitObject[] objects) => objects;

        // ---- construction ----

        [Test]
        public void TestFirstHitObject_IsNotCreatedForDifficulty()
        {
            // the first jump is formed by the first two objects, so n objects yield n - 1 difficulty objects
            Assert.That(DifficultyObjects(objs(Circle(0, 100, 100), Circle(500, 200, 100), Circle(1000, 300, 100))), Has.Count.EqualTo(2));
            Assert.That(DifficultyObjects(objs(Circle(0, 100, 100))), Is.Empty);
        }

        // ---- AdjustedDeltaTime / LastObjectEndDeltaTime ----

        [Test]
        public void TestAdjustedDeltaTime_ClampedToMinimumForSimultaneousObjects()
        {
            // Math.Max(DeltaTime, 25) picks the constant
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(10, 200, 100)));

            Assert.That(o[0].AdjustedDeltaTime, Is.EqualTo(OsuDifficultyHitObject.MIN_DELTA_TIME));
        }

        [Test]
        public void TestAdjustedDeltaTime_KeepsRealDeltaWhenLargeEnough()
        {
            // Math.Max picks DeltaTime
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(300, 200, 100)));

            Assert.That(o[0].AdjustedDeltaTime, Is.EqualTo(300).Within(1e-6));
        }

        [Test]
        public void TestAdjustedDeltaTime_IsScaledByClockRate()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(300, 200, 100)), new Mod[] { new OsuModDoubleTime() });

            Assert.That(o[0].AdjustedDeltaTime, Is.EqualTo(300 / 1.5).Within(1e-6));
        }

        [Test]
        public void TestLastObjectEndDeltaTime_FirstObject_EqualsAdjustedDeltaTime()
        {
            // Previous() is null for the first difficulty object
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(300, 200, 100)));

            Assert.That(o[0].LastObjectEndDeltaTime, Is.EqualTo(o[0].AdjustedDeltaTime));
        }

        [Test]
        public void TestLastObjectEndDeltaTime_AfterCircle_EqualsDeltaTime()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(300, 200, 100), Circle(600, 300, 100)));

            Assert.That(o[1].LastObjectEndDeltaTime, Is.EqualTo(300).Within(1e-6));
        }

        [Test]
        public void TestLastObjectEndDeltaTime_AfterSlider_MeasuredFromSliderEnd()
        {
            // slider B starts at 1000 and lasts ~714ms, so the gap to C (2000) is measured from its end
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Slider(1000, 100, 100, 100), Circle(2000, 300, 100)));

            Assert.That(o[1].DeltaTime, Is.EqualTo(1000).Within(1e-6));
            Assert.That(o[1].LastObjectEndDeltaTime, Is.InRange(250.0, 300.0));
        }

        [Test]
        public void TestLastObjectEndDeltaTime_NeverBelowMinimum()
        {
            // slider ends after the next object starts => Math.Max picks 25
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Slider(1000, 100, 100, 200), Circle(1100, 300, 100)));

            Assert.That(o[1].LastObjectEndDeltaTime, Is.EqualTo(OsuDifficultyHitObject.MIN_DELTA_TIME));
        }

        // ---- jump distances ----

        [Test]
        public void TestJumpDistance_IsNormalisedByCircleRadius()
        {
            var beatmapObjects = objs(Circle(0, 100, 100), Circle(500, 200, 100));
            var o = DifficultyObjects(beatmapObjects);
            double radius = Playable(objs(Circle(0, 100, 100), Circle(500, 200, 100))).HitObjects.Cast<OsuHitObject>().First().Radius;

            Assert.That(o[0].JumpDistance, Is.EqualTo(100 * OsuDifficultyHitObject.NORMALISED_RADIUS / radius).Within(1e-3));
            Assert.That(o[0].LazyJumpDistance, Is.EqualTo(o[0].JumpDistance).Within(1e-3));
        }

        [Test]
        public void TestJumpDistance_DoublesWithDoubleTheDistance()
        {
            double near = DifficultyObjects(objs(Circle(0, 100, 100), Circle(500, 200, 100)))[0].JumpDistance;
            double far = DifficultyObjects(objs(Circle(0, 100, 100), Circle(500, 300, 100)))[0].JumpDistance;

            Assert.That(far / near, Is.EqualTo(2).Within(1e-6));
        }

        [Test]
        public void TestJumpDistance_ZeroWhenSpinnerInvolved()
        {
            // BaseObject is Spinner || LastObject is Spinner => no distances computed
            var afterSpinner = DifficultyObjects(objs(Spinner(0, 500), Circle(800, 300, 100)));
            var toSpinner = DifficultyObjects(objs(Circle(0, 300, 100), Spinner(800, 500)));

            Assert.That(afterSpinner[0].JumpDistance, Is.EqualTo(0));
            Assert.That(toSpinner[0].JumpDistance, Is.EqualTo(0));
            Assert.That(afterSpinner[0].Angle, Is.Null);
        }

        [Test]
        public void TestMinimumJump_AfterCircle_EqualsLazyJumpAndAdjustedDeltaTime()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(500, 200, 100)))[0];

            Assert.That(o.MinimumJumpDistance, Is.EqualTo(o.LazyJumpDistance));
            Assert.That(o.MinimumJumpTime, Is.EqualTo(o.AdjustedDeltaTime));
        }

        [Test]
        public void TestMinimumJump_AfterSlider_IsNoLongerThanLazyJump()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Slider(500, 100, 100, 200), Circle(1500, 400, 100)));

            Assert.That(o[1].MinimumJumpDistance, Is.GreaterThanOrEqualTo(0));
            Assert.That(o[1].MinimumJumpDistance, Is.LessThanOrEqualTo(o[1].LazyJumpDistance));
            Assert.That(o[1].MinimumJumpTime, Is.InRange((double)OsuDifficultyHitObject.MIN_DELTA_TIME, o[1].AdjustedDeltaTime));
        }

        [Test]
        public void TestMinimumJumpTime_ClampedAfterLongSlider()
        {
            // the slider's travel time swallows the whole gap => Math.Max picks the minimum
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Slider(500, 100, 100, 200), Circle(1000, 400, 100)));

            Assert.That(o[1].MinimumJumpTime, Is.EqualTo(OsuDifficultyHitObject.MIN_DELTA_TIME));
        }

        // ---- slider travel ----

        [Test]
        public void TestCircle_HasNoTravel()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(500, 200, 100)))[0];

            Assert.That(o.TravelDistance, Is.EqualTo(0));
            Assert.That(o.TravelTime, Is.EqualTo(0));
            Assert.That(o.LazyEndPosition, Is.Null);
        }

        [Test]
        public void TestLongSlider_HasLazyTravelAndEndPosition()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Slider(500, 100, 100, 300)))[0];

            Assert.That(o.LazyTravelDistance, Is.GreaterThan(0));
            Assert.That(o.TravelDistance, Is.EqualTo(o.LazyTravelDistance).Within(1e-9));
            Assert.That(o.LazyTravelTime, Is.GreaterThan(0));
            Assert.That(o.TravelTime, Is.GreaterThanOrEqualTo(OsuDifficultyHitObject.MIN_DELTA_TIME));
            Assert.That(o.LazyEndPosition, Is.Not.Null);
            Assert.That(o.LazyEndPosition!.Value.X, Is.GreaterThan(100));
        }

        [Test]
        public void TestTinySlider_CursorNeverLeavesRequiredRadius()
        {
            // movement below the 1.8 radius threshold does not move the cursor or reward distance
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Slider(500, 100, 100, 20)))[0];

            Assert.That(o.LazyTravelDistance, Is.EqualTo(0));
            Assert.That(o.TravelTime, Is.GreaterThanOrEqualTo(OsuDifficultyHitObject.MIN_DELTA_TIME));
        }

        [Test]
        public void TestRepeatSlider_UsesTighterThresholdForRepeatsAndGetsTravelBonus()
        {
            // 60px is below the normal threshold but above the tighter repeat threshold
            double plain = DifficultyObjects(objs(Circle(0, 100, 100), Slider(500, 100, 100, 60)))[0].LazyTravelDistance;
            var repeating = DifficultyObjects(objs(Circle(0, 100, 100), Slider(500, 100, 100, 60, repeats: 1)))[0];

            Assert.That(plain, Is.EqualTo(0));
            Assert.That(repeating.LazyTravelDistance, Is.GreaterThan(0));
        }

        [Test]
        public void TestManyRepeats_TravelDistanceBonusExceedsLazyDistance()
        {
            // Math.Max(1, repeatCount ^ 0.3): > 1 once there are at least 2 repeats; exactly 1 for 0 or 1 repeats
            var none = DifficultyObjects(objs(Circle(0, 100, 100), Slider(500, 100, 100, 300)))[0];
            var many = DifficultyObjects(objs(Circle(0, 100, 100), Slider(500, 100, 100, 300, repeats: 5)))[0];

            Assert.That(none.TravelDistance, Is.EqualTo(none.LazyTravelDistance).Within(1e-9));
            Assert.That(many.TravelDistance, Is.GreaterThan(many.LazyTravelDistance));
        }

        [Test]
        public void TestSlider_TravelTimeIsScaledByClockRate()
        {
            var normal = DifficultyObjects(objs(Circle(0, 100, 100), Slider(500, 100, 100, 300)))[0];
            var doubleTime = DifficultyObjects(objs(Circle(0, 100, 100), Slider(500, 100, 100, 300)), new Mod[] { new OsuModDoubleTime() })[0];

            Assert.That(doubleTime.TravelTime, Is.EqualTo(normal.TravelTime / 1.5).Within(1e-6));
        }

        // ---- angles ----

        [Test]
        public void TestAngle_RequiresThreePreviousObjects()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(500, 200, 100), Circle(1000, 300, 100), Circle(1500, 300, 200)));

            Assert.That(o[0].Angle, Is.Null);
            Assert.That(o[1].Angle, Is.Null);
            Assert.That(o[2].Angle, Is.Not.Null);
        }

        [Test]
        public void TestAngle_RightTurn_IsHalfPi()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(500, 200, 100), Circle(1000, 300, 100), Circle(1500, 300, 200)));

            Assert.That(o[2].Angle!.Value, Is.EqualTo(Math.PI / 2).Within(1e-5));
            Assert.That(o[2].NormalisedVectorAngle!.Value, Is.EqualTo(Math.PI / 2).Within(1e-5));
        }

        [Test]
        public void TestAngle_StraightLine_IsPi()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(500, 200, 100), Circle(1000, 300, 100), Circle(1500, 400, 100)));

            Assert.That(o[2].Angle!.Value, Is.EqualTo(Math.PI).Within(1e-5));
            Assert.That(o[2].NormalisedVectorAngle!.Value, Is.EqualTo(0).Within(1e-5));
        }

        [Test]
        public void TestAngle_BackAndForth_IsZero()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(500, 200, 100), Circle(1000, 300, 100), Circle(1500, 200, 100)));

            Assert.That(o[2].Angle!.Value, Is.EqualTo(0).Within(1e-5));
        }

        [Test]
        public void TestAngle_WithSliderBeforeCurrentObject_IsWithinRange()
        {
            // lastDifficultyObject is a slider with travel => head / second-last nested positions are used
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(500, 150, 100), Slider(1000, 150, 100, 300), Circle(2000, 450, 250)));

            Assert.That(o[2].Angle, Is.Not.Null);
            Assert.That(o[2].Angle!.Value, Is.InRange(0, Math.PI));
        }

        [Test]
        public void TestAngle_NullWhenTheObjectTwoBackIsASpinner()
        {
            // lastLastDifficultyObject.BaseObject is not Spinner FALSE
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Spinner(500, 300), Circle(1200, 300, 100), Circle(1700, 400, 100)));

            Assert.That(o[2].Angle, Is.Null);
        }

        // ---- size / od ----

        [Test]
        public void TestSmallCircleBonus_OnlyForVerySmallCircles()
        {
            var normal = DifficultyObjects(objs(Circle(0, 100, 100), Circle(500, 200, 100)), cs: 4)[0];
            var tiny = DifficultyObjects(objs(Circle(0, 100, 100), Circle(500, 200, 100)), cs: 10)[0];

            Assert.That(normal.SmallCircleBonus, Is.EqualTo(1));
            Assert.That(tiny.SmallCircleBonus, Is.GreaterThan(1));
        }

        [Test]
        public void TestOverallDifficulty_FollowsMapOd()
        {
            var low = DifficultyObjects(objs(Circle(0, 100, 100), Circle(500, 200, 100)), od: 4)[0];
            var high = DifficultyObjects(objs(Circle(0, 100, 100), Circle(500, 200, 100)), od: 9)[0];

            Assert.That(high.OverallDifficulty, Is.GreaterThan(low.OverallDifficulty));
        }

        [Test]
        public void TestPreempt_IsScaledByClockRate()
        {
            var normal = DifficultyObjects(objs(Circle(0, 100, 100), Circle(500, 200, 100)))[0];
            var doubleTime = DifficultyObjects(objs(Circle(0, 100, 100), Circle(500, 200, 100)), new Mod[] { new OsuModDoubleTime() })[0];

            Assert.That(doubleTime.Preempt, Is.EqualTo(normal.Preempt / 1.5).Within(1e-6));
        }

        // ---- OpacityAt ----

        [Test]
        public void TestOpacity_AfterStartTime_IsZero()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(2000, 200, 100)))[0];

            Assert.That(o.OpacityAt(2001, false), Is.EqualTo(0));
            Assert.That(o.OpacityAt(2001, true), Is.EqualTo(0));
        }

        [Test]
        public void TestOpacity_BeforeFadeIn_IsZeroAndFullyFadedInIsOne()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(2000, 200, 100)))[0];
            double fadeInStart = 2000 - o.Preempt;

            Assert.That(o.OpacityAt(fadeInStart - 100, false), Is.EqualTo(0));
            Assert.That(o.OpacityAt(fadeInStart + 400, false), Is.EqualTo(1).Within(1e-9));
            Assert.That(o.OpacityAt(2000, false), Is.EqualTo(1).Within(1e-9));
        }

        [Test]
        public void TestOpacity_DuringFadeIn_IsBetweenZeroAndOne()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(2000, 200, 100)))[0];

            Assert.That(o.OpacityAt(2000 - o.Preempt + 200, false), Is.EqualTo(0.5).Within(1e-9));
        }

        [Test]
        public void TestOpacity_Hidden_FadesBackOutBeforeHitTime()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(2000, 200, 100)))[0];
            double fadeInStart = 2000 - o.Preempt;

            // before the fade-out begins hidden and normal agree, at the hit time hidden is invisible while normal is opaque
            Assert.That(o.OpacityAt(fadeInStart + 200, true), Is.EqualTo(o.OpacityAt(fadeInStart + 200, false)).Within(1e-9));
            Assert.That(o.OpacityAt(2000, true), Is.LessThan(o.OpacityAt(2000, false)));
            Assert.That(o.OpacityAt(2000, true), Is.GreaterThanOrEqualTo(0));
        }

        // ---- CalculateDoubleTapFeasibility ----

        [Test]
        public void TestDoubleTap_NoNextObject_IsZero()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(30, 100, 100)))[0];

            Assert.That(o.CalculateDoubleTapFeasibility(null), Is.EqualTo(0));
        }

        [Test]
        public void TestDoubleTap_FarApartCircles_CannotBeDoubleTapped()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(30, 400, 100), Circle(150, 400, 100)));

            Assert.That(o[0].CalculateDoubleTapFeasibility(o[1]), Is.EqualTo(0).Within(1e-9));
        }

        [Test]
        public void TestDoubleTap_OverlappingCirclesWithSlowingRhythm_IsFeasible()
        {
            // stacked jump, next gap (120ms) much larger than current (30ms) => speedRatio < 1
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(30, 100, 100), Circle(150, 100, 100)));

            double feasibility = o[0].CalculateDoubleTapFeasibility(o[1]);
            Assert.That(feasibility, Is.GreaterThan(0));
            Assert.That(feasibility, Is.LessThanOrEqualTo(1));
        }

        [Test]
        public void TestDoubleTap_SteadyRhythm_IsNotFeasible()
        {
            // identical gaps => speedRatio == 1 => 1 - 1^k == 0
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(30, 100, 100), Circle(60, 100, 100)));

            Assert.That(o[0].CalculateDoubleTapFeasibility(o[1]), Is.EqualTo(0).Within(1e-9));
        }

        [Test]
        public void TestDoubleTap_SlowGaps_LargerThanHitWindow_AreNotFeasible()
        {
            // windowRatio == 1 (gap >= great window) => exponent 0
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(500, 100, 100), Circle(1500, 100, 100)));

            Assert.That(o[0].CalculateDoubleTapFeasibility(o[1]), Is.EqualTo(0).Within(1e-9));
        }
    }
}
