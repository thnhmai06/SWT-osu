// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Models;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;

namespace osu.Game.Tests.Scoring
{
    [TestFixture]
    public class LegacyReplaySoloScoreInfoTest
    {
        private static ScoreInfo score() => new ScoreInfo(realmUser: new RealmUser { OnlineID = 42, Username = "u" });

        [Test]
        public void TestDefaults()
        {
            var info = new LegacyReplaySoloScoreInfo();

            Assert.That((info.OnlineID, info.UserID, info.TotalScoreWithoutMods, info.Rank), Is.EqualTo((-1L, -1, (long?)null, (ScoreRank?)null)));
            Assert.That(info.Mods, Is.Empty);
            Assert.That(info.Pauses, Is.Empty);
        }

        [Test]
        public void TestFromScoreCopiesScalarFields()
        {
            var s = score();
            s.OnlineID = 99;
            s.ClientVersion = "2026.1";
            s.Rank = ScoreRank.S;

            var info = LegacyReplaySoloScoreInfo.FromScore(s);

            Assert.That((info.OnlineID, info.ClientVersion, info.Rank, info.UserID), Is.EqualTo((99L, "2026.1", (ScoreRank?)ScoreRank.S, 42)));
        }

        [Test]
        public void TestZeroStatisticsAreDropped()
        {
            var s = score();
            s.Statistics = new System.Collections.Generic.Dictionary<HitResult, int> { [HitResult.Great] = 10, [HitResult.Miss] = 0, [HitResult.Ok] = 2 };
            s.MaximumStatistics = new System.Collections.Generic.Dictionary<HitResult, int> { [HitResult.Great] = 12, [HitResult.LargeBonus] = 0 };

            var info = LegacyReplaySoloScoreInfo.FromScore(s);

            Assert.That(info.Statistics, Is.EquivalentTo(new System.Collections.Generic.Dictionary<HitResult, int> { [HitResult.Great] = 10, [HitResult.Ok] = 2 }));
            Assert.That(info.MaximumStatistics, Is.EquivalentTo(new System.Collections.Generic.Dictionary<HitResult, int> { [HitResult.Great] = 12 }));
        }

        // CF: LegacyReplaySoloScoreInfo.FromScore | score.TotalScoreWithoutMods > 0 | TRUE
        [Test]
        public void TestPositiveTotalScoreWithoutModsKept()
        {
            var s = score();
            s.TotalScoreWithoutMods = 12345;

            Assert.That(LegacyReplaySoloScoreInfo.FromScore(s).TotalScoreWithoutMods, Is.EqualTo(12345));
        }

        // CF: LegacyReplaySoloScoreInfo.FromScore | score.TotalScoreWithoutMods > 0 | FALSE
        [Test]
        public void TestZeroTotalScoreWithoutModsBecomesNull()
        {
            Assert.That(LegacyReplaySoloScoreInfo.FromScore(score()).TotalScoreWithoutMods, Is.Null);
        }

        [Test]
        public void TestPausesCopied()
        {
            var s = score();
            s.Pauses.Add(1000);
            s.Pauses.Add(5000);

            var info = LegacyReplaySoloScoreInfo.FromScore(s);

            Assert.That(info.Pauses, Is.EqualTo(new[] { 1000, 5000 }));
            s.Pauses.Clear();
            Assert.That(info.Pauses, Has.Length.EqualTo(2), "copied, not aliased");
        }
    }
}
