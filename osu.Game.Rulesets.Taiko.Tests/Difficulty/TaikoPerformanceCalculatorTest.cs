// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko.Difficulty;
using osu.Game.Rulesets.Taiko.Mods;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Taiko.Tests.Difficulty
{
    [TestFixture]
    public class TaikoPerformanceCalculatorTest
    {
        private class SetOverallDifficultyMod : Mod, IApplicableToDifficulty
        {
            private readonly float od;

            public SetOverallDifficultyMod(float od) => this.od = od;

            public override string Name => "OD";
            public override string Acronym => "OD";
            public override LocalisableString Description => "";
            public override double ScoreMultiplier => 1;

            public void ApplyToDifficulty(BeatmapDifficulty difficulty) => difficulty.OverallDifficulty = od;
        }

        private static TaikoDifficultyAttributes attributes(double consistency = 1, double mono = 0) => new TaikoDifficultyAttributes
        {
            StarRating = 5,
            RhythmDifficulty = 1,
            ConsistencyFactor = consistency,
            MonoStaminaFactor = mono,
        };

        private static TaikoPerformanceAttributes calc(int greats, int misses = 0, bool convert = false, TaikoDifficultyAttributes? attrs = null, params Mod[] mods)
        {
            var ruleset = new TaikoRuleset().RulesetInfo;
            var score = new ScoreInfo
            {
                BeatmapInfo = new BeatmapInfo(convert ? new RulesetInfo { OnlineID = 0, ShortName = "osu", Name = "osu!" } : ruleset, new BeatmapDifficulty { OverallDifficulty = 5 }),
                Ruleset = ruleset,
                Mods = mods,
                Statistics = new Dictionary<HitResult, int> { [HitResult.Great] = greats, [HitResult.Miss] = misses },
            };

            return (TaikoPerformanceAttributes)new TaikoPerformanceCalculator().Calculate(score, attrs ?? attributes());
        }

        // CF: TaikoPerformanceCalculator.CreatePerformanceAttributes | countGreat == 0 | TRUE
        // CF: TaikoPerformanceCalculator.computeDifficultyValue | estimatedUnstableRate == null | TRUE
        // CF: TaikoPerformanceCalculator.computeAccuracyValue | estimatedUnstableRate == null | TRUE
        [Test]
        public void TestNoGreatsGivesNoPerformance()
        {
            var result = calc(0, 10);
            Assert.That(result.EstimatedUnstableRate, Is.Null);
            Assert.That(result.Difficulty, Is.EqualTo(0));
            Assert.That(result.Accuracy, Is.EqualTo(0));
            Assert.That(result.Total, Is.EqualTo(0));
        }

        // CF: TaikoPerformanceCalculator.CreatePerformanceAttributes | countGreat == 0 | FALSE
        // CF: TaikoPerformanceCalculator.CreatePerformanceAttributes | greatHitWindow <= 0 | FALSE
        // CF: TaikoPerformanceCalculator.computeDifficultyValue | estimatedUnstableRate == null | FALSE
        // CF: TaikoPerformanceCalculator.computeDifficultyValue | totalDifficultHits == 0 | FALSE
        // CF: TaikoPerformanceCalculator.computeAccuracyValue | greatHitWindow <= 0 | FALSE
        [Test]
        public void TestPerfectPlayHasPositivePerformance()
        {
            var result = calc(1000);
            Assert.That(result.EstimatedUnstableRate, Is.Not.Null.And.GreaterThan(0));
            Assert.That(result.Difficulty, Is.GreaterThan(0));
            Assert.That(result.Accuracy, Is.GreaterThan(0));
            Assert.That(result.Total, Is.EqualTo(result.Difficulty + result.Accuracy));
        }

        // CF: TaikoPerformanceCalculator.CreatePerformanceAttributes | greatHitWindow <= 0 | TRUE
        // CF: TaikoPerformanceCalculator.computeAccuracyValue | greatHitWindow <= 0 | TRUE
        [Test]
        public void TestNonPositiveHitWindowGivesNoPerformance()
        {
            var result = calc(1000, mods: new SetOverallDifficultyMod(1000));
            Assert.That(result.EstimatedUnstableRate, Is.Null);
            Assert.That(result.Total, Is.EqualTo(0));
        }

        // CF: TaikoPerformanceCalculator.computeDifficultyValue | totalDifficultHits == 0 | TRUE
        [Test]
        public void TestZeroConsistencyGivesNoDifficultyValue()
        {
            var result = calc(1000, attrs: attributes(consistency: 0));
            Assert.That(result.Difficulty, Is.EqualTo(0));
            Assert.That(result.Accuracy, Is.GreaterThan(0));
        }

        [Test]
        public void TestMissesReduceDifficultyValue()
            => Assert.That(calc(1000, 20).Difficulty, Is.LessThan(calc(1000, 0).Difficulty));

        // CF: TaikoPerformanceCalculator.computeDifficultyValue | score.Mods.Any(m => m is ModHidden) | TRUE
        // CF: TaikoPerformanceCalculator.computeDifficultyValue | isConvert | FALSE
        // CF: TaikoPerformanceCalculator.computeDifficultyValue | !score.Mods.Any(m => m is ModFlashlight) | TRUE
        // CF: TaikoPerformanceCalculator.computeDifficultyValue | !isClassic | TRUE
        // CF: TaikoPerformanceCalculator.computeDifficultyValue | Mods.Any(ModEasy) && isClassic | FALSE
        [Test]
        public void TestHiddenBonusIsPenalisedOnNonClassic()
            => Assert.That(calc(1000, mods: new Mod[] { new TaikoModHidden() }).Difficulty / calc(1000).Difficulty, Is.EqualTo(1.02).Within(1e-9));

        // CF: TaikoPerformanceCalculator.computeDifficultyValue | isConvert | TRUE
        [Test]
        public void TestHiddenBonusOnConvertIsSmaller()
            => Assert.That(calc(1000, convert: true, mods: new Mod[] { new TaikoModHidden() }).Difficulty / calc(1000, convert: true).Difficulty, Is.EqualTo(1.005).Within(1e-9));

        // CF: TaikoPerformanceCalculator.computeDifficultyValue | !isClassic | FALSE
        // CF: TaikoPerformanceCalculator.computeDifficultyValue | Mods.Any(ModEasy) | FALSE
        [Test]
        public void TestHiddenBonusOnClassicIsFull()
        {
            var classic = new TaikoModClassic();
            Assert.That(calc(1000, mods: new Mod[] { classic, new TaikoModHidden() }).Difficulty / calc(1000, mods: new Mod[] { classic }).Difficulty,
                Is.EqualTo(1.1).Within(1e-9));
        }

        // CF: TaikoPerformanceCalculator.computeDifficultyValue | Mods.Any(ModEasy) && isClassic | TRUE
        [Test]
        public void TestHiddenBonusOnClassicEasyIsHalved()
        {
            var easy = new TaikoModEasy();
            var classic = new TaikoModClassic();
            Assert.That(calc(1000, mods: new Mod[] { easy, classic, new TaikoModHidden() }).Difficulty / calc(1000, mods: new Mod[] { easy, classic }).Difficulty,
                Is.EqualTo(1.05).Within(1e-9));
        }

        // CF: TaikoPerformanceCalculator.computeDifficultyValue | !score.Mods.Any(m => m is ModFlashlight) | FALSE
        // CF: TaikoPerformanceCalculator.computeDifficultyValue | score.Mods.Any(m => m is ModFlashlight<TaikoHitObject>) | TRUE
        [Test]
        public void TestHiddenWithFlashlightIsNotPenalised()
            => Assert.That(calc(1000, mods: new Mod[] { new TaikoModHidden(), new TaikoModFlashlight() }).Difficulty / calc(1000, mods: new Mod[] { new TaikoModFlashlight() }).Difficulty,
                Is.EqualTo(1.1).Within(1e-9));

        // CF: TaikoPerformanceCalculator.computeDifficultyValue | score.Mods.Any(m => m is ModFlashlight<TaikoHitObject>) | FALSE
        [Test]
        public void TestFlashlightBonus()
            => Assert.That(calc(1000, mods: new Mod[] { new TaikoModFlashlight() }).Difficulty / calc(1000).Difficulty, Is.EqualTo(1.05).Within(1e-9));

        // CF: TaikoPerformanceCalculator.computeAccuracyValue | ModHidden && !isConvert | TRUE
        // CF: TaikoPerformanceCalculator.computeAccuracyValue | ModHidden && !isConvert | FALSE
        [Test]
        public void TestHiddenAccuracyBonusOnlyOnNonConverts()
        {
            Assert.That(calc(1000, mods: new Mod[] { new TaikoModHidden() }).Accuracy / calc(1000).Accuracy, Is.EqualTo(1.075).Within(1e-9));
            Assert.That(calc(1000, convert: true, mods: new Mod[] { new TaikoModHidden() }).Accuracy / calc(1000, convert: true).Accuracy, Is.EqualTo(1).Within(1e-9));
        }

        // CF: TaikoPerformanceCalculator.computeAccuracyValue | Flashlight && Hidden && !isConvert | TRUE
        // CF: TaikoPerformanceCalculator.computeAccuracyValue | Math.Max(1.0, 1.05 * memoryLengthBonus) | non-null
        [Test]
        public void TestMemoryBonusOnLongMaps()
        {
            // 5000 hits: memory length bonus is capped at 1.15 -> 1.05 * 1.15
            var hdfl = calc(5000, mods: new Mod[] { new TaikoModHidden(), new TaikoModFlashlight() }).Accuracy;
            var hd = calc(5000, mods: new Mod[] { new TaikoModHidden() }).Accuracy;
            Assert.That(hdfl / hd, Is.EqualTo(1.05 * 1.15).Within(1e-9));
        }

        // CF: TaikoPerformanceCalculator.computeAccuracyValue | Flashlight && Hidden && !isConvert | FALSE
        [Test]
        public void TestNoMemoryBonusForConverts()
        {
            var a = calc(5000, convert: true, mods: new Mod[] { new TaikoModHidden(), new TaikoModFlashlight() }).Accuracy;
            var b = calc(5000, convert: true, mods: new Mod[] { new TaikoModHidden() }).Accuracy;
            Assert.That(a / b, Is.EqualTo(1).Within(1e-9));
        }
    }
}
