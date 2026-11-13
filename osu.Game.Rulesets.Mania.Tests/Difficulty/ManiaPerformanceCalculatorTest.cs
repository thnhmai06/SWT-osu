// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Rulesets.Mania.Difficulty;
using osu.Game.Rulesets.Mania.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Mania.Tests.Difficulty
{
    [TestFixture]
    public class ManiaPerformanceCalculatorTest
    {
        private static double total(ScoreInfo score, double stars = 5) => new ManiaPerformanceCalculator().Calculate(score, new ManiaDifficultyAttributes { StarRating = stars }).Total;

        private static ScoreInfo score(int perfect, int great = 0, int miss = 0, params Mod[] mods) => new ScoreInfo
        {
            Statistics = { [HitResult.Perfect] = perfect, [HitResult.Great] = great, [HitResult.Miss] = miss },
            Mods = mods
        };

        private static double expectedBase(double stars, double accuracy, double hits)
            => 8.0 * Math.Pow(Math.Max(stars - 0.15, 0.05), 2.2) * Math.Max(0, 5 * accuracy - 4) * (1 + 0.1 * Math.Min(1, hits / 1500));

        // CF: ManiaPerformanceCalculator.calculateCustomAccuracy | totalHits == 0 | TRUE
        [Test]
        public void TestNoHitsGivesZero() => Assert.That(total(new ScoreInfo()), Is.Zero);

        // CF: ManiaPerformanceCalculator.calculateCustomAccuracy | totalHits == 0 | FALSE
        // CF: ManiaPerformanceCalculator.CreatePerformanceAttributes | score.Mods.Any(ModNoFail) | FALSE
        // CF: ManiaPerformanceCalculator.CreatePerformanceAttributes | score.Mods.Any(ModEasy) | FALSE
        // CF: ManiaPerformanceCalculator.computeDifficultyValue | Math.Max(0, 5 * acc - 4) | non-null
        [Test]
        public void TestAllPerfect() => Assert.That(total(score(100)), Is.EqualTo(expectedBase(5, 1, 100)).Within(1e-9));

        // CF: ManiaPerformanceCalculator.computeDifficultyValue | Math.Min(1, totalHits / 1500) | non-null
        [Test]
        public void TestLengthBonusIsCapped() => Assert.That(total(score(3000)), Is.EqualTo(expectedBase(5, 1, 3000)).Within(1e-9));

        // CF: ManiaPerformanceCalculator.computeDifficultyValue | accuracy below 80% -> Max(0, ...) clamps | non-null
        [Test]
        public void TestLowAccuracyAwardsNothing() => Assert.That(total(score(0, 0, 100)), Is.Zero);

        // CF: ManiaPerformanceCalculator.calculateCustomAccuracy | weighted accuracy | non-null
        [Test]
        public void TestGreatsLowerAccuracy()
        {
            double acc = 300.0 / 320;
            Assert.That(total(score(0, 100)), Is.EqualTo(expectedBase(5, acc, 100)).Within(1e-9));
        }

        // CF: ManiaPerformanceCalculator.computeDifficultyValue | Math.Max(StarRating - 0.15, 0.05) | non-null
        [Test]
        public void TestStarRatingFloor() => Assert.That(total(score(100), 0), Is.EqualTo(expectedBase(0, 1, 100)).Within(1e-9));

        // CF: ManiaPerformanceCalculator.CreatePerformanceAttributes | score.Mods.Any(ModNoFail) | TRUE
        [Test]
        public void TestNoFailMultiplier() => Assert.That(total(score(100, mods: new ManiaModNoFail())), Is.EqualTo(0.75 * expectedBase(5, 1, 100)).Within(1e-9));

        // CF: ManiaPerformanceCalculator.CreatePerformanceAttributes | score.Mods.Any(ModEasy) | TRUE
        [Test]
        public void TestEasyMultiplier() => Assert.That(total(score(100, mods: new ManiaModEasy())), Is.EqualTo(0.5 * expectedBase(5, 1, 100)).Within(1e-9));
    }
}
