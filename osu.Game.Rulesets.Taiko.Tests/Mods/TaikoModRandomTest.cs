// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Taiko.Beatmaps;
using osu.Game.Rulesets.Taiko.Mods;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Mods
{
    [TestFixture]
    public class TaikoModRandomTest
    {
        private static TaikoBeatmap create(int hits)
        {
            var beatmap = new TaikoBeatmap();
            for (int i = 0; i < hits; i++)
                beatmap.HitObjects.Add(new Hit { StartTime = i * 100 });
            return beatmap;
        }

        // CF: TaikoModRandom.ApplyToBeatmap | Seed.Value ??= RNG.Next() | non-null
        // CF: TaikoModRandom.ApplyToBeatmap | foreach taikoBeatmap.HitObjects | loop n
        // CF: TaikoModRandom.ApplyToBeatmap | obj is Hit hit | TRUE
        // CF: TaikoModRandom.ApplyToBeatmap | rng.Next(2) == 0 | TRUE
        // CF: TaikoModRandom.ApplyToBeatmap | rng.Next(2) == 0 | FALSE
        [Test]
        public void TestSeedDeterminesColours()
        {
            var mod = new TaikoModRandom();
            mod.Seed.Value = 1234;
            var beatmap = create(30);
            mod.ApplyToBeatmap(beatmap);

            var expectedRng = new Random(1234);
            var expected = Enumerable.Range(0, 30).Select(_ => expectedRng.Next(2) == 0 ? HitType.Centre : HitType.Rim).ToArray();

            Assert.That(beatmap.HitObjects.Cast<Hit>().Select(h => h.Type), Is.EqualTo(expected));
            Assert.That(expected, Does.Contain(HitType.Centre).And.Contain(HitType.Rim));
        }

        // CF: TaikoModRandom.ApplyToBeatmap | Seed.Value ??= RNG.Next() | null
        [Test]
        public void TestMissingSeedIsGenerated()
        {
            var mod = new TaikoModRandom();
            mod.ApplyToBeatmap(create(1));
            Assert.That(mod.Seed.Value, Is.Not.Null);
        }

        // CF: TaikoModRandom.ApplyToBeatmap | foreach taikoBeatmap.HitObjects | loop 0
        [Test]
        public void TestEmptyBeatmap()
        {
            var mod = new TaikoModRandom();
            mod.Seed.Value = 1;
            Assert.That(() => mod.ApplyToBeatmap(new TaikoBeatmap()), Throws.Nothing);
        }

        // CF: TaikoModRandom.ApplyToBeatmap | obj is Hit hit | FALSE
        [Test]
        public void TestNonHitUntouched()
        {
            var mod = new TaikoModRandom();
            mod.Seed.Value = 1;
            var beatmap = new TaikoBeatmap();
            var roll = new DrumRoll { StartTime = 5, Duration = 10 };
            beatmap.HitObjects.Add(roll);
            mod.ApplyToBeatmap(beatmap);
            Assert.That(roll.StartTime, Is.EqualTo(5));
        }

        // CF: TaikoModRandom.ApplyToBeatmap | (TaikoBeatmap)beatmap | throws InvalidCastException
        [Test]
        public void TestNonTaikoBeatmapThrows()
            => Assert.Throws<InvalidCastException>(() => new TaikoModRandom().ApplyToBeatmap(new Beatmap<HitObject>()));
    }
}
