// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Skills;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using static osu.Game.Rulesets.Osu.Tests.Difficulty.DifficultyTestHelper;

namespace osu.Game.Rulesets.Osu.Tests.Difficulty.Skills
{
    [TestFixture]
    public class OsuSkillsTest
    {
        private static readonly Mod[] no_mods = System.Array.Empty<Mod>();

        private static List<DifficultyHitObject> pattern(bool withSliders = false)
        {
            var objects = new List<OsuHitObject>();

            for (int i = 0; i < 40; i++)
            {
                double t = 1000 + i * 150;

                if (withSliders && i % 4 == 2)
                    objects.Add(Slider(t, 100 + (i % 8) * 50, 100 + (i % 3) * 40, 60, 40));
                else
                    objects.Add(Circle(t, 100 + (i % 7) * 50, 100 + (i % 5) * 40));
            }

            return Build(objects);
        }

        private static T run<T>(T skill, List<DifficultyHitObject> objects)
            where T : Skill
        {
            foreach (var o in objects)
                skill.Process(o);

            return skill;
        }

        private static double aim(Mod[] mods, bool sliders = true, bool withSliders = true) => run(new Aim(mods, sliders), pattern(withSliders)).DifficultyValue();
        private static double speed(Mod[] mods) => run(new Speed(mods), pattern()).DifficultyValue();
        private static double reading(Mod[] mods) => run(new Reading(mods), pattern()).DifficultyValue();
        private static double flashlight(Mod[] mods, int total = 400) => run(new Flashlight(mods, total), pattern()).DifficultyValue();

        // CF: Aim.StrainValueAt | Mods.Any(m => m is OsuModAutopilot) | TRUE
        [Test]
        public void TestAimAutopilotHasNoDifficulty()
        {
            Assert.That(aim(new Mod[] { new OsuModAutopilot() }), Is.EqualTo(0));
        }

        // CF: Aim.StrainValueAt | Mods.Any(m => m is OsuModAutopilot) | FALSE
        // CF: Aim.StrainValueAt | current.BaseObject is Slider | TRUE
        // CF: Aim.StrainValueAt | current.BaseObject is Slider | FALSE
        [Test]
        public void TestAimHasDifficulty()
        {
            Assert.That(aim(no_mods), Is.GreaterThan(0));
        }

        // CF: Aim.calculateAdjustedDifficulty | Mods.Any(m => m is OsuModMagnetised) | TRUE
        [Test]
        public void TestAimMagnetisedReducesDifficulty()
        {
            Assert.That(aim(new Mod[] { new OsuModMagnetised { AttractionStrength = { Value = 0.5f } } }), Is.EqualTo(aim(no_mods) * 0.5).Within(1e-6));
        }

        // CF: Aim.calculateTotalValue | Mods.Any(m => m is OsuModTouchDevice) | TRUE
        // CF: Aim.calculateTotalValue | Mods.Any(m => m is OsuModRelax) | TRUE
        [Test]
        public void TestAimModsChangeDifficulty()
        {
            double baseline = aim(no_mods);

            Assert.That(aim(new Mod[] { new OsuModTouchDevice() }), Is.Not.EqualTo(baseline));
            Assert.That(aim(new Mod[] { new OsuModRelax() }), Is.LessThan(baseline));
        }

        // CF: Aim.GetDifficultSliders | sliderStrains.Count == 0 | TRUE
        // CF: Aim.CountTopWeightedSliders | sliderStrains.Count == 0 | TRUE
        [Test]
        public void TestAimWithoutSliders()
        {
            var skill = run(new Aim(no_mods, true), pattern(false));

            Assert.That(skill.GetDifficultSliders(), Is.EqualTo(0));
            Assert.That(skill.CountTopWeightedSliders(skill.DifficultyValue()), Is.EqualTo(0));
        }

        // CF: Aim.GetDifficultSliders | sliderStrains.Count == 0 | FALSE
        // CF: Aim.GetDifficultSliders | maxSliderStrain == 0 | FALSE
        // CF: Aim.CountTopWeightedSliders | sliderStrains.Count == 0 | FALSE
        // CF: Aim.CountTopWeightedSliders | consistentTopStrain == 0 | FALSE
        [Test]
        public void TestAimWithSliders()
        {
            var skill = run(new Aim(no_mods, true), pattern(true));

            Assert.That(skill.GetDifficultSliders(), Is.GreaterThan(0));
            Assert.That(skill.CountTopWeightedSliders(skill.DifficultyValue()), Is.GreaterThan(0));
        }

        // CF: Aim.GetDifficultSliders | maxSliderStrain == 0 | TRUE
        // CF: Aim.CountTopWeightedSliders | consistentTopStrain == 0 | TRUE
        [Test]
        public void TestAimSlidersWithZeroStrain()
        {
            var skill = run(new Aim(new Mod[] { new OsuModAutopilot() }, true), pattern(true));

            Assert.That(skill.GetDifficultSliders(), Is.EqualTo(0));
            Assert.That(skill.CountTopWeightedSliders(0), Is.EqualTo(0));
        }

        [Test]
        public void TestAimWithoutSliderTravelIsNotHarder()
        {
            Assert.That(aim(no_mods, sliders: false), Is.LessThanOrEqualTo(aim(no_mods, sliders: true)));
        }

        // CF: Speed.ObjectDifficultyOf | Mods.Any(m => m is OsuModRelax) | TRUE
        [Test]
        public void TestSpeedRelaxHasNoDifficulty()
        {
            Assert.That(speed(new Mod[] { new OsuModRelax() }), Is.EqualTo(0));
        }

        // CF: Speed.ObjectDifficultyOf | Mods.Any(m => m is OsuModRelax) | FALSE
        // CF: Speed.calculateAdjustedDifficulty | Mods.Any(m => m is OsuModAutopilot) | FALSE
        [Test]
        public void TestSpeedHasDifficulty()
        {
            Assert.That(speed(no_mods), Is.GreaterThan(0));
        }

        // CF: Speed.calculateAdjustedDifficulty | Mods.Any(m => m is OsuModAutopilot) | TRUE
        [Test]
        public void TestSpeedAutopilotIsEasier()
        {
            Assert.That(speed(new Mod[] { new OsuModAutopilot() }), Is.LessThan(speed(no_mods)));
        }

        // CF: Speed.RelevantObjectCount | ObjectDifficulties.Count == 0 | TRUE
        // CF: Speed.CountTopWeightedSliders | sliderStrains.Count == 0 | TRUE
        [Test]
        public void TestSpeedWithoutObjects()
        {
            var skill = new Speed(no_mods);

            Assert.That(skill.RelevantObjectCount(), Is.EqualTo(0));
            Assert.That(skill.CountTopWeightedSliders(1), Is.EqualTo(0));
        }

        // CF: Speed.RelevantObjectCount | ObjectDifficulties.Count == 0 | FALSE
        // CF: Speed.RelevantObjectCount | maxStrain == 0 | FALSE
        // CF: Speed.CountTopWeightedSliders | sliderStrains.Count == 0 | FALSE
        // CF: Speed.CountTopWeightedSliders | ObjectWeightSum == 0 | FALSE
        // CF: Speed.CountTopWeightedSliders | consistentTopObject == 0 | FALSE
        // CF: Speed.ObjectDifficultyOf | current.BaseObject is Slider | TRUE
        [Test]
        public void TestSpeedWithSliders()
        {
            var skill = run(new Speed(no_mods), pattern(true));

            Assert.That(skill.RelevantObjectCount(), Is.GreaterThan(0));
            Assert.That(skill.CountTopWeightedSliders(skill.DifficultyValue()), Is.GreaterThan(0));
        }

        // CF: Speed.RelevantObjectCount | maxStrain == 0 | TRUE
        // CF: Speed.CountTopWeightedSliders | consistentTopObject == 0 | TRUE
        [Test]
        public void TestSpeedRelevantObjectsWithZeroStrain()
        {
            var skill = run(new Speed(new Mod[] { new OsuModRelax() }), pattern(true));

            Assert.That(skill.RelevantObjectCount(), Is.EqualTo(0));
            Assert.That(skill.CountTopWeightedSliders(0), Is.EqualTo(0));
        }

        [Test]
        public void TestReadingHasDifficultyAndReductionMods()
        {
            double baseline = reading(no_mods);

            Assert.That(baseline, Is.GreaterThan(0));
            Assert.That(reading(new Mod[] { new OsuModRelax() }), Is.LessThan(baseline));
            Assert.That(reading(new Mod[] { new OsuModAutopilot() }), Is.LessThan(baseline));
            Assert.That(reading(new Mod[] { new OsuModMagnetised { AttractionStrength = { Value = 0.5f } } }), Is.LessThan(baseline));
            Assert.That(reading(new Mod[] { new OsuModTouchDevice() }), Is.Not.EqualTo(baseline));
        }

        // CF: Reading.ctor | m => !m.OnlyFadeApproachCircles.Value | TRUE
        [Test]
        public void TestReadingFullHiddenIsHarder()
        {
            Assert.That(reading(new Mod[] { new OsuModHidden() }), Is.GreaterThanOrEqualTo(reading(no_mods)));
        }

        // CF: Reading.ctor | m => !m.OnlyFadeApproachCircles.Value | FALSE
        [Test]
        public void TestReadingApproachCircleOnlyHiddenIsIgnored()
        {
            Assert.That(reading(new Mod[] { new OsuModHidden { OnlyFadeApproachCircles = { Value = true } } }), Is.EqualTo(reading(no_mods)).Within(1e-9));
        }

        // CF: Reading.CountTopWeightedObjectDifficulties | ObjectDifficulties.Count == 0 | TRUE
        [Test]
        public void TestReadingCountWithoutObjects()
        {
            Assert.That(new Reading(no_mods).CountTopWeightedObjectDifficulties(1), Is.EqualTo(0));
        }

        // CF: Reading.CountTopWeightedObjectDifficulties | ObjectDifficulties.Count == 0 | FALSE
        // CF: Reading.CountTopWeightedObjectDifficulties | ObjectWeightSum == 0 | FALSE
        // CF: Reading.CountTopWeightedObjectDifficulties | consistentTopNote == 0 | FALSE
        [Test]
        public void TestReadingCountWithObjects()
        {
            var skill = run(new Reading(no_mods), pattern());
            Assert.That(skill.CountTopWeightedObjectDifficulties(skill.DifficultyValue()), Is.GreaterThan(0));
        }

        // CF: Reading.CountTopWeightedObjectDifficulties | consistentTopNote == 0 | TRUE
        [Test]
        public void TestReadingCountWithZeroDifficulty()
        {
            var skill = run(new Reading(no_mods), pattern());
            Assert.That(skill.CountTopWeightedObjectDifficulties(0), Is.EqualTo(0));
        }

        // CF: Flashlight.StrainValueAt | !Mods.Any(m => m is OsuModFlashlight) | TRUE
        [Test]
        public void TestFlashlightSkillRequiresMod()
        {
            Assert.That(flashlight(no_mods), Is.EqualTo(0));
        }

        // CF: Flashlight.StrainValueAt | !Mods.Any(m => m is OsuModFlashlight) | FALSE
        [Test]
        public void TestFlashlightSkillWithMod()
        {
            Assert.That(flashlight(new Mod[] { new OsuModFlashlight() }), Is.GreaterThan(0));
        }

        // CF: Flashlight.DifficultyValue | totalObjects > 200 | TRUE
        [Test]
        public void TestFlashlightLongerMapsCountMore()
        {
            var mods = new Mod[] { new OsuModFlashlight() };

            Assert.That(flashlight(mods, 400), Is.GreaterThan(flashlight(mods, 200)));
            Assert.That(flashlight(mods, 200), Is.GreaterThan(flashlight(mods, 50)));
            Assert.That(flashlight(mods, 1000), Is.EqualTo(flashlight(mods, 400)).Within(1e-9));
        }

        // CF: Flashlight.DifficultyValue | totalObjects > 200 | FALSE
        [Test]
        public void TestFlashlightShortMapsAreScaledDown()
        {
            var mods = new Mod[] { new OsuModFlashlight() };
            Assert.That(flashlight(mods, 100), Is.LessThan(flashlight(mods, 200)));
        }

        [Test]
        public void TestFlashlightModifiers()
        {
            double baseline = flashlight(new Mod[] { new OsuModFlashlight() });

            Assert.That(flashlight(new Mod[] { new OsuModFlashlight(), new OsuModTouchDevice() }), Is.Not.EqualTo(baseline));
            Assert.That(flashlight(new Mod[] { new OsuModFlashlight(), new OsuModMagnetised { AttractionStrength = { Value = 0.5f } } }), Is.LessThan(baseline));
            Assert.That(flashlight(new Mod[] { new OsuModFlashlight(), new OsuModDeflate { StartScale = { Value = 10 } } }), Is.LessThan(baseline));
            Assert.That(flashlight(new Mod[] { new OsuModFlashlight(), new OsuModRelax() }), Is.LessThan(baseline));
            Assert.That(flashlight(new Mod[] { new OsuModFlashlight(), new OsuModAutopilot() }), Is.LessThan(baseline));
        }

        [Test]
        public void TestFlashlightDifficultyToPerformanceIsQuadratic()
        {
            Assert.That(Flashlight.DifficultyToPerformance(2), Is.EqualTo(4 * Flashlight.DifficultyToPerformance(1)).Within(1e-9));
        }
    }
}
