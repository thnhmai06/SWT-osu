// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Osu.Mods;

namespace osu.Game.Rulesets.Osu.Tests.Mods
{
    [TestFixture]
    public class OsuModDifficultyAdjustTest
    {
        private static BeatmapDifficulty original() => new BeatmapDifficulty { CircleSize = 4, ApproachRate = 6, OverallDifficulty = 7, DrainRate = 3 };

        // ---- ApplySettings: `CircleSize.Value != null`, `ApproachRate.Value != null` ----

        [Test]
        public void TestNoOverrides_LeavesDifficultyUntouched()
        {
            var difficulty = original();

            new OsuModDifficultyAdjust().ApplyToDifficulty(difficulty);

            Assert.That(difficulty.CircleSize, Is.EqualTo(4));
            Assert.That(difficulty.ApproachRate, Is.EqualTo(6));
        }

        [Test]
        public void TestCircleSizeOverride_OnlyChangesCircleSize()
        {
            // CS != null TRUE, AR != null FALSE
            var difficulty = original();

            new OsuModDifficultyAdjust { CircleSize = { Value = 8 } }.ApplyToDifficulty(difficulty);

            Assert.That(difficulty.CircleSize, Is.EqualTo(8));
            Assert.That(difficulty.ApproachRate, Is.EqualTo(6));
        }

        [Test]
        public void TestApproachRateOverride_OnlyChangesApproachRate()
        {
            // CS != null FALSE, AR != null TRUE
            var difficulty = original();

            new OsuModDifficultyAdjust { ApproachRate = { Value = 9.5f } }.ApplyToDifficulty(difficulty);

            Assert.That(difficulty.CircleSize, Is.EqualTo(4));
            Assert.That(difficulty.ApproachRate, Is.EqualTo(9.5f));
        }

        [Test]
        public void TestBaseSettingsAreAppliedToo()
        {
            var difficulty = original();

            new OsuModDifficultyAdjust { OverallDifficulty = { Value = 2 }, DrainRate = { Value = 9 } }.ApplyToDifficulty(difficulty);

            Assert.That(difficulty.OverallDifficulty, Is.EqualTo(2));
            Assert.That(difficulty.DrainRate, Is.EqualTo(9));
        }

        // ---- ExtendedIconInformation ----

        [Test]
        public void TestIcon_NothingChanged_Empty()
        {
            // IsExactlyOneSettingChanged FALSE
            Assert.That(new OsuModDifficultyAdjust().ExtendedIconInformation, Is.Empty);
        }

        [Test]
        public void TestIcon_TwoSettingsChanged_Empty()
        {
            Assert.That(new OsuModDifficultyAdjust { CircleSize = { Value = 7 }, ApproachRate = { Value = 8 } }.ExtendedIconInformation, Is.Empty);
        }

        [Test]
        public void TestIcon_OnlyCircleSize() => Assert.That(new OsuModDifficultyAdjust { CircleSize = { Value = 7 } }.ExtendedIconInformation, Does.StartWith("CS"));

        [Test]
        public void TestIcon_OnlyApproachRate() => Assert.That(new OsuModDifficultyAdjust { ApproachRate = { Value = 8 } }.ExtendedIconInformation, Does.StartWith("AR"));

        [Test]
        public void TestIcon_OnlyOverallDifficulty() => Assert.That(new OsuModDifficultyAdjust { OverallDifficulty = { Value = 8 } }.ExtendedIconInformation, Does.StartWith("OD"));

        [Test]
        public void TestIcon_OnlyDrainRate() => Assert.That(new OsuModDifficultyAdjust { DrainRate = { Value = 8 } }.ExtendedIconInformation, Does.StartWith("HP"));

        // ---- SettingDescription ----

        [Test]
        public void TestSettingDescription_DefaultsAreOmitted()
        {
            Assert.That(new OsuModDifficultyAdjust().SettingDescription, Is.Empty);
        }

        [Test]
        public void TestSettingDescription_ListsOnlyChangedSettings_CircleSizeFirstApproachRateLast()
        {
            var mod = new OsuModDifficultyAdjust { CircleSize = { Value = 7 }, ApproachRate = { Value = 8 }, OverallDifficulty = { Value = 6 } };

            var names = mod.SettingDescription.Select(s => s.setting.ToString()).ToList();

            Assert.That(names.First(), Is.EqualTo("Circle size"));
            Assert.That(names.Last(), Is.EqualTo("Approach rate"));
            Assert.That(names.Count, Is.EqualTo(3));
        }

        [Test]
        public void TestSettingDescription_ApproachRateOnly()
        {
            Assert.That(new OsuModDifficultyAdjust { ApproachRate = { Value = 8 } }.SettingDescription.Select(s => s.setting.ToString()), Is.EqualTo(new[] { "Approach rate" }));
        }

        [Test]
        public void TestIncompatibleWithTargetPractice()
        {
            Assert.That(new OsuModDifficultyAdjust().IncompatibleMods, Does.Contain(typeof(OsuModTargetPractice)));
        }
    }
}
