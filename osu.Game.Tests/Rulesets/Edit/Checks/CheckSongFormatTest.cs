// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit.Checks;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckSongFormatTest
    {
        private readonly CheckSongFormat check = new CheckSongFormat();

        private static Beatmap beatmap(string audio = "a.mp3")
        {
            var b = new Beatmap();
            b.Metadata.AudioFile = audio;
            return b;
        }

        // CF: CheckSongFormat.Run | beatmapSet == null | TRUE
        [Test]
        public void TestNoSet()
        {
            Assert.That(check.Run(CheckTestUtils.Context(beatmap())), Is.Empty);
        }

        // CF: CheckSongFormat.Run | beatmapSet == null | FALSE
        // CF: CheckSongFormat.Run | audioFile == null | TRUE
        [Test]
        public void TestAudioNotInSet()
        {
            Assert.That(check.Run(CheckFakes.Context(beatmap(), new Dictionary<string, byte[]?> { ["other.mp3"] = [1] })), Is.Empty);
        }

        // CF: CheckSongFormat.Run | audioFile == null | FALSE
        // CF: CheckSongFormat.Run | audioFormat == 0 | TRUE
        [Test]
        public void TestUndecodableAudioIsUnsupported()
        {
            var issue = check.Run(CheckFakes.Context(beatmap(), new Dictionary<string, byte[]?> { ["a.mp3"] = [] })).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckSongFormat.IssueTemplateFormatUnsupported>());
        }

        // CF: CheckSongFormat.Run | audioFormat == 0 | FALSE
        // CF: CheckSongFormat.Run | !allowedFormats.Contains(audioFormat) | infeasible: requires native BASS to decode real audio
        [Test]
        public void TestPossibleTemplates()
        {
            Assert.That(check.PossibleTemplates.Count(), Is.EqualTo(2));
        }
    }
}
