// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Difficulty.Evaluators;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Colour.Data;
using osu.Game.Rulesets.Taiko.Difficulty.Skills;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Difficulty.Skills
{
    [TestFixture]
    public class StaminaTest
    {
        private class TestStamina : Stamina
        {
            public TestStamina(bool single, bool convert)
                : base(Array.Empty<Mod>(), single, convert)
            {
            }

            public double Strain(DifficultyHitObject o) => StrainValueAt(o);
            public double Initial(double time, DifficultyHitObject o) => CalculateInitialStrain(time, o);
        }

        // 25 centre notes at 100ms intervals; with `inStreak` they all belong to one mono streak.
        private static System.Collections.Generic.List<TaikoDifficultyHitObject> create(bool inStreak)
        {
            var o = DifficultyObjectFactory.Create(new (double, HitType)[25].Select(_ => (100d, HitType.Centre)).ToArray());

            if (inStreak)
            {
                var streak = new MonoStreak();
                streak.HitObjects.AddRange(o);
                foreach (var x in o)
                    x.ColourData.MonoStreak = streak;
            }

            return o;
        }

        private static double eval(DifficultyHitObject o) => StaminaEvaluator.EvaluateDifficultyOf(o) * 1.1;

        // CF: Stamina.StrainValueAt | currentObject?.ColourData.MonoStreak?. ?? 0 | null
        // CF: Stamina.StrainValueAt | isConvert | FALSE
        // CF: Stamina.StrainValueAt | !SingleColourStamina | TRUE
        // CF: Stamina.StrainValueAt | SingleColourStamina | FALSE
        [Test]
        public void TestNoStreakHasNoMonoLengthBonus()
        {
            var o = create(false);
            Assert.That(new TestStamina(false, false).Strain(o[5]), Is.EqualTo(eval(o[5])).Within(1e-9));
        }

        // CF: Stamina.StrainValueAt | currentObject?.ColourData.MonoStreak?. ?? 0 | non-null
        [Test]
        public void TestLongMonoStreakAddsBonus()
        {
            var o = create(true);
            // index 20 -> ReverseLerp(20, 5, 20) = 1 -> 1.5x
            Assert.That(new TestStamina(false, false).Strain(o[20]), Is.EqualTo(eval(o[20]) * 1.5).Within(1e-9));
        }

        // CF: Stamina.StrainValueAt | isConvert | TRUE
        [Test]
        public void TestConvertHasNoMonoLengthBonus()
        {
            var o = create(true);
            Assert.That(new TestStamina(false, true).Strain(o[20]), Is.EqualTo(eval(o[20])).Within(1e-9));
        }

        // CF: Stamina.StrainValueAt | !SingleColourStamina | FALSE
        // CF: Stamina.StrainValueAt | SingleColourStamina | TRUE
        [Test]
        public void TestSingleColourSkipsBonusAndIsHalvedAtIndexTen()
        {
            var o = create(true);
            // logistic midpoint at index 10 gives half the strain; no mono length bonus applies.
            Assert.That(new TestStamina(true, false).Strain(o[10]), Is.EqualTo(eval(o[10]) / 2).Within(1e-9));
        }

        [Test]
        public void TestStrainDecaysBetweenObjects()
        {
            var o = create(false);
            var skill = new TestStamina(false, true);
            double first = skill.Strain(o[5]);
            double second = skill.Strain(o[6]);
            Assert.That(second, Is.EqualTo(first * Math.Pow(0.4, 0.1) + eval(o[6])).Within(1e-9));
        }

        // CF: Stamina.CalculateInitialStrain | SingleColourStamina | TRUE
        [Test]
        public void TestInitialStrainSingleColourIsZero()
        {
            var o = create(false);
            var skill = new TestStamina(true, false);
            skill.Strain(o[5]);
            Assert.That(skill.Initial(o[5].StartTime, o[6]), Is.EqualTo(0));
        }

        // CF: Stamina.CalculateInitialStrain | SingleColourStamina | FALSE
        [Test]
        public void TestInitialStrainDecaysFromPreviousObject()
        {
            var o = create(false);
            var skill = new TestStamina(false, true);
            double strain = skill.Strain(o[5]);
            // 100ms after o[5] -> o[6].Previous(0) is o[5]
            Assert.That(skill.Initial(o[5].StartTime + 100, o[6]), Is.EqualTo(strain * Math.Pow(0.4, 0.1)).Within(1e-9));
        }
    }
}
