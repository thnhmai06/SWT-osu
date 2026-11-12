// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.Scoring;
using static osu.Game.Rulesets.Osu.Tests.UI.HitPolicyTestHelpers;

namespace osu.Game.Rulesets.Osu.Tests.UI
{
    [TestFixture]
    public class AnyOrderHitPolicyTest
    {
        [TestCase(HitResult.Great)]
        [TestCase(HitResult.None)]
        public void TestCheckHittable_AlwaysHits(HitResult result)
        {
            // single straight-line path; even an unjudged earlier object or HitResult.None never blocks
            var target = Circle(1000);
            var policy = new AnyOrderHitPolicy { HitObjectContainer = Container(Circle(500), target) };

            Assert.That(policy.CheckHittable(target, 0, result), Is.EqualTo(ClickAction.Hit));
        }

        [Test]
        public void TestHandleHit_DoesNothing()
        {
            var earlier = Circle(500);
            var target = Circle(1000, judged: true);
            var policy = new AnyOrderHitPolicy { HitObjectContainer = Container(earlier, target) };

            policy.HandleHit(target);

            Assert.That(earlier.Judged, Is.False);
        }
    }
}
