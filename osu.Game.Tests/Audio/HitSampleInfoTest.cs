// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Audio;

namespace osu.Game.Tests.Audio
{
    [TestFixture]
    public class HitSampleInfoTest
    {
        // CF: HitSampleInfo.LookupNames | !string.IsNullOrEmpty(Suffix) | TRUE
        [Test]
        public void TestLookupNamesWithSuffix()
        {
            var sample = new HitSampleInfo(HitSampleInfo.HIT_CLAP, HitSampleInfo.BANK_SOFT, "2");

            Assert.That(sample.LookupNames.ToArray(), Is.EqualTo(new[] { "Gameplay/soft-hitclap2", "Gameplay/soft-hitclap", "Gameplay/hitclap" }));
        }

        // CF: HitSampleInfo.LookupNames | !string.IsNullOrEmpty(Suffix) | FALSE
        [Test]
        public void TestLookupNamesWithoutSuffix()
        {
            var sample = new HitSampleInfo(HitSampleInfo.HIT_NORMAL, HitSampleInfo.BANK_DRUM);

            Assert.That(sample.LookupNames.ToArray(), Is.EqualTo(new[] { "Gameplay/drum-hitnormal", "Gameplay/hitnormal" }));
        }

        // CF: HitSampleInfo.With | newName.HasValue | TRUE
        // CF: HitSampleInfo.With | newBank.HasValue | TRUE
        // CF: HitSampleInfo.With | newSuffix.HasValue | TRUE
        // CF: HitSampleInfo.With | newVolume.HasValue | TRUE
        // CF: HitSampleInfo.With | newEditorAutoBank.HasValue | TRUE
        // CF: HitSampleInfo.With | newUseBeatmapSamples.HasValue | TRUE
        [Test]
        public void TestWithOverridesEverything()
        {
            var sample = new HitSampleInfo("a").With(newName: "b", newBank: "c", newSuffix: "d", newVolume: 5, newEditorAutoBank: false, newUseBeatmapSamples: true);

            Assert.That(sample.Name, Is.EqualTo("b"));
            Assert.That(sample.Bank, Is.EqualTo("c"));
            Assert.That(sample.Suffix, Is.EqualTo("d"));
            Assert.That(sample.Volume, Is.EqualTo(5));
            Assert.That(sample.EditorAutoBank, Is.False);
            Assert.That(sample.UseBeatmapSamples, Is.True);
        }

        // CF: HitSampleInfo.With | newName.HasValue | FALSE
        // CF: HitSampleInfo.With | newBank.HasValue | FALSE
        // CF: HitSampleInfo.With | newSuffix.HasValue | FALSE
        // CF: HitSampleInfo.With | newVolume.HasValue | FALSE
        // CF: HitSampleInfo.With | newEditorAutoBank.HasValue | FALSE
        // CF: HitSampleInfo.With | newUseBeatmapSamples.HasValue | FALSE
        [Test]
        public void TestWithNothingKeepsEverything()
        {
            var sample = new HitSampleInfo("a", "b", "c", 7, false, true).With();

            Assert.That(sample.Name, Is.EqualTo("a"));
            Assert.That(sample.Bank, Is.EqualTo("b"));
            Assert.That(sample.Suffix, Is.EqualTo("c"));
            Assert.That(sample.Volume, Is.EqualTo(7));
            Assert.That(sample.EditorAutoBank, Is.False);
            Assert.That(sample.UseBeatmapSamples, Is.True);
        }

        // CF: HitSampleInfo.Equals | other != null | FALSE
        [Test]
        public void TestEqualsNull() => Assert.That(new HitSampleInfo("a").Equals((HitSampleInfo?)null), Is.False);

        // CF: HitSampleInfo.Equals | other != null | TRUE
        // CF: HitSampleInfo.Equals | Name == other.Name | TRUE
        // CF: HitSampleInfo.Equals | Bank == other.Bank | TRUE
        // CF: HitSampleInfo.Equals | Suffix == other.Suffix | TRUE
        // CF: HitSampleInfo.Equals | UseBeatmapSamples == other.UseBeatmapSamples | TRUE
        [Test]
        public void TestEqualsIgnoresVolumeAndAutoBank()
        {
            var a = new HitSampleInfo("a", "b", "c", 10, true, true);
            var b = new HitSampleInfo("a", "b", "c", 90, false, true);

            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        // CF: HitSampleInfo.Equals | Name == other.Name | FALSE
        [Test]
        public void TestEqualsDifferentName() => Assert.That(new HitSampleInfo("a").Equals(new HitSampleInfo("x")), Is.False);

        // CF: HitSampleInfo.Equals | Bank == other.Bank | FALSE
        [Test]
        public void TestEqualsDifferentBank() => Assert.That(new HitSampleInfo("a", "b").Equals(new HitSampleInfo("a", "x")), Is.False);

        // CF: HitSampleInfo.Equals | Suffix == other.Suffix | FALSE
        [Test]
        public void TestEqualsDifferentSuffix() => Assert.That(new HitSampleInfo("a", "b", "1").Equals(new HitSampleInfo("a", "b", "2")), Is.False);

        // CF: HitSampleInfo.Equals | UseBeatmapSamples == other.UseBeatmapSamples | FALSE
        [Test]
        public void TestEqualsDifferentUseBeatmapSamples() => Assert.That(new HitSampleInfo("a", useBeatmapSamples: true).Equals(new HitSampleInfo("a")), Is.False);

        // CF: HitSampleInfo.Equals(object) | obj is HitSampleInfo | TRUE
        // CF: HitSampleInfo.Equals(object) | obj is HitSampleInfo | FALSE
        [Test]
        public void TestEqualsObject()
        {
            Assert.That(EqualityComparer<object>.Default.Equals(new HitSampleInfo("a"), new HitSampleInfo("a")), Is.True);
            Assert.That(EqualityComparer<object>.Default.Equals(new HitSampleInfo("a"), "a"), Is.False);
        }
    }
}
