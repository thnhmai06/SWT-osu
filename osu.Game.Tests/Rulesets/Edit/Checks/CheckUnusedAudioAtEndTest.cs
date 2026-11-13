// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Storyboards;
using osuTK;
using osu.Framework.Graphics;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckUnusedAudioAtEndTest
    {
        private readonly CheckUnusedAudioAtEnd check = new CheckUnusedAudioAtEnd();

        private Issue[] run(double? lastObject, bool storyboardElement = false)
        {
            var beatmap = new Beatmap();
            if (lastObject != null) beatmap.HitObjects.Add(new HitCircle { StartTime = lastObject.Value });

            var storyboard = new Storyboard();
            if (storyboardElement) storyboard.GetLayer("Foreground").Add(new StoryboardSprite(StoryboardElementSource.Beatmap, "a.png", Anchor.Centre, Vector2.Zero));

            var context = CheckFakes.Context(beatmap, new Dictionary<string, byte[]?>(), storyboard, new BitrateTrack(1000, null));
            return check.Run(context).ToArray();
        }

        // CF: CheckUnusedAudioAtEnd.Run | HitObjects.Any() | FALSE
        // CF: CheckUnusedAudioAtEnd.Run | mappedPercentage < 80 | TRUE
        // CF: CheckUnusedAudioAtEnd.Run | storyboardIsPresent | FALSE
        [Test]
        public void TestNoObjects()
        {
            var issue = run(null).Single();

            Assert.That(issue.Template, Is.InstanceOf<CheckUnusedAudioAtEnd.IssueTemplateUnusedAudioAtEnd>());
            Assert.That(issue.Arguments[0], Is.EqualTo(100d));
        }

        // CF: CheckUnusedAudioAtEnd.Run | HitObjects.Any() | TRUE
        [Test]
        public void TestMappedPartially()
        {
            Assert.That(run(500).Single().Arguments[0], Is.EqualTo(50d));
        }

        // CF: CheckUnusedAudioAtEnd.Run | mappedPercentage < 80 | FALSE
        [TestCase(796)]
        [TestCase(1000)]
        public void TestMappedEnough(double last)
        {
            Assert.That(run(last), Is.Empty);
        }

        [Test]
        public void TestJustBelowThreshold()
        {
            Assert.That(run(794), Has.Length.EqualTo(1));
        }

        // CF: CheckUnusedAudioAtEnd.Run | storyboardIsPresent | TRUE
        [Test]
        public void TestStoryboardPresentUsesStoryboardTemplate()
        {
            Assert.That(run(500, true).Single().Template, Is.InstanceOf<CheckUnusedAudioAtEnd.IssueTemplateUnusedAudioAtEndStoryboardOrVideo>());
        }
    }
}
