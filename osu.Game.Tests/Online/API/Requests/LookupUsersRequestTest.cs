// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Online.API.Requests;

namespace osu.Game.Tests.Online.API.Requests
{
    [TestFixture]
    public class LookupUsersRequestTest
    {
        // CF: LookupUsersRequest.ctor | userIds.Length > max_ids_per_request | TRUE
        // CF: LookupUsersRequest.ctor | too many ids | throws ArgumentException
        [Test]
        public void TestTooManyIdsThrows() => Assert.Throws<ArgumentException>(() => new LookupUsersRequest(Enumerable.Range(1, 51).ToArray()));

        // CF: LookupUsersRequest.ctor | userIds.Length > max_ids_per_request | FALSE
        // CF: LookupUsersRequest.Target | RulesetId != null | FALSE
        [Test]
        public void TestTargetWithoutRuleset() => Assert.That(RequestInspector.GetTarget(new LookupUsersRequest(new[] { 1, 2, 3 })), Is.EqualTo("users/lookup/?ids[]=1&ids[]=2&ids[]=3"));

        // CF: LookupUsersRequest.Target | RulesetId != null | TRUE
        [Test]
        public void TestTargetWithRuleset() => Assert.That(RequestInspector.GetTarget(new LookupUsersRequest(Enumerable.Range(1, 50).Take(1).ToArray(), 2)), Is.EqualTo("users/lookup/?ids[]=1&ruleset_id=2"));
    }
}
