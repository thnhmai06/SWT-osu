// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Replays.Legacy;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.Taiko.Replays;

namespace osu.Game.Rulesets.Taiko.Tests.Replays
{
    [TestFixture]
    public class TaikoReplayFrameTest
    {
        // CF: TaikoReplayFrame.FromLegacy | MouseRight1 TRUE / others FALSE
        [Test]
        public void TestFromLegacyRight1MapsToLeftRim() => assertFromLegacy(ReplayButtonState.Right1, TaikoAction.LeftRim);

        // CF: TaikoReplayFrame.FromLegacy | MouseRight2 TRUE / others FALSE
        [Test]
        public void TestFromLegacyRight2MapsToRightRim() => assertFromLegacy(ReplayButtonState.Right2, TaikoAction.RightRim);

        // CF: TaikoReplayFrame.FromLegacy | MouseLeft1 TRUE / others FALSE
        [Test]
        public void TestFromLegacyLeft1MapsToLeftCentre() => assertFromLegacy(ReplayButtonState.Left1, TaikoAction.LeftCentre);

        // CF: TaikoReplayFrame.FromLegacy | MouseLeft2 TRUE / others FALSE
        [Test]
        public void TestFromLegacyLeft2MapsToRightCentre() => assertFromLegacy(ReplayButtonState.Left2, TaikoAction.RightCentre);

        // CF: TaikoReplayFrame.FromLegacy | all four conditions FALSE
        [Test]
        public void TestFromLegacyNoButtonsYieldsNoActions() => assertFromLegacy(ReplayButtonState.None);

        // CF: TaikoReplayFrame.FromLegacy | all four conditions TRUE, evaluated in fixed order
        [Test]
        public void TestFromLegacyAllButtonsYieldsAllActionsInOrder()
        {
            assertFromLegacy(ReplayButtonState.Right1 | ReplayButtonState.Right2 | ReplayButtonState.Left1 | ReplayButtonState.Left2,
                TaikoAction.LeftRim, TaikoAction.RightRim, TaikoAction.LeftCentre, TaikoAction.RightCentre);
        }

        private static void assertFromLegacy(ReplayButtonState state, params TaikoAction[] expected)
        {
            var frame = new TaikoReplayFrame();
            frame.FromLegacy(new LegacyReplayFrame(0, null, null, state), new Beatmap());

            Assert.That(frame.Actions, Is.EqualTo(expected));
        }

        // CF: TaikoReplayFrame.ToLegacy | Actions.Contains(LeftRim) TRUE only
        [Test]
        public void TestToLegacyLeftRim() => assertToLegacy(ReplayButtonState.Right1, TaikoAction.LeftRim);

        // CF: TaikoReplayFrame.ToLegacy | Actions.Contains(RightRim) TRUE only
        [Test]
        public void TestToLegacyRightRim() => assertToLegacy(ReplayButtonState.Right2, TaikoAction.RightRim);

        // CF: TaikoReplayFrame.ToLegacy | Actions.Contains(LeftCentre) TRUE only
        [Test]
        public void TestToLegacyLeftCentre() => assertToLegacy(ReplayButtonState.Left1, TaikoAction.LeftCentre);

        // CF: TaikoReplayFrame.ToLegacy | Actions.Contains(RightCentre) TRUE only
        [Test]
        public void TestToLegacyRightCentre() => assertToLegacy(ReplayButtonState.Left2, TaikoAction.RightCentre);

        // CF: TaikoReplayFrame.ToLegacy | all four Contains FALSE
        [Test]
        public void TestToLegacyNoActions() => assertToLegacy(ReplayButtonState.None);

        // CF: TaikoReplayFrame.ToLegacy | all four Contains TRUE
        [Test]
        public void TestToLegacyAllActionsCombineFlags()
        {
            assertToLegacy(ReplayButtonState.Right1 | ReplayButtonState.Right2 | ReplayButtonState.Left1 | ReplayButtonState.Left2,
                TaikoAction.LeftRim, TaikoAction.RightRim, TaikoAction.LeftCentre, TaikoAction.RightCentre);
        }

        private static void assertToLegacy(ReplayButtonState expected, params TaikoAction[] actions)
        {
            var legacy = new TaikoReplayFrame(7, actions).ToLegacy(new Beatmap());

            Assert.That(legacy.ButtonState, Is.EqualTo(expected));
            Assert.That(legacy.Time, Is.EqualTo(7));
        }

        // CF: TaikoReplayFrame.IsEquivalentTo | other is TaikoReplayFrame FALSE
        [Test]
        public void TestEquivalentToDifferentFrameTypeIsFalse()
            => Assert.That(new TaikoReplayFrame(1, TaikoAction.LeftRim).IsEquivalentTo(new ReplayFrame(1)), Is.False);

        // CF: TaikoReplayFrame.IsEquivalentTo | Time == Time FALSE
        [Test]
        public void TestEquivalentToDifferentTimeIsFalse()
            => Assert.That(new TaikoReplayFrame(1, TaikoAction.LeftRim).IsEquivalentTo(new TaikoReplayFrame(2, TaikoAction.LeftRim)), Is.False);

        // CF: TaikoReplayFrame.IsEquivalentTo | Actions.SequenceEqual FALSE
        [Test]
        public void TestEquivalentToDifferentActionsIsFalse()
            => Assert.That(new TaikoReplayFrame(1, TaikoAction.LeftRim).IsEquivalentTo(new TaikoReplayFrame(1, TaikoAction.RightRim)), Is.False);

        // CF: TaikoReplayFrame.IsEquivalentTo | all conjuncts TRUE
        [Test]
        public void TestEquivalentToIdenticalFrameIsTrue()
            => Assert.That(new TaikoReplayFrame(1, TaikoAction.LeftRim).IsEquivalentTo(new TaikoReplayFrame(1, TaikoAction.LeftRim)), Is.True);
    }
}
