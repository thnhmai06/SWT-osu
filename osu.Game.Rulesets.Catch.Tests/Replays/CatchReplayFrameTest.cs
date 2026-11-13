// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Replays.Legacy;
using osu.Game.Rulesets.Catch.Replays;
using osu.Game.Rulesets.Replays;

namespace osu.Game.Rulesets.Catch.Tests.Replays
{
    [TestFixture]
    public class CatchReplayFrameTest
    {
        // CF: CatchReplayFrame.ctor | position ?? -1 | position null -> -1
        [Test]
        public void TestConstructorNullPositionDefaultsToMinusOne()
        {
            Assert.That(new CatchReplayFrame(0).Position, Is.EqualTo(-1f));
        }

        // CF: CatchReplayFrame.ctor | position ?? -1 | position supplied
        [Test]
        public void TestConstructorSuppliedPositionIsKept()
        {
            Assert.That(new CatchReplayFrame(0, 42f).Position, Is.EqualTo(42f));
        }

        // CF: CatchReplayFrame.ctor | if (Dashing) | TRUE
        [Test]
        public void TestConstructorDashingAddsDashAction()
        {
            var frame = new CatchReplayFrame(0, 10f, true);

            Assert.That(frame.Actions, Is.EqualTo(new[] { CatchAction.Dash }));
        }

        // CF: CatchReplayFrame.ctor | if (Dashing) | FALSE
        [Test]
        public void TestConstructorNotDashingHasNoActions()
        {
            Assert.That(new CatchReplayFrame(0, 10f).Actions, Is.Empty);
        }

        // CF: CatchReplayFrame.ctor | if (lastFrame != null) | FALSE
        [Test]
        public void TestConstructorWithoutLastFrameDoesNotThrow()
        {
            Assert.That(() => new CatchReplayFrame(0, 10f, false, null), Throws.Nothing);
        }

        // CF: CatchReplayFrame.ctor | lastFrame != null TRUE; Position > lastFrame.Position TRUE
        [Test]
        public void TestConstructorMovingRightAddsMoveRightToLastFrame()
        {
            var last = new CatchReplayFrame(0, 10f);
            _ = new CatchReplayFrame(1, 20f, false, last);

            Assert.That(last.Actions, Is.EqualTo(new[] { CatchAction.MoveRight }));
        }

        // CF: CatchReplayFrame.ctor | Position > last FALSE; Position < last TRUE
        [Test]
        public void TestConstructorMovingLeftAddsMoveLeftToLastFrame()
        {
            var last = new CatchReplayFrame(0, 20f);
            _ = new CatchReplayFrame(1, 10f, false, last);

            Assert.That(last.Actions, Is.EqualTo(new[] { CatchAction.MoveLeft }));
        }

        // CF: CatchReplayFrame.ctor | Position > last FALSE; Position < last FALSE (equal)
        [Test]
        public void TestConstructorSamePositionAddsNoMoveAction()
        {
            var last = new CatchReplayFrame(0, 20f);
            _ = new CatchReplayFrame(1, 20f, false, last);

            Assert.That(last.Actions, Is.Empty);
        }

        // CF: CatchReplayFrame.FromLegacy | ButtonState == Left1 | TRUE
        [Test]
        public void TestFromLegacyLeft1MeansDashing()
        {
            var frame = new CatchReplayFrame();
            frame.FromLegacy(new LegacyReplayFrame(0, 33f, null, ReplayButtonState.Left1), new Beatmap());

            Assert.That(frame.Position, Is.EqualTo(33f));
            Assert.That(frame.Dashing, Is.True);
            Assert.That(frame.Actions, Is.EqualTo(new[] { CatchAction.Dash }));
        }

        // CF: CatchReplayFrame.FromLegacy | ButtonState == Left1 | FALSE
        [Test]
        public void TestFromLegacyOtherButtonMeansNotDashing()
        {
            var frame = new CatchReplayFrame();
            frame.FromLegacy(new LegacyReplayFrame(0, 33f, null, ReplayButtonState.Right1), new Beatmap());

            Assert.That(frame.Dashing, Is.False);
            Assert.That(frame.Actions, Is.Empty);
        }

        // CF: CatchReplayFrame.FromLegacy | lastFrame is CatchReplayFrame | FALSE (null)
        [Test]
        public void TestFromLegacyNullLastFrameIsIgnored()
        {
            var frame = new CatchReplayFrame();

            Assert.That(() => frame.FromLegacy(new LegacyReplayFrame(0, 33f, null, ReplayButtonState.None), new Beatmap(), null), Throws.Nothing);
        }

        // CF: CatchReplayFrame.FromLegacy | lastFrame is CatchReplayFrame | FALSE (other ReplayFrame type)
        [Test]
        public void TestFromLegacyNonCatchLastFrameIsIgnored()
        {
            var frame = new CatchReplayFrame();

            Assert.That(() => frame.FromLegacy(new LegacyReplayFrame(0, 33f, null, ReplayButtonState.None), new Beatmap(), new ReplayFrame(0)), Throws.Nothing);
        }

        // CF: CatchReplayFrame.FromLegacy | lastFrame is CatchReplayFrame TRUE; Position > last TRUE
        [Test]
        public void TestFromLegacyMovingRightAddsMoveRightToLastFrame()
        {
            var last = new CatchReplayFrame(0, 10f);
            new CatchReplayFrame().FromLegacy(new LegacyReplayFrame(1, 20f, null, ReplayButtonState.None), new Beatmap(), last);

            Assert.That(last.Actions, Is.EqualTo(new[] { CatchAction.MoveRight }));
        }

        // CF: CatchReplayFrame.FromLegacy | Position > last FALSE; Position < last TRUE
        [Test]
        public void TestFromLegacyMovingLeftAddsMoveLeftToLastFrame()
        {
            var last = new CatchReplayFrame(0, 20f);
            new CatchReplayFrame().FromLegacy(new LegacyReplayFrame(1, 10f, null, ReplayButtonState.None), new Beatmap(), last);

            Assert.That(last.Actions, Is.EqualTo(new[] { CatchAction.MoveLeft }));
        }

        // CF: CatchReplayFrame.FromLegacy | Position > last FALSE; Position < last FALSE (equal)
        [Test]
        public void TestFromLegacySamePositionAddsNoMoveAction()
        {
            var last = new CatchReplayFrame(0, 20f);
            new CatchReplayFrame().FromLegacy(new LegacyReplayFrame(1, 20f, null, ReplayButtonState.None), new Beatmap(), last);

            Assert.That(last.Actions, Is.Empty);
        }

        // CF: CatchReplayFrame.ToLegacy | Actions.Contains(Dash) | TRUE
        [Test]
        public void TestToLegacyDashActionSetsLeft1()
        {
            var legacy = new CatchReplayFrame(5, 12f, true).ToLegacy(new Beatmap());

            Assert.That(legacy.ButtonState, Is.EqualTo(ReplayButtonState.Left1));
            Assert.That(legacy.Time, Is.EqualTo(5));
            Assert.That(legacy.MouseX, Is.EqualTo(12f));
        }

        // CF: CatchReplayFrame.ToLegacy | Actions.Contains(Dash) | FALSE
        [Test]
        public void TestToLegacyWithoutDashActionHasNoButtons()
        {
            var legacy = new CatchReplayFrame(5, 12f, false).ToLegacy(new Beatmap());

            Assert.That(legacy.ButtonState, Is.EqualTo(ReplayButtonState.None));
        }

        // CF: CatchReplayFrame.IsEquivalentTo | other is CatchReplayFrame | FALSE
        [Test]
        public void TestEquivalentToOtherFrameTypeIsFalse()
        {
            Assert.That(new CatchReplayFrame(1, 2f).IsEquivalentTo(new ReplayFrame(1)), Is.False);
        }

        // CF: CatchReplayFrame.IsEquivalentTo | Time == other.Time | FALSE
        [Test]
        public void TestEquivalentToDifferentTimeIsFalse()
        {
            Assert.That(new CatchReplayFrame(1, 2f).IsEquivalentTo(new CatchReplayFrame(9, 2f)), Is.False);
        }

        // CF: CatchReplayFrame.IsEquivalentTo | Position == other.Position | FALSE
        [Test]
        public void TestEquivalentToDifferentPositionIsFalse()
        {
            Assert.That(new CatchReplayFrame(1, 2f).IsEquivalentTo(new CatchReplayFrame(1, 3f)), Is.False);
        }

        // CF: CatchReplayFrame.IsEquivalentTo | Dashing == other.Dashing | FALSE
        [Test]
        public void TestEquivalentToDifferentDashingIsFalse()
        {
            var other = new CatchReplayFrame(1, 2f) { Dashing = true };

            Assert.That(new CatchReplayFrame(1, 2f).IsEquivalentTo(other), Is.False);
        }

        // CF: CatchReplayFrame.IsEquivalentTo | Actions.SequenceEqual | FALSE
        [Test]
        public void TestEquivalentToDifferentActionsIsFalse()
        {
            var other = new CatchReplayFrame(1, 2f);
            other.Actions.Add(CatchAction.MoveLeft);

            Assert.That(new CatchReplayFrame(1, 2f).IsEquivalentTo(other), Is.False);
        }

        // CF: CatchReplayFrame.IsEquivalentTo | all conjuncts | TRUE
        [Test]
        public void TestEquivalentToIdenticalFrameIsTrue()
        {
            Assert.That(new CatchReplayFrame(1, 2f, true).IsEquivalentTo(new CatchReplayFrame(1, 2f, true)), Is.True);
        }
    }
}
