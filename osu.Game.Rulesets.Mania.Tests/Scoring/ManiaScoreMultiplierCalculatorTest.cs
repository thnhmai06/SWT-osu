// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mania.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Mania.Tests.Scoring
{
    [TestFixture]
    public class ManiaScoreMultiplierCalculatorTest
    {
        private static double calculate(Mod[] mods, ScoreInfo? score = null)
            => new ManiaRuleset().CreateScoreMultiplierCalculator(new ScoreMultiplierContext(new BeatmapDifficulty(), score)).CalculateFor(mods);

        // CF: ManiaScoreMultiplierCalculator..ctor | Single<T> registrations (constant multipliers) | non-null
        [TestCase(typeof(ManiaModEasy), 0.5)]
        [TestCase(typeof(ManiaModNoFail), 0.5)]
        [TestCase(typeof(ManiaModNoRelease), 0.9)]
        [TestCase(typeof(ManiaModDifficultyAdjust), 0.5)]
        [TestCase(typeof(ManiaModConstantSpeed), 0.9)]
        [TestCase(typeof(ManiaModHoldOff), 0.9)]
        [TestCase(typeof(ModWindUp), 0.5)]
        [TestCase(typeof(ModWindDown), 0.5)]
        [TestCase(typeof(ModAdaptiveSpeed), 0.5)]
        [TestCase(typeof(ManiaModHardRock), 1)]
        public void TestConstantMultipliers(Type modType, double expected)
        {
            Assert.That(calculate([(Mod)Activator.CreateInstance(modType)!]), Is.EqualTo(expected).Within(Precision.DOUBLE_EPSILON));
        }

        // CF: ManiaScoreMultiplierCalculator.rateAdjustMultiplier | speedChange >= 1 | FALSE
        [TestCase(0.50, 0.1)]
        [TestCase(0.55, 0.1)]
        [TestCase(0.60, 0.2)]
        [TestCase(0.75, 0.3)]
        [TestCase(0.80, 0.4)]
        [TestCase(0.90, 0.5)]
        [TestCase(0.99, 0.5)]
        public void TestRateAdjustMultipliers(double speedChange, double expected)
        {
            Assert.That(calculate([new ManiaModHalfTime { SpeedChange = { Value = speedChange } }]), Is.EqualTo(expected).Within(Precision.DOUBLE_EPSILON));
            Assert.That(calculate([new ManiaModDaycore { SpeedChange = { Value = speedChange } }]), Is.EqualTo(expected).Within(Precision.DOUBLE_EPSILON));
        }

        // CF: ManiaScoreMultiplierCalculator.keyModMultiplier | scoreInfo == null | TRUE
        [Test]
        public void TestKeyModWithoutScoreUsesNewMultiplier()
        {
            Assert.That(calculate([new ManiaModKey4()]), Is.EqualTo(0.9).Within(Precision.DOUBLE_EPSILON));
        }

        // CF: ManiaScoreMultiplierCalculator..ctor | multipliers of several mods combined | non-null
        [Test]
        public void TestKeyModMultiplierCombinesWithOthers()
        {
            Assert.That(calculate([new ManiaModEasy(), new ManiaModKey4()]), Is.EqualTo(0.5 * 0.9).Within(Precision.DOUBLE_EPSILON));
        }

        // CF: ManiaScoreMultiplierCalculator.keyModMultiplier | scoreInfo == null | FALSE
        // CF: ManiaScoreMultiplierCalculator.keyModMultiplier | !string.IsNullOrEmpty(clientVersion) | TRUE
        // CF: ManiaScoreMultiplierCalculator.keyModMultiplier | int.TryParse(pieces[0]) | TRUE
        // CF: ManiaScoreMultiplierCalculator.keyModMultiplier | int.TryParse(pieces[1]) | TRUE
        // CF: ManiaScoreMultiplierCalculator.keyModMultiplier | year < 2025 | TRUE
        // CF: ManiaScoreMultiplierCalculator.keyModMultiplier | year < 2025 | FALSE
        // CF: ManiaScoreMultiplierCalculator.keyModMultiplier | year == 2025 | TRUE
        // CF: ManiaScoreMultiplierCalculator.keyModMultiplier | year == 2025 | FALSE
        // CF: ManiaScoreMultiplierCalculator.keyModMultiplier | monthDay < 718 | TRUE
        // CF: ManiaScoreMultiplierCalculator.keyModMultiplier | monthDay < 718 | FALSE
        [TestCase("2024.130.2", 1)]
        [TestCase("2025.605.3", 1)]
        [TestCase("2025.717.0-lazer", 1)]
        [TestCase("2025.718.0-tachyon", 0.9)]
        [TestCase("2025.721.0-tachyon", 0.9)]
        [TestCase("2026.518.0-lazer", 0.9)]
        // CF: ManiaScoreMultiplierCalculator.keyModMultiplier | int.TryParse(pieces[0]) | FALSE
        // CF: ManiaScoreMultiplierCalculator.keyModMultiplier | int.TryParse(pieces[1]) | FALSE
        [TestCase("abc.def", 0.9)]
        [TestCase("2024.xyz", 0.9)]
        public void TestKeyModMultiplierByClientVersion(string clientVersion, double expected)
        {
            // date is deliberately on the opposite side of the cutoff to prove the version takes priority.
            var score = new ScoreInfo { ClientVersion = clientVersion, Date = new DateTimeOffset(expected == 1 ? 2026 : 2020, 1, 1, 0, 0, 0, TimeSpan.Zero) };
            Assert.That(calculate([new ManiaModKey4()], score), Is.EqualTo(expected).Within(Precision.DOUBLE_EPSILON));
        }

        // CF: ManiaScoreMultiplierCalculator.keyModMultiplier | !string.IsNullOrEmpty(clientVersion) | FALSE
        // CF: ManiaScoreMultiplierCalculator.keyModMultiplier | scoreInfo.Date < 2025-07-18 | TRUE
        // CF: ManiaScoreMultiplierCalculator.keyModMultiplier | scoreInfo.Date < 2025-07-18 | FALSE
        [TestCase(2025, 7, 17, 1)]
        [TestCase(2024, 1, 31, 1)]
        [TestCase(2025, 7, 19, 0.9)]
        [TestCase(2026, 6, 18, 0.9)]
        public void TestKeyModMultiplierByDateWhenNoClientVersion(int year, int month, int day, double expected)
        {
            var score = new ScoreInfo { ClientVersion = string.Empty, Date = new DateTimeOffset(year, month, day, 11, 0, 0, TimeSpan.Zero) };
            Assert.That(calculate([new ManiaModKey4()], score), Is.EqualTo(expected).Within(Precision.DOUBLE_EPSILON));
        }

        // CF: ManiaScoreMultiplierCalculator.classicMultiplier | score != null && score.TotalScoreVersion < 30000017 | TRUE
        // CF: ManiaScoreMultiplierCalculator.classicMultiplier | score != null | FALSE
        // CF: ManiaScoreMultiplierCalculator.classicMultiplier | TotalScoreVersion < 30000017 | FALSE
        [TestCase(30000001, 0.96)]
        [TestCase(30000016, 0.96)]
        [TestCase(30000017, 1)]
        [TestCase(null, 1)]
        public void TestClassicMultiplierVersioning(int? totalScoreVersion, double expected)
        {
            var score = totalScoreVersion != null ? new ScoreInfo { TotalScoreVersion = totalScoreVersion.Value } : null;
            Assert.That(calculate([new ManiaModClassic()], score), Is.EqualTo(expected).Within(Precision.DOUBLE_EPSILON));
        }
    }
}
