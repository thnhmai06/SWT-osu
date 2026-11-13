// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Mania.Judgements;
using osu.Game.Rulesets.Mania.Objects;

namespace osu.Game.Rulesets.Mania.Tests.Judgements
{
    [TestFixture]
    public class HoldNoteJudgementResultTest
    {
        private static HoldNoteJudgementResult create()
        {
            var hold = new HoldNote();
            return new HoldNoteJudgementResult(hold, hold.CreateJudgement());
        }

        // CF: HoldNoteJudgementResult.IsHolding | initial state (-inf, false) | FALSE
        // CF: HoldNoteJudgementResult.getLastReport | while Peek().time > currentTime | loop 0
        // CF: HoldNoteJudgementResult.DroppedHoldAfter | foreach state | loop 1
        // CF: HoldNoteJudgementResult.DroppedHoldAfter | state.time >= time | FALSE
        [Test]
        public void TestInitialState()
        {
            var r = create();
            Assert.That(r.IsHolding(0), Is.False);
            Assert.That(r.DroppedHoldAfter(0), Is.False);
        }

        // CF: HoldNoteJudgementResult.ReportHoldState | holding != lastReport.holding | TRUE
        // CF: HoldNoteJudgementResult.IsHolding | holding reported | TRUE
        // CF: HoldNoteJudgementResult.DroppedHoldAfter | foreach state | loop n
        // CF: HoldNoteJudgementResult.DroppedHoldAfter | state.time >= time && !state.holding | FALSE
        // CF: HoldNoteJudgementResult.DroppedHoldAfter | !state.holding | FALSE
        [Test]
        public void TestHoldReported()
        {
            var r = create();
            r.ReportHoldState(10, true);

            Assert.That(r.IsHolding(10), Is.True);
            Assert.That(r.IsHolding(100), Is.True);
            Assert.That(r.DroppedHoldAfter(5), Is.False);
        }

        // CF: HoldNoteJudgementResult.ReportHoldState | holding != lastReport.holding | FALSE
        [Test]
        public void TestRepeatedSameStateIgnored()
        {
            var r = create();
            r.ReportHoldState(10, false);
            r.ReportHoldState(20, true);
            r.ReportHoldState(30, true);

            // if the redundant report at 30 were recorded, rewinding to 25 would still be fine, but a drop at 30 must not exist.
            Assert.That(r.DroppedHoldAfter(21), Is.False);
        }

        // CF: HoldNoteJudgementResult.getLastReport | while Peek().time > currentTime | loop n
        [Test]
        public void TestQueryingEarlierTimeRewindsState()
        {
            var r = create();
            r.ReportHoldState(10, true);
            r.ReportHoldState(20, false);

            Assert.That(r.IsHolding(15), Is.True);
            Assert.That(r.IsHolding(5), Is.False);
        }

        // CF: HoldNoteJudgementResult.DroppedHoldAfter | state.time >= time && !state.holding | TRUE
        // CF: HoldNoteJudgementResult.DroppedHoldAfter | !state.holding | TRUE
        [Test]
        public void TestDroppedHoldAfter()
        {
            var r = create();
            r.ReportHoldState(10, true);
            r.ReportHoldState(20, false);

            Assert.That(r.DroppedHoldAfter(15), Is.True);
            Assert.That(r.DroppedHoldAfter(20), Is.True);
            Assert.That(r.DroppedHoldAfter(21), Is.False);
        }
    }
}
