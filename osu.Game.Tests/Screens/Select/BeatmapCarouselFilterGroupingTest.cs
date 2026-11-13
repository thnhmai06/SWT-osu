// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Collections;
using osu.Game.Graphics.Carousel;
using osu.Game.Models;
using osu.Game.Rulesets;
using osu.Game.Scoring;
using osu.Game.Screens.Select;
using osu.Game.Screens.Select.Filter;

namespace osu.Game.Tests.Screens.Select
{
    [TestFixture]
    public class BeatmapCarouselFilterGroupingTest
    {
        private FilterCriteria criteria = null!;
        private List<BeatmapCollection> collections = null!;
        private HashSet<int> favourites = null!;
        private Dictionary<Guid, ScoreRank> ranks = null!;

        [SetUp]
        public void SetUp()
        {
            criteria = new FilterCriteria();
            collections = new List<BeatmapCollection>();
            favourites = new HashSet<int>();
            ranks = new Dictionary<Guid, ScoreRank>();
        }

        private static BeatmapInfo beatmap(Action<BeatmapInfo>? configure = null, int difficulties = 1)
        {
            var infos = Enumerable.Range(0, difficulties).Select(_ => new BeatmapInfo(new RulesetInfo { ShortName = "osu", OnlineID = 0 }, new BeatmapDifficulty(), new BeatmapMetadata
            {
                Artist = "Artist",
                Title = "Title",
                Author = new RealmUser { Username = "mapper" },
            }) { ID = Guid.NewGuid(), MD5Hash = Guid.NewGuid().ToString() }).ToList();

            var set = new BeatmapSetInfo(infos);
            foreach (var i in infos)
                i.BeatmapSet = set;

            configure?.Invoke(infos[0]);
            return infos[0];
        }

        private async Task<(BeatmapCarouselFilterGrouping filter, List<CarouselItem> result)> run(params BeatmapInfo[] beatmaps)
        {
            var filter = new BeatmapCarouselFilterGrouping
            {
                GetCriteria = () => criteria,
                GetCollections = () => collections,
                GetLocalUserTopRanks = _ => ranks,
                GetFavouriteBeatmapSets = () => favourites,
            };

            var items = beatmaps.Select(b => new CarouselItem(b)).ToList();
            return (filter, await filter.Run(items, CancellationToken.None));
        }

        private static int[] orders(BeatmapCarouselFilterGrouping filter) => filter.GroupItems.Keys.Select(g => g.Order).OrderBy(o => o).ToArray();

        // CF: BeatmapCarouselFilterGrouping.ShouldGroupBeatmapsTogether | criteria.Sort == SortMode.Difficulty | TRUE
        [Test]
        public void TestShouldGroupSortByDifficulty() => Assert.That(BeatmapCarouselFilterGrouping.ShouldGroupBeatmapsTogether(new FilterCriteria { Sort = SortMode.Difficulty }), Is.False);

        // CF: BeatmapCarouselFilterGrouping.ShouldGroupBeatmapsTogether | criteria.Sort == SortMode.Difficulty | FALSE
        // CF: BeatmapCarouselFilterGrouping.ShouldGroupBeatmapsTogether | criteria.Group == GroupMode.Difficulty | TRUE
        [Test]
        public void TestShouldGroupGroupByDifficulty() => Assert.That(BeatmapCarouselFilterGrouping.ShouldGroupBeatmapsTogether(new FilterCriteria { Group = GroupMode.Difficulty }), Is.False);

        // CF: BeatmapCarouselFilterGrouping.ShouldGroupBeatmapsTogether | criteria.Group == GroupMode.Difficulty | FALSE
        // CF: BeatmapCarouselFilterGrouping.ShouldGroupBeatmapsTogether | criteria.Sort == SortMode.LastPlayed && criteria.Group == GroupMode.LastPlayed | TRUE
        [Test]
        public void TestShouldGroupLastPlayedBoth() => Assert.That(BeatmapCarouselFilterGrouping.ShouldGroupBeatmapsTogether(new FilterCriteria { Sort = SortMode.LastPlayed, Group = GroupMode.LastPlayed }), Is.False);

        // CF: BeatmapCarouselFilterGrouping.ShouldGroupBeatmapsTogether | criteria.Sort == SortMode.LastPlayed | FALSE
        // CF: BeatmapCarouselFilterGrouping.ShouldGroupBeatmapsTogether | criteria.Sort == SortMode.LastPlayed (TRUE) && criteria.Group == GroupMode.LastPlayed | FALSE
        // CF: BeatmapCarouselFilterGrouping.ShouldGroupBeatmapsTogether | criteria.Group == GroupMode.RankAchieved | TRUE
        [Test]
        public void TestShouldGroupRankAchieved()
        {
            Assert.That(BeatmapCarouselFilterGrouping.ShouldGroupBeatmapsTogether(new FilterCriteria { Sort = SortMode.LastPlayed, Group = GroupMode.None }), Is.True);
            Assert.That(BeatmapCarouselFilterGrouping.ShouldGroupBeatmapsTogether(new FilterCriteria { Group = GroupMode.RankAchieved }), Is.False);
        }

        // CF: BeatmapCarouselFilterGrouping.ShouldGroupBeatmapsTogether | criteria.Group == GroupMode.RankAchieved | FALSE
        [Test]
        public void TestShouldGroupDefault() => Assert.That(BeatmapCarouselFilterGrouping.ShouldGroupBeatmapsTogether(new FilterCriteria()), Is.True);

        // CF: BeatmapCarouselFilterGrouping.Run | foreach groups | loop 0
        // CF: BeatmapCarouselFilterGrouping.getGroups | criteria.Group | case None
        [Test]
        public async Task TestNoItems()
        {
            var (filter, result) = await run();
            Assert.That(result, Is.Empty);
            Assert.That(filter.BeatmapItemsCount, Is.EqualTo(0));
        }

        // CF: BeatmapCarouselFilterGrouping.Run | group != null | FALSE
        // CF: BeatmapCarouselFilterGrouping.Run | foreach itemsInGroup | loop n
        // CF: BeatmapCarouselFilterGrouping.Run | newBeatmapSet | TRUE
        // CF: BeatmapCarouselFilterGrouping.Run | newBeatmapSet | FALSE
        // CF: BeatmapCarouselFilterGrouping.Run | !newSetMap.TryGetValue(groupedBeatmapSet) | TRUE
        // CF: BeatmapCarouselFilterGrouping.Run | BeatmapSetsGroupedTogether | TRUE
        // CF: BeatmapCarouselFilterGrouping.Run | groupItem != null | FALSE
        [Test]
        public async Task TestNoGroupingSetsGroupedTogether()
        {
            var a = beatmap(difficulties: 2);
            var setB = beatmap();
            var (filter, result) = await run(a, a.BeatmapSet!.Beatmaps[1], setB);

            // set header + 2 diffs, set header + 1 diff
            Assert.That(result.Select(r => r.Model.GetType()), Is.EqualTo(new[]
            {
                typeof(GroupedBeatmapSet), typeof(GroupedBeatmap), typeof(GroupedBeatmap), typeof(GroupedBeatmapSet), typeof(GroupedBeatmap)
            }));
            Assert.That(filter.BeatmapSetsGroupedTogether, Is.True);
            Assert.That(filter.BeatmapItemsCount, Is.EqualTo(3));
            // without a group, set headers are visible and difficulties are not.
            Assert.That(result.Select(r => r.IsVisible), Is.EqualTo(new[] { true, false, false, true, false }));
            Assert.That(filter.SetItems, Has.Count.EqualTo(2));
            Assert.That(filter.GroupItems, Is.Empty);
            Assert.That(filter.ItemMap, Has.Count.EqualTo(5));
        }

        // CF: BeatmapCarouselFilterGrouping.Run | BeatmapSetsGroupedTogether | FALSE
        // CF: BeatmapCarouselFilterGrouping.Run | i.Model is GroupedBeatmapSet || !BeatmapSetsGroupedTogether | TRUE
        [Test]
        public async Task TestStandaloneBeatmaps()
        {
            criteria.Sort = SortMode.Difficulty;
            var (filter, result) = await run(beatmap(), beatmap());

            Assert.That(filter.BeatmapSetsGroupedTogether, Is.False);
            Assert.That(result.Select(r => r.Model), Has.All.TypeOf<GroupedBeatmap>());
            Assert.That(result.Select(r => r.IsVisible), Has.All.True);
        }

        // CF: BeatmapCarouselFilterGrouping.Run | group != null | TRUE
        // CF: BeatmapCarouselFilterGrouping.Run | groupItem != null (grouped together) | TRUE
        // CF: BeatmapCarouselFilterGrouping.Run | groupItem != null (standalone) | TRUE
        // CF: BeatmapCarouselFilterGrouping.Run | i.Model is GroupDefinition | TRUE
        [Test]
        public async Task TestGroupHeadersAndNestedCounts()
        {
            criteria.Group = GroupMode.Source;
            var (filter, result) = await run(beatmap(b => b.Metadata.Source = "X"), beatmap(b => b.Metadata.Source = "X"));

            var header = result[0];
            Assert.That(header.Model, Is.TypeOf<GroupDefinition>());
            Assert.That(header.NestedItemCount, Is.EqualTo(2));
            Assert.That(header.IsVisible, Is.True);
            Assert.That(filter.GroupItems, Has.Count.EqualTo(1));
            Assert.That(result.Skip(1).Select(r => r.IsVisible), Has.All.False);

            criteria.Group = GroupMode.Difficulty;
            (_, result) = await run(beatmap(), beatmap());
            Assert.That(result[0].NestedItemCount, Is.EqualTo(2));
        }

        // CF: BeatmapCarouselFilterGrouping.Run | cancellationToken | throws OperationCanceledException
        [Test]
        public void TestCancelled()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var filter = new BeatmapCarouselFilterGrouping
            {
                GetCriteria = () => criteria,
                GetCollections = () => collections,
                GetLocalUserTopRanks = _ => ranks,
                GetFavouriteBeatmapSets = () => favourites,
            };

            Assert.ThrowsAsync<TaskCanceledException>(async () => await filter.Run(new List<CarouselItem> { new CarouselItem(beatmap()) }, cts.Token));
        }

        // CF: BeatmapCarouselFilterGrouping.getGroups | criteria.Group | case Artist
        // CF: BeatmapCarouselFilterGrouping.defineGroupAlphabetically | char.IsAsciiDigit(firstChar) | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupAlphabetically | char.IsAsciiLetter(firstChar) | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupAlphabetically | char.IsAsciiLetter(firstChar) | FALSE
        [Test]
        public async Task TestGroupByArtist()
        {
            criteria.Group = GroupMode.Artist;
            var (filter, _) = await run(beatmap(b => b.Metadata.Artist = "1st"), beatmap(b => b.Metadata.Artist = "bee"), beatmap(b => b.Metadata.Artist = "Bat"), beatmap(b => b.Metadata.Artist = "#hash"));
            Assert.That(orders(filter), Is.EqualTo(new[] { int.MinValue, 1, int.MaxValue }));
        }

        // CF: BeatmapCarouselFilterGrouping.getGroups | criteria.Group | case Author
        [Test]
        public async Task TestGroupByAuthor()
        {
            criteria.Group = GroupMode.Author;
            var (filter, _) = await run(beatmap(b => b.Metadata.Author = new RealmUser { Username = "Zed" }));
            Assert.That(orders(filter), Is.EqualTo(new[] { 25 }));
        }

        // CF: BeatmapCarouselFilterGrouping.getGroups | criteria.Group | case Title
        // CF: BeatmapCarouselFilterGrouping.defineGroupAlphabetically | name.FirstOrDefault() on empty string | default
        [Test]
        public async Task TestGroupByTitleEmpty()
        {
            criteria.Group = GroupMode.Title;
            var (filter, _) = await run(beatmap(b => b.Metadata.Title = string.Empty), beatmap(b => b.Metadata.Title = "Aa"));
            Assert.That(orders(filter), Is.EqualTo(new[] { 0, int.MaxValue }));
        }

        // CF: BeatmapCarouselFilterGrouping.getGroups | criteria.Group | case DateAdded
        // CF: BeatmapCarouselFilterGrouping.defineGroupByDate | elapsed.TotalDays < 1 | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByDate | elapsed.TotalDays < 2 | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByDate | elapsed.TotalDays < 7 | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByDate | elapsed.TotalDays < 30 | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByDate | loop i = 60..150 | loop n
        // CF: BeatmapCarouselFilterGrouping.defineGroupByDate | elapsed.TotalDays < i | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByDate | elapsed.TotalDays < i | FALSE
        [Test]
        public async Task TestGroupByDateAdded()
        {
            criteria.Group = GroupMode.DateAdded;
            DateTimeOffset now = DateTimeOffset.Now;

            var (filter, _) = await run(
                beatmap(b => b.BeatmapSet!.DateAdded = now.AddHours(-1)),
                beatmap(b => b.BeatmapSet!.DateAdded = now.AddHours(-30)),
                beatmap(b => b.BeatmapSet!.DateAdded = now.AddDays(-4)),
                beatmap(b => b.BeatmapSet!.DateAdded = now.AddDays(-20)),
                beatmap(b => b.BeatmapSet!.DateAdded = now.AddDays(-40)),
                beatmap(b => b.BeatmapSet!.DateAdded = now.AddDays(-149)));
            Assert.That(orders(filter), Is.EqualTo(new[] { 0, 1, 2, 3, 60, 150 }));
        }

        // CF: BeatmapCarouselFilterGrouping.defineGroupByDate | loop i = 60..150 | loop 0
        [Test]
        public async Task TestGroupByDateAddedVeryOld()
        {
            criteria.Group = GroupMode.DateAdded;
            var (filter, _) = await run(beatmap(b => b.BeatmapSet!.DateAdded = DateTimeOffset.Now.AddDays(-1000)));
            Assert.That(orders(filter), Is.EqualTo(new[] { 151 }));
        }

        // CF: BeatmapCarouselFilterGrouping.getGroups | criteria.Group | case DateRanked
        // CF: BeatmapCarouselFilterGrouping.defineGroupByRankedDate | date == null | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByRankedDate | date == null | FALSE
        [Test]
        public async Task TestGroupByDateRanked()
        {
            criteria.Group = GroupMode.DateRanked;
            var (filter, _) = await run(beatmap(), beatmap(b => b.BeatmapSet!.DateRanked = new DateTimeOffset(2020, 5, 5, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(orders(filter), Is.EqualTo(new[] { -2020, 0 }));
        }

        // CF: BeatmapCarouselFilterGrouping.getGroups | criteria.Group | case LastPlayed
        // CF: BeatmapCarouselFilterGrouping.getGroups | date == null | TRUE
        // CF: BeatmapCarouselFilterGrouping.getGroups | date == null | FALSE
        [Test]
        public async Task TestGroupByLastPlayed()
        {
            criteria.Group = GroupMode.LastPlayed;
            var (filter, _) = await run(beatmap(), beatmap(b => b.LastPlayed = DateTimeOffset.Now.AddHours(-1)));
            Assert.That(orders(filter), Is.EqualTo(new[] { 0, int.MaxValue }));
        }

        // CF: BeatmapCarouselFilterGrouping.getGroups | criteria.Group | case RankedStatus
        // CF: BeatmapCarouselFilterGrouping.defineGroupByStatus | status | case Ranked
        // CF: BeatmapCarouselFilterGrouping.defineGroupByStatus | status | case Approved
        // CF: BeatmapCarouselFilterGrouping.defineGroupByStatus | status | case Qualified
        // CF: BeatmapCarouselFilterGrouping.defineGroupByStatus | status | case WIP
        // CF: BeatmapCarouselFilterGrouping.defineGroupByStatus | status | case Pending
        // CF: BeatmapCarouselFilterGrouping.defineGroupByStatus | status | case Graveyard
        // CF: BeatmapCarouselFilterGrouping.defineGroupByStatus | status | case LocallyModified
        // CF: BeatmapCarouselFilterGrouping.defineGroupByStatus | status | case None
        // CF: BeatmapCarouselFilterGrouping.defineGroupByStatus | status | case Loved
        [Test]
        public async Task TestGroupByStatus()
        {
            criteria.Group = GroupMode.RankedStatus;
            BeatmapInfo withStatus(BeatmapOnlineStatus s) => beatmap(b => b.BeatmapSet!.Status = s);

            var (filter, _) = await run(
                withStatus(BeatmapOnlineStatus.Ranked), withStatus(BeatmapOnlineStatus.Approved), withStatus(BeatmapOnlineStatus.Qualified),
                withStatus(BeatmapOnlineStatus.WIP), withStatus(BeatmapOnlineStatus.Pending), withStatus(BeatmapOnlineStatus.Graveyard),
                withStatus(BeatmapOnlineStatus.LocallyModified), withStatus(BeatmapOnlineStatus.None), withStatus(BeatmapOnlineStatus.Loved));

            Assert.That(orders(filter), Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
            // ranked + approved merge into one group.
            Assert.That(filter.GroupItems.Single(g => g.Key.Order == 0).Value.Where(i => i.Model is GroupedBeatmap), Has.Exactly(2).Items);
        }

        // CF: BeatmapCarouselFilterGrouping.defineGroupByStatus | status | default
        [Test]
        public void TestGroupByStatusUnknown()
        {
            criteria.Group = GroupMode.RankedStatus;
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await run(beatmap(b => b.BeatmapSet!.Status = (BeatmapOnlineStatus)99)));
        }

        // CF: BeatmapCarouselFilterGrouping.getGroups | criteria.Group | case BPM
        // CF: BeatmapCarouselFilterGrouping.defineGroupByBPM | bpm < 60 | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByBPM | loop i = 70..300 | loop n
        // CF: BeatmapCarouselFilterGrouping.defineGroupByBPM | bpm < i | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByBPM | bpm < i | FALSE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByBPM | loop i = 70..300 | loop 0
        [Test]
        public async Task TestGroupByBpm()
        {
            criteria.Group = GroupMode.BPM;
            var (filter, _) = await run(beatmap(b => b.BPM = 30), beatmap(b => b.BPM = 65), beatmap(b => b.BPM = 180), beatmap(b => b.BPM = 400));
            Assert.That(orders(filter), Is.EqualTo(new[] { 60, 70, 190, 301 }));
        }

        // CF: BeatmapCarouselFilterGrouping.getGroups | criteria.Group | case Difficulty
        // CF: BeatmapCarouselFilterGrouping.defineGroupByStars | starInt == 0 | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByStars | starInt == 0 | FALSE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByStars | starInt < 15 | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByStars | starInt < 15 | FALSE
        [Test]
        public async Task TestGroupByStars()
        {
            criteria.Group = GroupMode.Difficulty;
            var (filter, _) = await run(beatmap(b => b.StarRating = 0.5), beatmap(b => b.StarRating = 5.9), beatmap(b => b.StarRating = 20));
            Assert.That(orders(filter), Is.EqualTo(new[] { 0, 5, 15 }));
        }

        // CF: BeatmapCarouselFilterGrouping.getGroups | criteria.Group | case Length
        // CF: BeatmapCarouselFilterGrouping.defineGroupByLength | loop i = 1..5 | loop n
        // CF: BeatmapCarouselFilterGrouping.defineGroupByLength | length <= i * 60000 | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByLength | length <= i * 60000 | FALSE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByLength | length <= 10 * 60000 | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByLength | length <= 10 * 60000 | FALSE
        [Test]
        public async Task TestGroupByLength()
        {
            criteria.Group = GroupMode.Length;
            var (filter, _) = await run(beatmap(b => b.Length = 30_000), beatmap(b => b.Length = 300_000), beatmap(b => b.Length = 400_000), beatmap(b => b.Length = 700_000));
            Assert.That(orders(filter), Is.EqualTo(new[] { 1, 5, 10, 11 }));
        }

        // CF: BeatmapCarouselFilterGrouping.getGroups | criteria.Group | case Source
        // CF: BeatmapCarouselFilterGrouping.defineGroupBySource | string.IsNullOrEmpty(source) | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupBySource | string.IsNullOrEmpty(source) | FALSE
        [Test]
        public async Task TestGroupBySource()
        {
            criteria.Group = GroupMode.Source;
            var (filter, _) = await run(beatmap(), beatmap(b => b.Metadata.Source = "Game"));
            Assert.That(orders(filter), Is.EqualTo(new[] { 0, 1 }));
        }

        // CF: BeatmapCarouselFilterGrouping.getGroups | criteria.Group | case Collections
        // CF: BeatmapCarouselFilterGrouping.defineGroupsByCollection | loop over allCollections | loop n
        // CF: BeatmapCarouselFilterGrouping.defineGroupsByCollection | loop over collection.BeatmapMD5Hashes | loop n
        // CF: BeatmapCarouselFilterGrouping.defineGroupsByCollection | !md5ToCollectionsMap.TryGetValue(md5) | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupsByCollection | !md5ToCollectionsMap.TryGetValue(md5) | FALSE
        // CF: BeatmapCarouselFilterGrouping.defineGroupsByCollection | string.IsNullOrEmpty(beatmap.MD5Hash) | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupsByCollection | string.IsNullOrEmpty(beatmap.MD5Hash) | FALSE
        // CF: BeatmapCarouselFilterGrouping.defineGroupsByCollection | md5ToCollectionsMap.TryGetValue(beatmap.MD5Hash) | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupsByCollection | md5ToCollectionsMap.TryGetValue(beatmap.MD5Hash) | FALSE
        // CF: BeatmapCarouselFilterGrouping.defineGroupsByCollection | mapping.ItemsInGroup.Count > 0 | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupsByCollection | mapping.ItemsInGroup.Count > 0 | FALSE
        [Test]
        public async Task TestGroupByCollections()
        {
            criteria.Group = GroupMode.Collections;
            var inFirst = beatmap();
            var inBoth = beatmap();
            var inNone = beatmap();
            var noHash = beatmap(b => b.MD5Hash = string.Empty);

            collections.Add(new BeatmapCollection("first", new List<string> { inFirst.MD5Hash, inBoth.MD5Hash }));
            collections.Add(new BeatmapCollection("second", new List<string> { inBoth.MD5Hash }));
            collections.Add(new BeatmapCollection("empty", new List<string> { "not-local" }));

            var (filter, _) = await run(inFirst, inBoth, inNone, noHash);

            Assert.That(orders(filter), Is.EqualTo(new[] { 0, 1, int.MaxValue }));
            Assert.That(filter.GroupItems.Single(g => g.Key.Order == int.MaxValue).Value.Where(i => i.Model is GroupedBeatmap), Has.Exactly(1).Items);
        }

        // CF: BeatmapCarouselFilterGrouping.getGroups | criteria.Group | case MyMaps
        // CF: BeatmapCarouselFilterGrouping.defineGroupByOwnMaps | author.OnlineID == localUserId | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByOwnMaps | author.OnlineID == localUserId | FALSE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByOwnMaps | author.OnlineID <= 1 && author.Username == localUserUsername | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByOwnMaps | author.OnlineID <= 1 | FALSE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByOwnMaps | author.Username == localUserUsername | FALSE
        [Test]
        public async Task TestGroupByMyMaps()
        {
            criteria.Group = GroupMode.MyMaps;
            criteria.LocalUserId = 10;
            criteria.LocalUserUsername = "me";

            var byId = beatmap(b => b.Metadata.Author = new RealmUser { OnlineID = 10, Username = "renamed" });
            var byName = beatmap(b => b.Metadata.Author = new RealmUser { OnlineID = 1, Username = "me" });
            var otherUser = beatmap(b => b.Metadata.Author = new RealmUser { OnlineID = 20, Username = "me" });
            var otherName = beatmap(b => b.Metadata.Author = new RealmUser { OnlineID = 1, Username = "someone" });

            var (filter, result) = await run(byId, byName, otherUser, otherName);

            Assert.That(filter.GroupItems.Single().Value.Count(i => i.Model is GroupedBeatmap), Is.EqualTo(2));
            Assert.That(result.Count(r => r.Model is GroupedBeatmap), Is.EqualTo(2));
        }

        // CF: BeatmapCarouselFilterGrouping.getGroups | criteria.Group | case RankAchieved
        // CF: BeatmapCarouselFilterGrouping.defineGroupByRankAchieved | topRankMapping.TryGetValue(beatmap.ID) | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByRankAchieved | topRankMapping.TryGetValue(beatmap.ID) | FALSE
        [Test]
        public async Task TestGroupByRankAchieved()
        {
            criteria.Group = GroupMode.RankAchieved;
            var played = beatmap();
            var unplayed = beatmap();
            ranks[played.ID] = ScoreRank.S;

            var (filter, _) = await run(played, unplayed);

            Assert.That(orders(filter), Is.EqualTo(new[] { -(int)ScoreRank.S, int.MaxValue }.OrderBy(o => o)));
        }

        // CF: BeatmapCarouselFilterGrouping.getGroups | criteria.Group | case Favourites
        // CF: BeatmapCarouselFilterGrouping.defineGroupByFavourites | beatmap.BeatmapSet?.OnlineID > 0 && favouriteBeatmapSets.Contains(...) | TRUE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByFavourites | beatmap.BeatmapSet?.OnlineID > 0 | FALSE
        // CF: BeatmapCarouselFilterGrouping.defineGroupByFavourites | favouriteBeatmapSets.Contains(...) | FALSE
        [Test]
        public async Task TestGroupByFavourites()
        {
            criteria.Group = GroupMode.Favourites;
            favourites.Add(5);

            var fav = beatmap(b => b.BeatmapSet!.OnlineID = 5);
            var notFav = beatmap(b => b.BeatmapSet!.OnlineID = 6);
            var offline = beatmap();

            var (filter, result) = await run(fav, notFav, offline);

            Assert.That(filter.GroupItems, Has.Count.EqualTo(1));
            Assert.That(result.Count(r => r.Model is GroupedBeatmap), Is.EqualTo(1));
        }

        // CF: BeatmapCarouselFilterGrouping.getGroups | criteria.Group | case Variant
        // CF: BeatmapCarouselFilterGrouping.getGroups | rulesetInstance == null | TRUE
        [Test]
        public async Task TestGroupByVariantWithoutRulesetFallsBackToNone()
        {
            criteria.Group = GroupMode.Variant;
            var (filter, _) = await run(beatmap());
            Assert.That(filter.GroupItems, Is.Empty);
        }

        // CF: BeatmapCarouselFilterGrouping.getGroups | criteria.Group | default
        [Test]
        public void TestUnknownGroupMode()
        {
            criteria.Group = (GroupMode)99;
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await run(beatmap()));
        }
    }
}
