// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Tests.Rulesets.Objects
{
    [TestFixture]
    public class BarLineGeneratorTest
    {
        private class TestBarLine : IBarLine
        {
            public double StartTime { get; set; }
            public bool Major { get; set; }
        }

        private static Beatmap createBeatmap(double lastObjectTime, params TimingControlPoint[] timingPoints)
        {
            var beatmap = new Beatmap { HitObjects = { new HitObject { StartTime = 1000 }, new HitObject { StartTime = lastObjectTime } } };

            foreach (var t in timingPoints)
                beatmap.ControlPointInfo.Add(t.Time, t);

            return beatmap;
        }

        private static TimingControlPoint timing(double time, double beatLength = 500, bool omitFirst = false) => new TimingControlPoint { Time = time, BeatLength = beatLength, OmitFirstBarLine = omitFirst };

        // CF: BarLineGenerator.BarLineGenerator | beatmap.HitObjects.Count == 0 | TRUE
        // CF: BarLineGenerator.BarLineGenerator | timingPoints.Count == 0 | TRUE
        // CF: BarLineGenerator.BarLineGenerator | timingPoints for loop | loop 0
        [Test]
        public void TestNoBarLinesWithoutObjectsOrTimingPoints()
        {
            Assert.That(new BarLineGenerator<TestBarLine>(new Beatmap()).BarLines, Is.Empty);
            Assert.That(new BarLineGenerator<TestBarLine>(createBeatmap(3000)).BarLines, Is.Empty);
        }

        // CF: BarLineGenerator.BarLineGenerator | beatmap.HitObjects.Count == 0 | FALSE
        // CF: BarLineGenerator.BarLineGenerator | timingPoints.Count == 0 | FALSE
        // CF: BarLineGenerator.BarLineGenerator | timingPoints for loop | loop 1
        // CF: BarLineGenerator.BarLineGenerator | i < timingPoints.Count - 1 | FALSE
        // CF: BarLineGenerator.BarLineGenerator | currentTimingPoint.Time > generationStartTime | TRUE
        // CF: BarLineGenerator.BarLineGenerator | currentTimingPoint.OmitFirstBarLine | FALSE
        // CF: BarLineGenerator.BarLineGenerator | for t | loop n
        [Test]
        public void TestBarLinesEveryMeasureWithMajorOnFirstBeat()
        {
            // 4/4 at beat length 500 => bar length 2000. Last object at 4500, bar lines continue until slightly past it (+ one bar).
            var bars = new BarLineGenerator<TestBarLine>(createBeatmap(4500, timing(1))).BarLines;

            Assert.That(bars.Select(b => b.StartTime), Is.EqualTo(new[] { 1.0, 2001, 4001, 6001 }));
            Assert.That(bars.Select(b => b.Major), Is.EqualTo(new[] { true, false, false, false }), "bars are counted in beats of the signature numerator");
        }

        // CF: BarLineGenerator.BarLineGenerator | i < timingPoints.Count - 1 | TRUE
        // CF: BarLineGenerator.BarLineGenerator | timingPoints for loop | loop n
        // CF: BarLineGenerator.BarLineGenerator | currentBeat % Numerator == 0 | TRUE
        [Test]
        public void TestGenerationStopsAtNextTimingPoint()
        {
            var bars = new BarLineGenerator<TestBarLine>(createBeatmap(9000, timing(1, 500), timing(5000, 1000))).BarLines;

            Assert.That(bars.Select(b => b.StartTime).Where(t => t < 5000), Is.EqualTo(new[] { 1.0, 2001, 4001 }));
            Assert.That(bars.First(b => b.StartTime >= 5000).StartTime, Is.EqualTo(5000));
            Assert.That(bars.First().Major, Is.True);
            Assert.That(bars[1].Major, Is.False);
        }

        // CF: BarLineGenerator.BarLineGenerator | currentTimingPoint.Time > generationStartTime | FALSE
        [Test]
        public void TestTimingPointBeforeGenerationStartIsAligned()
        {
            // timing point at -3000 with a 2000ms bar => bar lines at -3000, -1000, 1000 ...; generation starts at min(0, firstHitTime) = 0 => first bar at 1000.
            var bars = new BarLineGenerator<TestBarLine>(createBeatmap(2500, timing(-3000))).BarLines;

            Assert.That(bars.First().StartTime, Is.EqualTo(1000));
        }

        // CF: BarLineGenerator.BarLineGenerator | currentTimingPoint.OmitFirstBarLine | TRUE
        [Test]
        public void TestOmitFirstBarLine()
        {
            var bars = new BarLineGenerator<TestBarLine>(createBeatmap(2500, timing(0, 500, omitFirst: true))).BarLines;

            Assert.That(bars.First().StartTime, Is.EqualTo(2000));
        }

        // CF: BarLineGenerator.BarLineGenerator | currentBeat % Numerator == 0 | FALSE
        // CF: BarLineGenerator.BarLineGenerator | Precision.AlmostEquals(t, roundedTime) | FALSE
        [Test]
        public void TestNonIntegralBarTimesAreKept()
        {
            var bars = new BarLineGenerator<TestBarLine>(createBeatmap(3000, timing(0, 1000.0 / 3))).BarLines.Take(3).ToArray();

            Assert.That(bars[1].StartTime, Is.EqualTo(4000.0 / 3).Within(1e-6));
            Assert.That(bars[1].Major, Is.False);
        }
    }
}
