// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Edit.Checks.Components;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckFilePresenceTest
    {
        private readonly CheckAudioPresence check = new CheckAudioPresence();

        private static Beatmap beatmap(string audio)
        {
            var b = new Beatmap();
            b.Metadata.AudioFile = audio;
            return b;
        }

        // CF: CheckFilePresence.Run | string.IsNullOrEmpty(filename) | TRUE
        [TestCase("")]
        [TestCase(null)]
        public void TestNoneSet(string? audio)
        {
            var issue = check.Run(CheckTestUtils.Context(beatmap(audio!))).Single();
            Assert.That(issue.Template, Is.InstanceOf<CheckFilePresence.IssueTemplateNoneSet>());
        }

        // CF: CheckFilePresence.Run | string.IsNullOrEmpty(filename) | FALSE
        // CF: CheckFilePresence.Run | storagePath != null | FALSE
        [Test]
        public void TestSetButNoBeatmapSet()
        {
            var issue = check.Run(CheckTestUtils.Context(beatmap("a.mp3"))).Single();
            Assert.That(issue.Template, Is.InstanceOf<CheckFilePresence.IssueTemplateDoesNotExist>());
        }

        [Test]
        public void TestSetButFileMissingFromSet()
        {
            var b = beatmap("a.mp3");
            var context = CheckFakes.Context(b, new Dictionary<string, byte[]?> { ["other.mp3"] = [1] });

            Assert.That(check.Run(context).Single().Template, Is.InstanceOf<CheckFilePresence.IssueTemplateDoesNotExist>());
        }

        // CF: CheckFilePresence.Run | storagePath != null | TRUE
        [Test]
        public void TestPresent()
        {
            var b = beatmap("A.MP3");
            var context = CheckFakes.Context(b, new Dictionary<string, byte[]?> { ["a.mp3"] = [1] });

            Assert.That(check.Run(context), Is.Empty);
        }

        [Test]
        public void TestMetadata()
        {
            Assert.That(check.Metadata.Category, Is.EqualTo(CheckCategory.Audio));
            Assert.That(check.Metadata.Scope, Is.EqualTo(CheckScope.BeatmapSet));
            Assert.That(new CheckBackgroundPresence().Run(CheckTestUtils.Context(new Beatmap())).Single().Template,
                Is.InstanceOf<CheckFilePresence.IssueTemplateNoneSet>());
            Assert.That(check.PossibleTemplates.Count(), Is.EqualTo(2));
        }
    }
}
