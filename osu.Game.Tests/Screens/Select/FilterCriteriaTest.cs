// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Screens.Select;
using osu.Game.Screens.Select.Filter;

namespace osu.Game.Tests.Screens.Select
{
    [TestFixture]
    public class FilterCriteriaTest
    {
        private static FilterCriteria.OptionalTextFilter text(string term, bool exclude = false) => new FilterCriteria.OptionalTextFilter { SearchTerm = term, ExcludeTerm = exclude };

        // CF: FilterCriteria.SearchText | foreach difficulty-name segment | loop 0
        // CF: FilterCriteria.SearchText | foreach quoted segment | loop 0
        // CF: FilterCriteria.SearchText | SearchTerms.Length == 1 && int.TryParse | FALSE
        [Test]
        public void TestSearchTextPlainTerms()
        {
            var c = new FilterCriteria { SearchText = "two words" };
            Assert.That(c.SearchTerms, Has.Length.EqualTo(2));
            Assert.That(c.SearchNumber, Is.Null);
            Assert.That(c.DifficultyName.HasFilter, Is.False);
        }

        // CF: FilterCriteria.SearchText | foreach difficulty-name segment | loop 1
        [Test]
        public void TestSearchTextDifficultyNameInBrackets()
        {
            var c = new FilterCriteria { SearchText = "song [Insane]" };
            Assert.That(c.DifficultyName.SearchTerm, Is.EqualTo("Insane"));
            Assert.That(c.SearchTerms, Has.Length.EqualTo(1));
            Assert.That(c.SearchTerms[0].SearchTerm, Is.EqualTo("song"));
        }

        // CF: FilterCriteria.SearchText | foreach quoted segment | loop n
        [Test]
        public void TestSearchTextQuotedSegments()
        {
            var c = new FilterCriteria { SearchText = "\"a b\" \"c d\"! rest" };
            Assert.That(c.SearchTerms, Has.Length.EqualTo(3));
            Assert.That(c.SearchTerms[0].MatchMode, Is.EqualTo(FilterCriteria.MatchMode.IsolatedPhrase));
            Assert.That(c.SearchTerms[1].MatchMode, Is.EqualTo(FilterCriteria.MatchMode.FullPhrase));
        }

        // CF: FilterCriteria.SearchText | SearchTerms.Length == 1 && int.TryParse | TRUE
        [Test]
        public void TestSearchNumber()
        {
            Assert.That(new FilterCriteria { SearchText = "12345" }.SearchNumber, Is.EqualTo(12345));
            Assert.That(new FilterCriteria { SearchText = "12345 more" }.SearchNumber, Is.Null);
            Assert.That(new FilterCriteria { SearchText = "abc" }.SearchNumber, Is.Null);
        }

        // CF: FilterCriteria.OptionalRange.IsInRange | Min != null | FALSE
        // CF: FilterCriteria.OptionalRange.IsInRange | Max != null | FALSE
        // CF: FilterCriteria.OptionalRange.IsInRange | InvertRange | FALSE
        [Test]
        public void TestRangeUnbounded()
        {
            var r = new FilterCriteria.OptionalRange<int>();
            Assert.That(r.HasFilter, Is.False);
            Assert.That(r.IsInRange(5), Is.True);
        }

        // CF: FilterCriteria.OptionalRange.IsInRange | Min != null | TRUE
        // CF: FilterCriteria.OptionalRange.IsInRange | comparison > 0 (lower) | TRUE
        // CF: FilterCriteria.OptionalRange.IsInRange | comparison > 0 (lower) | FALSE
        // CF: FilterCriteria.OptionalRange.IsInRange | comparison == 0 && IsLowerInclusive | TRUE
        // CF: FilterCriteria.OptionalRange.IsInRange | comparison == 0 && IsLowerInclusive | FALSE
        // CF: FilterCriteria.OptionalRange.IsInRange | comparison == 0 (lower) | FALSE
        [Test]
        public void TestRangeLowerBound()
        {
            Assert.That(new FilterCriteria.OptionalRange<int> { Min = 5 }.IsInRange(6), Is.True);
            Assert.That(new FilterCriteria.OptionalRange<int> { Min = 5 }.IsInRange(4), Is.False);
            Assert.That(new FilterCriteria.OptionalRange<int> { Min = 5, IsLowerInclusive = true }.IsInRange(5), Is.True);
            Assert.That(new FilterCriteria.OptionalRange<int> { Min = 5 }.IsInRange(5), Is.False);
        }

        // CF: FilterCriteria.OptionalRange.IsInRange | Max != null | TRUE
        // CF: FilterCriteria.OptionalRange.IsInRange | comparison < 0 (upper) | TRUE
        // CF: FilterCriteria.OptionalRange.IsInRange | comparison < 0 (upper) | FALSE
        // CF: FilterCriteria.OptionalRange.IsInRange | comparison == 0 && IsUpperInclusive | TRUE
        // CF: FilterCriteria.OptionalRange.IsInRange | comparison == 0 && IsUpperInclusive | FALSE
        // CF: FilterCriteria.OptionalRange.IsInRange | comparison == 0 (upper) | FALSE
        [Test]
        public void TestRangeUpperBound()
        {
            Assert.That(new FilterCriteria.OptionalRange<int> { Max = 5 }.IsInRange(4), Is.True);
            Assert.That(new FilterCriteria.OptionalRange<int> { Max = 5 }.IsInRange(6), Is.False);
            Assert.That(new FilterCriteria.OptionalRange<int> { Max = 5, IsUpperInclusive = true }.IsInRange(5), Is.True);
            Assert.That(new FilterCriteria.OptionalRange<int> { Max = 5 }.IsInRange(5), Is.False);
        }

        // CF: FilterCriteria.OptionalRange.IsInRange | InvertRange | TRUE
        [Test]
        public void TestRangeInverted()
        {
            var r = new FilterCriteria.OptionalRange<int> { Min = 1, Max = 5, InvertRange = true };
            Assert.That(r.IsInRange(3), Is.False);
            Assert.That(r.IsInRange(10), Is.True);
        }

        [Test]
        public void TestRangeEquals()
        {
            var a = new FilterCriteria.OptionalRange<int> { Min = 1, Max = 2 };
            Assert.That(a.Equals(new FilterCriteria.OptionalRange<int> { Min = 1, Max = 2 }), Is.True);
            Assert.That(a.Equals(new FilterCriteria.OptionalRange<int> { Min = 1, Max = 3 }), Is.False);
        }

        // CF: FilterCriteria.OptionalTextFilter.Matches | !HasFilter | TRUE
        [Test]
        public void TestTextNoFilterMatchesAll()
        {
            Assert.That(new FilterCriteria.OptionalTextFilter().Matches("anything"), Is.True);
        }

        // CF: FilterCriteria.OptionalTextFilter.Matches | !HasFilter | FALSE
        // CF: FilterCriteria.OptionalTextFilter.Matches | string.IsNullOrEmpty(value) | TRUE
        // CF: FilterCriteria.OptionalTextFilter.Matches | string.IsNullOrEmpty(value) | FALSE
        [Test]
        public void TestTextEmptyValue()
        {
            Assert.That(text("a").Matches(""), Is.False);
            Assert.That(text("a", true).Matches(""), Is.True);
            Assert.That(text("a").Matches(null!), Is.False);
        }

        // CF: FilterCriteria.OptionalTextFilter.Matches | MatchMode | case Substring
        // CF: FilterCriteria.OptionalTextFilter.Matches | MatchMode | default
        // CF: FilterCriteria.OptionalTextFilter.Matches | ExcludeTerm | FALSE
        // CF: FilterCriteria.OptionalTextFilter.Matches | ExcludeTerm | TRUE
        [Test]
        public void TestTextSubstring()
        {
            Assert.That(text("bc").Matches("ABCD"), Is.True);
            Assert.That(text("xy").Matches("ABCD"), Is.False);
            Assert.That(text("bc", true).Matches("ABCD"), Is.False);
            Assert.That(text("xy", true).Matches("ABCD"), Is.True);
        }

        // CF: FilterCriteria.OptionalTextFilter.Matches | MatchMode | case IsolatedPhrase
        [Test]
        public void TestTextIsolatedPhrase()
        {
            Assert.That(text("\"big cat\"").Matches("a Big Cat sat"), Is.True);
            Assert.That(text("\"cat\"").Matches("category"), Is.False);
        }

        // CF: FilterCriteria.OptionalTextFilter.Matches | MatchMode | case FullPhrase
        [Test]
        public void TestTextFullPhrase()
        {
            Assert.That(text("\"big cat\"!").Matches("BIG CAT"), Is.True);
            Assert.That(text("\"big cat\"!").Matches("a big cat"), Is.False);
        }

        // CF: FilterCriteria.OptionalTextFilter.SearchTerm | searchTerm.StartsWith('"') | FALSE
        // CF: FilterCriteria.OptionalTextFilter.SearchTerm | searchTerm.EndsWith("\"!") && Length >= 3 | TRUE
        // CF: FilterCriteria.OptionalTextFilter.SearchTerm | searchTerm.EndsWith("\"!") && Length >= 3 | FALSE
        [Test]
        public void TestTextSearchTermParsing()
        {
            Assert.That(text("plain").MatchMode, Is.EqualTo(FilterCriteria.MatchMode.Substring));
            Assert.That(text("\"x\"!").MatchMode, Is.EqualTo(FilterCriteria.MatchMode.FullPhrase));
            Assert.That(text("\"x\"").MatchMode, Is.EqualTo(FilterCriteria.MatchMode.IsolatedPhrase));
            // too short to be both an opening quote and a closing `"!`
            Assert.That(text("\"!").MatchMode, Is.EqualTo(FilterCriteria.MatchMode.IsolatedPhrase));
            Assert.That(text("a").Equals(text("a")), Is.True);
        }

        [Test]
        public void TestSetInRange()
        {
            var set = new FilterCriteria.OptionalSet<BeatmapOnlineStatus>();
            Assert.That(set.IsInRange(BeatmapOnlineStatus.Ranked), Is.True);
            set.Values.Remove(BeatmapOnlineStatus.Ranked);
            Assert.That(set.IsInRange(BeatmapOnlineStatus.Ranked), Is.False);
            Assert.That(set.Equals(new FilterCriteria.OptionalSet<BeatmapOnlineStatus>()), Is.False);
        }

        // CF: FilterCriteria.RequiresSorting | Sort != newCriteria.Sort | TRUE
        [Test]
        public void TestRequiresSortingWhenModeChanges()
        {
            Assert.That(new FilterCriteria { Sort = SortMode.Title }.RequiresSorting(new FilterCriteria { Sort = SortMode.Artist }), Is.True);
        }

        // CF: FilterCriteria.RequiresSorting | Sort != newCriteria.Sort | FALSE
        // CF: FilterCriteria.RequiresSorting | Sort | case Artist
        // CF: FilterCriteria.RequiresSorting | Sort | case Author
        // CF: FilterCriteria.RequiresSorting | Sort | case DateSubmitted
        // CF: FilterCriteria.RequiresSorting | Sort | case DateAdded
        // CF: FilterCriteria.RequiresSorting | Sort | case DateRanked
        // CF: FilterCriteria.RequiresSorting | Sort | case Source
        // CF: FilterCriteria.RequiresSorting | Sort | case Title
        [TestCase(SortMode.Artist)]
        [TestCase(SortMode.Author)]
        [TestCase(SortMode.DateSubmitted)]
        [TestCase(SortMode.DateAdded)]
        [TestCase(SortMode.DateRanked)]
        [TestCase(SortMode.Source)]
        [TestCase(SortMode.Title)]
        public void TestStableSortsDoNotRequireSorting(SortMode mode)
        {
            Assert.That(new FilterCriteria { Sort = mode }.RequiresSorting(new FilterCriteria { Sort = mode }), Is.False);
        }

        // CF: FilterCriteria.RequiresSorting | Sort | case LastPlayed
        // CF: FilterCriteria.RequiresSorting | Sort | case BPM
        // CF: FilterCriteria.RequiresSorting | Sort | case Length
        // CF: FilterCriteria.RequiresSorting | Sort | case Difficulty
        [TestCase(SortMode.LastPlayed)]
        [TestCase(SortMode.BPM)]
        [TestCase(SortMode.Length)]
        [TestCase(SortMode.Difficulty)]
        public void TestAggregateSortsAlwaysRequireSorting(SortMode mode)
        {
            Assert.That(new FilterCriteria { Sort = mode }.RequiresSorting(new FilterCriteria { Sort = mode }), Is.True);
        }

        // CF: FilterCriteria.RequiresSorting | Sort | default
        [Test]
        public void TestUnknownSortModeThrows()
        {
            var c = new FilterCriteria { Sort = (SortMode)99 };
            Assert.Throws<ArgumentOutOfRangeException>(() => c.RequiresSorting(new FilterCriteria { Sort = (SortMode)99 }));
        }
    }
}
