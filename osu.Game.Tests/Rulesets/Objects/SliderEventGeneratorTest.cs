// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Tests.Rulesets.Objects
{
    [TestFixture]
    public class SliderEventGeneratorTest
    {
        private static SliderEventDescriptor[] generate(double start = 1000, double spanDuration = 1000, double velocity = 1, double tickDistance = 100, double distance = 350, int spans = 1)
            => SliderEventGenerator.Generate(start, spanDuration, velocity, tickDistance, distance, spans).ToArray();

        // CF: SliderEventGenerator.Generate | for span < spanCount | loop 1
        // CF: SliderEventGenerator.Generate | tickDistance != 0 | TRUE
        // CF: SliderEventGenerator.Generate | reversed | FALSE
        // CF: SliderEventGenerator.Generate | span < spanCount - 1 | FALSE
        // CF: SliderEventGenerator.Generate | spanCount % 2 == 0 | FALSE
        // CF: SliderEventGenerator.generateTicks | for d <= length | loop n
        // CF: SliderEventGenerator.generateTicks | d >= length - minDistanceFromEnd | TRUE
        [Test]
        public void TestSingleSpan()
        {
            var events = generate();

            Assert.That(events.Select(e => e.Type), Is.EqualTo(new[]
            {
                SliderEventType.Head, SliderEventType.Tick, SliderEventType.Tick, SliderEventType.Tick, SliderEventType.LegacyLastTick, SliderEventType.Tail
            }));
            Assert.That(events[0].Time, Is.EqualTo(1000));
            Assert.That(events[1].PathProgress, Is.EqualTo(100 / 350.0).Within(1e-9));
            Assert.That(events[1].Time, Is.EqualTo(1000 + 1000 * 100 / 350.0).Within(1e-9));
            Assert.That(events[4].Time, Is.EqualTo(1964), "tail leniency of 36ms before the end");
            Assert.That(events[4].PathProgress, Is.EqualTo(0.964).Within(1e-9));
            Assert.That(events[5].Time, Is.EqualTo(2000));
            Assert.That(events[5].PathProgress, Is.EqualTo(1));
        }

        // CF: SliderEventGenerator.Generate | for span < spanCount | loop n
        // CF: SliderEventGenerator.Generate | reversed | TRUE
        // CF: SliderEventGenerator.Generate | span < spanCount - 1 | TRUE
        // CF: SliderEventGenerator.Generate | spanCount % 2 == 0 | TRUE
        [Test]
        public void TestRepeatSpanTicksAreMirroredAndTimeOrdered()
        {
            var events = generate(spans: 2);

            var repeat = events.Single(e => e.Type == SliderEventType.Repeat);
            Assert.That(repeat.Time, Is.EqualTo(2000));
            Assert.That(repeat.PathProgress, Is.EqualTo(1));
            Assert.That(repeat.SpanIndex, Is.Zero);

            var reverseTicks = events.Where(e => e.Type == SliderEventType.Tick && e.SpanIndex == 1).ToArray();
            Assert.That(reverseTicks.Select(t => t.Time), Is.Ordered.Ascending);
            Assert.That(reverseTicks.First().PathProgress, Is.EqualTo(300 / 350.0).Within(1e-9), "first tick of the reversed span is the one nearest the far end");
            Assert.That(reverseTicks.First().Time, Is.EqualTo(2000 + (1 - 300 / 350.0) * 1000).Within(1e-9));

            var legacy = events.Single(e => e.Type == SliderEventType.LegacyLastTick);
            Assert.That(legacy.PathProgress, Is.EqualTo(1 - 0.964).Within(1e-9), "progress is mirrored for an even span count");
            Assert.That(events.Last().PathProgress, Is.Zero);
            Assert.That(events.Last().Time, Is.EqualTo(3000));
        }

        // CF: SliderEventGenerator.Generate | tickDistance != 0 | FALSE
        // CF: SliderEventGenerator.generateTicks | for d <= length | loop 0
        [Test]
        public void TestNoTicksWhenTickDistanceZeroOrLargerThanSlider()
        {
            Assert.That(generate(tickDistance: 0).Any(e => e.Type == SliderEventType.Tick), Is.False);
            Assert.That(generate(tickDistance: 1000).Any(e => e.Type == SliderEventType.Tick), Is.False, "tick distance is clamped to the length, which is within the end margin");
        }

        // CF: SliderEventGenerator.Generate | Math.Min(max_length, totalDistance) | no throw
        [Test]
        public void TestVeryLongSlidersAreCapped()
        {
            var ticks = generate(tickDistance: 50000, distance: 1_000_000).Where(e => e.Type == SliderEventType.Tick).ToArray();

            Assert.That(ticks.Length, Is.EqualTo(1));
            Assert.That(ticks[0].PathProgress, Is.EqualTo(0.5));
        }

        [Test]
        public void TestLegacyLastTickOfShortSliderIsAtHalfDuration()
        {
            var legacy = generate(spanDuration: 50, tickDistance: 0).Single(e => e.Type == SliderEventType.LegacyLastTick);

            Assert.That(legacy.Time, Is.EqualTo(1025));
            Assert.That(legacy.PathProgress, Is.EqualTo(0.5));
        }

        // CF: SliderEventGenerator.Generate | cancellationToken.ThrowIfCancellationRequested | throws OperationCanceledException
        [Test]
        public void TestCancellation()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() => SliderEventGenerator.Generate(0, 1000, 1, 100, 350, 1, cts.Token).ToArray());
        }
    }
}
