// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Scoring
{
    [TestFixture]
    public class OsuHealthProcessorTest
    {
        private Probe processor = null!;

        [SetUp]
        public void SetUp()
        {
            processor = new Probe();
            processor.ApplyBeatmap(createBeatmap());
            processor.Calls.Clear();
        }

        private static OsuBeatmap createBeatmap()
        {
            var circle = new HitCircle { StartTime = 1000, NewCombo = true, LastInCombo = true };
            var beatmap = new OsuBeatmap { BeatmapInfo = new BeatmapInfo { Difficulty = new BeatmapDifficulty { DrainRate = 5 } } };
            circle.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);
            beatmap.HitObjects.Add(circle);
            return beatmap;
        }

        private static JudgementResult result(HitObject obj, HitResult type) => new JudgementResult(obj, obj.Judgement) { Type = type };

        private static Slider createSlider()
        {
            var slider = new Slider
            {
                StartTime = 0,
                RepeatCount = 1,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(300, 0) }),
            };
            slider.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            return slider;
        }

        private static HitCircle circle(bool newCombo = false, bool last = false) => new HitCircle { NewCombo = newCombo, LastInCombo = last };

        // CF: OsuHealthProcessor.GetHealthIncreaseFor | IsSimulating | TRUE
        [Test]
        public void TestSimulationSkipsComboBonus()
        {
            var simulated = new Probe();
            simulated.ApplyBeatmap(createBeatmap());

            Assert.That(simulated.Calls, Is.Not.Empty);
            Assert.That(simulated.Calls, Has.All.Matches<(bool simulating, double value)>(c => c.simulating && c.value == 0.03));
        }

        // CF: OsuHealthProcessor.GetHealthIncreaseFor | IsSimulating | FALSE
        // CF: OsuHealthProcessor.GetHealthIncreaseFor | result.HitObject is not IHasComboInformation | TRUE
        [Test]
        public void TestObjectWithoutComboInformation()
        {
            Assert.That(processor.Increase(result(new PlainObject(), HitResult.Great)), Is.EqualTo(0.03));
        }

        // CF: OsuHealthProcessor.GetHealthIncreaseFor | result.HitObject is not IHasComboInformation | FALSE
        // CF: OsuHealthProcessor.GetHealthIncreaseFor | combo.NewCombo | FALSE
        // CF: OsuHealthProcessor.GetHealthIncreaseFor | combo.LastInCombo && result.Type.IsHit() | FALSE
        [Test]
        public void TestMidComboObjectGetsNoBonus()
        {
            Assert.That(processor.Increase(result(circle(), HitResult.Great)), Is.EqualTo(0.03));
        }

        // CF: OsuHealthProcessor.GetHealthIncreaseFor | combo.NewCombo | TRUE
        // CF: OsuHealthProcessor.GetHealthIncreaseFor | combo.LastInCombo | TRUE
        // CF: OsuHealthProcessor.GetHealthIncreaseFor | result.Type.IsHit() | TRUE
        // CF: OsuHealthProcessor.GetHealthIncreaseFor | switch (currentComboResult) | case Perfect
        [Test]
        public void TestPerfectComboBonus()
        {
            Assert.That(processor.Increase(result(circle(newCombo: true, last: true), HitResult.Great)), Is.EqualTo(0.03 + 0.07).Within(1e-9));
        }

        // CF: OsuHealthProcessor.GetHealthIncreaseFor | switch (result.Type) | case Ok
        // CF: OsuHealthProcessor.GetHealthIncreaseFor | switch (currentComboResult) | case Good
        [Test]
        public void TestGoodComboBonusAfterOk()
        {
            processor.Increase(result(circle(newCombo: true), HitResult.Ok));
            Assert.That(processor.Increase(result(circle(last: true), HitResult.Great)), Is.EqualTo(0.03 + 0.05).Within(1e-9));
        }

        // CF: OsuHealthProcessor.GetHealthIncreaseFor | switch (result.Type) | case LargeTickMiss
        [Test]
        public void TestGoodComboBonusAfterLargeTickMiss()
        {
            processor.Increase(result(circle(newCombo: true), HitResult.LargeTickMiss));
            Assert.That(processor.Increase(result(circle(last: true), HitResult.Great)), Is.EqualTo(0.03 + 0.05).Within(1e-9));
        }

        // CF: OsuHealthProcessor.GetHealthIncreaseFor | switch (result.Type) | case Meh
        // CF: OsuHealthProcessor.GetHealthIncreaseFor | switch (currentComboResult) | default
        [Test]
        public void TestLowestComboBonusAfterMeh()
        {
            processor.Increase(result(circle(newCombo: true), HitResult.Meh));
            Assert.That(processor.Increase(result(circle(last: true), HitResult.Great)), Is.EqualTo(0.03 + 0.03).Within(1e-9));
        }

        // CF: OsuHealthProcessor.GetHealthIncreaseFor | switch (result.Type) | case Miss
        // CF: OsuHealthProcessor.GetHealthIncreaseFor | result.Type.IsHit() | FALSE
        [Test]
        public void TestMissOnLastObjectGetsNoBonus()
        {
            Assert.That(processor.Increase(result(circle(newCombo: true, last: true), HitResult.Miss)), Is.EqualTo(-0.125).Within(1e-9));
        }

        // CF: OsuHealthProcessor.GetHealthIncreaseFor | switch (result.Type) | default
        [Test]
        public void TestOtherResultsDoNotAffectComboResult()
        {
            Assert.That(processor.Increase(result(circle(newCombo: true, last: true), HitResult.Great)), Is.EqualTo(0.03 + 0.07).Within(1e-9));
        }

        // CF: OsuHealthProcessor.GetHealthIncreaseFor | result.HitObject is SliderTailCircle && !result.IsHit | TRUE
        [Test]
        public void TestMissedSliderTailDowngradesComboToGood()
        {
            var slider = createSlider();
            processor.Increase(result(circle(newCombo: true), HitResult.Great));
            processor.Increase(result(slider.TailCircle, HitResult.IgnoreMiss));

            Assert.That(processor.Increase(result(circle(last: true), HitResult.Great)), Is.EqualTo(0.03 + 0.05).Within(1e-9));
        }

        // CF: OsuHealthProcessor.GetHealthIncreaseFor | result.HitObject is SliderTailCircle | FALSE
        // CF: OsuHealthProcessor.GetHealthIncreaseFor | !result.IsHit | FALSE
        [Test]
        public void TestHitSliderTailKeepsComboPerfect()
        {
            var slider = createSlider();
            processor.Increase(result(circle(newCombo: true), HitResult.Great));
            processor.Increase(result(slider.TailCircle, HitResult.SliderTailHit));

            Assert.That(processor.Increase(result(circle(last: true), HitResult.Great)), Is.EqualTo(0.03 + 0.07).Within(1e-9));
        }

        // CF: OsuHealthProcessor.Reset | currentComboResult reset | TRUE
        [Test]
        public void TestResetRestoresPerfectCombo()
        {
            processor.Increase(result(circle(newCombo: true), HitResult.Miss));
            processor.DoReset();

            Assert.That(processor.Increase(result(circle(last: true), HitResult.Great)), Is.EqualTo(0.03 + 0.07).Within(1e-9));
        }

        // CF: OsuHealthProcessor.getHealthIncreaseFor | switch (result.Type) | case SmallTickMiss
        // CF: OsuHealthProcessor.getHealthIncreaseFor | switch (result.Type) | case LargeTickMiss
        // CF: OsuHealthProcessor.getHealthIncreaseFor | switch (result.Type) | case Miss
        // CF: OsuHealthProcessor.getHealthIncreaseFor | switch (result.Type) | case SmallTickHit
        // CF: OsuHealthProcessor.getHealthIncreaseFor | switch (result.Type) | case Meh
        // CF: OsuHealthProcessor.getHealthIncreaseFor | switch (result.Type) | case Ok
        // CF: OsuHealthProcessor.getHealthIncreaseFor | switch (result.Type) | case Great
        // CF: OsuHealthProcessor.getHealthIncreaseFor | switch (result.Type) | case SmallBonus
        // CF: OsuHealthProcessor.getHealthIncreaseFor | switch (result.Type) | case LargeBonus
        [Test]
        public void TestFlatHealthValuesAtDrain5()
        {
            var obj = new PlainObject();

            Assert.That(processor.Increase(result(obj, HitResult.SmallTickMiss)), Is.EqualTo(-0.075).Within(1e-9));
            Assert.That(processor.Increase(result(obj, HitResult.LargeTickMiss)), Is.EqualTo(-0.075).Within(1e-9));
            Assert.That(processor.Increase(result(obj, HitResult.Miss)), Is.EqualTo(-0.125).Within(1e-9));
            Assert.That(processor.Increase(result(obj, HitResult.SmallTickHit)), Is.EqualTo(0.02));
            Assert.That(processor.Increase(result(obj, HitResult.Meh)), Is.EqualTo(0.002));
            Assert.That(processor.Increase(result(obj, HitResult.Ok)), Is.EqualTo(0.011));
            Assert.That(processor.Increase(result(obj, HitResult.Great)), Is.EqualTo(0.03));
            Assert.That(processor.Increase(result(obj, HitResult.SmallBonus)), Is.EqualTo(0.0085));
            Assert.That(processor.Increase(result(obj, HitResult.LargeBonus)), Is.EqualTo(0.01));
        }

        // CF: OsuHealthProcessor.getHealthIncreaseFor | switch (result.HitObject) | case SliderTick
        // CF: OsuHealthProcessor.getHealthIncreaseFor | switch (result.HitObject) | case SliderHeadCircle
        // CF: OsuHealthProcessor.getHealthIncreaseFor | switch (result.HitObject) | case SliderTailCircle
        // CF: OsuHealthProcessor.getHealthIncreaseFor | switch (result.HitObject) | case SliderRepeat
        [Test]
        public void TestSliderPartHealthValues()
        {
            var slider = createSlider();

            Assert.That(processor.Increase(result(slider.NestedHitObjects.OfType<SliderTick>().First(), HitResult.LargeTickHit)), Is.EqualTo(0.015));
            Assert.That(processor.Increase(result(slider.NestedHitObjects.OfType<SliderRepeat>().First(), HitResult.LargeTickHit)), Is.EqualTo(0.02));
            Assert.That(processor.Increase(result(slider.HeadCircle, HitResult.LargeTickHit)), Is.EqualTo(0.02));
            Assert.That(processor.Increase(result(slider.TailCircle, HitResult.SliderTailHit)), Is.EqualTo(0.02));
        }

        // CF: OsuHealthProcessor.getHealthIncreaseFor | switch (result.HitObject) | default
        // CF: OsuHealthProcessor.getHealthIncreaseFor | switch (result.Type) | default
        [Test]
        public void TestUnhandledCombinationsFallBackToJudgementValue()
        {
            var plain = new PlainObject();
            var tickHitOnPlainObject = result(plain, HitResult.LargeTickHit);
            var ignoreHit = result(plain, HitResult.IgnoreHit);

            Assert.That(processor.Increase(tickHitOnPlainObject), Is.EqualTo(tickHitOnPlainObject.Judgement.HealthIncreaseFor(tickHitOnPlainObject)));
            Assert.That(processor.Increase(ignoreHit), Is.EqualTo(ignoreHit.Judgement.HealthIncreaseFor(ignoreHit)));
        }

        private class PlainObject : HitObject
        {
        }

        private class Probe : OsuHealthProcessor
        {
            public readonly List<(bool simulating, double value)> Calls = new List<(bool, double)>();

            public Probe()
                : base(0)
            {
            }

            public double Increase(JudgementResult result) => GetHealthIncreaseFor(result);

            public void DoReset() => Reset(false);

            protected override double GetHealthIncreaseFor(JudgementResult result)
            {
                double value = base.GetHealthIncreaseFor(result);
                Calls.Add((IsSimulating, value));
                return value;
            }
        }
    }
}
