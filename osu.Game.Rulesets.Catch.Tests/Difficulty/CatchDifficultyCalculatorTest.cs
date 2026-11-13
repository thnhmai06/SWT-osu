// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Catch.Beatmaps;
using osu.Game.Rulesets.Catch.Difficulty;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Catch.Tests.Difficulty
{
    [TestFixture]
    public class CatchDifficultyCalculatorTest
    {
        private static CatchDifficultyAttributes calculate(float circleSize, params CatchHitObject[] hitObjects)
        {
            var beatmap = new CatchBeatmap { BeatmapInfo = { Ruleset = new CatchRuleset().RulesetInfo } };
            beatmap.Difficulty.CircleSize = circleSize;
            beatmap.HitObjects.AddRange(hitObjects);
            foreach (var h in hitObjects)
                h.ApplyDefaults(new ControlPointInfo(), beatmap.Difficulty);

            var ruleset = new CatchRuleset();
            return (CatchDifficultyAttributes)new CatchDifficultyCalculator(ruleset.RulesetInfo, new TestWorkingBeatmap(beatmap)).Calculate();
        }

        private static Fruit[] zigzag(int count) => Enumerable.Range(0, count).Select(i => new Fruit { StartTime = i * 100, X = i % 2 == 0 ? 50 : 450 }).ToArray();

        // CF: CatchDifficultyCalculator.CreateDifficultyAttributes | beatmap.HitObjects.Count == 0 | TRUE
        [Test]
        public void TestEmptyBeatmapHasZeroStars()
        {
            var attributes = calculate(5);

            Assert.That(attributes.StarRating, Is.Zero);
            Assert.That(attributes.MaxCombo, Is.Zero);
        }

        // CF: CatchDifficultyCalculator.CreateDifficultyAttributes | beatmap.HitObjects.Count == 0 | FALSE
        // CF: CatchDifficultyCalculator.CreateDifficultyHitObjects | foreach palpable | loop n
        // CF: CatchDifficultyCalculator.CreateDifficultyHitObjects | lastObject != null | FALSE
        // CF: CatchDifficultyCalculator.CreateDifficultyHitObjects | lastObject != null | TRUE
        [Test]
        public void TestMovementGivesPositiveStarsAndMaxCombo()
        {
            var attributes = calculate(5, zigzag(10));

            Assert.That(attributes.StarRating, Is.GreaterThan(0));
            Assert.That(attributes.MaxCombo, Is.EqualTo(10));
        }

        [Test]
        public void TestHarderPatternHasMoreStars()
        {
            Assert.That(calculate(5, zigzag(20)).StarRating, Is.GreaterThan(calculate(5, zigzag(3)).StarRating));
        }

        // CF: CatchDifficultyCalculator.CreateDifficultyHitObjects | foreach palpable | loop 0
        [Test]
        public void TestSingleObjectHasNoMovement()
        {
            Assert.That(calculate(5, new Fruit { X = 100 }).StarRating, Is.Zero);
        }

        // CF: CatchDifficultyCalculator.CreateDifficultyHitObjects | hitObject is Banana || hitObject is TinyDroplet | TRUE
        // CF: CatchDifficultyCalculator.CreateDifficultyHitObjects | hitObject is Banana || hitObject is TinyDroplet | FALSE
        [Test]
        public void TestBananasAreIgnored()
        {
            var plain = calculate(5, zigzag(10));

            var objects = zigzag(10).Cast<CatchHitObject>().ToList();
            objects.Add(new BananaShower { StartTime = 150, Duration = 600 });
            var withShower = calculate(5, objects.ToArray());

            Assert.That(withShower.StarRating, Is.EqualTo(plain.StarRating));
        }

        // CF: CatchDifficultyCalculator.CreateDifficultyHitObjects | Math.Max(0, CircleSize - 5.5f) | 0 side and positive side
        [Test]
        public void TestHighCircleSizeReducesCatcherWidthAndRaisesDifficulty()
        {
            Assert.That(calculate(8, zigzag(10)).StarRating, Is.GreaterThan(calculate(5, zigzag(10)).StarRating));
        }
    }
}
