// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Taiko.Mods;

namespace osu.Game.Rulesets.Taiko.Tests.Mods
{
    [TestFixture]
    public class TaikoModDifficultyAdjustTest
    {
        [SetUp]
        public void SetUp() => System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        // CF: TaikoModDifficultyAdjust.ExtendedIconInformation | !IsExactlyOneSettingChanged | TRUE
        [Test]
        public void TestNoSettingChangedGivesEmpty() => Assert.That(new TaikoModDifficultyAdjust().ExtendedIconInformation, Is.Empty);

        // CF: TaikoModDifficultyAdjust.ExtendedIconInformation | !IsExactlyOneSettingChanged | TRUE
        [Test]
        public void TestTwoSettingsChangedGivesEmpty()
        {
            var mod = new TaikoModDifficultyAdjust();
            mod.ScrollSpeed.Value = 2;
            mod.OverallDifficulty.Value = 7;
            Assert.That(mod.ExtendedIconInformation, Is.Empty);
        }

        // CF: TaikoModDifficultyAdjust.ExtendedIconInformation | !IsExactlyOneSettingChanged | FALSE
        // CF: TaikoModDifficultyAdjust.ExtendedIconInformation | !ScrollSpeed.IsDefault | TRUE
        [Test]
        public void TestScrollSpeedOnly()
        {
            var mod = new TaikoModDifficultyAdjust();
            mod.ScrollSpeed.Value = 2.25f;
            Assert.That(mod.ExtendedIconInformation, Is.EqualTo("SC2.25"));
        }

        // CF: TaikoModDifficultyAdjust.ExtendedIconInformation | !ScrollSpeed.IsDefault | FALSE
        // CF: TaikoModDifficultyAdjust.ExtendedIconInformation | !OverallDifficulty.IsDefault | TRUE
        [Test]
        public void TestOverallDifficultyOnly()
        {
            var mod = new TaikoModDifficultyAdjust();
            mod.OverallDifficulty.Value = 7.5f;
            Assert.That(mod.ExtendedIconInformation, Is.EqualTo("OD7.5"));
        }

        // CF: TaikoModDifficultyAdjust.ExtendedIconInformation | !OverallDifficulty.IsDefault | FALSE
        // CF: TaikoModDifficultyAdjust.ExtendedIconInformation | !DrainRate.IsDefault | TRUE
        [Test]
        public void TestDrainRateOnly()
        {
            var mod = new TaikoModDifficultyAdjust();
            mod.DrainRate.Value = 3.5f;
            Assert.That(mod.ExtendedIconInformation, Is.EqualTo("HP3.5"));
        }

        // infeasible: TaikoModDifficultyAdjust.ExtendedIconInformation final `return string.Empty` and `!DrainRate.IsDefault` FALSE: IsExactlyOneSettingChanged(ScrollSpeed, OD, HP) guarantees exactly one of the three is non-default

        // CF: TaikoModDifficultyAdjust.SettingDescription | foreach base.SettingDescription | loop 0
        // CF: TaikoModDifficultyAdjust.SettingDescription | !ScrollSpeed.IsDefault | FALSE
        [Test]
        public void TestDescriptionEmptyByDefault() => Assert.That(new TaikoModDifficultyAdjust().SettingDescription, Is.Empty);

        // CF: TaikoModDifficultyAdjust.SettingDescription | foreach base.SettingDescription | loop 1
        // CF: TaikoModDifficultyAdjust.SettingDescription | !ScrollSpeed.IsDefault | TRUE
        [Test]
        public void TestDescriptionIncludesScrollSpeed()
        {
            var mod = new TaikoModDifficultyAdjust();
            mod.ScrollSpeed.Value = 2;
            Assert.That(mod.SettingDescription.Last().value.ToString(), Is.EqualTo("x2.00"));
        }

        // CF: TaikoModDifficultyAdjust.SettingDescription | foreach base.SettingDescription | loop n
        [Test]
        public void TestDescriptionIncludesBaseSettings()
        {
            var mod = new TaikoModDifficultyAdjust();
            mod.ScrollSpeed.Value = 2;
            mod.DrainRate.Value = 3;
            Assert.That(mod.SettingDescription.Count(), Is.EqualTo(2));
        }

        // CF: TaikoModDifficultyAdjust.ApplySettings | ScrollSpeed.Value != null | TRUE
        [Test]
        public void TestApplyMultipliesSliderMultiplier()
        {
            var mod = new TaikoModDifficultyAdjust();
            mod.ScrollSpeed.Value = 2;
            var difficulty = new BeatmapDifficulty { SliderMultiplier = 1.4 };
            mod.ApplyToDifficulty(difficulty);
            Assert.That(difficulty.SliderMultiplier, Is.EqualTo(2.8).Within(1e-9));
        }

        // CF: TaikoModDifficultyAdjust.ApplySettings | ScrollSpeed.Value != null | FALSE
        [Test]
        public void TestApplyWithoutScrollSpeedKeepsSliderMultiplier()
        {
            var difficulty = new BeatmapDifficulty { SliderMultiplier = 1.4 };
            new TaikoModDifficultyAdjust().ApplyToDifficulty(difficulty);
            Assert.That(difficulty.SliderMultiplier, Is.EqualTo(1.4));
        }
    }
}
