// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Moq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Select;

namespace osu.Game.Tests.Beatmaps
{
    [TestFixture]
    public class BeatmapInfoExtensionsTest
    {
        private static BeatmapInfo info(string difficulty = "Hard", string artist = "A", string title = "T", string author = "")
            => new BeatmapInfo
            {
                DifficultyName = difficulty,
                Metadata = { Artist = artist, Title = title, Author = { Username = author } },
            };

        private static FilterCriteria.OptionalTextFilter filter(string term) => new FilterCriteria.OptionalTextFilter { SearchTerm = term };

        private static IAPIProvider api()
        {
            var mock = new Mock<IAPIProvider>();
            mock.SetupGet(a => a.Endpoints).Returns(new EndpointConfiguration { WebsiteUrl = "https://web.test" });
            return mock.Object;
        }

        [Test]
        public void TestUpdateStatisticsFromBeatmap()
        {
            var beatmap = new Beatmap();
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            beatmap.HitObjects.Add(new HitCircle { StartTime = 1000 });
            beatmap.HitObjects.Add(new Spinner { StartTime = 2000, Duration = 1000 });
            beatmap.HitObjects.Add(new Slider { StartTime = 5000 });
            var beatmapInfo = new BeatmapInfo();

            beatmapInfo.UpdateStatisticsFromBeatmap(beatmap);

            Assert.That(beatmapInfo.Length, Is.EqualTo(beatmap.CalculatePlayableLength()));
            Assert.That(beatmapInfo.BPM, Is.EqualTo(120).Within(1e-6));
            Assert.That(beatmapInfo.TotalObjectCount, Is.EqualTo(3));
            Assert.That(beatmapInfo.EndTimeObjectCount, Is.EqualTo(2), "spinner and slider have durations");
        }

        // CF: BeatmapInfoExtensions.getVersionString | string.IsNullOrEmpty(beatmapInfo.DifficultyName) | FALSE
        [Test]
        public void TestDisplayTitleIncludesDifficulty()
        {
            Assert.That(info().GetDisplayTitle(), Is.EqualTo("A - T [Hard]"));
        }

        // CF: BeatmapInfoExtensions.getVersionString | string.IsNullOrEmpty(beatmapInfo.DifficultyName) | TRUE
        [Test]
        public void TestDisplayTitleWithoutDifficultyIsTrimmed()
        {
            Assert.That(info("").GetDisplayTitle(), Is.EqualTo("A - T"));
        }

        // CF: BeatmapInfoExtensions.GetDisplayTitleRomanisable | includeDifficultyName | TRUE
        [Test]
        public void TestRomanisableTitleWithDifficulty()
        {
            var title = info(author: "me").GetDisplayTitleRomanisable();

            Assert.That(title.GetPreferred(false), Is.EqualTo("A - T (me) [Hard]"));
            Assert.That(title.GetPreferred(true), Is.EqualTo("A - T (me) [Hard]"));
        }

        // CF: BeatmapInfoExtensions.GetDisplayTitleRomanisable | includeDifficultyName | FALSE
        [Test]
        public void TestRomanisableTitleWithoutDifficultyOrCreator()
        {
            Assert.That(info(author: "me").GetDisplayTitleRomanisable(false, false).GetPreferred(false), Is.EqualTo("A - T"));
        }

        // CF: BeatmapInfoExtensions.Match | foreach filter | loop 0
        [Test]
        public void TestMatchWithoutFiltersIsTrue()
        {
            Assert.That(info().Match(), Is.True);
        }

        // CF: BeatmapInfoExtensions.Match | filter.Matches(DifficultyName) | TRUE
        [Test]
        public void TestMatchOnDifficultyName()
        {
            Assert.That(info("Insane Plus").Match(filter("insane")), Is.True);
        }

        // CF: BeatmapInfoExtensions.Match | filter.Matches(DifficultyName) | FALSE
        // CF: BeatmapInfoExtensions.Match | BeatmapMetadataInfoExtensions.Match(Metadata, filter) | TRUE
        [Test]
        public void TestMatchFallsBackToMetadata()
        {
            Assert.That(info("Easy", "Some Artist").Match(filter("artist")), Is.True);
        }

        // CF: BeatmapInfoExtensions.Match | BeatmapMetadataInfoExtensions.Match(Metadata, filter) | FALSE
        [Test]
        public void TestMatchFailsWhenAnyFilterMatchesNothing()
        {
            Assert.That(info("Easy", "Some Artist").Match(filter("artist"), filter("zzz")), Is.False);
            Assert.That(info("Easy", "Some Artist").Match(filter("easy"), filter("artist")), Is.True, "every filter may match a different field");
        }

        private static BeatmapInfo withRuleset(string shortName, int onlineId) => new BeatmapInfo(new RulesetInfo { ShortName = shortName, OnlineID = onlineId });

        // CF: BeatmapInfoExtensions.AllowGameplayWithRuleset | beatmap.Ruleset.ShortName == ruleset.ShortName | TRUE
        [Test]
        public void TestSameRulesetAllowed()
        {
            Assert.That(withRuleset("taiko", 1).AllowGameplayWithRuleset(new RulesetInfo { ShortName = "taiko", OnlineID = 1 }, false), Is.True);
        }

        // CF: BeatmapInfoExtensions.AllowGameplayWithRuleset | allowConversion && beatmap.Ruleset.OnlineID == 0 && ruleset.OnlineID != 0 | TRUE
        [Test]
        public void TestOsuBeatmapsConvertibleWhenAllowed()
        {
            Assert.That(withRuleset("osu", 0).AllowGameplayWithRuleset(new RulesetInfo { ShortName = "mania", OnlineID = 3 }, true), Is.True);
        }

        // CF: BeatmapInfoExtensions.AllowGameplayWithRuleset | allowConversion | FALSE
        [Test]
        public void TestConversionDisallowed()
        {
            Assert.That(withRuleset("osu", 0).AllowGameplayWithRuleset(new RulesetInfo { ShortName = "mania", OnlineID = 3 }, false), Is.False);
        }

        // CF: BeatmapInfoExtensions.AllowGameplayWithRuleset | beatmap.Ruleset.OnlineID == 0 | FALSE
        [Test]
        public void TestOnlyOsuBeatmapsConvert()
        {
            Assert.That(withRuleset("taiko", 1).AllowGameplayWithRuleset(new RulesetInfo { ShortName = "mania", OnlineID = 3 }, true), Is.False);
        }

        // CF: BeatmapInfoExtensions.AllowGameplayWithRuleset | ruleset.OnlineID != 0 | FALSE
        [Test]
        public void TestConversionToOsuFromOsuIsOnlyTheSameRulesetCase()
        {
            Assert.That(withRuleset("osu", 0).AllowGameplayWithRuleset(new RulesetInfo { ShortName = "other", OnlineID = 0 }, true), Is.False);
        }

        // CF: BeatmapInfoExtensions.GetOnlineURL | beatmapInfo.OnlineID <= 0 | TRUE
        // CF: BeatmapInfoExtensions.GetOnlineURL | beatmapInfo.BeatmapSet == null | TRUE
        [Test]
        public void TestNoUrlWithoutOnlineIdOrSet()
        {
            var noId = new BeatmapInfo(new RulesetInfo { ShortName = "osu" }) { OnlineID = -1, BeatmapSet = new BeatmapSetInfo { OnlineID = 5 } };
            var noSet = new BeatmapInfo(new RulesetInfo { ShortName = "osu" }) { OnlineID = 7 };

            Assert.That(noId.GetOnlineURL(api()), Is.Null);
            Assert.That(noSet.GetOnlineURL(api()), Is.Null);
        }

        // CF: BeatmapInfoExtensions.GetOnlineURL | ruleset?.ShortName ?? beatmapInfo.Ruleset.ShortName | default
        // CF: BeatmapInfoExtensions.GetOnlineURL | ruleset?.ShortName ?? beatmapInfo.Ruleset.ShortName | non-null
        [Test]
        public void TestUrlUsesBeatmapRulesetUnlessOverridden()
        {
            var beatmap = new BeatmapInfo(new RulesetInfo { ShortName = "osu" }) { OnlineID = 7, BeatmapSet = new BeatmapSetInfo { OnlineID = 5 } };

            Assert.That(beatmap.GetOnlineURL(api()), Is.EqualTo("https://web.test/beatmapsets/5#osu/7"));
            Assert.That(beatmap.GetOnlineURL(api(), new RulesetInfo { ShortName = "mania" }), Is.EqualTo("https://web.test/beatmapsets/5#mania/7"));
        }
    }
}
