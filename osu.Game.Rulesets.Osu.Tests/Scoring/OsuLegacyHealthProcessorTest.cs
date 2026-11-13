// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
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
    public class OsuLegacyHealthProcessorTest
    {
        private Probe processor = null!;
        private OsuBeatmap beatmap = null!;

        [SetUp]
        public void SetUp()
        {
            beatmap = new OsuBeatmap { BeatmapInfo = new BeatmapInfo { Difficulty = new BeatmapDifficulty { DrainRate = 5 } } };
            beatmap.HitObjects.Add(new HitCircle { StartTime = 1000 });
            beatmap.HitObjects.Add(new HitCircle { StartTime = 2000 });
            beatmap.HitObjects.Add(new HitCircle { StartTime = 3000 });
            foreach (var h in beatmap.HitObjects)
                h.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            processor = new Probe();
            processor.ApplyBeatmap(beatmap);
        }

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

        // CF: OsuLegacyHealthProcessor.EnumerateTopLevelHitObjects | returns Beatmap.HitObjects | non-null
        [Test]
        public void TestTopLevelObjectsAreBeatmapObjects()
        {
            Assert.That(processor.TopLevel(), Is.EqualTo(beatmap.HitObjects));
        }

        // CF: OsuLegacyHealthProcessor.EnumerateNestedHitObjects | switch (hitObject) | case Slider
        // CF: OsuLegacyHealthProcessor.EnumerateNestedHitObjects | foreach nested in slider.NestedHitObjects | loop n
        [Test]
        public void TestSliderYieldsAllNestedObjects()
        {
            var slider = createSlider();
            Assert.That(processor.Nested(slider), Is.EqualTo(slider.NestedHitObjects));
        }

        // CF: OsuLegacyHealthProcessor.EnumerateNestedHitObjects | switch (hitObject) | case Spinner
        // CF: OsuLegacyHealthProcessor.EnumerateNestedHitObjects | t is not SpinnerBonusTick | TRUE
        // CF: OsuLegacyHealthProcessor.EnumerateNestedHitObjects | t is not SpinnerBonusTick | FALSE
        [Test]
        public void TestSpinnerYieldsOnlyNonBonusTicks()
        {
            var spinner = new Spinner { StartTime = 0, Duration = 2000 };
            spinner.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty { OverallDifficulty = 5 });

            var nested = processor.Nested(spinner).ToList();

            Assert.That(spinner.NestedHitObjects.OfType<SpinnerBonusTick>(), Is.Not.Empty);
            Assert.That(nested, Is.Not.Empty);
            Assert.That(nested.OfType<SpinnerBonusTick>(), Is.Empty);
        }

        // CF: OsuLegacyHealthProcessor.EnumerateNestedHitObjects | switch (hitObject) | default
        [Test]
        public void TestCircleHasNoNestedObjects()
        {
            Assert.That(processor.Nested(new HitCircle()), Is.Empty);
        }

        // CF: OsuLegacyHealthProcessor.GetHealthIncreaseFor | switch (result) | case SmallTickMiss
        // CF: OsuLegacyHealthProcessor.GetHealthIncreaseFor | switch (result) | case LargeTickMiss
        // CF: OsuLegacyHealthProcessor.GetHealthIncreaseFor | switch (result) | case Miss
        [Test]
        public void TestMissValuesAreNotScaled()
        {
            var obj = new HitCircle();

            Assert.That(processor.Increase(obj, HitResult.SmallTickMiss), Is.EqualTo(-0.075).Within(1e-9));
            Assert.That(processor.Increase(obj, HitResult.LargeTickMiss), Is.EqualTo(-0.075).Within(1e-9));
            Assert.That(processor.Increase(obj, HitResult.Miss), Is.EqualTo(-0.125).Within(1e-9));
        }

        // CF: OsuLegacyHealthProcessor.GetHealthIncreaseFor | switch (result) | case SmallTickHit
        // CF: OsuLegacyHealthProcessor.GetHealthIncreaseFor | switch (result) | case Meh
        // CF: OsuLegacyHealthProcessor.GetHealthIncreaseFor | switch (result) | case Ok
        // CF: OsuLegacyHealthProcessor.GetHealthIncreaseFor | switch (result) | case Great
        // CF: OsuLegacyHealthProcessor.GetHealthIncreaseFor | switch (result) | case SmallBonus
        // CF: OsuLegacyHealthProcessor.GetHealthIncreaseFor | switch (result) | case LargeBonus
        [Test]
        public void TestHitValuesAreScaledByMultiplier()
        {
            var obj = new HitCircle();
            double m = processor.Multiplier;

            Assert.That(m, Is.GreaterThan(0));
            Assert.That(processor.Increase(obj, HitResult.SmallTickHit), Is.EqualTo(m * 0.02).Within(1e-9));
            Assert.That(processor.Increase(obj, HitResult.Meh), Is.EqualTo(m * 0.002).Within(1e-9));
            Assert.That(processor.Increase(obj, HitResult.Ok), Is.EqualTo(m * 0.011).Within(1e-9));
            Assert.That(processor.Increase(obj, HitResult.Great), Is.EqualTo(m * 0.03).Within(1e-9));
            Assert.That(processor.Increase(obj, HitResult.SmallBonus), Is.EqualTo(m * 0.0085).Within(1e-9));
            Assert.That(processor.Increase(obj, HitResult.LargeBonus), Is.EqualTo(m * 0.01).Within(1e-9));
        }

        // CF: OsuLegacyHealthProcessor.GetHealthIncreaseFor | hitObject is SliderTick | TRUE
        // CF: OsuLegacyHealthProcessor.GetHealthIncreaseFor | hitObject is SliderTick | FALSE
        [Test]
        public void TestTickAndRepeatValues()
        {
            var slider = createSlider();
            double m = processor.Multiplier;

            Assert.That(processor.Increase(slider.NestedHitObjects.OfType<SliderTick>().First(), HitResult.LargeTickHit), Is.EqualTo(m * 0.015).Within(1e-9));
            Assert.That(processor.Increase(slider.NestedHitObjects.OfType<SliderRepeat>().First(), HitResult.LargeTickHit), Is.EqualTo(m * 0.02).Within(1e-9));
            Assert.That(processor.Increase(slider.TailCircle, HitResult.SliderTailHit), Is.EqualTo(m * 0.02).Within(1e-9));
        }

        // CF: OsuLegacyHealthProcessor.GetHealthIncreaseFor | switch (result) | default
        [Test]
        public void TestUnhandledResultGivesNoHealth()
        {
            Assert.That(processor.Increase(new HitCircle(), HitResult.IgnoreHit), Is.EqualTo(0));
        }

        private class Probe : OsuLegacyHealthProcessor
        {
            public Probe()
                : base(0)
            {
            }

            public double Multiplier => HpMultiplierNormal;

            public IEnumerable<HitObject> TopLevel() => EnumerateTopLevelHitObjects();

            public IEnumerable<HitObject> Nested(HitObject obj) => EnumerateNestedHitObjects(obj);

            public double Increase(HitObject obj, HitResult result) => GetHealthIncreaseFor(obj, result);
        }
    }
}
