// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Storyboards;
using osu.Game.Tests.Beatmaps;
using osuTK;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class ResourcesCheckUtilsTest
    {
        private static TestWorkingBeatmap working(Storyboard storyboard) => new TestWorkingBeatmap(new Beatmap(), storyboard);

        private static StoryboardSprite sprite() => new StoryboardSprite(StoryboardElementSource.Beatmap, "a.png", Anchor.Centre, Vector2.Zero);

        // CF: ResourcesCheckUtils.HasAnyStoryboardElementPresent | foreach layer / foreach element | loop 0
        [Test]
        public void TestEmptyStoryboardHasNoElements()
        {
            Assert.That(ResourcesCheckUtils.HasAnyStoryboardElementPresent(working(new Storyboard())), Is.False);
        }

        // CF: ResourcesCheckUtils.HasAnyStoryboardElementPresent | foreach layer / foreach element | loop 1
        [Test]
        public void TestElementPresent()
        {
            var storyboard = new Storyboard();
            storyboard.GetLayer("Foreground").Add(sprite());

            Assert.That(ResourcesCheckUtils.HasAnyStoryboardElementPresent(working(storyboard)), Is.True);
        }

        // CF: ResourcesCheckUtils.GetDifficultyVideo | element is StoryboardVideo video | FALSE
        [Test]
        public void TestNoVideo()
        {
            var storyboard = new Storyboard();
            storyboard.GetLayer("Foreground").Add(sprite());

            Assert.That(ResourcesCheckUtils.GetDifficultyVideo(working(storyboard)), Is.Null);
        }

        // CF: ResourcesCheckUtils.GetDifficultyVideo | element is StoryboardVideo video | TRUE
        [Test]
        public void TestFirstVideoReturned()
        {
            var storyboard = new Storyboard();
            storyboard.GetLayer("Video").Add(sprite());
            var first = new StoryboardVideo(StoryboardElementSource.Beatmap, "1.mp4", 0);
            storyboard.GetLayer("Video").Add(first);
            storyboard.GetLayer("Video").Add(new StoryboardVideo(StoryboardElementSource.Beatmap, "2.mp4", 0));

            Assert.That(ResourcesCheckUtils.GetDifficultyVideo(working(storyboard)), Is.SameAs(first));
        }
    }
}
