// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.Difficulty;
using osu.Game.Rulesets.Mania.Mods;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mods;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Mania.Tests.Difficulty
{
    [TestFixture]
    public class ManiaDifficultyCalculatorTest
    {
        private class Exposed : ManiaDifficultyCalculator
        {
            public Exposed(IWorkingBeatmap beatmap)
                : base(new ManiaRuleset().RulesetInfo, beatmap)
            {
            }

            public Mod[] AdjustmentMods => DifficultyAdjustmentMods;
        }

        private static IWorkingBeatmap working(bool mania, params ManiaHitObject[] objects)
        {
            var beatmap = new ManiaBeatmap(new StageDefinition(4)) { HitObjects = objects.ToList() };
            beatmap.BeatmapInfo.Ruleset = mania ? new ManiaRuleset().RulesetInfo : new RulesetInfo { OnlineID = 0, ShortName = "osu" };
            beatmap.Difficulty.CircleSize = 4;
            return new TestWorkingBeatmap(beatmap);
        }

        // CF: ManiaDifficultyCalculator.Version | constant | non-null
        [Test]
        public void TestVersion() => Assert.That(new Exposed(working(true)).Version, Is.EqualTo(20241007));

        // CF: ManiaDifficultyCalculator.CreateDifficultyAttributes | beatmap.HitObjects.Count == 0 | TRUE
        [Test]
        public void TestEmptyBeatmapHasNoRating()
        {
            var attributes = new Exposed(working(true)).Calculate([new ManiaModDoubleTime()]);

            Assert.That(attributes.StarRating, Is.Zero);
            Assert.That(attributes.MaxCombo, Is.Zero);
            Assert.That(attributes.Mods.Single(), Is.TypeOf<ManiaModDoubleTime>());
        }

        // CF: ManiaDifficultyCalculator.CreateDifficultyAttributes | beatmap.HitObjects.Count == 0 | FALSE
        // CF: ManiaDifficultyCalculator.maxComboForObject | hitObject is HoldNote | TRUE
        // CF: ManiaDifficultyCalculator.maxComboForObject | hitObject is HoldNote | FALSE
        // CF: ManiaDifficultyCalculator.CreateDifficultyHitObjects | for i = 1 | loop n
        // CF: ManiaDifficultyCalculator.CreateDifficultyHitObjects | for column | loop n
        [Test]
        public void TestMaxComboCountsHoldNoteDurationInHundreds()
        {
            var attributes = new Exposed(working(true,
                new Note { StartTime = 0, Column = 0 },
                new HoldNote { StartTime = 500, Duration = 250, Column = 1 },
                new Note { StartTime = 1000, Column = 2 })).Calculate([]);

            // 1 + (1 + 250 / 100) + 1
            Assert.That(attributes.MaxCombo, Is.EqualTo(5));
            Assert.That(attributes.StarRating, Is.GreaterThan(0));
        }

        // CF: ManiaDifficultyCalculator.CreateDifficultyHitObjects | for i = 1 | loop 0
        [Test]
        public void TestSingleObjectHasNoDifficulty()
        {
            var attributes = new Exposed(working(true, new Note { StartTime = 0 })).Calculate([]);
            Assert.That(attributes.MaxCombo, Is.EqualTo(1));
            Assert.That(attributes.StarRating, Is.Zero);
        }

        // CF: ManiaDifficultyCalculator.CreateDifficultyHitObjects | LegacySortHelper sort comparer | non-null
        [Test]
        public void TestObjectsAreSortedByStartTime()
        {
            var ordered = new Exposed(working(true, new Note { StartTime = 0 }, new Note { StartTime = 100, Column = 1 }, new Note { StartTime = 200, Column = 2 })).Calculate([]);
            var shuffled = new Exposed(working(true, new Note { StartTime = 200, Column = 2 }, new Note { StartTime = 0 }, new Note { StartTime = 100, Column = 1 })).Calculate([]);

            Assert.That(shuffled.StarRating, Is.EqualTo(ordered.StarRating));
        }

        // CF: ManiaDifficultyCalculator.CreateDifficultyHitObjects | clockRate from mods | non-null
        [Test]
        public void TestHigherClockRateIsHarder()
        {
            var calc = new Exposed(working(true, new Note { StartTime = 0 }, new Note { StartTime = 100, Column = 1 }, new Note { StartTime = 200, Column = 2 }));
            Assert.That(calc.Calculate([new ManiaModDoubleTime()]).StarRating, Is.GreaterThan(calc.Calculate([]).StarRating));
        }

        // CF: ManiaDifficultyCalculator.DifficultyAdjustmentMods | isForCurrentRuleset | TRUE
        [Test]
        public void TestAdjustmentModsForNativeBeatmap()
        {
            Assert.That(new Exposed(working(true)).AdjustmentMods.Select(m => m.GetType()),
                Is.EqualTo(new[] { typeof(ManiaModDoubleTime), typeof(ManiaModHalfTime), typeof(ManiaModEasy), typeof(ManiaModHardRock) }));
        }

        // CF: ManiaDifficultyCalculator.DifficultyAdjustmentMods | isForCurrentRuleset | FALSE
        [Test]
        public void TestAdjustmentModsForConvertedBeatmapIncludeKeyMods()
        {
            var mods = new Exposed(working(false)).AdjustmentMods;

            Assert.That(mods, Has.Length.EqualTo(4 + 14));
            Assert.That(mods.OfType<ManiaModKey1>().Count(), Is.EqualTo(1));
            Assert.That(mods.OfType<MultiMod>().Count(), Is.EqualTo(5));
        }
    }
}
