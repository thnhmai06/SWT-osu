// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps.ControlPoints;

namespace osu.Game.Tests.Beatmaps.ControlPoints
{
    [TestFixture]
    public class ControlPointInfoTest
    {
        private static ControlPointInfo createInfo(params double[] timingTimes)
        {
            var info = new ControlPointInfo();

            foreach (double t in timingTimes)
                info.Add(t, new TimingControlPoint { BeatLength = 1000 });

            return info;
        }

        // CF: ControlPointInfo.BinarySearch | n == 0 | TRUE
        // CF: ControlPointInfo.BinarySearch | index >= 0 | FALSE
        // CF: ControlPointInfo.BinarySearchWithFallback | BinarySearch(...) ?? fallback | null
        [Test]
        public void TestEmptyListFallsBack()
        {
            var info = createInfo();

            Assert.That(ControlPointInfo.BinarySearch(info.TimingPoints, 100), Is.Null);
            Assert.That(info.TimingPointAt(100), Is.SameAs(TimingControlPoint.DEFAULT));
            Assert.That(info.EffectPointAt(100), Is.SameAs(EffectControlPoint.DEFAULT));
        }

        // CF: ControlPointInfo.BinarySearch | list == null | throws ArgumentNullException
        [Test]
        public void TestBinarySearchNullListThrows()
        {
            Assert.Throws<ArgumentNullException>(() => ControlPointInfo.BinarySearch<TimingControlPoint>(null, 0));
            Assert.Throws<ArgumentNullException>(() => ControlPointInfo.BinarySearch<TimingControlPoint>(null, 0, EqualitySelection.Leftmost));
        }

        // CF: ControlPointInfo.BinarySearch | time < list[0].Time | TRUE
        // CF: ControlPointInfo.BinarySearch | time > list[^1].Time | TRUE
        // CF: ControlPointInfo.BinarySearch | time > list[^1].Time | FALSE
        // CF: ControlPointInfo.BinarySearch | index >= 0 | TRUE
        // CF: ControlPointInfo.BinarySearchWithFallback | BinarySearch(...) ?? fallback | non-null
        // CF: ControlPointInfo.TimingPointAt | TimingPoints.Count > 0 | TRUE
        [Test]
        public void TestTimingPointAtBoundaries()
        {
            var info = createInfo(100, 200);

            Assert.That(info.TimingPointAt(50), Is.SameAs(info.TimingPoints[0]), "before first falls back to first point");
            Assert.That(info.TimingPointAt(100), Is.SameAs(info.TimingPoints[0]));
            Assert.That(info.TimingPointAt(150), Is.SameAs(info.TimingPoints[0]));
            Assert.That(info.TimingPointAt(200), Is.SameAs(info.TimingPoints[1]));
            Assert.That(info.TimingPointAt(999), Is.SameAs(info.TimingPoints[1]));
        }

        // CF: ControlPointInfo.BinarySearch(list,time,sel) | list[pivot].Time < time | TRUE
        // CF: ControlPointInfo.BinarySearch(list,time,sel) | list[pivot].Time > time | TRUE
        // CF: ControlPointInfo.BinarySearch(list,time,sel) | equalityFound | FALSE
        [Test]
        public void TestBinarySearchMissReturnsComplementOfInsertionPoint()
        {
            var info = createInfo(0, 100, 200, 300);

            Assert.That(ControlPointInfo.BinarySearch(info.TimingPoints, 150, EqualitySelection.FirstFound), Is.EqualTo(~2));
            Assert.That(ControlPointInfo.BinarySearch(info.TimingPoints, -1, EqualitySelection.FirstFound), Is.EqualTo(-1));
            Assert.That(ControlPointInfo.BinarySearch(info.TimingPoints, 301, EqualitySelection.FirstFound), Is.EqualTo(~4));
        }

        // CF: ControlPointInfo.BinarySearch(list,time,sel) | case Leftmost (in loop) | case Leftmost
        // CF: ControlPointInfo.BinarySearch(list,time,sel) | case Rightmost (in loop) | case Rightmost
        // CF: ControlPointInfo.BinarySearch(list,time,sel) | case FirstFound (in loop) | case FirstFound
        // CF: ControlPointInfo.BinarySearch(list,time,sel) | case Leftmost (result) | case Leftmost
        // CF: ControlPointInfo.BinarySearch(list,time,sel) | default (result) | default
        [Test]
        public void TestBinarySearchEqualitySelection()
        {
            // two timing points at the same time are not possible through groups, so use a plain list stub.
            var list = new[] { new T(0), new T(100), new T(100), new T(100), new T(200) };

            Assert.That(ControlPointInfo.BinarySearch(list, 100, EqualitySelection.Leftmost), Is.EqualTo(1));
            Assert.That(ControlPointInfo.BinarySearch(list, 100, EqualitySelection.Rightmost), Is.EqualTo(3));
            Assert.That(list[ControlPointInfo.BinarySearch(list, 100, EqualitySelection.FirstFound)].Time, Is.EqualTo(100));
        }

        private class T : IControlPoint
        {
            public double Time { get; }
            public T(double time) => Time = time;
            public int CompareTo(IControlPoint? other) => Time.CompareTo(other?.Time);
        }

        // CF: ControlPointInfo.TimingPointAfter | index < 0 | TRUE
        // CF: ControlPointInfo.TimingPointAfter | index < 0 | FALSE
        // CF: ControlPointInfo.TimingPointAfter | index < TimingPoints.Count | TRUE
        // CF: ControlPointInfo.TimingPointAfter | index < TimingPoints.Count | FALSE
        [Test]
        public void TestTimingPointAfter()
        {
            var info = createInfo(100, 200);

            Assert.That(info.TimingPointAfter(50), Is.SameAs(info.TimingPoints[0]));
            Assert.That(info.TimingPointAfter(100), Is.SameAs(info.TimingPoints[1]), "exact match returns the next point");
            Assert.That(info.TimingPointAfter(200), Is.Null);
        }

        // CF: ControlPointInfo.BPMMaximum | MinBy(BeatLength) ?? DEFAULT | non-null
        // CF: ControlPointInfo.BPMMinimum | MaxBy(BeatLength) ?? DEFAULT | non-null
        // CF: ControlPointInfo.BPMMaximum | MinBy(BeatLength) ?? DEFAULT | null
        // CF: ControlPointInfo.BPMMinimum | MaxBy(BeatLength) ?? DEFAULT | null
        [Test]
        public void TestBpmExtremes()
        {
            var info = new ControlPointInfo();
            Assert.That(info.BPMMaximum, Is.EqualTo(60000 / TimingControlPoint.DEFAULT.BeatLength));
            Assert.That(info.BPMMinimum, Is.EqualTo(60000 / TimingControlPoint.DEFAULT.BeatLength));

            info.Add(0, new TimingControlPoint { BeatLength = 500 });
            info.Add(1000, new TimingControlPoint { BeatLength = 1000 });

            Assert.That(info.BPMMaximum, Is.EqualTo(120));
            Assert.That(info.BPMMinimum, Is.EqualTo(60));
        }

        // CF: ControlPointInfo.Add | CheckAlreadyExisting | TRUE
        // CF: ControlPointInfo.Add | CheckAlreadyExisting | FALSE
        // CF: ControlPointInfo.CheckAlreadyExisting | case EffectControlPoint | case EffectControlPoint
        // CF: ControlPointInfo.CheckAlreadyExisting | case TimingControlPoint | case TimingControlPoint
        // CF: ControlPointInfo.CheckAlreadyExisting | newPoint?.IsRedundant(existing) == true | FALSE
        [Test]
        public void TestRedundantEffectPointIsNotAdded()
        {
            var info = new ControlPointInfo();

            Assert.That(info.Add(0, new EffectControlPoint { KiaiMode = false }), Is.False, "equal to the default effect point");
            Assert.That(info.EffectPoints, Is.Empty);

            Assert.That(info.Add(0, new EffectControlPoint { KiaiMode = true }), Is.True);
            Assert.That(info.Add(500, new EffectControlPoint { KiaiMode = true }), Is.False);
            Assert.That(info.Add(500, new EffectControlPoint { KiaiMode = false }), Is.True);
            Assert.That(info.EffectPoints.Count, Is.EqualTo(2));

            Assert.That(info.Add(0, new TimingControlPoint()), Is.True);
            Assert.That(info.Add(0, new TimingControlPoint()), Is.True, "timing points are never redundant");
        }

        // CF: ControlPointInfo.GroupAt | i >= 0 | TRUE
        // CF: ControlPointInfo.GroupAt | addIfNotExisting | FALSE
        // CF: ControlPointInfo.GroupAt | addIfNotExisting | TRUE
        [Test]
        public void TestGroupAt()
        {
            var info = new ControlPointInfo();

            Assert.That(info.GroupAt(10), Is.Null);

            var group = info.GroupAt(10, true);
            Assert.That(group, Is.Not.Null);
            Assert.That(info.GroupAt(10), Is.SameAs(group));
            Assert.That(info.Groups.Count, Is.EqualTo(1));
        }

        // CF: ControlPointInfo.GroupItemAdded | case TimingControlPoint | case TimingControlPoint
        // CF: ControlPointInfo.GroupItemAdded | case EffectControlPoint | case EffectControlPoint
        // CF: ControlPointInfo.GroupItemAdded | default | throws ArgumentException
        [Test]
        public void TestUnsupportedControlPointTypeThrows()
        {
            var info = new ControlPointInfo();
            var group = info.GroupAt(0, true);

            Assert.Throws<ArgumentException>(() => group.Add(new DifficultyControlPoint()));
        }

        // CF: ControlPointInfo.RemoveGroup | foreach item | loop n
        // CF: ControlPointInfo.GroupItemRemoved | case TimingControlPoint | case TimingControlPoint
        // CF: ControlPointInfo.GroupItemRemoved | case EffectControlPoint | case EffectControlPoint
        // CF: ControlPointInfo.GroupItemRemoved | no case match | default
        // CF: ControlPointInfo.raiseControlPointsChanged | ControlPointsChanged?.Invoke | non-null
        [Test]
        public void TestRemoveGroupRemovesPointsAndRaisesEvent()
        {
            var info = new ControlPointInfo();
            info.Add(0, new TimingControlPoint());
            info.Add(0, new EffectControlPoint { KiaiMode = true });

            int changes = 0;
            info.ControlPointsChanged += () => changes++;

            info.RemoveGroup(info.GroupAt(0));

            Assert.That(info.TimingPoints, Is.Empty);
            Assert.That(info.EffectPoints, Is.Empty);
            Assert.That(info.Groups, Is.Empty);
            Assert.That(changes, Is.EqualTo(2));
        }

        // CF: ControlPointInfo.Clear | groups/timing/effect cleared | no throw
        // CF: ControlPointInfo.DeepClone | foreach point | loop n
        [Test]
        public void TestDeepCloneAndClear()
        {
            var info = new ControlPointInfo();
            info.Add(0, new TimingControlPoint { BeatLength = 500 });
            info.Add(1000, new EffectControlPoint { KiaiMode = true });

            var clone = info.DeepClone();

            Assert.That(clone.AllControlPoints.Count(), Is.EqualTo(2));
            Assert.That(clone.TimingPoints[0], Is.Not.SameAs(info.TimingPoints[0]));
            Assert.That(clone.TimingPoints[0].BeatLength, Is.EqualTo(500));

            info.Clear();
            Assert.That(info.AllControlPoints, Is.Empty);
            Assert.That(clone.AllControlPoints.Count(), Is.EqualTo(2));
        }

        // CF: ControlPointInfo.getClosestPositiveSnappedTime | snappedTime >= 0 | TRUE
        // CF: ControlPointInfo.GetClosestSnappedTime | referenceTime.HasValue | FALSE
        // CF: ControlPointInfo.GetClosestSnappedTime | timingPointAfter is null | TRUE
        [Test]
        public void TestSnapToBeatDivisor()
        {
            var info = createInfo(0);

            Assert.That(info.GetClosestSnappedTime(260, 4), Is.EqualTo(250));
            Assert.That(info.GetClosestSnappedTime(260, 1), Is.EqualTo(0));
            Assert.That(info.GetClosestSnappedTime(600, 1), Is.EqualTo(1000));
        }

        // CF: ControlPointInfo.GetClosestSnappedTime | referenceTime.HasValue | TRUE
        [Test]
        public void TestSnapWithReferenceTimeIgnoresLaterTimingPoint()
        {
            var info = createInfo(0, 1000);

            // without a reference time the later timing point is closer (990 -> 1000); with a reference at 0, snapping uses the first point only.
            Assert.That(info.GetClosestSnappedTime(990, 2), Is.EqualTo(1000));
            Assert.That(info.GetClosestSnappedTime(990, 2, 0), Is.EqualTo(1000));
            Assert.That(info.GetClosestSnappedTime(1200, 4, 0), Is.EqualTo(1250));
        }

        // CF: ControlPointInfo.GetClosestSnappedTime | Math.Abs(time - snappedTime) < Math.Abs(time - timingPointAfter.Time) | FALSE
        // CF: ControlPointInfo.GetClosestSnappedTime | timingPointAfter is null | FALSE
        [Test]
        public void TestSnapPrefersCloserFollowingTimingPoint()
        {
            // beat length 1000 at 0, next timing point at 400: time 380 snaps (div 1) to 0, but the point at 400 is closer.
            var info = createInfo(0, 400);

            Assert.That(info.GetClosestSnappedTime(380, 1), Is.EqualTo(400));
        }

        // CF: ControlPointInfo.getClosestPositiveSnappedTime | snappedTime >= 0 | FALSE
        [Test]
        public void TestSnapNegativeTimeMovesForward()
        {
            var info = new ControlPointInfo();
            info.Add(-600, new TimingControlPoint { BeatLength = 1000 });

            // at div 1 the grid is ..., -600, 400, 1400; time 0 -> nearest grid time -600 is negative so the next beat (400) is used.
            Assert.That(info.GetClosestSnappedTime(0, 1), Is.EqualTo(400));
        }

        // CF: ControlPointInfo.GetClosestBeatDivisor | time < 0 | TRUE
        // CF: ControlPointInfo.GetClosestBeatDivisor | time < 0 | FALSE
        // CF: ControlPointInfo.GetClosestBeatDivisor | foreach divisor | loop n
        // CF: ControlPointInfo.GetClosestBeatDivisor | Precision.DefinitelyBigger(closestTime, distanceFromSnap) | TRUE
        // CF: ControlPointInfo.GetClosestBeatDivisor | Precision.DefinitelyBigger(closestTime, distanceFromSnap) | FALSE
        [Test]
        public void TestClosestBeatDivisor()
        {
            var info = createInfo(0);

            Assert.That(info.GetClosestBeatDivisor(500), Is.EqualTo(2));
            Assert.That(info.GetClosestBeatDivisor(250), Is.EqualTo(4));
            Assert.That(info.GetClosestBeatDivisor(0), Is.EqualTo(1));
            Assert.That(info.GetClosestBeatDivisor(-500), Is.EqualTo(2), "negative time is shifted back into the positive range");
            Assert.That(info.GetClosestBeatDivisor(500, 0), Is.EqualTo(2));
        }
    }
}
