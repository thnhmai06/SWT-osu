// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using NUnit.Framework;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Multiplayer.MatchTypes.RankedPlay;
using osu.Game.Online.Rooms;
using osu.Game.Online.Spectator;
using osu.Game.Users;

namespace osu.Game.Tests.Online
{
    [TestFixture]
    public class SmallModelsTest
    {
        private CultureInfo originalCulture = null!;

        [SetUp]
        public void SetUp()
        {
            originalCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        [TearDown]
        public void TearDown() => CultureInfo.CurrentCulture = originalCulture;

        // CF: MatchTypeExtensions.IsMatchmakingType | switch type | case Matchmaking
        // CF: MatchTypeExtensions.IsMatchmakingType | switch type | case RankedPlay
        [Test]
        public void TestMatchmakingTypes()
        {
            Assert.That(MatchType.Matchmaking.IsMatchmakingType(), Is.True);
            Assert.That(MatchType.RankedPlay.IsMatchmakingType(), Is.True);
        }

        // CF: MatchTypeExtensions.IsMatchmakingType | switch type | default
        [Test]
        public void TestOtherTypesAreNotMatchmaking()
        {
            Assert.That(MatchType.Playlists.IsMatchmakingType(), Is.False);
            Assert.That(MatchType.HeadToHead.IsMatchmakingType(), Is.False);
            Assert.That(MatchType.TeamVersus.IsMatchmakingType(), Is.False);
        }

        // CF: BeatmapAvailability.Equals | other != null | FALSE
        [Test]
        public void TestAvailabilityNotEqualNull() => Assert.That(BeatmapAvailability.Unknown().Equals(null), Is.False);

        // CF: BeatmapAvailability.Equals | other != null | TRUE
        // CF: BeatmapAvailability.Equals | State == other.State | TRUE
        // CF: BeatmapAvailability.Equals | DownloadProgress == other.DownloadProgress | TRUE
        [Test]
        public void TestAvailabilityEqual() => Assert.That(BeatmapAvailability.Downloading(0.5f).Equals(BeatmapAvailability.Downloading(0.5f)), Is.True);

        // CF: BeatmapAvailability.Equals | State == other.State | FALSE
        [Test]
        public void TestAvailabilityDifferentState() => Assert.That(BeatmapAvailability.Importing().Equals(BeatmapAvailability.LocallyAvailable()), Is.False);

        // CF: BeatmapAvailability.Equals | DownloadProgress == other.DownloadProgress | FALSE
        [Test]
        public void TestAvailabilityDifferentProgress() => Assert.That(BeatmapAvailability.Downloading(0.5f).Equals(BeatmapAvailability.Downloading(0.6f)), Is.False);

        // CF: BeatmapAvailability.factories | static factory methods set the state | no throw
        [Test]
        public void TestAvailabilityFactories()
        {
            Assert.That(BeatmapAvailability.Unknown().State, Is.EqualTo(DownloadState.Unknown));
            Assert.That(BeatmapAvailability.NotDownloaded().State, Is.EqualTo(DownloadState.NotDownloaded));
            Assert.That(BeatmapAvailability.Importing().State, Is.EqualTo(DownloadState.Importing));
            Assert.That(BeatmapAvailability.LocallyAvailable().State, Is.EqualTo(DownloadState.LocallyAvailable));
            Assert.That(BeatmapAvailability.Downloading(0.25f).DownloadProgress, Is.EqualTo(0.25f));
        }

        // CF: BeatmapAvailability.ToString | formats state and progress | no throw
        [Test]
        public void TestAvailabilityToString() => Assert.That(BeatmapAvailability.Downloading(0.5f).ToString(), Is.EqualTo("Downloading, 50.00%"));

        // CF: SpectatorState.Equals | ReferenceEquals(null, other) | TRUE
        [Test]
        public void TestSpectatorStateNotEqualNull() => Assert.That(new SpectatorState().Equals(null), Is.False);

        // CF: SpectatorState.Equals | ReferenceEquals(this, other) | TRUE
        [Test]
        public void TestSpectatorStateEqualSelf()
        {
            var s = new SpectatorState();
            Assert.That(s.Equals(s), Is.True);
        }

        // CF: SpectatorState.Equals | BeatmapID == other.BeatmapID | TRUE
        // CF: SpectatorState.Equals | Mods.SequenceEqual(other.Mods) | TRUE
        // CF: SpectatorState.Equals | RulesetID == other.RulesetID | TRUE
        // CF: SpectatorState.Equals | State == other.State | TRUE
        [Test]
        public void TestSpectatorStateEqualValues()
        {
            var a = new SpectatorState { BeatmapID = 1, RulesetID = 2, State = SpectatedUserState.Playing, Mods = new[] { new APIMod { Acronym = "HD" } } };
            var b = new SpectatorState { BeatmapID = 1, RulesetID = 2, State = SpectatedUserState.Playing, Mods = new[] { new APIMod { Acronym = "HD" } } };

            Assert.That(a.Equals(b), Is.True);
        }

        // CF: SpectatorState.Equals | BeatmapID == other.BeatmapID | FALSE
        [Test]
        public void TestSpectatorStateDifferentBeatmap() => Assert.That(new SpectatorState { BeatmapID = 1 }.Equals(new SpectatorState { BeatmapID = 2 }), Is.False);

        // CF: SpectatorState.Equals | Mods.SequenceEqual(other.Mods) | FALSE
        [Test]
        public void TestSpectatorStateDifferentMods() => Assert.That(new SpectatorState().Equals(new SpectatorState { Mods = new[] { new APIMod { Acronym = "HD" } } }), Is.False);

        // CF: SpectatorState.Equals | RulesetID == other.RulesetID | FALSE
        [Test]
        public void TestSpectatorStateDifferentRuleset() => Assert.That(new SpectatorState { RulesetID = 1 }.Equals(new SpectatorState { RulesetID = 2 }), Is.False);

        // CF: SpectatorState.Equals | State == other.State | FALSE
        [Test]
        public void TestSpectatorStateDifferentState() => Assert.That(new SpectatorState { State = SpectatedUserState.Playing }.Equals(new SpectatorState { State = SpectatedUserState.Quit }), Is.False);

        // CF: SpectatorState.ToString | joins mods | no throw
        [Test]
        public void TestSpectatorStateToString()
            => Assert.That(new SpectatorState { BeatmapID = 1, RulesetID = 2, Mods = new[] { new APIMod { Acronym = "HD" }, new APIMod { Acronym = "DT" } } }.ToString(), Is.EqualTo("Beatmap:1 Mods:HD,DT Ruleset:2 State:Idle"));

        // CF: SpectatorUser.Equals | other is null | TRUE
        [Test]
        public void TestSpectatorUserNull() => Assert.That(new SpectatorUser().Equals((SpectatorUser?)null), Is.False);

        // CF: SpectatorUser.Equals | ReferenceEquals(this, other) | TRUE
        [Test]
        public void TestSpectatorUserSelf()
        {
            var u = new SpectatorUser();
            Assert.That(u.Equals(u), Is.True);
        }

        // CF: SpectatorUser.Equals | OnlineID == other.OnlineID | TRUE
        // CF: SpectatorUser.Equals | OnlineID == other.OnlineID | FALSE
        // CF: SpectatorUser.Equals(object) | obj as SpectatorUser | non-null
        // CF: SpectatorUser.Equals(object) | obj as SpectatorUser | null
        [Test]
        public void TestSpectatorUserEqualityById()
        {
            Assert.That(new SpectatorUser { OnlineID = 1 }.Equals(new SpectatorUser { OnlineID = 1 }), Is.True);
            Assert.That(new SpectatorUser { OnlineID = 1 }.Equals(new SpectatorUser { OnlineID = 2 }), Is.False);
            Assert.That(EqualityComparer<object>.Default.Equals(new SpectatorUser { OnlineID = 1 }, new SpectatorUser { OnlineID = 1 }), Is.True);
            Assert.That(EqualityComparer<object>.Default.Equals(new SpectatorUser { OnlineID = 1 }, "x"), Is.False);
            Assert.That(new SpectatorUser { OnlineID = 7 }.GetHashCode(), Is.EqualTo(7));
        }

        // CF: SpectatorUser.constants | CountryCode and IsBot fixed | no throw
        [Test]
        public void TestSpectatorUserConstants()
        {
            var u = new SpectatorUser();

            Assert.That(u.CountryCode, Is.EqualTo(CountryCode.Unknown));
            Assert.That(u.IsBot, Is.False);
        }

        private static RankedPlayDamageInfo damage() => new RankedPlayDamageInfo { Damage = 1, RawDamage = 2, OldLife = 3, NewLife = 4, DirectDamage = 5, Multiplier = 1.5, BonusDamage = 6 };

        // CF: RankedPlayDamageInfo.Equals | other == null | TRUE
        [Test]
        public void TestDamageNotEqualNull() => Assert.That(damage().Equals(null), Is.False);

        // CF: RankedPlayDamageInfo.Equals | other == null | FALSE
        // CF: RankedPlayDamageInfo.Equals | Damage == other.Damage | TRUE
        // CF: RankedPlayDamageInfo.Equals | RawDamage == other.RawDamage | TRUE
        // CF: RankedPlayDamageInfo.Equals | OldLife == other.OldLife | TRUE
        // CF: RankedPlayDamageInfo.Equals | NewLife == other.NewLife | TRUE
        // CF: RankedPlayDamageInfo.Equals | DirectDamage == other.DirectDamage | TRUE
        // CF: RankedPlayDamageInfo.Equals | Multiplier == other.Multiplier | TRUE
        // CF: RankedPlayDamageInfo.Equals | BonusDamage == other.BonusDamage | TRUE
        [Test]
        public void TestDamageEqual() => Assert.That(damage().Equals(damage()), Is.True);

        private static void assertDamageDiffers(Action<RankedPlayDamageInfo> mutate)
        {
            var other = damage();
            mutate(other);
            Assert.That(damage().Equals(other), Is.False);
        }

        // CF: RankedPlayDamageInfo.Equals | Damage == other.Damage | FALSE
        [Test]
        public void TestDamageDiffersDamage() => assertDamageDiffers(d => d.Damage = 99);

        // CF: RankedPlayDamageInfo.Equals | RawDamage == other.RawDamage | FALSE
        [Test]
        public void TestDamageDiffersRaw() => assertDamageDiffers(d => d.RawDamage = 99);

        // CF: RankedPlayDamageInfo.Equals | OldLife == other.OldLife | FALSE
        [Test]
        public void TestDamageDiffersOldLife() => assertDamageDiffers(d => d.OldLife = 99);

        // CF: RankedPlayDamageInfo.Equals | NewLife == other.NewLife | FALSE
        [Test]
        public void TestDamageDiffersNewLife() => assertDamageDiffers(d => d.NewLife = 99);

        // CF: RankedPlayDamageInfo.Equals | DirectDamage == other.DirectDamage | FALSE
        [Test]
        public void TestDamageDiffersDirect() => assertDamageDiffers(d => d.DirectDamage = 99);

        // CF: RankedPlayDamageInfo.Equals | Multiplier == other.Multiplier | FALSE
        [Test]
        public void TestDamageDiffersMultiplier() => assertDamageDiffers(d => d.Multiplier = 99);

        // CF: RankedPlayDamageInfo.Equals | BonusDamage == other.BonusDamage | FALSE
        [Test]
        public void TestDamageDiffersBonus() => assertDamageDiffers(d => d.BonusDamage = 99);

        // CF: RankedPlayCardItem.Equals | other != null | TRUE
        // CF: RankedPlayCardItem.Equals | ID.Equals(other.ID) | TRUE
        // CF: RankedPlayCardItem.Equals(object) | obj is RankedPlayCardItem | TRUE
        [Test]
        public void TestCardEqualsById()
        {
            var id = Guid.NewGuid();
            var a = new RankedPlayCardItem { ID = id };
            var b = new RankedPlayCardItem { ID = id };

            Assert.That(a.Equals(b), Is.True);
            Assert.That(EqualityComparer<object>.Default.Equals(a, b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(id.GetHashCode()));
        }

        // CF: RankedPlayCardItem.Equals | other != null | FALSE
        // CF: RankedPlayCardItem.Equals | ID.Equals(other.ID) | FALSE
        // CF: RankedPlayCardItem.Equals(object) | obj is RankedPlayCardItem | FALSE
        [Test]
        public void TestCardNotEqual()
        {
            Assert.That(new RankedPlayCardItem().Equals(null), Is.False);
            Assert.That(new RankedPlayCardItem().Equals(new RankedPlayCardItem()), Is.False);
            Assert.That(EqualityComparer<object>.Default.Equals(new RankedPlayCardItem(), "x"), Is.False);
        }

        // CF: RankedPlayRoomState.ActiveUser | ActiveUserId == null | TRUE
        // CF: RankedPlayRoomState.WinningUser | WinningUserId == null | TRUE
        [Test]
        public void TestRankedPlayStateWithoutUsers()
        {
            var state = new RankedPlayRoomState();

            Assert.That(state.ActiveUser, Is.Null);
            Assert.That(state.WinningUser, Is.Null);
        }

        // CF: RankedPlayRoomState.ActiveUser | ActiveUserId == null | FALSE
        // CF: RankedPlayRoomState.WinningUser | WinningUserId == null | FALSE
        [Test]
        public void TestRankedPlayStateLooksUpUsers()
        {
            var active = new RankedPlayUserInfo { Rating = 0 };
            var winner = new RankedPlayUserInfo { Rating = 0 };
            var state = new RankedPlayRoomState { Users = { [1] = active, [2] = winner }, ActiveUserId = 1, WinningUserId = 2 };

            Assert.That(state.ActiveUser, Is.SameAs(active));
            Assert.That(state.WinningUser, Is.SameAs(winner));
        }

        // CF: RankedPlayRoomState.ActiveUser | Users[ActiveUserId.Value] | throws KeyNotFoundException
        [Test]
        public void TestRankedPlayStateUnknownActiveUserThrows()
            => Assert.Throws<KeyNotFoundException>(() => _ = new RankedPlayRoomState { ActiveUserId = 5 }.ActiveUser);

        private static CommentBundle bundle(params Comment[] comments) => new CommentBundle
        {
            Comments = new List<Comment>(comments),
            IncludedComments = new List<Comment>(),
            PinnedComments = new List<Comment>(),
        };

        // CF: CommentBundle.UserVotes | ForEach IsVoted | no throw
        [Test]
        public void TestUserVotesMarksComments()
        {
            var voted = new Comment { Id = 1 };
            var other = new Comment { Id = 2 };
            var b = bundle(voted, other);
            b.IncludedComments.Add(new Comment { Id = 3 });

            b.UserVotes = new List<long> { 1, 3 };

            Assert.That(voted.IsVoted, Is.True);
            Assert.That(other.IsVoted, Is.False);
            Assert.That(b.IncludedComments[0].IsVoted, Is.True);
        }

        // CF: CommentBundle.Users | comment.UserId == user.Id | TRUE
        // CF: CommentBundle.Users | comment.EditedById == user.Id | TRUE
        // CF: CommentBundle.Users | foreach user | loop n
        [Test]
        public void TestUsersAreLinkedToComments()
        {
            var comment = new Comment { Id = 1, UserId = 10, EditedById = 11 };
            var included = new Comment { Id = 2, UserId = 11 };
            var pinned = new Comment { Id = 3, UserId = 10 };
            var b = bundle(comment);
            b.IncludedComments.Add(included);
            b.PinnedComments.Add(pinned);
            var author = new APIUser { Id = 10 };
            var editor = new APIUser { Id = 11 };

            b.Users = new List<APIUser> { author, editor };

            Assert.That(comment.User, Is.SameAs(author));
            Assert.That(comment.EditedUser, Is.SameAs(editor));
            Assert.That(included.User, Is.SameAs(editor));
            Assert.That(pinned.User, Is.SameAs(author));
        }

        // CF: CommentBundle.Users | comment.UserId == user.Id | FALSE
        // CF: CommentBundle.Users | comment.EditedById == user.Id | FALSE
        // CF: CommentBundle.Users | foreach user | loop 0
        [Test]
        public void TestUnrelatedUsersAreNotLinked()
        {
            var comment = new Comment { Id = 1, UserId = 10 };
            var b = bundle(comment);

            b.Users = new List<APIUser> { new APIUser { Id = 99 } };

            Assert.That(comment.User, Is.Null);
            Assert.That(comment.EditedUser, Is.Null);

            b.Users = new List<APIUser>();
            Assert.That(comment.User, Is.Null);
        }

        // CF: CommentBundle.json | snake_case deserialisation | no throw
        [Test]
        public void TestCommentableMetaDefaults()
        {
            var meta = JsonConvert.DeserializeObject<CommentableMeta>("{}")!;

            Assert.That(meta.Title, Is.EqualTo(string.Empty));
            Assert.That(meta.OwnerTitle, Is.Null);
            Assert.That(meta.CurrentUserAttributes, Is.Null);
        }
    }
}
