// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko.Mods;
using osu.Game.Rulesets.Taiko.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Taiko.Tests.Scoring
{
    [TestFixture]
    public class TaikoScoreMultiplierCalculatorTest
    {
        private static double calc(Mod mod, ScoreInfo? score = null)
            => new TaikoScoreMultiplierCalculator(new ScoreMultiplierContext(new BeatmapDifficulty(), score)).CalculateFor(new[] { mod });

        // CF: TaikoScoreMultiplierCalculator.rateAdjustMultiplier | speedChange >= 1 | FALSE
        [Test]
        public void TestHalfTimeDefault() => Assert.That(calc(new TaikoModHalfTime()), Is.EqualTo(0.3).Within(1e-9));

        // CF: TaikoScoreMultiplierCalculator.rateAdjustMultiplier | speedChange >= 1 | TRUE
        [Test]
        public void TestDoubleTimeDefault() => Assert.That(calc(new TaikoModDoubleTime()), Is.EqualTo(1.1).Within(1e-9));

        // CF: TaikoScoreMultiplierCalculator.rateAdjustMultiplier | speedChange >= 1 | TRUE
        [Test]
        public void TestDoubleTimeRoundsDownToTenth()
        {
            var mod = new TaikoModDoubleTime();
            mod.SpeedChange.Value = 1.37;
            Assert.That(calc(mod), Is.EqualTo(1 + 0.3 / 5).Within(1e-9));
        }

        [Test]
        public void TestFlatMultipliers()
        {
            Assert.That(calc(new TaikoModEasy()), Is.EqualTo(0.5));
            Assert.That(calc(new TaikoModNoFail()), Is.EqualTo(0.5));
            Assert.That(calc(new TaikoModSimplifiedRhythm()), Is.EqualTo(0.6));
            Assert.That(calc(new TaikoModRelax()), Is.EqualTo(0.1));
            Assert.That(calc(new TaikoModConstantSpeed()), Is.EqualTo(0.9));
            Assert.That(calc(new TaikoModDifficultyAdjust()), Is.EqualTo(0.5));
        }

        // CF: TaikoScoreMultiplierCalculator.ctor | hardRock.UsesDefaultConfiguration | TRUE
        // CF: TaikoScoreMultiplierCalculator.ctor | hidden.UsesDefaultConfiguration | TRUE
        // CF: TaikoScoreMultiplierCalculator.ctor | flashlight.UsesDefaultConfiguration | TRUE
        [Test]
        public void TestDefaultConfigurationBonuses()
        {
            Assert.That(calc(new TaikoModHardRock()), Is.EqualTo(1.06));
            Assert.That(calc(new TaikoModHidden()), Is.EqualTo(1.06));
            Assert.That(calc(new TaikoModFlashlight()), Is.EqualTo(1.12));
        }

        // CF: TaikoScoreMultiplierCalculator.ctor | flashlight.UsesDefaultConfiguration | FALSE
        [Test]
        public void TestFlashlightNonDefaultConfiguration()
        {
            var mod = new TaikoModFlashlight();
            mod.SizeMultiplier.Value = 1.5f;
            Assert.That(calc(mod), Is.EqualTo(1));
        }

        // CF: TaikoScoreMultiplierCalculator.classicMultiplier | score != null | FALSE
        [Test]
        public void TestClassicWithoutScore() => Assert.That(calc(new TaikoModClassic()), Is.EqualTo(1));

        // CF: TaikoScoreMultiplierCalculator.classicMultiplier | score != null | TRUE
        // CF: TaikoScoreMultiplierCalculator.classicMultiplier | score.TotalScoreVersion < 30000017 | TRUE
        [Test]
        public void TestClassicOldScoreVersion()
            => Assert.That(calc(new TaikoModClassic(), new ScoreInfo { TotalScoreVersion = 30000016 }), Is.EqualTo(0.96));

        // CF: TaikoScoreMultiplierCalculator.classicMultiplier | score.TotalScoreVersion < 30000017 | FALSE
        [Test]
        public void TestClassicCurrentScoreVersion()
            => Assert.That(calc(new TaikoModClassic(), new ScoreInfo { TotalScoreVersion = 30000017 }), Is.EqualTo(1));
    }
}
