// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Tests.Rulesets.Scoring
{
    [TestFixture]
    public class DrainingHealthProcessorTest
    {
        private class TestDrainingProcessor : DrainingHealthProcessor
        {
            public TestDrainingProcessor(double drainStartTime, double lenience = 0)
                : base(drainStartTime, lenience)
            {
            }

            public double Lenience => DrainLenience;
            public double StartTime => DrainStartTime;
            public bool DefaultFail(JudgementResult r) => CheckDefaultFailCondition(r);
        }

        private class BonusJudgement : Judgement
        {
            public override HitResult MaxResult => HitResult.LargeBonus;
        }

        private class BonusObject : HitObject
        {
            protected override HitWindows CreateHitWindows() => HitWindows.Empty;

            public override Judgement CreateJudgement() => new BonusJudgement();
        }

        private static Beatmap beatmapWith(params double[] times)
        {
            var beatmap = new Beatmap();
            foreach (double t in times)
                beatmap.HitObjects.Add(new HitObject { StartTime = t });
            return beatmap;
        }

        // CF: DrainingHealthProcessor.DrainingHealthProcessor | Math.Clamp(drainLenience, 0, 1) | no throw
        [Test]
        public void TestLenienceIsClamped()
        {
            Assert.That(new TestDrainingProcessor(5, -1).Lenience, Is.Zero);
            Assert.That(new TestDrainingProcessor(5, 0.4).Lenience, Is.EqualTo(0.4));
            Assert.That(new TestDrainingProcessor(5, 2).Lenience, Is.EqualTo(1));
            Assert.That(new TestDrainingProcessor(5).StartTime, Is.EqualTo(5));
        }

        // CF: DrainingHealthProcessor.ComputeDrainRate | healthIncreases.Count <= 1 | TRUE
        // CF: DrainingHealthProcessor.ApplyBeatmap | beatmap.HitObjects.Count > 0 | FALSE
        [Test]
        public void TestNoDrainWithFewerThanTwoJudgements()
        {
            var empty = new TestDrainingProcessor(0);
            empty.ApplyBeatmap(new Beatmap());
            Assert.That(empty.DrainRate, Is.Zero);

            var single = new TestDrainingProcessor(0);
            single.ApplyBeatmap(beatmapWith(1000));
            Assert.That(single.DrainRate, Is.Zero);
        }

        // CF: DrainingHealthProcessor.ComputeDrainRate | healthIncreases.Count <= 1 | FALSE
        // CF: DrainingHealthProcessor.ComputeDrainRate | while adjustment > 0 | loop n
        // CF: DrainingHealthProcessor.ComputeDrainRate | for healthIncreases | loop n
        // CF: DrainingHealthProcessor.ComputeDrainRate | Math.Abs(lowestHealth - target) <= minimum_health_error | TRUE
        // CF: DrainingHealthProcessor.ApplyBeatmap | beatmap.HitObjects.Count > 0 | TRUE
        // CF: DrainingHealthProcessor.ApplyResultInternal | IsSimulating && !IsBonus | TRUE
        // CF: DrainingHealthProcessor.Reset | storeResults | TRUE
        [Test]
        public void TestDrainRateTargetsMinimumHealth()
        {
            var processor = new TestDrainingProcessor(0);
            processor.ApplyBeatmap(beatmapWith(1000, 2000));

            // Default drain rate 5 targets a lowest health of 0.9.
            // Two perfect judgements (+0.0525 each) separated by 1000ms: the lowest point is after the second drain period:
            //   1 - x -> (+0.0525, capped at 1) -> 1.0525 - 2x, with x = 1000 * drainRate.
            double x = processor.DrainRate * 1000;
            Assert.That(1.0525 - 2 * x, Is.EqualTo(0.9).Within(0.011));
        }

        // CF: DrainingHealthProcessor.ComputeDrainRate | currentBreak < Breaks.Count && Breaks[currentBreak].EndTime <= currentTime | TRUE
        // CF: DrainingHealthProcessor.ComputeDrainRate | lowestHealth < 0 | FALSE
        [Test]
        public void TestNoDrainAcrossBreaks()
        {
            var beatmap = beatmapWith(1000, 2000);
            beatmap.Breaks.Add(new BreakPeriod(1100, 1900));

            var processor = new TestDrainingProcessor(0);
            processor.ApplyBeatmap(beatmap);

            // only the first 1000ms is drained, so the minimum is 1 - 1000r.
            Assert.That(1 - processor.DrainRate * 1000, Is.EqualTo(0.9).Within(0.011));
        }

        // CF: DrainingHealthProcessor.ApplyResultInternal | IsSimulating && !IsBonus | FALSE
        [Test]
        public void TestBonusJudgementsDoNotContributeToDrain()
        {
            var beatmap = new Beatmap { HitObjects = { new HitObject { StartTime = 1000 }, new BonusObject { StartTime = 1500 }, new BonusObject { StartTime = 2000 } } };

            var processor = new TestDrainingProcessor(0);
            processor.ApplyBeatmap(beatmap);

            Assert.That(processor.DrainRate, Is.Zero, "only one non-bonus judgement remains");
        }

        // CF: DrainingHealthProcessor.CheckDefaultFailCondition | MaxResult.IsBonus() | TRUE
        // CF: DrainingHealthProcessor.CheckDefaultFailCondition | Type == IgnoreHit | TRUE
        // CF: DrainingHealthProcessor.CheckDefaultFailCondition | falls through to base | FALSE
        [Test]
        public void TestDefaultFailCondition()
        {
            var processor = new TestDrainingProcessor(0);
            processor.Health.Value = 0;

            Assert.That(processor.DefaultFail(new JudgementResult(new BonusObject(), new BonusJudgement()) { Type = HitResult.LargeBonus }), Is.False);
            Assert.That(processor.DefaultFail(new JudgementResult(new HitObject(), new Judgement()) { Type = HitResult.IgnoreHit }), Is.False);
            Assert.That(processor.DefaultFail(new JudgementResult(new HitObject(), new Judgement()) { Type = HitResult.Great }), Is.True, "zero health fails normal judgements");

            processor.Health.Value = 0.5;
            Assert.That(processor.DefaultFail(new JudgementResult(new HitObject(), new Judgement()) { Type = HitResult.Great }), Is.False);
        }
    }
}
