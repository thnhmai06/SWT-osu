// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;

namespace osu.Game.Tests.Rulesets.Mods
{
    [TestFixture]
    public class ModDifficultyAdjustTest
    {
        // CF: ModDifficultyAdjust.ApplySettings | DrainRate.Value != null | FALSE
        // CF: ModDifficultyAdjust.ApplySettings | OverallDifficulty.Value != null | FALSE
        [Test]
        public void TestUnsetSettingsLeaveDifficultyUntouched()
        {
            var difficulty = new BeatmapDifficulty { DrainRate = 3, OverallDifficulty = 4, CircleSize = 5, ApproachRate = 6 };

            new OsuModDifficultyAdjust().ApplyToDifficulty(difficulty);

            Assert.That((difficulty.DrainRate, difficulty.OverallDifficulty, difficulty.CircleSize, difficulty.ApproachRate), Is.EqualTo((3f, 4f, 5f, 6f)));
        }

        // CF: ModDifficultyAdjust.ApplySettings | DrainRate.Value != null | TRUE
        // CF: ModDifficultyAdjust.ApplySettings | OverallDifficulty.Value != null | TRUE
        [Test]
        public void TestSetSettingsOverrideDifficulty()
        {
            var mod = new OsuModDifficultyAdjust();
            mod.DrainRate.Value = 7;
            mod.OverallDifficulty.Value = 8;
            mod.CircleSize.Value = 2;
            mod.ApproachRate.Value = 9;
            var difficulty = new BeatmapDifficulty { DrainRate = 3, OverallDifficulty = 4, CircleSize = 5, ApproachRate = 6 };

            mod.ApplyToDifficulty(difficulty);

            Assert.That((difficulty.DrainRate, difficulty.OverallDifficulty, difficulty.CircleSize, difficulty.ApproachRate), Is.EqualTo((7f, 8f, 2f, 9f)));
        }

        // CF: ModDifficultyAdjust.IsExactlyOneSettingChanged | setting.IsDefault | TRUE
        // CF: ModDifficultyAdjust.IsExactlyOneSettingChanged | changedSetting != null | FALSE
        [Test]
        public void TestNoSettingChangedGivesNoIconInformation()
        {
            Assert.That(new OsuModDifficultyAdjust().ExtendedIconInformation, Is.Empty);
        }

        // CF: ModDifficultyAdjust.IsExactlyOneSettingChanged | setting.IsDefault | FALSE
        // CF: ModDifficultyAdjust.IsExactlyOneSettingChanged | changedSetting != null | TRUE
        [Test]
        public void TestSingleSettingChangedShowsItsValue()
        {
            var mod = new OsuModDifficultyAdjust();
            mod.OverallDifficulty.Value = 8.5f;

            Assert.That(mod.ExtendedIconInformation, Is.EqualTo("OD8.5"));
        }

        [TestCase("CS", 3f)]
        [TestCase("AR", 9.5f)]
        [TestCase("HP", 2f)]
        public void TestIconInformationForEachSetting(string acronym, float value)
        {
            var mod = new OsuModDifficultyAdjust();

            switch (acronym)
            {
                case "CS":
                    mod.CircleSize.Value = value;
                    break;

                case "AR":
                    mod.ApproachRate.Value = value;
                    break;

                case "HP":
                    mod.DrainRate.Value = value;
                    break;
            }

            Assert.That(mod.ExtendedIconInformation, Is.EqualTo($"{acronym}{value:0.#}"));
        }

        // CF: ModDifficultyAdjust.IsExactlyOneSettingChanged | changedSetting != null (second changed setting) | TRUE -> return false
        [Test]
        public void TestSeveralSettingsChangedGivesNoIconInformation()
        {
            var mod = new OsuModDifficultyAdjust();
            mod.OverallDifficulty.Value = 8;
            mod.DrainRate.Value = 2;

            Assert.That(mod.ExtendedIconInformation, Is.Empty);
        }

        [Test]
        public void TestSettingDescriptionOnlyListsChangedSettingsInOrder()
        {
            var mod = new OsuModDifficultyAdjust();
            Assert.That(mod.SettingDescription, Is.Empty);

            mod.ApproachRate.Value = 9;
            mod.DrainRate.Value = 2;
            mod.CircleSize.Value = 3;
            mod.OverallDifficulty.Value = 8;

            Assert.That(mod.SettingDescription.Select(s => s.setting.ToString()), Is.EqualTo(new[] { "Circle size", "HP drain", "Accuracy", "Approach rate" }));
        }

        [Test]
        public void TestExtendedLimitsPropagateToAllDifficultyBindables()
        {
            var mod = new OsuModDifficultyAdjust();

            mod.ExtendedLimits.Value = true;

            Assert.That(new[] { mod.DrainRate, mod.OverallDifficulty, mod.CircleSize, mod.ApproachRate }.Select(b => b.ExtendedLimits.Value), Has.All.True);
        }

        [Test]
        public void TestIncompatibleMods()
        {
            Assert.That(new OsuModDifficultyAdjust().IncompatibleMods, Is.SupersetOf(new[] { typeof(ModEasy), typeof(ModHardRock), typeof(OsuModTargetPractice) }));
        }
    }
}
