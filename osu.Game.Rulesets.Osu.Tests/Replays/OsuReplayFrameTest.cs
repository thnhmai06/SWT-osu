// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Replays.Legacy;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Replays;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Replays
{
    [TestFixture]
    public class OsuReplayFrameTest
    {
        private static readonly OsuBeatmap beatmap = new OsuBeatmap();

        // ---- FromLegacy: three independent ifs ----

        [Test]
        public void TestFromLegacy_NoButtons_NoActions()
        {
            // MouseLeft / MouseRight / Smoke all FALSE
            var frame = new OsuReplayFrame();
            frame.FromLegacy(new LegacyReplayFrame(0, 10, 20, ReplayButtonState.None), beatmap);

            Assert.That(frame.Position, Is.EqualTo(new Vector2(10, 20)));
            Assert.That(frame.Actions, Is.Empty);
        }

        [Test]
        public void TestFromLegacy_LeftOnly()
        {
            var frame = new OsuReplayFrame();
            frame.FromLegacy(new LegacyReplayFrame(0, 0, 0, ReplayButtonState.Left1), beatmap);

            Assert.That(frame.Actions, Is.EqualTo(new[] { OsuAction.LeftButton }));
        }

        [Test]
        public void TestFromLegacy_SecondaryLeftAlsoCountsAsLeft()
        {
            // MouseLeft = MouseLeft1 || MouseLeft2 -> second operand
            var frame = new OsuReplayFrame();
            frame.FromLegacy(new LegacyReplayFrame(0, 0, 0, ReplayButtonState.Left2), beatmap);

            Assert.That(frame.Actions, Is.EqualTo(new[] { OsuAction.LeftButton }));
        }

        [Test]
        public void TestFromLegacy_RightOnly()
        {
            var frame = new OsuReplayFrame();
            frame.FromLegacy(new LegacyReplayFrame(0, 0, 0, ReplayButtonState.Right1), beatmap);

            Assert.That(frame.Actions, Is.EqualTo(new[] { OsuAction.RightButton }));
        }

        [Test]
        public void TestFromLegacy_SmokeOnly()
        {
            var frame = new OsuReplayFrame();
            frame.FromLegacy(new LegacyReplayFrame(0, 0, 0, ReplayButtonState.Smoke), beatmap);

            Assert.That(frame.Actions, Is.EqualTo(new[] { OsuAction.Smoke }));
        }

        [Test]
        public void TestFromLegacy_AllButtons_AddsActionsInOrder()
        {
            var frame = new OsuReplayFrame();
            frame.FromLegacy(new LegacyReplayFrame(0, 0, 0, ReplayButtonState.Left1 | ReplayButtonState.Right1 | ReplayButtonState.Smoke), beatmap);

            Assert.That(frame.Actions, Is.EqualTo(new[] { OsuAction.LeftButton, OsuAction.RightButton, OsuAction.Smoke }));
        }

        // ---- ToLegacy: three independent ifs ----

        [Test]
        public void TestToLegacy_NoActions()
        {
            var legacy = new OsuReplayFrame(500, new Vector2(3, 4)).ToLegacy(beatmap);

            Assert.That(legacy.ButtonState, Is.EqualTo(ReplayButtonState.None));
            Assert.That(legacy.Time, Is.EqualTo(500));
            Assert.That(legacy.Position, Is.EqualTo(new Vector2(3, 4)));
        }

        [TestCase(OsuAction.LeftButton, ReplayButtonState.Left1)]
        [TestCase(OsuAction.RightButton, ReplayButtonState.Right1)]
        [TestCase(OsuAction.Smoke, ReplayButtonState.Smoke)]
        public void TestToLegacy_SingleAction(OsuAction action, ReplayButtonState expected)
        {
            Assert.That(new OsuReplayFrame(0, Vector2.Zero, action).ToLegacy(beatmap).ButtonState, Is.EqualTo(expected));
        }

        [Test]
        public void TestToLegacy_AllActions_CombinesFlags()
        {
            var frame = new OsuReplayFrame(0, Vector2.Zero, OsuAction.LeftButton, OsuAction.RightButton, OsuAction.Smoke);

            Assert.That(frame.ToLegacy(beatmap).ButtonState, Is.EqualTo(ReplayButtonState.Left1 | ReplayButtonState.Right1 | ReplayButtonState.Smoke));
        }

        // ---- IsEquivalentTo: a && b && c && d ----

        [Test]
        public void TestIsEquivalentTo_Identical_True()
        {
            var a = new OsuReplayFrame(10, new Vector2(1, 2), OsuAction.LeftButton);
            var b = new OsuReplayFrame(10, new Vector2(1, 2), OsuAction.LeftButton);

            Assert.That(a.IsEquivalentTo(b), Is.True);
        }

        [Test]
        public void TestIsEquivalentTo_DifferentFrameType_False()
        {
            // `other is OsuReplayFrame` FALSE
            Assert.That(new OsuReplayFrame().IsEquivalentTo(new LegacyReplayFrame(0, 0, 0, ReplayButtonState.None)), Is.False);
        }

        [Test]
        public void TestIsEquivalentTo_DifferentTime_False()
        {
            Assert.That(new OsuReplayFrame(10, Vector2.Zero).IsEquivalentTo(new OsuReplayFrame(11, Vector2.Zero)), Is.False);
        }

        [Test]
        public void TestIsEquivalentTo_DifferentPosition_False()
        {
            Assert.That(new OsuReplayFrame(10, Vector2.Zero).IsEquivalentTo(new OsuReplayFrame(10, Vector2.One)), Is.False);
        }

        [Test]
        public void TestIsEquivalentTo_DifferentActions_False()
        {
            Assert.That(new OsuReplayFrame(10, Vector2.Zero, OsuAction.LeftButton).IsEquivalentTo(new OsuReplayFrame(10, Vector2.Zero, OsuAction.RightButton)), Is.False);
        }
    }
}
