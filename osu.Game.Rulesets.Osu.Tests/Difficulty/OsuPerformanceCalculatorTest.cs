// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Osu.Tests.Difficulty
{
    [TestFixture]
    public class OsuPerformanceCalculatorTest
    {
        private static OsuDifficultyAttributes attributes(int hitCircles = 80, int sliders = 20, int spinners = 0, int maxCombo = 500, double sliderFactor = 0.9, double difficultSliders = 10)
            => new OsuDifficultyAttributes
            {
                MaxCombo = maxCombo,
                HitCircleCount = hitCircles,
                SliderCount = sliders,
                SpinnerCount = spinners,
                AimDifficulty = 3,
                SpeedDifficulty = 3,
                SpeedNoteCount = 100,
                FlashlightDifficulty = 2,
                ReadingDifficulty = 2,
                SliderFactor = sliderFactor,
                AimDifficultSliderCount = difficultSliders,
                AimDifficultStrainCount = 50,
                SpeedDifficultStrainCount = 50,
                ReadingDifficultNoteCount = 50,
                AimTopWeightedSliderFactor = 0.5,
                SpeedTopWeightedSliderFactor = 0.5,
            };

        private static ScoreInfo score(int great = 100, int ok = 0, int meh = 0, int miss = 0, int sliderTails = 20, int tickMiss = 0,
                                       int maxCombo = 500, double accuracy = 1, float ar = 5, float od = 5, float hp = 5, params Mod[] mods) => new ScoreInfo
        {
            Mods = mods,
            MaxCombo = maxCombo,
            Accuracy = accuracy,
            BeatmapInfo = new BeatmapInfo { Difficulty = new BeatmapDifficulty { ApproachRate = ar, OverallDifficulty = od, DrainRate = hp } },
            Statistics = new Dictionary<HitResult, int>
            {
                [HitResult.Great] = great,
                [HitResult.Ok] = ok,
                [HitResult.Meh] = meh,
                [HitResult.Miss] = miss,
                [HitResult.SliderTailHit] = sliderTails,
                [HitResult.LargeTickMiss] = tickMiss,
            },
        };

        private static OsuPerformanceAttributes calculate(ScoreInfo score, OsuDifficultyAttributes? attributes = null)
            => (OsuPerformanceAttributes)new OsuPerformanceCalculator().Calculate(score, attributes ?? OsuPerformanceCalculatorTest.attributes());

        // ---- baseline ----

        [Test]
        public void TestFullComboScore_HasAllComponentsAndNoMisses()
        {
            var result = calculate(score());

            Assert.That(result.EffectiveMissCount, Is.EqualTo(0));
            Assert.That(result.Aim, Is.GreaterThan(0));
            Assert.That(result.Speed, Is.GreaterThan(0));
            Assert.That(result.Accuracy, Is.GreaterThan(0));
            Assert.That(result.Reading, Is.GreaterThan(0));
            Assert.That(result.Total, Is.GreaterThan(Math.Max(result.Aim, result.Speed)));
            Assert.That(result.AimEstimatedSliderBreaks, Is.EqualTo(0));
        }

        [Test]
        public void TestSameInput_GivesSameOutput()
        {
            Assert.That(calculate(score(great: 95, miss: 5, maxCombo: 300, accuracy: 0.95)).Total, Is.EqualTo(calculate(score(great: 95, miss: 5, maxCombo: 300, accuracy: 0.95)).Total));
        }

        [Test]
        public void TestMoreMisses_NeverIncreasesPerformance()
        {
            double clean = calculate(score(great: 100)).Total;
            double fewMisses = calculate(score(great: 95, miss: 5, maxCombo: 300, accuracy: 0.95)).Total;
            double manyMisses = calculate(score(great: 70, miss: 30, maxCombo: 100, accuracy: 0.7)).Total;

            Assert.That(fewMisses, Is.LessThan(clean));
            Assert.That(manyMisses, Is.LessThan(fewMisses));
        }

        [Test]
        public void TestDifficultyToPerformance_IsMonotonicAndZeroAtZero()
        {
            Assert.That(OsuPerformanceCalculator.DifficultyToPerformance(0), Is.EqualTo(0));
            Assert.That(OsuPerformanceCalculator.DifficultyToPerformance(5), Is.GreaterThan(OsuPerformanceCalculator.DifficultyToPerformance(4)));
        }

        // ---- input clamps ----

        [Test]
        public void TestAccuracyAboveOne_IsClampedToOne()
            => Assert.That(calculate(score(accuracy: 1.5)).Total, Is.EqualTo(calculate(score(accuracy: 1)).Total));

        [Test]
        public void TestNegativeAccuracy_IsClampedToZero_AndZeroesAimAndReading()
        {
            var result = calculate(score(accuracy: -0.5));

            Assert.That(result.Aim, Is.EqualTo(0));
            Assert.That(result.Reading, Is.EqualTo(0));
        }

        [Test]
        public void TestMaxComboAboveMaximum_IsClamped()
            => Assert.That(calculate(score(maxCombo: 9999)).Total, Is.EqualTo(calculate(score(maxCombo: 500)).Total));

        [Test]
        public void TestNegativeMaxCombo_IsClampedToZero()
            => Assert.That(calculate(score(great: 90, miss: 10, maxCombo: -5)).Total, Is.EqualTo(calculate(score(great: 90, miss: 10, maxCombo: 0)).Total));

        [Test]
        public void TestAccuracyScalesAimLinearlyAndReadingCubically()
        {
            var full = calculate(score());
            var reduced = calculate(score(accuracy: 0.9));

            Assert.That(reduced.Aim / full.Aim, Is.EqualTo(0.9).Within(1e-9));
            Assert.That(reduced.Reading / full.Reading, Is.EqualTo(0.9 * 0.9 * 0.9).Within(1e-9));
        }

        // ---- mod guards ----

        [Test]
        public void TestFlashlight_AbsentGivesZero_PresentGivesPositive()
        {
            Assert.That(calculate(score()).Flashlight, Is.EqualTo(0));
            Assert.That(calculate(score(mods: new OsuModFlashlight())).Flashlight, Is.GreaterThan(0));
        }

        [Test]
        public void TestFlashlight_ScalesSlightlyWithAccuracy()
        {
            double full = calculate(score(mods: new OsuModFlashlight())).Flashlight;
            double reduced = calculate(score(accuracy: 0.9, mods: new OsuModFlashlight())).Flashlight;

            Assert.That(reduced / full, Is.EqualTo(0.5 + 0.9 / 2).Within(1e-9));
        }

        [Test]
        public void TestFlashlight_MissesAndLostComboReduceValue()
        {
            double clean = calculate(score(mods: new OsuModFlashlight())).Flashlight;
            double missed = calculate(score(great: 95, miss: 5, maxCombo: 500, mods: new OsuModFlashlight())).Flashlight;
            double brokenCombo = calculate(score(maxCombo: 250, mods: new OsuModFlashlight())).Flashlight;

            Assert.That(missed, Is.LessThan(clean));
            Assert.That(brokenCombo, Is.LessThan(clean));
        }

        [Test]
        public void TestFlashlight_ZeroMaxComboAttribute_SkipsComboScaling()
        {
            // getComboScalingFactor: MaxCombo <= 0 => 1.0
            var result = calculate(score(maxCombo: 0, mods: new OsuModFlashlight()), attributes(maxCombo: 0));

            Assert.That(double.IsNaN(result.Flashlight), Is.False);
            Assert.That(result.Flashlight, Is.GreaterThan(0));
        }

        [Test]
        public void TestAutopilot_ZeroesAimOnly()
        {
            var result = calculate(score(mods: new OsuModAutopilot()));

            Assert.That(result.Aim, Is.EqualTo(0));
            Assert.That(result.Speed, Is.GreaterThan(0));
            Assert.That(result.Accuracy, Is.GreaterThan(0));
        }

        [Test]
        public void TestRelax_ZeroesSpeedAndAccuracyOnly()
        {
            var result = calculate(score(mods: new OsuModRelax()));

            Assert.That(result.Speed, Is.EqualTo(0));
            Assert.That(result.Accuracy, Is.EqualTo(0));
            Assert.That(result.Aim, Is.GreaterThan(0));
        }

        [Test]
        public void TestRelax_CountsOksAndMehsAsMisses_ZeroOd()
        {
            // OD 0 => overallDifficulty == 0 => the `> 0` ternary falls back to 1.0: oks count as 0.75, mehs as 1
            var result = calculate(score(great: 90, ok: 8, meh: 2, od: 0, mods: new OsuModRelax()), attributes(hitCircles: 100, sliders: 0));

            Assert.That(result.EffectiveMissCount, Is.EqualTo(8 * 0.75 + 2).Within(1e-9));
        }

        [Test]
        public void TestRelax_HigherOd_CountsOksAndMehsLess()
        {
            // overallDifficulty > 0 TRUE: the multipliers shrink as OD grows
            var low = calculate(score(great: 90, ok: 8, meh: 2, od: 0, mods: new OsuModRelax()), attributes(hitCircles: 100, sliders: 0));
            var high = calculate(score(great: 90, ok: 8, meh: 2, od: 10, mods: new OsuModRelax()), attributes(hitCircles: 100, sliders: 0));

            Assert.That(high.EffectiveMissCount, Is.LessThan(low.EffectiveMissCount));
            Assert.That(high.EffectiveMissCount, Is.GreaterThan(0));
        }

        [Test]
        public void TestNoFail_PenaltyScalesWithMissesAndFloors()
        {
            var noSliders = attributes(hitCircles: 100, sliders: 0);

            double oneMissRatio = calculate(score(great: 99, miss: 1, sliderTails: 0, mods: new OsuModNoFail()), noSliders).Total / calculate(score(great: 99, miss: 1, sliderTails: 0), noSliders).Total;
            double manyMissRatio = calculate(score(great: 90, miss: 10, sliderTails: 0, mods: new OsuModNoFail()), noSliders).Total / calculate(score(great: 90, miss: 10, sliderTails: 0), noSliders).Total;

            Assert.That(oneMissRatio, Is.EqualTo(0.98).Within(1e-9));
            Assert.That(manyMissRatio, Is.EqualTo(0.90).Within(1e-9));
        }

        [Test]
        public void TestSpunOut_ReducesTotalByTheSpinnerShare()
        {
            var plain = calculate(score(), attributes(spinners: 10)).Total;
            var spunOut = calculate(score(mods: new OsuModSpunOut()), attributes(spinners: 10)).Total;
            var moreSpinners = calculate(score(mods: new OsuModSpunOut()), attributes(spinners: 30)).Total;

            Assert.That(spunOut, Is.LessThan(plain));
            Assert.That(moreSpinners, Is.LessThan(spunOut));
        }

        [Test]
        public void TestNoHits_DoesNotDivideByZero()
        {
            // totalHits == 0: SpunOut guard, no speed deviation
            var result = calculate(score(great: 0, sliderTails: 0, maxCombo: 0, mods: new OsuModSpunOut()), attributes(spinners: 10));

            Assert.That(double.IsNaN(result.Total), Is.False);
            Assert.That(result.Speed, Is.EqualTo(0));
            Assert.That(result.SpeedDeviation, Is.Null);
        }

        // ---- Blinds / Traceable bonuses ----

        [Test]
        public void TestBlinds_AppliesFixedSpeedAndAccuracyBonuses_AndBoostsAim()
        {
            var plain = calculate(score());
            var blinds = calculate(score(mods: new OsuModBlinds()));

            Assert.That(blinds.Speed / plain.Speed, Is.EqualTo(1.12).Within(1e-9));
            Assert.That(blinds.Accuracy / plain.Accuracy, Is.EqualTo(1.14).Within(1e-9));
            Assert.That(blinds.Aim, Is.GreaterThan(plain.Aim));
        }

        [Test]
        public void TestTraceable_RewardsLowerApproachRateMore()
        {
            double high = calculate(score(ar: 10, mods: new OsuModTraceable())).Aim;
            double low = calculate(score(ar: 5, mods: new OsuModTraceable())).Aim;
            double veryLow = calculate(score(ar: -2, mods: new OsuModTraceable())).Aim;
            double plain = calculate(score(ar: 10)).Aim;

            Assert.That(high, Is.GreaterThan(plain));
            Assert.That(low, Is.GreaterThan(high));
            Assert.That(veryLow, Is.GreaterThan(low));
        }

        [Test]
        public void TestTraceable_AccuracyBonusFadesAtHighApproachRate()
        {
            double lowAr = calculate(score(ar: 8, mods: new OsuModTraceable())).Accuracy / calculate(score(ar: 8)).Accuracy;
            double highAr = calculate(score(ar: 11, mods: new OsuModTraceable())).Accuracy / calculate(score(ar: 11)).Accuracy;

            Assert.That(lowAr, Is.GreaterThan(1));
            Assert.That(highAr, Is.LessThan(lowAr));
        }

        // ---- slider handling in aim ----

        [Test]
        public void TestDroppedDifficultSliders_ReduceAimDownToSliderFactor()
        {
            // non-classic: all slider ends dropped (clamped to difficult slider count) => factor == SliderFactor
            double full = calculate(score(sliderTails: 20)).Aim;
            double dropped = calculate(score(sliderTails: 0)).Aim;

            Assert.That(dropped / full, Is.EqualTo(0.9).Within(1e-9));
        }

        [Test]
        public void TestSliderNerf_NotAppliedWithoutDifficultSliders()
        {
            // AimDifficultSliderCount > 0 FALSE
            Assert.That(calculate(score(sliderTails: 0), attributes(difficultSliders: 0)).Aim, Is.EqualTo(calculate(score(sliderTails: 20), attributes(difficultSliders: 0)).Aim));
        }

        [Test]
        public void TestSliderNerf_NotAppliedWithoutSliders()
        {
            // SliderCount > 0 FALSE
            var noSliders = attributes(hitCircles: 100, sliders: 0);

            Assert.That(calculate(score(sliderTails: 0), noSliders).Aim, Is.EqualTo(calculate(score(sliderTails: 20), noSliders).Aim));
        }

        [Test]
        public void TestClassicScore_LostComboCountsAsDroppedSliders()
        {
            // classic: MaxCombo - scoreMaxCombo (10 lost, 10 imperfect hits) is treated as dropped difficult sliders
            double intact = calculate(score(mods: new OsuModClassic())).Aim;
            double dropped = calculate(score(great: 90, ok: 10, maxCombo: 490, accuracy: 1, mods: new OsuModClassic())).Aim;

            Assert.That(dropped, Is.LessThan(intact));
        }

        [Test]
        public void TestLongerMaps_GetLargerLengthBonus()
        {
            double shortMap = calculate(score(great: 1000), attributes(hitCircles: 1000, sliders: 0, maxCombo: 1000)).Aim;
            double longMap = calculate(score(great: 3000), attributes(hitCircles: 3000, sliders: 0, maxCombo: 3000)).Aim;

            Assert.That(longMap, Is.GreaterThan(shortMap));
        }

        // ---- estimated misses ----

        [Test]
        public void TestNoSliders_ComboBasedEstimateEqualsRealMisses()
        {
            // SliderCount <= 0 => returns countMiss
            var result = calculate(score(great: 97, miss: 3, sliderTails: 0, maxCombo: 100), attributes(hitCircles: 100, sliders: 0));

            Assert.That(result.ComboBasedEstimatedMissCount, Is.EqualTo(3));
            Assert.That(result.EffectiveMissCount, Is.EqualTo(3));
        }

        [Test]
        public void TestLostComboWithTickMisses_EstimatesSliderBreaks()
        {
            // non-classic: threshold 500, combo 300 => 500/300, limited by tick misses + misses (3)
            var result = calculate(score(tickMiss: 3, maxCombo: 300));

            Assert.That(result.ComboBasedEstimatedMissCount, Is.EqualTo(500.0 / 300).Within(1e-9));
            Assert.That(result.EffectiveMissCount, Is.EqualTo(500.0 / 300).Within(1e-9));
        }

        [Test]
        public void TestLostComboWithoutTickMisses_EstimateCappedAtZero()
        {
            // Math.Min(missCount, tickMiss + miss) == 0
            var result = calculate(score(maxCombo: 300));

            Assert.That(result.ComboBasedEstimatedMissCount, Is.EqualTo(0));
        }

        [Test]
        public void TestFullComboThreshold_UsesDroppedSliderEnds()
        {
            // combo 490, 20 dropped ends: threshold 480 <= 490 => no estimated breaks even with tick misses
            var result = calculate(score(sliderTails: 0, tickMiss: 3, maxCombo: 490));

            Assert.That(result.ComboBasedEstimatedMissCount, Is.EqualTo(0));
        }

        [Test]
        public void TestClassicEstimate_NeverExceedsImperfectHits()
        {
            var result = calculate(score(great: 90, ok: 10, maxCombo: 100, mods: new OsuModClassic()));

            Assert.That(result.ComboBasedEstimatedMissCount, Is.GreaterThan(0));
            Assert.That(result.ComboBasedEstimatedMissCount, Is.LessThanOrEqualTo(10));
        }

        [Test]
        public void TestClassicEstimate_FullComboHasNoEstimatedMisses()
        {
            Assert.That(calculate(score(mods: new OsuModClassic())).ComboBasedEstimatedMissCount, Is.EqualTo(0));
        }

        [Test]
        public void TestClassicEstimate_ComboWithinFullComboThreshold_HasNoEstimatedMisses()
        {
            // 499 >= the classic full-combo threshold (~494.9) so scoreMaxCombo < threshold is FALSE
            var result = calculate(score(great: 90, ok: 10, maxCombo: 499, mods: new OsuModClassic()));

            Assert.That(result.ComboBasedEstimatedMissCount, Is.EqualTo(0));
        }

        [Test]
        public void TestLegacyScore_UsesScoreBasedEstimate_OnlyWhenClassicWithoutScoreV2()
        {
            var withLegacy = score(great: 90, miss: 10, maxCombo: 250, accuracy: 0.9, mods: new OsuModClassic());
            withLegacy.LegacyTotalScore = 100_000;

            var scoreV2 = score(great: 90, miss: 10, maxCombo: 250, accuracy: 0.9, mods: new Mod[] { new OsuModClassic(), new ModScoreV2() });
            scoreV2.LegacyTotalScore = 100_000;

            var noLegacy = score(great: 90, miss: 10, maxCombo: 250, accuracy: 0.9, mods: new OsuModClassic());

            var attr = attributes();
            attr.LegacyScoreBaseMultiplier = 3;
            attr.MaximumLegacyComboScore = 1_000_000;
            attr.NestedScorePerObject = 20;

            Assert.That(calculate(withLegacy, attr).ScoreBasedEstimatedMissCount, Is.Not.Null);
            Assert.That(calculate(scoreV2, attr).ScoreBasedEstimatedMissCount, Is.Null);
            Assert.That(calculate(noLegacy, attr).ScoreBasedEstimatedMissCount, Is.Null);
        }

        [Test]
        public void TestNonClassicScore_NeverUsesScoreBasedEstimate()
        {
            var s = score(great: 90, miss: 10, maxCombo: 250);
            s.LegacyTotalScore = 100_000;

            Assert.That(calculate(s).ScoreBasedEstimatedMissCount, Is.Null);
        }

        [Test]
        public void TestEffectiveMissCount_IsBoundedByRealMissesAndTotalHits()
        {
            var result = calculate(score(great: 90, miss: 10, maxCombo: 50));

            Assert.That(result.EffectiveMissCount, Is.GreaterThanOrEqualTo(10));
            Assert.That(result.EffectiveMissCount, Is.LessThanOrEqualTo(100));
        }

        [Test]
        public void TestSliderBreakEstimates_OnlyForClassicScoresWithMistakesAndMisses()
        {
            // classic with oks and an effective miss count >= 2
            var classic = calculate(score(great: 88, ok: 10, miss: 2, maxCombo: 300, mods: new OsuModClassic()));
            // non-classic: always 0
            var modern = calculate(score(great: 88, ok: 10, miss: 2, maxCombo: 300));
            // classic but without oks/mehs: nonMissMistakes == 0
            var noMistakes = calculate(score(great: 98, miss: 2, maxCombo: 300, mods: new OsuModClassic()));

            Assert.That(classic.AimEstimatedSliderBreaks, Is.GreaterThan(0));
            Assert.That(classic.AimEstimatedSliderBreaks, Is.LessThanOrEqualTo(10));
            Assert.That(modern.AimEstimatedSliderBreaks, Is.EqualTo(0));
            Assert.That(noMistakes.AimEstimatedSliderBreaks, Is.EqualTo(0));
        }

        // ---- speed deviation ----

        [Test]
        public void TestSpeedDeviation_NullWithoutSuccessfulHits()
        {
            var result = calculate(score(great: 0, miss: 100, sliderTails: 0, accuracy: 0));

            Assert.That(result.SpeedDeviation, Is.Null);
            Assert.That(result.Speed, Is.EqualTo(0));
        }

        [Test]
        public void TestSpeedDeviation_NullWhenAllRelevantNotesWereMissed()
        {
            // relevant great + ok + meh <= 0 => null even though some hit succeeded elsewhere
            var result = calculate(score(great: 1, miss: 100, sliderTails: 0), withSpeedNotes(attributes(hitCircles: 101, sliders: 0), 0));

            Assert.That(result.SpeedDeviation, Is.Null);
        }

        private static OsuDifficultyAttributes withSpeedNotes(OsuDifficultyAttributes attributes, double count)
        {
            attributes.SpeedNoteCount = count;
            return attributes;
        }

        [Test]
        public void TestSpeedDeviation_OnlyOks_UsesOkWindowOverRootThree()
        {
            // pLowerBound <= 0.01 branch: deviation = okHitWindow / sqrt(3); OD 5 => ok window 99.5ms
            var result = calculate(score(great: 0, ok: 100, od: 5, sliderTails: 0), attributes(hitCircles: 100, sliders: 0));

            Assert.That(result.SpeedDeviation, Is.EqualTo(99.5 / Math.Sqrt(3)).Within(1e-6));
        }

        [Test]
        public void TestSpeedDeviation_GrowsWithWorseJudgements()
        {
            var noSliders = attributes(hitCircles: 100, sliders: 0);

            double allGreat = calculate(score(sliderTails: 0), noSliders).SpeedDeviation!.Value;
            double someOks = calculate(score(great: 80, ok: 20, sliderTails: 0), noSliders).SpeedDeviation!.Value;
            double withMehs = calculate(score(great: 80, ok: 10, meh: 10, sliderTails: 0), noSliders).SpeedDeviation!.Value;

            Assert.That(someOks, Is.GreaterThan(allGreat));
            Assert.That(withMehs, Is.GreaterThan(someOks));
        }

        [Test]
        public void TestHighDeviation_ReducesSpeedValue()
        {
            // calculateSpeedHighDeviationNerf: a very sloppy score is nerfed beyond what accuracy scaling does
            var hard = attributes(hitCircles: 100, sliders: 0);
            hard.SpeedDifficulty = 9;

            double tidy = calculate(score(sliderTails: 0), hard).Speed;
            double sloppy = calculate(score(great: 20, ok: 70, meh: 10, sliderTails: 0, accuracy: 0.6), hard).Speed;

            Assert.That(sloppy, Is.LessThan(tidy));
        }

        // ---- accuracy value ----

        [Test]
        public void TestAccuracyValue_NoHitObjectsWithAccuracy_IsZero()
        {
            Assert.That(calculate(score(sliderTails: 0), attributes(hitCircles: 0, sliders: 0)).Accuracy, Is.EqualTo(0));
        }

        [Test]
        public void TestAccuracyValue_NegativeProportion_IsClampedToZero()
        {
            // far more judged hits than objects with accuracy => negative proportion => clamped to 0
            var result = calculate(score(great: 10, miss: 200, sliderTails: 0, accuracy: 0.05), attributes(hitCircles: 80, sliders: 0));

            Assert.That(result.Accuracy, Is.EqualTo(0));
        }

        [Test]
        public void TestAccuracyValue_GrowsWithObjectCount_AcrossThousandBoundary()
        {
            double Accuracy(int count) => calculate(score(great: count, sliderTails: 0, maxCombo: count), attributes(hitCircles: count, sliders: 0, maxCombo: count)).Accuracy;

            Assert.That(Accuracy(500), Is.LessThan(Accuracy(1000)));
            Assert.That(Accuracy(1000), Is.LessThan(Accuracy(2000)));
        }

        [Test]
        public void TestAccuracyValue_ClassicIgnoresSlidersUnlessScoreV2()
        {
            double classic = calculate(score(mods: new OsuModClassic())).Accuracy;
            double classicV2 = calculate(score(mods: new Mod[] { new OsuModClassic(), new ModScoreV2() })).Accuracy;
            double modern = calculate(score()).Accuracy;

            // 100 accuracy-relevant objects (V2 or non-classic) beat 80 (classic)
            Assert.That(classicV2, Is.GreaterThan(classic));
            Assert.That(modern, Is.EqualTo(classicV2).Within(1e-9));
        }

        [Test]
        public void TestAccuracyValue_BetterHitWindowsAndRateRaiseIt()
        {
            double normal = calculate(score()).Accuracy;
            double hardRock = calculate(score(mods: new OsuModHardRock())).Accuracy;
            double doubleTime = calculate(score(mods: new OsuModDoubleTime())).Accuracy;

            Assert.That(hardRock, Is.GreaterThan(normal));
            Assert.That(doubleTime, Is.GreaterThan(normal));
        }

        // ---- reading / misses ----

        [Test]
        public void TestMisses_ReduceReadingAndAimAndSpeed()
        {
            var clean = calculate(score());
            var missed = calculate(score(great: 90, miss: 10, maxCombo: 250));

            Assert.That(missed.Reading, Is.LessThan(clean.Reading));
            Assert.That(missed.Aim, Is.LessThan(clean.Aim));
            Assert.That(missed.Speed, Is.LessThan(clean.Speed));
        }
    }
}
