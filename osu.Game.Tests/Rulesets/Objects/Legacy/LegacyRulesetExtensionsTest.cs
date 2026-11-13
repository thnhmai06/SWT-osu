// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Tests.Rulesets.Objects.Legacy
{
    [TestFixture]
    public class LegacyRulesetExtensionsTest
    {
        private class Velocity : IHasSliderVelocity
        {
            public Velocity(double multiplier) => SliderVelocityMultiplierBindable = new BindableDouble(multiplier);

            public double SliderVelocityMultiplier
            {
                get => SliderVelocityMultiplierBindable.Value;
                set => SliderVelocityMultiplierBindable.Value = value;
            }

            public BindableNumber<double> SliderVelocityMultiplierBindable { get; }
        }

        private static readonly TimingControlPoint timing = new TimingControlPoint { BeatLength = 500 };

        private static double beatLength(double sv, string ruleset) => LegacyRulesetExtensions.GetPrecisionAdjustedBeatLength(new Velocity(sv), timing, ruleset);

        // CF: LegacyRulesetExtensions.GetPrecisionAdjustedBeatLength | switch (rulesetShortName) | case osu
        // CF: LegacyRulesetExtensions.GetPrecisionAdjustedBeatLength | switch (rulesetShortName) | case fruits
        // CF: LegacyRulesetExtensions.GetPrecisionAdjustedBeatLength | sliderVelocityAsBeatLength < 0 | TRUE
        [TestCase("osu")]
        [TestCase("fruits")]
        public void TestOsuAndFruitsScaling(string ruleset)
        {
            Assert.That(beatLength(2, ruleset), Is.EqualTo(250), "2x velocity halves the beat length");
            Assert.That(beatLength(0.5, ruleset), Is.EqualTo(1000));
        }

        [TestCase("osu")]
        [TestCase("fruits")]
        public void TestOsuAndFruitsClamping(string ruleset)
        {
            Assert.That(beatLength(0.01, ruleset), Is.EqualTo(500 * 10), "multiplier is clamped to 10x");
            Assert.That(beatLength(100, ruleset), Is.EqualTo(500 * 0.1), "multiplier is clamped to 0.1x");
        }

        // CF: LegacyRulesetExtensions.GetPrecisionAdjustedBeatLength | switch (rulesetShortName) | case taiko
        // CF: LegacyRulesetExtensions.GetPrecisionAdjustedBeatLength | switch (rulesetShortName) | case mania
        [TestCase("taiko")]
        [TestCase("mania")]
        public void TestTaikoAndManiaScaling(string ruleset)
        {
            Assert.That(beatLength(2, ruleset), Is.EqualTo(250));
            Assert.That(beatLength(0.01, ruleset), Is.EqualTo(500 * 100), "multiplier allowed up to 100x");
            Assert.That(beatLength(0.0001, ruleset), Is.EqualTo(500 * 100), "multiplier is clamped to 100x");
        }

        // CF: LegacyRulesetExtensions.GetPrecisionAdjustedBeatLength | sliderVelocityAsBeatLength < 0 | FALSE
        [TestCase("osu")]
        [TestCase("taiko")]
        public void TestNonPositiveSliderVelocityLeavesBeatLength(string ruleset)
        {
            Assert.That(beatLength(-1, ruleset), Is.EqualTo(500));
        }

        // CF: LegacyRulesetExtensions.GetPrecisionAdjustedBeatLength | switch (rulesetShortName) | default
        [Test]
        public void TestNonLegacyRulesetRejected()
        {
            Assert.That(() => beatLength(1, "custom"), Throws.InstanceOf<ArgumentException>());
        }

        [TestCase(5f, 0.5f)]
        [TestCase(0f, 0.85f)]
        [TestCase(10f, 0.15f)]
        public void TestScaleFromCircleSize(float cs, float expected)
        {
            Assert.That(LegacyRulesetExtensions.CalculateScaleFromCircleSize(cs), Is.EqualTo(expected).Within(1e-5));
        }

        // CF: LegacyRulesetExtensions.CalculateScaleFromCircleSize | applyFudge | TRUE
        [Test]
        public void TestScaleWithFudgeIsSlightlyLarger()
        {
            Assert.That(LegacyRulesetExtensions.CalculateScaleFromCircleSize(5, true), Is.EqualTo(0.5f * 1.00041f).Within(1e-6));
        }

        private static BeatmapDifficulty difficulty(float hp, float od, float cs) => new BeatmapDifficulty { DrainRate = hp, OverallDifficulty = od, CircleSize = cs };

        // CF: LegacyRulesetExtensions.CalculateDifficultyPeppyStars | drainLength != 0 | TRUE
        [TestCase(5f, 5f, 5f, 100, 100, 3)]
        [TestCase(10f, 10f, 10f, 1000, 100, 6)]
        [TestCase(0f, 0f, 0f, 0, 100, 0)]
        public void TestPeppyStars(float hp, float od, float cs, int objects, int drain, int expected)
        {
            Assert.That(LegacyRulesetExtensions.CalculateDifficultyPeppyStars(difficulty(hp, od, cs), objects, drain), Is.EqualTo(expected));
        }

        // CF: LegacyRulesetExtensions.CalculateDifficultyPeppyStars | drainLength != 0 | FALSE
        [Test]
        public void TestPeppyStarsWithoutDrainUsesMaximumRatio()
        {
            Assert.That(LegacyRulesetExtensions.CalculateDifficultyPeppyStars(difficulty(5, 5, 5), 100, 0), Is.EqualTo(4));
        }
    }
}
