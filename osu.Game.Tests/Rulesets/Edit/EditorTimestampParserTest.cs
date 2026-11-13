// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using NUnit.Framework;
using osu.Game.Rulesets.Edit;

namespace osu.Game.Tests.Rulesets.Edit
{
    [TestFixture]
    public class EditorTimestampParserTest
    {
        private CultureInfo previous = null!;

        [SetUp]
        public void SetUp()
        {
            previous = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        [TearDown]
        public void TearDown() => CultureInfo.CurrentCulture = previous;

        // CF: EditorTimestampParser.TryParse | double.TryParse(timestamp, out msec) | TRUE
        [Test]
        public void TestRawMilliseconds()
        {
            Assert.That(EditorTimestampParser.TryParse("12345", out var time, out string? selection), Is.True);
            Assert.That(time, Is.EqualTo(TimeSpan.FromMilliseconds(12345)));
            Assert.That(selection, Is.Null);
        }

        // CF: EditorTimestampParser.TryParse | double.TryParse(timestamp, out msec) | FALSE
        // CF: EditorTimestampParser.TryParse | match.Success | TRUE
        // CF: EditorTimestampParser.TryParse | timeMin >= 600 | FALSE
        // CF: EditorTimestampParser.TryParse | !string.IsNullOrEmpty(parsedSelection) | FALSE
        [TestCase("01:23:456", 1, 23, 456)]
        [TestCase("01:23.456", 1, 23, 456)]
        [TestCase("1:2:003", 1, 2, 3)]
        [TestCase("599:59:999", 599, 59, 999)]
        public void TestMinutesSecondsMilliseconds(string input, int minutes, int seconds, int milliseconds)
        {
            Assert.That(EditorTimestampParser.TryParse(input, out var time, out string? selection), Is.True);
            Assert.That(time, Is.EqualTo(new TimeSpan(0, 0, minutes, seconds, milliseconds)));
            Assert.That(selection, Is.Null);
        }

        [Test]
        public void TestMillisecondsOptional()
        {
            Assert.That(EditorTimestampParser.TryParse("2:30", out var time, out _), Is.True);
            Assert.That(time, Is.EqualTo(new TimeSpan(0, 0, 2, 30)));
        }

        // CF: EditorTimestampParser.TryParse | !string.IsNullOrEmpty(parsedSelection) | TRUE
        [Test]
        public void TestSelectionExtractedWithoutParentheses()
        {
            Assert.That(EditorTimestampParser.TryParse("01:00:000 (1,2,3)", out var time, out string? selection), Is.True);
            Assert.That(time, Is.EqualTo(TimeSpan.FromMinutes(1)));
            Assert.That(selection, Is.EqualTo("1,2,3"));
        }

        [Test]
        public void TestSuffixAllowedAfterDash()
        {
            Assert.That(EditorTimestampParser.TryParse("01:00:000 (4) - some note", out _, out string? selection), Is.True);
            Assert.That(selection, Is.EqualTo("4"));
        }

        // CF: EditorTimestampParser.TryParse | match.Success | FALSE
        [TestCase("not a time")]
        [TestCase("")]
        [TestCase("1234:00:000")]
        [TestCase("1:60:000")]
        [TestCase("1:2:3456")]
        public void TestInvalid(string input)
        {
            Assert.That(EditorTimestampParser.TryParse(input, out var time, out string? selection), Is.False);
            Assert.That(time, Is.Null);
            Assert.That(selection, Is.Null);
        }

        // CF: EditorTimestampParser.TryParse | timeMin >= 600 | TRUE
        [Test]
        public void TestTenHoursOrMoreRejected()
        {
            Assert.That(EditorTimestampParser.TryParse("600:00:000", out var time, out string? selection), Is.False);
            Assert.That(time, Is.Null);
            Assert.That(selection, Is.Null);
        }

        [Test]
        public void TestStrictRegexRequiresFullFormat()
        {
            Assert.That(EditorTimestampParser.TIME_REGEX_STRICT.IsMatch("see 01:23:456 (1,2)"), Is.True);
            Assert.That(EditorTimestampParser.TIME_REGEX_STRICT.IsMatch("1:23:456"), Is.False, "minutes need at least two digits");
            Assert.That(EditorTimestampParser.TIME_REGEX_STRICT.IsMatch("01:23:45"), Is.False, "milliseconds need three digits");
            Assert.That(EditorTimestampParser.TIME_REGEX_STRICT.IsMatch("01:61:000"), Is.False, "seconds below 60");
        }
    }
}
