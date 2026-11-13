// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Online.API.Requests;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Play.Leaderboards;

namespace osu.Game.Tests.Online.API.Requests
{
    [TestFixture]
    public class GetScoresRequestTest
    {
        private static readonly BeatmapInfo beatmap = new BeatmapInfo { OnlineID = 5 };
        private static readonly OsuRuleset ruleset = new OsuRuleset();

        // CF: GetScoresRequest.ctor | beatmapInfo.OnlineID <= 0 | TRUE
        // CF: GetScoresRequest.ctor | online id missing | throws InvalidOperationException
        [Test]
        public void TestUnsubmittedBeatmapThrows()
            => Assert.Throws<InvalidOperationException>(() => new GetScoresRequest(new BeatmapInfo { OnlineID = -1 }, ruleset.RulesetInfo));

        // CF: GetScoresRequest.ctor | beatmapInfo.OnlineID <= 0 | FALSE
        // CF: GetScoresRequest.ctor | scope == Local | TRUE
        // CF: GetScoresRequest.ctor | local scope | throws InvalidOperationException
        [Test]
        public void TestLocalScopeThrows()
            => Assert.Throws<InvalidOperationException>(() => new GetScoresRequest(beatmap, ruleset.RulesetInfo, BeatmapLeaderboardScope.Local));

        // CF: GetScoresRequest.ctor | scope == Local | FALSE
        // CF: GetScoresRequest.ctor | mods ?? Array.Empty | null
        // CF: GetScoresRequest.ctor | RequiresSupporter | FALSE
        [Test]
        public void TestDefaultScoresRequested()
        {
            var request = new GetScoresRequest(beatmap, ruleset.RulesetInfo);

            Assert.That(request.ScoresRequested, Is.EqualTo(GetScoresRequest.DEFAULT_SCORES_PER_REQUEST));
            Assert.That(RequestInspector.GetUri(request), Is.EqualTo("http://api.test/api/v2/beatmaps/5/scores"));
        }

        // CF: GetScoresRequest.ctor | mods ?? Array.Empty | non-null
        // CF: GetScoresRequest.ctor | RequiresSupporter | TRUE
        [Test]
        public void TestSupporterScopeRequestsMaximum()
        {
            var request = new GetScoresRequest(beatmap, ruleset.RulesetInfo, BeatmapLeaderboardScope.Friend, new Mod[] { new OsuModHidden() });

            Assert.That(request.ScoresRequested, Is.EqualTo(GetScoresRequest.MAX_SCORES_PER_REQUEST));
        }

        // CF: GetScoresRequest.CreateWebRequest | foreach mods | loop n
        [Test]
        public void TestParameters()
        {
            var request = new GetScoresRequest(beatmap, ruleset.RulesetInfo, BeatmapLeaderboardScope.Country, new Mod[] { new OsuModHidden(), new OsuModHardRock() });

            Assert.That(RequestInspector.Values(request, "type"), Is.EqualTo(new[] { "country" }));
            Assert.That(RequestInspector.Values(request, "mode"), Is.EqualTo(new[] { "osu" }));
            Assert.That(RequestInspector.Values(request, "mods[]"), Is.EqualTo(new[] { "HD", "HR" }));
            Assert.That(RequestInspector.Values(request, "limit"), Is.EqualTo(new[] { "100" }));
        }

        // CF: GetScoresRequest.CreateWebRequest | foreach mods | loop 0
        [Test]
        public void TestParametersWithoutMods()
        {
            var request = new GetScoresRequest(beatmap, ruleset.RulesetInfo);

            Assert.That(RequestInspector.Values(request, "mods[]"), Is.Empty);
            Assert.That(RequestInspector.Values(request, "type"), Is.EqualTo(new[] { "global" }));
            Assert.That(RequestInspector.Values(request, "limit"), Is.EqualTo(new[] { "50" }));
        }

        // CF: GetScoresRequest.Equals | other == null | FALSE
        [Test]
        public void TestEqualsNull() => Assert.That(new GetScoresRequest(beatmap, ruleset.RulesetInfo).Equals(null), Is.False);

        // CF: GetScoresRequest.Equals | ReferenceEquals(this, other) | TRUE
        [Test]
        public void TestEqualsSelf()
        {
            var request = new GetScoresRequest(beatmap, ruleset.RulesetInfo);
            Assert.That(request.Equals(request), Is.True);
        }

        // CF: GetScoresRequest.Equals | beatmapInfo.Equals | TRUE
        // CF: GetScoresRequest.Equals | scope == other.scope | TRUE
        // CF: GetScoresRequest.Equals | ruleset.Equals | TRUE
        // CF: GetScoresRequest.Equals | mods.SequenceEqual | TRUE
        [Test]
        public void TestEqualsSameInputs()
        {
            var a = new GetScoresRequest(beatmap, ruleset.RulesetInfo, mods: new Mod[] { new OsuModHidden() });
            var b = new GetScoresRequest(beatmap, ruleset.RulesetInfo, mods: new Mod[] { new OsuModHidden() });

            Assert.That(a.Equals(b), Is.True);
        }

        // CF: GetScoresRequest.Equals | beatmapInfo.Equals | FALSE
        [Test]
        public void TestNotEqualsDifferentBeatmap()
            => Assert.That(new GetScoresRequest(beatmap, ruleset.RulesetInfo).Equals(new GetScoresRequest(new BeatmapInfo { OnlineID = 6 }, ruleset.RulesetInfo)), Is.False);

        // CF: GetScoresRequest.Equals | scope == other.scope | FALSE
        [Test]
        public void TestNotEqualsDifferentScope()
            => Assert.That(new GetScoresRequest(beatmap, ruleset.RulesetInfo).Equals(new GetScoresRequest(beatmap, ruleset.RulesetInfo, BeatmapLeaderboardScope.Friend)), Is.False);

        // CF: GetScoresRequest.Equals | ruleset.Equals | FALSE
        [Test]
        public void TestNotEqualsDifferentRuleset()
            => Assert.That(new GetScoresRequest(beatmap, ruleset.RulesetInfo).Equals(new GetScoresRequest(beatmap, new osu.Game.Rulesets.Taiko.TaikoRuleset().RulesetInfo)), Is.False);

        // CF: GetScoresRequest.Equals | mods.SequenceEqual | FALSE
        [Test]
        public void TestNotEqualsDifferentMods()
            => Assert.That(new GetScoresRequest(beatmap, ruleset.RulesetInfo).Equals(new GetScoresRequest(beatmap, ruleset.RulesetInfo, mods: new Mod[] { new OsuModHidden() })), Is.False);
    }
}
