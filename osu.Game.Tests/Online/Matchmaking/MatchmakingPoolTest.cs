// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Online.Matchmaking;

namespace osu.Game.Tests.Online.Matchmaking
{
    [TestFixture]
    public class MatchmakingPoolTest
    {
        // CF: MatchmakingPool.DisplayName | switch RulesetId | case 0
        [Test]
        public void TestOsu() => Assert.That(new MatchmakingPool { RulesetId = 0, Name = "n" }.DisplayName, Is.EqualTo("osu! (n)"));

        // CF: MatchmakingPool.DisplayName | switch RulesetId | case 1
        [Test]
        public void TestTaiko() => Assert.That(new MatchmakingPool { RulesetId = 1, Name = "n" }.DisplayName, Is.EqualTo("osu!taiko (n)"));

        // CF: MatchmakingPool.DisplayName | switch RulesetId | case 2
        [Test]
        public void TestCatch() => Assert.That(new MatchmakingPool { RulesetId = 2, Name = "n" }.DisplayName, Is.EqualTo("osu!catch (n)"));

        // CF: MatchmakingPool.DisplayName | switch RulesetId | case 3
        [Test]
        public void TestMania() => Assert.That(new MatchmakingPool { RulesetId = 3, Variant = 7, Name = "n" }.DisplayName, Is.EqualTo("osu!mania 7K (n)"));

        // CF: MatchmakingPool.DisplayName | switch RulesetId | default
        [Test]
        public void TestUnknownRulesetUsesName() => Assert.That(new MatchmakingPool { RulesetId = 9, Name = "custom" }.DisplayName, Is.EqualTo("custom"));

        private static MatchmakingPool create() => new MatchmakingPool { Id = 1, RulesetId = 2, Variant = 3, Name = "n" };

        // CF: MatchmakingPool.Equals | other != null | TRUE
        // CF: MatchmakingPool.Equals | Id == other.Id | TRUE
        // CF: MatchmakingPool.Equals | RulesetId == other.RulesetId | TRUE
        // CF: MatchmakingPool.Equals | Variant == other.Variant | TRUE
        // CF: MatchmakingPool.Equals | Name == other.Name | TRUE
        // CF: MatchmakingPool.Equals(object) | obj is MatchmakingPool | TRUE
        [Test]
        public void TestEqual()
        {
            Assert.That(create().Equals(create()), Is.True);
            Assert.That(System.Collections.Generic.EqualityComparer<object>.Default.Equals(create(), create()), Is.True);
            Assert.That(create().GetHashCode(), Is.EqualTo(create().GetHashCode()));
        }

        // CF: MatchmakingPool.Equals | other != null | FALSE
        // CF: MatchmakingPool.Equals(object) | obj is MatchmakingPool | FALSE
        [Test]
        public void TestNotEqualNullOrOtherType()
        {
            Assert.That(create().Equals((MatchmakingPool?)null), Is.False);
            Assert.That(System.Collections.Generic.EqualityComparer<object>.Default.Equals(create(), "x"), Is.False);
        }

        // CF: MatchmakingPool.Equals | Id == other.Id | FALSE
        [Test]
        public void TestDiffersId() => Assert.That(create().Equals(new MatchmakingPool { Id = 9, RulesetId = 2, Variant = 3, Name = "n" }), Is.False);

        // CF: MatchmakingPool.Equals | RulesetId == other.RulesetId | FALSE
        [Test]
        public void TestDiffersRuleset() => Assert.That(create().Equals(new MatchmakingPool { Id = 1, RulesetId = 9, Variant = 3, Name = "n" }), Is.False);

        // CF: MatchmakingPool.Equals | Variant == other.Variant | FALSE
        [Test]
        public void TestDiffersVariant() => Assert.That(create().Equals(new MatchmakingPool { Id = 1, RulesetId = 2, Variant = 9, Name = "n" }), Is.False);

        // CF: MatchmakingPool.Equals | Name == other.Name | FALSE
        [Test]
        public void TestDiffersName() => Assert.That(create().Equals(new MatchmakingPool { Id = 1, RulesetId = 2, Variant = 3, Name = "z" }), Is.False);
    }
}
