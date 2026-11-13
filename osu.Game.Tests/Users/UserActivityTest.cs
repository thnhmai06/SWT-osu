// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;
using osu.Game.Rulesets.Osu;
using osu.Game.Scoring;
using osu.Game.Users;

namespace osu.Game.Tests.Users
{
    [TestFixture]
    public class UserActivityTest
    {
        private static readonly BeatmapInfo beatmap = new BeatmapInfo(metadata: new BeatmapMetadata { Artist = "A", Title = "T" }, difficulty: new BeatmapDifficulty()) { OnlineID = 12, DifficultyName = "Hard" };
        private static readonly OsuRuleset ruleset = new OsuRuleset();

        // CF: UserActivity.GetDetails | base virtual | null
        // CF: UserActivity.GetBeatmapID | base virtual | null
        [Test]
        public void TestBaseDefaults()
        {
            var activity = new UserActivity.ChoosingBeatmap();

            Assert.That(activity.GetStatus(), Is.EqualTo("Choosing a beatmap"));
            Assert.That(activity.GetDetails(), Is.Null);
            Assert.That(activity.GetBeatmapID(), Is.Null);
        }

        // CF: UserActivity.InGame.GetStatus | returns RulesetPlayingVerb | non-null
        [Test]
        public void TestInSoloGame()
        {
            var activity = new UserActivity.InSoloGame(beatmap, ruleset.RulesetInfo);

            Assert.That(activity.GetStatus(), Is.EqualTo(ruleset.PlayingVerb));
            Assert.That(activity.GetDetails(), Is.EqualTo(activity.BeatmapDisplayTitle));
            Assert.That(activity.GetBeatmapID(), Is.EqualTo(12));
            Assert.That(activity.RulesetID, Is.EqualTo(0));
        }

        // CF: UserActivity.InMultiplayerGame.GetStatus | appends with others | non-null
        [Test]
        public void TestInMultiplayerGame() => Assert.That(new UserActivity.InMultiplayerGame(beatmap, ruleset.RulesetInfo).GetStatus(), Is.EqualTo($"{ruleset.PlayingVerb} with others"));

        // CF: UserActivity.PlayingDailyChallenge.GetStatus | appends in daily challenge | non-null
        [Test]
        public void TestPlayingDailyChallenge() => Assert.That(new UserActivity.PlayingDailyChallenge(beatmap, ruleset.RulesetInfo).GetStatus(), Is.EqualTo($"{ruleset.PlayingVerb} in daily challenge"));

        // CF: UserActivity.SpectatingMultiplayerGame.GetStatus | constant | non-null
        [Test]
        public void TestSpectatingMultiplayerGame() => Assert.That(new UserActivity.SpectatingMultiplayerGame(beatmap, ruleset.RulesetInfo).GetStatus(), Is.EqualTo("Spectating a multiplayer game"));

        // CF: UserActivity.EditingBeatmap.GetDetails | hideIdentifiableInformation | FALSE
        // CF: UserActivity.EditingBeatmap.GetBeatmapID | hideIdentifiableInformation | FALSE
        [Test]
        public void TestEditingVisible()
        {
            var activity = new UserActivity.EditingBeatmap(beatmap);

            Assert.That(activity.GetStatus(), Is.EqualTo("Editing a beatmap"));
            Assert.That(activity.GetDetails(), Is.EqualTo(activity.BeatmapDisplayTitle));
            Assert.That(activity.GetBeatmapID(), Is.EqualTo(12));
        }

        // CF: UserActivity.EditingBeatmap.GetDetails | hideIdentifiableInformation | TRUE
        // CF: UserActivity.EditingBeatmap.GetBeatmapID | hideIdentifiableInformation | TRUE
        [Test]
        public void TestEditingHidden()
        {
            var activity = new UserActivity.EditingBeatmap(beatmap);

            Assert.That(activity.GetDetails(true), Is.EqualTo(string.Empty));
            Assert.That(activity.GetBeatmapID(true), Is.Null);
        }

        // CF: UserActivity.ModdingBeatmap.GetStatus | constant | non-null
        // CF: UserActivity.TestingBeatmap.GetStatus | constant | non-null
        [Test]
        public void TestModdingAndTestingStatus()
        {
            Assert.That(new UserActivity.ModdingBeatmap(beatmap).GetStatus(), Is.EqualTo("Modding a beatmap"));
            Assert.That(new UserActivity.TestingBeatmap(beatmap).GetStatus(), Is.EqualTo("Testing a beatmap"));
        }

        private static ScoreInfo score() => new ScoreInfo { User = new APIUser { Username = "bob" }, BeatmapInfo = beatmap };

        // CF: UserActivity.WatchingReplay.ctor | score.BeatmapInfo?.OnlineID ?? -1 | non-null
        // CF: UserActivity.WatchingReplay.GetStatus | hideIdentifiableInformation | FALSE
        [Test]
        public void TestWatchingReplayVisible()
        {
            var activity = new UserActivity.WatchingReplay(score());

            Assert.That(activity.GetStatus(), Is.EqualTo("Watching bob's replay"));
            Assert.That(activity.BeatmapID, Is.EqualTo(12));
            Assert.That(activity.GetDetails(), Is.EqualTo(activity.BeatmapDisplayTitle));
        }

        // CF: UserActivity.WatchingReplay.GetStatus | hideIdentifiableInformation | TRUE
        [Test]
        public void TestWatchingReplayHidden() => Assert.That(new UserActivity.WatchingReplay(score()).GetStatus(true), Is.EqualTo("Watching a replay"));

        // CF: UserActivity.SpectatingUser.GetStatus | hideIdentifiableInformation | FALSE
        [Test]
        public void TestSpectatingUserVisible() => Assert.That(new UserActivity.SpectatingUser(score()).GetStatus(), Is.EqualTo("Spectating bob"));

        // CF: UserActivity.SpectatingUser.GetStatus | hideIdentifiableInformation | TRUE
        [Test]
        public void TestSpectatingUserHidden() => Assert.That(new UserActivity.SpectatingUser(score()).GetStatus(true), Is.EqualTo("Spectating a user"));

        // CF: UserActivity.InLobby.ctor(Room) | room.RoomID ?? -1 | null
        // CF: UserActivity.InLobby.GetDetails | hideIdentifiableInformation | FALSE
        [Test]
        public void TestInLobbyFromRoomWithoutId()
        {
            var activity = new UserActivity.InLobby(new Room { Name = "r" });

            Assert.That(activity.RoomID, Is.EqualTo(-1));
            Assert.That(activity.GetStatus(), Is.EqualTo("In a lobby"));
            Assert.That(activity.GetDetails(), Is.EqualTo("r"));
        }

        // CF: UserActivity.InLobby.ctor(Room) | room.RoomID ?? -1 | non-null
        // CF: UserActivity.InLobby.GetDetails | hideIdentifiableInformation | TRUE
        [Test]
        public void TestInLobbyFromRoomWithId()
        {
            var activity = new UserActivity.InLobby(new Room { RoomID = 8, Name = "r" });

            Assert.That(activity.RoomID, Is.EqualTo(8));
            Assert.That(activity.GetDetails(true), Is.Null);
        }

        // CF: UserActivity.InLobby.ctor(MultiplayerRoom) | switch MatchType | case Matchmaking
        [Test]
        public void TestInLobbyMatchmaking()
        {
            var activity = new UserActivity.InLobby(new MultiplayerRoom(5) { Settings = { MatchType = MatchType.Matchmaking } });

            Assert.That(activity.RoomID, Is.EqualTo(-1));
            Assert.That(activity.RoomName, Is.EqualTo("Quick Play"));
        }

        // CF: UserActivity.InLobby.ctor(MultiplayerRoom) | switch MatchType | case RankedPlay
        [Test]
        public void TestInLobbyRankedPlay()
        {
            var activity = new UserActivity.InLobby(new MultiplayerRoom(5) { Settings = { MatchType = MatchType.RankedPlay } });

            Assert.That(activity.RoomID, Is.EqualTo(-1));
            Assert.That(activity.RoomName, Is.EqualTo("Ranked Play"));
        }

        // CF: UserActivity.InLobby.ctor(MultiplayerRoom) | switch MatchType | default
        [Test]
        public void TestInLobbyDefaultMatchType()
        {
            var activity = new UserActivity.InLobby(new MultiplayerRoom(5) { Settings = { Name = "mp" } });

            Assert.That(activity.RoomID, Is.EqualTo(5));
            Assert.That(activity.RoomName, Is.EqualTo("mp"));
        }

        // CF: UserActivity.SearchingForLobby.GetStatus | constant | non-null
        // CF: UserActivity.InDailyChallengeLobby.GetStatus | constant | non-null
        [Test]
        public void TestConstantStatuses()
        {
            Assert.That(new UserActivity.SearchingForLobby().GetStatus(), Is.EqualTo("Looking for a lobby"));
            Assert.That(new UserActivity.InDailyChallengeLobby().GetStatus(), Is.EqualTo("In daily challenge lobby"));
        }
    }
}
