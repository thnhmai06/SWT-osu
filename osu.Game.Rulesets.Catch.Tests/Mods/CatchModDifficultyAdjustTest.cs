// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Beatmaps;
using osu.Game.Rulesets.Catch.Mods;

namespace osu.Game.Rulesets.Catch.Tests.Mods
{
    [TestFixture]
    public class CatchModDifficultyAdjustTest
    {
        private CultureInfo? previousCulture;

        [SetUp]
        public void SetUp()
        {
            previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        [TearDown]
        public void TearDown() => CultureInfo.CurrentCulture = previousCulture!;

        // CF: CatchModDifficultyAdjust.ExtendedIconInformation | !IsExactlyOneSettingChanged | TRUE
        [Test]
        public void TestIconInformationEmptyWhenNothingOrManyChanged()
        {
            Assert.That(new CatchModDifficultyAdjust().ExtendedIconInformation, Is.Empty);
            Assert.That(new CatchModDifficultyAdjust { CircleSize = { Value = 3 }, ApproachRate = { Value = 4 } }.ExtendedIconInformation, Is.Empty);
        }

        // CF: CatchModDifficultyAdjust.ExtendedIconInformation | !CircleSize.IsDefault | TRUE
        [Test]
        public void TestIconInformationCircleSize()
            => Assert.That(new CatchModDifficultyAdjust { CircleSize = { Value = 3 } }.ExtendedIconInformation, Is.EqualTo("CS3"));

        // CF: CatchModDifficultyAdjust.ExtendedIconInformation | !CircleSize.IsDefault | FALSE
        // CF: CatchModDifficultyAdjust.ExtendedIconInformation | !ApproachRate.IsDefault | TRUE
        [Test]
        public void TestIconInformationApproachRate()
            => Assert.That(new CatchModDifficultyAdjust { ApproachRate = { Value = 4 } }.ExtendedIconInformation, Is.EqualTo("AR4"));

        // CF: CatchModDifficultyAdjust.ExtendedIconInformation | !ApproachRate.IsDefault | FALSE
        // CF: CatchModDifficultyAdjust.ExtendedIconInformation | !OverallDifficulty.IsDefault | TRUE
        [Test]
        public void TestIconInformationOverallDifficulty()
            => Assert.That(new CatchModDifficultyAdjust { OverallDifficulty = { Value = 6 } }.ExtendedIconInformation, Is.EqualTo("OD6"));

        // CF: CatchModDifficultyAdjust.ExtendedIconInformation | !OverallDifficulty.IsDefault | FALSE
        // CF: CatchModDifficultyAdjust.ExtendedIconInformation | !DrainRate.IsDefault | TRUE
        [Test]
        public void TestIconInformationDrainRate()
            => Assert.That(new CatchModDifficultyAdjust { DrainRate = { Value = 2 } }.ExtendedIconInformation, Is.EqualTo("HP2"));

        // CF: CatchModDifficultyAdjust.SettingDescription | !CircleSize.IsDefault | FALSE
        // CF: CatchModDifficultyAdjust.SettingDescription | !ApproachRate.IsDefault | FALSE
        // CF: CatchModDifficultyAdjust.SettingDescription | !HardRockOffsets.IsDefault | FALSE
        // CF: CatchModDifficultyAdjust.SettingDescription | foreach base setting | loop 0
        [Test]
        public void TestDescriptionEmptyForDefaults()
            => Assert.That(new CatchModDifficultyAdjust().SettingDescription, Is.Empty);

        // CF: CatchModDifficultyAdjust.SettingDescription | !CircleSize.IsDefault | TRUE
        // CF: CatchModDifficultyAdjust.SettingDescription | foreach base setting | loop n
        // CF: CatchModDifficultyAdjust.SettingDescription | !ApproachRate.IsDefault | TRUE
        // CF: CatchModDifficultyAdjust.SettingDescription | !HardRockOffsets.IsDefault | TRUE
        [Test]
        public void TestDescriptionListsAllChangedSettingsInOrder()
        {
            var mod = new CatchModDifficultyAdjust
            {
                CircleSize = { Value = 3 },
                DrainRate = { Value = 2 },
                ApproachRate = { Value = 8 },
                HardRockOffsets = { Value = true },
            };

            var description = mod.SettingDescription.Select(d => (d.setting.ToString(), d.value.ToString())).ToArray();

            Assert.That(description.First(), Is.EqualTo(("Circle size", "3.0")));
            Assert.That(description.Last(), Is.EqualTo(("Spicy patterns", "On")));
            Assert.That(description, Does.Contain(("Approach rate", "8.0")));
            Assert.That(description.Length, Is.EqualTo(4));
        }

        // CF: CatchModDifficultyAdjust.ApplySettings | CircleSize.Value != null | TRUE
        // CF: CatchModDifficultyAdjust.ApplySettings | ApproachRate.Value != null | TRUE
        [Test]
        public void TestApplyOverridesCircleSizeAndApproachRate()
        {
            var difficulty = new BeatmapDifficulty { CircleSize = 5, ApproachRate = 5 };
            new CatchModDifficultyAdjust { CircleSize = { Value = 2 }, ApproachRate = { Value = 9 } }.ApplyToDifficulty(difficulty);

            Assert.That(difficulty.CircleSize, Is.EqualTo(2));
            Assert.That(difficulty.ApproachRate, Is.EqualTo(9));
        }

        // CF: CatchModDifficultyAdjust.ApplySettings | CircleSize.Value != null | FALSE
        // CF: CatchModDifficultyAdjust.ApplySettings | ApproachRate.Value != null | FALSE
        [Test]
        public void TestUnsetValuesLeaveDifficultyUntouched()
        {
            var difficulty = new BeatmapDifficulty { CircleSize = 5, ApproachRate = 6 };
            new CatchModDifficultyAdjust().ApplyToDifficulty(difficulty);

            Assert.That(difficulty.CircleSize, Is.EqualTo(5));
            Assert.That(difficulty.ApproachRate, Is.EqualTo(6));
        }

        [Test]
        public void TestApplyToBeatmapProcessorCopiesHardRockOffsets()
        {
            var processor = new CatchBeatmapProcessor(new CatchBeatmap());
            new CatchModDifficultyAdjust { HardRockOffsets = { Value = true } }.ApplyToBeatmapProcessor(processor);
            Assert.That(processor.HardRockOffsets, Is.True);
        }
    }
}
