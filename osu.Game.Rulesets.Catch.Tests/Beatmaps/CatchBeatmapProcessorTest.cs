// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Beatmaps;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Rulesets.Catch.Tests.Beatmaps
{
    [TestFixture]
    public class CatchBeatmapProcessorTest
    {
        private static CatchBeatmap createBeatmap(params CatchHitObject[] hitObjects)
        {
            var beatmap = new CatchBeatmap();
            beatmap.Difficulty.SliderMultiplier = 1;
            beatmap.HitObjects.AddRange(hitObjects);
            foreach (var h in hitObjects)
                h.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);
            return beatmap;
        }

        private static Fruit fruit(double time, float x) => new Fruit { StartTime = time, X = x };

        private static void offsets(CatchBeatmap beatmap, bool hardRock = true)
            => new CatchBeatmapProcessor(beatmap) { HardRockOffsets = hardRock }.ApplyPositionOffsets(beatmap);

        // CF: CatchBeatmapProcessor.PreProcess | obj is not BananaShower | TRUE
        // CF: CatchBeatmapProcessor.PreProcess | lastObj == null | TRUE
        // CF: CatchBeatmapProcessor.PreProcess | lastObj == null | FALSE
        // CF: CatchBeatmapProcessor.PreProcess | lastObj is BananaShower | TRUE
        // CF: CatchBeatmapProcessor.PreProcess | lastObj is BananaShower | FALSE
        // CF: CatchBeatmapProcessor.PreProcess | obj is not BananaShower | FALSE
        // CF: CatchBeatmapProcessor.PreProcess | foreach IHasComboInformation | loop n
        [Test]
        public void TestFirstObjectAndObjectAfterBananaShowerStartNewCombo()
        {
            var first = fruit(0, 0);
            var second = fruit(100, 0);
            var shower = new BananaShower { StartTime = 200, Duration = 100 };
            var afterShower = fruit(400, 0);
            var beatmap = createBeatmap(first, second, shower, afterShower);

            new CatchBeatmapProcessor(beatmap).PreProcess();

            Assert.That(first.NewCombo, Is.True);
            Assert.That(second.NewCombo, Is.False);
            Assert.That(shower.NewCombo, Is.False);
            Assert.That(afterShower.NewCombo, Is.True);
        }

        // CF: CatchBeatmapProcessor.PreProcess | foreach IHasComboInformation | loop 0
        [Test]
        public void TestPreProcessEmptyBeatmap()
        {
            Assert.DoesNotThrow(() => new CatchBeatmapProcessor(createBeatmap()).PreProcess());
        }

        // CF: CatchBeatmapProcessor.PostProcess | foreach CatchHitObject | loop n
        // CF: CatchBeatmapProcessor.PostProcess | obj.LastInCombo | TRUE
        // CF: CatchBeatmapProcessor.PostProcess | obj.LastInCombo && NestedHitObjects.LastOrDefault() is IHasComboInformation | TRUE
        // CF: CatchBeatmapProcessor.PostProcess | obj.LastInCombo | FALSE
        [Test]
        public void TestPostProcessAssignsIndicesAndPropagatesLastInCombo()
        {
            var stream = new JuiceStream
            {
                StartTime = 0,
                NewCombo = true,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }),
            };
            var next = fruit(2000, 0);
            next.NewCombo = true;
            var beatmap = createBeatmap(stream, next);

            var processor = new CatchBeatmapProcessor(beatmap);
            processor.PreProcess(); // computes combo information, including LastInCombo.
            processor.PostProcess();

            Assert.That(stream.IndexInBeatmap, Is.EqualTo(0));
            Assert.That(next.IndexInBeatmap, Is.EqualTo(1));
            Assert.That(stream.NestedHitObjects.OfType<CatchHitObject>().Select(n => n.IndexInBeatmap), Is.All.EqualTo(0));
            Assert.That(stream.LastInCombo, Is.True);
            Assert.That(((IHasComboInformation)stream.NestedHitObjects.Last()).LastInCombo, Is.True);
            Assert.That(((IHasComboInformation)stream.NestedHitObjects.First()).LastInCombo, Is.False);
        }

        // CF: CatchBeatmapProcessor.PostProcess | obj.LastInCombo && NestedHitObjects.LastOrDefault() is IHasComboInformation | FALSE (no nested objects)
        [Test]
        public void TestPostProcessFruitWithoutNestedObjects()
        {
            var a = fruit(0, 10);
            var beatmap = createBeatmap(a);

            Assert.DoesNotThrow(() => new CatchBeatmapProcessor(beatmap).PostProcess());
            Assert.That(a.IndexInBeatmap, Is.EqualTo(0));
        }

        // CF: CatchBeatmapProcessor.ApplyPositionOffsets | case Fruit; HardRockOffsets | FALSE
        [Test]
        public void TestOffsetsAreResetAndNotAppliedWithoutHardRock()
        {
            var a = fruit(0, 100);
            var b = fruit(300, 110);
            b.XOffset = 5;

            offsets(createBeatmap(a, b), hardRock: false);

            Assert.That(b.XOffset, Is.Zero);
        }

        // CF: CatchBeatmapProcessor.applyHardRockOffset | lastPosition == null | TRUE
        // CF: CatchBeatmapProcessor.applyHardRockOffset | positionDiff == 0 | FALSE
        // CF: CatchBeatmapProcessor.applyHardRockOffset | Math.Abs(positionDiff) < timeDiff / 3 | TRUE
        // CF: CatchBeatmapProcessor.applyOffset | amount > 0 | TRUE
        // CF: CatchBeatmapProcessor.applyOffset | position + amount < WIDTH | TRUE
        [Test]
        public void TestCloseRightwardJumpIsExaggerated()
        {
            var a = fruit(0, 100);
            var b = fruit(300, 110);

            offsets(createBeatmap(a, b));

            Assert.That(a.XOffset, Is.Zero);
            Assert.That(b.XOffset, Is.EqualTo(10));
        }

        // CF: CatchBeatmapProcessor.applyOffset | amount > 0 | FALSE
        // CF: CatchBeatmapProcessor.applyOffset | position + amount > 0 | TRUE
        [Test]
        public void TestCloseLeftwardJumpIsExaggerated()
        {
            var b = fruit(300, 90);

            offsets(createBeatmap(fruit(0, 100), b));

            Assert.That(b.XOffset, Is.EqualTo(-10));
        }

        // CF: CatchBeatmapProcessor.applyOffset | position + amount < WIDTH | FALSE
        [Test]
        public void TestRightwardOffsetIsNotAppliedBeyondPlayfield()
        {
            var b = fruit(300, 510);

            offsets(createBeatmap(fruit(0, 500), b));

            Assert.That(b.XOffset, Is.Zero);
        }

        // CF: CatchBeatmapProcessor.applyOffset | position + amount > 0 | FALSE
        [Test]
        public void TestLeftwardOffsetIsNotAppliedBeyondPlayfield()
        {
            var b = fruit(300, 5);

            offsets(createBeatmap(fruit(0, 10), b));

            Assert.That(b.XOffset, Is.Zero);
        }

        // CF: CatchBeatmapProcessor.applyHardRockOffset | Math.Abs(positionDiff) < timeDiff / 3 | FALSE
        [Test]
        public void TestDistantJumpIsNotOffset()
        {
            var b = fruit(300, 300);

            offsets(createBeatmap(fruit(0, 100), b));

            Assert.That(b.XOffset, Is.Zero);
        }

        // CF: CatchBeatmapProcessor.applyHardRockOffset | lastPosition == 0 | TRUE
        [Test]
        public void TestObjectAfterZeroPositionIsTreatedAsFirst()
        {
            var b = fruit(300, 5);
            var c = fruit(600, 5);

            offsets(createBeatmap(fruit(0, 0), b, c));

            // b is treated as a first object (matches stable), so it is not offset.
            Assert.That(b.XOffset, Is.Zero);
        }

        // CF: CatchBeatmapProcessor.applyHardRockOffset | timeDiff > 1000 | TRUE
        [Test]
        public void TestLongGapResetsReference()
        {
            var b = fruit(5000, 100);

            offsets(createBeatmap(fruit(0, 100), b));

            Assert.That(b.XOffset, Is.Zero);
        }

        // CF: CatchBeatmapProcessor.applyHardRockOffset | timeDiff > 1000 | FALSE
        // CF: CatchBeatmapProcessor.applyHardRockOffset | positionDiff == 0 | TRUE
        [TestCase(1)]
        [TestCase(511)]
        [TestCase(256)]
        public void TestIdenticalPositionsGetBoundedRandomOffset(float x)
        {
            var b = fruit(300, x);

            offsets(createBeatmap(fruit(0, x), b));

            // maximum offset is min(20, timeDiff / 4) = 20 here, and the result must stay within the playfield.
            Assert.That(Math.Abs(b.XOffset), Is.LessThanOrEqualTo(20));
            Assert.That(b.EffectiveX, Is.InRange(0f, CatchPlayfield.WIDTH));
            Assert.That(b.XOffset, Is.Not.Zero.Or.Zero); // may be zero when the random draw is zero.
        }

        [Test]
        public void TestRandomOffsetIsDeterministic()
        {
            float run()
            {
                var b = fruit(300, 256);
                offsets(createBeatmap(fruit(0, 256), b));
                return b.XOffset;
            }

            Assert.That(run(), Is.EqualTo(run()));
        }

        // CF: CatchBeatmapProcessor.ApplyPositionOffsets | switch obj | case BananaShower
        // CF: CatchBeatmapProcessor.ApplyPositionOffsets | foreach banana | loop n
        [Test]
        public void TestBananasGetDeterministicOffsetsWithinPlayfield()
        {
            CatchBeatmap create() => createBeatmap(new BananaShower { StartTime = 0, Duration = 500 });

            var first = create();
            var second = create();
            offsets(first, hardRock: false);
            offsets(second, hardRock: false);

            var firstOffsets = first.HitObjects[0].NestedHitObjects.Cast<Banana>().Select(b => b.XOffset).ToArray();
            var secondOffsets = second.HitObjects[0].NestedHitObjects.Cast<Banana>().Select(b => b.XOffset).ToArray();

            Assert.That(firstOffsets, Is.Not.Empty);
            Assert.That(firstOffsets, Is.EqualTo(secondOffsets));
            Assert.That(firstOffsets, Is.All.InRange(0f, (float)CatchPlayfield.WIDTH));
            Assert.That(firstOffsets.Distinct().Count(), Is.GreaterThan(1));
        }

        // CF: CatchBeatmapProcessor.ApplyPositionOffsets | foreach banana | loop 0
        [Test]
        public void TestBananaShowerWithoutBananas()
        {
            Assert.DoesNotThrow(() => offsets(createBeatmap(new BananaShower { StartTime = 0, Duration = 0 }), hardRock: false));
        }

        // CF: CatchBeatmapProcessor.ApplyPositionOffsets | switch obj | case JuiceStream
        // CF: CatchBeatmapProcessor.ApplyPositionOffsets | catchObject is TinyDroplet | TRUE
        // CF: CatchBeatmapProcessor.ApplyPositionOffsets | catchObject is Droplet | TRUE
        // CF: CatchBeatmapProcessor.ApplyPositionOffsets | catchObject is Droplet | FALSE
        [Test]
        public void TestJuiceStreamSetsReferenceAndOffsetsTinyDroplets()
        {
            var stream = new JuiceStream
            {
                StartTime = 0,
                X = 100,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(50, 0) }),
            };
            var after = fruit(300, 160);
            var beatmap = createBeatmap(stream, after);

            offsets(beatmap);

            Assert.That(stream.NestedHitObjects.OfType<TinyDroplet>(), Is.Not.Empty);
            Assert.That(stream.NestedHitObjects.OfType<TinyDroplet>().Select(t => t.XOffset), Is.All.InRange(-20f, 20f));
            Assert.That(stream.NestedHitObjects.OfType<Droplet>().Where(d => d is not TinyDroplet).Select(d => d.XOffset), Is.All.Zero);
            Assert.That(stream.NestedHitObjects.OfType<Fruit>().Select(d => d.XOffset), Is.All.Zero);

            // the stream ends at x = 150 (start + last control point), so the fruit at 160 within 300ms is a close jump of +10.
            Assert.That(after.XOffset, Is.EqualTo(10));
        }

        // CF: CatchBeatmapProcessor.ApplyPositionOffsets | catchObject is TinyDroplet | FALSE
        [Test]
        public void TestJuiceStreamWithoutTinyDroplets()
        {
            var stream = new JuiceStream
            {
                StartTime = 0,
                X = 100,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(5, 0) }),
            };

            offsets(createBeatmap(stream));

            Assert.That(stream.NestedHitObjects.OfType<TinyDroplet>(), Is.Empty);
        }

        // CF: CatchBeatmapProcessor.initialiseHyperDash | for i < palpableObjects.Length - 1 | loop 0
        [Test]
        public void TestSingleObjectHasNoHyperDash()
        {
            var a = fruit(0, 100);
            offsets(createBeatmap(a), hardRock: false);
            Assert.That(a.HyperDashTarget, Is.Null);
        }

        // CF: CatchBeatmapProcessor.initialiseHyperDash | distanceToHyper < 0 | TRUE
        // CF: CatchBeatmapProcessor.initialiseHyperDash | for i < palpableObjects.Length - 1 | loop 1
        // CF: CatchBeatmapProcessor.initialiseHyperDash | thisDirection ternary | TRUE
        [Test]
        public void TestImpossibleJumpIsHyperDash()
        {
            var a = fruit(0, 0);
            var b = fruit(10, 512);
            offsets(createBeatmap(a, b), hardRock: false);

            Assert.That(a.HyperDashTarget, Is.SameAs(b));
            Assert.That(a.HyperDash, Is.True);
        }

        // CF: CatchBeatmapProcessor.initialiseHyperDash | distanceToHyper < 0 | FALSE
        // CF: CatchBeatmapProcessor.initialiseHyperDash | thisDirection ternary | FALSE
        [Test]
        public void TestEasyJumpIsNotHyperDashAndRecordsDistance()
        {
            var a = fruit(0, 100);
            var b = fruit(1000, 90);
            offsets(createBeatmap(a, b), hardRock: false);

            Assert.That(a.HyperDashTarget, Is.Null);
            Assert.That(a.DistanceToHyperDash, Is.GreaterThan(0));
        }

        // CF: CatchBeatmapProcessor.initialiseHyperDash | lastDirection == thisDirection | TRUE
        // CF: CatchBeatmapProcessor.initialiseHyperDash | lastDirection == thisDirection | FALSE
        // CF: CatchBeatmapProcessor.initialiseHyperDash | for i < palpableObjects.Length - 1 | loop n
        [Test]
        public void TestLeftoverSlackIsReusedOnlyForSameDirection()
        {
            const double grace = 1000f / 60f / 4;
            double half = Catcher.CalculateCatchWidth(new BeatmapDifficulty()) / 2 / Catcher.ALLOWED_CATCH_RANGE;

            // First jump (300ms) leaves exactly 5px of slack; second jump covers 150px in 500ms.
            double dx1 = 300 - grace + half - 5;
            double timeToNext2 = 500 - grace;

            // same direction (rightward, rightward): the leftover 5px excess replaces the half catcher width.
            var a = fruit(0, 0);
            var b = fruit(300, (float)dx1);
            var c = fruit(800, (float)(dx1 + 150));
            offsets(createBeatmap(a, b, c), hardRock: false);
            Assert.That(b.DistanceToHyperDash, Is.EqualTo((float)(timeToNext2 - (150 - 5))).Within(0.05));

            // direction change (leftward, rightward): the full half catcher width is used.
            var d = fruit(0, 450);
            var e = fruit(300, (float)(450 - dx1));
            var f = fruit(800, (float)(450 - dx1 + 150));
            offsets(createBeatmap(d, e, f), hardRock: false);
            Assert.That(e.DistanceToHyperDash, Is.EqualTo((float)(timeToNext2 - (150 - half))).Within(0.05));
        }

        // CF: CatchBeatmapProcessor.initialiseHyperDash | Where(h is Fruit || (h is Droplet && h is not TinyDroplet)) | filters tiny droplets and bananas
        [Test]
        public void TestTinyDropletsAndBananasAreIgnoredForHyperDash()
        {
            var a = fruit(0, 0);
            var tiny = new TinyDroplet { StartTime = 5, X = 512 };
            var banana = new Banana { StartTime = 6, X = 512 };
            var droplet = new Droplet { StartTime = 1000, X = 10 };
            var beatmap = createBeatmap(a, tiny, banana, droplet);

            offsets(beatmap, hardRock: false);

            Assert.That(a.HyperDashTarget, Is.Null);
            Assert.That(tiny.HyperDashTarget, Is.Null);
        }
    }
}
