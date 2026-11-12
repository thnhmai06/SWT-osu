// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators.Aim;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators.Speed;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using static osu.Game.Rulesets.Osu.Tests.Difficulty.DifficultyTestHelpers;

namespace osu.Game.Rulesets.Osu.Tests.Difficulty
{
    [TestFixture]
    public class AimAndSpeedEvaluatorsTest
    {
        private static OsuHitObject[] objs(params OsuHitObject[] objects) => objects;

        /// <summary>Four circles on a zig-zag: gives difficulty objects with indices 0..2 (angles available from index 2).</summary>
        private static OsuHitObject[] zigzag(double delta = 200, float jump = 150)
            => objs(Circle(0, 100, 100), Circle(delta, 100 + jump, 100), Circle(delta * 2, 100, 100 + jump), Circle(delta * 3, 100 + jump, 100 + jump), Circle(delta * 4, 100, 100));

        // ================= SpeedEvaluator =================

        [Test]
        public void TestSpeed_Spinner_IsZero()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Spinner(500, 500), Circle(1500, 100, 100)));

            Assert.That(SpeedEvaluator.EvaluateDifficultyOf(o[0]), Is.EqualTo(0));
        }

        [Test]
        public void TestSpeed_FasterRhythm_IsHarder()
        {
            double fast = SpeedEvaluator.EvaluateDifficultyOf(DifficultyObjects(objs(Circle(0, 100, 100), Circle(100, 200, 100)))[0]);
            double slow = SpeedEvaluator.EvaluateDifficultyOf(DifficultyObjects(objs(Circle(0, 100, 100), Circle(200, 200, 100)))[0]);

            Assert.That(fast, Is.GreaterThan(slow));
            Assert.That(slow, Is.GreaterThan(0));
        }

        [Test]
        public void TestSpeed_BeyondBpmThreshold_GetsExtraBonusOverPureInverseTime()
        {
            // above 200 BPM 1/4 (75ms) the bonus applies, so difficulty grows faster than 1 / strainTime
            double at60 = SpeedEvaluator.EvaluateDifficultyOf(DifficultyObjects(objs(Circle(0, 100, 100), Circle(60, 200, 100)))[0]);
            double at90 = SpeedEvaluator.EvaluateDifficultyOf(DifficultyObjects(objs(Circle(0, 100, 100), Circle(90, 200, 100)))[0]);

            Assert.That(at60 / at90, Is.GreaterThan(90.0 / 60));
        }

        [Test]
        public void TestSpeed_PossibleDoubleTap_ReducesDifficulty()
        {
            // stacked notes followed by a slower gap can be double-tapped (feasibility > 0); far apart notes cannot
            double stacked = SpeedEvaluator.EvaluateDifficultyOf(DifficultyObjects(objs(Circle(0, 100, 100), Circle(30, 100, 100), Circle(150, 100, 100)))[0]);
            double apart = SpeedEvaluator.EvaluateDifficultyOf(DifficultyObjects(objs(Circle(0, 100, 100), Circle(30, 400, 100), Circle(150, 400, 100)))[0]);

            Assert.That(stacked, Is.LessThan(apart));
        }

        [Test]
        public void TestSpeed_LastObject_HasNoDoubleTapReduction()
        {
            // Next() is null for the final object => feasibility 0
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(30, 100, 100)));

            Assert.That(SpeedEvaluator.EvaluateDifficultyOf(o[0]), Is.GreaterThan(0));
        }

        // ================= AgilityEvaluator =================

        [Test]
        public void TestAgility_Spinner_IsZero()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Spinner(500, 500), Circle(1500, 100, 100)));

            Assert.That(AgilityEvaluator.EvaluateDifficultyOf(o[0]), Is.EqualTo(0));
        }

        [Test]
        public void TestAgility_StackedNotes_AreZero()
        {
            // first difficulty object: Index == 0, no previous travel; zero jump => zero difficulty
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(500, 100, 100)));

            Assert.That(AgilityEvaluator.EvaluateDifficultyOf(o[0]), Is.EqualTo(0).Within(1e-9));
        }

        [Test]
        public void TestAgility_GrowsWithDistanceThenSaturatesAtCap()
        {
            double Eval(float dx) => AgilityEvaluator.EvaluateDifficultyOf(DifficultyObjects(objs(Circle(0, 100, 100), Circle(300, 100 + dx, 100)))[0]);

            Assert.That(Eval(40), Is.LessThan(Eval(80)));
            // 1.2 diameters ~ 120 normalised units ~ 88px: anything farther is capped
            Assert.That(Eval(300), Is.EqualTo(Eval(400)).Within(1e-9));
        }

        [Test]
        public void TestAgility_FasterIsHarder()
        {
            double fast = AgilityEvaluator.EvaluateDifficultyOf(DifficultyObjects(objs(Circle(0, 100, 100), Circle(100, 300, 100)))[0]);
            double slow = AgilityEvaluator.EvaluateDifficultyOf(DifficultyObjects(objs(Circle(0, 100, 100), Circle(200, 300, 100)))[0]);

            Assert.That(fast, Is.GreaterThan(slow));
        }

        [Test]
        public void TestAgility_PreviousSliderTravelAddsToJump()
        {
            // the follow-up circle sits close to where a long slider ends vs close to a plain circle: travel makes the slider case harder
            var afterSlider = DifficultyObjects(objs(Circle(0, 100, 100), Slider(500, 100, 100, 300), Circle(2000, 420, 100)));
            var afterCircle = DifficultyObjects(objs(Circle(0, 100, 100), Circle(500, 400, 100), Circle(2000, 420, 100)));

            Assert.That(AgilityEvaluator.EvaluateDifficultyOf(afterSlider[1]), Is.GreaterThan(AgilityEvaluator.EvaluateDifficultyOf(afterCircle[1])));
        }

        [Test]
        public void TestAgility_SmallCirclesAreHarder()
        {
            double big = AgilityEvaluator.EvaluateDifficultyOf(DifficultyObjects(objs(Circle(0, 100, 100), Circle(300, 300, 100)), cs: 4)[0]);
            double small = AgilityEvaluator.EvaluateDifficultyOf(DifficultyObjects(objs(Circle(0, 100, 100), Circle(300, 300, 100)), cs: 10)[0]);

            Assert.That(small, Is.GreaterThan(big));
        }

        // ================= Snap / Flow shared guards =================

        [Test]
        public void TestSnapAndFlow_GuardClauses_ReturnZero()
        {
            // Index <= 1
            var plain = DifficultyObjects(zigzag());
            foreach (bool slider in new[] { false, true })
            {
                Assert.That(SnapAimEvaluator.EvaluateDifficultyOf(plain[0], slider), Is.EqualTo(0));
                Assert.That(SnapAimEvaluator.EvaluateDifficultyOf(plain[1], slider), Is.EqualTo(0));
                Assert.That(FlowAimEvaluator.EvaluateDifficultyOf(plain[0], slider), Is.EqualTo(0));
                Assert.That(FlowAimEvaluator.EvaluateDifficultyOf(plain[1], slider), Is.EqualTo(0));
            }

            // current object is a spinner
            var spinnerCurrent = DifficultyObjects(objs(Circle(0, 100, 100), Circle(300, 200, 100), Circle(600, 300, 100), Spinner(900, 300)));
            Assert.That(SnapAimEvaluator.EvaluateDifficultyOf(spinnerCurrent[2], false), Is.EqualTo(0));
            Assert.That(FlowAimEvaluator.EvaluateDifficultyOf(spinnerCurrent[2], false), Is.EqualTo(0));

            // previous object is a spinner
            var spinnerPrevious = DifficultyObjects(objs(Circle(0, 100, 100), Circle(300, 200, 100), Spinner(600, 200), Circle(1300, 300, 100)));
            Assert.That(SnapAimEvaluator.EvaluateDifficultyOf(spinnerPrevious[2], false), Is.EqualTo(0));
            Assert.That(FlowAimEvaluator.EvaluateDifficultyOf(spinnerPrevious[2], false), Is.EqualTo(0));
        }

        // ================= SnapAimEvaluator =================

        [Test]
        public void TestSnap_AngleAcuteness_IsMonotonicBetweenBounds()
        {
            Assert.That(SnapAimEvaluator.CalcAngleAcuteness(0), Is.EqualTo(1).Within(1e-9));
            Assert.That(SnapAimEvaluator.CalcAngleAcuteness(Math.PI), Is.EqualTo(0).Within(1e-9));
            Assert.That(SnapAimEvaluator.CalcAngleAcuteness(Math.PI / 2), Is.InRange(0.01, 0.99));
            Assert.That(SnapAimEvaluator.CalcAngleAcuteness(Math.PI / 3), Is.GreaterThan(SnapAimEvaluator.CalcAngleAcuteness(Math.PI / 2)));
        }

        [Test]
        public void TestSnap_JumpsAreHarderThanStacks()
        {
            double stacked = SnapAimEvaluator.EvaluateDifficultyOf(DifficultyObjects(objs(Circle(0, 100, 100), Circle(450, 100, 100), Circle(900, 100, 100), Circle(1350, 100, 100), Circle(1800, 100, 100)))[3], false);
            double jumps = SnapAimEvaluator.EvaluateDifficultyOf(DifficultyObjects(zigzag(delta: 450))[3], false);

            Assert.That(jumps, Is.GreaterThan(stacked));
        }

        [Test]
        public void TestSnap_AngleNotYetAvailable_AtIndexTwo_StillPositive()
        {
            // at Index 2 the previous object has no Angle => `Angle != null && lastObj.Angle != null` FALSE
            var o = DifficultyObjects(zigzag());

            Assert.That(o[1].Angle, Is.Null);
            Assert.That(SnapAimEvaluator.EvaluateDifficultyOf(o[2], false), Is.GreaterThan(0));
        }

        [Test]
        public void TestSnap_FasterJumps_AreHarder()
        {
            double slow = SnapAimEvaluator.EvaluateDifficultyOf(DifficultyObjects(zigzag(delta: 300))[3], false);
            double fast = SnapAimEvaluator.EvaluateDifficultyOf(DifficultyObjects(zigzag(delta: 150))[3], false);

            Assert.That(fast, Is.GreaterThan(slow));
        }

        [Test]
        public void TestSnap_SmallCircles_AreHarder()
        {
            double normal = SnapAimEvaluator.EvaluateDifficultyOf(DifficultyObjects(zigzag(), cs: 4)[3], false);
            double small = SnapAimEvaluator.EvaluateDifficultyOf(DifficultyObjects(zigzag(), cs: 10)[3], false);

            Assert.That(small, Is.GreaterThan(normal));
        }

        [Test]
        public void TestSnap_SliderTravelIsRewardedWhenRequested()
        {
            // the follow-up circle sits next to the slider head but far from its lazy end:
            // counting travel + lazy jump (true) is much larger than the short head-to-circle jump (false)
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(300, 250, 100), Slider(600, 250, 100, 300), Circle(1800, 270, 120)));

            Assert.That(SnapAimEvaluator.EvaluateDifficultyOf(o[2], true), Is.GreaterThan(SnapAimEvaluator.EvaluateDifficultyOf(o[2], false)));
        }

        [Test]
        public void TestSnap_CurrentSlider_AddsSliderBonusOnlyWhenRequested()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(300, 250, 100), Circle(600, 100, 100), Slider(900, 250, 100, 300)));

            Assert.That(SnapAimEvaluator.EvaluateDifficultyOf(o[2], true), Is.GreaterThan(SnapAimEvaluator.EvaluateDifficultyOf(o[2], false)));
        }

        // ================= FlowAimEvaluator =================

        [Test]
        public void TestFlow_StackedNotes_AreZero()
        {
            // Smootherstep(currDistance = 0) == 0
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(450, 100, 100), Circle(900, 100, 100), Circle(1350, 100, 100)));

            Assert.That(FlowAimEvaluator.EvaluateDifficultyOf(o[2], false), Is.EqualTo(0).Within(1e-9));
        }

        [Test]
        public void TestFlow_FastStreamIsHarderThanSlowStream()
        {
            double slow = FlowAimEvaluator.EvaluateDifficultyOf(DifficultyObjects(zigzag(delta: 300, jump: 120))[2], false);
            double fast = FlowAimEvaluator.EvaluateDifficultyOf(DifficultyObjects(zigzag(delta: 120, jump: 120))[2], false);

            Assert.That(fast, Is.GreaterThan(slow));
            Assert.That(slow, Is.GreaterThan(0));
        }

        [Test]
        public void TestFlow_SharpTurnsAreHarderThanStraightLines()
        {
            // index 2 has weight 1 for the overlap factor; back-and-forth has an acute angle, a straight line has none
            double straight = FlowAimEvaluator.EvaluateDifficultyOf(DifficultyObjects(objs(Circle(0, 100, 100), Circle(150, 200, 100), Circle(300, 300, 100), Circle(450, 400, 100)))[2], false);
            double backAndForth = FlowAimEvaluator.EvaluateDifficultyOf(DifficultyObjects(objs(Circle(0, 100, 100), Circle(150, 200, 100), Circle(300, 300, 100), Circle(450, 200, 100)))[2], false);

            Assert.That(backAndForth, Is.GreaterThan(straight));
        }

        [Test]
        public void TestFlow_OverlapFactorBranch_IsEvaluatedFromIndexThree()
        {
            // Index > 2: heavily overlapped notes shrink the angle / velocity-change contributions
            var o = DifficultyObjects(zigzag(delta: 150, jump: 40));

            Assert.That(FlowAimEvaluator.EvaluateDifficultyOf(o[3], false), Is.GreaterThanOrEqualTo(0));
            Assert.That(double.IsNaN(FlowAimEvaluator.EvaluateDifficultyOf(o[3], false)), Is.False);
        }

        [Test]
        public void TestFlow_CurrentSlider_AddsTravelOnlyWhenRequested()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(300, 250, 100), Circle(600, 100, 100), Slider(900, 250, 100, 300)));

            Assert.That(FlowAimEvaluator.EvaluateDifficultyOf(o[2], true), Is.GreaterThan(FlowAimEvaluator.EvaluateDifficultyOf(o[2], false)));
        }

        [Test]
        public void TestFlow_PreviousSlider_UsesTravelPlusJumpWhenRequested()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(300, 250, 100), Slider(600, 250, 100, 300), Circle(1800, 270, 120)));

            Assert.That(FlowAimEvaluator.EvaluateDifficultyOf(o[2], true), Is.GreaterThan(FlowAimEvaluator.EvaluateDifficultyOf(o[2], false)));
        }

        [Test]
        public void TestFlow_IdenticalVelocity_HasNoVelocityChangeBuff()
        {
            // Math.Max(prevVelocity, currVelocity) == 0 => skipped entirely: two stacked hops after a stack
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(450, 100, 100), Circle(900, 100, 100), Circle(1350, 100, 100)));

            Assert.That(FlowAimEvaluator.EvaluateDifficultyOf(o[2], false), Is.EqualTo(0).Within(1e-9));
        }

        // ================= FlashlightEvaluator =================

        private static readonly Mod[] no_mods = new Mod[0];

        [Test]
        public void TestFlashlight_Spinner_IsZero()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Spinner(500, 500)));

            Assert.That(FlashlightEvaluator.EvaluateDifficultyOf(o[0], no_mods), Is.EqualTo(0));
        }

        [Test]
        public void TestFlashlight_FirstObject_HasNoHistory()
        {
            // Math.Min(Index, 10) == 0 iterations
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Circle(300, 400, 100)));

            Assert.That(FlashlightEvaluator.EvaluateDifficultyOf(o[0], no_mods), Is.EqualTo(0));
        }

        [Test]
        public void TestFlashlight_LongerJumps_AreHarder()
        {
            double near = FlashlightEvaluator.EvaluateDifficultyOf(DifficultyObjects(zigzag(jump: 60))[3], no_mods);
            double far = FlashlightEvaluator.EvaluateDifficultyOf(DifficultyObjects(zigzag(jump: 200))[3], no_mods);

            Assert.That(far, Is.GreaterThan(near));
        }

        [Test]
        public void TestFlashlight_SmallDistanceNerf_OnlyAppliesToTheImmediateJump()
        {
            // i == 0: smallDistNerf = Min(1, distance / 75). A tiny hop after a big history is nerfed compared with a big hop
            var tiny = DifficultyObjects(objs(Circle(0, 100, 100), Circle(200, 400, 100), Circle(400, 100, 100), Circle(600, 120, 100)));
            var big = DifficultyObjects(objs(Circle(0, 100, 100), Circle(200, 400, 100), Circle(400, 100, 100), Circle(600, 400, 100)));

            Assert.That(FlashlightEvaluator.EvaluateDifficultyOf(tiny[2], no_mods), Is.LessThan(FlashlightEvaluator.EvaluateDifficultyOf(big[2], no_mods)));
        }

        [Test]
        public void TestFlashlight_Hidden_IncreasesDifficulty()
        {
            var o = DifficultyObjects(zigzag(), new Mod[] { new OsuModHidden() });

            double plain = FlashlightEvaluator.EvaluateDifficultyOf(o[3], no_mods);
            double hidden = FlashlightEvaluator.EvaluateDifficultyOf(o[3], new Mod[] { new OsuModHidden() });

            Assert.That(hidden, Is.GreaterThan(plain));
        }

        [Test]
        public void TestFlashlight_HiddenWithOnlyApproachCircleFade_StillGetsFlatBonusButNoOpacityBonus()
        {
            var o = DifficultyObjects(zigzag());

            double full = FlashlightEvaluator.EvaluateDifficultyOf(o[3], new Mod[] { new OsuModHidden() });
            double approachOnly = FlashlightEvaluator.EvaluateDifficultyOf(o[3], new Mod[] { new OsuModHidden { OnlyFadeApproachCircles = { Value = true } } });

            Assert.That(approachOnly, Is.GreaterThan(FlashlightEvaluator.EvaluateDifficultyOf(o[3], no_mods)));
            Assert.That(approachOnly, Is.LessThanOrEqualTo(full));
        }

        [Test]
        public void TestFlashlight_FastSlider_AddsSliderBonus()
        {
            // slider multiplier 10 makes the slider fast enough to exceed the minimum velocity
            var withSlider = DifficultyObjects(objs(Circle(0, 100, 100), Circle(1500, 300, 100), Slider(3000, 300, 100, 300)), sliderMultiplier: 10);
            var withCircle = DifficultyObjects(objs(Circle(0, 100, 100), Circle(1500, 300, 100), Circle(3000, 300, 100)), sliderMultiplier: 10);

            Assert.That(FlashlightEvaluator.EvaluateDifficultyOf(withSlider[1], no_mods), Is.GreaterThan(FlashlightEvaluator.EvaluateDifficultyOf(withCircle[1], no_mods)));
        }

        [Test]
        public void TestFlashlight_SlowSlider_AddsNoSliderBonus()
        {
            // Math.Max(0, velocity - 0.5) == 0 for the default slow slider
            var withSlider = DifficultyObjects(objs(Circle(0, 100, 100), Circle(1500, 300, 100), Slider(3000, 300, 100, 300)));
            var withCircle = DifficultyObjects(objs(Circle(0, 100, 100), Circle(1500, 300, 100), Circle(3000, 300, 100)));

            Assert.That(FlashlightEvaluator.EvaluateDifficultyOf(withSlider[1], no_mods), Is.EqualTo(FlashlightEvaluator.EvaluateDifficultyOf(withCircle[1], no_mods)).Within(1e-9));
        }

        [Test]
        public void TestFlashlight_SpinnerInHistory_IsSkipped()
        {
            var o = DifficultyObjects(objs(Circle(0, 100, 100), Spinner(300, 300), Circle(1000, 400, 100), Circle(1300, 100, 100)));

            Assert.That(double.IsNaN(FlashlightEvaluator.EvaluateDifficultyOf(o[2], no_mods)), Is.False);
            Assert.That(FlashlightEvaluator.EvaluateDifficultyOf(o[2], no_mods), Is.GreaterThanOrEqualTo(0));
        }
    }
}
