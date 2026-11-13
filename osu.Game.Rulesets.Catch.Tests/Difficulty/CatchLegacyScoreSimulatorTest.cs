// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Scoring.Legacy;
using osu.Game.Rulesets.Catch.Beatmaps;
using osu.Game.Rulesets.Catch.Difficulty;
using osu.Game.Rulesets.Catch.Mods;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Tests.Beatmaps;
using osuTK;

namespace osu.Game.Rulesets.Catch.Tests.Difficulty
{
    [TestFixture]
    public class CatchLegacyScoreSimulatorTest
    {
        private static CatchBeatmap createBeatmap(params CatchHitObject[] hitObjects)
        {
            var beatmap = new CatchBeatmap { BeatmapInfo = { Ruleset = new CatchRuleset().RulesetInfo } };
            beatmap.Difficulty.SliderMultiplier = 1;
            beatmap.HitObjects.AddRange(hitObjects);
            foreach (var h in hitObjects)
                h.ApplyDefaults(new ControlPointInfo(), beatmap.Difficulty);
            return beatmap;
        }

        private static LegacyScoreAttributes simulate(CatchBeatmap beatmap)
            => new CatchLegacyScoreSimulator().Simulate(new TestWorkingBeatmap(beatmap), beatmap);

        private static double multiplier(params Mod[] mods)
            => new CatchLegacyScoreSimulator().GetLegacyScoreMultiplier(mods, new LegacyBeatmapConversionDifficultyInfo());

        // CF: CatchLegacyScoreSimulator.Simulate | baseBeatmap.HitObjects.Count > 0 | FALSE
        // CF: CatchLegacyScoreSimulator.Simulate | foreach obj in base | loop 0
        // CF: CatchLegacyScoreSimulator.Simulate | legacyBonusScore == 0 | TRUE
        [Test]
        public void TestEmptyBeatmap()
        {
            var attributes = simulate(createBeatmap());

            Assert.That(attributes.AccuracyScore, Is.Zero);
            Assert.That(attributes.ComboScore, Is.Zero);
            Assert.That(attributes.BonusScore, Is.Zero);
            Assert.That(attributes.BonusScoreRatio, Is.Zero);
            Assert.That(attributes.MaxCombo, Is.Zero);
        }

        // CF: CatchLegacyScoreSimulator.Simulate | switch obj | default
        // CF: CatchLegacyScoreSimulator.Simulate | baseBeatmap.HitObjects.Count > 0 | TRUE
        // CF: CatchLegacyScoreSimulator.simulateHit | switch hitObject | case Fruit
        // CF: CatchLegacyScoreSimulator.simulateHit | addScoreComboMultiplier | TRUE
        // CF: CatchLegacyScoreSimulator.simulateHit | addScoreComboMultiplier | FALSE
        // CF: CatchLegacyScoreSimulator.simulateHit | isBonus | FALSE
        // CF: CatchLegacyScoreSimulator.simulateHit | increaseCombo | TRUE
        [Test]
        public void TestFruits()
        {
            var attributes = simulate(createBeatmap(new Fruit { StartTime = 0 }, new Fruit { StartTime = 1000 }, new Fruit { StartTime = 5000 }));

            Assert.That(attributes.AccuracyScore, Is.EqualTo(900));
            Assert.That(attributes.MaxCombo, Is.EqualTo(3));
            // the first fruit has no combo to multiply; later fruits do.
            Assert.That(attributes.ComboScore, Is.GreaterThan(0));
        }

        [Test]
        public void TestSingleFruitHasNoComboScore()
        {
            Assert.That(simulate(createBeatmap(new Fruit())).ComboScore, Is.Zero);
        }

        // CF: CatchLegacyScoreSimulator.Simulate | switch obj | case IHasPath
        // CF: CatchLegacyScoreSimulator.simulateHit | switch hitObject | case JuiceStream
        // CF: CatchLegacyScoreSimulator.simulateHit | switch hitObject | case TinyDroplet
        // CF: CatchLegacyScoreSimulator.simulateHit | switch hitObject | case Droplet
        // CF: CatchLegacyScoreSimulator.simulateHit | increaseCombo | FALSE
        [Test]
        public void TestJuiceStreamSimulatesNestedObjects()
        {
            var stream = new JuiceStream
            {
                StartTime = 0,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }),
            };
            var beatmap = createBeatmap(stream);
            beatmap.Difficulty.SliderTickRate = 2;
            stream.ApplyDefaults(new ControlPointInfo(), beatmap.Difficulty);

            int fruits = stream.NestedHitObjects.OfType<Fruit>().Count();
            int droplets = stream.NestedHitObjects.Count(h => h is Droplet && h is not TinyDroplet);
            int tiny = stream.NestedHitObjects.OfType<TinyDroplet>().Count();

            var attributes = simulate(beatmap);

            Assert.That(tiny, Is.GreaterThan(0));
            Assert.That(droplets, Is.GreaterThan(0));
            Assert.That(attributes.AccuracyScore, Is.EqualTo(fruits * 300 + droplets * 100 + tiny * 10));
            Assert.That(attributes.MaxCombo, Is.EqualTo(fruits + droplets));
        }

        // CF: CatchLegacyScoreSimulator.Simulate | switch obj | case IHasDuration
        // CF: CatchLegacyScoreSimulator.simulateHit | switch hitObject | case BananaShower
        // CF: CatchLegacyScoreSimulator.simulateHit | switch hitObject | case Banana
        // CF: CatchLegacyScoreSimulator.simulateHit | isBonus | TRUE
        // CF: CatchLegacyScoreSimulator.Simulate | legacyBonusScore == 0 | FALSE
        [Test]
        public void TestBananaShowerContributesBonusOnly()
        {
            var shower = new BananaShower { StartTime = 0, Duration = 500 };
            var beatmap = createBeatmap(shower);
            int bananas = shower.NestedHitObjects.Count;

            var attributes = simulate(beatmap);

            Assert.That(bananas, Is.GreaterThan(0));
            Assert.That(attributes.AccuracyScore, Is.Zero);
            Assert.That(attributes.MaxCombo, Is.Zero);
            Assert.That(attributes.BonusScore, Is.EqualTo(bananas * 1100));
            // standardised banana is worth 200 versus 1100 in legacy scoring.
            Assert.That(attributes.BonusScoreRatio, Is.EqualTo(200.0 / 1100).Within(1e-9));
        }

        // CF: CatchLegacyScoreSimulator.Simulate | breaks Select/Sum | loop 0 and loop n
        [Test]
        public void TestBreaksShortenDrainLengthAndThereforeComboScore()
        {
            CatchBeatmap create(bool withBreak)
            {
                var beatmap = createBeatmap(new Fruit { StartTime = 0 }, new Fruit { StartTime = 100000 }, new Fruit { StartTime = 200000 });
                if (withBreak)
                    beatmap.Breaks.Add(new BreakPeriod(1000, 199000));
                return beatmap;
            }

            Assert.That(simulate(create(true)).ComboScore, Is.Not.EqualTo(simulate(create(false)).ComboScore));
        }

        // CF: CatchLegacyScoreSimulator.GetLegacyScoreMultiplier | switch mod | default
        [Test]
        public void TestNoModsAndUnrelatedModsGiveNeutralMultiplier()
        {
            Assert.That(multiplier(), Is.EqualTo(1));
            Assert.That(multiplier(new CatchModMirror()), Is.EqualTo(1));
        }

        // CF: CatchLegacyScoreSimulator.GetLegacyScoreMultiplier | switch mod | case CatchModNoFail
        // CF: CatchLegacyScoreSimulator.GetLegacyScoreMultiplier | scoreV2 | FALSE
        [Test]
        public void TestNoFail() => Assert.That(multiplier(new CatchModNoFail()), Is.EqualTo(0.5));

        // CF: CatchLegacyScoreSimulator.GetLegacyScoreMultiplier | scoreV2 | TRUE
        [Test]
        public void TestNoFailAndHiddenAreNeutralWithScoreV2()
        {
            Assert.That(multiplier(new CatchModNoFail(), new ModScoreV2()), Is.EqualTo(1));
            Assert.That(multiplier(new CatchModHidden(), new ModScoreV2()), Is.EqualTo(1));
        }

        // CF: CatchLegacyScoreSimulator.GetLegacyScoreMultiplier | switch mod | case CatchModHidden
        [Test]
        public void TestHidden() => Assert.That(multiplier(new CatchModHidden()), Is.EqualTo(1.06));

        // CF: CatchLegacyScoreSimulator.GetLegacyScoreMultiplier | switch mod | case CatchModEasy
        [Test]
        public void TestEasy() => Assert.That(multiplier(new CatchModEasy()), Is.EqualTo(0.5));

        // CF: CatchLegacyScoreSimulator.GetLegacyScoreMultiplier | switch mod | case CatchModHalfTime
        // CF: CatchLegacyScoreSimulator.GetLegacyScoreMultiplier | switch mod | case CatchModDaycore
        [Test]
        public void TestRateReduction()
        {
            Assert.That(multiplier(new CatchModHalfTime()), Is.EqualTo(0.3));
            Assert.That(multiplier(new CatchModDaycore()), Is.EqualTo(0.3));
        }

        // CF: CatchLegacyScoreSimulator.GetLegacyScoreMultiplier | switch mod | case CatchModHardRock
        // CF: CatchLegacyScoreSimulator.GetLegacyScoreMultiplier | switch mod | case CatchModDoubleTime
        // CF: CatchLegacyScoreSimulator.GetLegacyScoreMultiplier | switch mod | case CatchModNightcore
        // CF: CatchLegacyScoreSimulator.GetLegacyScoreMultiplier | switch mod | case CatchModFlashlight
        [Test]
        public void TestDifficultyIncreasingMods()
        {
            Assert.That(multiplier(new CatchModHardRock()), Is.EqualTo(1.12));
            Assert.That(multiplier(new CatchModDoubleTime()), Is.EqualTo(1.06));
            Assert.That(multiplier(new CatchModNightcore()), Is.EqualTo(1.06));
            Assert.That(multiplier(new CatchModFlashlight()), Is.EqualTo(1.12));
        }

        // CF: CatchLegacyScoreSimulator.GetLegacyScoreMultiplier | foreach mod | loop n
        // CF: CatchLegacyScoreSimulator.GetLegacyScoreMultiplier | switch mod | case CatchModRelax
        [Test]
        public void TestMultipliersCompoundAndRelaxZeroes()
        {
            Assert.That(multiplier(new CatchModHidden(), new CatchModHardRock()), Is.EqualTo(1.06 * 1.12).Within(1e-9));
            Assert.That(multiplier(new CatchModHidden(), new CatchModRelax()), Is.Zero);
        }
    }
}
