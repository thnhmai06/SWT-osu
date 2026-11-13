// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators.Speed;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Tests.Difficulty
{
    [TestFixture]
    public class RhythmAndReadingEvaluatorsTest
    {
        // `current.BaseObject is Spinner` TRUE -> 0 (first operand of the OR).
        [Test]
        public void TestRhythm_Spinner_IsZero()
        {
            var objs = DifficultyTestHelpers.DifficultyObjects(new OsuHitObject[]
            {
                DifficultyTestHelpers.Circle(0, 100, 100),
                DifficultyTestHelpers.Spinner(300, 500),
                DifficultyTestHelpers.Circle(1000, 200, 100),
            });

            Assert.That(RhythmEvaluator.EvaluateDifficultyOf(objs[0]), Is.Zero);
        }

        // Spinner guard FALSE: regular circle in a stream yields non-negative value.
        [Test]
        public void TestRhythm_RegularObject_NonNegative()
        {
            var objs = DifficultyTestHelpers.DifficultyObjects(DifficultyTestHelpers.Stream(20, 200, 60));

            Assert.That(RhythmEvaluator.EvaluateDifficultyOf(objs[10]), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void TestReading_Spinner_IsZero()
        {
            var objs = DifficultyTestHelpers.DifficultyObjects(new OsuHitObject[]
            {
                DifficultyTestHelpers.Circle(0, 100, 100),
                DifficultyTestHelpers.Spinner(300, 500),
                DifficultyTestHelpers.Circle(1000, 200, 100),
            });

            Assert.That(ReadingEvaluator.EvaluateDifficultyOf(objs[0], false), Is.Zero);
        }

        // Regular object: value is non-negative, and hidden does not lower it.
        [Test]
        public void TestReading_Hidden_DoesNotLowerDifficulty()
        {
            var objs = DifficultyTestHelpers.DifficultyObjects(DifficultyTestHelpers.Stream(20, 150, 60), ar: 10);

            double plain = ReadingEvaluator.EvaluateDifficultyOf(objs[10], false);
            double hidden = ReadingEvaluator.EvaluateDifficultyOf(objs[10], true);

            Assert.That(plain, Is.GreaterThanOrEqualTo(0));
            Assert.That(hidden, Is.GreaterThanOrEqualTo(plain));
        }
    }
}
