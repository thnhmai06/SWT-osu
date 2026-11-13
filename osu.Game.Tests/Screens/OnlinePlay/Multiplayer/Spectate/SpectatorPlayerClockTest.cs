// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Moq;
using NUnit.Framework;
using osu.Framework.Timing;
using osu.Game.Screens.OnlinePlay.Multiplayer.Spectate;

namespace osu.Game.Tests.Screens.OnlinePlay.Multiplayer.Spectate
{
    [TestFixture]
    public class SpectatorPlayerClockTest
    {
        private Mock<IFrameBasedClock> master = null!;
        private SpectatorPlayerClock clock = null!;

        [SetUp]
        public void SetUp()
        {
            master = new Mock<IFrameBasedClock>();
            master.SetupGet(m => m.CurrentTime).Returns(1000);
            master.SetupGet(m => m.Rate).Returns(1.5);
            master.SetupGet(m => m.FramesPerSecond).Returns(60);
            clock = new SpectatorPlayerClock(master.Object);
        }

        private void setMaster(double time, double elapsed)
        {
            master.SetupGet(m => m.CurrentTime).Returns(time);
            master.SetupGet(m => m.ElapsedFrameTime).Returns(elapsed);
        }

        // CF: SpectatorPlayerClock.catchUpMultiplier | IsCatchingUp | FALSE
        [Test]
        public void TestRateFollowsMaster() => Assert.That(clock.Rate, Is.EqualTo(1.5));

        // CF: SpectatorPlayerClock.catchUpMultiplier | IsCatchingUp | TRUE
        [Test]
        public void TestRateWhileCatchingUp()
        {
            clock.IsCatchingUp = true;
            Assert.That(clock.Rate, Is.EqualTo(1.5 * SpectatorPlayerClock.CATCHUP_RATE));
        }

        [Test]
        public void TestRateCannotBeSet() => Assert.Throws<NotImplementedException>(() => clock.Rate = 2);

        [Test]
        public void TestSeekAndReset()
        {
            Assert.That(clock.Seek(500), Is.True);
            Assert.That(clock.CurrentTime, Is.EqualTo(500));
            clock.Reset();
            Assert.That(clock.CurrentTime, Is.EqualTo(0));
        }

        [Test]
        public void TestStartStopAreNoOps()
        {
            clock.Start();
            clock.Stop();
            clock.ResetSpeedAdjustments();
            Assert.That(clock.IsRunning, Is.False);
        }

        // CF: SpectatorPlayerClock.ProcessFrame | IsRunning | FALSE
        [Test]
        public void TestNotRunningProducesNoElapsedTime()
        {
            setMaster(1016, 16);
            clock.ProcessFrame();

            Assert.That(clock.CurrentTime, Is.EqualTo(0));
            Assert.That(clock.ElapsedFrameTime, Is.EqualTo(0));
            Assert.That(clock.FramesPerSecond, Is.EqualTo(0));
        }

        // CF: SpectatorPlayerClock.ProcessFrame | IsRunning | TRUE
        // CF: SpectatorPlayerClock.ProcessFrame | masterClock.ElapsedFrameTime != 0 | TRUE
        // CF: SpectatorPlayerClock.ProcessFrame | masterAdvanced | TRUE
        [Test]
        public void TestRunningAccumulatesMasterElapsedTime()
        {
            clock.IsRunning = true;
            setMaster(1016, 16);
            clock.ProcessFrame();

            Assert.That(clock.CurrentTime, Is.EqualTo(16));
            Assert.That(clock.ElapsedFrameTime, Is.EqualTo(16));
            Assert.That(clock.FramesPerSecond, Is.EqualTo(60));
            Assert.That(clock.TimeInfo.Current, Is.EqualTo(16));
            Assert.That(clock.TimeInfo.Elapsed, Is.EqualTo(16));
        }

        // CF: SpectatorPlayerClock.ProcessFrame | masterAdvanced | FALSE
        [Test]
        public void TestRepeatedProcessFrameDoesNotDoubleAccumulate()
        {
            clock.IsRunning = true;
            setMaster(1016, 16);
            clock.ProcessFrame();
            clock.ProcessFrame();

            Assert.That(clock.CurrentTime, Is.EqualTo(16));
        }

        // CF: SpectatorPlayerClock.ProcessFrame | masterClock.ElapsedFrameTime != 0 | FALSE
        [Test]
        public void TestStoppedMasterIsCaughtUpManuallyAndClamped()
        {
            clock.IsRunning = true;
            setMaster(1000, 0);

            clock.ProcessFrame();
            Assert.That(clock.CurrentTime, Is.EqualTo(16));

            setMaster(0, 0); // master behind the clock: no negative time.
            clock.ProcessFrame();
            Assert.That(clock.CurrentTime, Is.EqualTo(16));

            setMaster(20, 0);
            clock.ProcessFrame();
            Assert.That(clock.CurrentTime, Is.EqualTo(20));
        }

        [Test]
        public void TestCatchUpDoublesElapsed()
        {
            clock.IsRunning = true;
            clock.IsCatchingUp = true;
            setMaster(1016, 16);
            clock.ProcessFrame();

            Assert.That(clock.CurrentTime, Is.EqualTo(32));
        }
    }
}
