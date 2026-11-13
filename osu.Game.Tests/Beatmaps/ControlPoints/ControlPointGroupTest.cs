// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps.ControlPoints;

namespace osu.Game.Tests.Beatmaps.ControlPoints
{
    [TestFixture]
    public class ControlPointGroupTest
    {
        // CF: ControlPointGroup.Add | existing != null | FALSE
        // CF: ControlPointGroup.Add | ItemAdded?.Invoke | non-null
        [Test]
        public void TestAddRaisesEventAndAttachesGroup()
        {
            var group = new ControlPointGroup(100);
            ControlPoint? added = null;
            group.ItemAdded += p => added = p;

            var point = new TimingControlPoint();
            group.Add(point);

            Assert.That(added, Is.SameAs(point));
            Assert.That(group.ControlPoints, Is.EqualTo(new[] { point }));
            Assert.That(point.Time, Is.EqualTo(100));
        }

        // CF: ControlPointGroup.Add | existing != null | TRUE
        // CF: ControlPointGroup.Remove | ItemRemoved?.Invoke | non-null
        [Test]
        public void TestAddingSameTypeReplacesExisting()
        {
            var group = new ControlPointGroup(0);
            var first = new TimingControlPoint { BeatLength = 500 };
            var second = new TimingControlPoint { BeatLength = 250 };
            ControlPoint? removed = null;
            group.ItemRemoved += p => removed = p;

            group.Add(first);
            group.Add(second);
            group.Add(new EffectControlPoint());

            Assert.That(removed, Is.SameAs(first));
            Assert.That(group.ControlPoints.Count, Is.EqualTo(2));
            Assert.That(group.ControlPoints, Does.Contain(second));
        }

        // CF: ControlPointGroup.raiseItemChanged | ItemChanged?.Invoke | non-null
        // CF: ControlPointGroup.Remove | point.Changed -= raiseItemChanged | no throw
        [Test]
        public void TestPointChangesBubbleUntilRemoved()
        {
            var group = new ControlPointGroup(0);
            var point = new TimingControlPoint();
            int changes = 0;
            group.ItemChanged += _ => changes++;
            group.Add(point);

            point.BeatLength = 123;
            Assert.That(changes, Is.EqualTo(1));

            group.Remove(point);
            point.BeatLength = 321;
            Assert.That(changes, Is.EqualTo(1));
        }

        // CF: ControlPointGroup.CompareTo | other == null | TRUE
        [Test]
        public void TestCompareTo()
        {
            Assert.That(new ControlPointGroup(1).CompareTo(new ControlPointGroup(2)), Is.LessThan(0));
            Assert.That(new ControlPointGroup(2).CompareTo(new ControlPointGroup(2)), Is.Zero);
            Assert.That(new ControlPointGroup(2).CompareTo(null), Is.GreaterThan(0));
        }

        // CF: ControlPointGroup.Equals | other != null | FALSE
        // CF: ControlPointGroup.Equals | Time == other.Time | TRUE
        // CF: ControlPointGroup.Equals | Time == other.Time | FALSE
        // CF: ControlPointGroup.Equals | ControlPoints.SequenceEqual | TRUE
        // CF: ControlPointGroup.Equals | ControlPoints.SequenceEqual | FALSE
        // CF: ControlPointGroup.Equals(object) | obj is ControlPointGroup | FALSE
        [Test]
        public void TestEquality()
        {
            var a = new ControlPointGroup(10);
            a.Add(new TimingControlPoint { BeatLength = 500 });
            var b = new ControlPointGroup(10);
            b.Add(new TimingControlPoint { BeatLength = 500 });
            var differentPoints = new ControlPointGroup(10);
            differentPoints.Add(new TimingControlPoint { BeatLength = 400 });

            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a.Equals(differentPoints), Is.False);
            Assert.That(a.Equals(new ControlPointGroup(11)), Is.False);
            Assert.That(a.Equals((ControlPointGroup?)null), Is.False);
            Assert.That(EqualityComparer(a, "not a group"), Is.False);
            Assert.That(EqualityComparer(a, b), Is.True);
        }

        private static bool EqualityComparer(object x, object y) => System.Collections.Generic.EqualityComparer<object>.Default.Equals(x, y);
    }
}
