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
    public class OsuScoreMultiplierCalculatorV2Test
    {
        private static readonly BeatmapDifficulty base_difficulty = new BeatmapDifficulty
        {
            CircleSize = 4,
            DrainRate = 5,
            OverallDifficulty = 6,
            ApproachRate = 7
        };

        private static double calculate(params Mod[] mods)
            => new OsuScoreMultiplierCalculatorV2(new ScoreMultiplierContext(base_difficulty)).CalculateFor(mods);

        // ---- easyMultiplier: Math.Max(0, ...) and Math.Max(0.4, ...) ----

        [TestCase(2, 0.8)] // at default retries: Max(0, 0) arm
        [TestCase(0, 0.8)] // below default: Max(0, negative) -> 0
        [TestCase(5, 0.5)] // above default, above floor
        [TestCase(8, 0.4)] // above default, clamped by Max(0.4, ...)
        public void TestEasy(int retries, double expected)
            => Assert.That(calculate(new OsuModEasy { Retries = { Value = retries } }), Is.EqualTo(expected).Within(1e-9));

        // ---- halfTimeMultiplier ----

        [TestCase(0.5, 0.2)]
        [TestCase(0.75, 0.55)]
        public void TestHalfTime(double speed, double expected)
            => Assert.That(calculate(new OsuModHalfTime { SpeedChange = { Value = speed } }), Is.EqualTo(expected).Within(1e-9));

        [Test]
        public void TestDaycore_UsesHalfTimeFormula()
            => Assert.That(calculate(new OsuModDaycore { SpeedChange = { Value = 0.75 } }), Is.EqualTo(0.55).Within(1e-9));

        // ---- doubleTimeMultiplier: value != 1.5 && value != 1.0 ----

        [Test]
        public void TestDoubleTime_DefaultRate_NoPenalty()
            // value != 1.5 FALSE (short-circuits second condition)
            => Assert.That(calculate(new OsuModDoubleTime { SpeedChange = { Value = 1.5 } }), Is.EqualTo(1.23).Within(1e-9));

        [Test]
        public void TestDoubleTime_CustomRate_AppliesPenalty()
            // value != 1.5 TRUE and value != 1.0 TRUE
            => Assert.That(calculate(new OsuModDoubleTime { SpeedChange = { Value = 1.3 } }), Is.EqualTo(0.3 * 0.46 + 1 - 0.01).Within(1e-9));

        [Test]
        public void TestDoubleTime_RateFlooringToOne_NoPenalty()
            // value != 1.5 TRUE, value != 1.0 FALSE
            => Assert.That(calculate(new OsuModDoubleTime { SpeedChange = { Value = 1.05 } }), Is.EqualTo(1).Within(1e-9));

        [Test]
        public void TestNightcore_UsesDoubleTimeFormula()
            => Assert.That(calculate(new OsuModNightcore { SpeedChange = { Value = 1.5 } }), Is.EqualTo(1.23).Within(1e-9));

        // ---- hiddenMultiplier: OnlyFadeApproachCircles / otherModsProvideTimingInfo ----

        [Test]
        public void TestHidden_Default()
            // both conditions FALSE
            => Assert.That(calculate(new OsuModHidden()), Is.EqualTo(1.04).Within(1e-9));

        [Test]
        public void TestHidden_OnlyFadeApproachCircles()
            // OnlyFadeApproachCircles TRUE, timing FALSE
            => Assert.That(calculate(new OsuModHidden { OnlyFadeApproachCircles = { Value = true } }), Is.EqualTo(1.02).Within(1e-9));

        [Test]
        public void TestHidden_WithTimingMod()
            // OnlyFadeApproachCircles FALSE, timing TRUE
            => Assert.That(calculate(new OsuModHidden(), new OsuModWiggle()), Is.EqualTo(1.02).Within(1e-9));

        [Test]
        public void TestHidden_OnlyFadeApproachCirclesWithTimingMod()
            // both TRUE
            => Assert.That(calculate(new OsuModHidden { OnlyFadeApproachCircles = { Value = true } }, new OsuModGrow()), Is.EqualTo(1.00).Within(1e-9));

        [Test]
        public void TestHiddenRepel_TimingProvided()
            => Assert.That(calculate(new OsuModHidden(), new OsuModRepel()), Is.EqualTo(1.02).Within(1e-9));

        [Test]
        public void TestHiddenDepth_TimingProvided()
            => Assert.That(calculate(new OsuModHidden(), new OsuModDepth()), Is.EqualTo(1.02).Within(1e-9));

        [Test]
        public void TestHiddenDeflate_CombinesHiddenAndDeflateMultipliers()
            // default Deflate start scale -> deflate factor 1
            => Assert.That(calculate(new OsuModHidden(), new OsuModDeflate()), Is.EqualTo(1.02).Within(1e-9));

        [Test]
        public void TestHiddenBlinds_FixedCombination()
            => Assert.That(calculate(new OsuModHidden(), new OsuModBlinds()), Is.EqualTo(1.24).Within(1e-9));

        [Test]
        public void TestTraceableBlinds_FixedCombination()
            => Assert.That(calculate(new OsuModTraceable(), new OsuModBlinds()), Is.EqualTo(1.24).Within(1e-9));

        [Test]
        public void TestTraceable_Single()
            => Assert.That(calculate(new OsuModTraceable()), Is.EqualTo(1.02).Within(1e-9));

        [Test]
        public void TestBlinds_Single()
            => Assert.That(calculate(new OsuModBlinds()), Is.EqualTo(1.24).Within(1e-9));

        // ---- flashlightMultiplier: Min/Max arms and ComboBasedSize ----

        [Test]
        public void TestFlashlight_DefaultSize_ComboBased()
            => Assert.That(calculate(new OsuModFlashlight()), Is.EqualTo(1.2).Within(1e-9));

        [Test]
        public void TestFlashlight_SmallSize_CappedAtMaximum()
            // Min(1.2, 1.3) picks 1.2
            => Assert.That(calculate(new OsuModFlashlight { SizeMultiplier = { Value = 0.5f } }), Is.EqualTo(1.2).Within(1e-9));

        [Test]
        public void TestFlashlight_InteriorSize_Scales()
            // neither clamp is active
            => Assert.That(calculate(new OsuModFlashlight { SizeMultiplier = { Value = 1.5f } }), Is.EqualTo(1.1).Within(1e-6));

        [Test]
        public void TestFlashlight_LargeSize_FlooredAtMinimum()
            // Max(1.02, 1.0) picks 1.02
            => Assert.That(calculate(new OsuModFlashlight { SizeMultiplier = { Value = 2f } }), Is.EqualTo(1.02).Within(1e-6));

        [Test]
        public void TestFlashlight_NotComboBased_BonusReducedToFifth()
            // ComboBasedSize FALSE
            => Assert.That(calculate(new OsuModFlashlight { ComboBasedSize = { Value = false } }), Is.EqualTo(1.04).Within(1e-9));

        [Test]
        public void TestFlashlightFreezeFrame_HalfBonus()
            => Assert.That(calculate(new OsuModFlashlight(), new OsuModFreezeFrame()), Is.EqualTo(1.1).Within(1e-9));

        // ---- difficultyAdjustMultiplier: `??` and Math.Max clamps ----

        [Test]
        public void TestDifficultyAdjust_NothingOverridden_IsNeutral()
            // every `??` falls back to beatmap value (left operand null)
            => Assert.That(calculate(new OsuModDifficultyAdjust()), Is.EqualTo(1).Within(1e-9));

        [Test]
        public void TestDifficultyAdjust_AllOverridden_MultipliesPenalties()
        {
            // every `??` takes the override; each difference 0.2 -> 0.9
            var mod = new OsuModDifficultyAdjust
            {
                CircleSize = { Value = 4.2f },
                DrainRate = { Value = 5.2f },
                OverallDifficulty = { Value = 6.2f },
                ApproachRate = { Value = 7.2f }
            };

            Assert.That(calculate(mod), Is.EqualTo(0.9 * 0.9 * 0.9 * 0.9).Within(1e-4));
        }

        [Test]
        public void TestDifficultyAdjust_LargeDifference_ClampsToMinimum()
            // 1 - 2 * 0.5 = 0 -> Max(0.1, 0) picks 0.1
            => Assert.That(calculate(new OsuModDifficultyAdjust { CircleSize = { Value = 6f } }), Is.EqualTo(0.1).Within(1e-6));

        [Test]
        public void TestDifficultyAdjust_ProductBelowMinimum_ClampsToMinimum()
            // 0.1 * 0.1 = 0.01 -> outer Max(0.1, 0.01) picks 0.1
            => Assert.That(calculate(new OsuModDifficultyAdjust { CircleSize = { Value = 6f }, OverallDifficulty = { Value = 8f } }), Is.EqualTo(0.1).Within(1e-6));

        // ---- classic / deflate / time ramp ----

        [TestCase(true, 0.985)]
        [TestCase(false, 0.96)]
        public void TestClassic_NoteLockTernary(bool noteLock, double expected)
            => Assert.That(calculate(new OsuModClassic { ClassicNoteLock = { Value = noteLock } }), Is.EqualTo(expected).Within(1e-9));

        [Test]
        public void TestDeflate_AboveDefaultScale_Penalised()
            => Assert.That(calculate(new OsuModDeflate { StartScale = { Value = 5f } }), Is.EqualTo(1 - 0.02 * 3).Within(1e-6));

        [Test]
        public void TestDeflate_BelowDefaultScale_NotRewarded()
            // Max(0, negative) -> 0
            => Assert.That(calculate(new OsuModDeflate { StartScale = { Value = 1f } }), Is.EqualTo(1).Within(1e-9));

        [Test]
        public void TestWindUp_BothRatesAtOrAboveOne_UsesDoubleTimeFormulaForBoth()
        {
            // minSpeed < 1 FALSE, maxSpeed < 1 FALSE
            var mod = new ModWindUp { InitialRate = { Value = 1.0 }, FinalRate = { Value = 1.5 } };

            Assert.That(calculate(mod), Is.EqualTo(0.8 * 1.0 + 0.2 * 1.23).Within(1e-9));
        }

        [Test]
        public void TestWindDown_MinBelowOneMaxAtOne_MixesBothFormulas()
        {
            // minSpeed < 1 TRUE (half-time formula), maxSpeed < 1 FALSE (double-time formula)
            var mod = new ModWindDown { InitialRate = { Value = 1.0 }, FinalRate = { Value = 0.75 } };

            Assert.That(calculate(mod), Is.EqualTo(0.8 * 0.55 + 0.2 * 1.0).Within(1e-9));
        }

        [Test]
        public void TestWindDown_BothBelowOne_UsesHalfTimeFormulaForBoth()
        {
            // maxSpeed < 1 TRUE
            var mod = new ModWindDown { InitialRate = { Value = 0.9 }, FinalRate = { Value = 0.75 } };

            double expected = 0.8 * 0.55 + 0.2 * ((int)(0.9 * 20) / 20.0 * 1.4 - 0.5);
            Assert.That(calculate(mod), Is.EqualTo(expected).Within(1e-9));
        }

        // ---- remaining flat/derived singles ----

        [TestCase(typeof(OsuModNoFail), 0.5)]
        [TestCase(typeof(OsuModHardRock), 1.09)]
        [TestCase(typeof(OsuModTargetPractice), 0.01)]
        [TestCase(typeof(OsuModRandom), 0.7)]
        [TestCase(typeof(OsuModRelax), 0.1)]
        [TestCase(typeof(OsuModAutopilot), 0.1)]
        [TestCase(typeof(OsuModSpunOut), 0.95)]
        [TestCase(typeof(OsuModApproachDifferent), 0.7)]
        [TestCase(typeof(ModAdaptiveSpeed), 0.1)]
        [TestCase(typeof(OsuModSynesthesia), 0.99)]
        [TestCase(typeof(OsuModMirror), 1.0)]
        public void TestFlatMultiplier(System.Type modType, double expected)
            => Assert.That(calculate((Mod)System.Activator.CreateInstance(modType)!), Is.EqualTo(expected).Within(1e-9));

        [Test]
        public void TestMagnetised_StrengthReducesMultiplier()
            => Assert.That(calculate(new OsuModMagnetised { AttractionStrength = { Value = 0.5f } }), Is.EqualTo(0.7 - 0.5 * 0.6).Within(1e-6));
    }
}
