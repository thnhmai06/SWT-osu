// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Tests.Online.API.Requests.Responses
{
    [TestFixture]
    public class APIBeatmapSetTest
    {
        // CF: APIBeatmapSet.author | value.Id != 1 | TRUE
        [Test]
        public void TestUserPropertyReplacesAuthor()
        {
            var set = JsonConvert.DeserializeObject<APIBeatmapSet>("{\"user\":{\"id\":7,\"username\":\"mapper\"}}")!;

            Assert.That(set.Author.Id, Is.EqualTo(7));
            Assert.That(set.AuthorString, Is.EqualTo("mapper"));
        }

        // CF: APIBeatmapSet.author | value.Id != 1 | FALSE
        [Test]
        public void TestBanchoBotUserIsIgnored()
        {
            var set = JsonConvert.DeserializeObject<APIBeatmapSet>("{\"user_id\":5,\"creator\":\"real\",\"user\":{\"id\":1,\"username\":\"BanchoBot\"}}")!;

            Assert.That(set.AuthorID, Is.EqualTo(5));
            Assert.That(set.AuthorString, Is.EqualTo("real"));
        }

        // CF: APIBeatmapSet.Metadata | maps title artist author source tags | no throw
        [Test]
        public void TestMetadata()
        {
            IBeatmapSetInfo set = new APIBeatmapSet { Title = "t", TitleUnicode = "tu", Artist = "a", ArtistUnicode = "au", AuthorID = 3, AuthorString = "m", Source = "s", Tags = "x y" };

            Assert.That(set.Metadata.Title, Is.EqualTo("t"));
            Assert.That(set.Metadata.TitleUnicode, Is.EqualTo("tu"));
            Assert.That(set.Metadata.Artist, Is.EqualTo("a"));
            Assert.That(set.Metadata.ArtistUnicode, Is.EqualTo("au"));
            Assert.That(set.Metadata.Author.OnlineID, Is.EqualTo(3));
            Assert.That(set.Metadata.Author.Username, Is.EqualTo("m"));
            Assert.That(set.Metadata.Source, Is.EqualTo("s"));
            Assert.That(set.Metadata.Tags, Is.EqualTo("x y"));
        }

        // CF: APIBeatmapSet.Equals | other is APIBeatmapSet b | TRUE
        // CF: APIBeatmapSet.GetHashCode | OnlineID.GetHashCode() | no throw
        [Test]
        public void TestEqualsById()
        {
            Assert.That(new APIBeatmapSet { OnlineID = 2 }.Equals(new APIBeatmapSet { OnlineID = 2 }), Is.True);
            Assert.That(new APIBeatmapSet { OnlineID = 2 }.GetHashCode(), Is.EqualTo(2.GetHashCode()));
        }

        // CF: APIBeatmapSet.Equals | other is APIBeatmapSet b | FALSE
        [Test]
        public void TestNotEquals()
        {
            Assert.That(new APIBeatmapSet { OnlineID = 2 }.Equals(new APIBeatmapSet { OnlineID = 3 }), Is.False);
            Assert.That(new APIBeatmapSet { OnlineID = 2 }.Equals(new BeatmapSetInfo { OnlineID = 2 }), Is.False);
        }
    }
}
