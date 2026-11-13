// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Utils;

namespace osu.Game.Tests.Utils
{
    [TestFixture]
    public class OptionalTest
    {
        // CF: Optional.GetOr | HasValue | TRUE
        [Test]
        public void TestWithValue()
        {
            Optional<int> o = 5;
            Assert.That(o.HasValue, Is.True);
            Assert.That(o.GetOr(9), Is.EqualTo(5));
        }

        // CF: Optional.GetOr | HasValue | FALSE
        [Test]
        public void TestDefault()
        {
            var o = default(Optional<int>);
            Assert.That(o.HasValue, Is.False);
            Assert.That(o.GetOr(9), Is.EqualTo(9));
        }
    }
}
