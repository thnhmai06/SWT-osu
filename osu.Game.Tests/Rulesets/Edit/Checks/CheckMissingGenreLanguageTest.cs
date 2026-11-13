// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Edit.Checks.Components;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckMissingGenreLanguageTest
    {
        private readonly CheckMissingGenreLanguage check = new CheckMissingGenreLanguage();

        private Issue[] run(string tags)
        {
            var beatmap = new Beatmap();
            beatmap.Metadata.Tags = tags;
            return check.Run(CheckTestUtils.Context(beatmap)).ToArray();
        }

        // CF: CheckMissingGenreLanguage.Run | !hasTags<SearchGenre> | TRUE
        // CF: CheckMissingGenreLanguage.Run | !hasTags<SearchLanguage> | TRUE
        // CF: CheckMissingGenreLanguage.hasTags | foreach value | loop n
        // CF: CheckMissingGenreLanguage.hasTags | words.All(tags.Contains) | FALSE
        [Test]
        public void TestNoTagsReportsBoth()
        {
            var issues = run(string.Empty);

            Assert.That(issues.Select(i => i.Template.GetType()), Is.EquivalentTo(new[]
            {
                typeof(CheckMissingGenreLanguage.IssueTemplateMissingGenre),
                typeof(CheckMissingGenreLanguage.IssueTemplateMissingLanguage),
            }));
            Assert.That(check.PossibleTemplates.Count(), Is.EqualTo(2));
        }

        // CF: CheckMissingGenreLanguage.Run | !hasTags<SearchGenre> | FALSE
        // CF: CheckMissingGenreLanguage.Run | !hasTags<SearchLanguage> | FALSE
        // CF: CheckMissingGenreLanguage.hasTags | words.All(tags.Contains) | TRUE
        [TestCase("rock english")]
        [TestCase("ROCK ENGLISH")]
        [TestCase("some words pop japanese more words")]
        [TestCase("video game instrumental")]
        public void TestBothPresent(string tags)
        {
            Assert.That(run(tags), Is.Empty);
        }

        [Test]
        public void TestOnlyGenreOrOnlyLanguageMissing()
        {
            Assert.That(run("rock").Single().Template, Is.InstanceOf<CheckMissingGenreLanguage.IssueTemplateMissingLanguage>());
            Assert.That(run("english").Single().Template, Is.InstanceOf<CheckMissingGenreLanguage.IssueTemplateMissingGenre>());
        }

        [Test]
        public void TestMultiWordTagsNeedAllWords()
        {
            Assert.That(run("video english").Single().Template, Is.InstanceOf<CheckMissingGenreLanguage.IssueTemplateMissingGenre>(), "\"video game\" needs both words");
        }
    }
}
