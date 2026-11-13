// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators.Aim;
using static osu.Game.Rulesets.Osu.Tests.Difficulty.DifficultyTestHelper;

namespace osu.Game.Rulesets.Osu.Tests.Difficulty.Evaluators
{
    [TestFixture]
    public class AgilityEvaluatorTest
    {
        private static double evaluate(double spacing, float distance, int index = 1, BeatmapDifficulty? difficulty = null)
        {
            var objects = Build(Line(4, spacing, distance), difficulty: difficulty);
            return AgilityEvaluator.EvaluateDifficultyOf(objects[index]);
        }

        // CF: AgilityEvaluator.EvaluateDifficultyOf | current.BaseObject is Spinner | TRUE
        [Test]
        public void TestSpinnerHasNoDifficulty()
        {
            var objects = Build(new[] { Circle(0, 100), Circle(200, 200), Spinner(400) });
            Assert.That(AgilityEvaluator.EvaluateDifficultyOf(objects[1]), Is.EqualTo(0));
        }

        // CF: AgilityEvaluator.EvaluateDifficultyOf | current.BaseObject is Spinner | FALSE
        // CF: AgilityEvaluator.EvaluateDifficultyOf | current.Index > 0 | FALSE
        // CF: AgilityEvaluator.EvaluateDifficultyOf | osuPrevObj?.LazyTravelDistance ?? 0 | null
        [Test]
        public void TestFirstObjectHasDifficulty()
        {
            Assert.That(evaluate(150, 100, index: 0), Is.GreaterThan(0));
        }

        // CF: AgilityEvaluator.EvaluateDifficultyOf | current.Index > 0 | TRUE
        // CF: AgilityEvaluator.EvaluateDifficultyOf | osuPrevObj?.LazyTravelDistance ?? 0 | non-null
        [Test]
        public void TestLaterObjectHasDifficulty()
        {
            Assert.That(evaluate(150, 100, index: 2), Is.GreaterThan(0));
        }

        [Test]
        public void TestFasterIsHarder()
        {
            Assert.That(evaluate(100, 80), Is.GreaterThan(evaluate(200, 80)));
        }

        [Test]
        public void TestLargerDistanceIsHarderUntilCap()
        {
            Assert.That(evaluate(150, 80), Is.GreaterThan(evaluate(150, 20)));
            Assert.That(evaluate(150, 300), Is.EqualTo(evaluate(150, 400)).Within(1e-9));
        }

        [Test]
        public void TestSmallerCirclesAreHarder()
        {
            Assert.That(evaluate(150, 80, difficulty: new BeatmapDifficulty { CircleSize = 7 }), Is.GreaterThan(evaluate(150, 80)));
        }

        [Test]
        public void TestSliderTravelAddsDistance()
        {
            var withCircle = Build(new OsuHitObject[] { Circle(0, 100), Circle(200, 200), Circle(400, 300) });
            var withSlider = Build(new OsuHitObject[] { Circle(0, 100), Slider(200, 200, 192, 300, 0), Circle(1500, 600) });

            Assert.That(AgilityEvaluator.EvaluateDifficultyOf(withSlider[1]), Is.GreaterThan(0));
            Assert.That(AgilityEvaluator.EvaluateDifficultyOf(withCircle[1]), Is.GreaterThan(0));
        }
    }
}
