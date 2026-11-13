// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Spectator;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using Newtonsoft.Json.Linq;

namespace osu.Game.Tests.Online
{
    [TestFixture]
    public class ResponseModelsTest
    {
        private static readonly DateTimeOffset past = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset future = new DateTimeOffset(2999, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // CF: APIMenuImage.IsCurrent | Begins == null | TRUE
        // CF: APIMenuImage.IsCurrent | Expires == null | TRUE
        [Test]
        public void TestMenuImageWithoutBoundsIsCurrent() => Assert.That(new APIMenuImage().IsCurrent, Is.True);

        // CF: APIMenuImage.IsCurrent | Begins == null | FALSE
        // CF: APIMenuImage.IsCurrent | Begins < UtcNow | TRUE
        // CF: APIMenuImage.IsCurrent | Expires == null | FALSE
        // CF: APIMenuImage.IsCurrent | Expires > UtcNow | TRUE
        [Test]
        public void TestMenuImageInsideWindowIsCurrent() => Assert.That(new APIMenuImage { Begins = past, Expires = future }.IsCurrent, Is.True);

        // CF: APIMenuImage.IsCurrent | Begins < UtcNow | FALSE
        [Test]
        public void TestMenuImageNotYetStartedIsNotCurrent() => Assert.That(new APIMenuImage { Begins = future }.IsCurrent, Is.False);

        // CF: APIMenuImage.IsCurrent | Expires > UtcNow | FALSE
        [Test]
        public void TestMenuImageExpiredIsNotCurrent() => Assert.That(new APIMenuImage { Expires = past }.IsCurrent, Is.False);

        private static APIMenuImage image() => new APIMenuImage { Image = "i", Url = "u", Begins = past, Expires = future };

        // CF: APIMenuImage.Equals | ReferenceEquals(null, other) | TRUE
        [Test]
        public void TestMenuImageNotEqualNull() => Assert.That(image().Equals((APIMenuImage?)null), Is.False);

        // CF: APIMenuImage.Equals | ReferenceEquals(this, other) | TRUE
        [Test]
        public void TestMenuImageEqualSelf()
        {
            var i = image();
            Assert.That(i.Equals(i), Is.True);
        }

        // CF: APIMenuImage.Equals | Image == other.Image | TRUE
        // CF: APIMenuImage.Equals | Url == other.Url | TRUE
        // CF: APIMenuImage.Equals | Begins == other.Begins | TRUE
        // CF: APIMenuImage.Equals | Expires == other.Expires | TRUE
        // CF: APIMenuImage.Equals(object) | other is APIMenuImage | TRUE
        [Test]
        public void TestMenuImageEqualValues()
        {
            Assert.That(image().Equals(image()), Is.True);
            Assert.That(EqualityComparer<object>.Default.Equals(image(), image()), Is.True);
            Assert.That(image().GetHashCode(), Is.EqualTo(image().GetHashCode()));
        }

        // CF: APIMenuImage.Equals | Image == other.Image | FALSE
        [Test]
        public void TestMenuImageDiffersImage() => Assert.That(image().Equals(new APIMenuImage { Image = "x", Url = "u", Begins = past, Expires = future }), Is.False);

        // CF: APIMenuImage.Equals | Url == other.Url | FALSE
        [Test]
        public void TestMenuImageDiffersUrl() => Assert.That(image().Equals(new APIMenuImage { Image = "i", Url = "x", Begins = past, Expires = future }), Is.False);

        // CF: APIMenuImage.Equals | Begins == other.Begins | FALSE
        [Test]
        public void TestMenuImageDiffersBegins() => Assert.That(image().Equals(new APIMenuImage { Image = "i", Url = "u", Begins = null, Expires = future }), Is.False);

        // CF: APIMenuImage.Equals | Expires == other.Expires | FALSE
        // CF: APIMenuImage.Equals(object) | other is APIMenuImage | FALSE
        [Test]
        public void TestMenuImageDiffersExpires()
        {
            Assert.That(image().Equals(new APIMenuImage { Image = "i", Url = "u", Begins = past, Expires = null }), Is.False);
            Assert.That(EqualityComparer<object>.Default.Equals(image(), "x"), Is.False);
        }

        // CF: APIMenuContent.Equals | ReferenceEquals(null, other) | TRUE
        [Test]
        public void TestMenuContentNotEqualNull() => Assert.That(new APIMenuContent().Equals((APIMenuContent?)null), Is.False);

        // CF: APIMenuContent.Equals | ReferenceEquals(this, other) | TRUE
        [Test]
        public void TestMenuContentEqualSelf()
        {
            var c = new APIMenuContent();
            Assert.That(c.Equals(c), Is.True);
        }

        // CF: APIMenuContent.Equals | Images.SequenceEqual(other.Images) | TRUE
        // CF: APIMenuContent.GetHashCode | foreach images | loop n
        // CF: APIMenuContent.Equals(object) | other is APIMenuContent | TRUE
        [Test]
        public void TestMenuContentEqualImages()
        {
            var a = new APIMenuContent { Images = new[] { image(), image() } };
            var b = new APIMenuContent { Images = new[] { image(), image() } };

            Assert.That(a.Equals(b), Is.True);
            Assert.That(EqualityComparer<object>.Default.Equals(a, b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        // CF: APIMenuContent.Equals | Images.SequenceEqual(other.Images) | FALSE
        // CF: APIMenuContent.GetHashCode | foreach images | loop 0
        // CF: APIMenuContent.Equals(object) | other is APIMenuContent | FALSE
        [Test]
        public void TestMenuContentDifferentImages()
        {
            var a = new APIMenuContent { Images = new[] { image() } };

            Assert.That(a.Equals(new APIMenuContent()), Is.False);
            Assert.That(EqualityComparer<object>.Default.Equals(a, "x"), Is.False);
            Assert.That(new APIMenuContent().GetHashCode(), Is.EqualTo(new APIMenuContent().GetHashCode()));
        }

        // CF: APIUserScoreAggregate.CreateScoreInfo | maps fields | no throw
        [Test]
        public void TestUserScoreAggregateCreateScoreInfo()
        {
            var user = new APIUser { Id = 4 };
            var score = new APIUserScoreAggregate { Accuracy = 0.8, PP = 5, TotalScore = 123, User = user, Position = 2 }.CreateScoreInfo();

            Assert.That(score.Accuracy, Is.EqualTo(0.8));
            Assert.That(score.PP, Is.EqualTo(5));
            Assert.That(score.TotalScore, Is.EqualTo(123));
            Assert.That(score.User, Is.SameAs(user));
            Assert.That(score.Position, Is.EqualTo(2));
            Assert.That(score.Mods, Is.Empty);
        }

        // CF: APINotification.json | snake_case mapping | no throw
        [Test]
        public void TestNotificationDeserialisation()
        {
            var n = Newtonsoft.Json.JsonConvert.DeserializeObject<APINotification>("{\"id\":5,\"name\":\"n\",\"object_type\":\"t\",\"object_id\":\"1\",\"source_user_id\":2,\"is_read\":true,\"details\":{\"a\":1}}")!;

            Assert.That(n.Id, Is.EqualTo(5));
            Assert.That(n.ObjectType, Is.EqualTo("t"));
            Assert.That(n.ObjectId, Is.EqualTo("1"));
            Assert.That(n.SourceUserId, Is.EqualTo(2));
            Assert.That(n.IsRead, Is.True);
            Assert.That(n.Details, Is.EqualTo(JObject.Parse("{\"a\":1}")));
        }

        // CF: APIChangelogEntry.json | type enum and fields mapped | no throw
        [Test]
        public void TestChangelogEntryDeserialisation()
        {
            var e = Newtonsoft.Json.JsonConvert.DeserializeObject<APIChangelogEntry>("{\"id\":1,\"github_pull_request_id\":9,\"major\":true,\"title\":\"t\",\"type\":\"fix\"}")!;

            Assert.That(e.GithubPullRequestId, Is.EqualTo(9));
            Assert.That(e.Major, Is.True);
            Assert.That(e.Title, Is.EqualTo("t"));
        }

        // CF: FrameHeader.ctor(ScoreInfo) | copies score values | no throw
        [Test]
        public void TestFrameHeaderFromScore()
        {
            var stats = new Dictionary<HitResult, int> { [HitResult.Great] = 3 };
            var score = new ScoreInfo { TotalScore = 100, Accuracy = 0.5, Combo = 4, MaxCombo = 9, Statistics = stats, TotalScoreWithoutMods = 90, Mods = new[] { new OsuModHidden() } };
            var processorStats = new ScoreProcessorStatistics();

            var header = new FrameHeader(score, processorStats);

            Assert.That(header.TotalScore, Is.EqualTo(100));
            Assert.That(header.Accuracy, Is.EqualTo(0.5));
            Assert.That(header.Combo, Is.EqualTo(4));
            Assert.That(header.MaxCombo, Is.EqualTo(9));
            Assert.That(header.Statistics, Is.EqualTo(stats));
            Assert.That(header.Statistics, Is.Not.SameAs(stats));
            Assert.That(header.Mods, Has.Length.EqualTo(1));
            Assert.That(header.Mods[0].Acronym, Is.EqualTo("HD"));
            Assert.That(header.TotalScoreWithoutMods, Is.EqualTo(90));
            Assert.That(header.ScoreProcessorStatistics, Is.SameAs(processorStats));
        }

        // CF: FrameHeader.ctor(values) | assigns all fields | no throw
        [Test]
        public void TestFrameHeaderFromValues()
        {
            var received = DateTimeOffset.UnixEpoch;
            var mods = new[] { new APIMod { Acronym = "DT" } };
            var header = new FrameHeader(1, 0.2, 3, 4, new Dictionary<HitResult, int>(), new ScoreProcessorStatistics(), received, mods, 5, new[] { 6 });

            Assert.That(header.ReceivedTime, Is.EqualTo(received));
            Assert.That(header.Mods, Is.SameAs(mods));
            Assert.That(header.TotalScoreWithoutMods, Is.EqualTo(5));
            Assert.That(header.Pauses, Is.EqualTo(new[] { 6 }));
        }
    }
}
