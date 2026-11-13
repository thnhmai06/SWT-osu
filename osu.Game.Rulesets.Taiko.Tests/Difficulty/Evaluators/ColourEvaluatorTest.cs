// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Taiko.Difficulty.Evaluators;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Colour.Data;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Difficulty.Evaluators
{
    [TestFixture]
    public class ColourEvaluatorTest
    {
        private static double logistic(double exponent) => 1 / (1 + Math.Exp(exponent));

        // difficulty of the pieces for streak/pattern index 0 and repetition interval 1
        private static readonly double repeating = 2 * (1 - logistic(Math.E - 2 * Math.E));
        private static readonly double alternating = logistic(-2 * Math.E) * repeating;
        private static readonly double monoStreak = logistic(-2 * Math.E) * alternating * 0.5;

        private static List<TaikoDifficultyHitObject> uniform(int count)
            => DifficultyObjectFactory.Create(Enumerable.Repeat((100d, HitType.Centre), count).ToArray());

        /// <summary>
        /// Builds a streak -> pattern -> repeating pattern chain (repetition interval 1) whose first object is <paramref name="first"/>.
        /// </summary>
        private static (MonoStreak, AlternatingMonoPattern, RepeatingHitPatterns) chain(TaikoDifficultyHitObject first)
        {
            var streak = new MonoStreak();
            streak.HitObjects.Add(first);

            var alt = new AlternatingMonoPattern();
            alt.MonoStreaks.Add(streak);
            streak.Parent = alt;

            var previous = new RepeatingHitPatterns(null);
            previous.AlternatingMonoPatterns.Add(DifficultyObjectFactory.Pattern((1, HitType.Centre)));

            var rep = new RepeatingHitPatterns(previous);
            rep.AlternatingMonoPatterns.Add(alt);
            alt.Parent = rep;
            rep.FindRepetitionInterval();

            return (streak, alt, rep);
        }

        // CF: ColourEvaluator.EvaluateDifficultyOf | colourData.MonoStreak?.FirstHitObject == hitObject | FALSE
        // CF: ColourEvaluator.EvaluateDifficultyOf | colourData.AlternatingMonoPattern?.FirstHitObject == hitObject | FALSE
        // CF: ColourEvaluator.EvaluateDifficultyOf | colourData.RepeatingHitPattern?.FirstHitObject == hitObject | FALSE
        [Test]
        public void TestObjectWithoutColourDataHasNoDifficulty()
            => Assert.That(ColourEvaluator.EvaluateDifficultyOf(uniform(5)[4]), Is.EqualTo(0));

        // CF: ColourEvaluator.EvaluateDifficultyOf | colourData.RepeatingHitPattern?.FirstHitObject == hitObject | TRUE
        // CF: ColourEvaluator.consistentRatioPenalty | current.Index <= 1 | TRUE
        [Test]
        public void TestRepeatingPatternOnly()
        {
            var o = uniform(5);
            var (_, _, rep) = chain(o[1]);
            o[1].ColourData.RepeatingHitPattern = rep;

            Assert.That(rep.RepetitionInterval, Is.EqualTo(1));
            Assert.That(ColourEvaluator.EvaluateDifficultyOf(o[1]), Is.EqualTo(repeating).Within(1e-9));
        }

        // CF: ColourEvaluator.EvaluateDifficultyOf | colourData.AlternatingMonoPattern?.FirstHitObject == hitObject | TRUE
        [Test]
        public void TestAlternatingPatternOnly()
        {
            var o = uniform(5);
            var (_, alt, _) = chain(o[1]);
            o[1].ColourData.AlternatingMonoPattern = alt;

            Assert.That(ColourEvaluator.EvaluateDifficultyOf(o[1]), Is.EqualTo(alternating).Within(1e-9));
        }

        // CF: ColourEvaluator.EvaluateDifficultyOf | colourData.MonoStreak?.FirstHitObject == hitObject | TRUE
        [Test]
        public void TestMonoStreakOnly()
        {
            var o = uniform(5);
            var (streak, _, _) = chain(o[1]);
            o[1].ColourData.MonoStreak = streak;

            Assert.That(ColourEvaluator.EvaluateDifficultyOf(o[1]), Is.EqualTo(monoStreak).Within(1e-9));
        }

        // CF: ColourEvaluator.consistentRatioPenalty | Math.Abs(1 - currentRatio / previousRatio) <= threshold | TRUE
        // CF: ColourEvaluator.consistentRatioPenalty | consistentRatioCount > 0 | TRUE
        [Test]
        public void TestConsistentRhythmIsPenalised()
        {
            var o = uniform(5);
            var (streak, alt, rep) = chain(o[2]);
            o[2].ColourData.MonoStreak = streak;
            o[2].ColourData.AlternatingMonoPattern = alt;
            o[2].ColourData.RepeatingHitPattern = rep;

            // consistent ratios (1 and 1): penalty is 1 - 1 / 2 * 0.8 = 0.6
            Assert.That(ColourEvaluator.EvaluateDifficultyOf(o[2]), Is.EqualTo((monoStreak + alternating + repeating) * 0.6).Within(1e-9));
        }

        // CF: ColourEvaluator.consistentRatioPenalty | Math.Abs(1 - currentRatio / previousRatio) <= threshold | FALSE
        // CF: ColourEvaluator.consistentRatioPenalty | consistentRatioCount > 0 | FALSE
        // CF: ColourEvaluator.consistentRatioPenalty | recentRatios.Count <= 1 | TRUE
        [Test]
        public void TestInconsistentRatioWithTooLittleHistoryIsNotPenalised()
        {
            var o = DifficultyObjectFactory.Create((100, HitType.Centre), (100, HitType.Centre), (200, HitType.Centre));
            var (_, _, rep) = chain(o[2]);
            o[2].ColourData.RepeatingHitPattern = rep;

            Assert.That(ColourEvaluator.EvaluateDifficultyOf(o[2]), Is.EqualTo(repeating).Within(1e-9));
        }

        // infeasible: ColourEvaluator.consistentRatioPenalty `recentRatios.Count <= 1` FALSE and the maxRatioDeviation fallback: `previousHitObject` is never advanced inside the loop, so from the second iteration the object is compared with itself (always consistent) or the loop breaks at Index <= 1; at most one ratio is ever recorded without a consistent hit. Suspected production bug (previousHitObject should follow `current`).
    }
}
