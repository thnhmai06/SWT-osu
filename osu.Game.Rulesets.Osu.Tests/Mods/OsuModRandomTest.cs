// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Mods
{
    [TestFixture]
    public class OsuModRandomTest
    {
        private static OsuBeatmap createBeatmap(bool sliders = false)
        {
            var beatmap = new OsuBeatmap { BeatmapInfo = new BeatmapInfo { Difficulty = new BeatmapDifficulty() } };

            for (int i = 0; i < 30; i++)
            {
                OsuHitObject obj;

                if (sliders && i % 3 == 1)
                {
                    obj = new Slider
                    {
                        StartTime = 1000 + i * 300,
                        Position = new Vector2(100 + i * 10, 100 + (i % 5) * 30),
                        Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(80, 30) }),
                    };
                }
                else
                    obj = new HitCircle { StartTime = 1000 + i * 300, Position = new Vector2(100 + i * 10, 100 + (i % 5) * 30) };

                obj.NewCombo = i % 6 == 0;
                obj.ApplyDefaults(new ControlPointInfo(), beatmap.Difficulty);
                beatmap.HitObjects.Add(obj);
            }

            return beatmap;
        }

        private static Vector2[] positions(OsuBeatmap b) => b.HitObjects.Select(h => h.Position).ToArray();

        // CF: OsuModRandom.ApplyToBeatmap | beatmap is not OsuBeatmap osuBeatmap | TRUE
        [Test]
        public void TestNonOsuBeatmapIsIgnored()
        {
            var generic = new Beatmap();
            Assert.DoesNotThrow(() => new OsuModRandom().ApplyToBeatmap(generic));
        }

        // CF: OsuModRandom.ApplyToBeatmap | beatmap is not OsuBeatmap osuBeatmap | FALSE
        // CF: OsuModRandom.ApplyToBeatmap | for (i < positionInfos.Count) | loop n
        // CF: OsuModRandom.ApplyToBeatmap | i == 0 | TRUE
        // CF: OsuModRandom.ApplyToBeatmap | i == 0 | FALSE
        [Test]
        public void TestPositionsAreChangedButStayInsidePlayfield()
        {
            var beatmap = createBeatmap();
            var before = positions(beatmap);

            new OsuModRandom { Seed = { Value = 7 } }.ApplyToBeatmap(beatmap);

            Assert.That(positions(beatmap), Is.Not.EqualTo(before));

            foreach (var h in beatmap.HitObjects)
            {
                Assert.That(h.Position.X, Is.InRange(0, 512));
                Assert.That(h.Position.Y, Is.InRange(0, 384));
            }
        }

        // CF: OsuModRandom.ApplyToBeatmap | for (i < positionInfos.Count) | loop 0
        [Test]
        public void TestEmptyBeatmap()
        {
            var beatmap = new OsuBeatmap();
            Assert.DoesNotThrow(() => new OsuModRandom { Seed = { Value = 1 } }.ApplyToBeatmap(beatmap));
        }

        [Test]
        public void TestSameSeedIsDeterministic()
        {
            var a = createBeatmap(sliders: true);
            var b = createBeatmap(sliders: true);
            var c = createBeatmap(sliders: true);

            new OsuModRandom { Seed = { Value = 99 } }.ApplyToBeatmap(a);
            new OsuModRandom { Seed = { Value = 99 } }.ApplyToBeatmap(b);
            new OsuModRandom { Seed = { Value = 100 } }.ApplyToBeatmap(c);

            Assert.That(positions(a), Is.EqualTo(positions(b)));
            Assert.That(positions(a), Is.Not.EqualTo(positions(c)));
        }

        // CF: OsuModRandom.ApplyToBeatmap | positionInfos[i].HitObject is Slider slider && random.NextDouble() < 0.5 | TRUE
        [Test]
        public void TestSomeSlidersAreFlipped()
        {
            bool flippedAny = false;

            for (int seed = 0; seed < 20 && !flippedAny; seed++)
            {
                var beatmap = createBeatmap(sliders: true);
                new OsuModRandom { Seed = { Value = seed } }.ApplyToBeatmap(beatmap);

                flippedAny = beatmap.HitObjects.OfType<Slider>().Any(s => s.Path.ControlPoints[1].Position.X < 0);
            }

            Assert.That(flippedAny, Is.True);
        }

        // CF: OsuModRandom.ApplyToBeatmap | Seed.Value ??= RNG.Next() | non-null
        [Test]
        public void TestSeedIsGeneratedWhenMissing()
        {
            var mod = new OsuModRandom();
            mod.ApplyToBeatmap(createBeatmap());

            Assert.That(mod.Seed.Value, Is.Not.Null);
        }

        [Test]
        public void TestIncompatibleWithTargetPractice()
        {
            Assert.That(new OsuModRandom().IncompatibleMods, Does.Contain(typeof(OsuModTargetPractice)));
        }
    }
}
