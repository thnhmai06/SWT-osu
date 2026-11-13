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
    public class CheckTitleMarkersTest
    {
        private readonly CheckTitleMarkers check = new CheckTitleMarkers();

        private Issue[] run(string title, string? unicode = null)
        {
            var beatmap = new Beatmap();
            beatmap.Metadata.Title = title;
            beatmap.Metadata.TitleUnicode = unicode ?? title;
            return check.Run(CheckTestUtils.Context(beatmap)).ToArray();
        }

        // CF: CheckTitleMarkers.Run | foreach markerCheck | loop n
        // CF: CheckTitleMarkers.Run | AnyRegex.IsMatch(unicodeTitle) | FALSE
        // CF: CheckTitleMarkers.Run | hasRomanisedTitle | FALSE
        [TestCase("Song")]
        [TestCase("Song (TV Size)")]
        [TestCase("Song (Game Ver.)")]
        [TestCase("Song (Short Ver.)")]
        [TestCase("Song (Cut Ver.)")]
        [TestCase("Song (Sped Up Ver.)")]
        [TestCase("Song (Nightcore Mix)")]
        [TestCase("Song (Sped Up & Cut Ver.)")]
        [TestCase("Song (Nightcore & Cut Ver.)")]
        public void TestCorrectOrAbsentMarkers(string title)
        {
            Assert.That(run(title), Is.Empty);
            Assert.That(check.PossibleTemplates.Count(), Is.EqualTo(1));
        }

        // CF: CheckTitleMarkers.Run | AnyRegex.IsMatch(unicodeTitle) | TRUE
        // CF: CheckTitleMarkers.Run | !unicodeTitle.Contains(correct marker) | TRUE
        [TestCase("Song (tv size)")]
        [TestCase("Song (TV Ver)")]
        [TestCase("Song (game size)")]
        [TestCase("Song (short size)")]
        [TestCase("Song cut ver")]
        [TestCase("Song speed up ver")]
        [TestCase("Song night core mix")]
        public void TestIncorrectMarkerFormat(string title)
        {
            var issue = run(title).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckTitleMarkers.IssueTemplateIncorrectMarker>());
            Assert.That(issue.Arguments[0], Is.EqualTo("Title"));
        }

        // CF: CheckTitleMarkers.Run | hasRomanisedTitle | TRUE
        // CF: CheckTitleMarkers.Run | AnyRegex.IsMatch(romanisedTitle) | TRUE
        // CF: CheckTitleMarkers.Run | !romanisedTitle.Contains(correct marker) | TRUE
        [Test]
        public void TestRomanisedTitleReportedSeparately()
        {
            var issues = run("Song (tv size)", "Song (game ver)");

            Assert.That(issues.Select(i => i.Arguments[0]), Is.EquivalentTo(new object[] { "Title", "Romanised title" }));
        }

        // CF: CheckTitleMarkers.Run | !romanisedTitle.Contains(correct marker) | FALSE
        // CF: CheckTitleMarkers.Run | AnyRegex.IsMatch(romanisedTitle) | FALSE
        // CF: CheckTitleMarkers.Run | !unicodeTitle.Contains(correct marker) | FALSE
        [Test]
        public void TestCorrectRomanisedAndUnicodeTitlesDiffering()
        {
            Assert.That(run("Song (TV Size)", "Other (TV Size)"), Is.Empty);
            Assert.That(run("Plain", "Other (TV Size)"), Is.Empty);
        }

        [Test]
        public void TestCombinedMarkerOnlyMatchesCombinedPattern()
        {
            Assert.That(run("Song sped up & cut ver").Count(), Is.EqualTo(1), "'& cut' lookbehind prevents the plain cut marker from matching as well");
        }

        [Test]
        public void TestEqualTitlesAreReportedOnce()
        {
            Assert.That(run("Song (tv size)").Length, Is.EqualTo(1), "identical romanised and unicode titles are only checked once");
        }
    }
}
