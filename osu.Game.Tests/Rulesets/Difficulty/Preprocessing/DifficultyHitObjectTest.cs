// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Tests.Rulesets.Difficulty.Preprocessing
{
    [TestFixture]
    public class DifficultyHitObjectTest
    {
        private class NestingObject : HitObject
        {
            public NestingObject(HitObject nested)
            {
                HitWindows = HitWindows.Empty;
                AddNested(nested);
            }
        }

        private class DurationObject : HitObject, IHasDuration
        {
            public double EndTime { get; set; }

            public double Duration
            {
                get => EndTime - StartTime;
                set => EndTime = StartTime + value;
            }
        }

        private static HitObject emptyWindows(double time) => new HitObject { StartTime = time, HitWindows = HitWindows.Empty };

        private static HitObject withWindows(double time)
        {
            var windows = new DefaultHitWindows();
            windows.SetDifficulty(5);
            return new HitObject { StartTime = time, HitWindows = windows };
        }

        // CF: DifficultyHitObject.DifficultyHitObject | times divided by clockRate | no throw
        [Test]
        public void TestTimesAreScaledByClockRate()
        {
            var objects = new List<DifficultyHitObject>();
            var last = emptyWindows(1000);
            var current = new DurationObject { StartTime = 1500, EndTime = 2000, HitWindows = HitWindows.Empty };

            var diff = new DifficultyHitObject(current, last, 1.5, objects, 3);

            Assert.That(diff.Index, Is.EqualTo(3));
            Assert.That(diff.DeltaTime, Is.EqualTo(500 / 1.5).Within(1e-9));
            Assert.That(diff.StartTime, Is.EqualTo(1500 / 1.5).Within(1e-9));
            Assert.That(diff.EndTime, Is.EqualTo(2000 / 1.5).Within(1e-9));
            Assert.That(diff.BaseObject, Is.SameAs(current));
            Assert.That(diff.LastObject, Is.SameAs(last));
        }

        // CF: DifficultyHitObject.Previous | index >= 0 && index < Count | TRUE
        // CF: DifficultyHitObject.Previous | index >= 0 && index < Count | FALSE
        // CF: DifficultyHitObject.Next | index >= 0 && index < Count | TRUE
        // CF: DifficultyHitObject.Next | index >= 0 && index < Count | FALSE
        [Test]
        public void TestPreviousAndNext()
        {
            var list = new List<DifficultyHitObject>();
            for (int i = 0; i < 4; i++)
                list.Add(new DifficultyHitObject(emptyWindows(i * 100), emptyWindows(i * 100 - 100), 1, list, i));

            Assert.That(list[0].Previous(), Is.Null);
            Assert.That(list[1].Previous(), Is.SameAs(list[0]));
            Assert.That(list[3].Previous(1), Is.SameAs(list[1]));
            Assert.That(list[3].Previous(5), Is.Null);
            Assert.That(list[3].Next(), Is.Null);
            Assert.That(list[0].Next(), Is.SameAs(list[1]));
            Assert.That(list[0].Next(2), Is.SameAs(list[3]));
            Assert.That(list[0].Next(-5), Is.Null, "negative resulting index is rejected");
        }

        // CF: DifficultyHitObject.getRawHitWindow | BaseObject.HitWindows != Empty | TRUE
        // CF: DifficultyHitObject.HitWindow | 2 * raw / ClockRate | no throw
        [Test]
        public void TestHitWindowGreatUsesOwnWindowsScaledByClockRate()
        {
            var obj = withWindows(0);
            var diff = new DifficultyHitObject(obj, emptyWindows(-100), 2, new List<DifficultyHitObject>(), 0);

            Assert.That(diff.HitWindowGreat, Is.EqualTo(2 * obj.HitWindows.WindowFor(HitResult.Great) / 2).Within(1e-9));
        }

        // CF: DifficultyHitObject.getRawHitWindow | BaseObject.HitWindows != Empty | FALSE
        // CF: DifficultyHitObject.getRawHitWindow | foreach nested | loop n
        // CF: DifficultyHitObject.getRawHitWindow | nestedHitObject.HitWindows != Empty | TRUE
        [Test]
        public void TestHitWindowFallsBackToNestedObjects()
        {
            var nested = withWindows(0);
            var diff = new DifficultyHitObject(new NestingObject(nested), emptyWindows(-100), 1, new List<DifficultyHitObject>(), 0);

            Assert.That(diff.HitWindowGreat, Is.EqualTo(2 * nested.HitWindows.WindowFor(HitResult.Great)).Within(1e-9));
            Assert.That(diff.HitWindowGreat, Is.GreaterThan(0));
        }

        // CF: DifficultyHitObject.getRawHitWindow | nestedHitObject.HitWindows != Empty | FALSE
        // CF: DifficultyHitObject.getRawHitWindow | foreach nested | loop 0
        [Test]
        public void TestHitWindowIsZeroWithoutAnyWindows()
        {
            Assert.That(new DifficultyHitObject(emptyWindows(0), emptyWindows(-100), 1, new List<DifficultyHitObject>(), 0).HitWindowGreat, Is.Zero);
            Assert.That(new DifficultyHitObject(new NestingObject(emptyWindows(0)), emptyWindows(-100), 1, new List<DifficultyHitObject>(), 0).HitWindowGreat, Is.Zero);
        }
    }
}
