// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Mods
{
    [TestFixture]
    public class OsuModVisibilityTest
    {
        private static HitCircle circle(double time, bool newCombo = false)
        {
            var c = new HitCircle { StartTime = time, NewCombo = newCombo, Position = new Vector2(100, 100) };
            c.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            return c;
        }

        private static Slider slider(double time, int repeats)
        {
            var s = new Slider
            {
                StartTime = time,
                RepeatCount = repeats,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(300, 0) }),
            };
            s.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            return s;
        }

        private static OsuBeatmap beatmapOf(params OsuHitObject[] objects)
        {
            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.AddRange(objects);
            return beatmap;
        }

        // CF: OsuModFreezeFrame.ApplyToBeatmap | firstHitObject == null | TRUE
        [Test]
        public void TestFreezeFrameEmptyBeatmap()
        {
            Assert.DoesNotThrow(() => new OsuModFreezeFrame().ApplyToBeatmap(beatmapOf()));
        }

        // CF: OsuModFreezeFrame.ApplyToBeatmap | firstHitObject == null | FALSE
        // CF: OsuModFreezeFrame.ApplyToBeatmap | obj.NewCombo | TRUE
        // CF: OsuModFreezeFrame.ApplyToBeatmap | obj.NewCombo | FALSE
        // CF: OsuModFreezeFrame.ApplyToBeatmap | osuObject is not Spinner | TRUE
        [Test]
        public void TestFreezeFramePreemptGrowsWithTimeSinceLastNewCombo()
        {
            var a = circle(1000, newCombo: true);
            var b = circle(1500);
            var c = circle(3000, newCombo: true);
            double preempt = a.TimePreempt;

            new OsuModFreezeFrame().ApplyToBeatmap(beatmapOf(a, b, c));

            Assert.That(a.TimePreempt, Is.EqualTo(preempt));
            Assert.That(b.TimePreempt, Is.EqualTo(preempt + 500));
            Assert.That(c.TimePreempt, Is.EqualTo(preempt));
        }

        [Test]
        public void TestFreezeFrameFirstObjectWithoutNewComboUsesTimeZero()
        {
            var a = circle(1000);
            double preempt = a.TimePreempt;

            new OsuModFreezeFrame().ApplyToBeatmap(beatmapOf(a));

            Assert.That(a.TimePreempt, Is.EqualTo(preempt + 1000));
        }

        // CF: OsuModFreezeFrame.ApplyToBeatmap | osuObject is not Spinner | FALSE
        [Test]
        public void TestFreezeFrameSpinnerIsUnchanged()
        {
            var spinner = new Spinner { StartTime = 5000, Duration = 1000 };
            spinner.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            double preempt = spinner.TimePreempt;

            new OsuModFreezeFrame().ApplyToBeatmap(beatmapOf(circle(1000, true), spinner));

            Assert.That(spinner.TimePreempt, Is.EqualTo(preempt));
        }

        // CF: OsuModFreezeFrame.ApplyToBeatmap | switch (nested) | case SliderTick
        // CF: OsuModFreezeFrame.ApplyToBeatmap | switch (nested) | case SliderRepeat
        // CF: OsuModFreezeFrame.ApplyToBeatmap | repeatCount > 2 | FALSE
        // CF: OsuModFreezeFrame.ApplyToBeatmap | repeatCount > 2 | TRUE
        // CF: OsuModFreezeFrame.ApplyToBeatmap | switch (nested) | default
        [Test]
        public void TestFreezeFrameSliderNestedObjects()
        {
            var s = slider(1000, repeats: 4);
            var defaults = s.NestedHitObjects.Cast<OsuHitObject>().Select(n => n.TimePreempt).ToArray();

            new OsuModFreezeFrame().ApplyToBeatmap(beatmapOf(s));

            var nested = s.NestedHitObjects.Cast<OsuHitObject>().ToList();

            // ticks keep their preempt
            foreach (var tick in nested.OfType<SliderTick>())
                Assert.That(tick.TimePreempt, Is.EqualTo(defaults[nested.IndexOf(tick)]));

            // head and tail are adjusted
            Assert.That(s.HeadCircle.TimePreempt, Is.GreaterThan(defaults[nested.IndexOf(s.HeadCircle)]));
            Assert.That(s.TailCircle.TimePreempt, Is.GreaterThan(defaults[nested.IndexOf(s.TailCircle)]));

            // only the first three repeats are adjusted
            var repeats = nested.OfType<SliderRepeat>().ToList();
            Assert.That(repeats, Has.Count.EqualTo(4));
            for (int i = 0; i < 3; i++)
                Assert.That(repeats[i].TimePreempt, Is.GreaterThan(defaults[nested.IndexOf(repeats[i])]));
            Assert.That(repeats[3].TimePreempt, Is.EqualTo(defaults[nested.IndexOf(repeats[3])]));
        }

        // CF: OsuModHidden.ApplyToBeatmap | osuObject is not Slider | TRUE
        [Test]
        public void TestHiddenShortensCircleFadeIn()
        {
            var c = circle(1000);
            new OsuModHidden().ApplyToBeatmap(beatmapOf(c));

            Assert.That(c.TimeFadeIn, Is.EqualTo(c.TimePreempt * OsuModHidden.FADE_IN_DURATION_MULTIPLIER));
        }

        // CF: OsuModHidden.ApplyToBeatmap | osuObject is not Slider | FALSE
        // CF: OsuModHidden.ApplyToBeatmap | foreach nested in osuObject.NestedHitObjects | loop n
        [Test]
        public void TestHiddenKeepsSliderFadeInButAdjustsNested()
        {
            var s = slider(1000, repeats: 1);
            double sliderFadeIn = s.TimeFadeIn;

            new OsuModHidden().ApplyToBeatmap(beatmapOf(s));

            Assert.That(s.TimeFadeIn, Is.EqualTo(sliderFadeIn));
            Assert.That(s.HeadCircle.TimeFadeIn, Is.EqualTo(s.HeadCircle.TimePreempt * OsuModHidden.FADE_IN_DURATION_MULTIPLIER));
        }

        // CF: OsuModHidden.ApplyToBeatmap | foreach obj | loop 0
        [Test]
        public void TestHiddenEmptyBeatmap()
        {
            Assert.DoesNotThrow(() => new OsuModHidden().ApplyToBeatmap(beatmapOf()));
        }

        // CF: OsuModHidden.IsFirstAdjustableObject | hitObject is Spinner || hitObject is SpinnerTick | TRUE
        // CF: OsuModHidden.IsFirstAdjustableObject | hitObject is Spinner || hitObject is SpinnerTick | FALSE
        [Test]
        public void TestFirstAdjustableObject()
        {
            var hidden = new HiddenProbe();
            var traceable = new TraceableProbe();

            foreach (bool isHidden in new[] { true, false })
            {
                Assert.That(isHidden ? hidden.Check(new HitCircle()) : traceable.Check(new HitCircle()), Is.True);
                Assert.That(isHidden ? hidden.Check(new Spinner()) : traceable.Check(new Spinner()), Is.False);
                Assert.That(isHidden ? hidden.Check(new SpinnerTick()) : traceable.Check(new SpinnerTick()), Is.False);
            }
        }

        [Test]
        public void TestFailConditionsOfSuddenDeathLikeMod()
        {
            Assert.That(new OsuModHidden().IncompatibleMods, Does.Contain(typeof(OsuModSpinIn)));
            Assert.That(new OsuModTraceable().IncompatibleMods, Does.Contain(typeof(OsuModDepth)));
            Assert.That(new OsuModFreezeFrame().IncompatibleMods, Does.Contain(typeof(OsuModHidden)));
        }

        private class HiddenProbe : OsuModHidden
        {
            public bool Check(HitObject o) => IsFirstAdjustableObject(o);
        }

        private class TraceableProbe : OsuModTraceable
        {
            public bool Check(HitObject o) => IsFirstAdjustableObject(o);
        }
    }
}
