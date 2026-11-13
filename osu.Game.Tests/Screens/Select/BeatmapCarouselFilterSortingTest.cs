// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Graphics.Carousel;
using osu.Game.Models;
using osu.Game.Rulesets;
using osu.Game.Screens.Select;
using osu.Game.Screens.Select.Filter;

namespace osu.Game.Tests.Screens.Select
{
    [TestFixture]
    public class BeatmapCarouselFilterSortingTest
    {
        private static int counter;

        private static BeatmapInfo beatmap(Action<BeatmapInfo>? configure = null, RulesetInfo? ruleset = null)
        {
            var b = new BeatmapInfo(ruleset ?? new RulesetInfo { ShortName = "osu", OnlineID = 0 }, new BeatmapDifficulty(), new BeatmapMetadata
            {
                Artist = "A",
                Title = "T",
                Author = new RealmUser { Username = "m" },
            }) { ID = Guid.NewGuid() };

            b.BeatmapSet = new BeatmapSetInfo(new[] { b }) { DateAdded = DateTimeOffset.UnixEpoch.AddDays(counter++) };
            configure?.Invoke(b);
            return b;
        }

        private static async Task<BeatmapInfo[]> sort(SortMode mode, GroupMode group, params BeatmapInfo[] beatmaps)
        {
            var filter = new BeatmapCarouselFilterSorting(() => new FilterCriteria { Sort = mode, Group = group });
            var result = await filter.Run(beatmaps.Select(b => new CarouselItem(b)).ToList(), CancellationToken.None);
            Assert.That(filter.BeatmapItemsCount, Is.EqualTo(beatmaps.Length));
            return result.Select(r => (BeatmapInfo)r.Model).ToArray();
        }

        // CF: BeatmapCarouselFilterSorting.compare | sort | case Artist
        // CF: BeatmapCarouselFilterSorting.compare | comparison == 0 (artist) | TRUE
        // CF: BeatmapCarouselFilterSorting.compare | comparison == 0 (artist) | FALSE
        // CF: BeatmapCarouselFilterSorting.compare | sort | case Title
        [Test]
        public async Task TestSortByArtistThenTitle()
        {
            var b = beatmap(x => { x.BeatmapSet!.Beatmaps[0].Metadata.Artist = "B"; });
            var a2 = beatmap(x => x.Metadata.Title = "Z");
            var a1 = beatmap(x => x.Metadata.Title = "Y");

            Assert.That(await sort(SortMode.Artist, GroupMode.None, b, a2, a1), Is.EqualTo(new[] { a1, a2, b }));
        }

        // CF: BeatmapCarouselFilterSorting.compare | sort | case Title
        [Test]
        public async Task TestSortByTitle()
        {
            var z = beatmap(x => x.Metadata.Title = "Z");
            var a = beatmap(x => x.Metadata.Title = "A");
            Assert.That(await sort(SortMode.Title, GroupMode.None, z, a), Is.EqualTo(new[] { a, z }));
        }

        // CF: BeatmapCarouselFilterSorting.compare | sort | case Author
        [Test]
        public async Task TestSortByAuthor()
        {
            var z = beatmap(x => x.Metadata.Author = new RealmUser { Username = "z" });
            var a = beatmap(x => x.Metadata.Author = new RealmUser { Username = "a" });
            Assert.That(await sort(SortMode.Author, GroupMode.None, z, a), Is.EqualTo(new[] { a, z }));
        }

        // CF: BeatmapCarouselFilterSorting.compare | sort | case Source
        [Test]
        public async Task TestSortBySource()
        {
            var z = beatmap(x => x.Metadata.Source = "z");
            var a = beatmap(x => x.Metadata.Source = "a");
            Assert.That(await sort(SortMode.Source, GroupMode.None, z, a), Is.EqualTo(new[] { a, z }));
        }

        // CF: BeatmapCarouselFilterSorting.compare | sort | case Difficulty
        [Test]
        public async Task TestSortByDifficulty()
        {
            var hard = beatmap(x => x.StarRating = 5);
            var easy = beatmap(x => x.StarRating = 1);
            Assert.That(await sort(SortMode.Difficulty, GroupMode.None, hard, easy), Is.EqualTo(new[] { easy, hard }));
        }

        // CF: BeatmapCarouselFilterSorting.compare | sort | case DateAdded
        [Test]
        public async Task TestSortByDateAddedNewestFirst()
        {
            var older = beatmap();
            var newer = beatmap();
            Assert.That(await sort(SortMode.DateAdded, GroupMode.None, older, newer), Is.EqualTo(new[] { newer, older }));
        }

        // CF: BeatmapCarouselFilterSorting.compare | sort | case DateRanked
        [Test]
        public async Task TestSortByDateRankedNewestFirst()
        {
            var older = beatmap(x => x.BeatmapSet!.DateRanked = DateTimeOffset.UnixEpoch);
            var newer = beatmap(x => x.BeatmapSet!.DateRanked = DateTimeOffset.UnixEpoch.AddDays(5));
            Assert.That(await sort(SortMode.DateRanked, GroupMode.None, older, newer), Is.EqualTo(new[] { newer, older }));
        }

        // CF: BeatmapCarouselFilterSorting.compare | sort | case DateSubmitted
        [Test]
        public async Task TestSortByDateSubmittedNewestFirst()
        {
            var older = beatmap(x => x.BeatmapSet!.DateSubmitted = DateTimeOffset.UnixEpoch);
            var newer = beatmap(x => x.BeatmapSet!.DateSubmitted = DateTimeOffset.UnixEpoch.AddDays(5));
            Assert.That(await sort(SortMode.DateSubmitted, GroupMode.None, older, newer), Is.EqualTo(new[] { newer, older }));
        }

        // CF: BeatmapCarouselFilterSorting.compare | sort | case LastPlayed
        // CF: BeatmapCarouselFilterSorting.compare | aggregate (LastPlayed) | FALSE
        [Test]
        public async Task TestSortByLastPlayedMostRecentFirst()
        {
            var older = beatmap(x => x.LastPlayed = DateTimeOffset.UnixEpoch);
            var newer = beatmap(x => x.LastPlayed = DateTimeOffset.UnixEpoch.AddDays(5));
            Assert.That(await sort(SortMode.LastPlayed, GroupMode.Difficulty, older, newer), Is.EqualTo(new[] { newer, older }));
        }

        // CF: BeatmapCarouselFilterSorting.compare | sort | case BPM
        // CF: BeatmapCarouselFilterSorting.compare | aggregate (BPM) | FALSE
        [Test]
        public async Task TestSortByBpm()
        {
            var fast = beatmap(x => x.BPM = 200);
            var slow = beatmap(x => x.BPM = 100);
            Assert.That(await sort(SortMode.BPM, GroupMode.Difficulty, fast, slow), Is.EqualTo(new[] { slow, fast }));
        }

        // CF: BeatmapCarouselFilterSorting.compare | sort | case Length
        // CF: BeatmapCarouselFilterSorting.compare | aggregate (Length) | FALSE
        [Test]
        public async Task TestSortByLength()
        {
            var longer = beatmap(x => x.Length = 200);
            var shorter = beatmap(x => x.Length = 100);
            Assert.That(await sort(SortMode.Length, GroupMode.Difficulty, longer, shorter), Is.EqualTo(new[] { shorter, longer }));
        }

        // CF: BeatmapCarouselFilterSorting.compare | sort | default
        [Test]
        public void TestUnknownSortMode()
        {
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await sort((SortMode)99, GroupMode.None, beatmap(), beatmap()));
            Assert.That(ex!.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
        }

        // CF: BeatmapCarouselFilterSorting.compare | comparison == 0 (title fallback) | TRUE
        // CF: BeatmapCarouselFilterSorting.compare | comparison == 0 (date added fallback) | TRUE
        // CF: BeatmapCarouselFilterSorting.compare | comparison == 0 (set ID fallback) | TRUE
        [Test]
        public async Task TestFallbackTiebreakers()
        {
            // identical on every key but title: title breaks the tie.
            var t1 = beatmap(x => { x.Metadata.Title = "B"; x.BPM = 1; });
            var t2 = beatmap(x => { x.Metadata.Title = "A"; x.BPM = 1; });
            Assert.That(await sort(SortMode.BPM, GroupMode.None, t1, t2), Is.EqualTo(new[] { t2, t1 }));

            // identical on title as well: newer date added first.
            var d1 = beatmap(x => x.BPM = 1);
            var d2 = beatmap(x => x.BPM = 1);
            Assert.That(await sort(SortMode.BPM, GroupMode.None, d1, d2), Is.EqualTo(new[] { d2, d1 }));

            // identical dates too: set ID decides (descending).
            var i1 = beatmap(x => x.BPM = 1);
            var i2 = beatmap(x => x.BPM = 1);
            i2.BeatmapSet!.DateAdded = i1.BeatmapSet!.DateAdded;
            var expected = new[] { i1, i2 }.OrderByDescending(x => x.BeatmapSet!.ID).ToArray();
            Assert.That(await sort(SortMode.BPM, GroupMode.None, i1, i2), Is.EqualTo(expected));
        }

        // CF: BeatmapCarouselFilterSorting.Run | groupedSets | TRUE
        // CF: BeatmapCarouselFilterSorting.Run | ab.BeatmapSet!.Equals(bb.BeatmapSet) | TRUE
        // CF: BeatmapCarouselFilterSorting.compareDifficulty | comparison == 0 | TRUE
        // CF: BeatmapCarouselFilterSorting.compareDifficulty | comparison == 0 | FALSE
        [Test]
        public async Task TestSameSetSortedByRulesetThenStars()
        {
            var osu = new RulesetInfo { ShortName = "osu", OnlineID = 0 };
            var taiko = new RulesetInfo { ShortName = "taiko", OnlineID = 1 };

            var a = beatmap(x => x.StarRating = 5, osu);
            var set = a.BeatmapSet!;
            var b = new BeatmapInfo(osu, new BeatmapDifficulty(), a.Metadata) { StarRating = 2, ID = Guid.NewGuid(), BeatmapSet = set };
            var c = new BeatmapInfo(taiko, new BeatmapDifficulty(), a.Metadata) { StarRating = 1, ID = Guid.NewGuid(), BeatmapSet = set };

            Assert.That(await sort(SortMode.Title, GroupMode.None, c, a, b), Is.EqualTo(new[] { b, a, c }));
        }

        // CF: BeatmapCarouselFilterSorting.Run | groupedSets | FALSE
        // CF: BeatmapCarouselFilterSorting.compare | aggregate (grouped sets) | TRUE
        // CF: BeatmapCarouselFilterSorting.compare | aggregate (LastPlayed) | TRUE
        // CF: BeatmapCarouselFilterSorting.compare | aggregate (BPM) | TRUE
        // CF: BeatmapCarouselFilterSorting.compare | aggregate (Length) | TRUE
        // CF: BeatmapCarouselFilterSorting.compareUsingAggregateMax | !aAny && !bAny | TRUE
        [Test]
        public async Task TestAggregateSortsAcrossSets()
        {
            var slowSetWithFastDiff = beatmap(x => x.BPM = 100);
            slowSetWithFastDiff.BeatmapSet!.Beatmaps.Add(new BeatmapInfo(slowSetWithFastDiff.Ruleset, new BeatmapDifficulty(), slowSetWithFastDiff.Metadata) { BPM = 300, BeatmapSet = slowSetWithFastDiff.BeatmapSet });
            var mid = beatmap(x => x.BPM = 200);

            Assert.That(await sort(SortMode.BPM, GroupMode.None, slowSetWithFastDiff, mid), Is.EqualTo(new[] { mid, slowSetWithFastDiff }));

            var long1 = beatmap(x => x.Length = 1000);
            long1.BeatmapSet!.Beatmaps.Add(new BeatmapInfo(long1.Ruleset, new BeatmapDifficulty(), long1.Metadata) { Length = 9000, BeatmapSet = long1.BeatmapSet });
            var long2 = beatmap(x => x.Length = 5000);
            Assert.That(await sort(SortMode.Length, GroupMode.None, long1, long2), Is.EqualTo(new[] { long2, long1 }));

            var played1 = beatmap(x => x.LastPlayed = DateTimeOffset.UnixEpoch);
            var played2 = beatmap(x => x.LastPlayed = DateTimeOffset.UnixEpoch.AddDays(1));
            var unplayed = beatmap();
            Assert.That(await sort(SortMode.LastPlayed, GroupMode.None, played1, unplayed, played2), Is.EqualTo(new[] { played2, played1, unplayed }));

            // all hidden on both sides compares equal, falling through to the tiebreakers.
            var h1 = beatmap(x => x.Hidden = true);
            var h2 = beatmap(x => x.Hidden = true);
            Assert.That(await sort(SortMode.BPM, GroupMode.None, h1, h2), Is.EqualTo(new[] { h2, h1 }));
        }

        // CF: BeatmapCarouselFilterSorting.compareUsingAggregateMax | !aAny | TRUE
        // CF: BeatmapCarouselFilterSorting.compareUsingAggregateMax | !bAny | TRUE
        // CF: BeatmapCarouselFilterSorting.compareUsingAggregateMax | !aAny && !bAny | FALSE
        // CF: BeatmapCarouselFilterSorting.compareUsingAggregateMax | !aAny | FALSE
        // CF: BeatmapCarouselFilterSorting.compareUsingAggregateMax | !bAny | FALSE
        [Test]
        public async Task TestAggregateSortHiddenSetsSortFirst()
        {
            var hidden = beatmap(x => { x.Hidden = true; x.BPM = 999; });
            var shown = beatmap(x => x.BPM = 1);

            Assert.That(await sort(SortMode.BPM, GroupMode.None, shown, hidden), Is.EqualTo(new[] { hidden, shown }));
            Assert.That(await sort(SortMode.BPM, GroupMode.None, hidden, shown), Is.EqualTo(new[] { hidden, shown }));
        }
    }
}
