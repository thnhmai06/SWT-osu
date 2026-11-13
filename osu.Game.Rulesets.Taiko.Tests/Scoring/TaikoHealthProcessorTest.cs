// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko.Beatmaps;
using osu.Game.Rulesets.Taiko.Judgements;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Taiko.Scoring;

namespace osu.Game.Rulesets.Taiko.Tests.Scoring
{
    [TestFixture]
    public class TaikoHealthProcessorTest
    {
        private partial class TestHealthProcessor : TaikoHealthProcessor
        {
            public void CallApply(JudgementResult r) => ApplyResultInternal(r);
            public void CallRevert(JudgementResult r) => RevertResultInternal(r);
            public void CallReset(bool store) => Reset(store);
            public double IncreaseFor(JudgementResult r) => GetHealthIncreaseFor(r);
        }

        private static JudgementResult result(HitResult type) => new JudgementResult(new Hit(), new TaikoJudgement()) { Type = type };

        private static TaikoBeatmap beatmap(int hits, float drain)
        {
            var b = new TaikoBeatmap { Difficulty = new BeatmapDifficulty { DrainRate = drain } };
            for (int i = 0; i < hits; i++)
                b.HitObjects.Add(new Hit { StartTime = i * 100 });
            return b;
        }

        // CF: TaikoHealthProcessor.GetHealthIncreaseFor | result.IsHit | TRUE
        [Test]
        public void TestHitIncreaseScaledByObjectCountAndDrain()
        {
            var p = new TestHealthProcessor();
            p.ApplyBeatmap(beatmap(2, 5));
            // 3.0 / (3 * 2 * 0.75)
            Assert.That(p.IncreaseFor(result(HitResult.Great)), Is.EqualTo(3.0 / 4.5).Within(1e-9));
        }

        // CF: TaikoHealthProcessor.GetHealthIncreaseFor | result.IsHit | FALSE
        [Test]
        public void TestMissScaledByDrain()
        {
            var p = new TestHealthProcessor();
            p.ApplyBeatmap(beatmap(2, 5));
            Assert.That(p.IncreaseFor(result(HitResult.Miss)), Is.EqualTo(-1.0 * 0.0075).Within(1e-9));
        }

        // CF: TaikoHealthProcessor.ApplyBeatmap | Math.Max(1, hit count) | no throw
        [Test]
        public void TestEmptyBeatmapDoesNotDivideByZero()
        {
            var p = new TestHealthProcessor();
            p.ApplyBeatmap(beatmap(0, 5));
            Assert.That(p.IncreaseFor(result(HitResult.Great)), Is.EqualTo(3.0 / 2.25).Within(1e-9));
        }

        // CF: TaikoHealthProcessor.Reset | storeResults | TRUE
        // CF: TaikoHealthProcessor.Reset | sumOfMaxHealthIncreases == 0 | TRUE
        [Test]
        public void TestResetWithStoreAndNoResultsStartsFull()
        {
            var p = new TestHealthProcessor();
            p.CallReset(true);
            Assert.That(p.Health.Value, Is.EqualTo(1));
        }

        // CF: TaikoHealthProcessor.Reset | storeResults | FALSE
        [Test]
        public void TestResetWithoutStoreStartsEmpty()
        {
            var p = new TestHealthProcessor();
            p.CallReset(false);
            Assert.That(p.Health.Value, Is.EqualTo(0));
        }

        // CF: TaikoHealthProcessor.Reset | sumOfMaxHealthIncreases == 0 | FALSE
        // CF: TaikoHealthProcessor.ApplyResultInternal | sumOfMaxHealthIncreases += | no throw
        [Test]
        public void TestResetAfterResultsStartsEmpty()
        {
            var p = new TestHealthProcessor();
            p.CallApply(result(HitResult.Great));
            p.CallReset(true);
            Assert.That(p.Health.Value, Is.EqualTo(0));
        }

        // CF: TaikoHealthProcessor.RevertResultInternal | sumOfMaxHealthIncreases -= | no throw
        [Test]
        public void TestRevertRestoresSumSoResetStartsFull()
        {
            var p = new TestHealthProcessor();
            var r = result(HitResult.Great);
            p.CallApply(r);
            p.CallRevert(r);
            p.CallReset(true);
            Assert.That(p.Health.Value, Is.EqualTo(1));
        }
    }
}
