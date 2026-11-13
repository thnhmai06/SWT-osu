// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Osu.Tests.Scoring
{
    [TestFixture]
    public class OsuScoreMultiplierCalculatorV1Test
    {
        private static double calculate(params Mod[] mods)
            => new OsuScoreMultiplierCalculatorV1(new ScoreMultiplierContext(new BeatmapDifficulty())).CalculateFor(mods);

        // CF: OsuScoreMultiplierCalculatorV1.rateAdjustMultiplier | speedChange >= 1 | TRUE
        [Test]
        public void TestDoubleTimeDefaultRate()
        {
            // 1.5x -> 1 + 0.5 / 5
            Assert.That(calculate(new OsuModDoubleTime()), Is.EqualTo(1.1).Within(1e-9));
        }

        // CF: OsuScoreMultiplierCalculatorV1.rateAdjustMultiplier | speedChange >= 1 | FALSE
        [Test]
        public void TestHalfTimeDefaultRate()
        {
            // 0.75x -> rounds down to 0.7 -> 0.6 - 0.3
            Assert.That(calculate(new OsuModHalfTime()), Is.EqualTo(0.3).Within(1e-9));
        }

        // CF: OsuScoreMultiplierCalculatorV1.ctor | hardRock.UsesDefaultConfiguration | TRUE
        // CF: OsuScoreMultiplierCalculatorV1.ctor | hidden.UsesDefaultConfiguration | TRUE
        // CF: OsuScoreMultiplierCalculatorV1.ctor | flashlight.UsesDefaultConfiguration | TRUE
        [Test]
        public void TestDefaultConfigurationMultipliers()
        {
            Assert.That(calculate(new OsuModHardRock()), Is.EqualTo(1.06).Within(1e-9));
            Assert.That(calculate(new OsuModHidden()), Is.EqualTo(1.06).Within(1e-9));
            Assert.That(calculate(new OsuModFlashlight()), Is.EqualTo(1.12).Within(1e-9));
        }

        // CF: OsuScoreMultiplierCalculatorV1.ctor | hidden.UsesDefaultConfiguration | FALSE
        [Test]
        public void TestNonDefaultHiddenIsNeutral()
        {
            Assert.That(calculate(new OsuModHidden { OnlyFadeApproachCircles = { Value = true } }), Is.EqualTo(1).Within(1e-9));
        }

        // CF: OsuScoreMultiplierCalculatorV1.ctor | flashlight.UsesDefaultConfiguration | FALSE
        [Test]
        public void TestNonDefaultFlashlightIsNeutral()
        {
            Assert.That(calculate(new OsuModFlashlight { ComboBasedSize = { Value = false } }), Is.EqualTo(1).Within(1e-9));
        }

        [Test]
        public void TestFlatMultipliers()
        {
            Assert.That(calculate(new OsuModEasy()), Is.EqualTo(0.5).Within(1e-9));
            Assert.That(calculate(new OsuModRelax()), Is.EqualTo(0.1).Within(1e-9));
            Assert.That(calculate(new OsuModEasy(), new OsuModRelax()), Is.EqualTo(0.05).Within(1e-9));
        }
    }
}
