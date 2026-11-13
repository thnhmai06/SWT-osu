// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Objects
{
    [TestFixture]
    public class HitTest
    {
        private static bool has(Hit hit, string name) => hit.Samples.Any(s => s.Name == name);

        // CF: Hit.ctor | Type == HitType.Centre | TRUE
        // CF: Hit.updateSamplesFromType | isRimType != rimSamples.Any() | FALSE
        [Test]
        public void TestDefaultIsCentre()
        {
            var hit = new Hit();
            Assert.That(hit.Type, Is.EqualTo(HitType.Centre));
            Assert.That(hit.DisplayColour.Value, Is.EqualTo(Hit.COLOUR_CENTRE));
        }

        // CF: Hit.ctor | Type == HitType.Centre | FALSE
        // CF: Hit.updateSamplesFromType | isRimType != rimSamples.Any() | TRUE
        // CF: Hit.updateSamplesFromType | isRimType | TRUE
        [Test]
        public void TestSettingRimAddsClapSample()
        {
            var hit = new Hit { Type = HitType.Rim };
            Assert.That(hit.DisplayColour.Value, Is.EqualTo(Hit.COLOUR_RIM));
            Assert.That(has(hit, HitSampleInfo.HIT_CLAP), Is.True);
        }

        // CF: Hit.updateSamplesFromType | isRimType | FALSE
        // CF: Hit.updateSamplesFromType | foreach rimSamples | loop 1
        // CF: Hit.getRimSamples | s.Name == HitSampleInfo.HIT_CLAP | TRUE
        [Test]
        public void TestSettingCentreRemovesRimSamples()
        {
            var hit = new Hit { Type = HitType.Rim };
            hit.Type = HitType.Centre;
            Assert.That(hit.Samples.Any(s => s.Name == HitSampleInfo.HIT_CLAP), Is.False);
        }

        // CF: Hit.updateSamplesFromType | foreach rimSamples | loop n
        // CF: Hit.getRimSamples | s.Name == HitSampleInfo.HIT_CLAP | FALSE
        // CF: Hit.getRimSamples | s.Name == HitSampleInfo.HIT_WHISTLE | TRUE
        // CF: Hit.updateTypeFromSamples | getRimSamples().Any() | TRUE
        [Test]
        public void TestRimSamplesWithDirectCentreAssignment()
        {
            var hit = new Hit();
            hit.Samples.Add(new HitSampleInfo(HitSampleInfo.HIT_WHISTLE));
            hit.Samples.Add(new HitSampleInfo(HitSampleInfo.HIT_CLAP));
            Assert.That(hit.Type, Is.EqualTo(HitType.Rim));

            hit.Type = HitType.Centre;
            Assert.That(hit.Samples.Where(s => s.Name is HitSampleInfo.HIT_WHISTLE or HitSampleInfo.HIT_CLAP), Is.Empty);
        }

        // CF: Hit.updateTypeFromSamples | getRimSamples().Any() | FALSE
        // CF: Hit.getRimSamples | s.Name == HitSampleInfo.HIT_WHISTLE | FALSE
        [Test]
        public void TestNonRimSampleKeepsCentre()
        {
            var hit = new Hit();
            hit.Samples.Add(new HitSampleInfo(HitSampleInfo.HIT_NORMAL));
            Assert.That(hit.Type, Is.EqualTo(HitType.Centre));
        }

        // CF: Hit.CreateStrongNestedHit | IsStrong | TRUE
        // CF: TaikoStrongableHitObject.CreateNestedHitObjects | IsStrong | TRUE
        [Test]
        public void TestStrongHitCreatesNestedStrongHit()
        {
            var hit = new Hit { StartTime = 500, IsStrong = true };
            hit.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            Assert.That(hit.NestedHitObjects.Single(), Is.TypeOf<Hit.StrongNestedHit>());
            Assert.That(hit.NestedHitObjects.Single().StartTime, Is.EqualTo(500));
        }

        // CF: TaikoStrongableHitObject.CreateNestedHitObjects | IsStrong | FALSE
        [Test]
        public void TestNonStrongHitHasNoNested()
        {
            var hit = new Hit { StartTime = 500 };
            hit.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            Assert.That(hit.NestedHitObjects, Is.Empty);
        }

        // CF: TaikoStrongableHitObject.updateSamplesFromType | IsStrongBindable.Value != strongSamples.Any() | TRUE
        // CF: TaikoStrongableHitObject.updateSamplesFromType | IsStrongBindable.Value | TRUE
        [Test]
        public void TestSettingStrongAddsFinishSample()
        {
            var hit = new Hit { IsStrong = true };
            Assert.That(has(hit, HitSampleInfo.HIT_FINISH), Is.True);
        }

        // CF: TaikoStrongableHitObject.updateSamplesFromType | IsStrongBindable.Value | FALSE
        // CF: TaikoStrongableHitObject.updateSamplesFromType | foreach strongSamples | loop 1
        [Test]
        public void TestClearingStrongRemovesFinishSample()
        {
            var hit = new Hit { IsStrong = true };
            hit.IsStrong = false;
            Assert.That(has(hit, HitSampleInfo.HIT_FINISH), Is.False);
        }

        // CF: TaikoStrongableHitObject.updateSamplesFromType | foreach strongSamples | loop n
        [Test]
        public void TestClearingStrongRemovesAllFinishSamples()
        {
            var hit = new Hit();
            hit.Samples.Add(new HitSampleInfo(HitSampleInfo.HIT_FINISH));
            hit.Samples.Add(new HitSampleInfo(HitSampleInfo.HIT_FINISH));
            hit.IsStrong = false;
            Assert.That(has(hit, HitSampleInfo.HIT_FINISH), Is.False);
        }

        // CF: TaikoStrongableHitObject.updateSamplesFromType | IsStrongBindable.Value != strongSamples.Any() | FALSE
        // CF: TaikoStrongableHitObject.updateTypeFromSamples | strongSamples.Any() | TRUE
        [Test]
        public void TestFinishSampleMakesStrong()
        {
            var hit = new Hit();
            hit.Samples.Add(new HitSampleInfo(HitSampleInfo.HIT_FINISH));
            Assert.That(hit.IsStrong, Is.True);
            Assert.That(hit.Samples.Count(s => s.Name == HitSampleInfo.HIT_FINISH), Is.EqualTo(1));
        }
    }
}
