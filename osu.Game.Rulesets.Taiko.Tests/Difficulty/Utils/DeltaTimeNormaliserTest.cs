// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Utils;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Difficulty.Utils
{
    [TestFixture]
    public class DeltaTimeNormaliserTest
    {
        private static Hit hit(double time)
        {
            var hit = new Hit { StartTime = time };
            hit.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty()); // creates the hit windows the difficulty object reads.
            return hit;
        }

        private static List<TaikoDifficultyHitObject> create(params double[] deltaTimes)
        {
            var all = new List<DifficultyHitObject>();
            var centre = new List<TaikoDifficultyHitObject>();
            var rim = new List<TaikoDifficultyHitObject>();
            var notes = new List<TaikoDifficultyHitObject>();
            var result = new List<TaikoDifficultyHitObject>();

            double time = 1000;
            var last = hit(time);

            for (int i = 0; i < deltaTimes.Length; i++)
            {
                time += deltaTimes[i];
                var current = hit(time);
                var obj = new TaikoDifficultyHitObject(current, last, 1, all, centre, rim, notes, i, new ControlPointInfo(), 1);
                all.Add(obj);
                result.Add(obj);
                last = current;
            }

            return result;
        }

        private static double[] normalised(double margin, params double[] deltaTimes)
        {
            var objects = create(deltaTimes);
            var map = DeltaTimeNormaliser.Normalise(objects, margin);
            return objects.Select(o => map[o]).ToArray();
        }

        // CF: DeltaTimeNormaliser.Normalise | foreach deltaTimes | zero iterations (no objects)
        [Test]
        public void TestEmptyInputReturnsEmptyMap()
        {
            Assert.That(DeltaTimeNormaliser.Normalise(new List<TaikoDifficultyHitObject>(), 5), Is.Empty);
        }

        // CF: Normalise | current != null FALSE (first value starts a group); odd-count median branch
        [Test]
        public void TestSingleValueMapsToItself()
        {
            Assert.That(normalised(5, 123), Is.EqualTo(new[] { 123d }));
        }

        // CF: Normalise | current != null TRUE && |value - current[0]| <= margin TRUE; odd count median
        [Test]
        public void TestValuesWithinMarginShareOddGroupMedian()
        {
            Assert.That(normalised(5, 100, 103, 105), Is.EqualTo(new[] { 103d, 103d, 103d }));
        }

        // CF: Normalise | even count median branch ((set[mid-1] + set[mid]) / 2)
        [Test]
        public void TestValuesWithinMarginShareEvenGroupMedian()
        {
            Assert.That(normalised(5, 100, 102), Is.EqualTo(new[] { 101d, 101d }));
        }

        // CF: Normalise | |value - current[0]| <= margin FALSE (just above margin) -> new group
        [Test]
        public void TestValuesBeyondMarginFormSeparateGroups()
        {
            Assert.That(normalised(5, 100, 106), Is.EqualTo(new[] { 100d, 106d }));
        }

        // CF: Normalise | margin boundary: difference exactly equal to margin is grouped
        [Test]
        public void TestValueExactlyAtMarginIsGrouped()
        {
            Assert.That(normalised(5, 100, 105), Is.EqualTo(new[] { 102.5, 102.5 }));
        }

        // CF: Normalise | Distinct(): duplicate delta times collapse into one entry yet every object is mapped
        [Test]
        public void TestDuplicateDeltaTimesAreAllMapped()
        {
            Assert.That(normalised(5, 100, 100, 200), Is.EqualTo(new[] { 100d, 100d, 200d }));
        }
    }
}
