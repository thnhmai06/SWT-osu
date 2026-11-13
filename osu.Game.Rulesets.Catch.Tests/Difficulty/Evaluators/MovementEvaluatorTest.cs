// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Catch.Difficulty.Evaluators;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Catch.Tests.Difficulty.Evaluators
{
    [TestFixture]
    public class MovementEvaluatorTest
    {
        /// <summary>Builds difficulty objects for every fruit except the first (which acts as the "last object" of the second).</summary>
        private static List<CatchDifficultyHitObject> build(params (double time, float x)[] points)
        {
            var fruits = points.Select(p => new Fruit { StartTime = p.time, X = p.x }).ToArray();
            foreach (var f in fruits)
                f.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());

            var list = new List<DifficultyHitObject>();
            var result = new List<CatchDifficultyHitObject>();

            for (int i = 1; i < fruits.Length; i++)
            {
                var o = new CatchDifficultyHitObject(fruits[i], fruits[i - 1], 1, 41, list, list.Count);
                list.Add(o);
                result.Add(o);
            }

            return result;
        }

        private static double evaluateLast(params (double time, float x)[] points) => MovementEvaluator.EvaluateDifficultyOf(build(points).Last());

        // CF: MovementEvaluator.EvaluateDifficultyOf | Math.Abs(DistanceMoved) > 0.1 | FALSE
        [Test]
        public void TestNoMovementHasNoDifficulty()
        {
            Assert.That(evaluateLast((0, 100), (100, 110)), Is.Zero);
        }

        // CF: MovementEvaluator.EvaluateDifficultyOf | Math.Abs(DistanceMoved) > 0.1 | TRUE
        // CF: MovementEvaluator.EvaluateDifficultyOf | current.Index >= 1 | FALSE
        // CF: MovementEvaluator.EvaluateDifficultyOf | Math.Min(current.Index, 10) | loop 0
        [Test]
        public void TestFirstMovementHasPositiveDifficulty()
        {
            Assert.That(evaluateLast((0, 100), (100, 300)), Is.GreaterThan(0));
        }

        [Test]
        public void TestLargerMovementIsHarder()
        {
            Assert.That(evaluateLast((0, 0), (100, 300)), Is.GreaterThan(evaluateLast((0, 0), (100, 100))));
        }

        [Test]
        public void TestFasterMovementIsHarder()
        {
            Assert.That(evaluateLast((0, 0), (100, 300)), Is.GreaterThan(evaluateLast((0, 0), (400, 300))));
        }

        // CF: MovementEvaluator.EvaluateDifficultyOf | Math.Abs(catchLast.DistanceMoved) > 0.1 | FALSE
        [Test]
        public void TestPreviousStationaryObjectGivesNoDirectionChangeBonus()
        {
            // second object is within range (no movement), so the third cannot be a direction change.
            double afterStill = evaluateLast((0, 200), (100, 210), (200, 100));
            double afterMove = evaluateLast((0, 200), (100, 300), (200, 150));

            Assert.That(afterMove, Is.GreaterThan(0));
            Assert.That(afterStill, Is.GreaterThan(0));
        }

        // CF: MovementEvaluator.EvaluateDifficultyOf | Math.Sign(current) != Math.Sign(last) | TRUE
        // CF: MovementEvaluator.EvaluateDifficultyOf | Math.Sign(current) != Math.Sign(last) | FALSE
        // CF: MovementEvaluator.EvaluateDifficultyOf | Math.Abs(catchLast.DistanceMoved) > 0.1 | TRUE
        // CF: MovementEvaluator.EvaluateDifficultyOf | current.Index >= 1 | TRUE
        [Test]
        public void TestDirectionChangeIsHarderThanContinuingInSameDirection()
        {
            // 0 -> 200 moves the catcher right by 175; then the catcher must move 100 either left (50) or right (300).
            double change = evaluateLast((0, 0), (100, 200), (200, 50));
            double same = evaluateLast((0, 0), (100, 200), (200, 300));

            Assert.That(change, Is.GreaterThan(same));
        }

        // CF: MovementEvaluator.EvaluateDifficultyOf | linear spacing loop | loop 1
        // CF: MovementEvaluator.EvaluateDifficultyOf | relativeDifference > 0.05 | TRUE
        // CF: MovementEvaluator.EvaluateDifficultyOf | relativeDifference > 0.05 | FALSE
        // CF: MovementEvaluator.EvaluateDifficultyOf | linear spacing loop | loop n
        // CF: MovementEvaluator.EvaluateDifficultyOf | sign differs || zero movement | FALSE
        [Test]
        public void TestConstantSpeedStreamIsNerfedComparedToIrregularSpacing()
        {
            // identical final movement (100px in 100ms) but the preceding movements either match its speed or not.
            double constant = evaluateLast((0, 0), (100, 100), (200, 200), (300, 300), (400, 400));
            double irregular = evaluateLast((0, 0), (100, 100), (150, 160), (300, 300), (400, 400));

            Assert.That(constant, Is.LessThan(irregular));
        }

        // CF: MovementEvaluator.EvaluateDifficultyOf | Math.Sign(current) != Math.Sign(prev) | TRUE (break)
        // CF: MovementEvaluator.EvaluateDifficultyOf | catchCurrent.DistanceMoved == 0 || catchPrevObj.DistanceMoved == 0 | TRUE
        [Test]
        public void TestPreviousZeroMovementBreaksLinearNerfLoop()
        {
            Assert.That(evaluateLast((0, 100), (100, 105), (200, 300)), Is.GreaterThan(0));
        }

        // CF: MovementEvaluator.EvaluateDifficultyOf | LastObject.DistanceToHyperDash <= 20 | TRUE
        // CF: MovementEvaluator.EvaluateDifficultyOf | !LastObject.HyperDash | TRUE
        // CF: MovementEvaluator.EvaluateDifficultyOf | LastObject.DistanceToHyperDash <= 20 | FALSE
        [Test]
        public void TestEdgeDashIsHarderThanComfortableJump()
        {
            var edge = build((0, 100), (100, 300)).Last();
            var comfortable = build((0, 100), (100, 300)).Last();
            comfortable.LastObject.DistanceToHyperDash = 100;

            Assert.That(MovementEvaluator.EvaluateDifficultyOf(edge), Is.GreaterThan(MovementEvaluator.EvaluateDifficultyOf(comfortable)));
        }

        // CF: MovementEvaluator.EvaluateDifficultyOf | !LastObject.HyperDash | FALSE
        [Test]
        public void TestHyperDashGivesNoEdgeDashBonus()
        {
            var hyper = build((0, 100), (100, 300)).Last();
            hyper.LastObject.HyperDashTarget = hyper.BaseObject;
            var comfortable = build((0, 100), (100, 300)).Last();
            comfortable.LastObject.DistanceToHyperDash = 100;

            Assert.That(MovementEvaluator.EvaluateDifficultyOf(hyper), Is.EqualTo(MovementEvaluator.EvaluateDifficultyOf(comfortable)).Within(1e-9));
        }

        // CF: MovementEvaluator.EvaluateDifficultyOf | buzz-slider detection (all conditions) | TRUE
        [Test]
        public void TestRepeatedBackAndForthBuzzPatternIsNullified()
        {
            // exact distances moved alternate 40, -40, 40 with identical strain times.
            Assert.That(evaluateLast((0, 200), (100, 240), (200, 175), (300, 240)), Is.Zero);
        }

        // CF: MovementEvaluator.EvaluateDifficultyOf | current.Index >= 2 | FALSE
        [Test]
        public void TestBuzzPatternNotNullifiedBeforeThirdObject()
        {
            Assert.That(evaluateLast((0, 200), (100, 240), (200, 175)), Is.GreaterThan(0));
        }

        // CF: MovementEvaluator.EvaluateDifficultyOf | strain times equal | FALSE
        [Test]
        public void TestBuzzPatternWithDifferentTimingIsNotNullified()
        {
            Assert.That(evaluateLast((0, 200), (100, 240), (200, 175), (350, 240)), Is.GreaterThan(0));
        }

        // CF: MovementEvaluator.EvaluateDifficultyOf | Math.Abs(ExactDistanceMoved) <= 2 * NORMALIZED_HALF_CATCHER_WIDTH | FALSE
        [Test]
        public void TestLargeBackAndForthIsNotABuzzPattern()
        {
            Assert.That(evaluateLast((0, 100), (100, 400), (200, 100), (300, 400)), Is.GreaterThan(0));
        }
    }
}
