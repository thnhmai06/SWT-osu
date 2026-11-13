// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Online.API.Requests;
using osu.Game.Overlays.BeatmapListing;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Scoring;
using osu.Game.Overlays;

namespace osu.Game.Tests.Online.API.Requests
{
    [TestFixture]
    public class SearchBeatmapSetsRequestTest
    {
        private static readonly RulesetInfo osu = new OsuRuleset().RulesetInfo;

        // CF: SearchBeatmapSetsRequest.CreateWebRequest | query != null | FALSE
        // CF: SearchBeatmapSetsRequest.CreateWebRequest | General != null && General.Any() | FALSE
        // CF: SearchBeatmapSetsRequest.CreateWebRequest | ruleset.OnlineID >= 0 | FALSE
        // CF: SearchBeatmapSetsRequest.CreateWebRequest | Genre != Any | FALSE
        // CF: SearchBeatmapSetsRequest.CreateWebRequest | Language != Any | FALSE
        // CF: SearchBeatmapSetsRequest.CreateWebRequest | Extra != null && Extra.Any() | FALSE
        // CF: SearchBeatmapSetsRequest.CreateWebRequest | Ranks != null && Ranks.Any() | FALSE
        // CF: SearchBeatmapSetsRequest.CreateWebRequest | Played != Any | FALSE
        // CF: SearchBeatmapSetsRequest.CreateWebRequest | cursor != null | FALSE
        [Test]
        public void TestDefaults()
        {
            var request = new SearchBeatmapSetsRequest(null!, new RulesetInfo { OnlineID = -1 });

            Assert.That(RequestInspector.GetUri(request), Is.EqualTo("http://api.test/api/v2/beatmapsets/search"));
            Assert.That(RequestInspector.GetParameters(request).ConvertAll(p => $"{p.Key}={p.Value}"), Is.EquivalentTo(new[] { "s=any", "sort=ranked_desc", "nsfw=false" }));
        }

        // CF: SearchBeatmapSetsRequest.CreateWebRequest | query != null | TRUE
        // CF: SearchBeatmapSetsRequest.CreateWebRequest | General != null && General.Any() | TRUE
        // CF: SearchBeatmapSetsRequest.CreateWebRequest | ruleset.OnlineID >= 0 | TRUE
        // CF: SearchBeatmapSetsRequest.CreateWebRequest | Genre != Any | TRUE
        // CF: SearchBeatmapSetsRequest.CreateWebRequest | Language != Any | TRUE
        // CF: SearchBeatmapSetsRequest.CreateWebRequest | Extra != null && Extra.Any() | TRUE
        // CF: SearchBeatmapSetsRequest.CreateWebRequest | Ranks != null && Ranks.Any() | TRUE
        // CF: SearchBeatmapSetsRequest.CreateWebRequest | Played != Any | TRUE
        // CF: SearchBeatmapSetsRequest.CreateWebRequest | cursor != null | TRUE
        // CF: SearchBeatmapSetsRequest.directionString | SortDirection == Descending | FALSE
        // CF: SearchBeatmapSetsRequest.CreateWebRequest | ExplicitContent == Show | TRUE
        [Test]
        public void TestAllParameters()
        {
            var cursor = new Cursor();
            cursor.Properties["k"] = new Newtonsoft.Json.Linq.JValue("v");

            var request = new SearchBeatmapSetsRequest("hello", osu, cursor,
                new[] { SearchGeneral.Recommended, SearchGeneral.FeaturedArtists },
                SearchCategory.Ranked, SortCriteria.Title, SortDirection.Ascending, SearchGenre.Unspecified, SearchLanguage.Unspecified,
                new[] { SearchExtra.Video, SearchExtra.Storyboard }, new[] { ScoreRank.A, ScoreRank.S }, SearchPlayed.Unplayed, SearchExplicit.Show);

            Assert.That(RequestInspector.Values(request, "q"), Is.EqualTo(new[] { "hello" }));
            Assert.That(RequestInspector.Values(request, "c"), Is.EqualTo(new[] { "recommended.featured_artists" }));
            Assert.That(RequestInspector.Values(request, "m"), Is.EqualTo(new[] { "0" }));
            Assert.That(RequestInspector.Values(request, "s"), Is.EqualTo(new[] { "ranked" }));
            Assert.That(RequestInspector.Values(request, "g"), Is.EqualTo(new[] { "1" }));
            Assert.That(RequestInspector.Values(request, "l"), Is.EqualTo(new[] { "1" }));
            Assert.That(RequestInspector.Values(request, "sort"), Is.EqualTo(new[] { "title_asc" }));
            Assert.That(RequestInspector.Values(request, "e"), Is.EqualTo(new[] { "video.storyboard" }));
            Assert.That(RequestInspector.Values(request, "r"), Is.EqualTo(new[] { "A.S" }));
            Assert.That(RequestInspector.Values(request, "played"), Is.EqualTo(new[] { "unplayed" }));
            Assert.That(RequestInspector.Values(request, "nsfw"), Is.EqualTo(new[] { "true" }));
            Assert.That(RequestInspector.Values(request, "cursor[k]"), Is.EqualTo(new[] { "v" }));
        }

        // CF: SearchBeatmapSetsRequest.directionString | SortDirection == Descending | TRUE
        // CF: SearchBeatmapSetsRequest.CreateWebRequest | General != null && General.Any() | FALSE
        [Test]
        public void TestEmptyCollectionsAreOmitted()
        {
            var request = new SearchBeatmapSetsRequest("", osu, general: new SearchGeneral[0], extra: new SearchExtra[0], ranks: new ScoreRank[0]);

            Assert.That(RequestInspector.Values(request, "sort"), Is.EqualTo(new[] { "ranked_desc" }));
            Assert.That(RequestInspector.Values(request, "c"), Is.Empty);
            Assert.That(RequestInspector.Values(request, "e"), Is.Empty);
            Assert.That(RequestInspector.Values(request, "r"), Is.Empty);
        }
    }
}
