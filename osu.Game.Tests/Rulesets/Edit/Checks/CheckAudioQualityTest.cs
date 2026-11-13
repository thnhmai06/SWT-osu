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
    public class CheckAudioQualityTest
    {
        private readonly CheckAudioQuality check = new CheckAudioQuality();

        private Issue[] run(string audio, int? bitrate)
        {
            var beatmap = new Beatmap();
            beatmap.Metadata.AudioFile = audio;
            // audio file is intentionally absent from the set, so the format resolves to Unknown (non-OGG limits)
            return check.Run(CheckFakes.Context(beatmap, new Dictionary<string, byte[]?>(), null, new BitrateTrack(1000, bitrate))).ToArray();
        }

        // CF: CheckAudioQuality.Run | string.IsNullOrEmpty(audioFile) | TRUE
        [Test]
        public void TestNoAudioFile()
        {
            Assert.That(run("", 64), Is.Empty);
        }

        // CF: CheckAudioQuality.Run | string.IsNullOrEmpty(audioFile) | FALSE
        // CF: CheckAudioQuality.Run | track?.Bitrate == null | TRUE
        [Test]
        public void TestBitrateUnknown()
        {
            Assert.That(run("a.mp3", null).Single().Template, Is.InstanceOf<CheckAudioQuality.IssueTemplateNoBitrate>());
        }

        // CF: CheckAudioQuality.Run | track.Bitrate.Value == 0 | TRUE
        [Test]
        public void TestBitrateZero()
        {
            Assert.That(run("a.mp3", 0).Single().Template, Is.InstanceOf<CheckAudioQuality.IssueTemplateNoBitrate>());
        }

        // CF: CheckAudioQuality.Run | track.Bitrate.Value > upperBitrateLimit | TRUE
        // CF: CheckAudioQuality.Run | audioFormat.HasFlag(ChannelType.OGG) | FALSE
        [Test]
        public void TestTooHigh()
        {
            Assert.That(run("a.mp3", 193).Single().Template, Is.InstanceOf<CheckAudioQuality.IssueTemplateTooHighBitrate>());
        }

        // CF: CheckAudioQuality.Run | track.Bitrate.Value > upperBitrateLimit | FALSE
        // CF: CheckAudioQuality.Run | track.Bitrate.Value < min_bitrate | TRUE
        [Test]
        public void TestTooLow()
        {
            Assert.That(run("a.mp3", 127).Single().Template, Is.InstanceOf<CheckAudioQuality.IssueTemplateTooLowBitrate>());
        }

        // CF: CheckAudioQuality.Run | track.Bitrate.Value < min_bitrate | FALSE
        [TestCase(128)]
        [TestCase(192)]
        public void TestInRange(int bitrate)
        {
            Assert.That(run("a.mp3", bitrate), Is.Empty);
        }

        // CF: CheckAudioQuality.Run | audioFormat.HasFlag(ChannelType.OGG) | infeasible: needs BASS to decode a real OGG stream
        [Test]
        public void TestPossibleTemplates()
        {
            Assert.That(check.PossibleTemplates.Count(), Is.EqualTo(3));
        }
    }
}
