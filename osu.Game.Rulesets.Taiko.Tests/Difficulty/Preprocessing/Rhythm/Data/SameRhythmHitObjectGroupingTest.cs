// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm.Data;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Difficulty.Preprocessing.Rhythm.Data
{
    [TestFixture]
    public class SameRhythmHitObjectGroupingTest
    {
        private static List<TaikoDifficultyHitObject> objects(params double[] deltas)
            => DifficultyObjectFactory.Create(deltas.Select(d => (d, HitType.Centre)).ToArray());

        // CF: SameRhythmHitObjectGrouping.ctor | normalisedHitObjectDeltaTime.Count > 0 | FALSE
        // CF: SameRhythmHitObjectGrouping.ctor | previous?.HitObjectInterval is double && HitObjectInterval is double | FALSE
        // CF: SameRhythmHitObjectGrouping.ctor | previous != null | FALSE
        [Test]
        public void TestSingleObjectWithoutPrevious()
        {
            var group = new SameRhythmHitObjectGrouping(null, objects(100).Take(1).ToList());
            Assert.That(group.HitObjectInterval, Is.Null);
            Assert.That(group.HitObjectIntervalRatio, Is.EqualTo(1.0));
            Assert.That(group.Interval, Is.EqualTo(double.PositiveInfinity));
            Assert.That(group.Duration, Is.EqualTo(0));
        }

        // CF: SameRhythmHitObjectGrouping.ctor | normalisedHitObjectDeltaTime.Count > 0 | TRUE
        // CF: SameRhythmHitObjectGrouping.ctor | previous?.HitObjectInterval is double previousDelta | FALSE
        [Test]
        public void TestIntervalIsModalDelta()
        {
            var group = new SameRhythmHitObjectGrouping(null, objects(100, 100, 100));
            Assert.That(group.HitObjectInterval, Is.EqualTo(100));
            Assert.That(group.Duration, Is.EqualTo(200));
        }

        // CF: SameRhythmHitObjectGrouping.ctor | previous?.HitObjectInterval is double previousDelta | TRUE
        // CF: SameRhythmHitObjectGrouping.ctor | Math.Abs(modalDelta - previousDelta) <= snap_tolerance | TRUE
        // CF: SameRhythmHitObjectGrouping.ctor | previous?.HitObjectInterval is double && HitObjectInterval is double | TRUE
        // CF: SameRhythmHitObjectGrouping.ctor | previous != null | TRUE
        // CF: SameRhythmHitObjectGrouping.ctor | Math.Abs(StartTime - previous.StartTime) <= snap_tolerance | FALSE
        [Test]
        public void TestCloseIntervalSnapsToPrevious()
        {
            var o = objects(100, 100, 100, 103, 103, 103);
            var first = new SameRhythmHitObjectGrouping(null, o.Take(3).ToList());
            var second = new SameRhythmHitObjectGrouping(first, o.Skip(3).ToList());

            Assert.That(second.HitObjectInterval, Is.EqualTo(100));
            Assert.That(second.HitObjectIntervalRatio, Is.EqualTo(1.0));
            Assert.That(second.Interval, Is.EqualTo(o[3].StartTime - o[0].StartTime));
        }

        // CF: SameRhythmHitObjectGrouping.ctor | Math.Abs(modalDelta - previousDelta) <= snap_tolerance | FALSE
        [Test]
        public void TestDistantIntervalKeepsOwnAndGivesRatio()
        {
            var o = objects(100, 100, 100, 200, 200, 200);
            var first = new SameRhythmHitObjectGrouping(null, o.Take(3).ToList());
            var second = new SameRhythmHitObjectGrouping(first, o.Skip(3).ToList());

            Assert.That(second.HitObjectInterval, Is.EqualTo(200));
            Assert.That(second.HitObjectIntervalRatio, Is.EqualTo(2.0));
        }

        // CF: SameRhythmHitObjectGrouping.ctor | previous?.HitObjectInterval is double previousInterval | FALSE
        [Test]
        public void TestPreviousWithoutIntervalGivesRatioOne()
        {
            var o = objects(100, 100, 100);
            var first = new SameRhythmHitObjectGrouping(null, o.Take(1).ToList());
            var second = new SameRhythmHitObjectGrouping(first, o.Skip(1).ToList());

            Assert.That(second.HitObjectInterval, Is.Not.Null);
            Assert.That(second.HitObjectIntervalRatio, Is.EqualTo(1.0));
        }

        // CF: SameRhythmHitObjectGrouping.ctor | Math.Abs(StartTime - previous.StartTime) <= snap_tolerance | TRUE
        [Test]
        public void TestNearlySimultaneousGroupsHaveZeroInterval()
        {
            var o = objects(100, 3);
            var first = new SameRhythmHitObjectGrouping(null, o.Take(1).ToList());
            var second = new SameRhythmHitObjectGrouping(first, o.Skip(1).ToList());

            Assert.That(second.Interval, Is.EqualTo(0));
        }
    }
}
