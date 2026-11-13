// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Timing;

namespace osu.Game.Tests.Rulesets.Timing
{
    [TestFixture]
    public class MultiplierControlPointTest
    {
        [Test]
        public void TestDefaultMultiplierIsOne()
        {
            Assert.That(new MultiplierControlPoint().Multiplier, Is.EqualTo(1).Within(1e-9));
        }

        [Test]
        public void TestMultiplierScalesWithVelocityScrollSpeedAndBeatLength()
        {
            var point = new MultiplierControlPoint(100)
            {
                Velocity = 2,
                BaseBeatLength = 500,
                TimingPoint = { BeatLength = 250 },
                EffectPoint = { ScrollSpeed = 3 },
            };

            Assert.That(point.Time, Is.EqualTo(100));
            Assert.That(point.Multiplier, Is.EqualTo(2 * 3 * 500 / 250.0).Within(1e-9));
        }

        // CF: MultiplierControlPoint.CompareTo | other?.Time | non-null
        // CF: MultiplierControlPoint.CompareTo | other?.Time | null
        [Test]
        public void TestCompareToOrdersByTime()
        {
            var early = new MultiplierControlPoint(0);
            var late = new MultiplierControlPoint(10);

            Assert.That(early.CompareTo(late), Is.Negative);
            Assert.That(late.CompareTo(early), Is.Positive);
            Assert.That(early.CompareTo(new MultiplierControlPoint(0)), Is.Zero);
            Assert.That(early.CompareTo(null), Is.Positive, "null sorts before everything");
        }
    }
}
