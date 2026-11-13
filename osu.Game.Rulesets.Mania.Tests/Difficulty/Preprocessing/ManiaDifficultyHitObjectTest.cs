// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Mania.Objects;

namespace osu.Game.Rulesets.Mania.Tests.Difficulty.Preprocessing
{
    [TestFixture]
    public class ManiaDifficultyHitObjectTest
    {
        // CF: ManiaDifficultyHitObject..ctor | index > 0 | FALSE
        // CF: ManiaDifficultyHitObject..ctor | PrevInColumn(0)?.StartTime ?? StartTime | null
        // CF: ManiaDifficultyHitObject.PrevInColumn | index >= 0 | FALSE
        // CF: ManiaDifficultyHitObject.NextInColumn | index >= 0 && index < Count | FALSE
        [Test]
        public void TestFirstObject()
        {
            var o = DifficultyObjects.Create(2, new Note { StartTime = 100, Column = 0 })[0];

            Assert.That(o.PreviousHitObjects, Is.All.Null);
            Assert.That(o.ColumnStrainTime, Is.EqualTo(100));
            Assert.That(o.PrevInColumn(0), Is.Null);
            Assert.That(o.NextInColumn(0), Is.Null);
        }

        // CF: ManiaDifficultyHitObject..ctor | index > 0 | TRUE
        // CF: ManiaDifficultyHitObject..ctor | for i < prevNote.PreviousHitObjects.Length | loop n
        // CF: ManiaDifficultyHitObject..ctor | PrevInColumn(0)?.StartTime ?? StartTime | non-null
        // CF: ManiaDifficultyHitObject.PrevInColumn | index >= 0 && index < Count | TRUE
        // CF: ManiaDifficultyHitObject.PrevInColumn | index >= 0 | TRUE (index < Count FALSE: unreachable for prev)
        // CF: ManiaDifficultyHitObject.NextInColumn | index >= 0 && index < Count | TRUE
        // CF: ManiaDifficultyHitObject.NextInColumn | index < Count | FALSE
        [Test]
        public void TestNeighbours()
        {
            var list = DifficultyObjects.Create(2,
                new Note { StartTime = 100, Column = 0 },
                new Note { StartTime = 200, Column = 1 },
                new Note { StartTime = 400, Column = 0 });

            Assert.That(list[2].PreviousHitObjects[0], Is.SameAs(list[0]));
            Assert.That(list[2].PreviousHitObjects[1], Is.SameAs(list[1]));
            Assert.That(list[2].ColumnStrainTime, Is.EqualTo(300));
            Assert.That(list[2].PrevInColumn(0), Is.SameAs(list[0]));
            Assert.That(list[2].PrevInColumn(1), Is.Null);
            Assert.That(list[0].NextInColumn(0), Is.SameAs(list[2]));
            Assert.That(list[2].NextInColumn(0), Is.Null);
            Assert.That(list[1].ColumnStrainTime, Is.EqualTo(200));
        }
    }
}
