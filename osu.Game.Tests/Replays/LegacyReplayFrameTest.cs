// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Replays.Legacy;
using osu.Game.Rulesets.Replays;
using osuTK;

namespace osu.Game.Tests.Replays
{
    [TestFixture]
    public class LegacyReplayFrameTest
    {
        // CF: LegacyReplayFrame.Position | MouseX ?? 0 | null
        // CF: LegacyReplayFrame.Position | MouseY ?? 0 | null
        [Test]
        public void TestPositionDefaultsToOriginWhenUnset()
        {
            Assert.That(new LegacyReplayFrame(0, null, null, ReplayButtonState.None).Position, Is.EqualTo(Vector2.Zero));
        }

        // CF: LegacyReplayFrame.Position | MouseX ?? 0 | non-null
        // CF: LegacyReplayFrame.Position | MouseY ?? 0 | non-null
        [Test]
        public void TestPositionFromMouseCoordinates()
        {
            Assert.That(new LegacyReplayFrame(0, 12, 34, ReplayButtonState.None).Position, Is.EqualTo(new Vector2(12, 34)));
        }

        [Test]
        public void TestConstructorValues()
        {
            var frame = new LegacyReplayFrame(500, 1, 2, ReplayButtonState.Left1);

            Assert.That((frame.Time, frame.MouseX, frame.MouseY, frame.ButtonState), Is.EqualTo((500d, (float?)1, (float?)2, ReplayButtonState.Left1)));
        }

        // CF: LegacyReplayFrame.MouseLeft | MouseLeft1 || MouseLeft2 | TRUE/FALSE
        // CF: LegacyReplayFrame.MouseRight | MouseRight1 || MouseRight2 | TRUE/FALSE
        [TestCase(ReplayButtonState.None, false, false, false, false)]
        [TestCase(ReplayButtonState.Left1, true, false, true, false)]
        [TestCase(ReplayButtonState.Left2, true, false, true, false)]
        [TestCase(ReplayButtonState.Right1, false, true, false, true)]
        [TestCase(ReplayButtonState.Right2, false, true, false, true)]
        [TestCase(ReplayButtonState.Left1 | ReplayButtonState.Right2, true, true, true, true)]
        public void TestButtonAggregation(ReplayButtonState state, bool left, bool right, bool anyLeftExpected, bool anyRightExpected)
        {
            var frame = new LegacyReplayFrame(0, 0, 0, state);

            Assert.That(frame.MouseLeft, Is.EqualTo(left));
            Assert.That(frame.MouseRight, Is.EqualTo(right));
            Assert.That(frame.MouseLeft, Is.EqualTo(anyLeftExpected));
            Assert.That(frame.MouseRight, Is.EqualTo(anyRightExpected));
        }

        [Test]
        public void TestIndividualButtonsAndSmoke()
        {
            var frame = new LegacyReplayFrame(0, 0, 0, ReplayButtonState.Left1 | ReplayButtonState.Right2 | ReplayButtonState.Smoke);

            Assert.That((frame.MouseLeft1, frame.MouseLeft2, frame.MouseRight1, frame.MouseRight2, frame.Smoke), Is.EqualTo((true, false, false, true, true)));
            Assert.That(new LegacyReplayFrame(0, 0, 0, ReplayButtonState.None).Smoke, Is.False);
        }

        // CF: LegacyReplayFrame.IsEquivalentTo | other is LegacyReplayFrame legacyFrame | FALSE
        [Test]
        public void TestNotEquivalentToOtherFrameType()
        {
            Assert.That(new LegacyReplayFrame(0, 1, 2, ReplayButtonState.None).IsEquivalentTo(new ReplayFrame(0)), Is.False);
        }

        // CF: LegacyReplayFrame.IsEquivalentTo | Time == legacyFrame.Time | FALSE
        // CF: LegacyReplayFrame.IsEquivalentTo | MouseX == legacyFrame.MouseX | FALSE
        // CF: LegacyReplayFrame.IsEquivalentTo | MouseY == legacyFrame.MouseY | FALSE
        // CF: LegacyReplayFrame.IsEquivalentTo | ButtonState == legacyFrame.ButtonState | FALSE
        [TestCase(1.0, 1f, 2f, ReplayButtonState.Left1)]
        [TestCase(0.0, 9f, 2f, ReplayButtonState.Left1)]
        [TestCase(0.0, 1f, 9f, ReplayButtonState.Left1)]
        [TestCase(0.0, 1f, 2f, ReplayButtonState.Right1)]
        public void TestNotEquivalentWhenAnyFieldDiffers(double time, float x, float y, ReplayButtonState state)
        {
            var reference = new LegacyReplayFrame(0, 1, 2, ReplayButtonState.Left1);

            Assert.That(reference.IsEquivalentTo(new LegacyReplayFrame(time, x, y, state)), Is.False);
        }

        // CF: LegacyReplayFrame.IsEquivalentTo | all four comparisons | TRUE
        [Test]
        public void TestEquivalentWhenAllFieldsMatch()
        {
            Assert.That(new LegacyReplayFrame(5, 1, 2, ReplayButtonState.Left1).IsEquivalentTo(new LegacyReplayFrame(5, 1, 2, ReplayButtonState.Left1)), Is.True);
            Assert.That(new LegacyReplayFrame(5, null, null, ReplayButtonState.None).IsEquivalentTo(new LegacyReplayFrame(5, null, null, ReplayButtonState.None)), Is.True);
        }

        [Test]
        public void TestToStringContainsTimeAndPosition()
        {
            Assert.That(new LegacyReplayFrame(500, 1, 2, ReplayButtonState.Left1).ToString(), Does.StartWith("500\t(1,2)\tTrue\tFalse"));
        }
    }
}
