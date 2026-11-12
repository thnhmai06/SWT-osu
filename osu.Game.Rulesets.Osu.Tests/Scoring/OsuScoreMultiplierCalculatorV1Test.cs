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

        // ---- rateAdjustMultiplier: `speedChange >= 1` TRUE / FALSE ----

        [Test]
        public void TestDoubleTime_SpeedUp_UsesFifthOfExcess()
        {
            // speedChange >= 1 TRUE: 1 + (1.5 - 1) / 5
            Assert.That(calculate(new OsuModDoubleTime { SpeedChange = { Value = 1.5 } }), Is.EqualTo(1.1).Within(1e-9));
        }

        [Test]
        public void TestNightcore_SpeedUp_UsesSameFormulaAsDoubleTime()
        {
            // Nightcore lambda reads its own SpeedChange
            Assert.That(calculate(new OsuModNightcore { SpeedChange = { Value = 2.0 } }), Is.EqualTo(1.2).Within(1e-9));
        }

        [Test]
        public void TestHalfTime_SlowDown_SubtractsFromBase()
        {
            // speedChange >= 1 FALSE: 0.6 + (0.7 - 1)
            Assert.That(calculate(new OsuModHalfTime { SpeedChange = { Value = 0.75 } }), Is.EqualTo(0.3).Within(1e-9));
        }

        [Test]
        public void TestDaycore_SlowDown_UsesSameFormulaAsHalfTime()
        {
            Assert.That(calculate(new OsuModDaycore { SpeedChange = { Value = 0.5 } }), Is.EqualTo(0.1).Within(1e-9));
        }

        [Test]
        public void TestDoubleTime_RoundsDownToTenth()
        {
            // 1.59 floors to 1.5 before conversion
            Assert.That(calculate(new OsuModDoubleTime { SpeedChange = { Value = 1.59 } }), Is.EqualTo(1.1).Within(1e-9));
        }

        // ---- UsesDefaultConfiguration ternaries ----

        [Test]
        public void TestHardRock_DefaultConfiguration()
            => Assert.That(calculate(new OsuModHardRock()), Is.EqualTo(1.06).Within(1e-9));

        [Test]
        public void TestHidden_DefaultConfiguration_GetsBonus()
            => Assert.That(calculate(new OsuModHidden()), Is.EqualTo(1.06).Within(1e-9));

        [Test]
        public void TestHidden_NonDefaultConfiguration_NoBonus()
            => Assert.That(calculate(new OsuModHidden { OnlyFadeApproachCircles = { Value = true } }), Is.EqualTo(1).Within(1e-9));

        [Test]
        public void TestFlashlight_DefaultConfiguration_GetsBonus()
            => Assert.That(calculate(new OsuModFlashlight()), Is.EqualTo(1.12).Within(1e-9));

        [Test]
        public void TestFlashlight_NonDefaultConfiguration_NoBonus()
            => Assert.That(calculate(new OsuModFlashlight { FollowDelay = { Value = 300 } }), Is.EqualTo(1).Within(1e-9));

        [Test]
        public void TestBlinds_DefaultConfiguration_GetsBonus()
            => Assert.That(calculate(new OsuModBlinds()), Is.EqualTo(1.12).Within(1e-9));

        // ---- flat single multipliers ----

        [TestCase(typeof(OsuModEasy), 0.5)]
        [TestCase(typeof(OsuModNoFail), 0.5)]
        [TestCase(typeof(OsuModTargetPractice), 0.1)]
        [TestCase(typeof(OsuModDifficultyAdjust), 0.5)]
        [TestCase(typeof(OsuModClassic), 0.96)]
        [TestCase(typeof(OsuModRelax), 0.1)]
        [TestCase(typeof(OsuModAutopilot), 0.1)]
        [TestCase(typeof(OsuModSpunOut), 0.9)]
        [TestCase(typeof(ModWindUp), 0.5)]
        [TestCase(typeof(ModWindDown), 0.5)]
        [TestCase(typeof(OsuModMagnetised), 0.5)]
        [TestCase(typeof(ModAdaptiveSpeed), 0.5)]
        [TestCase(typeof(OsuModSynesthesia), 0.8)]
        public void TestFlatMultiplier(System.Type modType, double expected)
        {
            Assert.That(calculate((Mod)System.Activator.CreateInstance(modType)!), Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void TestModWithoutRegisteredMultiplier_IsNeutral()
        {
            // calculator: TryGetValue FALSE path
            Assert.That(calculate(new OsuModMirror()), Is.EqualTo(1));
        }

        [Test]
        public void TestMultipleMods_MultiplyTogether()
            => Assert.That(calculate(new OsuModEasy(), new OsuModNoFail()), Is.EqualTo(0.25).Within(1e-9));
    }
}
