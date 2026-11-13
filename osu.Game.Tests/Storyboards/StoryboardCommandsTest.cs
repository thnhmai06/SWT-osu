// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Game.Storyboards.Commands;

namespace osu.Game.Tests.Storyboards
{
    [TestFixture]
    public class StoryboardCommandsTest
    {
        // CF: StoryboardCommand.ctor | endTime < startTime | TRUE
        [Test]
        public void TestEndBeforeStartIsClamped()
        {
            var command = new StoryboardXCommand(Easing.None, 100, 50, 0, 1);

            Assert.That(command.EndTime, Is.EqualTo(100));
            Assert.That(command.Duration, Is.Zero);
        }

        // CF: StoryboardCommand.ctor | endTime < startTime | FALSE
        [Test]
        public void TestValuesStored()
        {
            var command = new StoryboardXCommand(Easing.OutQuad, 100, 300, 1, 2);

            Assert.That((command.StartTime, command.EndTime, command.StartValue, command.EndValue, command.Easing), Is.EqualTo((100d, 300d, 1f, 2f, Easing.OutQuad)));
            Assert.That(command.Duration, Is.EqualTo(200));
            Assert.That(command.ToString(), Does.Contain("100 -> 300"));
        }

        // CF: StoryboardCommand.CompareTo | other == null | TRUE
        // CF: StoryboardCommand.CompareTo | result != 0 | TRUE
        // CF: StoryboardCommand.CompareTo | result != 0 | FALSE
        [Test]
        public void TestCompareToOrdersByStartThenEnd()
        {
            var early = new StoryboardXCommand(Easing.None, 0, 500, 0, 1);
            var late = new StoryboardXCommand(Easing.None, 100, 200, 0, 1);
            var earlyShort = new StoryboardXCommand(Easing.None, 0, 100, 0, 1);

            Assert.That(early.CompareTo(null), Is.Positive);
            Assert.That(early.CompareTo(late), Is.Negative);
            Assert.That(late.CompareTo(early), Is.Positive);
            Assert.That(early.CompareTo(earlyShort), Is.Positive, "same start: later end sorts after");
            Assert.That(earlyShort.CompareTo(early), Is.Negative);
            Assert.That(early.CompareTo(new StoryboardXCommand(Easing.None, 0, 500, 5, 6)), Is.Zero);
        }

        [Test]
        public void TestGroupTracksTimeSpanAndEmptiness()
        {
            var group = new StoryboardCommandGroup();
            Assert.That(group.HasCommands, Is.False);
            Assert.That(group.AllCommands, Is.Empty);

            group.AddX(Easing.None, 200, 300, 0, 1);
            group.AddAlpha(Easing.None, 100, 250, 0, 1);

            Assert.That(group.HasCommands, Is.True);
            Assert.That(group.StartTime, Is.EqualTo(100));
            Assert.That(group.EndTime, Is.EqualTo(300));
            Assert.That(group.Duration, Is.EqualTo(200));
            Assert.That(group.AllCommands.Count(), Is.EqualTo(2));
        }

        [Test]
        public void TestGroupAddsEachCommandKindToItsOwnList()
        {
            var group = new StoryboardCommandGroup();

            group.AddX(Easing.None, 0, 1, 0, 1);
            group.AddY(Easing.None, 0, 1, 0, 1);
            group.AddScale(Easing.None, 0, 1, 0, 1);
            group.AddVectorScale(Easing.None, 0, 1, osuTK.Vector2.One, osuTK.Vector2.Zero);
            group.AddRotation(Easing.None, 0, 1, 0, 1);
            group.AddColour(Easing.None, 0, 1, osuTK.Graphics.Color4.White, osuTK.Graphics.Color4.Black);
            group.AddAlpha(Easing.None, 0, 1, 0, 1);
            group.AddBlendingParameters(Easing.None, 0, 1, osu.Framework.Graphics.BlendingParameters.Additive, osu.Framework.Graphics.BlendingParameters.Inherit);
            group.AddFlipH(Easing.None, 0, 1, false, true);
            group.AddFlipV(Easing.None, 0, 1, false, true);

            Assert.That(new[] { group.X.Count, group.Y.Count, group.Scale.Count, group.VectorScale.Count, group.Rotation.Count, group.Colour.Count, group.Alpha.Count, group.BlendingParameters.Count, group.FlipH.Count, group.FlipV.Count }, Has.All.EqualTo(1));
            Assert.That(group.AllCommands.Count(), Is.EqualTo(10));
        }

        [Test]
        public void TestGroupKeepsCommandsSortedByTime()
        {
            var group = new StoryboardCommandGroup();
            group.AddX(Easing.None, 500, 600, 0, 1);
            group.AddX(Easing.None, 100, 200, 0, 1);

            Assert.That(group.X.Select(c => c.StartTime), Is.EqualTo(new[] { 100d, 500d }));
        }

        // CF: StoryboardLoopingGroup.ctor | ArgumentOutOfRangeException.ThrowIfNegative(repeatCount) | throws ArgumentOutOfRangeException
        [Test]
        public void TestNegativeRepeatCountRejected()
        {
            Assert.That(() => new StoryboardLoopingGroup(0, -1), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        // CF: StoryboardLoopingGroup.ctor | ArgumentOutOfRangeException.ThrowIfNegative(repeatCount) | no throw
        [Test]
        public void TestIterationsIsRepeatCountPlusOne()
        {
            Assert.That(new StoryboardLoopingGroup(0, 0).TotalIterations, Is.EqualTo(1));
            Assert.That(new StoryboardLoopingGroup(0, 4).TotalIterations, Is.EqualTo(5));
            Assert.That(new StoryboardLoopingGroup(1000, 2).ToString(), Is.EqualTo("1000 x3"));
        }

        [Test]
        public void TestLoopingCommandsAreOffsetByLoopStart()
        {
            var group = new StoryboardLoopingGroup(1000, 2);

            group.AddAlpha(Easing.InQuad, 100, 300, 0.25f, 0.75f);

            var command = group.Alpha.Single();
            Assert.That((command.StartTime, command.EndTime), Is.EqualTo((1100d, 1300d)));
            Assert.That((command.StartValue, command.EndValue, command.Easing), Is.EqualTo((0.25f, 0.75f, Easing.InQuad)));
            Assert.That(command.PropertyName, Is.EqualTo(new StoryboardAlphaCommand(Easing.None, 0, 1, 0, 1).PropertyName));
            Assert.That(command, Is.InstanceOf<IStoryboardLoopingCommand>());
            Assert.That(((IStoryboardLoopingCommand)command).OriginalCommand.StartTime, Is.EqualTo(100));
            Assert.That(group.StartTime, Is.EqualTo(1100));
            Assert.That(group.EndTime, Is.EqualTo(1300));
        }
    }
}
