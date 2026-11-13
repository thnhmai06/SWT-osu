// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Osu.Judgements;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Scoring
{
    [TestFixture]
    public class OsuScoreProcessorTest
    {
        private static readonly IReadOnlyDictionary<HitResult, int> no_misses = new Dictionary<HitResult, int>();
        private static readonly IReadOnlyDictionary<HitResult, int> one_miss = new Dictionary<HitResult, int> { [HitResult.Miss] = 1 };

        private readonly OsuScoreProcessor processor = new OsuScoreProcessor();

        // CF: OsuScoreProcessor.RankFromScore | switch (rank) | case S
        // CF: OsuScoreProcessor.RankFromScore | results.GetValueOrDefault(HitResult.Miss) > 0 | TRUE
        [Test]
        public void TestSWithMissIsDowngradedToA()
        {
            Assert.That(processor.RankFromScore(0.96, one_miss), Is.EqualTo(ScoreRank.A));
        }

        // CF: OsuScoreProcessor.RankFromScore | switch (rank) | case X
        [Test]
        public void TestXWithMissIsDowngradedToA()
        {
            Assert.That(processor.RankFromScore(1, one_miss), Is.EqualTo(ScoreRank.A));
        }

        // CF: OsuScoreProcessor.RankFromScore | results.GetValueOrDefault(HitResult.Miss) > 0 | FALSE
        [Test]
        public void TestSAndXWithoutMissAreKept()
        {
            Assert.That(processor.RankFromScore(0.96, no_misses), Is.EqualTo(ScoreRank.S));
            Assert.That(processor.RankFromScore(1, no_misses), Is.EqualTo(ScoreRank.X));
        }

        // CF: OsuScoreProcessor.RankFromScore | switch (rank) | default
        [Test]
        public void TestLowerRanksAreUnaffectedByMisses()
        {
            Assert.That(processor.RankFromScore(0.92, one_miss), Is.EqualTo(ScoreRank.A));
            Assert.That(processor.RankFromScore(0.5, one_miss), Is.EqualTo(ScoreRank.D));
        }

        // CF: OsuScoreProcessor.CreateHitEvent | result as OsuHitCircleJudgementResult | non-null
        [Test]
        public void TestHitEventCarriesCursorPosition()
        {
            var circle = new HitCircle();
            var result = new OsuHitCircleJudgementResult(circle, circle.Judgement) { Type = HitResult.Great, CursorPositionAtHit = new Vector2(12, 34) };

            Assert.That(new Probe().Create(result).Position, Is.EqualTo(new Vector2(12, 34)));
        }

        // CF: OsuScoreProcessor.CreateHitEvent | result as OsuHitCircleJudgementResult | null
        [Test]
        public void TestHitEventWithoutCursorPosition()
        {
            var circle = new HitCircle();
            var result = new JudgementResult(circle, circle.Judgement) { Type = HitResult.Great };

            Assert.That(new Probe().Create(result).Position, Is.Null);
        }

        private class Probe : OsuScoreProcessor
        {
            public HitEvent Create(JudgementResult result) => CreateHitEvent(result);
        }
    }
}
