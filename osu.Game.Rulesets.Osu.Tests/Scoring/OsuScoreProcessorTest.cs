// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Osu.Tests.Scoring
{
    [TestFixture]
    public class OsuScoreProcessorTest
    {
        private static IReadOnlyDictionary<HitResult, int> misses(int count) => new Dictionary<HitResult, int> { [HitResult.Miss] = count };

        [Test]
        public void TestRank_PerfectAccuracyWithoutMiss_StaysX()
        {
            // switch case X, Miss == 0 -> condition FALSE
            Assert.That(new OsuScoreProcessor().RankFromScore(1, misses(0)), Is.EqualTo(ScoreRank.X));
        }

        [Test]
        public void TestRank_SWithoutMiss_StaysS()
        {
            // switch case S, Miss == 0 -> condition FALSE
            Assert.That(new OsuScoreProcessor().RankFromScore(0.96, misses(0)), Is.EqualTo(ScoreRank.S));
        }

        [Test]
        public void TestRank_SWithMiss_DowngradedToA()
        {
            // switch case S, Miss > 0 -> condition TRUE
            Assert.That(new OsuScoreProcessor().RankFromScore(0.96, misses(1)), Is.EqualTo(ScoreRank.A));
        }

        [Test]
        public void TestRank_XWithMiss_DowngradedToA()
        {
            // switch case X, Miss > 0 -> condition TRUE
            Assert.That(new OsuScoreProcessor().RankFromScore(1, misses(1)), Is.EqualTo(ScoreRank.A));
        }

        [Test]
        public void TestRank_SWithoutMissKey_StaysS()
        {
            // GetValueOrDefault on absent key -> 0 -> condition FALSE
            Assert.That(new OsuScoreProcessor().RankFromScore(0.96, new Dictionary<HitResult, int>()), Is.EqualTo(ScoreRank.S));
        }

        [Test]
        public void TestRank_BelowSWithMiss_NotAffected()
        {
            // switch has no matching case for B: miss count is irrelevant
            Assert.That(new OsuScoreProcessor().RankFromScore(0.85, misses(3)), Is.EqualTo(ScoreRank.B));
        }
    }
}
