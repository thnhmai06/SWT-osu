// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko.Beatmaps;
using osu.Game.Rulesets.Taiko.Judgements;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Taiko.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Taiko.Tests.Scoring
{
    [TestFixture]
    public class TaikoScoreProcessorTest
    {
        private partial class TestScoreProcessor : TaikoScoreProcessor
        {
            public double LastCombo;
            public double LastBonus;

            public double Total(double combo, double accuracy, double bonus) => ComputeTotalScore(combo, accuracy, bonus);

            protected override double GetComboScoreChange(JudgementResult result) => LastCombo = base.GetComboScoreChange(result);
            protected override double GetBonusScoreChange(JudgementResult result) => LastBonus = base.GetBonusScoreChange(result);
        }

        private static TestScoreProcessor create()
        {
            var p = new TestScoreProcessor();
            p.ApplyBeatmap(new TaikoBeatmap());
            return p;
        }

        private static IReadOnlyDictionary<HitResult, int> results(int miss) => new Dictionary<HitResult, int> { [HitResult.Miss] = miss };

        // CF: TaikoScoreProcessor.ComputeTotalScore | sequential | no throw
        [Test]
        public void TestTotalScoreFormula()
        {
            // full accuracy (default 1): 250000 * 0.5 + 750000 * 1 * 1 + 100
            Assert.That(create().Total(0.5, 1, 100), Is.EqualTo(875100).Within(1e-6));
        }

        // CF: TaikoScoreProcessor.GetBaseScoreForResult | case Ok | case Ok
        [Test]
        public void TestOkBaseScore() => Assert.That(new TaikoScoreProcessor().GetBaseScoreForResult(HitResult.Ok), Is.EqualTo(150));

        // CF: TaikoScoreProcessor.GetBaseScoreForResult | switch (result) | default
        [Test]
        public void TestGreatBaseScoreIsDefault() => Assert.That(new TaikoScoreProcessor().GetBaseScoreForResult(HitResult.Great), Is.EqualTo(300));

        // CF: TaikoScoreProcessor.RankFromScore | case S | case S
        // CF: TaikoScoreProcessor.RankFromScore | case X | case X
        // CF: TaikoScoreProcessor.RankFromScore | results.GetValueOrDefault(HitResult.Miss) > 0 | TRUE
        [TestCase(0.97)]
        [TestCase(1.0)]
        public void TestHighRankWithMissDropsToA(double accuracy)
            => Assert.That(new TaikoScoreProcessor().RankFromScore(accuracy, results(1)), Is.EqualTo(ScoreRank.A));

        // CF: TaikoScoreProcessor.RankFromScore | results.GetValueOrDefault(HitResult.Miss) > 0 | FALSE
        [Test]
        public void TestHighRankWithoutMissKept()
        {
            Assert.That(new TaikoScoreProcessor().RankFromScore(1.0, results(0)), Is.EqualTo(ScoreRank.X));
            Assert.That(new TaikoScoreProcessor().RankFromScore(0.97, results(0)), Is.EqualTo(ScoreRank.S));
        }

        // CF: TaikoScoreProcessor.RankFromScore | switch (rank) | default
        [Test]
        public void TestLowerRankUnaffectedByMiss()
            => Assert.That(new TaikoScoreProcessor().RankFromScore(0.9, results(5)), Is.EqualTo(ScoreRank.A).Or.EqualTo(ScoreRank.B));

        // CF: TaikoScoreProcessor.strongScaleValue | result.HitObject is StrongNestedHitObject | FALSE
        // CF: TaikoScoreProcessor.GetComboScoreChange | Math.Max(0.5, Log) | TRUE
        [Test]
        public void TestFirstComboUsesFloorOfHalf()
        {
            var p = create();
            p.ApplyResult(new JudgementResult(new Hit(), new TaikoJudgement()) { Type = HitResult.Great });
            Assert.That(p.LastCombo, Is.EqualTo(300 * 0.5).Within(1e-9));
        }

        // CF: TaikoScoreProcessor.GetComboScoreChange | Math.Max(0.5, Log) | FALSE
        [Test]
        public void TestMidComboUsesLogarithm()
        {
            var p = create();
            for (int i = 0; i < 16; i++)
                p.ApplyResult(new JudgementResult(new Hit(), new TaikoJudgement()) { Type = HitResult.Great });
            Assert.That(p.LastCombo, Is.EqualTo(300 * 2.0).Within(1e-9)); // log4(16) = 2
        }

        // CF: TaikoScoreProcessor.GetComboScoreChange | Math.Min cap at Log(400) | TRUE
        [Test]
        public void TestHugeComboIsCapped()
        {
            var p = create();
            for (int i = 0; i < 1000; i++)
                p.ApplyResult(new JudgementResult(new Hit(), new TaikoJudgement()) { Type = HitResult.Great });
            Assert.That(p.LastCombo, Is.EqualTo(300 * System.Math.Log(400, 4)).Within(1e-9));
        }

        // CF: TaikoScoreProcessor.strongScaleValue | result.HitObject is StrongNestedHitObject | TRUE
        // CF: TaikoScoreProcessor.strongScaleValue | strong.Parent is DrumRollTick | FALSE
        [Test]
        public void TestStrongHitBonusIsScaledBySeven()
        {
            var p = create();
            var strong = new Hit.StrongNestedHit(new Hit());
            p.ApplyResult(new JudgementResult(strong, strong.CreateJudgement()) { Type = strong.CreateJudgement().MaxResult });
            Assert.That(p.LastBonus, Is.EqualTo(p.GetBaseScoreForResult(strong.CreateJudgement().MaxResult) * 7).Within(1e-9));
        }

        // CF: TaikoScoreProcessor.strongScaleValue | strong.Parent is DrumRollTick | TRUE
        [Test]
        public void TestStrongTickBonusIsScaledByThree()
        {
            var p = create();
            var strong = new DrumRollTick.StrongNestedHit(new DrumRollTick(new DrumRoll()));
            p.ApplyResult(new JudgementResult(strong, strong.CreateJudgement()) { Type = strong.CreateJudgement().MaxResult });
            Assert.That(p.LastBonus, Is.EqualTo(p.GetBaseScoreForResult(strong.CreateJudgement().MaxResult) * 3).Within(1e-9));
        }
    }
}
