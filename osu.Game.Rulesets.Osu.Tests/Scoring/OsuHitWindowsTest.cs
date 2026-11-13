// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Osu.Tests.Scoring
{
    [TestFixture]
    public class OsuHitWindowsTest
    {
        // CF: OsuHitWindows.IsHitResultAllowed | switch (result) | case Great
        // CF: OsuHitWindows.IsHitResultAllowed | switch (result) | case Ok
        // CF: OsuHitWindows.IsHitResultAllowed | switch (result) | case Meh
        // CF: OsuHitWindows.IsHitResultAllowed | switch (result) | case Miss
        [TestCase(HitResult.Great)]
        [TestCase(HitResult.Ok)]
        [TestCase(HitResult.Meh)]
        [TestCase(HitResult.Miss)]
        public void TestAllowedResults(HitResult result)
        {
            Assert.That(new OsuHitWindows().IsHitResultAllowed(result), Is.True);
        }

        // CF: OsuHitWindows.IsHitResultAllowed | switch (result) | default
        [Test]
        public void TestDisallowedResult()
        {
            Assert.That(new OsuHitWindows().IsHitResultAllowed(HitResult.Perfect), Is.False);
        }

        // CF: OsuHitWindows.WindowFor | switch (result) | case Great
        // CF: OsuHitWindows.WindowFor | switch (result) | case Ok
        // CF: OsuHitWindows.WindowFor | switch (result) | case Meh
        // CF: OsuHitWindows.WindowFor | switch (result) | case Miss
        [Test]
        public void TestWindowsAtOd5()
        {
            var windows = new OsuHitWindows();
            windows.SetDifficulty(5);

            Assert.That(windows.WindowFor(HitResult.Great), Is.EqualTo(49.5));
            Assert.That(windows.WindowFor(HitResult.Ok), Is.EqualTo(99.5));
            Assert.That(windows.WindowFor(HitResult.Meh), Is.EqualTo(149.5));
            Assert.That(windows.WindowFor(HitResult.Miss), Is.EqualTo(OsuHitWindows.MISS_WINDOW));
        }

        // CF: OsuHitWindows.WindowFor | switch (result) | throws ArgumentOutOfRangeException
        // CF: OsuHitWindows.WindowFor | switch (result) | default
        [Test]
        public void TestWindowForUnsupportedResultThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new OsuHitWindows().WindowFor(HitResult.Perfect));
        }
    }
}
