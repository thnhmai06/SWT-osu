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
    public class CheckVideoResolutionTest
    {
        private readonly CheckVideoResolution check = new CheckVideoResolution();

        private static readonly byte[] wave_without_video =
        [
            0x52, 0x49, 0x46, 0x46, 36, 0, 0, 0, 0x57, 0x41, 0x56, 0x45,
            0x66, 0x6D, 0x74, 0x20, 16, 0, 0, 0, 1, 0, 1, 0, 0x40, 0x1F, 0, 0, 0x40, 0x1F, 0, 0, 1, 0, 8, 0,
            0x64, 0x61, 0x74, 0x61, 0, 0, 0, 0
        ];

        private Issue[] run(Dictionary<string, byte[]?> files, params string[] videos)
        {
            var sb = new Storyboard();
            foreach (string v in videos) sb.GetLayer("Video").Add(new StoryboardVideo(StoryboardElementSource.Beatmap, v, 0));
            return check.Run(CheckFakes.Context(new Beatmap(), files, sb)).ToArray();
        }

        // CF: CheckVideoResolution.Run | foreach filename in videoPaths | loop 0
        [Test]
        public void TestNoVideo()
        {
            Assert.That(run(new Dictionary<string, byte[]?>()), Is.Empty);
        }

        // CF: CheckVideoResolution.Run | storagePath == null | TRUE
        // CF: CheckVideoResolution.getVideoPaths | !videoPaths.Contains(video.Path) | FALSE
        [Test]
        public void TestMissingVideoNotReportedHere()
        {
            Assert.That(run(new Dictionary<string, byte[]?>(), "v.mp4", "v.mp4"), Is.Empty);
        }

        // CF: CheckVideoResolution.Run | height <= max_video_height || width <= max_video_width | TRUE
        [Test]
        public void TestSmallResolutionIsFine()
        {
            Assert.That(run(new Dictionary<string, byte[]?> { ["v.wav"] = wave_without_video }, "v.wav"), Is.Empty);
        }

        // CF: CheckVideoResolution.Run | catch CorruptFileException / UnsupportedFormatException / Exception | case any
        [Test]
        public void TestUnreadableFileReportsError()
        {
            var issue = run(new Dictionary<string, byte[]?> { ["v.mp4"] = [1, 2, 3] }, "v.mp4").Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckVideoResolution.IssueTemplateFileError>());
        }

        // CF: CheckVideoResolution.Run | height <= max_video_height || width <= max_video_width | infeasible: FALSE needs a real high-resolution video container
        [Test]
        public void TestPossibleTemplates()
        {
            Assert.That(check.PossibleTemplates.Count(), Is.GreaterThan(0));
        }
    }
}
