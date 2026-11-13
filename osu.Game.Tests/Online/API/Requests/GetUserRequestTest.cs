// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Online.API.Requests;
using osu.Game.Rulesets.Osu;

namespace osu.Game.Tests.Online.API.Requests
{
    [TestFixture]
    public class GetUserRequestTest
    {
        // CF: GetUserRequest.Target | lookup by id with ruleset | no throw
        [Test]
        public void TestById() => Assert.That(RequestInspector.GetTarget(new GetUserRequest(7, new OsuRuleset().RulesetInfo)), Is.EqualTo("users/7/osu?key=id"));

        // CF: GetUserRequest.Target | Ruleset?.ShortName | null
        [Test]
        public void TestByIdWithoutRuleset() => Assert.That(RequestInspector.GetTarget(new GetUserRequest(7)), Is.EqualTo("users/7/?key=id"));

        // CF: GetUserRequest.Target | lookup by username | no throw
        [Test]
        public void TestByUsername() => Assert.That(RequestInspector.GetTarget(new GetUserRequest("peppy")), Is.EqualTo("users/peppy/?key=username"));
    }
}
