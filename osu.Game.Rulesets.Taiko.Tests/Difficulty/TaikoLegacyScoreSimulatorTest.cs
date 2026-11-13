// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Scoring.Legacy;
using osu.Game.Rulesets.Taiko.Beatmaps;
using osu.Game.Rulesets.Taiko.Difficulty;
using osu.Game.Rulesets.Taiko.Mods;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Taiko.Scoring;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Taiko.Tests.Difficulty
{
    [TestFixture]
    public class TaikoLegacyScoreSimulatorTest
    {
        private static TaikoBeatmap create(int beatmapVersion = 30000013, double tickRate = 1, params HitObject[] objects)
        {
            var beatmap = new TaikoBeatmap
            {
                BeatmapInfo = { Ruleset = new TaikoRuleset().RulesetInfo, Metadata = new BeatmapMetadata() },
                BeatmapVersion = beatmapVersion,
                Difficulty = new BeatmapDifficulty { SliderTickRate = tickRate },
            };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            foreach (var o in objects)
                beatmap.HitObjects.Add((TaikoHitObject)o);
            beatmap.HitObjects.ForEach(h => h.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty));
            return beatmap;
        }

        private static LegacyScoreAttributes simulate(TaikoBeatmap beatmap)
            => new TaikoLegacyScoreSimulator().Simulate(new TestWorkingBeatmap(beatmap), beatmap);

        private static DrumRoll roll(double start = 1000, double duration = 1000, bool strong = false)
            => new DrumRoll { StartTime = start, Duration = duration, IsStrong = strong };

        // CF: TaikoLegacyScoreSimulator.Simulate | baseBeatmap.HitObjects.Count > 0 | FALSE
        // CF: TaikoLegacyScoreSimulator.Simulate | for i < playableBeatmap.HitObjects.Count | loop 0
        // CF: TaikoLegacyScoreSimulator.Simulate | legacyBonusScore == 0 | TRUE
        [Test]
        public void TestEmptyBeatmap()
        {
            var a = simulate(create());
            Assert.That(a.MaxCombo, Is.EqualTo(0));
            Assert.That(a.BonusScore, Is.EqualTo(0));
            Assert.That(a.BonusScoreRatio, Is.EqualTo(0));
        }

        // CF: TaikoLegacyScoreSimulator.Simulate | baseBeatmap.HitObjects.Count > 0 | TRUE
        // CF: TaikoLegacyScoreSimulator.Simulate | for i < playableBeatmap.HitObjects.Count | loop 1
        // CF: TaikoLegacyScoreSimulator.simulateHit | case Hit | case Hit
        // CF: TaikoLegacyScoreSimulator.simulateHit | addScoreComboMultiplier | TRUE
        // CF: TaikoLegacyScoreSimulator.simulateHit | hitObject is Swell | FALSE
        // CF: TaikoLegacyScoreSimulator.simulateHit | Kiai at hit start | FALSE
        // CF: TaikoLegacyScoreSimulator.simulateHit | strongable.IsStrong | FALSE
        // CF: TaikoLegacyScoreSimulator.simulateHit | isBonus | FALSE
        // CF: TaikoLegacyScoreSimulator.simulateHit | increaseCombo | TRUE
        [Test]
        public void TestSingleHit()
        {
            var a = simulate(create(objects: new Hit { StartTime = 1000 }));
            Assert.That(a.AccuracyScore, Is.EqualTo(300));
            Assert.That(a.ComboScore, Is.EqualTo(0));
            Assert.That(a.MaxCombo, Is.EqualTo(1));
        }

        // CF: TaikoLegacyScoreSimulator.simulateHit | strongable.IsStrong | TRUE
        [Test]
        public void TestStrongHitDoublesScore()
            => Assert.That(simulate(create(objects: new Hit { StartTime = 1000, IsStrong = true })).AccuracyScore, Is.EqualTo(600));

        // CF: TaikoLegacyScoreSimulator.simulateHit | Kiai at hit start | TRUE
        [Test]
        public void TestKiaiHitMovesBonusToComboScore()
        {
            var beatmap = create(objects: new Hit { StartTime = 1000 });
            beatmap.ControlPointInfo.Add(0, new EffectControlPoint { KiaiMode = true });
            var a = simulate(beatmap);
            Assert.That(a.AccuracyScore, Is.EqualTo(300));
            Assert.That(a.ComboScore, Is.EqualTo(60)); // 300 * 1.2 - 300
        }

        // CF: TaikoLegacyScoreSimulator.simulateHit | addScoreComboMultiplier | TRUE
        [Test]
        public void TestComboMultiplierAppliesFromTenthCombo()
        {
            var hits = new HitObject[11];
            for (int i = 0; i < hits.Length; i++)
                hits[i] = new Hit { StartTime = 1000 + i * 5000 };
            var beatmap = create(objects: hits);

            var difficulty = beatmap.Difficulty.Clone();
            difficulty.CircleSize = 2;
            int stars = LegacyRulesetExtensions.CalculateDifficultyPeppyStars(difficulty, 11, 50);

            // only the 11th hit (combo 10) has a combo multiplier: 300 / 35 * 2 * (stars + 1) * 1
            Assert.That(simulate(beatmap).ComboScore, Is.EqualTo(300 / 35 * 2 * (stars + 1)));
        }

        // CF: TaikoLegacyScoreSimulator.Simulate | foreach baseBeatmap.Breaks | loop 1
        [Test]
        public void TestBreaksAreExcludedFromDrainLength()
        {
            var hits = new HitObject[11];
            for (int i = 0; i < hits.Length; i++)
                hits[i] = new Hit { StartTime = 1000 + i * 5000 };
            var beatmap = create(objects: hits);
            beatmap.Breaks.Add(new BreakPeriod(11000, 41000));

            var difficulty = beatmap.Difficulty.Clone();
            difficulty.CircleSize = 2;
            int stars = LegacyRulesetExtensions.CalculateDifficultyPeppyStars(difficulty, 11, 20); // 50s - 30s break

            Assert.That(simulate(beatmap).ComboScore, Is.EqualTo(300 / 35 * 2 * (stars + 1)));
        }

        // CF: TaikoLegacyScoreSimulator.simulateHit | case Swell swell | case Swell
        // CF: TaikoLegacyScoreSimulator.simulateHit | case SwellTick | case SwellTick
        // CF: TaikoLegacyScoreSimulator.simulateHit | hitObject is Swell | TRUE
        // CF: TaikoLegacyScoreSimulator.simulateHit | increaseCombo | FALSE
        // CF: TaikoLegacyScoreSimulator.simulateHit | isBonus | TRUE
        [Test]
        public void TestSwellSimulatesTicksAsBonus()
        {
            // 1s swell: (int)(7.5) = 7 -> (int)(7 * 1.65) = 11 -> (int)(11 * 1.5) = 16 -> 17 ticks of 300, plus the swell itself 300 * 2.
            var a = simulate(create(objects: new Swell { StartTime = 1000, Duration = 1000 }));
            Assert.That(a.BonusScore, Is.EqualTo(17 * 300 + 600));
            Assert.That(a.AccuracyScore, Is.EqualTo(0));
            Assert.That(a.MaxCombo, Is.EqualTo(0));
        }

        // CF: TaikoLegacyScoreSimulator.simulateHit | Kiai at swell end | TRUE
        [Test]
        public void TestKiaiSwellScalesBonus()
        {
            var beatmap = create(objects: new Swell { StartTime = 1000, Duration = 1000 });
            beatmap.ControlPointInfo.Add(0, new EffectControlPoint { KiaiMode = true });
            // swell: (int)(300 * 1.2f) = 360, doubled = 720 (combo part 60 * 2 is moved to ComboScore)
            var a = simulate(beatmap);
            Assert.That(a.ComboScore, Is.EqualTo(120));
        }

        // CF: TaikoLegacyScoreSimulator.simulateHit | case DrumRoll drumRoll | case DrumRoll
        // CF: TaikoLegacyScoreSimulator.simulateHit | case DrumRollTick | case DrumRollTick
        // CF: TaikoLegacyScoreSimulator.simulateHit | nextHitObject is DrumRoll | FALSE
        // CF: TaikoLegacyScoreSimulator.simulateHit | nextObjectHittableStartTime == null | TRUE
        // CF: TaikoLegacyScoreSimulator.simulateHit | endpointHittable | TRUE
        // CF: TaikoLegacyScoreSimulator.simulateHit | tick Kiai | FALSE
        // CF: TaikoLegacyScoreSimulator.simulateHit | tick.IsStrong | FALSE
        // CF: TaikoLegacyScoreSimulator.getSliderTaikoMinHitDelay | BeatmapVersion >= 8 | TRUE
        // CF: TaikoLegacyScoreSimulator.getSliderTaikoMinHitDelay | sliderTickRate == 3 || 6 || 1.5 | FALSE
        // CF: TaikoLegacyScoreSimulator.getSliderTaikoMinHitDelay | maxRate < 60 | FALSE
        // CF: TaikoLegacyScoreSimulator.getSliderTaikoMinHitDelay | maxRate > 120 | FALSE
        [Test]
        public void TestDrumRollTicksAreBonus()
        {
            // beat 500, 1/8 -> 62.5ms delay: ticks from 1000 while < 2000 + 62
            var a = simulate(create(objects: roll()));
            Assert.That(a.BonusScore, Is.EqualTo(17 * 300));
            Assert.That(a.BonusScoreRatio, Is.EqualTo(17 * new TaikoScoreProcessor().GetBaseScoreForResult(HitResult.SmallBonus) / (17.0 * 300)));
            Assert.That(a.MaxCombo, Is.EqualTo(0));
        }

        // CF: TaikoLegacyScoreSimulator.simulateHit | tick.IsStrong | TRUE
        // CF: TaikoLegacyScoreSimulator.simulateHit | strongable.IsStrong | TRUE
        [Test]
        public void TestStrongDrumRollTicksGainAFifthAndDouble()
            => Assert.That(simulate(create(objects: roll(strong: true))).BonusScore, Is.EqualTo(17 * 360 * 2)); // +1/5 for strong ticks, then x2 for strongable objects

        // CF: TaikoLegacyScoreSimulator.simulateHit | tick Kiai | TRUE
        [Test]
        public void TestKiaiDrumRollTicksScale()
        {
            var beatmap = create(objects: roll());
            beatmap.ControlPointInfo.Add(0, new EffectControlPoint { KiaiMode = true });
            Assert.That(simulate(beatmap).BonusScore, Is.EqualTo(17 * 360));
        }

        // CF: TaikoLegacyScoreSimulator.getSliderTaikoMinHitDelay | sliderTickRate == 3 || 6 || 1.5 | TRUE
        [TestCase(3d)]
        [TestCase(6d)]
        [TestCase(1.5d)]
        public void TestTripletTickRatesUseSixthBeatDelay(double tickRate)
        {
            // 500 / 6 = 83.3ms: ticks while < 2000 + 83 -> 13 ticks
            Assert.That(simulate(create(tickRate: tickRate, objects: roll())).BonusScore, Is.EqualTo(13 * 300));
        }

        // CF: TaikoLegacyScoreSimulator.getSliderTaikoMinHitDelay | BeatmapVersion >= 8 | FALSE
        [Test]
        public void TestOldBeatmapVersionAlwaysUsesEighthBeatDelay()
            => Assert.That(simulate(create(beatmapVersion: 7, tickRate: 3, objects: roll())).BonusScore, Is.EqualTo(17 * 300));

        // CF: TaikoLegacyScoreSimulator.getSliderTaikoMinHitDelay | maxRate < 60 | TRUE
        [Test]
        public void TestFastBeatLengthDelayIsDoubledUpToRange()
        {
            // beat 100 -> 12.5 -> 100ms delay; drum roll 1000..2000 -> ticks while < 2000 + 100 -> 11 ticks
            var beatmap = create(objects: roll());
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 100 });
            Assert.That(simulate(beatmap).BonusScore, Is.EqualTo(11 * 300));
        }

        // CF: TaikoLegacyScoreSimulator.getSliderTaikoMinHitDelay | maxRate > 120 | TRUE
        [Test]
        public void TestSlowBeatLengthDelayIsHalvedIntoRange()
        {
            // beat 2000 -> 250 -> 125 -> 62.5ms delay; same tick count as the default case
            var beatmap = create(objects: roll());
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 2000 });
            Assert.That(simulate(beatmap).BonusScore, Is.EqualTo(17 * 300));
        }

        // CF: TaikoLegacyScoreSimulator.simulateHit | endpointHittable | FALSE
        // CF: TaikoLegacyScoreSimulator.simulateHit | nextObjectHittableStartTime == null | FALSE
        // CF: TaikoLegacyScoreSimulator.simulateHit | nextHitObject is DrumRoll | TRUE
        [Test]
        public void TestEndpointNotHittableWhenNextObjectIsTooClose()
        {
            // second roll starts 10ms after the first ends -> the first gets no extra end tick delay (16 ticks); the second has 17.
            var a = simulate(create(objects: new HitObject[] { roll(1000, 1000), roll(2010, 1000) }));
            Assert.That(a.BonusScore, Is.EqualTo((16 + 17) * 300));
        }

        // CF: TaikoLegacyScoreSimulator.simulateHit | case StrongNestedHitObject | case StrongNestedHitObject
        [Test]
        public void TestStandaloneStrongNestedObjectIsIgnored()
        {
            var beatmap = create();
            beatmap.HitObjects.Add(new Hit.StrongNestedHit(new Hit()));
            Assert.That(simulate(beatmap).MaxCombo, Is.EqualTo(0));
        }

        // CF: TaikoLegacyScoreSimulator.simulateHit | switch (hitObject) | default
        [Test]
        public void TestUnknownObjectScoresNothing()
        {
            var beatmap = create();
            beatmap.HitObjects.Add(new Hit.StrongNestedHit(new Hit()) { StartTime = 5 });
            var a = simulate(beatmap);
            Assert.That(a.AccuracyScore, Is.EqualTo(0));
        }

        private static double multiplier(params Mod[] mods)
            => new TaikoLegacyScoreSimulator().GetLegacyScoreMultiplier(mods, LegacyBeatmapConversionDifficultyInfo.FromBeatmap(create()));

        // CF: TaikoLegacyScoreSimulator.GetLegacyScoreMultiplier | foreach mods | loop 0
        [Test]
        public void TestNoModsMultiplier() => Assert.That(multiplier(), Is.EqualTo(1.0));

        // CF: TaikoLegacyScoreSimulator.GetLegacyScoreMultiplier | case TaikoModNoFail | case TaikoModNoFail
        // CF: TaikoLegacyScoreSimulator.GetLegacyScoreMultiplier | scoreV2 | FALSE
        [Test]
        public void TestNoFailMultiplier() => Assert.That(multiplier(new TaikoModNoFail()), Is.EqualTo(0.5));

        // CF: TaikoLegacyScoreSimulator.GetLegacyScoreMultiplier | scoreV2 | TRUE
        // CF: TaikoLegacyScoreSimulator.GetLegacyScoreMultiplier | foreach mods | loop n
        [Test]
        public void TestNoFailWithScoreV2Multiplier() => Assert.That(multiplier(new TaikoModNoFail(), new ModScoreV2()), Is.EqualTo(1.0));

        // CF: TaikoLegacyScoreSimulator.GetLegacyScoreMultiplier | case TaikoModEasy | case TaikoModEasy
        [Test]
        public void TestEasyMultiplier() => Assert.That(multiplier(new TaikoModEasy()), Is.EqualTo(0.5));

        // CF: TaikoLegacyScoreSimulator.GetLegacyScoreMultiplier | case TaikoModHalfTime | case TaikoModHalfTime
        // CF: TaikoLegacyScoreSimulator.GetLegacyScoreMultiplier | case TaikoModDaycore | case TaikoModDaycore
        [Test]
        public void TestSlowRateMultipliers()
        {
            Assert.That(multiplier(new TaikoModHalfTime()), Is.EqualTo(0.3));
            Assert.That(multiplier(new TaikoModDaycore()), Is.EqualTo(0.3));
        }

        // CF: TaikoLegacyScoreSimulator.GetLegacyScoreMultiplier | case TaikoModHidden | case TaikoModHidden
        // CF: TaikoLegacyScoreSimulator.GetLegacyScoreMultiplier | case TaikoModHardRock | case TaikoModHardRock
        [Test]
        public void TestHiddenAndHardRockMultipliers()
        {
            Assert.That(multiplier(new TaikoModHidden()), Is.EqualTo(1.06));
            Assert.That(multiplier(new TaikoModHardRock()), Is.EqualTo(1.06));
        }

        // CF: TaikoLegacyScoreSimulator.GetLegacyScoreMultiplier | case TaikoModDoubleTime | case TaikoModDoubleTime
        // CF: TaikoLegacyScoreSimulator.GetLegacyScoreMultiplier | case TaikoModNightcore | case TaikoModNightcore
        // CF: TaikoLegacyScoreSimulator.GetLegacyScoreMultiplier | case TaikoModFlashlight | case TaikoModFlashlight
        [Test]
        public void TestIncreasingMultipliers()
        {
            Assert.That(multiplier(new TaikoModDoubleTime()), Is.EqualTo(1.12));
            Assert.That(multiplier(new TaikoModNightcore()), Is.EqualTo(1.12));
            Assert.That(multiplier(new TaikoModFlashlight()), Is.EqualTo(1.12));
        }

        // CF: TaikoLegacyScoreSimulator.GetLegacyScoreMultiplier | case TaikoModRelax | case TaikoModRelax
        [Test]
        public void TestRelaxZeroesMultiplier() => Assert.That(multiplier(new TaikoModHidden(), new TaikoModRelax()), Is.EqualTo(0));

        // CF: TaikoLegacyScoreSimulator.GetLegacyScoreMultiplier | switch (mod) | default
        [Test]
        public void TestUnrelatedModDoesNotChangeMultiplier() => Assert.That(multiplier(new TaikoModClassic()), Is.EqualTo(1.0));
    }
}
