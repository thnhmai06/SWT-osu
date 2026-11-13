// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckInconsistentMetadataTest
    {
        private readonly CheckInconsistentMetadata check = new CheckInconsistentMetadata();

        private static Beatmap beatmap(string name, Action<BeatmapMetadata>? configure = null)
        {
            var b = new Beatmap();
            b.BeatmapInfo.DifficultyName = name;
            b.Metadata.Artist = "artist";
            b.Metadata.ArtistUnicode = "artist u";
            b.Metadata.Title = "title";
            b.Metadata.TitleUnicode = "title u";
            b.Metadata.Source = "source";
            b.Metadata.Author.Username = "mapper";
            b.Metadata.Tags = "a b";
            configure?.Invoke(b.Metadata);
            return b;
        }

        private Issue[] run(Beatmap current, params Beatmap[] others)
        {
            var context = new BeatmapVerifierContext(
                new BeatmapVerifierContext.VerifiedBeatmap(new TestWorkingBeatmap(current), current),
                others.Select(o => new BeatmapVerifierContext.VerifiedBeatmap(new TestWorkingBeatmap(o), o)).ToList(),
                DifficultyRating.ExpertPlus);

            return check.Run(context).ToArray();
        }

        // CF: CheckInconsistentMetadata.Run | context.AllDifficulties.Count() <= 1 | TRUE
        [Test]
        public void TestSingleDifficulty()
        {
            Assert.That(run(beatmap("a")), Is.Empty);
            Assert.That(check.PossibleTemplates.Count(), Is.EqualTo(2));
        }

        // CF: CheckInconsistentMetadata.Run | context.AllDifficulties.Count() <= 1 | FALSE
        // CF: CheckInconsistentMetadata.Run | foreach other difficulty | loop 1
        // CF: CheckInconsistentMetadata.Run | foreach field | loop n
        // CF: CheckInconsistentMetadata.Run | referenceField != currentField | FALSE
        // CF: CheckInconsistentMetadata.Run | referenceMetadata.Tags != currentMetadata.Tags | FALSE
        [Test]
        public void TestConsistentMetadata()
        {
            Assert.That(run(beatmap("a"), beatmap("b")), Is.Empty);
        }

        // CF: CheckInconsistentMetadata.Run | referenceField != currentField | TRUE
        [TestCase("artist")]
        [TestCase("unicode artist")]
        [TestCase("title")]
        [TestCase("unicode title")]
        [TestCase("source")]
        [TestCase("creator")]
        public void TestEachFieldIsCompared(string field)
        {
            Action<BeatmapMetadata> change = field switch
            {
                "artist" => m => m.Artist = "x",
                "unicode artist" => m => m.ArtistUnicode = "x",
                "title" => m => m.Title = "x",
                "unicode title" => m => m.TitleUnicode = "x",
                "source" => m => m.Source = "x",
                _ => m => m.Author.Username = "x",
            };

            var issue = run(beatmap("a"), beatmap("b", change)).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckInconsistentMetadata.IssueTemplateInconsistentOtherFields>());
            Assert.That(issue.Arguments[0], Is.EqualTo(field));
        }

        // CF: CheckInconsistentMetadata.Run | referenceMetadata.Tags != currentMetadata.Tags | TRUE
        // CF: CheckInconsistentMetadata.Run | !string.IsNullOrEmpty(difference) | TRUE
        [Test]
        public void TestTagDifferenceListsSymmetricDifference()
        {
            var issue = run(beatmap("a"), beatmap("b", m => m.Tags = "b c")).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckInconsistentMetadata.IssueTemplateInconsistentTags>());
            Assert.That(((string)issue.Arguments[2]).Split(' '), Is.EquivalentTo(new[] { "a", "c" }));
        }

        // CF: CheckInconsistentMetadata.Run | !string.IsNullOrEmpty(difference) | FALSE
        [Test]
        public void TestReorderedTagsAreNotAnIssue()
        {
            Assert.That(run(beatmap("a"), beatmap("b", m => m.Tags = "b a")), Is.Empty);
        }

        // CF: CheckInconsistentMetadata.Run | foreach other difficulty | loop n
        [Test]
        public void TestMultipleOtherDifficulties()
        {
            Assert.That(run(beatmap("a"), beatmap("b", m => m.Source = "x"), beatmap("c", m => m.Source = "y")).Length, Is.EqualTo(2));
        }
    }
}
