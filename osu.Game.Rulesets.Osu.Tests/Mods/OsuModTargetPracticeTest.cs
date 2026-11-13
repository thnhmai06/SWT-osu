// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Mods
{
    [TestFixture]
    public class OsuModTargetPracticeTest
    {
        private static OsuBeatmap createBeatmap()
        {
            var beatmap = new OsuBeatmap { BeatmapInfo = new BeatmapInfo { Difficulty = new BeatmapDifficulty() } };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

            for (int i = 0; i < 16; i++)
            {
                var c = new HitCircle { StartTime = 1000 + i * 500, NewCombo = i % 4 == 0, Position = new Vector2(100 + i * 10, 100) };
                c.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);
                beatmap.HitObjects.Add(c);
            }

            return beatmap;
        }

        // CF: OsuModTargetPractice.ApplyToDifficulty | difficulty.ApproachRate *= 0.5f | non-null
        [Test]
        public void TestApproachRateIsHalved()
        {
            var difficulty = new BeatmapDifficulty { ApproachRate = 9 };
            new OsuModTargetPractice().ApplyToDifficulty(difficulty);

            Assert.That(difficulty.ApproachRate, Is.EqualTo(4.5f));
        }

        [Test]
        public void TestSuddenDeathBehaviour()
        {
            var mod = new OsuModTargetPractice();

            Assert.That(mod.PerformFail(), Is.True);
            Assert.That(mod.RestartOnFail, Is.False);
        }

        // CF: OsuModTargetPractice.ApplyToBeatmap | osuBeatmap.HitObjects.Count == 0 | TRUE
        [Test]
        public void TestEmptyBeatmapIsLeftUntouched()
        {
            var beatmap = new OsuBeatmap { BeatmapInfo = new BeatmapInfo { Difficulty = new BeatmapDifficulty() } };
            new OsuModTargetPractice { Seed = { Value = 1 } }.ApplyToBeatmap(beatmap);

            Assert.That(beatmap.HitObjects, Is.Empty);
        }

        // CF: OsuModTargetPractice.ApplyToBeatmap | osuBeatmap.HitObjects.Count == 0 | FALSE
        [Test]
        public void TestObjectsAreReplacedByOrderedCirclesInsidePlayfield()
        {
            var beatmap = createBeatmap();
            new OsuModTargetPractice { Seed = { Value = 1234 } }.ApplyToBeatmap(beatmap);

            Assert.That(beatmap.HitObjects, Is.Not.Empty);
            Assert.That(beatmap.HitObjects, Has.All.TypeOf<HitCircle>());
            Assert.That(beatmap.HitObjects.Select(h => h.StartTime), Is.Ordered);

            foreach (var h in beatmap.HitObjects)
            {
                Assert.That(h.Position.X, Is.InRange(0, 512));
                Assert.That(h.Position.Y, Is.InRange(0, 384));
            }
        }

        [Test]
        public void TestSameSeedGivesSameLayoutAndDifferentSeedDiffers()
        {
            var a = createBeatmap();
            var b = createBeatmap();
            var c = createBeatmap();

            new OsuModTargetPractice { Seed = { Value = 42 } }.ApplyToBeatmap(a);
            new OsuModTargetPractice { Seed = { Value = 42 } }.ApplyToBeatmap(b);
            new OsuModTargetPractice { Seed = { Value = 43 } }.ApplyToBeatmap(c);

            Assert.That(a.HitObjects.Select(h => h.Position), Is.EqualTo(b.HitObjects.Select(h => h.Position)));
            Assert.That(a.HitObjects.Select(h => h.Position), Is.Not.EqualTo(c.HitObjects.Select(h => h.Position)));
        }

        // CF: OsuModTargetPractice.ApplyToBeatmap | Seed.Value ??= RNG.Next() | non-null
        [Test]
        public void TestSeedIsGeneratedWhenMissing()
        {
            var mod = new OsuModTargetPractice();
            mod.ApplyToBeatmap(createBeatmap());

            Assert.That(mod.Seed.Value, Is.Not.Null);
        }
    }
}
