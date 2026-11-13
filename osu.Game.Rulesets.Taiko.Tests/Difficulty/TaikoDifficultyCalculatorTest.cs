// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Beatmaps;
using osu.Game.Rulesets.Taiko.Difficulty;
using osu.Game.Rulesets.Taiko.Mods;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Taiko.Tests.Difficulty
{
    [TestFixture]
    public class TaikoDifficultyCalculatorTest
    {
        private static TaikoDifficultyAttributes calculate(int hits, params Mod[] mods)
        {
            var beatmap = new TaikoBeatmap
            {
                BeatmapInfo = { Ruleset = new TaikoRuleset().RulesetInfo, Metadata = new BeatmapMetadata() },
                Difficulty = new BeatmapDifficulty { OverallDifficulty = 5, SliderMultiplier = 1.4 },
            };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

            // deterministic busy pattern: mixed colours, 1/4 and 1/2 spacing, occasional triplets.
            double time = 1000;
            for (int i = 0; i < hits; i++)
            {
                beatmap.HitObjects.Add(new Hit { StartTime = time, Type = (i * 7 % 5) < 2 ? HitType.Centre : HitType.Rim });
                time += (i % 4 == 3) ? 250 : 125;
            }

            var calculator = new TaikoDifficultyCalculator(new TaikoRuleset().RulesetInfo, new TestWorkingBeatmap(beatmap));
            return (TaikoDifficultyAttributes)calculator.Calculate(mods);
        }

        // CF: TaikoDifficultyCalculator.CreateDifficultyAttributes | beatmap.HitObjects.Count == 0 | TRUE
        [Test]
        public void TestEmptyBeatmapHasNoDifficulty()
        {
            var attributes = calculate(0);
            Assert.That(attributes.StarRating, Is.EqualTo(0));
            Assert.That(attributes.MaxCombo, Is.EqualTo(0));
        }

        // CF: TaikoDifficultyCalculator.CreateDifficultyAttributes | beatmap.HitObjects.Count == 0 | FALSE
        // CF: TaikoDifficultyCalculator.combinedDifficultyValue | peaks.Count == 0 | TRUE
        // CF: TaikoDifficultyCalculator.CreateDifficultyHitObjects | for i = 2; i < Count | loop 0
        [Test]
        public void TestTooFewObjectsGivesNoStarRating()
        {
            var attributes = calculate(2);
            Assert.That(attributes.StarRating, Is.EqualTo(0));
            Assert.That(attributes.ConsistencyFactor, Is.EqualTo(0));
            Assert.That(attributes.MaxCombo, Is.EqualTo(2));
        }

        // CF: TaikoDifficultyCalculator.combinedDifficultyValue | peaks.Count == 0 | FALSE
        // CF: TaikoDifficultyCalculator.combinedDifficultyValue | hitObjectStrainPeaks.Count == 0 | FALSE
        // CF: TaikoDifficultyCalculator.CreateDifficultyHitObjects | for i = 2; i < Count | loop n
        // CF: TaikoDifficultyCalculator.CreateSkills | mods.Any(h => h is TaikoModRelax) | FALSE
        // CF: TaikoDifficultyCalculator.combinePeaks | isRelax | FALSE
        // CF: TaikoDifficultyCalculator.combinePeaks | isConvert || isRelax | FALSE
        [Test]
        public void TestBusyMapHasStarRating()
        {
            var a = calculate(300);

            Assert.That(a.StarRating, Is.GreaterThan(0));
            Assert.That(a.MaxCombo, Is.EqualTo(300));
            Assert.That(a.ConsistencyFactor, Is.GreaterThan(0).And.LessThanOrEqualTo(1));
            // the four skill contributions partition the star rating.
            Assert.That(a.RhythmDifficulty + a.ReadingDifficulty + a.ColourDifficulty + a.StaminaDifficulty, Is.EqualTo(a.StarRating).Within(1e-6));
            Assert.That(a.MechanicalDifficulty, Is.EqualTo(a.ColourDifficulty + a.StaminaDifficulty).Within(1e-9));
        }

        // CF: TaikoDifficultyCalculator.CreateSkills | mods.Any(h => h is TaikoModRelax) | TRUE
        // CF: TaikoDifficultyCalculator.combinePeaks | isRelax | TRUE
        // CF: TaikoDifficultyCalculator.combinePeaks | isConvert || isRelax | TRUE
        [Test]
        public void TestRelaxRemovesColourDifficulty()
        {
            var normal = calculate(300);
            var relax = calculate(300, new TaikoModRelax());

            Assert.That(relax.StarRating, Is.LessThan(normal.StarRating));
        }

        [Test]
        public void TestRateAdjustingModIncreasesStarRating()
            => Assert.That(calculate(300, new TaikoModDoubleTime()).StarRating, Is.GreaterThan(calculate(300).StarRating));
    }
}
