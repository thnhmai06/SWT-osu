// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Multiplayer.MatchTypes.Matchmaking;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Tests.Online.Multiplayer.MatchTypes.Matchmaking
{
    [TestFixture]
    public class MatchmakingTest
    {
        private static MatchmakingUser user(int id, int points = 0, DateTimeOffset? abandoned = null, params (int round, int placement)[] rounds)
        {
            var u = new MatchmakingUser { UserId = id, Points = points, AbandonedAt = abandoned };
            foreach (var (round, placement) in rounds)
                u.Rounds.GetOrAdd(round).Placement = placement;
            return u;
        }

        // CF: MatchmakingUserComparer.Compare | x == null | throws ArgumentNullException
        [Test]
        public void TestCompareNullXThrows() => Assert.Throws<ArgumentNullException>(() => new MatchmakingUserComparer(1).Compare(null, user(1)));

        // CF: MatchmakingUserComparer.Compare | y == null | throws ArgumentNullException
        [Test]
        public void TestCompareNullYThrows() => Assert.Throws<ArgumentNullException>(() => new MatchmakingUserComparer(1).Compare(user(1), null));

        // CF: MatchmakingUserComparer.Compare | abandonedAt compare != 0 | TRUE
        // CF: MatchmakingUserComparer.compareAbandonedAt | AbandonedAt ?? MaxValue | non-null
        // CF: MatchmakingUserComparer.compareAbandonedAt | AbandonedAt ?? MaxValue | null
        [Test]
        public void TestAbandonedUsersSortAfterActiveUsers()
        {
            var comparer = new MatchmakingUserComparer(1);
            var abandoned = user(1, 100, DateTimeOffset.UnixEpoch);
            var active = user(2, 0);

            Assert.That(comparer.Compare(abandoned, active), Is.GreaterThan(0));
            Assert.That(comparer.Compare(active, abandoned), Is.LessThan(0));
        }

        // CF: MatchmakingUserComparer.Compare | abandonedAt compare != 0 | FALSE
        // CF: MatchmakingUserComparer.Compare | points compare != 0 | TRUE
        [Test]
        public void TestMorePointsSortFirst() => Assert.That(new MatchmakingUserComparer(1).Compare(user(1, 10), user(2, 5)), Is.LessThan(0));

        // CF: MatchmakingUserComparer.Compare | points compare != 0 | FALSE
        // CF: MatchmakingUserComparer.Compare | round placement compare != 0 | TRUE
        // CF: MatchmakingUserComparer.compareRoundPlacements | for rounds | loop n
        // CF: MatchmakingUserComparer.compareRoundPlacements | compare != 0 | TRUE
        [Test]
        public void TestBetterEarlierRoundPlacementSortsFirst()
        {
            var comparer = new MatchmakingUserComparer(2);
            var a = user(1, 5, null, (1, 2), (2, 1));
            var b = user(2, 5, null, (1, 1), (2, 3));

            Assert.That(comparer.Compare(a, b), Is.GreaterThan(0));
        }

        // CF: MatchmakingUserComparer.compareRoundPlacements | compare != 0 | FALSE
        // CF: MatchmakingUserComparer.compareRoundPlacements | xRound?.Placement ?? int.MaxValue | null
        [Test]
        public void TestMissingRoundSortsAfterPlayedRound()
        {
            var comparer = new MatchmakingUserComparer(2);
            var played = user(1, 5, null, (1, 1), (2, 4));
            var missing = user(2, 5, null, (1, 1));

            Assert.That(comparer.Compare(played, missing), Is.LessThan(0));
        }

        // CF: MatchmakingUserComparer.compareRoundPlacements | for rounds | loop 0
        // CF: MatchmakingUserComparer.Compare | round placement compare != 0 | FALSE
        [Test]
        public void TestTiesBrokenByUserId()
        {
            var comparer = new MatchmakingUserComparer(0);

            Assert.That(comparer.Compare(user(1, 5), user(2, 5)), Is.LessThan(0));
            Assert.That(comparer.Compare(user(2, 5), user(1, 5)), Is.GreaterThan(0));
        }

        // CF: MatchmakingRoundList.GetOrAdd | RoundsDictionary.TryGetValue | FALSE
        // CF: MatchmakingRoundList.GetOrAdd | RoundsDictionary.TryGetValue | TRUE
        [Test]
        public void TestRoundListGetOrAdd()
        {
            var list = new MatchmakingRoundList();

            var first = list.GetOrAdd(2);
            var second = list.GetOrAdd(2);

            Assert.That(second, Is.SameAs(first));
            Assert.That(first.Round, Is.EqualTo(2));
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list.Single(), Is.SameAs(first));
        }

        // CF: MatchmakingUserList.GetOrAdd | UserDictionary.TryGetValue | FALSE
        // CF: MatchmakingUserList.GetOrAdd | UserDictionary.TryGetValue | TRUE
        [Test]
        public void TestUserListGetOrAdd()
        {
            var list = new MatchmakingUserList();

            var first = list.GetOrAdd(9);

            Assert.That(list.GetOrAdd(9), Is.SameAs(first));
            Assert.That(first.UserId, Is.EqualTo(9));
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list.Single(), Is.SameAs(first));
        }

        // CF: MatchmakingRoomState.AdvanceRound | increments round | no throw
        [Test]
        public void TestAdvanceRound()
        {
            var state = new MatchmakingRoomState();
            state.AdvanceRound();
            state.AdvanceRound();

            Assert.That(state.CurrentRound, Is.EqualTo(2));
        }

        // CF: MatchmakingRoomState.RecordScores | placementPoints.Length < scores.Length | TRUE
        // CF: MatchmakingRoomState.RecordScores | too few placement points | throws ArgumentException
        [Test]
        public void TestRecordScoresWithTooFewPointsThrows()
            => Assert.Throws<ArgumentException>(() => new MatchmakingRoomState().RecordScores(new[] { new SoloScoreInfo(), new SoloScoreInfo() }, new[] { 1 }));

        // CF: MatchmakingRoomState.RecordScores | placementPoints.Length < scores.Length | FALSE
        // CF: MatchmakingRoomState.RecordScores | foreach scoreGroup | loop n
        // CF: MatchmakingRoomState.RecordScores | foreach score | loop n
        // CF: MatchmakingRoomState.RecordScores | foreach user | loop n
        [Test]
        public void TestRecordScoresAssignsPlacementsPointsAndTies()
        {
            var state = new MatchmakingRoomState { CurrentRound = 1 };
            var stats = new Dictionary<HitResult, int> { [HitResult.Great] = 1 };

            state.RecordScores(new[]
            {
                new SoloScoreInfo { UserID = 3, TotalScore = 500, Accuracy = 0.3, MaxCombo = 3 },
                new SoloScoreInfo { UserID = 1, TotalScore = 1000, Accuracy = 0.9, MaxCombo = 10, Statistics = stats },
                new SoloScoreInfo { UserID = 2, TotalScore = 500, Accuracy = 0.2, MaxCombo = 2 },
            }, new[] { 5, 3, 2, 1 });

            // tied scores share the lowest placement of their group (3) and its points.
            Assert.That(state.Users.GetOrAdd(1).Points, Is.EqualTo(5));
            Assert.That(state.Users.GetOrAdd(2).Points, Is.EqualTo(2));
            Assert.That(state.Users.GetOrAdd(3).Points, Is.EqualTo(2));

            var round1 = state.Users.GetOrAdd(1).Rounds.GetOrAdd(1);
            Assert.That(round1.Placement, Is.EqualTo(1));
            Assert.That(round1.TotalScore, Is.EqualTo(1000));
            Assert.That(round1.Accuracy, Is.EqualTo(0.9));
            Assert.That(round1.MaxCombo, Is.EqualTo(10));
            Assert.That(round1.Statistics, Is.SameAs(stats));
            Assert.That(state.Users.GetOrAdd(2).Rounds.GetOrAdd(1).Placement, Is.EqualTo(3));

            Assert.That(state.Users.GetOrAdd(1).Placement, Is.EqualTo(1));
            Assert.That(state.Users.GetOrAdd(2).Placement, Is.EqualTo(2));
            Assert.That(state.Users.GetOrAdd(3).Placement, Is.EqualTo(3));
        }

        // CF: MatchmakingRoomState.RecordScores | foreach scoreGroup | loop 0
        // CF: MatchmakingRoomState.RecordScores | foreach user | loop 0
        [Test]
        public void TestRecordNoScores()
        {
            var state = new MatchmakingRoomState();

            state.RecordScores(Array.Empty<SoloScoreInfo>(), Array.Empty<int>());

            Assert.That(state.Users.Count, Is.EqualTo(0));
        }
    }
}
