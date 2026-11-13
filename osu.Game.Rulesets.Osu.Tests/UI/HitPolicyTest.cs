// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Osu.Tests.UI
{
    [TestFixture]
    public class HitPolicyTest
    {
        // CF: LegacyHitPolicy.CheckHittable | HitObjectContainer == null | TRUE
        [Test]
        public void TestLegacyPolicyRequiresContainer()
        {
            Assert.Throws<InvalidOperationException>(() => new LegacyHitPolicy().CheckHittable(null!, 0, HitResult.Great));
        }

        // CF: StartTimeOrderedHitPolicy.CheckHittable | HitObjectContainer == null | TRUE
        [Test]
        public void TestStartTimeOrderedPolicyRequiresContainer()
        {
            Assert.Throws<InvalidOperationException>(() => new StartTimeOrderedHitPolicy().CheckHittable(null!, 0, HitResult.Great));
        }
    }
}
