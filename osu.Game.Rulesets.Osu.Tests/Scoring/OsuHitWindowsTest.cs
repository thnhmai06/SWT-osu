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
        [TestCase(HitResult.Great)]
        [TestCase(HitResult.Ok)]
        [TestCase(HitResult.Meh)]
        [TestCase(HitResult.Miss)]
        public void TestIsHitResultAllowed_ListedCase_ReturnsTrue(HitResult result)
        {
            // switch: each case label reaches "return true"
            Assert.That(new OsuHitWindows().IsHitResultAllowed(result), Is.True);
        }

        [TestCase(HitResult.Perfect)]
        [TestCase(HitResult.LargeTickHit)]
        [TestCase(HitResult.None)]
        public void TestIsHitResultAllowed_UnlistedResult_ReturnsFalse(HitResult result)
        {
            // switch: no case matches, falls through to "return false"
            Assert.That(new OsuHitWindows().IsHitResultAllowed(result), Is.False);
        }

        // Windows are floor(DifficultyRange(od, range)) - 0.5.
        [TestCase(0, HitResult.Great, 79.5)]
        [TestCase(5, HitResult.Great, 49.5)]
        [TestCase(10, HitResult.Great, 19.5)]
        [TestCase(0, HitResult.Ok, 139.5)]
        [TestCase(5, HitResult.Ok, 99.5)]
        [TestCase(10, HitResult.Ok, 59.5)]
        [TestCase(0, HitResult.Meh, 199.5)]
        [TestCase(5, HitResult.Meh, 149.5)]
        [TestCase(10, HitResult.Meh, 99.5)]
        public void TestWindowFor_HitResultCase_UsesDifficulty(double difficulty, HitResult result, double expected)
        {
            // switch cases Great/Ok/Meh each return their own window
            var windows = new OsuHitWindows();
            windows.SetDifficulty(difficulty);

            Assert.That(windows.WindowFor(result), Is.EqualTo(expected));
        }

        [TestCase(0)]
        [TestCase(10)]
        public void TestWindowFor_Miss_IsFixedRegardlessOfDifficulty(double difficulty)
        {
            // switch case Miss returns constant MISS_WINDOW
            var windows = new OsuHitWindows();
            windows.SetDifficulty(difficulty);

            Assert.That(windows.WindowFor(HitResult.Miss), Is.EqualTo(OsuHitWindows.MISS_WINDOW));
        }

        [Test]
        public void TestWindowFor_UnsupportedResult_Throws()
        {
            // switch default branch throws
            Assert.Throws<ArgumentOutOfRangeException>(() => new OsuHitWindows().WindowFor(HitResult.Perfect));
        }
    }
}
