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
        // default timing: 1000ms beat, slider multiplier 1.4 -> 0.14 px/ms, tick distance 140 px
        private static Slider create(int repeats = 0, bool applyDefaults = true, bool generateTicks = true, bool classic = false)
        {
            var slider = new Slider
            {
                StartTime = 0,
                Position = new Vector2(100, 100),
                RepeatCount = repeats,
                GenerateTicks = generateTicks,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(300, 0) }),
            };

            slider.ClassicSliderBehaviour = classic;

            if (applyDefaults)
                slider.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());

            return slider;
        }

        [Test]
        public void TestTimingDerivedFromVelocity()
        {
            var slider = create(repeats: 1);

            Assert.That(slider.Velocity, Is.EqualTo(0.14).Within(1e-9));
            Assert.That(slider.SpanDuration, Is.EqualTo(300 / 0.14).Within(1e-6));
            Assert.That(slider.EndTime, Is.EqualTo(2 * 300 / 0.14).Within(1e-6));
            Assert.That(slider.Duration, Is.EqualTo(slider.EndTime - slider.StartTime));
            Assert.That(slider.Distance, Is.EqualTo(300).Within(1e-6));
        }

        [Test]
        public void TestDurationCannotBeSet()
        {
            Assert.Throws<NotSupportedException>(() => create().Duration = 1000);
        }

        [Test]
        public void TestPathIsCopiedNotShared()
        {
            var source = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(50, 0) });
            var slider = new Slider { Path = source };

            source.ControlPoints[1].Position = new Vector2(500, 0);

            Assert.That(slider.Path.ControlPoints[1].Position, Is.EqualTo(new Vector2(50, 0)));
        }

        [Test]
        public void TestEndPositionAndRepeatChange()
        {
            var slider = create(repeats: 0);
            Assert.That(slider.EndPosition.X, Is.EqualTo(400).Within(1e-3));
            Assert.That(slider.StackedPositionAt(0.5).X, Is.EqualTo(250).Within(1e-3));

            // after one repeat the slider ends where it started
            slider.RepeatCount = 1;
            slider.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            Assert.That(slider.EndPosition.X, Is.EqualTo(100).Within(1e-3));
        }

        // CF: Slider.ApplyDefaultsToSelf | GenerateTicks | TRUE
        // CF: Slider.CreateNestedHitObjects | foreach sliderEvents | loop n
        // CF: Slider.CreateNestedHitObjects | switch (e.Type) | case Tick
        // CF: Slider.CreateNestedHitObjects | switch (e.Type) | case Head
        // CF: Slider.CreateNestedHitObjects | switch (e.Type) | case Tail
        // CF: Slider.CreateNestedHitObjects | switch (e.Type) | case Repeat
        [Test]
        public void TestNestedObjectsWithTicks()
        {
            var slider = create(repeats: 1);

            Assert.That(slider.TickDistance, Is.EqualTo(140).Within(1e-6));
            Assert.That(slider.NestedHitObjects.OfType<SliderTick>().Count(), Is.EqualTo(4));
            Assert.That(slider.NestedHitObjects.OfType<SliderRepeat>().Count(), Is.EqualTo(1));
            Assert.That(slider.HeadCircle, Is.Not.Null);
            Assert.That(slider.TailCircle, Is.Not.Null);
            Assert.That(slider.LastRepeat, Is.Not.Null);
            Assert.That(slider.HeadCircle.Position, Is.EqualTo(slider.Position));
        }

        // CF: Slider.ApplyDefaultsToSelf | GenerateTicks | FALSE
        [Test]
        public void TestNoTicksWhenDisabled()
        {
            var slider = create(generateTicks: false);

            Assert.That(slider.TickDistance, Is.EqualTo(double.PositiveInfinity));
            Assert.That(slider.NestedHitObjects.OfType<SliderTick>(), Is.Empty);
        }

        [Test]
        public void TestStackHeightReachesNestedObjectsAndPositionFollows()
        {
            var slider = create();

            slider.Position = new Vector2(200, 50);
            slider.StackHeight = 2;

            foreach (var nested in slider.NestedHitObjects.Cast<OsuHitObject>())
                Assert.That(nested.StackHeight, Is.EqualTo(2));

            Assert.That(slider.HeadCircle.Position, Is.EqualTo(new Vector2(200, 50)));
            Assert.That(slider.TailCircle.Position.X, Is.EqualTo(500).Within(1e-3));
            Assert.That(slider.NestedHitObjects.OfType<SliderTick>().First().Position.X, Is.EqualTo(340).Within(1e-3));
        }

        [Test]
        public void TestRepeatPositionUpdatedOnMove()
        {
            var slider = create(repeats: 1);
            slider.Position = new Vector2(0, 0);

            Assert.That(slider.NestedHitObjects.OfType<SliderRepeat>().Single().Position.X, Is.EqualTo(300).Within(1e-3));
        }

        // CF: Slider.ClassicSliderBehaviour | HeadCircle != null | FALSE
        [Test]
        public void TestClassicBehaviourBeforeNestedObjectsExist()
        {
            Assert.DoesNotThrow(() => create(applyDefaults: false, classic: true));
        }

        // CF: Slider.ClassicSliderBehaviour | HeadCircle != null | TRUE
        [Test]
        public void TestClassicBehaviourPropagatesToHeadAndTail()
        {
            var slider = create();

            slider.ClassicSliderBehaviour = true;

            Assert.That(slider.HeadCircle.ClassicSliderBehaviour, Is.True);
            Assert.That(slider.TailCircle.ClassicSliderBehaviour, Is.True);
        }

        // CF: Slider.CreateJudgement | ClassicSliderBehaviour | TRUE
        [Test]
        public void TestClassicJudgement()
        {
            Assert.That(create(classic: true).CreateJudgement(), Is.TypeOf<OsuJudgement>());
        }

        // CF: Slider.CreateJudgement | ClassicSliderBehaviour | FALSE
        [Test]
        public void TestLazerJudgement()
        {
            Assert.That(create(classic: false).CreateJudgement(), Is.TypeOf<OsuIgnoreJudgement>());
        }

        // CF: Slider.UpdateNestedSamples | tickSample != null | FALSE
        [Test]
        public void TestTicksHaveNoSamplesWhenSliderHasNone()
        {
            var slider = create();
            Assert.That(slider.NestedHitObjects.OfType<SliderTick>().All(t => t.Samples.Count == 0), Is.True);
        }

        // CF: Slider.UpdateNestedSamples | tickSample != null | TRUE
        [Test]
        public void TestTicksUseSliderTickSample()
        {
            var slider = create();
            slider.Samples.Add(new HitSampleInfo(HitSampleInfo.HIT_NORMAL));

            var tick = slider.NestedHitObjects.OfType<SliderTick>().First();
            Assert.That(tick.Samples.Select(s => s.Name), Is.EqualTo(new[] { "slidertick" }));
        }

        [Test]
        public void TestNodeSamplesDistributed()
        {
            var slider = create(repeats: 1, applyDefaults: false);
            slider.Samples.Add(new HitSampleInfo(HitSampleInfo.HIT_CLAP));
            slider.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());

            Assert.That(slider.HeadCircle.Samples.Select(s => s.Name), Is.EqualTo(new[] { HitSampleInfo.HIT_CLAP }));
            Assert.That(slider.TailSamples.Select(s => s.Name), Is.EqualTo(new[] { HitSampleInfo.HIT_CLAP }));
            Assert.That(slider.NestedHitObjects.OfType<SliderRepeat>().Single().Samples, Is.Not.Empty);
            Assert.That(slider.AuxiliarySamples.Select(s => s.Name), Does.Contain(HitSampleInfo.HIT_CLAP));
        }

        [Test]
        public void TestSliderHasNoHitWindows()
        {
            Assert.That(create().HitWindows, Is.SameAs(HitWindows.Empty));
        }

        [Test]
        public void TestSliderVelocityMultiplierIsClamped()
        {
            var slider = new Slider { SliderVelocityMultiplier = 100 };
            Assert.That(slider.SliderVelocityMultiplier, Is.EqualTo(10));
        }
    }
}
