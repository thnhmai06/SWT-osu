// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Judgements;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Osu.Tests.Scoring
{
    [TestFixture]
    public class OsuHealthProcessorTest
    {
        // DrainRate 5 is the middle of every DifficultyRange used by the processor.
        private const double starting_health = 0.5;

        private OsuHealthProcessor processor = null!;

        [SetUp]
        public void SetUp()
        {
            processor = new OsuHealthProcessor(0);
            processor.ApplyBeatmap(new OsuBeatmap
            {
                BeatmapInfo = { Difficulty = { DrainRate = 5 } },
                HitObjects = { new HitCircle() }
            });
            processor.Health.Value = starting_health;
        }

        private double apply(HitObject hitObject, HitResult type)
        {
            double before = processor.Health.Value;
            var result = new JudgementResult(hitObject, hitObject.CreateJudgement()) { Type = type };
            processor.ApplyResult(result);
            return processor.Health.Value - before;
        }

        private static HitCircle circle(bool newCombo = false, bool last = false) => new HitCircle { NewCombo = newCombo, LastInCombo = last };

        // ---- non-combo object guard clause ----

        [Test]
        public void TestObjectWithoutComboInformation_ReturnsPlainIncrease()
        {
            // `result.HitObject is not IHasComboInformation` TRUE -> early return, no combo bonus
            Assert.That(apply(new PlainHitObject(), HitResult.Great), Is.EqualTo(0.03).Within(1e-9));
        }

        // ---- getHealthIncreaseFor switch (hit object is a combo object, not last in combo) ----

        [TestCase(HitResult.SmallTickMiss, -0.075)]
        [TestCase(HitResult.LargeTickMiss, -0.075)]
        [TestCase(HitResult.Miss, -0.125)]
        [TestCase(HitResult.SmallTickHit, 0.02)]
        [TestCase(HitResult.Meh, 0.002)]
        [TestCase(HitResult.Ok, 0.011)]
        [TestCase(HitResult.Great, 0.03)]
        [TestCase(HitResult.SmallBonus, 0.0085)]
        [TestCase(HitResult.LargeBonus, 0.01)]
        public void TestHealthIncrease_ResultCase(HitResult type, double expected)
        {
            // one test per outer switch case
            Assert.That(apply(circle(), type), Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void TestHealthIncrease_LargeTickHitOnSliderTick()
        {
            // inner switch: SliderTick case
            Assert.That(apply(new SliderTick(), HitResult.LargeTickHit), Is.EqualTo(0.015).Within(1e-9));
        }

        [Test]
        public void TestHealthIncrease_SliderTailHitOnSliderTailCircle()
        {
            // inner switch: SliderTailCircle case (shared with head/repeat)
            Assert.That(apply(new SliderTailCircle(new Slider()), HitResult.SliderTailHit), Is.EqualTo(0.02).Within(1e-9));
        }

        [Test]
        public void TestHealthIncrease_LargeTickHitOnSliderRepeat()
        {
            // inner switch: SliderRepeat case
            Assert.That(apply(new SliderRepeat(new Slider()), HitResult.LargeTickHit), Is.EqualTo(0.02).Within(1e-9));
        }

        [Test]
        public void TestHealthIncrease_LargeTickHitOnSliderHead()
        {
            // inner switch: SliderHeadCircle case
            Assert.That(apply(new SliderHeadCircle(), HitResult.LargeTickHit), Is.EqualTo(0.02).Within(1e-9));
        }

        [Test]
        public void TestHealthIncrease_LargeTickHitOnUnrelatedObject_FallsBackToJudgement()
        {
            // inner switch: no case matches -> break -> base.GetHealthIncreaseFor
            var hitObject = circle();
            var result = new JudgementResult(hitObject, hitObject.CreateJudgement()) { Type = HitResult.LargeTickHit };
            double expected = result.HealthIncrease;

            Assert.That(apply(hitObject, HitResult.LargeTickHit), Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void TestHealthIncrease_UnlistedResult_FallsBackToJudgement()
        {
            // outer switch: no case matches -> base.GetHealthIncreaseFor
            var hitObject = circle();
            var result = new JudgementResult(hitObject, hitObject.CreateJudgement()) { Type = HitResult.Perfect };
            double expected = result.HealthIncrease;

            Assert.That(apply(hitObject, HitResult.Perfect), Is.EqualTo(expected).Within(1e-9));
        }

        // ---- combo bonus: LastInCombo && IsHit ----

        [Test]
        public void TestComboBonus_PerfectCombo_AddsLargestBonus()
        {
            // LastInCombo TRUE, IsHit TRUE, currentComboResult == Perfect
            Assert.That(apply(circle(newCombo: true, last: true), HitResult.Great), Is.EqualTo(0.03 + 0.07).Within(1e-9));
        }

        [Test]
        public void TestComboBonus_GoodCombo_AddsMediumBonus()
        {
            // Ok earlier in combo -> Good; last Great gets Good bonus
            apply(circle(newCombo: true), HitResult.Ok);
            Assert.That(apply(circle(last: true), HitResult.Great), Is.EqualTo(0.03 + 0.05).Within(1e-9));
        }

        [Test]
        public void TestComboBonus_LargeTickMissInCombo_AddsMediumBonus()
        {
            // LargeTickMiss is the second label of the Good case
            apply(circle(newCombo: true), HitResult.LargeTickMiss);
            Assert.That(apply(circle(last: true), HitResult.Great), Is.EqualTo(0.03 + 0.05).Within(1e-9));
        }

        [Test]
        public void TestComboBonus_MissInCombo_AddsSmallestBonus()
        {
            // Miss earlier in combo -> None; switch default branch
            apply(circle(newCombo: true), HitResult.Miss);
            Assert.That(apply(circle(last: true), HitResult.Great), Is.EqualTo(0.03 + 0.03).Within(1e-9));
        }

        [Test]
        public void TestComboBonus_MehInCombo_AddsSmallestBonus()
        {
            // Meh is the second label of the None case
            apply(circle(newCombo: true), HitResult.Meh);
            Assert.That(apply(circle(last: true), HitResult.Great), Is.EqualTo(0.03 + 0.03).Within(1e-9));
        }

        [Test]
        public void TestComboBonus_NewComboResetsToPerfect()
        {
            // combo.NewCombo TRUE resets state even after a miss
            apply(circle(newCombo: true), HitResult.Miss);
            Assert.That(apply(circle(newCombo: true, last: true), HitResult.Great), Is.EqualTo(0.03 + 0.07).Within(1e-9));
        }

        [Test]
        public void TestComboBonus_NotLastInCombo_NoBonus()
        {
            // LastInCombo FALSE (short-circuits IsHit)
            Assert.That(apply(circle(newCombo: true, last: false), HitResult.Great), Is.EqualTo(0.03).Within(1e-9));
        }

        [Test]
        public void TestComboBonus_LastInComboButMiss_NoBonus()
        {
            // LastInCombo TRUE, IsHit FALSE
            Assert.That(apply(circle(newCombo: true, last: true), HitResult.Miss), Is.EqualTo(-0.125).Within(1e-9));
        }

        [Test]
        public void TestSliderTailNotHit_DowngradesComboToGood()
        {
            // SliderTailCircle TRUE && !IsHit TRUE
            apply(new SliderTailCircle(new Slider()) { NewCombo = true }, HitResult.IgnoreMiss);
            Assert.That(apply(circle(last: true), HitResult.Great), Is.EqualTo(0.03 + 0.05).Within(1e-9));
        }

        [Test]
        public void TestSliderTailHit_DoesNotDowngradeCombo()
        {
            // SliderTailCircle TRUE && !IsHit FALSE
            apply(new SliderTailCircle(new Slider()) { NewCombo = true }, HitResult.SliderTailHit);
            Assert.That(apply(circle(last: true), HitResult.Great), Is.EqualTo(0.03 + 0.07).Within(1e-9));
        }

        [Test]
        public void TestNonTailNotHit_DoesNotTriggerTailRule()
        {
            // SliderTailCircle FALSE (short-circuits !IsHit); IgnoreMiss on a circle leaves combo Perfect
            apply(new HitCircle { NewCombo = true }, HitResult.IgnoreMiss);
            Assert.That(apply(circle(last: true), HitResult.Great), Is.EqualTo(0.03 + 0.07).Within(1e-9));
        }

        // ---- Reset ----

        [Test]
        public void TestReset_RestoresPerfectCombo()
        {
            apply(circle(newCombo: true), HitResult.Miss);

            // re-applying the beatmap calls Reset, which must clear the stored combo result
            processor.ApplyBeatmap(new OsuBeatmap { HitObjects = { new HitCircle() } });
            processor.Health.Value = starting_health;

            Assert.That(apply(circle(last: true), HitResult.Great), Is.EqualTo(0.03 + 0.07).Within(1e-9));
        }

        private class PlainHitObject : HitObject
        {
            public override Judgement CreateJudgement() => new OsuJudgement();
        }
    }
}
