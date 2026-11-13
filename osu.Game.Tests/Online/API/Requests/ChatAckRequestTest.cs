// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net.Http;
using NUnit.Framework;
using osu.Game.Online.API.Requests;

namespace osu.Game.Tests.Online.API.Requests
{
    [TestFixture]
    public class ChatAckRequestTest
    {
        // CF: ChatAckRequest.CreateWebRequest | SinceMessageId != null | FALSE
        // CF: ChatAckRequest.CreateWebRequest | SinceSilenceId != null | FALSE
        [Test]
        public void TestNoParameters()
        {
            var request = new ChatAckRequest();

            Assert.That(RequestInspector.GetMethod(request), Is.EqualTo(HttpMethod.Post));
            Assert.That(RequestInspector.GetParameters(request), Is.Empty);
            Assert.That(RequestInspector.GetUri(request), Is.EqualTo("http://api.test/api/v2/chat/ack"));
        }

        // CF: ChatAckRequest.CreateWebRequest | SinceMessageId != null | TRUE
        // CF: ChatAckRequest.CreateWebRequest | SinceSilenceId != null | TRUE
        [Test]
        public void TestBothParameters()
        {
            var request = new ChatAckRequest { SinceMessageId = 10, SinceSilenceId = 20 };

            Assert.That(RequestInspector.Values(request, "since"), Is.EqualTo(new[] { "10" }));
            Assert.That(RequestInspector.Values(request, "history_since"), Is.EqualTo(new[] { "20" }));
        }
    }
}
