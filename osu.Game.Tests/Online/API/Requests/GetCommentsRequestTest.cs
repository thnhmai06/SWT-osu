// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Online.API.Requests;
using osu.Game.Overlays.Comments;

namespace osu.Game.Tests.Online.API.Requests
{
    [TestFixture]
    public class GetCommentsRequestTest
    {
        // CF: GetCommentsRequest.CreateWebRequest | parentId != null | FALSE
        [Test]
        public void TestParametersWithoutParent()
        {
            var request = new GetCommentsRequest(9, CommentableType.NewsPost);

            Assert.That(RequestInspector.Values(request, "commentable_id"), Is.EqualTo(new[] { "9" }));
            Assert.That(RequestInspector.Values(request, "commentable_type"), Is.EqualTo(new[] { "news_post" }));
            Assert.That(RequestInspector.Values(request, "page"), Is.EqualTo(new[] { "1" }));
            Assert.That(RequestInspector.Values(request, "sort"), Is.EqualTo(new[] { "new" }));
            Assert.That(RequestInspector.Values(request, "parent_id"), Is.Empty);
        }

        // CF: GetCommentsRequest.CreateWebRequest | parentId != null | TRUE
        [Test]
        public void TestParametersWithParent()
        {
            var request = new GetCommentsRequest(9, CommentableType.Beatmapset, CommentsSortCriteria.Top, 3, 77);

            Assert.That(RequestInspector.Values(request, "commentable_type"), Is.EqualTo(new[] { "beatmapset" }));
            Assert.That(RequestInspector.Values(request, "page"), Is.EqualTo(new[] { "3" }));
            Assert.That(RequestInspector.Values(request, "sort"), Is.EqualTo(new[] { "top" }));
            Assert.That(RequestInspector.Values(request, "parent_id"), Is.EqualTo(new[] { "77" }));
        }
    }
}
