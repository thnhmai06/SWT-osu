// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        private static OsuDifficultyAttributes attributes(int maxCombo = 500, int sliders = 100) => new OsuDifficultyAttributes
        {
            MaxCombo = maxCombo,
            SliderCount = sliders,
            LegacyScoreBaseMultiplier = 3,
            NestedScorePerObject = 20,
            MaximumLegacyComboScore = 400000,
            AimTopWeightedSliderFactor = 0.5,
        };

        private static ScoreInfo score(long? legacyTotal, int maxCombo, int misses, int oks = 0, int mehs = 0, double accuracy = 0.97, params Mod[] mods) => new ScoreInfo
        {
            LegacyTotalScore = legacyTotal,
            MaxCombo = maxCombo,
            Accuracy = accuracy,
            Mods = mods,
            Statistics = new Dictionary<HitResult, int>
            {
                [HitResult.Great] = 450,
                [HitResult.Ok] = oks,
                [HitResult.Meh] = mehs,
                [HitResult.Miss] = misses,
            },
        };

        // CF: OsuLegacyScoreMissCalculator.Calculate | attributes.MaxCombo == 0 | TRUE
        [Test]
        public void TestNoMaxCombo()
        {
            Assert.That(new OsuLegacyScoreMissCalculator(score(1000000, 100, 1), attributes(0)).Calculate(), Is.EqualTo(0));
        }

        // CF: OsuLegacyScoreMissCalculator.Calculate | score.LegacyTotalScore == null | TRUE
        [Test]
        public void TestNoLegacyScore()
        {
            Assert.That(new OsuLegacyScoreMissCalculator(score(null, 100, 1), attributes()).Calculate(), Is.EqualTo(0));
        }

        // CF: OsuLegacyScoreMissCalculator.Calculate | score.LegacyTotalScore == 0 | TRUE
        // CF: OsuLegacyScoreMissCalculator.Calculate | attributes.MaxCombo == 0 | FALSE
        // CF: OsuLegacyScoreMissCalculator.Calculate | score.LegacyTotalScore == null | FALSE
        [Test]
        public void TestZeroLegacyScore()
        {
            Assert.That(new OsuLegacyScoreMissCalculator(score(0, 100, 1), attributes()).Calculate(), Is.EqualTo(0));
        }

        // CF: OsuLegacyScoreMissCalculator.Calculate | remainingScore <= 0 | TRUE
        [Test]
        public void TestScoreAlreadyExplainedByComboReturnsComboBasedMaximum()
        {
            // a tiny legacy total is already less than what the max combo alone accounts for
            var calculator = new OsuLegacyScoreMissCalculator(score(1, 250, 3), attributes());
            double result = calculator.Calculate();

            Assert.That(result, Is.GreaterThanOrEqualTo(3));
        }

        // CF: OsuLegacyScoreMissCalculator.Calculate | remainingScore <= 0 | FALSE
        [Test]
        public void TestScoreBasedMissCountIsAtLeastOneAndCappedByComboBasedCount()
        {
            var attr = attributes();
            var s = score(long.MaxValue / 4, 250, 3);
            double result = new OsuLegacyScoreMissCalculator(s, attr).Calculate();

            // plenty of score remains unexplained by the combo -> only a small miss count is implied, but never below 1
            Assert.That(result, Is.GreaterThanOrEqualTo(1));
            Assert.That(result, Is.LessThanOrEqualTo(new OsuLegacyScoreMissCalculator(score(1, 250, 3), attr).Calculate()));
        }

        // CF: OsuLegacyScoreMissCalculator.calculateMaximumComboBasedMissCount | attributes.SliderCount <= 0 | TRUE
        [Test]
        public void TestNoSlidersMeansMissesOnlyFromJudgements()
        {
            Assert.That(new OsuLegacyScoreMissCalculator(score(1, 250, 4), attributes(sliders: 0)).Calculate(), Is.EqualTo(4));
        }

        // CF: OsuLegacyScoreMissCalculator.calculateMaximumComboBasedMissCount | score.MaxCombo < fullComboThreshold | FALSE
        [Test]
        public void TestFullComboHasOnlyJudgedMisses()
        {
            Assert.That(new OsuLegacyScoreMissCalculator(score(1, 500, 0), attributes()).Calculate(), Is.EqualTo(0));
        }

        // CF: OsuLegacyScoreMissCalculator.calculateMaximumComboBasedMissCount | score.MaxCombo < fullComboThreshold | TRUE
        // CF: OsuLegacyScoreMissCalculator.calculateMaximumComboBasedMissCount | sliderBreaks > maxPossibleSliderBreaks | TRUE
        [Test]
        public void TestSliderBreaksAreCappedByAvailableSliders()
        {
            // a large combo loss with many imperfect hits: estimated misses cannot exceed misses + max possible slider breaks
            double result = new OsuLegacyScoreMissCalculator(score(1, 100, 1, oks: 40, mehs: 40), attributes(sliders: 3)).Calculate();

            Assert.That(result, Is.EqualTo(1 + 3));
        }

        // CF: OsuLegacyScoreMissCalculator.calculateMaximumComboBasedMissCount | sliderBreaks > maxPossibleSliderBreaks | FALSE
        [Test]
        public void TestSliderBreaksWithinLimitAreKept()
        {
            double result = new OsuLegacyScoreMissCalculator(score(1, 490, 0, oks: 50), attributes(sliders: 100)).Calculate();

            Assert.That(result, Is.GreaterThan(0));
            Assert.That(result, Is.LessThanOrEqualTo(50));
        }

        [Test]
        public void TestModsAffectScoreMultiplier()
        {
            var attr = attributes();
            double none = new OsuLegacyScoreMissCalculator(score(2000000, 250, 3), attr).Calculate();

            foreach (var mod in new Mod[]
                     {
                         new OsuModNoFail(), new OsuModEasy(), new OsuModHalfTime(), new OsuModDaycore(), new OsuModHidden(), new OsuModHardRock(), new OsuModDoubleTime(),
                         new OsuModNightcore(), new OsuModFlashlight(), new OsuModSpunOut(), new OsuModRelax(), new OsuModAutopilot(), new ModScoreV2(),
                     })
            {
                double result = new OsuLegacyScoreMissCalculator(score(2000000, 250, 3, mods: new[] { mod }), attr).Calculate();
                Assert.That(result, Is.GreaterThanOrEqualTo(0), mod.Acronym);
            }

            Assert.That(none, Is.GreaterThanOrEqualTo(0));
        }
    }
}
