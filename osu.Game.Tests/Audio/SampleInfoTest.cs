// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Localisation;
using osu.Game.Audio;
using osu.Game.Collections;
using osu.Game.Online.API;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;
using System.Net;

namespace osu.Game.Tests.Audio
{
    [TestFixture]
    public class SampleInfoTest
    {
        // CF: SampleInfo.ctor | Array.Sort(sampleNames) | no throw
        [Test]
        public void TestNamesAreSorted() => Assert.That(new SampleInfo("b", "a", "c").LookupNames.ToArray(), Is.EqualTo(new[] { "a", "b", "c" }));

        // CF: SampleInfo.Volume | default 100 | no throw
        [Test]
        public void TestDefaultVolume() => Assert.That(new SampleInfo("a").Volume, Is.EqualTo(100));

        // CF: SampleInfo.Equals | other != null | FALSE
        [Test]
        public void TestNotEqualNull() => Assert.That(new SampleInfo("a").Equals((SampleInfo?)null), Is.False);

        // CF: SampleInfo.Equals | other != null | TRUE
        // CF: SampleInfo.Equals | sampleNames.SequenceEqual(other.sampleNames) | TRUE
        // CF: SampleInfo.Equals(object) | obj is SampleInfo | TRUE
        [Test]
        public void TestEqualIgnoringInputOrder()
        {
            Assert.That(new SampleInfo("a", "b").Equals(new SampleInfo("b", "a")), Is.True);
            Assert.That(EqualityComparer<object>.Default.Equals(new SampleInfo("a", "b"), new SampleInfo("b", "a")), Is.True);
            Assert.That(new SampleInfo("a", "b").GetHashCode(), Is.EqualTo(new SampleInfo("b", "a").GetHashCode()));
        }

        // CF: SampleInfo.Equals | sampleNames.SequenceEqual(other.sampleNames) | FALSE
        // CF: SampleInfo.Equals(object) | obj is SampleInfo | FALSE
        [Test]
        public void TestNotEqualDifferentNames()
        {
            Assert.That(new SampleInfo("a").Equals(new SampleInfo("b")), Is.False);
            Assert.That(EqualityComparer<object>.Default.Equals(new SampleInfo("a"), "a"), Is.False);
        }
    }
}

namespace osu.Game.Tests.Collections
{
    [TestFixture]
    public class CollectionFilterMenuItemTest
    {
        // CF: AllBeatmapsCollectionFilterMenuItem.Equals | other is AllBeatmapsCollectionFilterMenuItem | TRUE
        // CF: AllBeatmapsCollectionFilterMenuItem.GetHashCode | constant | no throw
        [Test]
        public void TestAllBeatmapsEquality()
        {
            Assert.That(new AllBeatmapsCollectionFilterMenuItem().Equals(new AllBeatmapsCollectionFilterMenuItem()), Is.True);
            Assert.That(new AllBeatmapsCollectionFilterMenuItem().GetHashCode(), Is.EqualTo(1));
        }

        // CF: AllBeatmapsCollectionFilterMenuItem.Equals | other is AllBeatmapsCollectionFilterMenuItem | FALSE
        [Test]
        public void TestAllBeatmapsNotEqualToManage() => Assert.That(new AllBeatmapsCollectionFilterMenuItem().Equals(new ManageCollectionsFilterMenuItem()), Is.False);

        // CF: ManageCollectionsFilterMenuItem.Equals | other is ManageCollectionsFilterMenuItem | TRUE
        // CF: ManageCollectionsFilterMenuItem.GetHashCode | constant | no throw
        [Test]
        public void TestManageEquality()
        {
            Assert.That(new ManageCollectionsFilterMenuItem().Equals(new ManageCollectionsFilterMenuItem()), Is.True);
            Assert.That(new ManageCollectionsFilterMenuItem().GetHashCode(), Is.EqualTo(2));
        }

        // CF: ManageCollectionsFilterMenuItem.Equals | other is ManageCollectionsFilterMenuItem | FALSE
        [Test]
        public void TestManageNotEqualToAll() => Assert.That(new ManageCollectionsFilterMenuItem().Equals(new AllBeatmapsCollectionFilterMenuItem()), Is.False);

        // CF: CollectionFilterMenuItem.ctor(name) | CollectionName assigned | no throw
        [Test]
        public void TestNames()
        {
            Assert.That(new AllBeatmapsCollectionFilterMenuItem().CollectionName, Is.Not.EqualTo(default(LocalisableString)));
            Assert.That(new AllBeatmapsCollectionFilterMenuItem().Collection, Is.Null);
        }
    }
}

namespace osu.Game.Tests.Online
{
    [TestFixture]
    public class MiscOnlineTest
    {
        // CF: StandardMatchRoomState.Create | maxParticipants == null | TRUE
        [Test]
        public void TestCreateWithoutLimit() => Assert.That(StandardMatchRoomState.Create().Slots, Is.Null);

        // CF: StandardMatchRoomState.Create | maxParticipants == null | FALSE
        [Test]
        public void TestCreateWithLimitAllocatesSlots()
        {
            var state = StandardMatchRoomState.Create(4);

            Assert.That(state.Slots, Has.Length.EqualTo(4));
            Assert.That(state.Slots, Is.All.Null);
            Assert.That(state.Locked, Is.False);
        }

        // CF: APIException.ctor | statusCode default null | no throw
        [Test]
        public void TestApiExceptionWithoutStatus()
        {
            var inner = new Exception("inner");
            var e = new APIException("msg", inner);

            Assert.That(e.Message, Is.EqualTo("msg"));
            Assert.That(e.InnerException, Is.SameAs(inner));
            Assert.That(e.StatusCode, Is.Null);
        }

        // CF: APIException.ctor | statusCode provided | no throw
        [Test]
        public void TestApiExceptionWithStatus() => Assert.That(new APIException("m", null, HttpStatusCode.Forbidden).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        // CF: RoomExtensions.GetOnlineURL | !room.RoomID.HasValue | TRUE
        [Test]
        public void TestRoomUrlWithoutId()
            => Assert.That(new Room().GetOnlineURL(new Moq.Mock<IAPIProvider>().Object), Is.Null);

        // CF: RoomExtensions.GetOnlineURL | !room.RoomID.HasValue | FALSE
        [Test]
        public void TestRoomUrlWithId()
        {
            var api = new Moq.Mock<IAPIProvider>();
            api.SetupGet(a => a.Endpoints).Returns(new osu.Game.Online.EndpointConfiguration { WebsiteUrl = "https://web.test" });

            Assert.That(new Room { RoomID = 12 }.GetOnlineURL(api.Object), Is.EqualTo("https://web.test/multiplayer/rooms/12"));
        }

        // CF: TrustedDomainOnlineStore.GetLookupUrl | Uri.TryCreate(url) | FALSE
        // CF: TrustedDomainOnlineStore.GetLookupUrl | uri.Host.EndsWith(".ppy.sh") | FALSE
        [Test]
        public void TestUntrustedUrlsAreBlocked()
        {
            Assert.That(lookup("not a url"), Is.EqualTo(string.Empty));
            Assert.That(lookup("https://example.com/a.png"), Is.EqualTo(string.Empty));
            Assert.That(lookup("https://ppy.sh.evil.com/a.png"), Is.EqualTo(string.Empty));
        }

        // CF: TrustedDomainOnlineStore.GetLookupUrl | uri.Host.EndsWith(".ppy.sh") | TRUE
        [Test]
        public void TestTrustedUrlsPass()
        {
            Assert.That(lookup("https://a.ppy.sh/x.png"), Is.EqualTo("https://a.ppy.sh/x.png"));
            Assert.That(lookup("https://A.PPY.SH/x.png"), Is.EqualTo("https://A.PPY.SH/x.png"));
        }

        private static string lookup(string url)
        {
            var store = new osu.Game.Online.TrustedDomainOnlineStore();
            return (string)typeof(osu.Game.Online.TrustedDomainOnlineStore).GetMethod("GetLookupUrl", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(store, new object[] { url })!;
        }
    }
}
