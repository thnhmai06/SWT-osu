// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
        private static double calculate(Mod[] mods, BeatmapDifficulty difficulty = null)
            => new OsuScoreMultiplierCalculatorV2(new ScoreMultiplierContext(difficulty ?? new BeatmapDifficulty())).CalculateFor(mods);

        private static void assertMultiplier(double expected, params Mod[] mods)
            => Assert.That(calculate(mods), Is.EqualTo(expected).Within(1e-9));

        // CF: OsuScoreMultiplierCalculatorV2.easyMultiplier | Math.Max(0, 0.1 * (Retries - Default)) | non-null
        [Test]
        public void TestEasyDefaultLives()
        {
            assertMultiplier(0.8, new OsuModEasy());
        }

        // CF: OsuScoreMultiplierCalculatorV2.easyMultiplier | Math.Max(0.4, value) | non-null
        [Test]
        public void TestEasyExtraLivesReduceMultiplier()
        {
            assertMultiplier(0.5, new OsuModEasy { Retries = { Value = 5 } });
        }

        [Test]
        public void TestEasyMultiplierIsClamped()
        {
            assertMultiplier(0.4, new OsuModEasy { Retries = { Value = 10 } });
        }

        // CF: OsuScoreMultiplierCalculatorV2.ctor | HalfTime/Daycore multiplier | non-null
        [Test]
        public void TestHalfTimeAndDaycore()
        {
            assertMultiplier(0.55, new OsuModHalfTime());
            assertMultiplier(0.55, new OsuModDaycore { SpeedChange = { Value = 0.75 } });
        }

        // CF: OsuScoreMultiplierCalculatorV2.doubleTimeMultiplier | value != 1.5 | FALSE
        [Test]
        public void TestDoubleTimeDefaultRateHasNoPenalty()
        {
            assertMultiplier(1.23, new OsuModDoubleTime());
        }

        // CF: OsuScoreMultiplierCalculatorV2.doubleTimeMultiplier | value != 1.5 | TRUE
        // CF: OsuScoreMultiplierCalculatorV2.doubleTimeMultiplier | value != 1.0 | TRUE
        [Test]
        public void TestDoubleTimeNonDefaultRateHasPenalty()
        {
            assertMultiplier(0.3 * 0.46 + 1 - 0.01, new OsuModNightcore { SpeedChange = { Value = 1.3 } });
        }

        // CF: OsuScoreMultiplierCalculatorV2.doubleTimeMultiplier | value != 1.0 | FALSE
        [Test]
        public void TestDoubleTimeAtUnitRateHasNoPenalty()
        {
            var doubleTime = new OsuModDoubleTime();
            doubleTime.SpeedChange.MinValue = 1;
            doubleTime.SpeedChange.Value = 1;

            assertMultiplier(1, doubleTime);
        }

        // CF: OsuScoreMultiplierCalculatorV2.hiddenMultiplier | hidden.OnlyFadeApproachCircles.Value | FALSE
        // CF: OsuScoreMultiplierCalculatorV2.hiddenMultiplier | otherModsProvideTimingInfo | FALSE
        [Test]
        public void TestHiddenAlone()
        {
            assertMultiplier(1.04, new OsuModHidden());
        }

        // CF: OsuScoreMultiplierCalculatorV2.hiddenMultiplier | hidden.OnlyFadeApproachCircles.Value | TRUE
        [Test]
        public void TestHiddenOnlyFadeApproachCircles()
        {
            assertMultiplier(1.02, new OsuModHidden { OnlyFadeApproachCircles = { Value = true } });
        }

        // CF: OsuScoreMultiplierCalculatorV2.hiddenMultiplier | otherModsProvideTimingInfo | TRUE
        [Test]
        public void TestHiddenWithTimingInfoMods()
        {
            assertMultiplier(1.02, new OsuModHidden(), new OsuModWiggle());
            assertMultiplier(1.02, new OsuModHidden(), new OsuModGrow());
            assertMultiplier(1.02, new OsuModHidden(), new OsuModRepel());
            assertMultiplier(1.02, new OsuModHidden(), new OsuModDepth());
            assertMultiplier(1.00, new OsuModHidden { OnlyFadeApproachCircles = { Value = true } }, new OsuModWiggle());
        }

        [Test]
        public void TestHiddenWithDeflate()
        {
            assertMultiplier(1.02, new OsuModHidden(), new OsuModDeflate());
        }

        // CF: OsuScoreMultiplierCalculatorV2.deflateMultiplier | Math.Max(0, 0.02 * (StartScale - Default)) | non-null
        [Test]
        public void TestDeflateLargerStartScaleReducesMultiplier()
        {
            assertMultiplier(1.0 - 0.02 * 3, new OsuModDeflate { StartScale = { Value = 5 } });
        }

        [Test]
        public void TestDeflateSmallerStartScaleIsClamped()
        {
            assertMultiplier(1.0, new OsuModDeflate { StartScale = { Value = 1 } });
        }

        [Test]
        public void TestBlindsCombinations()
        {
            assertMultiplier(1.24, new OsuModBlinds());
            assertMultiplier(1.24, new OsuModHidden(), new OsuModBlinds());
            assertMultiplier(1.24, new OsuModTraceable(), new OsuModBlinds());
            assertMultiplier(1.02, new OsuModTraceable());
        }

        // CF: OsuScoreMultiplierCalculatorV2.flashlightMultiplier | !flashlight.ComboBasedSize.Value | FALSE
        [Test]
        public void TestFlashlightComboBasedSize()
        {
            assertMultiplier(1.2, new OsuModFlashlight());
        }

        // CF: OsuScoreMultiplierCalculatorV2.flashlightMultiplier | !flashlight.ComboBasedSize.Value | TRUE
        [Test]
        public void TestFlashlightFixedSize()
        {
            assertMultiplier(1.04, new OsuModFlashlight { ComboBasedSize = { Value = false } });
        }

        [Test]
        public void TestFlashlightLargeSizeIsClampedToMinimum()
        {
            assertMultiplier(1.02, new OsuModFlashlight { SizeMultiplier = { Value = 2 } });
        }

        [Test]
        public void TestFlashlightSmallSizeIsClampedToMaximum()
        {
            assertMultiplier(1.2, new OsuModFlashlight { SizeMultiplier = { Value = 0.5f } });
        }

        [Test]
        public void TestFlashlightWithFreezeFrameHalvesBonus()
        {
            assertMultiplier(1.1, new OsuModFlashlight(), new OsuModFreezeFrame());
        }

        // CF: OsuScoreMultiplierCalculatorV2.difficultyAdjustMultiplier | difficultyAdjust.CircleSize.Value ?? beatmapDifficulty.CircleSize | null
        // CF: OsuScoreMultiplierCalculatorV2.difficultyAdjustMultiplier | difficultyAdjust.DrainRate.Value ?? beatmapDifficulty.DrainRate | null
        // CF: OsuScoreMultiplierCalculatorV2.difficultyAdjustMultiplier | difficultyAdjust.OverallDifficulty.Value ?? beatmapDifficulty.OverallDifficulty | null
        // CF: OsuScoreMultiplierCalculatorV2.difficultyAdjustMultiplier | difficultyAdjust.ApproachRate.Value ?? beatmapDifficulty.ApproachRate | null
        [Test]
        public void TestDifficultyAdjustWithoutOverrides()
        {
            Assert.That(calculate(new Mod[] { new OsuModDifficultyAdjust() }), Is.EqualTo(1).Within(1e-9));
        }

        // CF: OsuScoreMultiplierCalculatorV2.difficultyAdjustMultiplier | difficultyAdjust.CircleSize.Value ?? beatmapDifficulty.CircleSize | non-null
        // CF: OsuScoreMultiplierCalculatorV2.difficultyAdjustMultiplier | difficultyAdjust.DrainRate.Value ?? beatmapDifficulty.DrainRate | non-null
        // CF: OsuScoreMultiplierCalculatorV2.difficultyAdjustMultiplier | difficultyAdjust.OverallDifficulty.Value ?? beatmapDifficulty.OverallDifficulty | non-null
        // CF: OsuScoreMultiplierCalculatorV2.difficultyAdjustMultiplier | difficultyAdjust.ApproachRate.Value ?? beatmapDifficulty.ApproachRate | non-null
        [Test]
        public void TestDifficultyAdjustWithOverrides()
        {
            var difficulty = new BeatmapDifficulty { CircleSize = 4, DrainRate = 4, OverallDifficulty = 4, ApproachRate = 4 };
            var mod = new OsuModDifficultyAdjust
            {
                CircleSize = { Value = 4.5f },
                DrainRate = { Value = 4.5f },
                OverallDifficulty = { Value = 4.5f },
                ApproachRate = { Value = 4.5f },
            };

            // each parameter differs by 0.5 -> 0.75x each
            Assert.That(calculate(new Mod[] { mod }, difficulty), Is.EqualTo(Math.Pow(0.75, 4)).Within(1e-9));
        }

        [Test]
        public void TestDifficultyAdjustMultiplierIsClamped()
        {
            var difficulty = new BeatmapDifficulty { CircleSize = 0, DrainRate = 0, OverallDifficulty = 0, ApproachRate = 0 };
            var mod = new OsuModDifficultyAdjust
            {
                CircleSize = { Value = 10 },
                DrainRate = { Value = 10 },
                OverallDifficulty = { Value = 10 },
                ApproachRate = { Value = 10 },
            };

            // each parameter is clamped to 0.1x, then the product is clamped to 0.1x
            Assert.That(calculate(new Mod[] { mod }, difficulty), Is.EqualTo(0.1).Within(1e-9));
        }

        // CF: OsuScoreMultiplierCalculatorV2.ctor | classic.ClassicNoteLock.Value | TRUE
        [Test]
        public void TestClassicWithNoteLock()
        {
            assertMultiplier(0.985, new OsuModClassic { ClassicNoteLock = { Value = true } });
        }

        // CF: OsuScoreMultiplierCalculatorV2.ctor | classic.ClassicNoteLock.Value | FALSE
        [Test]
        public void TestClassicWithoutNoteLock()
        {
            assertMultiplier(0.96, new OsuModClassic { ClassicNoteLock = { Value = false } });
        }

        // CF: OsuScoreMultiplierCalculatorV2.timeRampMultiplier | minSpeed < 1 | TRUE
        // CF: OsuScoreMultiplierCalculatorV2.timeRampMultiplier | maxSpeed < 1 | FALSE
        [Test]
        public void TestWindUpAcrossUnitRate()
        {
            // 0.8 * halfTime(0.5) + 0.2 * doubleTime(2.0)
            double expected = 0.8 * ((int)(0.5 * 20) / 20.0 * 1.4 - 0.5) + 0.2 * ((2.0 - 1) * 0.46 + 1 - 0.01);
            assertMultiplier(expected, new ModWindUp { InitialRate = { Value = 0.5 }, FinalRate = { Value = 2.0 } });
        }

        // CF: OsuScoreMultiplierCalculatorV2.timeRampMultiplier | minSpeed < 1 | TRUE
        // CF: OsuScoreMultiplierCalculatorV2.timeRampMultiplier | maxSpeed < 1 | TRUE
        [Test]
        public void TestWindDownBelowUnitRate()
        {
            double expected = 0.8 * ((int)(0.5 * 20) / 20.0 * 1.4 - 0.5) + 0.2 * ((int)(0.9 * 20) / 20.0 * 1.4 - 0.5);
            assertMultiplier(expected, new ModWindDown { InitialRate = { Value = 0.9 }, FinalRate = { Value = 0.5 } });
        }

        // CF: OsuScoreMultiplierCalculatorV2.timeRampMultiplier | minSpeed < 1 | FALSE
        [Test]
        public void TestWindUpAboveUnitRate()
        {
            double expected = 0.8 * ((1.2 - 1) * 0.46 + 1 - 0.01) + 0.2 * ((1.8 - 1) * 0.46 + 1 - 0.01);
            assertMultiplier(expected, new ModWindUp { InitialRate = { Value = 1.2 }, FinalRate = { Value = 1.8 } });
        }

        [Test]
        public void TestMagnetisedAndFlatMultipliers()
        {
            assertMultiplier(0.7 - 0.5 * 0.6, new OsuModMagnetised { AttractionStrength = { Value = 0.5f } });
            assertMultiplier(1.09, new OsuModHardRock());
            assertMultiplier(0.7, new OsuModRandom());
            assertMultiplier(1, new OsuModMirror());
        }
    }
}
