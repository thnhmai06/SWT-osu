// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Osu.Difficulty.Skills;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Tests.Difficulty
{
    [TestFixture]
    public class OsuDifficultyCalculatorTest
    {
        private static OsuDifficultyAttributes calculate(System.Collections.Generic.IEnumerable<OsuHitObject> objects, params Mod[] mods)
            => (OsuDifficultyAttributes)new DifficultyTestHelpers.ExposedCalculator(DifficultyTestHelpers.Working(objects)).Calculate(mods);

        // ---- SumCognitionDifficulty (exact: guards and clamp are from the code's intent) ----

        // reading <= 0 TRUE -> flashlight only.
        [TestCase(0, 5)]
        [TestCase(-1, 5)]
        public void TestSumCognition_NonPositiveReading_ReturnsFlashlight(double reading, double flashlight)
            => Assert.That(OsuDifficultyCalculator.SumCognitionDifficulty(reading, flashlight), Is.EqualTo(flashlight));

        // reading <= 0 FALSE, flashlight <= 0 TRUE -> reading only.
        [TestCase(5, 0)]
        [TestCase(5, -2)]
        public void TestSumCognition_NonPositiveFlashlight_ReturnsReading(double reading, double flashlight)
            => Assert.That(OsuDifficultyCalculator.SumCognitionDifficulty(reading, flashlight), Is.EqualTo(reading));

        // Both guards FALSE: result is a norm so it is at least the larger input and below the plain sum.
        [Test]
        public void TestSumCognition_BothPositive_IsBetweenMaxAndSum()
        {
            double result = OsuDifficultyCalculator.SumCognitionDifficulty(4, 3);

            Assert.That(result, Is.GreaterThanOrEqualTo(4));
            Assert.That(result, Is.LessThan(4 + 3));
        }

        // Clamp lower bound: flashlight/reading < 0.25 -> factor 0.25; so a tiny flashlight is weighted by exactly 0.25.
        [Test]
        public void TestSumCognition_TinyFlashlight_UsesLowerClampFactor()
        {
            double a = OsuDifficultyCalculator.SumCognitionDifficulty(100, 1);
            double b = OsuDifficultyCalculator.SumCognitionDifficulty(100, 2);
            double expectedA = norm(100, 1 * 0.25);
            double expectedB = norm(100, 2 * 0.25);

            Assert.That(a, Is.EqualTo(expectedA).Within(1e-9));
            Assert.That(b, Is.EqualTo(expectedB).Within(1e-9));
        }

        // Clamp upper bound: flashlight >= reading -> factor 1.
        [Test]
        public void TestSumCognition_LargerFlashlight_UsesUpperClampFactor()
        {
            double result = OsuDifficultyCalculator.SumCognitionDifficulty(3, 10);

            Assert.That(result, Is.EqualTo(norm(3, 10)).Within(1e-9));
        }

        // Mid range: factor = flashlight / reading (not clamped).
        [Test]
        public void TestSumCognition_MidRange_UsesRatioFactor()
        {
            double result = OsuDifficultyCalculator.SumCognitionDifficulty(10, 5);

            Assert.That(result, Is.EqualTo(norm(10, 5 * 0.5)).Within(1e-9));
        }

        private static double norm(double a, double b)
        {
            double p = OsuPerformanceCalculator.PERFORMANCE_NORM_EXPONENT;
            return Math.Pow(Math.Pow(a, p) + Math.Pow(b, p), 1 / p);
        }

        // ---- CreateSkills ----

        // No Flashlight mod -> flashlight skill absent; Aim twice (with / without sliders), one Speed, one Reading.
        [Test]
        public void TestCreateSkills_WithoutFlashlight_HasNoFlashlightSkill()
        {
            var skills = createSkills();

            Assert.That(skills.OfType<Aim>().Count(), Is.EqualTo(2));
            Assert.That(skills.OfType<Aim>().Select(a => a.IncludeSliders), Is.EquivalentTo(new[] { true, false }));
            Assert.That(skills.OfType<Speed>().Count(), Is.EqualTo(1));
            Assert.That(skills.OfType<Reading>().Count(), Is.EqualTo(1));
            Assert.That(skills.OfType<Flashlight>(), Is.Empty);
        }

        // Flashlight mod present -> flashlight skill created.
        [Test]
        public void TestCreateSkills_WithFlashlight_HasFlashlightSkill()
        {
            var skills = createSkills(new OsuModFlashlight());

            Assert.That(skills.OfType<Flashlight>().Count(), Is.EqualTo(1));
        }

        private static Skill[] createSkills(params Mod[] mods)
        {
            var objects = DifficultyTestHelpers.Stream(10, 200, 60);
            var working = DifficultyTestHelpers.Working(objects);
            var playable = working.GetPlayableBeatmap(new OsuRuleset().RulesetInfo, mods);
            return new DifficultyTestHelpers.ExposedCalculator(working).Skills(playable, mods);
        }

        // ---- Calculate (end to end) ----

        // Empty beatmap: guard for no objects -> zeroed ratings (no throw).
        [Test]
        public void TestCalculate_NoObjects_ZeroStarRating()
        {
            var attributes = calculate(Array.Empty<OsuHitObject>());

            Assert.That(attributes.StarRating, Is.Zero);
            Assert.That(attributes.AimDifficulty, Is.Zero);
            Assert.That(attributes.SpeedDifficulty, Is.Zero);
        }

        // Object counts are reflected in attributes.
        [Test]
        public void TestCalculate_CountsObjectTypes()
        {
            var attributes = calculate(new OsuHitObject[]
            {
                DifficultyTestHelpers.Circle(0, 100, 100),
                DifficultyTestHelpers.Circle(300, 200, 100),
                DifficultyTestHelpers.Slider(600, 100, 200, 100),
                DifficultyTestHelpers.Spinner(1500, 1000),
            });

            Assert.That(attributes.HitCircleCount, Is.EqualTo(2));
            Assert.That(attributes.SliderCount, Is.EqualTo(1));
            Assert.That(attributes.SpinnerCount, Is.EqualTo(1));
        }

        // Harder (faster, farther) map -> higher star rating than an easy one.
        [Test]
        public void TestCalculate_HarderMap_HasHigherStarRating()
        {
            var easy = calculate(DifficultyTestHelpers.Stream(30, 600, 40));
            var hard = calculate(DifficultyTestHelpers.Stream(30, 120, 120));

            Assert.That(hard.StarRating, Is.GreaterThan(easy.StarRating));
            Assert.That(hard.AimDifficulty, Is.GreaterThan(easy.AimDifficulty));
            Assert.That(hard.SpeedDifficulty, Is.GreaterThan(easy.SpeedDifficulty));
        }

        // Circle-only map: aim with and without sliders is identical -> slider factor 1.
        [Test]
        public void TestCalculate_NoSliders_SliderFactorIsOne()
        {
            var attributes = calculate(DifficultyTestHelpers.Stream(20, 200, 80));

            Assert.That(attributes.SliderFactor, Is.EqualTo(1).Within(1e-9));
        }

        // Without Flashlight mod the flashlight rating is 0 (branch flashlight is null).
        [Test]
        public void TestCalculate_WithoutFlashlight_FlashlightDifficultyIsZero()
        {
            var attributes = calculate(DifficultyTestHelpers.Stream(20, 200, 80));

            Assert.That(attributes.FlashlightDifficulty, Is.Zero);
        }

        // With Flashlight mod the rating is positive.
        [Test]
        public void TestCalculate_WithFlashlight_FlashlightDifficultyPositive()
        {
            var attributes = calculate(DifficultyTestHelpers.Stream(30, 200, 100), new OsuModFlashlight());

            Assert.That(attributes.FlashlightDifficulty, Is.GreaterThan(0));
        }

        // Clock rate mods (DT) raise difficulty on the same map.
        [Test]
        public void TestCalculate_DoubleTime_RaisesStarRating()
        {
            var normal = calculate(DifficultyTestHelpers.Stream(30, 250, 90));
            var doubleTime = calculate(DifficultyTestHelpers.Stream(30, 250, 90), new OsuModDoubleTime());

            Assert.That(doubleTime.StarRating, Is.GreaterThan(normal.StarRating));
        }

        // Mods stored on the attributes.
        [Test]
        public void TestCalculate_ModsAreStoredOnAttributes()
        {
            var attributes = calculate(DifficultyTestHelpers.Stream(10, 250, 90), new OsuModHidden());

            Assert.That(attributes.Mods.Select(m => m.GetType()), Is.EqualTo(new[] { typeof(OsuModHidden) }));
        }
    }
}
