// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;

namespace osu.Game.Tests.Online.Multiplayer
{
    [TestFixture]
    public class MultiplayerRoomTest
    {
        // CF: MultiplayerRoom.ctor(Room) | room.RoomID ?? 0 | null
        // CF: MultiplayerRoom.ctor(Room) | room.Host != null | FALSE
        [Test]
        public void TestFromRoomWithoutIdOrHost()
        {
            var mp = new MultiplayerRoom(new Room());

            Assert.That(mp.RoomID, Is.EqualTo(0));
            Assert.That(mp.Host, Is.Null);
        }

        // CF: MultiplayerRoom.ctor(Room) | room.RoomID ?? 0 | non-null
        // CF: MultiplayerRoom.ctor(Room) | room.Host != null | TRUE
        [Test]
        public void TestFromRoomWithIdAndHost()
        {
            var room = new Room { RoomID = 4, ChannelId = 2, Host = new APIUser { Id = 9 }, Playlist = new[] { new PlaylistItem(new APIBeatmap { OnlineID = 1 }) { ID = 3 } } };

            var mp = new MultiplayerRoom(room);

            Assert.That(mp.RoomID, Is.EqualTo(4));
            Assert.That(mp.ChannelID, Is.EqualTo(2));
            Assert.That(mp.Host!.UserID, Is.EqualTo(9));
            Assert.That(mp.Playlist[0].ID, Is.EqualTo(3));
        }

        // CF: MultiplayerRoom.CanAddPlaylistItems | user.Equals(Host) | TRUE
        [Test]
        public void TestHostCanAddItems()
        {
            var host = new MultiplayerRoomUser(1);
            var mp = new MultiplayerRoom(1) { Host = host };

            Assert.That(mp.CanAddPlaylistItems(new MultiplayerRoomUser(1)), Is.True);
        }

        // CF: MultiplayerRoom.CanAddPlaylistItems | user.Equals(Host) | FALSE
        // CF: MultiplayerRoom.CanAddPlaylistItems | user.Role == Referee | TRUE
        [Test]
        public void TestRefereeCanAddItems()
            => Assert.That(new MultiplayerRoom(1) { Host = new MultiplayerRoomUser(1) }.CanAddPlaylistItems(new MultiplayerRoomUser(2) { Role = MultiplayerRoomUserRole.Referee }), Is.True);

        // CF: MultiplayerRoom.CanAddPlaylistItems | user.Role == Referee | FALSE
        // CF: MultiplayerRoom.CanAddPlaylistItems | Settings.QueueMode != HostOnly | FALSE
        [Test]
        public void TestPlayerCannotAddItemsInHostOnly()
            => Assert.That(new MultiplayerRoom(1) { Host = new MultiplayerRoomUser(1) }.CanAddPlaylistItems(new MultiplayerRoomUser(2)), Is.False);

        // CF: MultiplayerRoom.CanAddPlaylistItems | Settings.QueueMode != HostOnly | TRUE
        [Test]
        public void TestPlayerCanAddItemsInAllPlayers()
            => Assert.That(new MultiplayerRoom(1) { Host = new MultiplayerRoomUser(1), Settings = { QueueMode = QueueMode.AllPlayers } }.CanAddPlaylistItems(new MultiplayerRoomUser(2)), Is.True);

        // CF: MultiplayerRoom.CurrentPlaylistItem | Playlist.Single(item.ID == PlaylistItemId) | non-null
        [Test]
        public void TestCurrentPlaylistItem()
        {
            var mp = new MultiplayerRoom(1)
            {
                Settings = { PlaylistItemId = 2 },
                Playlist = new[] { new MultiplayerPlaylistItem { ID = 1 }, new MultiplayerPlaylistItem { ID = 2 } },
            };

            Assert.That(mp.CurrentPlaylistItem.ID, Is.EqualTo(2));
        }

        // CF: MultiplayerRoom.ToString | Host?.UserID | null
        [Test]
        public void TestToStringWithoutHost() => Assert.That(new MultiplayerRoom(3).ToString(), Does.StartWith("RoomID:3 Host: Users:0"));
    }

    [TestFixture]
    public class MultiplayerRoomUserTest
    {
        // CF: MultiplayerRoomUser.Equals | ReferenceEquals(this, other) | TRUE
        [Test]
        public void TestEqualsSelf()
        {
            var u = new MultiplayerRoomUser(1);
            Assert.That(u.Equals(u), Is.True);
        }

        // CF: MultiplayerRoomUser.Equals | other == null | TRUE
        [Test]
        public void TestEqualsNull() => Assert.That(new MultiplayerRoomUser(1).Equals((MultiplayerRoomUser?)null), Is.False);

        // CF: MultiplayerRoomUser.Equals | other == null | FALSE
        [Test]
        public void TestEqualsByUserId()
        {
            Assert.That(new MultiplayerRoomUser(1).Equals(new MultiplayerRoomUser(1)), Is.True);
            Assert.That(new MultiplayerRoomUser(1).Equals(new MultiplayerRoomUser(2)), Is.False);
            Assert.That(new MultiplayerRoomUser(1).GetHashCode(), Is.EqualTo(new MultiplayerRoomUser(1).GetHashCode()));
        }

        // CF: MultiplayerRoomUser.Equals(object) | ReferenceEquals(this, obj) | TRUE
        // CF: MultiplayerRoomUser.Equals(object) | obj?.GetType() != GetType() | FALSE
        [Test]
        public void TestObjectEquals()
        {
            var u = new MultiplayerRoomUser(1);

            Assert.That(System.Collections.Generic.EqualityComparer<object>.Default.Equals(u, u), Is.True);
            Assert.That(System.Collections.Generic.EqualityComparer<object>.Default.Equals(u, new MultiplayerRoomUser(1)), Is.True);
        }

        // CF: MultiplayerRoomUser.Equals(object) | obj?.GetType() != GetType() | TRUE
        [Test]
        public void TestObjectEqualsOtherType() => Assert.That(System.Collections.Generic.EqualityComparer<object>.Default.Equals(new MultiplayerRoomUser(1), 1), Is.False);

        // CF: MultiplayerRoomUser.CanStartGameplay | switch State | case Loaded
        [Test]
        public void TestCanStartWhenLoaded() => Assert.That(new MultiplayerRoomUser(1) { State = MultiplayerUserState.Loaded }.CanStartGameplay(), Is.True);

        // CF: MultiplayerRoomUser.CanStartGameplay | switch State | case ReadyForGameplay
        [Test]
        public void TestCanStartWhenReadyForGameplay() => Assert.That(new MultiplayerRoomUser(1) { State = MultiplayerUserState.ReadyForGameplay }.CanStartGameplay(), Is.True);

        // CF: MultiplayerRoomUser.CanStartGameplay | switch State | default
        [Test]
        public void TestCannotStartWhenIdle() => Assert.That(new MultiplayerRoomUser(1).CanStartGameplay(), Is.False);
    }
}
