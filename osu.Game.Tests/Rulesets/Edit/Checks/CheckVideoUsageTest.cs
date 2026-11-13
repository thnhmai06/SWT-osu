// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Storyboards;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckVideoUsageTest
    {
        private readonly CheckVideoUsage check = new CheckVideoUsage();

        private static BeatmapVerifierContext.VerifiedBeatmap diff(string name, string? videoPath, double start = 0)
        {
            var beatmap = new Beatmap { BeatmapInfo = { DifficultyName = name } };
            var storyboard = new Storyboard();

            if (videoPath != null)
                storyboard.GetLayer("Video").Add(new StoryboardVideo(StoryboardElementSource.Beatmap, videoPath, start));

            return CheckFakes.Verified(beatmap, storyboard);
        }

        private Issue[] run(BeatmapVerifierContext.VerifiedBeatmap current, params BeatmapVerifierContext.VerifiedBeatmap[] others)
            => check.Run(new BeatmapVerifierContext(current, others, DifficultyRating.Hard)).ToArray();

        // CF: CheckVideoUsage.Run | currentVideo == null | TRUE
        // CF: CheckVideoUsage.Run | GetDifficultyVideo(other) != null (current has none) | FALSE
        [Test]
        public void TestNoVideoAnywhere()
        {
            Assert.That(run(diff("cur", null), diff("other", null)), Is.Empty);
        }

        // CF: CheckVideoUsage.Run | GetDifficultyVideo(other) != null (current has none) | TRUE
        [Test]
        public void TestOnlyOtherHasVideo()
        {
            var issue = run(diff("cur", null), diff("a", "v.mp4"), diff("b", "v.mp4")).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckVideoUsage.IssueTemplateMissingVideo>());
            Assert.That(issue.Arguments, Is.EqualTo(new object[] { "cur" }));
        }

        // CF: CheckVideoUsage.Run | currentVideo != null | FALSE
        // CF: CheckVideoUsage.Run | otherVideo == null | TRUE
        [Test]
        public void TestOtherMissingVideo()
        {
            var issue = run(diff("cur", "v.mp4"), diff("other", null)).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckVideoUsage.IssueTemplateMissingVideo>());
            Assert.That(issue.Arguments, Is.EqualTo(new object[] { "other" }));
        }

        // CF: CheckVideoUsage.Run | currentVideo != null | TRUE
        // CF: CheckVideoUsage.Run | otherVideo == null | FALSE
        // CF: CheckVideoUsage.Run | !string.Equals(otherVideo.Path, referencePath, OrdinalIgnoreCase) | TRUE
        [Test]
        public void TestDifferentVideo()
        {
            var issue = run(diff("cur", "v.mp4"), diff("other", "w.mp4")).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckVideoUsage.IssueTemplateDifferentVideo>());
        }

        // CF: CheckVideoUsage.Run | !string.Equals(otherVideo.Path, referencePath, OrdinalIgnoreCase) | FALSE
        // CF: CheckVideoUsage.Run | !StartTime.Equals(StartTime) | FALSE
        // CF: CheckVideoUsage.Run | foreach groupedByVideoPath | loop n
        [Test]
        public void TestSameVideoSameStartCaseInsensitive()
        {
            Assert.That(run(diff("cur", "v.mp4", 100), diff("other", "V.MP4", 100)), Is.Empty);
        }

        // CF: CheckVideoUsage.Run | !StartTime.Equals(StartTime) | TRUE
        [Test]
        public void TestSameVideoDifferentStart()
        {
            var issue = run(diff("cur", "v.mp4", 0), diff("other", "v.mp4", 500)).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckVideoUsage.IssueTemplateDifferentStartTime>());
        }

        // CF: CheckVideoUsage.Run | foreach groupedByVideoPath | loop 0
        [Test]
        public void TestNoOtherDifficulties()
        {
            Assert.That(run(diff("cur", "v.mp4")), Is.Empty);
            Assert.That(check.PossibleTemplates.Count(), Is.EqualTo(3));
        }
    }
}
