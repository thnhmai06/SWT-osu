// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Storyboards;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckAudioInVideoTest
    {
        private readonly CheckAudioInVideo check = new CheckAudioInVideo();

        /// <summary>Minimal valid PCM wave header (1 channel, 8 kHz, 8-bit, no samples); TagLib reports one audio channel for it.</summary>
        private static readonly byte[] wave_with_audio =
        [
            0x52, 0x49, 0x46, 0x46, 36, 0, 0, 0, 0x57, 0x41, 0x56, 0x45,
            0x66, 0x6D, 0x74, 0x20, 16, 0, 0, 0, 1, 0, 1, 0, 0x40, 0x1F, 0, 0, 0x40, 0x1F, 0, 0, 1, 0, 8, 0,
            0x64, 0x61, 0x74, 0x61, 0, 0, 0, 0
        ];

        private static Storyboard storyboard(params string[] videos)
        {
            var sb = new Storyboard();
            foreach (string v in videos) sb.GetLayer("Video").Add(new StoryboardVideo(StoryboardElementSource.Beatmap, v, 0));
            return sb;
        }

        private Issue[] run(Storyboard sb, Dictionary<string, byte[]?> files)
            => check.Run(CheckFakes.Context(new Beatmap(), files, sb)).ToArray();

        // CF: CheckAudioInVideo.Run | foreach layer / foreach element | loop 0
        // CF: CheckAudioInVideo.Run | foreach filename in videoPaths | loop 0
        [Test]
        public void TestNoVideo()
        {
            Assert.That(run(new Storyboard(), new Dictionary<string, byte[]?>()), Is.Empty);
        }

        // CF: CheckAudioInVideo.Run | !(element is StoryboardVideo video) | TRUE
        [Test]
        public void TestNonVideoElementsIgnored()
        {
            var sb = new Storyboard();
            sb.GetLayer("Foreground").Add(new StoryboardSprite(StoryboardElementSource.Beatmap, "a.png", osu.Framework.Graphics.Anchor.Centre, osuTK.Vector2.Zero));

            Assert.That(run(sb, new Dictionary<string, byte[]?>()), Is.Empty);
        }

        // CF: CheckAudioInVideo.Run | !(element is StoryboardVideo video) | FALSE
        // CF: CheckAudioInVideo.Run | !videoPaths.Contains(video.Path) | TRUE
        // CF: CheckAudioInVideo.Run | !videoPaths.Contains(video.Path) | FALSE
        // CF: CheckAudioInVideo.Run | storagePath == null | TRUE
        [Test]
        public void TestMissingFileReportedOncePerPath()
        {
            var issue = run(storyboard("v.mp4", "v.mp4"), new Dictionary<string, byte[]?>()).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckAudioInVideo.IssueTemplateMissingFile>());
            Assert.That(issue.Arguments, Is.EqualTo(new object[] { "v.mp4" }));
        }

        // CF: CheckAudioInVideo.Run | storagePath == null | FALSE
        // CF: CheckAudioInVideo.Run | tagFile.Properties.AudioChannels == 0 | FALSE
        [Test]
        public void TestAudioTrackDetected()
        {
            var issue = run(storyboard("v.wav"), new Dictionary<string, byte[]?> { ["v.wav"] = wave_with_audio }).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckAudioInVideo.IssueTemplateHasAudioTrack>());
        }

        // CF: CheckAudioInVideo.Run | catch CorruptFileException / UnsupportedFormatException / Exception | case any
        [Test]
        public void TestUnreadableFileReportsError()
        {
            var issue = run(storyboard("v.mp4"), new Dictionary<string, byte[]?> { ["v.mp4"] = [1, 2, 3] }).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckAudioInVideo.IssueTemplateFileError>());
        }

        // CF: CheckAudioInVideo.Run | tagFile.Properties.AudioChannels == 0 | infeasible: needs a real video container without audio
        [Test]
        public void TestPossibleTemplates()
        {
            Assert.That(check.PossibleTemplates.Count(), Is.EqualTo(3));
        }
    }
}
