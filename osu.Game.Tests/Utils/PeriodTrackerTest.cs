// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Utils;

namespace osu.Game.Tests.Utils
{
    [TestFixture]
    public class PeriodTrackerTest
    {
        private static PeriodTracker create() => new PeriodTracker(new[] { new Period(50, 60), new Period(0, 10), new Period(20, 30) });

        // CF: PeriodTracker.IsInAny | periods.Count == 0 | TRUE
        [Test]
        public void TestEmpty()
        {
            Assert.That(new PeriodTracker(Array.Empty<Period>()).IsInAny(5, out var p), Is.False);
            Assert.That(p, Is.Null);
        }

        // CF: PeriodTracker.IsInAny | periods.Count == 0 | FALSE
        // CF: PeriodTracker.IsInAny | time > periods[nearestIndex].End | FALSE
        // CF: PeriodTracker.IsInAny | time < periods[nearestIndex].Start && nearestIndex > 0 | FALSE
        // CF: PeriodTracker.IsInAny | isInAny | TRUE
        [Test]
        public void TestInsideFirst()
        {
            Assert.That(create().IsInAny(5, out var p), Is.True);
            Assert.That(p!.Value.Start, Is.EqualTo(0));
        }

        // CF: PeriodTracker.IsInAny | time > periods[nearestIndex].End | TRUE
        // CF: PeriodTracker.IsInAny | while time > End && nearestIndex < Count - 1 | TRUE
        // CF: PeriodTracker.IsInAny | while time > End && nearestIndex < Count - 1 | FALSE
        [Test]
        public void TestForwardThenBackward()
        {
            var t = create();
            Assert.That(t.IsInAny(55), Is.True);
            Assert.That(t.IsInAny(100), Is.False);

            // CF: PeriodTracker.IsInAny | while time < Start && nearestIndex > 0 | TRUE
            Assert.That(t.IsInAny(25), Is.True);
            Assert.That(t.IsInAny(-5), Is.False);
        }

        // CF: PeriodTracker.IsInAny | isInAny | FALSE
        [Test]
        public void TestBetweenPeriods()
        {
            Assert.That(create().IsInAny(15), Is.False);
        }

        // CF: Period.ctor | start >= end | TRUE
        [Test]
        public void TestInvalidPeriod()
        {
            Assert.Throws<ArgumentException>(() => new Period(5, 5));
        }

        // CF: Period.ctor | start >= end | FALSE
        [Test]
        public void TestPeriodDuration()
        {
            Assert.That(new Period(5, 8).Duration, Is.EqualTo(3));
        }
    }
}
