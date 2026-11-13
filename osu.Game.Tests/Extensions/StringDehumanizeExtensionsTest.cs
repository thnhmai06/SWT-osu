// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Extensions;

namespace osu.Game.Tests.Extensions
{
    [TestFixture]
    public class StringDehumanizeExtensionsTest
    {
        // CF: StringDehumanizeExtensions.ToPascalCase | separator regex matches | non-null
        [TestCase("some_title here-now", "SomeTitleHereNow")]
        [TestCase("", "")]
        public void TestPascalCase(string input, string expected) => Assert.That(input.ToPascalCase(), Is.EqualTo(expected));

        // CF: StringDehumanizeExtensions.ToCamelCase | word.Length > 0 | TRUE
        // CF: StringDehumanizeExtensions.ToCamelCase | word.Length > 0 | FALSE
        [TestCase("some_title", "someTitle")]
        [TestCase("", "")]
        public void TestCamelCase(string input, string expected) => Assert.That(input.ToCamelCase(), Is.EqualTo(expected));

        // CF: StringDehumanizeExtensions.ToSnakeCase | input | non-null
        [TestCase("SomeTitleHTMLParser", "some_title_html_parser")]
        [TestCase("some-title here", "some_title_here")]
        [TestCase("abc1Def", "abc1_def")]
        public void TestSnakeCase(string input, string expected) => Assert.That(input.ToSnakeCase(), Is.EqualTo(expected));

        // CF: StringDehumanizeExtensions.ToKebabCase | input | non-null
        [Test]
        public void TestKebabCase() => Assert.That("SomeTitle".ToKebabCase(), Is.EqualTo("some-title"));
    }
}
