// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Skills;
using osu.Game.Rulesets.Osu.Mods;

namespace osu.Game.Rulesets.Osu.Tests.Difficulty
{
    [TestFixture]
    public class OsuSkillsTest
    {
        // Flashlight.DifficultyToPerformance = 25 * d^2 (documented constant).
        [TestCase(0, 0)]
        [TestCase(1, 25)]
        [TestCase(2, 100)]
        public void TestFlashlightDifficultyToPerformance(double difficulty, double expected)
            => Assert.That(Flashlight.DifficultyToPerformance(difficulty), Is.EqualTo(expected).Within(1e-9));

        // Count == 0 guards on a skill that has processed nothing.
        [Test]
        public void TestAim_NoObjects_SliderHelpersReturnZero()
        {
            var aim = new Aim(Array.Empty<Mod>(), true);

            Assert.That(aim.GetDifficultSliders(), Is.Zero);
            Assert.That(aim.CountTopWeightedSliders(10), Is.Zero);
        }

        [Test]
        public void TestSpeed_NoObjects_HelpersReturnZero()
        {
            var speed = new Speed(Array.Empty<Mod>());

            Assert.That(speed.RelevantObjectCount(), Is.Zero);
            Assert.That(speed.CountTopWeightedSliders(10), Is.Zero);
        }

        private static double aimValue(Mod[] mods, bool sliders, double deltaTime, float spacing)
        {
            var aim = new Aim(mods, sliders);
            foreach (var o in DifficultyTestHelpers.DifficultyObjects(DifficultyTestHelpers.Stream(30, deltaTime, spacing), mods))
                aim.Process(o);
            return aim.DifficultyValue();
        }

        private static double speedValue(Mod[] mods, double deltaTime)
        {
            var speed = new Speed(mods);
            foreach (var o in DifficultyTestHelpers.DifficultyObjects(DifficultyTestHelpers.Stream(30, deltaTime, 60), mods))
                speed.Process(o);
            return speed.DifficultyValue();
        }

        // Faster/larger stream -> larger aim strain.
        [Test]
        public void TestAim_HarderStream_HasHigherDifficulty()
            => Assert.That(aimValue(Array.Empty<Mod>(), true, 120, 120), Is.GreaterThan(aimValue(Array.Empty<Mod>(), true, 600, 40)));

        // Faster stream -> larger speed difficulty.
        [Test]
        public void TestSpeed_FasterStream_HasHigherDifficulty()
            => Assert.That(speedValue(Array.Empty<Mod>(), 100), Is.GreaterThan(speedValue(Array.Empty<Mod>(), 400)));

        // Autopilot branch halves speed difficulty (ObjectDifficultyOf *= 0.5, aggregation linear in per-object values).
        [Test]
        public void TestSpeed_Autopilot_LowersDifficulty()
            => Assert.That(speedValue(new Mod[] { new OsuModAutopilot() }, 100), Is.LessThan(speedValue(Array.Empty<Mod>(), 100)));

        // After processing, Speed.RelevantObjectCount is positive and bounded by the object count.
        [Test]
        public void TestSpeed_RelevantObjectCount_BoundedByObjects()
        {
            var speed = new Speed(Array.Empty<Mod>());
            foreach (var o in DifficultyTestHelpers.DifficultyObjects(DifficultyTestHelpers.Stream(30, 150, 60)))
                speed.Process(o);

            Assert.That(speed.RelevantObjectCount(), Is.InRange(0.1, 30.0));
        }
    }
}
