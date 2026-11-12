// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Tests.Difficulty;
using osu.Game.Rulesets.Osu.UI;

namespace osu.Game.Rulesets.Osu.Tests.Mods
{
    [TestFixture]
    public class OsuModRandomTest
    {
        private static OsuModRandom mod(int seed) => new OsuModRandom { Seed = { Value = seed } };

        private static IBeatmap beatmap() => DifficultyTestHelpers.Playable(DifficultyTestHelpers.Stream(20, 300, 60));

        // `beatmap is not OsuBeatmap` TRUE: nothing happens, no throw.
        [Test]
        public void TestNonOsuBeatmap_IsIgnored()
        {
            var plain = new Beatmap();

            Assert.DoesNotThrow(() => mod(1).ApplyToBeatmap(plain));
        }

        // Same seed twice -> identical positions (determinism).
        [Test]
        public void TestSameSeed_IsDeterministic()
        {
            var a = beatmap();
            var b = beatmap();
            mod(42).ApplyToBeatmap(a);
            mod(42).ApplyToBeatmap(b);

            Assert.That(a.HitObjects.Cast<OsuHitObject>().Select(h => h.Position), Is.EqualTo(b.HitObjects.Cast<OsuHitObject>().Select(h => h.Position)));
        }

        // Objects remain inside the playfield after randomisation.
        [Test]
        public void TestPositions_StayInsidePlayfield()
        {
            var map = beatmap();
            mod(7).ApplyToBeatmap(map);

            foreach (var h in map.HitObjects.Cast<OsuHitObject>())
            {
                Assert.That(h.Position.X, Is.InRange(0f, OsuPlayfield.BASE_SIZE.X));
                Assert.That(h.Position.Y, Is.InRange(0f, OsuPlayfield.BASE_SIZE.Y));
            }
        }
    }
}
