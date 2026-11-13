// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Filter;
using osu.Game.Screens.Select;
using osu.Game.Screens.Select.Filter;

namespace osu.Game.Tests.Screens.Select
{
    [TestFixture]
    public class FilterQueryParserTest
    {
        private static FilterCriteria apply(string query)
        {
            var criteria = new FilterCriteria();
            FilterQueryParser.ApplyQueries(criteria, query);
            return criteria;
        }

        // CF: FilterQueryParser.ApplyQueries | query_syntax_regex.Matches(query) has no matches | loop 0
        [Test]
        public void TestApplyQueriesNoKeywords()
        {
            var criteria = apply("looking for a beatmap");
            Assert.That(criteria.SearchText, Is.EqualTo("looking for a beatmap"));
            Assert.That(criteria.SearchTerms, Has.Length.EqualTo(4));
        }

        // CF: FilterQueryParser.ApplyQueries | query_syntax_regex.Matches(query) has one match | loop 1
        // CF: FilterQueryParser.ApplyQueries | tryParseKeywordCriteria(...) | TRUE
        [Test]
        public void TestApplyQueriesKeywordRemovedFromSearchText()
        {
            var criteria = apply("star>5 foo");
            Assert.That(criteria.StarDifficulty.Min, Is.EqualTo(5));
            Assert.That(criteria.SearchTerms, Has.Length.EqualTo(1));
            Assert.That(criteria.SearchTerms[0].SearchTerm, Is.EqualTo("foo"));
        }

        // CF: FilterQueryParser.ApplyQueries | query_syntax_regex.Matches(query) has several matches | loop n
        // CF: FilterQueryParser.ApplyQueries | tryParseKeywordCriteria(...) | FALSE
        [Test]
        public void TestApplyQueriesMultipleKeywordsAndUnparsableKept()
        {
            var criteria = apply("ar=9 cs<4 star>notanumber");
            Assert.That(criteria.ApproachRate.HasFilter, Is.True);
            Assert.That(criteria.CircleSize.HasFilter, Is.True);
            Assert.That(criteria.StarDifficulty.HasFilter, Is.False);
            Assert.That(criteria.SearchText, Does.Contain("star>notanumber"));
        }

        // CF: FilterQueryParser.tryParseKeywordCriteria | key == "star" | case star
        // CF: FilterQueryParser.tryParseKeywordCriteria | key == "stars" | case stars
        // CF: FilterQueryParser.tryParseKeywordCriteria | key == "sr" | case sr
        [TestCase("star")]
        [TestCase("stars")]
        [TestCase("sr")]
        public void TestStarKeys(string key)
        {
            var criteria = apply($"{key}>5");
            Assert.That(criteria.StarDifficulty.Min, Is.EqualTo(5));
        }

        // CF: FilterQueryParser.tryParseKeywordCriteria | key == "ar" | case ar
        [Test]
        public void TestApproachRate()
        {
            var criteria = apply("ar=9");
            Assert.That(criteria.ApproachRate.Min, Is.EqualTo(8.95f).Within(1e-4f));
            Assert.That(criteria.ApproachRate.Max, Is.EqualTo(9.05f).Within(1e-4f));
        }

        // CF: FilterQueryParser.tryParseKeywordCriteria | key == "dr" | case dr
        // CF: FilterQueryParser.tryParseKeywordCriteria | key == "hp" | case hp
        [TestCase("dr")]
        [TestCase("hp")]
        public void TestDrainRateKeys(string key)
        {
            var criteria = apply($"{key}>=5");
            Assert.That(criteria.DrainRate.Min, Is.EqualTo(4.95f).Within(1e-4f));
        }

        // CF: FilterQueryParser.tryParseKeywordCriteria | key == "cs" | case cs
        [Test]
        public void TestCircleSize()
        {
            var criteria = apply("cs<=4");
            Assert.That(criteria.CircleSize.Max, Is.EqualTo(4.05f).Within(1e-4f));
        }

        // CF: FilterQueryParser.tryParseKeywordCriteria | key == "od" | case od
        [Test]
        public void TestOverallDifficulty()
        {
            var criteria = apply("od<8");
            Assert.That(criteria.OverallDifficulty.Max, Is.EqualTo(7.95f).Within(1e-4f));
        }

        // CF: FilterQueryParser.tryParseKeywordCriteria | key == "bpm" | case bpm
        [Test]
        public void TestBpm()
        {
            var criteria = apply("bpm=180");
            Assert.That(criteria.BPM.Min, Is.EqualTo(179.5));
            Assert.That(criteria.BPM.Max, Is.EqualTo(180.5));
        }

        // CF: FilterQueryParser.tryParseKeywordCriteria | key == "divisor" | case divisor
        [Test]
        public void TestDivisor()
        {
            var criteria = apply("divisor>=4");
            Assert.That(criteria.BeatDivisor.Min, Is.EqualTo(4));
            Assert.That(criteria.BeatDivisor.IsLowerInclusive, Is.True);
        }

        // CF: FilterQueryParser.tryParseKeywordCriteria | key == "creator" | case creator
        // CF: FilterQueryParser.tryParseKeywordCriteria | key == "author" | case author
        // CF: FilterQueryParser.tryParseKeywordCriteria | key == "mapper" | case mapper
        [TestCase("creator")]
        [TestCase("author")]
        [TestCase("mapper")]
        public void TestCreatorKeys(string key)
        {
            Assert.That(apply($"{key}=peppy").Creator.SearchTerm, Is.EqualTo("peppy"));
        }

        // CF: FilterQueryParser.tryParseKeywordCriteria | key == "artist" | case artist
        // CF: FilterQueryParser.tryParseKeywordCriteria | key == "title" | case title
        // CF: FilterQueryParser.tryParseKeywordCriteria | key == "diff" | case diff
        // CF: FilterQueryParser.tryParseKeywordCriteria | key == "source" | case source
        [Test]
        public void TestTextKeys()
        {
            var criteria = apply("artist=a title=b diff=c source=d");
            Assert.That(criteria.Artist.SearchTerm, Is.EqualTo("a"));
            Assert.That(criteria.Title.SearchTerm, Is.EqualTo("b"));
            Assert.That(criteria.DifficultyName.SearchTerm, Is.EqualTo("c"));
            Assert.That(criteria.Source.SearchTerm, Is.EqualTo("d"));
        }

        // CF: FilterQueryParser.tryParseKeywordCriteria | key == "tag" | case tag
        [Test]
        public void TestTag()
        {
            var criteria = apply("tag=anime tag!=meme");
            Assert.That(criteria.UserTags, Has.Count.EqualTo(2));
            Assert.That(criteria.UserTags[0].SearchTerm, Is.EqualTo("anime"));
            Assert.That(criteria.UserTags[1].ExcludeTerm, Is.True);
        }

        // CF: FilterQueryParser.tryParseKeywordCriteria | key is unknown and RulesetCriteria == null | default
        // CF: FilterQueryParser.tryParseKeywordCriteria | criteria.RulesetCriteria?.TryParseCustomKeywordCriteria(...) ?? false | null
        [Test]
        public void TestUnknownKeyWithoutRulesetCriteria()
        {
            Assert.That(apply("foo=bar").SearchText, Is.EqualTo("foo=bar"));
        }

        // CF: FilterQueryParser.tryParseKeywordCriteria | criteria.RulesetCriteria?.TryParseCustomKeywordCriteria(...) ?? false | non-null
        [Test]
        public void TestUnknownKeyDelegatedToRulesetCriteria()
        {
            var criteria = new FilterCriteria { RulesetCriteria = new CustomCriteria() };
            FilterQueryParser.ApplyQueries(criteria, "custom=1 other");
            Assert.That(criteria.SearchTerms, Has.Length.EqualTo(1));
            Assert.That(criteria.SearchTerms[0].SearchTerm, Is.EqualTo("other"));
        }

        // CF: FilterQueryParser.tryParseKeywordCriteria | key == "length" | case length
        [Test]
        public void TestLengthKeyword()
        {
            // 1 minute, units seconds-scale -> tolerance of 500ms
            var criteria = apply("length=1:00");
            Assert.That(criteria.Length.Min, Is.EqualTo(59500));
            Assert.That(criteria.Length.Max, Is.EqualTo(60500));
        }

        // CF: FilterQueryParser.tryUpdateLengthRange | tryMatchRegex(clock format) matches | non-null
        // CF: FilterQueryParser.tryUpdateLengthRange | match["hours"].Success | TRUE
        // CF: FilterQueryParser.tryUpdateLengthRange | match["minutes"].Success | TRUE
        // CF: FilterQueryParser.tryUpdateLengthRange | match["seconds"].Success | TRUE
        // CF: FilterQueryParser.tryUpdateLengthRange | loop over parts | loop n
        [Test]
        public void TestLengthClockFormatWithHours()
        {
            var criteria = apply("length=1:02:03");
            double total = (3600 + 120 + 3) * 1000;
            Assert.That(criteria.Length.Min, Is.EqualTo(total - 500));
            Assert.That(criteria.Length.Max, Is.EqualTo(total + 500));
        }

        // CF: FilterQueryParser.tryUpdateLengthRange | match["seconds"].Success | FALSE
        // CF: FilterQueryParser.tryUpdateLengthRange | match["minutes"].Success | FALSE
        // CF: FilterQueryParser.tryUpdateLengthRange | match["hours"].Success | FALSE
        // CF: FilterQueryParser.getLengthScale | value.EndsWith("h") | TRUE
        [Test]
        public void TestLengthHoursOnly()
        {
            var criteria = apply("length>1h");
            Assert.That(criteria.Length.Min, Is.EqualTo(3600000 + 1800000));
        }

        // CF: FilterQueryParser.getLengthScale | value.EndsWith("m") | TRUE
        // CF: FilterQueryParser.getLengthScale | value.EndsWith("h") | FALSE
        [Test]
        public void TestLengthMinutesOnly()
        {
            var criteria = apply("length>=3m");
            Assert.That(criteria.Length.Min, Is.EqualTo(180000 - 30000));
        }

        // CF: FilterQueryParser.getLengthScale | value.EndsWith("s") | TRUE
        // CF: FilterQueryParser.getLengthScale | value.EndsWith("m") | FALSE
        // CF: FilterQueryParser.tryUpdateLengthRange | loop over parts | loop 1
        [Test]
        public void TestLengthSecondsSuffix()
        {
            var criteria = apply("length<=90s");
            Assert.That(criteria.Length.Max, Is.EqualTo(90500));
        }

        // CF: FilterQueryParser.getLengthScale | value.EndsWith("ms") | FALSE
        [Test]
        public void TestLengthMillisecondsSuffixIsNotSupportedByRegex()
        {
            // "ms" cannot be produced by the length regex, so a value like "5ms" is rejected.
            Assert.That(apply("length>5ms").SearchText, Does.Contain("length>5ms"));
        }

        // CF: FilterQueryParser.getLengthScale | value.EndsWith("s") | FALSE
        // CF: FilterQueryParser.tryMatchRegex | matches.Success | TRUE
        // CF: FilterQueryParser.tryMatchRegex | matches.Success | FALSE
        // CF: FilterQueryParser.tryUpdateLengthRange | match == null | TRUE
        [Test]
        public void TestLengthPlainNumberIsSeconds()
        {
            // plain number matches 3rd regex: the part ends in "s" after the append, scale 1000.
            var criteria = apply("length=30");
            Assert.That(criteria.Length.Min, Is.EqualTo(29500));
            Assert.That(apply("length=abc").SearchText, Does.Contain("length=abc"));
        }

        // CF: FilterQueryParser.tryUpdateLengthRange | i != parts.Count - 1 && length >= 60 | TRUE
        [Test]
        public void TestLengthNonLastPartOverSixtyRejected()
        {
            Assert.That(apply("length=1h90m").SearchText, Does.Contain("length=1h90m"));
        }

        // CF: FilterQueryParser.tryUpdateLengthRange | i != 0 && partNoUnit.Contains('.') | TRUE
        [Test]
        public void TestLengthFractionalLargerUnitRejected()
        {
            // the smallest unit (seconds) is index 0; fraction in minutes is index 1 -> rejected
            Assert.That(apply("length=1.5m30s").SearchText, Does.Contain("length=1.5m30s"));
        }

        // CF: FilterQueryParser.tryUpdateLengthRange | i != 0 && partNoUnit.Contains('.') | FALSE
        [Test]
        public void TestLengthFractionalSecondsAccepted()
        {
            Assert.That(apply("length=1.5s").Length.HasFilter, Is.True);
        }

        // CF: FilterQueryParser.tryUpdateDateAgoRange | op == NotEqual | case NotEqual
        // CF: FilterQueryParser.tryUpdateDateAgoRange | op == Equal | case Equal
        [TestCase("lastplayed!=5d")]
        [TestCase("lastplayed=5d")]
        public void TestLastPlayedEqualityRejected(string query)
        {
            Assert.That(apply(query).LastPlayed.HasFilter, Is.False);
        }

        // CF: FilterQueryParser.tryUpdateDateAgoRange | op == Less | case Less
        // CF: FilterQueryParser.tryUpdateDateAgoRange | tryMatchRegex(units) matches | non-null
        // CF: FilterQueryParser.tryUpdateDateAgoRange | key loop over units | loop n
        // CF: FilterQueryParser.tryUpdateDateAgoRange | match.TryGetValue(key) && group.Success | TRUE
        // CF: FilterQueryParser.tryUpdateDateAgoRange | match.TryGetValue(key) && group.Success | FALSE
        // CF: FilterQueryParser.tryUpdateDateAgoRange | key == "seconds" | case seconds
        // CF: FilterQueryParser.tryUpdateDateAgoRange | key == "minutes" | case minutes
        // CF: FilterQueryParser.tryUpdateDateAgoRange | key == "hours" | case hours
        // CF: FilterQueryParser.tryUpdateDateAgoRange | key == "days" | case days
        // CF: FilterQueryParser.tryUpdateDateAgoRange | key == "months" | case months
        // CF: FilterQueryParser.tryUpdateDateAgoRange | key == "years" | case years
        // CF: FilterQueryParser.tryUpdateDateAgoRange | dateTimeOffset.HasValue | TRUE
        [Test]
        public void TestLastPlayedAllUnitsLessFlipsToGreater()
        {
            var before = DateTimeOffset.Now;
            var criteria = apply("lastplayed<1y1M1d1h1m1s");
            var after = DateTimeOffset.Now;

            // "<" is flipped to ">" so the user's "less than X ago" means "after (now - X)": Min is set.
            Assert.That(criteria.LastPlayed.Min, Is.Not.Null);
            Assert.That(criteria.LastPlayed.Max, Is.Null);
            Assert.That(criteria.LastPlayed.Min!.Value, Is.LessThan(after.AddYears(-1).AddMonths(-1)));
            Assert.That(criteria.LastPlayed.Min!.Value, Is.GreaterThan(before.AddYears(-2)));
        }

        // CF: FilterQueryParser.tryUpdateDateAgoRange | op == LessOrEqual | case LessOrEqual
        [Test]
        public void TestLastPlayedLessOrEqual()
        {
            var criteria = apply("lastplayed<=2d");
            Assert.That(criteria.LastPlayed.Min, Is.Not.Null);
            Assert.That(criteria.LastPlayed.IsLowerInclusive, Is.True);
        }

        // CF: FilterQueryParser.tryUpdateDateAgoRange | op == Greater | case Greater
        // CF: FilterQueryParser.tryUpdateDateAgoRange | tryMatchRegex(plain number) matches | non-null
        [Test]
        public void TestLastPlayedGreaterPlainNumberDays()
        {
            var criteria = apply("lastplayed>5");
            Assert.That(criteria.LastPlayed.Max, Is.Not.Null);
            Assert.That(criteria.LastPlayed.Min, Is.Null);
            Assert.That(criteria.LastPlayed.Max!.Value, Is.LessThan(DateTimeOffset.Now.AddDays(-4.9)));
        }

        // CF: FilterQueryParser.tryUpdateDateAgoRange | op == GreaterOrEqual | case GreaterOrEqual
        [Test]
        public void TestLastPlayedGreaterOrEqual()
        {
            var criteria = apply("lastplayed>=2h");
            Assert.That(criteria.LastPlayed.Max, Is.Not.Null);
            Assert.That(criteria.LastPlayed.IsUpperInclusive, Is.True);
        }

        // CF: FilterQueryParser.tryUpdateDateAgoRange | match == null | TRUE
        [Test]
        public void TestLastPlayedInvalidValue()
        {
            Assert.That(apply("lastplayed>foo").LastPlayed.HasFilter, Is.False);
        }

        // CF: FilterQueryParser.tryUpdateDateAgoRange | dateTimeOffset.HasValue | FALSE
        [Test]
        public void TestLastPlayedEmptyValueHasNoDate()
        {
            // regex 1 matches the empty string, but no unit group succeeds.
            Assert.That(apply("lastplayed>").LastPlayed.HasFilter, Is.False);
        }

        // CF: FilterQueryParser.tryUpdateDateAgoRange | catch ArgumentOutOfRangeException | TRUE
        [Test]
        public void TestLastPlayedOverflowClampsToMinValue()
        {
            var criteria = apply("lastplayed<9999y");
            Assert.That(criteria.LastPlayed.Min, Is.EqualTo(DateTimeOffset.MinValue.AddMilliseconds(1)));
        }

        // CF: FilterQueryParser.tryParseKeywordCriteria | key == "played" | case played
        // CF: FilterQueryParser.tryParseKeywordCriteria | !tryParseBool(value, out played) | FALSE
        // CF: FilterQueryParser.tryParseKeywordCriteria | op == NotEqual | FALSE
        // CF: FilterQueryParser.tryParseKeywordCriteria | played | TRUE
        // CF: FilterQueryParser.tryParseBool | value == "1" | case 1
        // CF: FilterQueryParser.tryParseBool | value == "yes" | case yes
        [TestCase("played=1")]
        [TestCase("played=yes")]
        public void TestPlayedTrue(string query)
        {
            var range = apply(query).LastPlayed;
            Assert.That(range.Min, Is.EqualTo(DateTimeOffset.MinValue));
            Assert.That(range.Max, Is.EqualTo(DateTimeOffset.MaxValue));
            Assert.That(range.IsLowerInclusive, Is.False);
        }

        // CF: FilterQueryParser.tryParseKeywordCriteria | played | FALSE
        // CF: FilterQueryParser.tryParseBool | value == "0" | case 0
        // CF: FilterQueryParser.tryParseBool | value == "no" | case no
        [TestCase("played=0")]
        [TestCase("played=no")]
        public void TestPlayedFalse(string query)
        {
            var range = apply(query).LastPlayed;
            Assert.That(range.Max, Is.EqualTo(DateTimeOffset.MinValue));
            Assert.That(range.IsLowerInclusive, Is.True);
            Assert.That(range.IsUpperInclusive, Is.True);
        }

        // CF: FilterQueryParser.tryParseKeywordCriteria | op == NotEqual | TRUE
        [Test]
        public void TestPlayedNotEqualInverts()
        {
            var range = apply("played!=true").LastPlayed;
            Assert.That(range.Max, Is.EqualTo(DateTimeOffset.MinValue));
        }

        // CF: FilterQueryParser.tryParseKeywordCriteria | !tryParseBool(value, out played) | TRUE
        // CF: FilterQueryParser.tryParseBool | value is not a known token | default
        [Test]
        public void TestPlayedInvalidValue()
        {
            Assert.That(apply("played=maybe").LastPlayed.HasFilter, Is.False);
        }

        // CF: FilterQueryParser.tryParseBool | bool.TryParse(value) | TRUE
        [Test]
        public void TestPlayedBoolLiteral()
        {
            Assert.That(apply("played=false").LastPlayed.Max, Is.EqualTo(DateTimeOffset.MinValue));
        }

        // CF: FilterQueryParser.parseOperator | value == "=" | case =
        // CF: FilterQueryParser.parseOperator | value == ":" | case :
        // CF: FilterQueryParser.parseOperator | value == "!=" | case !=
        // CF: FilterQueryParser.parseOperator | value == "!:" | case !:
        // CF: FilterQueryParser.parseOperator | value == "<" | case <
        // CF: FilterQueryParser.parseOperator | value == "<=" | case <=
        // CF: FilterQueryParser.parseOperator | value == "<:" | case <:
        // CF: FilterQueryParser.parseOperator | value == ">" | case >
        // CF: FilterQueryParser.parseOperator | value == ">=" | case >=
        // CF: FilterQueryParser.parseOperator | value == ">:" | case >:
        [TestCase("=", false, "5", "5")]
        [TestCase(":", false, "5", "5")]
        [TestCase("!=", true, "5", "5")]
        [TestCase("!:", true, "5", "5")]
        [TestCase("<", false, null, "5")]
        [TestCase("<=", false, null, "5")]
        [TestCase("<:", false, null, "5")]
        [TestCase(">", false, "5", null)]
        [TestCase(">=", false, "5", null)]
        [TestCase(">:", false, "5", null)]
        public void TestOperatorsViaStarDifficulty(string op, bool inverted, string? min, string? max)
        {
            var range = apply("star" + op + "5").StarDifficulty;
            Assert.That(range.InvertRange, Is.EqualTo(inverted));
            Assert.That(range.Min?.ToString(), Is.EqualTo(min));
            Assert.That(range.Max?.ToString(), Is.EqualTo(max));
        }

        // CF: FilterQueryParser.tryUpdateCriteriaRange | tolerance == 0 | TRUE
        // CF: FilterQueryParser.tryUpdateCriteriaRange | tolerance == 0 | FALSE
        // CF: FilterQueryParser.tryUpdateCriteriaRange | op | default
        [Test]
        public void TestRangeEqualInclusivity()
        {
            // star difficulty uses tolerance 0 -> inclusive; ar uses 0.05 -> not set.
            Assert.That(apply("star=5").StarDifficulty.IsLowerInclusive, Is.True);
            Assert.That(apply("star=5").StarDifficulty.IsUpperInclusive, Is.True);
            Assert.That(apply("ar=5").ApproachRate.IsLowerInclusive, Is.False);

            var range = new FilterCriteria.OptionalRange<float>();
            Assert.That(FilterQueryParser.TryUpdateCriteriaRange(ref range, (Operator)99, "5"), Is.False);
        }

        // CF: FilterQueryParser.TryUpdateCriteriaRange | tryParseFloatWithPoint(val) | FALSE
        // CF: FilterQueryParser.TryUpdateCriteriaRange | tryParseFloatWithPoint(val) | TRUE
        [Test]
        public void TestFloatRangeParse()
        {
            var range = new FilterCriteria.OptionalRange<float>();
            Assert.That(FilterQueryParser.TryUpdateCriteriaRange(ref range, Operator.Equal, "x"), Is.False);
            Assert.That(FilterQueryParser.TryUpdateCriteriaRange(ref range, Operator.Equal, "1.5", 0.5f), Is.True);
            Assert.That(range.Min, Is.EqualTo(1f));
            Assert.That(range.Max, Is.EqualTo(2f));
        }

        // CF: FilterQueryParser.TryUpdateCriteriaRange | double: op == GreaterOrEqual && tolerance == 0 | TRUE
        // CF: FilterQueryParser.TryUpdateCriteriaRange | double: op == LessOrEqual && tolerance == 0 | TRUE
        // CF: FilterQueryParser.TryUpdateCriteriaRange | double: op == Greater | case Greater
        // CF: FilterQueryParser.TryUpdateCriteriaRange | double: op == Less | case Less
        // CF: FilterQueryParser.TryUpdateCriteriaRange | double: op | default
        [Test]
        public void TestDoubleRangeToleranceZeroInclusivity()
        {
            var range = new FilterCriteria.OptionalRange<double>();
            Assert.That(FilterQueryParser.TryUpdateCriteriaRange(ref range, Operator.GreaterOrEqual, "2", 0), Is.True);
            Assert.That(range.IsLowerInclusive, Is.True);

            range = new FilterCriteria.OptionalRange<double>();
            Assert.That(FilterQueryParser.TryUpdateCriteriaRange(ref range, Operator.LessOrEqual, "2", 0), Is.True);
            Assert.That(range.IsUpperInclusive, Is.True);

            range = new FilterCriteria.OptionalRange<double>();
            FilterQueryParser.TryUpdateCriteriaRange(ref range, Operator.Greater, "2", 0.5);
            Assert.That(range.Min, Is.EqualTo(2.5));
            FilterQueryParser.TryUpdateCriteriaRange(ref range, Operator.Less, "2", 0.5);
            Assert.That(range.Max, Is.EqualTo(1.5));

            Assert.That(FilterQueryParser.TryUpdateCriteriaRange(ref range, (Operator)99, "2"), Is.False);
            Assert.That(FilterQueryParser.TryUpdateCriteriaRange(ref range, Operator.Equal, "nan!"), Is.False);
        }

        // CF: FilterQueryParser.TryUpdateCriteriaRange | double: op == GreaterOrEqual && tolerance == 0 | FALSE
        // CF: FilterQueryParser.TryUpdateCriteriaRange | double: op == LessOrEqual && tolerance == 0 | FALSE
        [Test]
        public void TestDoubleRangeNonZeroToleranceNotInclusive()
        {
            var range = new FilterCriteria.OptionalRange<double>();
            FilterQueryParser.TryUpdateCriteriaRange(ref range, Operator.GreaterOrEqual, "2");
            FilterQueryParser.TryUpdateCriteriaRange(ref range, Operator.LessOrEqual, "2");
            Assert.That(range.IsLowerInclusive, Is.False);
            Assert.That(range.IsUpperInclusive, Is.False);
        }

        // CF: FilterQueryParser.TryUpdateCriteriaRange | generic: parseFunction.Invoke(...) | FALSE
        // CF: FilterQueryParser.tryParseInt | int.TryParse | FALSE
        [Test]
        public void TestGenericRangeParseFailure()
        {
            Assert.That(apply("divisor=-3").BeatDivisor.HasFilter, Is.False);
            Assert.That(apply("divisor=x").BeatDivisor.HasFilter, Is.False);
        }

        // CF: FilterQueryParser.tryUpdateCriteriaRange | generic: op == Equal | case Equal
        // CF: FilterQueryParser.tryUpdateCriteriaRange | generic: op == NotEqual | case NotEqual
        // CF: FilterQueryParser.tryUpdateCriteriaRange | generic: op == Greater | case Greater
        // CF: FilterQueryParser.tryUpdateCriteriaRange | generic: op == Less | case Less
        // CF: FilterQueryParser.tryUpdateCriteriaRange | generic: op == LessOrEqual | case LessOrEqual
        // CF: FilterQueryParser.tryUpdateCriteriaRange | generic: op | default
        [Test]
        public void TestGenericRangeOperators()
        {
            var r = apply("divisor=4").BeatDivisor;
            Assert.That((r.Min, r.Max, r.IsLowerInclusive, r.IsUpperInclusive), Is.EqualTo((4, 4, true, true)));

            Assert.That(apply("divisor!=4").BeatDivisor.InvertRange, Is.True);

            r = apply("divisor>4").BeatDivisor;
            Assert.That((r.Min, r.IsLowerInclusive), Is.EqualTo((4, false)));

            r = apply("divisor<4").BeatDivisor;
            Assert.That((r.Max, r.IsUpperInclusive), Is.EqualTo((4, false)));

            r = apply("divisor<=4").BeatDivisor;
            Assert.That((r.Max, r.IsUpperInclusive), Is.EqualTo((4, true)));

            var range = new FilterCriteria.OptionalRange<int>();
            Assert.That(FilterQueryParser.TryUpdateCriteriaRange(ref range, (Operator)99, "4", (string v, out int p) => int.TryParse(v, out p)), Is.False);
        }

        // CF: FilterQueryParser.TryUpdateCriteriaText | op == NotEqual | case NotEqual
        // CF: FilterQueryParser.TryUpdateCriteriaText | op == Equal | case Equal
        // CF: FilterQueryParser.TryUpdateCriteriaText | op is other | default
        [Test]
        public void TestTextOperators()
        {
            var filter = new FilterCriteria.OptionalTextFilter();
            Assert.That(FilterQueryParser.TryUpdateCriteriaText(ref filter, Operator.Equal, "a"), Is.True);
            Assert.That(filter.ExcludeTerm, Is.False);
            Assert.That(FilterQueryParser.TryUpdateCriteriaText(ref filter, Operator.NotEqual, "b"), Is.True);
            Assert.That(filter.ExcludeTerm, Is.True);
            Assert.That(FilterQueryParser.TryUpdateCriteriaText(ref filter, Operator.Greater, "c"), Is.False);
            Assert.That(filter.SearchTerm, Is.EqualTo("b"));
        }

        // CF: FilterQueryParser.TryUpdateCriteriaSet | filterValue.Contains(',') | TRUE
        // CF: FilterQueryParser.TryUpdateCriteriaSet | loop over splitValues | loop n
        // CF: FilterQueryParser.TryUpdateCriteriaSet | op == Equal | TRUE
        [Test]
        public void TestStatusListEqual()
        {
            var criteria = apply("status=ranked,loved");
            Assert.That(criteria.OnlineStatus.Values, Is.EquivalentTo(new[] { BeatmapOnlineStatus.Ranked, BeatmapOnlineStatus.Loved }));
        }

        // CF: FilterQueryParser.TryUpdateCriteriaSet | op == Equal | FALSE
        // CF: FilterQueryParser.TryUpdateCriteriaSet | op == NotEqual | TRUE
        [Test]
        public void TestStatusListNotEqual()
        {
            var criteria = apply("status!=ranked,loved");
            Assert.That(criteria.OnlineStatus.Values, Does.Not.Contain(BeatmapOnlineStatus.Ranked));
            Assert.That(criteria.OnlineStatus.Values, Does.Not.Contain(BeatmapOnlineStatus.Loved));
            Assert.That(criteria.OnlineStatus.Values, Does.Contain(BeatmapOnlineStatus.Graveyard));
        }

        // CF: FilterQueryParser.TryUpdateCriteriaSet | op == NotEqual | FALSE
        // CF: FilterQueryParser.TryUpdateCriteriaSet | list with unsupported op | no throw
        [Test]
        public void TestStatusListUnsupportedOperator()
        {
            var set = new FilterCriteria.OptionalSet<BeatmapOnlineStatus>();
            Assert.That(FilterQueryParser.TryUpdateCriteriaSet(ref set, Operator.Greater, "ranked,loved"), Is.False);
        }

        // CF: FilterQueryParser.TryUpdateCriteriaSet | !tryParseEnum(splitValue) | TRUE
        [Test]
        public void TestStatusListInvalidEntry()
        {
            Assert.That(apply("status=ranked,zzz").OnlineStatus.Values, Has.Count.EqualTo(Enum.GetValues<BeatmapOnlineStatus>().Length));
        }

        // CF: FilterQueryParser.TryUpdateCriteriaSet | !tryParseEnum(splitValue) | FALSE
        // CF: FilterQueryParser.tryParseEnum | Enum.TryParse exact | TRUE
        [Test]
        public void TestStatusSingleExact()
        {
            Assert.That(apply("status=loved").OnlineStatus.Values, Is.EquivalentTo(new[] { BeatmapOnlineStatus.Loved }));
        }

        // CF: FilterQueryParser.tryParseEnum | Enum.TryParse exact | FALSE
        // CF: FilterQueryParser.tryParseEnum | prefixMatch == null | FALSE
        [Test]
        public void TestStatusPrefixMatch()
        {
            Assert.That(apply("status=lov").OnlineStatus.Values, Is.EquivalentTo(new[] { BeatmapOnlineStatus.Loved }));
        }

        // CF: FilterQueryParser.tryParseEnum | prefixMatch == null | TRUE
        // CF: FilterQueryParser.TryUpdateCriteriaSet | !tryParseEnum(filterValue) | TRUE
        [Test]
        public void TestStatusSingleInvalid()
        {
            Assert.That(apply("status=zzz").SearchText, Does.Contain("status=zzz"));
        }

        // CF: FilterQueryParser.TryUpdateCriteriaSet | single: op == Less | case Less
        // CF: FilterQueryParser.TryUpdateCriteriaSet | single: compareResult < 0 | TRUE
        // CF: FilterQueryParser.TryUpdateCriteriaSet | single: compareResult < 0 | FALSE
        // CF: FilterQueryParser.TryUpdateCriteriaSet | single: loop over enum values | loop n
        [Test]
        public void TestStatusSingleLess()
        {
            var values = apply("status<ranked").OnlineStatus.Values;
            Assert.That(values, Does.Contain(BeatmapOnlineStatus.Pending));
            Assert.That(values, Does.Not.Contain(BeatmapOnlineStatus.Ranked));
        }

        // CF: FilterQueryParser.TryUpdateCriteriaSet | single: op == LessOrEqual | case LessOrEqual
        // CF: FilterQueryParser.TryUpdateCriteriaSet | single: compareResult <= 0 | TRUE
        // CF: FilterQueryParser.TryUpdateCriteriaSet | single: compareResult <= 0 | FALSE
        [Test]
        public void TestStatusSingleLessOrEqual()
        {
            var values = apply("status<=ranked").OnlineStatus.Values;
            Assert.That(values, Does.Contain(BeatmapOnlineStatus.Ranked));
            Assert.That(values, Does.Not.Contain(BeatmapOnlineStatus.Approved));
        }

        // CF: FilterQueryParser.TryUpdateCriteriaSet | single: op == Equal | case Equal
        // CF: FilterQueryParser.TryUpdateCriteriaSet | single: compareResult == 0 | TRUE
        // CF: FilterQueryParser.TryUpdateCriteriaSet | single: compareResult == 0 | FALSE
        [Test]
        public void TestStatusSingleEqual()
        {
            Assert.That(apply("status:ranked").OnlineStatus.Values, Is.EquivalentTo(new[] { BeatmapOnlineStatus.Ranked }));
        }

        // CF: FilterQueryParser.TryUpdateCriteriaSet | single: op == GreaterOrEqual | case GreaterOrEqual
        // CF: FilterQueryParser.TryUpdateCriteriaSet | single: compareResult >= 0 | TRUE
        // CF: FilterQueryParser.TryUpdateCriteriaSet | single: compareResult >= 0 | FALSE
        [Test]
        public void TestStatusSingleGreaterOrEqual()
        {
            var values = apply("status>=ranked").OnlineStatus.Values;
            Assert.That(values, Does.Contain(BeatmapOnlineStatus.Ranked));
            Assert.That(values, Does.Not.Contain(BeatmapOnlineStatus.Pending));
        }

        // CF: FilterQueryParser.TryUpdateCriteriaSet | single: op == Greater | case Greater
        // CF: FilterQueryParser.TryUpdateCriteriaSet | single: compareResult > 0 | TRUE
        // CF: FilterQueryParser.TryUpdateCriteriaSet | single: compareResult > 0 | FALSE
        [Test]
        public void TestStatusSingleGreater()
        {
            var values = apply("status>ranked").OnlineStatus.Values;
            Assert.That(values, Does.Not.Contain(BeatmapOnlineStatus.Ranked));
            Assert.That(values, Does.Contain(BeatmapOnlineStatus.Loved));
        }

        // CF: FilterQueryParser.TryUpdateCriteriaSet | single: op == NotEqual | case NotEqual
        // CF: FilterQueryParser.TryUpdateCriteriaSet | single: compareResult != 0 | TRUE
        // CF: FilterQueryParser.TryUpdateCriteriaSet | single: compareResult != 0 | FALSE
        [Test]
        public void TestStatusSingleNotEqual()
        {
            var values = apply("status!=ranked").OnlineStatus.Values;
            Assert.That(values, Does.Not.Contain(BeatmapOnlineStatus.Ranked));
            Assert.That(values, Does.Contain(BeatmapOnlineStatus.Loved));
        }

        // CF: FilterQueryParser.TryUpdateCriteriaSet | single: op | default
        [Test]
        public void TestStatusSingleInvalidOperator()
        {
            var set = new FilterCriteria.OptionalSet<BeatmapOnlineStatus>();
            Assert.That(FilterQueryParser.TryUpdateCriteriaSet(ref set, (Operator)99, "ranked"), Is.False);
        }

        // CF: FilterQueryParser.tryUpdateRankedDateRange | match == null | TRUE
        [Test]
        public void TestRankedDateInvalid()
        {
            Assert.That(apply("ranked=notadate").DateRanked.HasFilter, Is.False);
        }

        // CF: FilterQueryParser.tryUpdateRankedDateRange | match == null | FALSE
        // CF: FilterQueryParser.tryUpdateRankedDateRange | loop over keys | loop n
        // CF: FilterQueryParser.tryUpdateRankedDateRange | match.TryGetValue(key) && group.Success | TRUE
        // CF: FilterQueryParser.tryUpdateRankedDateRange | match.TryGetValue(key) && group.Success | FALSE
        // CF: FilterQueryParser.tryUpdateRankedDateRange | key == "year" | case year
        // CF: FilterQueryParser.tryUpdateRankedDateRange | key == "month" | case month
        // CF: FilterQueryParser.tryUpdateRankedDateRange | key == "day" | case day
        // CF: FilterQueryParser.tryUpdateRankedDateRange | op == Equal | case Equal
        // CF: FilterQueryParser.tryUpdateRankedDateRange | month == null (Equal) | FALSE
        // CF: FilterQueryParser.tryUpdateRankedDateRange | day == null (Equal) | FALSE
        [Test]
        public void TestRankedDateFullDateEqual()
        {
            var r = apply("ranked=2020-03-05").DateRanked;
            Assert.That(r.Min, Is.EqualTo(new DateTimeOffset(2020, 3, 5, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(r.Max, Is.EqualTo(new DateTimeOffset(2020, 3, 6, 0, 0, 0, TimeSpan.Zero)));
        }

        // CF: FilterQueryParser.tryUpdateRankedDateRange | month == null (Equal) | TRUE
        // CF: FilterQueryParser.tryUpdateRankedDateRange | key == "created" or "submitted" | case created
        [Test]
        public void TestSubmittedDateYearEqual()
        {
            var r = apply("created=2020").DateSubmitted;
            Assert.That(r.Min, Is.EqualTo(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(r.Max, Is.EqualTo(new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero)));

            // CF: FilterQueryParser.tryParseKeywordCriteria | key == "submitted" | case submitted
            Assert.That(apply("submitted=2020").DateSubmitted.HasFilter, Is.True);
        }

        // CF: FilterQueryParser.tryUpdateRankedDateRange | day == null (Equal) | TRUE
        [Test]
        public void TestRankedDateMonthEqual()
        {
            var r = apply("ranked=2020/03").DateRanked;
            Assert.That(r.Min, Is.EqualTo(new DateTimeOffset(2020, 3, 1, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(r.Max, Is.EqualTo(new DateTimeOffset(2020, 4, 1, 0, 0, 0, TimeSpan.Zero)));
        }

        // CF: FilterQueryParser.tryUpdateRankedDateRange | op == NotEqual | case NotEqual
        [Test]
        public void TestRankedDateNotEqual()
        {
            Assert.That(apply("ranked!=2020.03.05").DateRanked.InvertRange, Is.True);
        }

        // CF: FilterQueryParser.tryUpdateRankedDateRange | op == Less | case Less
        [Test]
        public void TestRankedDateLess()
        {
            Assert.That(apply("ranked<2020").DateRanked.Max, Is.EqualTo(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)));
        }

        // CF: FilterQueryParser.tryUpdateRankedDateRange | op == LessOrEqual | case LessOrEqual
        // CF: FilterQueryParser.tryUpdateRankedDateRange | month == null (LessOrEqual) | TRUE
        [Test]
        public void TestRankedDateLessOrEqualYear()
        {
            Assert.That(apply("ranked<=2020").DateRanked.Max, Is.EqualTo(new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero)));
        }

        // CF: FilterQueryParser.tryUpdateRankedDateRange | month == null (LessOrEqual) | FALSE
        // CF: FilterQueryParser.tryUpdateRankedDateRange | day == null (LessOrEqual) | TRUE
        [Test]
        public void TestRankedDateLessOrEqualMonth()
        {
            Assert.That(apply("ranked<=2020-12").DateRanked.Max, Is.EqualTo(new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero)));
        }

        // CF: FilterQueryParser.tryUpdateRankedDateRange | day == null (LessOrEqual) | FALSE
        [Test]
        public void TestRankedDateLessOrEqualDay()
        {
            Assert.That(apply("ranked<=2020-12-31").DateRanked.Max, Is.EqualTo(new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero)));
        }

        // CF: FilterQueryParser.tryUpdateRankedDateRange | op == GreaterOrEqual | case GreaterOrEqual
        [Test]
        public void TestRankedDateGreaterOrEqual()
        {
            Assert.That(apply("ranked>=2020-06").DateRanked.Min, Is.EqualTo(new DateTimeOffset(2020, 6, 1, 0, 0, 0, TimeSpan.Zero)));
        }

        // CF: FilterQueryParser.tryUpdateRankedDateRange | op == Greater | case Greater
        // CF: FilterQueryParser.tryUpdateRankedDateRange | month == null (Greater) | TRUE
        [Test]
        public void TestRankedDateGreaterYear()
        {
            Assert.That(apply("ranked>2020").DateRanked.Min, Is.EqualTo(new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero)));
        }

        // CF: FilterQueryParser.tryUpdateRankedDateRange | month == null (Greater) | FALSE
        // CF: FilterQueryParser.tryUpdateRankedDateRange | day == null (Greater) | TRUE
        [Test]
        public void TestRankedDateGreaterMonth()
        {
            Assert.That(apply("ranked>2020-02").DateRanked.Min, Is.EqualTo(new DateTimeOffset(2020, 3, 1, 0, 0, 0, TimeSpan.Zero)));
        }

        // CF: FilterQueryParser.tryUpdateRankedDateRange | day == null (Greater) | FALSE
        [Test]
        public void TestRankedDateGreaterDay()
        {
            Assert.That(apply("ranked>2020-02-28").DateRanked.Min, Is.EqualTo(new DateTimeOffset(2020, 2, 29, 0, 0, 0, TimeSpan.Zero)));
        }

        // CF: FilterQueryParser.tryUpdateRankedDateRange | catch ArgumentOutOfRangeException | TRUE
        // CF: FilterQueryParser.tryUpdateRankedDateRange | try body completes | no throw
        [Test]
        public void TestRankedDateOutOfRange()
        {
            Assert.That(apply("ranked=2020-13-45").DateRanked.HasFilter, Is.False);
        }

        private class CustomCriteria : IRulesetFilterCriteria
        {
            public bool Matches(BeatmapInfo beatmapInfo, FilterCriteria criteria) => true;

            public bool TryParseCustomKeywordCriteria(string key, Operator op, string value) => key == "custom";

            public bool FilterMayChangeFromMods(FilterCriteria criteria, ValueChangedEvent<IReadOnlyList<Mod>> mods) => false;
        }
    }
}
