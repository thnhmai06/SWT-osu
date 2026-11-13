// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Rulesets.Mania.Scoring;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Mania.Tests.Scoring
{
    [TestFixture]
    public class ManiaHitWindowsTest
    {
        private static ManiaHitWindows create(double od, bool classic = false, bool scoreV2 = false, bool convert = false)
        {
            var windows = new ManiaHitWindows { ClassicModActive = classic, ScoreV2Active = scoreV2, IsConvert = convert };
            windows.SetDifficulty(od);
            return windows;
        }

        private static void assertWindows(ManiaHitWindows w, double perfect, double great, double good, double ok, double meh, double miss)
        {
            Assert.That(w.WindowFor(HitResult.Perfect), Is.EqualTo(perfect).Within(1e-9));
            Assert.That(w.WindowFor(HitResult.Great), Is.EqualTo(great).Within(1e-9));
            Assert.That(w.WindowFor(HitResult.Good), Is.EqualTo(good).Within(1e-9));
            Assert.That(w.WindowFor(HitResult.Ok), Is.EqualTo(ok).Within(1e-9));
            Assert.That(w.WindowFor(HitResult.Meh), Is.EqualTo(meh).Within(1e-9));
            Assert.That(w.WindowFor(HitResult.Miss), Is.EqualTo(miss).Within(1e-9));
        }

        // CF: ManiaHitWindows.updateWindows | ClassicModActive | FALSE
        // CF: ManiaHitWindows.WindowFor | switch(result) | case Perfect
        // CF: ManiaHitWindows.WindowFor | switch(result) | case Great
        // CF: ManiaHitWindows.WindowFor | switch(result) | case Good
        // CF: ManiaHitWindows.WindowFor | switch(result) | case Ok
        // CF: ManiaHitWindows.WindowFor | switch(result) | case Meh
        // CF: ManiaHitWindows.WindowFor | switch(result) | case Miss
        [Test]
        public void TestDefaultWindowsAtMidOd() => assertWindows(create(5), 19.5, 49.5, 82.5, 112.5, 136.5, 173.5);

        // CF: ManiaHitWindows.updateWindows | ClassicModActive | FALSE
        [Test]
        public void TestDefaultWindowsAtOdZero() => assertWindows(create(0), 22.5, 64.5, 97.5, 127.5, 151.5, 188.5);

        // CF: ManiaHitWindows.updateWindows | ClassicModActive | FALSE
        [Test]
        public void TestDefaultWindowsAtOdTen() => assertWindows(create(10), 13.5, 34.5, 67.5, 97.5, 121.5, 158.5);

        // CF: ManiaHitWindows.updateWindows | ClassicModActive | TRUE
        // CF: ManiaHitWindows.updateWindows | !ScoreV2Active | FALSE
        [Test]
        public void TestClassicWithScoreV2UsesDefaultWindows() => assertWindows(create(5, classic: true, scoreV2: true), 19.5, 49.5, 82.5, 112.5, 136.5, 173.5);

        // CF: ManiaHitWindows.updateWindows | ClassicModActive | TRUE
        // CF: ManiaHitWindows.updateWindows | !ScoreV2Active | TRUE
        // CF: ManiaHitWindows.updateWindows | IsConvert | FALSE
        [Test]
        public void TestClassicNonConvertAtOdTen() => assertWindows(create(10, classic: true), 16.5, 34.5, 67.5, 97.5, 121.5, 158.5);

        // CF: ManiaHitWindows.updateWindows | IsConvert | FALSE
        [Test]
        public void TestClassicNonConvertClampsInvertedOdAtZero() => assertWindows(create(12, classic: true), 16.5, 34.5, 67.5, 97.5, 121.5, 158.5);

        // CF: ManiaHitWindows.updateWindows | IsConvert | FALSE
        [Test]
        public void TestClassicNonConvertClampsInvertedOdAtTen() => assertWindows(create(-2, classic: true), 16.5, 64.5, 97.5, 127.5, 151.5, 188.5);

        // CF: ManiaHitWindows.updateWindows | IsConvert | FALSE
        [Test]
        public void TestClassicNonConvertAtMidOd() => assertWindows(create(5, classic: true), 16.5, 49.5, 82.5, 112.5, 136.5, 173.5);

        // CF: ManiaHitWindows.updateWindows | IsConvert | TRUE
        // CF: ManiaHitWindows.updateWindows | Math.Round(overallDifficulty) > 4 | TRUE
        [Test]
        public void TestClassicConvertAboveOdFourUsesTightWindows() => assertWindows(create(5, classic: true, convert: true), 16.5, 34.5, 67.5, 97.5, 121.5, 158.5);

        // CF: ManiaHitWindows.updateWindows | IsConvert | TRUE
        // CF: ManiaHitWindows.updateWindows | Math.Round(overallDifficulty) > 4 | FALSE
        [Test]
        public void TestClassicConvertAtOrBelowOdFourUsesLooseWindows() => assertWindows(create(4, classic: true, convert: true), 16.5, 47.5, 77.5, 97.5, 121.5, 158.5);

        // CF: ManiaHitWindows.SpeedMultiplier.set | updateWindows | no throw
        [Test]
        public void TestSpeedMultiplierScalesWindows()
        {
            var windows = create(10, classic: true);
            windows.SpeedMultiplier = 1.5;

            Assert.That(windows.WindowFor(HitResult.Perfect), Is.EqualTo(24.5));
        }

        // CF: ManiaHitWindows.DifficultyMultiplier.set | updateWindows | no throw
        [Test]
        public void TestDifficultyMultiplierScalesWindows()
        {
            var windows = create(10, classic: true);
            windows.DifficultyMultiplier = 2;

            Assert.That(windows.WindowFor(HitResult.Perfect), Is.EqualTo(8.5));
        }

        // CF: ManiaHitWindows.ClassicModActive.set | updateWindows | no throw
        // CF: ManiaHitWindows.ScoreV2Active.set | updateWindows | no throw
        // CF: ManiaHitWindows.IsConvert.set | updateWindows | no throw
        [Test]
        public void TestTogglingFlagsAfterDifficultyRecomputesWindows()
        {
            var windows = create(10);
            Assert.That(windows.WindowFor(HitResult.Perfect), Is.EqualTo(13.5));

            windows.ClassicModActive = true;
            Assert.That(windows.WindowFor(HitResult.Perfect), Is.EqualTo(16.5));

            windows.ScoreV2Active = true;
            Assert.That(windows.WindowFor(HitResult.Perfect), Is.EqualTo(13.5));

            windows.ScoreV2Active = false;
            windows.IsConvert = true;
            Assert.That(windows.WindowFor(HitResult.Perfect), Is.EqualTo(16.5));
        }

        // CF: ManiaHitWindows.IsHitResultAllowed | switch(result) | case Perfect
        // CF: ManiaHitWindows.IsHitResultAllowed | switch(result) | case Great
        // CF: ManiaHitWindows.IsHitResultAllowed | switch(result) | case Good
        // CF: ManiaHitWindows.IsHitResultAllowed | switch(result) | case Ok
        // CF: ManiaHitWindows.IsHitResultAllowed | switch(result) | case Meh
        // CF: ManiaHitWindows.IsHitResultAllowed | switch(result) | case Miss
        [TestCase(HitResult.Perfect)]
        [TestCase(HitResult.Great)]
        [TestCase(HitResult.Good)]
        [TestCase(HitResult.Ok)]
        [TestCase(HitResult.Meh)]
        [TestCase(HitResult.Miss)]
        public void TestAllowedHitResults(HitResult result) => Assert.That(new ManiaHitWindows().IsHitResultAllowed(result), Is.True);

        // CF: ManiaHitWindows.IsHitResultAllowed | switch(result) | default
        [TestCase(HitResult.None)]
        [TestCase(HitResult.SmallTickHit)]
        [TestCase(HitResult.LargeBonus)]
        [TestCase(HitResult.IgnoreHit)]
        public void TestDisallowedHitResults(HitResult result) => Assert.That(new ManiaHitWindows().IsHitResultAllowed(result), Is.False);

        // CF: ManiaHitWindows.WindowFor | switch(result) | default
        // CF: ManiaHitWindows.WindowFor | switch(result) | throws ArgumentOutOfRangeException
        [Test]
        public void TestWindowForUnsupportedResultThrows()
        {
            Assert.That(() => create(5).WindowFor(HitResult.SmallTickHit), Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
