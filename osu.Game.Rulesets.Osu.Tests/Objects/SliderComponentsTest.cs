// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Judgements;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Objects
{
    [TestFixture]
    public class SliderComponentsTest
    {
        private static readonly BeatmapDifficulty difficulty = new BeatmapDifficulty { SliderMultiplier = 2, SliderTickRate = 1, ApproachRate = 5 };

        private static Slider createSlider()
        {
            var slider = new Slider
            {
                StartTime = 0,
                RepeatCount = 1,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(400, 0) })
            };
            slider.ApplyDefaults(new ControlPointInfo(), difficulty);
            return slider;
        }

        // ---- SliderEndCircle.ApplyDefaultsToSelf: RepeatIndex > 0 TRUE / FALSE ----

        [Test]
        public void TestEndCircle_FirstSpan_PreemptExtendedByDistanceFromSliderStart()
        {
            // RepeatIndex == 0: TimePreempt += StartTime - Slider.StartTime
            var repeat = createSlider().NestedHitObjects.OfType<SliderRepeat>().Single();

            Assert.That(repeat.RepeatIndex, Is.EqualTo(0));
            Assert.That(repeat.TimePreempt, Is.EqualTo(1200 + repeat.StartTime).Within(1e-6));
        }

        [Test]
        public void TestEndCircle_LaterSpan_FadeInRemovedAndPreemptIsTwoSpans()
        {
            // RepeatIndex > 0: TimeFadeIn = 0, TimePreempt = SpanDuration * 2
            var slider = createSlider();
            var tail = slider.TailCircle;

            Assert.That(tail.RepeatIndex, Is.GreaterThan(0));
            Assert.That(tail.TimeFadeIn, Is.EqualTo(0));
            Assert.That(tail.TimePreempt, Is.EqualTo(slider.SpanDuration * 2).Within(1e-6));
        }

        [Test]
        public void TestEndCircle_JudgementAndHitWindows()
        {
            var repeat = createSlider().NestedHitObjects.OfType<SliderRepeat>().Single();

            Assert.That(repeat.CreateJudgement().MaxResult, Is.EqualTo(HitResult.LargeTickHit));
            Assert.That(repeat.HitWindows, Is.SameAs(HitWindows.Empty));
        }

        // ---- SliderTailCircle.CreateJudgement: ClassicSliderBehaviour ----

        [Test]
        public void TestTailCircle_Classic_UsesLegacyTailJudgement()
        {
            var tail = new SliderTailCircle(new Slider()) { ClassicSliderBehaviour = true };

            Assert.That(tail.CreateJudgement(), Is.TypeOf<SliderTailCircle.LegacyTailJudgement>());
            Assert.That(tail.CreateJudgement().MaxResult, Is.EqualTo(HitResult.SmallTickHit));
        }

        [Test]
        public void TestTailCircle_NonClassic_UsesTailJudgement()
        {
            var tail = new SliderTailCircle(new Slider());

            Assert.That(tail.CreateJudgement(), Is.TypeOf<SliderTailCircle.TailJudgement>());
            Assert.That(tail.CreateJudgement().MaxResult, Is.EqualTo(HitResult.SliderTailHit));
        }

        // ---- SliderHeadCircle.CreateJudgement ----

        [Test]
        public void TestHeadCircle_Classic_JudgedLikeTick()
        {
            Assert.That(new SliderHeadCircle { ClassicSliderBehaviour = true }.CreateJudgement(), Is.TypeOf<SliderTickJudgement>());
        }

        [Test]
        public void TestHeadCircle_NonClassic_JudgedLikeCircle()
        {
            Assert.That(new SliderHeadCircle().CreateJudgement(), Is.TypeOf<OsuJudgement>());
        }

        // ---- SliderTick.ApplyDefaultsToSelf: SpanIndex > 0 TRUE / FALSE ----

        [Test]
        public void TestTick_FirstSpan_OffsetFromPreempt()
        {
            // offset = TimePreempt (1200 at AR5) * 0.66; TimePreempt = (StartTime - SpanStartTime) / 2 + offset
            var tick = new SliderTick { SpanIndex = 0, SpanStartTime = 1000, StartTime = 1500 };
            tick.ApplyDefaults(new ControlPointInfo(), difficulty);

            Assert.That(tick.TimePreempt, Is.EqualTo(250 + 1200 * 0.66).Within(0.01));
        }

        [Test]
        public void TestTick_RepeatedSpan_UsesFixedOffset()
        {
            var tick = new SliderTick { SpanIndex = 1, SpanStartTime = 1000, StartTime = 1500 };
            tick.ApplyDefaults(new ControlPointInfo(), difficulty);

            Assert.That(tick.TimePreempt, Is.EqualTo(250 + 200).Within(1e-6));
        }

        [Test]
        public void TestTick_JudgementAndHitWindows()
        {
            var tick = new SliderTick();
            tick.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());

            Assert.That(tick.CreateJudgement(), Is.TypeOf<SliderTickJudgement>());
            Assert.That(tick.HitWindows, Is.SameAs(HitWindows.Empty));
        }
    }
}
