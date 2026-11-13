// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Tests.Rulesets.Difficulty.Skills
{
    [TestFixture]
    public class StrainSkillsTest
    {
        private static List<DifficultyHitObject> create(params double[] times)
        {
            var list = new List<DifficultyHitObject>();
            HitObject last = new HitObject { StartTime = times[0] - 100, HitWindows = HitWindows.Empty };

            for (int i = 0; i < times.Length; i++)
            {
                var obj = new HitObject { StartTime = times[i], HitWindows = HitWindows.Empty };
                list.Add(new DifficultyHitObject(obj, last, 1, list, i));
                last = obj;
            }

            return list;
        }

        private class TestStrainSkill : StrainSkill
        {
            private readonly double[] strains;
            private readonly int sectionLength;

            public TestStrainSkill(double[] strains, int sectionLength = 400)
                : base(new Mod[0])
            {
                this.strains = strains;
                this.sectionLength = sectionLength;
            }

            protected override int SectionLength => sectionLength;

            protected override double StrainValueAt(DifficultyHitObject current) => strains[current.Index];

            protected override double CalculateInitialStrain(double time, DifficultyHitObject current) => 0;
        }

        private class TestHarmonicSkill : HarmonicSkill
        {
            private readonly double[] values;
            private readonly bool doubleTransform;

            public TestHarmonicSkill(double[] values, bool doubleTransform = false)
                : base(new Mod[0])
            {
                this.values = values;
                this.doubleTransform = doubleTransform;
            }

            protected override double ObjectDifficultyOf(DifficultyHitObject current) => values[current.Index];

            protected override List<double> GetTransformedDifficulties(List<double> difficulties)
                => doubleTransform ? difficulties.Select(d => d * 2).ToList() : difficulties;

            public double WeightSum => ObjectWeightSum;
        }

        private class TestVariableSkill : VariableLengthStrainSkill
        {
            private readonly double[] strains;

            public TestVariableSkill(double[] strains)
                : base(new Mod[0])
            {
                this.strains = strains;
            }

            protected override double StrainValueAt(DifficultyHitObject current) => strains[current.Index];

            protected override double CalculateInitialStrain(double time, DifficultyHitObject current) => 0;

            public override double DifficultyValue() => GetCurrentStrainPeaks().Sum(p => p.Value);
        }

        private static void process(Skill skill, List<DifficultyHitObject> objects)
        {
            foreach (var o in objects)
                skill.Process(o);
        }

        // CF: Skill.Process | ObjectDifficulties.Add | no throw
        [Test]
        public void TestObjectDifficultiesAreRecorded()
        {
            var skill = new TestStrainSkill(new[] { 1.0, 3, 2 });
            process(skill, create(100, 300, 700));

            Assert.That(skill.GetObjectDifficulties(), Is.EqualTo(new[] { 1.0, 3, 2 }));
        }

        // CF: StrainSkill.ProcessInternal | current.Index == 0 | TRUE
        // CF: StrainSkill.ProcessInternal | current.Index == 0 | FALSE
        // CF: StrainSkill.ProcessInternal | while current.StartTime > currentSectionEnd | loop 0
        // CF: StrainSkill.ProcessInternal | while current.StartTime > currentSectionEnd | loop 1
        // CF: StrainSkill.DifficultyValue | foreach strain | loop n
        [Test]
        public void TestStrainPeaksAndWeightedDifficulty()
        {
            var skill = new TestStrainSkill(new[] { 1.0, 3, 2 });
            process(skill, create(100, 300, 700));

            Assert.That(skill.GetCurrentStrainPeaks(), Is.EqualTo(new[] { 3.0, 2 }));
            Assert.That(skill.DifficultyValue(), Is.EqualTo(3 + 2 * 0.9).Within(1e-9));
        }

        // CF: StrainSkill.ProcessInternal | while current.StartTime > currentSectionEnd | loop n
        // CF: StrainSkill.DifficultyValue | peaks.Where(p => p > 0) | loop 0
        [Test]
        public void TestEmptySectionsDoNotContribute()
        {
            var skill = new TestStrainSkill(new[] { 1.0, 5 });
            process(skill, create(100, 1300));

            Assert.That(skill.GetCurrentStrainPeaks(), Is.EqualTo(new[] { 1.0, 0, 0, 5 }));
            Assert.That(skill.DifficultyValue(), Is.EqualTo(5 + 0.9).Within(1e-9));
            Assert.That(new TestStrainSkill(new double[0]).DifficultyValue(), Is.Zero);
        }

        [Test]
        public void TestSectionLengthIsConfigurable()
        {
            var skill = new TestStrainSkill(new[] { 1.0, 2 }, sectionLength: 100);
            process(skill, create(50, 250));

            // sections end at 100 and 200 (both before 250), so peaks: 1, 0, 0, 2 with the third section started at 200.
            Assert.That(skill.GetCurrentStrainPeaks().Count(), Is.EqualTo(3));
        }

        // CF: StrainSkill.CountTopWeightedStrains | ObjectDifficulties.Count == 0 | TRUE
        // CF: StrainSkill.CountTopWeightedStrains | consistentTopStrain == 0 | TRUE
        // CF: StrainSkill.CountTopWeightedStrains | consistentTopStrain == 0 | FALSE
        [Test]
        public void TestCountTopWeightedStrains()
        {
            Assert.That(new TestStrainSkill(new double[0]).CountTopWeightedStrains(5), Is.Zero);

            var skill = new TestStrainSkill(new[] { 4.0, 4 });
            process(skill, create(100, 200));

            Assert.That(skill.CountTopWeightedStrains(0), Is.EqualTo(2), "no difficulty => every object counts fully");

            var single = new TestStrainSkill(new[] { 7.0 });
            process(single, create(100));
            Assert.That(single.CountTopWeightedStrains(single.DifficultyValue()), Is.EqualTo(DiffUtils.Logistic(10, 0.88, 10, 1.1)).Within(1e-9));
        }

        // CF: HarmonicSkill.DifficultyValue | ObjectDifficulties.Count == 0 | TRUE
        [Test]
        public void TestHarmonicEmpty()
        {
            var skill = new TestHarmonicSkill(new double[0]);

            Assert.That(skill.DifficultyValue(), Is.Zero);
            Assert.That(skill.CountTopWeightedObjectDifficulties(10), Is.Zero);
        }

        // CF: HarmonicSkill.DifficultyValue | ObjectDifficulties.Count == 0 | FALSE
        // CF: HarmonicSkill.DifficultyValue | foreach difficulty (v > 0) | loop n
        [Test]
        public void TestHarmonicWeights()
        {
            var skill = new TestHarmonicSkill(new[] { 5.0, 10, 0 });
            process(skill, create(100, 200, 300));

            // sorted: 10, 5; weights: 1 and (1 + 1/2) / (1^0.9 + 1 + 1/2) = 0.6.
            Assert.That(skill.DifficultyValue(), Is.EqualTo(10 + 5 * 0.6).Within(1e-9));
            Assert.That(skill.WeightSum, Is.EqualTo(1.6).Within(1e-9), "zero-difficulty objects are excluded");
        }

        [Test]
        public void TestHarmonicTransformIsApplied()
        {
            var skill = new TestHarmonicSkill(new[] { 5.0 }, doubleTransform: true);
            process(skill, create(100));

            Assert.That(skill.DifficultyValue(), Is.EqualTo(10).Within(1e-9));
        }

        // CF: HarmonicSkill.CountTopWeightedObjectDifficulties | ObjectWeightSum == 0 | TRUE
        // CF: HarmonicSkill.CountTopWeightedObjectDifficulties | consistentTopObject == 0 | TRUE
        // CF: HarmonicSkill.CountTopWeightedObjectDifficulties | consistentTopObject == 0 | FALSE
        [Test]
        public void TestHarmonicCountTop()
        {
            var skill = new TestHarmonicSkill(new[] { 10.0 });
            process(skill, create(100));

            Assert.That(skill.CountTopWeightedObjectDifficulties(10), Is.Zero, "DifficultyValue() has not been computed yet, so there is no weight sum");

            double value = skill.DifficultyValue();
            Assert.That(skill.CountTopWeightedObjectDifficulties(0), Is.Zero);
            Assert.That(skill.CountTopWeightedObjectDifficulties(value), Is.EqualTo(DiffUtils.Logistic(1, 0.88, 10, 1.1)).Within(1e-9));
        }

        [Test]
        public void TestDifficultyToPerformance()
        {
            Assert.That(HarmonicSkill.DifficultyToPerformance(2), Is.EqualTo(32));
            Assert.That(HarmonicSkill.DifficultyToPerformance(0), Is.Zero);
        }

        // CF: VariableLengthStrainSkill.ProcessInternal | current.Index == 0 | TRUE
        // CF: VariableLengthStrainSkill.ProcessInternal | currentStrain > currentSectionPeak | TRUE
        // CF: VariableLengthStrainSkill.ProcessInternal | currentStrain > currentSectionPeak | FALSE
        // CF: VariableLengthStrainSkill.ProcessInternal | queuedStrains pruning | loop 0
        [Test]
        public void TestNewPeakEndsTheCurrentSection()
        {
            var skill = new TestVariableSkill(new[] { 1.0, 3, 2 });
            process(skill, create(0, 100, 200));

            var peaks = skill.GetCurrentStrainPeaks().ToArray();
            Assert.That(peaks.Select(p => (p.Value, p.SectionLength)), Is.EqualTo(new[] { (3.0, 400.0), (1.0, 100.0) }));
        }

        // CF: VariableLengthStrainSkill.backfillPeaks | while current.StartTime > currentSectionEnd | loop n
        // CF: VariableLengthStrainSkill.backfillPeaks | queuedStrains.Count > 0 | FALSE
        [Test]
        public void TestLargeGapBackfillsEmptySections()
        {
            var skill = new TestVariableSkill(new[] { 5.0, 1 });
            process(skill, create(0, 1000));

            Assert.That(skill.GetCurrentStrainPeaks().Select(p => p.Value), Is.EqualTo(new[] { 5.0, 1, 0, 0 }));
        }

        // CF: VariableLengthStrainSkill.backfillPeaks | queuedStrains.Count > 0 | TRUE
        // CF: VariableLengthStrainSkill.ProcessInternal | queuedStrains.Count > 0 && queuedStrains[^1] < currentStrain | FALSE
        [Test]
        public void TestQueuedStrainsInfluenceFollowingSections()
        {
            var skill = new TestVariableSkill(new[] { 5.0, 4, 3, 1 });
            process(skill, create(0, 100, 200, 1000));

            Assert.That(skill.GetCurrentStrainPeaks().Select(p => p.Value), Is.EqualTo(new[] { 5.0, 4, 3, 1, 0 }));
        }

        // CF: VariableLengthStrainSkill.ProcessInternal | queuedStrains.Count > 0 && queuedStrains[^1] < currentStrain | TRUE
        // CF: VariableLengthStrainSkill.ProcessInternal | queuedStrains pruning | loop 1
        [Test]
        public void TestSmallerQueuedStrainsArePruned()
        {
            var skill = new TestVariableSkill(new[] { 5.0, 2, 3, 0 });
            process(skill, create(0, 100, 200, 1000));

            var values = skill.GetCurrentStrainPeaks().Select(p => p.Value).ToArray();
            Assert.That(values, Does.Contain(3.0));
            Assert.That(values, Does.Not.Contain(2.0));
        }

        // CF: VariableLengthStrainSkill.saveCurrentPeak | finalPeak != null | TRUE
        // CF: VariableLengthStrainSkill.GetCurrentStrainPeaks | finalPeak == null | FALSE
        [Test]
        public void TestRequestingPeaksTwiceDoesNotDuplicateTheFinalPeak()
        {
            var skill = new TestVariableSkill(new[] { 1.0, 3 });
            process(skill, create(0, 100));

            Assert.That(skill.GetCurrentStrainPeaks().Count(), Is.EqualTo(skill.GetCurrentStrainPeaks().Count()));

        }

        // CF: VariableLengthStrainSkill.saveCurrentPeak | totalLength > maxStoredLength * MaxSectionLength | TRUE
        [Test]
        public void TestOldPeaksBeyondTheStoredLengthAreDropped()
        {
            double[] times = Enumerable.Range(0, 300).Select(i => i * 500.0).ToArray();
            var skill = new TestVariableSkill(times.Select(_ => 1.0).ToArray());
            process(skill, create(times));

            Assert.That(skill.GetCurrentStrainPeaks().Sum(p => p.SectionLength), Is.LessThan(44_000 + 1000));
        }

        // CF: VariableLengthStrainSkill.CountTopWeightedStrains | ObjectDifficulties.Count == 0 | TRUE
        // CF: VariableLengthStrainSkill.CountTopWeightedStrains | consistentTopStrain == 0 | TRUE
        [Test]
        public void TestVariableCountTop()
        {
            Assert.That(new TestVariableSkill(new double[0]).CountTopWeightedStrains(5), Is.Zero);

            var skill = new TestVariableSkill(new[] { 1.0, 2 });
            process(skill, create(0, 100));
            Assert.That(skill.CountTopWeightedStrains(0), Is.EqualTo(2));
            Assert.That(skill.CountTopWeightedStrains(10), Is.GreaterThan(0));
        }

        [Test]
        public void TestStrainPeakComparisonSortsHighestFirst()
        {
            var low = new VariableLengthStrainSkill.StrainPeak(1, 100.4);
            var high = new VariableLengthStrainSkill.StrainPeak(5, 100);

            Assert.That(low.SectionLength, Is.EqualTo(100));
            Assert.That(high.CompareTo(low), Is.LessThan(0));
            Assert.That(low.CompareTo(high), Is.GreaterThan(0));
            Assert.That(low.CompareTo(low), Is.Zero);
        }
    }
}
