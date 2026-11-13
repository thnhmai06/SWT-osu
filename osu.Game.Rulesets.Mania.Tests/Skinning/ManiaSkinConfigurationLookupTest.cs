// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Skinning;
using osu.Game.Rulesets.Mania.Skinning;

namespace osu.Game.Rulesets.Mania.Tests.Skinning
{
    [TestFixture]
    public class ManiaSkinConfigurationLookupTest
    {
        // CF: ManiaSkinConfigurationLookup..ctor | columnIndex = null default | null
        [Test]
        public void TestDefaultColumnIndexIsNull()
        {
            var l = new ManiaSkinConfigurationLookup(LegacyManiaSkinConfigurationLookups.ColumnWidth);
            Assert.That(l.ColumnIndex, Is.Null);
            Assert.That(l.ToString(), Is.EqualTo("[ManiaSkinConfigurationLookup lookup:ColumnWidth col:]"));
        }

        // CF: ManiaSkinConfigurationLookup..ctor | columnIndex provided | non-null
        [Test]
        public void TestColumnIndex()
        {
            var l = new ManiaSkinConfigurationLookup(LegacyManiaSkinConfigurationLookups.ColumnWidth, 3);
            Assert.That(l.ColumnIndex, Is.EqualTo(3));
            Assert.That(l.ToString(), Does.EndWith("col:3]"));
        }
    }
}
