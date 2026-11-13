// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Screens.Select;

namespace osu.Game.Tests.Beatmaps
{
    [TestFixture]
    public class BeatmapMetadataInfoExtensionsTest
    {
        private static BeatmapMetadata metadata(string artist = "", string title = "", string author = "", string artistU = "", string titleU = "", string source = "", string tags = "")
            => new BeatmapMetadata
            {
                Artist = artist,
                Title = title,
                ArtistUnicode = artistU,
                TitleUnicode = titleU,
                Source = source,
                Tags = tags,
                Author = { Username = author },
            };

        // CF: BeatmapMetadataInfoExtensions.CollectSearchableTerms | !string.IsNullOrEmpty(s) | FALSE
        [Test]
        public void TestNoSearchableTermsWhenEmpty()
        {
            Assert.That(metadata().GetSearchableTerms(), Is.Empty);
        }

        // CF: BeatmapMetadataInfoExtensions.CollectSearchableTerms | !string.IsNullOrEmpty(s) | TRUE
        [Test]
        public void TestSearchableTermsInDeclaredOrder()
        {
            var terms = metadata("ar", "ti", "au", "aru", "tiu", "so", "tag").GetSearchableTerms();

            Assert.That(terms, Is.EqualTo(new[] { "au", "ar", "aru", "ti", "tiu", "so", "tag" }));
            Assert.That(terms, Has.Length.EqualTo(BeatmapMetadataInfoExtensions.MAX_SEARCHABLE_TERM_COUNT));
        }

        // CF: BeatmapMetadataInfoExtensions.Match | filter.Matches(Author.Username) | TRUE
        [Test]
        public void TestMatchAuthor() => Assert.That(match(metadata(author: "needle"), "needle"), Is.True);

        // CF: BeatmapMetadataInfoExtensions.Match | filter.Matches(Artist) | TRUE
        [Test]
        public void TestMatchArtist() => Assert.That(match(metadata(artist: "needle"), "needle"), Is.True);

        // CF: BeatmapMetadataInfoExtensions.Match | filter.Matches(ArtistUnicode) | TRUE
        [Test]
        public void TestMatchArtistUnicode() => Assert.That(match(metadata(artistU: "needle"), "needle"), Is.True);

        // CF: BeatmapMetadataInfoExtensions.Match | filter.Matches(Title) | TRUE
        [Test]
        public void TestMatchTitle() => Assert.That(match(metadata(title: "needle"), "needle"), Is.True);

        // CF: BeatmapMetadataInfoExtensions.Match | filter.Matches(TitleUnicode) | TRUE
        [Test]
        public void TestMatchTitleUnicode() => Assert.That(match(metadata(titleU: "needle"), "needle"), Is.True);

        // CF: BeatmapMetadataInfoExtensions.Match | filter.Matches(Source) | TRUE
        [Test]
        public void TestMatchSource() => Assert.That(match(metadata(source: "needle"), "needle"), Is.True);

        // CF: BeatmapMetadataInfoExtensions.Match | filter.Matches(Tags) | TRUE
        [Test]
        public void TestMatchTags() => Assert.That(match(metadata(tags: "a needle b"), "needle"), Is.True);

        // CF: BeatmapMetadataInfoExtensions.Match | every field filter.Matches | FALSE
        [Test]
        public void TestNoMatch() => Assert.That(match(metadata("a", "b", "c", "d", "e", "f", "g"), "needle"), Is.False);

        private static bool match(BeatmapMetadata metadata, string term)
            => BeatmapMetadataInfoExtensions.Match(metadata, new FilterCriteria.OptionalTextFilter { SearchTerm = term });

        // CF: BeatmapMetadataInfoExtensions.GetDisplayTitle | string.IsNullOrEmpty(Author.Username) | TRUE
        // CF: BeatmapMetadataInfoExtensions.GetDisplayTitle | string.IsNullOrEmpty(Artist) | FALSE
        // CF: BeatmapMetadataInfoExtensions.GetDisplayTitle | string.IsNullOrEmpty(Title) | FALSE
        [Test]
        public void TestDisplayTitleWithoutAuthor() => Assert.That(metadata("A", "T").GetDisplayTitle(), Is.EqualTo("A - T"));

        // CF: BeatmapMetadataInfoExtensions.GetDisplayTitle | string.IsNullOrEmpty(Author.Username) | FALSE
        [Test]
        public void TestDisplayTitleWithAuthor() => Assert.That(metadata("A", "T", "me").GetDisplayTitle(), Is.EqualTo("A - T (me)"));

        // CF: BeatmapMetadataInfoExtensions.GetDisplayTitle | string.IsNullOrEmpty(Artist) | TRUE
        // CF: BeatmapMetadataInfoExtensions.GetDisplayTitle | string.IsNullOrEmpty(Title) | TRUE
        [Test]
        public void TestDisplayTitleFallbacks() => Assert.That(metadata().GetDisplayTitle(), Is.EqualTo("unknown artist - unknown title"));

        // CF: BeatmapMetadataInfoExtensions.GetDisplayTitleRomanisable | !includeCreator || string.IsNullOrEmpty(Author.Username) | TRUE
        // CF: BeatmapMetadataInfoExtensions.GetDisplayTitleRomanisable | string.IsNullOrEmpty(ArtistUnicode) | TRUE
        // CF: BeatmapMetadataInfoExtensions.GetDisplayTitleRomanisable | string.IsNullOrEmpty(TitleUnicode) | TRUE
        [Test]
        public void TestRomanisableFallsBackToRomanisedAndHidesCreator()
        {
            var title = metadata("A", "T", "me").GetDisplayTitleRomanisable(false);

            Assert.That(title.Romanised, Is.EqualTo("A - T"));
            Assert.That(title.Original, Is.EqualTo("A - T"));
        }

        // CF: BeatmapMetadataInfoExtensions.GetDisplayTitleRomanisable | !includeCreator || string.IsNullOrEmpty(Author.Username) | FALSE
        // CF: BeatmapMetadataInfoExtensions.GetDisplayTitleRomanisable | string.IsNullOrEmpty(ArtistUnicode) | FALSE
        // CF: BeatmapMetadataInfoExtensions.GetDisplayTitleRomanisable | string.IsNullOrEmpty(TitleUnicode) | FALSE
        [Test]
        public void TestRomanisableUsesUnicodeAndCreator()
        {
            var title = metadata("A", "T", "me", "AU", "TU").GetDisplayTitleRomanisable();

            Assert.That(title.Original, Is.EqualTo("AU - TU (me)"));
            Assert.That(title.Romanised, Is.EqualTo("A - T (me)"));
        }
    }
}
