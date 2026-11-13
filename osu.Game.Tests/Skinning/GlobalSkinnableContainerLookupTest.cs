// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Rulesets;
using osu.Game.Skinning;

namespace osu.Game.Tests.Skinning
{
    [TestFixture]
    public class GlobalSkinnableContainerLookupTest
    {
        private static RulesetInfo ruleset(string shortName) => new RulesetInfo { ShortName = shortName, Name = shortName, OnlineID = 0 };

        // CF: GlobalSkinnableContainerLookup.ToString | Ruleset == null | TRUE
        [Test]
        public void TestToStringGlobal()
        {
            Assert.That(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.SongSelect).ToString(), Is.EqualTo("Song select"));
        }

        // CF: GlobalSkinnableContainerLookup.ToString | Ruleset == null | FALSE
        [Test]
        public void TestToStringRulesetSpecific()
        {
            Assert.That(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.MainHUDComponents, ruleset("osu")).ToString(), Is.EqualTo("HUD (\"osu\" only)"));
        }

        // CF: GlobalSkinnableContainerLookup.Equals | ReferenceEquals(null, other) | TRUE
        [Test]
        public void TestEqualsNull()
        {
            Assert.That(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.Playfield).Equals((GlobalSkinnableContainerLookup?)null), Is.False);
        }

        // CF: GlobalSkinnableContainerLookup.Equals | ReferenceEquals(null, other) | FALSE
        // CF: GlobalSkinnableContainerLookup.Equals | ReferenceEquals(this, other) | TRUE
        [Test]
        public void TestEqualsSelf()
        {
            var l = new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.Playfield);
            Assert.That(l.Equals(l), Is.True);
        }

        // CF: GlobalSkinnableContainerLookup.Equals | ReferenceEquals(this, other) | FALSE
        // CF: GlobalSkinnableContainerLookup.Equals | Lookup == other.Lookup | TRUE
        // CF: GlobalSkinnableContainerLookup.Equals | ReferenceEquals(Ruleset, other.Ruleset) | TRUE
        [Test]
        public void TestEqualsBothGlobal()
        {
            Assert.That(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.Playfield).Equals(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.Playfield)), Is.True);
        }

        // CF: GlobalSkinnableContainerLookup.Equals | Lookup == other.Lookup | FALSE
        [Test]
        public void TestEqualsDifferentLookup()
        {
            Assert.That(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.Playfield).Equals(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.SongSelect)), Is.False);
        }

        // CF: GlobalSkinnableContainerLookup.Equals | ReferenceEquals(Ruleset, other.Ruleset) | FALSE
        // CF: GlobalSkinnableContainerLookup.Equals | Ruleset?.Equals(other.Ruleset) == true | TRUE
        // CF: GlobalSkinnableContainerLookup.Equals | Ruleset?.Equals(other.Ruleset) == true | FALSE
        [Test]
        public void TestEqualsRulesetByValue()
        {
            var a = new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.Playfield, ruleset("osu"));
            Assert.That(a.Equals(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.Playfield, ruleset("osu"))), Is.True);
            Assert.That(a.Equals(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.Playfield, ruleset("taiko"))), Is.False);
            Assert.That(a.Equals(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.Playfield)), Is.False);
        }

        // CF: GlobalSkinnableContainerLookup.Equals(object) | ReferenceEquals(null, obj) | TRUE
        // CF: GlobalSkinnableContainerLookup.Equals(object) | ReferenceEquals(this, obj) | TRUE
        // CF: GlobalSkinnableContainerLookup.Equals(object) | obj.GetType() != GetType() | TRUE
        // CF: GlobalSkinnableContainerLookup.Equals(object) | obj.GetType() != GetType() | FALSE
        [Test]
        public void TestEqualsObject()
        {
            var l = new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.Playfield);
            Assert.That(EqualityComparer<object?>.Default.Equals(l, null), Is.False);
            Assert.That(EqualityComparer<object?>.Default.Equals(l, (object)l), Is.True);
            Assert.That(EqualityComparer<object?>.Default.Equals(l, "string"), Is.False);
            Assert.That(EqualityComparer<object?>.Default.Equals(l, new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.Playfield)), Is.True);
        }

        [Test]
        public void TestHashCodeConsistentWithEquality()
        {
            Assert.That(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.Playfield).GetHashCode(),
                Is.EqualTo(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.Playfield).GetHashCode()));
        }
    }
}
