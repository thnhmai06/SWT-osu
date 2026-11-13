// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
using ManagedBass;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit.Checks.Components;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class AudioCheckUtilsTest
    {
        // CF: AudioCheckUtils.HasAudioExtension | AUDIO_EXTENSIONS.Contains(ext.ToLowerInvariant()) | TRUE
        [TestCase("a.mp3")]
        [TestCase("a.OGG")]
        [TestCase("dir/a.wav")]
        public void TestAudioExtension(string name)
        {
            Assert.That(AudioCheckUtils.HasAudioExtension(name), Is.True);
        }

        // CF: AudioCheckUtils.HasAudioExtension | AUDIO_EXTENSIONS.Contains(ext.ToLowerInvariant()) | FALSE
        [TestCase("a.png")]
        [TestCase("a")]
        [TestCase("a.mp3.txt")]
        public void TestNonAudioExtension(string name)
        {
            Assert.That(AudioCheckUtils.HasAudioExtension(name), Is.False);
        }

        // CF: AudioCheckUtils.GetAudioFormat | data.Length <= 0 | TRUE
        [Test]
        public void TestEmptyStreamIsUnknown()
        {
            Assert.That(AudioCheckUtils.GetAudioFormat(new MemoryStream()), Is.EqualTo(ChannelType.Unknown));
        }

        // CF: AudioCheckUtils.GetAudioFormat | data.Length <= 0 | FALSE
        // CF: AudioCheckUtils.GetAudioFormat | decodeStream == 0 | infeasible: requires native BASS library
        // CF: AudioCheckUtils.GetAudioFormatFromFile | beatmapSet == null || audioFile == null | TRUE
        [Test]
        public void TestFromFileWithoutSet()
        {
            Assert.That(AudioCheckUtils.GetAudioFormatFromFile(CheckTestUtils.Context(new Beatmap()), "a.mp3"), Is.EqualTo(ChannelType.Unknown));
        }

        [Test]
        public void TestFromFileMissingInSet()
        {
            var context = CheckFakes.Context(new Beatmap(), new Dictionary<string, byte[]?> { ["b.mp3"] = [1] });

            Assert.That(AudioCheckUtils.GetAudioFormatFromFile(context, "a.mp3"), Is.EqualTo(ChannelType.Unknown));
        }

        // CF: AudioCheckUtils.GetAudioFormatFromFile | beatmapSet == null || audioFile == null | FALSE
        [Test]
        public void TestFromFileEmptyContent()
        {
            var context = CheckFakes.Context(new Beatmap(), new Dictionary<string, byte[]?> { ["a.mp3"] = [] });

            Assert.That(AudioCheckUtils.GetAudioFormatFromFile(context, "a.mp3"), Is.EqualTo(ChannelType.Unknown));
        }
    }
}
