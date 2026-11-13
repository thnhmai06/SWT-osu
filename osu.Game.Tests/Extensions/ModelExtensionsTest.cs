// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Extensions;
using osu.Game.IO;
using osu.Game.Models;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Scoring;
using osu.Game.Screens.Play.Leaderboards;

namespace osu.Game.Tests.Extensions
{
    [TestFixture]
    public class ModelExtensionsTest
    {
        private static BeatmapMetadata metadata() => new BeatmapMetadata { Artist = "Artist", Title = "Title", Author = new RealmUser { Username = "mapper" } };

        [Test]
        public void TestGetStoragePath()
        {
            Assert.That(new FakeFile("abcdef").GetStoragePath(), Is.EqualTo(Path.Combine("a", "ab", "abcdef")));
        }

        // CF: ModelExtensions.GetDisplayString | model | case IBeatmapSetInfo
        [Test]
        public void TestDisplayStringBeatmapSet()
        {
            Assert.That(new BeatmapSetInfo { Beatmaps = { new BeatmapInfo { Metadata = metadata() } } }.GetDisplayString(), Is.EqualTo("Artist - Title (mapper)"));
        }

        // CF: ModelExtensions.GetDisplayString | model | case IBeatmapInfo
        [Test]
        public void TestDisplayStringBeatmap()
        {
            Assert.That(new BeatmapInfo { Metadata = metadata(), DifficultyName = "Hard" }.GetDisplayString(), Does.StartWith("Artist - Title (mapper)").And.Contain("Hard"));
        }

        // CF: ModelExtensions.GetDisplayString | model | case IBeatmapMetadataInfo
        [Test]
        public void TestDisplayStringMetadata()
        {
            Assert.That(metadata().GetDisplayString(), Is.EqualTo("Artist - Title (mapper)"));
        }

        // CF: ModelExtensions.GetDisplayString | model | case IScoreInfo
        [Test]
        public void TestDisplayStringScore()
        {
            var score = new ScoreInfo(new BeatmapInfo { Metadata = metadata() }, new RulesetInfo(), new RealmUser { Username = "player" });
            Assert.That(score.GetDisplayString(), Does.StartWith("player playing Artist - Title"));
        }

        // CF: ModelExtensions.GetDisplayString | model | case IRulesetInfo
        [Test]
        public void TestDisplayStringRuleset()
        {
            Assert.That(new RulesetInfo { Name = "osu!" }.GetDisplayString(), Is.EqualTo("osu!"));
        }

        // CF: ModelExtensions.GetDisplayString | model | case IUser
        [Test]
        public void TestDisplayStringUser()
        {
            Assert.That(new APIUser { Username = "someone" }.GetDisplayString(), Is.EqualTo("someone"));
        }

        // CF: ModelExtensions.GetDisplayString | result ??= model?.ToString() | non-null
        [Test]
        public void TestDisplayStringFallbackToString()
        {
            Assert.That(42.GetDisplayString(), Is.EqualTo("42"));
        }

        // CF: ModelExtensions.GetDisplayString | model?.ToString() ?? "null" | null
        [Test]
        public void TestDisplayStringNull()
        {
            object? o = null;
            Assert.That(o.GetDisplayString(), Is.EqualTo("null"));
            Assert.That(new NullToString().GetDisplayString(), Is.EqualTo("null"));
        }

        // CF: ModelExtensions.IsLegacyRuleset | ruleset.OnlineID >= 0 && ruleset.OnlineID <= MAX | TRUE
        // CF: ModelExtensions.IsLegacyRuleset | ruleset.OnlineID >= 0 | FALSE
        // CF: ModelExtensions.IsLegacyRuleset | ruleset.OnlineID <= MAX | FALSE
        [TestCase(-1, false)]
        [TestCase(0, true)]
        [TestCase(3, true)]
        [TestCase(4, false)]
        public void TestIsLegacyRuleset(int id, bool expected)
        {
            Assert.That(new RulesetInfo { OnlineID = id }.IsLegacyRuleset(), Is.EqualTo(expected));
        }

        // CF: ModelExtensions.matchesOnlineID | instance == null || other == null | TRUE
        // CF: ModelExtensions.matchesOnlineID | instance == null | TRUE
        // CF: ModelExtensions.matchesOnlineID | other == null | TRUE
        // CF: ModelExtensions.matchesOnlineID | instance.OnlineID < 0 || other.OnlineID < 0 | TRUE
        // CF: ModelExtensions.matchesOnlineID | instance.OnlineID < 0 | TRUE
        // CF: ModelExtensions.matchesOnlineID | other.OnlineID < 0 | TRUE
        // CF: ModelExtensions.matchesOnlineID | instance.OnlineID.Equals(other.OnlineID) | TRUE
        // CF: ModelExtensions.matchesOnlineID | instance.OnlineID.Equals(other.OnlineID) | FALSE
        [Test]
        public void TestMatchesOnlineIDInt()
        {
            var a = new BeatmapSetInfo { OnlineID = 1 };
            Assert.That(a.MatchesOnlineID(null), Is.False);
            Assert.That(((BeatmapSetInfo?)null).MatchesOnlineID(a), Is.False);
            Assert.That(a.MatchesOnlineID(new BeatmapSetInfo { OnlineID = -1 }), Is.False);
            Assert.That(new BeatmapSetInfo { OnlineID = -1 }.MatchesOnlineID(a), Is.False);
            Assert.That(a.MatchesOnlineID(new BeatmapSetInfo { OnlineID = 1 }), Is.True);
            Assert.That(a.MatchesOnlineID(new BeatmapSetInfo { OnlineID = 2 }), Is.False);

            Assert.That(new BeatmapInfo { OnlineID = 5 }.MatchesOnlineID(new BeatmapInfo { OnlineID = 5 }), Is.True);
            Assert.That(new RulesetInfo { OnlineID = 1 }.MatchesOnlineID(new RulesetInfo { OnlineID = 1 }), Is.True);
            Assert.That(new APIUser { Id = 9 }.MatchesOnlineID(new APIUser { Id = 9 }), Is.True);
        }

        // CF: ModelExtensions.MatchesOnlineID | matchesOnlineID(instance, other) (score) | TRUE
        [Test]
        public void TestScoreMatchesOnlineID()
        {
            Assert.That(new ScoreInfo { OnlineID = 3 }.MatchesOnlineID(new ScoreInfo { OnlineID = 3 }), Is.True);
        }

        // CF: ModelExtensions.MatchesOnlineID | matchesOnlineID(instance, other) (score) | FALSE
        // CF: ModelExtensions.MatchesOnlineID | instance == null || other == null | TRUE
        [Test]
        public void TestScoreMatchesOnlineIDNull()
        {
            Assert.That(new ScoreInfo().MatchesOnlineID(null), Is.False);
            Assert.That(((ScoreInfo?)null).MatchesOnlineID(new ScoreInfo()), Is.False);
        }

        // CF: ModelExtensions.MatchesOnlineID | instance == null || other == null | FALSE
        // CF: ModelExtensions.MatchesOnlineID | instance.LegacyOnlineID < 0 || other.LegacyOnlineID < 0 | TRUE
        // CF: ModelExtensions.MatchesOnlineID | instance.LegacyOnlineID < 0 | TRUE
        // CF: ModelExtensions.MatchesOnlineID | other.LegacyOnlineID < 0 | TRUE
        [Test]
        public void TestScoreLegacyIDNegative()
        {
            Assert.That(new ScoreInfo { LegacyOnlineID = 5 }.MatchesOnlineID(new ScoreInfo { LegacyOnlineID = -1 }), Is.False);
            Assert.That(new ScoreInfo { LegacyOnlineID = -1 }.MatchesOnlineID(new ScoreInfo { LegacyOnlineID = 5 }), Is.False);
        }

        // CF: ModelExtensions.MatchesOnlineID | instance.LegacyOnlineID.Equals(other.LegacyOnlineID) | TRUE
        // CF: ModelExtensions.MatchesOnlineID | instance.LegacyOnlineID.Equals(other.LegacyOnlineID) | FALSE
        [Test]
        public void TestScoreLegacyIDCompared()
        {
            Assert.That(new ScoreInfo { LegacyOnlineID = 5 }.MatchesOnlineID(new ScoreInfo { LegacyOnlineID = 5 }), Is.True);
            Assert.That(new ScoreInfo { LegacyOnlineID = 5 }.MatchesOnlineID(new ScoreInfo { LegacyOnlineID = 6 }), Is.False);
        }

        // CF: ModelExtensions.GetValidFilename | foreach invalid_filename_chars | loop n
        [Test]
        public void TestGetValidFilename()
        {
            Assert.That("a\"b<c>d|e:f*g?h\\i/j\0k".GetValidFilename(), Is.EqualTo("abcdefghijk"));
            Assert.That("clean name.osz".GetValidFilename(), Is.EqualTo("clean name.osz"));
        }

        // CF: ModelExtensions.RequiresSupporter | scope | case Local
        [Test]
        public void TestRequiresSupporterLocal()
        {
            Assert.That(BeatmapLeaderboardScope.Local.RequiresSupporter(true), Is.False);
        }

        // CF: ModelExtensions.RequiresSupporter | scope | case Country
        // CF: ModelExtensions.RequiresSupporter | scope | case Friend
        [TestCase(BeatmapLeaderboardScope.Country)]
        [TestCase(BeatmapLeaderboardScope.Friend)]
        public void TestRequiresSupporterCountryFriend(BeatmapLeaderboardScope scope)
        {
            Assert.That(scope.RequiresSupporter(false), Is.True);
        }

        // CF: ModelExtensions.RequiresSupporter | scope | default
        [Test]
        public void TestRequiresSupporterGlobalDependsOnModFilter()
        {
            Assert.That(BeatmapLeaderboardScope.Global.RequiresSupporter(true), Is.True);
            Assert.That(BeatmapLeaderboardScope.Global.RequiresSupporter(false), Is.False);
        }

        private class FakeFile : IFileInfo
        {
            public FakeFile(string hash) => Hash = hash;
            public string Hash { get; }
        }

        private class NullToString
        {
            public override string? ToString() => null;
        }
    }
}
