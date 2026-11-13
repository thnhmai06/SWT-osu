// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Tests.Rulesets.Scoring
{
    [TestFixture]
    public class ScoreMultiplierCalculatorTest
    {
        private class TestCalculator : ScoreMultiplierCalculator
        {
            public int CombinationInvocations;

            public TestCalculator()
                : base(new ScoreMultiplierContext(new BeatmapDifficulty()))
            {
                Single<OsuModEasy>(0.5);
                Single<OsuModHardRock>(mod => 1.5);
                Single<OsuModHidden>(1.1);
                Combination<OsuModDoubleTime, OsuModHidden>((dt, hd) =>
                {
                    CombinationInvocations++;
                    return 2;
                });
            }

            public ScoreMultiplierContext ExposedContext => Context;
        }

        private TestCalculator calculator = null!;

        [SetUp]
        public void SetUp() => calculator = new TestCalculator();

        // CF: ScoreMultiplierCalculator.CalculateFor | allModsByType.Count == 0 | TRUE
        [Test]
        public void TestNoMods()
        {
            Assert.That(calculator.CalculateFor([]), Is.EqualTo(1));
        }

        // CF: ScoreMultiplierCalculator.CalculateFor | allModsByType.Count == 0 | FALSE
        // CF: ScoreMultiplierCalculator.CalculateFor | allModsByType.Count > 1 | FALSE
        // CF: ScoreMultiplierCalculator.CalculateFor | singleMultipliers.TryGetValue(modType, out multiplier) | TRUE
        [Test]
        public void TestSingleMultipliers()
        {
            Assert.That(calculator.CalculateFor([new OsuModEasy()]), Is.EqualTo(0.5));
            Assert.That(calculator.CalculateFor([new OsuModHardRock()]), Is.EqualTo(1.5));
            Assert.That(calculator.CalculateFor([new OsuModHidden()]), Is.EqualTo(1.1));
        }

        // CF: ScoreMultiplierCalculator.CalculateFor | singleMultipliers.TryGetValue(modType, out multiplier) | FALSE
        [Test]
        public void TestUnregisteredModHasNoEffect()
        {
            Assert.That(calculator.CalculateFor([new OsuModSpunOut()]), Is.EqualTo(1));
        }

        // CF: ScoreMultiplierCalculator.CalculateFor | allModsByType.Count > 1 | TRUE
        // CF: ScoreMultiplierCalculator.CalculateFor | foreach combination | loop n
        // CF: ScoreMultiplierCalculator.CalculateFor | remainingModTypes.IsSupersetOf(combination) | FALSE
        [Test]
        public void TestSeveralSinglesMultiply()
        {
            Assert.That(calculator.CalculateFor([new OsuModEasy(), new OsuModHardRock()]), Is.EqualTo(0.75).Within(1e-9));
            Assert.That(calculator.CombinationInvocations, Is.Zero);
        }

        // CF: ScoreMultiplierCalculator.CalculateFor | remainingModTypes.IsSupersetOf(combination) | TRUE
        [Test]
        public void TestCombinationReplacesTheSinglesOfItsMods()
        {
            // HD's own 1.1x single must not apply since it is consumed by the DT+HD combination (2x); EZ remains.
            double result = calculator.CalculateFor([new OsuModHidden(), new OsuModEasy(), new OsuModDoubleTime()]);

            Assert.That(result, Is.EqualTo(2 * 0.5).Within(1e-9));
            Assert.That(calculator.CombinationInvocations, Is.EqualTo(1));
        }

        [Test]
        public void TestPartialCombinationFallsBackToSingles()
        {
            Assert.That(calculator.CalculateFor([new OsuModHidden(), new OsuModEasy()]), Is.EqualTo(0.55).Within(1e-9));
            Assert.That(calculator.CombinationInvocations, Is.Zero);
        }

        [Test]
        public void TestContextExposed()
        {
            var difficulty = new BeatmapDifficulty();
            var context = new ScoreMultiplierContext(difficulty);

            Assert.That(context.BeatmapDifficultyWithoutMods, Is.SameAs(difficulty));
            Assert.That(context.Score, Is.Null);
            Assert.That(calculator.ExposedContext.Score, Is.Null);
        }

        // CF: ScoreMultiplierCalculator.CalculateFor | mods.ToDictionary(m => m.GetType()) | throws ArgumentException
        [Test]
        public void TestDuplicateModTypesRejected()
        {
            Assert.That(() => calculator.CalculateFor([new OsuModEasy(), new OsuModEasy()]), Throws.InstanceOf<ArgumentException>());
        }
    }
}
