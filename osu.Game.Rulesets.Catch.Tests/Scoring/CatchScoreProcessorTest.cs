// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Rulesets.Catch.Beatmaps;
using osu.Game.Rulesets.Catch.Judgements;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.Scoring;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Catch.Tests.Scoring
{
    [TestFixture]
    public class CatchScoreProcessorTest
    {
        private class TestScoreProcessor : CatchScoreProcessor
        {
            public readonly List<double> ComboScoreChanges = new List<double>();

            public double Total(double comboProgress, double accuracyProgress, double bonus) => ComputeTotalScore(comboProgress, accuracyProgress, bonus);

            protected override double GetComboScoreChange(JudgementResult result)
            {
                double change = base.GetComboScoreChange(result);
                ComboScoreChanges.Add(change);
                return change;
            }
        }

        private static TestScoreProcessor createProcessor(params CatchHitObject[] hitObjects)
        {
            var beatmap = new CatchBeatmap();
            beatmap.HitObjects.AddRange(hitObjects);
            var processor = new TestScoreProcessor();
            processor.ApplyBeatmap(beatmap);
            return processor;
        }

        private static void apply(TestScoreProcessor processor, CatchHitObject hitObject, HitResult type)
            => processor.ApplyResult(new CatchJudgementResult(hitObject, hitObject.CreateJudgement()) { Type = type });

        // CF: CatchScoreProcessor.Reset | fruitTinyScaleDivisor == 0 | TRUE
        // CF: CatchScoreProcessor.ComputeTotalScore | MaximumResultCounts.GetValueOrDefault(SmallTickHit) == 0 | TRUE
        [Test]
        public void TestTotalScoreWithoutTinyDroplets()
        {
            var processor = createProcessor(new Fruit());

            Assert.That(processor.Total(0.5, 1, 1234), Is.EqualTo(500000 + 1234).Within(1e-6));
        }

        // CF: CatchScoreProcessor.Reset | fruitTinyScaleDivisor == 0 | FALSE
        // CF: CatchScoreProcessor.ComputeTotalScore | MaximumResultCounts.GetValueOrDefault(SmallTickHit) == 0 | FALSE
        [Test]
        public void TestTotalScoreWithTinyDropletsMissed()
        {
            // 1 fruit + 1 tiny droplet: tiny droplets are worth half (scale 0.5) of the 400000 portion.
            var processor = createProcessor(new Fruit(), new TinyDroplet());

            Assert.That(processor.Total(1, 1, 0), Is.EqualTo(800000).Within(1e-6));
        }

        [Test]
        public void TestTotalScoreWithTinyDropletsHit()
        {
            var tiny = new TinyDroplet();
            var processor = createProcessor(new Fruit(), tiny);
            apply(processor, tiny, HitResult.SmallTickHit);

            Assert.That(processor.Total(1, 1, 0), Is.EqualTo(1000000).Within(1e-6));
        }

        // CF: CatchScoreProcessor.GetBaseScoreForResult | switch result | case Great
        // CF: CatchScoreProcessor.GetBaseScoreForResult | switch result | case LargeTickHit
        // CF: CatchScoreProcessor.GetBaseScoreForResult | switch result | case SmallTickHit
        [TestCase(HitResult.Great)]
        [TestCase(HitResult.LargeTickHit)]
        [TestCase(HitResult.SmallTickHit)]
        public void TestBaseScoreOfAccuracyResultsIsEqual(HitResult result)
        {
            Assert.That(new CatchScoreProcessor().GetBaseScoreForResult(result), Is.EqualTo(300));
        }

        // CF: CatchScoreProcessor.GetBaseScoreForResult | switch result | case LargeBonus
        [Test]
        public void TestBaseScoreOfLargeBonus()
        {
            Assert.That(new CatchScoreProcessor().GetBaseScoreForResult(HitResult.LargeBonus), Is.EqualTo(200));
        }

        // CF: CatchScoreProcessor.GetBaseScoreForResult | switch result | default
        [Test]
        public void TestBaseScoreOfOtherResultsDefersToBase()
        {
            Assert.That(new CatchScoreProcessor().GetBaseScoreForResult(HitResult.Miss), Is.Zero);
        }

        // CF: CatchScoreProcessor.GetComboScoreChange | switch result.Type | case Great
        // CF: CatchScoreProcessor.GetComboScoreChange | switch result.Type | case LargeTickHit
        // CF: CatchScoreProcessor.GetComboScoreChange | switch result.Type | default
        // CF: CatchScoreProcessor.GetComboScoreChange | Math.Max(0.5, Math.Log(combo, 4)) | 0.5 side at combo 1 and log side at combo 16
        // CF: CatchScoreProcessor.GetComboScoreChange | Math.Min(.., Math.Log(combo_cap, 4)) | cap side at combo > 200
        [Test]
        public void TestComboScoreChange()
        {
            var fruit = new Fruit();
            var droplet = new Droplet();
            var processor = createProcessor(fruit, droplet);

            apply(processor, fruit, HitResult.Great); // combo 1: log4(1) = 0, floored to 0.5
            Assert.That(processor.ComboScoreChanges[^1], Is.EqualTo(300 * 0.5).Within(1e-6));

            apply(processor, droplet, HitResult.LargeTickHit); // combo 2
            Assert.That(processor.ComboScoreChanges[^1], Is.EqualTo(100 * 0.5).Within(1e-6));

            for (int i = 3; i < 16; i++)
                apply(processor, fruit, HitResult.Great);

            apply(processor, fruit, HitResult.Great); // combo 16: log4(16) = 2
            Assert.That(processor.ComboScoreChanges[^1], Is.EqualTo(300 * 2).Within(1e-6));

            for (int i = 17; i < 300; i++)
                apply(processor, fruit, HitResult.Great);

            apply(processor, fruit, HitResult.Great); // combo 300: capped at log4(200)
            Assert.That(processor.ComboScoreChanges[^1], Is.EqualTo(300 * Math.Log(200, 4)).Within(1e-6));
        }

        [Test]
        public void TestComboScoreChangeOfOtherResultIsZero()
        {
            var tiny = new TinyDroplet();
            var processor = createProcessor(tiny);
            apply(processor, tiny, HitResult.SmallTickHit);

            // SmallTickHit is scorable but is neither Great nor LargeTickHit, so does not contribute to the combo portion.
            Assert.That(processor.ComboScoreChanges[^1], Is.Zero);
        }

        // CF: CatchScoreProcessor.RankFromScore | accuracy == accuracy_cutoff_x | TRUE
        // CF: CatchScoreProcessor.RankFromScore | accuracy >= accuracy_cutoff_s | TRUE
        // CF: CatchScoreProcessor.RankFromScore | accuracy >= accuracy_cutoff_a | TRUE
        // CF: CatchScoreProcessor.RankFromScore | accuracy >= accuracy_cutoff_b | TRUE
        // CF: CatchScoreProcessor.RankFromScore | accuracy >= accuracy_cutoff_c | TRUE
        // CF: CatchScoreProcessor.RankFromScore | accuracy == accuracy_cutoff_x | FALSE
        // CF: CatchScoreProcessor.RankFromScore | accuracy >= accuracy_cutoff_s | FALSE
        // CF: CatchScoreProcessor.RankFromScore | accuracy >= accuracy_cutoff_a | FALSE
        // CF: CatchScoreProcessor.RankFromScore | accuracy >= accuracy_cutoff_b | FALSE
        // CF: CatchScoreProcessor.RankFromScore | accuracy >= accuracy_cutoff_c | FALSE
        [TestCase(1.0, ScoreRank.X)]
        [TestCase(0.99, ScoreRank.S)]
        [TestCase(0.98, ScoreRank.S)]
        [TestCase(0.97, ScoreRank.A)]
        [TestCase(0.94, ScoreRank.A)]
        [TestCase(0.93, ScoreRank.B)]
        [TestCase(0.90, ScoreRank.B)]
        [TestCase(0.89, ScoreRank.C)]
        [TestCase(0.85, ScoreRank.C)]
        [TestCase(0.84, ScoreRank.D)]
        [TestCase(0.0, ScoreRank.D)]
        public void TestRankFromScore(double accuracy, ScoreRank expected)
        {
            Assert.That(new CatchScoreProcessor().RankFromScore(accuracy, new Dictionary<HitResult, int>()), Is.EqualTo(expected));
        }

        // CF: CatchScoreProcessor.AccuracyCutoffFromRank | switch rank | case X
        // CF: CatchScoreProcessor.AccuracyCutoffFromRank | switch rank | case XH
        // CF: CatchScoreProcessor.AccuracyCutoffFromRank | switch rank | case S
        // CF: CatchScoreProcessor.AccuracyCutoffFromRank | switch rank | case SH
        // CF: CatchScoreProcessor.AccuracyCutoffFromRank | switch rank | case A
        // CF: CatchScoreProcessor.AccuracyCutoffFromRank | switch rank | case B
        // CF: CatchScoreProcessor.AccuracyCutoffFromRank | switch rank | case C
        // CF: CatchScoreProcessor.AccuracyCutoffFromRank | switch rank | case D
        [TestCase(ScoreRank.X, 1.0)]
        [TestCase(ScoreRank.XH, 1.0)]
        [TestCase(ScoreRank.S, 0.98)]
        [TestCase(ScoreRank.SH, 0.98)]
        [TestCase(ScoreRank.A, 0.94)]
        [TestCase(ScoreRank.B, 0.9)]
        [TestCase(ScoreRank.C, 0.85)]
        [TestCase(ScoreRank.D, 0.0)]
        public void TestAccuracyCutoffFromRank(ScoreRank rank, double expected)
        {
            Assert.That(new CatchScoreProcessor().AccuracyCutoffFromRank(rank), Is.EqualTo(expected));
        }

        // CF: CatchScoreProcessor.AccuracyCutoffFromRank | switch rank | default
        // CF: CatchScoreProcessor.AccuracyCutoffFromRank | switch rank | throws ArgumentOutOfRangeException
        [Test]
        public void TestAccuracyCutoffOfUnsupportedRankThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CatchScoreProcessor().AccuracyCutoffFromRank(ScoreRank.F));
        }
    }
}
