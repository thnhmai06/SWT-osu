// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Osu.Tests.Difficulty
{
    [TestFixture]
    public class OsuLegacyScoreMissCalculatorTest
    {
        private static OsuDifficultyAttributes attributes(int maxCombo = 500, int sliders = 0, double topWeighted = 0) => new OsuDifficultyAttributes
        {
            MaxCombo = maxCombo,
            SliderCount = sliders,
            AimTopWeightedSliderFactor = topWeighted,
            LegacyScoreBaseMultiplier = 3,
            MaximumLegacyComboScore = 1_000_000,
            NestedScorePerObject = 20,
        };

        private static ScoreInfo score(long? legacyTotal, int maxCombo, Dictionary<HitResult, int> statistics, params Mod[] mods) => new ScoreInfo
        {
            LegacyTotalScore = legacyTotal,
            MaxCombo = maxCombo,
            Accuracy = 0.95,
            Statistics = statistics,
            Mods = mods,
        };

        private static double calculate(ScoreInfo score, OsuDifficultyAttributes attributes) => new OsuLegacyScoreMissCalculator(score, attributes).Calculate();

        private static Dictionary<HitResult, int> stats(int great = 90, int ok = 0, int meh = 0, int miss = 0)
            => new Dictionary<HitResult, int> { [HitResult.Great] = great, [HitResult.Ok] = ok, [HitResult.Meh] = meh, [HitResult.Miss] = miss };

        // ---- guard clause: MaxCombo == 0 || LegacyTotalScore == null || LegacyTotalScore == 0 ----

        [Test]
        public void TestGuard_ZeroMaxCombo_ReturnsZero()
            => Assert.That(calculate(score(1000, 100, stats(miss: 5)), attributes(maxCombo: 0)), Is.EqualTo(0));

        [Test]
        public void TestGuard_NullLegacyScore_ReturnsZero()
            => Assert.That(calculate(score(null, 100, stats(miss: 5)), attributes()), Is.EqualTo(0));

        [Test]
        public void TestGuard_ZeroLegacyScore_ReturnsZero()
            => Assert.That(calculate(score(0, 100, stats(miss: 5)), attributes()), Is.EqualTo(0));

        [Test]
        public void TestGuard_AllConditionsFalse_ProceedsToEstimate()
            => Assert.That(calculate(score(1, 100, stats(miss: 5)), attributes()), Is.EqualTo(5));

        // ---- remainingScore <= 0 returns the combo-based maximum (observable with a tiny legacy score) ----

        [Test]
        public void TestMaximum_NoSliders_IsPlainMissCount()
        {
            // SliderCount <= 0 TRUE
            Assert.That(calculate(score(1, 100, stats(miss: 3)), attributes(sliders: 0)), Is.EqualTo(3));
        }

        [Test]
        public void TestMaximum_FullComboWithSliders_IsZero()
        {
            // score.MaxCombo < fullComboThreshold FALSE (500 - 4.8 = 495.2 <= 500)
            Assert.That(calculate(score(1, 500, stats(ok: 10)), attributes(sliders: 20)), Is.EqualTo(0));
        }

        [Test]
        public void TestMaximum_LostComboWithSliders_UsesComboRatioPower()
        {
            // threshold 495.2, combo 400: (495.2 / 400) ^ 2.5 ~ 1.7
            double result = calculate(score(1, 400, stats(ok: 10)), attributes(sliders: 20));

            Assert.That(result, Is.InRange(1.6, 1.8));
        }

        [Test]
        public void TestMaximum_CappedByImperfectHits()
        {
            // Math.Min(missCount, totalImperfectHits): only 1 imperfect hit available
            Assert.That(calculate(score(1, 400, stats(ok: 1)), attributes(sliders: 20)), Is.EqualTo(1));
        }

        [Test]
        public void TestMaximum_CappedBySliderBreaksPlusMisses()
        {
            // sliderBreaks > maxPossibleSliderBreaks TRUE: 3 misses + at most 2 slider breaks
            Assert.That(calculate(score(1, 100, stats(ok: 7, miss: 3)), attributes(sliders: 2)), Is.EqualTo(5));
        }

        [Test]
        public void TestMaximum_SliderBreaksBelowCap_NotClamped()
        {
            // sliderBreaks > maxPossibleSliderBreaks FALSE: ~1.7 <= 20
            double result = calculate(score(1, 400, stats(ok: 10)), attributes(sliders: 20));

            Assert.That(result, Is.LessThan(2));
        }

        // ---- score-based estimate ----

        [Test]
        public void TestHugeScore_MeansNoBreakDetected_ClampedToOneThenToMaximum()
        {
            // scoreBasedMissCount < 1 => Math.Max picks 1; Math.Min then picks the smaller combo-based maximum (5)
            Assert.That(calculate(score(2_000_000_000, 250, stats(miss: 5)), attributes()), Is.EqualTo(1));
        }

        [Test]
        public void TestHugeScore_NoMisses_MaximumOfZeroWins()
        {
            // Math.Min picks the maximum (0)
            Assert.That(calculate(score(2_000_000_000, 250, stats(miss: 0)), attributes()), Is.EqualTo(0));
        }

        [Test]
        public void TestScoreBasedEstimate_DecreasesWithHigherScoreAndStaysWithinBounds()
        {
            // scan legacy scores: the estimate must never increase with score, never exceed the maximum, and must pass through values above 1
            var attr = attributes();
            var statistics = stats(miss: 50);

            double previous = double.PositiveInfinity;
            bool sawAboveOne = false;

            for (double legacy = 1; legacy < 1e10; legacy *= 1.05)
            {
                double result = calculate(score((long)legacy, 250, statistics), attr);

                Assert.That(result, Is.LessThanOrEqualTo(50 + 1e-9));
                Assert.That(result, Is.LessThanOrEqualTo(previous + 1e-9), $"estimate rose at legacy score {legacy:N0}");
                Assert.That(double.IsNaN(result), Is.False);

                sawAboveOne |= result > 1 + 1e-9;
                previous = result;
            }

            Assert.That(sawAboveOne, Is.True);
        }

        // ---- getLegacyScoreMultiplier: one run per switch case ----

        private static readonly Type[] multiplier_mods =
        {
            typeof(OsuModNoFail), typeof(OsuModEasy), typeof(OsuModHalfTime), typeof(OsuModDaycore), typeof(OsuModHidden),
            typeof(OsuModHardRock), typeof(OsuModDoubleTime), typeof(OsuModNightcore), typeof(OsuModFlashlight), typeof(OsuModSpunOut),
            typeof(OsuModRelax), typeof(OsuModAutopilot),
        };

        [TestCaseSource(nameof(multiplier_mods))]
        public void TestEveryModCase_ProducesBoundedResult(Type modType)
        {
            var mod = (Mod)Activator.CreateInstance(modType)!;

            foreach (bool scoreV2 in new[] { false, true })
            {
                var mods = scoreV2 ? new[] { mod, new ModScoreV2() } : new[] { mod };
                double result = calculate(score(5_000_000, 250, stats(miss: 5), mods), attributes());

                Assert.That(double.IsNaN(result), Is.False);
                Assert.That(result, Is.InRange(0, 5), $"{modType.Name} scoreV2={scoreV2}");
            }
        }

        [Test]
        public void TestRelaxAndAutopilot_ShareTheZeroMultiplier()
        {
            // both return 0 from getLegacyScoreMultiplier, so identical scores give identical estimates
            double relax = calculate(score(5_000_000, 250, stats(miss: 5), new OsuModRelax()), attributes());
            double autopilot = calculate(score(5_000_000, 250, stats(miss: 5), new OsuModAutopilot()), attributes());

            Assert.That(relax, Is.EqualTo(autopilot));
        }
    }
}
