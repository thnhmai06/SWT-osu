// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Evaluators;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Colour.Data;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Difficulty.Evaluators
{
    [TestFixture]
    public class StaminaEvaluatorTest
    {
        private static List<TaikoDifficultyHitObject> centres(params double[] deltas)
        {
            var notes = new (double, HitType)[deltas.Length];
            for (int i = 0; i < deltas.Length; i++)
                notes[i] = (deltas[i], HitType.Centre);
            return DifficultyObjectFactory.Create(notes);
        }

        private static void streak(params TaikoDifficultyHitObject[] members)
        {
            var s = new MonoStreak();
            s.HitObjects.AddRange(members);
            foreach (var m in members)
                m.ColourData.MonoStreak = s;
        }

        // CF: StaminaEvaluator.EvaluateDifficultyOf | current.BaseObject is not Hit | TRUE
        [Test]
        public void TestNonHitHasNoDifficulty()
        {
            var roll = new DrumRoll { StartTime = 1000 };
            roll.ApplyDefaults(new ControlPointInfo(), new osu.Game.Beatmaps.BeatmapDifficulty());
            var obj = new TaikoDifficultyHitObject(roll, roll, 1, new List<DifficultyHitObject>(), new(), new(), new(), 0, new ControlPointInfo(), 1);
            Assert.That(StaminaEvaluator.EvaluateDifficultyOf(obj), Is.EqualTo(0));
        }

        // CF: StaminaEvaluator.EvaluateDifficultyOf | current.BaseObject is not Hit | FALSE
        // CF: StaminaEvaluator.EvaluateDifficultyOf | taikoPrevious == null | TRUE
        [Test]
        public void TestObjectWithoutPreviousGetsBaseStrain()
            => Assert.That(StaminaEvaluator.EvaluateDifficultyOf(centres(100, 100)[1]), Is.EqualTo(0.5));

        // CF: StaminaEvaluator.EvaluateDifficultyOf | taikoPrevious == null | FALSE
        // CF: StaminaEvaluator.EvaluateDifficultyOf | previousMono != null | FALSE
        // CF: StaminaEvaluator.availableFingersFor | previousColourChange != null | FALSE
        // CF: StaminaEvaluator.availableFingersFor | nextColourChange != null | FALSE
        [Test]
        public void TestNoPreviousMonoKeepsBaseStrain()
            => Assert.That(StaminaEvaluator.EvaluateDifficultyOf(centres(100, 100, 100, 100)[3]), Is.EqualTo(0.5));

        // CF: StaminaEvaluator.EvaluateDifficultyOf | previousMono != null | TRUE
        // CF: StaminaEvaluator.availableFingersFor | previousColourChange != null | TRUE
        // CF: StaminaEvaluator.availableFingersFor | hitObject.StartTime - previousColourChange.StartTime < 300 | TRUE
        // CF: StaminaEvaluator.speedBonus | Math.Max(interval, 1) | interval >= 1
        [Test]
        public void TestRecentColourChangeUsesTwoFingers()
        {
            var o = centres(100, 100, 100, 100, 100);
            streak(o[3], o[4]); // previous colour change is o[2], 200ms before o[4]
            // 0.5 + 20/200 + 0.5 * 20/200
            Assert.That(StaminaEvaluator.EvaluateDifficultyOf(o[4]), Is.EqualTo(0.65).Within(1e-9));
        }

        // CF: StaminaEvaluator.availableFingersFor | hitObject.StartTime - previousColourChange.StartTime < 300 | FALSE
        // CF: StaminaEvaluator.availableFingersFor | nextColourChange != null | TRUE
        // CF: StaminaEvaluator.availableFingersFor | nextColourChange.StartTime - hitObject.StartTime < 300 | TRUE
        [Test]
        public void TestUpcomingColourChangeUsesTwoFingers()
        {
            var o = centres(400, 400, 400, 100);
            streak(o[1], o[2]); // previous change o[0] is 800ms before o[2]; next change o[3] is 100ms after
            // 0.5 + 20/800 + 0.5 * 20/800
            Assert.That(StaminaEvaluator.EvaluateDifficultyOf(o[2]), Is.EqualTo(0.5375).Within(1e-9));
        }

        // CF: StaminaEvaluator.availableFingersFor | nextColourChange.StartTime - hitObject.StartTime < 300 | FALSE
        [Test]
        public void TestDistantColourChangesUseEightFingers()
        {
            var o = centres(400, 400, 400, 400);
            streak(o[1], o[2]);
            Assert.That(StaminaEvaluator.EvaluateDifficultyOf(o[2]), Is.EqualTo(0.5)); // 8 fingers: no mono 7 notes back
        }

        // CF: StaminaEvaluator.availableFingersFor | return 8 | default
        [Test]
        public void TestEightFingersReachFarBack()
        {
            var o = centres(100, 100, 100, 100, 100, 100, 100, 100, 100, 100);
            // previous mono 7 back is 800ms before o[9]; previous (2 back) 200ms
            Assert.That(StaminaEvaluator.EvaluateDifficultyOf(o[9]), Is.EqualTo(0.5 + 20.0 / 800 + 0.5 * 20.0 / 200).Within(1e-9));
        }

        // CF: StaminaEvaluator.speedBonus | Math.Max(interval, 1) | interval < 1
        [Test]
        public void TestZeroIntervalIsCapped()
        {
            var o = centres(100, 0, 0);
            streak(o[1], o[2]);
            // interval 0 is capped to 1 for both terms: 0.5 + 20/1 + 0.5 * 20/1
            Assert.That(StaminaEvaluator.EvaluateDifficultyOf(o[2]), Is.EqualTo(30.5).Within(1e-9));
        }
    }
}
