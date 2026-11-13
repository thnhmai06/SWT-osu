// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using NUnit.Framework;
using osu.Game.Beatmaps.Formats;

namespace osu.Game.Tests.Beatmaps.Formats
{
    [TestFixture]
    public class ParsingTest
    {
        // CF: Parsing.ParseFloat | output < -parseLimit | TRUE
        [Test]
        public void TestFloatTooLow()
        {
            Assert.That(() => Parsing.ParseFloat("-11", 10), Throws.InstanceOf<OverflowException>());
        }

        // CF: Parsing.ParseFloat | output > parseLimit | TRUE
        [Test]
        public void TestFloatTooHigh()
        {
            Assert.That(() => Parsing.ParseFloat("11", 10), Throws.InstanceOf<OverflowException>());
        }

        // CF: Parsing.ParseFloat | output < -parseLimit | FALSE
        // CF: Parsing.ParseFloat | output > parseLimit | FALSE
        // CF: Parsing.ParseFloat | !allowNaN && float.IsNaN(output) | FALSE
        [TestCase("10", 10f)]
        [TestCase("-10", -10f)]
        [TestCase("1.5", 1.5f)]
        public void TestFloatValid(string input, float expected)
        {
            Assert.That(Parsing.ParseFloat(input, 10), Is.EqualTo(expected));
        }

        // CF: Parsing.ParseFloat | !allowNaN && float.IsNaN(output) | TRUE
        [Test]
        public void TestFloatNaNRejected()
        {
            Assert.That(() => Parsing.ParseFloat("NaN"), Throws.InstanceOf<FormatException>());
        }

        // CF: Parsing.ParseFloat | !allowNaN | FALSE
        [Test]
        public void TestFloatNaNAllowed()
        {
            Assert.That(float.IsNaN(Parsing.ParseFloat("NaN", allowNaN: true)), Is.True);
        }

        [Test]
        public void TestFloatUsesInvariantCulture()
        {
            var previous = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                Assert.That(Parsing.ParseFloat("1.5"), Is.EqualTo(1.5f));
                Assert.That(Parsing.ParseDouble("1.5"), Is.EqualTo(1.5));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        // CF: Parsing.ParseDouble | output < -parseLimit | TRUE
        [Test]
        public void TestDoubleTooLow()
        {
            Assert.That(() => Parsing.ParseDouble("-11", 10), Throws.InstanceOf<OverflowException>());
        }

        // CF: Parsing.ParseDouble | output > parseLimit | TRUE
        [Test]
        public void TestDoubleTooHigh()
        {
            Assert.That(() => Parsing.ParseDouble("11", 10), Throws.InstanceOf<OverflowException>());
        }

        // CF: Parsing.ParseDouble | output < -parseLimit | FALSE
        // CF: Parsing.ParseDouble | output > parseLimit | FALSE
        // CF: Parsing.ParseDouble | !allowNaN && double.IsNaN(output) | FALSE
        [TestCase("10", 10d)]
        [TestCase("-10", -10d)]
        public void TestDoubleValid(string input, double expected)
        {
            Assert.That(Parsing.ParseDouble(input, 10), Is.EqualTo(expected));
        }

        // CF: Parsing.ParseDouble | !allowNaN && double.IsNaN(output) | TRUE
        [Test]
        public void TestDoubleNaNRejected()
        {
            Assert.That(() => Parsing.ParseDouble("NaN"), Throws.InstanceOf<FormatException>());
        }

        [Test]
        public void TestDoubleNaNAllowed()
        {
            Assert.That(double.IsNaN(Parsing.ParseDouble("NaN", allowNaN: true)), Is.True);
        }

        // CF: Parsing.ParseInt | output < -parseLimit | TRUE
        [Test]
        public void TestIntTooLow()
        {
            Assert.That(() => Parsing.ParseInt("-11", 10), Throws.InstanceOf<OverflowException>());
        }

        // CF: Parsing.ParseInt | output > parseLimit | TRUE
        [Test]
        public void TestIntTooHigh()
        {
            Assert.That(() => Parsing.ParseInt("11", 10), Throws.InstanceOf<OverflowException>());
        }

        // CF: Parsing.ParseInt | output < -parseLimit | FALSE
        // CF: Parsing.ParseInt | output > parseLimit | FALSE
        [TestCase("10", 10)]
        [TestCase("-10", -10)]
        [TestCase("0", 0)]
        public void TestIntValid(string input, int expected)
        {
            Assert.That(Parsing.ParseInt(input, 10), Is.EqualTo(expected));
        }
    }
}
