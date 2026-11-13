// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Osu.Objects.Drawables;

namespace osu.Game.Rulesets.Osu.Tests.Objects.Drawables
{
    [TestFixture]
    public class SpinnerSpinHistoryTest
    {
        private SpinnerSpinHistory history = null!;

        [SetUp]
        public void SetUp() => history = new SpinnerSpinHistory();

        // CF: ReportDelta | delta == 0 TRUE -> early return, state untouched
        [Test]
        public void TestZeroDeltaIsIgnored()
        {
            history.ReportDelta(100, 0);

            Assert.That(history.TotalRotation, Is.EqualTo(0f));
        }

        // CF: ReportDelta | delta == 0 FALSE, currentTime >= lastReportTime TRUE, while (max >= 360) zero iterations
        [Test]
        public void TestPartialSpinAccumulates()
        {
            history.ReportDelta(100, 90);
            Assert.That(history.TotalRotation, Is.EqualTo(90f));

            history.ReportDelta(200, 90);
            Assert.That(history.TotalRotation, Is.EqualTo(180f));
        }

        // CF: ReportDelta | currentTime >= lastReportTime boundary (equal times count as forward)
        [Test]
        public void TestReportAtSameTimeIsTreatedAsForward()
        {
            history.ReportDelta(100, 90);
            history.ReportDelta(100, 90);

            Assert.That(history.TotalRotation, Is.EqualTo(180f));
        }

        // CF: ReportDelta | Math.Max(currentSpinMaxRotation, ...) keeps the maximum when spinning backwards
        [Test]
        public void TestBackwardsSpinDoesNotReduceTotalRotation()
        {
            history.ReportDelta(100, 90);
            history.ReportDelta(200, -45);

            Assert.That(history.TotalRotation, Is.EqualTo(90f));
        }

        // CF: ReportDelta | while (max >= 360) one iteration, positive direction
        [Test]
        public void TestCompleteClockwiseSpinCountsFullRotation()
        {
            history.ReportDelta(100, 360);

            Assert.That(history.TotalRotation, Is.EqualTo(360f));
        }

        // CF: ReportDelta | while (max >= 360) one iteration, remainder carried into the next spin
        [Test]
        public void TestRemainderAfterCompletedSpinIsCarriedOver()
        {
            history.ReportDelta(100, 370);

            Assert.That(history.TotalRotation, Is.EqualTo(370f));
        }

        // CF: ReportDelta | while (max >= 360) multiple iterations in a single report
        [Test]
        public void TestLargeDeltaCompletesMultipleSpins()
        {
            history.ReportDelta(100, 800);

            Assert.That(history.TotalRotation, Is.EqualTo(800f));
        }

        // CF: ReportDelta | direction = Math.Sign(...) negative
        [Test]
        public void TestCompleteCounterClockwiseSpinCountsFullRotation()
        {
            history.ReportDelta(100, -360);

            Assert.That(history.TotalRotation, Is.EqualTo(360f));
        }

        // CF: ReportDelta | direction change: +90 then -90 returns to 0, then another full -360 turn completes exactly one spin
        [Test]
        public void TestChangingDirectionRequiresFullTurnInNewDirection()
        {
            history.ReportDelta(100, 90);
            history.ReportDelta(200, -90);
            history.ReportDelta(300, -360);

            Assert.That(history.TotalRotation, Is.EqualTo(360f));
        }

        // CF: ReportDelta | currentTime >= lastReportTime FALSE -> rewind; TryPeek TRUE, CompletionTime > currentTime FALSE (nothing popped)
        [Test]
        public void TestRewindAfterCompletionKeepsCompletedSpin()
        {
            history.ReportDelta(100, 360);
            history.ReportDelta(200, 90);
            Assert.That(history.TotalRotation, Is.EqualTo(450f));

            history.ReportDelta(150, -90);

            Assert.That(history.TotalRotation, Is.EqualTo(360f));
        }

        // CF: ReportDelta | rewind; TryPeek TRUE, CompletionTime > currentTime TRUE -> pop completed spin
        [Test]
        public void TestRewindBeforeCompletionPopsCompletedSpin()
        {
            history.ReportDelta(100, 360);
            history.ReportDelta(50, -10);

            Assert.That(history.TotalRotation, Is.EqualTo(350f));
        }

        // CF: ReportDelta | rewind; TryPeek FALSE (no completed spins)
        [Test]
        public void TestRewindWithoutCompletedSpinsOnlyRecomputesCurrentSpin()
        {
            history.ReportDelta(100, 90);
            history.ReportDelta(50, -30);

            Assert.That(history.TotalRotation, Is.EqualTo(60f));
        }

        // CF: ReportDelta | rewind; pop loop repeats (two spins popped), then TryPeek FALSE
        [Test]
        public void TestRewindPopsEveryLaterSpin()
        {
            history.ReportDelta(100, 360);
            history.ReportDelta(200, 360);
            history.ReportDelta(50, -10);

            Assert.That(history.TotalRotation, Is.EqualTo(710f));
        }

        // CF: ReportDelta | rewind; pop loop stops at first spin that completed before currentTime
        [Test]
        public void TestRewindStopsPoppingAtEarlierSpin()
        {
            history.ReportDelta(100, 360);
            history.ReportDelta(200, 360);
            history.ReportDelta(150, -10);

            Assert.That(history.TotalRotation, Is.EqualTo(710f));
        }
    }
}
