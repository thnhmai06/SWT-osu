// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Mania.Difficulty.Evaluators;
using osu.Game.Rulesets.Mania.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mania.Objects;

namespace osu.Game.Rulesets.Mania.Tests.Difficulty.Evaluators
{
    [TestFixture]
    public class StrainEvaluatorTest
    {
        private static ManiaDifficultyHitObject last(params ManiaHitObject[] objects)
        {
            var list = DifficultyObjects.Create(4, objects);
            return list[^1];
        }

        // CF: IndividualStrainEvaluator.EvaluateDifficultyOf | foreach previous | loop n
        // CF: IndividualStrainEvaluator.EvaluateDifficultyOf | maniaPrevious is null | TRUE
        // CF: IndividualStrainEvaluator.EvaluateDifficultyOf | maniaPrevious is null | FALSE
        // CF: IndividualStrainEvaluator.EvaluateDifficultyOf | DefinitelyBigger(prev.EndTime, endTime, 1) | FALSE
        [Test]
        public void TestIndividualPlainNotes()
        {
            var o = last(new Note { StartTime = 0, Column = 0 }, new Note { StartTime = 100, Column = 1 });
            Assert.That(IndividualStrainEvaluator.EvaluateDifficultyOf(o), Is.EqualTo(2.0));
        }

        // CF: IndividualStrainEvaluator.EvaluateDifficultyOf | DefinitelyBigger(prev.EndTime, endTime, 1) | TRUE
        // CF: IndividualStrainEvaluator.EvaluateDifficultyOf | DefinitelyBigger(startTime, prev.StartTime, 1) | TRUE
        [Test]
        public void TestIndividualNoteInsideHold()
        {
            var o = last(new HoldNote { StartTime = 0, Duration = 1000, Column = 0 }, new Note { StartTime = 100, Column = 1 });
            Assert.That(IndividualStrainEvaluator.EvaluateDifficultyOf(o), Is.EqualTo(2.5));
        }

        // CF: IndividualStrainEvaluator.EvaluateDifficultyOf | DefinitelyBigger(startTime, prev.StartTime, 1) | FALSE
        [Test]
        public void TestIndividualSimultaneousStartDoesNotCountAsHeld()
        {
            var o = last(new HoldNote { StartTime = 100, Duration = 1000, Column = 0 }, new Note { StartTime = 100, Column = 1 });
            Assert.That(IndividualStrainEvaluator.EvaluateDifficultyOf(o), Is.EqualTo(2.0));
        }

        // CF: OverallStrainEvaluator.EvaluateDifficultyOf | foreach previous | loop n
        // CF: OverallStrainEvaluator.EvaluateDifficultyOf | maniaPrevious is null | TRUE
        // CF: OverallStrainEvaluator.EvaluateDifficultyOf | maniaPrevious is null | FALSE
        // CF: OverallStrainEvaluator.EvaluateDifficultyOf | isOverlapping | FALSE
        // CF: OverallStrainEvaluator.EvaluateDifficultyOf | DefinitelyBigger(prev.EndTime, startTime, 1) | FALSE
        [Test]
        public void TestOverallPlainNotes()
        {
            var o = last(new Note { StartTime = 0, Column = 0 }, new Note { StartTime = 100, Column = 1 });
            Assert.That(OverallStrainEvaluator.EvaluateDifficultyOf(o), Is.EqualTo(1.0));
        }

        // CF: OverallStrainEvaluator.EvaluateDifficultyOf | DefinitelyBigger(prev.EndTime, endTime, 1) && DefinitelyBigger(startTime, prev.StartTime, 1) | TRUE
        // CF: OverallStrainEvaluator.EvaluateDifficultyOf | DefinitelyBigger(endTime, prev.EndTime, 1) | FALSE
        [Test]
        public void TestOverallNoteInsideHold()
        {
            var o = last(new HoldNote { StartTime = 0, Duration = 1000, Column = 0 }, new Note { StartTime = 100, Column = 1 });
            Assert.That(OverallStrainEvaluator.EvaluateDifficultyOf(o), Is.EqualTo(1.25));
        }

        // CF: OverallStrainEvaluator.EvaluateDifficultyOf | prev.EndTime > startTime && endTime > prev.EndTime && startTime > prev.StartTime | TRUE
        // CF: OverallStrainEvaluator.EvaluateDifficultyOf | isOverlapping | TRUE
        [Test]
        public void TestOverallOverlappingReleasesAtThreshold()
        {
            // previous ends at 500, current is 200..530: release gap is 30ms, the logistic midpoint, which gives half of the maximum addition.
            var o = last(new HoldNote { StartTime = 0, Duration = 500, Column = 0 }, new HoldNote { StartTime = 200, Duration = 330, Column = 1 });
            Assert.That(OverallStrainEvaluator.EvaluateDifficultyOf(o), Is.EqualTo(1.5).Within(1e-9));
        }

        // CF: OverallStrainEvaluator.EvaluateDifficultyOf | DefinitelyBigger(startTime, prev.StartTime, 1) (overlap) | FALSE
        [Test]
        public void TestOverallSimultaneousStartIsNotOverlap()
        {
            var o = last(new HoldNote { StartTime = 100, Duration = 400, Column = 0 }, new HoldNote { StartTime = 100, Duration = 1000, Column = 1 });
            Assert.That(OverallStrainEvaluator.EvaluateDifficultyOf(o), Is.EqualTo(1.0));
        }
    }
}
