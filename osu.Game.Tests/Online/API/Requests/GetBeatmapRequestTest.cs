// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Online.API.Requests;

namespace osu.Game.Tests.Online.API.Requests
{
    [TestFixture]
    public class GetBeatmapRequestTest
    {
        // CF: GetBeatmapRequest.CreateWebRequest | OnlineID > 0 | TRUE
        // CF: GetBeatmapRequest.CreateWebRequest | !string.IsNullOrEmpty(MD5Hash) | TRUE
        // CF: GetBeatmapRequest.CreateWebRequest | !string.IsNullOrEmpty(Filename) | TRUE
        [Test]
        public void TestAllParameters()
        {
            var request = new GetBeatmapRequest(12, "abc", "file.osu");

            Assert.That(RequestInspector.GetUri(request), Is.EqualTo("http://api.test/api/v2/beatmaps/lookup"));
            Assert.That(RequestInspector.Values(request, "id"), Is.EqualTo(new[] { "12" }));
            Assert.That(RequestInspector.Values(request, "checksum"), Is.EqualTo(new[] { "abc" }));
            Assert.That(RequestInspector.Values(request, "filename"), Is.EqualTo(new[] { "file.osu" }));
        }

        // CF: GetBeatmapRequest.CreateWebRequest | OnlineID > 0 | FALSE
        // CF: GetBeatmapRequest.CreateWebRequest | !string.IsNullOrEmpty(MD5Hash) | FALSE
        // CF: GetBeatmapRequest.CreateWebRequest | !string.IsNullOrEmpty(Filename) | FALSE
        [Test]
        public void TestNoParameters()
        {
            var request = new GetBeatmapRequest();

            Assert.That(RequestInspector.GetParameters(request), Is.Empty);
        }

        // CF: GetBeatmapRequest.ctor | beatmapInfo as BeatmapInfo | non-null
        [Test]
        public void TestFromBeatmapInfoWithoutFileHasNoPath()
        {
            var request = new GetBeatmapRequest(new BeatmapInfo { OnlineID = 3, MD5Hash = "h" });

            Assert.That(request.OnlineID, Is.EqualTo(3));
            Assert.That(request.MD5Hash, Is.EqualTo("h"));
            Assert.That(request.Filename, Is.Null);
        }

        // CF: GetBeatmapRequest.ctor | beatmapInfo as BeatmapInfo | null
        [Test]
        public void TestFromNonBeatmapInfoHasNoFilename()
        {
            var info = new Moq.Mock<IBeatmapInfo>();
            info.SetupGet(i => i.OnlineID).Returns(4);
            info.SetupGet(i => i.MD5Hash).Returns("h");

            Assert.That(new GetBeatmapRequest(info.Object).Filename, Is.Null);
        }
    }
}
