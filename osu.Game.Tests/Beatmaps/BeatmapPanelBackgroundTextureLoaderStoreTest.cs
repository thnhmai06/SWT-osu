// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Game.Beatmaps;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Game.Tests.Beatmaps
{
    [TestFixture]
    public class BeatmapPanelBackgroundTextureLoaderStoreTest
    {
        // rows are tagged through the red channel so that the crop region can be identified.
        private static TextureUpload upload(int width, int height)
        {
            var image = new Image<Rgba32>(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    image[x, y] = new Rgba32((byte)(y % 256), 0, 0, 255);
            }

            return new TextureUpload(image);
        }

        private static BeatmapPanelBackgroundTextureLoaderStore store(TextureUpload? result)
        {
            var mock = new Mock<IResourceStore<TextureUpload>>();
            mock.Setup(s => s.Get("bg")).Returns(result!);
            mock.Setup(s => s.GetAsync("bg", It.IsAny<System.Threading.CancellationToken>())).Returns(Task.FromResult(result!));
            return new BeatmapPanelBackgroundTextureLoaderStore(mock.Object);
        }

        // CF: BeatmapPanelBackgroundTextureLoaderStore.Get | textureUpload == null | TRUE
        [Test]
        public void TestMissingTextureReturnsNull()
        {
            Assert.That(store(null).Get("bg"), Is.Null);
        }

        // CF: BeatmapPanelBackgroundTextureLoaderStore.Get | textureStore?.Get(name) | null
        [Test]
        public void TestNoUnderlyingStore()
        {
            var empty = new BeatmapPanelBackgroundTextureLoaderStore(null);

            Assert.That(empty.Get("bg"), Is.Null);
            Assert.That(empty.GetStream("bg"), Is.Null);
            Assert.That(empty.GetAvailableResources(), Is.Empty);
            Assert.That(() => empty.Dispose(), Throws.Nothing);
        }

        // CF: BeatmapPanelBackgroundTextureLoaderStore.GetAsync | textureStore == null | TRUE
        [Test]
        public async Task TestAsyncWithoutUnderlyingStore()
        {
            Assert.That(await new BeatmapPanelBackgroundTextureLoaderStore(null).GetAsync("bg"), Is.Null);
        }

        // CF: BeatmapPanelBackgroundTextureLoaderStore.GetAsync | textureUpload == null | TRUE
        [Test]
        public async Task TestAsyncMissingTexture()
        {
            Assert.That(await store(null).GetAsync("bg"), Is.Null);
        }

        // CF: BeatmapPanelBackgroundTextureLoaderStore.limitTextureUploadSize | Math.Min(size.Height, usableHeight) | size.Height > usableHeight
        [Test]
        public void TestTallImageIsCroppedToPanelRatioAroundTheCentre()
        {
            var result = store(upload(512, 400)).Get("bg");

            // 512 / (512 / 80) = 80 usable rows, taken from the vertical centre: rows 160..239.
            Assert.That((result.Width, result.Height), Is.EqualTo((512, 80)));
            Assert.That(result.Data[0].R, Is.EqualTo(160));
            Assert.That(result.Data[^1].R, Is.EqualTo(239));
        }

        // CF: BeatmapPanelBackgroundTextureLoaderStore.limitTextureUploadSize | Math.Min(size.Height, usableHeight) | size.Height <= usableHeight
        [Test]
        public void TestWideImageIsNotCropped()
        {
            var result = store(upload(512, 50)).Get("bg");

            Assert.That((result.Width, result.Height), Is.EqualTo((512, 50)));
            Assert.That(result.Data[0].R, Is.EqualTo(0));
        }

        [Test]
        public async Task TestAsyncCropsLikeSync()
        {
            var result = await store(upload(512, 400)).GetAsync("bg");

            Assert.That((result.Width, result.Height), Is.EqualTo((512, 80)));
            Assert.That(result.Data[0].R, Is.EqualTo(160));
        }

        [Test]
        public void TestResourcePassThrough()
        {
            var mock = new Mock<IResourceStore<TextureUpload>>();
            mock.Setup(s => s.GetAvailableResources()).Returns(new[] { "a", "b" });
            var wrapper = new BeatmapPanelBackgroundTextureLoaderStore(mock.Object);

            Assert.That(wrapper.GetAvailableResources(), Is.EqualTo(new[] { "a", "b" }));

            wrapper.Dispose();
            mock.Verify(s => s.Dispose(), Times.Once);
        }
    }
}
