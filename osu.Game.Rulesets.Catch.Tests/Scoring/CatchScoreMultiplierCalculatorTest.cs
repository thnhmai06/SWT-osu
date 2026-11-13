// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Mods;
using osu.Game.Rulesets.Catch.Scoring;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Catch.Tests.Scoring
{
    [TestFixture]
    public class CatchScoreMultiplierCalculatorTest
    {
        private static double calculate(params Mod[] mods) => calculate((ScoreInfo?)null, mods);

        private static double calculate(ScoreInfo? score, params Mod[] mods)
            => new CatchScoreMultiplierCalculator(new ScoreMultiplierContext(new BeatmapDifficulty(), score)).CalculateFor(mods);

        [Test]
        public void TestFlatMultipliers()
        {
            Assert.That(calculate(new CatchModEasy()), Is.EqualTo(0.5));
            Assert.That(calculate(new CatchModNoFail()), Is.EqualTo(0.5));
            Assert.That(calculate(new CatchModDifficultyAdjust()), Is.EqualTo(0.5));
            Assert.That(calculate(new CatchModRelax()), Is.EqualTo(0.1));
            Assert.That(calculate(new CatchModSynesthesia()), Is.EqualTo(0.8));
            Assert.That(calculate(new ModWindUp()), Is.EqualTo(0.5));
            Assert.That(calculate(new ModWindDown()), Is.EqualTo(0.5));
        }

        // CF: CatchScoreMultiplierCalculator.rateAdjustMultiplier | speedChange >= 1 | TRUE
        [TestCase(1.01, 1.00)]
        [TestCase(1.10, 1.02)]
        [TestCase(1.50, 1.10)]
        [TestCase(2.00, 1.20)]
        public void TestRateIncreaseMultiplier(double speedChange, double expected)
        {
            Assert.That(calculate(new CatchModDoubleTime { SpeedChange = { Value = speedChange } }), Is.EqualTo(expected).Within(1e-9));
            Assert.That(calculate(new CatchModNightcore { SpeedChange = { Value = speedChange } }), Is.EqualTo(expected).Within(1e-9));
        }

        // CF: CatchScoreMultiplierCalculator.rateAdjustMultiplier | speedChange >= 1 | FALSE
        [TestCase(0.50, 0.1)]
        [TestCase(0.75, 0.3)]
        [TestCase(0.99, 0.5)]
        public void TestRateDecreaseMultiplier(double speedChange, double expected)
        {
            Assert.That(calculate(new CatchModHalfTime { SpeedChange = { Value = speedChange } }), Is.EqualTo(expected).Within(1e-9));
            Assert.That(calculate(new CatchModDaycore { SpeedChange = { Value = speedChange } }), Is.EqualTo(expected).Within(1e-9));
        }

        // CF: CatchScoreMultiplierCalculator..ctor | hardRock.UsesDefaultConfiguration | TRUE
        // CF: CatchScoreMultiplierCalculator..ctor | hidden.UsesDefaultConfiguration | TRUE
        // CF: CatchScoreMultiplierCalculator..ctor | flashlight.UsesDefaultConfiguration | TRUE
        [Test]
        public void TestDefaultConfigurationMultipliers()
        {
            Assert.That(calculate(new CatchModHardRock()), Is.EqualTo(1.12));
            Assert.That(calculate(new CatchModHidden()), Is.EqualTo(1.06));
            Assert.That(calculate(new CatchModFlashlight()), Is.EqualTo(1.12));
        }

        // CF: CatchScoreMultiplierCalculator..ctor | flashlight.UsesDefaultConfiguration | FALSE
        [Test]
        public void TestNonDefaultFlashlightHasNoMultiplier()
        {
            Assert.That(calculate(new CatchModFlashlight { ComboBasedSize = { Value = false } }), Is.EqualTo(1));
        }

        [Test]
        public void TestNoModsAndUnlistedModsAreNeutral()
        {
            Assert.That(calculate(), Is.EqualTo(1));
            Assert.That(calculate(new CatchModMirror(), new CatchModMuted()), Is.EqualTo(1));
        }

        [Test]
        public void TestMultipliersCompound()
        {
            Assert.That(calculate(new CatchModHidden(), new CatchModHardRock()), Is.EqualTo(1.06 * 1.12).Within(1e-9));
        }

        // CF: CatchScoreMultiplierCalculator.classicMultiplier | score != null | FALSE
        [Test]
        public void TestClassicWithoutScoreIsNeutral()
        {
            Assert.That(calculate((ScoreInfo?)null, new CatchModClassic()), Is.EqualTo(1));
        }

        // CF: CatchScoreMultiplierCalculator.classicMultiplier | score != null | TRUE
        // CF: CatchScoreMultiplierCalculator.classicMultiplier | score.TotalScoreVersion < 30000017 | TRUE
        // CF: CatchScoreMultiplierCalculator.classicMultiplier | score.TotalScoreVersion < 30000017 | FALSE
        [TestCase(30000001, 0.96)]
        [TestCase(30000016, 0.96)]
        [TestCase(30000017, 1)]
        public void TestClassicMultiplierDependsOnScoreVersion(int version, double expected)
        {
            Assert.That(calculate(new ScoreInfo { TotalScoreVersion = version }, new CatchModClassic()), Is.EqualTo(expected));
        }
    }
}
