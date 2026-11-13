// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Rooms;
using osu.Game.Utils;

namespace osu.Game.Tests.Online.Rooms
{
    [TestFixture]
    public class PlaylistItemTest
    {
        private static PlaylistItem create(long id = 1, int beatmapId = 10) => new PlaylistItem(new APIBeatmap { OnlineID = beatmapId }) { ID = id, RulesetID = 2 };

        // CF: PlaylistItem.ctor(MultiplayerPlaylistItem) | maps all fields | no throw
        [Test]
        public void TestConstructFromMultiplayerItem()
        {
            var played = DateTimeOffset.UnixEpoch;
            var mp = new MultiplayerPlaylistItem
            {
                ID = 1, OwnerID = 2, BeatmapID = 3, BeatmapChecksum = "abc", RulesetID = 4, Expired = true, PlaylistOrder = 5, PlayedAt = played, StarRating = 6.5, Freestyle = true,
                RequiredMods = new[] { new APIMod { Acronym = "HD" } },
                AllowedMods = new[] { new APIMod { Acronym = "DT" }, new APIMod { Acronym = "HR" } },
            };

            var item = new PlaylistItem(mp);

            Assert.That(item.ID, Is.EqualTo(1));
            Assert.That(item.OwnerID, Is.EqualTo(2));
            Assert.That(item.Beatmap.OnlineID, Is.EqualTo(3));
            Assert.That(item.Beatmap.MD5Hash, Is.EqualTo("abc"));
            Assert.That(item.Beatmap.StarRating, Is.EqualTo(6.5));
            Assert.That(item.RulesetID, Is.EqualTo(4));
            Assert.That(item.Expired, Is.True);
            Assert.That(item.PlaylistOrder, Is.EqualTo((ushort)5));
            Assert.That(item.PlayedAt, Is.EqualTo(played));
            Assert.That(item.RequiredMods.Single().Acronym, Is.EqualTo("HD"));
            Assert.That(item.AllowedMods, Has.Length.EqualTo(2));
            Assert.That(item.Freestyle, Is.True);
        }

        // CF: PlaylistItem.MarkInvalid | valid set to false | no throw
        // CF: PlaylistItem.MarkCompleted | completed set to true | no throw
        [Test]
        public void TestMarkInvalidAndCompleted()
        {
            var item = create();
            Assert.That(item.Valid.Value, Is.True);
            Assert.That(item.Completed.Value, Is.False);

            item.MarkInvalid();
            item.MarkCompleted();

            Assert.That(item.Valid.Value, Is.False);
            Assert.That(item.Completed.Value, Is.True);
        }

        // CF: PlaylistItem.With | Optional HasValue | TRUE
        [Test]
        public void TestWithOverrides()
        {
            var item = create(1, 10);
            var other = new APIBeatmap { OnlineID = 99 };

            var copy = item.With(new Optional<long>(5), new Optional<osu.Game.Beatmaps.IBeatmapInfo>(other), new Optional<ushort?>(7), new Optional<int>(3));

            Assert.That(copy.ID, Is.EqualTo(5));
            Assert.That(copy.Beatmap.OnlineID, Is.EqualTo(99));
            Assert.That(copy.PlaylistOrder, Is.EqualTo((ushort)7));
            Assert.That(copy.RulesetID, Is.EqualTo(3));
        }

        // CF: PlaylistItem.With | Optional HasValue | FALSE
        [Test]
        public void TestWithNothingKeepsValuesAndValidity()
        {
            var item = create(1, 10);
            item.PlaylistOrder = 4;
            item.Expired = true;
            item.MarkInvalid();

            var copy = item.With();

            Assert.That(copy.ID, Is.EqualTo(1));
            Assert.That(copy.Beatmap.OnlineID, Is.EqualTo(10));
            Assert.That(copy.PlaylistOrder, Is.EqualTo((ushort)4));
            Assert.That(copy.RulesetID, Is.EqualTo(2));
            Assert.That(copy.Expired, Is.True);
            Assert.That(copy.Valid.Value, Is.False);
            Assert.That(copy, Is.Not.SameAs(item));
        }

        // CF: PlaylistItem.Equals | ID == other.ID | TRUE
        // CF: PlaylistItem.Equals | Beatmap.OnlineID == other.Beatmap.OnlineID | TRUE
        // CF: PlaylistItem.Equals | RulesetID == other.RulesetID | TRUE
        // CF: PlaylistItem.Equals | Expired == other.Expired | TRUE
        // CF: PlaylistItem.Equals | PlaylistOrder == other.PlaylistOrder | TRUE
        // CF: PlaylistItem.Equals | AllowedMods.SequenceEqual | TRUE
        // CF: PlaylistItem.Equals | RequiredMods.SequenceEqual | TRUE
        // CF: PlaylistItem.Equals | Freestyle == other.Freestyle | TRUE
        [Test]
        public void TestEqualItems() => Assert.That(create().Equals(create()), Is.True);

        // CF: PlaylistItem.Equals | ID == other.ID | FALSE
        [Test]
        public void TestNotEqualId() => Assert.That(create(1).Equals(create(2)), Is.False);

        // CF: PlaylistItem.Equals | Beatmap.OnlineID == other.Beatmap.OnlineID | FALSE
        [Test]
        public void TestNotEqualBeatmap() => Assert.That(create(1, 1).Equals(create(1, 2)), Is.False);

        // CF: PlaylistItem.Equals | RulesetID == other.RulesetID | FALSE
        [Test]
        public void TestNotEqualRuleset()
        {
            var other = create();
            other.RulesetID = 0;
            Assert.That(create().Equals(other), Is.False);
        }

        // CF: PlaylistItem.Equals | Expired == other.Expired | FALSE
        [Test]
        public void TestNotEqualExpired()
        {
            var other = create();
            other.Expired = true;
            Assert.That(create().Equals(other), Is.False);
        }

        // CF: PlaylistItem.Equals | PlaylistOrder == other.PlaylistOrder | FALSE
        [Test]
        public void TestNotEqualOrder()
        {
            var other = create();
            other.PlaylistOrder = 1;
            Assert.That(create().Equals(other), Is.False);
        }

        // CF: PlaylistItem.Equals | AllowedMods.SequenceEqual | FALSE
        [Test]
        public void TestNotEqualAllowedMods()
        {
            var other = create();
            other.AllowedMods = new[] { new APIMod { Acronym = "HD" } };
            Assert.That(create().Equals(other), Is.False);
        }

        // CF: PlaylistItem.Equals | RequiredMods.SequenceEqual | FALSE
        [Test]
        public void TestNotEqualRequiredMods()
        {
            var other = create();
            other.RequiredMods = new[] { new APIMod { Acronym = "HD" } };
            Assert.That(create().Equals(other), Is.False);
        }

        // CF: PlaylistItem.Equals | Freestyle == other.Freestyle | FALSE
        [Test]
        public void TestNotEqualFreestyle()
        {
            var other = create();
            other.Freestyle = true;
            Assert.That(create().Equals(other), Is.False);
        }

        // CF: PlaylistItem.Equals | ID == other?.ID | FALSE
        [Test]
        public void TestNotEqualNull() => Assert.That(create().Equals(null), Is.False);
    }
}
