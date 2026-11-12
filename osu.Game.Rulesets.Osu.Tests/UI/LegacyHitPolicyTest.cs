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
    public class LegacyHitPolicyTest
    {
        [Test]
        public void TestNoContainer_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new LegacyHitPolicy().CheckHittable(Circle(1000), 1000, HitResult.Great));
        }

        [Test]
        public void TestFirstObject_InRange_Hits()
        {
            // index > 0 FALSE; result != None; loop breaks on the object itself; |dt| < range TRUE
            var target = Circle(1000);
            var policy = new LegacyHitPolicy { HitObjectContainer = Container(target) };

            Assert.That(policy.CheckHittable(target, 1100, HitResult.Great), Is.EqualTo(ClickAction.Hit));
        }

        [Test]
        public void TestObjectNotInContainer_IndexNegative_SkipsPreviousObjectCheck()
        {
            // index == -1 -> index > 0 FALSE; loop never finds the object, final range check decides
            var policy = new LegacyHitPolicy { HitObjectContainer = Container(Circle(500, judged: true)) };

            Assert.That(policy.CheckHittable(Circle(1000), 1000, HitResult.Great), Is.EqualTo(ClickAction.Hit));
        }

        [Test]
        public void TestPreviousStackedAndUnjudged_Ignores()
        {
            // index > 0 TRUE; StackHeight > 0 TRUE && !AllJudged TRUE
            var target = Circle(1000);
            var policy = new LegacyHitPolicy { HitObjectContainer = Container(Circle(1000, stackHeight: 1), target) };

            Assert.That(policy.CheckHittable(target, 1000, HitResult.Great), Is.EqualTo(ClickAction.Ignore));
        }

        [Test]
        public void TestPreviousStackedButJudged_DoesNotIgnore()
        {
            // StackHeight > 0 TRUE && !AllJudged FALSE
            var target = Circle(1000);
            var policy = new LegacyHitPolicy { HitObjectContainer = Container(Circle(1000, judged: true, stackHeight: 1), target) };

            Assert.That(policy.CheckHittable(target, 1000, HitResult.Great), Is.EqualTo(ClickAction.Hit));
        }

        [Test]
        public void TestPreviousNotStacked_DoesNotIgnore()
        {
            // StackHeight > 0 FALSE (short-circuits AllJudged); previous overlaps so it is not blocking
            var target = Circle(1001);
            var policy = new LegacyHitPolicy { HitObjectContainer = Container(Circle(1000), target) };

            Assert.That(policy.CheckHittable(target, 1001, HitResult.Great), Is.EqualTo(ClickAction.Hit));
        }

        [Test]
        public void TestResultNone_Shakes()
        {
            // result == None TRUE
            var target = Circle(1000);
            var policy = new LegacyHitPolicy { HitObjectContainer = Container(target) };

            Assert.That(policy.CheckHittable(target, 0, HitResult.None), Is.EqualTo(ClickAction.Shake));
        }

        [Test]
        public void TestEarlierUnjudgedObjectEndedBeforeTarget_Shakes()
        {
            // loop: AllJudged FALSE, not the target, end + 3 < start TRUE
            var target = Circle(1000);
            var policy = new LegacyHitPolicy { HitObjectContainer = Container(Circle(500), target) };

            Assert.That(policy.CheckHittable(target, 1000, HitResult.Great), Is.EqualTo(ClickAction.Shake));
        }

        [Test]
        public void TestEarlierUnjudgedObjectWithinLeniency_DoesNotBlock()
        {
            // end + 3 < start FALSE: 997 + 3 == 1000 is not strictly less
            var target = Circle(1000);
            var policy = new LegacyHitPolicy { HitObjectContainer = Container(Circle(997), target) };

            Assert.That(policy.CheckHittable(target, 1000, HitResult.Great), Is.EqualTo(ClickAction.Hit));
        }

        [Test]
        public void TestEarlierJudgedObject_IsSkipped()
        {
            // loop: AllJudged TRUE -> continue
            var target = Circle(1000);
            var policy = new LegacyHitPolicy { HitObjectContainer = Container(Circle(500, judged: true), target) };

            Assert.That(policy.CheckHittable(target, 1000, HitResult.Great), Is.EqualTo(ClickAction.Hit));
        }

        [Test]
        public void TestLaterUnjudgedObject_IsNotConsidered()
        {
            // loop breaks at the target before reaching later objects
            var target = Circle(1000);
            var policy = new LegacyHitPolicy { HitObjectContainer = Container(target, Circle(2000)) };

            Assert.That(policy.CheckHittable(target, 1000, HitResult.Great), Is.EqualTo(ClickAction.Hit));
        }

        [TestCase(1099, ClickAction.Hit)]
        [TestCase(1100, ClickAction.Shake)]
        [TestCase(901, ClickAction.Hit)]
        [TestCase(900, ClickAction.Shake)]
        public void TestHittableRangeBoundary(double time, ClickAction expected)
        {
            // final ternary: |start - time| < hittableRange is strict, on both sides
            var target = Circle(1000);
            var policy = new LegacyHitPolicy(100) { HitObjectContainer = Container(target) };

            Assert.That(policy.CheckHittable(target, time, HitResult.Great), Is.EqualTo(expected));
        }

        [Test]
        public void TestDefaultRange_IsMissWindow()
        {
            var target = Circle(1000);
            var policy = new LegacyHitPolicy { HitObjectContainer = Container(target) };

            Assert.That(policy.CheckHittable(target, 1000 + 399, HitResult.Great), Is.EqualTo(ClickAction.Hit));
            Assert.That(policy.CheckHittable(target, 1000 + 400, HitResult.Great), Is.EqualTo(ClickAction.Shake));
        }
    }
}
