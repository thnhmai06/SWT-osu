// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Colour.Data;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Difficulty.Preprocessing.Colour.Data
{
    [TestFixture]
    public class RepeatingHitPatternsTest
    {
        // Each pattern holds one mono streak with the given length.
        private static RepeatingHitPatterns create(RepeatingHitPatterns? previous, params int[] patternLengths)
        {
            var result = new RepeatingHitPatterns(previous);
            foreach (int length in patternLengths)
                result.AlternatingMonoPatterns.Add(DifficultyObjectFactory.Pattern((length, HitType.Centre)));
            return result;
        }

        // CF: RepeatingHitPatterns.FindRepetitionInterval | Previous == null | TRUE
        [Test]
        public void TestNoPreviousGivesMaxPlusOne()
        {
            var p = create(null, 1);
            p.FindRepetitionInterval();
            Assert.That(p.RepetitionInterval, Is.EqualTo(17));
        }

        // CF: RepeatingHitPatterns.FindRepetitionInterval | Previous == null | FALSE
        // CF: RepeatingHitPatterns.FindRepetitionInterval | interval < max_repetition_interval | TRUE
        // CF: RepeatingHitPatterns.FindRepetitionInterval | isRepetitionOf(other) | TRUE
        // CF: RepeatingHitPatterns.isRepetitionOf | AlternatingMonoPatterns.Count != other.Count | FALSE
        // CF: RepeatingHitPatterns.isRepetitionOf | for i < Math.Min(Count, 2) | loop 1
        [Test]
        public void TestImmediateRepetition()
        {
            var p = create(create(null, 3), 3);
            p.FindRepetitionInterval();
            Assert.That(p.RepetitionInterval, Is.EqualTo(1));
        }

        // CF: RepeatingHitPatterns.FindRepetitionInterval | isRepetitionOf(other) | FALSE
        // CF: RepeatingHitPatterns.FindRepetitionInterval | other == null | FALSE
        [Test]
        public void TestRepetitionTwoBack()
        {
            var a = create(null, 3);
            var b = create(a, 5);
            var c = create(b, 3);
            c.FindRepetitionInterval();
            Assert.That(c.RepetitionInterval, Is.EqualTo(2));
        }

        // CF: RepeatingHitPatterns.FindRepetitionInterval | other == null | TRUE
        [Test]
        public void TestNoRepetitionRunsOutOfHistory()
        {
            var b = create(create(null, 3), 5);
            b.FindRepetitionInterval();
            Assert.That(b.RepetitionInterval, Is.EqualTo(17));
        }

        // CF: RepeatingHitPatterns.FindRepetitionInterval | interval < max_repetition_interval | FALSE
        [Test]
        public void TestHistoryLongerThanMaxIntervalIsNotSearched()
        {
            RepeatingHitPatterns? previous = null;
            for (int i = 1; i <= 17; i++)
                previous = create(previous, i);
            var last = create(previous, 1); // equals the oldest, which is 17 back
            last.FindRepetitionInterval();
            Assert.That(last.RepetitionInterval, Is.EqualTo(17));
        }

        // CF: RepeatingHitPatterns.isRepetitionOf | AlternatingMonoPatterns.Count != other.Count | TRUE
        [Test]
        public void TestDifferentPatternCountIsNotRepetition()
        {
            var p = create(create(null, 3, 3), 3);
            p.FindRepetitionInterval();
            Assert.That(p.RepetitionInterval, Is.EqualTo(17));
        }

        // CF: RepeatingHitPatterns.isRepetitionOf | for i < Math.Min(Count, 2) | loop 0
        [Test]
        public void TestEmptyPatternsRepeatEachOther()
        {
            var p = create(create(null));
            p.FindRepetitionInterval();
            Assert.That(p.RepetitionInterval, Is.EqualTo(1));
        }

        // CF: RepeatingHitPatterns.isRepetitionOf | for i < Math.Min(Count, 2) | loop n
        // CF: RepeatingHitPatterns.isRepetitionOf | !HasIdenticalMonoLength | FALSE
        [Test]
        public void TestOnlyFirstTwoPatternsAreCompared()
        {
            var p = create(create(null, 2, 3, 4), 2, 3, 9);
            p.FindRepetitionInterval();
            Assert.That(p.RepetitionInterval, Is.EqualTo(1));
        }

        // CF: RepeatingHitPatterns.isRepetitionOf | !HasIdenticalMonoLength | TRUE
        [Test]
        public void TestDifferentSecondPatternIsNotRepetition()
        {
            var p = create(create(null, 2, 3), 2, 4);
            p.FindRepetitionInterval();
            Assert.That(p.RepetitionInterval, Is.EqualTo(17));
        }
    }
}
