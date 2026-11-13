// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
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
        private static OsuDifficultyAttributes attributes() => new OsuDifficultyAttributes
        {
            AimDifficulty = 3,
            AimDifficultSliderCount = 20,
            AimDifficultStrainCount = 100,
            AimTopWeightedSliderFactor = 0.5,
            SliderFactor = 0.9,
            SpeedDifficulty = 3,
            SpeedDifficultStrainCount = 100,
            SpeedTopWeightedSliderFactor = 0.5,
            ReadingDifficulty = 2,
            FlashlightDifficulty = 2,
            MaxCombo = 800,
            HitCircleCount = 400,
            SliderCount = 90,
            SpinnerCount = 10,
            LegacyScoreBaseMultiplier = 3,
            NestedScorePerObject = 20,
            MaximumLegacyComboScore = 400000,
        };

        private static ScoreInfo score(int combo = 800, int misses = 0, int oks = 0, int mehs = 0, long? legacy = null, params Mod[] mods)
        {
            int great = 500 - misses - oks - mehs;
            int total = 500;

            return new ScoreInfo
            {
                BeatmapInfo = new BeatmapInfo { Difficulty = new BeatmapDifficulty { OverallDifficulty = 8, ApproachRate = 9, DrainRate = 5 } },
                MaxCombo = combo,
                Accuracy = (great * 300.0 + oks * 100 + mehs * 50) / (total * 300.0),
                LegacyTotalScore = legacy,
                Mods = mods,
                Statistics = new Dictionary<HitResult, int>
                {
                    [HitResult.Great] = great,
                    [HitResult.Ok] = oks,
                    [HitResult.Meh] = mehs,
                    [HitResult.Miss] = misses,
                    [HitResult.SliderTailHit] = 90,
                },
            };
        }

        private static OsuPerformanceAttributes calculate(ScoreInfo s, OsuDifficultyAttributes? attr = null)
            => (OsuPerformanceAttributes)new OsuPerformanceCalculator().Calculate(s, attr ?? attributes());

        [Test]
        public void TestDifficultyToPerformance()
        {
            Assert.That(OsuPerformanceCalculator.DifficultyToPerformance(2), Is.EqualTo(32).Within(1e-9));
        }

        [Test]
        public void TestPerfectScoreHasPositiveValuesAndNoFlashlight()
        {
            var result = calculate(score());

            Assert.That(result.Total, Is.GreaterThan(0));
            Assert.That(result.Aim, Is.GreaterThan(0));
            Assert.That(result.Speed, Is.GreaterThan(0));
            Assert.That(result.Accuracy, Is.GreaterThan(0));
            Assert.That(result.Reading, Is.GreaterThan(0));
            Assert.That(result.Flashlight, Is.EqualTo(0));
            Assert.That(result.EffectiveMissCount, Is.EqualTo(0));
            Assert.That(result.ScoreBasedEstimatedMissCount, Is.Null);
        }

        // CF: OsuPerformanceCalculator.CreatePerformanceAttributes | effectiveMissCount > 0 | TRUE
        [Test]
        public void TestMissesReducePerformance()
        {
            var perfect = calculate(score());
            var missed = calculate(score(combo: 400, misses: 10));

            Assert.That(missed.Total, Is.LessThan(perfect.Total));
            Assert.That(missed.Aim, Is.LessThan(perfect.Aim));
            Assert.That(missed.Speed, Is.LessThan(perfect.Speed));
            Assert.That(missed.EffectiveMissCount, Is.GreaterThanOrEqualTo(10));
            Assert.That(missed.EffectiveMissCount, Is.LessThanOrEqualTo(500));
        }

        [Test]
        public void TestWorseAccuracyReducesAccuracyValue()
        {
            Assert.That(calculate(score(oks: 30, mehs: 10)).Accuracy, Is.LessThan(calculate(score()).Accuracy));
        }

        // CF: OsuPerformanceCalculator.computeAimValue | score.Mods.Any(h => h is OsuModAutopilot) | TRUE
        [Test]
        public void TestAutopilotHasNoAim()
        {
            Assert.That(calculate(score(mods: new Mod[] { new OsuModAutopilot() })).Aim, Is.EqualTo(0));
        }

        // CF: OsuPerformanceCalculator.computeSpeedValue | score.Mods.Any(h => h is OsuModRelax) | TRUE
        // CF: OsuPerformanceCalculator.computeAccuracyValue | score.Mods.Any(h => h is OsuModRelax) | TRUE
        [Test]
        public void TestRelaxHasNoSpeedOrAccuracy()
        {
            var result = calculate(score(mods: new Mod[] { new OsuModRelax() }));

            Assert.That(result.Speed, Is.EqualTo(0));
            Assert.That(result.Accuracy, Is.EqualTo(0));
            Assert.That(result.Aim, Is.GreaterThan(0));
        }

        // CF: OsuPerformanceCalculator.computeFlashlightValue | !score.Mods.Any(h => h is OsuModFlashlight) | FALSE
        [Test]
        public void TestFlashlightValueOnlyWithMod()
        {
            var plain = calculate(score());
            var fl = calculate(score(mods: new Mod[] { new OsuModFlashlight() }));
            var flMissed = calculate(score(combo: 400, misses: 10, mods: new Mod[] { new OsuModFlashlight() }));

            Assert.That(plain.Flashlight, Is.EqualTo(0));
            Assert.That(fl.Flashlight, Is.GreaterThan(0));
            Assert.That(flMissed.Flashlight, Is.LessThan(fl.Flashlight));
        }

        // CF: OsuPerformanceCalculator.CreatePerformanceAttributes | score.Mods.Any(m => m is OsuModNoFail) | TRUE
        [Test]
        public void TestNoFailPenaltyIsCappedAtTenPercent()
        {
            var plain = calculate(score(combo: 400, misses: 10));
            var nf = calculate(score(combo: 400, misses: 10, mods: new Mod[] { new OsuModNoFail() }));

            Assert.That(nf.Total, Is.EqualTo(plain.Total * 0.9).Within(1e-6));

            var plainFc = calculate(score());
            var nfFc = calculate(score(mods: new Mod[] { new OsuModNoFail() }));
            Assert.That(nfFc.Total, Is.EqualTo(plainFc.Total).Within(1e-9));
        }

        // CF: OsuPerformanceCalculator.CreatePerformanceAttributes | score.Mods.Any(m => m is OsuModSpunOut) && totalHits > 0 | TRUE
        [Test]
        public void TestSpunOutReducesTotal()
        {
            Assert.That(calculate(score(mods: new Mod[] { new OsuModSpunOut() })).Total, Is.LessThan(calculate(score()).Total));
        }

        // CF: OsuPerformanceCalculator.computeAimValue | score.Mods.Any(m => m is OsuModBlinds) | TRUE
        // CF: OsuPerformanceCalculator.computeAimValue | score.Mods.Any(m => m is OsuModTraceable) | TRUE
        [Test]
        public void TestVisualMods()
        {
            var plain = calculate(score());

            Assert.That(calculate(score(mods: new Mod[] { new OsuModBlinds() })).Aim, Is.GreaterThan(plain.Aim));
            Assert.That(calculate(score(mods: new Mod[] { new OsuModBlinds() })).Accuracy, Is.GreaterThan(plain.Accuracy));
            Assert.That(calculate(score(mods: new Mod[] { new OsuModTraceable() })).Aim, Is.GreaterThan(plain.Aim));
            Assert.That(calculate(score(mods: new Mod[] { new OsuModTraceable() })).Accuracy, Is.GreaterThan(plain.Accuracy));
        }

        // CF: OsuPerformanceCalculator.CreatePerformanceAttributes | usingClassicSliderAccuracy && !usingScoreV2 && score.LegacyTotalScore > 0 | TRUE
        [Test]
        public void TestClassicLegacyScoreUsesScoreBasedMissEstimate()
        {
            var result = calculate(score(combo: 400, misses: 5, legacy: 500000, mods: new Mod[] { new OsuModClassic() }));

            Assert.That(result.ScoreBasedEstimatedMissCount, Is.Not.Null);
            Assert.That(result.EffectiveMissCount, Is.GreaterThanOrEqualTo(5));
        }

        // CF: OsuPerformanceCalculator.CreatePerformanceAttributes | usingClassicSliderAccuracy && !usingScoreV2 && score.LegacyTotalScore > 0 | FALSE
        [Test]
        public void TestClassicWithoutLegacyScoreUsesComboEstimate()
        {
            Assert.That(calculate(score(combo: 400, misses: 5, mods: new Mod[] { new OsuModClassic() })).ScoreBasedEstimatedMissCount, Is.Null);
            Assert.That(calculate(score(combo: 400, misses: 5, legacy: 500000, mods: new Mod[] { new OsuModClassic(), new ModScoreV2() })).ScoreBasedEstimatedMissCount, Is.Null);
        }

        [Test]
        public void TestSpeedDeviationIsReportedForRegularScores()
        {
            Assert.That(calculate(score()).SpeedDeviation, Is.Not.Null);
        }

        [Test]
        public void TestComboScalingForPartialCombo()
        {
            var attr = attributes();
            var fl = new Mod[] { new OsuModFlashlight() };
            Assert.That(calculate(score(combo: 200, mods: fl), attr).Flashlight, Is.LessThan(calculate(score(combo: 800, mods: fl), attr).Flashlight));
        }

        [Test]
        public void TestPerformanceDisplayAttributes()
        {
            var attrs = new OsuPerformanceAttributes { Aim = 1, Speed = 2, Accuracy = 3, Flashlight = 4, Reading = 5, Total = 6 };
            var list = attrs.GetAttributesForDisplay().ToList();

            foreach (string name in new[] { "Aim", "Speed", "Accuracy", "Flashlight", "Reading" })
                Assert.That(list.Select(a => a.PropertyName), Does.Contain(name));

            Assert.That(list.Single(a => a.PropertyName == "Reading").Value, Is.EqualTo(5));
            Assert.That(list.Single(a => a.PropertyName == "Flashlight").DisplayName.ToString(), Is.EqualTo("Flashlight Bonus"));
        }
    }
}
