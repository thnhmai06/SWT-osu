// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Newtonsoft.Json;
using NUnit.Framework;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Scoring;
using osu.Game.Users;

namespace osu.Game.Tests.Users
{
    [TestFixture]
    public class UserModelsTest
    {
        // CF: UserStatistics.Grades.this[] | switch rank | case XH
        [Test]
        public void TestGradeXh() => Assert.That(new UserStatistics.Grades { SSPlus = 4 }[ScoreRank.XH], Is.EqualTo(4));

        // CF: UserStatistics.Grades.this[] | SSPlus ?? 0 | null
        [Test]
        public void TestGradeXhMissingIsZero() => Assert.That(new UserStatistics.Grades()[ScoreRank.XH], Is.EqualTo(0));

        // CF: UserStatistics.Grades.this[] | switch rank | case X
        [Test]
        public void TestGradeX() => Assert.That(new UserStatistics.Grades { SS = 3 }[ScoreRank.X], Is.EqualTo(3));

        // CF: UserStatistics.Grades.this[] | switch rank | case SH
        // CF: UserStatistics.Grades.this[] | SPlus ?? 0 | non-null
        [Test]
        public void TestGradeSh() => Assert.That(new UserStatistics.Grades { SPlus = 5 }[ScoreRank.SH], Is.EqualTo(5));

        // CF: UserStatistics.Grades.this[] | SPlus ?? 0 | null
        [Test]
        public void TestGradeShMissingIsZero() => Assert.That(new UserStatistics.Grades()[ScoreRank.SH], Is.EqualTo(0));

        // CF: UserStatistics.Grades.this[] | switch rank | case S
        [Test]
        public void TestGradeS() => Assert.That(new UserStatistics.Grades { S = 6 }[ScoreRank.S], Is.EqualTo(6));

        // CF: UserStatistics.Grades.this[] | switch rank | case A
        [Test]
        public void TestGradeA() => Assert.That(new UserStatistics.Grades { A = 7 }[ScoreRank.A], Is.EqualTo(7));

        // CF: UserStatistics.Grades.this[] | switch rank | default
        // CF: UserStatistics.Grades.this[] | unsupported rank | throws ArgumentException
        [Test]
        public void TestGradeUnsupportedThrows()
        {
            Assert.Throws<ArgumentException>(() => _ = new UserStatistics.Grades()[ScoreRank.B]);
            Assert.Throws<ArgumentException>(() => _ = new UserStatistics.Grades()[ScoreRank.D]);
        }

        // CF: IUser.Equals | other == null | TRUE
        [Test]
        public void TestUserEqualsNull() => Assert.That(((IUser)new APIUser { Id = 1 }).Equals(null), Is.False);

        // CF: IUser.Equals | OnlineID == other.OnlineID | TRUE
        // CF: IUser.Equals | Username == other.Username | TRUE
        [Test]
        public void TestUserEqualsSameIdAndName()
            => Assert.That(((IUser)new APIUser { Id = 1, Username = "a" }).Equals(new SpectatorUserStub(1, "a")), Is.True);

        // CF: IUser.Equals | OnlineID == other.OnlineID | FALSE
        [Test]
        public void TestUserDifferentId()
            => Assert.That(((IUser)new APIUser { Id = 1, Username = "a" }).Equals(new SpectatorUserStub(2, "a")), Is.False);

        // CF: IUser.Equals | Username == other.Username | FALSE
        [Test]
        public void TestUserDifferentName()
            => Assert.That(((IUser)new APIUser { Id = 1, Username = "a" }).Equals(new SpectatorUserStub(1, "b")), Is.False);

        private class SpectatorUserStub : IUser
        {
            public SpectatorUserStub(int id, string name)
            {
                OnlineID = id;
                Username = name;
            }

            public int OnlineID { get; }
            public string Username { get; }
            public CountryCode CountryCode => CountryCode.Unknown;
            public bool IsBot => false;
        }

        // CF: UserStatus.GetAppropriateColour | switch userStatus | case Offline
        [Test]
        public void TestOfflineColourIsBlack() => Assert.That(UserStatus.Offline.GetAppropriateColour(null!), Is.EqualTo(osuTK.Graphics.Color4.Black));

        // CF: UserStatus.GetAppropriateColour | switch userStatus | default
        // CF: UserStatus.GetAppropriateColour | unsupported status | throws ArgumentOutOfRangeException
        [Test]
        public void TestUnknownStatusThrows() => Assert.Throws<ArgumentOutOfRangeException>(() => ((UserStatus)99).GetAppropriateColour(null!));

        // CF: APIUser.CountryCode | Enum.TryParse | TRUE
        [Test]
        public void TestCountryCodeParsed() => Assert.That(JsonConvert.DeserializeObject<APIUser>("{\"country_code\":\"JP\"}")!.CountryCode, Is.EqualTo(CountryCode.JP));

        // CF: APIUser.CountryCode | Enum.TryParse | FALSE
        [Test]
        public void TestCountryCodeUnknownFallback()
        {
            Assert.That(JsonConvert.DeserializeObject<APIUser>("{\"country_code\":\"zz-nope\"}")!.CountryCode, Is.EqualTo(CountryCode.Unknown));
            Assert.That(new APIUser().CountryCode, Is.EqualTo(CountryCode.Unknown));
        }

        // CF: APIUser.CountryCode | setter stores name | no throw
        [Test]
        public void TestCountryCodeRoundTrips() => Assert.That(new APIUser { CountryCode = CountryCode.US }.CountryCode, Is.EqualTo(CountryCode.US));

        // CF: APIUser.Id | default 1 for deleted users | no throw
        [Test]
        public void TestNullIdDeserialisesAsDeletedUser()
        {
            Assert.That(JsonConvert.DeserializeObject<APIUser>("{\"id\":null}")!.Id, Is.EqualTo(1));
            Assert.That(JsonConvert.DeserializeObject<APIUser>("{\"id\":5}")!.Id, Is.EqualTo(5));
        }

        // CF: APIUser.CoverUrl | Cover?.Url | null
        // CF: APIUser.CoverUrl | Cover?.Url | non-null
        [Test]
        public void TestCoverUrl()
        {
            var user = new APIUser();
            Assert.That(user.CoverUrl, Is.Null);

            user.CoverUrl = "http://x/cover.jpg";
            Assert.That(user.Cover.Url, Is.EqualTo("http://x/cover.jpg"));
            Assert.That(user.CoverUrl, Is.EqualTo("http://x/cover.jpg"));
        }

        // CF: APIUser.playStyle | value?.Select(Enum.Parse) | non-null
        [Test]
        public void TestPlayStylesParsedCaseInsensitively()
        {
            var user = JsonConvert.DeserializeObject<APIUser>("{\"playstyle\":[\"mouse\",\"KEYBOARD\"]}")!;

            Assert.That(user.PlayStyles, Is.EqualTo(new[] { APIPlayStyle.Mouse, APIPlayStyle.Keyboard }));
        }

        // CF: APIUser.playStyle | value?.Select(Enum.Parse) | null
        [Test]
        public void TestNullPlayStyle() => Assert.That(JsonConvert.DeserializeObject<APIUser>("{\"playstyle\":null}")!.PlayStyles, Is.Null);

        // CF: APIUser.Statistics | statistics ??= new UserStatistics() | non-null
        [Test]
        public void TestStatisticsLazilyCreated()
        {
            var user = new APIUser();

            Assert.That(user.Statistics, Is.Not.Null);
            Assert.That(user.Statistics, Is.SameAs(user.Statistics));
        }

        // CF: APIUser.Statistics | statistics != null | FALSE
        [Test]
        public void TestStatisticsSetterWithoutExistingKeepsValue()
        {
            var stats = new UserStatistics();
            var user = new APIUser { Statistics = stats };

            Assert.That(user.Statistics, Is.SameAs(stats));
        }

        // CF: APIUser.Statistics | statistics != null | TRUE
        // CF: APIUser.rankHistory | setter writes to Statistics.RankHistory | no throw
        [Test]
        public void TestReplacingStatisticsKeepsRankHistory()
        {
            var user = JsonConvert.DeserializeObject<APIUser>("{\"rank_history\":{\"mode\":\"osu\",\"data\":[3,2,1]}}")!;
            var history = user.Statistics.RankHistory;

            user.Statistics = new UserStatistics();

            Assert.That(history.Data, Is.EqualTo(new[] { 3, 2, 1 }));
            Assert.That(user.Statistics.RankHistory, Is.SameAs(history));
        }

        // CF: APIUser.ToString | returns Username | no throw
        // CF: APIUser.UnknownUser | creates placeholder | no throw
        // CF: APIUser.SYSTEM_USER | constant | no throw
        [Test]
        public void TestWellKnownUsers()
        {
            Assert.That(new APIUser { Username = "peppy" }.ToString(), Is.EqualTo("peppy"));
            Assert.That(APIUser.UnknownUser(9).Id, Is.EqualTo(9));
            Assert.That(APIUser.UnknownUser(9).Username, Is.EqualTo("Unknown user"));
            Assert.That(APIUser.SYSTEM_USER.Id, Is.EqualTo(APIUser.SYSTEM_USER_ID));
            Assert.That(APIUser.SYSTEM_USER.Username, Is.EqualTo("system"));
        }

        // CF: APIUser.Equals | MatchesOnlineID(other) | TRUE
        // CF: APIUser.Equals | MatchesOnlineID(other) | FALSE
        [Test]
        public void TestApiUserEqualsByOnlineId()
        {
            Assert.That(new APIUser { Id = 3 }.Equals(new APIUser { Id = 3, Username = "x" }), Is.True);
            Assert.That(new APIUser { Id = 3 }.Equals(new APIUser { Id = 4 }), Is.False);
            Assert.That(new APIUser { Id = 3 }.OnlineID, Is.EqualTo(3));
        }
    }
}
