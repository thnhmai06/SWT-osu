// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Tests.Rulesets.Scoring
{
    [TestFixture]
    public class HitWindowsTest
    {
        private class TestHitWindows : HitWindows
        {
            // the base constructor calls IsHitResultAllowed, so allowed results must be a static lookup keyed by a ctor-independent mode.
            private readonly int mode;

            public TestHitWindows(int mode = 0)
            {
                this.mode = mode;
            }

            public override bool IsHitResultAllowed(HitResult result) => (mode, result) switch
            {
                (_, HitResult.Miss) => true,
                (0, HitResult.Meh or HitResult.Ok or HitResult.Great) => true,
                (1, HitResult.Great) => true,
                (2, HitResult.Meh) => true,
                _ => false
            };

            public override void SetDifficulty(double difficulty)
            {
            }

            public override double WindowFor(HitResult result) => result switch
            {
                HitResult.Perfect => 10,
                HitResult.Great => 20,
                HitResult.Ok => 40,
                HitResult.Meh => 80,
                HitResult.Miss => 120,
                _ => 0
            };

            public HitResult Lowest() => LowestSuccessfulHitResult();
        }

        private static TestHitWindows standard() => new TestHitWindows();

        // CF: HitWindows.GetAllAvailableWindows | for result Miss..Perfect | loop n
        // CF: HitWindows.GetAllAvailableWindows | IsHitResultAllowed(result) | TRUE
        // CF: HitWindows.GetAllAvailableWindows | IsHitResultAllowed(result) | FALSE
        [Test]
        public void TestGetAllAvailableWindowsOnlyReturnsAllowedResults()
        {
            var windows = standard().GetAllAvailableWindows().ToArray();

            Assert.That(windows.Select(w => w.result), Is.EqualTo(new[] { HitResult.Miss, HitResult.Meh, HitResult.Ok, HitResult.Great }));
            Assert.That(windows.Select(w => w.length), Is.EqualTo(new[] { 120.0, 80, 40, 20 }));
        }

        // CF: HitWindows.ResultFor | for result Perfect..Miss | loop n
        // CF: HitWindows.ResultFor | IsHitResultAllowed(result) && timeOffset <= WindowFor(result) | TRUE
        // CF: HitWindows.ResultFor | IsHitResultAllowed(result) | FALSE
        // CF: HitWindows.ResultFor | timeOffset <= WindowFor(result) | FALSE
        [TestCase(0, HitResult.Great)]
        [TestCase(20, HitResult.Great)]
        [TestCase(-20, HitResult.Great)]
        [TestCase(21, HitResult.Ok)]
        [TestCase(-79, HitResult.Meh)]
        [TestCase(100, HitResult.Miss)]
        public void TestResultFor(double offset, HitResult expected)
        {
            Assert.That(standard().ResultFor(offset), Is.EqualTo(expected));
        }

        // CF: HitWindows.ResultFor | for result Perfect..Miss | loop 0
        // CF: HitWindows.ResultFor | no result matched | default
        [Test]
        public void TestResultForBeyondAllWindowsIsNone()
        {
            Assert.That(standard().ResultFor(121), Is.EqualTo(HitResult.None));
        }

        // CF: HitWindows.LowestSuccessfulHitResult | IsHitResultAllowed(result) | TRUE
        // CF: HitWindows.LowestSuccessfulHitResult | IsHitResultAllowed(result) | FALSE
        [Test]
        public void TestLowestSuccessfulHitResultSkipsDisallowed()
        {
            Assert.That(standard().Lowest(), Is.EqualTo(HitResult.Meh));
            Assert.That(new TestHitWindows(1).Lowest(), Is.EqualTo(HitResult.Great));
        }

        // CF: HitWindows.LowestSuccessfulHitResult | for result Meh..Perfect | loop n
        [Test]
        public void TestLowestSuccessfulHitResultNone()
        {
            Assert.That(new TestHitWindows(3).Lowest(), Is.EqualTo(HitResult.None));
        }

        // CF: HitWindows.CanBeHit | timeOffset <= WindowFor(LowestSuccessfulHitResult()) | TRUE
        // CF: HitWindows.CanBeHit | timeOffset <= WindowFor(LowestSuccessfulHitResult()) | FALSE
        [Test]
        public void TestCanBeHit()
        {
            var windows = standard();

            Assert.That(windows.CanBeHit(80), Is.True);
            Assert.That(windows.CanBeHit(81), Is.False);
        }

        // CF: HitWindows.Empty | EmptyHitWindows | non-null
        [Test]
        public void TestEmptyHitWindows()
        {
            Assert.That(HitWindows.Empty.IsHitResultAllowed(HitResult.Great), Is.True);
            Assert.That(HitWindows.Empty.WindowFor(HitResult.Great), Is.Zero);
            Assert.That(HitWindows.Empty.GetAllAvailableWindows().Count(), Is.GreaterThan(1));
            Assert.That(HitWindows.Empty.ResultFor(0), Is.EqualTo(HitResult.Perfect));
        }
    }
}
