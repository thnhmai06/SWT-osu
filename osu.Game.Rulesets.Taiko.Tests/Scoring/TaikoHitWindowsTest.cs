// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko.Scoring;

namespace osu.Game.Rulesets.Taiko.Tests.Scoring
{
    [TestFixture]
    public class TaikoHitWindowsTest
    {
        // CF: TaikoHitWindows.IsHitResultAllowed | case Great | case Great
        // CF: TaikoHitWindows.IsHitResultAllowed | case Ok | case Ok
        // CF: TaikoHitWindows.IsHitResultAllowed | case Miss | case Miss
        [TestCase(HitResult.Great)]
        [TestCase(HitResult.Ok)]
        [TestCase(HitResult.Miss)]
        public void TestAllowedResults(HitResult result) => Assert.That(new TaikoHitWindows().IsHitResultAllowed(result), Is.True);

        // CF: TaikoHitWindows.IsHitResultAllowed | switch (result) | default
        [TestCase(HitResult.Meh)]
        [TestCase(HitResult.Perfect)]
        [TestCase(HitResult.None)]
        public void TestDisallowedResults(HitResult result) => Assert.That(new TaikoHitWindows().IsHitResultAllowed(result), Is.False);

        // CF: TaikoHitWindows.WindowFor | case Great | case Great
        // CF: TaikoHitWindows.WindowFor | case Ok | case Ok
        // CF: TaikoHitWindows.WindowFor | case Miss | case Miss
        // CF: TaikoHitWindows.SetDifficulty | sequential | no throw
        [TestCase(0, 49.5, 119.5, 134.5)]
        [TestCase(5, 34.5, 79.5, 94.5)]
        [TestCase(10, 19.5, 49.5, 69.5)]
        public void TestWindowsPerDifficulty(double difficulty, double great, double ok, double miss)
        {
            var windows = new TaikoHitWindows();
            windows.SetDifficulty(difficulty);

            Assert.That(windows.WindowFor(HitResult.Great), Is.EqualTo(great));
            Assert.That(windows.WindowFor(HitResult.Ok), Is.EqualTo(ok));
            Assert.That(windows.WindowFor(HitResult.Miss), Is.EqualTo(miss));
        }

        // CF: TaikoHitWindows.WindowFor | switch (result) | default
        // CF: TaikoHitWindows.WindowFor | switch (result) | throws ArgumentOutOfRangeException
        [Test]
        public void TestWindowForUnsupportedResultThrows()
            => Assert.Throws<ArgumentOutOfRangeException>(() => new TaikoHitWindows().WindowFor(HitResult.Meh));
    }
}
