// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Tests.Beatmaps;
using static osu.Game.Rulesets.Osu.Tests.Difficulty.DifficultyTestHelper;

namespace osu.Game.Rulesets.Osu.Tests.Difficulty
{
    [TestFixture]
    public class OsuDifficultyCalculatorTest
    {
        private static OsuDifficultyAttributes calculate(IEnumerable<OsuHitObject> objects, params Mod[] mods)
        {
            var ruleset = new OsuRuleset();
            var beatmap = new Beatmap
            {
                BeatmapInfo = new BeatmapInfo { Ruleset = ruleset.RulesetInfo, Difficulty = new BeatmapDifficulty() },
            };
            beatmap.HitObjects.AddRange(objects);

            return (OsuDifficultyAttributes)new OsuDifficultyCalculator(ruleset.RulesetInfo, new TestWorkingBeatmap(beatmap)).Calculate(mods);
        }

        private static OsuHitObject[] jumps(double spacing, float distance, int count = 60)
            => Enumerable.Range(0, count).Select(i => Circle(1000 + i * spacing, 100 + (i % 2) * distance, 100 + (i % 3) * 50)).ToArray();

        private static OsuHitObject[] mixed()
        {
            var list = new List<OsuHitObject>();

            for (int i = 0; i < 40; i++)
            {
                double t = 1000 + i * 300;
                if (i % 5 == 3)
                    list.Add(Slider(t, 100 + (i % 6) * 60, 100 + (i % 4) * 50, 120, 40));
                else
                    list.Add(Circle(t, 100 + (i % 6) * 60, 100 + (i % 4) * 50));
            }

            list.Add(Spinner(1000 + 40 * 300 + 200, 1500));
            return list.ToArray();
        }

        // CF: OsuDifficultyCalculator.CreateDifficultyAttributes | beatmap.HitObjects.Count == 0 | TRUE
        [Test]
        public void TestEmptyBeatmap()
        {
            var attributes = calculate(new OsuHitObject[0]);

            Assert.That(attributes.StarRating, Is.EqualTo(0));
            Assert.That(attributes.MaxCombo, Is.EqualTo(0));
        }

        // CF: OsuDifficultyCalculator.CreateDifficultyAttributes | beatmap.HitObjects.Count == 0 | FALSE
        // CF: OsuDifficultyCalculator.CreateDifficultyAttributes | flashlight is not null | FALSE
        // CF: OsuDifficultyCalculator.CreateDifficultyAttributes | aimDifficultyValue > 0 | TRUE
        [Test]
        public void TestObjectCountsAndRatings()
        {
            var attributes = calculate(mixed());

            Assert.That(attributes.StarRating, Is.GreaterThan(0));
            Assert.That(attributes.AimDifficulty, Is.GreaterThan(0));
            Assert.That(attributes.SpeedDifficulty, Is.GreaterThan(0));
            Assert.That(attributes.ReadingDifficulty, Is.GreaterThan(0));
            Assert.That(attributes.FlashlightDifficulty, Is.EqualTo(0));
            Assert.That(attributes.SliderFactor, Is.InRange(0, 1));
            Assert.That(attributes.SliderCount, Is.EqualTo(8));
            Assert.That(attributes.SpinnerCount, Is.EqualTo(1));
            Assert.That(attributes.HitCircleCount, Is.EqualTo(32));
            Assert.That(attributes.MaxCombo, Is.GreaterThan(41));
            Assert.That(attributes.NestedScorePerObject, Is.GreaterThan(0));
            Assert.That(attributes.LegacyScoreBaseMultiplier, Is.GreaterThan(0));
            Assert.That(attributes.MaximumLegacyComboScore, Is.GreaterThan(0));
        }

        // CF: OsuDifficultyCalculator.CreateDifficultyAttributes | aimDifficultyValue > 0 | FALSE
        [Test]
        public void TestSingleObjectHasNoAimDifficulty()
        {
            var attributes = calculate(new[] { Circle(1000, 100) });

            Assert.That(attributes.AimDifficulty, Is.EqualTo(0));
            Assert.That(attributes.SliderFactor, Is.EqualTo(1));
            Assert.That(attributes.HitCircleCount, Is.EqualTo(1));
        }

        // CF: OsuDifficultyCalculator.CreateDifficultyAttributes | flashlight is not null | TRUE
        [Test]
        public void TestFlashlightModAddsFlashlightRating()
        {
            Assert.That(calculate(mixed(), new OsuModFlashlight()).FlashlightDifficulty, Is.GreaterThan(0));
        }

        [Test]
        public void TestFasterAndWiderMapsAreHarder()
        {
            Assert.That(calculate(jumps(120, 200)).StarRating, Is.GreaterThan(calculate(jumps(400, 60)).StarRating));
        }

        [Test]
        public void TestDoubleTimeIncreasesRating()
        {
            Assert.That(calculate(jumps(200, 150), new OsuModDoubleTime()).StarRating, Is.GreaterThan(calculate(jumps(200, 150)).StarRating));
        }

        [Test]
        public void TestHalfTimeDecreasesRating()
        {
            Assert.That(calculate(jumps(200, 150), new OsuModHalfTime()).StarRating, Is.LessThan(calculate(jumps(200, 150)).StarRating));
        }

        [Test]
        public void TestRatingIsDeterministic()
        {
            Assert.That(calculate(mixed()).StarRating, Is.EqualTo(calculate(mixed()).StarRating));
        }

        // CF: OsuDifficultyCalculator.SumCognitionDifficulty | reading <= 0 | TRUE
        [Test]
        public void TestCognitionWithoutReading()
        {
            Assert.That(OsuDifficultyCalculator.SumCognitionDifficulty(0, 7), Is.EqualTo(7));
        }

        // CF: OsuDifficultyCalculator.SumCognitionDifficulty | reading <= 0 | FALSE
        // CF: OsuDifficultyCalculator.SumCognitionDifficulty | flashlight <= 0 | TRUE
        [Test]
        public void TestCognitionWithoutFlashlight()
        {
            Assert.That(OsuDifficultyCalculator.SumCognitionDifficulty(7, 0), Is.EqualTo(7));
        }

        // CF: OsuDifficultyCalculator.SumCognitionDifficulty | flashlight <= 0 | FALSE
        [Test]
        public void TestCognitionCombinesBoth()
        {
            double combined = OsuDifficultyCalculator.SumCognitionDifficulty(10, 5);

            Assert.That(combined, Is.GreaterThan(10));
            // a flashlight value that is larger than reading is not nerfed, so it contributes more
            Assert.That(OsuDifficultyCalculator.SumCognitionDifficulty(5, 10), Is.GreaterThan(combined));
        }
    }
}
