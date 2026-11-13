// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Models;
using osu.Game.Users;

namespace osu.Game.Tests.Models
{
    [TestFixture]
    public class RealmUserTest
    {
        [Test]
        public void TestDefaults()
        {
            var user = new RealmUser();

            Assert.That((user.OnlineID, user.Username, user.IsBot, user.CountryCode), Is.EqualTo((1, string.Empty, false, CountryCode.Unknown)));
        }

        // CF: RealmUser.CountryCode | Enum.TryParse(CountryString, out country) | TRUE
        [Test]
        public void TestCountryCodeRoundTripsThroughString()
        {
            var user = new RealmUser { CountryCode = CountryCode.JP };

            Assert.That(user.CountryString, Is.EqualTo("JP"));
            Assert.That(user.CountryCode, Is.EqualTo(CountryCode.JP));
        }

        // CF: RealmUser.CountryCode | Enum.TryParse(CountryString, out country) | FALSE
        [Test]
        public void TestUnparsableCountryStringIsUnknown()
        {
            Assert.That(new RealmUser { CountryString = "not a country" }.CountryCode, Is.EqualTo(CountryCode.Unknown));
        }

        // CF: RealmUser.Equals | ReferenceEquals(null, other) | TRUE
        [Test]
        public void TestNotEqualToNull()
        {
            Assert.That(new RealmUser().Equals(null), Is.False);
        }

        // CF: RealmUser.Equals | ReferenceEquals(this, other) | TRUE
        [Test]
        public void TestEqualToSelf()
        {
            var user = new RealmUser { OnlineID = 5, Username = "a" };

            Assert.That(user.Equals(user), Is.True);
        }

        // CF: RealmUser.Equals | OnlineID == other.OnlineID && Username == other.Username | TRUE
        // CF: RealmUser.Equals | OnlineID == other.OnlineID | FALSE
        // CF: RealmUser.Equals | Username == other.Username | FALSE
        [Test]
        public void TestEqualityComparesIdAndUsername()
        {
            var user = new RealmUser { OnlineID = 5, Username = "a" };

            Assert.That(user.Equals(new RealmUser { OnlineID = 5, Username = "a", CountryCode = CountryCode.US }), Is.True);
            Assert.That(user.Equals(new RealmUser { OnlineID = 6, Username = "a" }), Is.False);
            Assert.That(user.Equals(new RealmUser { OnlineID = 5, Username = "b" }), Is.False);
        }

        [Test]
        public void TestDeepCloneCopiesValues()
        {
            var user = new RealmUser { OnlineID = 5, Username = "a", CountryCode = CountryCode.DE };

            var clone = user.DeepClone();

            Assert.That(clone, Is.Not.SameAs(user));
            Assert.That(clone.Equals(user), Is.True);
            Assert.That(clone.CountryCode, Is.EqualTo(CountryCode.DE));
        }
    }
}
