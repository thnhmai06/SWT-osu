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
        private TestProcessor processor = null!;

        [SetUp]
        public void SetUp()
        {
            processor = new TestProcessor();
            processor.ApplyBeatmap(new OsuBeatmap
            {
                BeatmapInfo = { Difficulty = { DrainRate = 5 } },
                HitObjects = { new HitCircle() }
            });
        }

        private static Slider createSlider(int repeatCount)
        {
            var slider = new Slider
            {
                StartTime = 0,
                RepeatCount = repeatCount,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(200, 0) })
            };
            slider.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            return slider;
        }

        private static Spinner createSpinner()
        {
            var spinner = new Spinner { StartTime = 0, Duration = 2000 };
            spinner.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            return spinner;
        }

        // ---- EnumerateNestedHitObjects switch ----

        [Test]
        public void TestNested_Slider_YieldsAllNestedObjects()
        {
            // switch case Slider
            var slider = createSlider(1);

            Assert.That(processor.Nested(slider).ToList(), Is.EqualTo(slider.NestedHitObjects.ToList()));
            Assert.That(slider.NestedHitObjects.Count, Is.GreaterThan(0));
        }

        [Test]
        public void TestNested_Spinner_ExcludesBonusTicks()
        {
            // switch case Spinner; Where predicate TRUE for SpinnerTick, FALSE for SpinnerBonusTick
            var spinner = createSpinner();
            var nested = processor.Nested(spinner).ToList();

            Assert.That(spinner.NestedHitObjects.OfType<SpinnerBonusTick>(), Is.Not.Empty);
            Assert.That(spinner.NestedHitObjects.OfType<SpinnerTick>().Any(), Is.True);
            Assert.That(nested.OfType<SpinnerBonusTick>(), Is.Empty);
            Assert.That(nested, Is.EqualTo(spinner.NestedHitObjects.Where(t => t is not SpinnerBonusTick).ToList()));
        }

        [Test]
        public void TestNested_HitCircle_YieldsNothing()
        {
            // switch: no case matches
            Assert.That(processor.Nested(new HitCircle()), Is.Empty);
        }

        [Test]
        public void TestTopLevel_ReturnsBeatmapHitObjects()
        {
            Assert.That(processor.TopLevel().Count(), Is.EqualTo(1));
        }

        // ---- GetHealthIncreaseFor switch ----

        [TestCase(HitResult.SmallTickMiss, -0.075)]
        [TestCase(HitResult.LargeTickMiss, -0.075)]
        [TestCase(HitResult.Miss, -0.125)]
        public void TestIncrease_MissResults_AreReturnedWithoutMultiplier(HitResult type, double expected)
        {
            // early-return cases (no HpMultiplierNormal)
            Assert.That(processor.Increase(new HitCircle(), type), Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void TestIncrease_ResultsScaleRelativeToEachOther()
        {
            // all non-miss cases are base * HpMultiplierNormal, so ratios are multiplier independent
            double great = processor.Increase(new HitCircle(), HitResult.Great);

            Assert.That(processor.Increase(new HitCircle(), HitResult.Ok) / great, Is.EqualTo(0.011 / 0.03).Within(1e-9));
            Assert.That(processor.Increase(new HitCircle(), HitResult.Meh) / great, Is.EqualTo(0.002 / 0.03).Within(1e-9));
            Assert.That(processor.Increase(new HitCircle(), HitResult.SmallBonus) / great, Is.EqualTo(0.0085 / 0.03).Within(1e-9));
            Assert.That(processor.Increase(new HitCircle(), HitResult.LargeBonus) / great, Is.EqualTo(0.01 / 0.03).Within(1e-9));
            Assert.That(processor.Increase(new HitCircle(), HitResult.SmallTickHit) / great, Is.EqualTo(0.02 / 0.03).Within(1e-9));
        }

        [Test]
        public void TestIncrease_LargeTickHitOnSliderTick_UsesTickValue()
        {
            // ternary `hitObject is SliderTick` TRUE
            double great = processor.Increase(new HitCircle(), HitResult.Great);

            Assert.That(processor.Increase(new SliderTick(), HitResult.LargeTickHit) / great, Is.EqualTo(0.015 / 0.03).Within(1e-9));
        }

        [Test]
        public void TestIncrease_LargeTickHitOnRepeat_UsesRepeatValue()
        {
            // ternary `hitObject is SliderTick` FALSE
            double great = processor.Increase(new HitCircle(), HitResult.Great);

            Assert.That(processor.Increase(new SliderRepeat(new Slider()), HitResult.LargeTickHit) / great, Is.EqualTo(0.02 / 0.03).Within(1e-9));
        }

        [Test]
        public void TestIncrease_SliderTailHit_UsesRepeatValue()
        {
            // SliderTailHit shares the LargeTickHit case
            double great = processor.Increase(new HitCircle(), HitResult.Great);

            Assert.That(processor.Increase(new SliderTailCircle(new Slider()), HitResult.SliderTailHit) / great, Is.EqualTo(0.02 / 0.03).Within(1e-9));
        }

        [Test]
        public void TestIncrease_UnlistedResult_IsZero()
        {
            // no case matches: increase stays 0
            Assert.That(processor.Increase(new HitCircle(), HitResult.Perfect), Is.EqualTo(0));
        }

        private class TestProcessor : OsuLegacyHealthProcessor
        {
            public TestProcessor()
                : base(0)
            {
            }

            public IEnumerable<HitObject> Nested(HitObject hitObject) => EnumerateNestedHitObjects(hitObject);
            public IEnumerable<HitObject> TopLevel() => EnumerateTopLevelHitObjects();
            public double Increase(HitObject hitObject, HitResult result) => GetHealthIncreaseFor(hitObject, result);
        }
    }
}
