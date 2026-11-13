// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit.Timing;

namespace osu.Game.Tests.Screens.Edit.Timing
{
    [TestFixture]
    public class TimingSectionAdjustmentsTest
    {
        private static (Beatmap<HitObject> beatmap, TimingControlPoint first, TimingControlPoint second) create(params double[] hitObjectTimes)
        {
            var beatmap = new Beatmap<HitObject>();
            var first = new TimingControlPoint { Time = 0, BeatLength = 500 };
            var second = new TimingControlPoint { Time = 1000, BeatLength = 500 };
            beatmap.ControlPointInfo.Add(0, first);
            beatmap.ControlPointInfo.Add(1000, second);
            beatmap.HitObjects.AddRange(hitObjectTimes.Select(t => (HitObject)new HitCircle { StartTime = t }));
            return (beatmap, first, second);
        }

        // CF: TimingSectionAdjustments.HitObjectsInTimingRange | TimingPoints.Any(x => x.Time < timingControlPoint.Time) | FALSE
        // CF: TimingSectionAdjustments.HitObjectsInTimingRange | FirstOrDefault(x => x.Time > timingControlPoint.Time)?.Time ?? MaxValue | non-null
        [Test]
        public void TestFirstSectionIncludesEverythingBeforeNextPoint()
        {
            var (beatmap, first, _) = create(-500, 0, 999, 1000, 2000);
            Assert.That(TimingSectionAdjustments.HitObjectsInTimingRange(beatmap, first).Select(h => h.StartTime), Is.EqualTo(new[] { -500d, 0, 999 }));
        }

        // CF: TimingSectionAdjustments.HitObjectsInTimingRange | TimingPoints.Any(x => x.Time < timingControlPoint.Time) | TRUE
        // CF: TimingSectionAdjustments.HitObjectsInTimingRange | FirstOrDefault(x => x.Time > timingControlPoint.Time)?.Time ?? MaxValue | null
        [Test]
        public void TestLastSectionIncludesEverythingAfter()
        {
            var (beatmap, _, second) = create(-500, 999, 1000, 2000);
            Assert.That(TimingSectionAdjustments.HitObjectsInTimingRange(beatmap, second).Select(h => h.StartTime), Is.EqualTo(new[] { 1000d, 2000 }));
        }

        // CF: TimingSectionAdjustments.AdjustHitObjectOffset | foreach hitObjects in range | loop n
        [Test]
        public void TestAdjustOffsetOnlyAffectsSection()
        {
            var (beatmap, first, _) = create(100, 200, 1500);
            TimingSectionAdjustments.AdjustHitObjectOffset(beatmap, first, 50);
            Assert.That(beatmap.HitObjects.Select(h => h.StartTime), Is.EqualTo(new[] { 150d, 250, 1500 }));
        }

        // CF: TimingSectionAdjustments.AdjustHitObjectOffset | foreach hitObjects in range | loop 0
        [Test]
        public void TestAdjustOffsetNoObjects()
        {
            var (beatmap, first, _) = create(1500);
            TimingSectionAdjustments.AdjustHitObjectOffset(beatmap, first, 50);
            Assert.That(beatmap.HitObjects[0].StartTime, Is.EqualTo(1500));
        }

        // CF: TimingSectionAdjustments.SetHitObjectBPM | hitObject is not IHasRepeats && hitObject is IHasDuration | FALSE
        // CF: TimingSectionAdjustments.SetHitObjectBPM | foreach hitObjects in range | loop n
        [Test]
        public void TestSetBpmRescalesStartTimes()
        {
            var (beatmap, first, _) = create(250, 500);
            double oldBeatLength = first.BeatLength;
            first.BeatLength = 250;

            TimingSectionAdjustments.SetHitObjectBPM(beatmap, first, oldBeatLength);
            Assert.That(beatmap.HitObjects.Select(h => h.StartTime), Is.EqualTo(new[] { 125d, 250 }));
        }

        // CF: TimingSectionAdjustments.SetHitObjectBPM | hitObject is not IHasRepeats | TRUE
        // CF: TimingSectionAdjustments.SetHitObjectBPM | hitObject is IHasDuration | TRUE
        [Test]
        public void TestSetBpmRescalesSpinnerDuration()
        {
            var (beatmap, first, _) = create();
            var spinner = new Spinner { StartTime = 500, Duration = 400 };
            beatmap.HitObjects.Add(spinner);

            double oldBeatLength = first.BeatLength;
            first.BeatLength = 250;

            TimingSectionAdjustments.SetHitObjectBPM(beatmap, first, oldBeatLength);
            Assert.That(spinner.StartTime, Is.EqualTo(250));
            Assert.That(spinner.Duration, Is.EqualTo(200));
        }

        // CF: TimingSectionAdjustments.SetHitObjectBPM | hitObject is not IHasRepeats | FALSE
        [Test]
        public void TestSetBpmLeavesSliderDurationUntouched()
        {
            var (beatmap, first, _) = create();
            var slider = new Slider { StartTime = 500, Path = new SliderPath(PathType.LINEAR, new[] { osuTK.Vector2.Zero, new osuTK.Vector2(100, 0) }) };
            beatmap.HitObjects.Add(slider);
            double durationBefore = slider.Duration;

            double oldBeatLength = first.BeatLength;
            first.BeatLength = 250;

            TimingSectionAdjustments.SetHitObjectBPM(beatmap, first, oldBeatLength);
            Assert.That(slider.StartTime, Is.EqualTo(250));
            Assert.That(slider.Duration, Is.EqualTo(durationBefore));
        }
    }
}
