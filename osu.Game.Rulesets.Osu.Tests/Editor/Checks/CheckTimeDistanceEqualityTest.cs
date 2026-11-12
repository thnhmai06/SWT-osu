// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Checks;
using static osu.Game.Rulesets.Osu.Tests.Editor.Checks.CheckTestHelpers;

namespace osu.Game.Rulesets.Osu.Tests.Editor.Checks
{
    [TestFixture]
    public class CheckTimeDistanceEqualityTest
    {
        private readonly CheckTimeDistanceEquality check = new CheckTimeDistanceEquality();

        /// <summary>Three circles 100px / 300ms apart establish the expected spacing, followed by <paramref name="rest"/>.</summary>
        private static HitObject[] pattern(params HitObject[] rest)
            => new HitObject[] { Circle(0, 0), Circle(300, 100), Circle(600, 200) }.Concat(rest).ToArray();

        private System.Collections.Generic.List<osu.Game.Rulesets.Edit.Checks.Components.Issue> run(HitObject[] objects, DifficultyRating rating = DifficultyRating.Easy)
            => check.Run(Context(objects, rating)).ToList();

        [Test]
        public void TestRegularSpacing_NoIssue()
        {
            Assert.That(run(pattern(Circle(900, 300))), Is.Empty);
        }

        [Test]
        public void TestSpacingMuchLarger_ReportsProblem()
        {
            // |100 - 300| = 200 => (200 - 20) / 300 = 0.6 > 0.3
            var issues = run(pattern(Circle(900, 500)));

            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckTimeDistanceEquality.IssueTemplateIrregularSpacingProblem>());
            // distances use LengthFast (approximate inverse square root)
            Assert.That(issues[0].Arguments[0], Is.EqualTo(100).Within(1));
        }

        [Test]
        public void TestSpacingMuchSmaller_ReportsProblem()
        {
            // distance 60: |100 - 60| = 40 => (40 - 20) / 60 = 0.33 > 0.3
            Assert.That(run(pattern(Circle(900, 260)))[0].Template, Is.TypeOf<CheckTimeDistanceEquality.IssueTemplateIrregularSpacingProblem>());
        }

        [Test]
        public void TestSpacingSlightlyLarger_ReportsWarning()
        {
            // distance 140: problem (40 - 20) / 140 = 0.14 <= 0.3; warning (40 - 10) / 140 = 0.21 > 0.15
            var issues = run(pattern(Circle(900, 340)));

            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckTimeDistanceEquality.IssueTemplateIrregularSpacingWarning>());
        }

        [Test]
        public void TestSpacingWithinLeniency_NoIssue()
        {
            // distance 110: warning (10 - 10) / 110 = 0 <= 0.15
            Assert.That(run(pattern(Circle(900, 310))), Is.Empty);
        }

        [Test]
        public void TestIrregularObjectIsNotUsedAsReference()
        {
            // the far circle (problem) must not become the new expectation: the following regular pair is only compared with the earlier ones
            var issues = run(pattern(Circle(900, 500), Circle(1200, 600)));

            Assert.That(issues, Has.Count.EqualTo(1));
        }

        [Test]
        public void TestFirstObservation_HasNothingToCompareWith()
        {
            // expectedDistance == 0 for the first observation
            Assert.That(run(new HitObject[] { Circle(0, 0), Circle(300, 500) }), Is.Empty);
        }

        [Test]
        public void TestDifferentTimeDelta_BreaksTheComparison()
        {
            // 600ms later spacing differs by more than the 16ms leniency => no relevant observations
            Assert.That(run(new HitObject[] { Circle(0, 0), Circle(300, 100), Circle(900, 600) }), Is.Empty);
        }

        [Test]
        public void TestSimilarTimeDelta_WithinLeniency_StillCompared()
        {
            // 310ms vs 300ms is within 16ms; expected distance scales with time (103.3) => 500 is still irregular
            var issues = run(new HitObject[] { Circle(0, 0), Circle(300, 100), Circle(610, 600) });

            Assert.That(issues, Has.Count.EqualTo(1));
        }

        [Test]
        public void TestExpiredObservation_IsNotUsed()
        {
            // the only earlier observation is more than 4000ms old, so the irregular last pair is not flagged
            var objects = new HitObject[] { Circle(0, 0), Circle(300, 100), Circle(5300, 1000), Circle(5600, 1300) };

            Assert.That(run(objects), Is.Empty);
        }

        [Test]
        public void TestPairsMoreThan600msApart_AreIgnored()
        {
            Assert.That(run(pattern(Circle(1300, 800))), Is.Empty);
        }

        [Test]
        public void TestPairAtExactly600ms_IsConsidered()
        {
            // deltaTime == 600 is not > 600, so the pairs are evaluated: the second pair (500px vs the expected 100px) is a problem
            Assert.That(run(new HitObject[] { Circle(0, 0), Circle(600, 100), Circle(1200, 600) }), Has.Count.EqualTo(1));
        }

        [Test]
        public void TestStackedObjects_AreIgnored()
        {
            // distance 5 < 12
            Assert.That(run(pattern(Circle(900, 205))), Is.Empty);
        }

        [Test]
        public void TestDistanceJustAboveStackLeniency_IsConsidered()
        {
            // distance 13 is not < 12 and 87px off the expected 100 => problem
            Assert.That(run(pattern(Circle(900, 213))).Single().Template, Is.TypeOf<CheckTimeDistanceEquality.IssueTemplateIrregularSpacingProblem>());
        }

        [Test]
        public void TestSpinnerAsFirstOfPair_IsSkipped()
        {
            Assert.That(run(new HitObject[] { Spinner(0, 100), Circle(300, 500) }), Is.Empty);
        }

        [Test]
        public void TestSpinnerAsSecondOfPair_IsSkipped()
        {
            Assert.That(run(new HitObject[] { Circle(0, 0), Spinner(300, 100) }), Is.Empty);
        }

        [Test]
        public void TestNonOsuObjects_AreSkipped()
        {
            Assert.That(run(pattern(new PlainHitObject { StartTime = 900 }, Circle(1200, 900))), Is.Empty);
        }

        [Test]
        public void TestNormalDifficulty_IsStillChecked()
        {
            // InterpretedDifficulty > Normal FALSE at Normal
            Assert.That(run(pattern(Circle(900, 500)), DifficultyRating.Normal), Has.Count.EqualTo(1));
        }

        [Test]
        public void TestHardDifficulty_IsSkipped()
        {
            // InterpretedDifficulty > Normal TRUE
            Assert.That(run(pattern(Circle(900, 500)), DifficultyRating.Hard), Is.Empty);
        }

        [Test]
        public void TestEmptyAndSingleObject_NoIssue()
        {
            Assert.That(run(new HitObject[0]), Is.Empty);
            Assert.That(run(new HitObject[] { Circle(0) }), Is.Empty);
        }
    }
}
