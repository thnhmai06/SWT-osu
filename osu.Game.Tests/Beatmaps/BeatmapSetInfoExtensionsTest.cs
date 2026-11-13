// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Moq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Extensions;
using osu.Game.Models;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Rulesets;

namespace osu.Game.Tests.Beatmaps
{
    [TestFixture]
    public class BeatmapSetInfoExtensionsTest
    {
        private static BeatmapSetInfo setWith(params string[] filenames)
        {
            var set = new BeatmapSetInfo();
            for (int i = 0; i < filenames.Length; i++)
                set.Files.Add(new RealmNamedFileUsage(new RealmFile { Hash = $"ab{i:x4}cdef" }, filenames[i]));
            return set;
        }

        private static IAPIProvider api()
        {
            var mock = new Mock<IAPIProvider>();
            mock.SetupGet(a => a.Endpoints).Returns(new EndpointConfiguration { WebsiteUrl = "https://web.test" });
            return mock.Object;
        }

        [Test]
        public void TestGetFileIsCaseInsensitive()
        {
            var set = setWith("Audio.MP3", "bg.png");

            Assert.That(set.GetFile("audio.mp3"), Is.SameAs(set.Files[0]));
            Assert.That(set.GetFile("BG.PNG"), Is.SameAs(set.Files[1]));
        }

        [Test]
        public void TestGetMissingFile()
        {
            var set = setWith("a.png");

            Assert.That(set.GetFile("b.png"), Is.Null);
            Assert.That(set.GetPathForFile("b.png"), Is.Null);
        }

        [Test]
        public void TestGetPathForFileReturnsStoragePath()
        {
            var set = setWith("a.png");

            Assert.That(set.GetPathForFile("A.PNG"), Is.EqualTo(set.Files[0].File.GetStoragePath()));
        }

        [Test]
        public void TestDuplicateFilenamesAreRejected()
        {
            var set = setWith("a.png", "A.png");

            Assert.That(() => set.GetFile("a.png"), Throws.InvalidOperationException);
        }

        // CF: BeatmapSetInfoExtensions.GetOnlineURL | beatmapSetInfo.OnlineID <= 0 | TRUE
        [Test]
        public void TestNoUrlWithoutOnlineId()
        {
            Assert.That(new BeatmapSetInfo { OnlineID = -1 }.GetOnlineURL(api()), Is.Null);
            Assert.That(new BeatmapSetInfo { OnlineID = 0 }.GetOnlineURL(api()), Is.Null);
        }

        // CF: BeatmapSetInfoExtensions.GetOnlineURL | ruleset != null | FALSE
        [Test]
        public void TestUrlWithoutRuleset()
        {
            Assert.That(new BeatmapSetInfo { OnlineID = 5 }.GetOnlineURL(api()), Is.EqualTo("https://web.test/beatmapsets/5"));
        }

        // CF: BeatmapSetInfoExtensions.GetOnlineURL | ruleset != null | TRUE
        [Test]
        public void TestUrlWithRuleset()
        {
            Assert.That(new BeatmapSetInfo { OnlineID = 5 }.GetOnlineURL(api(), new RulesetInfo { ShortName = "taiko" }), Is.EqualTo("https://web.test/beatmapsets/5#taiko"));
        }
    }
}
