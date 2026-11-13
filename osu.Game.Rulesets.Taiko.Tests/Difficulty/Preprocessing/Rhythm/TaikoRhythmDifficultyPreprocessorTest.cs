// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Difficulty.Preprocessing.Rhythm
{
    [TestFixture]
    public class TaikoRhythmDifficultyPreprocessorTest
    {
        // CF: TaikoRhythmDifficultyPreprocessor.ProcessAndAssign | foreach rhythmGroup | loop 0
        // CF: TaikoRhythmDifficultyPreprocessor.ProcessAndAssign | foreach patternGroup | loop 0
        [Test]
        public void TestEmptyInputDoesNothing()
            => Assert.That(() => TaikoRhythmDifficultyPreprocessor.ProcessAndAssign(new List<TaikoDifficultyHitObject>()), Throws.Nothing);

        // CF: TaikoRhythmDifficultyPreprocessor.ProcessAndAssign | foreach rhythmGroup | loop n
        // CF: TaikoRhythmDifficultyPreprocessor.ProcessAndAssign | foreach hitObject in rhythmGroup.HitObjects | loop n
        // CF: TaikoRhythmDifficultyPreprocessor.ProcessAndAssign | foreach patternGroup | loop n
        // CF: TaikoRhythmDifficultyPreprocessor.ProcessAndAssign | foreach hitObject in patternGroup.AllHitObjects | loop n
        [Test]
        public void TestEveryObjectIsAssignedToItsGroups()
        {
            var objects = DifficultyObjectFactory.Create(Enumerable.Repeat(100d, 6).Concat(Enumerable.Repeat(250d, 6)).Select(d => (d, HitType.Centre)).ToArray());
            TaikoRhythmDifficultyPreprocessor.ProcessAndAssign(objects);

            foreach (var o in objects)
            {
                Assert.That(o.RhythmData.SameRhythmGroupedHitObjects, Is.Not.Null);
                Assert.That(o.RhythmData.SameRhythmGroupedHitObjects!.HitObjects, Does.Contain(o));
                Assert.That(o.RhythmData.SamePatternsGroupedHitObjects, Is.Not.Null);
                Assert.That(o.RhythmData.SamePatternsGroupedHitObjects!.AllHitObjects, Does.Contain(o));
            }

            Assert.That(objects.Select(o => o.RhythmData.SameRhythmGroupedHitObjects).Distinct().Count(), Is.GreaterThan(1));
        }

        // CF: TaikoRhythmDifficultyPreprocessor.ProcessAndAssign | foreach hitObject in rhythmGroup.HitObjects | loop 1
        [Test]
        public void TestSingleObjectFormsOwnGroup()
        {
            var objects = DifficultyObjectFactory.Create((100, HitType.Centre));
            TaikoRhythmDifficultyPreprocessor.ProcessAndAssign(objects);
            Assert.That(objects[0].RhythmData.SameRhythmGroupedHitObjects!.HitObjects, Is.EqualTo(objects));
        }
    }
}
