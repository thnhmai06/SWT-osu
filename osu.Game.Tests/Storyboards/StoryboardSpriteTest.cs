// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Game.Storyboards;
using osuTK;

namespace osu.Game.Tests.Storyboards
{
    [TestFixture]
    public class StoryboardSpriteTest
    {
        private static StoryboardSprite create() => new StoryboardSprite(StoryboardElementSource.Beatmap, "a.png", Anchor.Centre, new Vector2(10, 20));

        [Test]
        public void TestConstructorValues()
        {
            var s = create();

            Assert.That(s.Source, Is.EqualTo(StoryboardElementSource.Beatmap));
            Assert.That(s.Path, Is.EqualTo("a.png"));
            Assert.That(s.Origin, Is.EqualTo(Anchor.Centre));
            Assert.That(s.InitialPosition, Is.EqualTo(new Vector2(10, 20)));
        }

        // CF: StoryboardSprite.HasCommands | Commands.HasCommands || LoopingGroups.Any(l => l.HasCommands) | FALSE
        [Test]
        public void TestNoCommands()
        {
            var s = create();
            s.AddLoopingGroup(0, 1);

            Assert.That(s.HasCommands, Is.False);
            Assert.That(s.IsDrawable, Is.False);
        }

        // CF: StoryboardSprite.HasCommands | Commands.HasCommands | TRUE
        [Test]
        public void TestDirectCommands()
        {
            var s = create();
            s.Commands.AddX(Easing.None, 0, 10, 0, 1);

            Assert.That(s.HasCommands, Is.True);
            Assert.That(s.IsDrawable, Is.True);
        }

        // CF: StoryboardSprite.HasCommands | LoopingGroups.Any(l => l.HasCommands) | TRUE
        [Test]
        public void TestLoopingCommandsCount()
        {
            var s = create();
            s.AddLoopingGroup(0, 1).AddX(Easing.None, 0, 10, 0, 1);

            Assert.That(s.HasCommands, Is.True);
        }

        // CF: StoryboardSprite.StartTime | visibleAtStartOrEnd(command) | TRUE
        // CF: StoryboardSprite.StartTime | firstAlpha.StartValue == 0 && firstRealAlpha != null | TRUE
        [Test]
        public void TestStartTimeSkipsInvisibleNoOpAlphaCommands()
        {
            var s = create();
            s.Commands.AddAlpha(Easing.None, 0, 100, 0, 0);
            s.Commands.AddAlpha(Easing.None, 500, 600, 0, 1);

            Assert.That(s.StartTime, Is.EqualTo(500));
        }

        // CF: StoryboardSprite.StartTime | firstAlpha.StartValue == 0 | FALSE
        [Test]
        public void TestStartTimeUsesEarliestCommandWhenInitiallyVisible()
        {
            var s = create();
            s.Commands.AddX(Easing.None, 100, 200, 0, 5);
            s.Commands.AddAlpha(Easing.None, 300, 400, 1, 0);

            Assert.That(s.StartTime, Is.EqualTo(100));
        }

        // CF: StoryboardSprite.StartTime | firstRealAlpha != null | FALSE
        // CF: StoryboardSprite.StartTime | visibleAtStartOrEnd(command) | FALSE
        [Test]
        public void TestStartTimeFallsBackToEarliestCommandWhenAlphaNeverVisible()
        {
            var s = create();
            s.Commands.AddAlpha(Easing.None, 400, 500, 0, 0);
            s.Commands.AddX(Easing.None, 250, 300, 0, 5);

            Assert.That(s.StartTime, Is.EqualTo(250));
        }

        // CF: StoryboardSprite.StartTime | alphaCommands.Count > 0 | FALSE
        [Test]
        public void TestStartTimeWithoutAlphaUsesEarliestCommandIncludingLoops()
        {
            var s = create();
            s.Commands.AddX(Easing.None, 800, 900, 0, 5);
            s.AddLoopingGroup(100, 2).AddY(Easing.None, 0, 50, 0, 5);

            Assert.That(s.StartTime, Is.EqualTo(100));
            Assert.That(s.EarliestTransformTime, Is.EqualTo(100));
        }

        // CF: StoryboardSprite.StartTime | foreach loop in LoopingGroups / foreach command in loop.Alpha | loop n
        [Test]
        public void TestStartTimeConsidersAlphaInsideLoops()
        {
            var s = create();
            s.Commands.AddAlpha(Easing.None, 0, 50, 0, 0);
            s.AddLoopingGroup(1000, 1).AddAlpha(Easing.None, 0, 100, 0, 1);

            Assert.That(s.StartTime, Is.EqualTo(1000));
        }

        // CF: StoryboardSprite.EndTime | foreach l in LoopingGroups | loop n
        [Test]
        public void TestEndTimeIsLatestOfDirectAndLoopedCommands()
        {
            var s = create();
            s.Commands.AddX(Easing.None, 0, 300, 0, 1);
            Assert.That(s.EndTime, Is.EqualTo(300));

            s.AddLoopingGroup(1000, 3).AddX(Easing.None, 0, 100, 0, 1);
            Assert.That(s.EndTime, Is.EqualTo(1100), "a loop only counts its first iteration");
        }

        [Test]
        public void TestEndTimeForDisplayAccountsForAllLoopIterations()
        {
            var s = create();
            s.Commands.AddX(Easing.None, 0, 300, 0, 1);
            s.AddLoopingGroup(1000, 2).AddX(Easing.None, 0, 100, 0, 1);

            Assert.That(s.EndTimeForDisplay, Is.EqualTo(1000 + 100 * 3), "three iterations of 100ms");
        }

        [Test]
        public void TestAddGroups()
        {
            var s = create();

            var loop = s.AddLoopingGroup(10, 4);
            var trigger = s.AddTriggerGroup("HitSound", 0, 100, 2);

            Assert.That(s.LoopingGroups.Single(), Is.SameAs(loop));
            Assert.That(s.TriggerGroups.Single(), Is.SameAs(trigger));
            Assert.That(loop.TotalIterations, Is.EqualTo(5));
        }

        // CF: StoryboardSprite.ApplyTransforms | Add(PropertyName) / BeginAbsoluteSequence | infeasible: needs a Drawable and StoryboardTriggerController
        [Test]
        public void TestPlaceholder()
        {
            Assert.That(create().LoopingGroups, Is.Empty);
        }
    }
}
