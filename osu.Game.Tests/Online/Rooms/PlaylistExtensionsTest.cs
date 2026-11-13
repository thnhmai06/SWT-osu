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
    public class PlaylistExtensionsTest
    {
        private static readonly DateTimeOffset t0 = DateTimeOffset.UnixEpoch;

        private static PlaylistItem item(long id, bool expired, ushort? order = null, int playedMinutes = 0)
            => new PlaylistItem(new APIBeatmap { OnlineID = (int)id }) { ID = id, Expired = expired, PlaylistOrder = order, PlayedAt = expired ? t0.AddMinutes(playedMinutes) : null };

        private static MultiplayerPlaylistItem mpItem(long id, bool expired, ushort order = 0, int playedMinutes = 0)
            => new MultiplayerPlaylistItem { ID = id, Expired = expired, PlaylistOrder = order, PlayedAt = expired ? t0.AddMinutes(playedMinutes) : null };

        // CF: PlaylistExtensions.GetHistoricalItems | item.Expired | TRUE
        // CF: PlaylistExtensions.GetHistoricalItems | item.Expired | FALSE
        [Test]
        public void TestHistoricalItemsOrderedByPlayedAt()
        {
            var playlist = new[] { item(1, true, playedMinutes: 5), item(2, false, 0), item(3, true, playedMinutes: 1) };

            Assert.That(playlist.GetHistoricalItems().Select(i => i.ID), Is.EqualTo(new long[] { 3, 1 }));
        }

        // CF: PlaylistExtensions.GetUpcomingItems | !item.Expired | TRUE
        // CF: PlaylistExtensions.GetUpcomingItems | !item.Expired | FALSE
        [Test]
        public void TestUpcomingItemsOrderedByPlaylistOrder()
        {
            var playlist = new[] { item(1, false, 5), item(2, true), item(3, false, 2) };

            Assert.That(playlist.GetUpcomingItems().Select(i => i.ID), Is.EqualTo(new long[] { 3, 1 }));
        }

        // CF: PlaylistExtensions.GetCurrentItem | playlist.Count == 0 | TRUE
        [Test]
        public void TestCurrentItemOfEmptyPlaylist() => Assert.That(Array.Empty<PlaylistItem>().GetCurrentItem(), Is.Null);

        // CF: PlaylistExtensions.GetCurrentItem | playlist.Count == 0 | FALSE
        // CF: PlaylistExtensions.GetCurrentItem | playlist.All(item.Expired) | FALSE
        [Test]
        public void TestCurrentItemIsFirstUpcoming()
        {
            var playlist = new[] { item(1, true), item(2, false, 4), item(3, false, 1) };

            Assert.That(playlist.GetCurrentItem()!.ID, Is.EqualTo(3));
        }

        // CF: PlaylistExtensions.GetCurrentItem | playlist.All(item.Expired) | TRUE
        [Test]
        public void TestCurrentItemIsLastPlayedWhenAllExpired()
        {
            var playlist = new[] { item(1, true, playedMinutes: 9), item(2, true, playedMinutes: 3) };

            Assert.That(playlist.GetCurrentItem()!.ID, Is.EqualTo(1));
        }

        // CF: MultiplayerRoomExtensions.GetCurrentItem | room.Playlist.Count == 0 | TRUE
        [Test]
        public void TestRoomCurrentItemOfEmptyPlaylist() => Assert.That(new MultiplayerRoom(1).GetCurrentItem(), Is.Null);

        // CF: MultiplayerRoomExtensions.GetCurrentItem | room.Playlist.All(item.Expired) | FALSE
        // CF: MultiplayerRoomExtensions.GetUpcomingItems | !item.Expired | TRUE
        // CF: MultiplayerRoomExtensions.GetUpcomingItems | !item.Expired | FALSE
        [Test]
        public void TestRoomCurrentItemIsFirstUpcoming()
        {
            var room = new MultiplayerRoom(1) { Playlist = new[] { mpItem(1, true), mpItem(2, false, 3), mpItem(3, false, 1) } };

            Assert.That(room.GetCurrentItem()!.ID, Is.EqualTo(3));
            Assert.That(room.GetUpcomingItems().Select(i => i.ID), Is.EqualTo(new long[] { 3, 2 }));
        }

        // CF: MultiplayerRoomExtensions.GetCurrentItem | room.Playlist.All(item.Expired) | TRUE
        // CF: MultiplayerRoomExtensions.GetHistoricalItems | item.Expired | TRUE
        // CF: MultiplayerRoomExtensions.GetHistoricalItems | item.Expired | FALSE
        [Test]
        public void TestRoomCurrentItemIsLastPlayedWhenAllExpired()
        {
            var room = new MultiplayerRoom(1) { Playlist = new List<MultiplayerPlaylistItem> { mpItem(1, true, playedMinutes: 9), mpItem(2, true, playedMinutes: 3) } };

            Assert.That(room.GetCurrentItem()!.ID, Is.EqualTo(1));
            Assert.That(room.GetHistoricalItems().Select(i => i.ID), Is.EqualTo(new long[] { 2, 1 }));

            room.Playlist.Add(mpItem(3, false));
            Assert.That(room.GetHistoricalItems().Select(i => i.ID), Is.EqualTo(new long[] { 2, 1 }));
        }
    }
}
