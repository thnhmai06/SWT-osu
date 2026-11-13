// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckPreviewTimeTest
    {
        private readonly CheckPreviewTime check = new CheckPreviewTime();

        private static Beatmap beatmap(int previewTime, string name = "diff")
        {
            var b = new Beatmap();
            b.BeatmapInfo.Metadata.PreviewTime = previewTime;
            b.BeatmapInfo.DifficultyName = name;
            return b;
        }

        private static BeatmapVerifierContext context(Beatmap current, params Beatmap[] others)
            => new BeatmapVerifierContext(
                new BeatmapVerifierContext.VerifiedBeatmap(new TestWorkingBeatmap(current), current),
                others.Select(o => new BeatmapVerifierContext.VerifiedBeatmap(new TestWorkingBeatmap(o), o)).ToList(),
                DifficultyRating.ExpertPlus);

        // CF: CheckPreviewTime.Run | previewTime == -1 | TRUE
        // CF: CheckPreviewTime.Run | foreach other difficulty | loop 0
        [Test]
        public void TestUnsetPreviewTime()
        {
            var issue = check.Run(context(beatmap(-1))).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckPreviewTime.IssueTemplateHasNoPreviewTime>());
            Assert.That(check.PossibleTemplates.Count(), Is.EqualTo(2));
        }

        // CF: CheckPreviewTime.Run | previewTime == -1 | FALSE
        // CF: CheckPreviewTime.Run | foreach other difficulty | loop 1
        // CF: CheckPreviewTime.Run | other.PreviewTime != previewTime | FALSE
        [Test]
        public void TestMatchingPreviewTimes()
        {
            Assert.That(check.Run(context(beatmap(5000), beatmap(5000))), Is.Empty);
        }

        // CF: CheckPreviewTime.Run | other.PreviewTime != previewTime | TRUE
        // CF: CheckPreviewTime.Run | foreach other difficulty | loop n
        [Test]
        public void TestConflictingPreviewTimes()
        {
            var issues = check.Run(context(beatmap(5000), beatmap(6000, "a"), beatmap(5000, "b"), beatmap(-1, "c"))).ToArray();

            Assert.That(issues.Length, Is.EqualTo(2));
            Assert.That(issues.All(i => i.Template is CheckPreviewTime.IssueTemplatePreviewTimeConflict), Is.True);
            Assert.That(issues[0].Arguments[0], Is.EqualTo("a"));
        }

        // CF: CheckPreviewTime.IssueTemplatePreviewTimeConflict.Create | originalTime != -1 ? ... : "not set" | TRUE
        // CF: CheckPreviewTime.IssueTemplatePreviewTimeConflict.Create | originalTime != -1 ? ... : "not set" | FALSE
        // CF: CheckPreviewTime.IssueTemplatePreviewTimeConflict.Create | conflictTime != -1 ? ... : "not set" | TRUE
        // CF: CheckPreviewTime.IssueTemplatePreviewTimeConflict.Create | conflictTime != -1 ? ... : "not set" | FALSE
        [Test]
        public void TestUnsetTimesAreDescribedAsNotSet()
        {
            var template = new CheckPreviewTime.IssueTemplatePreviewTimeConflict(check);

            var setVsUnset = template.Create("d", 5000, -1);
            Assert.That(setVsUnset.Arguments[2], Is.EqualTo("not set"));
            Assert.That(setVsUnset.Arguments[1], Does.EndWith(" ms"));

            var unsetVsSet = template.Create("d", -1, 5000);
            Assert.That(unsetVsSet.Arguments[1], Is.EqualTo("not set"));
            Assert.That(unsetVsSet.Arguments[2], Does.EndWith(" ms"));
        }
    }
}
