// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Colour.Data;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Difficulty.Preprocessing.Colour.Data
{
    [TestFixture]
    public class AlternatingMonoPatternTest
    {
        // Builds a pattern of streaks; each streak is (length, type).
        private static AlternatingMonoPattern pattern(params (int length, HitType type)[] streaks)
        {
            var result = new AlternatingMonoPattern();

            foreach (var (length, type) in streaks)
            {
                var streak = new MonoStreak();
                var objects = DifficultyObjectFactory.Create(new (double, HitType)[length].Select(_ => (100d, type)).ToArray());
                streak.HitObjects.AddRange(objects);
                result.MonoStreaks.Add(streak);
            }

            return result;
        }

        // CF: AlternatingMonoPattern.HasIdenticalMonoLength | other.RunLength == RunLength | TRUE
        [Test]
        public void TestIdenticalLength()
            => Assert.That(pattern((2, HitType.Centre)).HasIdenticalMonoLength(pattern((2, HitType.Rim))), Is.True);

        // CF: AlternatingMonoPattern.HasIdenticalMonoLength | other.RunLength == RunLength | FALSE
        // CF: AlternatingMonoPattern.IsRepetitionOf | HasIdenticalMonoLength(other) | FALSE
        [Test]
        public void TestDifferentLengthIsNotRepetition()
        {
            var a = pattern((2, HitType.Centre));
            var b = pattern((3, HitType.Centre));
            Assert.That(a.HasIdenticalMonoLength(b), Is.False);
            Assert.That(a.IsRepetitionOf(b), Is.False);
        }

        // CF: AlternatingMonoPattern.IsRepetitionOf | HasIdenticalMonoLength(other) | TRUE
        // CF: AlternatingMonoPattern.IsRepetitionOf | other.MonoStreaks.Count == MonoStreaks.Count | FALSE
        [Test]
        public void TestDifferentStreakCountIsNotRepetition()
            => Assert.That(pattern((2, HitType.Centre), (1, HitType.Rim)).IsRepetitionOf(pattern((2, HitType.Centre))), Is.False);

        // CF: AlternatingMonoPattern.IsRepetitionOf | other.MonoStreaks.Count == MonoStreaks.Count | TRUE
        // CF: AlternatingMonoPattern.IsRepetitionOf | other.MonoStreaks[0].HitType == MonoStreaks[0].HitType | FALSE
        [Test]
        public void TestDifferentFirstTypeIsNotRepetition()
            => Assert.That(pattern((2, HitType.Centre)).IsRepetitionOf(pattern((2, HitType.Rim))), Is.False);

        // CF: AlternatingMonoPattern.IsRepetitionOf | other.MonoStreaks[0].HitType == MonoStreaks[0].HitType | TRUE
        [Test]
        public void TestSameShapeIsRepetition()
            => Assert.That(pattern((2, HitType.Centre), (1, HitType.Rim)).IsRepetitionOf(pattern((2, HitType.Centre), (1, HitType.Rim))), Is.True);
    }
}
