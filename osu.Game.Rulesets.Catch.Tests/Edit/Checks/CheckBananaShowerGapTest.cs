// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Edit.Checks;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Rulesets.Objects;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Catch.Tests.Edit.Checks
{
    [TestFixture]
    public class CheckBananaShowerGapTest
    {
        private readonly CheckBananaShowerGap check = new CheckBananaShowerGap();

        private List<Issue> run(DifficultyRating rating, params HitObject[] hitObjects)
        {
            var beatmap = new Beatmap<HitObject> { HitObjects = hitObjects.ToList() };
            return check.Run(new BeatmapVerifierContext(beatmap, new TestWorkingBeatmap(beatmap), rating)).ToList();
        }

        private static BananaShower shower(double start, double end) => new BananaShower { StartTime = start, EndTime = end };

        private static Fruit fruit(double time) => new Fruit { StartTime = time };

        // CF: CheckBananaShowerGap.Run | for (i < Count - 1) | zero iterations (empty and single object)
        [Test]
        public void TestEmptyAndSingleObjectBeatmapsYieldNoIssues()
        {
            Assert.That(run(DifficultyRating.Easy), Is.Empty);
            Assert.That(run(DifficultyRating.Easy, shower(0, 10)), Is.Empty);
        }

        // CF: Run | !(hitObjects[i] is BananaShower) | TRUE -> continue
        [Test]
        public void TestNonBananaShowerObjectsAreSkipped()
        {
            Assert.That(run(DifficultyRating.Easy, fruit(0), fruit(1), fruit(2)), Is.Empty);
        }

        // CF: Run | last object is never examined as banana shower (loop bound Count - 1) | no issue
        [Test]
        public void TestTrailingBananaShowerIsNotChecked()
        {
            Assert.That(run(DifficultyRating.Easy, fruit(0), shower(1, 2)), Is.Empty);
        }

        // CF: Run | i != 0 FALSE (first object is shower); end delta < threshold FALSE
        [Test]
        public void TestLeadingBananaShowerSkipsStartCheckAndPassesEndCheck()
        {
            Assert.That(run(DifficultyRating.Easy, shower(0, 100), fruit(350)), Is.Empty);
        }

        // CF: Run | i != 0 FALSE; end delta < threshold TRUE -> end gap issue only
        [Test]
        public void TestLeadingBananaShowerWithShortEndGapYieldsEndIssue()
        {
            var issues = run(DifficultyRating.Easy, shower(0, 100), fruit(349));

            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues.Single().Template, Is.InstanceOf<CheckBananaShowerGap.IssueTemplateBananaShowerEndGap>());
        }

        // CF: Run | previous is CatchHitObject FALSE (plain HitObject) -> start check skipped
        [Test]
        public void TestPreviousNonCatchObjectSkipsStartCheck()
        {
            Assert.That(run(DifficultyRating.Easy, new HitObject { StartTime = 0 }, shower(1, 100), fruit(350)), Is.Empty);
        }

        // CF: Run | !(previous is BananaShower) FALSE -> consecutive showers allowed; next is shower -> end check skipped
        [Test]
        public void TestConsecutiveBananaShowersYieldNoIssues()
        {
            Assert.That(run(DifficultyRating.Easy, shower(0, 100), shower(101, 200), shower(201, 300), shower(301, 400)), Is.Empty);
        }

        // CF: Run | next is CatchHitObject FALSE (plain HitObject) -> end check skipped
        [Test]
        public void TestNextNonCatchObjectSkipsEndCheck()
        {
            Assert.That(run(DifficultyRating.Easy, shower(0, 100), new HitObject { StartTime = 101 }), Is.Empty);
        }

        // CF: Run | spinnerStartDelta < expectedStartDelta | TRUE (Easy threshold 250, delta 249)
        [Test]
        public void TestStartGapJustBelowThresholdYieldsStartIssueWithFlooredDelta()
        {
            var issues = run(DifficultyRating.Easy, fruit(0), shower(249.7, 1000), fruit(1250));

            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues.Single().Template, Is.InstanceOf<CheckBananaShowerGap.IssueTemplateBananaShowerStartGap>());
            Assert.That(issues.Single().Arguments[0], Is.EqualTo(249d));
            Assert.That(issues.Single().Arguments[1], Is.EqualTo(250));
        }

        // CF: Run | spinnerStartDelta < expectedStartDelta | FALSE (delta == threshold)
        [Test]
        public void TestStartGapExactlyAtThresholdIsAllowed()
        {
            Assert.That(run(DifficultyRating.Easy, fruit(0), shower(250, 1000), fruit(1250)), Is.Empty);
        }

        // CF: Run | spinnerEndDelta < expectedEndDelta | TRUE (Easy threshold 250, delta 249)
        [Test]
        public void TestEndGapJustBelowThresholdYieldsEndIssue()
        {
            var issues = run(DifficultyRating.Easy, fruit(0), shower(250, 1000), fruit(1249));

            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues.Single().Template, Is.InstanceOf<CheckBananaShowerGap.IssueTemplateBananaShowerEndGap>());
        }

        // CF: Run | spinnerEndDelta < expectedEndDelta | FALSE (delta == threshold)
        [Test]
        public void TestEndGapExactlyAtThresholdIsAllowed()
        {
            Assert.That(run(DifficultyRating.Easy, fruit(0), shower(250, 1000), fruit(1250)), Is.Empty);
        }

        // CF: Run | both start and end TRUE -> two issues, start first
        [Test]
        public void TestBothGapsTooShortYieldStartThenEndIssue()
        {
            var issues = run(DifficultyRating.Easy, fruit(0), shower(10, 20), fruit(30));

            Assert.That(issues, Has.Count.EqualTo(2));
            Assert.That(issues[0].Template, Is.InstanceOf<CheckBananaShowerGap.IssueTemplateBananaShowerStartGap>());
            Assert.That(issues[1].Template, Is.InstanceOf<CheckBananaShowerGap.IssueTemplateBananaShowerEndGap>());
        }

        // CF: Run | threshold dictionary lookup per difficulty rating | (start,end) allowed at threshold, violated one below
        [TestCase(DifficultyRating.Easy, 250, 250)]
        [TestCase(DifficultyRating.Normal, 250, 250)]
        [TestCase(DifficultyRating.Hard, 125, 250)]
        [TestCase(DifficultyRating.Insane, 125, 125)]
        [TestCase(DifficultyRating.Expert, 62, 125)]
        [TestCase(DifficultyRating.ExpertPlus, 62, 125)]
        public void TestThresholdsPerDifficultyRating(DifficultyRating rating, int startGap, int endGap)
        {
            Assert.That(run(rating, fruit(0), shower(startGap, 1000), fruit(1000 + endGap)), Is.Empty);
            Assert.That(run(rating, fruit(0), shower(startGap - 1, 1000), fruit(1000 + endGap)).Single().Template,
                Is.InstanceOf<CheckBananaShowerGap.IssueTemplateBananaShowerStartGap>());
            Assert.That(run(rating, fruit(0), shower(startGap, 1000), fruit(1000 + endGap - 1)).Single().Template,
                Is.InstanceOf<CheckBananaShowerGap.IssueTemplateBananaShowerEndGap>());
        }
    }
}
