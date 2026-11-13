// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;

namespace osu.Game.Tests.Beatmaps
{
    [TestFixture]
    public class MetadataUtilsTest
    {
        // CF: MetadataUtils.IsRomanised | char.IsAscii(c) && !char.IsControl(c) | TRUE
        [TestCase('a')]
        [TestCase(' ')]
        [TestCase('~')]
        public void TestRomanisedChar(char c) => Assert.That(MetadataUtils.IsRomanised(c), Is.True);

        // CF: MetadataUtils.IsRomanised | char.IsAscii(c) | FALSE
        [TestCase('é')]
        [TestCase('あ')]
        public void TestNonAsciiChar(char c) => Assert.That(MetadataUtils.IsRomanised(c), Is.False);

        // CF: MetadataUtils.IsRomanised | !char.IsControl(c) | FALSE
        [TestCase('\n')]
        [TestCase('\u007f')]
        public void TestControlChar(char c) => Assert.That(MetadataUtils.IsRomanised(c), Is.False);

        // CF: MetadataUtils.IsRomanised | string.IsNullOrEmpty(str) | TRUE
        [TestCase(null)]
        [TestCase("")]
        public void TestEmptyStringIsRomanised(string? s) => Assert.That(MetadataUtils.IsRomanised(s), Is.True);

        // CF: MetadataUtils.IsRomanised | string.IsNullOrEmpty(str) | FALSE
        [Test]
        public void TestStringRomanisation()
        {
            Assert.That(MetadataUtils.IsRomanised("Hello World"), Is.True);
            Assert.That(MetadataUtils.IsRomanised("Hello é"), Is.False);
        }

        // CF: MetadataUtils.StripNonRomanisedCharacters | string.IsNullOrEmpty(str) | TRUE
        [TestCase(null)]
        [TestCase("")]
        public void TestStripEmpty(string? s) => Assert.That(MetadataUtils.StripNonRomanisedCharacters(s), Is.Empty);

        // CF: MetadataUtils.StripNonRomanisedCharacters | foreach c | IsRomanised(c) TRUE / FALSE
        [TestCase("abc", "abc")]
        [TestCase("abc é def", "abc  def")]
        [TestCase("あ a い", "a")]
        [TestCase("あい", "")]
        public void TestStrip(string input, string expected) => Assert.That(MetadataUtils.StripNonRomanisedCharacters(input), Is.EqualTo(expected));
    }
}
