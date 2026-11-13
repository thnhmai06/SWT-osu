// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Mania.Beatmaps.Patterns;
using osu.Game.Rulesets.Mania.Objects;

namespace osu.Game.Rulesets.Mania.Tests.Beatmaps.Patterns
{
    [TestFixture]
    public class PatternTest
    {
        private static Note note(int column) => new Note { Column = column };

        // CF: Pattern.HitObjects | hitObjects ?? Enumerable.Empty | null
        // CF: Pattern.ColumnHasObject | containedColumns?.Contains(column) == true | null
        // CF: Pattern.ColumnWithObjects | containedColumns?.Count ?? 0 | null
        // CF: Pattern.Clear | hitObjects?.Clear() | null
        // CF: Pattern.Clear | containedColumns?.Clear() | null
        [Test]
        public void TestEmptyPattern()
        {
            var pattern = new Pattern();

            Assert.That(pattern.HitObjects, Is.Empty);
            Assert.That(pattern.ColumnHasObject(0), Is.False);
            Assert.That(pattern.ColumnWithObjects, Is.EqualTo(0));
            Assert.That(() => pattern.Clear(), Throws.Nothing);
        }

        // CF: Pattern.Add | prepareStorage hitObjects ??= (null) | TRUE
        // CF: Pattern.HitObjects | hitObjects ?? Enumerable.Empty | non-null
        // CF: Pattern.ColumnHasObject | containedColumns?.Contains(column) == true | TRUE
        // CF: Pattern.ColumnHasObject | containedColumns?.Contains(column) == true | FALSE
        // CF: Pattern.ColumnWithObjects | containedColumns?.Count ?? 0 | non-null
        [Test]
        public void TestAddSingle()
        {
            var pattern = new Pattern();
            var a = note(1);
            var b = note(1);
            pattern.Add(a);
            pattern.Add(b);

            Assert.That(pattern.HitObjects.ToArray(), Is.EqualTo(new[] { a, b }));
            Assert.That(pattern.ColumnHasObject(1), Is.True);
            Assert.That(pattern.ColumnHasObject(2), Is.False);
            Assert.That(pattern.ColumnWithObjects, Is.EqualTo(1));
        }

        // CF: Pattern.Add | prepareStorage hitObjects ??= (null) | FALSE
        // CF: Pattern.Add | other.hitObjects != null | TRUE
        // CF: Pattern.Add | foreach h in other.hitObjects | loop n
        [Test]
        public void TestAddPattern()
        {
            var pattern = new Pattern();
            pattern.Add(note(0));

            var other = new Pattern();
            other.Add(note(1));
            other.Add(note(2));
            pattern.Add(other);

            Assert.That(pattern.HitObjects.Count(), Is.EqualTo(3));
            Assert.That(pattern.ColumnWithObjects, Is.EqualTo(3));
        }

        // CF: Pattern.Add | other.hitObjects != null | FALSE
        // CF: Pattern.Add | foreach h in other.hitObjects | loop 0
        [Test]
        public void TestAddEmptyPattern()
        {
            var pattern = new Pattern();
            pattern.Add(new Pattern());

            Assert.That(pattern.HitObjects, Is.Empty);
            Assert.That(pattern.ColumnWithObjects, Is.EqualTo(0));
        }

        // CF: Pattern.Clear | hitObjects?.Clear() | non-null
        // CF: Pattern.Clear | containedColumns?.Clear() | non-null
        [Test]
        public void TestClear()
        {
            var pattern = new Pattern();
            pattern.Add(note(3));
            pattern.Clear();

            Assert.That(pattern.HitObjects, Is.Empty);
            Assert.That(pattern.ColumnHasObject(3), Is.False);
        }
    }
}
