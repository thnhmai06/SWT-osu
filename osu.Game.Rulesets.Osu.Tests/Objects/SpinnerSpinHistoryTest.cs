// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Osu.Objects.Drawables;

namespace osu.Game.Rulesets.Osu.Tests.Objects
{
    [TestFixture]
    public class SpinnerSpinHistoryTest
    {
        private SpinnerSpinHistory history = null!;

        [SetUp]
        public void SetUp() => history = new SpinnerSpinHistory();

        [Test]
        public void TestZeroDelta_IsIgnored()
        {
            // delta == 0 TRUE: early return
            history.ReportDelta(0, 0);

            Assert.That(history.TotalRotation, Is.EqualTo(0));
        }

        [Test]
        public void TestForwardPositiveDelta_AccumulatesRotation()
        {
            // delta == 0 FALSE; currentTime >= lastReportTime TRUE; max < 360
            history.ReportDelta(0, 90);

            Assert.That(history.TotalRotation, Is.EqualTo(90).Within(1e-3));
        }

        [Test]
        public void TestForwardNegativeDelta_CountsAbsoluteRotation()
        {
            history.ReportDelta(0, -90);

            Assert.That(history.TotalRotation, Is.EqualTo(90).Within(1e-3));
        }

        [Test]
        public void TestForwardBacktracking_KeepsFurthestRotation()
        {
            // Math.Max keeps the previous maximum when the spin retreats
            history.ReportDelta(0, 90);
            history.ReportDelta(10, -45);

            Assert.That(history.TotalRotation, Is.EqualTo(90).Within(1e-3));
        }

        [Test]
        public void TestIncompleteSpin_DoesNotEnterCompletionLoop()
        {
            // while (max >= 360): FALSE on first evaluation (359 < 360)
            history.ReportDelta(0, 359);

            Assert.That(history.TotalRotation, Is.EqualTo(359).Within(1e-3));
        }

        [Test]
        public void TestExactFullSpin_CompletesOnce()
        {
            // while (max >= 360): TRUE once (boundary), then FALSE with zero remainder
            history.ReportDelta(0, 90);
            history.ReportDelta(1, 90);
            history.ReportDelta(2, 90);
            history.ReportDelta(3, 90);

            Assert.That(history.TotalRotation, Is.EqualTo(360).Within(1e-3));
        }

        [Test]
        public void TestNegativeFullSpin_CompletesWithNegativeDirection()
        {
            // Math.Sign(currentSpinRotation) == -1
            for (int i = 0; i < 4; i++)
                history.ReportDelta(i, -90);

            Assert.That(history.TotalRotation, Is.EqualTo(360).Within(1e-3));
        }

        [Test]
        public void TestOvershoot_CarriesRemainderIntoNextSpin()
        {
            history.ReportDelta(0, 400);

            Assert.That(history.TotalRotation, Is.EqualTo(400).Within(1e-3));
        }

        [Test]
        public void TestHugeDelta_CompletesMultipleSpinsInOneReport()
        {
            // while loop repeats (TRUE twice) before terminating
            history.ReportDelta(0, 720);

            Assert.That(history.TotalRotation, Is.EqualTo(720).Within(1e-3));
        }

        [Test]
        public void TestRewind_UndoesSpinCompletedAfterRewoundTime()
        {
            // currentTime >= lastReportTime FALSE; completion (t=100) > currentTime (50) -> popped
            history.ReportDelta(100, 360);
            Assert.That(history.TotalRotation, Is.EqualTo(360).Within(1e-3));

            history.ReportDelta(50, -90);

            Assert.That(history.TotalRotation, Is.EqualTo(270).Within(1e-3));
        }

        [Test]
        public void TestRewind_WithoutCompletedSpins_RecomputesCurrentSpin()
        {
            // TryPeek FALSE (empty stack)
            history.ReportDelta(100, 90);
            history.ReportDelta(50, -30);

            Assert.That(history.TotalRotation, Is.EqualTo(60).Within(1e-3));
        }

        [Test]
        public void TestRewind_KeepsSpinCompletedBeforeRewoundTime()
        {
            // TryPeek TRUE but CompletionTime > currentTime FALSE (100 > 150)
            history.ReportDelta(100, 360);
            history.ReportDelta(200, 90);
            history.ReportDelta(150, -10);

            Assert.That(history.TotalRotation, Is.EqualTo(360 + 80).Within(1e-3));
        }

        [Test]
        public void TestSameTimeReports_AreTreatedAsForward()
        {
            // currentTime >= lastReportTime TRUE at equality
            history.ReportDelta(10, 100);
            history.ReportDelta(10, 100);

            Assert.That(history.TotalRotation, Is.EqualTo(200).Within(1e-3));
        }
    }
}
