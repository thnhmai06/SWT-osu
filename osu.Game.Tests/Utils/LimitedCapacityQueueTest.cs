// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Utils;

namespace osu.Game.Tests.Utils
{
    [TestFixture]
    public class LimitedCapacityQueueTest
    {
        // CF: LimitedCapacityQueue.ctor | capacity < 0 | throws ArgumentOutOfRangeException
        [Test]
        public void TestNegativeCapacity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LimitedCapacityQueue<int>(-1));
        }

        // CF: LimitedCapacityQueue.ctor | capacity >= 0 | no throw
        // CF: LimitedCapacityQueue.GetEnumerator | Count == 0 | TRUE
        // CF: LimitedCapacityQueue.GetEnumerator | loop over Count | loop 0
        // CF: LimitedCapacityQueue.Dequeue | Count == 0 | throws InvalidOperationException
        [Test]
        public void TestEmpty()
        {
            var q = new LimitedCapacityQueue<int>(2);
            Assert.That(q, Is.Empty);
            Assert.That(q.Full, Is.False);
            Assert.Throws<InvalidOperationException>(() => q.Dequeue());
        }

        // CF: LimitedCapacityQueue.Enqueue | Count == capacity | FALSE
        // CF: LimitedCapacityQueue.Dequeue | Count == 0 | FALSE
        // CF: LimitedCapacityQueue.GetEnumerator | Count == 0 | FALSE
        // CF: LimitedCapacityQueue.GetEnumerator | loop over Count | loop n
        [Test]
        public void TestEnqueueDequeue()
        {
            var q = new LimitedCapacityQueue<int>(3);
            q.Enqueue(1);
            q.Enqueue(2);
            Assert.That(q.ToArray(), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(q.Dequeue(), Is.EqualTo(1));
            Assert.That(q.Count, Is.EqualTo(1));
        }

        // CF: LimitedCapacityQueue.Enqueue | Count == capacity | TRUE
        [Test]
        public void TestOverflowDropsOldest()
        {
            var q = new LimitedCapacityQueue<int>(2);
            q.Enqueue(1);
            q.Enqueue(2);
            Assert.That(q.Full, Is.True);
            q.Enqueue(3);
            Assert.That(q.ToArray(), Is.EqualTo(new[] { 2, 3 }));
            Assert.That(q.Count, Is.EqualTo(2));
        }

        // CF: LimitedCapacityQueue.this[] | index < 0 || index >= Count | throws ArgumentOutOfRangeException
        // CF: LimitedCapacityQueue.this[] | index < 0 | TRUE
        // CF: LimitedCapacityQueue.this[] | index >= Count | TRUE
        [Test]
        public void TestIndexerOutOfRange()
        {
            var q = new LimitedCapacityQueue<int>(2);
            q.Enqueue(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = q[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = q[1]);
        }

        // CF: LimitedCapacityQueue.this[] | index < 0 || index >= Count | no throw
        // CF: LimitedCapacityQueue.this[] | index < 0 | FALSE
        // CF: LimitedCapacityQueue.this[] | index >= Count | FALSE
        [Test]
        public void TestIndexerAfterWrap()
        {
            var q = new LimitedCapacityQueue<int>(2);
            q.Enqueue(1);
            q.Enqueue(2);
            q.Enqueue(3);
            Assert.That(q[0], Is.EqualTo(2));
            Assert.That(q[1], Is.EqualTo(3));
        }

        [Test]
        public void TestClear()
        {
            var q = new LimitedCapacityQueue<int>(2);
            q.Enqueue(1);
            q.Clear();
            Assert.That(q.Count, Is.EqualTo(0));
            q.Enqueue(7);
            Assert.That(q.ToArray(), Is.EqualTo(new[] { 7 }));
        }
    }
}
