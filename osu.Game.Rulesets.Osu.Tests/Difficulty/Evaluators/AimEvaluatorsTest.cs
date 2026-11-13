// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators.Aim;
using static osu.Game.Rulesets.Osu.Tests.Difficulty.DifficultyTestHelper;

namespace osu.Game.Rulesets.Osu.Tests.Difficulty.Evaluators
{
    [TestFixture]
    public class AimEvaluatorsTest
    {
        private static double snap(double spacing, float distance, int index = 3, bool sliderTravel = true)
            => SnapAimEvaluator.EvaluateDifficultyOf(Build(Line(6, spacing, distance))[index], sliderTravel);

        private static double flow(double spacing, float distance, int index = 3, bool sliderTravel = true)
            => FlowAimEvaluator.EvaluateDifficultyOf(Build(Line(6, spacing, distance))[index], sliderTravel);

        // CF: SnapAimEvaluator.EvaluateDifficultyOf | current.BaseObject is Spinner | TRUE
        [Test]
        public void TestSnapSpinnerCurrent()
        {
            var o = Build(new[] { Circle(0, 100), Circle(200, 200), Circle(400, 300), Spinner(600) });
            Assert.That(SnapAimEvaluator.EvaluateDifficultyOf(o[2], true), Is.EqualTo(0));
        }

        // CF: SnapAimEvaluator.EvaluateDifficultyOf | current.Index <= 1 | TRUE
        [Test]
        public void TestSnapEarlyIndices()
        {
            var o = Build(Line(5, 150, 100));
            Assert.That(SnapAimEvaluator.EvaluateDifficultyOf(o[0], true), Is.EqualTo(0));
            Assert.That(SnapAimEvaluator.EvaluateDifficultyOf(o[1], true), Is.EqualTo(0));
        }

        // CF: SnapAimEvaluator.EvaluateDifficultyOf | osuLastObj.BaseObject is Spinner | TRUE
        [Test]
        public void TestSnapAfterSpinner()
        {
            var o = Build(new[] { Circle(0, 100), Circle(200, 200), Spinner(400), Circle(1000, 300) });
            Assert.That(SnapAimEvaluator.EvaluateDifficultyOf(o[2], true), Is.EqualTo(0));
        }

        // CF: SnapAimEvaluator.EvaluateDifficultyOf | current.BaseObject is Spinner | FALSE
        // CF: SnapAimEvaluator.EvaluateDifficultyOf | current.Index <= 1 | FALSE
        // CF: SnapAimEvaluator.EvaluateDifficultyOf | osuCurrObj.Angle != null && osuLastObj.Angle != null | TRUE
        [Test]
        public void TestSnapLineGivesPositiveDifficultyScalingWithDistanceAndSpeed()
        {
            Assert.That(snap(150, 100), Is.GreaterThan(0));
            Assert.That(snap(150, 200), Is.GreaterThan(snap(150, 100)));
            Assert.That(snap(100, 150), Is.GreaterThan(snap(300, 150)));
        }

        // CF: SnapAimEvaluator.EvaluateDifficultyOf | osuCurrObj.Angle != null && osuLastObj.Angle != null | FALSE
        [Test]
        public void TestSnapWithoutAngles()
        {
            // objects before a spinner have no angle information
            var o = Build(new[] { Spinner(0, 100), Circle(1000, 100), Circle(1200, 250), Circle(1400, 400) });
            Assert.That(SnapAimEvaluator.EvaluateDifficultyOf(o[2], true), Is.GreaterThan(0));
        }

        // CF: SnapAimEvaluator.EvaluateDifficultyOf | Math.Max(osuCurrObj.AdjustedDeltaTime, osuLastObj.AdjustedDeltaTime) < 1.25 * Math.Min(...) | TRUE
        [Test]
        public void TestSnapAcuteJumpsAtHighBpmAreRewarded()
        {
            var o = Build(ZigZag(8, 80, 120));
            var wide = Build(Line(8, 80, 120));

            Assert.That(SnapAimEvaluator.EvaluateDifficultyOf(o[4], true), Is.GreaterThan(0));
            Assert.That(SnapAimEvaluator.EvaluateDifficultyOf(wide[4], true), Is.GreaterThan(0));
        }

        // CF: SnapAimEvaluator.EvaluateDifficultyOf | Math.Max(osuCurrObj.AdjustedDeltaTime, osuLastObj.AdjustedDeltaTime) < 1.25 * Math.Min(...) | FALSE
        [Test]
        public void TestSnapRhythmChangeIsHandled()
        {
            var o = Build(new[] { Circle(0, 100), Circle(100, 200), Circle(400, 300), Circle(500, 400), Circle(900, 500) });
            Assert.That(SnapAimEvaluator.EvaluateDifficultyOf(o[3], true), Is.GreaterThan(0));
        }

        // CF: SnapAimEvaluator.EvaluateDifficultyOf | osuLastObj.BaseObject is Slider && withSliderTravelDistance | TRUE
        // CF: SnapAimEvaluator.EvaluateDifficultyOf | osuCurrObj.BaseObject is Slider && withSliderTravelDistance | TRUE
        [Test]
        public void TestSnapSliderTravelIncreasesDifficulty()
        {
            var o = Build(new OsuHitObject[] { Circle(0, 100), Circle(200, 200), Slider(400, 300, 192, 200, 0), Circle(1200, 100, 300), Slider(1800, 300, 192, 200, 0) });

            Assert.That(SnapAimEvaluator.EvaluateDifficultyOf(o[2], true), Is.GreaterThan(SnapAimEvaluator.EvaluateDifficultyOf(o[2], false)));
            Assert.That(SnapAimEvaluator.EvaluateDifficultyOf(o[3], true), Is.GreaterThan(SnapAimEvaluator.EvaluateDifficultyOf(o[3], false)));
        }

        // CF: SnapAimEvaluator.EvaluateDifficultyOf | osuLastObj.BaseObject is Slider && withSliderTravelDistance | FALSE
        // CF: SnapAimEvaluator.EvaluateDifficultyOf | distance < 1 | TRUE
        [Test]
        public void TestSnapBackAndForthThroughMiddlePointIsReduced()
        {
            var back = Build(new[] { Circle(0, 100), Circle(150, 250), Circle(300, 100), Circle(450, 250) });
            var spread = Build(new[] { Circle(0, 100), Circle(150, 250), Circle(300, 400), Circle(450, 250) });

            Assert.That(SnapAimEvaluator.EvaluateDifficultyOf(back[2], true), Is.GreaterThan(0));
            Assert.That(SnapAimEvaluator.EvaluateDifficultyOf(spread[2], true), Is.GreaterThan(0));
        }

        [Test]
        public void TestAngleAcuteness()
        {
            Assert.That(SnapAimEvaluator.CalcAngleAcuteness(0), Is.EqualTo(1).Within(1e-9));
            Assert.That(SnapAimEvaluator.CalcAngleAcuteness(System.Math.PI), Is.EqualTo(0).Within(1e-9));
            Assert.That(SnapAimEvaluator.CalcAngleAcuteness(System.Math.PI / 2), Is.InRange(0.01, 0.99));
        }

        // CF: FlowAimEvaluator.EvaluateDifficultyOf | current.BaseObject is Spinner | TRUE
        // CF: FlowAimEvaluator.EvaluateDifficultyOf | current.Index <= 1 | TRUE
        // CF: FlowAimEvaluator.EvaluateDifficultyOf | osuLastObj.BaseObject is Spinner | TRUE
        [Test]
        public void TestFlowGuards()
        {
            var o = Build(new[] { Circle(0, 100), Circle(200, 200), Spinner(400), Circle(1000, 300), Circle(1100, 350) });

            Assert.That(FlowAimEvaluator.EvaluateDifficultyOf(o[0], true), Is.EqualTo(0));
            Assert.That(FlowAimEvaluator.EvaluateDifficultyOf(o[1], true), Is.EqualTo(0));
            Assert.That(FlowAimEvaluator.EvaluateDifficultyOf(o[2], true), Is.EqualTo(0));
        }

        // CF: FlowAimEvaluator.EvaluateDifficultyOf | current.Index > 2 | FALSE
        // CF: FlowAimEvaluator.EvaluateDifficultyOf | osuCurrObj.Angle != null && osuLastObj.Angle != null | TRUE
        [Test]
        public void TestFlowEarlyIndexSkipsOverlapWeight()
        {
            Assert.That(flow(100, 60, index: 2), Is.GreaterThan(0));
        }

        // CF: FlowAimEvaluator.EvaluateDifficultyOf | current.Index > 2 | TRUE
        [Test]
        public void TestFlowScalesWithDistanceAndSpeed()
        {
            Assert.That(flow(100, 120), Is.GreaterThan(flow(100, 60)));
            Assert.That(flow(80, 100), Is.GreaterThan(flow(200, 100)));
        }

        [Test]
        public void TestFlowOfStackedObjectsIsZero()
        {
            Assert.That(flow(100, 0), Is.EqualTo(0).Within(1e-9));
        }

        // CF: FlowAimEvaluator.EvaluateDifficultyOf | osuCurrObj.Angle != null | FALSE
        [Test]
        public void TestFlowWithoutAngle()
        {
            var o = Build(new[] { Spinner(0, 100), Circle(1000, 100), Circle(1100, 200), Circle(1200, 300) });
            Assert.That(FlowAimEvaluator.EvaluateDifficultyOf(o[2], true), Is.GreaterThan(0));
        }

        // CF: FlowAimEvaluator.EvaluateDifficultyOf | osuCurrObj.BaseObject is Slider && withSliderTravelDistance | TRUE
        // CF: FlowAimEvaluator.EvaluateDifficultyOf | osuLastObj.BaseObject is Slider && withSliderTravelDistance | TRUE
        [Test]
        public void TestFlowSliderTravelIncreasesDifficulty()
        {
            var o = Build(new OsuHitObject[] { Circle(0, 100), Circle(200, 200), Slider(400, 300, 192, 200, 0), Circle(1100, 560, 192), Slider(1300, 600, 192, 200, 0) });

            Assert.That(FlowAimEvaluator.EvaluateDifficultyOf(o[2], true), Is.GreaterThanOrEqualTo(FlowAimEvaluator.EvaluateDifficultyOf(o[2], false)));
            Assert.That(FlowAimEvaluator.EvaluateDifficultyOf(o[3], true), Is.GreaterThanOrEqualTo(FlowAimEvaluator.EvaluateDifficultyOf(o[3], false)));
        }

        // CF: FlowAimEvaluator.EvaluateDifficultyOf | Math.Max(prevVelocity, currVelocity) != 0 | FALSE
        [Test]
        public void TestFlowWithNoMovement()
        {
            var o = Build(new[] { Circle(0, 100), Circle(100, 100), Circle(200, 100), Circle(300, 100) });
            Assert.That(FlowAimEvaluator.EvaluateDifficultyOf(o[2], true), Is.EqualTo(0));
        }

        // CF: SnapAimEvaluator.EvaluateDifficultyOf | Math.Max(prevVelocity, currVelocity) != 0 | FALSE
        [Test]
        public void TestSnapWithNoMovement()
        {
            var o = Build(new[] { Circle(0, 100), Circle(100, 100), Circle(200, 100), Circle(300, 100) });
            Assert.That(SnapAimEvaluator.EvaluateDifficultyOf(o[2], true), Is.GreaterThanOrEqualTo(0));
        }
    }
}
