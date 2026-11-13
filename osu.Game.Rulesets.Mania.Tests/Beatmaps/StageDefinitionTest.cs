// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Rulesets.Mania.Beatmaps;

namespace osu.Game.Rulesets.Mania.Tests.Beatmaps
{
    [TestFixture]
    public class StageDefinitionTest
    {
        // CF: StageDefinition..ctor | columns < 1 | TRUE
        // CF: StageDefinition..ctor | columns < 1 | throws ArgumentException
        [TestCase(0)]
        [TestCase(-1)]
        public void TestNonPositiveColumnsThrows(int columns)
        {
            Assert.Throws<ArgumentException>(() => new StageDefinition(columns));
        }

        // CF: StageDefinition..ctor | columns < 1 | FALSE
        [Test]
        public void TestColumnsStored()
        {
            Assert.That(new StageDefinition(4).Columns, Is.EqualTo(4));
        }

        // CF: StageDefinition.IsSpecialColumn | Columns % 2 == 1 | TRUE
        // CF: StageDefinition.IsSpecialColumn | column == Columns / 2 | TRUE
        // CF: StageDefinition.IsSpecialColumn | column == Columns / 2 | FALSE
        [TestCase(5, 2, true)]
        [TestCase(5, 1, false)]
        [TestCase(5, 3, false)]
        [TestCase(1, 0, true)]
        // CF: StageDefinition.IsSpecialColumn | Columns % 2 == 1 | FALSE
        [TestCase(4, 2, false)]
        [TestCase(4, 1, false)]
        public void TestIsSpecialColumn(int columns, int column, bool expected)
        {
            Assert.That(new StageDefinition(columns).IsSpecialColumn(column), Is.EqualTo(expected));
        }
    }
}
