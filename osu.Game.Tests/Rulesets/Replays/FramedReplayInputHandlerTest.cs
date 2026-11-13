// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Input.StateChanges;
using osu.Game.Online.API;
using osu.Game.Online.Spectator;
using osu.Game.Replays;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Tests.Rulesets.Replays
{
    [TestFixture]
    public class FramedReplayInputHandlerTest
    {
        private class TestHandler : FramedReplayInputHandler<ReplayFrame>
        {
            public bool ImportantFrames;

            public TestHandler(Replay replay)
                : base(replay)
            {
            }

            protected override bool IsImportant(ReplayFrame frame) => ImportantFrames;

            public double Time => CurrentTime;
        }

        private static Replay replay(bool receivedAll = true, params double[] times)
        {
            var r = new Replay { HasReceivedAllFrames = receivedAll };
            r.Frames.AddRange(times.Select(t => new ReplayFrame(t)));
            return r;
        }

        // CF: FramedReplayInputHandler.ctor | replay.Frames ordered by time | no throw
        // CF: FramedReplayInputHandler.HasFrames | Frames.Count != 0 | TRUE
        // CF: FramedReplayInputHandler.CurrentFrame | currentFrameIndex == -1 | TRUE
        // CF: FramedReplayInputHandler.NextFrame | currentFrameIndex == Count - 1 | FALSE
        [Test]
        public void TestFramesAreOrderedAndInitialState()
        {
            var handler = new TestHandler(replay(true, 200, 0, 100));

            Assert.That(handler.HasFrames, Is.True);
            Assert.That(handler.IsActive, Is.True);
            Assert.That(handler.CurrentFrame, Is.Null);
            Assert.That(handler.NextFrame!.Time, Is.EqualTo(0));
            Assert.That(handler.StartFrame.Time, Is.EqualTo(0));
            Assert.That(handler.EndFrame.Time, Is.EqualTo(0));
            Assert.That(handler.Time, Is.EqualTo(double.NegativeInfinity));
        }

        // CF: FramedReplayInputHandler.StartFrame | !HasFrames | TRUE
        // CF: FramedReplayInputHandler.EndFrame | !HasFrames | TRUE
        // CF: FramedReplayInputHandler.SetFrameFromTime | !HasFrames | TRUE
        // CF: FramedReplayInputHandler.SetFrameFromTime | replay.HasReceivedAllFrames | FALSE
        [Test]
        public void TestNoFramesAndStillReceiving()
        {
            var handler = new TestHandler(replay(false));

            Assert.That(handler.HasFrames, Is.False);
            Assert.That(handler.IsActive, Is.False);
            Assert.Throws<InvalidOperationException>(() => _ = handler.StartFrame);
            Assert.Throws<InvalidOperationException>(() => _ = handler.EndFrame);
            Assert.That(handler.SetFrameFromTime(500), Is.Null);
        }

        // CF: FramedReplayInputHandler.SetFrameFromTime | replay.HasReceivedAllFrames | TRUE
        [Test]
        public void TestNoFramesButFullyReceivedAllowsTimeToProgress()
        {
            var handler = new TestHandler(replay(true));

            Assert.That(handler.SetFrameFromTime(500), Is.EqualTo(500));
            Assert.That(handler.Time, Is.EqualTo(500));
        }

        // CF: FramedReplayInputHandler.SetFrameFromTime | frameEnd <= time | TRUE
        // CF: FramedReplayInputHandler.SetFrameFromTime | frameEnd <= time | FALSE
        // CF: FramedReplayInputHandler.getFrameTime | index < 0 | TRUE
        // CF: FramedReplayInputHandler.getFrameTime | index >= Frames.Count | TRUE
        [Test]
        public void TestForwardProgressStopsAtFrameBoundaries()
        {
            var handler = new TestHandler(replay(true, 0, 100, 200));

            Assert.That(handler.SetFrameFromTime(0), Is.EqualTo(0));
            Assert.That(handler.SetFrameFromTime(50), Is.EqualTo(50));
            Assert.That(handler.CurrentFrame!.Time, Is.EqualTo(0));
            Assert.That(handler.NextFrame!.Time, Is.EqualTo(100));

            Assert.That(handler.SetFrameFromTime(150), Is.EqualTo(100), "stops exactly on the frame");
            Assert.That(handler.CurrentFrame!.Time, Is.EqualTo(100));
            Assert.That(handler.SetFrameFromTime(150), Is.EqualTo(150));
            Assert.That(handler.StartFrame.Time, Is.EqualTo(100));
            Assert.That(handler.EndFrame.Time, Is.EqualTo(200));
        }

        // CF: FramedReplayInputHandler.SetFrameFromTime | time < frameStart && CurrentTime == frameStart | TRUE
        // CF: FramedReplayInputHandler.SetFrameFromTime | time < frameStart && CurrentTime == frameStart | FALSE
        [Test]
        public void TestRewindingAtAFrameBoundary()
        {
            var handler = new TestHandler(replay(true, 0, 100, 200));
            handler.SetFrameFromTime(0);
            handler.SetFrameFromTime(100);
            Assert.That(handler.CurrentFrame!.Time, Is.EqualTo(100));

            Assert.That(handler.SetFrameFromTime(50), Is.EqualTo(50));
            Assert.That(handler.CurrentFrame!.Time, Is.EqualTo(0), "stepped back one frame");

            // not at a boundary: the jump back does not change frame, time is only clamped.
            handler.SetFrameFromTime(150);
            handler.SetFrameFromTime(120);
            Assert.That(handler.Time, Is.EqualTo(120));
        }

        // CF: FramedReplayInputHandler.WaitingForFrame | !HasReceivedAllFrames && currentFrameIndex == Count - 1 | TRUE
        // CF: FramedReplayInputHandler.SetFrameFromTime | WaitingForFrame && frameStart < time | TRUE
        // CF: FramedReplayInputHandler.SetFrameFromTime | WaitingForFrame && frameStart < time | FALSE
        [Test]
        public void TestWaitsForMoreFramesAtEndOfIncompleteReplay()
        {
            var handler = new TestHandler(replay(false, 0, 100, 200));
            handler.SetFrameFromTime(0);
            handler.SetFrameFromTime(100);
            Assert.That(handler.SetFrameFromTime(200), Is.EqualTo(200));
            Assert.That(handler.WaitingForFrame, Is.True);

            Assert.That(handler.SetFrameFromTime(250), Is.Null);
            Assert.That(handler.Time, Is.EqualTo(200), "time is held at the last received frame");
            Assert.That(handler.NextFrame, Is.Null);

            Assert.That(new TestHandler(replay(true, 0, 100)).WaitingForFrame, Is.False);
        }

        // CF: FramedReplayInputHandler.inImportantSection | !HasFrames || !FrameAccuratePlayback || currentFrameIndex == -1 | TRUE
        // CF: FramedReplayInputHandler.inImportantSection | !HasFrames || !FrameAccuratePlayback || currentFrameIndex == -1 | FALSE
        // CF: FramedReplayInputHandler.inImportantSection | IsImportant(StartFrame) && within span | TRUE
        // CF: FramedReplayInputHandler.inImportantSection | IsImportant(StartFrame) && within span | FALSE
        [Test]
        public void TestImportantSectionsDisallowMidFrameTimes()
        {
            // not frame accurate: mid-frame time allowed even when important.
            var lenient = new TestHandler(replay(true, 0, 1000)) { ImportantFrames = true };
            lenient.SetFrameFromTime(0);
            Assert.That(lenient.SetFrameFromTime(990), Is.EqualTo(990));

            var strict = new TestHandler(replay(true, 0, 1000)) { ImportantFrames = true, FrameAccuratePlayback = true };
            strict.SetFrameFromTime(0);
            Assert.That(strict.SetFrameFromTime(500), Is.EqualTo(500), "next frame is too far away to matter");
            Assert.That(strict.SetFrameFromTime(990), Is.Null, "within the allowed span of the next important frame");

            var notImportant = new TestHandler(replay(true, 0, 1000)) { ImportantFrames = false, FrameAccuratePlayback = true };
            notImportant.SetFrameFromTime(0);
            Assert.That(notImportant.SetFrameFromTime(990), Is.EqualTo(990));
        }

        // CF: FramedReplayInputHandler.CollectPendingInputs | CurrentFrame?.Header != null | TRUE
        // CF: FramedReplayInputHandler.CollectPendingInputs | CurrentFrame?.Header != null | FALSE
        [Test]
        public void TestStatisticsInputOnlyWhenFrameHasHeader()
        {
            var r = replay(true, 0, 100);
            r.Frames[1].Header = new FrameHeader(1, 1, 1, 1, new Dictionary<HitResult, int>(), new osu.Game.Rulesets.Scoring.ScoreProcessorStatistics(), DateTimeOffset.UnixEpoch, Array.Empty<APIMod>(), null, null);
            var handler = new TestHandler(r);

            var inputs = new List<IInput>();
            handler.SetFrameFromTime(0);
            handler.CollectPendingInputs(inputs);
            Assert.That(inputs, Is.Empty);

            handler.SetFrameFromTime(100);
            handler.CollectPendingInputs(inputs);
            Assert.That(inputs.Single(), Is.InstanceOf<osu.Game.Input.Handlers.ReplayInputHandler.ReplayStatisticsFrameInput>());
        }
    }
}
