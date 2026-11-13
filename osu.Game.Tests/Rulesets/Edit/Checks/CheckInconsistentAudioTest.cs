// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckInconsistentAudioTest
    {
        private readonly CheckInconsistentAudio check = new CheckInconsistentAudio();

        private static Beatmap diff(string name, string audio)
        {
            var b = new Beatmap { BeatmapInfo = { DifficultyName = name } };
            b.Metadata.AudioFile = audio;
            return b;
        }

        private object[][] run(Beatmap current, params Beatmap[] others)
            => check.Run(new BeatmapVerifierContext(CheckFakes.Verified(current), others.Select(o => CheckFakes.Verified(o)).ToList(), DifficultyRating.Hard))
                    .Select(i => i.Arguments).ToArray();

        // CF: CheckInconsistentAudio.Run | AllDifficulties.Count() <= 1 | TRUE
        [Test]
        public void TestSingleDifficulty()
        {
            Assert.That(run(diff("a", "x.mp3")), Is.Empty);
        }

        // CF: CheckInconsistentAudio.Run | AllDifficulties.Count() <= 1 | FALSE
        // CF: CheckInconsistentAudio.Run | referenceAudioFile != currentAudioFile | FALSE
        [Test]
        public void TestSameAudio()
        {
            Assert.That(run(diff("a", "x.mp3"), diff("b", "x.mp3")), Is.Empty);
        }

        // CF: CheckInconsistentAudio.Run | referenceAudioFile != currentAudioFile | TRUE
        // CF: CheckInconsistentAudio.Run | string.IsNullOrEmpty(referenceAudioFile) | FALSE
        // CF: CheckInconsistentAudio.Run | string.IsNullOrEmpty(currentAudioFile) | FALSE
        [Test]
        public void TestDifferentAudio()
        {
            Assert.That(run(diff("a", "x.mp3"), diff("b", "y.mp3")).Single(), Is.EqualTo(new object[] { "x.mp3", "b", "y.mp3" }));
        }

        // CF: CheckInconsistentAudio.Run | string.IsNullOrEmpty(referenceAudioFile) | TRUE
        // CF: CheckInconsistentAudio.Run | string.IsNullOrEmpty(currentAudioFile) | TRUE
        [Test]
        public void TestUnsetShownAsNotSet()
        {
            Assert.That(run(diff("a", ""), diff("b", "y.mp3")).Single(), Is.EqualTo(new object[] { "not set", "b", "y.mp3" }));
            Assert.That(run(diff("a", "x.mp3"), diff("b", "")).Single(), Is.EqualTo(new object[] { "x.mp3", "b", "not set" }));
        }
    }
}
