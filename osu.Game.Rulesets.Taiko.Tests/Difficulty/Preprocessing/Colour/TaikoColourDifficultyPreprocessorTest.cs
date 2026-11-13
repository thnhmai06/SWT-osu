// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Colour;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Difficulty.Preprocessing.Colour
{
    [TestFixture]
    public class TaikoColourDifficultyPreprocessorTest
    {
        private static List<TaikoDifficultyHitObject> process(string colours)
        {
            var objects = DifficultyObjectFactory.Create(colours.Select(c => (100d, c == 'C' ? HitType.Centre : HitType.Rim)).ToArray());
            TaikoColourDifficultyPreprocessor.ProcessAndAssign(objects.Cast<DifficultyHitObject>().ToList());
            return objects;
        }

        // CF: TaikoColourDifficultyPreprocessor.encodeMonoStreak | for i < data.Count | loop 0
        // CF: TaikoColourDifficultyPreprocessor.encodeAlternatingMonoPattern | for i < data.Count | loop 0
        // CF: TaikoColourDifficultyPreprocessor.encodeRepeatingHitPattern | for i < data.Count | loop 0
        // CF: TaikoColourDifficultyPreprocessor.ProcessAndAssign | foreach repeatingHitPattern | loop 0
        [Test]
        public void TestEmptyInput() => Assert.That(process(""), Is.Empty);

        // CF: TaikoColourDifficultyPreprocessor.encodeMonoStreak | currentMonoStreak == null | TRUE
        // CF: TaikoColourDifficultyPreprocessor.encodeMonoStreak | previousObject == null | TRUE
        // CF: TaikoColourDifficultyPreprocessor.encodeMonoStreak | colour changed | FALSE
        // CF: TaikoColourDifficultyPreprocessor.encodeMonoStreak | colour changed | TRUE
        // CF: TaikoColourDifficultyPreprocessor.encodeAlternatingMonoPattern | currentMonoPattern == null | TRUE
        // CF: TaikoColourDifficultyPreprocessor.encodeAlternatingMonoPattern | RunLength != previous RunLength | TRUE
        // CF: TaikoColourDifficultyPreprocessor.encodeAlternatingMonoPattern | RunLength != previous RunLength | FALSE
        // CF: TaikoColourDifficultyPreprocessor.encodeRepeatingHitPattern | i < data.Count - 2 | FALSE
        // CF: TaikoColourDifficultyPreprocessor.encodeRepeatingHitPattern | isCoupled | FALSE
        // CF: TaikoColourDifficultyPreprocessor.ProcessAndAssign | foreach repeatingHitPattern | loop n
        // CF: TaikoColourDifficultyPreprocessor.ProcessAndAssign | foreach monoStreak.HitObjects | loop n
        [Test]
        public void TestStreaksAndIndexing()
        {
            // streaks: CC | RRR | C  -> three differently-sized streaks, hence three patterns
            var o = process("CCRRRC");

            Assert.That(o[0].ColourData.MonoStreak!.HitObjects, Is.EqualTo(new[] { o[0], o[1] }));
            Assert.That(o[2].ColourData.MonoStreak!.HitObjects, Is.EqualTo(new[] { o[2], o[3], o[4] }));
            Assert.That(o[5].ColourData.MonoStreak!.HitObjects, Is.EqualTo(new[] { o[5] }));

            Assert.That(o[2].ColourData.AlternatingMonoPattern!.Index, Is.EqualTo(0));
            Assert.That(o[2].ColourData.MonoStreak!.Index, Is.EqualTo(0));
            Assert.That(o[2].ColourData.MonoStreak!.Parent, Is.SameAs(o[2].ColourData.AlternatingMonoPattern));
            Assert.That(o[2].ColourData.AlternatingMonoPattern!.Parent, Is.SameAs(o[2].ColourData.RepeatingHitPattern));
            Assert.That(o[0].ColourData.RepeatingHitPattern, Is.Not.SameAs(o[5].ColourData.RepeatingHitPattern));
        }

        // CF: TaikoColourDifficultyPreprocessor.encodeAlternatingMonoPattern | RunLength != previous RunLength | FALSE
        // CF: TaikoColourDifficultyPreprocessor.ProcessAndAssign | for j < monoPattern.MonoStreaks.Count | loop n
        [Test]
        public void TestEqualLengthStreaksShareAPattern()
        {
            // CC RR CC -> three streaks of equal length share one alternating pattern
            var o = process("CCRRCC");

            var pattern = o[0].ColourData.AlternatingMonoPattern!;
            Assert.That(pattern.MonoStreaks, Has.Count.EqualTo(3));
            Assert.That(o[2].ColourData.MonoStreak!.Index, Is.EqualTo(1));
            Assert.That(o[4].ColourData.MonoStreak!.Index, Is.EqualTo(2));
            Assert.That(o[4].ColourData.AlternatingMonoPattern, Is.SameAs(pattern));
        }

        // CF: TaikoColourDifficultyPreprocessor.encodeRepeatingHitPattern | i < data.Count - 2 | TRUE
        // CF: TaikoColourDifficultyPreprocessor.encodeRepeatingHitPattern | isCoupled | TRUE
        // CF: TaikoColourDifficultyPreprocessor.encodeRepeatingHitPattern | while (isCoupled) | loop n
        [Test]
        public void TestRepeatingPatternsAreCoupled()
        {
            // streak lengths 2,1,2,1,2,1: each pattern repeats the one two places on, so all six are coupled into one hit pattern.
            var o = process("CCRCCRCCR");

            Assert.That(o[0].ColourData.RepeatingHitPattern!.AlternatingMonoPatterns, Has.Count.EqualTo(6));
            Assert.That(o[8].ColourData.RepeatingHitPattern, Is.SameAs(o[0].ColourData.RepeatingHitPattern));
        }

        // CF: TaikoColourDifficultyPreprocessor.encodeRepeatingHitPattern | while (isCoupled) | loop 1
        [Test]
        public void TestCouplingStopsWhenRepetitionEnds()
        {
            // streak lengths 2,1,2,3: first three couple, the last stands alone.
            var o = process("CCRCCRRR");

            Assert.That(o[0].ColourData.RepeatingHitPattern!.AlternatingMonoPatterns, Has.Count.EqualTo(3));
            Assert.That(o[7].ColourData.RepeatingHitPattern!.AlternatingMonoPatterns, Has.Count.EqualTo(1));
            Assert.That(o[7].ColourData.RepeatingHitPattern, Is.Not.SameAs(o[0].ColourData.RepeatingHitPattern));
        }
    }
}
