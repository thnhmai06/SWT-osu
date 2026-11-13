// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Replays.Legacy;
using osu.Game.Rulesets.Mania.Replays;
using osu.Game.Rulesets.Replays;

namespace osu.Game.Rulesets.Mania.Tests.Replays
{
    [TestFixture]
    public class ManiaReplayFrameTest
    {
        // CF: ManiaReplayFrame.FromLegacy | while activeColumns > 0 | loop 0
        // CF: ManiaReplayFrame.FromLegacy | MouseX ?? 0 | null
        [Test]
        public void TestFromLegacyNoKeys()
        {
            var frame = new ManiaReplayFrame();
            frame.FromLegacy(new LegacyReplayFrame(0, null, null, ReplayButtonState.None), null!);
            Assert.That(frame.Actions, Is.Empty);
        }

        // CF: ManiaReplayFrame.FromLegacy | while activeColumns > 0 | loop n
        // CF: ManiaReplayFrame.FromLegacy | (activeColumns & 1) > 0 | TRUE
        // CF: ManiaReplayFrame.FromLegacy | (activeColumns & 1) > 0 | FALSE
        // CF: ManiaReplayFrame.FromLegacy | MouseX ?? 0 | non-null
        [Test]
        public void TestFromLegacyBitmask()
        {
            var frame = new ManiaReplayFrame();
            frame.FromLegacy(new LegacyReplayFrame(0, 0b1101, null, ReplayButtonState.None), null!);
            Assert.That(frame.Actions, Is.EqualTo(new[] { ManiaAction.Key1, ManiaAction.Key3, ManiaAction.Key4 }));
        }

        // CF: ManiaReplayFrame.ToLegacy | foreach action | loop 0
        [Test]
        public void TestToLegacyNoActions()
        {
            Assert.That(new ManiaReplayFrame(5).ToLegacy(null!).MouseX, Is.EqualTo(0));
        }

        // CF: ManiaReplayFrame.ToLegacy | foreach action | loop n
        [Test]
        public void TestToLegacyRoundTrip()
        {
            var frame = new ManiaReplayFrame(10, ManiaAction.Key1, ManiaAction.Key4);
            var legacy = frame.ToLegacy(null!);

            Assert.That(legacy.Time, Is.EqualTo(10));
            Assert.That(legacy.MouseX, Is.EqualTo(0b1001));
        }

        // CF: ManiaReplayFrame.IsEquivalentTo | other is ManiaReplayFrame | FALSE
        [Test]
        public void TestEquivalentToOtherFrameType()
        {
            Assert.That(new ManiaReplayFrame(0).IsEquivalentTo(new ReplayFrame(0)), Is.False);
        }

        // CF: ManiaReplayFrame.IsEquivalentTo | other is ManiaReplayFrame | TRUE
        // CF: ManiaReplayFrame.IsEquivalentTo | Time == other.Time | FALSE
        [Test]
        public void TestEquivalentDifferentTime()
        {
            Assert.That(new ManiaReplayFrame(0, ManiaAction.Key1).IsEquivalentTo(new ManiaReplayFrame(1, ManiaAction.Key1)), Is.False);
        }

        // CF: ManiaReplayFrame.IsEquivalentTo | Time == other.Time | TRUE
        // CF: ManiaReplayFrame.IsEquivalentTo | Actions.SequenceEqual | FALSE
        [Test]
        public void TestEquivalentDifferentActions()
        {
            Assert.That(new ManiaReplayFrame(0, ManiaAction.Key1).IsEquivalentTo(new ManiaReplayFrame(0, ManiaAction.Key2)), Is.False);
        }

        // CF: ManiaReplayFrame.IsEquivalentTo | Actions.SequenceEqual | TRUE
        [Test]
        public void TestEquivalent()
        {
            Assert.That(new ManiaReplayFrame(0, ManiaAction.Key1).IsEquivalentTo(new ManiaReplayFrame(0, ManiaAction.Key1)), Is.True);
        }
    }
}
