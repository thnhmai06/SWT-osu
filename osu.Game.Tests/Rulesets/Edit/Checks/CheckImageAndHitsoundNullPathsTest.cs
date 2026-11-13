// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit.Checks;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    /// <summary>
    /// Covers the early-exit branches of the checks whose remaining logic needs BASS or GL textures.
    /// </summary>
    [TestFixture]
    public class CheckImageAndHitsoundNullPathsTest
    {
        private static readonly byte[] big = new byte[500];

        private static Beatmap beatmap(string audio = "", string background = "")
        {
            var b = new Beatmap();
            b.Metadata.AudioFile = audio;
            b.Metadata.BackgroundFile = background;
            return b;
        }

        // CF: CheckBackgroundQuality.Run | string.IsNullOrEmpty(backgroundFile) | TRUE
        [Test]
        public void TestBackgroundQualityNoFile()
        {
            Assert.That(new CheckBackgroundQuality().Run(CheckTestUtils.Context(beatmap())), Is.Empty);
        }

        // CF: CheckBackgroundQuality.Run | string.IsNullOrEmpty(backgroundFile) | FALSE
        // CF: CheckBackgroundQuality.Run | texture == null | TRUE
        // CF: CheckBackgroundQuality.Run | texture.Width/Height comparisons | infeasible: a Texture needs a GL context
        [Test]
        public void TestBackgroundQualityNoTexture()
        {
            Assert.That(new CheckBackgroundQuality().Run(CheckTestUtils.Context(beatmap(background: "bg.png"))), Is.Empty);
        }

        // CF: CheckHitsoundsFormat.Run | beatmapSet == null | TRUE
        [Test]
        public void TestHitsoundsFormatNoSet()
        {
            Assert.That(new CheckHitsoundsFormat().Run(CheckTestUtils.Context(beatmap())), Is.Empty);
        }

        // CF: CheckHitsoundsFormat.Run | audioFiles.Contains(file) | TRUE
        // CF: CheckHitsoundsFormat.Run | data == null | TRUE
        // CF: CheckHitsoundsFormat.Run | !HasAudioExtension || !probablyHasAudioData | TRUE
        // CF: CheckHitsoundsFormat.Run | decodeStream == 0 | infeasible: requires native BASS
        [Test]
        public void TestHitsoundsFormatSkipsNonCandidates()
        {
            var context = CheckFakes.Context(beatmap(audio: "song.mp3"), new Dictionary<string, byte[]?>
            {
                ["song.mp3"] = big, // the beatmap's own audio, excluded
                ["missing.wav"] = null, // no stream
                ["image.png"] = big, // not an audio extension
                ["tiny.wav"] = new byte[100], // 100 bytes is not more than 100
            });

            Assert.That(new CheckHitsoundsFormat().Run(context), Is.Empty);
        }

        // CF: CheckTooShortAudioFiles.Run | beatmapSet != null | FALSE
        [Test]
        public void TestTooShortNoSet()
        {
            Assert.That(new CheckTooShortAudioFiles().Run(CheckTestUtils.Context(beatmap())), Is.Empty);
        }

        // CF: CheckTooShortAudioFiles.Run | data == null | TRUE
        // CF: CheckTooShortAudioFiles.Run | decodeStream == 0 / ms < ms_threshold | infeasible: requires native BASS
        [Test]
        public void TestTooShortNullStream()
        {
            var context = CheckFakes.Context(beatmap(), new Dictionary<string, byte[]?> { ["a.wav"] = null });

            Assert.That(new CheckTooShortAudioFiles().Run(context), Is.Empty);
        }

        // CF: CheckDelayedHitsounds.Run | beatmapSet == null | TRUE
        [Test]
        public void TestDelayedHitsoundsNoSet()
        {
            Assert.That(new CheckDelayedHitsounds().Run(CheckTestUtils.Context(beatmap())), Is.Empty);
        }

        // CF: CheckDelayedHitsounds.Run | stream == null | TRUE
        // CF: CheckDelayedHitsounds.Run | !isHitSound(file.Filename) | TRUE
        // CF: CheckDelayedHitsounds.Run | waveform amplitude analysis | infeasible: Waveform requires native BASS
        [Test]
        public void TestDelayedHitsoundsSkipsNonHitsounds()
        {
            var context = CheckFakes.Context(beatmap(), new Dictionary<string, byte[]?>
            {
                ["normal-hitnormal.wav"] = null, // hitsound name but no stream
                ["song.mp3"] = big, // not <bank>-<sampleset>
                ["normal-hitnormal.png"] = big, // wrong extension
                ["bogus-hitnormal.wav"] = big, // unknown bank
                ["normal-bogus.wav"] = big, // unknown sample set
            });

            Assert.That(new CheckDelayedHitsounds().Run(context), Is.Empty);
        }
    }
}
