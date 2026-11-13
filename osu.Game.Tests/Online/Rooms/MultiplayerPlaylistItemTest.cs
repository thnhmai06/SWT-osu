// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Rooms;

namespace osu.Game.Tests.Online.Rooms
{
    [TestFixture]
    public class MultiplayerPlaylistItemTest
    {
        private static MultiplayerPlaylistItem create() => new MultiplayerPlaylistItem
        {
            ID = 1, OwnerID = 2, BeatmapID = 3, BeatmapChecksum = "c", RulesetID = 4, Expired = false, PlaylistOrder = 5, StarRating = 1.5, Freestyle = false,
            RequiredMods = new[] { new APIMod { Acronym = "HD" } },
        };

        // CF: MultiplayerPlaylistItem.ctor(PlaylistItem) | item.PlaylistOrder ?? 0 | null
        [Test]
        public void TestFromPlaylistItemWithoutOrder()
        {
            var item = new MultiplayerPlaylistItem(new PlaylistItem(new APIBeatmap { OnlineID = 3, Checksum = "c", StarRating = 2 }) { ID = 1, OwnerID = 9, RulesetID = 1, Freestyle = true });

            Assert.That(item.PlaylistOrder, Is.EqualTo(0));
            Assert.That(item.BeatmapID, Is.EqualTo(3));
            Assert.That(item.BeatmapChecksum, Is.EqualTo("c"));
            Assert.That(item.StarRating, Is.EqualTo(2));
            Assert.That(item.OwnerID, Is.EqualTo(9));
            Assert.That(item.Freestyle, Is.True);
        }

        // CF: MultiplayerPlaylistItem.ctor(PlaylistItem) | item.PlaylistOrder ?? 0 | non-null
        [Test]
        public void TestFromPlaylistItemWithOrder()
            => Assert.That(new MultiplayerPlaylistItem(new PlaylistItem(new APIBeatmap()) { PlaylistOrder = 6 }).PlaylistOrder, Is.EqualTo(6));

        // CF: MultiplayerPlaylistItem.Clone | mod collections copied | no throw
        [Test]
        public void TestCloneCopiesModArrays()
        {
            var original = create();
            var clone = original.Clone();

            Assert.That(clone, Is.Not.SameAs(original));
            Assert.That(clone.Equals(original), Is.True);
            Assert.That(clone.RequiredMods, Is.Not.SameAs(original.RequiredMods));
        }

        // CF: MultiplayerPlaylistItem.Equals | other != null | FALSE
        [Test]
        public void TestEqualsNull() => Assert.That(create().Equals((MultiplayerPlaylistItem?)null), Is.False);

        // CF: MultiplayerPlaylistItem.Equals | other != null | TRUE
        // CF: MultiplayerPlaylistItem.Equals | ID == other.ID | TRUE
        // CF: MultiplayerPlaylistItem.Equals | OwnerID == other.OwnerID | TRUE
        // CF: MultiplayerPlaylistItem.Equals | BeatmapID == other.BeatmapID | TRUE
        // CF: MultiplayerPlaylistItem.Equals | BeatmapChecksum == other.BeatmapChecksum | TRUE
        // CF: MultiplayerPlaylistItem.Equals | RulesetID == other.RulesetID | TRUE
        // CF: MultiplayerPlaylistItem.Equals | RequiredMods.SequenceEqual | TRUE
        // CF: MultiplayerPlaylistItem.Equals | AllowedMods.SequenceEqual | TRUE
        // CF: MultiplayerPlaylistItem.Equals | Expired == other.Expired | TRUE
        // CF: MultiplayerPlaylistItem.Equals | PlaylistOrder == other.PlaylistOrder | TRUE
        // CF: MultiplayerPlaylistItem.Equals | PlayedAt == other.PlayedAt | TRUE
        // CF: MultiplayerPlaylistItem.Equals | StarRating == other.StarRating | TRUE
        // CF: MultiplayerPlaylistItem.Equals | Freestyle == other.Freestyle | TRUE
        // CF: MultiplayerPlaylistItem.Equals(object) | obj is MultiplayerPlaylistItem | TRUE
        [Test]
        public void TestEqualsSameValues()
        {
            Assert.That(create().Equals(create()), Is.True);
            Assert.That(System.Collections.Generic.EqualityComparer<object>.Default.Equals(create(), create()), Is.True);
        }

        // CF: MultiplayerPlaylistItem.Equals(object) | obj is MultiplayerPlaylistItem | FALSE
        [Test]
        public void TestEqualsOtherType() => Assert.That(System.Collections.Generic.EqualityComparer<object>.Default.Equals(create(), "x"), Is.False);

        private static void assertDiffers(Action<MultiplayerPlaylistItem> mutate)
        {
            var other = create();
            mutate(other);
            Assert.That(create().Equals(other), Is.False);
        }

        // CF: MultiplayerPlaylistItem.Equals | ID == other.ID | FALSE
        [Test]
        public void TestDiffersId() => assertDiffers(i => i.ID = 99);

        // CF: MultiplayerPlaylistItem.Equals | OwnerID == other.OwnerID | FALSE
        [Test]
        public void TestDiffersOwner() => assertDiffers(i => i.OwnerID = 99);

        // CF: MultiplayerPlaylistItem.Equals | BeatmapID == other.BeatmapID | FALSE
        [Test]
        public void TestDiffersBeatmap() => assertDiffers(i => i.BeatmapID = 99);

        // CF: MultiplayerPlaylistItem.Equals | BeatmapChecksum == other.BeatmapChecksum | FALSE
        [Test]
        public void TestDiffersChecksum() => assertDiffers(i => i.BeatmapChecksum = "z");

        // CF: MultiplayerPlaylistItem.Equals | RulesetID == other.RulesetID | FALSE
        [Test]
        public void TestDiffersRuleset() => assertDiffers(i => i.RulesetID = 99);

        // CF: MultiplayerPlaylistItem.Equals | RequiredMods.SequenceEqual | FALSE
        [Test]
        public void TestDiffersRequiredMods() => assertDiffers(i => i.RequiredMods = Enumerable.Empty<APIMod>());

        // CF: MultiplayerPlaylistItem.Equals | AllowedMods.SequenceEqual | FALSE
        [Test]
        public void TestDiffersAllowedMods() => assertDiffers(i => i.AllowedMods = new[] { new APIMod { Acronym = "DT" } });

        // CF: MultiplayerPlaylistItem.Equals | Expired == other.Expired | FALSE
        [Test]
        public void TestDiffersExpired() => assertDiffers(i => i.Expired = true);

        // CF: MultiplayerPlaylistItem.Equals | PlaylistOrder == other.PlaylistOrder | FALSE
        [Test]
        public void TestDiffersOrder() => assertDiffers(i => i.PlaylistOrder = 99);

        // CF: MultiplayerPlaylistItem.Equals | PlayedAt == other.PlayedAt | FALSE
        [Test]
        public void TestDiffersPlayedAt() => assertDiffers(i => i.PlayedAt = DateTimeOffset.UnixEpoch);

        // CF: MultiplayerPlaylistItem.Equals | StarRating == other.StarRating | FALSE
        [Test]
        public void TestDiffersStarRating() => assertDiffers(i => i.StarRating = 9);

        // CF: MultiplayerPlaylistItem.Equals | Freestyle == other.Freestyle | FALSE
        [Test]
        public void TestDiffersFreestyle() => assertDiffers(i => i.Freestyle = true);

        // CF: MultiplayerPlaylistItem.GetHashCode | combines fields | no throw
        [Test]
        public void TestHashCodeStableForSameInstance()
        {
            var item = create();
            Assert.That(item.GetHashCode(), Is.EqualTo(item.GetHashCode()));
        }
    }
}
