// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Extensions;
using osu.Game.Models;
using osu.Game.Storyboards;
using osuTK;

namespace osu.Game.Tests.Storyboards
{
    [TestFixture]
    public class StoryboardTest
    {
        private static StoryboardSprite sprite(string path = "a.png", double alphaStart = 100, double alphaEnd = 200)
        {
            var s = new StoryboardSprite(StoryboardElementSource.Beatmap, path, Anchor.Centre, Vector2.Zero);
            s.Commands.AddAlpha(Easing.None, alphaStart, alphaEnd, 1, 1);
            return s;
        }

        [Test]
        public void TestDefaultLayersExistInDepthOrder()
        {
            var storyboard = new Storyboard();

            Assert.That(storyboard.Layers.Select(l => l.Name), Is.EquivalentTo(new[] { "Video", "Background", "Fail", "Pass", "Foreground", "Overlay" }));
            Assert.That(storyboard.GetLayer("Background").Depth, Is.EqualTo(3));
            Assert.That(storyboard.GetLayer("Fail").VisibleWhenPassing, Is.False);
            Assert.That(storyboard.GetLayer("Pass").VisibleWhenFailing, Is.False);
            Assert.That(storyboard.GetLayer("Overlay").Depth, Is.EqualTo(int.MinValue));
        }

        // CF: Storyboard.GetLayer | !layers.TryGetValue(name, out layer) | TRUE
        [Test]
        public void TestUnknownLayerCreatedInFrontOfExistingOnes()
        {
            var storyboard = new Storyboard();

            var first = storyboard.GetLayer("Custom1");
            var second = storyboard.GetLayer("Custom2");

            Assert.That(first.Depth, Is.EqualTo(-1));
            Assert.That(second.Depth, Is.EqualTo(-2));
            Assert.That(storyboard.Layers, Has.Member(first));
        }

        // CF: Storyboard.GetLayer | !layers.TryGetValue(name, out layer) | FALSE
        [Test]
        public void TestExistingLayerReturnedAsIs()
        {
            var storyboard = new Storyboard();

            Assert.That(storyboard.GetLayer("Foreground"), Is.SameAs(storyboard.GetLayer("Foreground")));
            Assert.That(storyboard.Layers.Count(), Is.EqualTo(6));
        }

        // CF: Storyboard.HasDrawable | Layers.Any(l => l.Elements.Any(e => e.IsDrawable)) | FALSE
        [Test]
        public void TestHasDrawableFalseWithoutDrawableElements()
        {
            var storyboard = new Storyboard();
            Assert.That(storyboard.HasDrawable, Is.False);

            storyboard.GetLayer("Foreground").Add(new StoryboardSprite(StoryboardElementSource.Beatmap, "a.png", Anchor.Centre, Vector2.Zero));
            Assert.That(storyboard.HasDrawable, Is.False, "a sprite without commands is not drawable");
        }

        // CF: Storyboard.HasDrawable | Layers.Any(l => l.Elements.Any(e => e.IsDrawable)) | TRUE
        [Test]
        public void TestHasDrawableTrueWithCommands()
        {
            var storyboard = new Storyboard();
            storyboard.GetLayer("Foreground").Add(sprite());

            Assert.That(storyboard.HasDrawable, Is.True);
        }

        // CF: Storyboard.EarliestEventTime | MinBy(e => e.StartTime)?.StartTime | null
        // CF: Storyboard.LatestEventTime | MaxBy(e => e.GetEndTime())?.GetEndTime() | null
        [Test]
        public void TestEventTimesNullWithoutElements()
        {
            var storyboard = new Storyboard();

            Assert.That(storyboard.EarliestEventTime, Is.Null);
            Assert.That(storyboard.LatestEventTime, Is.Null);
        }

        // CF: Storyboard.EarliestEventTime | MinBy(e => e.StartTime)?.StartTime | non-null
        // CF: Storyboard.LatestEventTime | MaxBy(e => e.GetEndTime())?.GetEndTime() | non-null
        [Test]
        public void TestEventTimesSpanElementsAcrossLayersIgnoringVideo()
        {
            var storyboard = new Storyboard();
            storyboard.GetLayer("Foreground").Add(sprite("a.png", 500, 900));
            storyboard.GetLayer("Background").Add(sprite("b.png", 300, 700));
            storyboard.GetLayer("Video").Add(new StoryboardVideo(StoryboardElementSource.Beatmap, "v.mp4", -5000));

            Assert.That(storyboard.EarliestEventTime, Is.EqualTo(300));
            Assert.That(storyboard.LatestEventTime, Is.EqualTo(900));
        }

        // CF: Storyboard.PrimaryVideo | OfType<StoryboardVideo>().FirstOrDefault() | null
        [Test]
        public void TestPrimaryVideoNullWithoutVideo()
        {
            var storyboard = new Storyboard();
            storyboard.GetLayer("Video").Add(sprite());

            Assert.That(storyboard.PrimaryVideo, Is.Null);
        }

        // CF: Storyboard.PrimaryVideo | OfType<StoryboardVideo>().FirstOrDefault() | non-null
        [Test]
        public void TestPrimaryVideoIsFirstVideo()
        {
            var storyboard = new Storyboard();
            var first = new StoryboardVideo(StoryboardElementSource.Beatmap, "1.mp4", 0);
            storyboard.GetLayer("Video").Add(sprite());
            storyboard.GetLayer("Video").Add(first);
            storyboard.GetLayer("Video").Add(new StoryboardVideo(StoryboardElementSource.Beatmap, "2.mp4", 0));

            Assert.That(storyboard.PrimaryVideo, Is.SameAs(first));
        }

        // CF: Storyboard.ReplacesBackground | string.IsNullOrEmpty(backgroundPath) | TRUE
        [Test]
        public void TestDoesNotReplaceBackgroundWithoutBackgroundFile()
        {
            var storyboard = new Storyboard();
            storyboard.GetLayer("Background").Add(sprite("bg.png"));

            Assert.That(storyboard.ReplacesBackground, Is.False);
        }

        // CF: Storyboard.ReplacesBackground | string.IsNullOrEmpty(backgroundPath) | FALSE
        // CF: Storyboard.ReplacesBackground | GetLayer("Background").Elements.Any(e => string.Equals(e.Path, backgroundPath, OrdinalIgnoreCase)) | TRUE
        [Test]
        public void TestReplacesBackgroundWhenBackgroundLayerUsesSameFileIgnoringCase()
        {
            var storyboard = new Storyboard();
            storyboard.BeatmapInfo.Metadata.BackgroundFile = "BG.PNG";
            storyboard.GetLayer("Background").Add(sprite("bg.png"));

            Assert.That(storyboard.ReplacesBackground, Is.True);
        }

        // CF: Storyboard.ReplacesBackground | GetLayer("Background").Elements.Any(e => string.Equals(...)) | FALSE
        [Test]
        public void TestDoesNotReplaceBackgroundWhenOtherFileOrOtherLayer()
        {
            var storyboard = new Storyboard();
            storyboard.BeatmapInfo.Metadata.BackgroundFile = "bg.png";
            storyboard.GetLayer("Background").Add(sprite("other.png"));
            storyboard.GetLayer("Foreground").Add(sprite("bg.png"));

            Assert.That(storyboard.ReplacesBackground, Is.False);
        }

        private static Storyboard storyboardWithFiles(params string[] filenames)
        {
            var storyboard = new Storyboard();
            var set = new BeatmapSetInfo();

            foreach (string name in filenames)
                set.Files.Add(new RealmNamedFileUsage(new RealmFile { Hash = "ab" + name.GetHashCode().ToString("x8") }, name));

            storyboard.BeatmapInfo.BeatmapSet = set;
            return storyboard;
        }

        // CF: Storyboard.GetStoragePathFromStoryboardPath | Path.HasExtension(path) | TRUE
        [Test]
        public void TestStoragePathForPathWithExtension()
        {
            var storyboard = storyboardWithFiles("sb/a.png");

            Assert.That(storyboard.GetStoragePathFromStoryboardPath("sb/a.png"), Is.EqualTo(storyboard.BeatmapInfo.BeatmapSet!.Files.Single().File.GetStoragePath()));
            Assert.That(storyboard.GetStoragePathFromStoryboardPath("sb/b.png"), Is.Null);
        }

        // CF: Storyboard.GetStoragePathFromStoryboardPath | BeatmapSet?.GetPathForFile | null
        [Test]
        public void TestStoragePathWithoutBeatmapSet()
        {
            var storyboard = new Storyboard();

            Assert.That(storyboard.GetStoragePathFromStoryboardPath("a.png"), Is.Null);
            Assert.That(storyboard.GetStoragePathFromStoryboardPath("a"), Is.Null);
        }

        // CF: Storyboard.GetStoragePathFromStoryboardPath | Path.HasExtension(path) | FALSE
        // CF: Storyboard.GetStoragePathFromStoryboardPath | foreach ext in IMAGE_EXTENSIONS | loop n
        // CF: Storyboard.GetStoragePathFromStoryboardPath | (resolvedPath = ...) != null | TRUE
        // CF: Storyboard.GetStoragePathFromStoryboardPath | (resolvedPath = ...) != null | FALSE
        [Test]
        public void TestExtensionlessPathGuessesImageExtension()
        {
            var storyboard = storyboardWithFiles("sb/a.png", "sb/b.jpg");
            var files = storyboard.BeatmapInfo.BeatmapSet!.Files;

            Assert.That(storyboard.GetStoragePathFromStoryboardPath("sb/a"), Is.EqualTo(files[0].File.GetStoragePath()), ".jpg and .jpeg fail first, .png succeeds");
            Assert.That(storyboard.GetStoragePathFromStoryboardPath("sb/b"), Is.EqualTo(files[1].File.GetStoragePath()));
            Assert.That(storyboard.GetStoragePathFromStoryboardPath("sb/missing"), Is.Null);
        }

        [Test]
        public void TestSimpleProperties()
        {
            var storyboard = new Storyboard { BackgroundOffset = new Vector2(1, 2), UseSkinSprites = true };

            Assert.That(storyboard.BackgroundOffset, Is.EqualTo(new Vector2(1, 2)));
            Assert.That(storyboard.UseSkinSprites, Is.True);
        }
    }
}
