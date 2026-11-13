// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Catch.Edit;

namespace osu.Game.Rulesets.Catch.Tests.Edit
{
    [TestFixture]
    public class PositionRangeTest
    {
        // CF: PositionRange.Length | Max - Min > 0 (Math.Max arg selection) | TRUE
        [Test]
        public void TestLengthOfClosedIntervalIsMaxMinusMin()
        {
            Assert.That(new PositionRange(10, 25).Length, Is.EqualTo(15f));
        }

        // CF: PositionRange.Length | Max - Min > 0 (Math.Max arg selection) | FALSE
        [Test]
        public void TestLengthOfEmptyRangeIsZero()
        {
            Assert.That(PositionRange.EMPTY.Length, Is.EqualTo(0f));
        }

        // CF: PositionRange.ctor(float) | delegation to ctor(min, max) | Min == Max
        [Test]
        public void TestSingleValueConstructorCreatesDegenerateInterval()
        {
            var range = new PositionRange(7);

            Assert.That(range.Min, Is.EqualTo(7f));
            Assert.That(range.Max, Is.EqualTo(7f));
        }

        // CF: PositionRange.GetFlippedPosition | Min <= Max | TRUE
        [Test]
        public void TestFlippedPositionMirrorsAroundRangeCentre()
        {
            // range [10, 30], x = 12 -> 30 - (12 - 10) = 28
            Assert.That(new PositionRange(10, 30).GetFlippedPosition(12), Is.EqualTo(28f));
        }

        // CF: PositionRange.GetFlippedPosition | Min <= Max (boundary equal) | TRUE
        [Test]
        public void TestFlippedPositionOfDegenerateRangeReturnsRangeValue()
        {
            Assert.That(new PositionRange(5).GetFlippedPosition(5), Is.EqualTo(5f));
        }

        // CF: PositionRange.GetFlippedPosition | Min <= Max | FALSE
        [Test]
        public void TestFlippedPositionOfEmptyRangeReturnsInputUnchanged()
        {
            Assert.That(PositionRange.EMPTY.GetFlippedPosition(123f), Is.EqualTo(123f));
        }

        // CF: PositionRange.Union | Math.Min/Math.Max both operands | disjoint spans both
        [Test]
        public void TestUnionOfDisjointRangesSpansBoth()
        {
            var union = PositionRange.Union(new PositionRange(0, 10), new PositionRange(20, 30));

            Assert.That(union.Min, Is.EqualTo(0f));
            Assert.That(union.Max, Is.EqualTo(30f));
        }

        // CF: PositionRange.Union | Math.Min/Math.Max with EMPTY operand | identity
        [Test]
        public void TestUnionWithEmptyIsIdentity()
        {
            var union = PositionRange.Union(PositionRange.EMPTY, new PositionRange(4, 9));

            Assert.That(union.Min, Is.EqualTo(4f));
            Assert.That(union.Max, Is.EqualTo(9f));
        }
    }
}
