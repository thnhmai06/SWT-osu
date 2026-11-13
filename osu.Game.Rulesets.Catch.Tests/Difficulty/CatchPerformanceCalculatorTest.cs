// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Difficulty;
using osu.Game.Rulesets.Catch.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Catch.Tests.Difficulty
{
    [TestFixture]
    public class CatchPerformanceCalculatorTest
    {
        private static double calculate(int greats = 100, int misses = 0, int maxCombo = -1, double ar = 8, int attrMaxCombo = 100, params Mod[] mods)
        {
            var score = new ScoreInfo
            {
                Ruleset = new CatchRuleset().RulesetInfo,
                BeatmapInfo = new BeatmapInfo { Difficulty = new BeatmapDifficulty { ApproachRate = (float)ar } },
                MaxCombo = maxCombo < 0 ? greats : maxCombo,
                Mods = mods,
                Statistics = new Dictionary<HitResult, int>
                {
                    [HitResult.Great] = greats,
                    [HitResult.Miss] = misses,
                },
            };

            var attributes = new CatchDifficultyAttributes { StarRating = 3, MaxCombo = attrMaxCombo };
            return new CatchPerformanceCalculator().Calculate(score, attributes).Total;
        }

        private static double lengthBonus(int hits) => 0.95 + 0.3 * Math.Min(1.0, hits / 2500.0) + (hits > 2500 ? Math.Log10(hits / 2500.0) * 0.475 : 0.0);

        [Test]
        public void TestNoHitsGivesZero()
        {
            Assert.That(calculate(greats: 0), Is.Zero);
        }

        [Test]
        public void TestMissesReduceValue()
        {
            Assert.That(calculate(greats: 99, misses: 1, maxCombo: 100), Is.LessThan(calculate()));
        }

        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | catchAttributes.MaxCombo > 0 | TRUE
        [Test]
        public void TestComboScalingReducesValueForBrokenCombo()
        {
            Assert.That(calculate(maxCombo: 50) / calculate(), Is.EqualTo(Math.Pow(50, 0.35) / Math.Pow(100, 0.35)).Within(1e-9));
        }

        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | catchAttributes.MaxCombo > 0 | FALSE
        [Test]
        public void TestNoComboScalingWithoutMapCombo()
        {
            Assert.That(calculate(maxCombo: 50, attrMaxCombo: 0), Is.EqualTo(calculate(maxCombo: 100, attrMaxCombo: 0)));
        }

        [Test]
        public void TestComboAboveMapMaximumIsClamped()
        {
            Assert.That(calculate(maxCombo: 500), Is.EqualTo(calculate()));
        }

        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | numTotalHits > 2500 | FALSE
        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | numTotalHits > 2500 | TRUE
        [TestCase(100)]
        [TestCase(5000)]
        public void TestFlashlightScalesByLengthBonus(int hits)
        {
            double ratio = calculate(hits, attrMaxCombo: hits, mods: new Mod[] { new CatchModFlashlight() }) / calculate(hits, attrMaxCombo: hits);
            Assert.That(ratio, Is.EqualTo(1.35 * lengthBonus(hits)).Within(1e-9));
        }

        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | approachRateFactor: approachRate > 9 | TRUE
        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | approachRate > 10 | FALSE
        [Test]
        public void TestApproachRateAboveNineBonus()
        {
            Assert.That(calculate(ar: 9.5) / calculate(ar: 8), Is.EqualTo(1.05).Within(1e-6));
        }

        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | approachRate > 10 | TRUE
        [Test]
        public void TestApproachRateAboveTenBonus()
        {
            Assert.That(calculate(ar: 11) / calculate(ar: 8), Is.EqualTo(1.3).Within(1e-6));
        }

        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | approachRate > 9 | FALSE
        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | approachRate < 8 | TRUE
        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | preempt > 1200 | FALSE
        [Test]
        public void TestLowApproachRateBonus()
        {
            Assert.That(calculate(ar: 6) / calculate(ar: 8), Is.EqualTo(1.05).Within(1e-6));
        }

        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | preempt > 1200 | TRUE
        [Test]
        public void TestVeryLowApproachRateBonus()
        {
            Assert.That(calculate(ar: 4) / calculate(ar: 8), Is.EqualTo(1.1).Within(1e-6));
        }

        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | approachRate < 8 | FALSE
        [Test]
        public void TestApproachRateEightHasNoBonus()
        {
            Assert.That(calculate(ar: 8), Is.EqualTo(calculate(ar: 8.5) / 1.0).Within(calculate(ar: 8) * 1e-6));
        }

        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | mods.Any(ModHidden) | TRUE
        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | approachRate <= 10 | TRUE
        [Test]
        public void TestHiddenBonusGrowsWithLowerApproachRate()
        {
            Assert.That(calculate(ar: 8, mods: new Mod[] { new CatchModHidden() }) / calculate(ar: 8), Is.EqualTo(1.05 + 0.075 * 2).Within(1e-6));
        }

        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | approachRate <= 10 | FALSE
        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | approachRate > 10 (hidden) | TRUE
        [Test]
        public void TestHiddenBonusAboveApproachRateTen()
        {
            Assert.That(calculate(ar: 11, mods: new Mod[] { new CatchModHidden() }) / calculate(ar: 11), Is.EqualTo(1.01).Within(1e-6));
        }

        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | mods.Any(ModNoFail) | TRUE
        [TestCase(1, 0.98)]
        [TestCase(20, 0.90)]
        public void TestNoFailPenaltyIsFlooredAtTenPercent(int misses, double expected)
        {
            double ratio = calculate(misses: misses, maxCombo: 100, mods: new Mod[] { new CatchModNoFail() }) / calculate(misses: misses, maxCombo: 100);
            Assert.That(ratio, Is.EqualTo(expected).Within(1e-9));
        }

        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | mods.Any(ModHidden) | FALSE
        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | mods.Any(ModFlashlight) | FALSE
        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | mods.Any(ModNoFail) | FALSE
        // CF: CatchPerformanceCalculator.CreatePerformanceAttributes | mods.Any(ModFlashlight) | TRUE
        [Test]
        public void TestUnrelatedModsDoNotChangeValue()
        {
            Assert.That(calculate(mods: new Mod[] { new CatchModMirror() }), Is.EqualTo(calculate()));
        }
    }
}
