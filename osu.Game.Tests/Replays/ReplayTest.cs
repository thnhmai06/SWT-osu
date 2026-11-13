// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Replays;
using osu.Game.Rulesets.Replays;

namespace osu.Game.Tests.Replays
{
    [TestFixture]
    public class ReplayTest
    {
        [Test]
        public void TestDefaults()
        {
            var replay = new Replay();

            Assert.That(replay.HasReceivedAllFrames, Is.True);
            Assert.That(replay.Frames, Is.Empty);
        }

        [Test]
        public void TestDeepCloneCopiesFrameListButSharesFrames()
        {
            var frame = new ReplayFrame(100);
            var replay = new Replay { HasReceivedAllFrames = false, Frames = { frame } };

            var clone = replay.DeepClone();

            Assert.That(clone, Is.Not.SameAs(replay));
            Assert.That(clone.HasReceivedAllFrames, Is.False);
            Assert.That(clone.Frames, Is.Not.SameAs(replay.Frames));
            Assert.That(clone.Frames, Is.EqualTo(new[] { frame }));

            clone.Frames.Add(new ReplayFrame(200));
            Assert.That(replay.Frames, Has.Count.EqualTo(1), "the clone's list is independent");
        }
    }
}
