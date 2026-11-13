// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Catch.Beatmaps;
using osu.Game.Rulesets.Catch.Mods;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Rulesets.Catch.Tests.Mods
{
    [TestFixture]
    public class CatchModMirrorTest
    {
        private static CatchBeatmap apply(params CatchHitObject[] hitObjects)
        {
            var beatmap = new CatchBeatmap();
            beatmap.Difficulty.SliderMultiplier = 1;
            beatmap.HitObjects.AddRange(hitObjects);
            foreach (var h in hitObjects)
                h.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);
            new CatchModMirror().ApplyToBeatmap(beatmap);
            return beatmap;
        }

        // CF: CatchModMirror.applyToHitObject | switch catchObject | case Fruit
        [Test]
        public void TestFruitIsMirrored()
        {
            var fruit = new Fruit { X = 100, XOffset = 7 };

            apply(fruit);

            Assert.That(fruit.OriginalX, Is.EqualTo(CatchPlayfield.WIDTH - 100));
            Assert.That(fruit.XOffset, Is.EqualTo(-7));
        }

        // CF: CatchModMirror.applyToHitObject | switch catchObject | case JuiceStream
        // CF: CatchModMirror.mirrorEffectiveX | foreach nested | loop n
        // CF: CatchModMirror.mirrorJuiceStreamPath | foreach point | loop n
        [Test]
        public void TestJuiceStreamAndNestedObjectsAndPathAreMirrored()
        {
            var stream = new JuiceStream
            {
                StartTime = 0,
                X = 100,
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(Vector2.Zero, PathType.LINEAR),
                    new PathControlPoint(new Vector2(60, 40)),
                }, 70),
            };
            var beatmap = apply(stream);

            Assert.That(stream.OriginalX, Is.EqualTo(CatchPlayfield.WIDTH - 100));
            Assert.That(stream.Path.ControlPoints[1].Position, Is.EqualTo(new Vector2(-60, 40)));
            Assert.That(stream.Path.ControlPoints[0].Type, Is.EqualTo(PathType.LINEAR));
            Assert.That(stream.Path.ExpectedDistance.Value, Is.EqualTo(70));
            Assert.That(beatmap.HitObjects, Has.Count.EqualTo(1));
        }

        // CF: CatchModMirror.mirrorEffectiveX | foreach nested | loop n (nested positions mirrored)
        [Test]
        public void TestNestedObjectPositionsAreMirrored()
        {
            var stream = new JuiceStream
            {
                StartTime = 0,
                X = 100,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(50, 0) }),
            };
            stream.ApplyDefaults(new osu.Game.Beatmaps.ControlPoints.ControlPointInfo(), new osu.Game.Beatmaps.BeatmapDifficulty { SliderMultiplier = 1 });
            var head = stream.NestedHitObjects.OfType<Fruit>().First();
            float headX = head.OriginalX;

            var beatmap = new CatchBeatmap();
            beatmap.HitObjects.Add(stream);
            new CatchModMirror().ApplyToBeatmap(beatmap);

            Assert.That(head.OriginalX, Is.EqualTo(CatchPlayfield.WIDTH - headX));
        }

        // CF: CatchModMirror.applyToHitObject | switch catchObject | case BananaShower
        // CF: CatchModMirror.mirrorBananaShower | foreach banana | loop n
        [Test]
        public void TestBananaOffsetsAreMirrored()
        {
            var shower = new BananaShower { StartTime = 0, Duration = 200 };
            shower.ApplyDefaults(new osu.Game.Beatmaps.ControlPoints.ControlPointInfo(), new osu.Game.Beatmaps.BeatmapDifficulty());
            var bananas = shower.NestedHitObjects.Cast<Banana>().ToArray();
            bananas[0].XOffset = 100;

            var beatmap = new CatchBeatmap();
            beatmap.HitObjects.Add(shower);
            new CatchModMirror().ApplyToBeatmap(beatmap);

            Assert.That(bananas[0].XOffset, Is.EqualTo(CatchPlayfield.WIDTH - 100));
        }

        // CF: CatchModMirror.applyToHitObject | switch catchObject | default
        // CF: CatchModMirror.ApplyToBeatmap | foreach hitObject | loop 0
        [Test]
        public void TestUnhandledTopLevelObjectsAndEmptyBeatmapAreUntouched()
        {
            var droplet = new Droplet { X = 100 };
            apply(droplet);
            Assert.That(droplet.OriginalX, Is.EqualTo(100));

            Assert.DoesNotThrow(() => new CatchModMirror().ApplyToBeatmap(new CatchBeatmap()));
        }
    }
}
