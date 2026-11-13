// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Catch.Beatmaps;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.Replays;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Rulesets.Catch.Tests.Replays
{
    [TestFixture]
    public class CatchAutoGeneratorTest
    {
        private static CatchReplayFrame[] generate(params CatchHitObject[] hitObjects)
        {
            var beatmap = new CatchBeatmap();
            beatmap.Difficulty.SliderMultiplier = 1;
            beatmap.HitObjects.AddRange(hitObjects);
            foreach (var h in hitObjects)
                h.ApplyDefaults(new ControlPointInfo(), beatmap.Difficulty);

            return new CatchAutoGenerator(beatmap).Generate().Frames.Cast<CatchReplayFrame>().ToArray();
        }

        private static Fruit fruit(double time, float x) => new Fruit { StartTime = time, X = x };

        // CF: CatchAutoGenerator.GenerateFrames | Beatmap.HitObjects.Count == 0 | TRUE
        [Test]
        public void TestEmptyBeatmapHasNoFrames()
        {
            Assert.That(generate(), Is.Empty);
        }

        // CF: CatchAutoGenerator.GenerateFrames | positionChange == 0 | TRUE
        // CF: CatchAutoGenerator.moveToNext | lastPosition - half < x && lastPosition + half > x | TRUE
        [Test]
        public void TestObjectAlreadyUnderCatcherOnlyHoldsPosition()
        {
            var frames = generate(fruit(1000, CatchPlayfield.CENTER_X));

            Assert.That(frames.Select(f => (f.Time, f.Position)), Is.EqualTo(new[] { (1000.0, CatchPlayfield.CENTER_X) }));
        }

        // CF: CatchAutoGenerator.moveToNext | lastPosition - half < x && lastPosition + half > x | FALSE
        // CF: CatchAutoGenerator.moveToNext | impossibleJump | FALSE
        // CF: CatchAutoGenerator.moveToNext | h.HyperDash | FALSE
        // CF: CatchAutoGenerator.moveToNext | dashRequired | FALSE
        // CF: CatchAutoGenerator.moveToNext | positionChange == 0 | FALSE
        [Test]
        public void TestWalkableMovementStartsEarlyEnoughAtWalkSpeed()
        {
            float target = CatchPlayfield.CENTER_X + 200;
            var frames = generate(fruit(1000, target));

            // 200px at 0.5px/ms takes 400ms.
            Assert.That(frames.Select(f => (f.Time, f.Position)), Is.EqualTo(new[] { (600.0, CatchPlayfield.CENTER_X), (1000.0, target) }));
            Assert.That(frames.Select(f => f.Dashing), Is.All.False);
        }

        // CF: CatchAutoGenerator.moveToNext | dashRequired | TRUE
        [Test]
        public void TestFastMovementDashesFirst()
        {
            float target = CatchPlayfield.CENTER_X + 240;
            var frames = generate(fruit(300, target));

            Assert.That(frames, Has.Length.EqualTo(3));
            Assert.That(frames[0].Dashing, Is.True);
            Assert.That(frames[0].Time, Is.EqualTo(1));
            Assert.That(frames[^1].Time, Is.EqualTo(300));
            Assert.That(frames[^1].Position, Is.EqualTo(target));
            Assert.That(frames[1].Position, Is.InRange(CatchPlayfield.CENTER_X, target));
        }

        // CF: CatchAutoGenerator.moveToNext | impossibleJump | TRUE
        [Test]
        public void TestImpossibleJumpTeleportsOnObject()
        {
            float target = CatchPlayfield.CENTER_X + 240;
            var frames = generate(fruit(100, target));

            Assert.That(frames.Select(f => (f.Time, f.Position)), Is.EqualTo(new[] { (100.0, target) }));
        }

        // CF: CatchAutoGenerator.moveToNext | h.HyperDash | TRUE
        [Test]
        public void TestHyperDashStartsImmediatelyAndMovesInstantlyOnArrival()
        {
            float target = CatchPlayfield.CENTER_X + 240;
            var hyper = fruit(300, target);
            hyper.HyperDashTarget = fruit(1000, 0);

            var frames = generate(hyper);

            Assert.That(frames.Select(f => (f.Time, f.Position)), Is.EqualTo(new[] { (0.0, CatchPlayfield.CENTER_X), (300.0, target) }));
        }

        // CF: CatchAutoGenerator.moveToNext | timeAvailable < 0 | TRUE
        // CF: CatchAutoGenerator.GenerateFrames | foreach obj | loop n
        [Test]
        public void TestObjectBeforePreviousOneIsSkipped()
        {
            var frames = generate(fruit(1000, CatchPlayfield.CENTER_X + 200), fruit(500, 50));

            Assert.That(frames.Last().Position, Is.EqualTo(CatchPlayfield.CENTER_X + 200));
            Assert.That(frames.Select(f => f.Position), Does.Not.Contain(50f));
        }

        // CF: CatchAutoGenerator.moveToNext | timeAvailable < 0 | FALSE
        [Test]
        public void TestObjectsAtSameTimeAreNotSkipped()
        {
            var frames = generate(fruit(1000, CatchPlayfield.CENTER_X), fruit(1000, CatchPlayfield.CENTER_X));
            Assert.That(frames, Has.Length.EqualTo(2));
        }

        // CF: CatchAutoGenerator.GenerateFrames | obj is PalpableCatchHitObject | FALSE
        // CF: CatchAutoGenerator.GenerateFrames | nestedObj is PalpableCatchHitObject | TRUE
        // CF: CatchAutoGenerator.GenerateFrames | foreach nestedObj | loop n
        [Test]
        public void TestJuiceStreamIsFollowedThroughNestedObjects()
        {
            var stream = new JuiceStream
            {
                StartTime = 0,
                X = CatchPlayfield.CENTER_X,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }),
            };

            var frames = generate(stream);

            // the stream is not itself catchable; its nested head, droplets and tail are.
            Assert.That(frames.Select(f => f.Time).Distinct().Count(), Is.GreaterThan(2));
            Assert.That(frames.Last().Position, Is.EqualTo(CatchPlayfield.CENTER_X + 100).Within(1e-3));
        }

        // CF: CatchAutoGenerator.GenerateFrames | foreach nestedObj | loop 0
        // CF: CatchAutoGenerator.GenerateFrames | obj is PalpableCatchHitObject | TRUE
        [Test]
        public void TestFruitWithoutNestedObjects()
        {
            Assert.That(generate(fruit(0, CatchPlayfield.CENTER_X)), Has.Length.EqualTo(1));
        }
    }
}
