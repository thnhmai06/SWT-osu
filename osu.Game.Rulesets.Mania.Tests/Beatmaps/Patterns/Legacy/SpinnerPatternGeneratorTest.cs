// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Rulesets.Mania.Beatmaps.Patterns;
using osu.Game.Rulesets.Mania.Beatmaps.Patterns.Legacy;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Utils;

namespace osu.Game.Rulesets.Mania.Tests.Beatmaps.Patterns.Legacy
{
    [TestFixture]
    public class SpinnerPatternGeneratorTest
    {
        private static ManiaHitObject generate(double duration, int columns, Pattern? previous = null, int seed = 1, bool finish = false)
        {
            var spinner = new FakeDurationObject { StartTime = 1000, Duration = duration };
            spinner.Samples.Add(new HitSampleInfo(HitSampleInfo.HIT_NORMAL));
            if (finish)
                spinner.Samples.Add(new HitSampleInfo(HitSampleInfo.HIT_FINISH));

            var patterns = new SpinnerPatternGenerator(new LegacyRandom(seed), spinner, PatternTestHelper.LowDifficulty(), columns, previous ?? new Pattern()).Generate().ToArray();
            Assert.That(patterns, Has.Length.EqualTo(1));
            return patterns[0].HitObjects.Single();
        }

        // CF: SpinnerPatternGenerator.generate | endTime - StartTime >= 100 | FALSE
        // CF: SpinnerPatternGenerator.addToPattern | holdNote | FALSE
        // CF: SpinnerPatternGenerator..ctor | (HitObject as IHasDuration)?.EndTime ?? 0 | non-null
        [Test]
        public void TestShortSpinnerBecomesNote()
        {
            var o = generate(99, 4);
            Assert.That(o, Is.TypeOf<Note>());
            Assert.That(o.StartTime, Is.EqualTo(1000));
        }

        // CF: SpinnerPatternGenerator.generate | endTime - StartTime >= 100 | TRUE
        // CF: SpinnerPatternGenerator.addToPattern | holdNote | TRUE
        [Test]
        public void TestLongSpinnerBecomesHoldNoteWithNormalSamplesOnHead()
        {
            var hold = (HoldNote)generate(100, 4, finish: true);

            Assert.That(hold.EndTime, Is.EqualTo(1100));
            Assert.That(hold.NodeSamples[0].Select(s => s.Name), Is.EqualTo(new[] { HitSampleInfo.HIT_NORMAL }));
            Assert.That(hold.NodeSamples[1].Select(s => s.Name), Is.EquivalentTo(new[] { HitSampleInfo.HIT_NORMAL, HitSampleInfo.HIT_FINISH }));
        }

        // CF: SpinnerPatternGenerator.generate | switch(TotalColumns) | default
        // CF: SpinnerPatternGenerator.getRandomColumn | (convertType & ForceNotStack) > 0 | TRUE
        // CF: SpinnerPatternGenerator..ctor | PreviousPattern.ColumnWithObjects == TotalColumns | FALSE
        [Test]
        public void TestSpinnerAvoidsColumnsOfPreviousPattern()
        {
            var previous = PatternTestHelper.PatternOf(0, 1, 2);
            for (int seed = 0; seed < 20; seed++)
                Assert.That(generate(500, 4, previous, seed).Column, Is.EqualTo(3));
        }

        // CF: SpinnerPatternGenerator..ctor | PreviousPattern.ColumnWithObjects == TotalColumns | TRUE
        // CF: SpinnerPatternGenerator.getRandomColumn | (convertType & ForceNotStack) > 0 | FALSE
        [Test]
        public void TestSpinnerMayStackWhenPreviousPatternFillsAllColumns()
        {
            var previous = PatternTestHelper.PatternOf(0, 1, 2, 3);
            for (int seed = 0; seed < 20; seed++)
                Assert.That(generate(500, 4, previous, seed).Column, Is.InRange(0, 3));
        }

        // CF: SpinnerPatternGenerator.generate | switch(TotalColumns) | default (1 column)
        [Test]
        public void TestSingleColumn() => Assert.That(generate(500, 1).Column, Is.EqualTo(0));

        // CF: SpinnerPatternGenerator.generate | case 8 when HIT_FINISH && endTime - StartTime < 1000 | case 8 (special column)
        [Test]
        public void TestEightColumnsShortFinishSpinnerUsesSpecialColumn() => Assert.That(generate(999, 8, finish: true).Column, Is.EqualTo(0));

        // CF: SpinnerPatternGenerator.generate | case 8 when HIT_FINISH && endTime - StartTime < 1000 | FALSE (duration >= 1000)
        // CF: SpinnerPatternGenerator.generate | case 8 when HIT_FINISH ... | FALSE (no finish)
        // CF: SpinnerPatternGenerator.generate | case 8 | case 8
        [TestCase(1000, true)]
        [TestCase(500, false)]
        public void TestEightColumnsOtherwiseAvoidsSpecialColumn(double duration, bool finish)
        {
            for (int seed = 0; seed < 20; seed++)
                Assert.That(generate(duration, 8, seed: seed, finish: finish).Column, Is.InRange(1, 7));
        }
    }
}
