// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring.Legacy;

namespace osu.Game.Rulesets.Osu.Tests.Difficulty
{
    [TestFixture]
    public class OsuLegacyScoreSimulatorTest
    {
        private static LegacyScoreAttributes simulate(IEnumerable<OsuHitObject> objects)
        {
            var list = new List<OsuHitObject>(objects);
            return new OsuLegacyScoreSimulator().Simulate(DifficultyTestHelpers.Working(list), DifficultyTestHelpers.Playable(list));
        }

        private static double multiplier(params Mod[] mods) => new OsuLegacyScoreSimulator().GetLegacyScoreMultiplier(mods, new LegacyBeatmapConversionDifficultyInfo());

        // Simulate: empty beatmap -> HitObjects.Count > 0 FALSE (drain length stays 0), no objects simulated.
        [Test]
        public void TestSimulate_NoObjects_AllZero()
        {
            var attributes = simulate(Array.Empty<OsuHitObject>());

            Assert.That(attributes.AccuracyScore, Is.Zero);
            Assert.That(attributes.ComboScore, Is.Zero);
            Assert.That(attributes.BonusScore, Is.Zero);
            Assert.That(attributes.BonusScoreRatio, Is.Zero);
            Assert.That(attributes.MaxCombo, Is.Zero);
        }

        // HitCircle case: 300 accuracy score, combo +1. First circle has combo-1 clamped to 0 -> no combo score.
        [Test]
        public void TestSimulate_SingleCircle_Gives300AccuracyAndNoComboScore()
        {
            var attributes = simulate(new[] { DifficultyTestHelpers.Circle(0, 100, 100) });

            Assert.That(attributes.AccuracyScore, Is.EqualTo(300));
            Assert.That(attributes.ComboScore, Is.Zero);
            Assert.That(attributes.MaxCombo, Is.EqualTo(1));
            Assert.That(attributes.BonusScore, Is.Zero);
            Assert.That(attributes.BonusScoreRatio, Is.Zero);
        }

        // Second circle has combo = 1 -> Math.Max(0, 0) branch vs. > 0 branch: later circles add combo score.
        [Test]
        public void TestSimulate_ComboScoreGrowsOnlyAfterSecondCircle()
        {
            var two = simulate(DifficultyTestHelpers.Stream(2, 300, 50));
            var five = simulate(DifficultyTestHelpers.Stream(5, 300, 50));

            Assert.That(two.AccuracyScore, Is.EqualTo(600));
            Assert.That(five.AccuracyScore, Is.EqualTo(1500));
            Assert.That(five.ComboScore, Is.GreaterThan(two.ComboScore));
            Assert.That(five.MaxCombo, Is.EqualTo(5));
        }

        // Slider case: head+tail+repeat = 30 each, tick = 10, slider body = 300 and body itself does not raise combo.
        [Test]
        public void TestSimulate_SliderWithoutTicksOrRepeats_AccuracyIsHeadTailAndBody()
        {
            // length short enough to produce no ticks.
            var attributes = simulate(new[] { DifficultyTestHelpers.Slider(0, 100, 100, 10) });

            Assert.That(attributes.AccuracyScore, Is.EqualTo(30 + 30 + 300));
            Assert.That(attributes.MaxCombo, Is.EqualTo(2));
        }

        // SliderRepeat branch contributes +30 and +1 combo per repeat.
        [Test]
        public void TestSimulate_SliderRepeat_AddsRepeatScoreAndCombo()
        {
            var none = simulate(new[] { DifficultyTestHelpers.Slider(0, 100, 100, 10) });
            var one = simulate(new[] { DifficultyTestHelpers.Slider(0, 100, 100, 10, 1) });

            Assert.That(one.AccuracyScore - none.AccuracyScore, Is.EqualTo(30));
            Assert.That(one.MaxCombo - none.MaxCombo, Is.EqualTo(1));
        }

        // SliderTick branch contributes +10 and +1 combo: a long slider has ticks, so score > head+tail+body.
        [Test]
        public void TestSimulate_LongSlider_HasTicksWorth10Each()
        {
            var attributes = simulate(new[] { DifficultyTestHelpers.Slider(0, 100, 100, 300) });

            int ticks = attributes.MaxCombo - 2;
            Assert.That(ticks, Is.GreaterThan(0));
            Assert.That(attributes.AccuracyScore, Is.EqualTo(30 + 30 + ticks * 10 + 300));
        }

        // Spinner case: a short (zero length) spinner yields no ticks, only the 300 completion and no combo / bonus.
        [Test]
        public void TestSimulate_ZeroDurationSpinner_Only300()
        {
            var attributes = simulate(new[] { DifficultyTestHelpers.Spinner(0, 0) });

            Assert.That(attributes.AccuracyScore, Is.EqualTo(300));
            Assert.That(attributes.BonusScore, Is.Zero);
            Assert.That(attributes.MaxCombo, Is.EqualTo(1)); // spinner object itself raises combo
        }

        // SpinnerTick / SpinnerBonusTick branches: a long spinner gives bonus score, ratio > 0, and ticks never raise combo.
        [Test]
        public void TestSimulate_LongSpinner_ProducesBonusAndTicksDoNotRaiseCombo()
        {
            var attributes = simulate(new[] { DifficultyTestHelpers.Spinner(0, 4000) });

            Assert.That(attributes.AccuracyScore, Is.EqualTo(300));
            Assert.That(attributes.BonusScore, Is.GreaterThan(0));
            Assert.That(attributes.BonusScoreRatio, Is.GreaterThan(0));
            Assert.That(attributes.MaxCombo, Is.EqualTo(1)); // only the spinner itself
        }

        // Longer spinner -> strictly more bonus score (more half spins possible).
        [Test]
        public void TestSimulate_LongerSpinner_GivesMoreBonus()
        {
            var shorter = simulate(new[] { DifficultyTestHelpers.Spinner(0, 3000) });
            var longer = simulate(new[] { DifficultyTestHelpers.Spinner(0, 6000) });

            Assert.That(longer.BonusScore, Is.GreaterThan(shorter.BonusScore));
        }

        // Bonus score composed only of 100 (tick) and 1100 (bonus tick) increments.
        [Test]
        public void TestSimulate_SpinnerBonusScore_IsCombinationOfTickValues()
        {
            var attributes = simulate(new[] { DifficultyTestHelpers.Spinner(0, 5000) });

            // 100a + 1100b with a,b >= 0: always a multiple of 100.
            Assert.That(attributes.BonusScore % 100, Is.Zero);
        }

        // Mixed beatmap: circle after a spinner gets combo score; slider counts as IHasPath, spinner as IHasDuration, circle default.
        [Test]
        public void TestSimulate_MixedObjects_AccumulatesAccuracyOfEachKind()
        {
            var attributes = simulate(new OsuHitObject[]
            {
                DifficultyTestHelpers.Circle(0, 100, 100),
                DifficultyTestHelpers.Slider(500, 100, 200, 10),
                DifficultyTestHelpers.Spinner(1500, 0),
            });

            Assert.That(attributes.AccuracyScore, Is.EqualTo(300 + (30 + 30 + 300) + 300));
        }

        // Non-empty beatmap without breaks: drain-length branch (Count > 0) runs with empty Breaks. Break subtraction value not asserted.
        [Test]
        public void TestSimulate_BeatmapWithMultipleObjects_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => simulate(DifficultyTestHelpers.Stream(10, 2000, 50)));
        }

        // ---- GetLegacyScoreMultiplier ----

        [Test]
        public void TestMultiplier_NoMods_IsOne() => Assert.That(multiplier(), Is.EqualTo(1.0));

        [Test]
        public void TestMultiplier_NoFail_HalfWithoutScoreV2() => Assert.That(multiplier(new OsuModNoFail()), Is.EqualTo(0.5));

        [Test]
        public void TestMultiplier_NoFail_FullWithScoreV2() => Assert.That(multiplier(new OsuModNoFail(), new ModScoreV2()), Is.EqualTo(1.0));

        [Test]
        public void TestMultiplier_Easy_Half() => Assert.That(multiplier(new OsuModEasy()), Is.EqualTo(0.5));

        [Test]
        public void TestMultiplier_HalfTime_Point3() => Assert.That(multiplier(new OsuModHalfTime()), Is.EqualTo(0.3));

        [Test]
        public void TestMultiplier_Daycore_Point3() => Assert.That(multiplier(new OsuModDaycore()), Is.EqualTo(0.3));

        [Test]
        public void TestMultiplier_Hidden() => Assert.That(multiplier(new OsuModHidden()), Is.EqualTo(1.06));

        [Test]
        public void TestMultiplier_HardRock_ScoreV1() => Assert.That(multiplier(new OsuModHardRock()), Is.EqualTo(1.06));

        [Test]
        public void TestMultiplier_HardRock_ScoreV2() => Assert.That(multiplier(new OsuModHardRock(), new ModScoreV2()), Is.EqualTo(1.10));

        [Test]
        public void TestMultiplier_DoubleTime_ScoreV1() => Assert.That(multiplier(new OsuModDoubleTime()), Is.EqualTo(1.12));

        [Test]
        public void TestMultiplier_Nightcore_ScoreV2() => Assert.That(multiplier(new OsuModNightcore(), new ModScoreV2()), Is.EqualTo(1.20));

        [Test]
        public void TestMultiplier_Flashlight() => Assert.That(multiplier(new OsuModFlashlight()), Is.EqualTo(1.12));

        [Test]
        public void TestMultiplier_SpunOut() => Assert.That(multiplier(new OsuModSpunOut()), Is.EqualTo(0.9));

        [Test]
        public void TestMultiplier_Relax_ReturnsZero() => Assert.That(multiplier(new OsuModRelax()), Is.Zero);

        [Test]
        public void TestMultiplier_Autopilot_ReturnsZero() => Assert.That(multiplier(new OsuModAutopilot()), Is.Zero);

        // Relax early-returns even when it comes after other mods (loop over mods).
        [Test]
        public void TestMultiplier_RelaxAfterOtherMods_StillZero() => Assert.That(multiplier(new OsuModHidden(), new OsuModRelax()), Is.Zero);

        // Mod without a case (default fallthrough) leaves multiplier untouched; combined mods multiply.
        [Test]
        public void TestMultiplier_UnlistedModIgnored_AndModsMultiply()
        {
            Assert.That(multiplier(new OsuModSuddenDeath()), Is.EqualTo(1.0));
            Assert.That(multiplier(new OsuModHidden(), new OsuModDoubleTime()), Is.EqualTo(1.06 * 1.12).Within(1e-12));
        }
    }
}
