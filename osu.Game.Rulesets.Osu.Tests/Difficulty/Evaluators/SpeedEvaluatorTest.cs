// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators.Speed;
using static osu.Game.Rulesets.Osu.Tests.Difficulty.DifficultyTestHelper;

namespace osu.Game.Rulesets.Osu.Tests.Difficulty.Evaluators
{
    [TestFixture]
    public class SpeedEvaluatorTest
    {
        private static double evaluate(double spacing, float distance = 60, int index = 1)
            => SpeedEvaluator.EvaluateDifficultyOf(Build(Line(5, spacing, distance))[index]);

        // CF: SpeedEvaluator.EvaluateDifficultyOf | current.BaseObject is Spinner | TRUE
        [Test]
        public void TestSpinnerHasNoDifficulty()
        {
            var objects = Build(new[] { Circle(0, 100), Spinner(200) });
            Assert.That(SpeedEvaluator.EvaluateDifficultyOf(objects[0]), Is.EqualTo(0));
        }

        // CF: SpeedEvaluator.EvaluateDifficultyOf | current.BaseObject is Spinner | FALSE
        // CF: SpeedEvaluator.EvaluateDifficultyOf | DiffUtils.MillisecondsToBPM(strainTime) > min_speed_bonus | FALSE
        [Test]
        public void TestSlowNotesHaveNoStreamBonus()
        {
            Assert.That(evaluate(300), Is.GreaterThan(0));
        }

        // CF: SpeedEvaluator.EvaluateDifficultyOf | DiffUtils.MillisecondsToBPM(strainTime) > min_speed_bonus | TRUE
        [Test]
        public void TestFastStreamsAreHarderThanSlowOnes()
        {
            Assert.That(evaluate(60), Is.GreaterThan(evaluate(100)));
            Assert.That(evaluate(100), Is.GreaterThan(evaluate(300)));
        }

        // CF: SpeedEvaluator.EvaluateDifficultyOf | osuCurrObj.Next() == null | TRUE
        [Test]
        public void TestLastObjectHasNoDoubleTapPenalty()
        {
            Assert.That(evaluate(150, index: 3), Is.GreaterThan(0));
        }

        [Test]
        public void TestDoubleTappableDoubleIsEasierThanRegularStream()
        {
            // a tight pair followed by a gap can be double tapped
            var pair = Build(new[] { Circle(0, 100), Circle(150, 110), Circle(300, 120), Circle(450, 130) });
            var spaced = Build(new[] { Circle(0, 100), Circle(150, 200), Circle(300, 300), Circle(450, 400) });

            Assert.That(SpeedEvaluator.EvaluateDifficultyOf(pair[1]), Is.LessThanOrEqualTo(SpeedEvaluator.EvaluateDifficultyOf(spaced[1])));
        }
    }
}
