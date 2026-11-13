// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Evaluators;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Difficulty.Evaluators
{
    [TestFixture]
    public class RhythmEvaluatorTest
    {
        private static List<TaikoDifficultyHitObject> processed(params double[] deltas)
        {
            var o = DifficultyObjectFactory.Create(deltas.Select(d => (d, HitType.Centre)).ToArray());
            TaikoRhythmDifficultyPreprocessor.ProcessAndAssign(o);
            return o;
        }

        // CF: RhythmEvaluator.EvaluateDifficultyOf | hitObject.BaseObject is not Hit | TRUE
        [Test]
        public void TestNonHitHasNoDifficulty()
        {
            var roll = new DrumRoll { StartTime = 1000 };
            roll.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            var obj = new TaikoDifficultyHitObject(roll, roll, 1, new List<DifficultyHitObject>(), new(), new(), new(), 0, new ControlPointInfo(), 1);
            Assert.That(RhythmEvaluator.EvaluateDifficultyOf(obj), Is.EqualTo(0));
        }

        // CF: RhythmEvaluator.EvaluateDifficultyOf | hitObject.BaseObject is not Hit | FALSE
        // CF: RhythmEvaluator.EvaluateDifficultyOf | rhythmData.SameRhythmGroupedHitObjects?.FirstHitObject == hitObject | FALSE
        // CF: RhythmEvaluator.EvaluateDifficultyOf | rhythmData.SamePatternsGroupedHitObjects?.FirstHitObject == hitObject | FALSE
        [Test]
        public void TestUnprocessedHitHasNoDifficulty()
            => Assert.That(RhythmEvaluator.EvaluateDifficultyOf(DifficultyObjectFactory.Create((100, HitType.Centre))[0]), Is.EqualTo(0));

        // CF: RhythmEvaluator.EvaluateDifficultyOf | rhythmData.SameRhythmGroupedHitObjects?.FirstHitObject == hitObject | FALSE
        [Test]
        public void TestObjectInsideGroupHasNoDifficulty()
        {
            var o = processed(Enumerable.Repeat(100d, 10).ToArray());
            var group = o[3].RhythmData.SameRhythmGroupedHitObjects!;
            var inner = group.HitObjects.Skip(1).First(x => x.RhythmData.SamePatternsGroupedHitObjects!.FirstHitObject != x);

            Assert.That(RhythmEvaluator.EvaluateDifficultyOf(inner), Is.EqualTo(0));
        }

        // CF: RhythmEvaluator.EvaluateDifficultyOf | rhythmData.SameRhythmGroupedHitObjects?.FirstHitObject == hitObject | TRUE
        // CF: RhythmEvaluator.EvaluateDifficultyOf | rhythmData.SamePatternsGroupedHitObjects?.FirstHitObject == hitObject | TRUE
        // CF: RhythmEvaluator.longGapPenalty | previous == null | TRUE
        // CF: RhythmEvaluator.longGapPenalty | previous == null | FALSE
        [Test]
        public void TestRhythmChangesAreDifficultAndEverythingIsFinite()
        {
            var o = processed(Enumerable.Repeat(100d, 8).Concat(Enumerable.Repeat(250d, 8)).Concat(Enumerable.Repeat(100d, 8)).ToArray());
            var values = o.Select(RhythmEvaluator.EvaluateDifficultyOf).ToList();

            Assert.That(values, Has.All.GreaterThanOrEqualTo(0).And.All.Matches<double>(double.IsFinite));
            Assert.That(values.Max(), Is.GreaterThan(0));
            // the very first group has no previous group (no gap penalty) and later group starts do.
            Assert.That(o[0].RhythmData.SameRhythmGroupedHitObjects!.Previous, Is.Null);
            Assert.That(o.Any(x => x.RhythmData.SameRhythmGroupedHitObjects!.Previous != null), Is.True);
        }
    }
}
