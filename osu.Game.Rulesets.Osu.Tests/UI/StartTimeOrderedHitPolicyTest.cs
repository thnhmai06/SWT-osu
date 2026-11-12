// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.Scoring;
using static osu.Game.Rulesets.Osu.Tests.UI.HitPolicyTestHelpers;

namespace osu.Game.Rulesets.Osu.Tests.UI
{
    [TestFixture]
    public class StartTimeOrderedHitPolicyTest
    {
        // ---- CheckHittable ----

        [Test]
        public void TestCheckHittable_NoContainer_Throws()
        {
            // HitObjectContainer == null TRUE
            Assert.Throws<InvalidOperationException>(() => new StartTimeOrderedHitPolicy().CheckHittable(Circle(1000), 1000, HitResult.Great));
        }

        [Test]
        public void TestCheckHittable_NoEarlierObjects_Hits()
        {
            // blockingObject == null; result != None
            var target = Circle(1000);
            var policy = new StartTimeOrderedHitPolicy { HitObjectContainer = Container(target) };

            Assert.That(policy.CheckHittable(target, 1000, HitResult.Great), Is.EqualTo(ClickAction.Hit));
        }

        [Test]
        public void TestCheckHittable_EarlierUnjudgedCircleAndTimeBeforeItsStart_Shakes()
        {
            // blockingObject != null; !Judged TRUE && time < StartTime TRUE
            var target = Circle(1000);
            var policy = new StartTimeOrderedHitPolicy { HitObjectContainer = Container(Circle(500), target) };

            Assert.That(policy.CheckHittable(target, 400, HitResult.Great), Is.EqualTo(ClickAction.Shake));
        }

        [Test]
        public void TestCheckHittable_EarlierUnjudgedCircleButTimeReachedItsStart_Hits()
        {
            // !Judged TRUE && time < StartTime FALSE (time == StartTime is allowed)
            var target = Circle(1000);
            var policy = new StartTimeOrderedHitPolicy { HitObjectContainer = Container(Circle(500), target) };

            Assert.That(policy.CheckHittable(target, 500, HitResult.Great), Is.EqualTo(ClickAction.Hit));
        }

        [Test]
        public void TestCheckHittable_EarlierCircleAlreadyJudged_Hits()
        {
            // !Judged FALSE (short-circuits the time comparison)
            var target = Circle(1000);
            var policy = new StartTimeOrderedHitPolicy { HitObjectContainer = Container(Circle(500, judged: true), target) };

            Assert.That(policy.CheckHittable(target, 400, HitResult.Great), Is.EqualTo(ClickAction.Hit));
        }

        [Test]
        public void TestCheckHittable_EarlierObjectCannotBlock_Hits()
        {
            // hitObjectCanBlockFutureHits FALSE -> blockingObject stays null
            var target = Circle(1000);
            var policy = new StartTimeOrderedHitPolicy { HitObjectContainer = Container(NonBlocking(500), target) };

            Assert.That(policy.CheckHittable(target, 400, HitResult.Great), Is.EqualTo(ClickAction.Hit));
        }

        [Test]
        public void TestCheckHittable_LastBlockingObjectDecides()
        {
            // loop keeps overwriting blockingObject: the later (judged) circle wins over the earlier unjudged one
            var target = Circle(1000);
            var policy = new StartTimeOrderedHitPolicy
            {
                HitObjectContainer = Container(Circle(400), Circle(600, judged: true), target)
            };

            Assert.That(policy.CheckHittable(target, 300, HitResult.Great), Is.EqualTo(ClickAction.Hit));
        }

        [Test]
        public void TestCheckHittable_ObjectAtSameStartTimeDoesNotBlock()
        {
            // enumeration stops at StartTime >= target start (yield break), so a simultaneous unjudged circle is ignored
            var target = Circle(1000);
            var policy = new StartTimeOrderedHitPolicy { HitObjectContainer = Container(Circle(1000), target) };

            Assert.That(policy.CheckHittable(target, 900, HitResult.Great), Is.EqualTo(ClickAction.Hit));
        }

        [Test]
        public void TestCheckHittable_ResultNone_Shakes()
        {
            // result == None TRUE (no blocking object)
            var target = Circle(1000);
            var policy = new StartTimeOrderedHitPolicy { HitObjectContainer = Container(target) };

            Assert.That(policy.CheckHittable(target, 100, HitResult.None), Is.EqualTo(ClickAction.Shake));
        }

        // ---- HandleHit ----

        [Test]
        public void TestHandleHit_NoContainer_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new StartTimeOrderedHitPolicy().HandleHit(Circle(1000)));
        }

        [Test]
        public void TestHandleHit_ObjectCannotBlock_ReturnsWithoutChecking()
        {
            // !hitObjectCanBlockFutureHits TRUE -> early return; the unjudged result (None) would otherwise make CheckHittable shake and throw
            var target = NonBlocking(1000);
            var policy = new StartTimeOrderedHitPolicy { HitObjectContainer = Container(Circle(500, judged: true), target) };

            Assert.DoesNotThrow(() => policy.HandleHit(target));
        }

        [Test]
        public void TestHandleHit_HitBeforeHittable_Throws()
        {
            // CheckHittable != Hit TRUE: the circle has no result yet (HitResult.None -> Shake)
            var target = Circle(1000);
            var policy = new StartTimeOrderedHitPolicy { HitObjectContainer = Container(target) };

            Assert.Throws<InvalidOperationException>(() => policy.HandleHit(target));
        }

        [Test]
        public void TestHandleHit_HittableWithAllEarlierObjectsJudged_DoesNotTouchThem()
        {
            // CheckHittable != Hit FALSE; loop: obj.Judged TRUE -> continue (no forced miss)
            var earlier = Circle(500, judged: true);
            var target = Circle(1000, judged: true);
            var policy = new StartTimeOrderedHitPolicy { HitObjectContainer = Container(earlier, target) };

            Assert.DoesNotThrow(() => policy.HandleHit(target));
            Assert.That(earlier.Result.Type, Is.EqualTo(HitResult.Great));
        }

        [Test]
        public void TestHandleHit_EarlierObjectCannotBlock_IsNotMissed()
        {
            // loop: Judged FALSE, hitObjectCanBlockFutureHits FALSE -> not missed
            var earlier = NonBlocking(500);
            var target = Circle(1000, judged: true);
            var policy = new StartTimeOrderedHitPolicy { HitObjectContainer = Container(earlier, target) };

            policy.HandleHit(target);

            Assert.That(earlier.Judged, Is.False);
        }
    }
}
