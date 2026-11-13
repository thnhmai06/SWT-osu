// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;

namespace osu.Game.Tests.Online.Rooms
{
    [TestFixture]
    public class RoomTest
    {
        private static PlaylistItem item(long id, int beatmapId = 1) => new PlaylistItem(new APIBeatmap { OnlineID = beatmapId }) { ID = id };

        // CF: Room.SetField | EqualityComparer.Equals(field, value) | FALSE
        [Test]
        public void TestSetterRaisesPropertyChanged()
        {
            var changed = new List<string?>();
            var room = new Room();
            room.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            room.Name = "x";

            Assert.That(changed, Is.EqualTo(new[] { nameof(Room.Name) }));
        }

        // CF: Room.SetField | EqualityComparer.Equals(field, value) | TRUE
        [Test]
        public void TestSetterWithSameValueDoesNotRaise()
        {
            var changed = 0;
            var room = new Room { Name = "x" };
            room.PropertyChanged += (_, _) => changed++;

            room.Name = "x";

            Assert.That(changed, Is.EqualTo(0));
        }

        // CF: Room.SetList | list.SequenceEqual(value) | FALSE
        [Test]
        public void TestListChangeRaises()
        {
            var changed = new List<string?>();
            var room = new Room();
            room.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            room.Playlist = new[] { item(1) };

            Assert.That(changed, Is.EqualTo(new[] { nameof(Room.Playlist) }));
        }

        // CF: Room.SetList | list.SequenceEqual(value) | TRUE
        [Test]
        public void TestListWithSameContentDoesNotRaise()
        {
            var changed = 0;
            var room = new Room { Playlist = new[] { item(1) } };
            room.PropertyChanged += (_, _) => changed++;

            room.Playlist = new[] { item(1) };

            Assert.That(changed, Is.EqualTo(0));
        }

        // CF: Room.Password | !string.IsNullOrEmpty(value) | TRUE
        [Test]
        public void TestPasswordSetsHasPassword()
        {
            var room = new Room { Password = "secret" };

            Assert.That(room.HasPassword, Is.True);
        }

        // CF: Room.Password | !string.IsNullOrEmpty(value) | FALSE
        [Test]
        public void TestEmptyPasswordClearsHasPassword()
        {
            var room = new Room { Password = "secret" };
            room.Password = string.Empty;

            Assert.That(room.HasPassword, Is.False);
        }

        // CF: Room.Duration | duration == null | TRUE
        // CF: Room.Duration | value == null | TRUE
        [Test]
        public void TestDurationNull()
        {
            var room = new Room { Duration = null };

            Assert.That(room.Duration, Is.Null);
        }

        // CF: Room.Duration | duration == null | FALSE
        // CF: Room.Duration | value == null | FALSE
        [Test]
        public void TestDurationStoredInWholeMinutes()
        {
            var room = new Room { Duration = TimeSpan.FromSeconds(150) };

            Assert.That(room.Duration, Is.EqualTo(TimeSpan.FromMinutes(2)));
        }

        // CF: Room.AutoStartDuration | stored as whole seconds | no throw
        [Test]
        public void TestAutoStartDuration()
        {
            var room = new Room { AutoStartDuration = TimeSpan.FromSeconds(30.9) };

            Assert.That(room.AutoStartDuration, Is.EqualTo(TimeSpan.FromSeconds(30)));
        }

        // CF: Room.HasEnded | DateTimeOffset.Now >= EndDate | TRUE
        [Test]
        public void TestHasEndedInPast() => Assert.That(new Room { EndDate = DateTimeOffset.Now.AddDays(-1) }.HasEnded, Is.True);

        // CF: Room.HasEnded | DateTimeOffset.Now >= EndDate | FALSE
        [Test]
        public void TestHasEndedInFutureOrUnset()
        {
            Assert.That(new Room { EndDate = DateTimeOffset.Now.AddDays(1) }.HasEnded, Is.False);
            Assert.That(new Room().HasEnded, Is.False);
        }

        // CF: Room.ctor(MultiplayerRoom) | room.Host != null | TRUE
        [Test]
        public void TestConstructFromMultiplayerRoomWithHost()
        {
            var mpRoom = new MultiplayerRoom(5)
            {
                ChannelID = 9,
                Host = new MultiplayerRoomUser(77),
                Settings = { Name = "n", Password = "p", MatchType = MatchType.TeamVersus, QueueMode = QueueMode.AllPlayers, AutoStartDuration = TimeSpan.FromSeconds(10), AutoSkip = true, MaxParticipants = 4 },
                Playlist = new[] { new MultiplayerPlaylistItem { ID = 3, BeatmapID = 8 } },
            };

            var room = new Room(mpRoom);

            Assert.That(room.RoomID, Is.EqualTo(5));
            Assert.That(room.ChannelId, Is.EqualTo(9));
            Assert.That(room.Name, Is.EqualTo("n"));
            Assert.That(room.Password, Is.EqualTo("p"));
            Assert.That(room.Type, Is.EqualTo(MatchType.TeamVersus));
            Assert.That(room.QueueMode, Is.EqualTo(QueueMode.AllPlayers));
            Assert.That(room.AutoStartDuration, Is.EqualTo(TimeSpan.FromSeconds(10)));
            Assert.That(room.AutoSkip, Is.True);
            Assert.That(room.MaxParticipants, Is.EqualTo((byte)4));
            Assert.That(room.Host!.Id, Is.EqualTo(77));
            Assert.That(room.Playlist.Single().ID, Is.EqualTo(3));
            Assert.That(room.Playlist.Single().Beatmap.OnlineID, Is.EqualTo(8));
        }

        // CF: Room.ctor(MultiplayerRoom) | room.Host != null | FALSE
        [Test]
        public void TestConstructFromMultiplayerRoomWithoutHost() => Assert.That(new Room(new MultiplayerRoom(1)).Host, Is.Null);

        // CF: Room.CopyFrom | copies all fields | no throw
        [Test]
        public void TestCopyFrom()
        {
            var source = new Room
            {
                RoomID = 4,
                Name = "n",
                Description = "d",
                Category = RoomCategory.DailyChallenge,
                Host = new APIUser { Id = 2 },
                ChannelId = 6,
                Status = RoomStatus.Playing,
                Type = MatchType.Matchmaking,
                MaxParticipants = 3,
                ParticipantCount = 2,
                QueueMode = QueueMode.AllPlayers,
                AutoSkip = true,
                AutoStartDuration = TimeSpan.FromSeconds(5),
                Playlist = new[] { item(1) },
                Password = "pw",
            };

            var target = new Room();
            target.CopyFrom(source);

            Assert.That(target.RoomID, Is.EqualTo(4));
            Assert.That(target.Name, Is.EqualTo("n"));
            Assert.That(target.Description, Is.EqualTo("d"));
            Assert.That(target.Category, Is.EqualTo(RoomCategory.DailyChallenge));
            Assert.That(target.Host!.Id, Is.EqualTo(2));
            Assert.That(target.ChannelId, Is.EqualTo(6));
            Assert.That(target.Status, Is.EqualTo(source.Status));
            Assert.That(target.Type, Is.EqualTo(MatchType.Matchmaking));
            Assert.That(target.MaxParticipants, Is.EqualTo((byte)3));
            Assert.That(target.ParticipantCount, Is.EqualTo(2));
            Assert.That(target.QueueMode, Is.EqualTo(QueueMode.AllPlayers));
            Assert.That(target.AutoSkip, Is.True);
            Assert.That(target.AutoStartDuration, Is.EqualTo(TimeSpan.FromSeconds(5)));
            Assert.That(target.Playlist, Has.Count.EqualTo(1));
            Assert.That(target.HasPassword, Is.True);
        }
    }
}
