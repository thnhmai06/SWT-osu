// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Skinning;

namespace osu.Game.Tests.Skinning
{
    [TestFixture]
    public class LegacyManiaSkinConfigurationTest
    {
        [Test]
        public void TestArraysSizedByKeyCount()
        {
            var c = new LegacyManiaSkinConfiguration(4);

            Assert.That(c.Keys, Is.EqualTo(4));
            Assert.That(c.ColumnLineWidth, Has.Length.EqualTo(5));
            Assert.That(c.ColumnSpacing, Has.Length.EqualTo(3));
            Assert.That(c.ColumnWidth, Has.Length.EqualTo(4));
            Assert.That(c.ExplosionWidth, Has.Length.EqualTo(4));
            Assert.That(c.HoldNoteLightWidth, Has.Length.EqualTo(4));
            Assert.That(c.ColumnNoteBodyStyles, Has.Length.EqualTo(4));
        }

        [Test]
        public void TestDefaults()
        {
            var c = new LegacyManiaSkinConfiguration(2);

            Assert.That(c.ColumnLineWidth, Is.All.EqualTo(2f));
            Assert.That(c.ColumnWidth, Is.All.EqualTo(LegacyManiaSkinConfiguration.DEFAULT_COLUMN_SIZE));
            Assert.That(c.MinimumColumnWidth, Is.EqualTo(LegacyManiaSkinConfiguration.DEFAULT_COLUMN_SIZE));
            Assert.That(c.HitPosition, Is.EqualTo(LegacyManiaSkinConfiguration.DEFAULT_HIT_POSITION));
        }

        [Test]
        public void TestMinimumColumnWidth()
        {
            var c = new LegacyManiaSkinConfiguration(3);
            c.ColumnWidth[1] = 10;
            Assert.That(c.MinimumColumnWidth, Is.EqualTo(10));
        }
    }
}
