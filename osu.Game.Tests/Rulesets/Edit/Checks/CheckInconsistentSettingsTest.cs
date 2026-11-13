// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Storyboards;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckInconsistentSettingsTest
    {
        private readonly CheckInconsistentSettings check = new CheckInconsistentSettings();

        private static Beatmap beatmap(string name, Action<Beatmap>? configure = null)
        {
            var b = new Beatmap();
            b.BeatmapInfo.DifficultyName = name;
            configure?.Invoke(b);
            return b;
        }

        private Issue[] run(Beatmap current, Beatmap other, Storyboard? storyboard = null)
        {
            var context = new BeatmapVerifierContext(
                new BeatmapVerifierContext.VerifiedBeatmap(new TestWorkingBeatmap(current, storyboard), current),
                new[] { new BeatmapVerifierContext.VerifiedBeatmap(new TestWorkingBeatmap(other), other) },
                DifficultyRating.ExpertPlus);

            return check.Run(context).ToArray();
        }

        // CF: CheckInconsistentSettings.Run | context.AllDifficulties.Count() <= 1 | TRUE
        [Test]
        public void TestSingleDifficulty()
        {
            var b = beatmap("a");
            var context = new BeatmapVerifierContext(b, new TestWorkingBeatmap(b));

            Assert.That(check.Run(context), Is.Empty);
            Assert.That(check.PossibleTemplates.Count(), Is.EqualTo(2));
        }

        // CF: CheckInconsistentSettings.Run | context.AllDifficulties.Count() <= 1 | FALSE
        // CF: CheckInconsistentSettings.checkIssue | !EqualityComparer<T>.Default.Equals | FALSE
        // CF: CheckInconsistentSettings.Run | hasStoryboard | FALSE
        [Test]
        public void TestIdenticalSettings()
        {
            Assert.That(run(beatmap("a"), beatmap("b")), Is.Empty);
        }

        // CF: CheckInconsistentSettings.checkIssue | !EqualityComparer<T>.Default.Equals | TRUE
        [TestCase("Audio lead-in", false)]
        [TestCase("Countdown", false)]
        [TestCase("Countdown offset", false)]
        [TestCase("Epilepsy warning", false)]
        [TestCase("Letterbox during breaks", false)]
        [TestCase("Samples match playback rate", false)]
        [TestCase("Tick Rate", true)]
        public void TestEachSettingIsCompared(string field, bool negligible)
        {
            Action<Beatmap> change = field switch
            {
                "Audio lead-in" => b => b.AudioLeadIn = 500,
                "Countdown" => b => b.Countdown = CountdownType.DoubleSpeed,
                "Countdown offset" => b => b.CountdownOffset = 3,
                "Epilepsy warning" => b => b.EpilepsyWarning = true,
                "Letterbox during breaks" => b => b.LetterboxInBreaks = true,
                "Samples match playback rate" => b => b.SamplesMatchPlaybackRate = true,
                _ => b => b.Difficulty.SliderTickRate = 4,
            };

            var issue = run(beatmap("a"), beatmap("b", change)).Single();

            Assert.That(issue.Arguments[0], Is.EqualTo(field));
            Assert.That(issue.Template.Type, Is.EqualTo(negligible ? IssueType.Negligible : IssueType.Warning));
        }

        // CF: CheckInconsistentSettings.Run | hasStoryboard | TRUE
        [Test]
        public void TestWidescreenIsOnlyComparedWhenStoryboardPresent()
        {
            var current = beatmap("a", b => b.WidescreenStoryboard = false);
            var other = beatmap("b", b => b.WidescreenStoryboard = true);

            Assert.That(run(current, other), Is.Empty);

            var storyboard = new Storyboard();
            storyboard.GetLayer("Background").Add(new StoryboardSprite(StoryboardElementSource.Beatmap, "x.png", osu.Framework.Graphics.Anchor.Centre, osuTK.Vector2.Zero));

            Assert.That(run(current, other, storyboard).Single().Arguments[0], Is.EqualTo("Widescreen support"));
        }
    }
}
