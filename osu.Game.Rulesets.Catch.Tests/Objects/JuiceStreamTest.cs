// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Rulesets.Catch.Tests.Objects
{
    [TestFixture]
    public class JuiceStreamTest
    {
        private static JuiceStream create(float length, int repeats = 0, float tickRate = 2, double tickDistanceMultiplier = 1)
        {
            var stream = new JuiceStream
            {
                StartTime = 0,
                X = 100,
                RepeatCount = repeats,
                TickDistanceMultiplier = tickDistanceMultiplier,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(length, 0) }),
            };

            // default beat length is 1000ms and slider multiplier 1, so velocity is 0.1 px/ms.
            stream.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty { SliderMultiplier = 1, SliderTickRate = tickRate });
            return stream;
        }

        [Test]
        public void TestVelocityAndDuration()
        {
            var stream = create(100);

            Assert.That(stream.Velocity, Is.EqualTo(0.1).Within(1e-9));
            Assert.That(stream.Duration, Is.EqualTo(1000).Within(1e-6));
            Assert.That(stream.EndTime, Is.EqualTo(1000).Within(1e-6));
            Assert.That(stream.SpanDuration, Is.EqualTo(1000).Within(1e-6));
        }

        [Test]
        public void TestSpanDurationWithRepeats()
        {
            var stream = create(100, repeats: 1);
            Assert.That(stream.Duration, Is.EqualTo(2000).Within(1e-6));
            Assert.That(stream.SpanDuration, Is.EqualTo(1000).Within(1e-6));
        }

        [Test]
        public void TestTickDistanceUsesMultiplier()
        {
            Assert.That(create(100, tickRate: 2).TickDistance, Is.EqualTo(50).Within(1e-6));
            Assert.That(create(100, tickRate: 2, tickDistanceMultiplier: 2).TickDistance, Is.EqualTo(100).Within(1e-6));
        }

        [Test]
        public void TestDurationSetterIsNotSupported()
        {
            Assert.Throws<NotSupportedException>(() => create(100).Duration = 5);
        }

        [Test]
        public void TestPathSetterCopiesControlPoints()
        {
            var stream = new JuiceStream();
            var source = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(10, 0) }, 10);

            stream.Path = source;

            Assert.That(stream.Path, Is.Not.SameAs(source));
            Assert.That(stream.Path.ControlPoints, Has.Count.EqualTo(2));
            Assert.That(stream.Path.ControlPoints[1], Is.Not.SameAs(source.ControlPoints[1]));
            Assert.That(stream.Path.ExpectedDistance.Value, Is.EqualTo(10));
        }

        [Test]
        public void TestEndX()
        {
            Assert.That(create(100).EndX, Is.EqualTo(200).Within(1e-3));
        }

        // CF: JuiceStream.CreateNestedHitObjects | lastEvent != null | FALSE (first event)
        // CF: JuiceStream.CreateNestedHitObjects | lastEvent != null | TRUE
        // CF: JuiceStream.CreateNestedHitObjects | sinceLastTick > 80 | TRUE
        // CF: JuiceStream.CreateNestedHitObjects | timeBetweenTiny > 100 | loop 1
        // CF: JuiceStream.CreateNestedHitObjects | timeBetweenTiny > 100 | loop n
        // CF: JuiceStream.CreateNestedHitObjects | t < sinceLastTick | loop n
        // CF: JuiceStream.CreateNestedHitObjects | switch e.Type | case Tick
        // CF: JuiceStream.CreateNestedHitObjects | switch e.Type | case Head
        // CF: JuiceStream.CreateNestedHitObjects | switch e.Type | case Tail
        [Test]
        public void TestNestedObjectsOfStreamWithTick()
        {
            var stream = create(100);
            var nested = stream.NestedHitObjects;

            Assert.That(nested.OfType<Fruit>().Select(f => f.StartTime), Is.EqualTo(new[] { 0.0, 1000.0 }));
            Assert.That(nested.OfType<Droplet>().Where(d => d is not TinyDroplet).Select(d => d.StartTime), Is.EqualTo(new[] { 500.0 }));

            // each 500ms interval is halved until at most 100ms: 500 -> 250 -> 125 -> 62.5, giving 7 tiny droplets per interval.
            Assert.That(nested.OfType<TinyDroplet>(), Has.Exactly(14).Items);
            Assert.That(nested.OfType<Droplet>().First(d => d is not TinyDroplet).EffectiveX, Is.EqualTo(150).Within(1e-3));
        }

        // CF: JuiceStream.CreateNestedHitObjects | switch e.Type | case Repeat
        [Test]
        public void TestRepeatCreatesFruitAtTurningPoint()
        {
            var stream = create(100, repeats: 1, tickRate: 0.5f);

            Assert.That(stream.NestedHitObjects.OfType<Fruit>().Select(f => f.StartTime), Is.EqualTo(new[] { 0.0, 1000.0, 2000.0 }));
        }

        // CF: JuiceStream.CreateNestedHitObjects | sinceLastTick > 80 | FALSE
        // CF: JuiceStream.CreateNestedHitObjects | timeBetweenTiny > 100 | loop 0
        [Test]
        public void TestShortStreamHasNoTinyDroplets()
        {
            var stream = create(5, tickRate: 1);

            Assert.That(stream.NestedHitObjects.OfType<TinyDroplet>(), Is.Empty);
            Assert.That(stream.NestedHitObjects.OfType<Fruit>(), Has.Exactly(2).Items);
        }

        // CF: JuiceStream.CreateNestedHitObjects | timeBetweenTiny > 100 | FALSE
        // CF: JuiceStream.CreateNestedHitObjects | t < sinceLastTick | loop 0
        [Test]
        public void TestIntervalJustAboveEightyCreatesNoTinyDroplets()
        {
            // 90ms interval: spacing stays 90, so the first candidate (t = 90) is not strictly inside the interval.
            var stream = create(9, tickRate: 1);

            Assert.That(stream.NestedHitObjects.OfType<TinyDroplet>(), Is.Empty);
        }
    }
}
