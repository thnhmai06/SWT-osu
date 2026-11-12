// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Audio;
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
    public class SliderTest
    {
        // SliderMultiplier 2 with the default 1000ms beat => velocity 0.2 px/ms, tick distance 200 px.
        private static readonly BeatmapDifficulty difficulty = new BeatmapDifficulty { SliderMultiplier = 2, SliderTickRate = 1 };

        private static Slider createSlider(int repeatCount = 1, bool ticks = true, bool classic = false, Vector2? position = null, Action<Slider>? configure = null)
        {
            var slider = new Slider
            {
                StartTime = 0,
                Position = position ?? new Vector2(50, 50),
                RepeatCount = repeatCount,
                GenerateTicks = ticks,
                ClassicSliderBehaviour = classic,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(400, 0) })
            };

            configure?.Invoke(slider);
            slider.ApplyDefaults(new ControlPointInfo(), difficulty);
            return slider;
        }

        // ---- ApplyDefaultsToSelf ----

        [Test]
        public void TestApplyDefaults_VelocityAndTickDistanceDerivedFromDifficulty()
        {
            var slider = createSlider();

            Assert.That(slider.Velocity, Is.EqualTo(0.2).Within(1e-9));
            Assert.That(slider.TickDistance, Is.EqualTo(200).Within(1e-6));
        }

        [Test]
        public void TestApplyDefaults_TickDistanceMultiplierScalesTickDistance()
        {
            var slider = createSlider(configure: s => s.TickDistanceMultiplier = 0.5);

            Assert.That(slider.TickDistance, Is.EqualTo(100).Within(1e-6));
        }

        [Test]
        public void TestApplyDefaults_GenerateTicksDisabled_TickDistanceInfinite()
        {
            // ternary GenerateTicks FALSE
            Assert.That(createSlider(ticks: false).TickDistance, Is.EqualTo(double.PositiveInfinity));
        }

        // ---- timing ----

        [Test]
        public void TestDurationAndEndTime_CoverEverySpan()
        {
            var slider = createSlider(repeatCount: 1);

            Assert.That(slider.Duration, Is.EqualTo(4000).Within(1e-6));
            Assert.That(slider.EndTime, Is.EqualTo(4000).Within(1e-6));
            Assert.That(slider.SpanDuration, Is.EqualTo(2000).Within(1e-6));
        }

        [Test]
        public void TestDurationSetter_Throws()
        {
            Assert.Throws<NotSupportedException>(() => createSlider().Duration = 1);
        }

        // ---- CreateNestedHitObjects: each SliderEventType case ----

        [Test]
        public void TestNested_ContainsHeadTailRepeatAndTicks()
        {
            var slider = createSlider(repeatCount: 1);

            Assert.That(slider.HeadCircle, Is.Not.Null);
            Assert.That(slider.TailCircle, Is.Not.Null);
            Assert.That(slider.LastRepeat, Is.Not.Null);
            Assert.That(slider.NestedHitObjects.OfType<SliderHeadCircle>().Count(), Is.EqualTo(1));
            Assert.That(slider.NestedHitObjects.OfType<SliderTailCircle>().Count(), Is.EqualTo(1));
            Assert.That(slider.NestedHitObjects.OfType<SliderRepeat>().Count(), Is.EqualTo(1));
            Assert.That(slider.NestedHitObjects.OfType<SliderTick>().Count(), Is.GreaterThan(0));
        }

        [Test]
        public void TestNested_NoRepeat_HasNoRepeatObject()
        {
            // Repeat case of the switch is not hit
            var slider = createSlider(repeatCount: 0);

            Assert.That(slider.LastRepeat, Is.Null);
            Assert.That(slider.NestedHitObjects.OfType<SliderRepeat>(), Is.Empty);
        }

        [Test]
        public void TestNested_TicksDisabled_HasNoTicks()
        {
            // Tick case of the switch is not hit
            var slider = createSlider(ticks: false);

            Assert.That(slider.NestedHitObjects.OfType<SliderTick>(), Is.Empty);
        }

        [Test]
        public void TestNested_PositionsFollowPath()
        {
            var slider = createSlider(repeatCount: 1);

            Assert.That(slider.HeadCircle.Position, Is.EqualTo(new Vector2(50, 50)));
            // an even number of spans ends where the slider started
            Assert.That(slider.TailCircle.Position, Is.EqualTo(new Vector2(50, 50)));
            Assert.That(slider.LastRepeat!.Position.X, Is.EqualTo(450).Within(1e-3));
        }

        [Test]
        public void TestNested_ClassicBehaviourPropagatedOnCreation()
        {
            var slider = createSlider(classic: true);

            Assert.That(slider.HeadCircle.ClassicSliderBehaviour, Is.True);
            Assert.That(slider.TailCircle.ClassicSliderBehaviour, Is.True);
        }

        // ---- ClassicSliderBehaviour setter: `?.` on null / non-null nested circles ----

        [Test]
        public void TestClassicBehaviour_BeforeNestedCreated_DoesNotThrow()
        {
            // HeadCircle / TailCircle null path of `?.`
            Assert.DoesNotThrow(() => new Slider { ClassicSliderBehaviour = true });
        }

        [Test]
        public void TestClassicBehaviour_AfterNestedCreated_PropagatesToCircles()
        {
            // HeadCircle / TailCircle non-null path of `?.`
            var slider = createSlider(classic: false);

            slider.ClassicSliderBehaviour = true;

            Assert.That(slider.HeadCircle.ClassicSliderBehaviour, Is.True);
            Assert.That(slider.TailCircle.ClassicSliderBehaviour, Is.True);
        }

        [Test]
        public void TestCreateJudgement_Classic_UsesScoringJudgement()
        {
            Assert.That(createSlider(classic: true).CreateJudgement(), Is.TypeOf<OsuJudgement>());
        }

        [Test]
        public void TestCreateJudgement_NonClassic_UsesIgnoreJudgement()
        {
            Assert.That(createSlider(classic: false).CreateJudgement(), Is.TypeOf<OsuIgnoreJudgement>());
        }

        // ---- updateNestedPositions switch (head / tail / repeat / tick) ----

        [Test]
        public void TestPositionChange_MovesAllNestedObjects()
        {
            var slider = createSlider(repeatCount: 1);
            var tick = slider.NestedHitObjects.OfType<SliderTick>().First();
            var repeat = slider.LastRepeat!;
            var offsetFromSlider = tick.Position - slider.Position;
            float repeatOffsetX = repeat.Position.X - slider.Position.X;

            slider.Position = new Vector2(100, 120);

            Assert.That(slider.HeadCircle.Position, Is.EqualTo(new Vector2(100, 120)));
            Assert.That(slider.TailCircle.Position, Is.EqualTo(slider.EndPosition));
            Assert.That(tick.Position - slider.Position, Is.EqualTo(offsetFromSlider));
            Assert.That(repeat.Position.X - slider.Position.X, Is.EqualTo(repeatOffsetX).Within(1e-3));
        }

        [Test]
        public void TestRepeatCountChange_RelocatesTail()
        {
            var slider = createSlider(repeatCount: 1);
            Assert.That(slider.TailCircle.Position, Is.EqualTo(new Vector2(50, 50)));

            slider.RepeatCount = 0;

            Assert.That(slider.TailCircle.Position.X, Is.EqualTo(450).Within(1e-3));
        }

        [Test]
        public void TestPathChange_RelocatesEndPosition()
        {
            var slider = createSlider(repeatCount: 0);
            Assert.That(slider.EndPosition.X, Is.EqualTo(450).Within(1e-3));

            slider.Path.ControlPoints[1].Position = new Vector2(200, 0);

            Assert.That(slider.EndPosition.X, Is.EqualTo(250).Within(1e-3));
            Assert.That(slider.TailCircle.Position.X, Is.EqualTo(250).Within(1e-3));
        }

        // ---- Path setter ----

        [Test]
        public void TestPathSetter_CopiesControlPointsAndExpectedDistance()
        {
            var source = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }, 80);
            var slider = new Slider { Path = source };

            Assert.That(slider.Path.ControlPoints.Count, Is.EqualTo(2));
            Assert.That(slider.Path.ControlPoints[1].Position, Is.EqualTo(new Vector2(100, 0)));
            Assert.That(slider.Path.ExpectedDistance.Value, Is.EqualTo(80));
            Assert.That(slider.Path, Is.Not.SameAs(source));
            Assert.That(slider.Distance, Is.EqualTo(80).Within(1e-6));
        }

        // ---- UpdateNestedSamples: tickSample selection and null guards ----

        [Test]
        public void TestTickSamples_NoSlidersSamples_TicksHaveNone()
        {
            // tickSample == null -> `if (tickSample != null)` FALSE
            var slider = createSlider();

            Assert.That(slider.NestedHitObjects.OfType<SliderTick>().All(t => t.Samples.Count == 0), Is.True);
        }

        [Test]
        public void TestTickSamples_NormalSamplePreferred()
        {
            // FirstOrDefault(normal) non-null -> `??` right side not used
            var slider = createSlider(configure: s => s.Samples = new[]
            {
                new HitSampleInfo(HitSampleInfo.HIT_WHISTLE, "soft"),
                new HitSampleInfo(HitSampleInfo.HIT_NORMAL, "drum"),
            });

            var tickSample = slider.NestedHitObjects.OfType<SliderTick>().First().Samples.Single();

            Assert.That(tickSample.Name, Is.EqualTo("slidertick"));
            Assert.That(tickSample.Bank, Is.EqualTo("drum"));
        }

        [Test]
        public void TestTickSamples_FallsBackToFirstSampleWithoutNormal()
        {
            // FirstOrDefault(normal) null -> `??` falls back to first sample
            var slider = createSlider(configure: s => s.Samples = new[] { new HitSampleInfo(HitSampleInfo.HIT_WHISTLE, "soft") });

            var tickSample = slider.NestedHitObjects.OfType<SliderTick>().First().Samples.Single();

            Assert.That(tickSample.Name, Is.EqualTo("slidertick"));
            Assert.That(tickSample.Bank, Is.EqualTo("soft"));
        }

        [Test]
        public void TestSamplesChange_RefreshesTickSamples()
        {
            var slider = createSlider();
            var tick = slider.NestedHitObjects.OfType<SliderTick>().First();
            Assert.That(tick.Samples, Is.Empty);

            slider.Samples = new[] { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, "drum") };

            Assert.That(tick.Samples.Single().Bank, Is.EqualTo("drum"));
        }

        [Test]
        public void TestHeadSamples_UpdatedWhenHeadExists_AndNoThrowWhenAbsent()
        {
            // `HeadCircle?.Samples =`: null path (no nested yet) must not throw
            Assert.DoesNotThrow(() => new Slider { Samples = new[] { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) } });

            // non-null path: the head receives node-0 samples (slider samples when no node samples are defined)
            var slider = createSlider(configure: s => s.Samples = new[] { new HitSampleInfo(HitSampleInfo.HIT_CLAP, "soft") });
            Assert.That(slider.HeadCircle.Samples.Any(s => s.Name == HitSampleInfo.HIT_CLAP), Is.True);
        }

        [Test]
        public void TestAuxiliarySamples_IncludeTailSamples()
        {
            var slider = createSlider(configure: s => s.Samples = new[] { new HitSampleInfo(HitSampleInfo.HIT_CLAP, "soft") });

            Assert.That(slider.TailSamples, Is.Not.Null);
            Assert.That(slider.AuxiliarySamples.Any(s => s.Name == HitSampleInfo.HIT_CLAP), Is.True);
        }

        // ---- misc ----

        [Test]
        public void TestHitWindows_AreEmpty()
        {
            Assert.That(createSlider().HitWindows, Is.SameAs(HitWindows.Empty));
        }

        [Test]
        public void TestSliderVelocityMultiplier_RoundTripsThroughBindable()
        {
            var slider = new Slider { SliderVelocityMultiplier = 2 };

            Assert.That(slider.SliderVelocityMultiplierBindable.Value, Is.EqualTo(2));
        }
    }
}
