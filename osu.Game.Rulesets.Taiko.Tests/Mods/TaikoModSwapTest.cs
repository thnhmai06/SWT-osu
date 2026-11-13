// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Taiko.Beatmaps;
using osu.Game.Rulesets.Taiko.Mods;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Mods
{
    [TestFixture]
    public class TaikoModSwapTest
    {
        // CF: TaikoModSwap.ApplyToBeatmap | foreach taikoBeatmap.HitObjects | loop 0
        [Test]
        public void TestEmptyBeatmap()
        {
            Assert.That(() => new TaikoModSwap().ApplyToBeatmap(new TaikoBeatmap()), Throws.Nothing);
        }

        // CF: TaikoModSwap.ApplyToBeatmap | foreach taikoBeatmap.HitObjects | loop 1
        // CF: TaikoModSwap.ApplyToBeatmap | obj is Hit hit | TRUE
        // CF: TaikoModSwap.ApplyToBeatmap | hit.Type == HitType.Centre | TRUE
        [Test]
        public void TestCentreBecomesRim()
        {
            var hit = new Hit { Type = HitType.Centre };
            apply(hit);
            Assert.That(hit.Type, Is.EqualTo(HitType.Rim));
        }

        // CF: TaikoModSwap.ApplyToBeatmap | foreach taikoBeatmap.HitObjects | loop n
        // CF: TaikoModSwap.ApplyToBeatmap | hit.Type == HitType.Centre | FALSE
        // CF: TaikoModSwap.ApplyToBeatmap | obj is Hit hit | FALSE
        [Test]
        public void TestRimBecomesCentreAndNonHitsUntouched()
        {
            var rim = new Hit { Type = HitType.Rim };
            var roll = new DrumRoll { StartTime = 100, Duration = 100 };
            apply(rim, roll);
            Assert.That(rim.Type, Is.EqualTo(HitType.Centre));
            Assert.That(roll.StartTime, Is.EqualTo(100));
        }

        // CF: TaikoModSwap.ApplyToBeatmap | (TaikoBeatmap)beatmap | throws InvalidCastException
        [Test]
        public void TestNonTaikoBeatmapThrows()
            => Assert.Throws<System.InvalidCastException>(() => new TaikoModSwap().ApplyToBeatmap(new Beatmap<HitObject>()));

        private static void apply(params TaikoHitObject[] objects)
        {
            var beatmap = new TaikoBeatmap();
            beatmap.HitObjects.AddRange(objects);
            new TaikoModSwap().ApplyToBeatmap(beatmap);
        }
    }
}
