// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators.Speed;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using static osu.Game.Rulesets.Osu.Tests.Difficulty.DifficultyTestHelper;

namespace osu.Game.Rulesets.Osu.Tests.Difficulty.Evaluators
{
    [TestFixture]
    public class CognitionEvaluatorsTest
    {
        private static OsuHitObject[] withDeltas(params double[] deltas)
        {
            double time = 1000;
            var list = new System.Collections.Generic.List<OsuHitObject> { Circle(time, 100) };

            for (int i = 0; i < deltas.Length; i++)
            {
                time += deltas[i];
                list.Add(Circle(time, 100 + 80 * (i + 1)));
            }

            return list.ToArray();
        }

        // CF: RhythmEvaluator.EvaluateDifficultyOf | current.BaseObject is Spinner | TRUE
        [Test]
        public void TestRhythmSpinner()
        {
            var o = Build(new[] { Circle(0, 100), Spinner(200) });
            Assert.That(RhythmEvaluator.EvaluateDifficultyOf(o[0]), Is.EqualTo(0));
        }

        // CF: RhythmEvaluator.EvaluateDifficultyOf | current.BaseObject is Spinner | FALSE
        // CF: RhythmEvaluator.EvaluateDifficultyOf | rhythmStart < historicalNoteCount - 2 | FALSE
        [Test]
        public void TestRhythmWithoutHistoryIsNeutral()
        {
            var o = Build(withDeltas(150, 150));
            Assert.That(RhythmEvaluator.EvaluateDifficultyOf(o[0]), Is.EqualTo(1).Within(1e-9));
        }

        // CF: RhythmEvaluator.EvaluateDifficultyOf | rhythmStart < historicalNoteCount - 2 | TRUE
        // CF: RhythmEvaluator.EvaluateDifficultyOf | deltaDifference < deltaDifferenceEpsilon | TRUE
        [Test]
        public void TestSteadyRhythmIsNeutral()
        {
            var o = Build(withDeltas(150, 150, 150, 150, 150, 150, 150));
            Assert.That(RhythmEvaluator.EvaluateDifficultyOf(o[^1]), Is.EqualTo(1).Within(1e-9));
        }

        // CF: RhythmEvaluator.EvaluateDifficultyOf | deltaDifference < deltaDifferenceEpsilon | FALSE
        [Test]
        public void TestIrregularRhythmIsHarderThanSteady()
        {
            var steady = Build(withDeltas(150, 150, 150, 150, 150, 150, 150));
            var irregular = Build(withDeltas(150, 150, 100, 200, 150, 100, 200));

            Assert.That(RhythmEvaluator.EvaluateDifficultyOf(irregular[^1]), Is.GreaterThan(RhythmEvaluator.EvaluateDifficultyOf(steady[^1])));
        }

        // CF: RhythmEvaluator.EvaluateDifficultyOf | prevObj.BaseObject is Slider | TRUE
        // CF: RhythmEvaluator.EvaluateDifficultyOf | currObj.BaseObject is Spinner | TRUE
        [Test]
        public void TestRhythmWithSlidersAndSpinnersInHistory()
        {
            var o = Build(new[]
            {
                Circle(0, 100), Circle(200, 200), Slider(400, 300, 192, 50, 0), Circle(700, 100, 100), Spinner(1000, 300), Circle(1700, 200), Circle(1900, 300), Circle(2050, 400), Circle(2300, 450),
            });

            foreach (var d in o)
                Assert.That(RhythmEvaluator.EvaluateDifficultyOf(d), Is.GreaterThanOrEqualTo(1).Or.EqualTo(0));
        }

        // CF: ReadingEvaluator.EvaluateDifficultyOf | current.BaseObject is Spinner | TRUE
        [Test]
        public void TestReadingSpinner()
        {
            var o = Build(new[] { Circle(0, 100), Circle(200, 200), Spinner(400) });
            Assert.That(ReadingEvaluator.EvaluateDifficultyOf(o[1], false), Is.EqualTo(0));
        }

        // CF: ReadingEvaluator.EvaluateDifficultyOf | current.Index == 0 | TRUE
        [Test]
        public void TestReadingFirstObject()
        {
            Assert.That(ReadingEvaluator.EvaluateDifficultyOf(Build(Line(4, 150, 80))[0], false), Is.EqualTo(0));
        }

        // CF: ReadingEvaluator.EvaluateDifficultyOf | current.Index == 0 | FALSE
        [Test]
        public void TestReadingOfDensePatternIsPositive()
        {
            var o = Build(ZigZag(20, 120, 100));
            Assert.That(ReadingEvaluator.EvaluateDifficultyOf(o[^1], false), Is.GreaterThan(0));
        }

        [Test]
        public void TestHiddenDoesNotReduceReadingDifficulty()
        {
            var o = Build(ZigZag(20, 120, 100));
            Assert.That(ReadingEvaluator.EvaluateDifficultyOf(o[^1], true), Is.GreaterThanOrEqualTo(ReadingEvaluator.EvaluateDifficultyOf(o[^1], false)));
        }

        // CF: FlashlightEvaluator.EvaluateDifficultyOf | current.BaseObject is Spinner | TRUE
        [Test]
        public void TestFlashlightSpinner()
        {
            var o = Build(new[] { Circle(0, 100), Spinner(200) });
            Assert.That(FlashlightEvaluator.EvaluateDifficultyOf(o[0], new Mod[] { new OsuModFlashlight() }), Is.EqualTo(0));
        }

        // CF: FlashlightEvaluator.EvaluateDifficultyOf | current.BaseObject is Spinner | FALSE
        // CF: FlashlightEvaluator.EvaluateDifficultyOf | for (i < Math.Min(current.Index, 10)) | loop n
        [Test]
        public void TestFlashlightScalesWithDistanceTravelled()
        {
            var near = Build(Line(8, 150, 40));
            var far = Build(Line(8, 150, 120));
            var mods = new Mod[] { new OsuModFlashlight() };

            Assert.That(FlashlightEvaluator.EvaluateDifficultyOf(far[^1], mods), Is.GreaterThan(FlashlightEvaluator.EvaluateDifficultyOf(near[^1], mods)));
        }

        // CF: FlashlightEvaluator.EvaluateDifficultyOf | for (i < Math.Min(current.Index, 10)) | loop 0
        [Test]
        public void TestFlashlightFirstObject()
        {
            var o = Build(Line(3, 150, 80));
            Assert.That(FlashlightEvaluator.EvaluateDifficultyOf(o[0], new Mod[] { new OsuModFlashlight() }), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void TestFlashlightHiddenAddsDifficulty()
        {
            var o = Build(Line(8, 150, 80));
            double plain = FlashlightEvaluator.EvaluateDifficultyOf(o[^1], new Mod[] { new OsuModFlashlight() });
            double hidden = FlashlightEvaluator.EvaluateDifficultyOf(o[^1], new Mod[] { new OsuModFlashlight(), new OsuModHidden() });

            Assert.That(hidden, Is.GreaterThan(plain));
        }

        [Test]
        public void TestFlashlightWithSliderInHistory()
        {
            var o = Build(new[] { Circle(0, 100), Slider(200, 200, 192, 200, 0), Circle(1200, 300), Circle(1400, 350), Circle(1600, 400) });
            Assert.That(o.Select(d => FlashlightEvaluator.EvaluateDifficultyOf(d, new Mod[] { new OsuModFlashlight() })), Has.All.GreaterThanOrEqualTo(0));
        }
    }
}
