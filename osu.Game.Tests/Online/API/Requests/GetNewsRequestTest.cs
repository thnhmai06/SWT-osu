// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json.Linq;
using NUnit.Framework;
using osu.Game.Online.API.Requests;

namespace osu.Game.Tests.Online.API.Requests
{
    [TestFixture]
    public class GetNewsRequestTest
    {
        // CF: GetNewsRequest.CreateWebRequest | cursor != null | FALSE
        // CF: GetNewsRequest.CreateWebRequest | year.HasValue | FALSE
        [Test]
        public void TestNoParameters()
        {
            var request = new GetNewsRequest();

            Assert.That(RequestInspector.GetParameters(request), Is.Empty);
            Assert.That(RequestInspector.GetUri(request), Is.EqualTo("http://api.test/api/v2/news"));
        }

        // CF: GetNewsRequest.CreateWebRequest | cursor != null | TRUE
        // CF: GetNewsRequest.CreateWebRequest | year.HasValue | TRUE
        [Test]
        public void TestYearAndCursor()
        {
            var cursor = new Cursor();
            cursor.Properties["id"] = new JValue(5);

            var request = new GetNewsRequest(2024, cursor);

            Assert.That(RequestInspector.Values(request, "year"), Is.EqualTo(new[] { "2024" }));
            Assert.That(RequestInspector.Values(request, "cursor[id]"), Is.EqualTo(new[] { "5" }));
        }
    }
}
