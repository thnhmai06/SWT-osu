// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Catch.Tests.Difficulty.Preprocessing
{
    [TestFixture]
    public class CatchDifficultyHitObjectTest
    {
        private static Fruit fruit(double time, float x)
        {
            var f = new Fruit { StartTime = time, X = x };
            f.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            return f;
        }

        private static CatchDifficultyHitObject create(Fruit current, Fruit last, double clockRate = 1, float halfCatcherWidth = 41, List<DifficultyHitObject>? list = null)
        {
            list ??= new List<DifficultyHitObject>();
            var obj = new CatchDifficultyHitObject(current, last, clockRate, halfCatcherWidth, list, list.Count);
            list.Add(obj);
            return obj;
        }

        // CF: CatchDifficultyHitObject.setMovementState | Index == 0 | TRUE
        [Test]
        public void TestFirstObjectStartsFromLastObjectPosition()
        {
            var o = create(fruit(100, 200), fruit(0, 100));

            Assert.That(o.LastPlayerPosition, Is.EqualTo(100));
            // the catcher moves as little as possible: just enough to be within 41 - 16 = 25 of the object.
            Assert.That(o.PlayerPosition, Is.EqualTo(175));
            Assert.That(o.DistanceMoved, Is.EqualTo(75));
            Assert.That(o.ExactDistanceMoved, Is.EqualTo(100));
        }

        [Test]
        public void TestNoMovementWhenAlreadyWithinRange()
        {
            var o = create(fruit(100, 110), fruit(0, 100));

            Assert.That(o.DistanceMoved, Is.Zero);
            Assert.That(o.ExactDistanceMoved, Is.EqualTo(10));
        }

        [Test]
        public void TestLeftwardMovementIsNegative()
        {
            Assert.That(create(fruit(100, 0), fruit(0, 100)).DistanceMoved, Is.EqualTo(-75));
        }

        // CF: CatchDifficultyHitObject.setMovementState | Index == 0 | FALSE
        [Test]
        public void TestLaterObjectsContinueFromPreviousPlayerPosition()
        {
            var list = new List<DifficultyHitObject>();
            var first = create(fruit(100, 200), fruit(0, 100), list: list);
            var second = create(fruit(200, 300), fruit(100, 200), list: list);

            Assert.That(second.LastPlayerPosition, Is.EqualTo(first.PlayerPosition));
            Assert.That(second.PlayerPosition, Is.EqualTo(275));
        }

        // CF: CatchDifficultyHitObject.setMovementState | LastObject.HyperDash | TRUE
        [Test]
        public void TestPlayerSnapsToObjectAfterHyperDash()
        {
            var last = fruit(0, 100);
            last.HyperDashTarget = fruit(100, 400);

            var o = create(fruit(100, 400), last);

            Assert.That(o.PlayerPosition, Is.EqualTo(400));
            Assert.That(o.DistanceMoved, Is.EqualTo(275)); // movement is still measured before snapping.
        }

        // CF: CatchDifficultyHitObject.setMovementState | LastObject.HyperDash | FALSE
        [Test]
        public void TestPlayerDoesNotSnapWithoutHyperDash()
        {
            Assert.That(create(fruit(100, 400), fruit(0, 100)).PlayerPosition, Is.EqualTo(375));
        }

        [TestCase(10, 1, 40)]
        [TestCase(100, 1, 100)]
        [TestCase(100, 2, 50)]
        [TestCase(30, 2, 40)]
        public void TestStrainTimeHasFortyMillisecondMinimum(double deltaTime, double clockRate, double expected)
        {
            Assert.That(create(fruit(deltaTime, 0), fruit(0, 0), clockRate).StrainTime, Is.EqualTo(expected));
        }

        [Test]
        public void TestPositionsAreNormalisedByCatcherWidth()
        {
            var o = create(fruit(100, 200), fruit(0, 100), halfCatcherWidth: 82);

            Assert.That(o.NormalizedPosition, Is.EqualTo(100));
            Assert.That(o.LastNormalizedPosition, Is.EqualTo(50));
        }
    }
}
