// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Graphics.Carousel;
using osu.Game.Models;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Filter;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Select;
using osu.Game.Screens.Select.Filter;

namespace osu.Game.Tests.Screens.Select
{
    [TestFixture]
    public class BeatmapCarouselFilterMatchingTest
    {
        private static readonly RulesetInfo osu_ruleset = new RulesetInfo { ShortName = "osu", OnlineID = 0 };
        private static readonly RulesetInfo taiko_ruleset = new RulesetInfo { ShortName = "taiko", OnlineID = 1 };

        private static BeatmapInfo beatmap(Action<BeatmapInfo>? configure = null)
        {
            var b = new BeatmapInfo(osu_ruleset, new BeatmapDifficulty(), new BeatmapMetadata
            {
                Artist = "Artist",
                ArtistUnicode = "ArtistU",
                Title = "Title",
                TitleUnicode = "TitleU",
                Author = new RealmUser { Username = "mapper" },
                Source = "Source",
            }) { DifficultyName = "Insane", ID = Guid.NewGuid() };
            b.BeatmapSet = new BeatmapSetInfo(new[] { b });
            configure?.Invoke(b);
            return b;
        }

        private static bool match(BeatmapInfo b, Action<FilterCriteria> configure)
        {
            var c = new FilterCriteria();
            configure(c);
            return BeatmapCarouselFilterMatching.CheckCriteriaMatch(b, c);
        }

        private static FilterCriteria.OptionalTextFilter text(string term, bool exclude = false) => new FilterCriteria.OptionalTextFilter { SearchTerm = term, ExcludeTerm = exclude };

        // CF: BeatmapCarouselFilterMatching.Run | foreach items | loop 0
        [Test]
        public async Task TestRunEmpty()
        {
            var filter = new BeatmapCarouselFilterMatching(() => new FilterCriteria());
            Assert.That(await filter.Run(Array.Empty<CarouselItem>(), CancellationToken.None), Is.Empty);
            Assert.That(filter.BeatmapItemsCount, Is.EqualTo(0));
        }

        // CF: BeatmapCarouselFilterMatching.matchItems | foreach items | loop n
        // CF: BeatmapCarouselFilterMatching.matchItems | beatmap.Hidden | TRUE
        // CF: BeatmapCarouselFilterMatching.matchItems | beatmap.Hidden | FALSE
        // CF: BeatmapCarouselFilterMatching.matchItems | !CheckCriteriaMatch(beatmap, criteria) | TRUE
        // CF: BeatmapCarouselFilterMatching.matchItems | !CheckCriteriaMatch(beatmap, criteria) | FALSE
        [Test]
        public async Task TestRunFiltersHiddenAndNonMatching()
        {
            var visible = beatmap();
            var hidden = beatmap(b => b.Hidden = true);
            var nonMatching = beatmap(b => b.DifficultyName = "Easy");

            var criteria = new FilterCriteria { DifficultyName = text("Insane") };
            var filter = new BeatmapCarouselFilterMatching(() => criteria);

            var result = await filter.Run(new[] { visible, hidden, nonMatching }.Select(b => new CarouselItem(b)), CancellationToken.None);

            Assert.That(result.Select(r => r.Model), Is.EqualTo(new[] { visible }));
            Assert.That(filter.BeatmapItemsCount, Is.EqualTo(1));
        }

        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | criteria.Ruleset == null | TRUE
        [Test]
        public void TestNoCriteriaMatches()
        {
            Assert.That(match(beatmap(), _ => { }), Is.True);
        }

        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | criteria.Ruleset == null | FALSE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | beatmap.AllowGameplayWithRuleset(...) | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | beatmap.AllowGameplayWithRuleset(...) | FALSE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | !match (ruleset) | TRUE
        [Test]
        public void TestRulesetFilter()
        {
            Assert.That(match(beatmap(), c => c.Ruleset = osu_ruleset), Is.True);
            Assert.That(match(beatmap(), c => c.Ruleset = taiko_ruleset), Is.False);
            Assert.That(match(beatmap(), c => { c.Ruleset = taiko_ruleset; c.AllowConvertedBeatmaps = true; }), Is.True);
        }

        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | criteria.SelectedBeatmapSet != null | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | beatmap.BeatmapSet?.Equals(SelectedBeatmapSet) == true && match | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | beatmap.BeatmapSet?.Equals(SelectedBeatmapSet) == true | FALSE
        [Test]
        public void TestSelectedBeatmapSet()
        {
            var b = beatmap();
            Assert.That(match(b, c => c.SelectedBeatmapSet = b.BeatmapSet), Is.True);
            Assert.That(match(b, c => c.SelectedBeatmapSet = new BeatmapSetInfo { ID = Guid.NewGuid() }), Is.False);
            // a selected set only checks ruleset: other filters are ignored.
            Assert.That(match(b, c => { c.SelectedBeatmapSet = b.BeatmapSet; c.DifficultyName = text("nothing"); }), Is.True);
            // ... and the ruleset match still applies.
            Assert.That(match(b, c => { c.SelectedBeatmapSet = b.BeatmapSet; c.Ruleset = taiko_ruleset; }), Is.False);
        }

        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | criteria.SearchTerms.Length > 0 | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | criteria.SearchTerms.Length > 0 | FALSE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | !match && criteria.SearchNumber.HasValue | FALSE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | !match (after search) | TRUE
        [Test]
        public void TestSearchText()
        {
            Assert.That(match(beatmap(), c => c.SearchText = "Title"), Is.True);
            Assert.That(match(beatmap(), c => c.SearchText = "zzz"), Is.False);
        }

        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | !match && criteria.SearchNumber.HasValue | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | beatmap.OnlineID == SearchNumber | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | beatmap.OnlineID == SearchNumber | FALSE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | beatmap.BeatmapSet?.OnlineID == SearchNumber | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | beatmap.BeatmapSet?.OnlineID == SearchNumber | FALSE
        [Test]
        public void TestSearchNumberMatchesOnlineIDs()
        {
            var b = beatmap(x => { x.OnlineID = 123; x.BeatmapSet!.OnlineID = 456; });
            Assert.That(match(b, c => c.SearchText = "123"), Is.True);
            Assert.That(match(b, c => c.SearchText = "456"), Is.True);
            Assert.That(match(b, c => c.SearchText = "789"), Is.False);
        }

        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | !criteria.StarDifficulty.HasFilter | FALSE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | criteria.StarDifficulty.IsInRange(...) | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | criteria.StarDifficulty.IsInRange(...) | FALSE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | !criteria.StarDifficulty.HasFilter | TRUE
        [Test]
        public void TestStarDifficultyFilter()
        {
            var b = beatmap(x => x.StarRating = 5.678);
            Assert.That(match(b, c => c.StarDifficulty = new FilterCriteria.OptionalRange<double> { Min = 5.67, IsLowerInclusive = true }), Is.True);
            Assert.That(match(b, c => c.StarDifficulty = new FilterCriteria.OptionalRange<double> { Min = 5.68, IsLowerInclusive = true }), Is.False);
        }

        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | difficulty/length/bpm/divisor/status range filters | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | difficulty/length/bpm/divisor/status range filters | FALSE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | !criteria.LastPlayed.HasFilter || IsInRange(beatmap.LastPlayed ?? MinValue) | FALSE
        [Test]
        public void TestNumericFilters()
        {
            var b = beatmap(x =>
            {
                x.Difficulty.ApproachRate = 9;
                x.Difficulty.DrainRate = 5;
                x.Difficulty.CircleSize = 4;
                x.Difficulty.OverallDifficulty = 8;
                x.Length = 120000;
                x.BPM = 180;
                x.BeatDivisor = 4;
                x.Status = BeatmapOnlineStatus.Ranked;
            });

            var inRange = new FilterCriteria.OptionalRange<float> { Min = 0, Max = 10 };
            var outRange = new FilterCriteria.OptionalRange<float> { Min = 100 };

            Assert.That(match(b, c => c.ApproachRate = inRange), Is.True);
            Assert.That(match(b, c => c.ApproachRate = outRange), Is.False);
            Assert.That(match(b, c => c.DrainRate = inRange), Is.True);
            Assert.That(match(b, c => c.DrainRate = outRange), Is.False);
            Assert.That(match(b, c => c.CircleSize = inRange), Is.True);
            Assert.That(match(b, c => c.CircleSize = outRange), Is.False);
            Assert.That(match(b, c => c.OverallDifficulty = inRange), Is.True);
            Assert.That(match(b, c => c.OverallDifficulty = outRange), Is.False);
            Assert.That(match(b, c => c.Length = new FilterCriteria.OptionalRange<double> { Max = 130000 }), Is.True);
            Assert.That(match(b, c => c.Length = new FilterCriteria.OptionalRange<double> { Max = 1000 }), Is.False);
            Assert.That(match(b, c => c.BPM = new FilterCriteria.OptionalRange<double> { Min = 100 }), Is.True);
            Assert.That(match(b, c => c.BPM = new FilterCriteria.OptionalRange<double> { Min = 200 }), Is.False);
            Assert.That(match(b, c => c.BeatDivisor = new FilterCriteria.OptionalRange<int> { Min = 4, Max = 4, IsLowerInclusive = true, IsUpperInclusive = true }), Is.True);
            Assert.That(match(b, c => c.BeatDivisor = new FilterCriteria.OptionalRange<int> { Min = 8 }), Is.False);

            Assert.That(match(b, c => c.OnlineStatus.Values.Clear()), Is.False);
            Assert.That(match(b, c => c.OnlineStatus.Values.IntersectWith(new[] { BeatmapOnlineStatus.Ranked })), Is.True);
        }

        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | beatmap.LastPlayed ?? DateTimeOffset.MinValue | null
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | beatmap.LastPlayed ?? DateTimeOffset.MinValue | non-null
        [Test]
        public void TestLastPlayedFilter()
        {
            var never = new FilterCriteria.OptionalRange<DateTimeOffset> { Min = DateTimeOffset.MinValue, Max = DateTimeOffset.MinValue, IsLowerInclusive = true, IsUpperInclusive = true };
            Assert.That(match(beatmap(), c => c.LastPlayed = never), Is.True);
            Assert.That(match(beatmap(x => x.LastPlayed = DateTimeOffset.UnixEpoch), c => c.LastPlayed = never), Is.False);
        }

        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | beatmap.BeatmapSet?.DateRanked != null && IsInRange | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | beatmap.BeatmapSet?.DateRanked != null | FALSE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | beatmap.BeatmapSet?.DateSubmitted != null && IsInRange | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | beatmap.BeatmapSet?.DateSubmitted != null | FALSE
        [Test]
        public void TestDateFilters()
        {
            var range = new FilterCriteria.OptionalRange<DateTimeOffset> { Min = DateTimeOffset.UnixEpoch };
            var b = beatmap(x =>
            {
                x.BeatmapSet!.DateRanked = DateTimeOffset.UnixEpoch.AddDays(10);
                x.BeatmapSet.DateSubmitted = DateTimeOffset.UnixEpoch.AddDays(5);
            });

            Assert.That(match(b, c => c.DateRanked = range), Is.True);
            Assert.That(match(b, c => c.DateSubmitted = range), Is.True);
            Assert.That(match(beatmap(), c => c.DateRanked = range), Is.False);
            Assert.That(match(beatmap(), c => c.DateSubmitted = range), Is.False);
            Assert.That(match(b, c => c.DateRanked = new FilterCriteria.OptionalRange<DateTimeOffset> { Max = DateTimeOffset.UnixEpoch }), Is.False);
        }

        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | !criteria.Creator.HasFilter || Creator.Matches(...) | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | !criteria.Creator.HasFilter || Creator.Matches(...) | FALSE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | DifficultyName / Source filters | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | DifficultyName / Source filters | FALSE
        [Test]
        public void TestSimpleTextFilters()
        {
            var b = beatmap();
            Assert.That(match(b, c => c.Creator = text("mapp")), Is.True);
            Assert.That(match(b, c => c.Creator = text("other")), Is.False);
            Assert.That(match(b, c => c.DifficultyName = text("insane")), Is.True);
            Assert.That(match(b, c => c.DifficultyName = text("easy")), Is.False);
            Assert.That(match(b, c => c.Source = text("sour")), Is.True);
            Assert.That(match(b, c => c.Source = text("zzz")), Is.False);
        }

        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | criteria.Artist.HasFilter | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | criteria.Artist.HasFilter | FALSE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | criteria.Artist.ExcludeTerm | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | criteria.Artist.ExcludeTerm | FALSE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | Artist.Matches(Artist) && Artist.Matches(ArtistUnicode) | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | Artist.Matches(Artist) && Artist.Matches(ArtistUnicode) | FALSE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | Artist.Matches(Artist) || Artist.Matches(ArtistUnicode) | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | Artist.Matches(Artist) || Artist.Matches(ArtistUnicode) | FALSE
        [Test]
        public void TestArtistFilter()
        {
            var b = beatmap();
            Assert.That(match(b, c => c.Artist = text("Artist")), Is.True);
            Assert.That(match(b, c => c.Artist = text("zzz")), Is.False);
            // only the unicode variant matches
            Assert.That(match(beatmap(x => x.Metadata.Artist = "x"), c => c.Artist = text("ArtistU")), Is.True);
            // exclusion requires both variants to not match
            Assert.That(match(b, c => c.Artist = text("zzz", true)), Is.True);
            Assert.That(match(b, c => c.Artist = text("Artist", true)), Is.False);
        }

        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | criteria.Title.HasFilter | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | criteria.Title.ExcludeTerm | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | criteria.Title.ExcludeTerm | FALSE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | Title.Matches(Title) && Title.Matches(TitleUnicode) | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | Title.Matches(Title) && Title.Matches(TitleUnicode) | FALSE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | Title.Matches(Title) || Title.Matches(TitleUnicode) | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | Title.Matches(Title) || Title.Matches(TitleUnicode) | FALSE
        [Test]
        public void TestTitleFilter()
        {
            var b = beatmap();
            Assert.That(match(b, c => c.Title = text("Title")), Is.True);
            Assert.That(match(b, c => c.Title = text("zzz")), Is.False);
            Assert.That(match(beatmap(x => x.Metadata.Title = "x"), c => c.Title = text("TitleU")), Is.True);
            Assert.That(match(b, c => c.Title = text("zzz", true)), Is.True);
            Assert.That(match(b, c => c.Title = text("Title", true)), Is.False);
        }

        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | criteria.UserTags.Any() | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | criteria.UserTags.Any() | FALSE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | foreach criteria.UserTags | loop n
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | tagFilter.ExcludeTerm | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | tagFilter.ExcludeTerm | FALSE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | foreach beatmap.Metadata.UserTags | loop n
        [Test]
        public void TestUserTagFilters()
        {
            var b = beatmap(x =>
            {
                x.Metadata.UserTags.Add("anime");
                x.Metadata.UserTags.Add("jump");
            });

            Assert.That(match(b, c => c.UserTags.Add(text("anime"))), Is.True);
            Assert.That(match(b, c => c.UserTags.Add(text("zzz"))), Is.False);
            Assert.That(match(b, c => c.UserTags.Add(text("anime", true))), Is.False);
            Assert.That(match(b, c => c.UserTags.Add(text("zzz", true))), Is.True);
            Assert.That(match(b, c => { c.UserTags.Add(text("anime")); c.UserTags.Add(text("jump")); }), Is.True);
        }

        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | foreach beatmap.Metadata.UserTags | loop 0
        [Test]
        public void TestUserTagFiltersNoTags()
        {
            Assert.That(match(beatmap(), c => c.UserTags.Add(text("anime"))), Is.False);
            Assert.That(match(beatmap(), c => c.UserTags.Add(text("anime", true))), Is.True);
        }

        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | !criteria.UserStarDifficulty.HasFilter | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | UserStarDifficulty.IsInRange(beatmap.StarRating) | FALSE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | !match (after tags) | TRUE
        [Test]
        public void TestUserStarDifficultyFilter()
        {
            var b = beatmap(x => x.StarRating = 3);
            Assert.That(match(b, c => c.UserStarDifficulty = new FilterCriteria.OptionalRange<double> { Min = 5 }), Is.False);
            Assert.That(match(b, c => c.UserStarDifficulty = new FilterCriteria.OptionalRange<double> { Min = 1 }), Is.True);
        }

        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | CollectionBeatmapMD5Hashes?.Contains(MD5Hash) ?? true | null
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | CollectionBeatmapMD5Hashes?.Contains(MD5Hash) ?? true | non-null
        [Test]
        public void TestCollectionFilter()
        {
            var b = beatmap(x => x.MD5Hash = "abc");
            Assert.That(match(b, c => c.CollectionBeatmapMD5Hashes = new[] { "abc" }), Is.True);
            Assert.That(match(b, c => c.CollectionBeatmapMD5Hashes = new[] { "def" }), Is.False);
        }

        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | match && criteria.RulesetCriteria != null | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | match && criteria.RulesetCriteria != null | FALSE
        [Test]
        public void TestRulesetCriteria()
        {
            Assert.That(match(beatmap(), c => c.RulesetCriteria = new FakeRulesetCriteria(true)), Is.True);
            Assert.That(match(beatmap(), c => c.RulesetCriteria = new FakeRulesetCriteria(false)), Is.False);
        }

        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | match && criteria.HasOnlineID == true | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | match && criteria.HasOnlineID == true | FALSE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | beatmap.OnlineID >= 0 | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | beatmap.OnlineID >= 0 | FALSE
        [Test]
        public void TestHasOnlineIDFilter()
        {
            Assert.That(match(beatmap(x => x.OnlineID = 5), c => c.HasOnlineID = true), Is.True);
            Assert.That(match(beatmap(), c => c.HasOnlineID = true), Is.False);
            Assert.That(match(beatmap(), c => c.HasOnlineID = false), Is.True);
        }

        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | match && criteria.BeatmapSetId != null | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | match && criteria.BeatmapSetId != null | FALSE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | criteria.BeatmapSetId == beatmap.BeatmapSet?.OnlineID | TRUE
        // CF: BeatmapCarouselFilterMatching.CheckCriteriaMatch | criteria.BeatmapSetId == beatmap.BeatmapSet?.OnlineID | FALSE
        [Test]
        public void TestBeatmapSetIdFilter()
        {
            var b = beatmap(x => x.BeatmapSet!.OnlineID = 77);
            Assert.That(match(b, c => c.BeatmapSetId = 77), Is.True);
            Assert.That(match(b, c => c.BeatmapSetId = 78), Is.False);
            Assert.That(match(b, c => { }), Is.True);
        }

        private class FakeRulesetCriteria : IRulesetFilterCriteria
        {
            private readonly bool result;

            public FakeRulesetCriteria(bool result) => this.result = result;

            public bool Matches(BeatmapInfo beatmapInfo, FilterCriteria criteria) => result;

            public bool TryParseCustomKeywordCriteria(string key, Operator op, string value) => false;

            public bool FilterMayChangeFromMods(FilterCriteria criteria, ValueChangedEvent<IReadOnlyList<Mod>> mods) => false;
        }
    }
}
